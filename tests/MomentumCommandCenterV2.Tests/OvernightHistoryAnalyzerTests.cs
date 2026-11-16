using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OvernightHistoryAnalyzerTests
/// PURPOSE: Verifies the research overnight-history analyzer: open/previous-close returns, buy-weekday grouping, the
/// weekend gap, no bridging across a missing session, and the statistics. All sessions are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OvernightHistoryAnalyzerTests
{
    /// <summary>PURPOSE: Builds a session with the given open and close. WHEN CALLED: By tests.</summary>
    private static DailySessionObservation S(DateOnly date, double open, double close) =>
        new(date, new DateTimeOffset(date.ToDateTime(new TimeOnly(5, 0)), TimeSpan.Zero), open, Math.Max(open, close) + 1, Math.Min(open, close) - 1, close, 1_000_000);

    /// <summary>PURPOSE: Wraps sessions as a provider result. WHEN CALLED: By tests.</summary>
    private static DailyRecentSessions Wrap(IReadOnlyList<DateOnly> expected, IReadOnlyList<DailySessionObservation> sessions) =>
        new("TSTA", "Alpaca", "sip", "CONSOLIDATED_SIP", "split", DateTimeOffset.UtcNow, "TEST", expected,
            expected.Where(d => sessions.All(s => s.SessionDate != d)).ToArray(), sessions);

    // Mon 2026-11-09 .. Mon 2026-11-16 (8 calendar days, 6 sessions).
    private static readonly DateOnly[] Week =
    {
        new(2026, 11, 9), new(2026, 11, 10), new(2026, 11, 11), new(2026, 11, 12), new(2026, 11, 13), new(2026, 11, 16)
    };

    /// <summary>PURPOSE: Returns, weekday labels and the Friday weekend gap. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Pairs_UseNextOpenOverBuyClose_ByBuyWeekday()
    {
        var sessions = new[] { S(Week[0], 99, 100), S(Week[1], 101, 102), S(Week[2], 101.98, 103), S(Week[3], 103, 104), S(Week[4], 104, 105), S(Week[5], 106.05, 106) };
        var r = OvernightHistoryAnalyzer.Analyze(Wrap(Week, sessions));

        Assert.Equal(5, r.Pairs.Count);
        Assert.Equal("Monday", r.Pairs[0].BuyWeekday);
        Assert.Equal(1.0, r.Pairs[0].ReturnPct, 6);                    // 101 / 100 - 1
        Assert.Equal(-0.019608, r.Pairs[1].ReturnPct, 5);              // 101.98 / 102 - 1
        Assert.Equal("Friday", r.Pairs[4].BuyWeekday);
        Assert.Equal(3, r.Pairs[4].CalendarGapDays);                   // Fri -> Mon
        Assert.Equal(1.0, r.Pairs[4].ReturnPct, 6);                    // 106.05 / 105 - 1
        Assert.Equal(1, r.ByWeekday.Single(w => w.BuyWeekday == "Friday").Count);
        Assert.Equal(5, r.AllNights.Count);
    }

    /// <summary>PURPOSE: A missing expected session is not bridged: both pairs touching it are skipped. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void MissingSession_IsNotBridged()
    {
        var sessions = new[] { S(Week[0], 99, 100), S(Week[1], 101, 102), S(Week[3], 103, 104), S(Week[4], 104, 105), S(Week[5], 106, 106) };
        var r = OvernightHistoryAnalyzer.Analyze(Wrap(Week, sessions));

        Assert.Equal(new[] { Week[2] }, r.MissingSessionDates);
        Assert.Equal(2, r.PairsSkippedForMissingSessions);
        Assert.Equal(3, r.Pairs.Count);
        Assert.DoesNotContain(r.Pairs, p => p.BuySessionDate == Week[1] && p.NextSessionDate == Week[3]);
    }

    /// <summary>PURPOSE: Statistics: win rate, mean, median, sample std dev and t-stat. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Stats_AreComputedCorrectly()
    {
        var s = OvernightHistoryAnalyzer.Stats("Monday", new[] { 1.0, -1.0, 2.0, 2.0 });

        Assert.Equal(4, s.Count);
        Assert.Equal(75.0, s.WinRatePct);
        Assert.Equal(1.0, s.MeanPct);
        Assert.Equal(1.5, s.MedianPct);
        Assert.Equal(1.4142, s.StdDevPct!.Value, 4);                    // sqrt(6/3)
        Assert.Equal(1.41, s.TStat!.Value, 2);                          // 1 / (1.4142 / 2)
        Assert.Null(OvernightHistoryAnalyzer.Stats("Friday", Array.Empty<double>()).MeanPct);
    }
}
