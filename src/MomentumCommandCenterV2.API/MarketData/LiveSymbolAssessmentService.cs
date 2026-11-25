using System.Collections.Concurrent;
using System.Diagnostics;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Retrieves current Alpaca 1m/5m observations and evaluates the existing V2 signal engine.
/// WHEN CALLED: The MCC live-symbol assessment endpoint is requested by Main Chart or Red Zone.
/// IMPORTANT: Uses a fresh, provenance-confirmed WebSocket 1m series when eligible; otherwise falls back to 1m REST. 5m permission remains REST-backed. Permission is withheld when source evidence is incomplete.
/// </summary>
public sealed class LiveSymbolAssessmentService(
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore barStore,
    AlpacaMinuteBarStreamWorker streamWorker,
    IConfiguration configuration,
    LiveSignalLogStore signalLog,
    IntradaySettingsStore intradaySettings,
    ILogger<LiveSymbolAssessmentService> logger)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<string, (DateTimeOffset ExpiresAt, object Value)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// PURPOSE: Drops every cached assessment so the next request re-evaluates with current settings.
    /// WHEN CALLED: After the operator changes the opening hold (PUT /api/intraday/settings).
    /// </summary>
    public void InvalidateCache() => _cache.Clear();

    /// <summary>
    /// PURPOSE: Retrieves source bars, persists observations, computes indicators, and evaluates 5M permission / 1M execution.
    /// WHEN CALLED: GET /api/market-data/live-assessment?symbol=...
    /// </summary>
    public async Task<object> GetAsync(string symbol, string providerName, CancellationToken cancellationToken)
    {
        var requestStopwatch = Stopwatch.StartNew();
        var websocketStoreReadMs = 0d;
        var providerFetchMs = 0d;
        var persistenceMs = 0d;
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var provider = providers.FirstOrDefault(x =>
            string.Equals(x.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
            throw new ArgumentException($"Unknown provider '{providerName}'.");
        if (!provider.IsConfigured)
            throw new ProviderNotConfiguredException($"{provider.ProviderName} is not configured. Configure provider credentials on the API server.");

        var cacheKey = provider.ProviderName + "/" + normalizedSymbol;
        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now)
            return cached.Value;

        var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_cache.TryGetValue(cacheKey, out cached) && cached.ExpiresAt > now)
                return cached.Value;

            // 21 calendar days normally contain at least 10 regular sessions, including weekends/holidays.
            // A 3-day 1m window supplies enough history for the current EMA/SMA/ATR calculations.
            IReadOnlyList<MarketBar> oneMinute = Array.Empty<MarketBar>();
            var oneMinuteFromWebSocket = false;
            var configuredFeed = configuration["MarketData:Alpaca:Feed"] ?? "sip";
            if (string.Equals(provider.ProviderName, "Alpaca", StringComparison.OrdinalIgnoreCase) &&
                streamWorker.IsSubscribed &&
                string.Equals(streamWorker.Feed, configuredFeed, StringComparison.OrdinalIgnoreCase))
            {
                var websocketStoreStopwatch = Stopwatch.StartNew();
                var storedOneMinute = await barStore.GetLatestAsync(
                    normalizedSymbol, provider.ProviderName, now.AddDays(-3), now, 10000, cancellationToken, "1m");
                websocketStoreStopwatch.Stop();
                websocketStoreReadMs = websocketStoreStopwatch.Elapsed.TotalMilliseconds;
                var streamBars = CompletedBars(
                    storedOneMinute
                        .Where(x => string.Equals(x.Bar.Feed, streamWorker.Feed, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Bar).ToArray(),
                    TimeSpan.FromMinutes(1), now);
                var streamLatest = streamBars.LastOrDefault();
                var streamAge = streamLatest is null
                    ? (double?)null
                    : (now - streamLatest.Timestamp.AddMinutes(1)).TotalSeconds;
                if (streamBars.Count >= 50 && streamAge is >= 0 and <= 120 &&
                    string.Equals(streamLatest?.SourceTransport, "WEBSOCKET", StringComparison.OrdinalIgnoreCase))
                {
                    oneMinute = streamBars;
                    oneMinuteFromWebSocket = true;
                }
            }

            var oneMinuteFetchStopwatch = Stopwatch.StartNew();
            var oneMinuteFetchTask = oneMinuteFromWebSocket
                ? Task.FromResult(oneMinute)
                : provider.GetBarsAsync(normalizedSymbol, TimeSpan.FromMinutes(1),
                    now.AddDays(-3), now, cancellationToken);
            var oneMinuteTask = MeasureFetchAsync(oneMinuteFetchTask, oneMinuteFetchStopwatch);
            var fiveMinuteFetchStopwatch = Stopwatch.StartNew();
            var fiveMinuteFetchTask = provider.GetBarsAsync(normalizedSymbol, TimeSpan.FromMinutes(5),
                now.AddDays(-21), now, cancellationToken);
            var fiveMinuteTask = MeasureFetchAsync(fiveMinuteFetchTask, fiveMinuteFetchStopwatch);
            var providerFetchStopwatch = Stopwatch.StartNew();
            await Task.WhenAll(oneMinuteTask, fiveMinuteTask);
            providerFetchStopwatch.Stop();
            providerFetchMs = providerFetchStopwatch.Elapsed.TotalMilliseconds;

            if (!oneMinuteFromWebSocket)
                oneMinute = CompletedBars(await oneMinuteTask, TimeSpan.FromMinutes(1), now);
            var fiveMinute = CompletedBars(await fiveMinuteTask, TimeSpan.FromMinutes(5), now);
            var persistenceStopwatch = Stopwatch.StartNew();
            await barStore.AppendAsync(oneMinute, cancellationToken);
            await barStore.AppendAsync(fiveMinute, cancellationToken);
            persistenceStopwatch.Stop();
            persistenceMs = persistenceStopwatch.Elapsed.TotalMilliseconds;

            var calculationStopwatch = Stopwatch.StartNew();
            var evaluatedAt = DateTimeOffset.UtcNow;
            var one = oneMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.Calculate(oneMinute, oneMinute.Count - 1, TimeSpan.FromMinutes(1), 0m)
                : null;
            var rvol = fiveMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveMinute, fiveMinute.Count - 1)
                : (Value: (decimal?)null, Samples: 0);
            var five = fiveMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.Calculate(fiveMinute, fiveMinute.Count - 1, TimeSpan.FromMinutes(5), rvol.Value ?? 0m)
                : null;
            var previousRvol = fiveMinute.Count >= 51
                ? LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveMinute, fiveMinute.Count - 2)
                : (Value: (decimal?)null, Samples: 0);
            var previousFive = fiveMinute.Count >= 51
                ? LiveSymbolTechnicalCalculator.Calculate(fiveMinute, fiveMinute.Count - 2, TimeSpan.FromMinutes(5), previousRvol.Value ?? 0m)
                : null;

            // 5M permission = the installed TOS study's latch, run over every completed 5m bar (operator decision 2026-11-17).
            var latch = fiveMinute.Count >= 50
                ? FiveMinutePermissionLatch.Evaluate(LiveSymbolTechnicalCalculator.PermissionSeries(fiveMinute))
                : null;
            var latchLatest = latch?.Latest is { } latestStep && latestStep.Timestamp == fiveMinute[^1].Timestamp ? latestStep : null;

            var oneLatest = oneMinute.LastOrDefault();
            var fiveLatest = fiveMinute.LastOrDefault();
            var oneCloseAt = oneLatest?.Timestamp.AddMinutes(1);
            var fiveCloseAt = fiveLatest?.Timestamp.AddMinutes(5);
            var oneAgeSeconds = oneCloseAt.HasValue ? (evaluatedAt - oneCloseAt.Value).TotalSeconds : (double?)null;
            var fiveAgeSeconds = fiveCloseAt.HasValue ? (evaluatedAt - fiveCloseAt.Value).TotalSeconds : (double?)null;
            var oneFresh = oneAgeSeconds is >= 0 and <= 120;
            var fiveFresh = fiveAgeSeconds is >= 0 and <= 360;
            var regularSession = LiveSymbolTechnicalCalculator.IsRegularSession(evaluatedAt);
            var holdMinutes = intradaySettings.Current.OpeningEntryHoldMinutes;
            var openingHold = OpeningEntryHold.Evaluate(evaluatedAt, holdMinutes);

            SignalDecision? decision = null;
            // Operator-declared holding: the same snapshot evaluated with hasPosition=true so the Dashboard can show
            // RUNNER / RUNNER WANING / PREPARE TO SELL / SELL states. MCC has no broker position data (Schwab parked).
            SignalDecision? heldDecision = null;
            ExecutionEventStep? executionWindow = null;
            string evaluationStatus;
            string evaluationReason;
            if (one is null || five is null || previousFive is null)
            {
                evaluationStatus = "INSUFFICIENT_HISTORY";
                evaluationReason = $"Need at least 50 completed 1m bars and 51 completed 5m bars; received {oneMinute.Count} 1m and {fiveMinute.Count} 5m.";
            }
            else if (!oneFresh || !fiveFresh)
            {
                evaluationStatus = "STALE_MARKET_DATA";
                var staleDetails = new List<string>();
                if (!oneFresh) staleDetails.Add($"1m bar age {oneAgeSeconds?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}s (limit 120s; source {oneLatest?.Timestamp:O}; expected close {oneCloseAt:O})");
                if (!fiveFresh) staleDetails.Add($"5m bar age {fiveAgeSeconds?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}s (limit 360s; source {fiveLatest?.Timestamp:O}; expected close {fiveCloseAt:O})");
                evaluationReason = "STALE_MARKET_DATA: " + string.Join("; ", staleDetails) + $". Feed={oneLatest?.Feed ?? fiveLatest?.Feed ?? "unknown"}. Permission and execution are not evaluated.";
            }
            else if (rvol.Value is null)
            {
                evaluationStatus = "RVOL_BASELINE_INSUFFICIENT";
                evaluationReason = "A same-time-of-day 5m relative-volume baseline requires at least five prior regular-session observations for this 5m slot. 5M permission remains UNKNOWN.";
            }
            else if (!regularSession)
            {
                evaluationStatus = "OUTSIDE_REGULAR_SESSION";
                evaluationReason = "The V2 intraday permission/execution engine is evaluated only during the US regular session. Historical bars remain available.";
            }
            else
            {
                var (coveragePresent, coveragePossible) = LiveSymbolTechnicalCalculator.OneMinuteCoverage(oneMinute, evaluatedAt);
                // 1M execution window (operator decision 2026-11-25): READY is a 2-bar event from the 1M study's triggers.
                executionWindow = latch is null ? null : LiveSymbolTechnicalCalculator.LatestExecutionStep(oneMinute, fiveMinute, latch);
                var snapshot = new CommandCenterSnapshot(
                    evaluatedAt,
                    normalizedSymbol,
                    one,
                    five,
                    MarketPhase.RegularSession,
                    previousFive,
                    latchLatest?.Permission,
                    coveragePresent,
                    coveragePossible,
                    executionWindow);
                // Opening entry hold (operator setting, 2026-11-21) applies to fresh entries only; the held-position
                // evaluation is identical with or without it because exits are never held.
                var engine = new V2SignalEngine(new V2Config(OpeningEntryHoldMinutes: holdMinutes));
                decision = engine.Evaluate(snapshot, false);
                heldDecision = engine.Evaluate(snapshot, true);
                evaluationStatus = "EVALUATED";
                evaluationReason = "Calculated from completed Alpaca OHLCV bars using the existing V2SignalEngine. This is decision support, not an order instruction.";
            }

            var status = !oneFresh || !fiveFresh
                ? "STALE"
                : one is null || five is null
                    ? "INSUFFICIENT_HISTORY"
                    : "LIVE_OBSERVATIONS";
            var scoringConfig = new V2Config();
            var scoreCriteria = new[]
            {
                new { key = "closeAboveVwap", label = "5M close > VWAP", passed = five is null ? (bool?)null : five.Close > five.Vwap },
                new { key = "closeAboveEma9", label = "5M close > EMA 9", passed = five is null ? (bool?)null : five.Close > five.Ema9 },
                new { key = "ema9AboveEma20", label = "5M EMA 9 > EMA 20", passed = five is null ? (bool?)null : five.Ema9 > five.Ema20 },
                new { key = "ema20AboveSma50", label = "5M EMA 20 > SMA 50", passed = five is null ? (bool?)null : five.Ema20 > five.Sma50 },
                new { key = "rsiSupportive", label = $"5M RSI >= {scoringConfig.MinRsi:0}", passed = five is null ? (bool?)null : five.Rsi >= scoringConfig.MinRsi },
                new { key = "rvolSupportive", label = $"5M RVOL >= {scoringConfig.MinRvol:0.00}", passed = rvol.Value.HasValue ? rvol.Value.Value >= scoringConfig.MinRvol : (bool?)null }
            };
            // The 6-condition score stays as an alignment diagnostic. 5M Permission is the TOS latch (no RVOL input),
            // shown only for a fresh latest 5m bar; it does not depend on 1M freshness/history or the combined decision.
            var fiveMinuteScore = five is not null && fiveFresh && rvol.Value.HasValue
                ? V2SignalEngine.CalculateFiveMinuteScore(five, scoringConfig)
                : (int?)null;
            var fiveMinutePermission = five is not null && fiveFresh && latchLatest is not null
                ? latchLatest.Permission
                : (bool?)null;


            OpportunityQualitySnapshot? opportunitySnapshot = null;
            OpportunityQualityAssessment opportunityAssessment;
            if (decision is null || one is null || oneCloseAt is null || fiveCloseAt is null)
            {
                opportunityAssessment = new OpportunityQualityAssessment(
                    normalizedSymbol,
                    OpportunityClassification.DataUnknown,
                    OpportunityQualityUnavailableReasons.For(evaluationStatus, regularSession),
                    evaluatedAt);
            }
            else
            {
                opportunitySnapshot = OpportunityQualitySnapshotAdapter.Create(
                    normalizedSymbol,
                    evaluatedAt,
                    oneCloseAt.Value,
                    fiveCloseAt.Value,
                    decision.LongPermission,
                    decision.EntryAllowed,
                    rvol.Value,
                    decision.Evaluation?.OneMinuteMomentumPositive == true,
                    one.AtrExtension,
                    one.Atr,
                    oneMinute);
                opportunityAssessment = new OpportunityQualityAssessor().Evaluate(opportunitySnapshot.Input);
            }

            calculationStopwatch.Stop();
            var jsonlStoreDiagnostics = (barStore as JsonlMarketBarObservationStore)?.GetDiagnostics();
            var result = new
            {
                performanceDiagnostics = new
                {
                    providerFetchElapsedMs = Math.Round(providerFetchMs, 1),
                    oneMinuteFetchElapsedMs = Math.Round(oneMinuteFetchStopwatch.Elapsed.TotalMilliseconds, 1),
                    fiveMinuteFetchElapsedMs = Math.Round(fiveMinuteFetchStopwatch.Elapsed.TotalMilliseconds, 1),
                    oneMinuteSource = oneMinuteFromWebSocket ? "WEBSOCKET_STORE" : "ALPACA_REST",
                    fiveMinuteSource = "ALPACA_REST",
                    websocketStoreReadElapsedMs = Math.Round(websocketStoreReadMs, 1),
                    persistenceElapsedMs = Math.Round(persistenceMs, 1),
                    calculationElapsedMs = Math.Round(calculationStopwatch.Elapsed.TotalMilliseconds, 1),
                    totalElapsedMs = Math.Round(requestStopwatch.Elapsed.TotalMilliseconds, 1),
                    jsonlIndexLoaded = jsonlStoreDiagnostics?.IndexLoaded,
                    jsonlIndexLoadCount = jsonlStoreDiagnostics?.IndexLoadCount,
                    jsonlIndexLastLoadElapsedMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadElapsedMs, 1),
                    jsonlIndexUniqueObservationKeys = jsonlStoreDiagnostics?.IndexedUniqueKeyCount,
                    jsonlIndexFileBytes = jsonlStoreDiagnostics?.LastIndexLoadFileBytes,
                    jsonlIndexLinesRead = jsonlStoreDiagnostics?.LastIndexLoadLinesRead,
                    jsonlIndexDeserializeElapsedMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadDeserializeMs, 1),
                    jsonlIndexIndexElapsedMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadIndexMs, 1),
                    jsonlIndexReadElapsedMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadReadMs, 1),
                    jsonlIndexOpenElapsedMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadOpenMs, 1),
                    jsonlIndexGateWaitBeforeLoadMs = jsonlStoreDiagnostics is null ? (double?)null : Math.Round(jsonlStoreDiagnostics.LastIndexLoadGateWaitMs, 1),
                    jsonlIndexLoadTrigger = jsonlStoreDiagnostics?.LastIndexLoadTrigger,
                    jsonlIndexLoadCompletedAtUtc = jsonlStoreDiagnostics?.LastIndexLoadCompletedAtUtc,
                    note = "Server-side stage timings for this uncached assessment. JSONL index-load timing is the most recent one-time process-local rebuild, not necessarily work performed by this request. File bytes and lines are captured during that rebuild. Read (line I/O + UTF-8 decode), deserialize and index timings are measured separately; the remainder of the total is file open and loop overhead. Gate wait is time the rebuilding caller waited for the store lock before starting the rebuild. Load trigger is STARTUP_WARMUP (background build at API start), READ or APPEND (a request or worker built it). Provider fetch duration covers 1m and 5m requests in parallel; it is not a provider-side timing breakdown."
                },
                symbol = normalizedSymbol,
                provider = provider.ProviderName,
                feed = oneLatest?.Feed ?? fiveLatest?.Feed,
                configuredFeed,
                evaluatedAtUtc = evaluatedAt,
                status,
                evaluationStatus,
                evaluationReason,
                openingHold = new
                {
                    rulesVersion = OpeningEntryHold.RulesVersion,
                    minutes = openingHold.Minutes,
                    enabled = openingHold.Enabled,
                    // Active only inside a real regular session (weekdays outside the holiday calendar).
                    active = openingHold.Active && regularSession &&
                             MomentumCommandCenterV2.API.BoringOvernight.UsEquitiesSessionCalendar.IsTradingSession(openingHold.EasternDate),
                    regularOpenUtc = openingHold.RegularOpenUtc,
                    releaseUtc = openingHold.ReleaseUtc,
                    minutesRemaining = openingHold.MinutesRemaining,
                    withheldEntry = decision?.Evaluation?.OpeningHoldWithheld == true,
                    settingSource = intradaySettings.Current.Source,
                    note = "Fresh entries only: inside the hold an entry that meets every condition is shown as PREPARE TO BUY (OPENING HOLD). Exits and held-position states are never delayed. Provisional operator setting; not validated against outcomes."
                },
                executionWindow = new
                {
                    rulesVersion = OneMinuteExecutionEvents.RulesVersion,
                    step = executionWindow,
                    withheldEntry = decision?.Evaluation?.ExecutionWindowWithheld == true,
                    note = "Fresh entries only (operator decision 2026-11-25): EXECUTION READY needs a new 1M trigger from the 1M study (FRESH, IN_TREND, CONTINUATION or CONFIRMATION) within the last 2 bars. Age 0 = trigger bar. Exits are never delayed."
                },
                dataFresh = oneFresh && fiveFresh,
                freshnessDiagnostics = new
                {
                    oneMinute = new { isFresh = oneFresh, ageSeconds = oneAgeSeconds, limitSeconds = 120, sourceTimestampUtc = oneLatest?.Timestamp, expectedCloseUtc = oneCloseAt },
                    fiveMinute = new { isFresh = fiveFresh, ageSeconds = fiveAgeSeconds, limitSeconds = 360, sourceTimestampUtc = fiveLatest?.Timestamp, expectedCloseUtc = fiveCloseAt },
                    staleTimeframes = new[] { !oneFresh ? "1m" : null, !fiveFresh ? "5m" : null }.Where(x => x is not null).ToArray()
                },
                oneMinute = new
                {
                    barCount = oneMinute.Count,
                    coverageLast15 = LiveSymbolTechnicalCalculator.OneMinuteCoverage(oneMinute, evaluatedAt) is var cov
                        ? new { present = cov.Present, possible = cov.Possible, thin = cov.Possible > 0 && cov.Present * 100 < cov.Possible * new V2Config().MinOneMinuteCoveragePercent }
                        : null,
                    sourceTransport = oneMinuteFromWebSocket ? "ALPACA_WEBSOCKET_LATEST_BAR" : (oneLatest?.SourceTransport ?? "UNKNOWN"),
                    sourceTimestampUtc = oneLatest?.Timestamp,
                    completedAtUtc = oneCloseAt,
                    ageSeconds = oneAgeSeconds,
                    freshness = FreshnessLabel(oneFresh, oneAgeSeconds),
                    feed = oneLatest?.Feed,
                    close = oneLatest?.Close,
                    ema9 = one?.Ema9,
                    ema20 = one?.Ema20,
                    vwap = one?.Vwap,
                    rsi = one?.Rsi,
                    macd = one?.Macd,
                    macdSignal = one?.MacdSignal,
                    atr = one?.Atr,
                    extensionAtr = one?.AtrExtension,
                    chartBars = oneMinute.TakeLast(180).Select(ToChartBar).ToArray()
                },
                fiveMinute = new
                {
                    barCount = fiveMinute.Count,
                    sourceTimestampUtc = fiveLatest?.Timestamp,
                    completedAtUtc = fiveCloseAt,
                    ageSeconds = fiveAgeSeconds,
                    freshness = FreshnessLabel(fiveFresh, fiveAgeSeconds),
                    feed = fiveLatest?.Feed,
                    close = fiveLatest?.Close,
                    ema9 = five?.Ema9,
                    ema20 = five?.Ema20,
                    vwap = five?.Vwap,
                    rsi = five?.Rsi,
                    macd = five?.Macd,
                    macdSignal = five?.MacdSignal,
                    atr = five?.Atr,
                    relativeVolume = rvol.Value,
                    relativeVolumeBaselineSamples = rvol.Samples,
                    scoreBreakdown = new
                    {
                        score = fiveMinuteScore,
                        maxScore = 6,
                        permissionThreshold = 5,
                        legacyScorePermission = fiveMinuteScore.HasValue ? fiveMinuteScore.Value >= 5 : (bool?)null,
                        permission = fiveMinutePermission,
                        permissionRule = "TOS latch (installed 5M study); the score is an alignment diagnostic only",
                        status = fiveMinuteScore.HasValue ? "CURRENT" : "UNKNOWN",
                        criteria = scoreCriteria
                    },
                    permissionLatch = latchLatest is null ? null : new
                    {
                        rulesVersion = FiveMinutePermissionLatch.RulesVersion,
                        permission = latchLatest.Permission,
                        transition = latchLatest.Transition,
                        blockCode = latchLatest.BlockCode,
                        blockLabel = FiveMinutePermissionLatch.BlockLabel(latchLatest.BlockCode),
                        raw = latchLatest.Raw,
                        structuralDamage = latchLatest.StructuralDamage,
                        structuralDamagePersistent = latchLatest.StructuralDamagePersistent,
                        majorFailurePersistent = latchLatest.MajorFailurePersistent,
                        eodWindow = latchLatest.EodWindow,
                        grantedSinceUtc = latch!.GrantedSinceUtc,
                        barTimestampUtc = latchLatest.Timestamp,
                        note = "Port of the installed TOS 5M study PERMISSION STATE: set by close > VWAP, close > EMA9, EMA9 >= EMA20, RSI >= 50; held until 2 bars of structural damage (close < VWAP and EMA9 < EMA20), 2 bars of major failure, or the study's EOD window; restarts at NO on each Eastern date (1 D chart). VWAP is MCC's regular-session IEX VWAP, which can differ from TOS."
                    }
                },
                opportunityQuality = new
                {
                    status = opportunityAssessment.Classification == OpportunityClassification.DataUnknown
                        ? "DATA?"
                        : "DIAGNOSTIC_ONLY",
                    classification = opportunityAssessment.Classification.ToString(),
                    displayLabel = opportunityAssessment.DisplayLabel,
                    reasons = opportunityAssessment.Reasons.Select(reason => reason.ToString()).ToArray(),
                    metrics = opportunitySnapshot?.Metrics,
                    evaluatedAtUtc = opportunityAssessment.EvaluatedAt,
                    thresholds = "OpportunityQualityConfig.DiagnosticDefaults (provisional; not calibrated)",
                    executionEventBasis = "Current V2 decision LongPermission / EntryAllowed snapshot; not independently verified Thinkorswim finite READY parity.",
                    note = "Separate diagnostic ranking only. It cannot create or override V2 entry eligibility, execution state, HOLD, EXIT, or order actions. The 30-bar range and prior-high proximity are explicit research proxies, not calibrated predictors."
                },
                decision = LiveDecisionProjection.Project(decision),
                heldPositionDecision = LiveDecisionProjection.Project(heldDecision),
                heldPositionNote = "heldPositionDecision evaluates the same bars as if the operator holds a long position. It is used only when the operator marks the symbol as HOLDING on the Dashboard; MCC has no broker position data.",
                methodology = new
                {
                    indicatorHistory = "EMA9/EMA20, SMA50, RSI14, MACD(12,26,9), ATR14 and session VWAP are calculated from provider OHLCV.",
                    extension = "1m absolute close-to-session-VWAP distance divided by the simple 14-bar average of True Range; intended to match the installed Thinkorswim 1M formula. Session VWAP parity must be confirmed on matched bars.",
                    relativeVolume = "5m current volume divided by average volume for the same regular-session 5m clock slot across prior sessions; minimum five prior samples. This is not a vendor-supplied consolidated RVOL field.",
                    freshness = "Age is measured from the expected bar close (bar timestamp + timeframe), not the bar-open timestamp.",
                    limitations = new[]
                    {
                        oneMinuteFromWebSocket
                            ? "1m execution bars are read from the confirmed Alpaca WebSocket append-only store; 5m permission bars remain historical REST."
                            : "1m/5m decision bars are sourced through historical REST; the optional WebSocket is not currently subscribed or lacks a fresh 50-bar 1m history.",
                        "Provider feed and entitlement determine coverage.",
                        "No catalyst, halt, spread, order-book, or Thinkorswim study data is inferred.",
                        "A true signal remains unavailable when history, freshness, or same-slot volume baseline is insufficient."
                    }
                }
            };

            // Live signal audit log (2026-11-18): one line per state change per symbol. Never blocks the assessment.
            try
            {
                var (logPresent, logPossible) = LiveSymbolTechnicalCalculator.OneMinuteCoverage(oneMinute, evaluatedAt);
                var reasonText = decision?.Reason ?? evaluationReason;
                await signalLog.RecordAsync(new LiveSignalObservation(
                    normalizedSymbol, evaluatedAt, evaluationStatus,
                    decision is null ? null : LiveDecisionProjection.ToStateLabel(decision.State, decision.EntryAllowed).Label,
                    decision?.EntryAllowed,
                    heldDecision is null ? null : LiveDecisionProjection.ToStateLabel(heldDecision.State, heldDecision.EntryAllowed).Label,
                    decision?.Score,
                    latchLatest?.Permission,
                    oneLatest is null ? null : (decimal)oneLatest.Close,
                    logPossible > 0 ? logPresent : null,
                    logPossible > 0 ? logPossible : null,
                    oneLatest?.Timestamp, fiveLatest?.Timestamp,
                    oneLatest?.Feed ?? fiveLatest?.Feed,
                    reasonText is { Length: > 400 } ? reasonText[..400] : reasonText,
                    opportunityAssessment.Classification.ToString()), DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Live signal log write failed for {Symbol}", normalizedSymbol);
            }

            _cache[cacheKey] = (evaluatedAt.Add(CacheLifetime), result);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Live symbol assessment failed for {Symbol} using {Provider}.", normalizedSymbol, provider.ProviderName);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<IReadOnlyList<MarketBar>> MeasureFetchAsync(
        Task<IReadOnlyList<MarketBar>> task, Stopwatch stopwatch)
    {
        try { return await task; }
        finally { stopwatch.Stop(); }
    }

    private static IReadOnlyList<MarketBar> CompletedBars(
        IReadOnlyList<MarketBar> bars, TimeSpan timeframe, DateTimeOffset now) =>
        bars.Where(x => x.Timestamp + timeframe <= now)
            .OrderBy(x => x.Timestamp)
            .GroupBy(x => x.Timestamp)
            .Select(x => x.Last())
            .ToArray();

    private static object ToChartBar(MarketBar bar) => new
    {
        timestamp = bar.Timestamp,
        open = bar.Open,
        high = bar.High,
        low = bar.Low,
        close = bar.Close,
        volume = bar.Volume
    };

    private static string FreshnessLabel(bool fresh, double? ageSeconds) =>
        ageSeconds is null ? "UNKNOWN" : ageSeconds < 0 ? "FUTURE_TIMESTAMP" : fresh ? "FRESH" : "STALE";
}

/// <summary>
/// PURPOSE: Computes the technical inputs consumed by the V2 signal engine from timestamped OHLCV.
/// WHEN CALLED: LiveSymbolAssessmentService builds the latest 1m/5m snapshots.
/// </summary>
public static class LiveSymbolTechnicalCalculator
{
    private static readonly TimeZoneInfo Eastern = ResolveEasternTimeZone();

    /// <summary>
    /// PURPOSE: Builds one fully populated V2 BarSnapshot using only bars through the requested index.
    /// WHEN CALLED: For the current and previous completed 5m bar and the current completed 1m bar.
    /// </summary>
    public static BarSnapshot Calculate(
        IReadOnlyList<MarketBar> bars,
        int index,
        TimeSpan timeframe,
        decimal rvol5m)
    {
        if (index < 49 || index >= bars.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "At least 50 bars through index are required.");

        var closes = bars.Take(index + 1).Select(x => (decimal)x.Close).ToArray();
        var latest = bars[index];
        var ema9 = Ema(closes, 9);
        var ema20 = Ema(closes, 20);
        var sma50 = closes.TakeLast(50).Average();
        var rsi = Rsi(closes, 14);
        var (macd, macdSignal) = Macd(closes);
        var atr = Atr(bars, index, 14);
        var vwap = SessionVwap(bars, index);
        // Match installed Thinkorswim 1M study: AbsValue(close - dailyVWAP) / Average(TrueRange, 14).
        var extension = V2SignalEngine.CalculateAtrExtension((decimal)latest.Close, vwap, atr);

        return new BarSnapshot(
            latest.Timestamp,
            (decimal)latest.Open,
            (decimal)latest.High,
            (decimal)latest.Low,
            (decimal)latest.Close,
            latest.Volume,
            rvol5m,
            vwap,
            ema9,
            ema20,
            sma50,
            rsi,
            macd,
            macdSignal,
            atr,
            extension);
    }

    /// <summary>
    /// PURPOSE: Builds the 5M permission-latch inputs for every bar from index 49 on in one O(n) pass. Each value equals
    /// what <see cref="Calculate"/> returns for that index (same EMA seeding, RSI smoothing, SMA window, session VWAP,
    /// and the same decimal operation order), so the latch sees exactly the indicators MCC displays.
    /// WHEN CALLED: By the live assessment and the historical replay before <see cref="FiveMinutePermissionLatch.Evaluate"/>.
    /// </summary>
    public static IReadOnlyList<PermissionLatchBar> PermissionSeries(IReadOnlyList<MarketBar> bars)
    {
        var result = new List<PermissionLatchBar>(Math.Max(0, bars.Count - 49));
        if (bars.Count < 50) return result;

        var closes = bars.Select(x => (decimal)x.Close).ToArray();
        var ema9 = EmaRunning(closes, 9);
        var ema20 = EmaRunning(closes, 20);
        var rsi = RsiRunning(closes, 14);

        DateOnly? vwapDate = null;
        decimal pv = 0m, volume = 0m;
        for (var i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            var local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern);
            var date = DateOnly.FromDateTime(local.DateTime);
            if (date != vwapDate) { vwapDate = date; pv = 0m; volume = 0m; }
            if (SessionSlot(local, 1) is not null)
            {
                volume += bar.Volume;
                pv += (((decimal)bar.High + (decimal)bar.Low + (decimal)bar.Close) / 3m) * bar.Volume;
            }
            if (i < 49) continue;

            var vwap = volume <= 0m ? closes[i] : pv / volume;
            decimal sum = 0m;
            for (var j = i - 49; j <= i; j++) sum += closes[j];
            result.Add(new PermissionLatchBar(bar.Timestamp, closes[i], vwap, ema9[i], ema20[i], sum / 50m, rsi[i]));
        }
        return result;
    }

    /// <summary>
    /// METHOD: LatestExecutionStep
    /// PURPOSE: The 1M study green-lane step for the latest 1m bar (null when there is not enough history), using MCC's
    /// own 2.25 ATR extension limit until the operator decides the 2.00 vs 2.25 question.
    /// WHEN CALLED: By the live assessment and the historical replay to fill CommandCenterSnapshot.ExecutionWindow.
    /// </summary>
    public static ExecutionEventStep? LatestExecutionStep(
        IReadOnlyList<MarketBar> oneBars,
        IReadOnlyList<MarketBar> fiveBars,
        PermissionLatchResult latch)
    {
        // Extension: MCC's own hard limit (2.25 ATR) until the operator decides the 2.00 vs 2.25 question (decision 3).
        var steps = ExecutionEventSeries(oneBars, fiveBars, latch,
            new ExecutionEventConfig(ExtensionDangerAtr: new V2Config().AtrExtensionHardExit));
        return steps.Count > 0 && steps[^1].Timestamp == oneBars[^1].Timestamp ? steps[^1] : null;
    }

    /// <summary>
    /// METHOD: ExecutionEventSeries
    /// PURPOSE: Runs the 1M study's green execution lane (<see cref="OneMinuteExecutionEvents"/>) over every completed 1m
    /// bar from the 50th on, and returns one step per bar (the last step belongs to the latest 1m bar). 1M inputs use the
    /// same formulas as <see cref="Calculate"/> (EMA9/20, RSI14, MACD 12/26/9, regular-session VWAP, SMA(TrueRange,14)
    /// extension) plus RVOL = volume / SMA(volume, 50) as in the study. The 5M context for a 1m bar comes from the 5m bars
    /// completed by that 1m bar's close and the 5M permission latch over them (operator decisions 2026-11-25).
    /// WHEN CALLED: By the live assessment and the historical replay before building the CommandCenterSnapshot.
    /// </summary>
    public static IReadOnlyList<ExecutionEventStep> ExecutionEventSeries(
        IReadOnlyList<MarketBar> oneBars,
        IReadOnlyList<MarketBar> fiveBars,
        PermissionLatchResult latch,
        ExecutionEventConfig config)
    {
        if (oneBars.Count < 50) return Array.Empty<ExecutionEventStep>();

        var closes = oneBars.Select(x => (decimal)x.Close).ToArray();
        var ema9 = EmaRunning(closes, 9);
        var ema20 = EmaRunning(closes, 20);
        var rsi = RsiRunning(closes, 14);
        var ema12 = EmaSeries(closes, 12);
        var ema26 = EmaSeries(closes, 26);
        var macdLine = closes.Select((_, i) => ema12[i] - ema26[i]).ToArray();
        var macdSignal = EmaRunning(macdLine, 9);

        var fiveCloses = fiveBars.Select(x => (decimal)x.Close).ToArray();
        var f9 = EmaRunning(fiveCloses, 9);
        var f20 = EmaRunning(fiveCloses, 20);
        var fRsi = RsiRunning(fiveCloses, 14);
        var f12 = EmaSeries(fiveCloses, 12);
        var f26 = EmaSeries(fiveCloses, 26);
        var fLine = fiveCloses.Select((_, i) => f12[i] - f26[i]).ToArray();
        var fSignal = EmaRunning(fLine, 9);
        var fSma50 = new decimal[fiveCloses.Length];
        decimal fSum = 0m;
        for (var i = 0; i < fiveCloses.Length; i++)
        {
            fSum += fiveCloses[i];
            if (i >= 50) fSum -= fiveCloses[i - 50];
            fSma50[i] = i >= 49 ? fSum / 50m : 0m;
        }
        var latchByTimestamp = latch.Steps.ToDictionary(s => s.Timestamp, s => s.Permission);

        var eventBars = new List<ExecutionEventBar>(oneBars.Count - 49);
        DateOnly? vwapDate = null;
        decimal pv = 0m, sessionVolume = 0m, volumeSum = 0m, trueRangeSum = 0m;
        var trueRanges = new decimal[oneBars.Count];
        var fiveIndex = -1;
        for (var i = 0; i < oneBars.Count; i++)
        {
            var bar = oneBars[i];
            var local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern);
            var date = DateOnly.FromDateTime(local.DateTime);
            if (date != vwapDate) { vwapDate = date; pv = 0m; sessionVolume = 0m; }
            if (SessionSlot(local, 1) is not null)
            {
                sessionVolume += bar.Volume;
                pv += (((decimal)bar.High + (decimal)bar.Low + (decimal)bar.Close) / 3m) * bar.Volume;
            }
            var previousClose = i > 0 ? closes[i - 1] : closes[i];
            trueRanges[i] = Math.Max((decimal)bar.High - (decimal)bar.Low,
                Math.Max(Math.Abs((decimal)bar.High - previousClose), Math.Abs((decimal)bar.Low - previousClose)));
            trueRangeSum += trueRanges[i];
            if (i >= 14) trueRangeSum -= trueRanges[i - 14];
            volumeSum += bar.Volume;
            if (i >= 50) volumeSum -= oneBars[i - 50].Volume;

            // 5m bars completed by this 1m bar's close.
            var oneCloseAt = bar.Timestamp.AddMinutes(1);
            while (fiveIndex + 1 < fiveBars.Count && fiveBars[fiveIndex + 1].Timestamp.AddMinutes(5) <= oneCloseAt) fiveIndex++;
            if (i < 49) continue;

            var vwap = sessionVolume <= 0m ? closes[i] : pv / sessionVolume;
            var atr = i + 1 >= 14 ? trueRangeSum / 14m : 0m;
            var averageVolume = volumeSum / Math.Min(i + 1, 50);
            var rvol = averageVolume > 0m ? bar.Volume / averageVolume : 0m;
            var aboveVwap = closes[i] > vwap;

            FiveMinuteExecutionContext five;
            if (fiveIndex >= 52)
            {
                var recent = new List<FiveMinuteStudyBar>(4);
                for (var k = fiveIndex; k > fiveIndex - 4; k--)
                    recent.Add(new FiveMinuteStudyBar(fiveBars[k].Timestamp, fiveCloses[k], f9[k], f20[k], fSma50[k], fRsi[k], fLine[k], fSignal[k]));
                var permission = latchByTimestamp.TryGetValue(fiveBars[fiveIndex].Timestamp, out var granted) && granted;
                five = FiveMinuteExecutionContext.Evaluate(recent, permission, aboveVwap, config.RsiStrong, config.RsiWeak);
            }
            else
            {
                five = new FiveMinuteExecutionContext(false, false, false);
            }

            eventBars.Add(new ExecutionEventBar(
                bar.Timestamp, (decimal)bar.Open, (decimal)bar.High, (decimal)bar.Low, closes[i], vwap,
                ema9[i], ema20[i], rsi[i], macdLine[i], macdSignal[i], rvol,
                V2SignalEngine.CalculateAtrExtension(closes[i], vwap, atr), five,
                FiveMinutePermissionLatch.IsEodWindow(local, new PermissionLatchConfig())));
        }
        return OneMinuteExecutionEvents.Evaluate(eventBars, config);
    }

    /// <summary>EMA value at every index, identical to <see cref="Ema"/> applied to closes[0..i]. Called by PermissionSeries.</summary>
    private static decimal[] EmaRunning(IReadOnlyList<decimal> values, int period)
    {
        var result = new decimal[values.Count];
        if (values.Count < period) return result;
        var ema = values.Take(period).Average();
        result[period - 1] = ema;
        var multiplier = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
            result[i] = ema;
        }
        return result;
    }

    /// <summary>RSI value at every index, identical to <see cref="Rsi"/> applied to closes[0..i]. Called by PermissionSeries.</summary>
    private static decimal[] RsiRunning(IReadOnlyList<decimal> closes, int period)
    {
        var result = new decimal[closes.Count];
        for (var i = 0; i < Math.Min(closes.Count, period + 1); i++) result[i] = 50m;
        if (closes.Count <= period) return result;
        decimal averageGain = 0m, averageLoss = 0m;
        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain += Math.Max(0m, change);
            averageLoss += Math.Max(0m, -change);
        }
        averageGain /= period;
        averageLoss /= period;
        result[period] = RsiFrom(averageGain, averageLoss);
        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain = (averageGain * (period - 1) + Math.Max(0m, change)) / period;
            averageLoss = (averageLoss * (period - 1) + Math.Max(0m, -change)) / period;
            result[i] = RsiFrom(averageGain, averageLoss);
        }
        return result;
    }

    private static decimal RsiFrom(decimal averageGain, decimal averageLoss)
    {
        if (averageLoss == 0m) return averageGain == 0m ? 50m : 100m;
        var relativeStrength = averageGain / averageLoss;
        return 100m - 100m / (1m + relativeStrength);
    }

    /// <summary>
    /// PURPOSE: Computes same-clock-slot relative volume from prior regular-session days.
    /// WHEN CALLED: For the current and previous 5m bar before the V2 permission evaluation.
    /// </summary>
    public static (decimal? Value, int Samples) CalculateSameTimeOfDayRelativeVolume(
        IReadOnlyList<MarketBar> bars,
        int index)
    {
        if (index < 0 || index >= bars.Count)
            return (null, 0);

        var current = bars[index];
        var local = TimeZoneInfo.ConvertTime(current.Timestamp, Eastern);
        var slot = SessionSlot(local, 5);
        if (slot is null)
            return (null, 0);

        var currentDate = DateOnly.FromDateTime(local.DateTime);
        var samples = bars.Take(index)
            .Select(bar => new
            {
                Bar = bar,
                Local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern)
            })
            .Where(x => DateOnly.FromDateTime(x.Local.DateTime) < currentDate &&
                        SessionSlot(x.Local, 5) == slot)
            .GroupBy(x => DateOnly.FromDateTime(x.Local.DateTime))
            .OrderByDescending(group => group.Key)
            .Take(10)
            .Select(group => group.OrderByDescending(x => x.Bar.Timestamp).First().Bar.Volume)
            .Where(volume => volume > 0)
            .ToArray();

        if (samples.Length < 5)
            return (null, samples.Length);

        var average = samples.Average(x => (decimal)x);
        return average <= 0m ? (null, samples.Length) : ((decimal)current.Volume / average, samples.Length);
    }

    /// <summary>
    /// PURPOSE: Determines whether the evaluation clock is inside the US regular equity session.
    /// WHEN CALLED: V2 intraday decisions are withheld outside 09:30–16:00 Eastern on weekdays.
    /// </summary>
    /// <summary>
    /// PURPOSE: 1-minute data coverage for the V2 coverage gate (2026-11-18): of the last 15 completed minutes before
    /// asOf (the minute containing asOf is still forming), how many are regular-session minutes (possible) and how many
    /// of those have a bar (present). In the first 15 minutes after 09:30 ET only session minutes count.
    /// WHEN CALLED: By the live assessment and the historical replay before building the V2 snapshot.
    /// </summary>
    public static (int Present, int Possible) OneMinuteCoverage(IReadOnlyList<MarketBar> bars, DateTimeOffset asOf)
    {
        var end = new DateTimeOffset(asOf.UtcDateTime.Ticks - asOf.UtcDateTime.Ticks % TimeSpan.TicksPerMinute, TimeSpan.Zero);
        var start = end.AddMinutes(-15);
        var possible = 0;
        for (var m = start; m < end; m = m.AddMinutes(1)) if (IsRegularSession(m)) possible++;
        var present = bars.Select(b => b.Timestamp).Where(t => t >= start && t < end && IsRegularSession(t)).Distinct().Count();
        return (present, possible);
    }

    public static bool IsRegularSession(DateTimeOffset timestamp)
    {
        var local = TimeZoneInfo.ConvertTime(timestamp, Eastern);
        var time = local.TimeOfDay;
        return local.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
               time >= new TimeSpan(9, 30, 0) && time < new TimeSpan(16, 0, 0);
    }

    private static decimal Ema(IReadOnlyList<decimal> values, int period)
    {
        if (values.Count < period) return 0m;
        var ema = values.Take(period).Average();
        var multiplier = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
            ema = (values[i] - ema) * multiplier + ema;
        return ema;
    }

    private static decimal Rsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period) return 50m;
        decimal averageGain = 0m, averageLoss = 0m;
        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain += Math.Max(0m, change);
            averageLoss += Math.Max(0m, -change);
        }
        averageGain /= period;
        averageLoss /= period;
        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain = (averageGain * (period - 1) + Math.Max(0m, change)) / period;
            averageLoss = (averageLoss * (period - 1) + Math.Max(0m, -change)) / period;
        }
        if (averageLoss == 0m) return averageGain == 0m ? 50m : 100m;
        var relativeStrength = averageGain / averageLoss;
        return 100m - 100m / (1m + relativeStrength);
    }

    private static (decimal Macd, decimal Signal) Macd(IReadOnlyList<decimal> closes)
    {
        if (closes.Count < 35) return (0m, 0m);
        var ema12 = EmaSeries(closes, 12);
        var ema26 = EmaSeries(closes, 26);
        var line = Enumerable.Range(0, closes.Count).Select(i => ema12[i] - ema26[i]).ToArray();
        var signal = Ema(line, 9);
        return (line[^1], signal);
    }

    private static decimal[] EmaSeries(IReadOnlyList<decimal> values, int period)
    {
        var result = new decimal[values.Count];
        if (values.Count == 0) return result;
        var seed = values.Take(Math.Min(period, values.Count)).Average();
        for (var i = 0; i < Math.Min(period, values.Count); i++) result[i] = seed;
        var multiplier = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
            result[i] = (values[i] - result[i - 1]) * multiplier + result[i - 1];
        return result;
    }

    private static decimal Atr(IReadOnlyList<MarketBar> bars, int index, int period)
    {
        if (index + 1 < period) return 0m;

        // The installed Thinkorswim study uses Average(TrueRange(...), 14),
        // a simple moving average rather than Wilder's recursive smoothing.
        var start = index - period + 1;
        decimal total = 0m;
        for (var i = start; i <= index; i++)
        {
            var current = bars[i];
            var previousClose = i > 0 ? (decimal)bars[i - 1].Close : (decimal)current.Close;
            var high = (decimal)current.High;
            var low = (decimal)current.Low;
            total += Math.Max(high - low,
                Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
        }

        return total / period;
    }

    private static decimal SessionVwap(IReadOnlyList<MarketBar> bars, int index)
    {
        var current = bars[index];
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(current.Timestamp, Eastern).DateTime);
        var sessionBars = bars.Take(index + 1)
            .Where(bar =>
            {
                var local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern);
                return DateOnly.FromDateTime(local.DateTime) == localDate && SessionSlot(local, 1) is not null;
            })
            .ToArray();
        var volume = sessionBars.Sum(x => (decimal)x.Volume);
        if (volume <= 0m) return (decimal)current.Close;
        return sessionBars.Sum(x => (((decimal)x.High + (decimal)x.Low + (decimal)x.Close) / 3m) * x.Volume) / volume;
    }

    private static int? SessionSlot(DateTimeOffset easternTimestamp, int intervalMinutes)
    {
        var minutes = (int)easternTimestamp.TimeOfDay.TotalMinutes;
        var start = 9 * 60 + 30;
        var end = 16 * 60;
        return minutes < start || minutes >= end ? null : (minutes - start) / intervalMinutes;
    }

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }
}
