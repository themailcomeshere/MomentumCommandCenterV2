using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class CatalystHumanReviewStoreTests
{
    [Fact]
    public async Task AppendAndReadPreservesHumanLabelWithoutChangingAutomatedAssessment()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mcc-catalyst-review-tests", Guid.NewGuid().ToString("N"));
        var store = new CatalystHumanReviewStore(Path.Combine(directory, "reviews.jsonl"));
        try
        {
            var record = await store.AppendAsync(
                new CatalystHumanReviewInput(
                    "ever",
                    "article-123",
                    "Company reports quarterly results",
                    "https://example.com/news/123",
                    DateTimeOffset.Parse("2026-11-02T20:05:00Z"),
                    "Example News",
                    "material_event",
                    "MCC-CATALYST-TRIAGE-1.0",
                    "Earnings released after close"),
                CancellationToken.None);

            Assert.Equal("EVER", record.Symbol);
            Assert.Equal("MATERIAL_EVENT", record.Label);
            Assert.Equal("article-123", record.ArticleId);

            var latest = await store.GetLatestAsync("EVER", 10, CancellationToken.None);
            var saved = Assert.Single(latest);
            Assert.Equal(record.ReviewId, saved.ReviewId);
            Assert.Equal("MATERIAL_EVENT", saved.Label);

            var otherSymbol = await store.GetLatestAsync("RTO", 10, CancellationToken.None);
            Assert.Empty(otherSymbol);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("CLEAR")]
    [InlineData("CLEAR_WITH_SOURCE")]
    [InlineData("BUY")]
    [InlineData("")]
    public async Task RejectsLabelsOutsideHumanReviewTaxonomy(string label)
    {
        var directory = Path.Combine(Path.GetTempPath(), "mcc-catalyst-review-tests", Guid.NewGuid().ToString("N"));
        var store = new CatalystHumanReviewStore(Path.Combine(directory, "reviews.jsonl"));
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(
                new CatalystHumanReviewInput(
                    "RTO", null, "A headline", "https://example.com/a", null,
                    "Example News", label),
                CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
