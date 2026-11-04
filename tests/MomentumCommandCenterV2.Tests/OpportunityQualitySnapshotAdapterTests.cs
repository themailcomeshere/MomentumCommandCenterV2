using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class OpportunityQualitySnapshotAdapterTests
{
    [Fact]
    public void UsesLastThirtyCompletedOneMinuteBarsForRangeAndPriorHighDistance()
    {
        var bars = BuildBars();
        bars[29] = bars[29] with { High = 102d };

        var snapshot = OpportunityQualitySnapshotAdapter.Create(
            "RTO",
            DateTimeOffset.Parse("2026-11-04T19:00:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:59:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:55:00Z"),
            true,
            true,
            2.2m,
            true,
            0.4m,
            2m,
            bars);

        Assert.Equal(1.5m, snapshot.Metrics.RecentRangeInAtr);
        Assert.Equal(0.5m, snapshot.Metrics.ResistanceDistanceInAtr);
        Assert.Equal(30, snapshot.Metrics.RangeLookbackBars);
        Assert.Equal(30, snapshot.Metrics.ResistanceLookbackBars);
        Assert.Equal(31, snapshot.Metrics.AvailableOneMinuteBars);
        Assert.Equal(2.2m, snapshot.Input.RelativeVolume);
        Assert.True(snapshot.Input.ExistingEntryEligible);
        Assert.True(snapshot.Input.ExecutionEventActive);
    }

    [Fact]
    public void DoesNotInventRangeOrResistanceWhenHistoryOrAtrIsInsufficient()
    {
        var shortHistory = BuildBars().Take(20).ToArray();
        var noAtr = OpportunityQualitySnapshotAdapter.Create(
            "RTO",
            DateTimeOffset.Parse("2026-11-04T19:00:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:59:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:55:00Z"),
            false,
            false,
            null,
            false,
            null,
            null,
            shortHistory);

        Assert.Null(noAtr.Input.RecentRangeInAtr);
        Assert.Null(noAtr.Input.ResistanceDistanceInAtr);
        Assert.Null(noAtr.Metrics.OneMinuteAtr);

        var noResistanceBars = BuildBars();
        for (var i = 0; i < noResistanceBars.Count - 1; i++)
            noResistanceBars[i] = noResistanceBars[i] with { High = 99.5d };
        var noResistance = OpportunityQualitySnapshotAdapter.Create(
            "RTO",
            DateTimeOffset.Parse("2026-11-04T19:00:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:59:00Z"),
            DateTimeOffset.Parse("2026-11-04T18:55:00Z"),
            false,
            false,
            1.8m,
            false,
            0.3m,
            2m,
            noResistanceBars);

        Assert.Null(noResistance.Input.ResistanceDistanceInAtr);
        Assert.NotNull(noResistance.Input.RecentRangeInAtr);
    }

    private static List<MarketBar> BuildBars()
    {
        var start = DateTimeOffset.Parse("2026-11-04T18:29:00Z");
        return Enumerable.Range(0, 31)
            .Select(i => new MarketBar(
                "RTO", start.AddMinutes(i), 100d, 101d, 99d, 100d, 1000,
                "Alpaca", "sip", "1m"))
            .ToList();
    }
}
