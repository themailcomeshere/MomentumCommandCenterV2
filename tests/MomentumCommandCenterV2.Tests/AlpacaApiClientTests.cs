using System.Net;
using System.Text;
using MomentumCommandCenterV2.Infrastructure.Alpaca;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaApiClientTests
{
    private static AlpacaOptions ValidOptions() => new()
    {
        ApiKey = "test-key",
        ApiSecret = "test-secret"
    };

    [Fact]
    public async Task ValidateConnectionAsync_RejectsMissingCredentialsBeforeHttpRequest()
    {
        var handler = new RecordingHandler(_ => throw new Xunit.Sdk.XunitException(
            "No HTTP request should be sent when credentials are missing."));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaApiClient(httpClient, new AlpacaOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ValidateConnectionAsync());

        Assert.Contains(AlpacaOptions.ApiKeyEnvironmentVariable, exception.Message);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ValidateConnectionAsync_ParsesAccountStatusAndSendsCredentialsInHeaders()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://paper-api.alpaca.markets/v2/account", request.RequestUri!.ToString());
            Assert.Equal("test-key", request.Headers.GetValues("APCA-API-KEY-ID").Single());
            Assert.Equal("test-secret", request.Headers.GetValues("APCA-API-SECRET-KEY").Single());

            return JsonResponse("{\"status\":\"ACTIVE\",\"id\":\"must-not-be-returned\"}");
        });

        using var httpClient = new HttpClient(handler);
        var result = await new AlpacaApiClient(httpClient, ValidOptions())
            .ValidateConnectionAsync();

        Assert.True(result.IsConnected);
        Assert.Equal("ACTIVE", result.AccountStatus);
        Assert.DoesNotContain("must-not-be-returned", result.Message);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task ValidateConnectionAsync_ThrowsSanitizedHttpErrorForUnauthorizedCredentials()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaApiClient(httpClient, ValidOptions());

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.ValidateConnectionAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.DoesNotContain("test-secret", exception.Message);
    }

    [Fact]
    public async Task GetLatestBarAsync_NormalizesSymbolAndParsesBar()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(
                "https://data.alpaca.markets/v2/stocks/AAPL/bars/latest?feed=iex",
                request.RequestUri!.ToString());
            return JsonResponse(
                "{\"symbol\":\"AAPL\",\"bar\":{\"t\":\"2026-10-23T15:30:00Z\",\"o\":250.1,\"h\":251.2,\"l\":249.8,\"c\":250.9,\"v\":12345}}");
        });

        using var httpClient = new HttpClient(handler);
        var bar = await new AlpacaApiClient(httpClient, ValidOptions())
            .GetLatestBarAsync(" aapl ");

        Assert.Equal("AAPL", bar.Symbol);
        Assert.Equal(250.9m, bar.Close);
        Assert.Equal(251.2m, bar.High);
        Assert.Equal(249.8m, bar.Low);
        Assert.Equal(12345L, bar.Volume);
        Assert.Equal(DateTimeOffset.Parse("2026-10-23T15:30:00Z"), bar.Timestamp);
    }

    [Fact]
    public async Task GetLatestBarAsync_ThrowsFormatExceptionWhenBarIsMissing()
    {
        var handler = new RecordingHandler(_ => JsonResponse("{\"symbol\":\"AAPL\"}"));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaApiClient(httpClient, ValidOptions());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => client.GetLatestBarAsync("AAPL"));

        Assert.Contains("no latest bar", exception.Message);
    }

    [Fact]
    public async Task GetLatestNewsAsync_ParsesArticleMetadataAndRequestsSymbol()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.StartsWith(
                "https://data.alpaca.markets/v1beta1/news?symbols=AAPL&limit=5&sort=desc&start=",
                request.RequestUri!.ToString());
            Assert.Contains("&end=", request.RequestUri!.ToString());
            return JsonResponse(
                "{\"news\":[{\"id\":123,\"headline\":\"AAPL test headline\",\"summary\":\"summary\",\"created_at\":\"2026-10-25T14:00:00Z\",\"updated_at\":\"2026-10-25T14:05:00Z\",\"url\":\"https://example.test/story\",\"symbols\":[\"AAPL\"]}]}");
        });

        using var httpClient = new HttpClient(handler);
        var articles = await new AlpacaApiClient(httpClient, ValidOptions())
            .GetLatestNewsAsync(" aapl ");

        var article = Assert.Single(articles);
        Assert.Equal("123", article.Id);
        Assert.Equal("AAPL test headline", article.Headline);
        Assert.Equal("summary", article.Summary);
        Assert.Equal("https://example.test/story", article.Url);
        Assert.Contains("AAPL", article.Symbols);
    }

    [Fact]
    public async Task GetLatestNewsAsync_ThrowsSanitizedHttpErrorWhenNewsAccessIsDenied()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaApiClient(httpClient, ValidOptions());

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetLatestNewsAsync("AAPL"));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.DoesNotContain("test-secret", exception.Message);
    }

    [Fact]
    public async Task GetLatestNewsAsync_RejectsInvalidLimitBeforeHttpRequest()
    {
        var handler = new RecordingHandler(_ => throw new Xunit.Sdk.XunitException(
            "No HTTP request should be sent for an invalid limit."));
        using var httpClient = new HttpClient(handler);
        var client = new AlpacaApiClient(httpClient, ValidOptions());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GetLatestNewsAsync("AAPL", 51));
        Assert.Equal(0, handler.RequestCount);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
