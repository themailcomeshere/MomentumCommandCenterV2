using System.Net;
using System.Text;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class YahooFinanceQuoteClientTests
{
    [Fact]
    public async Task GetQuoteAsync_ParsesAfterHoursPriceTimeAndInstrumentName()
    {
        const string json = """{"chart":{"result":[{"meta":{"symbol":"RKLX","longName":"Defiance Daily Target 2X Long RKLB ETF","regularMarketPrice":10.70,"regularMarketTime":1794268800,"postMarketPrice":9.07,"postMarketTime":1794281880}}],"error":null}}""";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com/") };
        var client = new YahooFinanceQuoteClient(http);

        var result = await client.GetQuoteAsync("RKLX", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("RKLX", result.Symbol);
        Assert.Equal("Defiance Daily Target 2X Long RKLB ETF", result.Name);
        Assert.Equal(9.07m, result.PostMarketPrice);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1794281880), result.PostMarketTimeUtc);
        Assert.Equal("Yahoo Finance chart metadata", result.Source);
        Assert.Contains("includePrePost=true", handler.Request!.RequestUri!.Query, StringComparison.Ordinal);
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
