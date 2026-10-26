namespace MomentumCommandCenterV2.Infrastructure.MarketData;

// =========================================================
/// CLASS: OvernightLimitSellCalculator
/// PURPOSE:
///   Produce a historical-reach-based limit-sell reference from completed
///   order-eligible-window high excursions, rather than treating median close-to-open
///   return as a price target.
/// SAFETY:
///   Decision support only. Does not place orders or guarantee fills.
///   Results are invalid when the history, cost assumptions, or entry price
///   are missing. Validate the provisional defaults before production use.
// =========================================================
public sealed class OvernightLimitSellCalculator
{
    public const string RulesVersion = "OVERNIGHT-LIMIT-0.1-PROVISIONAL";
    public const int DefaultMinimumSamples = 20;
    public const double DefaultDesiredHistoricalTouchProbabilityPercent = 70.0;
    public const double DefaultMinimumNetProfitPercent = 0.10;
    public const double DefaultEstimatedRoundTripCostsPercent = 0.0;
    public const decimal DefaultTickSize = 0.01m;

    public OvernightLimitSellRecommendation Calculate(OvernightLimitSellInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var normalizedSymbol = input.Symbol?.Trim().ToUpperInvariant() ?? string.Empty;
        var history = input.CompletedEligibleWindowHighExcursionsPercent?
            .Where(double.IsFinite)
            .OrderBy(x => x)
            .ToArray() ?? Array.Empty<double>();

        string? invalid = null;
        if (normalizedSymbol.Length == 0) invalid = "SYMBOL_MISSING";
        else if (input.EntryPrice <= 0) invalid = "ENTRY_PRICE_INVALID";
        else if (input.DesiredHistoricalTouchProbabilityPercent is <= 0 or >= 100)
            invalid = "TOUCH_PROBABILITY_MUST_BE_BETWEEN_0_AND_100";
        else if (input.MinimumNetProfitPercent < 0) invalid = "MINIMUM_NET_PROFIT_CANNOT_BE_NEGATIVE";
        else if (input.EstimatedRoundTripCostsPercent < 0) invalid = "COST_ESTIMATE_CANNOT_BE_NEGATIVE";
        else if (input.TickSize <= 0) invalid = "TICK_SIZE_MUST_BE_POSITIVE";
        else if (history.Length < input.MinimumSamplesRequired)
            invalid = "INSUFFICIENT_OVERNIGHT_HIGH_HISTORY";

        if (invalid is not null)
            return NoRecommendation(input, history.Length, invalid);

        // If the desired touch probability is 70%, use the 30th percentile
        // of historical overnight HIGH excursions. The sample must represent
        // the same entry and trading session the user intends to trade.
        var percentile = 100.0 - input.DesiredHistoricalTouchProbabilityPercent;
        var excursionPercent = PercentileLinear(history, percentile);
        var grossTarget = input.EntryPrice * (1m + (decimal)(excursionPercent / 100.0));
        var costsMultiplier = 1m + (decimal)(input.EstimatedRoundTripCostsPercent / 100.0);
        var minimumNetMultiplier = 1m + (decimal)(input.MinimumNetProfitPercent / 100.0);
        var costAdjustedMinimum = input.EntryPrice * costsMultiplier * minimumNetMultiplier;
        var rawLimit = Math.Max(grossTarget, costAdjustedMinimum);

        // Round down to a valid tick: a sell limit at a lower price is no
        // harder to reach, while the reported historical touch rate is
        // recomputed against the rounded price.
        var tickUnits = decimal.Floor(rawLimit / input.TickSize);
        var limitPrice = tickUnits * input.TickSize;
        var targetReturnPercent = (double)((limitPrice / input.EntryPrice - 1m) * 100m);
        var observedTouchRate = history.Count(x =>
            input.EntryPrice * (1m + (decimal)(x / 100.0)) >= limitPrice) * 100.0 / history.Length;
        var expectedNetReturnPercent = targetReturnPercent - input.EstimatedRoundTripCostsPercent;

        var reasons = new List<string>
        {
            "HISTORICAL_HIGH_EXCURSION_PERCENTILE",
            "NOT_A_GUARANTEED_FILL",
            "NO_ORDER_SUBMITTED"
        };
        if (observedTouchRate + 1e-9 < input.DesiredHistoricalTouchProbabilityPercent)
            reasons.Add("HISTORICAL_TOUCH_RATE_BELOW_REQUESTED");
        if (excursionPercent < input.MinimumNetProfitPercent + input.EstimatedRoundTripCostsPercent)
            reasons.Add("HISTORICAL_EXCURSION_BELOW_MINIMUM_NET_EDGE");

        return new OvernightLimitSellRecommendation(
            normalizedSymbol,
            limitPrice,
            targetReturnPercent,
            excursionPercent,
            input.DesiredHistoricalTouchProbabilityPercent,
            observedTouchRate,
            expectedNetReturnPercent,
            history.Length,
            percentile,
            RulesVersion,
            true,
            reasons);
    }

    private static OvernightLimitSellRecommendation NoRecommendation(
        OvernightLimitSellInput input,
        int samples,
        string reason) =>
        new(
            input.Symbol?.Trim().ToUpperInvariant() ?? string.Empty,
            null, null, null,
            input.DesiredHistoricalTouchProbabilityPercent,
            null, null, samples, null, RulesVersion, false,
            new[] { reason, "NO_ORDER_SUBMITTED" });

    private static double PercentileLinear(IReadOnlyList<double> sorted, double percentile)
    {
        var index = (sorted.Count - 1) * percentile / 100.0;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return sorted[lower];
        var fraction = index - lower;
        return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
    }
}

public sealed record OvernightLimitSellInput
{
    public string? Symbol { get; init; }
    /// <summary>Actual average entry fill price, not automatically the prior close.</summary>
    public decimal EntryPrice { get; init; }
    /// <summary>
    /// Completed historical maximum order-eligible-window high excursions, in percent from
    /// each session's reference entry/close through the relevant sell window.
    /// Do not pass close-to-open returns here.
    /// </summary>
    public IReadOnlyList<double>? CompletedEligibleWindowHighExcursionsPercent { get; init; }
    public double DesiredHistoricalTouchProbabilityPercent { get; init; } =
        OvernightLimitSellCalculator.DefaultDesiredHistoricalTouchProbabilityPercent;
    public double MinimumNetProfitPercent { get; init; } =
        OvernightLimitSellCalculator.DefaultMinimumNetProfitPercent;
    public double EstimatedRoundTripCostsPercent { get; init; } =
        OvernightLimitSellCalculator.DefaultEstimatedRoundTripCostsPercent;
    public decimal TickSize { get; init; } = OvernightLimitSellCalculator.DefaultTickSize;
    public int MinimumSamplesRequired { get; init; } = OvernightLimitSellCalculator.DefaultMinimumSamples;
}

public sealed record OvernightLimitSellRecommendation(
    string Symbol,
    decimal? LimitPrice,
    double? TargetReturnPercent,
    double? HistoricalExcursionPercentile,
    double DesiredHistoricalTouchProbabilityPercent,
    double? ObservedHistoricalTouchRatePercent,
    double? EstimatedNetReturnPercent,
    int SampleCount,
    double? PercentileUsed,
    string RulesVersion,
    bool IsCalculable,
    IReadOnlyList<string> ReasonCodes);
