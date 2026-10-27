namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Evaluates explicit Boring Overnight qualification inputs. It never places orders
/// and never substitutes missing values with estimates.
/// </summary>
public static class BoringOvernightEvaluator
{
    public static BoringOvernightEvaluation Evaluate(BoringOvernightEvaluationRequest r)
    {
        var reasons = new List<string>();
        var missing = new List<string>();
        var blockers = new List<string>();
        var warnings = new List<string>();

        void Require(bool? condition, string field, string failure)
        {
            if (condition is null) missing.Add(field);
            else if (!condition.Value) blockers.Add(failure);
        }

        Require(r.DataFresh, "dataFresh", "Market/metric data is stale.");
        Require(r.Excluded is not null, "excluded", "Excluded-list status is unknown.");
        if (r.Excluded == true) blockers.Add("Symbol is on the excluded list.");

        Require(r.Price is > 0, "price", "Price is missing or invalid.");
        Require(r.Sma20 is > 0, "sma20", "20-day SMA is missing or invalid.");
        Require(r.AverageDailyVolume20 is >= 0, "averageDailyVolume20", "20-day average daily volume is missing.");
        Require(r.OvernightWinRatePct is >= 0 and <= 100, "overnightWinRatePct", "Overnight win rate is missing or invalid.");
        Require(r.AverageOvernightPct20 is not null, "averageOvernightPct20", "20-day average overnight return is missing.");
        Require(r.AverageOvernightPct5 is not null, "averageOvernightPct5", "5-day average overnight return is missing.");
        Require(r.MedianOvernightPct20 is not null, "medianOvernightPct20", "20-day median overnight return is missing.");
        Require(r.CurrentOvernightPct is not null, "currentOvernightPct", "Current overnight move is missing.");
        Require(r.CatalystStatus is not null, "catalystStatus", "Catalyst review is incomplete.");
        Require(r.FiveMinutePermission is not null, "fiveMinutePermission", "5M permission is unknown.");
        Require(!string.IsNullOrWhiteSpace(r.FiveMinuteStructure), "fiveMinuteStructure", "5M structure is unknown.");
        Require(!string.IsNullOrWhiteSpace(r.OneMinuteState), "oneMinuteState", "1M state is unknown.");
        Require(r.ExtensionAtr is not null and >= 0, "extensionAtr", "1M extension is missing.");
        Require(r.CurrentVolumeVsAverage is not null and >= 0, "currentVolumeVsAverage", "Current volume ratio is missing.");

        if (r.Price is > 0 && r.Price <= 5) blockers.Add("Price must be above $5.");
        if (r.Price is > 0 && r.Sma20 is > 0 && r.Price <= r.Sma20) blockers.Add("Price is not above the 20-day SMA.");
        if (r.AverageDailyVolume20 is >= 0 and < 500_000) blockers.Add("20-day average daily volume is below 500,000 shares.");
        if (r.OvernightWinRatePct is >= 0 and < 60) blockers.Add("Overnight win rate is below 60%.");
        if (r.AverageOvernightPct20 is not null and < 0.10) blockers.Add("20-day average overnight return is below +0.10%.");
        if (r.AverageOvernightPct5 is not null && r.AverageOvernightPct20 is not null &&
            r.AverageOvernightPct5 <= r.AverageOvernightPct20)
            blockers.Add("5-day average overnight return is not strictly greater than the 20-day average.");
        if (string.Equals(r.CatalystStatus, "RISK", StringComparison.OrdinalIgnoreCase))
            blockers.Add("Material catalyst/event risk requires rejection or explicit manual review.");
        if (r.FiveMinutePermission == false) blockers.Add("5M permission is NO.");
        if (!string.IsNullOrWhiteSpace(r.FiveMinuteStructure) &&
            !r.FiveMinuteStructure.Equals("BULLISH", StringComparison.OrdinalIgnoreCase))
            blockers.Add("5M structure is not BULLISH.");
        if (!string.IsNullOrWhiteSpace(r.OneMinuteState) &&
            (r.OneMinuteState.Contains("GET OUT", StringComparison.OrdinalIgnoreCase) ||
             r.OneMinuteState.Contains("SELL", StringComparison.OrdinalIgnoreCase) ||
             r.OneMinuteState.Contains("BLOCK", StringComparison.OrdinalIgnoreCase)))
            blockers.Add("1M state indicates SELL/GET OUT/BLOCK; do not qualify an entry.");
        if (r.ExtensionAtr is >= 2.0) warnings.Add("1M extension is at or above the 2.0 ATR danger threshold.");
        if (r.CurrentOvernightPct is >= 3.0) warnings.Add("Current overnight move is at or above 3%; treat as too hot.");
        if (r.CurrentVolumeVsAverage is >= 3.0) warnings.Add("Current volume is at least 3x its reference average; investigate an abnormal session.");
        if (r.DailyRangePct is >= 0 && r.AtrPct is >= 0 && r.DailyRangePct > r.AtrPct * 1.5)
            warnings.Add("Current daily range exceeds 1.5x ATR%; investigate unusually large range.");
        if (string.Equals(r.CatalystStatus, "UNKNOWN", StringComparison.OrdinalIgnoreCase))
            missing.Add("catalyst clearance");

        var uniqueMissing = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var uniqueBlockers = blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var uniqueWarnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var status = uniqueMissing.Length > 0 ? "DATA?" :
            uniqueBlockers.Length > 0 ? "BLOCKED" :
            uniqueWarnings.Length > 0 ? "WATCH" : "READY";

        if (status == "READY") reasons.Add("All configured hard gates passed; this is eligibility only, not an order or profitability claim.");
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
