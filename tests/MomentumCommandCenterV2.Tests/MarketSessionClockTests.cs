using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class MarketSessionClockTests
{
    [Fact]
    public void RegularSessionTargetsRegularCloseAndFinalHourBuyWindow()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T15:00:00Z"));
        Assert.Equal("REGULAR", result.Phase);
        Assert.Equal("Remaining time in regular market", result.Label);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), result.TargetUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightEvaluationStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), result.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.Equal(new DateOnly(2026, 11, 2), result.OvernightBuySessionDate);
        Assert.Equal(new DateOnly(2026, 11, 3), result.OvernightSellSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T12:00:00Z"), result.OvernightSellWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-04T01:00:00Z"), result.OvernightSellWindowEndUtc);
        Assert.False(result.OvernightEvaluationOpen);
        Assert.False(result.OvernightBuyWindowOpen);
    }

    [Fact]
    public void CandidateResearchAndBuyOpenDuringFinalHourInArizonaTime()
    {
        // 20:15Z = 1:15 PM Arizona / 3:15 PM Eastern in November.
        var insideFinalHour = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T20:15:00Z"));
        Assert.True(insideFinalHour.OvernightEvaluationOpen);
        Assert.True(insideFinalHour.OvernightBuyWindowOpen);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:00:00Z"), insideFinalHour.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T21:00:00Z"), insideFinalHour.OvernightBuyDeadlineUtc);

        var beforeFinalHour = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T19:59:00Z"));
        Assert.False(beforeFinalHour.OvernightEvaluationOpen);
        Assert.False(beforeFinalHour.OvernightBuyWindowOpen);
    }

    [Fact]
    public void BuyWindowTracksEasternCloseAcrossDaylightSavingTime()
    {
        // During EDT, 3:00 PM Eastern is noon Arizona; close is 1:00 PM Arizona.
        var daylight = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-10-29T19:15:00Z"));
        Assert.Equal(new DateOnly(2026, 10, 29), daylight.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-10-29T19:00:00Z"), daylight.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-10-29T20:00:00Z"), daylight.OvernightBuyDeadlineUtc);
        Assert.True(daylight.OvernightBuyWindowOpen);

        // Friday is not a buy day; the next eligible buy day is Monday after the DST transition.
        var friday = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-10-30T19:15:00Z"));
        Assert.Equal(new DateOnly(2026, 11, 2), friday.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T20:00:00Z"), friday.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T21:00:00Z"), friday.OvernightBuyDeadlineUtc);
    }

    [Fact]
    public void AfterHoursIsPartOfOvernightMeasurementButBuyWindowHasClosed()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-02T22:30:00Z"));
        Assert.Equal("AFTER_HOURS", result.Phase);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T01:00:00Z"), result.TargetUtc);
        Assert.False(result.OvernightEvaluationOpen);
        Assert.False(result.OvernightBuyWindowOpen);
        Assert.Equal(new DateOnly(2026, 11, 3), result.OvernightBuySessionDate);
        Assert.Contains("Buy Day after-hours", result.OvernightScheduleNote);
    }

    [Fact]
    public void EarlyCloseMovesFinalHourAndBuyDeadlineToActualRegularClose()
    {
        // Christmas Eve 2026 is a Thursday early-close session: 1 PM Eastern close.
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-12-24T17:30:00Z"));
        Assert.Equal(new DateOnly(2026, 12, 24), result.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-12-24T17:00:00Z"), result.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-12-24T18:00:00Z"), result.OvernightBuyDeadlineUtc);
        Assert.True(result.OvernightBuyWindowOpen);
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
    public void AfterRegularCloseMovesToNextEligibleMondayThroughThursday()
    {
        var result = MarketSessionClock.GetSnapshot(DateTimeOffset.Parse("2026-11-03T00:56:00Z"));
        Assert.Equal(new DateOnly(2026, 11, 3), result.OvernightBuySessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:00:00Z"), result.OvernightBuyWindowStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T21:00:00Z"), result.OvernightBuyDeadlineUtc);
    }
}
