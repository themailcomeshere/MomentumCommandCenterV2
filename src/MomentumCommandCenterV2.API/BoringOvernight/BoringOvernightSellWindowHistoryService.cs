using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Assemble exact, source/feed-isolated 07:00–20:00 ET GTC_EXT research windows
/// from already-persisted 1-minute observations.
/// IMPORTANT: Missing minutes and missing prior-session reference closes remain explicit gaps.
/// No provider fetch, interpolation, nearest-bar substitution, or fill inference occurs here.
/// </summary>
public sealed class BoringOvernightSellWindowHistoryService(IMarketBarObservationStore store)
{
    private const int RequiredWindow = 20;
    private const int MinutesPerEligibleWindow = 13 * 60;
    private const int QueryLimitPerSession = 1500;
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public async Task<BoringOvernightSellWindowHistoryResult> GetHistoryAsync(
        string symbol,
        string provider,
        string feed,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8 ||
            !symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1–8 letters, digits, dot, or hyphen.", nameof(symbol));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required to isolate historical observations.", nameof(provider));
        if (string.IsNullOrWhiteSpace(feed))
            throw new ArgumentException("Feed is required; IEX, SIP and other feeds must never be mixed.", nameof(feed));
        if (asOfUtc == default || asOfUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("asOfUtc must be a non-default UTC timestamp.", nameof(asOfUtc));
        if (asOfUtc > DateTimeOffset.UtcNow)
            throw new ArgumentException("asOfUtc cannot be in the future.", nameof(asOfUtc));

        var easternNow = TimeZoneInfo.ConvertTime(asOfUtc, Eastern);
        var latestSellSession = DateOnly.FromDateTime(easternNow.Date);
        if (!UsEquitiesSessionCalendar.IsTradingSession(latestSellSession) ||
            TimeOnly.FromDateTime(easternNow.DateTime) < new TimeOnly(20, 0))
        {
            do { latestSellSession = latestSellSession.AddDays(-1); }
            while (!UsEquitiesSessionCalendar.IsTradingSession(latestSellSession));
        }

        // Each boundary's NextSessionDate is the sell-window date; PreviousRegularCloseUtc
        // is the exact reference-close bar timestamp (15:59 or 12:59 ET).
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(latestSellSession, RequiredWindow);
        var windows = new List<BoringOvernightSellWindowAudit>(RequiredWindow);
        foreach (var boundary in boundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sessionDate = boundary.NextSessionDate;
            var windowStartUtc = ToUtc(sessionDate, new TimeOnly(7, 0));
            var windowEndUtc = ToUtc(sessionDate, new TimeOnly(20, 0)); // exclusive; final expected bar starts 19:59 ET

            var observations = await store.GetLatestAsync(
                symbol.Trim().ToUpperInvariant(), provider.Trim(),
                boundary.PreviousRegularCloseUtc, windowEndUtc,
                QueryLimitPerSession, cancellationToken, "1m");

            var sourceBars = observations
                .Select(x => x.Bar)
                .Where(x => x.Timestamp >= boundary.PreviousRegularCloseUtc && x.Timestamp < windowEndUtc)
                .Where(x => string.Equals(x.Provider, provider.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.Feed, feed.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.Timeframe, "1m", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Timestamp)
                .Select(group => group.Last())
                .ToArray();

            var referenceBar = sourceBars.SingleOrDefault(x => x.Timestamp == boundary.PreviousRegularCloseUtc);
            var barsByTimestamp = sourceBars
                .Where(x => x.Timestamp >= windowStartUtc && x.Timestamp < windowEndUtc)
                .ToDictionary(x => x.Timestamp, x => x);

            var missing = new List<DateTimeOffset>();
            for (var minute = 0; minute < MinutesPerEligibleWindow; minute++)
            {
                var expected = windowStartUtc.AddMinutes(minute);
                if (!barsByTimestamp.ContainsKey(expected)) missing.Add(expected);
            }

            var observedBars = barsByTimestamp.Values.OrderBy(x => x.Timestamp).ToArray();
            var highest = observedBars.Length == 0 ? (decimal?)null : observedBars.Max(x => (decimal)x.High);
            var complete = referenceBar is not null && missing.Count == 0;
            windows.Add(new BoringOvernightSellWindowAudit(
                boundary.PreviousSessionDate,
                sessionDate,
                boundary.PreviousRegularCloseUtc,
                referenceBar is null ? null : (decimal)referenceBar.Close,
                windowStartUtc,
                windowEndUtc,
                MinutesPerEligibleWindow,
                observedBars.Length,
                missing.Count,
                missing.Take(10).ToArray(),
                observedBars.FirstOrDefault()?.Timestamp,
                observedBars.LastOrDefault()?.Timestamp,
                highest,
                complete));
        }

        var completeCount = windows.Count(x => x.CoverageComplete);
        var calculatorObservations = windows.Select(x => new BoringOvernightSellWindowObservation(
            x.SellSessionDate,
            x.ReferenceClose ?? 0m,
            x.HighestEligiblePrice ?? 0m,
            x.CoverageComplete,
            x.MissingIntervalCount)).ToArray();

        return new BoringOvernightSellWindowHistoryResult(
            symbol.Trim().ToUpperInvariant(), provider.Trim(), feed.Trim(), asOfUtc,
            RequiredWindow, MinutesPerEligibleWindow, completeCount,
            completeCount == RequiredWindow,
            feed.Trim().Equals("sip", StringComparison.OrdinalIgnoreCase) ? "CONSOLIDATED_SIP" : "PARTIAL_FEED",
            completeCount == RequiredWindow && feed.Trim().Equals("sip", StringComparison.OrdinalIgnoreCase),
            windows, calculatorObservations,
            "Uses persisted 1-minute bars only. Each sell window requires all 780 exact bar-start timestamps from 07:00 through 19:59 America/New_York plus the exact prior regular-session close bar. Missing minutes and missing reference closes are not imputed. IEX is partial-exchange coverage and is never marked qualification-usable even if every timestamp exists; SIP is required for consolidated market-wide target research. Coverage is not proof of executable quotes, a limit touch, or an actual fill.");
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }
}

public sealed record BoringOvernightSellWindowAudit(
    DateOnly PreviousSessionDate,
    DateOnly SellSessionDate,
    DateTimeOffset ReferenceCloseTimestampUtc,
    decimal? ReferenceClose,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtcExclusive,
    int ExpectedIntervalCount,
    int ObservedIntervalCount,
    int MissingIntervalCount,
    IReadOnlyList<DateTimeOffset> MissingIntervalSampleUtc,
    DateTimeOffset? FirstObservedTimestampUtc,
    DateTimeOffset? LastObservedTimestampUtc,
    decimal? HighestEligiblePrice,
    bool CoverageComplete);

public sealed record BoringOvernightSellWindowHistoryResult(
    string Symbol,
    string Provider,
    string Feed,
    DateTimeOffset AsOfUtc,
    int RequiredWindow,
    int ExpectedIntervalsPerSession,
    int CompleteWindowCount,
    bool AllWindowsComplete,
    string FeedCoverage,
    bool QualificationUsable,
    IReadOnlyList<BoringOvernightSellWindowAudit> Windows,
    IReadOnlyList<BoringOvernightSellWindowObservation> CalculatorObservations,
    string Note);
