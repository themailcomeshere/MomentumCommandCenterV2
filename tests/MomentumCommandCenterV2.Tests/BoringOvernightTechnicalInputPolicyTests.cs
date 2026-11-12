using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightTechnicalInputPolicyTests
{
    [Fact]
    public void AcceptsInputsOnlyWhenBothTimeframesAreFreshAndV2Evaluated()
    {
        Assert.True(BoringOvernightTechnicalInputPolicy.AreIntradayInputsUsable(
            feedMatchesCapture: true, oneMinuteFresh: true, fiveMinuteFresh: true, evaluationStatus: "EVALUATED"));
    }

    [Theory]
    [InlineData(false, true, true, "EVALUATED")]
    [InlineData(true, false, true, "EVALUATED")]
    [InlineData(true, true, false, "EVALUATED")]
    [InlineData(true, null, true, "EVALUATED")]
    [InlineData(true, true, null, "EVALUATED")]
    [InlineData(true, true, true, "STALE_MARKET_DATA")]
    [InlineData(true, true, true, "OUTSIDE_REGULAR_SESSION")]
    [InlineData(true, true, true, "RVOL_BASELINE_INSUFFICIENT")]
    [InlineData(true, true, true, null)]
    public void RejectsStaleUnknownMismatchedOrUnevaluatedEvidence(
        bool feedMatchesCapture, bool? oneMinuteFresh, bool? fiveMinuteFresh, string? evaluationStatus)
    {
        Assert.False(BoringOvernightTechnicalInputPolicy.AreIntradayInputsUsable(
            feedMatchesCapture, oneMinuteFresh, fiveMinuteFresh, evaluationStatus));
    }
}
