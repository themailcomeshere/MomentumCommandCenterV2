using System.Net;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaDailyMarketDataClientTests
{
    [Fact]
    public async Task GetMetricsAsync_UsesAdjustedDailyBarsAndExactCompletedSessions()
    {
        var now = new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero);
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var dates = new List<DateOnly>();
        for (var date = new DateOnly(2026, 11, 3); dates.Count < 20; date = date.AddDays(-1))
            if (UsEquitiesSessionCalendar.IsTradingSession(date)) dates.Add(date);
        dates.Reverse();

        var bars = dates.Select((date, index) =>
        {
            var local = date.ToDateTime(new TimeOnly(9, 30), DateTimeKind.Unspecified);
            var timestamp = new DateTimeOffset(local, eastern.GetUtcOffset(local)).ToUniversalTime();
            return $$"""{"t":"{{timestamp:O}}","o":10,"h":12,"l":9,"c":{{100 + index + 1}},"v":{{1000 + index + 1}}}""";
        }).ToList();
        bars.Add("""{"t":"2026-11-04T14:30:00Z","o":10,"h":12,"l":9,"c":999,"v":999999}""");

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"bars":[""" + string.Join(",", bars) + "]}"),
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "sip"
        }).Build();
        var client = new AlpacaDailyMarketDataClient(http, config);

        var result = await client.GetMetricsAsync("RTO", now, CancellationToken.None);

        Assert.Equal(20, result.ValidSessionCount);
        Assert.Equal(20, result.AvailableCompletedSessionCount);
        Assert.Equal(120d, result.Price);
        Assert.Equal(110.5d, result.Sma20);
        Assert.Equal(1010.5d, result.AverageDailyVolume20);
        Assert.Equal("all (split/dividend adjusted)", result.Adjustment);
        Assert.True(result.DataFresh);
        Assert.Equal("COMPLETE_20_EXPECTED_SESSIONS", result.Completeness);
        Assert.Equal("CONSOLIDATED_SIP", result.FeedCoverage);
        Assert.True(result.QualificationUsable);
        Assert.Equal(20, result.Sessions.Count);
        Assert.Empty(result.MissingSessionDates);
        Assert.Equal(dates, result.ExpectedSessionDates);
        Assert.Equal(dates[^1], result.LatestExpectedSessionDate);
        Assert.Equal(120d, result.ObservedClose);
        Assert.Equal(110.5d, result.ObservedSma20);
        Assert.Equal(1010.5d, result.ObservedAverageDailyVolume20);
        Assert.Contains("timeframe=1Day", handler.Request!.RequestUri!.ToString());
        Assert.Contains("adjustment=all", handler.Request.RequestUri.ToString());
        Assert.Contains("feed=sip", handler.Request.RequestUri.ToString());
        Assert.Equal("key", handler.Request.Headers.GetValues("APCA-API-KEY-ID").Single());
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsNullRollingMetricsWhenExpectedSessionIsMissing()
    {
        const string json = """{"bars":[{"t":"2026-11-03T14:30:00Z","o":10,"h":11,"l":9,"c":10.5,"v":1000}]}""";
        using var http = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "iex"
        }).Build();
        var client = new AlpacaDailyMarketDataClient(http, config);

        var result = await client.GetMetricsAsync("EVR", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(1, result.ValidSessionCount);
        Assert.True(result.DataFresh);
        Assert.Null(result.Price);
        Assert.Null(result.Sma20);
        Assert.Null(result.AverageDailyVolume20);
        Assert.Equal(10.5d, result.ObservedClose);
        Assert.Equal("IEX_PARTIAL", result.FeedCoverage);
        Assert.False(result.QualificationUsable);
        Assert.Equal("INCOMPLETE_20_EXPECTED_SESSIONS", result.Completeness);
        Assert.Equal(20, result.ExpectedSessionDates.Count);
        Assert.Equal(19, result.MissingSessionDates.Count);
        Assert.Single(result.Sessions);
    }


    [Fact]
    public async Task GetMetricsAsync_EmptyBarsReturnsIncompleteDataInsteadOfThrowing()
    {
        using var http = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"bars":[]}""")
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "sip"
        }).Build();
        var client = new AlpacaDailyMarketDataClient(http, config);

        var result = await client.GetMetricsAsync("RTO",
            new DateTimeOffset(2026, 11, 6, 22, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(0, result.ValidSessionCount);
        Assert.Empty(result.Sessions);
        Assert.Equal(20, result.ExpectedSessionDates.Count);
        Assert.Equal(20, result.MissingSessionDates.Count);
        Assert.False(result.DataFresh);
        Assert.False(result.QualificationUsable);
        Assert.Null(result.Price);
        Assert.Null(result.Sma20);
        Assert.Null(result.AverageDailyVolume20);
    }


    [Fact]
    public async Task GetMetricsAsync_AcceptsSymbolKeyedBarsEnvelope()
    {
        using var http = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"bars":{"BAYRY":[{"t":"2026-11-06T14:30:00Z","o":10,"h":11,"l":9,"c":10.5,"v":1200}]}}""")
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "iex"
        }).Build();

        var result = await new AlpacaDailyMarketDataClient(http, config).GetMetricsAsync("BAYRY",
            new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(1, result.ValidSessionCount);
        Assert.Single(result.Sessions);
        Assert.Equal(10.5d, result.ObservedClose);
        Assert.Equal("IEX_PARTIAL", result.FeedCoverage);
        Assert.False(result.QualificationUsable);
    }

    [Fact]
    public async Task GetMetricsAsync_RetriesTransientGatewayFailureBeforeReturningDailyMetrics()
    {
        var attempts = 0;
        using var http = new HttpClient(new SequenceHandler(() =>
        {
            attempts++;
            return attempts < 3
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"bars":[]}""") };
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "sip"
        }).Build();

        var result = await new AlpacaDailyMarketDataClient(http, config).GetMetricsAsync("BAYRY",
            new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(0, result.ValidSessionCount);
        Assert.False(result.QualificationUsable);
    }

    [Fact]
    public async Task GetMetricsAsync_DoesNotRetryNonTransientUnprocessableSymbolResponse()
    {
        var attempts = 0;
        using var http = new HttpClient(new SequenceHandler(() =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = new StringContent("symbol not supported") };
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret",
            ["MarketData:Alpaca:Feed"] = "sip"
        }).Build();

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new AlpacaDailyMarketDataClient(http, config).GetMetricsAsync("BAYRY",
            new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), CancellationToken.None));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.StatusCode);
        Assert.Equal(1, attempts);
    }

    private sealed class SequenceHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory());
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
