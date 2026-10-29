using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightHistoryCalculatorTests
{
    [Fact]
    public void CalculatesCloseToNextOpenReturnsAndRollingStatisticsFromExplicitPairs()
    {
        var observations = new List<MarketBarObservation>();
        var boundaries = new List<OvernightSessionBoundary>();
        var start = new DateOnly(2026, 9, 1);
        for (var i = 0; i < 20; i++)
        {
            var previousDate = start.AddDays(i);
            var nextDate = previousDate.AddDays(1);
            var closeTs = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero).AddDays(i);
            var openTs = closeTs.AddHours(17);
            var close = 100m + i;
            var open = close * (i % 4 == 0 ? 0.99m : 1.01m);
            observations.Add(Observation($"close-{i}", closeTs, (double)close, (double)close, "TestProvider"));
            observations.Add(Observation($"open-{i}", openTs, (double)open, (double)open, "TestProvider"));
            boundaries.Add(new(previousDate, nextDate, closeTs, openTs));
        }

        var result = BoringOvernightHistoryCalculator.Calculate(
            "TEST", "TestProvider", "test-feed", observations, boundaries, 20);

        Assert.True(result.AllPairsComplete);
        Assert.True(result.EnoughHistory);
        Assert.Equal(20, result.ValidPairCount);
        Assert.Equal(0, result.MissingPairCount);
        Assert.Equal(20, result.Returns.Count);
        Assert.NotNull(result.WinRatePct);
        Assert.NotNull(result.AverageReturnPct20D);
        Assert.NotNull(result.AverageReturnPct5D);
        Assert.Equal((result.Returns[0].NextOpen / result.Returns[0].PreviousClose - 1m) * 100m,
            result.Returns[0].ReturnPct);
    }

    [Fact]
    public void MissingBoundaryBarFailsClosedAndEmitsNoRollingMetrics()
    {
        var closeTs = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
        var openTs = closeTs.AddHours(17);
        var observations = new[] { Observation("close", closeTs, 100, 100, "TestProvider") };
        var boundaries = new[]
        {
            new OvernightSessionBoundary(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), closeTs, openTs),
            new OvernightSessionBoundary(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3), openTs, openTs.AddDays(1))
        };

        var result = BoringOvernightHistoryCalculator.Calculate(
            "TEST", "TestProvider", "test-feed", observations, boundaries, 5);

        Assert.False(result.AllPairsComplete);
        Assert.False(result.EnoughHistory);
        Assert.Equal(2, result.MissingPairCount);
        Assert.Null(result.WinRatePct);
        Assert.Null(result.AverageReturnPct20D);
        Assert.Equal("PREVIOUS_SESSION_CLOSE_MISSING", result.Missing[1].ReasonCode);
    }

    [Fact]
    public void RejectsDuplicateNextSessionBoundaries()
    {
        var closeTs = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
        var boundary = new OvernightSessionBoundary(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), closeTs, closeTs.AddHours(17));

        Assert.Throws<ArgumentException>(() => BoringOvernightHistoryCalculator.Calculate(
            "TEST", "TestProvider", null, Array.Empty<MarketBarObservation>(),
            new[] { boundary, boundary }, 5));
    }

    [Fact]
    public void RejectsMixedFeedsWhenFeedIsNotSpecified()
    {
        var closeTs = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
        var openTs = closeTs.AddHours(17);
        var iex = Observation("close-iex", closeTs, 100, 100, "TestProvider");
        var sipBar = new MarketBar("TEST", closeTs, 100, 101, 99, 100, 1000, "TestProvider", "sip");
        var sip = new MarketBarObservation("close-sip", closeTs.AddMinutes(2), "TestProvider", "TEST", closeTs, sipBar, "close-sip", null);
        var boundary = new OvernightSessionBoundary(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), closeTs, openTs);

        Assert.Throws<InvalidDataException>(() => BoringOvernightHistoryCalculator.Calculate(
            "TEST", "TestProvider", null, new[] { iex, sip }, new[] { boundary }, 5));
    }

    private static MarketBarObservation Observation(
        string id, DateTimeOffset timestamp, double open, double close, string provider)
    {
        var bar = new MarketBar("TEST", timestamp, open, Math.Max(open, close), Math.Min(open, close),
            close, 1000, provider, "test-feed");
        return new MarketBarObservation(id, timestamp.AddMinutes(1), provider, "TEST", timestamp, bar, id, null);
    }
}
