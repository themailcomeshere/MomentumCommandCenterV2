namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Operator-confirmed intervals in which Thinkorswim displayed no 5-minute candle and
/// the Strategy Report export accurately reflected the chart. These are no-bar periods,
/// not omitted export observations. Entries are exact and deliberately symbol/timeframe scoped.
/// </summary>
internal static class AtrVerifiedNoBarIntervals
{
    private static readonly IReadOnlyList<VerifiedNoBarInterval> Entries =
    [
        new("EVR", "5m", DateTimeOffset.Parse("2026-11-05T09:00:00Z"),
            DateTimeOffset.Parse("2026-11-05T14:00:00Z"), "Operator confirmed no TOS candles; export matches chart", "2026-11-08"),
        new("EVR", "5m", DateTimeOffset.Parse("2026-11-05T14:10:00Z"),
            DateTimeOffset.Parse("2026-11-05T14:25:00Z"), "Operator confirmed no TOS candles; export matches chart", "2026-11-08"),
        new("CBOE", "5m", DateTimeOffset.Parse("2026-11-04T21:05:00Z"),
            DateTimeOffset.Parse("2026-11-04T23:35:00Z"), "Operator confirmed no TOS candles; export matches chart", "2026-11-08")
    ];

    public static IReadOnlyList<VerifiedNoBarInterval> GetFor(string symbol, string timeframe) =>
        Entries.Where(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) &&
            x.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed record VerifiedNoBarInterval(
    string Symbol,
    string Timeframe,
    DateTimeOffset FirstExpectedMissingBarAtUtc,
    DateTimeOffset LastExpectedMissingBarAtUtc,
    string Evidence,
    string ConfirmedOn);
