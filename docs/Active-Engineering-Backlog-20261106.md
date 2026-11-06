# Active Engineering Backlog — 2026-11-06

**Canonical execution checklist:** [Master-To-Do.md](Master-To-Do.md)  
**Current handoff:** [NewChatHandoff-20261106.md](NewChatHandoff-20261106.md)  
**Branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html`, served at `http://localhost:53360/`  
**Order routing:** NOT IMPLEMENTED. MCC remains decision-support-only.

## Scope and reconciliation method

The former Master To-Do had 310 unchecked boxes across 2,680 lines. Most were repeated operator steps, historical open items later implemented, or tasks superseded by newer architecture. The original file is preserved verbatim in [archive/Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md). Its historical checkbox states are not current status.

This document consolidates the distinct unfinished workstreams and defines what is missing, the implementation boundary, dependencies, and acceptance evidence. It does not convert external dependencies or user-side acceptance into claimed code completion.

### Verified current checkpoint

- Release build and tests: operator-reported PASS.
- Operator-verified local source SHA: `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- API health: `OK`; mode `decision-support-only`; order submission `NOT IMPLEMENTED`.
- Alpaca: configured and validated on IEX; one validation bar returned. This is connectivity evidence, not SIP or full-history qualification.
- Massive: not configured.
- Main Dashboard is the only supported Dashboard entry point; do not recreate/use PrototypeIndex.
- ATR import persistence is separate and append-only. Latest repeated imports were idempotent (0 appended, 0 corrections).

### Latest persisted Thinkorswim ATR parity

| Series | Status | Source observations | Compared bars | Gaps in comparison window | Mean absolute error | Maximum absolute error |
|---|---|---:|---:|---:|---:|---:|
| EVR 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.0000260340 | 0.0000466859 |
| EVR 5m | `NOT_MATCH` | 163 | 20 | 1 | 0.0010467166 | 0.0019357350 |
| CBOE 5m | `NOT_MATCH` | 165 | 20 | 3 | 0.0005274676 | 0.0009650641 |

All responses report `MCC-WILDER-ATR-PARITY-1.1`, ATR(14) Wilder, tolerance 0.00005, 99 warm-up bars, 20 comparison bars, and study `MCC_OHLCV_ATR_V2.1`. The 1m numeric errors are within tolerance but its gaps prevent MATCH. Both 5m series fail numeric tolerance; CBOE also has gaps. These results compare the server ATR to the imported TOS ATR on the imported TOS OHLCV. They do not establish provider-feed equality. The successful 2026-11-06 evidence export and row-level recurrence analysis are documented in [ATR-Evidence-Export-Analysis-20261106.md](Validation/ATR-Evidence-Export-Analysis-20261106.md).

## P0 — ATR correctness and sell-limit qualification

### P0.1 Diagnose numeric ATR mismatches

**Current state:** Parity endpoint and full-history Wilder seed are implemented and locally exercised. EVR 5m and CBOE 5m remain `NOT_MATCH`. Root cause is not established.

**Missing work**
- Export the persisted TOS ATR observations, including exact timestamp, OHLCV, volume, ATR and import provenance.
- Independently recompute True Range and Wilder ATR from the exact same ordered OHLCV rows.
- Compare per-bar calculated ATR versus exported ATR, not just aggregate error.
- Verify first-bar/seed convention, previous-close alignment, recurrence, bar timestamp convention, completed-bar selection, aggregation, session scope, chart adjustment, and whether TOS had more prefetch history than the imported payload. **Priority hypothesis to test, not a conclusion:** both 5m imports contain only 163/165 observations, while the 1m import contains 346; TOS computes Wilder ATR with ThinkScript prefetch on chart history that may extend before the first exported payload row. A different initial ATR state may therefore persist into the comparison window. Re-export substantially more 5m history before the latest 119-bar qualification window and see whether per-bar error decays/converges. Do not change the algorithm or tolerance until this is tested against the exact rows.
- Keep symbol/timeframe/import provenance isolated. Reject mixed or duplicate observations.
- Do not change the 0.00005 precision-derived tolerance to make a failing sample pass.

**Acceptance**
- A documented root cause supported by row-level evidence.
- Regression fixtures using real exported rows, plus independent expected calculations.
- At least 20 consecutive compared bars after the required 99-bar warm-up.
- `MATCH` only when maximum absolute error <= 0.00005 and the qualification window has no unexpected gaps.

**Evidence capture added:** [Export-AtrParityEvidence.ps1](../scripts/validation/Export-AtrParityEvidence.ps1). It saves parity responses and raw persisted TOS observation JSON separately for EVR/CBOE 1m/5m, health/provider status, a manifest and SHA-256 hashes.

### P0.2 Obtain continuous corrected 5m ATR history

**Current state:** EVR 5m export has 82 observations and one 10-minute gap. CBOE's 165-observation two-day export has six gaps overall; the current 119-bar comparison window has three. Count alone does not prove continuity.

**Missing work**
- Compile the exact committed V2.1 ThinkScript in Thinkorswim.
- Set the chart aggregation and study aggregation to 5m; ATR length 14; Wilder; Phoenix chart timezone; extended hours ON; corporate-action adjustment ON.
- Load sufficient chart history for the full ATR prefetch/seed context. Export materially more than the 119-bar qualification window so the server has earlier source history with which to reproduce the TOS Wilder state; preserve at least 119 consecutive completed 5m observations in the latest qualification window. The 119 observations are 99 warm-up + 20 comparison bars, not proof that the initial seed context is identical.
- Confirm embedded Unix-millisecond source epochs, not Strategy Report Date/Time or Work Time.
- Determine from the chart/source whether every timestamp gap is a no-trade interval or an export/study omission. Never synthesize a bar.

**Acceptance:** corrected export imports with provenance intact; 119-bar window is continuous under the declared source/session contract; parity returns MATCH. A numeric MATCH on an incomplete window is not enough.

### P0.3 EVR 1m continuity

**Current state:** Numeric comparison is within tolerance, but 13 gaps in the latest 119-observation window produce `GAPS_PRESENT`.

**Missing work:** obtain a source-complete 1m export or independently establish which missing timestamps are legitimately absent under the exact TOS chart/session rules. If they cannot be resolved, retain blocked status. 1m parity is not a substitute for the independent 5m sell-limit gate.

### P0.4 Provider/TOS bar parity and reference close

**Current state:** Independent orchestration gates exist; current IEX feed and supplied TOS exports do not establish equality.

**Missing work**
- Capture the same symbol, 5m aggregation, completed-bar timestamps, session scope and adjustment basis from the configured provider and TOS.
- Compare all 119 timestamps and OHLCV fields exactly (or an explicitly approved numeric normalization for price fields); volume must match exactly.
- Verify provider/feed identity on every row. Never mix IEX and SIP.
- Verify the exact previous regular-session 1m close from the same provider/feed.

**Acceptance:** all 119 matched bars and exact reference close pass; otherwise the orchestrator remains BLOCKED.

### P0.5 SIP and 40-session GTC_EXT history

**Current state:** Alpaca is effectively IEX. Prior SIP recent-history access returned HTTP 403 entitlement denial. Do not retry until entitlement/access changes.

**Missing work after entitlement changes**
1. User confirms SIP entitlement is active.
2. Configure effective `MarketData:Alpaca:Feed=sip` (inspect secrets and environment overrides), restart API, and verify provider status reports SIP.
3. Test one recent SIP SPY request before any long capture.
4. Capture 40 complete sell sessions: 20 chronological training + 20 later unseen test sessions.
5. Each session requires all 780 exact 1m bar-start timestamps from 07:00 through 19:59 ET and the exact previous regular-session close.
6. Independently read back persisted observations and verify provider/feed/timeframe/timestamps and content.

**Acceptance:** 40/40 complete windows, exact close boundaries, SIP source, no nearest-bar substitution or imputation. IEX remains diagnostic only.

### P0.6 Walk-forward runtime qualification

**Implementation status:** implemented in `BoringOvernightWalkForwardValidator`; the sell-limit orchestrator has a hard `WALK_FORWARD_VALIDATION` gate. Old audit text saying this was not implemented is stale.

**Missing work:** run it only after 40 complete, eligible, source-qualified windows exist. Preserve 20 training then 20 later unseen test sessions. Verify 100% coverage, positive training target, test target-touch rate >= training empirical rate, and test maximum adverse excursion <= training adverse-excursion P95 bound. The target quantile is selected from training only.

**Acceptance:** persist/report complete train/test counts, quantile, target, touch counts/rates, adverse-excursion statistics, exclusions and source IDs. A passing validator does not imply an actual fill or independently authorize an order.

### P0.7 Sell-limit fail-closed release gate

Keep numeric output blocked until P0.1–P0.6 and all existing orchestration gates pass, including fresh completed 5m ATR, exact source provenance, 119 provider/TOS OHLCV matches, exact reference close and current data. The endpoint reads persisted observations only; it must not fetch/impute bars or submit orders.

## P1 — data quality, ranking and strategy correctness

### P1.1 Daily market data and liquidity

**Implementation status:** separate Alpaca Daily client and completed-session 20-day metric calculation exist. Runtime/provider parity and operator acceptance remain outstanding.

**Missing work**
- Smoke-test Daily responses for RTO and EVR; preserve full metadata and any 401/403 errors.
- Compare latest completed Daily close, SMA20 and 20-session average volume with an independent source on the same adjustment basis.
- Confirm exactly 20 expected completed sessions; missing/duplicate/stale/current-partial dates must suppress qualification fields.
- Confirm IEX Daily results remain diagnostic and cannot satisfy SIP qualification.
- Verify Daily values and provenance in evaluator/snapshot/UI.

**Acceptance:** exact 20-session coverage and adjustment parity; incomplete data yields DATA?/UNKNOWN, never READY.

### P1.2 Catalyst classification

**Implementation status:** Alpaca ticker-scoped news retrieval and transparent keyword triage exist; a human-review store exists. This is not a validated classifier and cannot assert CLEAR.

**Missing work**
- Collect a representative, time-stamped article set including no-news and difficult/ambiguous cases.
- Human-adjudicate labels; double-review a subset and measure inter-reviewer agreement.
- Split chronologically into development and holdout sets.
- Measure per-class precision/recall, false negatives/positives, source coverage and time-window coverage.
- Add explicit tests for article pagination, duplicates, stale/no-news, ticker mismatch, primary-source links and unknown categories.
- Keep CLEAR_WITH_SOURCE unavailable until holdout metrics and source coverage meet a documented acceptance policy.

### P1.3 Opportunity Quality full-pool ranking

**Implementation status:** deterministic diagnostic assessor and market-metrics adapter exist. The assessor uses explicitly provisional thresholds; adapter range/resistance values are proxies. The current scanner Automatic mode is a score preview, not a calibrated full-pool ranker. The last recorded scanner audit found 296 rows with unavailable custom-expression values; refresh this count before treating it as current.

**Missing work**
- Inventory every scanner field and its availability by symbol; report missing-field denominators and source timestamps.
- Define a versioned candidate-pool evidence contract. Never rank a symbol as qualified when required custom expressions are unavailable.
- Implement a full-pool ranker only after data completeness is adequate; keep rank preview separate from V2 entry eligibility.
- Define calibrated score bands using labelled outcomes, rank stability and hysteresis; provisional bands cannot become trading gates.
- Integrate exclusions, current price/liquidity, catalyst, authoritative 5m structure and 1m timing as separate gates.
- Add deterministic API/unit/UI fixtures for complete, incomplete, stale and excluded candidate pools.

### P1.4 Boring Overnight Decision column (separate V3 deliverable)

The detailed design remains in [MomentumCommandCenterV3_Boring_Overnight_Decision_Column_TODO.md](project-control/Trading_System/MomentumCommandCenterV3_Boring_Overnight_Decision_Column_TODO.md). It is a planned Thinkorswim watchlist/chart deliverable, not implemented by the MCC Opportunity Quality assessor.

**Missing work:** implement the watchlist column first; keep Overnight Edge and Boringness scores separate; preserve the listed hard gates and score weights as provisional design policy; display READY/WATCH/BLOCKED/DATA? plus reason codes; make C# authoritative for rolling 5D/20D statistics, catalyst, freshness and decision logging. Add the optional chart study only after column calculations validate against known fixtures. Do not duplicate score definitions across ThinkScript and C#.

### P1.5 Overnight boundary and official opening print

**Current state:** old evidence found missing exact 09:30 ET next-session opens for EVR and other boundary pairs. The 09:30 1m bar Open is only an opening-minute proxy, not proof of the official auction print.

**Missing work**
- Preserve the exact expected 09:30 bar start and nearest observed timestamp for every missing boundary.
- Determine whether the cause is no eligible print, IEX coverage, provider omission, or bar convention.
- Decide and document whether the strategy's executable entry reference is the official auction print or first eligible observed trade/bar after 09:30.
- If official auction price is required, source trade-condition/auction-print data from an entitled provider and compare with the official print. Never relabel a 1m bar Open as the official auction price.
- Verify the historical Overnight return endpoint separately from path high/low and executable fills.

### P1.6 Thinkorswim 5m/1m authority and EOD

**Missing work**
- Compile the exact current study in Thinkorswim (not covered by .NET CI).
- Confirm 5m is authoritative and 1m is execution timing only; 1m must not override 5m Permission.
- Export matched 5m/1m bars plus actual state labels for replay. OHLCV-only rows cannot prove Permission/READY/RUNNER/WANING/SWING/GET OUT.
- Verify exact Arizona EOD cutoff with bars immediately before, at and after the configured cutoff, on the correct study and aggregation.
- Keep EOD cutoff acceptance separate from general daytime screenshot review.

**Acceptance:** matched timestamp/state fixtures, compile evidence, exact cutoff transition and no contradictory 1m/5m authority.

### P1.7 WebSocket, collector and recovery

**Implementation status:** stream/collector and runtime validation scripts exist. Current operator evidence does not prove regular-session bar delivery or recovery behavior.

**Missing work**
- During regular market hours capture before/after stream status for SPY, RKLX, ASTX and XRPN over 5–10 minutes.
- Verify per-symbol `barsBySymbol`, `receivedBars`, `persistedBars`, `lastBarUtc` and source timestamps advance.
- Verify 1m switches to WebSocket only after subscription acknowledgement, >=50 completed persisted bars, freshness and provenance. 5m Permission remains REST-backed.
- Exercise restart, sleep/wake, network outage, provider/auth failure, duplicate/replayed events, session boundary and historical backfill.
- Verify collector watermarks advance per symbol and recovery is idempotent.

### P1.8 Dashboard/browser and performance acceptance

**Current surface:** main `frontend/index.html` only. PrototypeIndex must not return.

**Missing operator acceptance**
- Hard-refresh `http://localhost:53360/`; verify no console exceptions and no failed required requests.
- Verify StrategyReport import, all five ATR provenance controls, persisted observation counts, parity output and blocked sell-limit gate explanations.
- Verify Red Zone Manual/Automatic modes, ordered pins, dismissal/restore persistence, imported CSV top-four preview, exclusion list, and DATA? states.
- Verify Morning Brief initial-load deferral, modal/history, replay form and Arizona-local timestamps.
- Measure document, DOMContentLoaded, load, largest request and Fetch & Analyze API durations using browser Network/HAR. Compare to a captured baseline; do not claim improvement without measurements.
- Verify responsive layout and sound permission only where applicable.

### P1.9 Runtime data and audit artifacts

Older runtime artifacts exist in the repository. Analyze only artifacts that are actually present; do not request a second upload if the files are already attached. For each run, record exact SHA, API SHA where available, provider/feed, UTC interval, symbol, source transport, completed/source timestamps, freshness and per-symbol deltas. API health alone does not reveal process SHA.

## P2 — execution, integrations and external dependencies

### P2.1 Actual execution reconciliation

**Implementation status:** not implemented/verified.

**Missing work:** define an import contract for actual broker order/fill activity with broker order ID, client order ID, symbol, side, order type, requested quantity, filled quantity, fill price, fill timestamp, fees, status and correction/cancel lineage. Reconcile partial fills and multiple executions. Keep theoretical OHLCV target touches in a separate table. Never infer fills from bar highs/lows or Strategy Report order events.

### P2.2 Schwab read-only integration

Blocked on Individual Developer/API approval, app registration, callback URL and secure server-side OAuth configuration. If approved, implement read-only positions/orders first with token lifecycle, least privilege, audit and disconnect behavior. No order submission without a separate explicit authorization and design review.

### P2.3 Provider/scanner entitlement, licensing and coverage

Document account plan, feed entitlement, market coverage, scanner universe completeness, rate limits, retention and licensing for each provider. Alpaca movers are bounded top gainers/losers, not full-market breadth. Massive remains unconfigured. Do not select or label a source production-ready until a representative symbol/timeframe/session comparison passes.

### P2.4 Packaging and startup

Before selecting technology, decide target OS, installer/update strategy, API lifecycle, local data directory/backup/restore, browser launch, log rotation, health monitoring and unattended collector policy. Do not claim an executable/background service exists until built and tested.

## P3 — operating discipline and release governance

- Keep [Technical-Workflow.md](Technical-Workflow.md) as the safe pull/build/test/restart workflow.
- Keep [Runtime-Acceptance-Runbook.md](Runtime-Acceptance-Runbook.md) as the specialized runtime evidence procedure.
- Each code change must pass exact-head Release build/tests and CI; ThinkScript compilation and browser acceptance are separate gates.
- Update this backlog and the current handoff after every material implementation/evidence change.
- Keep the feature branch as the work branch. Do not modify `master` without explicit direction.
- Never claim a blocked external/user-side item complete based solely on repository code or CI.

## Immediate execution order

1. Obtain substantially longer EVR and CBOE 5m V2.1 Thinkorswim exports, including a long prefetch/seed runway before the latest 119-bar qualification window. The evidence ZIP is already received; do not run the exporter again or re-import identical CSVs.
2. Inspect each timestamp discontinuity in Thinkorswim/source evidence and classify it only when supported as a legitimate no-trade interval or an export/study omission. Do not impute.
3. Import only genuinely new/corrected rows, record append/identical/correction counts, and rerun parity with the existing 0.00005 tolerance.
4. Compare per-bar server/TOS ATR as earlier history is added; verify Wilder seed, first true-range handling, recurrence, ordering, session boundaries and gap behavior. The seed/prefetch explanation remains a hypothesis until proven.
5. Capture provider 5m OHLCV independently for the exact same 119 timestamps and the exact prior RTH close when an entitled feed is available. The current IEX export does not establish SIP qualification.
6. Resolve EVR 1m's 13 qualification-window gaps or preserve its blocked status.
7. After SIP entitlement is confirmed, run the SPY SIP smoke test, then capture and audit 40 exact GTC_EXT sessions and run the implemented 20-train/20-unseen-test walk-forward validator.
8. Perform main Dashboard browser acceptance and regular-session WebSocket/collector acceptance.
9. Proceed to catalyst/Opportunity Quality calibration, official opening print, actual execution reconciliation and V3 Boring Overnight column as their dependencies and evidence become available.

## [Uncertainties]

- The root cause of EVR 5m and CBOE 5m numeric ATR mismatch is unknown; row-level evidence is required.
- Missing TOS timestamps may be no-trade intervals or export/study omissions; the supplied CSV alone cannot distinguish them.
- SIP entitlement remains unconfirmed; current effective feed is IEX.
- Runtime walk-forward pass, provider/TOS OHLCV equality, browser acceptance and regular-session WebSocket recovery are not demonstrated.
- The current scanner missing-field count has not been refreshed after the last 296-row audit.
- No actual broker execution ledger or verified broker-fill integration is present.
