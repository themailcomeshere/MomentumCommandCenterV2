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

    [Theory]
    [InlineData(double.NaN, "price (non-finite)")]
    [InlineData(double.PositiveInfinity, "price (non-finite)")]
    [InlineData(double.NegativeInfinity, "price (non-finite)")]
    public void NonFiniteNumericInputFailsClosed(double price, string expectedMissingInput)
    {
        var result = BoringOvernightEvaluator.Evaluate(ReadyRequest() with { Price = price });

        Assert.Equal("DATA?", result.Status);
        Assert.False(result.Eligible);
        Assert.Contains(expectedMissingInput, result.MissingInputs);
        Assert.Contains(result.Reasons, reason => reason.Contains("Invalid input", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("SELL")]
    [InlineData("GET OUT")]
    [InlineData("BLOCK")]
    public void OneMinuteExecutionExitStateBlocksOvernightQualification(string state)
    {
        var result = BoringOvernightEvaluator.Evaluate(
            ReadyRequest() with { OneMinuteState = state });

        Assert.Equal("BLOCKED", result.Status);
        Assert.False(result.Eligible);
        Assert.Contains(result.Blockers, x =>
            x.Contains("1M state", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Reasons, x =>
            x.Contains("market is closed", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("trading day has ended", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InvalidSymbolFailsClosedWithoutThrowing()
    {
        var result = BoringOvernightEvaluator.Evaluate(ReadyRequest() with { Symbol = "TEST/../../" });

        Assert.Equal("DATA?", result.Status);
        Assert.False(result.Eligible);
        Assert.Contains("symbol", result.MissingInputs);
    }

    /// <summary>
    /// PURPOSE: Price gate boundary (operator decision 2026-11-16): exactly $5.00 passes, $4.99 blocks.
    /// The SMA20 is set below both prices so only the price gate is tested.
    /// WHEN CALLED: By xUnit once per row.
    /// </summary>
    [Theory]
    [InlineData(5.00, "READY")]
    [InlineData(4.99, "BLOCKED")]
    public void PriceGateBoundary_FiveDollarsPasses(double price, string expectedStatus)
    {
        var result = BoringOvernightEvaluator.Evaluate(ReadyRequest() with { Price = price, Sma20 = 4.90 });

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(price < 5, result.Blockers.Any(x => x.Contains("at least $5", StringComparison.Ordinal)));
    }

    private static BoringOvernightEvaluationRequest ReadyRequest() => new(
        "TEST", true, 25, 24, 1_000_000, 75, 0.45, 0.65, 0.40, 0.25,
        false, "CLEAR", true, "BULLISH", "EXECUTION READY", 0.8, 1.2, 1.8, 1.5);
}
