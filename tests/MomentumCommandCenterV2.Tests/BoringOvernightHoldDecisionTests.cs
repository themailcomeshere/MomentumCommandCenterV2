using Xunit;
using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightHoldDecisionTests
{
    private static readonly TimeZoneInfo Arizona =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "US Mountain Standard Time" : "America/Phoenix");

    [Fact]
    public void ReadyCandidateInsideWindowProducesGreenHoldOvernight()
    {
        var result = BoringOvernightHoldDecision.Evaluate(
            ReadyEvaluation(), AtArizona(12, 42), Arizona);

        Assert.Equal("HOLD_OVERNIGHT", result.Status);
        Assert.Equal("HOLD OVERNIGHT", result.BubbleText);
        Assert.Equal("GREEN", result.Color);
        Assert.True(result.HoldOvernight);
    }

    [Theory]
    [InlineData(12, 30)]
    [InlineData(12, 55)]
    public void WindowEndpointsAreInclusive(int hour, int minute)
    {
        var result = BoringOvernightHoldDecision.Evaluate(
            ReadyEvaluation(), AtArizona(hour, minute), Arizona);
        Assert.Equal("HOLD_OVERNIGHT", result.Status);
    }

    [Theory]
    [InlineData(12, 29)]
    [InlineData(12, 56)]
    [InlineData(15, 0)]
    public void OutsideWindowDoesNotEmitHoldOrGetOut(int hour, int minute)
    {
        var result = BoringOvernightHoldDecision.Evaluate(
            ReadyEvaluation(), AtArizona(hour, minute), Arizona);
        Assert.Equal("OUTSIDE_WINDOW", result.Status);
        Assert.Equal("NO DECISION", result.BubbleText);
        Assert.Equal("NEUTRAL", result.Color);
        Assert.False(result.HoldOvernight);
    }

    [Fact]
    public void FailedGatesInsideWindowProduceRedEodGetOut()
    {
        var result = BoringOvernightHoldDecision.Evaluate(
            ReadyEvaluation() with { Status = "BLOCKED", Eligible = false },
            AtArizona(12, 45), Arizona);
        Assert.Equal("EOD_GET_OUT", result.Status);
        Assert.Equal("EOD GET OUT", result.BubbleText);
        Assert.Equal("RED", result.Color);
        Assert.False(result.HoldOvernight);
    }

    [Fact]
    public void MissingEvidenceInsideWindowProducesYellowDataNotFalseCertainty()
    {
        var result = BoringOvernightHoldDecision.Evaluate(
            ReadyEvaluation() with { Status = "DATA?", Eligible = false },
            AtArizona(12, 45), Arizona);
        Assert.Equal("DATA?", result.Status);
        Assert.Equal("DATA? — NO HOLD", result.BubbleText);
        Assert.Equal("YELLOW", result.Color);
    }

    private static BoringOvernightEvaluation ReadyEvaluation() =>
        new("TEST", "READY", true, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), Array.Empty<string>(), "test");

    private static DateTimeOffset AtArizona(int hour, int minute)
    {
        var date = new DateTime(2026, 10, 28, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(date, Arizona), TimeSpan.Zero);
    }
}
