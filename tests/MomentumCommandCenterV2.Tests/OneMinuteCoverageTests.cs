using MomentumCommandCenterV2.API.MarketData;
using Xunit;
using MarketBar = MomentumCommandCenterV2.API.MarketData.MarketBar;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OneMinuteCoverageTests
/// PURPOSE: Verifies the 1M coverage counter behind the V2 coverage gate (2026-11-18): the window is the last 15 completed
/// minutes before asOf, the still-forming minute is excluded, pre-market minutes are not counted as possible, and
/// duplicate timestamps count once. Synthetic bars (2026-11-18, Eastern standard time: 09:30 ET = 14:30 UTC).
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OneMinuteCoverageTests
{
    private static MarketBar Bar(DateTimeOffset start) => new("TEST", start, 10, 10, 10, 10, 100, "Alpaca", "iex");

    /// <summary>PURPOSE: Mid-session window with gaps, a forming minute and a duplicate. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void CountsLastFifteenCompletedMinutes()
    {
        var asOf = DateTimeOffset.Parse("2026-11-18T15:28:30Z");               // 08:28:30 AZ
        var minutes = new[] { 15, 16, 19, 20, 21, 22, 23, 24 };                 // MWH-like: 8 of 15
        var bars = minutes.Select(m => Bar(DateTimeOffset.Parse("2026-11-18T15:00:00Z").AddMinutes(m))).ToList();
        bars.Add(Bar(DateTimeOffset.Parse("2026-11-18T15:24:00Z")));            // duplicate counts once
        bars.Add(Bar(DateTimeOffset.Parse("2026-11-18T15:28:00Z")));            // forming minute: excluded
        bars.Add(Bar(DateTimeOffset.Parse("2026-11-18T15:12:00Z")));            // before the window: excluded

        var (present, possible) = LiveSymbolTechnicalCalculator.OneMinuteCoverage(bars, asOf);

        Assert.Equal(15, possible);
        Assert.Equal(8, present);
    }

    /// <summary>PURPOSE: Right after the open only session minutes are possible. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FirstMinutesCountOnlySessionMinutes()
    {
        var asOf = DateTimeOffset.Parse("2026-11-18T14:35:10Z");               // 09:35 ET
        var bars = new[] { 14_25, 14_30, 14_31, 14_32, 14_33 }.Select(hm =>
            Bar(new DateTimeOffset(2026, 11, 18, hm / 100, hm % 100, 0, TimeSpan.Zero))).ToList();   // 14:25 is pre-market

        var (present, possible) = LiveSymbolTechnicalCalculator.OneMinuteCoverage(bars, asOf);

        Assert.Equal(5, possible);    // 14:30–14:34
        Assert.Equal(4, present);
    }
}
