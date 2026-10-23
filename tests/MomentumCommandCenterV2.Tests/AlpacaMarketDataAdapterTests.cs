using System.Net;
using System.Text;
using Xunit;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// PURPOSE: Verifies Alpaca credential configuration and normalized historical-bar requests without network access.
/// WHEN CALLED: Run with the repository's xUnit suite; real-provider access is tested separately with local credentials.
/// </summary>
public sealed class AlpacaMarketDataAdapterTests
{
    /// <summary>
    /// PURPOSE: Proves configured credentials are sent only as Alpaca request headers and a valid bar is normalized.
/// WHEN CALLED: Executed by xUnit to protect the local provider-configuration workflow.
    /// </summary>
    [Fact]
    public async Task GetBarsAsync_UsesConfiguredCredentialsAndParsesBar()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"bars":[{"t":"2026-10-22T16:30:00Z","o":10.1,"h":10.5,"l":10.0,"c":10.4,"v":1234}]}""",
                Encoding.UTF8,
                "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "test-key",
            ["MarketData:Alpaca:ApiSecret"] = "test-secret",
            ["MarketData:Alpaca:Feed"] = "iex"
        }).Build();
        var adapter = new AlpacaMarketDataAdapter(client, configuration);

        var bars = await adapter.GetBarsAsync("SPY", TimeSpan.FromMinutes(5),
            DateTimeOffset.Parse("2026-10-22T16:30:00Z"), DateTimeOffset.Parse("2026-10-22T17:00:00Z"),
            CancellationToken.None);

        Assert.True(adapter.IsConfigured);
        Assert.Single(bars);
        Assert.Equal("SPY", bars[0].Symbol);
        Assert.Equal(DateTimeOffset.Parse("2026-10-22T16:30:00Z"), bars[0].Timestamp);
        Assert.Equal(10.1, bars[0].Open);
        Assert.Equal(10.5, bars[0].High);
        Assert.Equal(10.0, bars[0].Low);
        Assert.Equal(10.4, bars[0].Close);
        Assert.Equal(1234, bars[0].Volume);
        Assert.Equal("Alpaca", bars[0].Provider);
        Assert.Equal("iex", bars[0].Feed);
        Assert.Equal("test-key", handler.Request!.Headers.GetValues("APCA-API-KEY-ID").Single());
        Assert.Equal("test-secret", handler.Request.Headers.GetValues("APCA-API-SECRET-KEY").Single());
        Assert.Contains("feed=iex", handler.Request.RequestUri!.Query, StringComparison.Ordinal);
    }

    /// <summary>
    /// PURPOSE: Ensures missing credentials fail before any HTTP request is sent.
    /// WHEN CALLED: Executed by xUnit to make an unconfigured provider's behavior deterministic.
    /// </summary>
    [Fact]
    public async Task GetBarsAsync_WithoutCredentials_ThrowsBeforeSendingRequest()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var adapter = new AlpacaMarketDataAdapter(client, configuration);

        Assert.False(adapter.IsConfigured);
        var exception = await Assert.ThrowsAsync<ProviderNotConfiguredException>(() =>
            adapter.GetBarsAsync("SPY", TimeSpan.FromMinutes(1),
                DateTimeOffset.Parse("2026-10-22T16:30:00Z"), DateTimeOffset.Parse("2026-10-22T17:00:00Z"),
                CancellationToken.None));
        Assert.Contains("ALPACA_API_KEY", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.Request);
    }

    /// <summary>
    /// PURPOSE: Captures the outgoing request while replacing network I/O with a deterministic response.
    /// WHEN CALLED: Used only by this test class.
    /// </summary>
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        /// <summary>
        /// PURPOSE: Records the request and returns the test's canned response.
        /// WHEN CALLED: HttpClient sends a request during an adapter test.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
