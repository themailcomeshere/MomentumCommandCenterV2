using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Application.Models;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaScannerDiscoverySourceTests
{
    [Fact]
    public async Task ParsesTopGainersAndLosersAsDiscoveryOnlyEvents()
    {
        const string json = """
        {
          "market_type": "stocks",
          "last_updated": "2026-10-29T16:00:00Z",
          "gainers": [{ "symbol": "AAA", "change": 1.25, "percent_change": 12.5, "price": 11.25 }],
          "losers": [{ "symbol": "BBB", "change": -2.0, "percent_change": -10.0, "price": 18.0 }]
        }
        """;
        var handler = new StubHandler(json);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "test-key",
            ["MarketData:Alpaca:ApiSecret"] = "test-secret"
        }).Build();
        var source = new AlpacaScannerDiscoverySource(client, config);
        var observedAt = DateTimeOffset.Parse("2026-10-29T16:00:05Z");

        var events = await source.GetDiscoveriesAsync(observedAt);

        Assert.Equal(2, events.Count);
        Assert.Equal("AAA", events[0].Symbol);
        Assert.Equal("ALPACA_TOP_GAINERS", events[0].ScannerGroup);
        Assert.Equal("BBB", events[1].Symbol);
        Assert.Equal("ALPACA_TOP_LOSERS", events[1].ScannerGroup);
        Assert.Equal(DateTimeOffset.Parse("2026-10-29T16:00:00Z"), events[0].SourceTimestamp);
        Assert.Equal(ScannerCandidateDisposition.DataUnknown, events[0].Disposition);
        Assert.Contains("NOT_A_TRADE_SIGNAL", events[0].ReasonCodes);
        Assert.Equal("12.5", events[0].RawFields["percent_change"]);
        Assert.Equal("GET", handler.Method);
        Assert.Contains("top=50", handler.RequestUri);
    }

    [Fact]
    public async Task ReportsProviderEntitlementErrorsInsteadOfReturningEmptyDiscovery()
    {
        var handler = new StubHandler("{\"message\":\"subscription required\"}", HttpStatusCode.Forbidden);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "test-key",
            ["MarketData:Alpaca:ApiSecret"] = "test-secret"
        }).Build();
        var source = new AlpacaScannerDiscoverySource(client, config);

        await Assert.ThrowsAsync<HttpRequestException>(() => source.GetDiscoveriesAsync(DateTimeOffset.UtcNow));
    }

    private sealed class StubHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Method { get; private set; }
        public string? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method.Method;
            RequestUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
