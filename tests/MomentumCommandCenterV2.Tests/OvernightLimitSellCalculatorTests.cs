using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class OvernightLimitSellCalculatorTests
{
    private static OvernightLimitSellInput GoodInput() => new()
    {
        Symbol = "AAPL",
        EntryPrice = 100m,
        // Synthetic fixtures only: 20 completed overnight high excursions.
        CompletedOvernightHighExcursionsPercent = new double[]
        {
            -1.0, -0.5, -0.2, 0.0, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35,
            0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.8, 1.0, 1.2
        },
        DesiredHistoricalTouchProbabilityPercent = 70,
        MinimumNetProfitPercent = 0.10,
        EstimatedRoundTripCostsPercent = 0,
        TickSize = 0.01m
    };

    [Fact]
    public void UsesLowerExcursionPercentileForRequestedHistoricalTouchRate()
    {
        var result = new OvernightLimitSellCalculator().Calculate(GoodInput());

        Assert.True(result.IsCalculable);
        Assert.NotNull(result.LimitPrice);
        Assert.Equal(OvernightLimitSellCalculator.RulesVersion, result.RulesVersion);
        Assert.Equal(20, result.SampleCount);
        Assert.Equal(30, result.PercentileUsed);
        Assert.True(result.ObservedHistoricalTouchRatePercent >= 70);
        Assert.Contains("NOT_A_GUARANTEED_FILL", result.ReasonCodes);
        Assert.Contains("NO_ORDER_SUBMITTED", result.ReasonCodes);
    }

    [Fact]
    public void MissingOrInsufficientHistoryProducesNoPrice()
    {
        var result = new OvernightLimitSellCalculator().Calculate(
            GoodInput() with { CompletedOvernightHighExcursionsPercent = new[] { 0.2, 0.3 } });

        Assert.False(result.IsCalculable);
        Assert.Null(result.LimitPrice);
        Assert.Contains("INSUFFICIENT_OVERNIGHT_HIGH_HISTORY", result.ReasonCodes);
    }

    [Fact]
    public void InvalidEntryPriceProducesNoPrice()
    {
        var result = new OvernightLimitSellCalculator().Calculate(GoodInput() with { EntryPrice = 0 });

        Assert.False(result.IsCalculable);
        Assert.Null(result.LimitPrice);
        Assert.Contains("ENTRY_PRICE_INVALID", result.ReasonCodes);
    }

    [Fact]
    public void CostsAndMinimumNetProfitCanRaiseRequiredLimit()
    {
        var baseline = new OvernightLimitSellCalculator().Calculate(GoodInput());
        var costed = new OvernightLimitSellCalculator().Calculate(GoodInput() with
        {
            EstimatedRoundTripCostsPercent = 0.15,
            MinimumNetProfitPercent = 0.20
        });

        Assert.True(costed.IsCalculable);
        Assert.True(costed.LimitPrice > baseline.LimitPrice);
        Assert.True(costed.EstimatedNetReturnPercent >= 0.199);
    }

    [Fact]
    public void ResultNeverRepresentsAnOrderSubmission()
    {
        var result = new OvernightLimitSellCalculator().Calculate(GoodInput());
        Assert.Contains("NO_ORDER_SUBMITTED", result.ReasonCodes);
    }
}
