using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class UsEquitiesSessionCalendarTests
{
    [Fact]
    public void BuildsTwentyConsecutiveTradingSessionPairsAndSkipsWeekend()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 2), 20);

        Assert.Equal(20, boundaries.Count);
        Assert.Equal(new DateOnly(2026, 10, 30), boundaries[^1].PreviousSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 2), boundaries[^1].NextSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-10-30T19:59:00Z"), boundaries[^1].PreviousCloseBarTimestampUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-02T14:30:00Z"), boundaries[^1].NextOpenBarTimestampUtc);
        Assert.All(boundaries, x => Assert.True(x.PreviousSessionDate < x.NextSessionDate));
    }


    [Fact]
    public void BuildsFortyChronologicalTrainingAndTestSessionBoundaries()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 2), 40);

        Assert.Equal(40, boundaries.Count);
        Assert.Equal(40, boundaries.Select(x => x.NextSessionDate).Distinct().Count());
        Assert.All(boundaries, x => Assert.True(x.PreviousSessionDate < x.NextSessionDate));
        Assert.Equal(new DateOnly(2026, 11, 2), boundaries[^1].NextSessionDate);
    }

    [Fact]
    public void SkipsThanksgivingAndUsesDayAfterThanksgivingEarlyClose()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 30), 5);

        Assert.Equal(new DateOnly(2026, 11, 27), boundaries[^1].PreviousSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T17:59:00Z"), boundaries[^1].PreviousCloseBarTimestampUtc);
        Assert.DoesNotContain(boundaries.SelectMany(x => new[] { x.PreviousSessionDate, x.NextSessionDate }),
            x => x == new DateOnly(2026, 11, 26));
    }

    [Fact]
    public void SkipsGoodFriday()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 4, 6), 5);

        Assert.DoesNotContain(boundaries.SelectMany(x => new[] { x.PreviousSessionDate, x.NextSessionDate }),
            x => x == new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void UsesChristmasEveEarlyClose()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 12, 28), 5);

        Assert.Equal(new DateOnly(2026, 12, 24), boundaries[^1].PreviousSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-12-24T17:59:00Z"), boundaries[^1].PreviousCloseBarTimestampUtc);
    }
    [Fact]
    public void DoesNotObserveNewYearsDayOnPriorFridayWhenJanuaryFirstIsSaturday()
    {
        Assert.True(UsEquitiesSessionCalendar.IsTradingSession(new DateOnly(2027, 12, 31)));
        Assert.False(UsEquitiesSessionCalendar.IsTradingSession(new DateOnly(2028, 1, 1)));
        Assert.True(UsEquitiesSessionCalendar.IsTradingSession(new DateOnly(2028, 1, 3)));
    }

    [Fact]
    public void TreatsJulyThird2026AsObservedIndependenceDayHoliday()
    {
        Assert.False(UsEquitiesSessionCalendar.IsTradingSession(new DateOnly(2026, 7, 3)));
    }

    [Fact]
    public void UsesJulyThird2028EarlyClose()
    {
        Assert.Equal(new TimeOnly(13, 0), UsEquitiesSessionCalendar.GetRegularClose(new DateOnly(2028, 7, 3)));
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2028, 7, 5), 5);
        var julyThirdClose = Assert.Single(
            boundaries,
            x => x.PreviousSessionDate == new DateOnly(2028, 7, 3));
        Assert.Equal(DateTimeOffset.Parse("2028-07-03T16:59:00Z"), julyThirdClose.PreviousCloseBarTimestampUtc);
    }


    [Fact]
    public void MatchesOfficialNyseHolidayClosuresFor2026Through2028()
    {
        var holidays = new[]
        {
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 19), new DateOnly(2026, 2, 16),
            new DateOnly(2026, 4, 3), new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 19),
            new DateOnly(2026, 7, 3), new DateOnly(2026, 9, 7), new DateOnly(2026, 11, 26),
            new DateOnly(2026, 12, 25),
            new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 18), new DateOnly(2027, 2, 15),
            new DateOnly(2027, 3, 26), new DateOnly(2027, 5, 31), new DateOnly(2027, 6, 18),
            new DateOnly(2027, 7, 5), new DateOnly(2027, 9, 6), new DateOnly(2027, 11, 25),
            new DateOnly(2027, 12, 24),
            new DateOnly(2028, 1, 17), new DateOnly(2028, 2, 21), new DateOnly(2028, 4, 14),
            new DateOnly(2028, 5, 29), new DateOnly(2028, 6, 19), new DateOnly(2028, 7, 4),
            new DateOnly(2028, 9, 4), new DateOnly(2028, 11, 23), new DateOnly(2028, 12, 25)
        };

        Assert.All(holidays, date => Assert.False(UsEquitiesSessionCalendar.IsTradingSession(date), date.ToString()));
    }

    [Fact]
    public void MatchesOfficialNyseEarlyClosesFor2026Through2028()
    {
        var earlyCloses = new[]
        {
            new DateOnly(2026, 11, 27), new DateOnly(2026, 12, 24),
            new DateOnly(2027, 11, 26),
            new DateOnly(2028, 7, 3), new DateOnly(2028, 11, 24)
        };

        Assert.All(earlyCloses, date => Assert.Equal(new TimeOnly(13, 0), UsEquitiesSessionCalendar.GetRegularClose(date)));
    }


    [Fact]
    public void CurrentWindowKeepsTodaysOpeningIntervalSeparateBeforeRegularClose()
    {
        var asOf = DateTimeOffset.Parse("2026-11-12T19:50:00Z"); // 14:50 ET, before 16:00 close.
        var window = UsEquitiesSessionCalendar.BuildCurrentHistoryWindow(asOf, 20);

        Assert.Equal(20, window.HistoricalBoundaries.Count);
        Assert.Equal(new DateOnly(2026, 11, 11), window.HistoricalBoundaries[^1].NextSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 11), window.CurrentIntervalBoundary.PreviousSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 12), window.CurrentIntervalBoundary.NextSessionDate);
        Assert.DoesNotContain(window.HistoricalBoundaries, x => x.NextSessionDate == new DateOnly(2026, 11, 12));
    }

    [Fact]
    public void CurrentWindowRollsForwardAfterRegularClose()
    {
        var asOf = DateTimeOffset.Parse("2026-11-12T21:01:00Z"); // 16:01 ET.
        var window = UsEquitiesSessionCalendar.BuildCurrentHistoryWindow(asOf, 20);

        Assert.Equal(new DateOnly(2026, 11, 12), window.HistoricalBoundaries[^1].NextSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 12), window.CurrentIntervalBoundary.PreviousSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 13), window.CurrentIntervalBoundary.NextSessionDate);
    }

    [Fact]
    public void CurrentWindowUsesDstAdjustedOpenAndCloseBoundaries()
    {
        var asOf = DateTimeOffset.Parse("2026-03-09T13:00:00Z"); // 09:00 EDT after spring-forward.
        var window = UsEquitiesSessionCalendar.BuildCurrentHistoryWindow(asOf, 20);

        Assert.Equal(DateTimeOffset.Parse("2026-03-06T20:59:00Z"),
            window.CurrentIntervalBoundary.PreviousCloseBarTimestampUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-03-09T13:30:00Z"),
            window.CurrentIntervalBoundary.NextOpenBarTimestampUtc);
        Assert.Equal(new DateOnly(2026, 3, 6), window.HistoricalBoundaries[^1].NextSessionDate);
    }

    [Fact]
    public void CurrentWindowUsesNextSessionAfterHolidayAndWeekend()
    {
        var asOf = DateTimeOffset.Parse("2026-11-28T17:00:00Z"); // Saturday after Thanksgiving.
        var window = UsEquitiesSessionCalendar.BuildCurrentHistoryWindow(asOf, 20);

        Assert.Equal(new DateOnly(2026, 11, 27), window.CurrentIntervalBoundary.PreviousSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 30), window.CurrentIntervalBoundary.NextSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T17:59:00Z"),
            window.CurrentIntervalBoundary.PreviousCloseBarTimestampUtc);
    }

    [Fact]
    public void CurrentWindowRollsForwardAtScheduledEarlyClose()
    {
        var asOf = DateTimeOffset.Parse("2026-11-27T18:01:00Z"); // 13:01 ET, after Thanksgiving Friday early close.
        var window = UsEquitiesSessionCalendar.BuildCurrentHistoryWindow(asOf, 20);

        Assert.Equal(new DateOnly(2026, 11, 27), window.HistoricalBoundaries[^1].NextSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 27), window.CurrentIntervalBoundary.PreviousSessionDate);
        Assert.Equal(new DateOnly(2026, 11, 30), window.CurrentIntervalBoundary.NextSessionDate);
        Assert.Equal(DateTimeOffset.Parse("2026-11-27T17:59:00Z"),
            window.CurrentIntervalBoundary.PreviousCloseBarTimestampUtc);
    }
}
