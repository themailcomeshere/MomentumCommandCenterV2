using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightSellWindowHistoryServiceTests
{
    [Fact]
    public async Task BuildsTwentyCompleteExactEtWindowsFromPersistedBars()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 20);
        var bars = BuildBars(boundaries);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.AllWindowsComplete);
        Assert.Equal(20, result.Windows.Count);
        Assert.Equal(20, result.CompleteWindowCount);
        Assert.Equal("CONSOLIDATED_SIP", result.FeedCoverage);
        Assert.True(result.QualificationUsable);
        Assert.All(result.Windows, window =>
        {
            Assert.Equal(780, window.ExpectedIntervalCount);
            Assert.Equal(780, window.ObservedIntervalCount);
            Assert.Equal(0, window.MissingIntervalCount);
            Assert.True(window.ReferenceClose.HasValue);
            Assert.Equal(101m, window.HighestEligiblePrice);
            Assert.True(window.CoverageComplete);
        });
        Assert.All(result.CalculatorObservations, observation => Assert.True(observation.CoverageComplete));
    }

    [Fact]
    public async Task MissingExactMinuteRemainsAnExplicitGapAndInvalidatesThatWindow()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 20);
        var bars = BuildBars(boundaries);
        var lastWindow = boundaries[^1];
        var missingTimestamp = ToUtc(lastWindow.NextSessionDate, new TimeOnly(8, 17));
        bars.RemoveAll(bar => bar.Timestamp == missingTimestamp);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.False(result.AllWindowsComplete);
        Assert.Equal(19, result.CompleteWindowCount);
        var incomplete = Assert.Single(result.Windows.Where(x => x.SellSessionDate == lastWindow.NextSessionDate));
        Assert.False(incomplete.CoverageComplete);
        Assert.Equal(779, incomplete.ObservedIntervalCount);
        Assert.Equal(1, incomplete.MissingIntervalCount);
        Assert.Contains(missingTimestamp, incomplete.MissingIntervalSampleUtc);
        Assert.False(result.CalculatorObservations.Single(x => x.SessionDate == lastWindow.NextSessionDate).CoverageComplete);
    }

    [Fact]
    public async Task CompleteIexTimestampsRemainDiagnosticAndCannotQualifyConsolidatedResearch()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 20);
        var bars = BuildBars(boundaries, "iex");
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "iex",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.AllWindowsComplete);
        Assert.Equal(20, result.CompleteWindowCount);
        Assert.Equal("PARTIAL_FEED", result.FeedCoverage);
        Assert.False(result.QualificationUsable);
    }

    [Fact]
    public async Task DoesNotTreatCurrentSessionAsCompleteBeforeTwentyHundredEastern()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 29), 20);
        var bars = BuildBars(boundaries);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        // 19:00 EDT on October 30: that day's 07:00–20:00 window has not closed.
        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-30T23:00:00Z"), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 10, 29), result.Windows[^1].SellSessionDate);
        Assert.True(result.AllWindowsComplete);
    }

    [Fact]
    public async Task FeedIsolationPreventsIexAndSipBarsFromBeingCombined()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 20);
        var bars = BuildBars(boundaries);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "iex",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.False(result.AllWindowsComplete);
        Assert.Equal(0, result.CompleteWindowCount);
        Assert.All(result.Windows, window =>
        {
            Assert.Equal(0, window.ObservedIntervalCount);
            Assert.Equal(780, window.MissingIntervalCount);
            Assert.False(window.CoverageComplete);
        });
    }

    private static List<MarketBar> BuildBars(IReadOnlyList<OvernightSessionBoundary> boundaries, string feed = "sip")
    {
        var bars = new List<MarketBar>(boundaries.Count * 781);
        foreach (var boundary in boundaries)
        {
            bars.Add(new MarketBar("RTO", boundary.PreviousRegularCloseUtc,
                100, 100.2, 99.8, 100, 1000, "Alpaca", feed, "1m"));
            var start = ToUtc(boundary.NextSessionDate, new TimeOnly(7, 0));
            for (var minute = 0; minute < 780; minute++)
            {
                var timestamp = start.AddMinutes(minute);
                bars.Add(new MarketBar("RTO", timestamp,
                    100, 101, 99, 100, 1000, "Alpaca", "sip", "1m"));
            }
        }
        return bars;
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    private sealed class InMemoryStore(IReadOnlyList<MarketBar> bars) : IMarketBarObservationStore
    {
        public Task<int> AppendAsync(IReadOnlyCollection<MarketBar> observations, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(
            string symbol, string? provider, DateTimeOffset fromUtc, DateTimeOffset toUtc,
            int limit, CancellationToken cancellationToken, string? timeframe = null)
        {
            IReadOnlyList<MarketBarObservation> result = bars
                .Where(bar => bar.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(bar => provider is null || bar.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
                .Where(bar => bar.Timestamp >= fromUtc && bar.Timestamp <= toUtc)
                .Where(bar => timeframe is null || bar.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase))
                .OrderBy(bar => bar.Timestamp)
                .Take(limit)
                .Select((bar, index) => new MarketBarObservation(
                    "obs-" + index, bar.Timestamp.AddMinutes(1), bar.Provider, bar.Symbol,
                    bar.Timestamp, bar, "hash-" + index, null))
                .ToArray();
            return Task.FromResult(result);
        }
    }
}
