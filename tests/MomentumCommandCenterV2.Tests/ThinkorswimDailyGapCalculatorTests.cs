using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class ThinkorswimDailyGapCalculatorTests
{
    [Fact]
    public void CalculatesAllEightScannerFieldsFromTwentyOneDailyBars()
    {
        var bars = Enumerable.Range(0, 21).Select(i =>
        {
            var close = 100m + i;
            var open = close * (i == 20 ? 1.02m : i % 3 == 0 ? 0.99m : 1.01m);
            return new ThinkorswimDailyGapBar(new DateOnly(2026, 10, 1).AddDays(i),
                open, Math.Max(open, close) + 1m, Math.Min(open, close) - 1m, close);
        }).ToArray();
        var request = new ThinkorswimDailyGapImport("TEST", "Thinkorswim Strategy Scanner export",
            "DAY", true, true, "America/Phoenix", bars);

        var result = ThinkorswimDailyGapCalculator.Calculate(request);

        Assert.Equal("MCC-TOS-DAILY-GAP-1.0", result.CalculationVersion);
        Assert.Equal("COMPLETE_20_GAPS", result.Status);
        Assert.Equal(21, result.BarCount);
        Assert.Equal(20, result.ReturnCount);
        Assert.NotNull(result.OnWinPct);
        Assert.NotNull(result.AvgOnPct);
        Assert.NotNull(result.MedianOnPct);
        Assert.NotNull(result.FiveDayOnPct);
        Assert.NotNull(result.OnMomentum);
        Assert.NotNull(result.CurrentOnPct);
        Assert.NotNull(result.Above20Sma);
        Assert.NotNull(result.OvernightScore);
        var expected = (result.OnWinPct!.Value / 100m * 50m)
            + (result.AvgOnPct!.Value / 100m * 1000m)
            + (result.MedianOnPct!.Value / 100m * 1000m)
            + (result.OnMomentum!.Value / 100m * 1000m)
            + (result.Above20Sma == 1 ? 10m : 0m);
        Assert.Equal(expected, result.OvernightScore);
    }

    [Fact]
    public void WithholdsAllStatisticsUntilTwentyGapObservationsExist()
    {
        var bars = Enumerable.Range(0, 20).Select(i =>
            new ThinkorswimDailyGapBar(new DateOnly(2026, 10, 1).AddDays(i), 101m, 102m, 99m, 100m)).ToArray();
        var result = ThinkorswimDailyGapCalculator.Calculate(new("TEST", "Thinkorswim export",
            "DAY", false, true, "America/Phoenix", bars));
        Assert.Equal("INSUFFICIENT_HISTORY", result.Status);
        Assert.Null(result.OnWinPct);
        Assert.Null(result.OvernightScore);
    }

    [Fact]
    public void RejectsDuplicateDatesAndInvalidSourceMetadata()
    {
        var bar = new ThinkorswimDailyGapBar(new DateOnly(2026, 10, 1), 100m, 101m, 99m, 100m);
        var duplicate = new ThinkorswimDailyGapImport("TEST", "Thinkorswim export", "DAY", false, true,
            "America/Phoenix", new[] { bar, bar });
        Assert.Throws<InvalidDataException>(() => ThinkorswimDailyGapCalculator.Calculate(duplicate));
        var wrongSource = duplicate with { Source = "Alpaca" };
        Assert.Throws<ArgumentException>(() => ThinkorswimDailyGapCalculator.Calculate(wrongSource));
    }
}
