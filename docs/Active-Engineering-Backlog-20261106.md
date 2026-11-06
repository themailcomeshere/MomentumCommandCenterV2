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

- Release build and tests: operator-reported PASS at local source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- GitHub Actions `build-and-test`: PASS at commit `d00dbd61ede861f6a9924165377a17e9d8001022`. The qualification runner was subsequently hardened in commit `fc13d6b3199cf9fbbc8a9a4059b1fe6567fb1327`; that PowerShell edit has not yet been executed locally.
- API health in the 2026-11-06 10:53 Arizona capture: `OK`; mode `decision-support-only`; order submission `NOT IMPLEMENTED`.
- Alpaca: effective feed `iex`; provider validated. The capture returned IEX bars only; no SIP request was made. This is diagnostic connectivity, not qualification.
- Massive: not configured.
- Main Dashboard is the only supported Dashboard entry point; do not recreate/use PrototypeIndex.
- ATR import persistence is separate and append-only. Earlier repeated imports were idempotent; the latest 2026-11-06 broad exports appended 2 EVR and 9 CBOE observations with zero corrections.

### Latest persisted Thinkorswim ATR parity

| Series | Status | Source observations | Compared bars | Gaps in comparison window | Mean absolute error | Maximum absolute error |
|---|---|---:|---:|---:|---:|---:|
| EVR 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.0000260340 | 0.0000466859 |
| EVR 5m | `GAPS_PRESENT` | 327 | 20 | 3 | 0.0000294330 | 0.0000495349 |
| CBOE 5m | `GAPS_PRESENT` | 338 | 20 | 1 | 0.0000203866 | 0.0000426076 |
| CBOE 1m | `NO_DATA` | 0 | 0 | 0 | — | — |

The latest 5m parity responses report `MCC-WILDER-ATR-PARITY-1.1`, ATR(14) Wilder, tolerance 0.00005, 99 warm-up bars and 20 compared bars. EVR and CBOE numeric errors are within tolerance, but 3 EVR and 1 CBOE timestamp gaps in the latest 119-observation window keep both at `GAPS_PRESENT`. The latest imports appended 2 EVR and 9 CBOE observations, with no corrections. The revised ThinkScript removes the old time-of-day export filter but remains uncompiled/unverified in Thinkorswim. See [ATR export coverage diagnosis](Validation/ATR-Export-Coverage-Fix-20261106.md) and [latest provider evidence](Validation/Sell-Limit-Evidence-Analysis-20261106.md).

The latest IEX provider comparison requested 119 timestamps per symbol, returned 104 EVR and 109 CBOE bars, and found 0/119 exact OHLCV rows for each. It is explicitly diagnostic: the response feed was IEX, not SIP. This cannot establish which source is wrong or predict SIP equality.
## P0 — ATR correctness and sell-limit qualification

### P0.1 Diagnose numeric ATR mismatches

**Current state:** Parity endpoint and full-history Wilder seed are implemented. Latest EVR/CBOE 5m numeric errors are within 0.00005; the last verified API result was `GAPS_PRESENT` (3 EVR / 1 CBOE timestamp-gap groups). Independent recurrence recalculation across all persisted adjacent pairs found no large residuals (max 0.0000928571 for each symbol), consistent with four-decimal ATR rounding. A code defect was identified: same-date 00:00–00:10 ET gaps wholly inside the 20:00–04:00 ET closure were incorrectly counted. `AtrTimestampGapDetector` now counts same-date gaps only when they overlap 04:00–20:00 ET, with regression tests for closure, premarket-overlap and after-hours cases. GitHub Actions passed build and 236/236 tests on exact code/test commit `151cb97976121600833c8927bddf4cf175567b7b`; a post-restart local API parity re-read is still pending. Remaining session-overlapping gaps still require chart/source evidence.

**Latest code/test verification:** GitHub Actions run [42307942293](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42307942293) passed Release build (0 warnings/errors) and 236/236 tests on `151cb97976121600833c8927bddf4cf175567b7b`. This validates the code/tests, not the operator's local API or TOS chart.

**Missing work**
- [x] Parse the expanded EVR/CBOE exports and recompute row-level Wilder recurrence from exact payload OHLCV.
- [x] Re-audit the latest persisted EVR/CBOE observation sequences: all 326 EVR and 337 CBOE adjacent-pair Wilder recurrence checks have maximum absolute residual 0.0000928571, consistent with four-decimal ATR rounding; no residual exceeds 0.0002. This supersedes the earlier large EVR residual diagnosis for the older import.
- [x] Identify the export predicate mismatch: ATR calculation uses every host-chart bar, but the former Strategy Report predicate omitted rows outside 07:00–19:59 ET.
- [x] Update the V2.1 ThinkScript source to export all available chart bars in the selected date range; retain date filtering only.
- Compile the revised study in Thinkorswim and verify its full-history export includes premarket, RTH and after-hours bars.
- Do not re-import or re-upload the existing EVR/CBOE CSVs. After the detector change passes CI, pull/build/restart and re-read parity against the existing persisted observations. Only request a genuinely new export if chart evidence proves a specific omission and the revised study is expected to produce new source rows.
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

**Current state:** Latest persisted history contains 327 EVR and 338 CBOE 5m observations. These are already committed and available; do not request or re-import the same ScannerExports. Final-20 numeric ATR errors are within 0.00005. A fresh recurrence audit across all adjacent persisted pairs found no large residuals (maximum 0.0000928571 for both symbols, consistent with four-decimal ATR rounding). The previous detector reported 3 EVR / 1 CBOE gaps. One EVR gap (00:00–00:10 ET) is wholly outside the 04:00–20:00 ET ETH chart session and is now excluded by the updated detector; runtime parity has not yet been re-read. Remaining premarket/after-hours gaps stay blocked until chart/source evidence establishes whether rows were omitted or no chart bars existed. Revised full-chart export source is committed but TOS compilation remains outstanding.

**Missing work**
- First pull/build/restart the new session-aware detector and re-read parity using the existing persisted observations; do not re-import the same files.
- Compile the revised exact V2.1 ThinkScript in Thinkorswim. It exports all available chart bars in the selected date range without the former time-of-day filter.
- Confirm chart/study aggregation 5m; ATR14 Wilder; Phoenix timezone; ETH ON; corporate-action adjustment ON.
- Inspect the existing payload timestamps and actual TOS chart around each remaining premarket/after-hours gap. Determine whether a chart bar existed but was omitted or no bar existed because there was no eligible chart print. Never synthesize a bar.
- Only if a specific export omission is proven, produce a genuinely new export from the compiled revised study; do not re-send existing CSVs.

**Acceptance:** corrected export imports with provenance intact; 119-bar window is continuous under the declared source/session contract; parity returns MATCH. A numeric MATCH on an incomplete window is not enough.

### P0.3 EVR 1m continuity

**Current state:** Numeric comparison is within tolerance, but 13 gaps in the latest 119-observation window produce `GAPS_PRESENT`.

**Missing work:** obtain a source-complete 1m export or independently establish which missing timestamps are legitimately absent under the exact TOS chart/session rules. If they cannot be resolved, retain blocked status. 1m parity is not a substitute for the independent 5m sell-limit gate.

### P0.4 Provider/TOS bar parity and reference close

**Current state:** Independent orchestration gates exist. The 2026-11-06 10:53 Arizona run compared 119 timestamps per symbol against IEX: EVR returned 104 bars, CBOE 109, and neither had any exact OHLCV rows. This is diagnostic only; it does not prove which source is wrong or predict SIP results. The runner was hardened to distinguish comparison mode, configured/response/row-level feed identity and per-field mismatch counts. The updated PowerShell source has not yet been executed locally.

**Missing work**
- Capture the same symbol, 5m aggregation, completed-bar timestamps, session scope and adjustment basis from the configured provider and TOS.
- Compare all 119 timestamps and OHLCV fields exactly (or an explicitly approved numeric normalization for price fields); volume must match exactly.
- Verify provider/feed identity on every row. Never mix IEX and SIP.
- Verify the exact previous regular-session 1m close from the same provider/feed.

**Acceptance:** all 119 matched bars and exact reference close pass; otherwise the orchestrator remains BLOCKED.

### P0.5 SIP and 40-session GTC_EXT history

**Current state:** Alpaca is effectively IEX. The operator has repeatedly confirmed recent SIP requests return HTTP 403. Treat this as an established external blocker; no further SIP requests, configuration changes, smoke tests, or captures are planned unless the operator independently reports a genuine entitlement change. The runner's SIP preflight remains a code safety guard, not an instruction to try SIP again.

**Parked dependency — no operator action requested**
- The operator has repeatedly confirmed recent SIP requests return HTTP 403. Do not ask for another entitlement check, reconfigure the feed, run a smoke request, or initiate a capture.
- Only if the operator independently reports a genuine entitlement change should this workstream be reopened under a new explicit instruction. The technical contract remains 40 complete sell sessions (20 training + 20 unseen test), each with 780 exact 1m bar-start timestamps from 07:00 through 19:59 ET and the exact previous regular-session close, followed by persisted-source read-back and feed/provenance verification.

**Acceptance:** 40/40 complete windows, exact close boundaries, SIP source, no nearest-bar substitution or imputation. IEX remains diagnostic only.

### P0.6 Walk-forward runtime qualification

**Implementation status:** implemented in `BoringOvernightWalkForwardValidator`; the sell-limit orchestrator has a hard `WALK_FORWARD_VALIDATION` gate. Old audit text saying this was not implemented is stale.

**Missing work:** runtime qualification is blocked until 40 complete, eligible, source-qualified windows exist. No SIP capture is currently planned because of the repeated HTTP 403 entitlement denial. If a valid source-qualified 40-session dataset becomes available later, preserve 20 training then 20 later unseen test sessions. Verify 100% coverage, positive training target, test target-touch rate >= training empirical rate, and test maximum adverse excursion <= training adverse-excursion P95 bound. The target quantile is selected from training only.

**Acceptance:** persist/report complete train/test counts, quantile, target, touch counts/rates, adverse-excursion statistics, exclusions and source IDs. A passing validator does not imply an actual fill or independently authorize an order.

### P0.7 Sell-limit fail-closed release gate

Keep numeric output blocked until P0.1–P0.6 and all existing orchestration gates pass, including fresh completed 5m ATR, exact source provenance, 119 provider/TOS OHLCV matches, exact reference close and current data. The endpoint reads persisted observations only; it must not fetch/impute bars or submit orders.

## P1 — data quality, ranking and strategy correctness

### P1.1 Daily market data and liquidity

**Detailed procedure and independent reference sample:** [Daily-Data-Qualification-20261106.md](Validation/Daily-Data-Qualification-20261106.md).  

**Implementation status:** API, tests, Dashboard panel, PowerShell evidence runner and independent 20-session reference CSVs are committed. Operator executed the runner on 2026-11-06; both symbols passed internal consistency, coverage (20/20), freshness and zero-missing checks, but failed independent OHLCV parity. Effective feed was IEX and API correctly withheld qualification metrics (`DATA? · IEX_PARTIAL`). Dashboard screenshot/browser parity remains outstanding.

**Completed engineering**
- [x] Fixed empty Daily history handling so zero bars return incomplete DATA? metrics rather than throwing on an empty-session `Max()`.
- [x] Added `latestExpectedSessionDate`, ordered `expectedSessionDates`, `missingSessionDates` and `sessions` (session date, source timestamp, OHLCV) to the Daily API response.
- [x] Added unit coverage for 20 complete sessions, exact date lists, independently recalculated close/SMA20/average volume, incomplete IEX data and empty provider history.
- [x] Added Dashboard RTO/EVR Daily audit showing provider/feed, adjustment, latest expected session, API retrieval timestamp, observed close, SMA20, average volume, freshness, qualification and missing dates.
- [x] Added `scripts/validation/Test-DailyMarketDataQualification.ps1` to save raw responses, recalculate metrics, enforce feed fail-closed behavior and compare independent OHLCV.
- [x] Added `data/validation/daily-reference-20261106/{RTO,EVR}.csv`: 20 sessions from 2026-10-12 through 2026-11-06, sourced from Stock Analysis / S&P Global Market Intelligence.
- [x] Independently recalculated the reference baselines: RTO close 23.63, SMA20 22.3130, ADV20 1,616,207.4; EVR close 284.47, SMA20 272.2235, ADV20 643,488.4.
- [x] Executed live qualification run `20261106T215302Z`; exact 20-session coverage, freshness, zero missing dates and internal arithmetic passed for both symbols.
- [x] Confirmed fail-closed IEX behavior: both API responses report `IEX_PARTIAL`, `qualificationUsable=false`, and null qualification fields.
- [x] Recorded independent parity failure: both symbols have OHLC differences and all 20 daily volume rows differ from consolidated reference; raw evidence is committed.

**Adjustment note:** reference pages disclose split-adjusted history, while Alpaca requests `adjustment=all`. For this sample the known EVR ex-dates (2026-08-28 and next 2026-11-27) and RTO ex-date (2026-08-13) are outside the 20-session window. The runner therefore requires the explicit `-ConfirmWindowAdjustmentEquivalence` switch before comparing split-adjusted reference rows to the all-adjusted API. Recheck corporate actions before reusing this switch for any other date range.

**Remaining operator/runtime work**
- Run Release build/tests against the exact latest code-bearing branch source (the current updates are documentation/evidence only; prior operator-reported PASS was at SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`).
- Do not repeat the same IEX qualification run; the 2026-11-06 run already passed internal 20-session checks and failed independent consolidated OHLCV parity.
- SIP entitlement is a known external blocker after repeated HTTP 403 responses. No further operator action or SIP request is planned; park this gate unless the operator independently reports a genuine entitlement change.
- Keep the current IEX results diagnostic-only. Do not repeat the unchanged Daily run. RTO/EVR independent parity already failed; CBOE's independent reference is absent. Any future independent validation requires a different source/feed with known coverage, not another identical IEX run.
- Click the Dashboard Daily audit button and verify the UI matches the saved API evidence exactly; capture a screenshot.
- Verify evaluator/snapshot behavior remains DATA? whenever feed is non-SIP, history is incomplete/stale, or any required metric is missing.

**Acceptance:** internal arithmetic consistency AND independent 20-session OHLCV parity; same adjustment basis (or explicitly evidenced window-equivalent basis); source/feed/retrieval timestamp visible; SIP and freshness gates pass before metrics may be used for qualification; incomplete/stale/non-SIP data yields DATA?/UNKNOWN and never READY.



### P1 runtime acceptance — Dashboard, WebSocket, and collector

**Operator confirmed:** main Dashboard is the only target; the visible panel label is **Daily data audit**. RTO/EVR/CBOE audit returned 3 rows, no API errors, no qualification-usable rows; all remain IEX_PARTIAL / DATA? despite 20/20 fresh sessions. This is the expected fail-closed display, not a Daily qualification pass.

**Scripts added:**
- `scripts/validation/Capture-DashboardBrowserDiagnostics.ps1` — Chromium CDP capture of console errors, uncaught exceptions, sanitized network request/response/failure events, and a screenshot.
- `scripts/validation/Test-RegularSessionStreamCollector.ps1` — 5-minute / 15-second read-only capture of health, provider, stream and collector status, plus per-symbol bar/timestamp deltas and post-capture live assessments.

**Still open:** both scripts require operator execution against the local API/browser. The collector may be disabled; if so, report NOT TESTED/disabled rather than treating it as a pass. WebSocket `SUBSCRIBED` alone is not delivery proof. Full Dashboard browser acceptance remains open after the Daily panel check.

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

1. Pull the latest feature branch, inspect the revised runner, run Release build/tests, and restart the API only after the exact source is built.
2. Compile the revised V2.1 ThinkScript in TOS. Set 5m aggregation, ATR14 Wilder, Phoenix timezone, extended hours ON and corporate-action adjustment ON. Choose a date range that actually contains at least 99 warm-up bars before the latest 119-bar window.
3. Export EVR and CBOE with all available premarket, RTH and after-hours chart bars. Confirm embedded source epochs and actual first/last timestamps; filenames do not prove date coverage.
4. Import each file once; record append/identical/correction counts and inspect per-row provenance. Re-run parity at 0.00005; investigate every gap without imputation.
5. Run the updated provider comparison only after the TOS window is continuous. With IEX, treat all results as diagnostic-only. Require effective feed, response feed and every row feed to agree.
6. Keep SIP-dependent qualification parked. Do not request another SIP entitlement check, reconfigure the feed, or run a SIP smoke request unless the operator independently reports that entitlement has changed.
7. Capture exact SIP provider/TOS 119-bar OHLCV and exact prior RTH close. Then capture/audit 40 complete SIP GTC_EXT sessions (20 train + 20 unseen test), each with 780 exact minute bars and the exact previous close.
8. Run the sell-limit endpoint and preserve all gate results. The walk-forward validator is implemented, but only real complete source-qualified history can establish runtime PASS.
9. Complete main Dashboard browser acceptance, regular-session WebSocket/collector acceptance, Daily parity, catalyst label validation and scanner field-availability audit as separate workstreams.
## [Uncertainties]

- The root cause of EVR 5m and CBOE 5m numeric ATR mismatch is unknown; row-level evidence is required.
- Missing TOS timestamps may be no-trade intervals or export/study omissions; the supplied CSV alone cannot distinguish them.
- SIP entitlement remains unconfirmed; current effective feed is IEX.
- Runtime walk-forward pass, provider/TOS OHLCV equality, browser acceptance and regular-session WebSocket recovery are not demonstrated.
- The current scanner missing-field count has not been refreshed after the last 296-row audit.
- No actual broker execution ledger or verified broker-fill integration is present.
