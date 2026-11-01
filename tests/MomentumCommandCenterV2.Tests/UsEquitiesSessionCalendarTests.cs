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
}
