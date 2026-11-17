using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Calculate historical close-to-next-open returns from explicitly defined, source-backed session boundaries.
/// WHEN CALLED: Research and qualification pipelines provide exact regular-session close/open bar timestamps.
/// IMPORTANT: This class does not guess exchange calendars, infer bars from incomplete sessions, or validate provider entitlements.
/// </summary>
public static class BoringOvernightHistoryCalculator
{
    /// <summary>Reference kind for a next-session open taken from the 09:30 one-minute bar.</summary>
    public const string OneMinuteOpenKind = "ONE_MINUTE_BAR_OPEN_PROXY";

    /// <summary>Reference kind for a next-session open taken from the unadjusted SIP Daily bar (operator decision 2026-11-17).</summary>
    public const string DailyOpenKind = "DAILY_BAR_OPEN_SIP_RAW";

    /// <summary>Largest allowed gap between the Daily close and the 15:59 one-minute close of the previous session before
    /// the Daily open is refused as a different price basis (for example a split or adjusted bars). The Daily close includes
    /// the closing auction, so a small difference is normal.</summary>
    public const decimal DailyBasisTolerancePct = 1.0m;

    /// <summary>
    /// PURPOSE: Builds the close-to-next-open pairs for the given boundaries and the rolling metrics when every pair exists.
    /// When the 09:30 one-minute bar is missing and <paramref name="dailyOpenFallback"/> holds unadjusted consolidated (SIP)
    /// Daily bars for both sessions of the pair, the Daily bar's open is used instead (operator decision 2026-11-17, HIG
    /// 2026-10-21 had no 09:30 SIP minute bar). The Daily open is the session's first consolidated price, not a nearby bar,
    /// and the pair is labelled DAILY_BAR_OPEN_SIP_RAW. It is refused if the Daily close of the previous session differs
    /// from the 15:59 minute close by more than DailyBasisTolerancePct.
    /// WHEN CALLED: By capture-history and the explicit history endpoints.
    /// </summary>
    public static BoringOvernightHistoryResult Calculate(
        string symbol,
        string provider,
        string? feed,
        IReadOnlyCollection<MarketBarObservation> observations,
        IReadOnlyList<OvernightSessionBoundary> boundaries,
        int requiredWindow = 20,
        IReadOnlyDictionary<DateOnly, DailySessionObservation>? dailyOpenFallback = null)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol is required.", nameof(symbol));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(boundaries);
        if (requiredWindow < 5) throw new ArgumentOutOfRangeException(nameof(requiredWindow), "Required window must be at least five sessions.");
        if (boundaries.Count == 0) throw new ArgumentException("At least one explicit session boundary is required.", nameof(boundaries));

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedProvider = provider.Trim();
        var relevant = observations
            .Where(x => x.Symbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase)
                && x.Provider.Equals(normalizedProvider, StringComparison.OrdinalIgnoreCase)
                && (feed is null || string.Equals(x.Bar.Feed, feed, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var duplicateIds = relevant.GroupBy(x => x.ObservationId, StringComparer.Ordinal).Any(g => g.Count() > 1);
        if (duplicateIds) throw new InvalidDataException("Duplicate observation IDs were supplied.");

        // Do not silently combine IEX/SIP (or other) feed rows when the caller omitted feed.
        var distinctFeeds = relevant.Select(x => x.Bar.Feed ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (feed is null && distinctFeeds.Length > 1)
            throw new InvalidDataException("Feed must be specified when stored observations span multiple feeds.");

        var latestByTimestamp = relevant
            .GroupBy(x => x.SourceTimestamp)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ReceivedAt).First());

        var orderedBoundaries = boundaries.OrderBy(x => x.NextSessionDate).ToArray();
        if (orderedBoundaries.GroupBy(x => x.NextSessionDate).Any(g => g.Count() > 1))
            throw new ArgumentException("Only one Overnight pair per next-session date is allowed.", nameof(boundaries));

        var missing = new List<OvernightMissingBoundary>();
        var returns = new List<OvernightReturnObservation>();
        foreach (var boundary in orderedBoundaries)
        {
            if (boundary.PreviousSessionDate >= boundary.NextSessionDate)
                throw new ArgumentException("PreviousSessionDate must precede NextSessionDate.", nameof(boundaries));
            if (boundary.PreviousCloseBarTimestampUtc.Offset != TimeSpan.Zero ||
                boundary.NextOpenBarTimestampUtc.Offset != TimeSpan.Zero ||
                boundary.PreviousCloseBarTimestampUtc >= boundary.NextOpenBarTimestampUtc)
                throw new ArgumentException("Boundary bar timestamps must be UTC and close must precede next open.", nameof(boundaries));

            if (!latestByTimestamp.TryGetValue(boundary.PreviousCloseBarTimestampUtc, out var closeObservation))
            {
                missing.Add(new(boundary.NextSessionDate, "PREVIOUS_SESSION_CLOSE_MISSING", boundary.PreviousCloseBarTimestampUtc));
                continue;
            }
            var close = (decimal)closeObservation.Bar.Close;
            decimal open;
            string openObservationId, openKind;
            if (latestByTimestamp.TryGetValue(boundary.NextOpenBarTimestampUtc, out var openObservation))
            {
                open = (decimal)openObservation.Bar.Open;
                openObservationId = openObservation.ObservationId;
                openKind = OneMinuteOpenKind;
            }
            else if (dailyOpenFallback is not null
                && dailyOpenFallback.TryGetValue(boundary.NextSessionDate, out var nextDaily)
                && dailyOpenFallback.TryGetValue(boundary.PreviousSessionDate, out var previousDaily))
            {
                var dailyClose = (decimal)previousDaily.Close;
                if (close <= 0 || dailyClose <= 0 || Math.Abs(dailyClose / close - 1m) * 100m > DailyBasisTolerancePct)
                {
                    missing.Add(new(boundary.NextSessionDate, "NEXT_SESSION_OPEN_MISSING_DAILY_BASIS_MISMATCH", boundary.NextOpenBarTimestampUtc));
                    continue;
                }
                open = (decimal)nextDaily.Open;
                openObservationId = "DAILY:" + boundary.NextSessionDate.ToString("yyyy-MM-dd");
                openKind = DailyOpenKind;
            }
            else
            {
                missing.Add(new(boundary.NextSessionDate, "NEXT_SESSION_OPEN_MISSING", boundary.NextOpenBarTimestampUtc));
                continue;
            }

            if (close <= 0 || open <= 0)
            {
                missing.Add(new(boundary.NextSessionDate, "NON_POSITIVE_BOUNDARY_PRICE", close <= 0
                    ? boundary.PreviousCloseBarTimestampUtc : boundary.NextOpenBarTimestampUtc));
                continue;
            }

            returns.Add(new(
                boundary.PreviousSessionDate,
                boundary.NextSessionDate,
                boundary.PreviousCloseBarTimestampUtc,
                boundary.NextOpenBarTimestampUtc,
                close,
                open,
                (open / close - 1m) * 100m,
                closeObservation.ObservationId,
                openObservationId,
                openKind,
                false));
        }

        returns = returns.OrderBy(x => x.NextSessionDate).ToList();
        var complete = missing.Count == 0 && returns.Count == boundaries.Count;
        var enoughHistory = returns.Count >= requiredWindow;
        var window = returns.TakeLast(requiredWindow).ToArray();
        var lastFive = returns.TakeLast(5).ToArray();
        var dailyOpenDates = returns.Where(x => x.NextOpenReferenceKind == DailyOpenKind)
            .Select(x => x.NextSessionDate.ToString("yyyy-MM-dd")).ToArray();
        var dailyOpenNote = dailyOpenDates.Length == 0 ? string.Empty
            : $" DAILY OPEN USED for {string.Join(", ", dailyOpenDates)}: no 09:30 one-minute bar, so the unadjusted SIP Daily bar's open was used (operator decision 2026-11-17).";

        return new BoringOvernightHistoryResult(
            normalizedSymbol,
            normalizedProvider,
            feed,
            DateTimeOffset.UtcNow,
            requiredWindow,
            boundaries.Count,
            returns.Count,
            missing.Count,
            complete,
            enoughHistory,
            complete && enoughHistory ? returns.Count(x => x.ReturnPct > 0) * 100m / returns.Count : null,
            complete && enoughHistory ? returns.Average(x => x.ReturnPct) : null,
            complete && enoughHistory ? Median(returns.Select(x => x.ReturnPct)) : null,
            complete && enoughHistory ? window.Average(x => x.ReturnPct) : null,
            complete && enoughHistory ? lastFive.Average(x => x.ReturnPct) : null,
            complete && enoughHistory ? window.Count(x => x.ReturnPct > 0) * 100m / window.Length : null,
            returns,
            missing,
            complete && enoughHistory
                ? "Calculated from previous regular-session close through the next regular-session 09:30 one-minute bar Open. This interval spans Day 1 after-hours and Day 2 premarket in its net endpoint return, but does not reconstruct the path or eligible-window extrema. The 09:30 bar Open is a first-minute bar-open proxy, not a verified official opening-auction print. Provider truth, exchange-calendar correctness, corporate-action adjustment, and source coverage still require independent validation." + dailyOpenNote
                : "INCOMPLETE_HISTORY: no rolling metric is emitted unless every requested pair exists and the required history count is met." + dailyOpenNote);
    }

    private static decimal Median(IEnumerable<decimal> values)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        if (sorted.Length == 0) throw new ArgumentException("Median requires at least one value.", nameof(values));
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2m
            : sorted[middle];
    }
}

public sealed record OvernightSessionBoundary(
    DateOnly PreviousSessionDate,
    DateOnly NextSessionDate,
    DateTimeOffset PreviousCloseBarTimestampUtc,
    DateTimeOffset NextOpenBarTimestampUtc);

public sealed record OvernightReturnObservation(
    DateOnly PreviousSessionDate,
    DateOnly NextSessionDate,
    DateTimeOffset PreviousCloseBarTimestampUtc,
    DateTimeOffset NextOpenBarTimestampUtc,
    decimal PreviousClose,
    decimal NextOpen,
    decimal ReturnPct,
    string PreviousCloseObservationId,
    string NextOpenObservationId,
    string NextOpenReferenceKind = "ONE_MINUTE_BAR_OPEN_PROXY",
    bool OfficialOpeningAuctionVerified = false);

public sealed record OvernightMissingBoundary(
    DateOnly NextSessionDate,
    string ReasonCode,
    DateTimeOffset MissingTimestampUtc);

public sealed record BoringOvernightHistoryResult(
    string Symbol,
    string Provider,
    string? Feed,
    DateTimeOffset CalculatedAtUtc,
    int RequiredWindow,
    int RequestedPairCount,
    int ValidPairCount,
    int MissingPairCount,
    bool AllPairsComplete,
    bool EnoughHistory,
    decimal? WinRatePct,
    decimal? AverageReturnPct,
    decimal? MedianReturnPct,
    decimal? AverageReturnPct20D,
    decimal? AverageReturnPct5D,
    decimal? WinRatePct20D,
    IReadOnlyList<OvernightReturnObservation> Returns,
    IReadOnlyList<OvernightMissingBoundary> Missing,
    string ProvenanceNote);

public sealed record BoringOvernightHistoryRequest(
    string Symbol,
    string Provider,
    string? Feed,
    IReadOnlyList<OvernightSessionBoundary> Boundaries,
    int RequiredWindow = 20);
