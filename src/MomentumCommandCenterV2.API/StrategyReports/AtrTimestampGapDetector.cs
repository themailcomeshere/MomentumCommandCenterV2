namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Counts missing source intervals within the same New York calendar session date.
/// Scheduled overnight, weekend and holiday closures are not missing intraday bars.
/// Full-session coverage is a separate qualification check and must not be inferred here.
/// </summary>
internal static class AtrTimestampGapDetector
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static bool IsUnexpectedGap(DateTimeOffset previousUtc, DateTimeOffset currentUtc, TimeSpan interval)
    {
        if (currentUtc <= previousUtc || interval <= TimeSpan.Zero)
            return false;

        var previousDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(previousUtc, Eastern).DateTime);
        var currentDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(currentUtc, Eastern).DateTime);

        // Bars on different New York dates are separated by a scheduled overnight closure.
        // Missing entire sessions are handled by session-coverage qualification, not this intraday detector.
        return previousDate == currentDate && currentUtc - previousUtc > interval + TimeSpan.FromSeconds(1);
    }
}
