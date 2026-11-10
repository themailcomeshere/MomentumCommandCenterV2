using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class MarketSessionClockTests
{
    [Fact]
    public void RegularSessionKeepsMarketCountdownAndComputesBuyAndSellWindows()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T15:00:00Z"));
        Assert.Equal("REGULAR", result.Phase);
        Assert.Equal("Remaining time in regular market", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T22:00:00Z"), result.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T00:55:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.Equal(new DateOnly(2026, 11, 2), result.OvernightBuySessionDate);
        Assert.Equal(new DateOnly(2026, 11, 3), result.OvernightSellSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T12:00:00Z"), result.OvernightSellWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-04T01:00:00Z"), result.OvernightSellWindowEndUtc);
        Assert.False(result.OvernightEvaluationOpen);
        Assert.False(result.OvernightBuyWindowOpen);
    }

    [Fact]
    public void ResearchOpensAtOnePmArizonaAndBuyWindowAtThreePmArizona()
    {
        var beforeBuy = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T20:15:00Z"));
        Assert.True(beforeBuy.OvernightEvaluationOpen);
        Assert.False(beforeBuy.OvernightBuyWindowOpen);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:00:00Z"), beforeBuy.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T22:00:00Z"), beforeBuy.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-04T00:55:00Z"), beforeBuy.OvernightBuyDeadlineUtc);

        var insideBuy = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T22:15:00Z"));
        Assert.True(insideBuy.OvernightEvaluationOpen);
        Assert.True(insideBuy.OvernightBuyWindowOpen);
    }

    [Fact]
    public void BuyDeadlineUsesEasternTimezoneAcrossDst()
    {
        var standard = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T22:15:00Z"));
        Assert.Equal(DateTimeOffset.Parse("2026-11-04T00:55:00Z"), standard.OvernightBuyDeadlineUtc);
        var daylight = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-10-30T19:15:00Z"));
        Assert.Equal(new DateOnly(2026, 11, 2), daylight.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T00:55:00Z"), daylight.OvernightBuyDeadlineUtc);
    }

    [Fact]
    public void AfterHoursPhaseAndOvernightBuyWindowAreIndependent()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T22:30:00Z"));
        Assert.Equal("AFTER_HOURS", result.Phase);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T01:00:00Z"), result.TargetUtc);
        Assert.True(result.OvernightEvaluationOpen);
        Assert.True(result.OvernightBuyWindowOpen);
        Assert.Equal(new DateOnly(2026, 11, 2), result.OvernightBuySessionDate);
    }

    [Fact]
    public void ThanksgivingAndFridayAreSkippedForBuyAndNextTradingDayIsSellWindow()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-26T18:00:00Z"));
        Assert.Equal("CLOSED", result.Phase);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T14:30:00Z"), result.TargetUtc);
        Assert.Equal(new DateOnly(2026, 11, 30), result.OvernightBuySessionDate);
        Assert.Equal(new DateOnly(2026, 12, 1), result.OvernightSellSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-12-01T12:00:00Z"), result.OvernightSellWindowStartUtc);
    }

    [Fact]
    public void AfterBuyCutoffMovesToNextEligibleMondayThroughThursday()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T00:56:00Z"));
        Assert.Equal(new DateOnly(2026, 11, 3), result.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-04T00:55:00Z"), result.OvernightBuyDeadlineUtc);
    }
}
