using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Calculate historical close-to-next-open returns from explicitly defined, source-backed session boundaries.
/// WHEN CALLED: Research and qualification pipelines provide exact regular-session close/open bar timestamps.
/// IMPORTANT: This class does not guess exchange calendars, infer bars from incomplete sessions, or validate provider entitlements.
/// </summary>
public static class BoringOvernightHistoryCalculator
{
    public static BoringOvernightHistoryResult Calculate(
        string symbol,
        string provider,
        string? feed,
        IReadOnlyCollection<MarketBarObservation> observations,
        IReadOnlyList<OvernightSessionBoundary> boundaries,
        int requiredWindow = 20)
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
            if (!latestByTimestamp.TryGetValue(boundary.NextOpenBarTimestampUtc, out var openObservation))
            {
                missing.Add(new(boundary.NextSessionDate, "NEXT_SESSION_OPEN_MISSING", boundary.NextOpenBarTimestampUtc));
                continue;
            }

            var close = (decimal)closeObservation.Bar.Close;
            var open = (decimal)openObservation.Bar.Open;
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
                openObservation.ObservationId));
        }

        returns = returns.OrderBy(x => x.NextSessionDate).ToList();
        var complete = missing.Count == 0 && returns.Count == boundaries.Count;
        var enoughHistory = returns.Count >= requiredWindow;
        var window = returns.TakeLast(requiredWindow).ToArray();
        var lastFive = returns.TakeLast(5).ToArray();

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
                ? "Calculated from explicitly supplied close/open boundary timestamps and persisted observations. Provider truth, exchange-calendar correctness, corporate-action adjustment, and source coverage still require independent validation."
                : "INCOMPLETE_HISTORY: no rolling metric is emitted unless every requested pair exists and the required history count is met.");
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
    string NextOpenObservationId);

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
