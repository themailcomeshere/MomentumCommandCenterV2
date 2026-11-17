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
        Assert.Equal("ONE_MINUTE_BAR_OPEN_PROXY", result.Returns[0].NextOpenReferenceKind);
        Assert.False(result.Returns[0].OfficialOpeningAuctionVerified);
        Assert.Contains("Day 1 after-hours and Day 2 premarket", result.ProvenanceNote);
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

    /// <summary>
    /// PURPOSE: Operator decision 2026-11-17. A missing 09:30 bar uses the unadjusted SIP Daily open when the Daily close of
    /// the previous session matches the 15:59 minute close; the pair is labelled and the metrics are emitted. Synthetic data.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void MissingOpenBarUsesDailyOpenWhenBasisMatches()
    {
        var (observations, boundaries) = TwentyPairsWithoutOpenAt(7);
        var daily = DailyFor(boundaries[7], previousClose: 107.2, nextOpen: 108.5);

        var result = BoringOvernightHistoryCalculator.Calculate(
            "TEST", "TestProvider", "test-feed", observations, boundaries, 20, daily);

        Assert.True(result.AllPairsComplete);
        Assert.Equal(20, result.ValidPairCount);
        Assert.NotNull(result.WinRatePct);
        var pair = result.Returns.Single(x => x.NextSessionDate == boundaries[7].NextSessionDate);
        Assert.Equal(BoringOvernightHistoryCalculator.DailyOpenKind, pair.NextOpenReferenceKind);
        Assert.Equal(108.5m, pair.NextOpen);
        Assert.Equal(107m, pair.PreviousClose); // the close stays the 15:59 minute bar
        Assert.Equal("DAILY:" + boundaries[7].NextSessionDate.ToString("yyyy-MM-dd"), pair.NextOpenObservationId);
        Assert.Contains("DAILY OPEN USED", result.ProvenanceNote);
        Assert.Equal(19, result.Returns.Count(x => x.NextOpenReferenceKind == BoringOvernightHistoryCalculator.OneMinuteOpenKind));
    }

    /// <summary>
    /// PURPOSE: The Daily open is refused when the Daily close differs from the minute close by more than the tolerance
    /// (a different price basis, for example adjusted bars), when the Daily bar is absent, and when no fallback is supplied.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void DailyOpenIsRefusedOnBasisMismatchOrMissingDailyBar()
    {
        var (observations, boundaries) = TwentyPairsWithoutOpenAt(7);

        var mismatch = BoringOvernightHistoryCalculator.Calculate("TEST", "TestProvider", "test-feed", observations, boundaries, 20,
            DailyFor(boundaries[7], previousClose: 107 * 1.02, nextOpen: 108.5));
        Assert.Null(mismatch.WinRatePct);
        Assert.Equal("NEXT_SESSION_OPEN_MISSING_DAILY_BASIS_MISMATCH", Assert.Single(mismatch.Missing).ReasonCode);

        var noNextDaily = DailyFor(boundaries[7], previousClose: 107, nextOpen: 108.5);
        noNextDaily.Remove(boundaries[7].NextSessionDate);
        var absent = BoringOvernightHistoryCalculator.Calculate("TEST", "TestProvider", "test-feed", observations, boundaries, 20, noNextDaily);
        Assert.Equal("NEXT_SESSION_OPEN_MISSING", Assert.Single(absent.Missing).ReasonCode);

        var none = BoringOvernightHistoryCalculator.Calculate("TEST", "TestProvider", "test-feed", observations, boundaries, 20);
        Assert.Equal("NEXT_SESSION_OPEN_MISSING", Assert.Single(none.Missing).ReasonCode);
        Assert.Null(none.WinRatePct);
    }

    private static (List<MarketBarObservation>, List<OvernightSessionBoundary>) TwentyPairsWithoutOpenAt(int skip)
    {
        var observations = new List<MarketBarObservation>();
        var boundaries = new List<OvernightSessionBoundary>();
        for (var i = 0; i < 20; i++)
        {
            var closeTs = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero).AddDays(i);
            var openTs = closeTs.AddHours(17);
            observations.Add(Observation($"close-{i}", closeTs, 100 + i, 100 + i, "TestProvider"));
            if (i != skip) observations.Add(Observation($"open-{i}", openTs, 100.5 + i, 100.5 + i, "TestProvider"));
            boundaries.Add(new(new DateOnly(2026, 9, 1).AddDays(i), new DateOnly(2026, 9, 2).AddDays(i), closeTs, openTs));
        }
        return (observations, boundaries);
    }

    private static Dictionary<DateOnly, DailySessionObservation> DailyFor(OvernightSessionBoundary boundary, double previousClose, double nextOpen) => new()
    {
        [boundary.PreviousSessionDate] = new(boundary.PreviousSessionDate, boundary.PreviousCloseBarTimestampUtc, previousClose, previousClose, previousClose, previousClose, 1000),
        [boundary.NextSessionDate] = new(boundary.NextSessionDate, boundary.NextOpenBarTimestampUtc, nextOpen, nextOpen, nextOpen, nextOpen, 1000)
    };

    private static MarketBarObservation Observation(
        string id, DateTimeOffset timestamp, double open, double close, string provider)
    {
        var bar = new MarketBar("TEST", timestamp, open, Math.Max(open, close), Math.Min(open, close),
            close, 1000, provider, "test-feed");
        return new MarketBarObservation(id, timestamp.AddMinutes(1), provider, "TEST", timestamp, bar, id, null);
    }
}
