using System.Text.Json;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: ReadyShadowLogStore
/// PURPOSE: Append-only log of READY evaluations made inside the buy window, used to validate the signal (shadow mode)
/// and to count logged buy sessions before the live signal may be enabled.
/// WHEN CALLED: Written by BoringOvernightReadyService after each in-window evaluation; read by the shadow summary.
/// </summary>
public sealed class ReadyShadowLogStore(string? path = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = path
        ?? Environment.GetEnvironmentVariable("MCC_READY_SHADOW_LOG_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "ready-shadow-log.jsonl");

    /// <summary>
    /// METHOD: AppendAsync
    /// PURPOSE: Appends one entry built from a READY evaluation.
    /// WHEN CALLED: After an evaluation made while the buy window is open.
    /// </summary>
    public async Task AppendAsync(ReadyEvaluation evaluation, CancellationToken ct)
    {
        var entry = new ReadyShadowEntry(Guid.NewGuid().ToString("N"), evaluation.Symbol, evaluation.BuySessionDate,
            evaluation.SellSessionDate, evaluation.EvaluatedAtUtc, evaluation.Status, evaluation.EffectiveMode,
            evaluation.CalculatedLimitSell, evaluation.ReferencePrice, evaluation.TargetReturnPct,
            evaluation.Gates.Where(g => g.State != BoringOvernightReadyEvaluator.GatePass).Select(g => g.Code).ToArray(),
            evaluation.EntryWindow);
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, ct);
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// METHOD: GetAllAsync
    /// PURPOSE: Returns every logged entry in file order.
    /// WHEN CALLED: Shadow summary and logged-session count.
    /// </summary>
    public async Task<IReadOnlyList<ReadyShadowEntry>> GetAllAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<ReadyShadowEntry>();
            return (await File.ReadAllLinesAsync(_path, ct))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonSerializer.Deserialize<ReadyShadowEntry>(line, JsonOptions)
                    ?? throw new InvalidDataException("READY shadow log contains an empty record."))
                .ToArray();
        }
        finally { _gate.Release(); }
    }
}

/// <summary>
/// TYPE: ReadyShadowSummary
/// PURPOSE: Pure grouping of shadow-log entries by symbol and buy session, with the Day 2 outcome for signalled sessions:
/// TOUCHED when the complete Day 2 07:00–20:00 ET SIP window's high reached the limit at the first READY, NOT_TOUCHED
/// when the complete window stayed below it, PENDING before the window has ended (+16 min SIP delay) or before it is
/// captured, INCOMPLETE when the captured window is not complete. A touch is not a fill.
/// WHEN CALLED: GET /api/boring-overnight/ready/shadow-log.
/// </summary>
public static class ReadyShadowSummary
{
    /// <summary>
    /// METHOD: CountLoggedSessions
    /// PURPOSE: Distinct buy sessions with at least one in-window evaluation; gates the live signal.
    /// WHEN CALLED: Before each READY evaluation.
    /// </summary>
    public static int CountLoggedSessions(IReadOnlyList<ReadyShadowEntry> entries) =>
        entries.Select(e => e.BuySessionDate).Distinct().Count();

    /// <summary>
    /// METHOD: Build
    /// PURPOSE: Returns one row per symbol and buy session, newest first.
    /// WHEN CALLED: Shadow summary endpoint; windows maps (symbol, sell date) to the audited SIP window when available.
    /// </summary>
    public static IReadOnlyList<ReadyShadowRow> Build(
        IReadOnlyList<ReadyShadowEntry> entries,
        IReadOnlyDictionary<(string Symbol, DateOnly SellDate), BoringOvernightSellWindowAudit> windows,
        DateTimeOffset nowUtc)
    {
        return entries
            .GroupBy(e => (Symbol: e.Symbol.ToUpperInvariant(), e.BuySessionDate))
            .Select(g =>
            {
                var ordered = g.OrderBy(e => e.EvaluatedAtUtc).ToArray();
                var firstReady = ordered.FirstOrDefault(e => e.Status == "READY" && e.CalculatedLimitSell is not null);
                var sellDate = ordered[0].SellSessionDate;
                string outcome;
                decimal? high = null;
                if (firstReady is null) outcome = "NOT_SIGNALLED";
                else if (nowUtc < SellWindowEndUtc(sellDate) + Hosting.SipDelay) outcome = "PENDING";
                else if (!windows.TryGetValue((g.Key.Symbol, sellDate), out var window)) outcome = "PENDING";
                else if (!window.CoverageComplete || window.HighestEligiblePrice is null) outcome = "INCOMPLETE";
                else
                {
                    high = window.HighestEligiblePrice;
                    outcome = window.HighestEligiblePrice >= firstReady.CalculatedLimitSell ? "TOUCHED" : "NOT_TOUCHED";
                }
                return new ReadyShadowRow(g.Key.Symbol, g.Key.BuySessionDate, sellDate, ordered.Length,
                    firstReady is not null, firstReady?.EvaluatedAtUtc, firstReady?.CalculatedLimitSell,
                    firstReady?.ReferencePrice, firstReady?.TargetReturnPct, ordered[^1].Status, outcome, high,
                    firstReady?.EntryWindow);
            })
            .OrderByDescending(r => r.BuySessionDate).ThenBy(r => r.Symbol)
            .ToArray();
    }

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static DateTimeOffset SellWindowEndUtc(DateOnly sellDate)
    {
        var local = sellDate.ToDateTime(new TimeOnly(20, 0), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>Shared SIP delay alias so the summary uses the same 16-minute cutoff as the capture.</summary>
    private static class Hosting
    {
        public static readonly TimeSpan SipDelay = MomentumCommandCenterV2.API.MarketData.AlpacaDailyMarketDataClient.SipDelay;
    }
}

/// <summary>
/// TYPE: ReadyShadowEntry
/// PURPOSE: One logged in-window READY evaluation. EntryWindow (FINAL_HOUR / LATE / CLOSED; null in entries before
/// 2026-11-17) separates actionable final-hour signals from observe-only late-window ones.
/// WHEN CALLED: Written by ReadyShadowLogStore.
/// </summary>
public sealed record ReadyShadowEntry(
    string EntryId,
    string Symbol,
    DateOnly BuySessionDate,
    DateOnly SellSessionDate,
    DateTimeOffset EvaluatedAtUtc,
    string Status,
    string EffectiveMode,
    decimal? CalculatedLimitSell,
    decimal? ReferencePrice,
    decimal? TargetReturnPct,
    IReadOnlyList<string> NonPassingGates,
    string? EntryWindow = null);

/// <summary>
/// TYPE: ReadyShadowRow
/// PURPOSE: Summary of one symbol and buy session, with the Day 2 outcome when signalled. WindowAtFirstReady says
/// whether the first READY came in the final hour or the observe-only late window (null for older entries).
/// WHEN CALLED: Returned by ReadyShadowSummary.Build.
/// </summary>
public sealed record ReadyShadowRow(
    string Symbol,
    DateOnly BuySessionDate,
    DateOnly SellSessionDate,
    int Evaluations,
    bool Signalled,
    DateTimeOffset? FirstReadyAtUtc,
    decimal? LimitAtFirstReady,
    decimal? ReferencePriceAtFirstReady,
    decimal? TargetReturnPct,
    string LastStatus,
    string Outcome,
    decimal? Day2High,
    string? WindowAtFirstReady = null);
