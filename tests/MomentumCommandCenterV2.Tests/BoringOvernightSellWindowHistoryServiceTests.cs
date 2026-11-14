using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightSellWindowHistoryServiceTests
{
    [Fact]
    public async Task BuildsFortyCompleteExactEtWindowsFromPersistedBars()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
        var bars = BuildBars(boundaries);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.AllWindowsComplete);
        Assert.Equal(40, result.Windows.Count);
        Assert.Equal(40, result.CompleteWindowCount);
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
            new DateOnly(2026, 10, 30), 40);
        var bars = BuildBars(boundaries);
        var lastWindow = boundaries[^1];
        var missingTimestamp = ToUtc(lastWindow.NextSessionDate, new TimeOnly(8, 17));
        bars.RemoveAll(bar => bar.Timestamp == missingTimestamp);
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.False(result.AllWindowsComplete);
        Assert.Equal(39, result.CompleteWindowCount);
        var incomplete = Assert.Single(result.Windows, x => x.SellSessionDate == lastWindow.NextSessionDate);
        Assert.False(incomplete.CoverageComplete);
        Assert.Equal(779, incomplete.ObservedIntervalCount);
        Assert.Equal(1, incomplete.MissingIntervalCount);
        Assert.Contains(missingTimestamp, incomplete.MissingIntervalSampleUtc);
        Assert.False(result.CalculatorObservations.Single(x => x.SessionDate == lastWindow.NextSessionDate).CoverageComplete);
    }

    [Fact]
    public async Task RequestedFeedCoverageRemainsCompleteWhenOtherFeedsShareTheStore()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
        var bars = BuildBars(boundaries, "sip");
        bars.AddRange(BuildBars(boundaries, "iex"));
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.AllWindowsComplete);
        Assert.True(result.QualificationUsable);
        Assert.Equal(40, result.CompleteWindowCount);
    }

    [Fact]
    public async Task CompleteIexTimestampsRemainDiagnosticAndCannotQualifyConsolidatedResearch()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
        var bars = BuildBars(boundaries, "iex");
        var service = new BoringOvernightSellWindowHistoryService(new InMemoryStore(bars));

        var result = await service.GetHistoryAsync(
            "RTO", "Alpaca", "iex",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.AllWindowsComplete);
        Assert.Equal(40, result.CompleteWindowCount);
        Assert.Equal("PARTIAL_FEED", result.FeedCoverage);
        Assert.False(result.QualificationUsable);
    }

    [Fact]
    public async Task DoesNotTreatCurrentSessionAsCompleteBeforeTwentyHundredEastern()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 29), 40);
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
    public async Task CaptureFetchesPersistsAndReturnsFortyWindowAudit()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
        var providerBars = BuildBars(boundaries);
        var store = new InMemoryStore(new List<MarketBar>());
        var provider = new InMemoryProvider(providerBars, "Alpaca");
        var service = new BoringOvernightSellWindowHistoryService(store);

        var result = await service.CaptureHistoryAsync(
            provider, "RTO", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None);

        Assert.True(result.CapturePerformed);
        var distinctSourceTimestamps = providerBars.Select(x => x.Timestamp).Distinct().Count();
        Assert.Equal(distinctSourceTimestamps, result.FetchedBarCount);
        Assert.Equal(distinctSourceTimestamps, result.NewlyPersistedBarCount);
        Assert.Equal(distinctSourceTimestamps, store.Bars.Count);
        Assert.True(result.AllWindowsComplete);
        Assert.True(result.QualificationUsable);
    }

    [Fact]
    public async Task CaptureFailsFastWhenRequestedAlpacaFeedDiffersFromEffectiveConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketData:Alpaca:ApiKey"] = "test-key",
                ["MarketData:Alpaca:ApiSecret"] = "test-secret",
                ["MarketData:Alpaca:Feed"] = "iex"
            })
            .Build();
        var provider = new AlpacaMarketDataAdapter(new HttpClient(), configuration);
        var store = new InMemoryStore(new List<MarketBar>());
        var service = new BoringOvernightSellWindowHistoryService(store);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureHistoryAsync(
            provider, "RTO", "sip", DateTimeOffset.UtcNow, CancellationToken.None));

        Assert.Contains("does not match the running Alpaca configuration 'iex'", exception.Message);
        Assert.Contains("/api/providers/status", exception.Message);
        Assert.Empty(store.Bars);
    }

    [Fact]
    public async Task CaptureRejectsWrongFeedBeforePersistingAnyBars()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
        var provider = new InMemoryProvider(BuildBars(boundaries, "iex"), "Alpaca");
        var store = new InMemoryStore(new List<MarketBar>());
        var service = new BoringOvernightSellWindowHistoryService(store);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureHistoryAsync(
            provider, "RTO", "sip",
            DateTimeOffset.Parse("2026-10-31T00:30:00Z"), CancellationToken.None));
        Assert.Empty(store.Bars);
    }

    [Fact]
    public async Task FeedIsolationPreventsIexAndSipBarsFromBeingCombined()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(
            new DateOnly(2026, 10, 30), 40);
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

    /// <summary>
    /// METHOD: DelayedSipCaptureUsesSipOnlyForCompletedWindowsOlderThanTheCutoff
    /// PURPOSE: With Feed=iex and delayed historical SIP enabled, a capture at 20:10 ET requests only feed=sip, never
    /// asks for data newer than now - 16 min, treats the previous session as the latest completed window, and
    /// produces a usable 40/40 SIP audit from synthetic bars.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task DelayedSipCaptureUsesSipOnlyForCompletedWindowsOlderThanTheCutoff()
    {
        var asOf = ToUtc(new DateOnly(2026, 10, 30), new TimeOnly(20, 10));
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 10, 29), 40);
        var handler = new AlpacaBarsHandler(BuildBars(boundaries));
        var provider = new AlpacaMarketDataAdapter(new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") },
            AlpacaConfig(allowDelayedSip: true));
        var store = new InMemoryStore(new List<MarketBar>());

        var result = await new BoringOvernightSellWindowHistoryService(store)
            .CaptureHistoryAsync(provider, "RTO", "sip", asOf, CancellationToken.None);

        Assert.NotEmpty(handler.Requests);
        Assert.All(handler.Requests, uri =>
        {
            Assert.Contains("feed=sip", uri);
            var end = DateTimeOffset.Parse(Uri.UnescapeDataString(uri.Split("&end=")[1].Split('&')[0]));
            Assert.True(end <= asOf - AlpacaDailyMarketDataClient.SipDelay, $"request end {end:O} is newer than the SIP cutoff");
        });
        Assert.Equal(new DateOnly(2026, 10, 29), result.Windows[^1].SellSessionDate);
        Assert.True(result.AllWindowsComplete);
        Assert.True(result.QualificationUsable);
        Assert.All(store.Bars, bar => Assert.Equal("sip", bar.Feed));
    }

    /// <summary>
    /// METHOD: DelayedSipCaptureIsRefusedWhenTheModeIsDisabled
    /// PURPOSE: Without AllowDelayedHistoricalSipMinutes, a SIP capture on an IEX configuration still fails before any request.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task DelayedSipCaptureIsRefusedWhenTheModeIsDisabled()
    {
        var handler = new AlpacaBarsHandler(new List<MarketBar>());
        var provider = new AlpacaMarketDataAdapter(new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") },
            AlpacaConfig(allowDelayedSip: false));
        var store = new InMemoryStore(new List<MarketBar>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BoringOvernightSellWindowHistoryService(store)
            .CaptureHistoryAsync(provider, "RTO", "sip", ToUtc(new DateOnly(2026, 10, 30), new TimeOnly(20, 30)), CancellationToken.None));
        Assert.Empty(handler.Requests);
        Assert.Empty(store.Bars);
    }

    /// <summary>
    /// METHOD: AdapterRefusesDelayedSipRequestsNewerThanTheCutoff
    /// PURPOSE: The adapter guard rejects an end time within 16 minutes of now, and rejects everything when disabled; no request is sent.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task AdapterRefusesDelayedSipRequestsNewerThanTheCutoff()
    {
        var now = DateTimeOffset.Parse("2026-11-14T20:00:00Z");
        var handler = new AlpacaBarsHandler(new List<MarketBar>());
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var enabled = new AlpacaMarketDataAdapter(http, AlpacaConfig(allowDelayedSip: true));
        var disabled = new AlpacaMarketDataAdapter(http, AlpacaConfig(allowDelayedSip: false));

        await Assert.ThrowsAsync<ArgumentException>(() => enabled.GetDelayedSipBarsAsync("RTO", TimeSpan.FromMinutes(1), now.AddHours(-2), now.AddMinutes(-15), now, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => disabled.GetDelayedSipBarsAsync("RTO", TimeSpan.FromMinutes(1), now.AddHours(-2), now.AddHours(-1), now, CancellationToken.None));
        Assert.Empty(handler.Requests);

        await enabled.GetDelayedSipBarsAsync("RTO", TimeSpan.FromMinutes(1), now.AddHours(-2), now.AddMinutes(-16), now, CancellationToken.None);
        Assert.Contains("feed=sip", Assert.Single(handler.Requests));
    }

    private static IConfiguration AlpacaConfig(bool allowDelayedSip) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "test-key",
            ["MarketData:Alpaca:ApiSecret"] = "test-secret",
            ["MarketData:Alpaca:Feed"] = "iex",
            ["MarketData:Alpaca:AllowDelayedHistoricalSipMinutes"] = allowDelayedSip ? "true" : "false"
        })
        .Build();

    /// <summary>
    /// TYPE: AlpacaBarsHandler
    /// PURPOSE: Fake Alpaca bars endpoint: records request URIs and returns the synthetic bars inside each request's start/end.
    /// WHEN CALLED: By AlpacaMarketDataAdapter under test.
    /// </summary>
    private sealed class AlpacaBarsHandler(List<MarketBar> bars) : HttpMessageHandler
    {
        public List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            Requests.Add(uri);
            var start = DateTimeOffset.Parse(Uri.UnescapeDataString(uri.Split("&start=")[1].Split('&')[0]));
            var end = DateTimeOffset.Parse(Uri.UnescapeDataString(uri.Split("&end=")[1].Split('&')[0]));
            var items = bars.Where(b => b.Timestamp >= start && b.Timestamp <= end)
                .Select(b => $$"""{"t":"{{b.Timestamp.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}}","o":{{b.Open}},"h":{{b.High}},"l":{{b.Low}},"c":{{b.Close}},"v":{{b.Volume}}}""");
            var body = """{"bars":[""" + string.Join(",", items) + """],"next_page_token":null}""";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private static List<MarketBar> BuildBars(IReadOnlyList<OvernightSessionBoundary> boundaries, string feed = "sip")
    {
        var bars = new List<MarketBar>(boundaries.Count * 781);
        foreach (var boundary in boundaries)
        {
            bars.Add(new MarketBar("RTO", boundary.PreviousCloseBarTimestampUtc,
                100, 100.2, 99.8, 100, 1000, "Alpaca", feed, "1m"));
            var start = ToUtc(boundary.NextSessionDate, new TimeOnly(7, 0));
            for (var minute = 0; minute < 780; minute++)
            {
                var timestamp = start.AddMinutes(minute);
                bars.Add(new MarketBar("RTO", timestamp,
                    100, 101, 99, 100, 1000, "Alpaca", feed, "1m"));
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

    private sealed class InMemoryProvider(IReadOnlyList<MarketBar> bars, string providerName) : IMarketDataProvider
    {
        public string ProviderName => providerName;
        public bool IsConfigured => true;
        public bool HasSuccessfulDataValidation => false;
        public DateTimeOffset? LastSuccessfulDataValidationUtc => null;
        public int LastSuccessfulDataValidationBarCount => 0;
        public IReadOnlyList<string> Capabilities => new[] { "Test 1m bars" };

        public Task<IReadOnlyList<MarketBar>> GetBarsAsync(
            string symbol, TimeSpan timeframe, DateTimeOffset from, DateTimeOffset to,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<MarketBar> result = bars
                .Where(bar => bar.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(bar => bar.Timestamp >= from && bar.Timestamp <= to)
                .Where(bar => timeframe == TimeSpan.FromMinutes(1) && bar.Timeframe == "1m")
                .ToArray();
            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryStore(List<MarketBar> bars) : IMarketBarObservationStore
    {
        public List<MarketBar> Bars { get; } = bars;

        public Task<int> AppendAsync(IReadOnlyCollection<MarketBar> observations, CancellationToken cancellationToken)
        {
            var added = 0;
            foreach (var observation in observations)
            {
                if (Bars.Any(existing => existing.Symbol == observation.Symbol &&
                    existing.Provider == observation.Provider && existing.Feed == observation.Feed &&
                    existing.Timeframe == observation.Timeframe && existing.Timestamp == observation.Timestamp))
                    continue;
                Bars.Add(observation);
                added++;
            }
            return Task.FromResult(added);
        }

        public Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(
            string symbol, string? provider, DateTimeOffset fromUtc, DateTimeOffset toUtc,
            int limit, CancellationToken cancellationToken, string? timeframe = null, string? feed = null)
        {
            IReadOnlyList<MarketBarObservation> result = Bars
                .Where(bar => bar.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(bar => provider is null || bar.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
                .Where(bar => bar.Timestamp >= fromUtc && bar.Timestamp <= toUtc)
                .Where(bar => timeframe is null || bar.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase))
                .Where(bar => feed is null || string.Equals(bar.Feed, feed, StringComparison.OrdinalIgnoreCase))
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
