using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class MarketSessionClockTests
{
    [Fact]
    public void RegularSessionCountsDownToEasternCloseAfterDstEnds()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T15:00:00Z"));
        Assert.Equal("REGULAR", result.Phase);
        Assert.Equal("Remaining time in regular market", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), result.TargetUtc);
    }

    [Fact]
    public void AfterHoursCountsDownToEightPmEastern()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T21:30:00Z"));
        Assert.Equal("AFTER_HOURS", result.Phase);
        Assert.Equal("Remaining time in After-Hours", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T01:00:00Z"), result.TargetUtc);
    }

    [Fact]
    public void PreMarketCountsDownToRegularOpen()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T12:00:00Z"));
        Assert.Equal("PRE_MARKET", result.Phase);
        Assert.Equal("Until Market opens", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T14:30:00Z"), result.TargetUtc);
    }

    [Fact]
    public void WeekendCountsDownToNextTradingSession()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-01T16:00:00Z"));
        Assert.Equal("CLOSED", result.Phase);
        Assert.Equal("Until Market opens", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T14:30:00Z"), result.TargetUtc);
    }

    [Fact]
    public void ThanksgivingCountsDownToFridayOpen()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-26T18:00:00Z"));
        Assert.Equal("CLOSED", result.Phase);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T14:30:00Z"), result.TargetUtc);
    }
}
