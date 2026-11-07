namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Counts expected source timestamp slots only while the US equity extended-hours chart
/// session is scheduled (04:00–20:00 America/New_York). The scheduled 20:00–04:00 closure
/// is excluded even when a gap crosses part of it on the same New York calendar date.
/// This is a timestamp-continuity diagnostic, not proof that a trade or chart candle existed.
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
        if (currentUtc - previousUtc <= interval + TimeSpan.FromSeconds(1))
            return null;

        var previousLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(previousUtc, Eastern).DateTime);
        var currentLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(currentUtc, Eastern).DateTime);
        if (previousLocalDate != currentLocalDate)
            return null;

        // Walk the expected chart timestamps, not wall-clock elapsed/interval. A gap
        // spanning 00:10–09:05 ET contains 106 wall-clock 5m slots but only 61 slots
        // inside the declared 04:00–20:00 ET chart session.
        var expected = new List<DateTimeOffset>();
        for (var candidate = previousUtc.Add(interval); candidate < currentUtc; candidate = candidate.Add(interval))
        {
            var localTime = TimeZoneInfo.ConvertTime(candidate, Eastern).TimeOfDay;
            if (localTime >= ExtendedSessionStart && localTime < ExtendedSessionEnd)
                expected.Add(candidate);
        }

        if (expected.Count == 0)
            return null;

        return new ThinkorswimAtrTimestampGap(
            previousUtc,
            currentUtc,
            expected[0],
            currentUtc,
            expected.Count,
            currentUtc - previousUtc - interval,
            expected[0],
            expected[^1]);
    }
}
