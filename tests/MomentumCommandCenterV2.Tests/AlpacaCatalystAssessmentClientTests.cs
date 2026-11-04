using System.Net;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaCatalystAssessmentClientTests
{
    [Theory]
    [InlineData("Company announces public offering to raise capital", "RISK_PRESENT", "CAPITAL_RAISE_OR_DILUTION")]
    [InlineData("Company reports quarterly earnings and raises guidance", "MATERIAL_EVENT_REVIEW", "EARNINGS_OR_GUIDANCE")]
    [InlineData("Company updates its corporate website", "UNCLASSIFIED_NEWS_REVIEW", null)]
    public async Task AssessAsync_ClassifiesHeadlineWithoutEverClaimingClear(string headline, string expectedStatus, string? expectedCategory)
    {
        var json = $$"""{"news":[{"id":"n1","headline":{{System.Text.Json.JsonSerializer.Serialize(headline)}},"summary":"","created_at":"2026-11-04T14:00:00Z","url":"https://example.test/news","symbols":["RTO"]}],"next_page_token":null}""";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret"
        }).Build();
        var client = new AlpacaCatalystAssessmentClient(http, config);

        var result = await client.AssessAsync("RTO", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.CoverageComplete);
        Assert.Equal("MCC-CATALYST-TRIAGE-1.0", result.RuleSetVersion);
        Assert.NotEqual("CLEAR_WITH_SOURCE", result.Status);
        Assert.Contains("symbols=RTO", handler.Request!.RequestUri!.ToString());
        if (expectedCategory is not null)
            Assert.Contains(expectedCategory, result.Articles.Single().Categories);
    }

    [Fact]
    public async Task AssessAsync_NoNewsRemainsUnknownRatherThanClear()
    {
        using var http = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"news":[],"next_page_token":null}""")
        })) { BaseAddress = new Uri("https://data.alpaca.markets/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:Alpaca:ApiKey"] = "key",
            ["MarketData:Alpaca:ApiSecret"] = "secret"
        }).Build();

        var result = await new AlpacaCatalystAssessmentClient(http, config).AssessAsync(
            "EVR", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.Equal("UNKNOWN", result.Status);
        Assert.Empty(result.Articles);
        Assert.Contains("never emits CLEAR_WITH_SOURCE", result.Note);
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
