using Xunit;
using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightEvaluatorTests
{
    [Fact]
    public void MissingCatalystOrPermissionCannotProduceReady()
    {
        var request = ReadyRequest() with { CatalystStatus = "UNKNOWN", FiveMinutePermission = null };
        var result = BoringOvernightEvaluator.Evaluate(request);
        Assert.Equal("DATA?", result.Status);
        Assert.False(result.Eligible);
        Assert.NotEmpty(result.MissingInputs);
    }

    [Fact]
    public void FailedVolumeGateBlocksCandidate()
    {
        var result = BoringOvernightEvaluator.Evaluate(ReadyRequest() with { AverageDailyVolume20 = 499_999 });
        Assert.Equal("BLOCKED", result.Status);
        Assert.Contains(result.Blockers, x => x.Contains("500,000", StringComparison.Ordinal));
    }

    [Fact]
    public void PassingGatesButHotExtensionIsWatchNotReady()
    {
        var result = BoringOvernightEvaluator.Evaluate(ReadyRequest() with { ExtensionAtr = 2.1 });
        Assert.Equal("WATCH", result.Status);
        Assert.False(result.Eligible);
        Assert.NotEmpty(result.Warnings);
    }

    private static BoringOvernightEvaluationRequest ReadyRequest() => new(
        "TEST", true, 25, 24, 1_000_000, 75, 0.45, 0.65, 0.40, 0.25,
        false, "CLEAR", true, "BULLISH", "EXECUTION READY", 0.8, 1.2, 1.8, 1.5);
}
