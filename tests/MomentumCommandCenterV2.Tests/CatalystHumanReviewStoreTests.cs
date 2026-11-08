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
                    "Earnings released after close",
                    "MATERIAL",
                    new[] { "EARNINGS_OR_GUIDANCE" }),
                CancellationToken.None);

            Assert.Equal("EVER", record.Symbol);
            Assert.Equal("MATERIAL_EVENT", record.Label);
            Assert.Equal("article-123", record.ArticleId);
            Assert.Equal("MATERIAL", record.PredictedSeverity);
            Assert.Contains("EARNINGS_OR_GUIDANCE", record.PredictedCategories!);

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


    [Fact]
    public void ValidationUsesLaterChronologicalHoldoutAndReportsConfusionMetrics()
    {
        var start = DateTimeOffset.Parse("2026-11-01T00:00:00Z");
        var records = Enumerable.Range(0, 10).Select(index =>
        {
            var positive = index % 2 == 0;
            var label = positive ? "MATERIAL_EVENT" : "NON_MATERIAL";
            var prediction = index == 9 ? "MATERIAL" : (positive ? "RISK" : "UNCLASSIFIED");
            return new CatalystHumanReviewRecord(
                "review-" + index, "TEST", "article-" + index, "headline",
                "https://example.com/" + index, start.AddDays(index), "Example",
                label, "MCC-CATALYST-TRIAGE-1.0", null, start.AddDays(index),
                prediction, Array.Empty<string>());
        }).ToArray();

        var result = CatalystHumanReviewValidation.Evaluate(records);

        Assert.Equal("TIME_ORDERED_HOLDOUT_METRICS_AVAILABLE", result.Status);
        Assert.Equal(8, result.TrainingCount);
        Assert.Equal(2, result.HoldoutCount);
        Assert.Equal(start.AddDays(8), result.HoldoutStartUtc);
        Assert.Equal(1, result.Holdout.TruePositive);
        Assert.Equal(1, result.Holdout.FalsePositive);
        Assert.Equal(0, result.Holdout.FalseNegative);
        Assert.Equal(0.5m, result.Holdout.Precision);
        Assert.Equal(1m, result.Holdout.Recall);
    }

    [Fact]
    public void ValidationExcludesUncertainDuplicateAndConflictingArticleReviews()
    {
        var now = DateTimeOffset.Parse("2026-11-06T20:00:00Z");
        var records = new[]
        {
            Review("1", "a", "MATERIAL_EVENT", "MATERIAL", now.AddMinutes(1)),
            Review("2", "b", "NON_MATERIAL", "UNCLASSIFIED", now.AddMinutes(2)),
            Review("3", "c", "UNCERTAIN", "RISK", now.AddMinutes(3)),
            Review("4", "d", "DUPLICATE", "MATERIAL", now.AddMinutes(4)),
            Review("5", "e", "MATERIAL_RISK", "RISK", now.AddMinutes(5)),
            Review("6", "e", "NON_MATERIAL", "RISK", now.AddMinutes(6))
        };

        var result = CatalystHumanReviewValidation.Evaluate(records);

        Assert.Equal(4, result.ExcludedReviewCount);
        Assert.Equal(1, result.ConflictingArticleCount);
        var conflict = Assert.Single(result.ConflictingArticles!);
        Assert.Equal("e", conflict.ArticleId);
        Assert.Equal(new[] { "MATERIAL_RISK", "NON_MATERIAL" }, conflict.ConflictingLabels);
        Assert.Equal(2, conflict.Reviews.Count);
        Assert.Equal(2, result.IncludedUniqueReviewCount);
        Assert.Equal("INSUFFICIENT_LABELS", result.Status);
        Assert.Contains("at least 6 unique", result.Note);
    }

    private static CatalystHumanReviewRecord Review(
        string id, string articleId, string label, string prediction, DateTimeOffset reviewedAt) =>
        new(id, "TEST", articleId, "headline", "https://example.com/" + articleId,
            reviewedAt, "Example", label, "MCC-CATALYST-TRIAGE-1.0", null,
            reviewedAt, prediction, Array.Empty<string>());


    [Fact]
    public void ValidationExplainsWhySavedReviewsAreExcluded()
    {
        var now = DateTimeOffset.Parse("2026-11-06T20:00:00Z");
        var records = new[]
        {
            Review("1", "a", "MATERIAL_EVENT", "MATERIAL", now.AddMinutes(1)),
            Review("2", "b", "UNCERTAIN", "RISK", now.AddMinutes(2)),
            Review("3", "c", "NON_MATERIAL", "UNCLASSIFIED", now.AddMinutes(3)) with { PredictedSeverity = null }
        };

        var result = CatalystHumanReviewValidation.Evaluate(records);

        Assert.Equal(1, result.IncludedUniqueReviewCount);
        Assert.Equal(2, result.ExcludedReviewCount);
        Assert.Equal(1, result.ExclusionBreakdown!.NonEvaluableLabel);
        Assert.Equal(1, result.ExclusionBreakdown.MissingSavedPrediction);
        Assert.Equal(0, result.ExclusionBreakdown.DuplicateSameLabelReview);
        Assert.Equal(0, result.ExclusionBreakdown.ConflictingArticleReview);
    }

    [Fact]
    public async Task ExplicitSupersessionResolvesArticleConflictWithoutDeletingAuditHistory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mcc-catalyst-review-tests", Guid.NewGuid().ToString("N"));
        var store = new CatalystHumanReviewStore(Path.Combine(directory, "reviews.jsonl"));
        try
        {
            var published = DateTimeOffset.Parse("2026-11-06T20:00:00Z");
            var first = await store.AppendAsync(new CatalystHumanReviewInput(
                "EWTX", "article-1", "Headline", "https://example.com/a", published,
                "Example", "MATERIAL_RISK", null, "First adjudication", "UNCLASSIFIED"),
                CancellationToken.None);
            await store.AppendAsync(new CatalystHumanReviewInput(
                "EWTX", "article-1", "Headline", "https://example.com/a", published,
                "Example", "NON_MATERIAL", null, "Conflicting adjudication", "UNCLASSIFIED"),
                CancellationToken.None);

            var correction = await store.AppendAsync(new CatalystHumanReviewInput(
                "EWTX", "article-1", "Headline", "https://example.com/a", published,
                "Example", "MATERIAL_RISK", null, "Reviewed source; supersedes incorrect prior label",
                "UNCLASSIFIED", null, new[] { first.ReviewId }),
                CancellationToken.None);

            var records = await store.GetLatestAsync(null, 20, CancellationToken.None);
            var result = CatalystHumanReviewValidation.Evaluate(records);
            Assert.Equal(3, result.RawReviewCount);
            Assert.Equal(0, result.ConflictingArticleCount);
            Assert.Equal(1, result.IncludedUniqueReviewCount);
            Assert.Equal(2, result.ExclusionBreakdown!.SupersededReviewCount);
            Assert.Contains(first.ReviewId, correction.SupersedesReviewIds!);
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
