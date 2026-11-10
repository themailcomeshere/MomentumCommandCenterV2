using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Append-only local persistence for raw provider OHLCV observations.
/// WHEN CALLED: The API receives provider bars during comparisons or brief generation.
/// IMPORTANT: Identical observations are idempotent; changed values are appended as corrections.
/// Latest observations are indexed in memory by symbol so reads do not rescan the JSONL file.
/// </summary>
public sealed class JsonlMarketBarObservationStore : IMarketBarObservationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "market-bars.jsonl");

    private readonly Dictionary<(string Provider, string Symbol, string Timeframe, string Feed, DateTimeOffset Timestamp), MarketBarObservation> _latest = new();
    private readonly Dictionary<string, HashSet<(string Provider, string Symbol, string Timeframe, string Feed, DateTimeOffset Timestamp)>> _keysBySymbol = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>
    /// PURPOSE: Store bars while deduplicating identical source observations and retaining corrections.
    /// WHEN CALLED: A provider response is received by an API request.
    /// </summary>
    public async Task<int> AppendAsync(IReadOnlyCollection<MarketBar> bars, CancellationToken cancellationToken)
    {
        if (bars.Count == 0) return 0;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);
            // Validate the entire batch before opening the append stream. A malformed later
            // bar must not leave a partially written batch of otherwise valid observations.
            foreach (var bar in bars) Validate(bar);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var appended = 0;
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            foreach (var bar in bars)
            {
                var normalized = bar with { Symbol = bar.Symbol.ToUpperInvariant(), Timeframe = NormalizeTimeframe(bar.Timeframe) };
                var key = CreateKey(normalized);
                var hash = Hash(normalized);
                if (_latest.TryGetValue(key, out var previous) && previous.ContentHash == hash)
                    continue;

                var observation = new MarketBarObservation(
                    Guid.NewGuid().ToString("N"),
                    DateTimeOffset.UtcNow,
                    normalized.Provider,
                    normalized.Symbol,
                    normalized.Timestamp,
                    normalized,
                    hash,
                    previous?.ObservationId);
                await writer.WriteLineAsync(JsonSerializer.Serialize(observation, JsonOptions).AsMemory(), cancellationToken);
                IndexObservation(key, observation);
                appended++;
            }
            await writer.FlushAsync(cancellationToken);
            return appended;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// PURPOSE: Read the latest retained observation for each source bar in a requested window.
    /// WHEN CALLED: A consumer needs source-backed historical bars for analysis.
    /// IMPORTANT: Corrections remain in JSONL; this query returns the latest version per provider/symbol/timestamp.
    /// The JSONL history is loaded once per process; subsequent reads use the symbol index in memory.
    /// </summary>
    public async Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(
        string symbol,
        string? provider,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken,
        string? timeframe = null,
        string? feed = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.", nameof(symbol));
        if (fromUtc.Offset != TimeSpan.Zero || toUtc.Offset != TimeSpan.Zero || fromUtc > toUtc)
            throw new ArgumentException("fromUtc and toUtc must be UTC timestamps and fromUtc must not exceed toUtc.");
        if (limit is < 1 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1–10000.");

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        var normalizedTimeframe = timeframe is null ? null : NormalizeTimeframe(timeframe);
        var normalizedFeed = string.IsNullOrWhiteSpace(feed) ? null : feed.Trim();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);
            if (!_keysBySymbol.TryGetValue(normalizedSymbol, out var symbolKeys))
                return Array.Empty<MarketBarObservation>();

            return symbolKeys
                .Select(key => _latest[key])
                .Where(observation =>
                    (normalizedProvider is null || observation.Provider.Equals(normalizedProvider, StringComparison.OrdinalIgnoreCase)) &&
                    (normalizedTimeframe is null || NormalizeTimeframe(observation.Bar.Timeframe).Equals(normalizedTimeframe, StringComparison.OrdinalIgnoreCase)) &&
                    (normalizedFeed is null || string.Equals(observation.Bar.Feed, normalizedFeed, StringComparison.OrdinalIgnoreCase)) &&
                    observation.SourceTimestamp >= fromUtc && observation.SourceTimestamp <= toUtc)
                .OrderBy(observation => observation.SourceTimestamp)
                .ThenBy(observation => observation.Provider, StringComparer.OrdinalIgnoreCase)
                .TakeLast(limit)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// PURPOSE: Rebuild the in-memory latest-observation and symbol indexes from append-only JSONL.
    /// WHEN CALLED: The first read or append after process start.
    /// IMPORTANT: This is the only full-file scan in a process lifetime.
    /// </summary>
    private async Task EnsureIndexLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var observation = JsonSerializer.Deserialize<MarketBarObservation>(line, JsonOptions)
                    ?? throw new InvalidDataException("Market bar JSONL contains an empty observation.");
                var key = CreateKey(observation.Bar);
                IndexObservation(key, observation);
            }
        }
        _loaded = true;
    }

    private void IndexObservation(
        (string Provider, string Symbol, string Timeframe, string Feed, DateTimeOffset Timestamp) key,
        MarketBarObservation observation)
    {
        _latest[key] = observation;
        if (!_keysBySymbol.TryGetValue(key.Symbol, out var keys))
        {
            keys = new HashSet<(string Provider, string Symbol, string Timeframe, string Feed, DateTimeOffset Timestamp)>();
            _keysBySymbol[key.Symbol] = keys;
        }
        keys.Add(key);
    }

    private static (string Provider, string Symbol, string Timeframe, string Feed, DateTimeOffset Timestamp) CreateKey(MarketBar bar) =>
        (bar.Provider, bar.Symbol.ToUpperInvariant(), NormalizeTimeframe(bar.Timeframe), bar.Feed ?? string.Empty, bar.Timestamp);

    private static string NormalizeTimeframe(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim().ToLowerInvariant() switch
        {
            "1min" or "1minute" => "1m",
            "5min" or "5minute" => "5m",
            var normalized => normalized
        };

    /// <summary>
    /// PURPOSE: Reject malformed OHLCV instead of silently persisting impossible bars.
    /// WHEN CALLED: Before any new source observation is appended.
    /// </summary>
    private static void Validate(MarketBar bar)
    {
        if (string.IsNullOrWhiteSpace(bar.Provider) || string.IsNullOrWhiteSpace(bar.Symbol))
            throw new InvalidDataException("Provider and symbol are required for raw bar storage.");
        if (bar.Timestamp == default || bar.Volume < 0 ||
            !double.IsFinite(bar.Open) || !double.IsFinite(bar.High) ||
            !double.IsFinite(bar.Low) || !double.IsFinite(bar.Close) ||
            bar.Low > Math.Min(bar.Open, bar.Close) || bar.High < Math.Max(bar.Open, bar.Close) ||
            bar.Low > bar.High)
            throw new InvalidDataException($"Invalid OHLCV bar for {bar.Symbol} at {bar.Timestamp:O}.");
    }

    /// <summary>
    /// PURPOSE: Create a stable content fingerprint for idempotency and correction detection.
    /// WHEN CALLED: Every candidate bar is compared against the latest source observation.
    /// </summary>
    private static string Hash(MarketBar bar)
    {
        var canonical = string.Join("|",
            bar.Provider,
            bar.Symbol.ToUpperInvariant(),
            NormalizeTimeframe(bar.Timeframe),
            bar.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            bar.Open.ToString("R", CultureInfo.InvariantCulture),
            bar.High.ToString("R", CultureInfo.InvariantCulture),
            bar.Low.ToString("R", CultureInfo.InvariantCulture),
            bar.Close.ToString("R", CultureInfo.InvariantCulture),
            bar.Volume.ToString(CultureInfo.InvariantCulture),
            bar.Feed ?? string.Empty,
            bar.SourceTransport ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
