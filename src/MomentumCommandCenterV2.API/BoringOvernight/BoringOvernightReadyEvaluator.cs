using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: BoringOvernightReadyEvaluator
/// PURPOSE: Pure READY decision for one symbol, per docs/Engineering/Boring-Overnight-READY-Semantics-20261114.md.
/// READY requires, inside the buy window: a fresh qualification snapshot whose hard gates all pass (status READY, not
/// WATCH), no exclusion match, the catalyst gate, and an issued research sell limit. Nothing is imputed; any missing
/// evidence is DATA?. MCC never submits an order; READY is decision support for the operator's own order.
/// WHEN CALLED: By BoringOvernightReadyService for each evaluation, and by unit tests.
/// </summary>
public static class BoringOvernightReadyEvaluator
{
    /// <summary>Maximum age of the capture-history snapshot used for READY.</summary>
    public static readonly TimeSpan MaxSnapshotAge = TimeSpan.FromMinutes(10);

    /// <summary>Maximum age of the latest trade used to apply the tested target % during the buy window.</summary>
    public static readonly TimeSpan MaxLiveTradeAge = TimeSpan.FromMinutes(2);

    /// <summary>Distinct logged buy sessions required before the live signal may be used.</summary>
    public const int MinimumShadowSessions = 10;

    /// <summary>Catalyst placeholder passed to the qualification evaluator so it neither blocks nor clears on news;
    /// the catalyst is decided only by the READY catalyst gate. It is never shown as CLEAR.</summary>
    public const string CatalystDeferred = "DEFERRED_TO_READY_CATALYST_GATE";

    /// <summary>
    /// METHOD: Evaluate
    /// PURPOSE: Applies every READY gate and returns the status, signal label, gate list, warnings and required actions.
    /// Status precedence: BLOCKED, then DATA?, then NOT_READY, then OUTSIDE_BUY_WINDOW, then READY.
    /// WHEN CALLED: Once per symbol per evaluation.
    /// </summary>
    public static ReadyEvaluation Evaluate(ReadyInputs input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var symbol = input.Symbol.Trim().ToUpperInvariant();
        var gates = new List<ReadyGate>();
        var warnings = new List<string>();
        var actions = new List<string>();

        // 1. Buy window.
        var windowOpen = input.Clock.OvernightBuyWindowOpen;
        gates.Add(new("BUY_WINDOW", windowOpen ? GatePass : GateNotYet,
            windowOpen
                ? $"Buy window open until {input.Clock.OvernightBuyDeadlineUtc:O}."
                : $"Buy window for {input.Clock.OvernightBuySessionDate:yyyy-MM-dd} opens {input.Clock.OvernightBuyWindowStartUtc:O} and closes {input.Clock.OvernightBuyDeadlineUtc:O}."));

        // 2-3. Fresh snapshot and qualification.
        var snapshot = input.Snapshot;
        var snapshotFresh = snapshot is not null && snapshot.AsOfUtc <= input.NowUtc && input.NowUtc - snapshot.AsOfUtc <= MaxSnapshotAge;
        if (snapshot is null)
        {
            gates.Add(new("QUALIFICATION_SNAPSHOT", GateData, "No capture-history snapshot exists for this symbol."));
            actions.Add("Run Fetch / retry 20-session close/open history for this symbol.");
        }
        else if (!snapshotFresh)
        {
            gates.Add(new("QUALIFICATION_SNAPSHOT", GateData,
                $"Latest snapshot is from {snapshot.AsOfUtc:O}; READY needs one from the last {MaxSnapshotAge.TotalMinutes:0} minutes."));
            actions.Add("Re-run the 20-session capture (the READY panel does this automatically when you evaluate).");
        }
        else
        {
            gates.Add(new("QUALIFICATION_SNAPSHOT", GatePass, $"Snapshot {snapshot.SnapshotId} captured {snapshot.AsOfUtc:O}."));
        }

        if (snapshot is not null)
        {
            var request = snapshot.EvaluationRequest with { CatalystStatus = CatalystDeferred, Excluded = input.Excluded };
            var qualification = BoringOvernightEvaluator.Evaluate(request);
            var detail = qualification.Status == "READY"
                ? "All qualification hard gates passed (price/SMA20, ADV20, overnight win rate and averages, 5M permission and structure, 1M state, data freshness)."
                : string.Join(" ", qualification.Reasons);
            var state = qualification.Status switch
            {
                "READY" => GatePass,
                "BLOCKED" => GateBlocked,
                "DATA?" => GateData,
                _ => GateFail // WATCH: warnings present
            };
            gates.Add(new("QUALIFICATION", state, $"Qualification status {qualification.Status}. {detail}"));
            warnings.AddRange(qualification.Warnings);
        }

        // 4. Exclusion watchlist (live).
        gates.Add(input.Excluded switch
        {
            true => new("EXCLUSION", GateBlocked, "Symbol is on the exclusion watchlist."),
            false => new("EXCLUSION", GatePass, "Not on the exclusion watchlist."),
            null => new("EXCLUSION", GateData, "Exclusion watchlist could not be read.")
        });

        // 5. Catalyst.
        var catalyst = ReadyCatalystGate.Evaluate(snapshot?.CatalystEvidence, input.HumanReviews, input.Attestation,
            symbol, input.Clock.OvernightBuySessionDate, input.NowUtc);
        gates.Add(new("CATALYST", catalyst.GateState, catalyst.Detail));
        warnings.AddRange(catalyst.Warnings);
        actions.AddRange(catalyst.Actions);

        // 6. Sell limit. The tested target % comes from the SIP history / walk-forward evidence. During the buy window
        // the day's close does not exist yet, so the limit is applied to the latest trade (the price the operator buys at),
        // which must be at most MaxLiveTradeAge old. Nothing is computed from a stale or missing price.
        var sell = input.SellLimit;
        decimal? limit = null, referenceClose = null, targetPct = null;
        if (sell is null)
        {
            gates.Add(new("SELL_LIMIT", GateData, input.SellLimitError ?? "Sell-limit evidence was not evaluated."));
        }
        else
        {
            targetPct = sell.TargetReturnPct;
            var failedRequired = sell.Gates.Where(g => g.Required && !g.Passed).Select(g => g.Code).ToArray();
            var tradeFresh = input.LiveTradePrice is > 0 && input.LiveTradeTimestampUtc is { } tradeAt &&
                tradeAt <= input.NowUtc && input.NowUtc - tradeAt <= MaxLiveTradeAge;
            if (sell.Status == "PROVISIONAL" && targetPct is not null && tradeFresh)
            {
                referenceClose = input.LiveTradePrice;
                limit = decimal.Round(input.LiveTradePrice!.Value * (1m + targetPct.Value / 100m), 2, MidpointRounding.AwayFromZero);
                gates.Add(new("SELL_LIMIT", GatePass,
                    $"Limit {limit:0.00} = latest trade {input.LiveTradePrice:0.00} at {input.LiveTradeTimestampUtc:O} ({input.LiveTradeFeed ?? "feed unknown"}) x (1 + {targetPct:0.####}%). Target is P{sell.TargetQuantilePct} of the 20 training sessions; walk-forward passed. Re-check against your actual fill price."));
            }
            else if (sell.Status == "PROVISIONAL" && targetPct is not null)
            {
                gates.Add(new("SELL_LIMIT", GateData,
                    $"Target +{targetPct:0.####}% is tested, but no trade price from the last {MaxLiveTradeAge.TotalMinutes:0} minutes is available to apply it to."));
            }
            else
            {
                var historyIncomplete = failedRequired.Contains("HISTORY_40_OF_40") || failedRequired.Contains("REFERENCE_CLOSE");
                gates.Add(new("SELL_LIMIT", historyIncomplete ? GateData : GateFail,
                    $"No tested target. Failed required gates: {(failedRequired.Length == 0 ? "SELL_LIMIT_CALCULATION" : string.Join(", ", failedRequired))}."));
            }
        }

        var status = gates.Any(g => g.State == GateBlocked) ? "BLOCKED"
            : gates.Any(g => g.State == GateData) ? "DATA?"
            : gates.Any(g => g.State == GateFail) ? "NOT_READY"
            : !windowOpen ? "OUTSIDE_BUY_WINDOW"
            : "READY";

        var liveAllowed = input.RequestedMode == "live" && input.LoggedShadowSessions >= MinimumShadowSessions;
        var effectiveMode = liveAllowed ? "live" : "shadow";
        var label = status switch
        {
            "READY" => effectiveMode == "live" ? "READY — BUY WINDOW OPEN" : "READY (SHADOW — not yet validated)",
            "OUTSIDE_BUY_WINDOW" => "ALL GATES PASS — OUTSIDE BUY WINDOW",
            "NOT_READY" => "NOT READY",
            "BLOCKED" => "BLOCKED",
            _ => "DATA?"
        };
        var modeNote = input.RequestedMode == "live" && !liveAllowed
            ? $"Live mode requested but only {input.LoggedShadowSessions} of {MinimumShadowSessions} shadow sessions are logged; staying in shadow."
            : effectiveMode == "shadow"
                ? "Shadow mode: the signal is logged with its Day 2 outcome and no sound plays. Switch BoringOvernight:ReadySignalMode to live after review."
                : "Live mode.";

        return new ReadyEvaluation(symbol, status, label, effectiveMode, input.RequestedMode, input.LoggedShadowSessions,
            modeNote, input.NowUtc, input.Clock.OvernightBuySessionDate, input.Clock.OvernightSellSessionDate,
            input.Clock.OvernightBuyWindowStartUtc, input.Clock.OvernightBuyDeadlineUtc,
            limit, referenceClose, targetPct, catalyst.State, gates, warnings.Distinct().ToArray(), actions.Distinct().ToArray(),
            "READY is decision support only. MCC never submits an order; position size is the operator's decision. The limit is a research target from SIP history (a touch is not a fill).");
    }

    /// <summary>Gate states.</summary>
    public const string GatePass = "PASS", GateFail = "FAIL", GateBlocked = "BLOCKED", GateData = "DATA?", GateNotYet = "NOT_YET";
}

/// <summary>
/// TYPE: ReadyCatalystGate
/// PURPOSE: Catalyst rules (operator decisions 2026-11-14):
///  - a human MATERIAL_RISK label, or an unreviewed triage RISK article, blocks;
///  - a human MATERIAL_EVENT blocks when the article is earnings/guidance or FDA/clinical, or when its event type is
///    unknown; other events (M&amp;A, contract, analyst action) pass with a warning showing the headline;
///  - NON_MATERIAL / IRRELEVANT / DUPLICATE are fine; UNCERTAIN or unreviewed MATERIAL/UNCLASSIFIED need review;
///  - after that, a valid operator sign-off for this buy session is required (also when there is zero news).
/// Zero news is UNKNOWN and is never CLEAR; a passed gate is OPERATOR_ATTESTED.
/// WHEN CALLED: By BoringOvernightReadyEvaluator.
/// </summary>
public static class ReadyCatalystGate
{
    private static readonly string[] BlockingEventCategories = { "EARNINGS_OR_GUIDANCE", "FDA_OR_CLINICAL" };

    /// <summary>
    /// METHOD: Evaluate
    /// PURPOSE: Returns the catalyst state, gate state, detail, warnings and required actions.
    /// WHEN CALLED: Once per READY evaluation.
    /// </summary>
    public static ReadyCatalystResult Evaluate(
        BoringOvernightCatalystEvidence? evidence,
        IReadOnlyList<CatalystHumanReviewRecord> reviews,
        CatalystAttestation? attestation,
        string symbol,
        DateOnly buySessionDate,
        DateTimeOffset nowUtc)
    {
        if (evidence is null)
            return new("DATA?", BoringOvernightReadyEvaluator.GateData, "No catalyst evidence in the snapshot (news fetch failed or no snapshot).",
                Array.Empty<string>(), new[] { "Re-run the capture so the news check is included." });
        if (!evidence.CoverageComplete)
            return new("DATA?", BoringOvernightReadyEvaluator.GateData, "News pages were truncated; coverage is incomplete.",
                Array.Empty<string>(), Array.Empty<string>());

        var latest = reviews
            .Where(r => r.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => Key(r.ArticleId, r.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.ReviewedAtUtc).Last().Label.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        var blocked = new List<string>();
        var needsReview = new List<string>();
        var warnings = new List<string>();
        foreach (var article in evidence.Articles)
        {
            latest.TryGetValue(Key(article.Id, article.Url), out var label);
            var headline = article.Headline.Length > 120 ? article.Headline[..120] + "…" : article.Headline;
            switch (label)
            {
                case "MATERIAL_RISK":
                    blocked.Add($"Human-labelled MATERIAL_RISK: \"{headline}\".");
                    break;
                case "MATERIAL_EVENT":
                    var categories = article.Categories ?? Array.Empty<string>();
                    if (categories.Any(c => BlockingEventCategories.Contains(c, StringComparer.OrdinalIgnoreCase)))
                        blocked.Add($"Scheduled overnight event ({string.Join("/", categories)}): \"{headline}\".");
                    else if (categories.Count == 0)
                        blocked.Add($"MATERIAL_EVENT with unknown event type (could be earnings/FDA): \"{headline}\".");
                    else
                        warnings.Add($"EVENT ({string.Join("/", categories)}): \"{headline}\" — allowed with sign-off.");
                    break;
                case "NON_MATERIAL":
                case "IRRELEVANT":
                case "DUPLICATE":
                    break;
                default: // unreviewed or UNCERTAIN
                    if (label is null && string.Equals(article.Severity, "RISK", StringComparison.OrdinalIgnoreCase))
                        blocked.Add($"Unreviewed risk-phrase article: \"{headline}\" (a NON_MATERIAL/IRRELEVANT review lifts this).");
                    else
                        needsReview.Add(headline);
                    break;
            }
        }

        if (blocked.Count > 0)
            return new("BLOCKED", BoringOvernightReadyEvaluator.GateBlocked, string.Join(" ", blocked), warnings,
                Array.Empty<string>());
        if (needsReview.Count > 0)
            return new("NEEDS_REVIEW", BoringOvernightReadyEvaluator.GateFail,
                $"{needsReview.Count} article(s) need a human review label first.", warnings,
                needsReview.Select(h => $"Review and label: \"{h}\".").ToArray());

        var newsSummary = evidence.Articles.Count == 0
            ? "No news returned in the window (UNKNOWN, not CLEAR)."
            : $"{evidence.Articles.Count} article(s) reviewed; none blocking.";
        if (attestation is null || !CatalystAttestationStore.IsValidFor(attestation, symbol, buySessionDate, nowUtc))
            return new("NEEDS_SIGNOFF", BoringOvernightReadyEvaluator.GateFail,
                $"{newsSummary} Operator sign-off for {buySessionDate:yyyy-MM-dd} is required.", warnings,
                new[] { "Sign off the catalyst check (tick the sources you checked)." });

        return new("OPERATOR_ATTESTED", BoringOvernightReadyEvaluator.GatePass,
            $"{newsSummary} CATALYST: OPERATOR-ATTESTED at {attestation.AttestedAtUtc:O} ({string.Join(", ", attestation.SourcesChecked)}); expires {attestation.ExpiresAtUtc:O}.",
            warnings, Array.Empty<string>());
    }

    private static string Key(string? articleId, string? url) =>
        string.IsNullOrWhiteSpace(articleId) ? (url ?? string.Empty).Trim() : articleId.Trim();
}

/// <summary>
/// TYPE: ReadyInputs
/// PURPOSE: Everything the pure READY evaluator needs; collected by BoringOvernightReadyService.
/// WHEN CALLED: Built once per evaluation.
/// </summary>
public sealed record ReadyInputs(
    string Symbol,
    DateTimeOffset NowUtc,
    MarketSessionClockSnapshot Clock,
    BoringOvernightCandidateSnapshot? Snapshot,
    bool? Excluded,
    IReadOnlyList<CatalystHumanReviewRecord> HumanReviews,
    CatalystAttestation? Attestation,
    ReadySellEvidence? SellLimit,
    string? SellLimitError,
    string RequestedMode,
    int LoggedShadowSessions,
    decimal? LiveTradePrice = null,
    DateTimeOffset? LiveTradeTimestampUtc = null,
    string? LiveTradeFeed = null);

/// <summary>
/// TYPE: ReadySellEvidence
/// PURPOSE: The parts of the sell-limit evidence READY needs: status, quantile, tested target % and gates.
/// WHEN CALLED: Mapped from BoringOvernightSellLimitOrchestrationResult by the service (FromOrchestration) or built in tests.
/// </summary>
public sealed record ReadySellEvidence(string Status, int TargetQuantilePct, decimal? TargetReturnPct, IReadOnlyList<SellLimitEvidenceGate> Gates)
{
    /// <summary>Maps the orchestrator result.</summary>
    public static ReadySellEvidence FromOrchestration(BoringOvernightSellLimitOrchestrationResult result) =>
        new(result.Status, result.TargetQuantilePct, result.Calculation?.TargetReturnPct, result.Gates);
}

/// <summary>
/// TYPE: ReadyGate
/// PURPOSE: One READY gate: code, state (PASS, FAIL, BLOCKED, DATA?, NOT_YET) and detail.
/// WHEN CALLED: Produced by the READY evaluator.
/// </summary>
public sealed record ReadyGate(string Code, string State, string Detail);

/// <summary>
/// TYPE: ReadyCatalystResult
/// PURPOSE: Catalyst gate outcome.
/// WHEN CALLED: Produced by ReadyCatalystGate.
/// </summary>
public sealed record ReadyCatalystResult(string State, string GateState, string Detail,
    IReadOnlyList<string> Warnings, IReadOnlyList<string> Actions);

/// <summary>
/// TYPE: ReadyEvaluation
/// PURPOSE: READY result for one symbol, returned to the Dashboard and written to the shadow log.
/// WHEN CALLED: Returned by BoringOvernightReadyEvaluator.Evaluate.
/// </summary>
public sealed record ReadyEvaluation(
    string Symbol,
    string Status,
    string SignalLabel,
    string EffectiveMode,
    string RequestedMode,
    int LoggedShadowSessions,
    string ModeNote,
    DateTimeOffset EvaluatedAtUtc,
    DateOnly BuySessionDate,
    DateOnly SellSessionDate,
    DateTimeOffset BuyWindowStartUtc,
    DateTimeOffset BuyDeadlineUtc,
    decimal? CalculatedLimitSell,
    decimal? ReferencePrice,
    decimal? TargetReturnPct,
    string CatalystState,
    IReadOnlyList<ReadyGate> Gates,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> RequiredActions,
    string Disclaimer);
