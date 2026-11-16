using System.Collections.Concurrent;
using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.API.StrategyReports;
using MomentumCommandCenterV2.API.Watchlists;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: BoringOvernightReadyService
/// PURPOSE: Collects READY inputs (latest capture snapshot, exclusion list, catalyst reviews and sign-off, SIP sell-window
/// evidence, latest trade) and runs BoringOvernightReadyEvaluator. Keeps the SIP sell-window history current with one
/// delayed-SIP capture per symbol per completed session. Logs evaluations made inside the buy window to the shadow log.
/// Settings: BoringOvernight:ReadySignalMode (shadow | live; default shadow) and BoringOvernight:ReadyTargetQuantilePct
/// (default 50). Never submits an order.
/// WHEN CALLED: POST /api/boring-overnight/ready/evaluate and GET /api/boring-overnight/ready/shadow-log.
/// </summary>
public sealed class BoringOvernightReadyService(
    IConfiguration configuration,
    BoringOvernightCandidateSnapshotStore snapshotStore,
    ExclusionWatchlistStore exclusionStore,
    CatalystHumanReviewStore reviewStore,
    CatalystAttestationStore attestationStore,
    IMarketBarObservationStore barStore,
    IThinkorswimAtrObservationStore atrStore,
    AlpacaMarketDataAdapter alpaca,
    ReadyShadowLogStore shadowLog,
    ILogger<BoringOvernightReadyService> logger)
{
    // One delayed-SIP sell-window capture per symbol per latest completed sell session (process-wide).
    private static readonly ConcurrentDictionary<string, DateOnly> CapturedThrough = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Requested signal mode from configuration.</summary>
    public string RequestedMode =>
        string.Equals(configuration["BoringOvernight:ReadySignalMode"], "live", StringComparison.OrdinalIgnoreCase) ? "live" : "shadow";

    private int TargetQuantilePct =>
        int.TryParse(configuration["BoringOvernight:ReadyTargetQuantilePct"], out var q) && q is >= 1 and <= 99 ? q : 50;

    /// <summary>
    /// Untested sell-limit heuristic in percent (operator decision 2026-11-16; default +0.5%). Used only when no tested
    /// target exists; the card labels it LIMIT UNTESTED. Setting: BoringOvernight:UntestedLimitPct (0.05-5).
    /// </summary>
    private decimal UntestedLimitPct =>
        decimal.TryParse(configuration["BoringOvernight:UntestedLimitPct"], System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var pct) && pct is >= 0.05m and <= 5m ? pct : 0.5m;

    /// <summary>
    /// METHOD: EvaluateAsync
    /// PURPOSE: Builds the READY evaluation for one symbol at nowUtc and logs it when the buy window is open.
    /// WHEN CALLED: Each READY evaluation request.
    /// </summary>
    public async Task<ReadyEvaluation> EvaluateAsync(string symbol, DateTimeOffset nowUtc, CancellationToken ct)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var clock = MarketSessionClock.GetSnapshot(nowUtc);

        var snapshot = (await snapshotStore.GetLatestAsync(symbol, 1, ct)).FirstOrDefault();

        bool? excluded;
        try { excluded = (await exclusionStore.GetAsync(ct)).Any(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        { excluded = null; logger.LogWarning(ex, "Exclusion list unreadable during READY for {Symbol}", symbol); }

        var reviews = await reviewStore.GetLatestAsync(symbol, 500, ct);
        var attestation = await attestationStore.GetValidAsync(symbol, clock.OvernightBuySessionDate, nowUtc, ct);

        ReadySellEvidence? sellLimit = null;
        string? sellLimitError = null;
        try
        {
            await EnsureSellWindowHistoryAsync(symbol, nowUtc, ct);
            sellLimit = ReadySellEvidence.FromOrchestration(await new BoringOvernightSellLimitOrchestrator(barStore, atrStore).CalculateAsync(
                new BoringOvernightSellLimitRequest(symbol, "Alpaca", "sip", TargetQuantilePct), nowUtc, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException or TaskCanceledException)
        {
            sellLimitError = $"{ex.GetType().Name}: {ex.Message}";
        }

        LatestTradeSnapshot? trade = null;
        try { trade = await alpaca.GetLatestTradeAsync(symbol, ct); }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or ProviderNotConfiguredException)
        { logger.LogWarning(ex, "Latest trade unavailable during READY for {Symbol}", symbol); }

        // Late window: the regular-session close reference is the last regular-session 1m bar on the configured feed,
        // the same feed as the latest trade, so the after-hours comparison is like for like.
        decimal? regularClose = null;
        string? regularCloseSource = null;
        if (clock.OvernightLateBuyWindowOpen && !clock.OvernightBuyWindowOpen)
        {
            try
            {
                var closeUtc = clock.OvernightBuyDeadlineUtc;
                var bars = await alpaca.GetBarsAsync(symbol, TimeSpan.FromMinutes(1), closeUtc.AddMinutes(-15), closeUtc, ct);
                var lastRegular = bars.Where(b => b.Timestamp < closeUtc).OrderBy(b => b.Timestamp).LastOrDefault();
                if (lastRegular is not null && lastRegular.Close > 0)
                {
                    regularClose = (decimal)lastRegular.Close;
                    regularCloseSource = $"Alpaca {lastRegular.Feed ?? alpaca.ConfiguredFeed} last regular-session 1m bar {lastRegular.Timestamp:O}";
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException or TaskCanceledException or ProviderNotConfiguredException)
            { logger.LogWarning(ex, "Regular-close reference unavailable during late READY for {Symbol}", symbol); }
        }

        var loggedSessions = ReadyShadowSummary.CountLoggedSessions(await shadowLog.GetAllAsync(ct));
        var evaluation = BoringOvernightReadyEvaluator.Evaluate(new ReadyInputs(
            symbol, nowUtc, clock, snapshot, excluded, reviews, attestation, sellLimit, sellLimitError,
            RequestedMode, loggedSessions, trade?.Price, trade?.TimestampUtc, trade?.Feed,
            regularClose, regularCloseSource, UntestedLimitPct));

        if (clock.OvernightBuyWindowOpen || clock.OvernightLateBuyWindowOpen)
            await shadowLog.AppendAsync(evaluation, ct);
        return evaluation;
    }

    /// <summary>
    /// METHOD: GetShadowSummaryAsync
    /// PURPOSE: Returns the shadow-log summary with Day 2 outcomes. For signalled sessions whose Day 2 window has ended,
    /// it refreshes the SIP sell-window history once (delayed SIP) so the outcome can be resolved.
    /// WHEN CALLED: GET /api/boring-overnight/ready/shadow-log.
    /// </summary>
    public async Task<ReadyShadowReport> GetShadowSummaryAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        var entries = await shadowLog.GetAllAsync(ct);
        var signalledSymbols = entries.Where(e => e.Status == "READY").Select(e => e.Symbol.ToUpperInvariant()).Distinct().ToArray();
        var windows = new Dictionary<(string, DateOnly), BoringOvernightSellWindowAudit>();
        var errors = new List<string>();
        foreach (var symbol in signalledSymbols)
        {
            try
            {
                await EnsureSellWindowHistoryAsync(symbol, nowUtc, ct);
                var history = await new BoringOvernightSellWindowHistoryService(barStore)
                    .GetHistoryAsync(symbol, "Alpaca", "sip", nowUtc - AlpacaDailyMarketDataClient.SipDelay, ct);
                foreach (var window in history.Windows) windows[(symbol, window.SellSessionDate)] = window;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException or TaskCanceledException)
            {
                errors.Add($"{symbol}: {ex.Message}");
            }
        }

        var rows = ReadyShadowSummary.Build(entries, windows, nowUtc);
        var resolved = rows.Where(r => r.Outcome is "TOUCHED" or "NOT_TOUCHED").ToArray();
        return new ReadyShadowReport(RequestedMode, ReadyShadowSummary.CountLoggedSessions(entries),
            BoringOvernightReadyEvaluator.MinimumShadowSessions, rows.Count(r => r.Signalled), resolved.Length,
            resolved.Count(r => r.Outcome == "TOUCHED"), rows, errors,
            "Outcome compares the Day 2 07:00-20:00 ET SIP high with the limit at the first READY of that session. A touch is not a fill. PENDING = window not ended (+16 min) or not yet captured.");
    }

    private async Task EnsureSellWindowHistoryAsync(string symbol, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var latestCompleted = BoringOvernightSellWindowHistoryService.GetLatestCompletedSellSessionDate(nowUtc - AlpacaDailyMarketDataClient.SipDelay);
        if (CapturedThrough.TryGetValue(symbol, out var through) && through >= latestCompleted) return;
        if (!alpaca.DelayedHistoricalSipMinutesAllowed && !string.Equals(alpaca.ConfiguredFeed, "sip", StringComparison.OrdinalIgnoreCase))
            return; // SIP history cannot be fetched; the sell-limit gates will report it.
        var existing = await new BoringOvernightSellWindowHistoryService(barStore)
            .GetHistoryAsync(symbol, "Alpaca", "sip", nowUtc - AlpacaDailyMarketDataClient.SipDelay, ct);
        if (!existing.Windows.Any(w => w.SellSessionDate == latestCompleted && w.ObservedIntervalCount > 0))
            await new BoringOvernightSellWindowHistoryService(barStore).CaptureHistoryAsync(alpaca, symbol, "sip", nowUtc, ct);
        CapturedThrough[symbol] = latestCompleted;
    }
}

/// <summary>
/// TYPE: ReadyEvaluateRequest
/// PURPOSE: Request body for POST /api/boring-overnight/ready/evaluate.
/// WHEN CALLED: Bound by the endpoint.
/// </summary>
public sealed record ReadyEvaluateRequest(string Symbol);

/// <summary>
/// TYPE: ReadyShadowReport
/// PURPOSE: Shadow-log summary returned to the Dashboard.
/// WHEN CALLED: Returned by GetShadowSummaryAsync.
/// </summary>
public sealed record ReadyShadowReport(
    string RequestedMode,
    int LoggedSessions,
    int MinimumSessionsForLive,
    int SignalledSessions,
    int ResolvedSignals,
    int TouchedSignals,
    IReadOnlyList<ReadyShadowRow> Rows,
    IReadOnlyList<string> Errors,
    string Note);
