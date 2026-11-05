using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.API.StrategyReports;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Composes existing persisted evidence into a sell-limit research result.
/// This service never fetches or imputes bars and never submits orders.
/// </summary>
public sealed class BoringOvernightSellLimitOrchestrator(
    IMarketBarObservationStore barStore,
    IThinkorswimAtrObservationStore atrStore)
{
    private const int RequiredWindow = 20;
    private const int AtrWarmupBars = 99;
    private const int RequiredProviderAtrBars = AtrWarmupBars;
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public async Task<BoringOvernightSellLimitOrchestrationResult> CalculateAsync(
        BoringOvernightSellLimitRequest request,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Symbol) || request.Symbol.Length > 8 ||
            !request.Symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1–8 letters, digits, dot, or hyphen.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ProviderName) || string.IsNullOrWhiteSpace(request.Feed))
            throw new ArgumentException("ProviderName and Feed are required.", nameof(request));
        if (request.TargetQuantilePct is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(request.TargetQuantilePct), "Target quantile must be 1–99.");
        if (asOfUtc == default || asOfUtc.Offset != TimeSpan.Zero || asOfUtc > DateTimeOffset.UtcNow.AddMinutes(1))
            throw new ArgumentException("asOfUtc must be a valid UTC timestamp no later than one minute in the future.", nameof(asOfUtc));

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var provider = request.ProviderName.Trim();
        var feed = request.Feed.Trim();
        var gates = new List<SellLimitEvidenceGate>();

        if (!feed.Equals("sip", StringComparison.OrdinalIgnoreCase))
            gates.Add(new("SIP_REQUIRED", false, $"Feed '{feed}' is not consolidated SIP. IEX or another partial feed cannot qualify the historical GTC_EXT target sample."));
        else
            gates.Add(new("SIP_REQUIRED", true, "Requested evidence feed is SIP; stored observations are still checked independently below."));

        var history = await new BoringOvernightSellWindowHistoryService(barStore)
            .GetHistoryAsync(symbol, provider, feed, asOfUtc, cancellationToken);
        gates.Add(new("HISTORY_20_OF_20", history.QualificationUsable,
            $"Complete eligible GTC_EXT windows: {history.CompleteWindowCount}/{history.RequiredWindow}; feed coverage: {history.FeedCoverage}. " +
            (history.QualificationUsable ? "Every 07:00–19:59 ET minute and exact prior-session close is present." : "All 20 windows must be complete on SIP; missing timestamps and reference closes are not imputed.")));

        var referenceSession = GetLatestCompletedRegularSessionDate(asOfUtc);
        var referenceCloseTimestamp = ToUtc(referenceSession, UsEquitiesSessionCalendar.GetRegularClose(referenceSession).AddMinutes(-1));
        var referenceRows = await barStore.GetLatestAsync(
            symbol, provider, referenceCloseTimestamp, referenceCloseTimestamp, 2,
            cancellationToken, "1m", feed);
        var referenceBar = referenceRows.Select(x => x.Bar)
            .SingleOrDefault(x => x.Timestamp == referenceCloseTimestamp &&
                x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Feed, feed, StringComparison.OrdinalIgnoreCase) &&
                x.Timeframe.Equals("1m", StringComparison.OrdinalIgnoreCase));
        var referenceClose = referenceBar is null ? (decimal?)null : (decimal)referenceBar.Close;
        gates.Add(new("EXACT_REFERENCE_CLOSE", referenceClose is > 0,
            referenceBar is null
                ? $"Missing exact {referenceCloseTimestamp:O} 1m {provider}/{feed} reference-close bar for session {referenceSession:yyyy-MM-dd}."
                : $"Reference close is {referenceClose.GetValueOrDefault()} from exact 1m bar {referenceCloseTimestamp:O} ({provider}/{feed})."));

        var atrRows = (await atrStore.GetLatestAsync(symbol, "5m", null, null, 10000, cancellationToken))
            .Where(x => x.Metadata.StudyVersion.Equals("MCC_OHLCV_ATR_V2.1", StringComparison.Ordinal))
            .ToArray();
        var parity = ThinkorswimAtrParityService.Compare(atrRows);
        var orderedAtrRows = atrRows.OrderBy(x => x.Observation.SourceTimestampUtc).ToArray();
        var latestAtr = orderedAtrRows.LastOrDefault();
        var metadata = latestAtr?.Metadata;

        var settingsValid = metadata is not null &&
            metadata.AtrPeriod == 14 &&
            metadata.AtrAverageType.Equals("Wilder", StringComparison.OrdinalIgnoreCase) &&
            metadata.ChartTimezone.Equals("America/Phoenix", StringComparison.OrdinalIgnoreCase) &&
            metadata.ExtendedHours &&
            metadata.CorporateActionAdjustment &&
            metadata.StudyVersion.Equals("MCC_OHLCV_ATR_V2.1", StringComparison.Ordinal);
        gates.Add(new("ATR_STUDY_SETTINGS", settingsValid,
            settingsValid
                ? "ATR(14) Wilder; Phoenix timezone; extended-hours ON; corporate-action adjustment ON; MCC_OHLCV_ATR_V2."
                : "ATR provenance must be ATR(14) Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON and MCC_OHLCV_ATR_V2."));

        var parityMatched = parity.Status == "MATCH";
        gates.Add(new("ATR_PARITY", parityMatched,
            $"Timestamp-matched server Wilder ATR status: {parity.Status}; compared bars: {parity.ComparedBarCount}; mean absolute error: {parity.MeanAbsoluteError?.ToString("0.000000") ?? "DATA?"}; maximum absolute error: {parity.MaximumAbsoluteError?.ToString("0.000000") ?? "DATA?"}. {parity.Explanation}"));

        var lastWarmupRows = orderedAtrRows.TakeLast(AtrWarmupBars).ToArray();
        var continuousWarmup = lastWarmupRows.Length == RequiredProviderAtrBars &&
            lastWarmupRows.Zip(lastWarmupRows.Skip(1), (left, right) =>
                right.Observation.SourceTimestampUtc - left.Observation.SourceTimestampUtc)
                .All(delta => delta == TimeSpan.FromMinutes(5));
        gates.Add(new("CONTINUOUS_5M_ATR_WARMUP", continuousWarmup,
            $"Need {AtrWarmupBars} consecutive completed 5m TOS observations; found {lastWarmupRows.Length} in the latest warm-up window."));

        var providerAtrBars = Array.Empty<MarketBarObservation>();
        var providerBarsMatch = false;
        var providerMismatchCount = 0;
        if (lastWarmupRows.Length > 0)
        {
            providerAtrBars = (await barStore.GetLatestAsync(
                symbol, provider, lastWarmupRows[0].Observation.SourceTimestampUtc,
                lastWarmupRows[^1].Observation.SourceTimestampUtc, 10000,
                cancellationToken, "5m", feed)).ToArray();
            var providerByTimestamp = providerAtrBars
                .Select(x => x.Bar)
                .GroupBy(x => x.Timestamp)
                .ToDictionary(g => g.Key, g => g.Last());
            foreach (var stored in lastWarmupRows)
            {
                var source = stored.Observation;
                if (!providerByTimestamp.TryGetValue(source.SourceTimestampUtc, out var providerBar) ||
                    !MatchesExactOhlcv(providerBar, source))
                    providerMismatchCount++;
            }
            providerBarsMatch = lastWarmupRows.Length == AtrWarmupBars &&
                providerMismatchCount == 0 &&
                providerAtrBars.Length >= AtrWarmupBars;
        }
        gates.Add(new("SAME_SOURCE_5M_OHLCV", providerBarsMatch,
            $"Exact 5m provider/TOS OHLCV matches over the latest warm-up: {Math.Max(0, lastWarmupRows.Length - providerMismatchCount)}/{AtrWarmupBars}; mismatches or missing timestamps: {providerMismatchCount}."));

        var atrTimestampValid = latestAtr is not null &&
            latestAtr.Observation.SourceTimestampUtc.AddMinutes(5) <= asOfUtc &&
            asOfUtc - latestAtr.Observation.SourceTimestampUtc.AddMinutes(5) <= TimeSpan.FromMinutes(10);
        gates.Add(new("CURRENT_5M_ATR_FRESHNESS", atrTimestampValid,
            latestAtr is null
                ? "No current 5m Thinkorswim ATR observation is persisted."
                : $"Latest 5m ATR bar starts {latestAtr.Observation.SourceTimestampUtc:O}; completion is {latestAtr.Observation.SourceTimestampUtc.AddMinutes(5):O}; evaluated at {asOfUtc:O}."));

        gates.Add(new("WALK_FORWARD_VALIDATION", false,
            "Time-separated train/test target-touch and adverse-excursion validation is not yet implemented. The numeric sell limit remains withheld until this independent validation exists."));

        var allPassed = gates.All(x => x.Passed);
        BoringOvernightSellLimitResult? calculation = null;
        if (allPassed && referenceClose is > 0 && latestAtr is not null)
        {
            calculation = BoringOvernightSellLimitCalculator.Calculate(
                referenceClose.GetValueOrDefault(),
                latestAtr.Observation.Atr,
                latestAtr.Observation.SourceTimestampUtc,
                parity.Status,
                history.CalculatorObservations,
                request.TargetQuantilePct,
                RequiredWindow);
            if (calculation.Status != "PROVISIONAL" || calculation.CalculatedLimitSell is null)
                gates.Add(new("SELL_LIMIT_CALCULATION", false, calculation.Explanation));
            else
                gates.Add(new("SELL_LIMIT_CALCULATION", true, calculation.Explanation));
        }
        else
        {
            gates.Add(new("SELL_LIMIT_CALCULATION", false,
                "Numeric sell limit withheld because one or more upstream evidence gates failed."));
        }

        var approved = gates.All(x => x.Passed) && calculation?.CalculatedLimitSell is not null;
        return new BoringOvernightSellLimitOrchestrationResult(
            approved ? "PROVISIONAL" : "BLOCKED",
            symbol, provider, feed, request.TargetQuantilePct, asOfUtc,
            referenceSession, referenceCloseTimestamp, referenceClose,
            latestAtr?.Observation.Atr, latestAtr?.Observation.SourceTimestampUtc,
            parity, lastWarmupRows.Length, providerAtrBars.Length, providerMismatchCount,
            history, calculation, gates,
            approved
                ? "A research-only sell-limit candidate was calculated from 20 complete SIP GTC_EXT sessions and matched current 5m ATR evidence. This is not a broker order, executable quote, fill probability or validated walk-forward result."
                : "No numeric sell limit is issued. Resolve every failed evidence gate; no data has been imputed and no order has been submitted.");
    }

    private static bool MatchesExactOhlcv(MarketBar provider, StrategyReportAtrObservation tos)
    {
        const decimal tolerance = 0m;
        return Math.Abs((decimal)provider.Open - tos.Open) <= tolerance &&
            Math.Abs((decimal)provider.High - tos.High) <= tolerance &&
            Math.Abs((decimal)provider.Low - tos.Low) <= tolerance &&
            Math.Abs((decimal)provider.Close - tos.Close) <= tolerance &&
            provider.Volume == tos.Volume;
    }

    private static DateOnly GetLatestCompletedRegularSessionDate(DateTimeOffset asOfUtc)
    {
        var easternNow = TimeZoneInfo.ConvertTime(asOfUtc, Eastern);
        var candidate = DateOnly.FromDateTime(easternNow.Date);
        if (!UsEquitiesSessionCalendar.IsTradingSession(candidate) ||
            TimeOnly.FromDateTime(easternNow.DateTime) < UsEquitiesSessionCalendar.GetRegularClose(candidate))
        {
            do { candidate = candidate.AddDays(-1); }
            while (!UsEquitiesSessionCalendar.IsTradingSession(candidate));
        }
        return candidate;
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }
}

public sealed record BoringOvernightSellLimitRequest(
    string Symbol,
    string ProviderName,
    string Feed,
    int TargetQuantilePct);

public sealed record SellLimitEvidenceGate(string Code, bool Passed, string Detail);

public sealed record BoringOvernightSellLimitOrchestrationResult(
    string Status,
    string Symbol,
    string Provider,
    string Feed,
    int TargetQuantilePct,
    DateTimeOffset EvaluatedAtUtc,
    DateOnly ReferenceSessionDate,
    DateTimeOffset ReferenceCloseTimestampUtc,
    decimal? ReferenceClose,
    decimal? CurrentAtr,
    DateTimeOffset? AtrAsOfUtc,
    ThinkorswimAtrParityResult AtrParity,
    int TOSWarmupObservationCount,
    int ProviderWarmupObservationCount,
    int ProviderBarMismatchCount,
    BoringOvernightSellWindowHistoryResult History,
    BoringOvernightSellLimitResult? Calculation,
    IReadOnlyList<SellLimitEvidenceGate> Gates,
    string Explanation);
