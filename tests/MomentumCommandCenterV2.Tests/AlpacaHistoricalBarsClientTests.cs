using System.Net;
using System.Text;
using MomentumCommandCenterV2.Infrastructure.Alpaca;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaHistoricalBarsClientTests
{
    private static AlpacaOptions ValidOptions() => new()
    {
        ApiKey = "test-key",
        ApiSecret = "test-secret"
    };

    [Fact]
    public async Task GetBarsAsync_FollowsPageTokensAndReturnsChronologicalBars()
    {
        var handler = new RecordingHandler((request, count) =>
        {
            var url = request.RequestUri!.ToString();
            Assert.StartsWith(
                "https://data.alpaca.markets/v2/stocks/AAPL/bars?timeframe=1Min&start=",
                url);
            Assert.Contains("&end=", url);
            Assert.Contains("&limit=10000&feed=iex&sort=asc", url);

            if (count == 1)
            {
                Assert.DoesNotContain("page_token=", url);
                return JsonResponse(
                    "{\"bars\":[{\"t\":\"2026-10-27T14:31:00Z\",\"o\":10,\"h\":11,\"l\":9,\"c\":10.5,\"v\":100}],\"next_page_token\":\"next-page\"}");
            }

            Assert.Contains("page_token=next-page", url);
            return JsonResponse(
                "{\"bars\":[{\"t\":\"2026-10-27T14:30:00Z\",\"o\":9,\"h\":10,\"l\":8,\"c\":9.5,\"v\":90}],\"next_page_token\":null}");
        });

        using var httpClient = new HttpClient(handler);
        var client = new AlpacaHistoricalBarsClient(httpClient, ValidOptions(), retryDelay: TimeSpan.Zero);

        var bars = await client.GetBarsAsync(
            " aapl ",
            "1Min",
            DateTimeOffset.Parse("2026-10-27T14:30:00Z"),
            DateTimeOffset.Parse("2026-10-27T14:32:00Z"));

        Assert.Equal(2, handler.RequestCount);
        Assert.Equal(new[] { 9.5m, 10.5m }, bars.Select(x => x.Close));
        Assert.All(bars, bar => Assert.Equal("AAPL", bar.Symbol));
    }

    [Fact]
    public async Task GetBarsAsync_RetriesRateLimitThenSucceeds()
    {
        var handler = new RecordingHandler((_, count) =>
            count == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : JsonResponse(
                    "{\"bars\":[{\"t\":\"2026-10-27T14:30:00Z\",\"o\":9,\"h\":10,\"l\":8,\"c\":9.5,\"v\":90}],\"next_page_token\":null}"));

        using var httpClient = new HttpClient(handler);
        var client = new AlpacaHistoricalBarsClient(
            httpClient,
            ValidOptions(),
            maxAttempts: 2,
            retryDelay: TimeSpan.Zero);

        var bars = await client.GetBarsAsync(
            "AAPL",
            "1Min",
            DateTimeOffset.Parse("2026-10-27T14:30:00Z"),
            DateTimeOffset.Parse("2026-10-27T14:31:00Z"));

        Assert.Single(bars);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetBarsAsync_DoesNotRetryNonTransientClientError()
    {
        var handler = new RecordingHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaHistoricalBarsClient(
            httpClient,
            ValidOptions(),
            maxAttempts: 3,
            retryDelay: TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetBarsAsync(
                "AAPL",
                "1Min",
                DateTimeOffset.Parse("2026-10-27T14:30:00Z"),
                DateTimeOffset.Parse("2026-10-27T14:31:00Z")));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(1, handler.RequestCount);
        Assert.DoesNotContain("test-secret", exception.Message);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request, RequestCount));
        }
    }
}
