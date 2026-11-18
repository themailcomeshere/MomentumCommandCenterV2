using System.Collections.Concurrent;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: LiveSignalLogStore
/// PURPOSE: Append-only audit log of live intraday signals (operator request 2026-11-18, after the MWH 08:28 AZ signal
/// could not be verified). Each live assessment is compared with the last one recorded for that symbol in this process;
/// a line is appended to live-signal-log.jsonl only when the no-position state, the holder state or the evaluation
/// status changes (the first observation of a symbol after start-up is recorded as FIRST_SEEN). Lines are never
/// rewritten. Stored in %LOCALAPPDATA%\MomentumCommandCenterV2 (override: MCC_SIGNAL_LOG_DIRECTORY or the constructor).
/// WHEN CALLED: By LiveSymbolAssessmentService after every uncached assessment; read by GET /api/market-data/signal-log.
/// </summary>
public sealed class LiveSignalLogStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _lastKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>PURPOSE: Resolves the log file in MCC's data folder. WHEN CALLED: Once by dependency injection.</summary>
    public LiveSignalLogStore() : this(null) { }

    /// <summary>PURPOSE: Uses an explicit folder (tests) or the default data folder. WHEN CALLED: By DI and tests.</summary>
    public LiveSignalLogStore(string? directory)
    {
        var dir = directory
            ?? Environment.GetEnvironmentVariable("MCC_SIGNAL_LOG_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MomentumCommandCenterV2");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "live-signal-log.jsonl");
    }

    /// <summary>Full path of the log file (shown by the endpoint).</summary>
    public string FilePath => _path;

    /// <summary>
    /// PURPOSE: Appends the observation when its state key differs from the symbol's last recorded key. Returns the
    /// written entry, or null when nothing changed. WHEN CALLED: After each uncached live assessment.
    /// </summary>
    public async Task<LiveSignalLogEntry?> RecordAsync(LiveSignalObservation o, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var key = $"{o.EvaluationStatus}|{o.State}|{o.HeldState}";
        var symbol = o.Symbol.ToUpperInvariant();
        var hadPrevious = _lastKey.TryGetValue(symbol, out var previous);
        if (hadPrevious && previous == key) return null;
        _lastKey[symbol] = key;
        var entry = new LiveSignalLogEntry(nowUtc, hadPrevious ? "CHANGED" : "FIRST_SEEN", previous, o with { Symbol = symbol });
        await _gate.WaitAsync(ct);
        try { await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(entry, Json) + Environment.NewLine, ct); }
        finally { _gate.Release(); }
        return entry;
    }

    /// <summary>
    /// PURPOSE: Entries whose evaluation time falls on the given Eastern date, optionally for one symbol, oldest first.
    /// Damaged lines are skipped, never repaired. WHEN CALLED: By GET /api/market-data/signal-log.
    /// </summary>
    public async Task<IReadOnlyList<LiveSignalLogEntry>> ReadAsync(DateOnly easternDate, string? symbol, CancellationToken ct)
    {
        if (!File.Exists(_path)) return Array.Empty<LiveSignalLogEntry>();
        string[] lines;
        await _gate.WaitAsync(ct);
        try { lines = await File.ReadAllLinesAsync(_path, ct); }
        finally { _gate.Release(); }
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var result = new List<LiveSignalLogEntry>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            LiveSignalLogEntry? e;
            try { e = JsonSerializer.Deserialize<LiveSignalLogEntry>(line, Json); }
            catch (JsonException) { continue; }
            if (e is null) continue;
            if (DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(e.Observation.EvaluatedAtUtc, eastern).DateTime) != easternDate) continue;
            if (!string.IsNullOrWhiteSpace(symbol) && !e.Observation.Symbol.Equals(symbol.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(e);
        }
        return result.OrderBy(e => e.Observation.EvaluatedAtUtc).ToArray();
    }
}

/// <summary>
/// TYPE: LiveSignalObservation
/// PURPOSE: What the live assessment showed for one symbol at one moment: evaluation status, no-position state label
/// (WAIT / PREPARE TO BUY / EXECUTION READY …) and entry flag, holder state label, 5M score and permission, last 1M close,
/// 1M coverage of the last 15 minutes, source bar times, feed and the decision reason (truncated to 400 characters).
/// WHEN CALLED: Built by LiveSymbolAssessmentService; stored by LiveSignalLogStore.
/// </summary>
public sealed record LiveSignalObservation(
    string Symbol,
    DateTimeOffset EvaluatedAtUtc,
    string EvaluationStatus,
    string? State,
    bool? EntryAllowed,
    string? HeldState,
    int? Score,
    bool? FiveMinutePermission,
    decimal? LastClose,
    int? OneMinuteBarsPresent,
    int? OneMinuteBarsPossible,
    DateTimeOffset? OneMinuteBarUtc,
    DateTimeOffset? FiveMinuteBarUtc,
    string? Feed,
    string? Reason);

/// <summary>
/// TYPE: LiveSignalLogEntry
/// PURPOSE: One audit line: when it was written, FIRST_SEEN or CHANGED, the previous state key, and the observation.
/// WHEN CALLED: Written and read by LiveSignalLogStore.
/// </summary>
public sealed record LiveSignalLogEntry(DateTimeOffset LoggedAtUtc, string Change, string? PreviousKey, LiveSignalObservation Observation);
