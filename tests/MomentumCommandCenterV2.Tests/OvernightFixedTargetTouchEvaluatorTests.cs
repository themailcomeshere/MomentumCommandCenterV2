using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class OvernightFixedTargetTouchEvaluatorTests
{
    private static OvernightFixedTargetTouchInput GoodInput() => new()
    {
        Symbol = " bmy ",
        SessionDate = new DateOnly(2026, 10, 20),
        ReferenceClose = 100m,
        TargetReturnPercent = 0.5m,
        TickSize = 0.01m,
        ObservedHigh = 100.50m,
        ObservedHighTimestamp = new DateTimeOffset(2026, 10, 21, 8, 15, 0, TimeSpan.FromHours(-4)),
        WindowStart = new DateTimeOffset(2026, 10, 21, 7, 0, 0, TimeSpan.FromHours(-4)),
        WindowEnd = new DateTimeOffset(2026, 10, 21, 20, 0, 0, TimeSpan.FromHours(-4)),
        SourceName = "test fixture source",
        Feed = "test fixture feed",
        CoverageComplete = true
    };

    [Fact]
    public void EvaluatesFixedClosePlusHalfPercentTarget()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(GoodInput());

        Assert.True(result.IsEvaluable);
        Assert.Equal("BMY", result.Symbol);
        Assert.Equal(100.50m, result.TargetPrice);
        Assert.True(result.TargetTouched);
        Assert.Equal("TOUCHED", result.Status);
        Assert.Contains("HISTORICAL_HIGH_TOUCH_IS_NOT_A_FILL", result.ReasonCodes);
        Assert.Contains("NO_ORDER_SUBMITTED", result.ReasonCodes);
    }

    [Fact]
    public void DoesNotCountHighBelowTargetAsTouch()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with { ObservedHigh = 100.49m });

        Assert.True(result.IsEvaluable);
        Assert.False(result.TargetTouched);
        Assert.Equal("NOT_TOUCHED", result.Status);
    }

    [Fact]
    public void RoundsTargetUpToTickSoItDoesNotUndercutRequestedReturn()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with { ReferenceClose = 10.01m, TickSize = 0.05m });

        Assert.True(result.IsEvaluable);
        Assert.Equal(10.10m, result.TargetPrice);
    }

    [Fact]
    public void IncompleteSessionCoverageReturnsDataGapAndNoTouchAnswer()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with { CoverageComplete = false });

        Assert.False(result.IsEvaluable);
        Assert.Null(result.TargetTouched);
        Assert.Equal("DATA_GAP", result.Status);
        Assert.Contains("DATA_GAP_OR_COVERAGE_UNVERIFIED", result.ReasonCodes);
    }

    [Fact]
    public void MissingProvenanceReturnsDataGap()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with { SourceName = null });

        Assert.False(result.IsEvaluable);
        Assert.Null(result.TargetTouched);
        Assert.Equal("DATA_GAP", result.Status);
        Assert.Contains("SOURCE_MISSING", result.ReasonCodes);
    }

    [Fact]
    public void HighOutsideEligibleWindowIsNotCounted()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with
            {
                ObservedHighTimestamp = new DateTimeOffset(2026, 10, 21, 6, 59, 0, TimeSpan.FromHours(-4))
            });

        Assert.False(result.IsEvaluable);
        Assert.Null(result.TargetTouched);
        Assert.Equal("DATA_GAP", result.Status);
        Assert.Contains("HIGH_TIMESTAMP_OUTSIDE_ELIGIBLE_WINDOW", result.ReasonCodes);
    }

    [Fact]
    public void InvalidCloseProducesNoRecommendation()
    {
        var result = new OvernightFixedTargetTouchEvaluator().Evaluate(
            GoodInput() with { ReferenceClose = 0m });

        Assert.False(result.IsEvaluable);
        Assert.Null(result.TargetPrice);
        Assert.Equal("INVALID_INPUT", result.Status);
        Assert.Contains("REFERENCE_CLOSE_INVALID", result.ReasonCodes);
    }
}
