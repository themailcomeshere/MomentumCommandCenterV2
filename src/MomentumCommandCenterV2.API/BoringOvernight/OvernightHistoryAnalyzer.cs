using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: OvernightPair
/// PURPOSE: One overnight observation: buy-session close to the next session's open. BuyWeekday is the weekday of the
/// buy session (the night held). CalendarGapDays &gt; 1 marks a weekend or holiday gap.
/// WHEN CALLED: Produced by <see cref="OvernightHistoryAnalyzer.Analyze"/>.
/// </summary>
public sealed record OvernightPair(
    DateOnly BuySessionDate,
    DateOnly NextSessionDate,
    string BuyWeekday,
    int CalendarGapDays,
    double BuyClose,
    double NextOpen,
    double ReturnPct);

/// <summary>
/// TYPE: OvernightWeekdayStats
/// PURPOSE: Statistics of overnight returns grouped by buy weekday. TStat = mean / (sample std dev / sqrt(n)); with many
/// symbols and weekdays tested at once, |t| near 2 is expected by chance about 1 time in 20, so it is a screen, not proof.
/// WHEN CALLED: Produced by <see cref="OvernightHistoryAnalyzer.Analyze"/>.
/// </summary>
public sealed record OvernightWeekdayStats(
    string BuyWeekday,
    int Count,
    double? WinRatePct,
    double? MeanPct,
    double? MedianPct,
    double? StdDevPct,
    double? TStat);

/// <summary>
/// TYPE: OvernightHistoryResult
/// PURPOSE: Overnight history for one symbol with provenance: every pair, per-weekday statistics, all-nights statistics,
/// and the sessions missing from the source (pairs that would cross a missing session are not formed).
/// WHEN CALLED: Returned by GET /api/research/overnight-history.
/// </summary>
public sealed record OvernightHistoryResult(
    string Symbol,
    string Provider,
    string Feed,
    string FeedCoverage,
    string Adjustment,
    string RequestStatus,
    DateTimeOffset RetrievedAtUtc,
    int ExpectedSessionCount,
    int SessionCount,
    IReadOnlyList<DateOnly> MissingSessionDates,
    int PairsSkippedForMissingSessions,
    DateOnly? FirstObservedSessionDate,
    int PreListingSessionCount,
    IReadOnlyList<OvernightWeekdayStats> ByWeekday,
    OvernightWeekdayStats AllNights,
    IReadOnlyList<OvernightPair> Pairs,
    string Note);

/// <summary>
/// TYPE: OvernightHistoryAnalyzer
/// PURPOSE: Research-only overnight history: r = next open / buy close - 1 for each pair of consecutive expected sessions
/// (the same definition as the installed Overnight Scanner columns), grouped by the buy session's weekday. A pair is
/// formed only when both sessions are present and consecutive in the expected calendar; gaps are never bridged.
/// Results are research only, never READY or a trade instruction.
/// WHEN CALLED: By GET /api/research/overnight-history and unit tests.
/// </summary>
public static class OvernightHistoryAnalyzer
{
    private static readonly string[] WeekdayOrder = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    /// <summary>
    /// PURPOSE: Builds pairs and statistics from the provider's expected and observed sessions.
    /// WHEN CALLED: Once per symbol per request.
    /// </summary>
    public static OvernightHistoryResult Analyze(DailyRecentSessions sessions)
    {
        var byDate = sessions.Sessions.ToDictionary(s => s.SessionDate);
        // Expected sessions before the first observed bar are treated as "not yet listed" (an IPO or new listing),
        // not as missing data. Gaps after the first bar remain missing and are never bridged.
        DateOnly? firstObserved = sessions.Sessions.Count == 0 ? null : sessions.Sessions.Min(s => s.SessionDate);
        var allExpected = sessions.ExpectedSessionDates.OrderBy(d => d).ToArray();
        var preListing = firstObserved is null ? 0 : allExpected.Count(d => d < firstObserved.Value);
        var expected = firstObserved is null ? allExpected : allExpected.Where(d => d >= firstObserved.Value).ToArray();
        var missing = sessions.MissingSessionDates.Where(d => firstObserved is null || d >= firstObserved.Value).OrderBy(d => d).ToArray();
        var pairs = new List<OvernightPair>();
        var skipped = 0;
        for (var i = 1; i < expected.Length; i++)
        {
            if (!byDate.TryGetValue(expected[i - 1], out var buy) || !byDate.TryGetValue(expected[i], out var next))
            {
                skipped++;
                continue;
            }
            if (buy.Close <= 0 || next.Open <= 0) { skipped++; continue; }
            pairs.Add(new OvernightPair(buy.SessionDate, next.SessionDate, buy.SessionDate.DayOfWeek.ToString(),
                next.SessionDate.DayNumber - buy.SessionDate.DayNumber, buy.Close, next.Open,
                Math.Round((next.Open / buy.Close - 1d) * 100d, 6)));
        }

        var byWeekday = WeekdayOrder.Select(day => Stats(day, pairs.Where(p => p.BuyWeekday == day).Select(p => p.ReturnPct).ToArray())).ToArray();
        return new OvernightHistoryResult(sessions.Symbol, sessions.Provider, sessions.Feed, sessions.FeedCoverage, sessions.Adjustment,
            sessions.RequestStatus, sessions.RetrievedAtUtc, expected.Length, sessions.Sessions.Count, missing,
            skipped, firstObserved, preListing, byWeekday, Stats("All", pairs.Select(p => p.ReturnPct).ToArray()), pairs,
            "RESEARCH ONLY. Overnight return = next regular-session open / buy-session close - 1 (Daily bars). Weekday = the buy session's weekday; Friday pairs span the weekend. " +
            "With many symbols and 5 weekdays tested, about 1 in 20 groups shows |t| near 2 by chance: treat weekday effects as hypotheses to confirm out of sample, never as a signal. " +
            (sessions.FeedCoverage == "CONSOLIDATED_SIP" ? "Source: consolidated SIP Daily (delayed historical query)." : "Source is NOT consolidated (IEX partial): diagnostic only."));
    }

    /// <summary>
    /// PURPOSE: Count, win rate (&gt; 0), mean, median, sample std dev and t-stat; null statistics when there are too few values.
    /// WHEN CALLED: By <see cref="Analyze"/> per weekday and for all nights.
    /// </summary>
    public static OvernightWeekdayStats Stats(string label, IReadOnlyList<double> values)
    {
        if (values.Count == 0) return new OvernightWeekdayStats(label, 0, null, null, null, null, null);
        var mean = values.Average();
        var sorted = values.OrderBy(v => v).ToArray();
        var median = sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2d : sorted[sorted.Length / 2];
        double? sd = values.Count > 1 ? Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1)) : null;
        double? t = sd is > 0 ? mean / (sd.Value / Math.Sqrt(values.Count)) : null;
        return new OvernightWeekdayStats(label, values.Count, Math.Round(values.Count(v => v > 0) * 100d / values.Count, 2),
            Math.Round(mean, 4), Math.Round(median, 4), sd is null ? null : Math.Round(sd.Value, 4), t is null ? null : Math.Round(t.Value, 2));
    }
}
