using System.Net;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: DailySourceDelayedSipAndCrossCheckTests
/// PURPOSE: Verifies the delayed historical SIP Daily mode (cutoff, refusal fallback, error surfacing), the
/// Massive Daily parser and the session-by-session cross-check. All data is synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class DailySourceDelayedSipAndCrossCheckTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>
    /// METHOD: DelayedSipRequestsSipWithEndAtLeastSixteenMinutesOld
    /// PURPOSE: The SIP request end is now - 16 min, the feed is sip and the result is consolidated and usable.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task DelayedSipRequestsSipWithEndAtLeastSixteenMinutesOld()
    {
        var now = new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero);
        var handler = new QueueHandler(_ => Ok(BarsJson(LatestSessions(new DateOnly(2026, 11, 3), 20))));
        var result = await Client(handler).GetMetricsAsync("RTO", now, CancellationToken.None);

        var uri = Uri.UnescapeDataString(handler.Requests.Single().RequestUri!.ToString());
        Assert.Contains("feed=sip", uri);
        Assert.Contains("end=" + (now - AlpacaDailyMarketDataClient.SipDelay).UtcDateTime.ToString("O"), uri);
        Assert.Equal("sip", result.Feed);
        Assert.Equal("CONSOLIDATED_SIP", result.FeedCoverage);
        Assert.Equal("SIP_DELAYED_HISTORICAL_OK", result.SipRequestStatus);
        Assert.True(result.QualificationUsable);
        Assert.Contains("delayed historical query", result.Note);
    }

    /// <summary>
    /// METHOD: DelayedSipDoesNotCountTodayUntilTheCloseIsOlderThanTheCutoff
    /// PURPOSE: At 16:10 ET the cutoff is 15:54 ET, so today's session is not yet completed even if a bar exists;
    /// at 16:20 ET it is.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task DelayedSipDoesNotCountTodayUntilTheCloseIsOlderThanTheCutoff()
    {
        var today = new DateOnly(2026, 11, 4);
        var bars = BarsJson(LatestSessions(today, 21));
        var early = await Client(new QueueHandler(_ => Ok(bars))).GetMetricsAsync("RTO", EasternTime(today, 16, 10), CancellationToken.None);
        var later = await Client(new QueueHandler(_ => Ok(bars))).GetMetricsAsync("RTO", EasternTime(today, 16, 20), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 3), early.LatestExpectedSessionDate);
        Assert.Equal(today, later.LatestExpectedSessionDate);
        Assert.True(early.QualificationUsable);
        Assert.True(later.QualificationUsable);
    }

    /// <summary>
    /// METHOD: SipRefusalFallsBackToConfiguredFeedAndIsNeverUsable
    /// PURPOSE: A 403 on SIP repeats the request on IEX; the result is IEX_PARTIAL, not usable, and records the refusal.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "forbidden")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "{\"code\":42210000,\"message\":\"subscription does not permit querying recent SIP data\"}")]
    public async Task SipRefusalFallsBackToConfiguredFeedAndIsNeverUsable(HttpStatusCode refusal, string body)
    {
        var bars = BarsJson(LatestSessions(new DateOnly(2026, 11, 3), 20));
        var handler = new QueueHandler(
            _ => new HttpResponseMessage(refusal) { Content = new StringContent(body) },
            _ => Ok(bars));
        var result = await Client(handler).GetMetricsAsync("RTO", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("feed=sip", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("feed=iex", handler.Requests[1].RequestUri!.ToString());
        Assert.Equal("iex", result.Feed);
        Assert.Equal("IEX_PARTIAL", result.FeedCoverage);
        Assert.False(result.QualificationUsable);
        Assert.Null(result.Price);
        Assert.StartsWith("SIP_REFUSED_FELL_BACK_TO_IEX: " + (int)refusal, result.SipRequestStatus);
    }

    /// <summary>
    /// METHOD: NonSipUnprocessableResponseStillThrows
    /// PURPOSE: A 422 that is not about SIP (for example an unsupported symbol) is an error, not a fallback.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task NonSipUnprocessableResponseStillThrows()
    {
        var handler = new QueueHandler(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = new StringContent("invalid symbol") });
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            Client(handler).GetMetricsAsync("ZZZZ", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.StatusCode);
        Assert.Single(handler.Requests);
    }

    /// <summary>
    /// METHOD: WithoutDailyFeedSettingTheConfiguredFeedIsUsedUnchanged
    /// PURPOSE: Backward compatibility: no DailyFeed setting means one request on MarketData:Alpaca:Feed and no SIP status.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task WithoutDailyFeedSettingTheConfiguredFeedIsUsedUnchanged()
    {
        var handler = new QueueHandler(_ => Ok(BarsJson(LatestSessions(new DateOnly(2026, 11, 3), 20))));
        var result = await Client(handler, dailyFeed: null).GetMetricsAsync("RTO", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);
        Assert.Contains("feed=iex", handler.Requests.Single().RequestUri!.ToString());
        Assert.Null(result.SipRequestStatus);
        Assert.False(result.QualificationUsable);
    }

    /// <summary>
    /// METHOD: SplitAdjustmentIsRequestedAndLabelled
    /// PURPOSE: The cross-check asks Alpaca for split-only adjustment to match Massive adjusted=true.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public async Task SplitAdjustmentIsRequestedAndLabelled()
    {
        var handler = new QueueHandler(_ => Ok(BarsJson(LatestSessions(new DateOnly(2026, 11, 3), 20))));
        var result = await Client(handler).GetMetricsAsync("RTO", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None, adjustment: "split");
        Assert.Contains("adjustment=split", handler.Requests.Single().RequestUri!.ToString());
        Assert.Equal("split (split adjusted only)", result.Adjustment);
    }

    /// <summary>
    /// METHOD: MassiveParseUsesEasternSessionDatesAndKeepsGapsEmpty
    /// PURPOSE: Massive millisecond timestamps at Eastern midnight map to that Eastern date; missing days are absent, not filled.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void MassiveParseUsesEasternSessionDatesAndKeepsGapsEmpty()
    {
        var d1 = EasternTime(new DateOnly(2026, 11, 2), 0, 0).ToUnixTimeMilliseconds();
        var d3 = EasternTime(new DateOnly(2026, 11, 4), 0, 0).ToUnixTimeMilliseconds();
        var body = $$"""{"status":"OK","results":[{"t":{{d1}},"o":1,"h":2,"l":0.5,"c":1.5,"v":1000.0},{"t":{{d3}},"o":2,"h":3,"l":1.5,"c":2.5,"v":2000}]}""";
        var result = MassiveDailyMarketDataClient.Parse("RTO", body, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4) }, result.Sessions.Select(s => s.SessionDate));
        Assert.Equal(1000, result.Sessions[0].Volume);
        Assert.Equal("split (adjusted=true)", result.Adjustment);
    }

    /// <summary>
    /// METHOD: MassiveParseTreatsNoResultsAsEmptyAndErrorStatusAsFailure
    /// PURPOSE: An empty response is zero sessions; NOT_AUTHORIZED is an error.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void MassiveParseTreatsNoResultsAsEmptyAndErrorStatusAsFailure()
    {
        Assert.Empty(MassiveDailyMarketDataClient.Parse("RTO", """{"status":"OK","resultsCount":0}""", DateTimeOffset.UtcNow).Sessions);
        Assert.Throws<HttpRequestException>(() => MassiveDailyMarketDataClient.Parse("RTO", """{"status":"NOT_AUTHORIZED"}""", DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// METHOD: CrossCheckPassesOnlyWhenEveryExpectedSessionMatches
    /// PURPOSE: Exact rows pass; one price difference, one volume difference and a missing reference row each fail and are counted.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void CrossCheckPassesOnlyWhenEveryExpectedSessionMatches()
    {
        var dates = new[] { new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 5) };
        var primary = dates.Select(d => Obs(d, 10, 100)).ToArray();

        var pass = DailySourceCrossCheck.Compare(dates, primary, dates.Select(d => Obs(d, 10, 100)).ToArray());
        Assert.Equal("PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE", pass.Verdict);

        var reference = new[] { Obs(dates[0], 10.005, 100), Obs(dates[1], 10.02, 100), Obs(dates[2], 10, 90) };
        var fail = DailySourceCrossCheck.Compare(dates, primary, reference);
        Assert.Equal("FAIL", fail.Verdict);
        Assert.Equal(new[] { "MATCH", "PRICE_MISMATCH", "VOLUME_MISMATCH", "MISSING_REFERENCE" }, fail.Rows.Select(r => r.Status));
        Assert.Equal(10, fail.Rows[2].VolumeDiff);
        Assert.Equal(10d / 90d, fail.Rows[2].VolumeRelativeDiff!.Value, 10);
        Assert.Null(fail.Rows[3].CloseDiff);
        Assert.Equal(1, fail.StatusCounts["MISSING_REFERENCE"]);
    }

    /// <summary>
    /// METHOD: CrossCheckVolumeToleranceIsRelative
    /// PURPOSE: Default tolerance (0.5%) passes a 0.5% volume difference and fails 1%; an explicit 2% tolerance passes 1%;
    /// a zero tolerance restores the old exact rule.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void CrossCheckVolumeToleranceIsRelative()
    {
        var dates = new[] { new DateOnly(2026, 11, 2) };
        var r = new[] { Obs(dates[0], 10, 1000) };
        Assert.Equal("PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 10, 1005) }, r).Verdict);
        Assert.Equal("FAIL", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 10, 1010) }, r).Verdict);
        Assert.Equal("PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 10, 1010) }, r, volumeRelativeTolerance: 0.02).Verdict);
        Assert.Equal("FAIL", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 10, 1005) }, r, priceTolerance: 0, volumeRelativeTolerance: 0).Verdict);
    }

    /// <summary>
    /// METHOD: CrossCheckPriceToleranceIsOneCentByDefault
    /// PURPOSE: A one-cent difference passes (2-decimal rounding); two cents fails.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void CrossCheckPriceToleranceIsOneCentByDefault()
    {
        var dates = new[] { new DateOnly(2026, 11, 2) };
        var r = new[] { Obs(dates[0], 296.35, 1000) };
        Assert.Equal("PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 296.36, 1000) }, r).Verdict);
        Assert.Equal("FAIL", DailySourceCrossCheck.Compare(dates, new[] { Obs(dates[0], 296.37, 1000) }, r).Verdict);
    }

    private static DailySessionObservation Obs(DateOnly date, double close, long volume) =>
        new(date, EasternTime(date, 0, 0), close, close, close, close, volume);

    private static AlpacaDailyMarketDataClient Client(QueueHandler handler, string? dailyFeed = "sip_delayed")
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var settings = new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "iex",
        };
        if (dailyFeed is not null) settings["MarketData:Alpaca:DailyFeed"] = dailyFeed;
        return new AlpacaDailyMarketDataClient(http, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    private static DateTimeOffset EasternTime(DateOnly date, int hour, int minute)
    {
        var local = date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    private static List<DateOnly> LatestSessions(DateOnly latest, int count)
    {
        var dates = new List<DateOnly>();
        for (var date = latest; dates.Count < count; date = date.AddDays(-1))
            if (UsEquitiesSessionCalendar.IsTradingSession(date)) dates.Add(date);
        dates.Reverse();
        return dates;
    }

    private static string BarsJson(IEnumerable<DateOnly> dates) =>
        """{"bars":[""" + string.Join(",", dates.Select((d, i) =>
            $$"""{"t":"{{EasternTime(d, 0, 0):O}}","o":10,"h":12,"l":9,"c":{{100 + i}},"v":{{1000 + i}}}""")) + "]}";

    private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    /// <summary>
    /// TYPE: QueueHandler
    /// PURPOSE: Test HTTP handler that records requests and returns responses in order (the last one repeats).
    /// WHEN CALLED: By the Daily client under test.
    /// </summary>
    private sealed class QueueHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var factory = responses[Math.Min(Requests.Count - 1, responses.Length - 1)];
            return Task.FromResult(factory(request));
        }
    }
}
