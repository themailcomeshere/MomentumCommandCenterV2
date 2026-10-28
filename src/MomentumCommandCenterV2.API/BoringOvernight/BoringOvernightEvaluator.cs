namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Evaluates explicit Boring Overnight qualification inputs. It never places orders
/// and never substitutes missing values with estimates.
/// </summary>
public static class BoringOvernightEvaluator
{
    public static BoringOvernightEvaluation Evaluate(BoringOvernightEvaluationRequest r)
    {
        // Reject malformed numeric payloads before any comparison. NaN bypasses ordinary
        // range comparisons, and Infinity can otherwise accidentally satisfy some gates.
        var invalid = new List<string>();
        if (string.IsNullOrWhiteSpace(r.Symbol) ||
            r.Symbol.Length > 8 ||
            !r.Symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            invalid.Add("symbol");

        var numericInputs = new (string Name, double? Value)[]
        {
            ("price", r.Price),
            ("sma20", r.Sma20),
            ("averageDailyVolume20", r.AverageDailyVolume20),
            ("overnightWinRatePct", r.OvernightWinRatePct),
            ("averageOvernightPct20", r.AverageOvernightPct20),
            ("averageOvernightPct5", r.AverageOvernightPct5),
            ("medianOvernightPct20", r.MedianOvernightPct20),
            ("currentOvernightPct", r.CurrentOvernightPct),
            ("extensionAtr", r.ExtensionAtr),
            ("currentVolumeVsAverage", r.CurrentVolumeVsAverage),
            ("dailyRangePct", r.DailyRangePct),
            ("atrPct", r.AtrPct)
        };
        invalid.AddRange(numericInputs
            .Where(input => input.Value.HasValue && !double.IsFinite(input.Value.Value))
            .Select(input => input.Name + " (non-finite)"));

        if (invalid.Count > 0)
        {
            var reasons = invalid.Select(name => $"Invalid input: {name}.").ToArray();
            var symbol = string.IsNullOrWhiteSpace(r.Symbol) ? "UNKNOWN" : r.Symbol.Trim().ToUpperInvariant();
            return new BoringOvernightEvaluation(symbol, "DATA?", false, reasons,
                Array.Empty<string>(), Array.Empty<string>(), invalid,
                "Invalid input data fails closed. No order was submitted.");
        }

        var missing = new List<string>();
        var blockers = new List<string>();
        var warnings = new List<string>();

        if (r.DataFresh is null) missing.Add("dataFresh");
        else if (!r.DataFresh.Value) blockers.Add("Market/metric data is stale.");

        if (r.Excluded is null) missing.Add("excluded");
        else if (r.Excluded.Value) blockers.Add("Symbol is on the excluded list.");

        if (r.Price is null) missing.Add("price");
        else if (r.Price.Value <= 0) blockers.Add("Price is invalid.");
        else if (r.Price.Value <= 5) blockers.Add("Price must be above $5.");

        if (r.Sma20 is null or <= 0) missing.Add("sma20");
        if (r.Price is > 0 && r.Sma20 is > 0 && r.Price.Value <= r.Sma20.Value)
            blockers.Add("Price is not above the 20-day SMA.");

        if (r.AverageDailyVolume20 is null) missing.Add("averageDailyVolume20");
        else if (r.AverageDailyVolume20.Value < 500_000)
            blockers.Add("20-day average daily volume is below 500,000 shares.");

        if (r.OvernightWinRatePct is null) missing.Add("overnightWinRatePct");
        else if (r.OvernightWinRatePct.Value < 60 || r.OvernightWinRatePct.Value > 100)
            blockers.Add("Overnight win rate must be between 60% and 100%.");

        if (r.AverageOvernightPct20 is null) missing.Add("averageOvernightPct20");
        else if (r.AverageOvernightPct20.Value < 0.10)
            blockers.Add("20-day average overnight return is below +0.10%.");

        if (r.AverageOvernightPct5 is null) missing.Add("averageOvernightPct5");
        else if (r.AverageOvernightPct20 is not null &&
                 r.AverageOvernightPct5.Value <= r.AverageOvernightPct20.Value)
            blockers.Add("5-day average overnight return is not strictly greater than the 20-day average.");

        if (r.MedianOvernightPct20 is null) missing.Add("medianOvernightPct20");
        if (r.CurrentOvernightPct is null) missing.Add("currentOvernightPct");

        if (string.IsNullOrWhiteSpace(r.CatalystStatus)) missing.Add("catalystStatus");
        else if (r.CatalystStatus.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
            missing.Add("catalyst clearance");
        else if (r.CatalystStatus.Equals("RISK", StringComparison.OrdinalIgnoreCase))
            blockers.Add("Material catalyst/event risk requires rejection or explicit manual review.");

        if (r.FiveMinutePermission is null) missing.Add("fiveMinutePermission");
        else if (!r.FiveMinutePermission.Value) blockers.Add("5M permission is NO.");

        if (string.IsNullOrWhiteSpace(r.FiveMinuteStructure)) missing.Add("fiveMinuteStructure");
        else if (!r.FiveMinuteStructure.Equals("BULLISH", StringComparison.OrdinalIgnoreCase))
            blockers.Add("5M structure is not BULLISH.");

        if (string.IsNullOrWhiteSpace(r.OneMinuteState)) missing.Add("oneMinuteState");
        else if (r.OneMinuteState.Contains("GET OUT", StringComparison.OrdinalIgnoreCase) ||
                 r.OneMinuteState.Contains("SELL", StringComparison.OrdinalIgnoreCase) ||
                 r.OneMinuteState.Contains("BLOCK", StringComparison.OrdinalIgnoreCase))
            blockers.Add("1M state indicates SELL/GET OUT/BLOCK; do not qualify an entry.");

        if (r.ExtensionAtr is null) missing.Add("extensionAtr");
        else if (r.ExtensionAtr.Value >= 2.0)
            warnings.Add("1M extension is at or above the 2.0 ATR danger threshold.");

        if (r.CurrentVolumeVsAverage is null) missing.Add("currentVolumeVsAverage");
        else if (r.CurrentVolumeVsAverage.Value >= 3.0)
            warnings.Add("Current volume is at least 3x its reference average; investigate an abnormal session.");

        if (r.DailyRangePct is null) missing.Add("dailyRangePct");
        if (r.AtrPct is null) missing.Add("atrPct");
        if (r.DailyRangePct is >= 0 && r.AtrPct is > 0 &&
            r.DailyRangePct.Value > r.AtrPct.Value * 1.5)
            warnings.Add("Current daily range exceeds 1.5x ATR%; investigate unusually large range.");

        if (r.CurrentOvernightPct is >= 3.0)
            warnings.Add("Current overnight move is at or above 3%; treat as too hot.");

        var uniqueMissing = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var uniqueBlockers = blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var uniqueWarnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var status = uniqueMissing.Length > 0 ? "DATA?" :
            uniqueBlockers.Length > 0 ? "BLOCKED" :
            uniqueWarnings.Length > 0 ? "WATCH" : "READY";

        var reasons = new List<string>();
        if (status == "READY")
            reasons.Add("All configured hard gates passed; this is eligibility only, not an order or profitability claim.");
        reasons.AddRange(uniqueBlockers);
        reasons.AddRange(uniqueWarnings);
        reasons.AddRange(uniqueMissing.Select(x => $"Missing/unknown: {x}."));

        return new BoringOvernightEvaluation(r.Symbol.ToUpperInvariant(), status,
            status == "READY", reasons, uniqueBlockers, uniqueWarnings, uniqueMissing,
            "No order was submitted. READY means only that the supplied snapshot passed this version of the qualification rules.");
    }
}

/// <summary>Caller-supplied snapshot. Percent fields are percentage points (e.g. 0.5 means 0.5%).</summary>
public sealed record BoringOvernightEvaluationRequest(
    string Symbol,
    bool? DataFresh,
    double? Price,
    double? Sma20,
    double? AverageDailyVolume20,
    double? OvernightWinRatePct,
    double? AverageOvernightPct20,
    double? AverageOvernightPct5,
    double? MedianOvernightPct20,
    double? CurrentOvernightPct,
    bool? Excluded,
    string? CatalystStatus,
    bool? FiveMinutePermission,
    string? FiveMinuteStructure,
    string? OneMinuteState,
    double? ExtensionAtr,
    double? CurrentVolumeVsAverage,
    double? DailyRangePct,
    double? AtrPct);

public sealed record BoringOvernightEvaluation(
    string Symbol,
    string Status,
    bool Eligible,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> MissingInputs,
    string Disclaimer);
