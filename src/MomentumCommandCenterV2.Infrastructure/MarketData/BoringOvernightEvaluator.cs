namespace MomentumCommandCenterV2.Infrastructure.MarketData;

// =========================================================
/// CLASS: BoringOvernightEvaluator
/// PURPOSE:
///   Deterministically calculate historical overnight metrics, a provisional
///   Boringness score, hard gates, and a conservative qualification status.
/// SAFETY:
///   This is decision support only. It never places or modifies orders.
///   Rule maps are BORING-0.1-PROVISIONAL and are not profitability-validated.
// =========================================================
public sealed class BoringOvernightEvaluator
{
    public const string RulesVersion = "BORING-0.1-PROVISIONAL";
    private readonly SchwabExclusionList _exclusions;

    public BoringOvernightEvaluator(SchwabExclusionList exclusions) =>
        _exclusions = exclusions ?? throw new ArgumentNullException(nameof(exclusions));

    public BoringOvernightResult Evaluate(BoringOvernightInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var reasons = new List<string>();
        var returns = input.CompletedOvernightReturnsPercent?.ToArray() ?? Array.Empty<double>();
        var historyValid = returns.Length >= 20 && returns.All(double.IsFinite);
        var fiveDayValid = returns.Length >= 5 && returns.All(double.IsFinite);

        double? winRate = historyValid ? returns.Count(x => x > 0) * 100d / returns.Length : null;
        double? median = historyValid ? Median(returns) : null;
        double? mean20 = historyValid ? returns.Average() : null;
        double? mean5 = fiveDayValid ? returns.Take(5).Average() : null;
        double? consistency = historyValid
            ? returns.Count(x => x >= 0 && x <= 2.0) * 100d / returns.Length
            : null;
        double? volatility = historyValid ? StandardDeviation(returns) : null;

        var excluded = _exclusions.IsExcluded(input.Symbol);
        if (excluded) reasons.Add("GATE_SCHWAB_EXCLUSION");

        // Known hard failures take precedence over missing ancillary data.
        if (input.Price is <= 0 || input.Price is not null && input.Price < 5)
            reasons.Add("GATE_PRICE_BELOW_MIN");
        if (input.AverageDailyVolume is < 500_000)
            reasons.Add("GATE_AVG_VOLUME_BELOW_MIN");
        if (winRate is < 60)
            reasons.Add("GATE_ON_WIN_RATE_BELOW_MIN");
        if (mean20 is < 0.10)
            reasons.Add("GATE_ON_MEAN_BELOW_MIN");
        if (mean5 is not null && mean20 is not null && mean5 <= mean20)
            reasons.Add("GATE_5D_NOT_ABOVE_20D");
        if (input.Price is not null && input.Sma20 is not null && input.Price <= input.Sma20)
            reasons.Add("GATE_BELOW_20D_SMA");
        if (input.CatalystStatus == BoringCatalystStatus.RiskPresent)
            reasons.Add("BLOCK_MATERIAL_CATALYST");
        if (input.FiveMinutePermission == false)
            reasons.Add("BLOCK_5M_PERMISSION");
        if (input.StructureDeteriorating == true)
            reasons.Add("BLOCK_STRUCTURE_DETERIORATION");
        if (input.ExtensionDanger == true)
            reasons.Add("BLOCK_CURRENT_EXTENSION");
        if (input.AbnormalVolumeDanger == true)
            reasons.Add("BLOCK_ABNORMAL_VOLUME");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(input.Symbol)) missing.Add("DATA_SYMBOL_MISSING");
        if (input.Price is null || input.Price <= 0 || input.PriceTimestampUtc is null || !input.PriceIsFresh)
            missing.Add("DATA_PRICE_MISSING_OR_STALE");
        if (input.AverageDailyVolume is null || input.AverageDailyVolume < 0 || !input.VolumeIsFresh)
            missing.Add("DATA_VOLUME_MISSING_OR_STALE");
        if (input.Sma20 is null || input.Sma20 <= 0) missing.Add("DATA_SMA20_MISSING");
        if (!historyValid) missing.Add("DATA_HISTORY_INSUFFICIENT");
        if (input.CurrentOvernightMovePercent is null || input.CurrentOvernightMoveTimestampUtc is null || !input.CurrentOvernightMoveIsFresh)
            missing.Add("DATA_CURRENT_ON_MOVE_MISSING_OR_STALE");
        if (input.CatalystStatus is BoringCatalystStatus.Unknown or BoringCatalystStatus.Stale)
            missing.Add("DATA_CATALYST_REVIEW_REQUIRED");
        if (input.CatalystStatus == BoringCatalystStatus.ClearWithSource &&
            (string.IsNullOrWhiteSpace(input.CatalystEvidenceReference) || input.CatalystReviewedAtUtc is null))
            missing.Add("DATA_CATALYST_EVIDENCE_MISSING");
        if (input.FiveMinutePermission is null) missing.Add("DATA_5M_STATE_UNAVAILABLE");
        if (input.StructureDeteriorating is null) missing.Add("DATA_5M_STRUCTURE_UNAVAILABLE");
        if (input.ExtensionDanger is null) missing.Add("DATA_EXTENSION_CHECK_UNAVAILABLE");
        if (input.AbnormalVolumeDanger is null) missing.Add("DATA_RELATIVE_VOLUME_CHECK_UNAVAILABLE");

        reasons.AddRange(missing);

        var scoreParts = new Dictionary<string, double>();
        if (winRate is not null) scoreParts["OnWinRate"] = ScoreAnchored(winRate.Value, (60, 0), (70, 10), (80, 18), (90, 25));
        if (median is not null) scoreParts["MedianOnReturn"] = ScoreAnchored(median.Value, (0, 0), (0.10, 8), (0.25, 14), (0.50, 20));
        if (mean20 is not null) scoreParts["Mean20OnReturn"] = ScoreAnchored(mean20.Value,
            (0.10, 5), (0.25, 10), (0.50, 15), (1.00, 10), (1.50, 5), (2.00, 0));
        if (mean5 is not null && mean20 is not null)
            scoreParts["Mean5OnReturn"] = ScoreFiveDayGap(mean5.Value - mean20.Value);
        if (consistency is not null) scoreParts["Consistency"] = ScoreAnchored(consistency.Value, (60, 0), (70, 5), (80, 8), (90, 10));
        if (volatility is not null) scoreParts["OvernightVolatility"] = ScoreAnchoredDescending(volatility.Value,
            (0.25, 10), (0.50, 8), (1.00, 5), (1.50, 2), (2.00, 0));
        if (input.AverageDailyVolume is not null) scoreParts["AverageDailyVolume"] = ScoreAnchored(input.AverageDailyVolume.Value,
            (500_000, 1), (750_000, 2), (1_000_000, 3), (2_000_000, 5));
        if (input.Price is not null) scoreParts["Price"] = ScoreAnchored(input.Price.Value, (5, 0), (10, 1), (25, 3), (50, 5));

        var scoreComplete = scoreParts.Count == 8;
        int? totalScore = scoreComplete ? (int)Math.Round(scoreParts.Values.Sum(), MidpointRounding.AwayFromZero) : null;
        var status = reasons.Any(IsBlockingReason)
            ? BoringOvernightStatus.Blocked
            : missing.Count > 0
                ? BoringOvernightStatus.DataUnknown
                : IsCurrentCaution(input, mean5, mean20)
                    ? BoringOvernightStatus.Watch
                    : BoringOvernightStatus.Ready;

        if (input.CurrentOvernightMovePercent >= 3.0)
            reasons.Add("WATCH_CURRENT_ON_MOVE_HIGH");
        if (mean5 is not null && mean5 >= 5.0)
            reasons.Add("WATCH_5D_MEAN_HOT");
        if (totalScore is not null && totalScore < 70)
            reasons.Add("WATCH_SCORE_BAND");
        if (status == BoringOvernightStatus.Ready)
            reasons.Add("READY_ALL_GATES_PASSED");

        return new BoringOvernightResult(
            input.Symbol?.Trim() ?? string.Empty, status, totalScore, Band(totalScore),
            winRate, median, mean20, mean5, consistency, volatility, returns.Length,
            new Dictionary<string, double>(scoreParts), reasons.Distinct(StringComparer.Ordinal).ToArray(),
            RulesVersion, input.EvaluatedAtUtc ?? DateTimeOffset.UtcNow);
    }

    private static bool IsBlockingReason(string reason) =>
        reason.StartsWith("GATE_", StringComparison.Ordinal) ||
        reason.StartsWith("BLOCK_", StringComparison.Ordinal);

    private static bool IsCurrentCaution(BoringOvernightInput input, double? mean5, double? mean20) =>
        input.CurrentOvernightMovePercent >= 3.0 ||
        input.ExtensionDanger == true ||
        input.AbnormalVolumeDanger == true ||
        input.StructureDeteriorating == true ||
        mean5 is >= 5.0 ||
        input.CatalystStatus == BoringCatalystStatus.ConflictingEvidence;

    private static string Band(int? score) => score switch
    {
        >= 90 => "PERFECT BORING",
        >= 80 => "VERY GOOD",
        >= 70 => "POSSIBLE",
        >= 60 => "AGGRESSIVE",
        not null => "REJECT / NOT BORING ENOUGH",
        _ => "UNSCORED"
    };

    private static double Median(IReadOnlyCollection<double> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2d
            : ordered[middle];
    }

    private static double StandardDeviation(IReadOnlyCollection<double> values)
    {
        var mean = values.Average();
        return Math.Sqrt(values.Sum(x => Math.Pow(x - mean, 2)) / values.Count);
    }

    private static double ScoreAnchored(double value, params (double X, double Y)[] anchors)
    {
        if (value <= anchors[0].X) return anchors[0].Y;
        for (var i = 1; i < anchors.Length; i++)
        {
            if (value <= anchors[i].X)
            {
                var fraction = (value - anchors[i - 1].X) / (anchors[i].X - anchors[i - 1].X);
                return anchors[i - 1].Y + fraction * (anchors[i].Y - anchors[i - 1].Y);
            }
        }
        return anchors[^1].Y;
    }

    private static double ScoreAnchoredDescending(double value, params (double X, double Y)[] anchors)
    {
        if (value <= anchors[0].X) return anchors[0].Y;
        for (var i = 1; i < anchors.Length; i++)
        {
            if (value <= anchors[i].X)
            {
                var fraction = (value - anchors[i - 1].X) / (anchors[i].X - anchors[i - 1].X);
                return anchors[i - 1].Y + fraction * (anchors[i].Y - anchors[i - 1].Y);
            }
        }
        return anchors[^1].Y;
    }

    private static double ScoreFiveDayGap(double gap)
    {
        if (gap <= 0) return 5;
        if (gap <= 0.10) return 5 + 2 * gap / 0.10;
        if (gap <= 0.50) return 7 + 3 * (gap - 0.10) / 0.40;
        if (gap <= 1.00) return 10 - 3 * (gap - 0.50) / 0.50;
        if (gap <= 3.00) return 7 - 4 * (gap - 1.00) / 2.00;
        return 3;
    }
}

public enum BoringOvernightStatus { Ready, Watch, Blocked, DataUnknown }
public enum BoringCatalystStatus { ClearWithSource, RiskPresent, Unknown, Stale, ConflictingEvidence }

public sealed record BoringOvernightInput
{
    public string? Symbol { get; init; }
    public IReadOnlyList<double>? CompletedOvernightReturnsPercent { get; init; }
    public double? Price { get; init; }
    public DateTimeOffset? PriceTimestampUtc { get; init; }
    public bool PriceIsFresh { get; init; }
    public double? AverageDailyVolume { get; init; }
    public bool VolumeIsFresh { get; init; }
    public double? Sma20 { get; init; }
    public double? CurrentOvernightMovePercent { get; init; }
    public DateTimeOffset? CurrentOvernightMoveTimestampUtc { get; init; }
    public bool CurrentOvernightMoveIsFresh { get; init; }
    public BoringCatalystStatus CatalystStatus { get; init; } = BoringCatalystStatus.Unknown;
    public string? CatalystEvidenceReference { get; init; }
    public DateTimeOffset? CatalystReviewedAtUtc { get; init; }
    public bool? FiveMinutePermission { get; init; }
    public bool? StructureDeteriorating { get; init; }
    public bool? ExtensionDanger { get; init; }
    public bool? AbnormalVolumeDanger { get; init; }
    public DateTimeOffset? EvaluatedAtUtc { get; init; }
}

public sealed record BoringOvernightResult(
    string Symbol,
    BoringOvernightStatus Status,
    int? Score,
    string ScoreBand,
    double? OnWinRatePercent,
    double? MedianOnReturnPercent,
    double? Mean20OnReturnPercent,
    double? Mean5OnReturnPercent,
    double? ConsistencyPercent,
    double? OvernightVolatilityPercent,
    int HistorySampleCount,
    IReadOnlyDictionary<string, double> ScoreComponents,
    IReadOnlyList<string> ReasonCodes,
    string RulesVersion,
    DateTimeOffset EvaluatedAtUtc);
