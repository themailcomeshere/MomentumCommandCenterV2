namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Compares the fixed keyword-triage prediction saved at review time with human labels.
/// Uses a chronological holdout; it does not train or tune the classifier.
/// </summary>
public static class CatalystHumanReviewValidation
{
    public static CatalystHumanReviewValidationResult Evaluate(
        IReadOnlyCollection<CatalystHumanReviewRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var eligible = records
            .Where(x => x.PredictedSeverity is "RISK" or "MATERIAL" or "UNCLASSIFIED")
            .Where(x => TryGetHumanTruth(x.Label, out _))
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ArticleId)
                ? x.Url.Trim()
                : x.ArticleId.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var conflicts = eligible.Count(group =>
            group.Select(x => x.Label).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);
        var deduplicated = eligible
            .Where(group => group.Select(x => x.Label).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            .Select(group => group.OrderByDescending(x => x.ReviewedAtUtc).First())
            .OrderBy(x => x.ReviewedAtUtc)
            .ThenBy(x => x.ReviewId, StringComparer.Ordinal)
            .ToArray();

        var excluded = records.Count - deduplicated.Length;
        if (deduplicated.Length < 2)
            return Empty(deduplicated.Length, excluded, conflicts,
                "INSUFFICIENT_LABELS: at least two unique, non-conflicting reviews with a saved triage prediction are required.");

        // The later 20% of reviewed articles is held out chronologically. This does not
        // tune the fixed rules; it prevents reporting in-sample metrics as holdout results.
        var splitIndex = Math.Clamp((int)Math.Floor(deduplicated.Length * 0.8), 1, deduplicated.Length - 1);
        var training = deduplicated[..splitIndex];
        var holdout = deduplicated[splitIndex..];
        var trainMetrics = Calculate(training);
        var testMetrics = Calculate(holdout);
        var hasBothTruthClasses = testMetrics.ActualPositiveCount > 0 && testMetrics.ActualNegativeCount > 0;
        var status = hasBothTruthClasses
            ? "TIME_ORDERED_HOLDOUT_METRICS_AVAILABLE"
            : "INSUFFICIENT_HOLDOUT_CLASS_COVERAGE";

        return new CatalystHumanReviewValidationResult(
            status,
            "MCC-CATALYST-HUMAN-VALIDATION-1.0",
            records.Count,
            deduplicated.Length,
            excluded,
            conflicts,
            training.Length,
            holdout.Length,
            holdout[0].ReviewedAtUtc,
            holdout[^1].ReviewedAtUtc,
            trainMetrics,
            testMetrics,
            "Human-positive labels are MATERIAL_RISK/MATERIAL_EVENT; human-negative labels are NON_MATERIAL/IRRELEVANT. DUPLICATE, UNCERTAIN, legacy reviews without a saved prediction, and conflicting duplicate-article labels are excluded. A prediction is positive for RISK or MATERIAL and negative for UNCLASSIFIED. These are descriptive metrics for the fixed triage rules, not a trained classifier, not evidence of catalyst clearance, and not sufficient by themselves to approve production use.");
    }

    private static CatalystHumanReviewConfusionMetrics Calculate(
        IReadOnlyCollection<CatalystHumanReviewRecord> rows)
    {
        var tp = 0;
        var fp = 0;
        var tn = 0;
        var fn = 0;
        foreach (var row in rows)
        {
            TryGetHumanTruth(row.Label, out var actualPositive);
            var predictedPositive = row.PredictedSeverity is "RISK" or "MATERIAL";
            if (actualPositive && predictedPositive) tp++;
            else if (!actualPositive && predictedPositive) fp++;
            else if (!actualPositive && !predictedPositive) tn++;
            else fn++;
        }

        return new CatalystHumanReviewConfusionMetrics(
            rows.Count,
            tp,
            fp,
            tn,
            fn,
            tp + fn,
            tn + fp,
            tp + fp == 0 ? null : (decimal)tp / (tp + fp),
            tp + fn == 0 ? null : (decimal)tp / (tp + fn),
            tn + fp == 0 ? null : (decimal)tn / (tn + fp),
            tp + fp + tn + fn == 0 ? null : (decimal)(tp + tn) / (tp + fp + tn + fn));
    }

    private static bool TryGetHumanTruth(string label, out bool positive)
    {
        positive = label switch
        {
            "MATERIAL_RISK" or "MATERIAL_EVENT" => true,
            "NON_MATERIAL" or "IRRELEVANT" => false,
            _ => false
        };
        return label is "MATERIAL_RISK" or "MATERIAL_EVENT" or "NON_MATERIAL" or "IRRELEVANT";
    }

    private static CatalystHumanReviewValidationResult Empty(int included, int excluded, int conflicts, string note) =>
        new("INSUFFICIENT_LABELS", "MCC-CATALYST-HUMAN-VALIDATION-1.0",
            included + excluded, included, excluded, conflicts, 0, 0, null, null,
            Calculate(Array.Empty<CatalystHumanReviewRecord>()),
            Calculate(Array.Empty<CatalystHumanReviewRecord>()), note);
}

public sealed record CatalystHumanReviewConfusionMetrics(
    int SampleCount,
    int TruePositive,
    int FalsePositive,
    int TrueNegative,
    int FalseNegative,
    int ActualPositiveCount,
    int ActualNegativeCount,
    decimal? Precision,
    decimal? Recall,
    decimal? Specificity,
    decimal? Accuracy);

public sealed record CatalystHumanReviewValidationResult(
    string Status,
    string ValidationVersion,
    int RawReviewCount,
    int IncludedUniqueReviewCount,
    int ExcludedReviewCount,
    int ConflictingArticleCount,
    int TrainingCount,
    int HoldoutCount,
    DateTimeOffset? HoldoutStartUtc,
    DateTimeOffset? HoldoutEndUtc,
    CatalystHumanReviewConfusionMetrics Training,
    CatalystHumanReviewConfusionMetrics Holdout,
    string Note);
