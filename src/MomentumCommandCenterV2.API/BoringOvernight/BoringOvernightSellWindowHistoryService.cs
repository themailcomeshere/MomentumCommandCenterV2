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
    private const int RequiredWindow = 40;
    private const int MinutesPerEligibleWindow = 13 * 60;
    private const int QueryLimitPerChunk = 10000;
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateOnly GetLatestCompletedSellSessionDate(DateTimeOffset asOfUtc)
    {
        if (asOfUtc == default || asOfUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("asOfUtc must be a non-default UTC timestamp.", nameof(asOfUtc));
        var easternNow = TimeZoneInfo.ConvertTime(asOfUtc, Eastern);
        var latest = DateOnly.FromDateTime(easternNow.Date);
        if (!UsEquitiesSessionCalendar.IsTradingSession(latest) ||
            TimeOnly.FromDateTime(easternNow.DateTime) < new TimeOnly(20, 0))
        {
            do { latest = latest.AddDays(-1); }
            while (!UsEquitiesSessionCalendar.IsTradingSession(latest));
        }
        return latest;
    }

    public async Task<BoringOvernightSellWindowHistoryResult> CaptureHistoryAsync(
        IMarketDataProvider provider,
        string symbol,
        string feed,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (!provider.IsConfigured)
            throw new ProviderNotConfiguredException($"Provider {provider.ProviderName} is not configured.");
        // Fail before making a multi-week provider request when the requested feed differs
        // from the provider's effective configuration. This is especially important for
        // SIP-only GTC_EXT qualification: silently fetching IEX cannot satisfy that contract.
        // Delayed historical SIP (operator decision 2026-11-14): when SIP is requested, the running feed is not SIP
        // and the mode is enabled, fetch completed windows on SIP with every request ending >= 16 min before now.
        var delayedSip = provider is AlpacaMarketDataAdapter delayedAlpaca &&
            string.Equals(feed?.Trim(), "sip", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(delayedAlpaca.ConfiguredFeed, "sip", StringComparison.OrdinalIgnoreCase) &&
            delayedAlpaca.DelayedHistoricalSipMinutesAllowed;
        if (provider is AlpacaMarketDataAdapter alpaca && !delayedSip &&
            !string.Equals(alpaca.ConfiguredFeed, feed?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Requested feed '{feed}' does not match the running Alpaca configuration '{alpaca.ConfiguredFeed}'. " +
                "No provider request was made and no observations were persisted. Check /api/providers/status and the effective " +
                "MarketData:Alpaca:Feed setting (including environment-variable overrides), restart the API, then retry.");
        }
        if (string.IsNullOrWhiteSpace(feed))
            throw new ArgumentException("Feed is required to keep GTC_EXT evidence source-isolated.", nameof(feed));
        if (asOfUtc == default || asOfUtc.Offset != TimeSpan.Zero || asOfUtc > DateTimeOffset.UtcNow)
            throw new ArgumentException("asOfUtc must be a non-default UTC timestamp no later than now.", nameof(asOfUtc));

        // In delayed-SIP mode a window counts as completed only once its 20:00 ET end is older than the SIP delay.
        var effectiveAsOfUtc = delayedSip ? asOfUtc - AlpacaDailyMarketDataClient.SipDelay : asOfUtc;
        var latestSession = GetLatestCompletedSellSessionDate(effectiveAsOfUtc);
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(latestSession, RequiredWindow);
        var fromUtc = boundaries[0].PreviousCloseBarTimestampUtc;
        var toUtc = ToUtc(boundaries[^1].NextSessionDate, new TimeOnly(20, 0));
        var fetched = new List<MarketBar>();
        var cursor = fromUtc;
        while (cursor < toUtc)
        {
            var chunkEnd = cursor.AddDays(7) < toUtc ? cursor.AddDays(7) : toUtc;
            var chunk = delayedSip
                ? await ((AlpacaMarketDataAdapter)provider).GetDelayedSipBarsAsync(
                    symbol.Trim().ToUpperInvariant(), TimeSpan.FromMinutes(1), cursor, chunkEnd, asOfUtc, cancellationToken)
                : await provider.GetBarsAsync(
                    symbol.Trim().ToUpperInvariant(), TimeSpan.FromMinutes(1), cursor, chunkEnd, cancellationToken);
            fetched.AddRange(chunk);
            cursor = chunkEnd;
        }

        var wrongSource = fetched.FirstOrDefault(x =>
            !string.Equals(x.Symbol, symbol.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(x.Provider, provider.ProviderName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(x.Feed, feed.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(x.Timeframe, "1m", StringComparison.OrdinalIgnoreCase) ||
            x.Timestamp < fromUtc || x.Timestamp > toUtc);
        if (wrongSource is not null)
            throw new InvalidOperationException(
                $"Provider returned a bar outside the requested source contract (symbol={wrongSource.Symbol}, provider={wrongSource.Provider}, feed={wrongSource.Feed ?? "UNKNOWN"}, timeframe={wrongSource.Timeframe}, timestamp={wrongSource.Timestamp:O}). Requested symbol={symbol}, provider={provider.ProviderName}, feed={feed}, timeframe=1m. No bars were persisted.");

        var normalized = fetched.GroupBy(x => x.Timestamp).Select(group => group.First())
            .OrderBy(x => x.Timestamp).ToArray();
        var persisted = normalized.Length == 0 ? 0 : await store.AppendAsync(normalized, cancellationToken);
        var audit = await GetHistoryAsync(symbol, provider.ProviderName, feed, effectiveAsOfUtc, cancellationToken);
        return audit with
        {
            CapturePerformed = true,
            FetchedBarCount = normalized.Length,
            NewlyPersistedBarCount = persisted
        };
    }

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

        var latestSellSession = GetLatestCompletedSellSessionDate(asOfUtc);

        // Each boundary's NextSessionDate is the sell-window date; PreviousCloseBarTimestampUtc
        // is the exact reference-close bar timestamp (15:59 or 12:59 ET).
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(latestSellSession, RequiredWindow);
        var allObservations = new List<MarketBarObservation>();
        var queryCursor = boundaries[0].PreviousCloseBarTimestampUtc;
        var queryEndUtc = ToUtc(boundaries[^1].NextSessionDate, new TimeOnly(20, 0));
        // Read source-isolated 7-day chunks instead of rescanning the full JSONL store once per session.
        // Feed is filtered in the store before the 10,000-row cap is applied.
        while (queryCursor < queryEndUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkEnd = queryCursor.AddDays(7) < queryEndUtc ? queryCursor.AddDays(7) : queryEndUtc;
            var chunk = await store.GetLatestAsync(
                symbol.Trim().ToUpperInvariant(), provider.Trim(),
                queryCursor, chunkEnd, QueryLimitPerChunk, cancellationToken, "1m", feed.Trim());
            allObservations.AddRange(chunk);
            queryCursor = chunkEnd;
        }

        var windows = new List<BoringOvernightSellWindowAudit>(RequiredWindow);
        foreach (var boundary in boundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sessionDate = boundary.NextSessionDate;
            var windowStartUtc = ToUtc(sessionDate, new TimeOnly(7, 0));
            var windowEndUtc = ToUtc(sessionDate, new TimeOnly(20, 0)); // exclusive; final expected bar starts 19:59 ET

            var observations = allObservations
                .Where(x => x.SourceTimestamp >= boundary.PreviousCloseBarTimestampUtc && x.SourceTimestamp < windowEndUtc)
                .ToArray();

            var sourceBars = observations
                .Select(x => x.Bar)
                .Where(x => x.Timestamp >= boundary.PreviousCloseBarTimestampUtc && x.Timestamp < windowEndUtc)
                .Where(x => string.Equals(x.Provider, provider.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.Feed, feed.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.Timeframe, "1m", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Timestamp)
                .Select(group => group.Last())
                .ToArray();

            var referenceBar = sourceBars.SingleOrDefault(x => x.Timestamp == boundary.PreviousCloseBarTimestampUtc);
            var barsByTimestamp = sourceBars
                .Where(x => x.Timestamp >= windowStartUtc && x.Timestamp < windowEndUtc)
                .ToDictionary(x => x.Timestamp, x => x);

            var empty = new List<DateTimeOffset>();
            for (var minute = 0; minute < MinutesPerEligibleWindow; minute++)
            {
                var expected = windowStartUtc.AddMinutes(minute);
                if (!barsByTimestamp.ContainsKey(expected)) empty.Add(expected);
            }

            // NO_TRADE rule (operator decision 2026-11-14). A minute without a bar is NO_TRADE, never priced, only when:
            //  1. the feed is consolidated SIP (on IEX an empty minute may hide a trade on another venue);
            //  2. the exact reference-close bar is present; and
            //  3. the sell session's regular session has at least one bar, which shows data was delivered for that day.
            // SIP bars are persisted only by the sell-window capture, which writes nothing unless every provider request
            // succeeded without truncation, so an accepted window comes from clean responses. Otherwise empty minutes stay MISSING.
            var regularOpenUtc = ToUtc(sessionDate, new TimeOnly(9, 30));
            var regularCloseUtc = ToUtc(sessionDate, UsEquitiesSessionCalendar.GetRegularClose(sessionDate));
            var regularSessionHasBars = barsByTimestamp.Keys.Any(t => t >= regularOpenUtc && t < regularCloseUtc);
            var consolidated = feed.Trim().Equals("sip", StringComparison.OrdinalIgnoreCase);
            string noTradeRule;
            if (empty.Count == 0) noTradeRule = "NOT_NEEDED";
            else if (!consolidated) noTradeRule = "NOT_APPLIED_PARTIAL_FEED";
            else if (referenceBar is null) noTradeRule = "NOT_APPLIED_REFERENCE_CLOSE_MISSING";
            else if (!regularSessionHasBars) noTradeRule = "NOT_APPLIED_NO_REGULAR_SESSION_BARS";
            else noTradeRule = "APPLIED";
            var noTradeCount = noTradeRule == "APPLIED" ? empty.Count : 0;
            var missing = noTradeRule == "APPLIED" ? new List<DateTimeOffset>() : empty;

            var observedBars = barsByTimestamp.Values.OrderBy(x => x.Timestamp).ToArray();
            var highest = observedBars.Length == 0 ? (decimal?)null : observedBars.Max(x => (decimal)x.High);
            var lowest = observedBars.Length == 0 ? (decimal?)null : observedBars.Min(x => (decimal)x.Low);
            var complete = referenceBar is not null && missing.Count == 0;
            windows.Add(new BoringOvernightSellWindowAudit(
                boundary.PreviousSessionDate,
                sessionDate,
                boundary.PreviousCloseBarTimestampUtc,
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
                complete)
            {
                LowestEligiblePrice = lowest,
                NoTradeIntervalCount = noTradeCount,
                NoTradeRule = noTradeRule,
                NoTradeIntervalSampleUtc = noTradeRule == "APPLIED" ? empty.Take(10).ToArray() : Array.Empty<DateTimeOffset>()
            });
        }

        var completeCount = windows.Count(x => x.CoverageComplete);
        var calculatorObservations = windows.Select(x => new BoringOvernightSellWindowObservation(
            x.SellSessionDate,
            x.ReferenceClose ?? 0m,
            x.HighestEligiblePrice ?? 0m,
            x.CoverageComplete,
            x.MissingIntervalCount)
        { LowestEligiblePrice = x.LowestEligiblePrice ?? 0m }).ToArray();

        return new BoringOvernightSellWindowHistoryResult(
            symbol.Trim().ToUpperInvariant(), provider.Trim(), feed.Trim(), asOfUtc,
            RequiredWindow, MinutesPerEligibleWindow, completeCount,
            completeCount == RequiredWindow,
            feed.Trim().Equals("sip", StringComparison.OrdinalIgnoreCase) ? "CONSOLIDATED_SIP" : "PARTIAL_FEED",
            completeCount == RequiredWindow && feed.Trim().Equals("sip", StringComparison.OrdinalIgnoreCase),
            windows, calculatorObservations,
            "Uses persisted 1-minute bars only. Each sell window covers the 780 bar-start minutes from 07:00 through 19:59 America/New_York plus the exact prior regular-session close bar. On consolidated SIP, a minute with no bar is NO_TRADE (counted, never priced) when the reference close is present and the sell session's regular session has bars; otherwise it is MISSING. A window is complete only with zero MISSING minutes. NO_TRADE minutes cannot touch a limit; if one were really a lost bar, touch research would be conservative (understated), never overstated. Missing minutes and missing reference closes are not imputed. IEX is partial-exchange coverage and is never marked qualification-usable even if every timestamp exists; SIP is required for consolidated market-wide target research. Coverage is not proof of executable quotes, a limit touch, or an actual fill.");
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
    bool CoverageComplete)
{
    public decimal? LowestEligiblePrice { get; init; }

    /// <summary>Minutes without a bar classified NO_TRADE (consolidated SIP, reference close present, regular-session bars present). Never priced.</summary>
    public int NoTradeIntervalCount { get; init; }

    /// <summary>Why NO_TRADE was or was not applied: APPLIED, NOT_NEEDED, NOT_APPLIED_PARTIAL_FEED, NOT_APPLIED_REFERENCE_CLOSE_MISSING, NOT_APPLIED_NO_REGULAR_SESSION_BARS.</summary>
    public string NoTradeRule { get; init; } = "NOT_NEEDED";

    /// <summary>Up to ten NO_TRADE minute timestamps for audit.</summary>
    public IReadOnlyList<DateTimeOffset> NoTradeIntervalSampleUtc { get; init; } = Array.Empty<DateTimeOffset>();
}

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
    string Note,
    bool CapturePerformed = false,
    int FetchedBarCount = 0,
    int NewlyPersistedBarCount = 0);

public sealed record BoringOvernightSellWindowCaptureRequest(
    string Symbol,
    string ProviderName,
    string Feed);
