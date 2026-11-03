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
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.False(result.OvernightEvaluationOpen);
    }

    [Fact]
    public void EvaluationWindowOpensOneHourBeforeCloseInArizonaStandardTime()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T20:15:00Z"));
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T21:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.True(result.OvernightEvaluationOpen);
    }

    [Fact]
    public void EvaluationWindowOpensOneHourBeforeCloseInArizonaDaylightTime()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-10-30T19:15:00Z"));
        Assert.Equal(DateTimeOffset.Parse("2026-10-30T19:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-10-30T20:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.True(result.OvernightEvaluationOpen);
    }

    [Fact]
    public void EarlyCloseMovesEvaluationAndBuyDeadlineWithExchangeClose()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-27T17:15:00Z"));
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T17:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T18:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.True(result.OvernightEvaluationOpen);
    }

    [Fact]
    public void AfterHoursCountsDownToEightPmEasternAndTargetsNextSessionForOvernight()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T21:30:00Z"));
        Assert.Equal("AFTER_HOURS", result.Phase);
        Assert.Equal("Remaining time in After-Hours", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T01:00:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T21:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.False(result.OvernightEvaluationOpen);
    }

    [Fact]
    public void PreMarketCountsDownToRegularOpenAndTargetsSameDayEvaluation()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T12:00:00Z"));
        Assert.Equal("PRE_MARKET", result.Phase);
        Assert.Equal("Until Market opens", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T14:30:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightEvaluationStartUtc);
    }

    [Fact]
    public void WeekendCountsDownToNextTradingSession()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-01T16:00:00Z"));
        Assert.Equal("CLOSED", result.Phase);
        Assert.Equal("Until Market opens", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T14:30:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightEvaluationStartUtc);
    }

    [Fact]
    public void ThanksgivingCountsDownToFridayOpen()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-26T18:00:00Z"));
        Assert.Equal("CLOSED", result.Phase);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T14:30:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T17:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T18:00:00Z"), result.OvernightBuyDeadlineUtc);
    }
}
