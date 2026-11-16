using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: V3ColumnEvaluation
/// PURPOSE: Response of GET /api/boring-overnight/v3-column: the V3 calculation from MCC's Daily source plus its
/// provenance, so the operator can compare it with the Thinkorswim V3 columns for the same symbol and session.
/// WHEN CALLED: Built by <see cref="BoringOvernightV3ColumnEvaluation.FromSessions"/>.
/// </summary>
public sealed record V3ColumnEvaluation(
    string Symbol,
    bool ParityComparable,
    V3ColumnResult Calculation,
    string Provider,
    string Feed,
    string FeedCoverage,
    string Adjustment,
    string RequestStatus,
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<DateOnly> SessionDates,
    IReadOnlyList<DateOnly> MissingSessionDates,
    string ParityInstructions);

/// <summary>
/// TYPE: BoringOvernightV3ColumnEvaluation
/// PURPOSE: Turns the latest 21 expected completed Daily sessions into a V3 column result. A missing expected session
/// or a non-consolidated feed returns DATA? without metrics: gaps are never bridged and IEX volume is never treated
/// as consolidated.
/// WHEN CALLED: By GET /api/boring-overnight/v3-column and unit tests.
/// </summary>
public static class BoringOvernightV3ColumnEvaluation
{
    /// <summary>
    /// PURPOSE: Maps provider sessions to a V3 evaluation.
    /// WHEN CALLED: Once per request after <see cref="AlpacaDailyMarketDataClient.GetRecentSessionsAsync"/>.
    /// </summary>
    public static V3ColumnEvaluation FromSessions(DailyRecentSessions sessions)
    {
        var bars = sessions.Sessions
            .Select(s => new V3DailyBar(s.SessionDate, (decimal)s.Open, (decimal)s.High, (decimal)s.Low, (decimal)s.Close, s.Volume))
            .ToList();
        var consolidated = sessions.FeedCoverage == "CONSOLIDATED_SIP";

        V3ColumnResult calculation;
        if (sessions.MissingSessionDates.Count > 0)
            calculation = Unknown(bars, "DATA_SESSION_MISSING",
                "Expected session(s) missing from the provider: " + string.Join(", ", sessions.MissingSessionDates.Select(d => d.ToString("yyyy-MM-dd"))) + ". Gaps are not bridged.");
        else if (!consolidated)
            calculation = Unknown(bars, "DATA_FEED_NOT_CONSOLIDATED",
                $"Feed {sessions.Feed} ({sessions.FeedCoverage}) is partial-exchange; its volume is not comparable with Thinkorswim.");
        else
            calculation = BoringOvernightV3Calculator.Calculate(bars);

        return new V3ColumnEvaluation(sessions.Symbol, consolidated && sessions.MissingSessionDates.Count == 0 && calculation.Status != "DATA?",
            calculation, sessions.Provider, sessions.Feed, sessions.FeedCoverage, sessions.Adjustment, sessions.RequestStatus,
            sessions.RetrievedAtUtc, sessions.Sessions.Select(s => s.SessionDate).ToArray(), sessions.MissingSessionDates, ParityInstructions);
    }

    /// <summary>PURPOSE: A DATA? calculation with a source-level reason. WHEN CALLED: For missing sessions or a partial feed.</summary>
    private static V3ColumnResult Unknown(IReadOnlyList<V3DailyBar> bars, string reason, string detail) =>
        new(BoringOvernightV3Calculator.Version, BoringOvernightV3Calculator.ScoreMapVersion,
            bars.Count > 0 ? bars[^1].SessionDate : null, bars.Count, "DATA?", 0, reason, new[] { reason },
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            Array.Empty<V3ScoreComponent>(), detail);

    private const string ParityInstructions =
        "Compare after the regular close (MCC uses completed sessions only; Thinkorswim includes the forming bar during the session). " +
        "MCC source: Alpaca SIP Daily, split-adjusted, request end at least 16 minutes old. In Thinkorswim use the V3 columns with " +
        "Aggregation D, extended hours off. Expect small differences if Thinkorswim adjusts for dividends or its daily open differs from SIP.";
}
