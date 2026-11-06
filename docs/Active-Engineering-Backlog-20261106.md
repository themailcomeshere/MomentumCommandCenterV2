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
| EVR 5m | `NOT_MATCH` | 325 | 20 | 1 | 0.0366528495 | 0.0677729555 |
| CBOE 5m | `GAPS_PRESENT` | 329 | 20 | 1 | 0.0000200950 | 0.0000436336 |
| CBOE 1m | `NO_DATA` | 0 | 0 | 0 | — | — |

All responses report `MCC-WILDER-ATR-PARITY-1.1`, ATR(14) Wilder, tolerance 0.00005, 99 warm-up bars, 20 comparison bars, and study `MCC_OHLCV_ATR_V2.1`. EVR 1m is numerically within tolerance but has 13 qualification-window gaps. EVR 5m has a large numeric mismatch; row-level analysis localizes the first major recurrence divergence to 2026-11-05T14:05Z after a 17h05m gap. CBOE 5m is now numerically within tolerance but still has one qualification-window gap, so it is `GAPS_PRESENT`, not `MATCH`. The study source previously filtered export payloads to 07:00–19:59 ET even though Wilder ATR was calculated on all host-chart bars. That could omit bars which update ATR. The study source now exports every available chart bar in the selected date range; it has not yet been compiled/run in Thinkorswim. See [ATR export coverage diagnosis](Validation/ATR-Export-Coverage-Fix-20261106.md). These comparisons use imported TOS OHLCV and do not establish provider-feed equality.

## P0 — ATR correctness and sell-limit qualification

### P0.1 Diagnose numeric ATR mismatches

**Current state:** Parity endpoint and full-history Wilder seed are implemented. Expanded imports show EVR 5m `NOT_MATCH` and CBOE 5m `GAPS_PRESENT` with numeric error inside tolerance. EVR's first large recurrence divergence occurs exactly at a long source-history gap; the later consecutive rows follow Wilder recurrence within export rounding. The old study's time-of-day export filter is a proven design mismatch with its unfiltered ATR calculation. Revised study source now exports all chart bars in the selected date range; Thinkorswim compilation and fresh-export validation remain outstanding.

**Missing work**
- [x] Parse the expanded EVR/CBOE exports and recompute row-level Wilder recurrence from exact payload OHLCV.
- [x] Localize EVR's large recurrence break: 2026-11-05T14:05Z, immediately after a 17h05m gap; recurrence residual +5.3671071. Subsequent consecutive rows return to rounding-scale residuals.
- [x] Identify the export predicate mismatch: ATR calculation uses every host-chart bar, but the former Strategy Report predicate omitted rows outside 07:00–19:59 ET.
- [x] Update the V2.1 ThinkScript source to export all available chart bars in the selected date range; retain date filtering only.
- Compile the revised study in Thinkorswim and verify its full-history export includes premarket, RTH and after-hours bars.
- Import revised EVR/CBOE exports once, then compare the same 119-bar window and per-bar ATR errors. Preserve 0.00005 tolerance.
- Verify first-bar/seed convention, previous-close alignment, recurrence, timestamp alignment, completed-bar selection, aggregation, session scope and chart adjustment. Do not change the algorithm or tolerance without a proven defect.
- Keep symbol/timeframe/import provenance isolated. Reject mixed or duplicate observations.
- Do not change the 0.00005 precision-derived tolerance to make a failing sample pass.

**Acceptance**
- A documented root cause supported by row-level evidence.
- Regression fixtures using real exported rows, plus independent expected calculations.
- At least 20 consecutive compared bars after the required 99-bar warm-up.
- `MATCH` only when maximum absolute error <= 0.00005 and the qualification window has no unexpected gaps.

**Evidence capture added:** [Export-AtrParityEvidence.ps1](../scripts/validation/Export-AtrParityEvidence.ps1). It saves parity responses and raw persisted TOS observation JSON separately for EVR/CBOE 1m/5m, health/provider status, a manifest and SHA-256 hashes.

### P0.2 Obtain continuous corrected 5m ATR history

**Current state:** Expanded EVR/CBOE imports now contain 325/329 observations. EVR has 3 gaps overall and 1 in the latest 119-bar window; CBOE has 10 gaps overall and 1 in the latest 119-bar window. The revised full-chart export source is committed but not yet compiled or run in Thinkorswim.

**Missing work**
- Compile the revised exact V2.1 ThinkScript in Thinkorswim. It now exports all available chart bars within the selected date range and no longer filters payloads by time of day.
- Set chart and study aggregation to 5m; ATR length 14; Wilder; Phoenix timezone; extended hours ON; corporate-action adjustment ON.
- Set a sufficiently early start date for at least 99 completed source bars before the latest 119-bar qualification window.
- Confirm premarket, regular-session and after-hours bars are present in the Strategy Report. Preserve embedded Unix-millisecond source epochs; do not use Strategy Report Date/Time or Work Time as bar timestamps.
- Determine from the chart/source whether any remaining timestamp gap is a legitimate absent bar or export omission. Never synthesize a bar.

**Acceptance:** corrected export imports with provenance intact; 119-bar window is continuous under the declared source/session contract; parity returns MATCH. A numeric MATCH on an incomplete window is not enough.

### P0.3 EVR 1m continuity

**Current state:** Numeric comparison is within tolerance, but 13 gaps in the latest 119-observation window produce `GAPS_PRESENT`.

**Missing work:** obtain a source-complete 1m export or independently establish which missing timestamps are legitimately absent under the exact TOS chart/session rules. If they cannot be resolved, retain blocked status. 1m parity is not a substitute for the independent 5m sell-limit gate.

### P0.4 Provider/TOS bar parity and reference close

**Current state:** Independent orchestration gates exist. The new `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1` captures provider 5m bars for the exact latest 119 TOS timestamps and reports exact OHLCV differences on the currently configured feed. IEX results are diagnostic only; SIP remains required for qualification. The runner has not yet been executed locally.

**Missing work**
- Capture the same symbol, 5m aggregation, completed-bar timestamps, session scope and adjustment basis from the configured provider and TOS.
- Compare all 119 timestamps and OHLCV fields exactly (or an explicitly approved numeric normalization for price fields); volume must match exactly.
- Verify provider/feed identity on every row. Never mix IEX and SIP.
- Verify the exact previous regular-session 1m close from the same provider/feed.

**Acceptance:** all 119 matched bars and exact reference close pass; otherwise the orchestrator remains BLOCKED.

### P0.5 SIP and 40-session GTC_EXT history

**Current state:** Alpaca is effectively IEX. Prior SIP recent-history access returned HTTP 403 entitlement denial. Do not retry until entitlement/access changes. The qualification runner automatically skips the 40-session SIP capture and sell-limit call unless `/api/providers/status` reports effective feed `sip`.

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

**Missing work:** the new qualification runner will call the sell-limit endpoint only after effective SIP configuration and a 40-session capture attempt. Run it only after 40 complete, eligible, source-qualified windows exist. Preserve 20 training then 20 later unseen test sessions. Verify 100% coverage, positive training target, test target-touch rate >= training empirical rate, and test maximum adverse excursion <= training adverse-excursion P95 bound. The target quantile is selected from training only.

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

1. Pull the latest feature branch and compile the revised V2.1 ThinkScript. The new source exports all available chart bars in the selected date range; the previous time-of-day filter is removed.
2. Export EVR and CBOE 5m with extended hours ON, including premarket, RTH and after-hours bars. Choose a sufficiently early start date for the 99-bar warm-up runway.
3. Import both revised exports once; record appended/identical/correction counts. Re-run parity at the unchanged 0.00005 tolerance.
4. Resolve any remaining timestamp gaps from chart/source evidence. EVR must be numeric MATCH; CBOE must move from GAPS_PRESENT to MATCH. Preserve blocked status if any required gap remains.
5. Once TOS 119-bar continuity and ATR MATCH pass, capture provider 5m bars for those exact timestamps and verify exact OHLCV equality plus the exact prior RTH close.
6. Do not attempt SIP until account entitlement/access changes. Once confirmed, configure SIP, verify the provider-status endpoint, and run one recent SIP SPY smoke request before a 40-session capture.
7. Capture/audit 40 complete SIP GTC_EXT sessions (20 chronological training + 20 unseen test), each with 780 exact minute bars and the exact prior regular-session close.
8. Run the sell-limit endpoint and preserve all gate results. The walk-forward validator is implemented, but only real complete source-qualified history can establish runtime PASS.
9. Complete main Dashboard browser acceptance and regular-session WebSocket/collector acceptance.

## [Uncertainties]

- The root cause of EVR 5m and CBOE 5m numeric ATR mismatch is unknown; row-level evidence is required.
- Missing TOS timestamps may be no-trade intervals or export/study omissions; the supplied CSV alone cannot distinguish them.
- SIP entitlement remains unconfirmed; current effective feed is IEX.
- Runtime walk-forward pass, provider/TOS OHLCV equality, browser acceptance and regular-session WebSocket recovery are not demonstrated.
- The current scanner missing-field count has not been refreshed after the last 296-row audit.
- No actual broker execution ledger or verified broker-fill integration is present.
