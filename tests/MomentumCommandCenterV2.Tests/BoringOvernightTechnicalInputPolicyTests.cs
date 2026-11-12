using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightTechnicalInputPolicyTests
{
    [Fact]
    public void FreshMatchedBarsPermitNumericIntradayIndicators()
    {
        Assert.True(BoringOvernightTechnicalInputPolicy.AreIntradayBarsUsable(true, true, true));
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, null, true)]
    [InlineData(true, true, null)]
    public void StaleUnknownOrMismatchedBarsBlockNumericIntradayIndicators(
        bool feedMatchesCapture, bool? oneMinuteFresh, bool? fiveMinuteFresh)
    {
        Assert.False(BoringOvernightTechnicalInputPolicy.AreIntradayBarsUsable(
            feedMatchesCapture, oneMinuteFresh, fiveMinuteFresh));
    }

    [Fact]
    public void FreshBarsOutsideRegularSessionDoNotAuthorizeV2DecisionFields()
    {
        var barsUsable = BoringOvernightTechnicalInputPolicy.AreIntradayBarsUsable(true, true, true);

        Assert.True(barsUsable);
        Assert.False(BoringOvernightTechnicalInputPolicy.IsV2DecisionUsable(barsUsable, "OUTSIDE_REGULAR_SESSION"));
    }

    [Theory]
    [InlineData("STALE_MARKET_DATA")]
    [InlineData("RVOL_BASELINE_INSUFFICIENT")]
    [InlineData("OUTSIDE_REGULAR_SESSION")]
    [InlineData("INSUFFICIENT_HISTORY")]
    [InlineData(null)]
    public void V2DecisionFieldsRequireExplicitSuccessfulEvaluation(string? status)
    {
        Assert.False(BoringOvernightTechnicalInputPolicy.IsV2DecisionUsable(true, status));
    }

    [Fact]
    public void EvaluatedV2DecisionRequiresFreshMatchedBars()
    {
        Assert.True(BoringOvernightTechnicalInputPolicy.IsV2DecisionUsable(true, "EVALUATED"));
        Assert.False(BoringOvernightTechnicalInputPolicy.IsV2DecisionUsable(false, "EVALUATED"));
    }
}
