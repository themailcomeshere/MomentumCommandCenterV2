namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: DailySourceCrossCheck
/// PURPOSE: Compares two Daily sources session by session over the exact expected session dates. Prices must
/// match within an absolute tolerance; volume is reported as an absolute and relative difference and must
/// match within a relative tolerance. Missing sessions are reported, never filled.
/// WHEN CALLED: By GET /api/market-data/daily/cross-check and by unit tests.
/// </summary>
public static class DailySourceCrossCheck
{
    /// <summary>Default absolute price tolerance, matching the existing Daily qualification script.</summary>
    public const double DefaultPriceTolerance = 0.0001;

    /// <summary>Default relative volume tolerance (0 = exact, matching the existing Daily qualification script).</summary>
    public const double DefaultVolumeRelativeTolerance = 0.0;

    /// <summary>
    /// METHOD: Compare
    /// PURPOSE: Returns one row per expected date plus a summary verdict. The verdict is
    /// PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE only when every expected date is present in both sources and
    /// every row matches; otherwise FAIL with counts by status.
    /// WHEN CALLED: Once per cross-check request.
    /// </summary>
    public static DailyCrossCheckResult Compare(
        IReadOnlyList<DateOnly> expectedDates,
        IReadOnlyList<DailySessionObservation> primary,
        IReadOnlyList<DailySessionObservation> reference,
        double priceTolerance = DefaultPriceTolerance,
        double volumeRelativeTolerance = DefaultVolumeRelativeTolerance)
    {
        var a = primary.GroupBy(x => x.SessionDate).ToDictionary(g => g.Key, g => g.First());
        var b = reference.GroupBy(x => x.SessionDate).ToDictionary(g => g.Key, g => g.First());
        var rows = new List<DailyCrossCheckRow>(expectedDates.Count);
        foreach (var date in expectedDates)
        {
            a.TryGetValue(date, out var p);
            b.TryGetValue(date, out var r);
            if (p is null || r is null)
            {
                rows.Add(new DailyCrossCheckRow(date,
                    p is null && r is null ? "MISSING_BOTH" : p is null ? "MISSING_PRIMARY" : "MISSING_REFERENCE",
                    p, r, null, null, null, null, null, null));
                continue;
            }

            var open = p.Open - r.Open;
            var high = p.High - r.High;
            var low = p.Low - r.Low;
            var close = p.Close - r.Close;
            var volumeDiff = p.Volume - r.Volume;
            double? volumeRel = r.Volume == 0 ? null : (double)volumeDiff / r.Volume;
            var priceOk = new[] { open, high, low, close }.All(d => Math.Abs(d) <= priceTolerance);
            var volumeOk = volumeDiff == 0 || (volumeRel is not null && Math.Abs(volumeRel.Value) <= volumeRelativeTolerance);
            var status = priceOk && volumeOk ? "MATCH" : !priceOk && !volumeOk ? "PRICE_AND_VOLUME_MISMATCH" : !priceOk ? "PRICE_MISMATCH" : "VOLUME_MISMATCH";
            rows.Add(new DailyCrossCheckRow(date, status, p, r, open, high, low, close, volumeDiff, volumeRel));
        }

        var counts = rows.GroupBy(x => x.Status).ToDictionary(g => g.Key, g => g.Count());
        var verdict = rows.Count > 0 && rows.All(x => x.Status == "MATCH")
            ? "PASS_EXACT_DATES_OHLCV_WITHIN_TOLERANCE"
            : "FAIL";
        return new DailyCrossCheckResult(verdict, priceTolerance, volumeRelativeTolerance, counts, rows);
    }
}

/// <summary>
/// TYPE: DailyCrossCheckRow
/// PURPOSE: One expected session's comparison: status, both observations and primary-minus-reference differences.
/// WHEN CALLED: Produced by DailySourceCrossCheck.Compare.
/// </summary>
public sealed record DailyCrossCheckRow(
    DateOnly SessionDate,
    string Status,
    DailySessionObservation? Primary,
    DailySessionObservation? Reference,
    double? OpenDiff,
    double? HighDiff,
    double? LowDiff,
    double? CloseDiff,
    long? VolumeDiff,
    double? VolumeRelativeDiff);

/// <summary>
/// TYPE: DailyCrossCheckResult
/// PURPOSE: Verdict, tolerances, per-status counts and rows for a Daily source cross-check.
/// WHEN CALLED: Returned by DailySourceCrossCheck.Compare.
/// </summary>
public sealed record DailyCrossCheckResult(
    string Verdict,
    double PriceTolerance,
    double VolumeRelativeTolerance,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyList<DailyCrossCheckRow> Rows);
