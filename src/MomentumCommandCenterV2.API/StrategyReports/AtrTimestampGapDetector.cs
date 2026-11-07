namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Counts missing source intervals only when the gap overlaps the same New York calendar
/// date's extended-hours chart session (04:00–20:00 ET). Scheduled overnight closures,
/// including the after-midnight portion before 04:00 ET, are not missing intraday bars.
/// Cross-date continuity and full-session coverage are separate qualification checks.
/// </summary>
internal static class AtrTimestampGapDetector
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeSpan ExtendedSessionStart = new(4, 0, 0);
    private static readonly TimeSpan ExtendedSessionEnd = new(20, 0, 0);

    public static bool IsUnexpectedGap(DateTimeOffset previousUtc, DateTimeOffset currentUtc, TimeSpan interval) =>
        GetUnexpectedGap(previousUtc, currentUtc, interval) is not null;

    public static ThinkorswimAtrTimestampGap? GetUnexpectedGap(
        DateTimeOffset previousUtc,
        DateTimeOffset currentUtc,
        TimeSpan interval)
    {
        if (currentUtc <= previousUtc || interval <= TimeSpan.Zero)
            return null;
        var elapsed = currentUtc - previousUtc;
        if (elapsed <= interval + TimeSpan.FromSeconds(1))
            return null;

        var previousLocal = TimeZoneInfo.ConvertTime(previousUtc, Eastern).DateTime;
        var currentLocal = TimeZoneInfo.ConvertTime(currentUtc, Eastern).DateTime;
        if (DateOnly.FromDateTime(previousLocal) != DateOnly.FromDateTime(currentLocal))
            return null;

        // TOS ETH equity charts do not have a scheduled chart session from 20:00 through
        // 04:00 ET. A timestamp gap wholly inside that closure must not count as a missing
        // 5m/1m chart bar, even though both timestamps can share one New York calendar date.
        var gapStart = previousLocal.TimeOfDay + interval;
        var gapEnd = currentLocal.TimeOfDay;
        if (gapStart >= ExtendedSessionEnd || gapEnd <= ExtendedSessionStart)
            return null;

        var missingIntervals = Math.Max(1, (int)Math.Ceiling((double)elapsed.Ticks / interval.Ticks) - 1);
        return new ThinkorswimAtrTimestampGap(
            previousUtc,
            currentUtc,
            previousUtc.Add(interval),
            currentUtc,
            missingIntervals,
            elapsed - interval);
    }
}
