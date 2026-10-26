namespace MomentumCommandCenterV2.Infrastructure.MarketData;

/// <summary>
/// Evaluates the fixed Boring Overnight strategy target against one completed,
/// source-backed GTC_EXT-eligible sell window. This is a historical touch test,
/// not a fill simulator and not an order instruction.
/// </summary>
public sealed class OvernightFixedTargetTouchEvaluator
{
    public const string RulesVersion = "OVERNIGHT-FIXED-CLOSE-TARGET-0.1";
    public const decimal DefaultTargetReturnPercent = 0.5m;
    public const decimal DefaultTickSize = 0.01m;

    public OvernightFixedTargetTouchResult Evaluate(OvernightFixedTargetTouchInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var symbol = input.Symbol?.Trim().ToUpperInvariant() ?? string.Empty;
        if (symbol.Length == 0)
            return Invalid(input, symbol, "SYMBOL_MISSING");
        if (input.ReferenceClose <= 0)
            return Invalid(input, symbol, "REFERENCE_CLOSE_INVALID");
        if (input.TargetReturnPercent < 0)
            return Invalid(input, symbol, "TARGET_RETURN_CANNOT_BE_NEGATIVE");
        if (input.TickSize <= 0)
            return Invalid(input, symbol, "TICK_SIZE_MUST_BE_POSITIVE");
        if (input.WindowStart is not null && input.WindowEnd is not null &&
            input.WindowEnd <= input.WindowStart)
            return Invalid(input, symbol, "WINDOW_BOUNDS_INVALID");

        var missing = new List<string>();
        if (!input.CoverageComplete) missing.Add("DATA_GAP_OR_COVERAGE_UNVERIFIED");
        if (string.IsNullOrWhiteSpace(input.SourceName)) missing.Add("SOURCE_MISSING");
        if (string.IsNullOrWhiteSpace(input.Feed)) missing.Add("FEED_MISSING");
        if (input.WindowStart is null || input.WindowEnd is null) missing.Add("WINDOW_BOUNDS_MISSING");
        if (input.ObservedHigh is null || input.ObservedHigh <= 0) missing.Add("OBSERVED_HIGH_MISSING");
        if (input.ObservedHighTimestamp is null) missing.Add("HIGH_TIMESTAMP_MISSING");

        if (missing.Count == 0 &&
            (input.ObservedHighTimestamp!.Value < input.WindowStart!.Value ||
             input.ObservedHighTimestamp.Value > input.WindowEnd!.Value))
            missing.Add("HIGH_TIMESTAMP_OUTSIDE_ELIGIBLE_WINDOW");

        if (missing.Count > 0)
            return new OvernightFixedTargetTouchResult(
                symbol, input.SessionDate, input.ReferenceClose, null,
                input.TargetReturnPercent, input.ObservedHigh, input.ObservedHighTimestamp,
                null, null, "DATA_GAP", RulesVersion, false, missing);

        var unroundedTarget = input.ReferenceClose * (1m + input.TargetReturnPercent / 100m);

        // Round up so the executable limit is never below the requested target.
        // If the broker's applicable tick/price rules differ, supply the correct tick size.
        var tickUnits = decimal.Ceiling(unroundedTarget / input.TickSize);
        var targetPrice = tickUnits * input.TickSize;
        var touch = input.ObservedHigh!.Value >= targetPrice;
        var excursion = (double)((input.ObservedHigh.Value / input.ReferenceClose - 1m) * 100m);

        return new OvernightFixedTargetTouchResult(
            symbol, input.SessionDate, input.ReferenceClose, targetPrice,
            input.TargetReturnPercent, input.ObservedHigh, input.ObservedHighTimestamp,
            excursion, touch, touch ? "TOUCHED" : "NOT_TOUCHED",
            RulesVersion, true,
            new[]
            {
                "HISTORICAL_HIGH_TOUCH_IS_NOT_A_FILL",
                "NO_ORDER_SUBMITTED"
            });
    }

    private static OvernightFixedTargetTouchResult Invalid(
        OvernightFixedTargetTouchInput input,
        string symbol,
        string reason) =>
        new(
            symbol, input.SessionDate, input.ReferenceClose, null,
            input.TargetReturnPercent, input.ObservedHigh, input.ObservedHighTimestamp,
            null, null, "INVALID_INPUT", RulesVersion, false,
            new[] { reason, "NO_ORDER_SUBMITTED" });
}

/// <summary>
/// One historical reference-close observation. Timestamps must describe the
/// actual sell-eligible window and use explicit offsets/time-zone conversion upstream.
/// </summary>
public sealed record OvernightFixedTargetTouchInput
{
    public string? Symbol { get; init; }
    public DateOnly SessionDate { get; init; }
    public decimal ReferenceClose { get; init; }
    public decimal TargetReturnPercent { get; init; } =
        OvernightFixedTargetTouchEvaluator.DefaultTargetReturnPercent;
    public decimal TickSize { get; init; } =
        OvernightFixedTargetTouchEvaluator.DefaultTickSize;
    public decimal? ObservedHigh { get; init; }
    public DateTimeOffset? ObservedHighTimestamp { get; init; }
    public DateTimeOffset? WindowStart { get; init; }
    public DateTimeOffset? WindowEnd { get; init; }
    public string? SourceName { get; init; }
    public string? Feed { get; init; }
    public bool CoverageComplete { get; init; }
}

/// <summary>
/// Per-observation fixed-target result. A null touch result means the observation
/// cannot be counted because inputs or session coverage are invalid/incomplete.
/// </summary>
public sealed record OvernightFixedTargetTouchResult(
    string Symbol,
    DateOnly SessionDate,
    decimal ReferenceClose,
    decimal? TargetPrice,
    decimal TargetReturnPercent,
    decimal? ObservedHigh,
    DateTimeOffset? ObservedHighTimestamp,
    double? ObservedHighExcursionPercent,
    bool? TargetTouched,
    string Status,
    string RulesVersion,
    bool IsEvaluable,
    IReadOnlyList<string> ReasonCodes);
