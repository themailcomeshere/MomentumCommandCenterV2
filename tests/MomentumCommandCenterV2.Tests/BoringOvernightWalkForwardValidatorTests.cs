using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightWalkForwardValidatorTests
{
    [Fact]
    public void UsesEarlierTrainingSessionsAndLaterUnseenTestSessions()
    {
        var observations = BuildObservations(
            Enumerable.Range(1, 20).Select(x => (HighReturn: (decimal)x, Adverse: 0.5m))
                .Concat(Enumerable.Range(15, 20).Select(x => (HighReturn: (decimal)x, Adverse: 0.25m))));

        var result = BoringOvernightWalkForwardValidator.Validate(observations, 75);

        Assert.Equal("PASS", result.Status);
        Assert.True(result.Passed);
        Assert.Equal(20, result.TrainingSessionCount);
        Assert.Equal(20, result.TestSessionCount);
        Assert.Equal(100m, result.TestCoveragePct);
        Assert.Equal(15m, result.TrainingTargetReturnPct);
        Assert.Equal(30m, result.TrainingExpectedTouchRatePct);
        Assert.Equal(100m, result.TestTargetTouchRatePct);
        Assert.Equal(0.25m, result.TestMaximumAdverseExcursionPct);
    }

    [Fact]
    public void FailsWhenUnseenTargetTouchRateFallsBelowTrainingBaseline()
    {
        var observations = BuildObservations(
            Enumerable.Range(1, 20).Select(x => (HighReturn: (decimal)x, Adverse: 0.5m))
                .Concat(Enumerable.Range(1, 20).Select(x => (HighReturn: (decimal)x, Adverse: 0.25m))));

        var result = BoringOvernightWalkForwardValidator.Validate(observations, 75);

        Assert.Equal("FAILED", result.Status);
        Assert.False(result.Passed);
        Assert.Equal(0m, result.TestTargetTouchRatePct);
    }

    [Fact]
    public void FailsWhenUnseenAdverseExcursionExceedsTrainingNinetyFifthPercentile()
    {
        var observations = BuildObservations(
            Enumerable.Range(1, 20).Select(x => (HighReturn: (decimal)x, Adverse: 0.5m))
                .Concat(Enumerable.Range(15, 20).Select(x => (HighReturn: (decimal)x, Adverse: 1.0m))));

        var result = BoringOvernightWalkForwardValidator.Validate(observations, 75);

        Assert.Equal("FAILED", result.Status);
        Assert.False(result.Passed);
        Assert.Equal(1m, result.TestMaximumAdverseExcursionPct);
        Assert.Equal(0.5m, result.TrainingAdverseExcursionP95Pct);
    }

    [Fact]
    public void BlocksWhenThereAreNotTwentyTrainingAndTwentyTestSessions()
    {
        var observations = BuildObservations(
            Enumerable.Range(1, 39).Select(x => (HighReturn: (decimal)x, Adverse: 0.5m)));

        var result = BoringOvernightWalkForwardValidator.Validate(observations, 75);

        Assert.Equal("INSUFFICIENT_SAMPLE", result.Status);
        Assert.False(result.Passed);
        Assert.Null(result.TrainingTargetReturnPct);
    }

    [Fact]
    public void BlocksWhenAnyTrainingOrTestSessionHasIncompleteCoverage()
    {
        var observations = BuildObservations(
            Enumerable.Range(1, 40).Select(x => (HighReturn: (decimal)x, Adverse: 0.5m))).ToArray();
        observations[25] = observations[25] with { CoverageComplete = false, MissingIntervalCount = 1 };

        var result = BoringOvernightWalkForwardValidator.Validate(observations, 75);

        Assert.Equal("INSUFFICIENT_COVERAGE", result.Status);
        Assert.False(result.Passed);
        Assert.Equal(97.5m, result.TestCoveragePct);
    }

    private static IReadOnlyList<BoringOvernightSellWindowObservation> BuildObservations(
        IEnumerable<(decimal HighReturn, decimal Adverse)> returns)
    {
        var start = new DateOnly(2026, 1, 2);
        return returns.Select((x, index) =>
        {
            var reference = 100m;
            return new BoringOvernightSellWindowObservation(
                start.AddDays(index), reference, reference * (1m + x.HighReturn / 100m),
                true, 0)
            {
                LowestEligiblePrice = reference * (1m - x.Adverse / 100m)
            };
        }).ToArray();
    }
}
