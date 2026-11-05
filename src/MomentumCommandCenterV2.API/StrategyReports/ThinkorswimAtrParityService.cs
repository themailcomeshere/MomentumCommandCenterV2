namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Compares a server Wilder ATR series with Thinkorswim's exported ATR values on the exact
/// same imported OHLCV rows and source timestamps. This does not prove provider-feed parity.
/// </summary>
public static class ThinkorswimAtrParityService
{
    public const string Version = "MCC-WILDER-ATR-PARITY-1.0";
    public const int DefaultWarmupBars = 99;
    public const int MinimumComparisonBars = 20;
    // TOS V2 payloads export ATR to at most four decimal places; this is half one unit of that precision.
    public const decimal ExportRoundingTolerance = 0.00005m;

    public static ThinkorswimAtrParityResult Compare(
        IReadOnlyList<ThinkorswimAtrStoredObservation> stored,
        int warmupBars = DefaultWarmupBars)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (warmupBars < 15 || warmupBars > 5000)
            throw new ArgumentOutOfRangeException(nameof(warmupBars), "Warm-up bars must be between 15 and 5000.");
        if (stored.Count == 0)
            return Empty("No persisted Thinkorswim ATR observations were found.");

        var ordered = stored.OrderBy(x => x.Observation.SourceTimestampUtc).ToArray();
        var first = ordered[0].Metadata;
        if (ordered.Any(x =>
            x.Metadata.AtrPeriod != first.AtrPeriod ||
            !x.Metadata.AtrAverageType.Equals(first.AtrAverageType, StringComparison.OrdinalIgnoreCase) ||
            !x.Metadata.StudyVersion.Equals(first.StudyVersion, StringComparison.Ordinal) ||
            !x.Metadata.ChartTimezone.Equals(first.ChartTimezone, StringComparison.OrdinalIgnoreCase) ||
            x.Metadata.ExtendedHours != first.ExtendedHours ||
            x.Metadata.CorporateActionAdjustment != first.CorporateActionAdjustment))
            return Empty("Mixed ATR settings or chart provenance are present; import a single consistent study configuration before parity comparison.", "MIXED_PROVENANCE");

        if (!first.AtrAverageType.Equals("Wilder", StringComparison.OrdinalIgnoreCase))
            return Empty("Only Wilder ATR is supported by this parity implementation.", "UNSUPPORTED_AVERAGE_TYPE");

        var rows = ordered.Select(x => x.Observation).ToArray();
        if (rows.Select(x => (x.Symbol, x.Timeframe)).Distinct().Count() != 1)
            return Empty("Multiple symbols or timeframes are present; request parity for one symbol and timeframe.", "MIXED_SERIES");
        if (rows.Select(x => x.SourceTimestampUtc).Distinct().Count() != rows.Length)
            return Empty("Multiple ATR observations share a source timestamp under different provenance; isolate one import configuration.", "DUPLICATE_SOURCE_TIMESTAMPS");

        var period = first.AtrPeriod;
        if (rows.Length < period + 1)
            return Empty($"Need at least {period + 1} OHLCV rows to seed Wilder ATR; received {rows.Length}.", "INSUFFICIENT_HISTORY");

        var trueRanges = new decimal[rows.Length];
        for (var i = 1; i < rows.Length; i++)
        {
            var current = rows[i];
            var previousClose = rows[i - 1].Close;
            trueRanges[i] = Math.Max(current.High - current.Low,
                Math.Max(Math.Abs(current.High - previousClose), Math.Abs(current.Low - previousClose)));
        }

        var calculated = new decimal?[rows.Length];
        var atr = trueRanges.Skip(1).Take(period).Average();
        calculated[period] = atr;
        for (var i = period + 1; i < rows.Length; i++)
        {
            atr = ((atr * (period - 1)) + trueRanges[i]) / period;
            calculated[i] = atr;
        }

        var interval = rows[0].Timeframe switch
        {
            "1m" => TimeSpan.FromMinutes(1),
            "5m" => TimeSpan.FromMinutes(5),
            "15m" => TimeSpan.FromMinutes(15),
            "30m" => TimeSpan.FromMinutes(30),
            "1h" => TimeSpan.FromHours(1),
            "4h" => TimeSpan.FromHours(4),
            _ => TimeSpan.Zero
        };
        var gaps = interval == TimeSpan.Zero ? 0 : rows.Zip(rows.Skip(1), (left, right) => right.SourceTimestampUtc - left.SourceTimestampUtc)
            .Count(delta => delta > interval + TimeSpan.FromSeconds(1));

        if (rows.Length <= warmupBars)
            return new ThinkorswimAtrParityResult(
                "INSUFFICIENT_WARMUP", Version, rows[0].Symbol, rows[0].Timeframe, period,
                warmupBars, MinimumComparisonBars, ExportRoundingTolerance, rows.Length, 0, gaps,
                null, null, null, null, null, null, first,
                $"Only {rows.Length} observations are available; {warmupBars} warm-up observations are required before comparing completed-bar ATR. Export more history before the comparison window.");

        var errors = new List<(DateTimeOffset Timestamp, decimal Absolute, decimal RelativePct)>();
        for (var i = Math.Max(warmupBars, period); i < rows.Length; i++)
        {
            if (calculated[i] is not { } calculatedAtr) continue;
            var absolute = Math.Abs(calculatedAtr - rows[i].Atr);
            var relative = rows[i].Atr > 0m ? absolute / rows[i].Atr * 100m : 0m;
            errors.Add((rows[i].SourceTimestampUtc, absolute, relative));
        }

        if (errors.Count < MinimumComparisonBars)
            return new ThinkorswimAtrParityResult(
                "INSUFFICIENT_COMPARISONS", Version, rows[0].Symbol, rows[0].Timeframe, period,
                warmupBars, MinimumComparisonBars, ExportRoundingTolerance, rows.Length, errors.Count, gaps,
                null, null, null, errors.FirstOrDefault().Timestamp == default ? null : errors[0].Timestamp,
                errors.LastOrDefault().Timestamp == default ? null : errors[^1].Timestamp, null, first,
                $"Only {errors.Count} matched completed bars remain after the warm-up; at least {MinimumComparisonBars} are required.");

        var orderedErrors = errors.Select(x => x.Absolute).OrderBy(x => x).ToArray();
        var meanAbsolute = orderedErrors.Average();
        var meanRelative = errors.Average(x => x.RelativePct);
        var maxAbsolute = orderedErrors[^1];
        var p90 = orderedErrors[(int)Math.Ceiling(orderedErrors.Length * 0.90m) - 1];
        var matched = maxAbsolute <= ExportRoundingTolerance && gaps == 0;
        var status = maxAbsolute > ExportRoundingTolerance ? "NOT_MATCH" : gaps > 0 ? "GAPS_PRESENT" : "MATCH";
        return new ThinkorswimAtrParityResult(
            status, Version, rows[0].Symbol, rows[0].Timeframe, period,
            warmupBars, MinimumComparisonBars, ExportRoundingTolerance, rows.Length, errors.Count, gaps,
            meanAbsolute, maxAbsolute, p90, errors[0].Timestamp, errors[^1].Timestamp, meanRelative, first,
            matched
                ? "Every compared Wilder ATR value matches the Thinkorswim export within the four-decimal export rounding bound, and the comparison window has no timestamp gaps. This validates the ATR algorithm on these imported bars only; provider-feed and live-history parity remain separate."
                : status == "GAPS_PRESENT"
                    ? "ATR values are within numeric tolerance, but one or more source timestamps are missing. Wilder recurrence across missing bars cannot be certified; no gap is imputed and parity is not MATCH."
                    : "Server Wilder ATR does not match the exported Thinkorswim ATR within the four-decimal export rounding bound. Numeric sell-limit orchestration must remain blocked. This comparison uses the imported TOS OHLCV bars and does not diagnose provider-feed differences.");
    }

    private static ThinkorswimAtrParityResult Empty(string reason, string status = "NO_DATA") =>
        new(status, Version, null, null, null, DefaultWarmupBars, MinimumComparisonBars,
            ExportRoundingTolerance, 0, 0, 0, null, null, null, null, null, null, null, reason);
}

public sealed record ThinkorswimAtrParityResult(
    string Status,
    string ComparisonVersion,
    string? Symbol,
    string? Timeframe,
    int? AtrPeriod,
    int WarmupBars,
    int MinimumComparisonBars,
    decimal Tolerance,
    int SourceObservationCount,
    int ComparedBarCount,
    int TimestampGapCount,
    decimal? MeanAbsoluteError,
    decimal? MaximumAbsoluteError,
    decimal? P90AbsoluteError,
    DateTimeOffset? FirstComparedAtUtc,
    DateTimeOffset? LastComparedAtUtc,
    decimal? MeanRelativeErrorPct,
    StrategyReportAtrImportMetadata? Metadata,
    string Explanation);
