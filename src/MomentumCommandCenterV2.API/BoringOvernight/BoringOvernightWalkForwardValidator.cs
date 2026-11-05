namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Chronological, session-level walk-forward validation for theoretical sell-limit targets.
/// Training observations determine the target and expected hit rate; later test observations
/// are never used to fit the target. OHLCV high/low touches are theoretical, not broker fills.
/// </summary>
public static class BoringOvernightWalkForwardValidator
{
    public static BoringOvernightWalkForwardResult Validate(
        IReadOnlyList<BoringOvernightSellWindowObservation> observations,
        int targetQuantilePct,
        int minimumTrainingSessions = 20,
        int minimumTestSessions = 20)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (targetQuantilePct is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(targetQuantilePct), "Target quantile must be between 1 and 99.");
        if (minimumTrainingSessions < 20)
            throw new ArgumentOutOfRangeException(nameof(minimumTrainingSessions), "At least 20 training sessions are required.");
        if (minimumTestSessions < 20)
            throw new ArgumentOutOfRangeException(nameof(minimumTestSessions), "At least 20 unseen test sessions are required.");

        var ordered = observations.OrderBy(x => x.SessionDate).ToArray();
        var required = minimumTrainingSessions + minimumTestSessions;
        if (ordered.Length < required)
            return Blocked("INSUFFICIENT_SAMPLE",
                $"Walk-forward validation requires {minimumTrainingSessions} chronological training sessions plus {minimumTestSessions} later unseen test sessions ({required} total); only {ordered.Length} were supplied.",
                ordered.Length, minimumTrainingSessions, minimumTestSessions);

        var selected = ordered.TakeLast(required).ToArray();
        if (selected.Select(x => x.SessionDate).Distinct().Count() != selected.Length)
            return Blocked("INVALID_SAMPLE", "Duplicate sell-session dates prevent a chronological train/test split.",
                selected.Length, minimumTrainingSessions, minimumTestSessions);

        var training = selected.Take(minimumTrainingSessions).ToArray();
        var test = selected.Skip(minimumTrainingSessions).ToArray();
        var invalidTraining = training.Count(IsInvalid);
        var invalidTest = test.Count(IsInvalid);
        var coverage = (training.Length + test.Length - invalidTraining - invalidTest) * 100m / required;
        if (invalidTraining > 0 || invalidTest > 0)
            return new("INSUFFICIENT_COVERAGE", false, training.Length, test.Length,
                training.Length - invalidTraining, test.Length - invalidTest, coverage,
                null, null, null, null, null,
                $"All {required} train/test sessions must have complete OHLCV coverage, a positive reference close, and observed eligible-window high and low. Invalid training sessions: {invalidTraining}; invalid test sessions: {invalidTest}. No target is published.");

        var trainingReturns = training
            .Select(x => (x.HighestEligiblePrice / x.ReferenceClose - 1m) * 100m)
            .OrderBy(x => x)
            .ToArray();
        var targetRank = (int)Math.Ceiling(targetQuantilePct / 100m * trainingReturns.Length);
        var targetReturn = trainingReturns[Math.Clamp(targetRank - 1, 0, trainingReturns.Length - 1)];
        if (targetReturn <= 0m)
            return new("FAILED", false, training.Length, test.Length, training.Length, test.Length, coverage,
                targetReturn, null, 0m, null, null,
                "Training-only target quantile is zero or negative. No positive sell target is supportable.");

        var trainingTouchRate = trainingReturns.Count(x => x >= targetReturn) * 100m / training.Length;
        var testReturns = test.Select(x => (x.HighestEligiblePrice / x.ReferenceClose - 1m) * 100m).ToArray();
        var testTouchCount = testReturns.Count(x => x >= targetReturn);
        var testTouchRate = testTouchCount * 100m / test.Length;

        var trainingAdverse = training
            .Select(AdverseExcursionPct)
            .OrderBy(x => x)
            .ToArray();
        var adverseRank = (int)Math.Ceiling(0.95m * trainingAdverse.Length);
        var trainingAdverseP95 = trainingAdverse[Math.Clamp(adverseRank - 1, 0, trainingAdverse.Length - 1)];
        var testAdverse = test.Select(AdverseExcursionPct).ToArray();
        var meanTestAdverse = testAdverse.Average();
        var maxTestAdverse = testAdverse.Max();

        // Fail closed against two explicit training-derived baselines:
        // (1) unseen target-touch rate must meet the training empirical hit rate;
        // (2) worst unseen adverse excursion must not exceed the training 95th percentile.
        var touchGate = testTouchRate >= trainingTouchRate;
        var adverseGate = maxTestAdverse <= trainingAdverseP95;
        var passed = touchGate && adverseGate;
        var status = passed ? "PASS" : "FAILED";
        var explanation = $"Chronological split: {training.Length} training sessions followed by {test.Length} unseen test sessions; coverage {coverage:0.0}%. " +
            $"Training-only {targetQuantilePct}th-percentile target return: {targetReturn:0.0000}%; " +
            $"training expected touch rate: {trainingTouchRate:0.0}%; unseen test touch rate: {testTouchRate:0.0}% ({testTouchCount}/{test.Length}). " +
            $"Unseen adverse excursion mean/max: {meanTestAdverse:0.0000}%/{maxTestAdverse:0.0000}%; training adverse-excursion P95 bound: {trainingAdverseP95:0.0000}%. " +
            $"Touch-rate gate: {(touchGate ? "PASS" : "FAIL")}; adverse-excursion gate: {(adverseGate ? "PASS" : "FAIL")}. " +
            "Touches are inferred from theoretical OHLCV highs, never broker fills.";
        return new(status, passed, training.Length, test.Length, training.Length, test.Length, coverage,
            targetReturn, trainingTouchRate, testTouchRate, meanTestAdverse, maxTestAdverse,
            explanation, trainingAdverseP95);
    }

    private static bool IsInvalid(BoringOvernightSellWindowObservation x) =>
        !x.CoverageComplete || x.MissingIntervalCount != 0 ||
        x.ReferenceClose <= 0m || x.HighestEligiblePrice <= 0m ||
        x.LowestEligiblePrice <= 0m || x.LowestEligiblePrice > x.HighestEligiblePrice;

    private static decimal AdverseExcursionPct(BoringOvernightSellWindowObservation x) =>
        Math.Max(0m, (x.ReferenceClose - x.LowestEligiblePrice) / x.ReferenceClose * 100m);

    private static BoringOvernightWalkForwardResult Blocked(
        string status, string explanation, int supplied, int training, int test) =>
        new(status, false, Math.Min(supplied, training), Math.Max(0, supplied - training),
            0, 0, 0m, null, null, null, null, null, explanation);
}

public sealed record BoringOvernightWalkForwardResult(
    string Status,
    bool Passed,
    int TrainingSessionCount,
    int TestSessionCount,
    int CompleteTrainingSessionCount,
    int CompleteTestSessionCount,
    decimal TestCoveragePct,
    decimal? TrainingTargetReturnPct,
    decimal? TrainingExpectedTouchRatePct,
    decimal? TestTargetTouchRatePct,
    decimal? TestMeanAdverseExcursionPct,
    decimal? TestMaximumAdverseExcursionPct,
    string Explanation,
    decimal? TrainingAdverseExcursionP95Pct = null);
