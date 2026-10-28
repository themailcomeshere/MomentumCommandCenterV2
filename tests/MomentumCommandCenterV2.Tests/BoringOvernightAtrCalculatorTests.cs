using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.Core.Models;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightAtrCalculatorTests
{
    [Fact]
    public void CalculatesWilderAtrAndAtrPercentFromExplicitBars()
    {
        var bars = new[]
        {
            Bar(0, 10m, 11m, 9m, 10m),
            Bar(1, 10m, 12m, 10m, 11m), // TR = 2
            Bar(2, 11m, 13m, 10m, 12m), // TR = 3
            Bar(3, 12m, 13m, 11m, 12m)  // TR = 2; 2-period Wilder ATR seed 2.5, smoothed with final TR to 2.25
        };

        var result = BoringOvernightAtrCalculator.Calculate(bars, 2);

        Assert.Equal(2, result.Period);
        Assert.Equal(BarTimeframe.FiveMinute, result.Timeframe);
        Assert.Equal(2m, result.LatestTrueRange);
        Assert.Equal(2.5m, result.Atr);
        Assert.Equal(2.25m / 12m * 100m, result.AtrPercent);
        Assert.Contains("Wilder ATR", result.Provenance);
    }

    [Fact]
    public void SignedExtensionUsesExplicitReferencePrice()
    {
        Assert.Equal(2m, BoringOvernightAtrCalculator.CalculateSignedExtensionAtr(12m, 10m, 1m));
        Assert.Equal(-2m, BoringOvernightAtrCalculator.CalculateSignedExtensionAtr(8m, 10m, 1m));
    }

    [Fact]
    public void InsufficientHistoryFailsRatherThanEstimating()
    {
        Assert.Throws<ArgumentException>(() =>
            BoringOvernightAtrCalculator.Calculate(new[] { Bar(0, 10m, 11m, 9m, 10m) }, 1));
    }

    [Fact]
    public void MixedTimeframesFailClosed()
    {
        var bars = new[]
        {
            Bar(0, 10m, 11m, 9m, 10m),
            Bar(1, 10m, 11m, 9m, 10m) with { Timeframe = BarTimeframe.OneMinute }
        };
        Assert.Throws<ArgumentException>(() => BoringOvernightAtrCalculator.Calculate(bars, 1));
    }

    private static MarketBar Bar(int minute, decimal open, decimal high, decimal low, decimal close) =>
        new("TEST", BarTimeframe.FiveMinute, DateTimeOffset.Parse("2026-10-28T16:00:00Z").AddMinutes(minute * 5),
            open, high, low, close, 1000m);
}
