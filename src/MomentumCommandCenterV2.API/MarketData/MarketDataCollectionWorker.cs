using System.Collections.Concurrent;
using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Opt-in recovery collector. Configure MarketData:Collector:Symbols to enable.
/// Each cycle re-fetches an overlapping recent window; the append-only store deduplicates repeats.
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
    private long _cycles;
    private long _newObservations;
    private int _lastCycleErrors;
    private DateTimeOffset? _lastCycleStartedUtc;
    private DateTimeOffset? _lastCycleCompletedUtc;
    private readonly ConcurrentQueue<string> _recentErrors = new();

    public object Snapshot() => new
    {
        enabled = _symbols.Length > 0,
        configuredSymbols = _symbols,
        intervals = new[] { "1m", "5m" },
        intervalSeconds = _intervalSeconds,
        lookbackMinutes = _lookbackMinutes,
        cycles = Interlocked.Read(ref _cycles),
        newlyPersistedObservations = Interlocked.Read(ref _newObservations),
        lastCycleErrors = Volatile.Read(ref _lastCycleErrors),
        lastCycleStartedUtc = _lastCycleStartedUtc,
        lastCycleCompletedUtc = _lastCycleCompletedUtc,
        recentErrors = _recentErrors.ToArray(),
        mode = "request-overlap historical REST polling; not websocket/live tick streaming"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_symbols.Length == 0)
        {
            logger.LogInformation("Market-data collector is disabled; configure MarketData:Collector:Symbols to enable it.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _lastCycleStartedUtc = DateTimeOffset.UtcNow;
            var errors = 0;
            try
            {
                foreach (var provider in providers.Where(p => p.IsConfigured))
                {
                    foreach (var symbol in _symbols)
                    {
                        foreach (var timeframe in new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5) })
                        {
                            try
                            {
                                var to = DateTimeOffset.UtcNow;
                                var from = to.AddMinutes(-_lookbackMinutes);
                                var bars = await provider.GetBarsAsync(symbol, timeframe, from, to, stoppingToken);
                                var count = await store.AppendAsync(bars, stoppingToken);
                                Interlocked.Add(ref _newObservations, count);
                            }
                            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                            catch (Exception ex)
                            {
                                errors++;
                                var message = $"{provider.ProviderName}/{symbol}/{(timeframe == TimeSpan.FromMinutes(1) ? "1m" : "5m")}: {ex.GetType().Name}: {Trim(ex.Message)}";
                                _recentErrors.Enqueue(message);
                                while (_recentErrors.Count > 20) _recentErrors.TryDequeue(out _);
                                logger.LogWarning("Market-data collection failed: {CollectionError}", message);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            _lastCycleErrors = errors;
            _lastCycleCompletedUtc = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref _cycles);
            try { await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    private static string Trim(string value) => value.Length <= 240 ? value : value[..240];
}

