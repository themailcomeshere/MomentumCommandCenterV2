using System.Collections.Concurrent;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Collect configured symbols using overlap polling plus durable per-provider/timeframe watermarks.
/// WHEN CALLED: Hosted API worker runs each collection cycle and recovers historical bars after restart/outage.
/// IMPORTANT: Explicit-symbol historical REST polling only; not a market-wide scanner or websocket stream.
/// </summary>
public sealed class MarketDataCollectionWorker(
    IConfiguration configuration,
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore store,
    ILogger<MarketDataCollectionWorker> logger) : BackgroundService
{
    private readonly string[] _symbols = (configuration["MarketData:Collector:Symbols"] ?? "")
        .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.ToUpperInvariant()).Where(x => x.Length <= 8 && x.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private readonly int _intervalSeconds = Math.Clamp(configuration.GetValue("MarketData:Collector:IntervalSeconds", 60), 15, 3600);
    private readonly int _lookbackMinutes = Math.Clamp(configuration.GetValue("MarketData:Collector:LookbackMinutes", 20), 5, 1440);
    private readonly int _overlapMinutes = Math.Clamp(configuration.GetValue("MarketData:Collector:OverlapMinutes", 5), 1, 60);
    private readonly string _checkpointPath = Environment.GetEnvironmentVariable("MCC_MARKET_COLLECTOR_CHECKPOINT_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "collector-watermarks.json");
    private readonly Dictionary<string, DateTimeOffset> _watermarks = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _checkpointGate = new(1, 1);
    private long _cycles;
    private long _newObservations;
    private int _lastCycleErrors;
    private DateTimeOffset? _lastCycleStartedUtc;
    private DateTimeOffset? _lastCycleCompletedUtc;
    private readonly ConcurrentQueue<string> _recentErrors = new();

    /// <summary>
    /// PURPOSE: Expose operational state without representing a configured worker as a healthy feed.
    /// WHEN CALLED: GET /api/market-data/collector/status.
    /// </summary>
    public object Snapshot() => new
    {
        enabled = _symbols.Length > 0,
        configuredSymbols = _symbols,
        configuredProviders = providers.Where(p => p.IsConfigured).Select(p => p.ProviderName).ToArray(),
        intervals = new[] { "1m", "5m" },
        intervalSeconds = _intervalSeconds,
        initialLookbackMinutes = _lookbackMinutes,
        overlapMinutes = _overlapMinutes,
        checkpointPath = _checkpointPath,
        watermarks = _watermarks.Select(kvp => new { key = kvp.Key, sourceTimestampUtc = kvp.Value }).ToArray(),
        cycles = Interlocked.Read(ref _cycles),
        newlyPersistedObservations = Interlocked.Read(ref _newObservations),
        lastCycleErrors = Volatile.Read(ref _lastCycleErrors),
        lastCycleStartedUtc = _lastCycleStartedUtc,
        lastCycleCompletedUtc = _lastCycleCompletedUtc,
        recentErrors = _recentErrors.ToArray(),
        mode = "durable-watermark historical REST polling with bounded backfill chunks; not websocket/live tick streaming"
    };

    /// <summary>
    /// PURPOSE: Run repeated collection cycles and resume each stream from its last persisted source timestamp.
    /// WHEN CALLED: ASP.NET Core starts the hosted service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_symbols.Length == 0)
        {
            logger.LogInformation("Market-data collector is disabled; configure MarketData:Collector:Symbols to enable it.");
            return;
        }

        try { await LoadCheckpointsAsync(stoppingToken); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            RecordError($"checkpoint-load: {ex.GetType().Name}: {Trim(ex.Message)}");
            logger.LogError(ex, "Collector checkpoint could not be loaded; fresh symbols will use the configured initial lookback.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _lastCycleStartedUtc = DateTimeOffset.UtcNow;
            var errors = 0;
            var configuredProviders = providers.Where(p => p.IsConfigured).ToArray();
            if (configuredProviders.Length == 0)
            {
                errors++;
                RecordError("No market-data providers are configured; collector cycle did not retrieve data.");
            }

            foreach (var provider in configuredProviders)
            {
                foreach (var symbol in _symbols)
                {
                    foreach (var timeframe in new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5) })
                    {
                        var key = WatermarkKey(provider.ProviderName, symbol, timeframe);
                        try
                        {
                            var to = DateTimeOffset.UtcNow;
                            var initialFrom = to.AddMinutes(-_lookbackMinutes);
                            var from = _watermarks.TryGetValue(key, out var watermark)
                                ? watermark.AddMinutes(-_overlapMinutes)
                                : initialFrom;

                            // Provider REST adapters cap requests at 31 days. Catch up in 30-day chunks
                            // so a long outage cannot silently skip the middle of the missing interval.
                            if (to - from > TimeSpan.FromDays(30))
                                to = from.AddDays(30);

                            var bars = await provider.GetBarsAsync(symbol, timeframe, from, to, stoppingToken);
                            var ordered = bars.OrderBy(b => b.Timestamp).ToArray();
                            var newlyPersisted = await store.AppendAsync(ordered, stoppingToken);
                            Interlocked.Add(ref _newObservations, newlyPersisted);
                            if (ordered.Length > 0)
                            {
                                var latest = ordered[^1].Timestamp;
                                if (!_watermarks.TryGetValue(key, out var previous) || latest > previous)
                                {
                                    _watermarks[key] = latest;
                                    await SaveCheckpointsAsync(stoppingToken);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                        catch (Exception ex)
                        {
                            errors++;
                            var message = $"{key}: {ex.GetType().Name}: {Trim(ex.Message)}";
                            RecordError(message);
                            logger.LogWarning("Market-data collection failed: {CollectionError}", message);
                        }
                    }
                }
            }

            _lastCycleErrors = errors;
            _lastCycleCompletedUtc = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref _cycles);
            try { await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    /// <summary>
    /// PURPOSE: Restore source-timestamp watermarks after an API process restart.
    /// WHEN CALLED: Once before the first collector cycle.
    /// </summary>
    private async Task LoadCheckpointsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_checkpointPath)) return;
        await using var stream = new FileStream(_checkpointPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, DateTimeOffset>>(stream, cancellationToken: cancellationToken);
        if (loaded is null) throw new InvalidDataException("Collector checkpoint file was empty or invalid.");
        foreach (var item in loaded) _watermarks[item.Key] = item.Value;
    }

    /// <summary>
    /// PURPOSE: Atomically save the latest source timestamps only after bars were successfully persisted.
    /// WHEN CALLED: A symbol/timeframe/provider returns non-empty bars newer than its previous watermark.
    /// </summary>
    private async Task SaveCheckpointsAsync(CancellationToken cancellationToken)
    {
        await _checkpointGate.WaitAsync(cancellationToken);
        var temporaryPath = _checkpointPath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_checkpointPath))!);
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                16 * 1024, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, _watermarks, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, _checkpointPath, true);
        }
        finally
        {
            _checkpointGate.Release();
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch (IOException) { }
        }
    }

    private static string WatermarkKey(string provider, string symbol, TimeSpan timeframe) =>
        $"{provider}/{symbol}/{(timeframe == TimeSpan.FromMinutes(1) ? "1m" : "5m")}";

    private void RecordError(string message)
    {
        _recentErrors.Enqueue(message);
        while (_recentErrors.Count > 20) _recentErrors.TryDequeue(out _);
    }

    private static string Trim(string value) => value.Length <= 240 ? value : value[..240];
}
