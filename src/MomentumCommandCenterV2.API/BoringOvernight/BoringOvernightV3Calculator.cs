namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: V3DailyBar
/// PURPOSE: One daily regular-session OHLCV bar, split-adjusted, used by the V3 Boring Overnight column.
/// WHEN CALLED: Built from Alpaca SIP Daily sessions or a Thinkorswim daily export, then passed to
/// <see cref="BoringOvernightV3Calculator.Calculate"/>.
/// </summary>
public sealed record V3DailyBar(DateOnly SessionDate, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

/// <summary>
/// TYPE: V3ScoreComponent
/// PURPOSE: One Boringness component: its raw value (percent units unless stated), awarded points and maximum.
/// WHEN CALLED: Part of <see cref="V3ColumnResult"/>.
/// </summary>
public sealed record V3ScoreComponent(string Key, string Label, decimal RawValue, string RawUnit, decimal Points, decimal MaxPoints);

/// <summary>
/// TYPE: V3ColumnResult
/// PURPOSE: Every value the V3 Thinkorswim columns display, calculated by the authoritative C# reference, plus all
/// reason codes. Percent fields are percentage points (0.25 = 0.25%).
/// WHEN CALLED: Returned by <see cref="BoringOvernightV3Calculator.Calculate"/>.
/// </summary>
public sealed record V3ColumnResult(
    string Version,
    string ScoreMapVersion,
    DateOnly? EvaluationSessionDate,
    int BarsSupplied,
    string Status,
    int StatusCode,
    string PrimaryReason,
    IReadOnlyList<string> Reasons,
    decimal? OvernightEdgeScore,
    bool? EdgeGatesPass,
    decimal? WinRatePct,
    decimal? AverageOvernightPct20,
    decimal? MedianOvernightPct20,
    decimal? AverageOvernightPct5,
    decimal? OvernightMomentumPp,
    decimal? CurrentOvernightPct,
    decimal? Close,
    decimal? Sma20,
    bool? AboveSma20,
    decimal? AverageVolumePrior20,
    decimal? ConsistencyPct,
    decimal? VolatilityPct,
    decimal? DailyRangePct,
    decimal? AtrPct14,
    decimal? VolumeVsAveragePrior20,
    decimal? BoringnessScore,
    string? BoringnessBand,
    IReadOnlyList<V3ScoreComponent> BoringnessComponents,
    string Note)
{
    /// <summary>
    /// PURPOSE: The value of the Thinkorswim "V3 Rank" sort column: StatusCode x 100 + Boringness (0 for DATA?), so a
    /// descending sort lists CANDIDATE first, ordered by Boringness.
    /// WHEN CALLED: Serialized with every result (added 2026-11-16).
    /// </summary>
    public decimal RankValue => StatusCode == 0 || BoringnessScore is null ? 0m : StatusCode * 100m + BoringnessScore.Value;
}

/// <summary>
/// TYPE: BoringOvernightV3Calculator
/// PURPOSE: Authoritative reference for the V3 Boring Overnight Thinkorswim watchlist columns
/// (docs/Column Formulas/V3). It computes, from daily bars ending at the evaluation session:
/// (1) the Overnight Edge Score (the installed Overnight Score formula, unchanged) and its three edge gates,
/// (2) the Boringness Score with the BORING-0.1-PROVISIONAL point maps (Boring_Overnight_Column_Data_Contract_v1 §10A),
/// (3) a TOS-computable status: CANDIDATE / WATCH / BLOCKED / DATA?. It never returns READY: catalyst, exclusion list,
/// freshness and 5M permission are checked only by MCC's READY evaluator.
/// Overnight return r = Open / previous Close − 1 (daily bars, regular session, split-adjusted).
/// WHEN CALLED: By GET /api/boring-overnight/v3-column and unit tests; the ThinkScript files must match it line for line.
/// </summary>
public static class BoringOvernightV3Calculator
{
    /// <summary>Calculation version shared with the ThinkScript column headers.</summary>
    public const string Version = "MCC-V3-BORING-0.1";

    /// <summary>Point-map version from the data contract (provisional, not calibrated).</summary>
    public const string ScoreMapVersion = "BORING-0.1-PROVISIONAL";

    /// <summary>Minimum bars: 20 overnight returns need 21 daily bars (the 14-day ATR needs 16).</summary>
    public const int MinimumBars = 21;

    /// <summary>
    /// PURPOSE: Calculates every V3 column value from ascending daily bars; the last bar is the evaluation session.
    /// Only the latest 21 bars are used. Fewer bars, or a non-positive price, returns DATA? with no metrics.
    /// WHEN CALLED: Once per symbol per evaluation.
    /// </summary>
    public static V3ColumnResult Calculate(IReadOnlyList<V3DailyBar> barsAscending)
    {
        var bars = barsAscending.OrderBy(b => b.SessionDate).ToArray();
        if (bars.Length < MinimumBars)
            return DataUnknown(bars, "DATA_HISTORY_INSUFFICIENT", $"At least {MinimumBars} daily bars are required (20 overnight returns); {bars.Length} supplied.");
        if (bars.Select(b => b.SessionDate).Distinct().Count() != bars.Length)
            return DataUnknown(bars, "DATA_DUPLICATE_SESSION", "Duplicate session dates were supplied.");
        bars = bars[^MinimumBars..];
        if (bars.Any(b => b.Open <= 0 || b.High <= 0 || b.Low <= 0 || b.Close <= 0 || b.Volume < 0 || b.High < b.Low))
            return DataUnknown(bars, "DATA_INVALID_BAR", "A bar has a non-positive price, negative volume or high below low.");

        var e = bars.Length - 1; // evaluation bar index (20)
        var returns = Enumerable.Range(e - 19, 20).Select(i => bars[i].Open / bars[i - 1].Close - 1m).ToArray();
        var last5 = returns[^5..];

        var winRate = returns.Count(r => r > 0m) / 20m;                 // fraction
        var avg20 = returns.Average();                                   // fraction
        var median20 = Median(returns);                                  // fraction
        var avg5 = last5.Average();                                      // fraction
        var momentumPp = (avg5 - avg20) * 100m;
        var currentOnPct = returns[^1] * 100m;
        var close = bars[e].Close;
        var sma20 = Enumerable.Range(e - 19, 20).Average(i => bars[i].Close);
        var aboveSma = close > sma20;
        var avgVolumePrior = (decimal)Enumerable.Range(e - 20, 20).Average(i => (double)bars[i].Volume);
        var consistency = returns.Count(r => r >= 0m && r <= 0.02m) / 20m;
        var volatilityPct = PopulationStdDev(returns) * 100m;
        var atr = Enumerable.Range(e - 14, 14).Average(i => TrueRange(bars[i], bars[i - 1].Close));
        var atrPct = atr / bars[e - 1].Close * 100m;
        var rangePct = (bars[e].High - bars[e].Low) / bars[e - 1].Close * 100m;
        decimal? volumeRatio = avgVolumePrior > 0m ? bars[e].Volume / avgVolumePrior : null;

        // Overnight Edge Score: the installed "Overnight Score" column formula, unchanged.
        var edgeScore = winRate * 50m + avg20 * 1000m + median20 * 1000m + (avg5 - avg20) * 1000m + (aboveSma ? 10m : 0m);

        var components = new[]
        {
            Component("winRate", "ON win rate", winRate * 100m, "%", 25m, WinPoints(winRate * 100m)),
            Component("median", "Median ON return (20)", median20 * 100m, "%", 20m, MedianPoints(median20 * 100m)),
            Component("mean20", "Mean ON return (20)", avg20 * 100m, "%", 15m, Mean20Points(avg20 * 100m)),
            Component("fiveDay", "5D mean minus 20D mean", momentumPp, "pp", 10m, FiveDayPoints(momentumPp)),
            Component("consistency", "Returns in [0%, +2%] (20)", consistency * 100m, "%", 10m, ConsistencyPoints(consistency * 100m)),
            Component("volatility", "ON return std dev (20, population)", volatilityPct, "%", 10m, VolatilityPoints(volatilityPct)),
            Component("averageVolume", "Average volume (prior 20 sessions)", avgVolumePrior, "shares", 5m, VolumePoints(avgVolumePrior)),
            Component("price", "Price (evaluation close)", close, "$", 5m, PricePoints(close))
        };
        var boringness = components.Sum(c => c.Points);
        var band = Band(boringness);

        var blocked = new List<string>();
        if (PriceGateBlocks(close)) blocked.Add("GATE_PRICE_BELOW_MIN");
        if (avgVolumePrior < 500_000m) blocked.Add("GATE_AVG_VOLUME_BELOW_MIN");
        if (winRate < 0.60m) blocked.Add("GATE_ON_WIN_RATE_BELOW_MIN");
        if (avg20 < 0.001m) blocked.Add("GATE_ON_MEAN_BELOW_MIN");
        if (avg5 <= avg20) blocked.Add("GATE_5D_NOT_ABOVE_20D");
        if (!aboveSma) blocked.Add("GATE_BELOW_20D_SMA");
        var edgePass = winRate >= 0.60m && avg20 >= 0.001m && avg5 > avg20;

        var watch = new List<string>();
        if (currentOnPct >= 3m) watch.Add("WATCH_CURRENT_ON_MOVE_HIGH");
        if (avg5 * 100m >= 5m) watch.Add("WATCH_5D_MEAN_HIGH");
        if (momentumPp >= 3m) watch.Add("WATCH_5D_JUMP");
        if (rangePct > atrPct * 1.5m) watch.Add("WATCH_RANGE_ABOVE_1_5_ATR");
        if (volumeRatio >= 3m) watch.Add("WATCH_VOLUME_3X");
        if (boringness < 60m) watch.Add("WATCH_SCORE_BAND");

        var (status, code) = blocked.Count > 0 ? ("BLOCKED", 1) : watch.Count > 0 ? ("WATCH", 2) : ("CANDIDATE", 3);
        var reasons = blocked.Concat(watch).ToList();
        if (reasons.Count == 0) reasons.Add("CANDIDATE_MCC_READY_CHECK");

        return new V3ColumnResult(Version, ScoreMapVersion, bars[e].SessionDate, barsAscending.Count, status, code, reasons[0], reasons,
            edgeScore, edgePass, winRate * 100m, avg20 * 100m, median20 * 100m, avg5 * 100m, momentumPp, currentOnPct,
            close, sma20, aboveSma, avgVolumePrior, consistency * 100m, volatilityPct, rangePct, atrPct, volumeRatio,
            boringness, band, components, Note);
    }

    /// <summary>
    /// PURPOSE: Price hard gate: blocks only below $5.00; exactly $5.00 passes (operator decision 2026-11-16,
    /// data contract price >= $5.00). Same rule as BoringOvernightEvaluator.
    /// WHEN CALLED: By <see cref="Calculate"/> and tests.
    /// </summary>
    public static bool PriceGateBlocks(decimal price) => price < 5m;

    /// <summary>PURPOSE: Win-rate points: 60%=0, 70%=10, 80%=18, 90%+=25. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal WinPoints(decimal winPct) => Piecewise(winPct, (60m, 0m), (70m, 10m), (80m, 18m), (90m, 25m));

    /// <summary>PURPOSE: Median points: ≤0%=0, 0.10%=8, 0.25%=14, 0.50%+=20. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal MedianPoints(decimal medianPct) => Piecewise(medianPct, (0m, 0m), (0.10m, 8m), (0.25m, 14m), (0.50m, 20m));

    /// <summary>
    /// PURPOSE: 20D mean points: below 0.10% = 0 (hard gate), 0.10%=5, 0.25%=10, 0.50%=15, 1.00%=10, 1.50%=5, 2.00%+=0.
    /// WHEN CALLED: By <see cref="Calculate"/> and tests.
    /// </summary>
    public static decimal Mean20Points(decimal meanPct) => meanPct < 0.10m ? 0m
        : Piecewise(meanPct, (0.10m, 5m), (0.25m, 10m), (0.50m, 15m), (1.00m, 10m), (1.50m, 5m), (2.00m, 0m));

    /// <summary>
    /// PURPOSE: 5D-minus-20D points (percentage points): ≤0 = 0 (hard gate), 0+=5, 0.10=7, 0.50=10, 1.00=7, 3.00=3,
    /// 5.00+=0. The contract's "≥ +3.00 = 0–3" is implemented as 3 at +3.00 falling to 0 at +5.00 (Claude's
    /// interpretation, 2026-11-16).
    /// WHEN CALLED: By <see cref="Calculate"/> and tests.
    /// </summary>
    public static decimal FiveDayPoints(decimal diffPp) => diffPp <= 0m ? 0m
        : Piecewise(diffPp, (0m, 5m), (0.10m, 7m), (0.50m, 10m), (1.00m, 7m), (3.00m, 3m), (5.00m, 0m));

    /// <summary>PURPOSE: Consistency points: 60%=0, 70%=5, 80%=8, 90%+=10. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal ConsistencyPoints(decimal consistencyPct) => Piecewise(consistencyPct, (60m, 0m), (70m, 5m), (80m, 8m), (90m, 10m));

    /// <summary>PURPOSE: Volatility points: ≤0.25%=10, 0.50%=8, 1.00%=5, 1.50%=2, 2.00%+=0. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal VolatilityPoints(decimal stdDevPct) => Piecewise(stdDevPct, (0.25m, 10m), (0.50m, 8m), (1.00m, 5m), (1.50m, 2m), (2.00m, 0m));

    /// <summary>PURPOSE: Average-volume points: below 500k = 0 (hard gate), 500k=1, 750k=2, 1M=3, 2M+=5. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal VolumePoints(decimal averageVolume) => averageVolume < 500_000m ? 0m
        : Piecewise(averageVolume, (500_000m, 1m), (750_000m, 2m), (1_000_000m, 3m), (2_000_000m, 5m));

    /// <summary>PURPOSE: Price points: $5=0, $10=1, $25=3, $50+=5. WHEN CALLED: By <see cref="Calculate"/> and tests.</summary>
    public static decimal PricePoints(decimal price) => Piecewise(price, (5m, 0m), (10m, 1m), (25m, 3m), (50m, 5m));

    /// <summary>
    /// PURPOSE: Descriptive band for a Boringness total: 90+ PERFECT BORING, 80+ VERY GOOD, 70+ POSSIBLE, 60+ AGGRESSIVE,
    /// otherwise REJECT. Bands never override a gate.
    /// WHEN CALLED: By <see cref="Calculate"/> and tests.
    /// </summary>
    public static string Band(decimal score) =>
        score >= 90m ? "PERFECT BORING" : score >= 80m ? "VERY GOOD" : score >= 70m ? "POSSIBLE" : score >= 60m ? "AGGRESSIVE" : "REJECT";

    /// <summary>
    /// PURPOSE: Linear interpolation between ascending anchors, clamped to the first/last anchor value outside the range.
    /// WHEN CALLED: By the point maps.
    /// </summary>
    public static decimal Piecewise(decimal x, params (decimal X, decimal Y)[] anchors)
    {
        if (x <= anchors[0].X) return anchors[0].Y;
        for (var i = 1; i < anchors.Length; i++)
        {
            if (x <= anchors[i].X)
            {
                var (x0, y0) = anchors[i - 1];
                var (x1, y1) = anchors[i];
                return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
            }
        }
        return anchors[^1].Y;
    }

    /// <summary>PURPOSE: Median of an even or odd set (mean of the two middle values when even, as Thinkorswim Median). WHEN CALLED: By <see cref="Calculate"/>.</summary>
    private static decimal Median(IReadOnlyCollection<decimal> values)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        return sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2m : sorted[sorted.Length / 2];
    }

    /// <summary>
    /// PURPOSE: Population standard deviation sqrt(mean(x²) − mean(x)²), the same formula written out in the ThinkScript
    /// column (it does not rely on Thinkorswim's StDev definition).
    /// WHEN CALLED: By <see cref="Calculate"/>.
    /// </summary>
    private static decimal PopulationStdDev(IReadOnlyCollection<decimal> values)
    {
        var mean = values.Average();
        var meanSquare = values.Average(v => v * v);
        var variance = meanSquare - mean * mean;
        return variance <= 0m ? 0m : (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>PURPOSE: True range of a bar against the previous close (Thinkorswim TrueRange). WHEN CALLED: By the ATR calculation.</summary>
    private static decimal TrueRange(V3DailyBar bar, decimal previousClose) =>
        Math.Max(bar.High - bar.Low, Math.Max(Math.Abs(bar.High - previousClose), Math.Abs(bar.Low - previousClose)));

    /// <summary>PURPOSE: Builds a score component with points rounded to 4 decimals. WHEN CALLED: By <see cref="Calculate"/>.</summary>
    private static V3ScoreComponent Component(string key, string label, decimal raw, string unit, decimal max, decimal points) =>
        new(key, label, raw, unit, Math.Round(points, 4), max);

    /// <summary>PURPOSE: A DATA? result with no metrics. WHEN CALLED: On insufficient or invalid history.</summary>
    private static V3ColumnResult DataUnknown(IReadOnlyList<V3DailyBar> bars, string reason, string detail) =>
        new(Version, ScoreMapVersion, bars.Count > 0 ? bars[^1].SessionDate : null, bars.Count, "DATA?", 0, reason, new[] { reason },
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            Array.Empty<V3ScoreComponent>(), detail + " " + Note);

    private const string Note =
        "CANDIDATE means every Thinkorswim-computable V3 gate passed; it is never READY. Catalyst, exclusion list, data freshness " +
        "and 5M permission are checked only by MCC's READY evaluator. Scores use provisional point maps (not calibrated) and are " +
        "not probabilities or trade instructions.";
}
