namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Builds a research candidate limit from the distribution of historical maximum favorable
/// prices during the complete eligible sell window. ATR is reported as volatility context;
/// it is not multiplied by an unvalidated coefficient to manufacture a target.
/// </summary>
public static class BoringOvernightSellLimitCalculator
{
    public static BoringOvernightSellLimitResult Calculate(
        decimal currentReferenceClose,
        decimal currentAtr,
        DateTimeOffset atrAsOfUtc,
        string atrParityStatus,
        IReadOnlyList<BoringOvernightSellWindowObservation> history,
        int targetQuantilePct,
        int requiredWindow = 20)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (requiredWindow < 20)
            throw new ArgumentOutOfRangeException(nameof(requiredWindow), "Sell-limit research requires at least 20 sessions.");
        if (targetQuantilePct is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(targetQuantilePct), "Target quantile must be between 1 and 99.");
        if (currentReferenceClose <= 0 || currentAtr <= 0 || atrAsOfUtc == default || atrAsOfUtc.Offset != TimeSpan.Zero)
            return Missing("Current reference close, positive ATR, and a UTC ATR timestamp are required.");
        if (!string.Equals(atrParityStatus, "MATCH", StringComparison.OrdinalIgnoreCase))
            return new("NOT VERIFIED", null, null, null, null, 0, history.Count, 0,
                "Exact ATR/Thinkorswim parity is not MATCH; numeric limit withheld.");

        if (history.Count < requiredWindow)
            return Missing($"Need {requiredWindow} complete eligible-window sessions; received {history.Count}.", history.Count);

        var selected = history.OrderBy(x => x.SessionDate).TakeLast(requiredWindow).ToArray();
        if (selected.Select(x => x.SessionDate).Distinct().Count() != selected.Length)
            return Missing("Duplicate eligible-window session dates were supplied.", selected.Length);
        if (selected.Any(x => !x.CoverageComplete || x.ReferenceClose <= 0 || x.HighestEligiblePrice <= 0 || x.MissingIntervalCount != 0))
            return Missing("At least one historical eligible sell window is incomplete or invalid; numeric limit withheld.", selected.Length,
                selected.Count(x => !x.CoverageComplete || x.MissingIntervalCount != 0));

        var favorableReturns = selected
            .Select(x => (x.HighestEligiblePrice / x.ReferenceClose - 1m) * 100m)
            .OrderBy(x => x)
            .ToArray();
        // Nearest-rank percentile: rank = ceil(p * n). No interpolation or hidden rounding.
        var rank = (int)Math.Ceiling(targetQuantilePct / 100m * favorableReturns.Length);
        var targetReturnPct = favorableReturns[Math.Clamp(rank - 1, 0, favorableReturns.Length - 1)];
        var touchCount = favorableReturns.Count(x => x >= targetReturnPct);
        var touchRate = touchCount * 100m / favorableReturns.Length;
        if (targetReturnPct <= 0m)
        {
            return new("NO_POSITIVE_TARGET", null, targetReturnPct, null, null,
                selected.Length, selected.Length, 0,
                "The complete eligible-window sample is valid, but the selected maximum-favorable-return quantile is zero or negative. No positive-upside sell target is issued. This is an observed adverse/flat outcome, not missing data.");
        }

        var limit = decimal.Round(currentReferenceClose * (1m + targetReturnPct / 100m), 2, MidpointRounding.AwayFromZero);
        var atrDistance = (limit - currentReferenceClose) / currentAtr;

        return new("PROVISIONAL", limit, targetReturnPct, touchRate, atrDistance,
            selected.Length, selected.Length, 0,
            "Historical maximum-favorable-price quantile over complete eligible windows. Touch rate is an in-sample descriptive statistic, not a fill probability. Walk-forward validation and actual execution records are still required.");
    }

    private static BoringOvernightSellLimitResult Missing(string reason, int supplied = 0, int incomplete = 0) =>
        new("DATA?", null, null, null, null, 0, supplied, incomplete, reason);
}

public sealed record BoringOvernightSellWindowObservation(
    DateOnly SessionDate,
    decimal ReferenceClose,
    decimal HighestEligiblePrice,
    bool CoverageComplete,
    int MissingIntervalCount)
{
    public decimal LowestEligiblePrice { get; init; }
}

public sealed record BoringOvernightSellLimitResult(
    string Status,
    decimal? CalculatedLimitSell,
    decimal? TargetReturnPct,
    decimal? HistoricalTouchRatePct,
    decimal? TargetDistanceAtr,
    int ValidSessionCount,
    int SuppliedSessionCount,
    int IncompleteSessionCount,
    string Explanation);
