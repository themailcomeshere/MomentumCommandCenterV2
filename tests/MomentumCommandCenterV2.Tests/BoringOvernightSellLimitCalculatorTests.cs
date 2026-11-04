using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightSellLimitCalculatorTests
{
    [Fact]
    public void CalculatesQuantileLimitAndAtrContextFromTwentyCompleteWindows()
    {
        var history = Enumerable.Range(0, 20).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 100m + i, true, 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            120m, 2m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "MATCH", history, 25);

        Assert.Equal("PROVISIONAL", result.Status);
        Assert.Equal(124.80m, result.CalculatedLimitSell);
        Assert.Equal(4m, result.TargetReturnPct);
        Assert.Equal(80m, result.HistoricalTouchRatePct);
        Assert.Equal(2.4m, result.TargetDistanceAtr);
        Assert.Equal(20, result.ValidSessionCount);
    }

    [Fact]
    public void CompleteWindowsWithNonPositiveUpsideAreValidButDoNotProduceSellTarget()
    {
        var history = Enumerable.Range(0, 20).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 99m, true, 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            100m, 1m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "MATCH", history, 25);

        Assert.Equal("NO_POSITIVE_TARGET", result.Status);
        Assert.Null(result.CalculatedLimitSell);
        Assert.Equal(-1m, result.TargetReturnPct);
        Assert.Null(result.HistoricalTouchRatePct);
        Assert.Equal(20, result.ValidSessionCount);
        Assert.Equal(0, result.IncompleteSessionCount);
    }

    [Fact]
    public void CompleteFlatWindowsAreNotMisreportedAsMissingData()
    {
        var history = Enumerable.Range(0, 20).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 100m, true, 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            100m, 1m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "MATCH", history, 50);

        Assert.Equal("NO_POSITIVE_TARGET", result.Status);
        Assert.Null(result.CalculatedLimitSell);
        Assert.Equal(0m, result.TargetReturnPct);
        Assert.Equal(100m, result.HistoricalTouchRatePct);
        Assert.Equal(20, result.ValidSessionCount);
        Assert.Equal(0, result.IncompleteSessionCount);
    }

    [Fact]
    public void IncompleteEligibleWindowWithholdsNumericLimit()
    {
        var history = Enumerable.Range(0, 20).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 101m, i != 7, i == 7 ? 1 : 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            100m, 1m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "MATCH", history, 25);

        Assert.Equal("DATA?", result.Status);
        Assert.Null(result.CalculatedLimitSell);
        Assert.Equal(1, result.IncompleteSessionCount);
    }

    [Fact]
    public void UnverifiedAtrParityWithholdsNumericLimit()
    {
        var history = Enumerable.Range(0, 20).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 101m, true, 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            100m, 1m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "NOT VERIFIED", history, 25);

        Assert.Equal("NOT VERIFIED", result.Status);
        Assert.Null(result.CalculatedLimitSell);
    }

    [Fact]
    public void ShortHistoryWithholdsNumericLimit()
    {
        var history = Enumerable.Range(0, 19).Select(i => new BoringOvernightSellWindowObservation(
            new DateOnly(2026, 9, 1).AddDays(i), 100m, 101m, true, 0)).ToArray();

        var result = BoringOvernightSellLimitCalculator.Calculate(
            100m, 1m, DateTimeOffset.Parse("2026-10-30T20:00:00Z"), "MATCH", history, 25);

        Assert.Equal("DATA?", result.Status);
        Assert.Null(result.CalculatedLimitSell);
    }
}
