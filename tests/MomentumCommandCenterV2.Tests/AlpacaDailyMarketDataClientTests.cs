using System.Net;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaDailyMarketDataClientTests
{
    [Fact]
    public async Task GetMetricsAsync_UsesAdjustedDailyBarsAndExcludesCurrentIncompleteSession()
    {
        var now = new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero);
        var bars = Enumerable.Range(1, 20).Select(i =>
            $$"""{"t":"{{new DateTimeOffset(2026, 10, 15, 13, 30, 0, TimeSpan.Zero).AddDays(i - 1):O}}","o":10,"h":12,"l":9,"c":{{100 + i}},"v":{{1000 + i}}}""").ToList();
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
            ["MarketData:Alpaca:Feed"] = "iex"
        }).Build();
        var client = new AlpacaDailyMarketDataClient(http, config);

        var result = await client.GetMetricsAsync("RTO", now, CancellationToken.None);

        Assert.Equal(20, result.ValidSessionCount);
        Assert.Equal(20, result.AvailableCompletedSessionCount);
        Assert.Equal(120, result.Price);
        Assert.Equal(110.5, result.Sma20);
        Assert.Equal(1010.5, result.AverageDailyVolume20);
        Assert.Equal("all (split/dividend adjusted)", result.Adjustment);
        Assert.True(result.DataFresh);
        Assert.Contains("timeframe=1Day", handler.Request!.RequestUri!.ToString());
        Assert.Contains("adjustment=all", handler.Request.RequestUri.ToString());
        Assert.Contains("feed=iex", handler.Request.RequestUri.ToString());
        Assert.Equal("key", handler.Request.Headers.GetValues("APCA-API-KEY-ID").Single());
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsNullRollingMetricsWhenFewerThanTwentyCompletedSessions()
    {
        const string json = """{"bars":[{"t":"2026-11-02T14:30:00Z","o":10,"h":11,"l":9,"c":10.5,"v":1000}]}""";
        using var http = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret"
        }).Build();
        var client = new AlpacaDailyMarketDataClient(http, config);

        var result = await client.GetMetricsAsync("EVR", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(1, result.ValidSessionCount);
        Assert.Null(result.Sma20);
        Assert.Null(result.AverageDailyVolume20);
        Assert.Equal("INSUFFICIENT_DAILY_HISTORY", result.Completeness);
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
