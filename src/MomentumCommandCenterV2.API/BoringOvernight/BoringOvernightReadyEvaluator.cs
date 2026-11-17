using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.API.Watchlists;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: BoringOvernightReadyEvaluator
/// PURPOSE: Pure READY decision for one symbol, per docs/Engineering/Boring-Overnight-READY-Semantics-20261114.md.
/// READY requires, inside the final-hour or late buy window: a fresh qualification snapshot whose hard gates all pass
/// (status READY, not WATCH), no exclusion match, the catalyst gate, a fresh trade price for the sell limit and, in the
/// late window, an after-hours price at or below the regular close. Operator decisions 2026-11-16: 5M momentum is
/// informational; an untested sell limit uses the operator's heuristic and is labelled LIMIT UNTESTED; a late window runs
/// until close + 60 minutes. Nothing is imputed; any missing evidence is DATA?. MCC never submits an order.
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
        var qualificationNotes = new List<string>();

        // 1. Buy window: final regular hour, or the late after-hours window (close to close + 60 min).
        var finalHourOpen = input.Clock.OvernightBuyWindowOpen;
        var lateOpen = input.Clock.OvernightLateBuyWindowOpen && !finalHourOpen;
        var windowOpen = finalHourOpen || lateOpen;
        var entryWindow = finalHourOpen ? "FINAL_HOUR" : lateOpen ? "LATE" : "CLOSED";
        gates.Add(new("BUY_WINDOW", windowOpen ? GatePass : GateNotYet,
            finalHourOpen
                ? $"Final-hour buy window open until the regular close {input.Clock.OvernightBuyDeadlineUtc:O}; a late window follows until {input.Clock.OvernightLateBuyDeadlineUtc:O}."
                : lateOpen
                    ? $"LATE buy window (after-hours) open until {input.Clock.OvernightLateBuyDeadlineUtc:O}. Use a LMT EXT order. The historical edge is measured from the regular close."
                    : $"Buy window for {input.Clock.OvernightBuySessionDate:yyyy-MM-dd} opens {input.Clock.OvernightBuyWindowStartUtc:O}; final hour until {input.Clock.OvernightBuyDeadlineUtc:O}, late window until {input.Clock.OvernightLateBuyDeadlineUtc:O}."));

        var tradeFresh = input.LiveTradePrice is > 0 && input.LiveTradeTimestampUtc is { } tradeAt &&
            tradeAt <= input.NowUtc && input.NowUtc - tradeAt <= MaxLiveTradeAge;
        // Late window: the free IEX feed carries no after-hours trades (checked 2026-11-16: zero 16:00-17:00 ET bars
        // even for SPY). The operator may enter the after-hours price read in Thinkorswim/Schwab; it is used only when
        // no fresh feed trade exists and is labelled OPERATOR-ENTERED.
        var useOperatorPrice = lateOpen && !tradeFresh && input.OperatorPrice is > 0;
        decimal? entryPrice = tradeFresh ? input.LiveTradePrice : useOperatorPrice ? input.OperatorPrice : null;
        var entrySource = tradeFresh
            ? $"latest trade {input.LiveTradePrice:0.00} at {input.LiveTradeTimestampUtc:O} ({input.LiveTradeFeed ?? "feed unknown"})"
            : useOperatorPrice ? $"OPERATOR-ENTERED after-hours price {input.OperatorPrice:0.00} (from Thinkorswim/Schwab, {input.NowUtc:O})" : null;
        if (useOperatorPrice)
            warnings.Add($"Price {input.OperatorPrice:0.00} was typed in by the operator (no after-hours trade on the free feed). Re-check it before ordering.");

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
            var qualificationMode = lateOpen ? BoringOvernightEvaluationMode.ReadyLate : BoringOvernightEvaluationMode.ReadyFinalHour;
            var qualification = BoringOvernightEvaluator.Evaluate(request, qualificationMode);
            qualificationNotes.AddRange(qualification.Notes ?? Array.Empty<string>());
            var detail = qualification.Status == "READY"
                ? "All qualification hard gates passed (price >= $5 and above SMA20, ADV20, overnight win rate and averages, data freshness, 1M not SELL). 5M momentum is informational."
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

        // 4a. Pending binary catalyst (operator list, 2026-11-17): an overnight hold through a binary readout is not boring.
        if (input.PendingCatalystListUnreadable)
            gates.Add(new("PENDING_BINARY_CATALYST", GateData, "The pending-catalyst list could not be read."));
        else if (input.PendingCatalyst is { } pending)
        {
            gates.Add(new("PENDING_BINARY_CATALYST", GateBlocked,
                $"Pending binary catalyst: {pending.Event} (expected {pending.ExpectedWindow}). An overnight hold would be exposed to it."));
            actions.Add("Remove the symbol from Catalysts (left dock) only after the readout is public.");
        }
        else
            gates.Add(new("PENDING_BINARY_CATALYST", GatePass, "Not on the pending binary-catalyst list."));

        // 4b. Late window: the after-hours price must not be above the regular close (the edge is measured from the close).
        decimal? lateMovePct = null;
        if (lateOpen)
        {
            if (input.RegularCloseReference is not > 0)
                gates.Add(new("LATE_ENTRY_PRICE", GateData, "The regular-session close reference is unavailable, so the after-hours price cannot be checked."));
            else if (entryPrice is null)
                gates.Add(new("LATE_ENTRY_PRICE", GateData,
                    $"No after-hours price: the free IEX feed has no after-hours trades. Type the after-hours price from Thinkorswim/Schwab into the card to compare it with the regular close {input.RegularCloseReference:0.00}."));
            else
            {
                lateMovePct = decimal.Round((entryPrice.Value / input.RegularCloseReference.Value - 1m) * 100m, 2);
                gates.Add(entryPrice.Value <= input.RegularCloseReference.Value
                    ? new("LATE_ENTRY_PRICE", GatePass,
                        $"After-hours price ({entrySource}) is at or below the regular close {input.RegularCloseReference:0.00} ({lateMovePct:+0.00;-0.00;0.00}%; {input.RegularCloseReferenceSource ?? "source unknown"}).")
                    : new("LATE_ENTRY_PRICE", GateFail,
                        $"After-hours already up {lateMovePct:+0.00}% vs the regular close {input.RegularCloseReference:0.00} ({entrySource}): part of the overnight edge is used up. WATCH, do not chase."));
            }
        }

        // 5. Catalyst.
        var catalyst = ReadyCatalystGate.Evaluate(snapshot?.CatalystEvidence, input.HumanReviews, input.Attestation,
            symbol, input.Clock.OvernightBuySessionDate, input.NowUtc);
        gates.Add(new("CATALYST", catalyst.GateState, catalyst.Detail));
        warnings.AddRange(catalyst.Warnings);
        actions.AddRange(catalyst.Actions);

        // 6. Sell limit. A tested target % comes from the SIP history / walk-forward evidence. Without one (operator decision
        // 2026-11-16) the operator's heuristic +UntestedLimitPct is used and labelled LIMIT UNTESTED; it no longer blocks
        // READY. Either way the limit is applied to the latest trade (at most MaxLiveTradeAge old); nothing is computed
        // from a stale or missing price.
        var sell = input.SellLimit;
        decimal? limit = null, referenceClose = null, targetPct = null;
        string? limitBasis = null;
        var tested = sell is { Status: "PROVISIONAL", TargetReturnPct: not null };
        var untestedReason = sell is null
            ? input.SellLimitError ?? "sell-limit evidence was not evaluated"
            : "failed required gates: " + (sell.Gates.Where(g => g.Required && !g.Passed).Select(g => g.Code).DefaultIfEmpty("SELL_LIMIT_CALCULATION").Aggregate((a, b) => a + ", " + b));
        if (tested && entryPrice is not null)
        {
            targetPct = sell!.TargetReturnPct;
            referenceClose = entryPrice;
            limit = decimal.Round(entryPrice.Value * (1m + targetPct!.Value / 100m), 2, MidpointRounding.AwayFromZero);
            limitBasis = $"TESTED_P{sell.TargetQuantilePct}";
            gates.Add(new("SELL_LIMIT", GatePass,
                $"Limit {limit:0.00} = {entrySource} x (1 + {targetPct:0.####}%). Target is P{sell.TargetQuantilePct} of the 20 training sessions; walk-forward passed. Re-check against your actual fill price."));
        }
        else if (entryPrice is not null && input.UntestedLimitPct > 0m)
        {
            targetPct = input.UntestedLimitPct;
            referenceClose = entryPrice;
            limit = decimal.Round(entryPrice.Value * (1m + targetPct.Value / 100m), 2, MidpointRounding.AwayFromZero);
            limitBasis = "UNTESTED_HEURISTIC";
            gates.Add(new("SELL_LIMIT", GatePass,
                $"LIMIT UNTESTED: {limit:0.00} = {entrySource} x (1 + {targetPct:0.####}%), your heuristic. No tested target ({untestedReason})."));
            warnings.Add($"Sell limit is UNTESTED: +{targetPct:0.####}% heuristic, not a tested target ({untestedReason}).");
        }
        else
        {
            gates.Add(new("SELL_LIMIT", GateData, tested
                ? $"Target +{sell!.TargetReturnPct:0.####}% is tested, but no trade price from the last {MaxLiveTradeAge.TotalMinutes:0} minutes is available to apply it to."
                : lateOpen
                    ? $"No after-hours price to apply the +{input.UntestedLimitPct:0.####}% heuristic to: type the price from Thinkorswim/Schwab into the card. No tested target ({untestedReason})."
                    : $"No trade price from the last {MaxLiveTradeAge.TotalMinutes:0} minutes to apply the +{input.UntestedLimitPct:0.####}% heuristic to; no tested target ({untestedReason})."));
        }

        var status = gates.Any(g => g.State == GateBlocked) ? "BLOCKED"
            : gates.Any(g => g.State == GateData) ? "DATA?"
            : gates.Any(g => g.State == GateFail) ? "NOT_READY"
            : !windowOpen ? "OUTSIDE_BUY_WINDOW"
            : "READY";

        var liveAllowed = input.RequestedMode == "live" && input.LoggedShadowSessions >= MinimumShadowSessions;
        var effectiveMode = liveAllowed ? "live" : "shadow";
        var onlyLateMoveFails = status == "NOT_READY" &&
            gates.Where(g => g.State == GateFail).All(g => g.Code == "LATE_ENTRY_PRICE");
        var label = status switch
        {
            "READY" => effectiveMode == "live" ? "READY — BUY WINDOW OPEN" : "READY (SHADOW — not yet validated)",
            "OUTSIDE_BUY_WINDOW" => "ALL GATES PASS — OUTSIDE BUY WINDOW",
            "NOT_READY" when onlyLateMoveFails => $"WATCH — AFTER-HOURS UP {lateMovePct:0.00}%",
            "NOT_READY" => "NOT READY",
            "BLOCKED" => "BLOCKED",
            _ => "DATA?"
        };
        if (status == "READY" && lateOpen) label += " · LATE WINDOW (LMT EXT)";
        if (status is "READY" or "OUTSIDE_BUY_WINDOW" && limitBasis == "UNTESTED_HEURISTIC") label += " · LIMIT UNTESTED";
        var modeNote = input.RequestedMode == "live" && !liveAllowed
            ? $"Live mode requested but only {input.LoggedShadowSessions} of {MinimumShadowSessions} shadow sessions are logged; staying in shadow."
            : effectiveMode == "shadow"
                ? "Shadow mode: the signal is logged with its Day 2 outcome and no sound plays. Switch BoringOvernight:ReadySignalMode to live after review."
                : "Live mode.";

        return new ReadyEvaluation(symbol, status, label, effectiveMode, input.RequestedMode, input.LoggedShadowSessions,
            modeNote, input.NowUtc, input.Clock.OvernightBuySessionDate, input.Clock.OvernightSellSessionDate,
            input.Clock.OvernightBuyWindowStartUtc, input.Clock.OvernightBuyDeadlineUtc,
            limit, referenceClose, targetPct, catalyst.State, gates, warnings.Distinct().ToArray(), actions.Distinct().ToArray(),
            "READY is decision support only. MCC never submits an order; position size is the operator's decision. A tested limit is a research target from SIP history; an UNTESTED limit is the operator's heuristic. A touch is not a fill.",
            entryWindow, input.Clock.OvernightLateBuyDeadlineUtc, limitBasis, qualificationNotes.Distinct().ToArray(),
            catalyst.ReviewArticles ?? Array.Empty<ReadyReviewArticle>());
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
        var reviewArticles = new List<ReadyReviewArticle>();
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
                    // Every unreviewed or UNCERTAIN article can be labelled directly on the READY card.
                    if (label is null or "UNCERTAIN")
                        reviewArticles.Add(new ReadyReviewArticle(article.Id, article.Headline, article.Url, article.PublishedAtUtc,
                            article.Severity, article.Categories ?? Array.Empty<string>(), evidence.Provider, evidence.RuleSetVersion, label));
                    break;
            }
        }

        if (blocked.Count > 0)
            return new("BLOCKED", BoringOvernightReadyEvaluator.GateBlocked, string.Join(" ", blocked), warnings,
                Array.Empty<string>(), reviewArticles);
        if (needsReview.Count > 0)
            return new("NEEDS_REVIEW", BoringOvernightReadyEvaluator.GateFail,
                $"{needsReview.Count} article(s) need a human review label first.", warnings,
                needsReview.Select(h => $"Review and label: \"{h}\" (label it on this card).").ToArray(), reviewArticles);

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
    string? LiveTradeFeed = null,
    decimal? RegularCloseReference = null,
    string? RegularCloseReferenceSource = null,
    decimal UntestedLimitPct = 0.5m,
    decimal? OperatorPrice = null,
    PendingCatalystEntry? PendingCatalyst = null,
    bool PendingCatalystListUnreadable = false);

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
    IReadOnlyList<string> Warnings, IReadOnlyList<string> Actions, IReadOnlyList<ReadyReviewArticle>? ReviewArticles = null);

/// <summary>
/// TYPE: ReadyReviewArticle
/// PURPOSE: An article on a READY card that still needs a human label (unreviewed or UNCERTAIN), with what the
/// Dashboard needs to post the review to /api/market-data/catalyst-reviews.
/// WHEN CALLED: Produced by ReadyCatalystGate; rendered by the READY card (added 2026-11-16).
/// </summary>
public sealed record ReadyReviewArticle(string? Id, string Headline, string? Url, DateTimeOffset? PublishedAtUtc,
    string Severity, IReadOnlyList<string> Categories, string Source, string RuleSetVersion, string? CurrentLabel);

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
    string Disclaimer,
    string EntryWindow = "CLOSED",
    DateTimeOffset LateBuyDeadlineUtc = default,
    string? LimitBasis = null,
    IReadOnlyList<string>? QualificationNotes = null,
    IReadOnlyList<ReadyReviewArticle>? ArticlesToReview = null);
