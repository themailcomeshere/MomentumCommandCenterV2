## 2026-11-05 — ATR import-control recovery and V2.2 after-hours export audit

### Completed
- [x] Added no-store/no-cache response headers for Dashboard `index.html` and `app.js` in API static-file middleware to prevent mixing stale HTML with newer JavaScript.
- [x] Confirmed the committed `frontend/index.html` contains all five ATR metadata controls. The operator's running DOM nevertheless reports all five absent while the current import handler is executing. This is a page/asset generation mismatch, not a missing control definition in the current source HTML.
- [x] Added a runtime recovery bootstrap to `frontend/app.js`: if the StrategyReport form exists but any ATR metadata control is absent, it creates the missing controls before the submit handler is registered. Defaults use the operator's already-confirmed V2.1 settings: ATR 14, Wilder, America/Phoenix, extended hours ON, corporate-action adjustment ON. Existing controls remain editable and are not overwritten.
- [x] Bumped the Dashboard app.js cache key to `20261105-atr-import-control-recovery-v3`.
- [x] Audited `StrategyReports_EVR_11042026_5M_ATR_v2_2.csv`: 82 payload observations; first source bar 2026-11-04 14:05Z (09:05 ET / 07:05 AZ); last 2026-11-04 21:00Z (16:00 ET / 14:00 AZ).
- [x] Found one 15-minute source timestamp gap, 09:15–09:30 ET. The report has one source bar timestamped exactly 16:00 ET, but no later source bars (16:05–19:55 ET). Thus the export does not yet demonstrate consistent extended-hours OHLCV coverage.
- [x] Confirmed 82 observations are insufficient for the required 119-observation 5m parity window (99 warm-up + 20 comparison bars). No 5m ATR parity certification or 99-bar continuous warm-up is possible from this file.
- [x] Kept the Nov 4 v2.2 payload epoch as the authoritative source-bar timestamp. The StrategyReport Work Time and order-event Date/Time fields are not substitutes for the embedded source epoch.

### Still open / operator verification
- [ ] Pull latest branch and run `.scripts\Get-Latest-And-Test.ps1`; check the frontend syntax and all tests.
- [ ] Restart the API and hard-refresh Dashboard. Confirm the five ATR controls now appear. The runtime bootstrap should restore them even if the HTML is stale.
- [ ] Retry Nov 3 1m and 5m V2.1 imports. Send both full import summaries and server parity results.
- [ ] Export another EVR 5m report with the updated study and verify source epochs after 16:00 ET. Confirm actual 5-minute observations through the intended 19:55 ET cutoff. If absent, investigate Thinkorswim chart ETH display, loaded chart history, study inputs, and source trade availability; do not infer continuous coverage from Work Time.
- [ ] Investigate the 09:15–09:30 ET gap in v2.2.
- [ ] Continue only after import and ATR evidence is visible: 119-bar continuous parity window, 99 consecutive warm-up bars, and exact provider/TOS OHLCV match.
- [ ] Keep SIP GTC_EXT separate; prior SIP HTTP 403 remains unchanged. Do not retry until entitlement changes.
- [ ] Walk-forward validation remains 20 training + 20 unseen test sessions and is not itself sufficient to publish a limit. Actual broker fills remain a separate source.

### [Uncertainties]
The exact reason the operator's running DOM omitted all five controls is not established. The runtime bootstrap is a defensive recovery rather than proof of the underlying static-file routing/cache cause. The single 16:00 ET payload bar is the only post-regular-session boundary observation in v2.2; whether additional eligible bars were available from Thinkorswim is not established by this CSV.

---

## 2026-11-05 — StrategyReport import crash, extended-hours export window, and walk-forward engine

### Completed in this engineering pass
- [x] Fixed the Dashboard ATR import handler so it validates all five required metadata controls before reading values. A stale/mismatched HTML page now gets a precise missing-control error instead of an uncaught null.value exception; the CSV is not sent when metadata controls are missing.
- [x] Bumped the Dashboard app.js cache key to force the browser to load the guarded handler with the current HTML.
- [x] Audited `StrategyReports_EVR_11042026_5M_ATR_v2_1.csv`: 82 V2.1 observations; timestamps 2026-11-04 14:05Z–21:00Z (09:05–16:00 Eastern / 07:05–14:00 Arizona); one 15-minute timestamp gap from 09:15 to 09:30 Eastern. No payload observations occur after 16:00 Eastern.
- [x] Identified why the Nov 4 payload stops at the regular close: the ThinkScript study itself filtered source bars to `startTime=0600`, `endTime=1600`. Extended-hours chart display ON cannot override this script-level time window.
- [x] Updated the V2.1 ThinkScript defaults to 07:00–20:00 Eastern and documented that `SecondsFromTime` / `SecondsTillTime` use the EST/Eastern clock even when the chart display timezone is Phoenix. The study must be reloaded/compiled and a new report exported before extended-hours inclusion is verified.
- [x] Implemented `BoringOvernightWalkForwardValidator`: 20 earlier training sessions + 20 later unseen test sessions; training-only nearest-rank target quantile; test target-touch rate; mean/max test adverse excursion; training adverse-excursion P95 bound; complete-sample coverage and fail-closed outcomes.
- [x] Extended sell-window history from 20 to 40 sessions and persisted session lows as well as highs. The sell-limit calculator receives only the first 20 training sessions, never the unseen test sessions.
- [x] Added a hard `WALK_FORWARD_VALIDATION` gate to sell-limit orchestration. Numeric output remains withheld unless the walk-forward gate and every existing independent evidence gate pass.
- [x] Added unit tests for a passing split, failed test touch rate, failed adverse excursion, insufficient sample, and incomplete coverage.
- [x] Added `docs/Validation/Walk-Forward-Validation-Design-20261105.md` with the formulas, sample policy, pass/fail rules, data separation, and limitations.

### Still open / operator verification
- [ ] Pull the branch and run `.scripts\Get-Latest-And-Test.ps1`; verify the new C# validator and tests compile and pass in CI/local.
- [ ] Hard-refresh the Dashboard (Ctrl+F5). Retry the Nov 3 1m and 5m V2.1 imports. Confirm parsed/persisted counts and parity statuses; the UI crash fix has not yet been browser-verified against the user's running instance.
- [ ] Pull the updated ThinkScript, set chartAggregation=FIVE_MIN and both chart/study aggregation to 5m, confirm extended-hours ON and corporate-action adjustment ON, compile, and export a fresh EVR 5m report using the new 07:00–20:00 ET study window.
- [ ] Inspect the new report for payload timestamps after 16:00 ET through the final eligible extended-hours interval. Confirm after-hours bars are present; do not assume the updated filter guarantees bars where no trades occurred.
- [ ] Investigate the 09:15–09:30 ET 15-minute gap in the Nov 4 report. It may be missing observations or absent trades; the CSV alone does not identify the cause.
- [ ] Capture/audit 40 complete SIP GTC_EXT sessions only after actual SIP entitlement/effective-feed access changes. Existing SIP HTTP 403 remains a blocker; do not retry SIP now.
- [ ] Verify ATR V2.1 5m parity, 99-bar warm-up, exact provider/TOS OHLCV match, exact reference close, and all other independent sell-limit gates. Walk-forward PASS alone does not qualify a numeric limit.
- [ ] Keep actual broker fills completely separate; no broker execution connector or fill ledger was added in this pass.
- [ ] Run browser acceptance on the Dashboard import flow and sell-limit assessment UI after local tests pass.

### Walk-forward v1.0 policy
- Training: earliest 20 complete chronological sell sessions.
- Test: next 20 complete unseen sell sessions.
- Training-only target: nearest-rank quantile of session maximum favorable return.
- Pass requires test target-touch rate >= training empirical touch rate and maximum test adverse excursion <= training adverse-excursion P95.
- Test coverage must be 100%; no missing rows are imputed.
- The 5m export's ATR observations are a separate technical-validation input; walk-forward uses 1m eligible sell-window OHLCV.
- OHLCV target touches are theoretical and are never counted as actual broker fills.

### [Uncertainties]
The import error's exact missing DOM element is not observable from the reported exception alone; the new guard will name any missing control on retry. The Nov 4 report shows a filter cutoff at 16:00 ET consistent with the old study default, but the source data does not prove whether every omitted after-hours minute had a trade. The new 40-session walk-forward engine and tests have been committed but require CI/local execution and operator runtime acceptance.

---

## 2026-11-05 — corrected V2.1 exports, ATR session-gap handling, and Overnight candidate review

### Completed in this pass
- [x] Reviewed the newly committed EVR V2.1 Strategy Reports: 346 one-minute observations and 81 five-minute observations. The explicit payload markers are `v2.1`.
- [x] Recomputed one-minute Wilder ATR from the exported OHLCV using the parity service's seed and recurrence. The 20 bars after the 99-observation warm-up have mean absolute error approximately 0.00002603 and maximum absolute error approximately 0.00004669, within the 0.00005 export-rounding tolerance. This is a numeric diagnostic only; the latest 119-row window has 13 intraday timestamp gaps, so parity correctly remains blocked.
- [x] Confirmed the five-minute report has only 81 observations and one 10-minute timestamp jump. It cannot satisfy the 99-observation warm-up or 20-bar comparison requirement.
- [x] Added New York session-date-aware ATR gap detection to the parser and parity service. Normal overnight/weekend date transitions no longer count as missing intraday bars; same-session missing intervals still block MATCH.
- [x] Added regression tests for scheduled overnight/weekend closures and intraday missing-bar detection. CI for code commit `c9003fd24d3b39817a874304195e01619c79de21` is green: build succeeded and 225/225 tests passed in [Actions run 42121843682](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42121843682).
- [x] Located and verified the Dashboard assessment control. Navigation: Workspace = Boring Overnight → Calculated Limit Sell → Research target quantile (%) → **Assess sell-limit evidence gates**. HTML ID: `overnightCalculateSellLimit`.
- [x] Reviewed AVNT, BLMN, EVER, BLK and CBOE scanner metrics and current catalysts. Added `docs/Validation/Overnight-Candidate-Review-20261105.md`.
- [x] Reconciled SIP history: prior recent-history SIP request returned HTTP 403 due subscription entitlement; IEX preflight rejection is a separate known limitation. No new SIP request was made and the operator is not being asked to repeat failed attempts.

### Still open after this pass
- [ ] Import the two supplied V2.1 reports through Dashboard → StrategyReport and verify persisted observation counts and parity response. Repository review of the CSVs is not the same as a successful Dashboard import.
- [ ] Export a longer 5m V2.1 history with at least 119 observations (99 warm-up + 20 comparisons). Multiple sessions are allowed; the updated detector excludes scheduled overnight/weekend transitions. Missing intervals within a New York session date still block MATCH.
- [ ] Resolve the 13 intraday gaps in the latest 119 observations of the supplied 1m export if 1m parity is needed for validation. The last 20 post-warm-up numeric values are within tolerance, but the full window is not gap-free.
- [ ] Add explicit full-session coverage checks so an entirely missing trading session cannot be mistaken for a normal date transition. The current gap detector is intraday/session-date aware, not a full-session completeness validator.
- [ ] Implement chronological walk-forward target-touch and adverse-excursion validation. This remains the numeric sell-limit gate.
- [ ] Keep actual broker fills/fees/quantities separate from theoretical OHLCV target touches.
- [ ] Continue non-SIP work; do not request another SIP history attempt unless the account entitlement or provider access changes.
- [ ] Complete Daily/catalyst qualification, current 5m/1m evidence, scanner full-pool field availability, and local browser/runtime acceptance.

### Candidate review outcome
- Preferred addition to the next ordinary Overnight shortlist: **CBOE**.
- Strong historical consistency: **BLK**.
- Momentum watch: **AVNT**, but it is immediately post-earnings.
- Secondary only: **EVER**, because current ON is negative and mean ON substantially exceeds median ON.
- Exclude from an ordinary overnight hold into November 6: **BLMN**, because its Q3 earnings are scheduled for November 6 premarket.
- These are research priorities, not READY/buy decisions. Full rationale and sources are in `docs/Validation/Overnight-Candidate-Review-20261105.md`.

### Operator UI and SIP note
The **Assess sell-limit evidence gates** button is in the Boring Overnight workspace under Calculated Limit Sell, immediately below the research quantile input. Do not click **Fetch 20-session GTC_EXT sell-window history** while the previously tested SIP access remains denied. No repeated SIP test is requested.

---

## 2026-11-05 — ATR V2 ingestion, parity diagnosis, and second-pass unblock review

This section supersedes the earlier statement below that only the ThinkScript compiler correction had been implemented. Code has now been added on `feature/opportunity-quality-classification`; the earlier history is retained for audit.

### First pass — implement every item that current evidence permits
- [x] Enumerated the remaining unchecked entries and completed a second-pass dependency review; current workstream status is consolidated in `docs/Validation/Master-To-Do-Audit-20261105.md`.

- [x] Parsed both new full ATR V2 exports: 346 1m ATR observations and 81 5m ATR observations. Their Strategy Reports contain 692 and 162 numbered order rows respectively because each source bar emits a transport-close row and an ATR payload row.
- [x] Added a dedicated V2 parser with 11-field payload validation, all six documented timeframes, UTC epoch-millisecond parsing, invariant decimal parsing, symbol/timeframe consistency, duplicate rejection, and explicit gap reporting. V1 parser remains unchanged.
- [x] Added an isolated append-only Thinkorswim ATR JSONL store. It records OHLCV + ATR, chart settings, study version, source file, import batch and correction linkage. Identical re-imports are skipped; changed values append a correction. It is not mixed with Alpaca IEX/SIP observations.
- [x] Added `POST /api/strategy-report/atr-import`, `GET /api/strategy-report/atr-observations`, and `GET /api/strategy-report/atr-parity`.
- [x] Added the Dashboard import controls for ATR length, average type, chart timezone, extended-hours and corporate-action adjustment. V2 uploads are persisted separately; V1 uploads retain the existing parser flow.
- [x] Added a versioned Wilder ATR parity service: 99-observation warm-up, at least 20 compared bars, timestamp matching over the latest 119 rows (99 warm-up + 20 comparison), gap count, mean/max/P90 absolute error, mean relative error, and a fixed 0.00005 tolerance tied to the export's four-decimal precision. Any gap in that window blocks MATCH. It compares the server calculation with TOS ATR on the exact same imported TOS OHLCV; it does not claim provider-feed parity.
- [x] Added tests using both actual exports, plus malformed payload, duplicate timestamp, gap, idempotent persistence, correction, positive parity, mismatch and insufficient-warm-up cases. Build and tests passed on exact code commit `c990faf973fb9e3a14ec948386e210b9eb8b4fc5` (Actions run [42119562504](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42119562504)): 223/223 tests passed, including frontend JavaScript syntax validation.
- [x] Added `docs/Validation/Overnight-Scanner-Review-20261105.md` comparing original and updated scanner exports, row completeness and top-20 scores.
- [x] Updated the StrategyReport UI to display the ATR parity state after import. The import/parity and sell-limit assessment UI is included in the exact-head CI run above; browser acceptance remains user-side.

### New evidence discovered while implementing parity

- [x] Found a one-bar previous-close shift in the ThinkScript V2 source: `TrueRange(high, close[1], low)` passes a shifted close series to a function that already uses the previous close. Corrected it to `TrueRange(high, close, low)` in commit `efecef0c26a9af064e5cba68d522fa9d7137ca51`. Official reference: https://toslc.thinkorswim.com/center/reference/thinkScript/Functions/Tech-Analysis/TrueRange
- [x] Quantified the defect against the 346-row 1m export after 99 warm-up observations: standard Wilder ATR on the exported OHLCV had mean absolute error about 0.06370 and maximum about 0.14788; reproducing the extra previous-close shift had mean absolute error about 0.00002545 and maximum about 0.00004974. This is within the four-decimal export rounding bound. The November 3 export therefore reflects the old shifted formula; it must not be used to certify corrected-study parity.
- [x] Operator supplied corrected ThinkScript V2.1 EVR 1m/5m Strategy Reports with the `v2.1` payload marker, confirming the corrected study executed and exported. The 1m report has 346 observations but 13 intraday gaps in the latest 119; the 5m report has 81 observations and one intraday gap, below the 99+20 parity requirement. Dashboard import and parity verification remain open.
- [ ] The legacy V2.0 5m export has only 81 observations, below the 99-bar warm-up. The corrected V2.1 payload is separately versioned so legacy values cannot contaminate parity. Export at least 119 consecutive completed 5m bars (99 warm-up + 20 comparison bars), with no timestamp gaps in that parity window and the corrected study.
- [ ] Re-import the corrected exports and require `MATCH` from the ATR parity endpoint before enabling any numeric sell limit.

### Second pass — re-check whether first-pass work unblocked other tasks

- [x] ATR parity was previously blocked by missing TOS ATR values. V2 export + persistence + comparison now make the algorithm-level parity test executable. The legacy 1m file deliberately reports `NOT_MATCH`; the current 5m file reports `INSUFFICIENT_WARMUP`.
- [x] Fail-closed sell-limit orchestration API/UI is implemented and checks corrected-study parity, 99 warm-up + 20 comparison 5m bars, exact provider/TOS OHLCV equality across all 119 rows, exact reference close and 20/20 SIP GTC_EXT windows. It deliberately withholds a numeric limit because walk-forward validation is not implemented and prior SIP historical access returned HTTP 403 due subscription entitlement.
- [x] ATR import/provenance and sell-limit evidence-assessment UI are implemented. Browser acceptance and saved-observation review after page reload remain open.
- [ ] Opportunity Quality full-pool ranking remains blocked by 296 rows with unavailable custom-expression values in the updated scanner. The top-score preview is not a qualified candidate list; current price/liquidity, catalyst, exclusion, 5m and 1m evidence are still separate gates.
- [ ] SIP history remains blocked: prior recent-history SIP requests returned HTTP 403 due subscription entitlement; effective runtime feed remains IEX. Do not repeat SIP requests or qualify GTC_EXT from IEX.
- [ ] Catalyst validation remains blocked by the absence of human-labelled adjudication data and measured classification performance.
- [ ] Walk-forward target validation remains blocked by unqualified SIP history and corrected ATR parity.
- [ ] Execution reconciliation remains blocked by broker fill/fee/quantity records. TOS Strategy Report order events and OHLCV highs are not broker fills.
- [ ] Remaining historical runtime/UI tasks (WebSocket recovery, browser acceptance, EOD cutoff screenshots, performance measurements, official auction-print parity and operator runbook verification) were checked for new dependencies; the supplied CSVs do not provide the missing runtime or broker evidence. Keep those items open.

### User actions needed to unblock the next pass

1. Pull the latest feature branch and run `.\scripts\Get-Latest-And-Test.ps1`.
2. In Thinkorswim, replace the V2 study with the corrected source, compile it, and verify the chart label says `chart=MATCH`.
3. Export EVR 1m and 5m again using ATR 14 / Wilder, Phoenix timezone, extended-hours ON and corporate-action adjustment ON. For 5m include at least 119 consecutive completed bars (99 warm-up + 20 comparison bars).
4. Import both files through Dashboard → StrategyReport upload. Send me the import and parity results if either does not show the expected counts/status.
5. No SIP retry is requested. Prior recent-history SIP access returned HTTP 403 due subscription entitlement. Keep SIP qualification disabled; continue with IEX only for non-qualifying diagnostics unless account entitlement materially changes.

## 2026-11-05 — ATR V2 compile-error correction and sell-limit implementation status

- [x] Diagnosed the user's ThinkScript compile errors: `AggregationPeriod.ONE_MIN` is not the ThinkScript one-minute constant; the supported constant is `AggregationPeriod.MIN`. The script also assigned string labels to `def` variables, which are numeric-only in ThinkScript and caused the cascade of `Expected double` / incompatible string parameter errors.
- [x] Corrected the V2 study: replaced `ONE_MIN` with `MIN` and inlined the string conditional into the label and Strategy Report order-name expressions instead of storing a string in `def timeframeLabel`. Commit: [39cacef](https://github.com/themailcomeshere/MomentumCommandCenterV2/commit/39cacef3a68f88e2e3fc33467d260ac00f175b37).
- [x] User confirmed the earlier aggregation-selector/compiler correction works. The later V2.1 TrueRange/payload revision is a separate source change and still requires Thinkorswim compile/export validation.
- [x] V2 Strategy Report parser: dedicated V2 parser now validates the 11-field ATR payload, six timeframes, UTC timestamps, invariant decimals, duplicates/gaps, and actual full 1m/5m fixtures; V1 parser remains unchanged.
- [x] Separate append-only Thinkorswim ATR observation persistence: JSONL store records symbol/timeframe/timestamp, ATR settings, study revision, chart timezone, ETH/adjustment, import batch/hash and correction linkage. It remains isolated from provider OHLCV.
- [x] Server Wilder ATR diagnostic: versioned series calculation and timestamp-matched comparison now use the same imported TOS OHLCV with 99-bar warm-up. Production provider-feed calculation/validation remains a separate acceptance gate.
- [x] Timestamp-matched ATR parity service: reports compared bars, gaps, mean/max/P90 absolute error and relative error; fixed 0.00005 tolerance is tied to four-decimal export precision. Corrected V2.1 platform parity still requires a fresh operator export.
- [x] Fail-closed sell-limit orchestration API: `POST /api/boring-overnight/sell-limit` now composes 20/20 history, exact reference close, V2.1 ATR provenance/parity, 99 consecutive 5m bars and exact provider/TOS OHLCV. It deliberately returns BLOCKED until walk-forward validation exists; it never submits orders.
- [x] UI status/provenance/failure reasons: Dashboard now imports V2 ATR, displays parity and gap details, and exposes a sell-limit evidence assessment button with per-gate reasons. Browser acceptance remains open; numeric output remains DATA? until walk-forward is implemented.
- [ ] SIP history qualification: user runtime currently reports effective Alpaca IEX. SIP entitlement is not demonstrated; prior SIP request returned 403. Do not mark complete or request SIP capture while effective feed is IEX or entitlement is denied.
- [ ] Chronological walk-forward validation: use time-separated train/test windows, record target touches and adverse excursion, avoid look-ahead, and report sample sizes/coverage.
- [ ] Execution reconciliation: import actual broker fills, quantities, fees and timestamps separately from theoretical bar-high target touches. No execution integration may infer fills from OHLCV.

**Status note:** This historical section is superseded by the first section above. Subsequent commits added the V2 parser, separate ATR store, Wilder parity diagnostic, fail-closed sell-limit orchestration API and Dashboard gate UI. SIP qualification, corrected V2.1 platform re-export, walk-forward validation and actual broker-fill reconciliation remain open.

---

## 2026-11-04 — first-pass / second-pass reconciliation and Red Zone mode work

- [x] Diagnosed the GTC_EXT HTTP 422: the runtime feed is IEX from local `secrets.json`, overriding tracked `appsettings.json` SIP. The preflight correctly rejects requested SIP before fetching. Keep IEX as diagnostic until a recent SIP request succeeds; prior SIP access returned 403. Do not change configuration and assume entitlement.
- [x] Reconciled current GTC_EXT runtime results: RTO and EVR each have 0/20 complete IEX windows, with hundreds of missing exact extended-hours minutes per session. These data are not qualification-usable. Calculator connection and target validation remain blocked.
- [x] Reconciled EVR's 8 exact 09:30 ET bar gaps (Oct 12, 14, 15, 22, 26, 30 and Nov 3, 4; Nov 3/4 timestamps are 14:30Z after DST). Provider returned only later bars 1–7 minutes after the required timestamp. Do not impute.
- [x] Added persistent Red Zone Manual / Automatic mode in production and prototype. Manual mode shows ordered pins; Automatic mode shows the top supplied scanner-score preview after imported historical gates. Pins and dismissals persist across mode changes.
- [x] Removed the Schwab importer’s fabricated `quantity=1` fallback. Filled-order CSV imports now require an explicit positive `Quantity` column; added multi-share FIFO and missing-quantity tests. This prevents false share counts/P&L but does not add fees, account sync or broker reconciliation.
- [x] Added append-only human catalyst review storage and `POST/GET /api/market-data/catalyst-reviews` with a bounded label taxonomy. Labels remain separate from automated triage and cannot emit CLEAR; see `docs/Catalyst-Human-Review-Workflow.md`.
- [x] Added live Opportunity Quality diagnostic/reason-code explanation to Red Zone card reasoning. It remains provisional and does not alter V2 Permission/entry eligibility or order state.
- [x] Added `docs/Overnight-Scanner-to-Boring-Overnight-Workflow.md` with the operator sequence, source-quality rules, and the distinction between scanner score, current technical evidence, historical Overnight qualification and actual fills.
- [x] Added `docs/Validation/Overnight-Scanner-Review-20261104.md` with CSV completeness, historical-gate results, top candidates, company news/catalyst assessment, market backdrop, and manual-review shortlist.
- [ ] Opportunity Quality still needs a versioned full-pool ranker, candidate-pool evidence strategy, calibrated thresholds, rank stability/hysteresis, and unit/API/UI acceptance. Current Automatic mode is explicitly a scanner-score preview, not that ranker.
- [ ] Re-run exact-head CI and local Release tests for the Red Zone mode/UI changes; browser acceptance remains operator-side.
- [x] Located and analyzed `data/ScannerExports/2026-11-04-OvernightWatchListScanner_1244.csv` (443 symbols, 200 complete rows, 243 custom-expression-limit failures, 168 historical-gate passes); saved the symbol/news review to `docs/Validation/Overnight-Scanner-Review-20261104.md`. The unavailable 243 rows remain UNKNOWN.
- [ ] Switch effective Alpaca feed to SIP only after provider entitlement is confirmed. Current `secrets.json` IEX override is the effective runtime configuration; changing it without entitlement will produce provider authorization failure.
- [ ] Browser acceptance still required for the Manual / Automatic toggle, ordered pins, dismissal persistence, top-four preview and diagnostic explanation in both production and prototype.
- [ ] All remaining source-dependent items remain open: official auction-print parity, human-labelled catalyst validation, 20/20 GTC_EXT SIP observations, walk-forward target validation, Schwab fills/fees/P&L, Thinkorswim matched-state replay, WebSocket delivery/recovery, and operator browser/performance checks.

**First pass:** Re-read all 223 unchecked checklist boxes present before this audit section was added. They span repeated historical checkpoints as well as current work. Consolidated them into the active workstreams below; duplicate historical entries are not independent deliverables. The current file now contains additional open follow-up boxes added during this audit (227 unchecked boxes at the latest recount). Implemented the Red Zone mode and diagnostic explanation where source and runtime contracts permit. Updated the Overnight operator workflow and reconciled the newly supplied feed/history evidence.

**Second pass:** Re-tested dependencies after the first pass. The mode/diagnostic UI work does not unblock SIP entitlement, missing provider bars, official auction trades, broker execution records, Thinkorswim state tapes, or browser/runtime evidence. It does make the Opportunity Quality diagnostic visible in Red Zone and provides a manual mode that can be used while automatic ranking remains provisional. The scanner export was found in the feature branch and analyzed. The second pass also found the catalyst label collection work can be unblocked at the data-capture layer, so an append-only review API and label taxonomy were added; the remaining blocker is actual human review/labels, adjudication, and UI workflow. Schwab quantity handling was hardened so imports no longer invent one share; actual quantity-bearing exports are now required.

**Checklist reconciliation rule:** Keep a task open if it needs user-side browser/runtime evidence, external entitlement/data, Thinkorswim compilation/state tape, human labels, or broker fills. Historical duplicate checkboxes should be reconciled against the latest implementation evidence rather than blindly marked complete.

---

## 2026-11-04 — qualification UI and sell-target data-quality follow-up

- [x] Production and prototype Overnight qualification panels now support manual Daily + catalyst refresh for the four highest-scored imported candidates. Daily price/20D average volume are accepted only when the API reports complete, fresh SIP qualification data. IEX/incomplete/stale data remains DATA?; `RISK_PRESENT` blocks; all other catalyst states remain manual review. The UI does not query all imported rows and cannot emit READY. Release build, frontend JavaScript syntax check and 199/199 tests passed on code head `fcbb55d8e12268cfcb3d8592d9b7daf1aafa1f76` (Actions run [41884154586](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41884154586)); browser acceptance remains pending.
- [x] Corrected `BoringOvernightSellLimitCalculator`: complete sessions with a maximum eligible price below or equal to the reference close are valid adverse/flat observations, not missing data. When the selected quantile is non-positive, result is `NO_POSITIVE_TARGET`, with no numeric sell limit and no target-touch rate.
- [x] Added regression tests for 20 complete adverse windows and 20 complete flat windows; included in the 199/199 passing suite on code head `fcbb55d8e12268cfcb3d8592d9b7daf1aafa1f76`.
- [x] Added `POST /api/boring-overnight/capture-sell-window-history` to fetch configured provider 1m bars in bounded 7-day chunks, persist raw observations, and return the 20-window audit plus fetched/newly-stored counts. Request body: `{ "symbol": "RTO", "providerName": "Alpaca", "feed": "sip" }`. The configured provider feed must match the requested feed; mismatches are rejected before persistence.
- [x] Added `GET /api/boring-overnight/sell-window-history` to audit persisted 1m bars without another provider fetch. Capture counts are deduplicated by exact source timestamp before append, so repeated timestamps across chunk boundaries do not inflate fetched/persisted counts. It audits all 780 expected minute timestamps per sell session plus the exact previous regular-session close, and returns missing interval samples, source timestamps, high-water mark, and calculator observations. IEX is partial-exchange diagnostic only; even complete IEX timestamps are not qualification-usable. SIP is required. Eight tests cover complete SIP windows, a missing exact minute, pre-20:00 cutoff, feed isolation, complete-but-nonqualifying IEX, coexisting IEX/SIP store rows, provider-backed capture/persistence, and wrong-feed rejection. The per-session query uses the store's 10,000-row maximum before filtering the requested feed so another feed cannot truncate coverage.
- [ ] Populate persisted 1m history with complete GTC_EXT 07:00–20:00 ET coverage and run the endpoint for RTO/EVR. Data not found in sources for complete 20-window extended-hours coverage. Do not use incomplete windows in target calculations.
- [ ] Connect the endpoint's calculator observations to the sell-limit calculator only after 20/20 windows are complete and ATR parity is MATCH; then add walk-forward target-touch/adverse-excursion validation.

## 2026-11-04 — Daily data and catalyst source integration

### Implemented on `feature/opportunity-quality-classification`
- [x] Added `AlpacaDailyMarketDataClient` and `GET /api/market-data/daily?symbol=RTO`. It requests Alpaca `1Day` bars with `adjustment=all` and the configured feed; the response exposes provider/feed, adjustment mode, retrieval time, latest completed session, price, SMA20, 20-session average daily volume, freshness and completeness.
- [x] Daily qualification metrics are only exposed when the configured Alpaca feed is SIP. With IEX, the endpoint exposes diagnostic observed fields but sets qualification price/SMA20/average-volume to null because IEX is partial-exchange coverage and cannot establish market-wide liquidity. Daily calculations use the exact latest 20 expected sessions from `MCC_US_EQUITIES_CALENDAR_V2`, exclude the current session until its scheduled regular close (including early closes), and do not reach farther back when an expected session is missing. SMA20 and average volume remain null unless all 20 expected sessions are present. Price is null when the latest expected completed session is absent.
- [x] Added Daily adapter tests for adjusted request parameters, exact exchange-session coverage, current partial-session exclusion, rolling calculations, missing-session behavior and freshness.
- [x] Added ticker-scoped Alpaca news retrieval and `GET /api/market-data/catalyst-assessment?symbol=RTO`. It queries the preceding 24 hours, follows pagination up to 10 pages, records coverage completeness, and applies versioned transparent phrase triage (`MCC-CATALYST-TRIAGE-1.0`).
- [x] Catalyst triage statuses are `RISK_PRESENT`, `MATERIAL_EVENT_REVIEW`, `UNCLASSIFIED_NEWS_REVIEW`, or `UNKNOWN`. No-news is UNKNOWN. It never emits `CLEAR_WITH_SOURCE`; no article-level human validation or historical classifier validation is claimed.
- [x] `POST /api/boring-overnight/evaluate-with-history` now fetches Daily and catalyst evidence, overwrites caller-supplied price/SMA20/average-volume/catalyst values, and persists source timestamps. Only explicit `RISK_PRESENT` maps to evaluator `RISK`; every other catalyst state remains UNKNOWN. Missing/incomplete Daily data remains null. Existing 5M/1M inputs are not automatically sourced by this change.
- [x] Daily API implementation through commit `62320098083cfc4aeb353397059e83ffc27aaf47` passed Release build and **188/188 tests**, 0 warnings/0 errors, Actions run [41875185646](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41875185646).
- [x] Daily + catalyst evidence now flows through the actual `POST /api/boring-overnight/capture-history` button workflow as well as `evaluate-with-history`. Snapshots persist exact requested/valid/missing history pairs, Daily feed/adjustment/completeness/observed metrics, and catalyst article IDs/headlines/publication times/URLs/categories/rule version/coverage.
- [x] Production and prototype saved-snapshot tables now show historical pair completeness, Daily qualification values/feed coverage/adjustment/source timestamp, and catalyst article count/coverage/rule version plus the first two headlines. This is snapshot evidence visibility; the scanner qualification panel now has a manual top-four refresh that calls both Daily and catalyst APIs and shows source-backed results. It intentionally does not fetch all imported rows.
- [x] Daily + catalyst capture/snapshot evidence integration, exact-session calendar checks, SIP-only qualification gating, snapshot persistence tests, and production/prototype table changes passed Release build and **188/188 tests**, 0 warnings/0 errors, on code commit `9fb66f14e907ef029ac7955ef46802aa789e74c6`, Actions run [41876561554](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41876561554).

### Operator validation required
- [ ] Pull latest feature branch using `scripts/Get-Latest-And-Test.ps1`, restart the API, and confirm exact-head local Release build/tests.
- [ ] Smoke-test Daily endpoint for RTO and EVR; save full JSON responses and `/api/providers/status`. Compare latest completed Daily close, SMA20 and average volume with an independent chart/provider using the same adjustment basis. Report any 401/403/provider errors without sharing credentials.
- [ ] Smoke-test catalyst endpoint for RTO and EVR; save JSON including article headlines, publication timestamps, URLs, pagination and coverage status. Confirm ticker-linked articles are returned and review classifications against primary filings/news manually.
- [ ] Run the 20-session capture/evaluate-with-history flow once for RTO and EVR. Confirm snapshot Daily fields are populated only when the exact 20 expected sessions exist, and catalyst remains UNKNOWN unless an explicit risk phrase is detected. No candidate should become READY from this integration alone.
- [x] Added a manual **Fetch Daily + catalyst for top 4** action to production and prototype qualification panels. It requests both APIs for the four highest-scored imported CSV candidates, shows Daily price/20D average volume only when the endpoint marks complete fresh SIP data qualification-usable, flags IEX/incomplete data as DATA?, blocks explicit RISK_PRESENT, and leaves material/unclassified/incomplete catalyst evidence for manual review. It never emits READY; 5M/1M gates remain disconnected.
- [ ] Browser-verify the top-four refresh for a SIP-enabled account and for IEX/incomplete-data behavior. Confirm the four requested symbols match the highest imported scores and all other rows remain DATA?.

### Remaining strategy/data work
- [x] Snapshot completeness visibility added to persisted snapshot contract and saved-snapshot table: expected/valid/missing close-open pairs, Daily evidence quality/provenance and catalyst evidence status/source details.
- [ ] Browser-verify the expanded saved-snapshot table in production and prototype after pulling the latest branch.
- [ ] Official 09:30 ET opening-print source/feed parity; the 09:30 one-minute bar Open remains a proxy, not the official auction print.
- [ ] EVR's eight missing NEXT_SESSION_OPEN observations remain absent from provider response and store; nearest bars 1–7 minutes later are diagnostic only. Do not impute.
- [ ] Complete GTC_EXT 07:00–20:00 ET eligible-window outcome study, walk-forward sell-target quantile validation, and actual fills/exits/fees/realized P&L linkage.
- [ ] Opportunity Quality current-data adapter/ranking and Red Zone integration remain incomplete; provisional domain bands are not calibrated.
- [ ] Validated catalyst classification still requires human-labelled examples, authoritative-source coverage assessment, and measured false-positive/false-negative performance before CLEAR_WITH_SOURCE can exist.

## 2026-11-03 7:43 PM Arizona — operator acceptance and performance follow-up

### Verified by user
- [x] Red Zone: clear-all, restore/repopulate and CSV re-import all succeeded in browser.
- [x] 20-session capture improved from approximately 2.4 minutes to approximately 7–24 seconds after the bounded one-scan store lookup.
- [x] User selected the official 09:30 ET opening print as the intended strategy definition. Do not substitute 09:31 or nearest later bars.
- [ ] The current calculator uses the Open of the exact 09:30 ET one-minute bar. This is an opening-minute bar-open proxy, not independently verified as the official opening-auction print. Provider trade-condition/auction-print parity is a blocking definition/data task before calling this metric the official opening print.
- [ ] Full page load approximately 45 seconds; investigate using browser Network timing/HAR after latest deferral changes.
- [ ] Fetch & Analyze bars approximately 28 seconds for RTO 1m 13:00–19:12 Arizona; add instrumentation and inspect API/provider timing before changing provider retry/pagination behavior.

### Changes in this pass
- [x] Deferred the multi-symbol Morning Brief fetch until the user opens its modal. The endpoint currently performs sequential provider history requests across 14 benchmark/sector symbols plus news; it must not be on the critical dashboard startup path.
- [x] Deduplicated concurrent browser live-assessment requests by symbol so the initial chart and decision panel share the same in-flight API response.
- [x] Collapsed the 40-row exact-boundary audit behind “Show exact boundary coverage audit (40 rows)” in production and prototype; opened table is height-bounded and scrollable.
- [x] Made ticker-vs-SPY a collapsed optional diagnostic rather than a routine step.
- [x] Added bar count and elapsed seconds to Fetch & Analyze completion feedback.
- [x] A successful 20-session capture now automatically appends an immutable Boring Overnight evaluator-input snapshot and refreshes the saved-snapshot list. Historical inputs are sourced from the capture; current price, Daily liquidity, catalyst and chart gates remain UNKNOWN. Incomplete history keeps historical metrics null. No READY result is fabricated.
- [x] Snapshot provenance explicitly labels the 09:30 ET one-minute bar Open as a proxy pending independent official auction-print verification.

### How to use the diagnostic panels
- The “Provider response vs persisted store” audit is not a daily workflow. Open it only when history reports missing pairs or provider/store mismatch. Provider exact = exact timestamp present in the just-fetched response; Store exact = exact timestamp already present in MCC's append-only bar store; nearest provider bar and offset help diagnose gaps. Never replace the required timestamp with nearest bar automatically.
- Ticker-vs-SPY is optional troubleshooting only. Use the same UTC window after a symbol has unexpectedly sparse/empty coverage; compare the symbol and SPY. It does not prove entitlement or qualify a ticker.
- Saved Overnight snapshots are append-only audit inputs, not decisions. Run 20-session capture to create a history-backed snapshot; the snapshot table then shows the record and evaluator status. A capture can save an incomplete snapshot, but missing inputs remain UNKNOWN/DATA?.

### Remaining work
- [ ] Pull latest feature branch, run local Release build/tests and confirm exact-head CI.
- [ ] Browser verify page load after Morning Brief deferral; compare Network document/DOMContentLoaded/load and largest request durations. Target and baseline not yet established.
- [ ] Browser verify 1m RTO fetch elapsed time now includes bar count and seconds; use HAR and API logs if still >10 seconds.
- [ ] Verify 20-session capture automatically creates one snapshot and GET lists it; confirm missing history/current gates remain UNKNOWN.
- [ ] Resolve official opening-print data source. Current 09:30 1m bar Open is not sufficient proof of the official auction print.
- [ ] EVR history is 12/20 and provider/store exact coverage 32/40. Diagnose its eight missing boundaries independently; do not reuse RTO assumptions.
- [ ] Implement Daily close/SMA20/20D average volume from a validated Daily data source; adapter currently supports only 1m/5m.
- [ ] Connect validated catalyst/news classification; headlines are unclassified and no validated catalyst provider is wired.
- [ ] Continue GTC_EXT 07:00–20:00 ET outcome study, walk-forward target validation, and actual fills/exits/realized P&L.
- [ ] Keep Ticket-vs-SPY diagnostic optional; it is not a strategy gate.
- [ ] Keep 09:30 official opening-print parity as a blocking evidence requirement; do not label one-minute bar-open returns as official-print returns.

---

## 2026-11-03 5:01 PM Arizona — Red Zone recovery and Overnight capture follow-up

**Execution source:** This file is the active execution checklist. The companion `docs/Validation/Full-Master-To-Do-Audit-20261103.md` is the reconciliation/dependency audit; it is not a second competing execution list.

### Completed in this pass
- [x] Diagnosed Red Zone persistence: `mcc.redZoneState.v1` stored dismissed symbols globally; imported rows were filtered against that stale dismissal list before the top four were selected.
- [x] Added **Dismiss/Clear all Red Zone Candidates**. It dismisses all symbols in the currently selected scanner group and removes those group symbols from manual pins.
- [x] Renamed the recovery action to **Restore dismissed / repopulate**; it clears the dismissal list and immediately re-renders eligible candidates.
- [x] New CSV imports clear prior dismissals for symbols present in the newly imported file, so a fresh import is not permanently blanked by an earlier manual dismissal. Import feedback reports how many old dismissals were reset.
- [x] Separated saved input-snapshot status from the bar-fetch status. `GET /api/boring-overnight/snapshots` returning 0 snapshots no longer overwrites the completed `/api/market-data/bars` result.
- [x] Added elapsed-time progress updates during 20-session capture.
- [x] Optimized 20-session persisted-history retrieval: use one range query when fewer than 10,000 observations are returned; preserve exact per-pair fallback when the store limit is reached. This avoids 20 full JSONL scans in the normal bounded case without weakening coverage.
- [ ] Pull, build/test and browser-verify both production and prototype UI. CI and browser acceptance are separate.
- [ ] Verify clear-all empties Red Zone; Restore dismissed repopulates up to four; re-importing the 198-row CSV resets matching old dismissals and repopulates candidates.
- [ ] Verify bar-fetch status remains visible after snapshot refresh; snapshot status appears separately.
- [ ] Time the 20-session capture after the one-scan optimization; record duration, response, exact boundary coverage and store size. No performance claim until measured.
- [ ] Resolve the three missing exact next-session-open bars using source/feed evidence. Current result remains 17/20 pairs and 37/40 exact provider/store boundaries; no nearest-bar substitution.
- [ ] Keep rolling metrics DATA? until all 20 exact close/open pairs are present. 3,965 fetched / 0 newly stored is consistent with an idempotent refresh and does not repair missing provider timestamps.

### Current Overnight blockers
- [ ] Missing exact open bars: identify whether IEX had no eligible trade in the exact 09:30 ET minute, the provider response omitted the bar, or the calendar/bar-start convention is wrong. Use the returned per-boundary expected timestamp, nearest timestamp and offset; do not silently shift the definition.
- [ ] Decide and document whether the strategy's executable next-open price means the official 09:30 ET opening print or the first observed eligible bar after 09:30. These are different measurements; do not change the calculator without an explicit policy decision.
- [ ] Daily price/SMA20/20D average-volume provider and validated catalyst classification remain unimplemented; no READY decision.
- [ ] GTC_EXT 07:00–20:00 ET outcome study, walk-forward target quantile and actual fill/exit evidence remain unimplemented; do not use +0.5% as a validated sell target.

---

## 2026-11-03 follow-up — runtime fix, CI and Overnight process progress

- [x] Uploaded runtime-validation archive received: `20261103T190356Z.zip` contains the six expected JSON artifacts. Receipt/file inventory confirmed; JSON contents are **not yet analyzed**, so no WebSocket provenance conclusion is recorded.
- [x] Fixed startup temporal-dead-zone error in `frontend/app.js` by moving Red Zone state declarations before restored-import/startup rendering. Commit `4906d4c1119bedec6ec83f67cd21ff19d2c409fa`; exact commit Actions run [41730308073](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41730308073) completed successfully (restore, build and tests).
- [x] Documentation update commit `7b9a944f6141eb45a6b809e2da00c17fc584678e` exact-head Actions run [41730346398](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41730346398) completed successfully.
- [ ] Browser acceptance still required: pull latest, hard-refresh prototype, confirm no console exception, restored Imported CSV, and expected four Red Zone cards. CI is not browser acceptance.
- [x] Corrected Overnight strategy definition retained: Day 1 after-hours + Day 2 premarket; actionable candidate review starts in the final 60 minutes BEFORE Day 1 regular-session close; buy deadline is actual regular close; use America/Phoenix display and actual exchange calendar/early close.
- [x] Added [Overnight Process Operational Checklist](Validation/Overnight-Process-Operational-Checklist-20261103.md) with pre-window, final-hour, overnight monitoring, data-evidence and readiness gates.
- [x] Current CSV acceptance evidence remains 198 imported/rendered; 187 historical PASS, 11 FAIL, 0 UNKNOWN, 0 exclusions, 0 READY. This is not complete trade readiness.
- [ ] Runtime JSON analysis remains open. Do not lower the 50-completed-WebSocket-bar/freshness/provenance rules; inspect each symbol's sourceTransport, sourceTimestampUtc, completedAtUtc, age and per-symbol deltas.
- [ ] Daily price/SMA20/20D average-volume source, validated catalyst clearance, matched 5M/1M evidence, broker GTC_EXT eligibility and actual fill/outcome evidence remain open. No automated order submission.
- [ ] RTO EOD Thinkorswim cutoff acceptance and the 20-session exact-boundary audit remain separate validation tracks.

---

## 2026-11-03 3:20 PM Arizona — post-restart acceptance and startup exception

- [x] API health returned service `MomentumCommandCenterV2.API`, status `OK`, mode `decision-support-only`, and `orderSubmission=NOT IMPLEMENTED`.
- [x] Exclusions endpoint returned 56 entries. Filtering OSW/EVR/MHK/TEST returned TEST only; this matches the feature-branch canonical 55 plus TEST.
- [x] User browser console exposed a startup temporal-dead-zone error: `Cannot access 'redZoneAssessmentRefreshers' before initialization` in `renderMorningReport`, triggered by a startup/import render before the `let` declaration.
- [x] Fixed `frontend/app.js`: moved `redZoneAssessmentRefreshers` and related Red Zone state declarations to immediately after `scannerData` initialization and before restored-import/startup refresh code. Declaration is now at line 111; first startup render call is at line 148.
- [ ] Pull this fix, hard-refresh and verify no console exception; confirm Red Zone renders four cards. Do not count CI alone as browser acceptance.
- [ ] Inspect actual runtime JSON files from `artifacts/runtime-validation/20261103T190356Z`. The ZIP is now uploaded and its six-file inventory is confirmed; JSON contents still need analysis. Do not request another upload. Required first: `live-assessments.json`, `stream-delta.json`, `runtime-before.json`, `runtime-after.json`; use `provider-refresh.json` and `stored-bars.json` to corroborate.
- [ ] From live-assessments, record per-symbol `oneMinute.sourceTransport`, `sourceTimestampUtc`, `completedAtUtc`, freshness and completed WebSocket bar count. Do not infer from subscription acknowledgements.

# 2026-11-03 — User runtime follow-up and UI acceptance corrections

- [x] User confirms the qualification import rendered **198 scanner rows**, **187 historical PASS**, **11 historical FAIL**, **0 unknown historical metrics**, **0 exclusion matches**, and **0 final READY**. This is the current observed import result; do not require 248 rows. The 248 figure was a different prior scan snapshot and is not a fixed expected row count.
- [x] Corrected interpretation of the exclusion query: the persisted store count of 56 is consistent with the seeded 55 canonical exclusions plus the preserved TEST entry. OSW, EVR, and MHK are not present in the canonical 55-symbol list, so their absence is not evidence that the store failed to load. Live Schwab tradability remains a separate question.
- [x] User's November 3 RKLX runtime test: API health passed; 1M/5M refresh/store counts were 40/31; refresh-to-store idempotency comparisons were True/True. This establishes exact persistence matches for the returned provider bars in that invocation, not WebSocket delivery.
- [x] User's five-minute regular-session stream snapshot: SPY advanced 5 bars; RKLX, ASTX, XRPN did not advance. Acknowledged subscriptions are not delivery passes. A symbol with no eligible IEX prints is not automatically a failure; inspect each assessment's sourceTransport, sourceTimestampUtc, completedAtUtc, and freshness before deciding whether WebSocket is the selected 1M source.
- [x] Clarify the 50-bar rule: it is the minimum completed persisted WebSocket 1M history required for WebSocket source selection, not a requirement to receive 50 new bars during a five-minute test. 5M Permission remains REST-backed.
- [x] UI source changes: sticky table headers added to bounded table wrappers; exclusions list given a bounded 460px scroll viewport; Unknown historical metrics added to the qualification summary. Browser verification and exact-head CI remain pending.
- [ ] Investigate Red Zone import discrepancy: user sees 0/4 cards after importing 198 rows, while qualification reports 187 historical passes. The Red Zone source filter is intended to use the same five historical gates and select the top four by Overnight Score. Do not close this until browser acceptance confirms the top four appear; preserve any manual pins/dismissals and verify the selected scanner group is Imported CSV.
- [ ] After pulling the latest feature branch and restarting the API, verify `GET /api/watchlists/exclusions` returns 56 entries and that the expected canonical symbols/reasons are present. Do not expect OSW/EVR/MHK to match unless the operator separately adds them.
- [ ] Browser-verify the new Unknown metric summary, sticky qualification table header, and exclusions list scroll viewport.
- [ ] Inspect the full runtime artifact folder `artifacts/runtime-validation/20261103T190356Z`: record assessment `oneMinute.sourceTransport`, `sourceTimestampUtc`, `completedAtUtc`, freshness, and per-symbol stream deltas. Do not infer transport provenance from the symbol subscription table alone.
- [ ] RTO chart screenshot dated 2026-11-03 12:00 PM Arizona is daytime evidence, not the 3:55 PM Arizona EOD cutoff test. It shows 5M Permission NO / neutral 5M structure, 1M WAIT, and repeated PREPARE TO SELL labels; no visible EOD GET OUT event is established. Keep the exact cutoff test open until bars immediately before/at/after 3:55 PM Arizona are captured from the correct study.
- [ ] Keep the historical 20-session RTO boundary audit separate from the live WebSocket test. Existing IEX artifact has 37/40 exact boundaries; three next-session opens remain missing. No nearest-bar imputation and no SIP label.
- [ ] User asks what prior Steps 5–7 meant: see the clarified operator sequence in the handoff and Runtime Acceptance Runbook. These are separate validations, not a request to repeat the scanner export or chase a fixed 248-row count.

---

## 2026-11-03 follow-up — corrected Overnight process, 10:52 scanner export, exclusions and runtime evidence

- [x] Reconciled the new master-branch procedure commit `c14380c867887f98331e382b7dc7049ac1b86894`: Overnight spans Day 1 after-hours plus Day 2 premarket; candidate review starts in the final 60 minutes BEFORE the Day 1 regular-session close. The mistaken 20:00 ET process start is withdrawn.
- [x] Copied the current master source-of-truth procedure, decision data contract, provisional trade-readiness runbook, and Schwab exclusions documentation into `docs/project-control/` on this feature branch. Copied the canonical machine-readable exclusion list into `src/MomentumCommandCenterV2.Infrastructure/Resources/schwab-exclusions.txt`. This is a selective documentation/source sync, not a branch merge.
- [x] Verified `MarketSessionClock` already derives the final-hour evaluation start from the exchange's actual regular close and the versioned calendar. It moves with early closes. Unit tests explicitly cover Nov 27, 2026 early close; visual browser acceptance remains pending.
- [x] Parsed `2026-11-03-WatchListScanner_1052am.csv`: 198 data rows, 198 unique symbols, zero duplicates. The reported 248-row scanner output is still 50 rows larger than the file.
- [x] Applied five historical gates to all 198 rows: 187 pass; 11 fail; 0 unknown. Failures: current ON >=3% (HLF, EVER, XHR, PHG, JAN, AHRT, ALSN); 5D ON <= 20D mean (CTRE, KVUE, POR); Above 20 SMA = 0 (DG).
- [x] Cross-checked all 198 symbols against the canonical 55-symbol Schwab exclusion list: zero overlaps, including OSW/EVR/MHK.
- [x] Fixed the feature branch's persisted exclusion JSON: seeded 55 canonical exclusions and preserved TEST, for 56 entries. Seed commit `2368ab989fe79fd991bf739835167eb956ed17f9` passed CI run [41694304187](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41694304187). Local API must be restarted and queried after pull; runtime count 56 is not yet user-confirmed.
- [x] Inspected uploaded runtime artifacts. RKLX 1M/5M refresh returned one exact bar each and the store contains matching observations with zero missing/mismatched bars; the idempotency flag is false because this capture inserted a new observation, not because a previously persisted duplicate was mishandled.
- [x] Runtime live assessments show SPY 1M uses ALPACA_WEBSOCKET_LATEST_BAR with 545 bars and a fresh 16:31Z source timestamp. RKLX (68 bars, 16:17Z), ASTX (117 bars, 16:24Z) and XRPN (53 bars, 16:28Z) use REST because their latest timestamps were stale at the 16:32Z assessment; bar count alone does not qualify for WebSocket latest-bar authority.
- [x] RTO 20-session artifact confirms Alpaca IEX, 3,965 fetched bars, 37/40 exact boundaries. Missing exact next-session opens: Oct 14 (+4m), Oct 21 (+1m), Nov 3 (+1m). Keep the 20-session result incomplete; do not fill from nearest bars or label it SIP.
- [x] Updated importer success feedback to report actual rendered scanner row count and actual Red Zone card count/symbols. Red Zone subtitle now clarifies imported preview is top four by Overnight Score among the five historical-gate passes; not a final Boring Overnight rank. Code CI passed in run [41693652415](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41693652415).
- [x] Added an explicit Boring Overnight Qualification panel to production and prototype dashboards. It applies the five historical CSV gates to all imported rows, queries MCC's local exclusion API, and leaves current price, 20D average volume, catalyst and chart gates as DATA?; READY remains zero until those gates are implemented and evidenced. JS commit `daf8564d751f27bca40b1c42e745ec62593f70be` passed CI [41695106655](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695106655); styles commit `23d9dafeb6f61f3c2895a0292e1084ccbc9aba5b` passed [41695141848](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695141848).
- [x] Imported scanner CSV is now persisted in browser localStorage (`mcc.scannerImport.v1`) with filename and import timestamp, restored on page reload, and re-renders scanner rows, Red Zone preview and qualification panel. Commit `ae89734ab62c702e4d16a6ba34761de99f56a0f8`; CI [41695693825](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695693825) passed.
- [ ] Browser-verify the qualification panel shows 198 imported / 187 historical pass / 11 fail / 0 unknown / 0 excluded after the latest exclusion seed is active; current price and 20D volume must remain DATA?.

- [ ] Browser-verify import feedback, `tbody#scannerRows`, and actual Red Zone `#redZoneGrid`. `#mcc-static-0278-div` is only the static scanner-table wrapper; it is not the Red Zone element and is not expected to be replaced.
- [x] Superseded by operator correction 2026-11-03: do not require a fixed 248-row count; the current accepted import has 198 rows and all imported rows render in the scanner table.
- [ ] Current price >$5, 20D average daily volume >=500K, catalysts, Daily charts, 5M Permission/structure, and 1M execution evidence are not in this CSV. No Boring Overnight READY/BUY decision is supported yet.
- [ ] Thinkorswim cutoff test remains inconclusive: no bars were available at Arizona 15:54/15:55/15:56 for the supplied RTO/SPY chart views. Bubbles/arrows on other after-hours bars do not establish the exact cutoff bar or correct source study.
- [x] User runtime query returned 56 entries; OSW/EVR/MHK/TEST filter returned TEST only. This matches the current feature-branch JSON (55 canonical + TEST); OSW/EVR/MHK are not in the canonical source list.

## 2026-11-03 — production and prototype clock UI verified

- [x] Added evaluation-start and buy-deadline countdown rows to both `frontend/index.html` (production) and `frontend/prototypeIndex.html`, with matching styles in `frontend/styles.css`.
- [x] Countdown switches to `OPEN` at the exact evaluation-window start using client epoch time; API also reports `overnightEvaluationOpen`.
- [x] Exact-head CI for production markup commit `95c95a5217ce9b3fc168e7f3e94532fd43e82923` passed in run 41681383369; CSS commit `61d08a86e969186a6d4e9468714c0f7120531699` passed in [run 41681395871](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681395871).
- [ ] Browser visual acceptance remains pending.

## 2026-11-03 — V14.5.1 Library baseline checkpoint

- [x] Found the actual 1M V14.5.1 source in the Project Library; the uploaded V14.5 source is materially different and is not a replacement for it.
- [x] Rebased `docs/Charts/1M/MainStudy_V14.5.2.txt` on the V14.5.1 Library source. The rebase commit `d877cf90949ca5e480e45b5c242808ad8a770da5` passed Release build and 182/182 tests in [Actions run 41680840299](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41680840299).
- [x] Added explicit V14.5.1 source provenance and scope comments to the V14.5.2 header in commit `600415fce8b3d7d390e7cf7eebdf4053bfc508ff`; exact-head CI [41681045730](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681045730) passed.
- [ ] TOS compile and matched-bar validation remain mandatory before installing V14.5.2.

## 2026-11-03 — latest verified code checkpoint

- [x] Latest code head `7583f5233c7dbd5d937fd7504218dd0235dc4677` passed exact-head Release build and 182/182 tests in [Actions run 41680258800](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41680258800).
- [x] Overnight Quick Start updated with final-hour evaluation timing, after-hours + next-day premarket definition, and full CSV metric import behavior.
- [x] Thinkorswim/MCC parity audit updated with the separate V14.5.2 candidate source and required TOS validation.
- [ ] Browser import/Red Zone and market-clock visual acceptance remain user-side tasks.



## 2026-11-03 follow-up — scanner importer and full-universe exclusion cross-check

- [x] Compared all 195 scanner symbols against the canonical 55-symbol Schwab exclusion source on master: **zero overlaps**. This is a list-membership result, not live broker-tradability proof.
- [x] Extended the CSV importer to preserve ON win %, 20D average ON %, median ON %, 5D ON %, ON momentum, current ON %, and above-20-SMA in `overnightMetrics`; the existing reason column now displays a compact summary. Commit `649361834984292eab66fa577e960237d6ff1e7e`.
- [x] Executed the importer-equivalent parsing logic against the actual CSV: header row 3, comma delimiter, 195 rows, 195 unique symbols, zero duplicates.
- [ ] Browser upload/Red Zone rendering remains pending. Importer code commit `649361834984292eab66fa577e960237d6ff1e7e` passed build and 182/182 tests in [Actions run 41679681456](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41679681456).
## 2026-11-03 — latest Overnight timing, scanner export, 1M source and market clock

**This dated update supersedes stale status statements below where they conflict.** Detailed 195-row findings: [Overnight Scanner Full-Universe Review — 2026-11-03](Validation/Overnight-Scanner-Full-Universe-20261103.md).

- [x] Located and reviewed the newer November 3 Overnight procedure documents on `master`. The 20:00 ET start proposal is withdrawn. Overnight includes Buy Day after-hours plus Sell Day premarket; candidate review begins in the final 60 minutes of regular trading.
- [x] Defined schedule in exchange-local terms: evaluation starts at regular close minus 60 minutes; buy deadline is the regular close. America/Phoenix display therefore normally shows noon during EDT and 1:00 PM during EST. The schedule follows the calendar's early-close time.
- [x] Implemented API clock fields for Overnight evaluation start, buy deadline, and whether the evaluation window is open. Added two countdown rows to both the production dashboard and prototype header.
- [x] Added DST/standard-time, early-close, premarket, weekend and after-hours tests. Exact-head CI run [41679069855](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41679069855) passed all 182 tests. The first test attempt failed because its early-close fixture was 30 minutes before the evaluation window; the fixture was corrected, not the production logic.
- [x] User uploaded `CurrentStudy_CouldBeWrong.txt` (1,424 lines, V14.5 header). Created a separate candidate `docs/Charts/1M/MainStudy_V14.5.2.txt` (1,456 lines) with exact 5M V4.1.7 Permission inputs, including Daily VWAP, and DST-aware 3:55 PM Arizona EOD mapping (18:55 EDT / 17:55 EST). Original upload preserved unchanged.
- [x] Exact-head CI on source commit `7c265a8777eab3e9f0e2093e89674f98a8f7b75c`, run [41679267305](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41679267305), passed build and 182/182 tests. CI does not compile ThinkScript.
- [x] Parsed the uploaded November 3 scanner CSV: 195 unique ticker rows, no duplicates, metadata preamble intact.
- [x] Full-universe historical-metric pass: 107 rows pass all five available numeric gates; 81 are incomplete due to `loading`; 7 have known gate failures. The file has 195 rows, not the reported 248. No current price or 20D volume columns exist.
- [x] Located canonical 55-symbol Schwab exclusion source on `master`; OSW, EVR and MHK are not in that canonical list.
- [ ] Live Schwab tradability and feature runtime `App_Data/exclusions-watchlist.json` remain unverified; the feature branch local store is not seeded from the canonical 55-list provider.
- [ ] Browser-import/Red Zone acceptance for the 195-row file is pending. The importer source supports the preamble/header and maps Overnight Score, but browser behavior was not run here.
- [x] Superseded by operator correction 2026-11-03: 248 was a prior snapshot count, not a current acceptance target.
- [ ] Current Daily/5M/1M charts, catalyst clearance, price and 20D volume for the full universe remain outstanding.
- [ ] Compile V14.5.2 and normal 5M V4.1.7 in Thinkorswim; run matched-bar EOD and Permission parity diagnostics. Do not install V14.5.2 as authoritative until TOS compile and chart validation pass.
- [ ] Runtime test on 2026-11-03: SPY advanced 5 bars and XRPN 1; RKLX and ASTX did not advance. RKLX idempotency comparison reported false for both timeframes. Preserve raw JSON artifacts with exact SHA/window and inspect source transport/timestamps before sign-off.
- [ ] RTO 20-session output shows three missing exact next-session opens: Oct 14 (+4 min), Oct 21 (+1 min), Nov 3 (+1 min). All displayed previous-session close boundaries were exact. Feed provenance for this new capture is not included in the pasted table; do not label it SIP without inspecting the JSON.
- [ ] Merge remains HOLD. Branch compare reports 1,194 commits ahead / 160 behind and 300 changed files; PR #7 was explicitly closed as incorrectly based on master. Master has the newer Overnight docs and canonical exclusion provider; reconcile selectively before creating a merge PR. Do not wholesale merge or force-update either branch.

## 2026-11-03 — consolidated audit / current status

**Current authoritative reconciliation:** [Full Master To-Do Audit — 2026-11-03](Validation/Full-Master-To-Do-Audit-20261103.md). This pass reviewed all 138 unchecked entries and performed a second dependency pass. Older unchecked entries below are retained as historical chronology; use the consolidated audit to determine current status and avoid treating repeated historical checkboxes as separate open work.

- [x] User ran `Get-Latest-And-Test.ps1` at `0917a90...`: Release build succeeded; 179/179 tests passed, 0 failed, 0 skipped.
- [x] User confirmed the committed 50-row scanner CSV imported successfully and Red Zone updated; MHK, OSW, EVR and EHC populated.
- [x] User accepted prototype clock/transport/header positioning.
- [x] Fixed prototype topbar horizontal overflow for desktop 104px dock and mobile 78px dock (commit `3aac28cf638228fd0901251e59969d08af248e17`). Browser confirmation after latest pull remains open.
- [x] Corrected EOD standard-time constant to 17:55 Eastern in repository 5M V4.1.7 and repository 1M V14.4.11 chart-study copies; daylight remains 18:55 Eastern.
- [ ] Full 1M V14.5.1 Library source was not found in repository/Project files. `docs/Charts/1M/MainStudy.txt` is V14.4.11 and must not be installed as a replacement. Export/upload the full V14.5.1 file before patching the actual full study.
- [ ] Compile both actual studies in Thinkorswim and validate exact before/at/after bars across DST and standard time. Repository/CI cannot compile ThinkScript.
- [x] Shortlist cross-check: OSW, EVR and MHK are all above $5 and have public 20D average-volume statistics above 500K. All three have recent earnings catalysts; none has 5M Permission YES in the supplied Nov 2 screenshots.
- [ ] Exclusion status for OSW/EVR/MHK: Data not found in sources. Obtain complete exclusion list/runtime state and current Daily/5M/1M charts before re-review.
- [ ] Full 248-row Overnight Scanner CSV is not committed. Current CSV contains 50 rows. Obtain full export for import/parse validation.
- [ ] Automatic TOS saved-scan API/webhook is not documented by public Schwab developer materials. Watched-folder CSV ingestion is the feasible automation bridge; not implemented yet.
- [ ] Alpaca SIP recent-data requests were denied with HTTP 403. IEX is the usable fallback; regular-session discovery/stream freshness and recovery acceptance remain open.
- [ ] Boring Overnight hard-gate/history/snapshot/quantile components exist, but end-to-end READY remains blocked by daily liquidity source, complete exclusions, catalyst classification, matched TOS states and full GTC_EXT outcomes. Existing OpportunityQualityAssessor is not the versioned Red Zone top-four ranker.
- [ ] Calculated Limit Sell remains DATA?/not calculated; no validated 20-window GTC_EXT walk-forward target or ATR parity.
- [ ] Exact-head CI passed on `89b9fe73408cee06f0ddfedc1486b7038d335a2a`, Actions run [41575143773](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41575143773).
- [ ] See audit document for the full workstream-by-workstream second-pass disposition and operator steps.

---

## 2026-11-03 — Scanner CSV import retry

The synced export is now present at `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`. It contains two metadata preamble lines (`Watchlist Scanner`, `Results`) followed by the actual comma-delimited header on line 3. The importer previously assumed line 1 was the header, which caused the reported missing Symbol/Ticker error.

`frontend/app.js` now searches the first 25 nonblank rows for a normalized Symbol/Ticker header, then imports the data rows after it. The uploaded file was parsed against the same delimiter/header rules: comma delimiter, header row 3, **50 nonblank ticker rows**, first three MHK/OSW/EVR with Overnight Scores 73.42/70.97/68.25. Browser interaction has not yet been exercised, so final UI acceptance remains pending. Do not re-upload or edit the CSV to remove its preamble.

Current importer fix commit: `b05c63713452cbbbb5632a87c639f655ad027e73`. GitHub Actions run 41549757022 is still running at the time this note was added.

## 2026-11-02 — latest user follow-up: script, CSV, prototype, 1M parity, overnight

### Completed source changes
- [x] Fixed PowerShell refspec interpolation in `scripts/Get-Latest-And-Test.ps1` using `${Branch}` before the colon.
- [x] Hardened the scanner CSV importer for UTF-8 BOM, comma/semicolon/tab separators, quoted fields, Symbol/Ticker header variants, and the Overnight Score alias.
- [x] Repositioned the prototype market-session clock immediately beside the title inside the title parent; corrected the transport label's inherited yellow background.
- [x] Audited Library 1M V14.5 against normal 5M V4.1.7 and documented the missing 5M Daily VWAP condition in the reconstructed 5M raw Permission.


### Additional 1M/5M EOD parity defect
- [x] Source audit found V14.5.1 uses 15:55 Eastern for a 3:55 PM Arizona cutoff; this fires 2–3 hours early. V4.1.7 uses 19:55 Eastern during standard time; this is two hours late in Arizona. Correct mapping is 18:55 EDT / 17:55 EST.
- [ ] Correct both studies with the same DST-aware Arizona cutoff and validate exact bars immediately before/at/after the cutoff across both DST regimes. Do not treat screenshot EOD bubbles as proven output from a particular study until matched-bar diagnostics isolate their source.


- [x] Clarified the current full 1M Library baseline: `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (V14.5.1). The later V14.4.10 Library artifact is audit-only, not the full execution study. V14.5.1 retains the two source-level parity defects.

### Remaining validation / strategy disposition
- [x] Original and updated November 5 Overnight Scanner CSVs are present on the active branch and reviewed. See `docs/Validation/Overnight-Scanner-Review-20261105.md`; browser import acceptance remains separate.
- [ ] Run `Get-Latest-And-Test.ps1` locally after pulling the fix; record the Windows PowerShell execution and local Release build/test output.
- [x] Exact-head GitHub Actions run 41546963701 passed the .NET build/test on commit `825276a844a5b3079aa3ea4bd1cb8ccbd90e283b`.
- [ ] Browser acceptance remains open for prototype clock/transport label and scanner CSV import using the original export.
- [ ] Keep V14.5.1 as current full 1M baseline until a targeted parity revision adds the 5M Daily VWAP condition, compiles in TOS, and matches V4.1.7 on exact completed 5M bars. Preserve the separate 1M VWAP execution gate.
- [x] Reviewed November 2 MHK/OSW/EVR charts: none qualifies for a new overnight BUY/HOLD. The supplied charts are 1D/5m (not 1m): EVR and OSW show visible EOD GET OUT bubbles on their 5m charts, but the generating study/source has not been isolated; their top 5M labels remain WAIT/Permission NO/BEARISH; MHK shows 5M WAIT/Permission NO/neutral structure and no visible EOD GET OUT. No 1M chart was supplied; MHK's current ON move is 3.13%.
- [ ] Calculated Limit Sell remains DATA?/not calculated until 20 complete GTC_EXT windows, walk-forward target quantile/touch evidence, and ATR parity are available. Do not substitute the +0.5% heuristic.

Related: [Active Work Checklist](Active-Work-Checklist-20261102.md), [1M V14.5.1 parity audit](Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).

---

## 2026-11-02 — after-hours WebSocket snapshot

- [x] API health was OK.
- [x] WebSocket connected and state was SUBSCRIBED; SPY, RKLX, ASTX, XRPN were acknowledged.
- [ ] Regular-session delivery remains unverified. User ran the status check after 3 PM Arizona; lastBarUtc was null and received/persisted counts were zero. This is expected after-hours and is not a delivery failure. Run the §1A five-minute before/after test during regular market hours.

---
## 2026-11-02 — Technical Workflow, Overnight Scanner, Red Zone

Current operator entry point: [Technical Workflow](Technical-Workflow.md). The [Runtime Acceptance Runbook](Runtime-Acceptance-Runbook.md) remains the detailed runtime acceptance source of truth; do not run every specialized procedure after every commit. Follow the Technical Workflow's standard update/build/restart/API smoke test, then run only feature-specific acceptance tests when relevant.

### Completed
- [x] Added `scripts/Get-Latest-And-Test.ps1`: clean-tree guard, fetch/switch/fast-forward pull, Release restore/build/test, and exact local SHA output.
- [x] Latest source head `237fcbfc71f9b03bda64b1e6047d61355c849447` passed Release build and 179/179 tests with 0 warnings/0 errors in [Actions run 41540235754](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41540235754).
- [x] Added `docs/Technical-Workflow.md` with the operator sequence, stop/restart steps, API endpoint order, expected status fields, and when to use the detailed runbook.
- [x] Fixed prototype header structure so `system-readiness` is a sibling of `topbar-brand`, matching the production header layout.
- [x] Fixed `marketTransportLabel` word wrapping so it wraps on spaces rather than splitting into one character per line.
- [x] Fixed scanner CSV import to refresh Red Zone immediately, not just the scanner table.
- [x] Documented existing automatic Red Zone behavior: live 1M/5M card assessments refresh every 15 seconds; Alpaca persisted discoveries refresh every 30 seconds when the live discovery group is selected.
- [x] Added Overnight Scanner Quick Start and updated Dashboard Quick Start with the separate overnight workflow.
- [x] Added November 2 scanner review with the top visible candidates MHK, OSW, EVR and current catalyst research.

### Overnight Scanner — current shortlist
- [x] Reviewed screenshot: 248 results, first 50 displayed, sorted by Overnight Score.
- [x] Shortlist for chart review: OSW first, EVR second, MHK third. MHK has the highest raw scanner score but current ON is 3.13% and the recent price run makes it the hottest/least boring of the three.
- [ ] Review Daily and 5M charts for OSW, EVR, MHK; verify price >$5, 20D average volume >=500K, exclusion status, catalyst risk and current structure.
- [ ] Preserve complete CSV export if available; current screenshot only shows the first 50 of 248.

### Red Zone — implementation truth and remaining work
- [x] CSV import immediately re-renders Red Zone from imported candidates.
- [x] Candidate market assessments refresh every 15 seconds while the page is visible.
- [x] Alpaca live discovery refreshes every 30 seconds only when the live discovery scanner group is selected.
- [ ] Automatic Thinkorswim Overnight scan ingestion is not implemented; no verified TOS scanner API integration exists. CSV export/import remains the handoff.
- [ ] Validate live Alpaca discovery collector freshness/health after recent-SIP entitlement is available. Current endpoint is SIP-based and the user's subscription denied recent SIP requests with HTTP 403.
- [ ] Complete the separate Boring Overnight qualification engine and versioned opportunity ranking; Red Zone technical assessment does not certify overnight history, liquidity, exclusions or catalysts.

### Current browser acceptance
- [ ] Pull latest branch and hard-refresh prototype. Verify System Readiness on the right side of the header, transport label word wrapping, and no horizontal overflow.
- [ ] Verify CSV import updates Red Zone immediately and card values continue refreshing.
- [ ] Regular-session WebSocket delivery acceptance remains a separate market-hours test; the November 2 after-hours SUBSCRIBED result was not a delivery pass.

### Key new documents
- [Technical Workflow](Technical-Workflow.md)
- [Overnight Scanner Quick Start](Overnight-Scanner-Quick-Start.md)
- [November 2 Overnight Scanner Review](Validation/Overnight-Scanner-Review-20261102.md)
- [Active Work Checklist](Active-Work-Checklist-20261102.md)

---
## 2026-11-02 — latest runtime evidence and active checklist

Current actionable checklist: [Active Work Checklist — 2026-11-02](Active-Work-Checklist-20261102.md). This is the current operational summary; older dated sections below are retained as historical evidence.

- [x] IEX override diagnosed: local secrets.json overrode appsettings.json.
- [x] SIP selection confirmed, but Alpaca recent SIP request returned HTTP 403 entitlement denial. IEX restored and returned VALIDATED_DATA_RETURNED with 62 bars.
- [x] User visually accepted Main Chart layout with no horizontal overflow.
- [x] Corrected Main Chart timestamp display to show completedAtUtc (bar close) and the bar interval, rather than showing only the bar-open timestamp.
- [x] Recorded current TOS/MCC INO readings and the distinction between the live EXT label and historical ExtensionATR plot.
- [x] User reports Schwab Trader API — Individual approval. App creation, product attachment, callback, app-level approval and OAuth remain open.
- [x] Defined RTO audit: exact 40 close/open boundary timestamps across 20 sessions. Prior 38/40 result is IEX; SIP comparison is blocked by the explicit 403.
- [x] Release build and 179/179 tests passed on head `83cd9780daa9f8e94628e8d24f4bc6ab4b03c415` in Actions run [41514111035](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41514111035).
- [ ] Measure actual completed-bar age from live API fields during regular market hours.
- [ ] Complete matched TOS parity, Red Zone ranking, Boring Overnight source/target validation, WebSocket and collector recovery acceptance.
- [ ] Continue Schwab read-only integration after app registration and callback configuration.

---
## 2026-11-02 — user's live IEX response and final Main Chart layout pass

- User confirmed `src/MomentumCommandCenterV2.API/appsettings.json` contains `Feed: sip`; `appsettings.Development.json` is absent; launch profile sets only `ASPNETCORE_ENVIRONMENT=Development`.
- Local `/api/providers/status` still reported `configuredFeed: iex`, and live assessment also reported IEX. The launch profile shown does not explain that override. Development User Secrets and process/user/machine environment variables have higher precedence than JSON. New provider status now reports `configuredFeedSource` and `feedConfigurationSources`, and `configuredFeed` comes from the actual adapter instance.
- REST adapter, WebSocket worker, and live assessment now default to SIP when the setting is absent; adapter feed is normalized and exposed. Added a test for missing-feed configuration defaulting to SIP. Explicit higher-precedence IEX configuration can still override.
- Main Chart prototype markup has been rearranged per user: ticker at left, Main Chart title/description in the middle, Live Opportunity Classification at right. Chart is ~2/3 desktop width; right side is 2x3 metrics (1M Close / Daily VWAP; ATR14 / Relative Volume; ATR Extension / TBD). It stacks at narrower widths. This layout is scoped to `prototypeIndex.html`, not the production page.
- Close, Daily VWAP, ATR14 now display five decimals; the exact source timestamp is shown in Arizona time and UTC below 1M Close.
- Reviewed user commit `e7299ddd`: `RTO-SIP-20261101.json` content explicitly says `feed: iex`, 38/40 exact boundary bars, missing Oct 14 and Oct 21 opens, 18/20 overnight pairs. The filename does not establish SIP.
- Thinkorswim how-to / study: [Thinkorswim-MCC-Extension-Parity-20261102.md](Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).
- [x] Latest code source head `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae` passed Release build and **179/179 tests** in GitHub Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777). The current head at that time, `4443506be982d0ff6726992c7b2294d97871bad2`, also passed in run [41504521572](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504521572): Release build succeeded, 179/179 tests passed.
- [ ] After local rebuild/restart, inspect `configuredFeedSource` and `feedConfigurationSources`. If User Secrets is the effective source, inspect `dotnet user-secrets list --project src/MomentumCommandCenterV2.API`; if EnvironmentVariables, inspect `MarketData__Alpaca__Feed`. Do not expose credentials.
- [ ] Confirm observed 1M and 5M feeds are SIP, then repeat RTO audit.
- [ ] Compare TOS and MCC using same exact date/time, with five-decimal close/VWAP/ATR14 and same session scope.
- [ ] Browser-check the new Main Chart layout and no horizontal overflow at desktop/mobile sizes.

## 2026-11-02 — latest user commits, feed source diagnosis, and Main Chart re-layout

### User commits reviewed
- [x] Reviewed two commits ahead of the previously verified head: RTO JSON update and a substantial `frontend/prototypeIndex.html` update.
- [x] Preserved the user's prototype edits and applied the requested Main Chart layout on top.
- [x] Important evidence correction: file `RTO-SIP-20261101.json` is still `feed: "iex"`, not SIP. It records 3,949 fetched bars, 38/40 exact boundaries, two missing opens (Oct 14 and Oct 21), and 18/20 valid overnight pairs. Its filename does not establish SIP provenance.

### Latest fixes in progress / committed source
- [x] API provider status now reports the effective feed from the Alpaca adapter and the configuration providers that supplied `MarketData:Alpaca:Feed`. This should reveal whether IEX came from an environment variable, user secrets, JSON provider, or another override.
- [x] Changed REST adapter, WebSocket worker, and live assessment fallback default from IEX to SIP. Missing configuration no longer silently selects IEX.
- [x] Added an adapter test proving missing feed configuration defaults to SIP.
- [x] Main Chart now has three header columns: ticker chip, Main Chart title/description, and Live Opportunity Classification.
- [x] Chart occupies about two-thirds of the desktop content width. Five existing metrics plus a clearly marked TBD slot are arranged in a 2-column × 3-row grid to the right. Layout collapses responsively rather than forcing horizontal scrolling.
- [x] Close, Daily VWAP, and ATR14 are now shown at five decimal places; diagnostic text uses the same precision.
- [x] Added [Thinkorswim / MCC Extension Parity](Validation/Thinkorswim-MCC-Extension-Parity-20261102.md), including a ThinkScript study and exact-bar worksheet.

### Feed acceptance — still requires user's local runtime
The user supplied `appsettings.json` with `Feed: sip`, but both live endpoints still report `iex`. `launchSettings.json` contains only `ASPNETCORE_ENVIRONMENT=Development`; no explicit feed override is shown there. The cause is not yet proven. The new `/api/providers/status` diagnostics report:
- `configuredFeed`: feed cached by the actual Alpaca adapter instance
- `configuredFeedSource`: highest-precedence configuration provider with that key
- `feedConfigurationSources`: every provider contributing a value for that key, in provider order

After rebuild/restart, inspect these fields. Environment variables and user secrets can override appsettings. If the new source says `appsettings.json` but feed remains IEX, confirm the actual process content root/output directory and that the latest API assembly is running. If it identifies EnvironmentVariables, inspect only the named feed variable `MarketData__Alpaca__Feed` (do not expose credentials).

### Thinkorswim parity status
At the user's reported 11:26 bar: close approximately $2.00 in both; MCC previously showed VWAP $1.838, ATR14 $0.013, extension 11.89; TOS showed EXT 22.92. The prior MCC UI rounded inputs too aggressively for a conclusive ratio check. The UI now shows five decimals. MCC VWAP is HLC3 volume-weighted using RTH 1M bars; ATR14 is SMA(TrueRange,14). The new parity note tells the user how to expose ATR14 and extension as ThinkScript plots and inspect the historical candle with the Data Box. No numerical parity conclusion is made yet.

### Remaining acceptance
- [x] Release build and 179/179 tests passed on source head `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae` (Actions run 41504217777). The latest docs head was also verified by run 41504521572 (179/179).
- [ ] Re-query `/api/providers/status`; capture `configuredFeed`, `configuredFeedSource`, and `feedConfigurationSources`.
- [ ] Verify live assessment `configuredFeed`, top-level `feed`, `oneMinute.feed`, and `fiveMinute.feed` are SIP.
- [ ] Confirm SIP entitlement / provider delivery; do not infer from config alone.
- [ ] Compare TOS/MCC close, VWAP, ATR14, and extension on the same date and exact completed 1M timestamp with five-decimal inputs.
- [ ] Browser-check desktop and mobile Main Chart layout and verify no horizontal overflow.
- [ ] Re-run RTO boundary audit only after observed SIP is confirmed. The latest committed RTO JSON is IEX, 38/40 exact, with Oct 14 and Oct 21 opening gaps.
- [ ] Confirm whether the new TBD metric should remain a placeholder or be assigned a defined metric. Do not invent one.

### [Uncertainties]
- Local API still reports IEX despite the supplied JSON showing SIP; source provenance is pending the new endpoint diagnostics.
- SIP entitlement and actual SIP delivery are not established.
- The exact date/timezone of the reported 11:26 comparison and the unrounded inputs are not yet available.
- Browser rendering has not been observed from the user's machine.

---

## 2026-11-02 — Extension input visibility, test regressions, dock correction, and SIP feed

### Completed source work
- [x] Corrected the new supplied-extension regression test to evaluate an existing position (`hasPosition: true`), because hard-exit flags are position-management outputs.
- [x] Corrected the TradeAnalyzer `StrongSnapshot` test fixture: VWAP is now 0.25 below close rather than 10 below close. The old fixture accidentally represented a 10-ATR extension under the revised VWAP-distance metric and was correctly rejected by the engine. No production entry threshold was relaxed.
- [x] Centralized `Abs(close - Daily VWAP) / ATR14` in `V2SignalEngine.CalculateAtrExtension`; live technical calculation and fallback now call the same method. Added positive-distance, negative-distance, and zero-ATR tests.
- [x] Exposed effective `configuredFeed` through `GET /api/providers/status` and live assessment, plus separate observed `oneMinute.feed` and `fiveMinute.feed` values.
- [x] Changed committed API `appsettings.json` default to `sip`; added an adapter test that proves the configured SIP value is sent and returned without IEX fallback. Provider entitlement is still an external runtime check.
- [x] Prototype dock now places **INTRADAY MOMENTUM** left of the toggle, **BORING OVERNIGHT** right of it, and **WORKSPACE** directly beneath the toggle.
- [x] Prototype Main Chart now displays 1M Close, Daily VWAP, ATR14 (price units), 5M RVOL, and normalized ATR Extension. Red Zone prints the four 1M extension inputs. The `11.20 ATR` shown previously was the normalized extension, not raw ATR14.
- [x] Added [Live Extension and Feed Diagnostics](Validation/Live-Extension-and-Feed-Diagnostics-20261102.md) with formula, provenance, and acceptance instructions.

### Runtime acceptance still open
- [x] Verified source commit `c0412da1a5e4c40f8853bd07d5791277fc3c7f22` with GitHub Actions run [41491616284](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41491616284): Release build passed and 178/178 tests passed. This validates that source commit; check CI again after subsequent source changes.
- [ ] After a full API stop/rebuild/restart, verify `/api/providers/status` reports `configuredFeed: "sip"`, then verify live assessment reports `configuredFeed: "sip"` and observed 1M/5M feeds of `sip`. If configuredFeed is still IEX, inspect `appsettings.Development.json`, `MarketData__Alpaca__Feed`, Visual Studio launch environment, and the actual running API/output directory. If SIP is configured but the observed feed is IEX, capture the sanitized response; do not merge feed histories.
- [ ] Compare MCC and Thinkorswim on the same symbol and completed 1M timestamp: close, Daily VWAP, ATR14, and normalized extension. Numerical parity is not yet proven.
- [ ] Browser-check the prototype dock and five Main Chart metric cards at desktop/mobile widths.
- [ ] Run the RTO boundary audit using observed `feed=sip`. The previous 38/40 evidence was IEX; October 14 13:30Z and October 21 13:30Z exact opens remain missing from that IEX response.
- [ ] Confirm Alpaca SIP entitlement and actual boundary coverage. Configuration alone is not proof of entitlement or SIP data delivery.

## 2026-11-01 — Prototype header placement

- [x] Move `#system-readiness` inside the same `.topbar-brand` container as the product title/tools, on the right side; keep `#marketSessionClock` above the readiness heading. Corrected escaped newline text in the markup. Production `frontend/index.html` remains untouched.
- [ ] Browser-check the prototype at desktop and mobile widths for right alignment, sensible wrapping, and no overlap/clipping. Do not promote to production without approval.

---

## 2026-11-01 — Red Zone automatic/manual design and current-state audit

### Current source behavior (verified in `frontend/app.js`)
- Live discovery rows are currently sorted by absolute daily percentage change, not by a composite opportunity/excitement score.
- Manually pinned symbols are placed before ranked scanner rows. There is no Manual/Automatic mode toggle.
- Each displayed card separately requests a live assessment and shows the 5M score/permission, 1M execution state, extension, freshness, decision reason and a 1M price sparkline.
- Sound alerts are already user-toggleable and fire when a new symbol enters the four visible slots.
- Current ranking does not include relative volume, absolute liquidity, participation quality, acceleration, or an explicit low-volume penalty. The current Red Zone therefore does **not** yet implement the requested NFL Red Zone-style behavior.
- The independent 5M score is not EXECUTION READY. Preserve the separate 1M entry gate; Red Zone is prioritization/attention, never an order signal.

### Required Red Zone work — add to active P1
- [x] Add a visible **Manual / Automatic** toggle at the top of Red Zone. Manual mode uses the user's ordered pinned symbols; switching modes preserves pins and dismissals. Automatic mode currently shows the top four imported scanner-score candidates after historical gates; the full live Opportunity Quality ranker remains open below.
- [ ] Implement a deterministic, versioned **Excitement / Opportunity Quality** rank separate from the 5M Permission score and the 1M EXECUTION READY decision.
  - Candidate-universe boundary: current Alpaca top-gainers/top-losers discovery is bounded, not a complete market-wide scanner. Automatic mode must display its source/pool scope and may claim only the best four within that available pool until full-universe coverage is separately validated.
- [x] Display the currently available 5M same-time-of-day RVOL on every Red Zone card, label it BELOW V2 THRESHOLD when the existing V2 RVOL criterion fails, and add the participation caution to the card's Decision Explanation. This is a visible participation diagnostic, not a ranking input or 1M RVOL.
- [ ] Incorporate 5M same-slot RVOL plus validated 20D average daily volume/liquidity and freshness into the automatic rank. Do not invent 1M RVOL or spread. A high-setup/low-participation symbol may still make the top four, but must be penalized and explicitly explained.
- [ ] Include live 5M authority/score, 1M execution state, momentum/price acceleration, extension/risk, and source freshness. Missing/stale required inputs must lower confidence or yield DATA?; never substitute zero or a stale score.
- [ ] Show a concise **Decision Explanation** on every Red Zone tile: why it is in the top four, strongest supporting evidence, limiting factor(s), and why it outranks/does not outrank a competing candidate. Explicitly call out cases such as EXECUTION READY + very low RVOL.
- [ ] Apply a restrained semantic visual treatment to the tile border/left accent: green = strong, well-supported opportunity; cyan/blue = developing/monitor; amber = caution such as weak participation or extension; red = hard block/confirmed invalidation; neutral = DATA?/unknown. Always include text/reason labels so color is not the only signal.
- [x] Research UI/accessibility direction: prefer a narrow left-edge accent (or restrained border) plus a short status tag, rather than tinting the entire tile. Keep the color mapping consistent; pair every color with text/icon/reason and preserve readable contrast. This is a design recommendation, not yet implemented or browser-tested.
- [ ] Automatic mode must re-rank on new completed market evidence, avoid unnecessary flicker with configurable minimum hold / rank-change hysteresis, and show last-evaluated source timestamps. Sound alerts remain separately toggleable and should alert only on meaningful entry/re-entry to the four slots, not every refresh.
- [ ] Add unit/API/UI acceptance tests for mode persistence, manual ordering, automatic top-four selection, low-volume explanation/penalty, stale/missing data, stable ordering, border semantics, and sound-alert deduplication. Do not promote to production until prototype browser acceptance.

### Design references
- W3C WCAG 2.2, SC 1.4.1: color must not be the only means of communicating status: https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html
- SAP Fiori semantic-color guidance: define meanings consistently and add a text status indicator: https://www.sap.com/design-system/fiori-design-web/v1-108/foundations/best-practices/ui-elements/how-to-use-semantic-colors
- VA.gov Design System card guidance: use status tags with accessible colors and never rely on color alone: https://dev-design.va.gov/5462/components/card

### Ranking safety
Do not rank by raw percentage change alone. Do not equate the highest 5M score with the most exciting trade. Do not let an EXECUTION READY label conceal weak volume, wide spread, stale data, extreme extension, or a failed 5M authority gate. The Red Zone should direct attention and explain trade-offs; the user retains the decision.

---

## 2026-11-01 — Header clock, markup IDs, RTO boundary audit, and operating guidance

### Completed source work
- [x] Added `MarketSessionClock` API service using `UsEquitiesSessionCalendar`; the header clock is not a browser-side guessed calendar. Corrected the New Year's rule against the official NYSE calendar: January 1, 2028 falls Saturday and NYSE observes no prior-Friday closure; bumped the calendar ruleset to V2.
- [x] Static markup audit: all 628 opening elements in `frontend/index.html` now have unique IDs (628/628; no duplicates).
- [x] Added a live exchange-session countdown to the dashboard header. It changes between **Remaining time in regular market**, **Remaining time in After-Hours**, and **Until Market opens**. The API uses America/New_York and the versioned MCC calendar; it does not guess when the clock API is unavailable.
- [x] Moved `#workspace-toolbar` into `.topbar-brand`, below the brand/tools area, and tightened the toolbar/header spacing.
- [x] Added stable, unique `id` attributes to every static element in `frontend/index.html` that previously lacked one (454 IDs assigned; existing IDs preserved).
- [x] Extended the 20-session capture response with an exact 40-boundary audit. For each expected close/open timestamp it distinguishes provider-response presence from persisted-store presence and reports the nearest provider timestamp.
- [x] Added a dashboard table rendering the exact boundary audit. This is the actual exact-timestamp provider/store diagnostic; the older ticker-vs-SPY post-market comparison is now correctly labelled a control test, not a full provider coverage audit.
- [x] Added clock tests for regular session, after-hours, pre-market, weekend, Thanksgiving, and the post-DST Eastern offset.
- [x] Compared the RTO 2026-10-02 through 2026-10-30 session dates and UTC boundaries with the official NYSE calendar: no holidays/early closes occur in this sample; normal 09:30–16:00 ET sessions produce the 13:30Z open and 19:59Z final-minute bar starts.
- [x] Corrected the calendar's incorrect 2027-12-31 closure assumption for Saturday New Year's Day 2028; added July 3, 2026 observed-holiday and July 3, 2028 early-close fixtures. Official reference: https://www.nyse.com/trade/hours-calendars.

### RTO — 2026-11-01 capture result supplied by user
- [x] Recorded that RTO returned 4,048 1m bars, 0 new writes, 18/20 valid close/open pairs, and 2 missing next-session open bars.
- [x] Re-run RTO capture and inspect `boundaryCoverageAudit`: 38/40 boundaries are exact; 2026-10-14 13:30Z is absent from the provider response (nearest 13:34Z), and 2026-10-21 13:30Z is absent (nearest 13:31Z). Store agrees with provider; this is not a persistence loss.
- [ ] Compare those two RTO IEX gaps against an independently entitled SIP or validated alternative-provider response. Do not substitute a neighboring minute or mix feeds in one series. November 2 follow-up still reports `feed=iex`; it is not an SIP comparison.
  - Exact local capture procedure: `docs/Runtime-Acceptance-Runbook.md` §1B. The endpoint uses configured feed; verify response `feed=sip` to rule out fallback to stored IEX.
- [ ] Confirm provider plan/feed entitlement and representative-symbol boundary coverage before treating any 20-session sample as qualified.
- [x] Fixed xUnit2031 in `UsesJulyThird2028EarlyClose` using `Assert.Single(boundaries, predicate)`; keep the test enabled. Release build and 178/178 tests passed on source commit `c0412da1a5e4c40f8853bd07d5791277fc3c7f22` (Actions run 41491616284).
- [x] Updated prototype dock labels: INTRADAY MOMENTUM left of the toggle, BORING OVERNIGHT right of it, and WORKSPACE beneath the toggle. Browser visual acceptance remains pending.
- [x] Corrected MCC 1M extension source calculation to `Abs(close - Daily VWAP) / SMA(TrueRange, 14)` and centralized the formula for live calculation/fallback; added formula and supplied-extension regression tests. Raw close, Daily VWAP, ATR14, and extension are now exposed in the prototype.
- [ ] Validate corrected 1M extension against Thinkorswim on matched symbol/time/bar. Confirm Daily VWAP and bar timestamp parity before accepting numeric parity.
- [ ] Connect source-validated catalyst status, daily SMA20/average volume, reconciled Schwab exclusions, and current authoritative 5M/1M evidence before allowing any Boring Overnight READY evaluation; unknown or stale inputs must fail closed.

### Strategy policy decision
- [x] Preserve the existing Boring Overnight thresholds for now. Do not retune from one incomplete 18/20 RTO sample. The 20-pair sample is a minimum operational window, not statistical proof; validate on a larger point-in-time, corporate-action-aware, walk-forward dataset with costs and outliers before changing thresholds.
- [x] Recorded the installed 5M Command Center V4.1.7 source, 5M Authority Diagnostic V2 source, and 1M Command Center V14.4.10 source in `docs/Validation/Installed-5M-Study.txt`, `Installed-5M-Authority-Diagnostic.txt`, and `Installed-1M-Study.txt`. Their repository blob SHAs are recorded in the handoff. Do not reopen study identification/source collection.
- [ ] Retain only matched-bar output parity replay between the documented installed studies and MCC; this is distinct from identifying which studies are installed.

### Monday 2026-11-02 user runtime acceptance
- [x] Added a specific 07:30 Arizona / 09:30 Eastern WebSocket acceptance procedure and PowerShell command to `docs/Runtime-Acceptance-Runbook.md`.
- [ ] User must run the procedure while the local API is running during regular market hours. Subscription acknowledgement alone is not acceptance; per-symbol completed bar counts and timestamps must advance.

### Operating model
- [x] Documented the current workflow: run the MCC API locally (normally Visual Studio F5) and use its localhost-hosted dashboard in the browser. Desktop installer/packaging is not implemented; no remote hosting or standalone .exe is implied.

---


## 2026-10-31 — Consolidated active To-Do and 20-session capture update

### Boring Overnight — current implementation (GTC_EXT target-touch implementation is tracked once under P1; Thinkorswim parity is tracked once under P1)
- [x] Add provider-backed `POST /api/boring-overnight/capture-history` to fetch and persist 1-minute bars and calculate the latest 20 regular close/open pairs.
- [x] Add versioned MCC US-equities session rules with timezone-derived DST, weekday/holiday exclusions, and common early-close handling.
- [x] Add dashboard action: **Refresh/Capture 20-session close/open calculation**; show metrics, pair-level values/timestamps, missing pairs, provider/feed, bar counts and calendar version.
- [x] Add calendar tests for 20 pairs, weekend, DST, Thanksgiving, Good Friday and Christmas Eve.
- [x] Verified source implementation commit `a0bf119c68f688a017e179e28a0bf145727f734f` with GitHub Actions run 864: .NET Release build succeeded with 0 warnings/0 errors; 165/165 tests passed.
- [x] Pull the branch, hard-refresh dashboard, click the new capture action for RTO, and save the JSON/runtime result; exact-boundary audit evidence is recorded above.
- [x] Compare generated session dates and UTC boundaries against the official NYSE calendar for the RTO 2026-10-02–2026-10-30 sample.
- [x] Certify the published NYSE 2026–2028 holiday and early-close dates with regression fixtures. Refresh the supported calendar horizon when NYSE publishes new years; this is not a live calendar feed.
- [ ] Compare the two RTO IEX missing opens against an independently entitled SIP or validated alternative provider; do not mix feeds in one return series.
- [ ] Confirm Alpaca configured feed entitlement and returned 1-minute bar coverage for each expected close/open boundary; any missing boundary keeps all rolling metrics DATA?. November 2 evidence: 38/40 exact; Oct 14 13:30Z and Oct 21 13:30Z still absent.
- [ ] Connect validated catalyst source, current daily SMA20/average volume, exclusions reconciliation, and current authoritative 5M/1M states before any READY evaluation.
- [x] Record the installed Thinkorswim study sources and diagnostic files; see the three `docs/Validation/Installed-*` files.

### P0 — current runtime/release acceptance
- [x] GitHub Actions run 864 verified source commit `a0bf119c68f688a017e179e28a0bf145727f734f`: Release build 0 warnings/0 errors; 165/165 tests passed. Documentation updates after that source commit require the normal docs-head workflow.
- [ ] During regular market hours, verify WebSocket bar delivery for SPY, RKLX, ASTX and XRPN using advancing per-symbol bar counts, lastBarUtc, receivedBars and persistedBars—not subscription acknowledgment alone.
- [ ] Confirm eligible 1M assessments select WebSocket only after subscription acknowledgment, at least 50 completed persisted bars, freshness, correct feed and provenance; 5M Permission remains REST-backed.
- [ ] Verify independent 5M score status CURRENT, six criteria populated, score 0–6, Permission iff score >=5; verify 1M and combined entryAllowed separately.
- [ ] Complete browser acceptance of the dashboard: independent 5M score/Red Zone, new Overnight 20-session capture, Calculated Limit Sell DATA? state, Morning Brief modal/history, replay, responsive layout and sound permission.
- [ ] Run stored-bars evidence review for RKLX exact requested window/provider/feed/timeframes; the automated provider-to-store comparison matched returned bars, but inspect saved raw JSON timestamps/content when needed.
- [ ] Complete controlled collector outage/recovery, clean API restart/checkpoint reload, and sleep/wake acceptance on the target host. Baseline checkpoint hash alone is not a recovery test.

### P1 — strategy/data correctness
- [ ] Matched Thinkorswim state replay: same symbol/date/bars/study version and explicit 1M/5M state diagnostics; OHLCV-only exports cannot prove Permission/READY/RUNNER/WANING/SWING/GET OUT, expiry, re-arm or re-entry.
- [ ] Extend existing `MorningBriefSnapshotStore` lineage: it already appends immutable report-version + SHA-256 snapshots and extracts market-bar source watermarks. Add news/discovery watermarks, per-source request windows/raw-response references and retention, a full dependency manifest, and outcome linkage.
- [ ] Build complete GTC_EXT 07:00–20:00 ET historical coverage and classify missing intervals; calculate target touch, time-to-touch and adverse excursion separately from close/open returns.
- [ ] Empirically select sell-target quantile using pre-decision history and walk-forward validation; no arbitrary ATR multiplier and no promotion of +0.5% heuristic as validated target.
- [ ] Integrate Calculated Limit Sell with fresh reference close, selected target quantile, ATR context/parity, target-touch evidence and coverage status; actual fill claims require broker fill records.
- [ ] Connect validated catalyst/news/event source with source health, coverage horizon, timestamps and event classification. Empty/unavailable feed remains UNKNOWN.
- [ ] Validate daily history for price, SMA20 and 20D average daily volume; validate 20 valid close/open pairs, sample count, gaps, exact feed and corporate-action handling.
- [ ] Seed/reconcile the complete Schwab exclusion list into MCC's local `App_Data/exclusions-watchlist.json` and integrate it consistently across scanner and qualification paths. Current `ExclusionWatchlistStore` is local-only Add/Remove storage with no initial source-list seed or Schwab account sync.
- [ ] Complete Boring Overnight scanner-universe provenance and immutable candidate decision snapshots; never infer READY from preview rankings or caller assertions.

### P2 — providers, integrations and product delivery
- [ ] Validate production scanner/provider account plan, feed entitlement, full-market coverage, licensing, rate limits, historical backfill, retention and representative-symbol behavior. Current Alpaca mover source is bounded top gainers/losers, not complete-market breadth.
- [ ] Complete Schwab read-only integration after Individual Developer/API approval, app registration, callback URL and secure server-side OAuth. No credentials in Git/chat; order submission remains disabled.
- [ ] Decide desktop packaging/startup: target platform, installer/update model, local API lifecycle, data directory/backup and browser launch behavior. Browser-hosted API is current behavior; no .exe claim.
- [ ] Decide long-term raw-bar/discovery/news retention and backup/restore policy.

### P3 — older validation follow-ups retained as active
- [ ] Re-run PMI sparse-window case and verify one-bar result is INSUFFICIENT DATA with no fabricated 0% change/range; compare provider/feed/count/timestamps/gaps with a known liquid control symbol.
- [x] Source audit confirms Provider Comparison initializes an empty date using `phoenixParts(new Date())` (`America/Phoenix`) and preserves an existing user selection.
- [ ] Browser-verify a July date on a non-Arizona OS timezone; runtime acceptance remains open.
- [ ] Finish remaining browser checks for Morning Brief latency/modal/history, Arizona-local replay and HTTP 400 behavior, Red Zone 1×4 layout, sound permissions, and frontend error-free reload.
- [ ] If Visual Studio Browser Link `$.noConflict` persists, disable Browser Link for the debugging session; do not change MCC source to mask the injected script.

**Deduplication note:** Older dated sections below retain historical evidence and some repeated unchecked entries. The consolidated checklist above is the current operational list; it contains 35 individual unchecked work items across the five priority groups (3 Boring Overnight, 7 P0 including prototype header browser acceptance, 17 P1 including 8 Red Zone items, 4 P2, and 4 P3). Repeated historical rows are not separate tasks.


## 2026-10-31 — Monday operating readiness, RKLX validation, and independent 5M score

### User-supplied runtime evidence

- [x] Corrected market-data script ran against `http://localhost:53360` for RKLX / Alpaca / IEX / 2026-10-30 13:00Z–19:35Z.
- [x] Script reported 25 stored 1M and 23 stored 5M observations.
- [x] Automated provider-to-store comparison returned `1m=True` and `5m=True`; for the provider bars returned in that query, zero new writes is consistent with exact already-stored duplicates.
- [x] Runtime artifacts saved by user at `artifacts/runtime-validation/20261031T202332Z`.
- [x] Collector checkpoint baseline copied at `artifacts/collector-checkpoint/20261031T202543Z`; SHA-256 `8E3AFBD5DAD5C3502DB625F0133DE697F9C8615BB35BDA09609A4CF7FAE44500`; 3 cycles and 0 latest-cycle errors.
- [x] User confirms MCC application JavaScript errors are gone except Visual Studio Browser Link's `$.noConflict is not a function` injection. Keep this separate from MCC source defects.
- [ ] Regular-session WebSocket acceptance remains open: the supplied capture was Saturday, with acknowledged subscriptions but zero bars before/after for SPY/RKLX/ASTX/XRPN.
- [ ] Collector controlled-failure/recovery, checkpoint reload after restart, and sleep/wake acceptance remain open. The captured hash is only a baseline.

### Red Zone / V2 5M score — implementation change

- [x] Identified that `fiveMinute.scoreBreakdown.score` was sourced from the combined V2 decision. This made the 5M authority score UNKNOWN whenever the 1M/combined decision was unavailable, even if current 5M evidence existed.
- [x] Added canonical `V2SignalEngine.CalculateFiveMinuteScore` using the existing six conditions and V2 thresholds; the signal engine now calls this same method.
- [x] Live assessment now emits independent `fiveMinute.scoreBreakdown.score`, `permission`, and `status`. The score is withheld unless the 5M technical snapshot is present/fresh and same-slot RVOL is available.
- [x] Main Decision Explanation and Red Zone cards consume the independent score and permission. 5M score >=5 is labeled 5M permission only; EXECUTION READY still requires combined `decision.entryAllowed=true`.
- [x] Added unit coverage for 6/6, 5/6 (permission), and 4/6 (no permission).
- [ ] Exact-head Release build/test and Monday browser/API acceptance still required. Do not rely on earlier CI for these commits.

### Boring Overnight — Monday disposition

- [x] Reviewed `docs/BoringOvernight.md`, evaluator, explicit-boundary history calculator, snapshot store, and API routes.
- [x] Created `docs/Monday-Readiness-2026-11-02.md` and updated `docs/BoringOvernight.md` with the Monday manual-only workflow and launch gates.
- [ ] Boring Overnight is **NOT READY** for actionable/automated READY qualification. It remains WATCH/manual research only until complete source-backed 20-session history, exact session policy, full GTC_EXT coverage where needed, validated catalyst clearance, exclusions reconciliation, and Thinkorswim state parity are established.
- [ ] Do not set catalyst CLEAR from caller assertion. The history-backed API path deliberately forces catalyst status UNKNOWN until a validated provider exists.

### Daylight-saving operating times — Monday 2026-11-02

- [x] Recorded Phoenix/Eastern offset change: Phoenix stays UTC−07:00; Eastern returns to UTC−05:00; Phoenix is two hours behind Eastern.
- [x] Regular-session open/close convert to 07:30 / 14:00 Arizona.
- [x] User's 08:30 Arizona start maps to 10:30 Eastern.
- [x] Project GTC_EXT window 07:00–20:00 ET maps to 05:00–18:00 Arizona.
- [x] Added timezone conversion guidance to the Monday runbook.

### Immediate Monday acceptance

- [ ] Verify CI for exact branch HEAD before relying on independent 5M score change.
- [ ] After 07:30 Arizona, verify SPY/RKLX/ASTX/XRPN live 5M score status CURRENT, six criteria populated, score 0–6, permission iff score >=5, and recent 5M REST source timestamps.
- [ ] Verify 1M freshness/history and combined decision separately; only `entryAllowed=true` is EXECUTION READY.
- [ ] Verify WebSocket per-symbol bars, `lastBarUtc`, received/persisted counters during regular hours; REST fallback is acceptable when WS gates do not pass.
- [ ] Complete browser acceptance of the new score display and ensure no MCC console errors. Browser Link injection can be disabled in Visual Studio.
- [ ] Continue Boring Overnight manual-only. Do not force a candidate to READY.

Evidence/runbook: `docs/Monday-Readiness-2026-11-02.md`, `docs/BoringOvernight.md`, and `docs/Runtime-Acceptance-Runbook.md`.

---

## 2026-10-31 — Runtime validation failure diagnosis and collector recovery instrumentation

### Completed in this pass

- [x] Diagnosed the PowerShell connection reset: the Development profile maps port 53359 to HTTPS and 53360 to HTTP. The attempted `http://localhost:53359` was a protocol/port mismatch.
- [x] Updated `Test-MarketDataRuntime.ps1` to reject that known mismatch, perform an `/api/health` preflight, re-fetch provider bars for both timeframes, and compare fresh bars with persisted rows to determine whether zero new writes are consistent with idempotent duplicates.
- [x] Fixed the frontend temporal-dead-zone defect: `requestedMinuteBarSymbols` is initialized before startup refresh functions can call `ensureMinuteBarSubscription`.
- [x] Reviewed the uploaded RKLX stored-bar artifacts. They prove 13 current 1M and 12 current 5M RKLX/Alpaca/IEX observations for 2026-10-30 13:30Z–17:30Z.
- [x] Distinguished that uploaded window from the separate four 1M/four 5M bars previously described at 19:00Z–19:35Z. The latter are not present in the uploaded files and remain unverified.
- [x] Added `scripts/validation/Capture-CollectorCheckpoint.ps1` to capture collector status, copy the configured checkpoint, and record its SHA-256 and file metadata.
- [x] Added explicit API status diagnostics for checkpoint startup loading: `checkpointLoadCompleted`, `checkpointFileFound`, `checkpointLoadedEntryCount`, `checkpointLoadCompletedUtc`, and `checkpointLoadError`.
- [x] Expanded `docs/Runtime-Acceptance-Runbook.md` with exact collector status semantics, failure acceptance criteria, restart/reload procedure, and sleep/wake evidence requirements.

### Still open — target-machine acceptance

- [ ] Pull this branch and verify the exact-head CI before running the revised scripts.
- [x] Re-ran `Test-MarketDataRuntime.ps1` using `http://localhost:53360` for 2026-10-30 13:00Z–19:35Z. User supplied output folder `artifacts/runtime-validation/20261031T202332Z`; 25 1M and 23 5M stored observations reported.
- [x] Revised script reports `1m=True`, `5m=True` for exact provider-to-store comparison over the 13:00Z–19:35Z request. This verifies idempotent matches for all returned provider bars in the window, including the previously queried later interval. Raw JSON files remain on the user machine for individual timestamp audit.
- [ ] During regular market hours, verify SPY/RKLX/ASTX/XRPN subscription acknowledgement, per-symbol bar count/lastBarUtc advancement, received/persisted totals, and eligible 1M WebSocket source selection.
- [ ] Run `Capture-CollectorCheckpoint.ps1`, then perform a controlled local failure/recovery, clean API restart/checkpoint reload, and sleep/wake test using section 4 of the runbook. Do not induce a failure against production credentials or disable the workstation network.
- [x] User confirms MCC JavaScript errors are gone after pulling the frontend fix. The remaining `browserLink:33 $.noConflict is not a function` is Visual Studio Browser Link injection, not MCC source. Full layout/score display browser acceptance remains open.
- [ ] If `browserLink:33 $.noConflict` persists, disable Visual Studio Browser Link for the session or run without Browser Link; it is a Visual Studio development-time injected script, not an MCC application source file.

### Interpretation of the supplied collector status sample

- `cycles=6`, `lastCycleErrors=0`, and `recentErrors=[]` show no recorded errors in that latest cycle / retained queue; no failure was injected, so error surfacing is not yet accepted.
- `newlyPersistedObservations=0` is cumulative since the API process started, not a per-cycle count.
- Current configured symbols are SPY, QQQ, IWM, RTO, and AMT. TTE and FMS entries are old checkpoint keys retained after configuration changes; they are not current configured collection targets.
- The Saturday cycle timestamps with Friday source watermarks do not by themselves indicate a failure. The worker continues polling on weekends; an empty successful response does not advance a source watermark.

## 2026-10-31 — ChatDisconnect execution and runtime acceptance checkpoint

**Source reviewed:** `docs/ChatDisconnect.txt`, current `docs/Master-To-Do.md`, current `docs/NewChatHandoff.md`, and the implementation backlog. This section supersedes older status snapshots where they conflict; historical sections remain for evidence.

### Completed in this pass — source and documentation

- [x] Read and reconciled the ChatDisconnect note's explicit remaining-work list against current source and prior To-Do entries.
- [x] Implemented append-only, versioned Morning Brief report snapshots with generated/recorded UTC timestamps, unique ID, SHA-256 payload hash, and full report JSON.
- [x] Captured explicit UTC source-bar watermarks from the report's marketLandscape (provider/symbol/timeframe/feed/transport and latest source timestamp).
- [x] Added `GET /api/morning-brief/history?limit=20`; live `/api/morning-brief` now reports snapshot persistence status, ID, and version.
- [x] Added unit tests for snapshot round-trip/provenance, append-only behavior, UTC validation, and history limits.
- [x] Added `scripts/validation/Test-MarketDataRuntime.ps1` to capture RKLX stored bars, before/after stream and collector status, per-symbol bar deltas, provider status, and full live assessments.
- [x] Added `docs/Runtime-Acceptance-Runbook.md` with repeatable acceptance steps and explicit blockers.
- [x] Updated the Morning Brief technical guide and implementation backlog to reflect the new snapshot capability.

### P0 — runtime acceptance still required

- [ ] Run the stored-bars capture against the actual running MCC API for RKLX, exact UTC window, Alpaca provider, IEX feed, 1M and 5M. Resolve whether the prior `newlyPersistedObservationCount=0` means existing idempotent observations.
- [ ] During regular market hours, run the 5-minute stream capture for SPY, RKLX, ASTX, and XRPN. Verify acknowledged subscription, per-symbol bar-count/timestamp advancement, and received/persisted totals.
- [ ] Confirm eligible live assessments select WebSocket 1M only with subscription acknowledgement, at least 50 completed 1M observations, freshness, and explicit WebSocket provenance. 5M Permission remains REST.
- [ ] Complete browser acceptance of header, status-chip wrapping, chart-side 2x2 metrics, Morning Brief modal/section, replay, Red Zone responsive layout, and sound permission.
- [x] GitHub Actions run **792** passed for source commit `61e8483c6cce4cea5e5131e773086db1ff91bd74`: Release build succeeded with 0 warnings/0 errors and **156/156 tests passed**, including immutable Morning Brief snapshot and source-watermark tests.
- [x] Runtime-validation script commit `fd6fe0b66b9ad9be5fd1ec31fdf26780d2eb4534` passed workflow run **780**; documentation runs **781–785** passed.
- [x] Exact branch-head CI: GitHub Actions run **803** passed for `ecd31a28248a3599a18c7a9a4080113c3b1f3e92`; .NET 8 Release build and **156/156 tests** passed. This verified head includes the README, To-Do, Handoff, User Manual, runbook, and snapshot/source-watermark changes. Any subsequent commit requires its own exact-head check.

### P1 — engineering and evidence work

- [ ] Matched Thinkorswim state replay: obtain same-symbol/date/study-version 1M and 5M state diagnostics. OHLCV exports alone cannot prove Permission, READY, RUNNER, WANING, SWING, GET OUT/EOD, expiry, re-arm, or re-entry.
- [ ] Controlled collector outage/restart/sleep-wake acceptance. Source already has durable watermarks, overlap polling, bounded backfill chunks, retries, and atomic checkpoint replacement; target-host recovery is not yet proven.
- [x] Morning Brief response snapshot persistence implemented (see above).
- [ ] Complete Morning Brief input-manifest/versioning: raw provider response references/retention policy, news/discovery watermarks, full calculation dependency manifest, and later outcome linkage remain open. Market-landscape bar watermarks are now recorded in each snapshot.
- [PARTIAL — CAPTURE ENDPOINT ADDED 2026-10-31] `/api/boring-overnight/capture-history` now generates 20 close/open pairs with the MCC-maintained `MCC_US_EQUITIES_CALENDAR_V1` rules and fetches/persists provider 1m bars. Still validate calendar dates against official NYSE schedules, test all exceptional early closes/holidays, and keep GTC_EXT 07:00–20:00 ET target-window collection separate.
- [ ] Continue Boring Overnight qualification only with source-backed daily/overnight history, validated session pairs, catalyst status, and authoritative 5M state. Unknown catalyst remains UNKNOWN; no qualification from caller assertions alone.

### P2 — external approvals and product decisions

- [ ] Production scanner/provider validation: confirm account plan, feed entitlement, market coverage, licensing, rate limits, and acceptable discovery universe. Current Alpaca movers are bounded top gainers/losers, not full-market breadth.
- [ ] Schwab read-only integration: blocked on Individual Developer/API approval, app registration, callback URL, and secure server-side OAuth configuration. Order submission remains disabled.
- [ ] Desktop packaging/startup: choose target platform, installer/update model, local API lifecycle, data directory/backup, and browser launch behavior before selecting packaging technology.

### Current boundaries

- The existing collector/reconnect implementations are source facts, not proof of target-host recovery.
- The Morning Brief remains a partial market snapshot; immutable storage preserves what it reported, including DATA?/PARTIAL fields. It does not create missing breadth, catalyst clearance, probability, or Red Zone qualification.
- The RTO OHLCV parity result remains six candles only. Do not infer installed Thinkorswim study states.
- Runtime/browser/external tasks remain unchecked until their specific evidence is supplied.

Evidence and procedures: `docs/Runtime-Acceptance-Runbook.md`, `docs/MarketData-ChatTests-20261030.md`, `docs/Validation/Runner-Exit-State-Validation-20261029.md`, and `docs/Thinkorswim-Schwab-Integration-Plan.md`.

---

## 2026-10-30 — RKLX post-close assessment and persistence follow-up

**Checklist count clarification:** this file currently contains 30 unchecked checkbox rows across active acceptance items and retained historical sections. This is a raw row count, not 30 unique current blockers; some legacy items overlap newer checkpoints. Use the current dated sections at the top for active work and the historical sections for evidence/context.

- [x] Reviewed the 22:55:20 UTC RKLX live assessment. Both 1M and 5M were correctly marked stale because their latest completed bars ended at 20:00 UTC, and the assessment occurred after the 20:00 UTC / 1:00 PM Arizona regular-session close. Permission and execution were not evaluated.
- [x] Reconciled the 22:50:31 UTC WebSocket snapshot with source code. Connected/subscribed and a recent `lastMessageUtc` do not prove bar delivery; `lastMessageUtc` includes control/auth/subscription messages, while `lastBarUtc` and `barsBySymbol` represent actual bar observations. The zero-bar snapshot was post-close and is not an in-session failure test.
- [x] Recorded that the 122-bar 1M and 441-bar 5M assessment histories cover multi-day lookbacks and must not be compared by total count with the 35-minute REST queries.
- [ ] Verify RKLX observations in `GET /api/market-data/stored-bars` for matching provider/feed/timeframe/window; determine whether `newlyPersistedObservationCount=0` reflects expected idempotent duplicates.
- [ ] Next regular session: capture WebSocket status before/after 5–10 minutes and verify per-symbol bar counts/timestamps advance; then verify 1M source switches only when all subscription/history/freshness/provenance gates pass.
- Evidence: `docs/MarketData-ChatTests-20261030.md`, section “Additional RKLX assessment and stream snapshot”.

---

## 2026-10-30 — RTO exported OHLCV reconciliation

- [x] Parsed data/validation/RTO/StrategyReports_RTO_102826_1m.csv and the corresponding 5m export.
- [x] Verified 30 consecutive 1M bars (12:30–12:59 Arizona) and six consecutive 5M bars (12:30–12:55 Arizona), with valid OHLCV, no duplicate timestamps, and no gaps within the exported windows.
- [x] Aggregated each set of five 1M candles into OHLCV. All six aggregates exactly match the corresponding 5M export across open, high, low, close, and volume.
- [x] Determined embedded epoch-millisecond timestamps map to 2026-10-28 19:30Z–19:59Z / 12:30–12:59 Arizona and are the candle-start timestamps. StrategyReport Date/Time fields are separate event times and must not be substituted for candle timestamps.
- [ ] A matched Thinkorswim state tape remains unavailable: these CSVs contain transport OHLCV rows, not per-bar Permission/READY/RUNNER/WANING/SWING/GET OUT labels. Do not infer state transitions from the transport-close rows.
- Evidence and calculations: docs/Validation/Runner-Exit-State-Validation-20261029.md, section “RTO exported OHLCV reconciliation”.

---

## 2026-10-30 — RKLX diagnosis, dashboard layout, and Visual Studio frontend visibility

### Completed in this pass
- [x] Analyzed the user's RKLX 1M/5M `/api/market-data/bars` responses against the earlier in-session stale observation. The 1M query returned four bars (19:00, 19:08, 19:25, 19:35 UTC); the 5M query returned four (19:00, 19:05, 19:25, 19:35 UTC).
- [x] Resolved the 19:26 UTC 5M stale explanation: the 19:25 5M candle had not completed; the last completed candle was 19:05 with expected close 19:10. At 19:26 it was about 16 minutes old, above the 360-second limit. Missing 19:10, 19:15, and 19:20 5M bars explain why there was no newer completed 5M candle. The 19:25 1M candle completed at 19:26 and could be fresh.
- [x] Confirmed the short-window bars response is consistent with the wider assessment history over their overlapping 19:00–19:35 interval. The assessment uses a three-day 1M window; the direct query is only 35 minutes. The two bar counts are not comparable.
- [x] Recorded the remaining uncertainty correctly: Alpaca/IEX returned sparse RKLX observations. The supplied evidence does not distinguish no IEX-eligible prints from upstream/provider coverage behavior and does not prove there were no trades on other venues. Missing bars are not zero-volume bars.
- [x] Corrected stream-status interpretation: the 22:50 UTC snapshot was after the 20:00 UTC regular-session close. Connected/subscribed with zero bars is not proof of an in-session stream failure. Regular-session delivery still needs a timed smoke test.
- [x] Fixed header status-pill overlap by making the demo indicator dot target only the first span, restoring the text span, and allowing the WebSocket status chip to wrap without colliding.
- [x] Expanded the header to full viewport width and removed the 1240px cap from its inner layout; updated header padding to match.
- [x] Increased the focused-symbol label prominence.
- [x] Moved the four Opportunity Status cards beside the Main Chart itself and changed them to a 2×2 grid, keeping the chart as the primary wide column and retaining responsive stacking.
- [x] Added `frontend/index.html`, `frontend/styles.css`, `frontend/app.js`, and `frontend/favicon.svg` as Solution Items under a new `frontend` folder in `MomentumCommandCenterV2.sln`. These files are now directly discoverable/editable in Visual Studio Solution Explorer.
- [x] Updated the User Manual, this To-Do, the handoff, and the market-data evidence note with the current source layout and diagnosis.

### Still open — genuine runtime or external dependencies
- [ ] During regular market hours, capture stream status before/after 5–10 minutes and confirm `barsBySymbol`, `receivedBars`, `persistedBars`, and `lastBarUtc` advance for SPY, RKLX, ASTX, and XRPN.
- [ ] After a symbol has at least 50 completed 1M WebSocket observations and a fresh latest bar, confirm live assessment reports `ALPACA_WEBSOCKET_LATEST_BAR`.
- [ ] Browser-test the revised header, chart-side 2×2 metrics, Morning Brief modal, replay, Red Zone 1×4 layout, and sound permission in the user's running API-hosted dashboard. Browser execution is not available from this repository-editing session.
- [x] GitHub Actions run 767 passed the .NET 8 build and test workflow for commit 3192c8f34d11273360bed82367563466a30b08ad (the code, solution, and documentation changes at that head).
- [ ] Schwab Trader API Individual approval and secure OAuth setup remain pending. Read-only position sync is blocked until approval and user-side configuration.
- [x] Order submission remains disabled by design.

### Frontend architecture decision
The dashboard remains a static HTML/CSS/JavaScript frontend hosted by the ASP.NET Core API through its physical-file provider. It is not an MVC/Razor application. The immediate usability issue was Solution Explorer visibility, now addressed with a `frontend` Solution Items folder. No MVC migration was made because it would be a larger architectural change without a demonstrated requirement for Razor views or controller-driven page rendering.

### Evidence
- `docs/MarketData-ChatTests-20261030.md` — original ChatTest and RKLX follow-up evidence.
- `docs/User-Manual.md` — where to edit the frontend and how the API hosts it.
- `docs/NewChatHandoff.md` — current continuation instructions.

---

## Final implementation validation — 2026-10-30

- Branch: `feature/opportunity-quality-classification`
- Code validation: GitHub Actions run **742** passed the .NET 8 build and **152/152 tests**, including the dynamic WebSocket subscription cap and stream persistence changes.
- Related runs **739–741** passed, covering the full-width Red Zone layout, replay/local-time UI, and Morning Brief API caching.
- Documentation/cache follow-up runs **743–746** passed.
- Frontend `app.js` syntax check: PASS. Duplicate HTML IDs: none.
- Browser acceptance is still outstanding: actual audio permission, modal/accordion interaction, local replay form, live symbol subscription acknowledgement, and per-symbol bar advancement must be checked in the user's running MCC instance.
- Live provider acceptance is still outstanding: all uploaded ChatTests were captured after market close; separate in-session RKLX 5M stale condition needs a fresh same-session response. Do not change freshness thresholds to conceal it.
- Schwab Trader API Individual approval remains pending. No Schwab credentials or tokens were requested or stored. Order execution remains disabled.

## MCC update — 2026-10-30 (user review follow-up)

### Completed in this iteration

- [x] Investigated all four `data/ChatTests/MarketData_*.json` files. SPY, RKLX, ASTX, and XRPN all returned `STALE_MARKET_DATA`; all four reported REST as the 1M source. Their evaluation timestamps were 20:08–20:12 UTC, after the 20:00 UTC / 1:00 PM Arizona regular-session close.
- [x] Recorded the separate RKLX in-session observation: 5M bar age 982s (360s limit), source 19:05 UTC / expected close 19:10 UTC, observed around 19:26 UTC. Exact provider/feed cause remains unresolved.
- [x] Added dynamic Alpaca subscription endpoint `POST /api/market-data/stream/subscribe?symbol=XYZ`. MCC requests subscriptions for the selected symbol and Red Zone cards; `StreamSymbols` is now only the initial server-side seed.
- [x] Dynamic symbols remain in API memory and are automatically re-subscribed after WebSocket reconnect. Default cap is 30 symbols (`MarketData:Alpaca:StreamMaxSymbols` can be changed to match entitlement); symbols are not persisted across API restarts, and the front end re-requests the currently selected and visible Red Zone symbols.
- [x] Stream status now reports acknowledged symbols, per-symbol bar counts/last timestamps, and separate received vs persisted totals.
- [x] Morning Brief modal now opens immediately using the existing rendered report; it no longer waits for a second full API call on every open. The API and browser each cache the report for 60 seconds; the modal opens immediately and refreshes stale data in the background.
- [x] Removed the duplicate Morning Command Brief heading from the modal body.
- [x] Moved Opportunity Classification, Last Price, RVOL, and ATR Extension into the Main Chart heading on the right; constrained the chart width.
- [x] Red Zone now occupies the full available row and uses a four-column desktop grid with responsive 2-column/tablet and 1-column/mobile layouts.
- [x] Timeframe Handoff now explains how to use 5M permission vs 1M execution and how often to review each.
- [x] Historical replay now accepts Arizona-local date/time and converts it to UTC in the browser. Missing replay query parameters now return HTTP 400 with a clear explanation instead of an unhandled 500.
- [x] Added opt-in two-tone sound alert when a new ticker enters the four visible Red Zone slots. Sound must be enabled by clicking the control; live Alpaca discovery refreshes every 30 seconds while the Live Discovery group is selected.
- [x] Live discovery candidates now sort by absolute provider-reported percentage move, not alphabetically because a formatted percentage string was not numeric.

### Still open / acceptance

- [ ] Deploy and verify dynamic subscriptions in a running MCC API during regular market hours.
- [ ] Verify `barsBySymbol` advances for SPY, RKLX, ASTX, and XRPN and that eligible 1M assessments switch to `ALPACA_WEBSOCKET_LATEST_BAR`.
- [x] Diagnose the in-session RKLX 5M stale condition from same-window Alpaca/IEX bars. The 19:25 5M bar was incomplete at the 19:26 assessment; the last completed bar was 19:05 (expected close 19:10), approximately 16 minutes old. Missing 19:10/19:15/19:20 bars explain the stale result. Do not lower freshness limits.
- [ ] Verify Morning Brief initial-load latency and modal behavior in browser.
- [ ] Verify Arizona-local replay form and API HTTP 400 behavior.
- [ ] Verify Red Zone 1x4 layout and browser sound permissions.
- [ ] Schwab Trader API Individual remains pending approval; OAuth and read-only position sync are blocked until the user completes app registration and securely configures server-side credentials.
- [x] Keep order submission intentionally disabled.

### Evidence document

See [MarketData-ChatTests-20261030.md](MarketData-ChatTests-20261030.md) for per-symbol timestamps, freshness ages, stream status, and runtime acceptance steps.

## Latest verified checkpoint — 2026-10-30

- Branch: `feature/opportunity-quality-classification`
- Verified branch head at checkpoint: `0d669ad888400682575b7d8bd895f7ff55b2436d`
- GitHub Actions run **707** passed Release build and **152/152 tests** after correcting the transport-provenance test fixture.
- Runs **708–714** completed successfully, including hybrid transport UI/header, documentation, and latest cache-bust updates.
- Frontend JavaScript syntax parsed successfully after Morning Brief, replay, and hybrid-source UI changes.
- Browser acceptance remains with the user: modal/accordion behavior, actual symbol freshness JSON, replay against populated stored history, and live WebSocket subscription/data flow.
- Historical per-symbol stale causes for RKLX, ASTX, and XRPN are not yet known. The generic stale response supplied earlier does not contain the timeframe ages or timestamps. **Data not found in sources.**
- Schwab OAuth/account/position sync remains unimplemented pending the user's Developer Portal approval, callback URL, and secure server-side app credentials. Order execution remains disabled.

## Validation and hybrid-source checkpoint — 2026-10-30

- [DONE — SOURCE] Raw `MarketBar` observations now retain `SourceTransport` (`REST` or `WEBSOCKET`). The append-only store includes transport in its content hash, so identical OHLCV arriving by a different transport is recorded as a provenance change rather than silently deduplicated.
- [DONE — SOURCE] Live assessment uses the persisted 1m series only when the Alpaca WebSocket is confirmed subscribed, at least 50 completed 1m bars are available, the latest bar is fresh, and the latest observation is explicitly tagged `WEBSOCKET`. Otherwise it falls back to REST. 5m permission remains REST.
- [DONE — UI] Decision Explanation, Main Chart, and the header now identify the actual 1m transport and 5m REST source. Header transport label follows the selected symbol's assessment.
- [PASS — TESTS] GitHub Actions run **707** for commit `8b015d73e88cda62ffef79e9a103836218b5a8e9`: Release build succeeded; **152/152 tests passed**. The new transport-provenance test initially failed because its fixture omitted `Timeframe="1m"`; that fixture was corrected. Runs 701–706 are historical failures from that one test-fixture issue and are superseded by the passing run 707.
- [PASS — CI] Runs 708, 709, and 710 passed for frontend cache-bust/header updates. Run 711 for commit `980dea5d3cf4e217fab072689db04f7b6c0971e6` was in progress at this checkpoint.
- [OPEN — CURRENT HEAD] Verify CI for the latest documentation and cache-bust commits before treating the newest branch head as validated.
- [OPEN — RUNTIME] WebSocket is optional and disabled by default. User must configure it server-side and verify subscription, bar arrival, reconnect, and 1m/REST parity. Browser runtime verification remains outstanding.

## Active implementation checkpoint — Morning Brief, replay, stream, and stale diagnostics — 2026-10-30

## Active implementation checkpoint — Morning Brief, replay, stream, and stale diagnostics — 2026-10-30

### Completed in source (CI and browser acceptance pending)
- [DONE — UI] Morning Command Brief now has two entry points: a full-width collapsed-by-default section near the bottom of MCC and a header button that opens a modal. Both show the same API-backed report snapshot.
- [DONE — API] Live assessment stale-data failures now identify which timeframe(s) are stale and include observed age, threshold, source timestamp, expected close, and feed. The response also includes structured `freshnessDiagnostics`.
- [DONE — API/UI] Added `GET /api/market-data/replay` and a Historical Decision Replay panel. It requires an explicit UTC as-of timestamp and one explicit feed; only completed stored bars at or before as-of are considered. Missing history returns an explicit status.
- [DONE — API] Replay calls the same current `V2SignalEngine` with the default `V2Config`. The UI checkbox allows a simulated `hasPosition=true` run to exercise the existing position-management branch. This is a simulation, not a Schwab position read.
- [DONE — API] Added an optional Alpaca WebSocket minute-bar worker. It is disabled by default, authenticates, confirms the subscription acknowledgement, reconnects with backoff, persists `bars` and late `updatedBars` as append-only observations, and exposes `/api/market-data/stream/status`.
- [DONE — UI] Added a header status indicator for the optional WebSocket minute-bar collector. It distinguishes disabled/connected/error states. The live assessment now uses the persisted 1m WebSocket series when the stream is confirmed, at least 50 completed bars exist, the latest bar is fresh, and the latest stored observation is explicitly tagged WEBSOCKET. Otherwise it falls back to 1m REST. 5m permission bars remain REST.
- [DONE — SAFETY] Order submission remains unimplemented and disabled.

### Critical open work
- [PRIORITY 1 — USER DATA REQUIRED] After pulling this branch and refreshing MCC, request the live-assessment JSON for RKLX, ASTX, and XRPN. The new stale reason now tells us whether 1M, 5M, or both exceeded their freshness limits, with exact ages/timestamps/feed. The generic message previously supplied does not contain those per-symbol values. **Data not found in sources.** Do not assume the three tickers have the same stale timeframe or cause.
- [OPEN — REPLAY VALIDATION] Run the new replay panel against a timestamp with known stored 1M/5M bars. Confirm it returns REPLAYED, uses the selected feed, excludes all bars after as-of, and returns INSUFFICIENT_STORED_HISTORY when coverage is missing. Historical V2 config versioning is not yet archived; replay uses the current default config.
- [OPEN — STREAM VALIDATION] Configure the worker, verify `/api/market-data/stream/status` reports SUBSCRIBED/CONNECTED, and verify 1m bars plus late corrections are appended. The live assessment now uses WebSocket-sourced 1m bars when the connection/history/freshness/provenance guards pass; it falls back to REST otherwise. 5m permission remains REST-backed.
- [OPEN — STREAM CONFIG] Set `MarketData:Alpaca:StreamEnabled=true` and `MarketData:Alpaca:StreamSymbols=SPY,RKLX,ASTX,XRPN` in server-side configuration. Keep the existing feed selection aligned with entitlement (`iex` for Basic; `sip` only when authorized). Do not place credentials in frontend files or chat.
- [OPEN — SCHWAB] Read-only Schwab OAuth/account/position synchronization is not yet implemented. The API product has no clearly published monthly price on its official product page; confirm any fees/approval with Schwab. User must register an Individual Developer role, request Trader API Individual access, create an approved app, and provide the app key/secret plus callback URL through secure server-side configuration. Never share credentials in chat.
- [OPEN — POSITION-AWARE] The replay checkbox tests the engine's existing boolean `hasPosition` branch only. Actual broker positions, quantity, side, average entry, sync freshness, and unknown/stale-position fail-closed behavior are not connected. Add a normalized read-only position snapshot before claiming real position-aware operation.
- [OPEN — BROWSER] User has confirmed the red X/low score presentation, but must verify the new Morning Brief locations/modal, replay controls, and WebSocket status chip after pulling latest branch.
- [OPEN — BRIEF DATA] `/api/morning-brief` is still a partial market snapshot. It does not yet establish complete-market breadth, verified sector money flow, classified catalysts, historical outcome probabilities, a validated Red Zone rank, or Boring Overnight qualification. Missing inputs must remain DATA NOT FOUND IN SOURCES.

### Acceptance checklist
1. Run Release build/tests and inspect CI for the exact new branch head.
2. Pull branch and hard-refresh MCC.
3. Open Morning Command Brief from the header; close it; expand/collapse the full-width footer section. Verify both show the same current API report timestamp.
4. Inspect RKLX, ASTX, XRPN and copy each full live-assessment JSON response; compare `freshnessDiagnostics.oneMinute` and `fiveMinute`.
5. Replay one known timestamp with adequate stored history; repeat with `hasPosition=true`; inspect the full audit JSON and confirm future-bar exclusion.
6. Enable WebSocket only after server configuration; verify subscription acknowledgement, bar count, reconnect state, and late-bar corrections. Compare streamed 1M bars with REST for the same feed.
7. Complete Schwab Developer Portal setup and supply only non-secret callback/approval facts here; enter secrets directly into server-side secret storage.
8. Keep order submission disabled.


## Validation status correction — 2026-10-30

- GitHub Actions run 663 **PASSED** for live Decision Explanation / score UI commit `d2dbd628fa40de11e49b0693b25280e2dc22453e`.
- GitHub Actions run 664 **PASSED** for the Master To-Do documentation update.
- GitHub Actions run 665 is still running for the latest Handoff documentation update; the code itself has already passed run 663.
- Frontend JavaScript syntax check passed. User confirmed the live assessment endpoint returned Success.
- Local browser verification of this newest Decision Explanation/score presentation remains open.

## Latest validation checkpoint — 2026-10-30

- Current implementation adds live Decision Explanation, six-factor V2 score breakdown, colored Red Zone score bands, exact UNKNOWN reasons, 15-second REST refresh, and a 10-second assessment cache.
- [USER CONFIRMED] Direct `GET /api/market-data/live-assessment` endpoint test returned Success.
- [PASS] Frontend `app.js` JavaScript syntax check.
- [PASS] GitHub Actions run 661 for commit `8ff0ea4a01af356c55c901321585fb92b5d90241`.
- [PASS] GitHub Actions run 662 for commit `c5e14a6f30a463062e3d7ef221b73a1ca505ba60`.
- [IN PROGRESS] GitHub Actions run 663 for commit `d2dbd628fa40de11e49b0693b25280e2dc22453e` (state-label formatting). Do not mark the current branch head validated until this completes.
- [OPEN — USER BROWSER TEST] The new live Decision Explanation, score colors, UNKNOWN explanations, and Product Feedback bottom placement need a local browser check.
- [OPEN — SYMBOL DATA] Exact RKLX/ASTX/XRPN evaluation reasons were not supplied in this conversation. The updated cards now show each symbol's server-returned status/reason; inspect those instead of guessing.
- [OPEN — STREAM] Current hybrid path: 1m uses confirmed WebSocket-persisted bars when eligible, otherwise REST; 5m permission remains REST. The V2 rules operate on completed 1m/5m bars; raw tick streaming is not required.
- [OPEN — POSITIONS] Schwab/Thinkorswim read-only position integration is design-only; see `docs/Thinkorswim-Schwab-Integration-Plan.md`.

## 2026-10-30 — Live Decision Explanation, score semantics, replay clarification

- [DONE — CODE] Added `fiveMinute.scoreBreakdown` to the live-assessment API. It returns the six V2 5M conditions individually (true/false/null), score maximum 6, and permission threshold 5.
- [DONE — CODE] Replaced the top-level demo snapshot Decision Explanation with symbol-specific live API data. It now shows the engine state/reason, 5M score conditions, 1M/5M source timestamps, freshness, feed, RVOL baseline sample count, and 1M entry context.
- [DONE — CODE] Red Zone score display is color-coded: 0–2 red with X; 3–4 amber; 5–6 green. The green **EXECUTION READY** label is reserved for `decision.entryAllowed=true`; a strong 5M score without 1M confirmation says to wait.
- [DONE — CODE] UNKNOWN cards now display the exact API evaluation status/reason rather than an unexplained UNKNOWN.
- [DONE — CODE] Removed the misleading Replay point slider. It previously selected five hard-coded examples, not historical bars or historical decisions. The UI now explicitly says historical replay is not implemented.
- [DONE — CODE] Moved Product Feedback to the bottom, after Provider Comparison.
- [DONE — CODE] Changed the dashboard refresh cadence to 15 seconds and the per-symbol API cache to 10 seconds. This remains REST polling, not streaming.
- [DONE — DOCS] Updated this To-Do, New Chat Handoff, User Manual, and Live Symbol Assessment guide; added a Schwab/Thinkorswim integration plan.
- [VALIDATION] GitHub Actions run 648 passed for commit `c808456b1cb36382fe9491735e79c6de336f47e8` (nullable RVOL compile fix). The current Decision Explanation/score/UI commits are newer and require their own CI run before acceptance.
- [OPEN — USER TEST] Confirm live Decision Explanation follows selected symbol; verify 6-factor criteria and timestamp/feed; confirm 0–2 red, 3–4 amber, 5–6 green, and entry-ready only when 1M entryAllowed is true.
- [OPEN — SYMBOL DIAGNOSIS] RKLX, ASTX, and XRPN previously displayed UNKNOWN. The new UI shows each symbol's actual `evaluationStatus` and `evaluationReason`; inspect those responses to establish the individual cause. Do not assume the same cause for all three.
- [OPEN — REPLAY] Implement true historical decision replay using stored 1m/5m bars, explicit as-of time, and the same versioned V2 engine/config. Preserve historical data quality and never use today's future bars.
- [OPEN — STREAMING DECISION] Optional Alpaca WebSocket minute bars and late corrections are implemented. The live assessment consumes a fresh WebSocket-sourced 1m latest bar when eligible; 5m remains REST. Validate source parity and reconnect behavior before treating this as production-ready.
- [OPEN — THINKORSWIM/SCHWAB] Read-only Schwab Trader API position integration plan is documented in `docs/Thinkorswim-Schwab-Integration-Plan.md`. Requires user-owned Schwab developer app, approved API access, OAuth token lifecycle, secure token storage, account/position mapping, and reconciliation. Do not enable order submission as part of the position-read phase.

## User browser verification update — 2026-10-30

- [USER VERIFIED] Morning Brief News Link hover.
- [USER VERIFIED] Superscript legend lines.
- [USER VERIFIED] Main Chart placement.
- [USER VERIFIED] Red Zone focus updates.
- [USER VERIFIED] Red Zone refresh.
- [USER VERIFIED] Manual Red Zone management.
- [USER VERIFIED] System Readiness topbar.
- [USER VERIFIED] Provider Comparison placement at bottom.
- [USER VERIFIED] Quick Start modal.
- [USER VERIFIED] Favicon 404 no longer present.
- [IMPLEMENTED; USER RETEST PENDING] Section IDs.
- [STILL ONGOING] BrowserLink `$.noConflict` error. Do not mark fixed until the injecting script and stack are identified.

## Final implementation checkpoint — live data and layout (2026-10-30)

- Current implementation includes `GET /api/market-data/live-assessment`, `LiveSymbolAssessmentService`, the live frontend consumers, 600px scanner table, compact topbar, tests, and documentation.
- The latest technical-calculation refinements use Wilder RSI14 and ATR14, regular-session-only VWAP, same-clock-slot 5m RVOL, and explicit future timestamp status.
- Live UI is REST-polled every 60 seconds; API results are cached for 30 seconds. This is not streaming/tick data.
- Latest code SHA before this documentation update: `bc1d7f9bf3c862d8780c665b1b68585991d64644`.
- GitHub Actions for this work have been created but are still queued; no successful build/test result exists yet for the live-assessment changes. Previous successful run 620 predates this feature and must not be used as evidence that the new service compiles.
- Browser runtime acceptance is pending for the new live endpoint, auto-updating Red Zone/Main Chart, 600px table, sticky Watch action and compact header.

## 2026-10-30 — Live symbol assessment / Main Chart / Red Zone

- [DONE — CODE] Added `GET /api/market-data/live-assessment?symbol=...` and `LiveSymbolAssessmentService`. It retrieves Alpaca 1m bars over a 3-calendar-day lookback and 5m bars over a 21-calendar-day lookback, persists both source sets, and caches each symbol/provider assessment for 30 seconds.
- [DONE — CODE] Added deterministic OHLCV calculations for EMA9, EMA20, SMA50, RSI14, MACD(12,26,9), ATR14 and session VWAP.
- [DONE — CODE] Added same-time-of-day 5m relative volume: current 5m bar volume divided by the mean volume for the same regular-session 5m slot across up to ten previous sessions. Requires at least five prior session samples; this is a calculated same-slot baseline, not a vendor-provided consolidated RVOL value.
- [DONE — CODE] Connected the existing `V2SignalEngine` to current completed 1m/5m snapshots. Response includes 5M score/permission, 5M structure, 1M structure/momentum, entry permission, state/action, extension and reason.
- [DONE — CODE] Added explicit fail-closed statuses for insufficient history, stale source bars, insufficient same-slot RVOL baseline and outside-regular-session requests. Permission/execution are not calculated in those states.
- [DONE — CODE] Red Zone now requests a live assessment for every visible pinned/discovered ticker and updates extension, 1m/5m freshness, V2 score, 5M permission, 1M execution and mini-chart. A manually pinned ticker is no longer permanently stuck at DATA?/UNKNOWN; those values resolve when the provider returns sufficient source evidence.
- [DONE — CODE] Main Chart now consumes the same live-assessment endpoint and uses returned 1m OHLCV bars. Symbol changes refresh the chart; browser refreshes the focused chart and Red Zone cards every 60 seconds.
- [DONE — CODE] Moved `workspace-toolbar` inside the header. System Readiness now sits to the right of the brand in a 2×2 grid.
- [DONE — CODE] Scoped Scanner Workbench `.table-wrap` to 600px height with vertical and horizontal scrolling, sticky table headers, and a sticky right-side Watch action column to keep Watch visible while horizontally scrolling.
- [DONE — TESTS ADDED] Added `LiveSymbolTechnicalCalculatorTests.cs` covering indicator snapshots, same-time-of-day RVOL sample sufficiency, and Eastern regular-session boundaries.
- [LIMITATION — IMPORTANT] This is near-live historical REST polling, not a websocket/tick stream. The browser refresh cadence is 60 seconds and the server assessment cache is 30 seconds. It does not guarantee every trade/quote update.
- [LIMITATION — IMPORTANT] V2 is evaluated with `hasPosition=false` because MCC does not yet have a broker-position source. The resulting 1M execution label is an entry-context assessment, not open-position management.
- [LIMITATION — IMPORTANT] No catalyst/news classification, halt status, NBBO spread, order-book depth, or Thinkorswim study values are inferred. Provider/feed entitlement still determines bar coverage.
- [OPEN — VALIDATION] Run GitHub Actions for the latest head and inspect test results. Then test live API response with a known liquid ticker during regular session; verify returned source timestamps, feed, bar counts, same-slot baseline sample count, V2 output, and failure behavior with an unconfigured/invalid symbol.
- [OPEN — USER BROWSER RETEST] Pull latest branch and verify: manual pin resolves from actual provider data; extension and freshness update; 5M permission and 1M execution show real engine output; Main Chart displays latest 1m bars; data refreshes on 60s cadence; stale/out-of-session/insufficient baseline states fail closed; table-wrap is 600px and Watch remains visible; header compactness and 2×2 readiness; toolbar inside header.
- [OPEN — PRODUCT] True websocket streaming / trade-level live charting remains future work if the 60-second REST refresh is not sufficiently current.

## 2026-10-30 — MCC browser feedback / layout pass
- [DONE — USER CONFIRMED] Morning Brief News Link hover behavior works in the user's local browser.
- [DONE — CODE] Split the Alpaca market-mover footnote into separate lines for ATG, ATL, Change, and Source.
- [DONE — CODE] Moved the focused-symbol 1-minute close-price chart to the top of the main workspace and labeled it **Main Chart**.
- [DONE — CODE] Red Zone tile focus now invokes the dashboard renderer as well as refreshing the focused chart; this addresses the prior behavior where the focused chart changed but `status-grid` / `layout` did not consistently rerender.
- [DONE — CODE] Added a dedicated **Refresh Red Zone** control. It re-renders the currently selected scanner group's candidates and reloads chart requests. To retrieve newly persisted Alpaca discovery events, use **Refresh Alpaca market movers**; the Red Zone button does not itself query the provider.
- [DONE — CODE] Added browser-local Red Zone management: manually pin a ticker, dismiss a scanner candidate, and restore dismissed candidates. Pinned symbols persist in localStorage under `mcc.redZoneState.v1`; they are not server-synced. Manually pinned symbols without source rows display `MANUAL — REVIEW` and DATA?/UNKNOWN fields rather than fabricated metrics.
- [DONE — CODE] Moved System Readiness into the header topbar and added stable IDs to the major workspace sections, including `main-chart-section`, `morning-command`, `morning-brief`, `red-zone`, `opportunity-status`, `decision-layout`, `scanner-workbench`, and `provider-comparison`.
- [DONE — CODE] Moved Provider Comparison out of the primary morning workspace and relabeled it as a diagnostic step, not a daily workflow step. It compares historical REST responses over the same symbol/date/interval; it does not prove full-session coverage, live streaming, reconnect/backfill, or entitlements.
- [DONE — CODE] Added a Quick Start modal with the broad morning/daily workflow and an opener in the header plus a footer opener.
- [DONE — CODE] Added an explicit SVG favicon link and `frontend/favicon.svg` to address the missing favicon asset. Browser verification of the disappearance of the 404 is still required.
- [UNRESOLVED — BROWSER ENVIRONMENT] The reported BrowserLink `$.noConflict` error remains unverified/unfixed. Neither `frontend/app.js`, `frontend/index.html`, nor API `Program.cs` contains a BrowserLink/noConflict reference; this suggests a development-host/browser-injected script may be involved, but the exact source is not established. Do not mark fixed without reproducing in the local browser and identifying the script URL/stack.
- [CI] GitHub Actions .NET build/test run 41095658724 passed for commit `c4bc11c3b894fb027bb1608c3e9976ae2e17da35`. This is a .NET workflow result, not browser JavaScript or UI validation. Later favicon/docs commits require their own latest-head CI check.
- [OPEN — USER BROWSER RETEST] Pull the latest branch and verify: hover News Link; each legend line; Main Chart placement and focus; Red Zone refresh/pin/dismiss/restore; topbar readiness; Provider Comparison placement; Quick Start open/close; stable section IDs; favicon 404; BrowserLink console error. Record the exact tested SHA and console/network output.
- [OPEN — DATA/PRODUCT] Main Chart is currently an API-backed 1-minute close-price line, not a full OHLC candlestick chart or a 1M/5M study chart. Decision summary cards still show the existing illustrative replay snapshot; clicking a Red Zone symbol rerenders them but does not yet calculate per-symbol 5M permission/1M execution state. Do not present demo values as live symbol-specific state.

## 2026-10-29 — SPY post-market control and dashboard coverage audit

- [VERIFIED LIVE BY USER] SPY 1m Alpaca/IEX for 2026-10-28 20:00–23:55 UTC returned 2 bars at 20:00 and 20:54 UTC, gapCount 1. 20:00 UTC = 16:00 EDT (regular close boundary); 20:54 UTC = 16:54 EDT (post-market). The 20:54 observation establishes that the configured IEX route returned at least one SPY post-market bar in this window. Two bars are extremely sparse and do not establish complete after-hours coverage.
- [UPDATED INTERPRETATION] RTO's zero bars for the same post-market window is therefore symbol-specific in the observed result, not evidence that Alpaca/IEX categorically cannot return extended-hours observations. Still unresolved: whether RTO had eligible IEX prints, IEX venue coverage, provider behavior, and the account's feed entitlement. Do not claim SIP is required or switch feed without entitlement evidence.
- [DASHBOARD IMPLEMENTED] Added a Post-market control test to the Boring Overnight panel. It queries the selected symbol and SPY over the same user-entered UTC window, displays provider/feed, count, first/latest source timestamps, and explicitly labels empty/sparse responses as inconclusive. It does not infer no-trade or qualification.
- [DASHBOARD OPEN P0] Saved snapshot table still correctly labels records as input snapshots, not final decisions. Evaluator request requires explicit 20+ session boundaries and caller-supplied non-history evidence; do not auto-submit or display READY until session boundaries, independent source evidence, and decision persistence are implemented.
- [OPEN] Timeframe identity in persisted bar storage, exchange calendar, automatic boundary-pair generation, final decision persistence, independent catalyst/5M/1M evidence, and market-wide discovery remain open. These are not safely solved by frontend-only changes.
## 2026-10-29 — RTO live retest: regular-session bars present, after-hours not demonstrated

- [VERIFIED LIVE BY USER] Exact RTO request for 2026-10-28 20:00–23:55 UTC returned a normalized empty result: feed null, barCount 0, no timestamps, no gaps. The null-bars adapter behavior is now confirmed in the user's running API. An empty response is not proof of no trading and does not validate an extended-hours feed.
- [VERIFIED LIVE BY USER] RTO request for 2026-10-28 19:00–23:55 UTC returned 47 IEX bars, first 19:01 UTC and last 19:59 UTC, with six reported missing-minute gaps. All returned bars are within the first hour only.
- [TIME CONVERSION — IMPORTANT] On 2026-10-28, US Eastern daylight time was UTC−4. Therefore 19:00–19:59 UTC is 15:00–15:59 ET (the final regular-session hour); 20:00 UTC is 16:00 ET (regular close); 20:00–23:55 UTC is 16:00–19:55 ET (post-market). The second request proves RTO regular-session data near the close, not extended-hours coverage.
- [INTERPRETATION] These two responses alone do not establish that Alpaca globally fails to return extended-hours bars. Need a same-window liquid-symbol control (SPY) using the same provider/feed, plus ideally a symbol known to have post-market trades. Compare response timestamps and feed. Do not switch from IEX to SIP or claim an entitlement defect without account/provider evidence.
- [NEXT LIVE TEST] Query SPY with timeframe=1m, from=2026-10-28T20:00:00Z, to=2026-10-28T23:55:00Z, providerName=Alpaca. Then, if practical, query RTO for 2026-10-28T19:50:00Z–2026-10-28T20:10:00Z to verify the close boundary. Record feed, barCount, first/latest timestamps and sanitized provider errors. SPY returning bars after 20:00Z demonstrates some IEX post-market coverage for that symbol/window; SPY empty means the cause remains ambiguous (IEX venue coverage, no eligible trades, provider request semantics, or entitlement).
- [OPEN — OVERNIGHT P0] The current evidence is sufficient to label RTO extended-hours coverage unverified/absent in this request, not to diagnose its cause. Keep missing bars as missing; never synthesize or carry forward regular-session prices into post-market.

## 2026-10-29 — RTO bars:null follow-up

- [VERIFIED LIVE BY USER] Retesting RTO for UTC `2026-10-28T20:00:00Z`–`2026-10-28T23:55:00Z` returned HTTP 502 because the successful Alpaca JSON payload had top-level `bars: null`, `next_page_token`, and `symbol`. This is different from the previously anticipated symbol-keyed array shape.
- [CODE CHANGE — TEST PENDING] The adapter now treats `bars: null` on the first page with no continuation token as an empty result, not as a successful data validation. It still rejects missing `bars`, and rejects null bars on a continuation page to avoid silently returning partial paginated data.
- [INTERPRETATION] An empty result is not proof of zero trading, nor does it establish whether the cause is feed entitlement, historical availability, symbol coverage, or a provider behavior. The actual cause remains unresolved without a provider response that includes an explanatory code/message or a non-empty RTO result.
- [NEXT LIVE TEST] Pull/restart with this change, repeat the exact RTO request, and inspect whether the endpoint returns HTTP 200 with `barCount: 0` / empty bars. If empty, compare the same time window against a known liquid symbol and a nearby regular-session RTO window. Do not mark the provider validated from an empty result.
- [OPEN — OVERNIGHT P0] Timeframe-aware observation identity, authoritative exchange sessions/calendar, automated 20-session boundary pairs, independently sourced catalyst/market evidence, and persistence/display of the final evaluator decision remain incomplete. These remain release blockers for Overnight readiness.

## 2026-10-29 — Live Alpaca REST verification and RTO payload-shape follow-up

- [VERIFIED LIVE BY USER] `/api/providers/status` reports Alpaca configured and `VALIDATED_DATA_RETURNED`, last successful validation at `2026-10-29T17:44:51.874Z`, 54 bars. This validates a non-empty response in that running API process, not every symbol/session/feed entitlement.
- [VERIFIED LIVE BY USER] `/api/market-data/bars` for SPY, 1m, UTC `2026-10-29T17:20:00Z`–`17:40:00Z`, Alpaca/IEX returned 21 bars, first/last timestamps exactly matched the requested bounds, gapCount 0, and no provider error. This confirms the live REST path returns usable data for that specific regular-session window.
- [VERIFIED LIVE BY USER] `/api/market-data/compare` returned the same 21 Alpaca bars with no error. Its persistedObservationCount was 0 because the preceding bars request had already persisted the observations; zero newly persisted records is not a fetch failure. Massive is unconfigured and should be treated separately.
- [ROOT CAUSE HYPOTHESIS — FIX IMPLEMENTED, NEEDS LIVE RETEST] The failed RTO after-hours response has top-level keys `bars`, `next_page_token`, and `symbol`. The adapter assumed `bars` must be a JSON array. It now also accepts an object under `bars` keyed by the requested symbol, while refusing to consume another symbol's array. Added a regression test for `bars: { "RTO": [...] }`. The user's provided error did not expose the nested `bars` value type or keys, so this is a strongly indicated response-shape mismatch, not yet confirmed as the exact runtime shape.
- [DONE — DIAGNOSTICS] If the response still does not match either supported shape, the exception now includes the top-level keys, `bars` value kind, nested `bars` keys, and requested symbol, with bounded provider code/message when present.
- [NEXT P0 — USER RETEST] Restart the API and retry RTO for UTC `2026-10-28T20:00:00Z`–`2026-10-28T23:55:00Z`. Verify that bars and source timestamps are from Oct 28 and inside the requested interval. If it still fails, capture the new sanitized shape diagnostic; do not infer after-hours coverage from the SPY regular-session test.
- [OPEN — OVERNIGHT P0] Timeframe identity in persisted observations, authoritative exchange-calendar/session-boundary generation, full 20-pair history evaluation, and persisted/displayed final decisions remain unfinished. Catalyst status remains unknown without a validated catalyst source; no READY decision may be inferred from missing evidence.

## 2026-10-29 — Alpaca live-response diagnostics and Overnight stale-result fix

- [DONE — CODE] The Overnight dashboard now clears all previously displayed bar counts/timestamps/gap values before each request and when a request fails. A failed request can no longer leave an older successful response looking like the result for the newly selected date window.
- [DONE — CODE] When Alpaca returns HTTP 2xx JSON without the expected `bars` array, the API now reports top-level response keys and sanitized provider `code`/`message` fields (bounded length) instead of only saying the bars array is missing. No credential/header values are included.
- [DONE — TEST ADDED] Added a fixture test for an unexpected 2xx payload, checking diagnostic content and that the adapter does not mark the provider validated. Fixture tests do not prove live Alpaca connectivity.
- [NEXT — USER LIVE TEST] Restart the API and request a small recent completed 1m window during regular market hours using `/api/market-data/bars`; compare it with `/api/market-data/compare`. Record HTTP status, provider/feed, requested UTC window, bar count, first/latest source timestamps and sanitized error fields. Do not share credentials or headers.
- [OPEN — DATA/ENTITLEMENT] The actual Alpaca response for the user's failed Oct 28 RTO after-hours window is not available to this repository session. We cannot conclude whether it was a feed entitlement issue, provider payload variation, a historical window limitation, or another cause until the new diagnostic response is captured.
- [OPEN — OVERNIGHT P0] Timeframe is still not part of persisted bar identity. Do not use stored observations for timeframe-specific Overnight metrics until that identity/query filter is implemented.
- [OPEN — OVERNIGHT P0] Authoritative exchange calendar/session boundaries and full 20-pair automated evaluation remain incomplete. Unknown catalyst evidence continues to block READY.

## Current implementation status — 2026-10-29 (Arizona time)

- [DONE — CI VERIFIED BASELINE] Prior workflow `40820328662` passed for commit `ff1e00450904fe0939cbf62c5bfabbefced0a2ea`. History-backed service/test/API commits `65603d9`, `76d90c2`, `cc1d852` each have green CI runs. Catalyst fail-closed fix `9ae6b79f43c07293b545b6447fdc82104b7f2790` and newest docs are pending their own workflow results.
- [IMPLEMENTED — CI PENDING] Added `BoringOvernightHistoryEvaluationService`. It replaces caller-entered overnight win rate, 20-session average, 5-session average, and 20-session median with values derived from the history calculator. If history is incomplete or too short, all four fields are set to null; no manual-value fallback is permitted.
- [IMPLEMENTED — CI PENDING] Added `POST /api/boring-overnight/evaluate-with-history`: reads stored bars, calculates history using explicit UTC close/open boundaries, resolves MCC's local exclusion list, evaluates the hard gates, and appends the resulting candidate snapshot. Because no validated catalyst provider is connected, the endpoint forcibly sets `CatalystStatus=UNKNOWN` regardless of caller input, preventing READY until catalyst evidence is wired. Other non-history inputs are still caller supplied.
- [IMPLEMENTED — TESTS ADDED, CI PENDING] Added tests that prove history values override caller-supplied metrics, incomplete history clears those metrics, and symbol mismatches are rejected. Also reject ambiguous history calculations that span multiple feeds when `feed` is omitted; this prevents combining unlike IEX/SIP observations into one overnight statistic.
- [NEXT P0] Add authoritative exchange-calendar/session boundary generation and validation (holidays, half-days, DST, provider bar timestamp convention). Until then the API requires explicit boundary timestamps.
- [NEXT P0] Replace externally supplied catalyst status, 5M Permission/structure, 1M state, price/SMA/volume and freshness with independently sourced, timestamped evidence. Unknown catalyst status must continue to block READY.
- [NEXT P0] Validate corporate-action adjustment and full eligible GTC_EXT window coverage; compare MCC ATR/extension and boundary pairing against matching Thinkorswim/provider data.
- [NEXT P1] Integrate the persisted evaluated snapshot into scanner, Morning Brief and MarketWatch surfaces after evidence contracts and tests are stable.
- [USER ACTION — LOCAL VALIDATION] After CI passes, pull the branch and run the API tests described in `docs/Next-Steps-and-User-Inputs.md`. Do not send credentials.

## Local Alpaca setup and XRPN evidence — 2026-10-23

- [DONE — SOURCE CHANGE, CI PENDING] Added API `UserSecretsId`, removed empty credential placeholders from tracked `appsettings.json`, documented local User Secrets commands, and standardized Alpaca credential resolution on ASP.NET Core configuration.
- [DONE — TESTS ADDED, CI PENDING] Added `AlpacaMarketDataAdapterTests` for configured credentials/request headers/bar parsing and missing-credentials behavior. These are fixture-based tests and do not contact Alpaca.
- [DONE — EVIDENCE RECEIVED] User supplied the XRPN 2026-10-02 screenshot showing the normal 5M study, Authority Diagnostic V2 and readable target time axis; top-left chart date is explicitly excluded per user clarification. Do not request it again.
- [USER ACTION — NEXT] Create a free Alpaca account, configure credentials locally using `src/MomentumCommandCenterV2.API/README.md`, run a small real API smoke test, and share only sanitized status/errors/timestamps. Do not share credentials. Massive and paid plans remain deferred.
- [OPEN] Check CI/build/test results at the latest branch head after these commits. A source edit or successful prior build does not establish current-head verification.

# Momentum Command Center V3 — Master To-Do

**Canonical location:** `docs/Master-To-Do.md`  
**Reviewed:** 2026-10-23 (Arizona time)  
**Branch:** `feature/opportunity-quality-classification`


## UPC extension-danger example — 2026-10-23

- [PARTIAL — EVIDENCE REVIEWED] Added `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md` for the user's UPC 5M/1M screenshots and event facts. The reported state is 5M `Perm=1`, `PBLOCK=0`, while 1M says `CANDIDATE BLOCKED: EXTENSION DANGER`, `IN-TREND CANDIDATE`, `EXT=2.35`. This is consistent with a valid higher-timeframe permission and a blocked 1M entry; do not weaken the gate.
- [DONE — USER CONFIRMED] Screenshots/event are from 2026-10-22 and Thinkorswim chart timezone is Arizona. This resolves the date/timezone question only; it does not classify the missing intervals or prove study-state parity.
- [OPEN] [PARTIAL] Reviewed `data/StrategyReports_UPC_102226.csv`: this is a strategy report covering 09:37–09:47, not a continuous OHLCV export; it cannot establish the reported 09:16–09:36 missing-bar intervals. Obtain a full bar export or exact timestamp inspection. Missing chart bars do not prove zero volume or no movement; never synthesize them.
- [OPEN] Confirm installed 1M source and `BLK=6` mapping. Study names alone do not prove installed source parity.

## Provider comparison refresh — 2026-10-23

- [DONE — RESEARCH DOCUMENTATION] Expanded `docs/Provider-Comparison.md` to compare free and paid tiers against ultimate MCC requirements. Free tiers are suitable for plumbing/prototyping only; they are not approved as live authority feeds.
- [RECOMMENDED SHORTLIST] Alpaca Algo Trader Plus ($99/mo public list price) versus Massive Stocks Advanced ($199/mo public list price), pending proof of fit, account entitlements, licensing, small-cap/premarket coverage and TOS comparison.
- [OPEN] Run proof-of-fit tests before subscription/provider selection. No provider is selected or connected.

## Status and migration note

This file preserves the prior consolidated To-Do text below, after reviewing all 180 lines. Completed repository tasks are marked in the current status addendum at the end; unresolved ThinkOrSwim/chart validation tasks remain open because source-controlled files do not prove which study is installed in the user's TOS workspace. The prior reference to a Boring Overnight TODO under `/Trading System/` is not present in this repository; the criteria are now documented in `docs/BoringOvernight.md`. The current repository's `docs/Charts/1M/` and `docs/Charts/5M/` studies are the source-controlled references; do not infer that they match the installed study without comparison.

# THIS IS A .MD FILE

# Momentum Command Center V3 — Consolidated Master To-Do
**Updated:** 2026-10-15 (Arizona time)  
**Purpose:** Canonical master list. Consolidates the 2026-10-06 master TODO, the 2026-10-13 Boring Overnight Decision Column TODO, and subsequent documented project status. The dedicated Boring Overnight TODO remains a detailed companion; this master list must be updated whenever that companion changes.

## Status key
- [NOW] Current priority
- [NEXT] Next in sequence
- [HOLD] Deliberately deferred
- [VALIDATED] Substantially validated, not a claim of profitability
- [DONE] Completed and verified
- [UNVERIFIED] Must be checked against repository/current installed artifact

## 1. 5M authority / Permission — [NOW]
- [NOW] Reconstruct the known XRPN 10/2 Permission `1 -> 0` transition at 12:45 Arizona chart time. Known facts already supplied: Permission 0 premarket; Permission 1 plus BUY at 8:25; Permission 0 by 12:45. Do not ask user to rediscover these transitions.
- [EVIDENCE REVIEWED, 2026-10-22] First screenshot (`Screenshot 2026-10-22 161411.png`) showed the normal 5M study and Authority Diagnostic V2 working on morning bars, but not the target transition.
- [TARGET WINDOW CAPTURED — BRANCH INDICATED, 2026-10-22] The follow-up Project Source screenshot (`Screenshot 2026-10-22 161705.png`) shows XRPN 10/2/26 5M around 12:10–12:55 Arizona chart time, with both studies visible. Around the Permission revocation, red diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, and `EOD=0`; the displayed conditions include `VWAP=0`, `EMA9=0`, and `EMA9>20=0`. In Authority Diagnostic V2, `PBLOCK=2` is the persistent-major-failure branch, so the screenshot strongly indicates that branch caused the revocation rather than EOD. Verify the exact ordering/priority in source before changing logic; do not infer a defect from the revocation alone.
- [AUDIT RESULT — 2026-10-22] The follow-up screenshot indicates `PBLOCK=2` (persistent major failure) with `EOD=0` on the revoked bars. `docs/Charts/5M/MainStudy.txt` confirms `majorFailureRaw = price < dailyVWAP AND price < ema9 AND ema9 < ema20 AND ema20 < sma50`; `majorFailurePersistent` requires this condition on `majorFailureBars` bars (default 2). The Permission latch checks EOD first, then persistent major failure, then persistent structural damage, and recovers only when `bullishPermissionRaw` is true. This matches the observed PBLOCK mapping and does not by itself demonstrate a logic defect. Preserve Permission logic; proceed to the 5M→1M handoff audit.
- [DONE — USER EVIDENCE RECEIVED, 2026-10-23] The uploaded XRPN screenshot shows the 5M chart on 2026-10-02 with both the normal 5M study and Authority Diagnostic V2 visible and a readable time axis spanning approximately 12:00–13:35 Arizona; the 1M chart is also dated 2026-10-02 and spans the requested area. Per user clarification, ignore the date displayed on the separate top-left chart. The screenshot covers the requested 12:30–13:00 window. Use this as the evidence artifact for the Permission-revocation audit; do not request the same screenshot again.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- [DONE — SOURCE AUDIT ONLY, 2026-10-22] Compare 5M authority predicates with the 1M reconstructed 5M layer and document concrete raw-Permission, structural-damage, and major-failure differences in `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`. No ThinkScript edits made; installed-study parity and behavioral contract remain open.
- [NEXT] Validate fresh candidate, in-trend candidate, continuation, confirmation, event-armed READY, expiration, and re-entry paths against same-bar installed-study evidence and the intended 5M gate.
- [NEXT] Test whether RUNNER / WANING / SWING suppress legitimate 1M opportunities; source audit alone does not establish behavior.
- Verify extension danger blocks chasing without falsely declaring the 5M thesis dead.
- Preserve architecture: 5M never references 1M; 1M may reference 5M.

## 3. 1M execution pipeline — [HOLD broad edits; audit targeted behavior]
- [UNVERIFIED] Reconcile current user-installed study with Library copies: `V14_4_9_AggressiveSetupHandoff_CompositeBubble` (known compile-clean diagnostic baseline, duplicate files) and `V14_4_10_AggressiveAuditOnly` (later audit-only files, duplicate copies). Filename/modified date alone does not establish production baseline.
- Trace actual transitions and bars before editing; avoid version churn that changes only labels or does not address a demonstrated defect.
- Validate fresh execution candidate, in-trend candidate, continuation candidate, confirmation event, event-armed opportunity, finite READY window, READY event, candidate-block reasons, execution expiration, extension warning/danger, swing reclaim/re-entry.
- Preserve negative test: opportunity can exist while READY is blocked by extension danger. This is not automatically a broken bullish thesis.
- Validate historical debug on scrolled bars; use per-bar bubbles for history rather than relying on right-edge AddLabel values.
- Distinguish `NO NEW SIGNAL` from HOLD or EXIT. Missing signal bubbles are not themselves a SELL signal.

## 4. Runner management — [NEXT after authority/handoff validation]
- Validate RUNNER RISING -> RUNNER; RUNNER -> RUNNER RISING; RUNNER -> RUNNER WANING; WANING -> SWING ZONE; SWING recovery; and WANING/SWING -> actual thesis failure.
- Runner waning is caution, not automatic SELL. Preserve goal of holding valid runners rather than repeatedly taking tiny profits.
- Define and test explicit position-management/exit behavior separately from entry readiness.

## 5. Boring Overnight decision column / chart study — [NOW: specification first]
Detailed companion: `docs/BoringOvernight.md`. Preserve its complete criteria and update it alongside this master.

### Required pipeline
1. Overnight Edge Score (historical overnight profitability/edge).
2. Boringness Score (consistency and controlled risk/return distribution).
3. Event/Catalyst Risk (earnings, M&A, regulatory/government decisions, corporate actions, material news, sector/regime shocks).
4. Current-Day Qualification (abnormal intraday extension, unusual volume, fresh catalyst spike, excessive ON move, deterioration from normal behavior).
5. 5M structure/permission where appropriate.
6. 1M entry timing where appropriate.
7. Decision status: `READY`, `WATCH`, `BLOCKED`, `DATA?`, plus reason codes and data freshness.
8. Zero position is valid; never silently treat missing/stale data as READY.

### Initial gates to preserve pending validation
- Price >= $5.
- 20-day average volume >= 500,000 shares/day.
- Overnight win rate >= 60%.
- 20-day average ON return >= +0.10%.
- 5-day average ON return > 20-day average ON return under current cheat-sheet rule.
- Price above 20-day SMA.
- Not on the maintained Schwab exclusion list.
- No material event/catalyst risk incompatible with boring overnight holding.

### Initial Boringness score (100 points)
- ON win % 25; median ON % 20; 20D average ON % 15; 5D average ON % 10; consistency 10; overnight volatility 10; average volume 5; price 5.
- Bands: 90–100 PERFECT BORING; 80–89 VERY GOOD; 70–79 POSSIBLE; 60–69 AGGRESSIVE; below 60 REJECT / NOT BORING ENOUGH. Score never overrides hard gates.
- Preferred ranges: current ON move <3% (>=3% increases penalty/review); 20D average +0.10% to <2.0%; 5D average positive to <5.0%; consistency >=70% preferred and >=80% excellent; median should be positive and reasonably close to mean.
- Penalize abnormal intraday moves/volume, sharp extension, fresh news/earnings/catalyst, M&A/regulatory/government decisions, corporate actions, sector/regime shocks, deteriorating 5M structure.
- Existing exclusion list includes `SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE`; other maintained lists contain additional exclusions. Reconcile all current sources; do not assume this subset is exhaustive.

### Implementation sequence
- [NOW] Write column specification/data contract: field definitions, windows, session boundaries, source, freshness, missing-data handling, reason codes, extended-hours assumptions.
- Implement only calculations ThinkScript can reliably support; label proxies honestly. Split into coherent columns if one column truncates critical information.
- ThinkScript cannot establish catalyst-clear status reliably or retain full authoritative rolling history. Missing event data => DATA? / manual review, never READY.
- Validate known winners, losers, catalyst spikes, overextended candidates, and all hard-gate failures. Compare same-symbol/same-date ThinkScript results against independently calculated C# results.
- Add chart study only if it explains information not usefully shown by the column.
- Track false positives, missed opportunities, ON win rate, average/median return, adverse excursion, spread/slippage. Do not infer profitability from compilation or a few chart examples.
- No automatic order placement until a separate risk/order-control workflow is specified and tested.

## Extended momentum / continuation lane (user requirement, 2026-10-22)

- [RESEARCH REQUIREMENT] Do not treat a large current-day gain or an EXT warning as an automatic permanent rejection. Preserve the existing risk warning, but evaluate a separate **Extended Momentum / Continuation Watch** path for stocks already up roughly 10–15% when the user begins reviewing them.
- [DESIGN ONLY — NOT A BUY SIGNAL] This path should combine fresh price/volume continuation evidence, catalyst/news context with source and timestamp, tradability/liquidity/spread checks, current 5M Permission, and current 1M execution state. News sentiment or a strong narrative alone must never grant eligibility.
- [SEPARATE STATES] Distinguish (1) extended and not currently executable, (2) continuation remains credible and should be watched, and (3) a fresh, valid entry event exists under the unchanged 5M/1M gates. A continuation candidate can stay on a pullback/reclaim watchlist without being marked EXECUTION READY.
- [SAFETY] Never bypass the exact 1M extension formula or turn red EXT into a green/permission state. Any alternative entry policy needs explicit risk limits, gap/halts/spread safeguards, an auditable reason code, and historical/out-of-sample validation before it can affect actionable ranking.
- [OPEN] Determine whether the existing red In-trend candidate bubble is a diagnostic, setup classification, or actual eligibility state by checking the exact installed ThinkScript and chart examples. Do not claim it means technically safe to buy until its formula and semantics are verified.
- [OPEN] Test whether continuation logic should allow a separate re-entry trigger (for example a defined pullback/reclaim or consolidation breakout) rather than weakening the original EXECUTION READY gate.
- [TIME CONTEXT] User's 08:30 reference is Arizona local time. Arizona generally does not observe US daylight saving time, while US Eastern market hours do; therefore the US market open shifts between 07:30 and 08:30 Arizona time depending on the season. Label dashboard/session times with timezone and market-session context.

## Broker integration boundary (user preference, 2026-10-22)

- [DECISION] Schwab website/manual order entry is the preferred baseline. Do not implement Schwab API access, credential handling, or automated order submission merely for convenience.
- [POSSIBLE FUTURE UX] Consider a clearly labeled Open Schwab / open trade-ticket handoff only if a legitimate, supported URL workflow is verified. Do not imply that it can prefill symbol, quantity, account, or order type until confirmed.
- [NOT APPROVED] Do not implement a one-click Buy Maximum Amount control. It is too easy to submit a mistaken oversized order, and broker-side buying power/margin/order constraints cannot be safely inferred from MCC's local data. If a sizing helper is later requested, make it a non-submitting calculator with explicit custom/max budget choice, visible price and share calculation, reserve/cushion, and a final manual review in Schwab.

## Dashboard feedback and scanner feed decision (2026-10-22)

- [PARTIAL — UI FEEDBACK SLICE] The static dashboard now has an explicit system-readiness panel and a local feedback notebook so the user can review layout and behavior before provider integration. Notes persist only in browser local storage and can be exported as JSON; they are not uploaded automatically.
- [NOW] Use dashboard feedback to prioritize UI changes. Keep demo/unknown labels visible; never make mock data look live.
- [BLOCKED — PROVIDER NOT SELECTED] Thinkorswim is not connected as a live scanner/API feed. It is the source of chart-study behavior and validation evidence. No live scanner provider/feed is selected or verified in repository sources.
- [NEXT] Compare eligible feed/provider options against real-time 1M/5M OHLCV, scanner API coverage, timestamps/session/ETH behavior, historical data, entitlements, licensing, rate limits, and outage/reconnect behavior before implementing an adapter.
- [HOLD] Do not bind the dashboard to an assumed Thinkorswim endpoint or a guessed third-party API.

## Scanner integration readiness and EXT prequalification (2026-10-22)

- [NOW] Define a provider-neutral scanner-event contract before wiring any provider: normalized symbol, scanner group, source/event ID when available, first-seen and observed timestamps, data-as-of timestamps, raw-field provenance, schema/version, freshness, current status/reason codes, and evaluator/study version. Discovery is not an entry signal.
- [NOW] Add extension qualification to the scanner design. The user's reported problem is that red-state 1M EXT / extension-danger often coincides with no EXECUTION READY bubble, wasting chart-review time. Keep this as a measured workflow hypothesis until timestamped examples quantify it.
- [NOW] Default actionable scanner queue must deprioritize/filter candidates whose current extension exceeds the exact versioned 1M study envelope. Preserve them in a recoverable WATCH_PULLBACK list; do not permanently discard a symbol merely because it is extended now. Pullback requalification requires fresh data, a new valid event, and unchanged 5M authority / 1M execution gates.
- [NOW] Distinguish extension warning from extension danger. Never substitute a guessed percentage threshold or the provisional OpportunityQualityAssessor threshold for the exact ThinkScript ATR-extension definition. Missing/stale extension inputs => DATA?, never “not extended” or ACTIONABLE.
- [BLOCKER] Before live scanner gating, complete exact-study indicator parity for ATR/extension and related gates; classify the 31 irregular 1M intervals; finish full-session 1M-to-5M reconciliation; repeat RTH/extended-hours and Arizona/Eastern DST tests. See docs/Validation/MCC-OHLCV-20261020.md.
- [PARTIAL — CONTRACT SCAFFOLD COMMITTED 2026-10-22] Added `ScannerDiscoveryEvent` and `IScannerDiscoverySource` in Application. This defines a provider-neutral DTO/source boundary only; append-only storage, freshness/deduplication validation, qualification workflow, tests, and provider adapter remain open. No build/test result has been verified for this commit.
- [NEXT] Implement extension prequalification only after its inputs and thresholds have parity tests. Verify warning/danger behavior, pullback requalification, stale-data handling, and that scanner filtering never changes the 5M thesis.
- [BLOCKED — PROVIDER DECISION] Choose a provider only after API coverage, licensing, rate limits, latency, credentials, and failure behavior are specified. Data not found in sources: a selected live scanner provider and verified access contract.
- [NEXT] Run shadow mode against timestamped scanner discovery, first 5M Permission, first 1M READY, extension blocks, pullback/re-entry, and outcomes. Measure chart reviews avoided and valid opportunities hidden before enabling hard exclusion by default.
- [HOLD] Do not connect live scanner results to orders or let scanner/quality ranking override 5M authority or 1M READY.

## 6. Scanners / daily workflow — [PARTIAL: local workbench implemented]
- [DONE] Add a local scanner/watchlist workbench with four scanner groups, candidate statuses/reasons, local CSV import, and browser-local watchlist persistence. See `docs/Scanner-and-Watchlist-Workbench.md`.
- [HOLD] Reconnect the Command Center to a live scanner workflow until 5M/1M behavior is sufficiently validated and a provider-neutral scanner contract exists.
- Scanner groups: A Momentum Hunter; B Breakout Trigger; C Reclaim/Continuation; D Market Movers/Discovery (daytime, not Overnight).
- Maintain regular-session default and separate overnight workflow explicitly.
- Daily Gainer Review after 6 PM Arizona time: regular-session biggest intraday gainers only, exclude overnight holds; record scanner appearances/timing, chart states, news/catalysts, Schwab tradability/exclusions, and miss classification.
- Preserve timestamped scanner screenshots and actual trades for V2/V3 validation.
- Keep Schwab-unavailable/undesired securities excluded across relevant scans; reconcile maintained lists rather than replacing them with a partial list.

[NOW] Scanner discovery timestamp and missed-move audit
	• Record the earliest known timestamp each symbol appears in each scanner, including scanner/group name and the scan result at that time.
	• Record when the symbol is first added to the working watchlist, if different from its scanner appearance.
	• Preserve timestamped scanner snapshots or append-only candidate logs so discovery history is not lost when scanner results change.
	• For each candidate, correlate discovery time with:
		○ First 5M Permission=1 and its EXT value.
		○ First 5M BUY / PREPARE TO BUY / runner state.
		○ First 1M EXECUTION READY.
		○ READY expiration and candidate-block reasons, including extension danger.
		○ Subsequent runner waning, pullback, swing recovery, or thesis failure.
	• Calculate the price move already realized between the earliest recorded discovery and first actionable READY, with a clearly defined reference price and session boundary.
	• Classify missed opportunities as discovery too late, qualification too late, execution blocked/expired, no valid setup, data unavailable, or unknown.
	• Track scanner misses and false positives across historical sessions before changing scan thresholds or signal logic.
	• Keep regular-session DGR separate from Boring Overnight tracking.

From <https://chatgpt.com/g/g-p-6ad0f6bcb72c81918e3cdd194cda6630-momentum-command-center-v3/c/6ad0f88f-d89c-83e8-9eeb-9923a03fc1d2> 





## 7. C# V3 implementation — [NEXT / data and replay foundation]
Repository supplied by user: https://github.com/themailcomeshere/MomentumCommandCenterV2.git
- [DONE] Verify the actual repository branch, latest commit, solution/project tree, framework, namespaces, and build/test commands directly against the repository. The working branch is `feature/opportunity-quality-classification`; it targets .NET 8.
- [DONE] `ITradeImporter`, `SchwabTradeImporter.ImportFile()`, and `TradeImportResult` exist in the current solution.
- [PARTIAL] Importer validation and xUnit tests exist. Expand coverage for any uncovered duplicate/idempotency, partial-failure, and diagnostic cases when importer work resumes.
- [NEXT] Persistence, provider-neutral indicator snapshots, decision logging, market-data parity, and later production UI remain future work. This milestone adds raw-bar replay primitives only.
- Keep boundaries clean: raw import -> normalized application/domain models -> persistence -> bar/indicator calculations -> deterministic 5M authority -> 1M execution -> separate overnight decision -> logged outcome/backtest.
- Use pure, testable calculations/state transitions; make data source, time zone, session boundary, missing-data and freshness behavior explicit.
- C# should own/consume authoritative rolling ON stats, median/mean and 5D/20D metrics, consistency/volatility, scores, catalyst metadata, freshness, reason-coded decisions and outcome logs.
- Record candidate snapshots, status/reasons, intended and actual entry/exit, and outcomes for audit and threshold evaluation.
- Preserve `///` XML documentation with flowerboxing. Deliver complete coherent code and next-step instructions together.
- No claims that live brokerage integration, external event feeds, backtesting, automated orders, or production UI exist until verified in source/tests. No automated orders until separately specified and tested.
- Ensure any intentional divergence between C# and TOS behavior is documented and covered by comparison tests.

## 8. Repository README / docs — [PARTIAL]
- [DONE] README and architecture/data contracts have been refreshed from verified repository contents. The original README is preserved at `docs/archive/README_ORIGINAL.md`.
- [PARTIAL] README now documents .NET 8 architecture, replay command, demo UI, safety boundaries, and core roadmap. Provider configuration, scanner interfaces, and deployment workflow remain unimplemented and are not claimed.
- Keep source-of-truth boundaries explicit: current TOS studies are live behavioral reference under validation; C# is intended to make logic/data/decisions reproducible and testable.

## 9. Historical validation / diagnostics — [NEXT]
- Resolve known XRPN 10/2 Permission revocation before collecting new-day evidence for that question.
- Then use controlled sessions to gather transition evidence, not assume production readiness.
- Historical diagnostics must show per-bar state accurately when scrolled; distinguish current/right-edge state from historical bar state.
- [DOC ADDED] Use `docs/Charts/1M/DiagnosticStudy.txt` as the audit procedure for historical 1M READY bubbles. It documents the existing per-bar diagnostics in `MainStudy.txt`; it does not alter signal logic or claim the installed Thinkorswim study matches the repository source.
- Compare scanner candidates, signals, fills, exits, and outcomes; classify false positives, missed trades, invalid entries, exits too early, and missed runner continuation.
- Validate Arizona/Eastern session conversions and EOD logic across DST/standard-time boundaries.

## 10. User interaction / operating rules
- Correctness and thoroughness over expediency. Review all relevant Library items, not just the newest one.
- On screenshot analysis, identify date/session boundary and timeframe; explicitly separate observed facts from inference and unknowns.
- For study changes, use handoff heading “1. What I changed”; explain screenshot purpose, exact Study/Library artifact, user actions, and next steps.
- Do not request screenshots or transitions already provided. Do not misstate current Arizona time or tell user to wait for a future session when already in it.
- Do not create new study versions without a demonstrated defect and explicit test purpose; confirm compilation and chart effect before calling it successful.
- Trading signals are evidence, not guarantees. `EXECUTION READY` does not replace position sizing, risk limits, or exit rules. Distinguish HOLD, EXIT, and NO NEW SIGNAL.

## 11. Immediate order of operations
1. [NOW] Build the current branch and run the full xUnit suite; GitHub file edits do not execute .NET.
2. [NOW] Reconcile the repository's 1M/5M studies with the actually installed ThinkOrSwim versions; do not infer identity from filenames.
3. [NEXT] Define timestamp open/close semantics, session/extended-hours policy, and DST behavior for historical bars.
4. [NEXT] Add sanitized real historical fixtures and golden indicator-parity tests against the source studies.
5. [NEXT] Connect replay frames to V2 signal evaluation without changing existing signal rules; log transitions and opportunity assessments.
6. [NEXT] Investigate the known XRPN 10/2 Permission revocation from existing chart evidence; keep this separate from speculative edits and replay-foundation work.
7. [NEXT] Continue targeted 5M/1M handoff, READY expiration/extension, runner, and exit-behavior validation.
8. [NEXT] Resume scanner-discovery correlation, trade-quality analytics, and V1/V2 historical validation after data parity is trustworthy.
9. [HOLD] Production UI/provider/news integration and any order automation until their contracts and safety requirements are separately specified.

---

## 12. Repository and documentation status update (2026-10-16)

### Done in this branch
- [DONE] Inspect the current branch and latest commits, including separated Models, development rules, chart-study source files, and documentation additions.
- [DONE] Add XML flowerboxing with purpose/when-called context to current classes and methods; a follow-up source audit found no unflowerboxed method signatures in the inspected C# files.
- [DONE] Add raw timestamped OHLCV bar contract and deterministic replay foundation; replay emits frames at 1M timestamps and attaches only 5M bars with timestamp <= the frame timestamp.
- [DONE] Add CSV ingestion with explicit timezone-offset requirement, invariant numeric parsing, and duplicate/malformed OHLCV validation.
- [DONE] Add CLI replay command and synthetic CSV fixture.
- [DONE] Add unit tests for no-look-ahead, duplicate bars, and invalid OHLC.
- [DONE] Add `docs/Market-Data-and-Replay-Contract.md`.
- [DONE] Expand `docs/BoringOvernight.md` with Design vs Use, gates, score components, ranges, catalyst risk, and limitations.
- [DONE] Populate `docs/NewChatHandoff.md`.
- [DONE] Scaffold static front end under `frontend/`; it is demo-only and not connected to market data or a broker.
- [DONE] Put development rules in `docs/rules/abpDevRules.md` (already moved by user).

### Still open / not verified by this branch
- [NOW] Run the local build and all tests after the new replay/adapter/CLI changes; GitHub file operations do not run .NET.
- [DONE] Add XML flowerboxing to the current C# class/type and method declarations identified during the repository audit; continue enforcing this on all new and changed code.
- [NOW] Reconcile the source-controlled chart studies against the actual installed 1M/5M ThinkOrSwim studies; filenames and repository state do not establish runtime version.
- [NEXT] Add sanitized historical 1M/5M fixtures and provenance.
- [NEXT] Define bar timestamp open/close convention, session calendar, extended-hours policy, regular-session VWAP reset, and DST handling.
- [NEXT] Port TOS indicator formulas with golden parity tests before connecting calculated values to the signal engine.
- [NEXT] Connect replay frames to existing V2 signal evaluation without changing signal behavior; persist transitions and opportunity assessments.
- [NEXT] Add daily trading-session grouping, MFE/MAE, hold-time/entry-to-peak/exit-giveback, runner/early-exit/missed-runner analysis, limit-price analysis, scanner events, and V1/V2 historical validation.
- [NEXT] Reconcile all Schwab exclusion lists; the documented list is not guaranteed exhaustive.
- [HOLD] Live market-data provider, news/catalyst feed, production front-end/API, and broker connectivity until the contracts and provider are chosen.
- [OUT OF SCOPE] Automated order placement and autonomous Schwab submission.

### Line-by-line disposition of the prior checklist

- Sections 1–4 (5M permission, 5M→1M handoff, 1M execution, runner management): retained as unresolved TOS validation work. No speculative study edits were made.
- Section 5 (Boring Overnight): strategy criteria migrated into `docs/BoringOvernight.md`; implementation and historical validation remain open. Its former companion path under `/Trading System/` did not exist in this repository.
- Section 6 (scanners/Daily Gainer Review): retained as future scanner-event and discovery-timestamp work; no scanner data source is connected.
- Section 7 (C# V3 implementation): repository was inspected; importer/trade-analysis work already exists in the solution. Market-data/replay is now a first slice, not a complete backtest. Remaining persistence, historical market correlation, event sourcing, and provider work stay open.
- Section 8 (README/docs): README and the replay contract were updated from verified repository content; this master list and handoff now exist under `docs/`.
- Section 9 (historical validation): retained. Replay primitives exist but no source-study parity, fills, or P&L simulation is claimed.
- Section 10 (user interaction/operating rules): retained as operating constraints; development-specific rules are in `docs/rules/abpDevRules.md`.
- Section 11 (immediate order): reprioritized to build/test this milestone first, then TOS version/behavior audit, then data/session/indicator parity, then signal/replay integration, and later trade/scanner analytics. The historical XRPN 10/2 revocation remains a separate unresolved investigation requiring chart evidence; it is not solved by this C# replay slice.
- The old README-derived trade importer and statistics bullets remain relevant background; implemented functionality is described in `README.md`, while market-dependent performance judgments remain unverified until historical bars are available.


- [DOC ADDED] `docs/Research/ClearStreet-Comparison.md` records relevant product patterns (unified decision surface, multi-factor discovery, transition alerts, risk context, API contracts) and explicitly excludes broker infrastructure/automated execution from MCC scope.
- [RULES UPDATED] `docs/rules/thinkscript.md` now reflects MCC's 5M-authority/1M-execution hierarchy, historical-bar semantics, formula-fidelity requirements, and validation gates; generic `AddOrder()` advice no longer implies orders belong in a chart study.

## 13. Boring Overnight source reconciliation

The original master list and `docs/BoringOvernight.md` agree on the initial 100-point weights, hard gates, score bands, preferred ranges, “too hot” penalties, event/catalyst caution, 5M/1M review, and zero-position principle. The exclusions list is explicitly partial pending reconciliation. Overnight Edge and Boringness must remain separate; no final score should override a failed/unknown hard gate.

## 14. Current execution checkpoint (2026-10-18)

- [BLOCKED HERE / STILL OPEN] Attempted to clone the current branch and run `dotnet test MomentumCommandCenterV2.sln`; this environment could not resolve `github.com`, so the clone failed before restore/build/test. This is an environment/network limitation, not a code failure and not a passing test result.
- [NOW] Use the repository's GitHub Actions `.NET build and test` workflow to validate the current branch, or run the documented Release restore/build/test commands locally. Confirm the run corresponds to the current branch head before marking this item DONE.
- [NEXT] Once build/test is green, reconcile the installed Thinkorswim 1M/5M studies against `docs/Charts/1M/MainStudy.txt` and `docs/Charts/5M/MainStudy.txt`; then capture sanitized historical bars with provenance before indicator parity work.
- No signal logic, ThinkScript thresholds, or replay semantics were changed during this checkpoint.

## 15. Opportunity classification: practical verification and news boundary (2026-10-18)

- [DOC ADDED] `docs/Opportunity-Classification-Verification.md` defines a shadow-mode workflow, required input provenance, outcome comparisons, acceptance gates, and the boundary between a news provider, web search, and an LLM.
- [USER-REPORTED PASS] User reports all unit tests pass. This is recorded as user-reported; no local build/test execution was possible in the prior environment attempt. Do not confuse unit-test success with live market-data parity or profitability.
- [NOW] User is handling sanitized historical 1M/5M fixtures and provenance, plus session/timezone definitions.
- [NOW] Implement MCC-owned OHLCV export contract/parser and preserve the legacy `kg_EveryTickSOHLCP` report as a rejected negative fixture. Complete Thinkorswim compilation, timestamp/order-name verification, native-export comparison, and consecutive 1M/5M chart parity before treating any export as production-ready.
- [NEXT] Implement golden indicator-parity tests against exact versioned ThinkScript studies, then connect replay to the existing V2 signal engine without changing rules.
- [NEXT] Build shadow-mode opportunity snapshots and outcome linkage only after timestamp/session/indicator parity. Keep classification diagnostic-only until out-of-sample evaluation demonstrates value over the READY-only baseline.
- [HOLD] News/catalyst provider selection and AI-assisted headline classification until coverage, licensing, latency, costs, provenance, and unknown/stale behavior are specified. Current repository has no verified live-news or per-symbol ChatGPT/Google query integration.

## 16. MCC-owned OHLCV export checkpoint (2026-10-20)

- [DONE] Add the uniquely named `MCC_OHLCV_V1` strategy artifact with explicit date, Eastern-time session, aggregation, actual-volume payload, visible diagnostics, and simulated close/reopen report transport.
- [DONE] Add the strategy-report parser and reject legacy `kg_EveryTickSOHLCP` rows, transport-only reports, malformed OHLC, invalid timestamps, negative volume, duplicates, and symbol/timeframe mismatches.
- [DONE] Validate the repository against successful 1M and 5M report shapes. `StrategyReports_XRPN_102026_933.csv` contains 355 1M payload rows; `StrategyReports_XRPN_102026_5m_offset_1027.csv` contains 81 5M payload rows.
- [DONE] Enforce that `aggregationLabel` matches the actual Thinkorswim chart aggregation; a mismatch must emit no payload rows and show a diagnostic mismatch state.
- [DONE] Add dependency-free PowerShell validation scripts under `tools/` for single-report structure, 1M/5M export comparison, and report-versus-payload timestamp offsets.
- [DONE] Add `tools/New-MccChartParityTemplate.ps1` and generate first-20-row 1M/5M comparison templates under `data/validation/`.
- [DONE] Add a local scanner/watchlist front-end workbench without claiming live provider, news, broker, or order connectivity.
- [DONE] Record automated 1M/5M validation results in `docs/Validation/MCC-OHLCV-20261020.md`: 52 complete 5M windows reconstructed from 1M rows match OHLCV; 29 windows are incomplete; report timestamps are transport metadata.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 1M sample contains 20 consecutive bars (13:28–13:47 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; rows with precision differences are labeled `ROUNDING ONLY`. See `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv`.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 5M sample contains 20 consecutive bars (13:20–14:55 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; precision differences are labeled `ROUNDING ONLY` where applicable. See `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Constituent 1M-to-5M reconstruction is independently possible only for the overlapping complete 1M windows in the supplied 1M sample; the full-session reconciliation remains open.
- [VALIDATED] The `_UPDATED_3.csv` 1M and 5M files provide 20 consecutive payload rows each and now include independent chart OHLCV values and row-level findings. This completes the requested manual chart sample for those two windows; it does not by itself classify the separate 31 gaps in the broader 1M export.
- [DONE — SAMPLE PARITY] Independent chart OHLCV values are supplied for 20 consecutive 1M bars and 20 consecutive 5M bars in the `_UPDATED_3.csv` files. The broader 1M export's 31 gaps remain unclassified and require separate evidence.
- [VALIDATED — OVERLAP CHECK] The three complete shared 5M windows beginning 13:30, 13:35, and 13:40 UTC reproduce the 5M payload OHLCV exactly when their five constituent 1M payload bars are aggregated. The 13:45 window is not fully testable from this 1M sample because 13:48 and 13:49 are outside its range. Details: `docs/Validation/MCC-OHLCV-20261020.md`.
- [DONE — CLARIFIED] The raw Strategy Reports remain payload-only and are not completed parity evidence. The completed manual comparison evidence is in `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv` and `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Do not revert to the original templates or treat `_UPDATED_2.csv` as the latest completed sample.
- [USER-CONFIRMED DONE LOCALLY — 2026-10-22; REPOSITORY EVIDENCE SYNC PENDING] User reports that all 31 irregular 1M intervals were reviewed and classified locally, full-session 1M-to-5M reconciliation was completed, and RTH/extended-hours plus Arizona/Eastern DST checks were completed; observed outcomes were MATCH or ROUNDING ONLY. Do not ask the user to repeat these tests. Detailed local row-level results/settings/provenance are not in the current repo; see `docs/Validation/MCC-OHLCV-20261020.md`. If auditability is needed, sync the completed artifacts/manifest rather than redoing the work.
- [NEXT] Reconcile the user-confirmed local results into repository provenance: exact files and hashes where available, chart/study version, date/timezone, RTH/ETH settings, DST boundary dates, row/window counts, and result summary. Do not fabricate missing metadata.
- [NEXT] Investigate native Thinkorswim chart-data export as the preferred transport if it exposes the same timestamp/OHLCV fields without strategy-order timing semantics.
- [HOLD] Do not mark the export production-ready, connect it to replay as authoritative market data, or calibrate signal/opportunity logic until the external chart-parity items are complete.


## Morning Report / Morning Routine / Dashboard workflow (2026-10-22)

- [NOW — PRODUCT WORKFLOW] User wants to sit down around 08:30 Arizona time and see a dated Morning Report with an automatically populated/re-ranked watchlist, top 1–4 ranked candidate “Red Zone” multi-pane, and a Boring Overnight decision.
- [DOC ADDED] `docs/User-Manual.md` is the user manual and workflow specification. It distinguishes existing prototype behavior from planned functionality and documents launch, data readiness, watchlist, Red Zone, Boring Overnight, continuation, manual Schwab, and feedback flows.
- [IMPORTANT — NOT IMPLEMENTED] Current Dashboard is static HTML/CSS/JS opened via `frontend/index.html` in a browser, not an .exe. It uses demo fixtures/local CSV and browser-local watchlist/feedback. No live feed/scanner, automatic ranking, Morning Report generator, live Red Zone, live Boring Overnight calculator, C# engine wiring, or broker integration is present.
- [SOURCE AUDIT — REPOSITORY ONLY] `docs/Charts/1M/MainStudy.txt` defines `extensionATR = AbsValue(price - dailyVWAP) / ATR(14)`, warning at 1.50 ATR and danger at 2.00 ATR by default; bullish extension danger is price above Daily VWAP and at/above danger threshold. `inTrendExecutionCandidate = executionCandidate AND NOT freshTrigger`; its event is transition/re-arm gated. Final READY separately requires an active execution window, 5M execution permission, and no bullish extension danger. This establishes repository source semantics, not that the installed Thinkorswim study matches.
- [NOW] Continue audit of the exact 1M/5M installed-study parity and the bubble's real chart examples before any rule changes. Do not reinterpret IN-TREND CANDIDATE as READY or weaken extension thresholds without validation.
- [DONE — SPECIFICATION] Define provider-neutral ingestion, persistence, freshness, recovery, report snapshot, and validation requirements in `docs/Overnight-Collection-Contract.md`.
- [BLOCKED — PROVIDER / ENTITLEMENT] Choose and verify a supported market-data/scanner provider and licensing before implementing its adapter, exact freshness thresholds, and backfill/reconnect behavior.
- [NEXT] Implement append-only event/observation persistence, idempotent ingestion, health monitoring, recovery tests, deterministic report generation, ranking, Red Zone, and Boring Overnight wiring after provider selection.
- [NEXT] Decide desktop delivery only after app integration architecture is known. Browser launch is current reality; an .exe is not built or required yet.
- [USER HISTORY GAP] Repository search and available project handoff do not contain the full prior Morning Routine conversation. The manual documents a proposed workflow from known requirements, but the user should supply or point to the prior routine if exact historical steps/metrics must be restored.


## Overnight scanner / persistent Morning Report requirement (2026-10-22)

User wants to leave the released/deployed MCC program open overnight so scanner discoveries/data are collected before the 08:30 Arizona Morning Report. Do not mark this satisfied by an open browser tab or a static preview.

- [x] Add preview-only Morning Command Brief and responsive ranked 1–4 Red Zone layout to the static UI; include explicit “overnight collection NOT RUNNING” status and JSON report-preview export.
- [ ] Select a supported/licensed scanner and market-data provider; document actual APIs, entitlements, timestamp/session conventions, rate limits, and terms. Provider is currently unselected.
- [x] Write the provider-neutral collection contract in `docs/Overnight-Collection-Contract.md`; it deliberately leaves provider-dependent thresholds unspecified.
- [x] Implemented the API-hosted MarketDataCollectionWorker and AlpacaMinuteBarStreamWorker. They run independently of the browser, use cancellation on host shutdown, and the REST collector resumes from durable provider/symbol/timeframe watermarks with overlapping backfill chunks. Operational recovery tests remain open below.
- [PARTIAL] JSONL discovery-event and raw OHLCV observation stores are implemented. Full event provenance/session/schema audit coverage and long-term retention validation remain to be reviewed.
- [PARTIAL] Collector status exposes configured symbols/providers, per-key watermarks, cycles, last-cycle timestamps, errors, and recent errors. Explicit RUNNING/DEGRADED/STOPPED/UNKNOWN state semantics and end-to-end freshness/gap health validation remain open.
- [ ] Implement recovery tests for machine sleep/wake, network/provider outage, authentication expiry, replayed/duplicate events, session boundaries, and process restart.
- [ ] Implement deterministic, versioned Morning Report generation from persisted data; include provenance/freshness per candidate and exportable report snapshots.
- [PARTIAL] Alpaca discovery polling, event ingestion/deduplication, scanner UI refresh, candidate re-ranking, and four-slot Red Zone integration exist. Persistent continuation-watch logic and full production-universe coverage remain unverified.
- [ ] Wire Boring Overnight calculations and hard gates to persisted, validated inputs; unknown catalyst/data remains DATA? / manual review.
- [ ] Define packaging and startup: browser/local service vs desktop app/installer. Do not claim .exe/background collection exists until built, deployed, and tested.
- [x] Provider credentials are read server-side from configuration/environment; frontend requests contain no provider secrets, and stream logging does not include credentials. Continue to keep real keys out of committed configuration.
- [x] Verify the latest branch-head .NET workflow after the UI changes; head `bcb1ef5027859cd4448daa08191df83e0e6eb871`, workflow run 91, conclusion `success` (checked 2026-10-23 UTC).
- [ ] Run a browser-based end-to-end smoke test of the Morning Command Brief and JSON export; structural checks are not a browser test.


## Morning Routine recovery, provider comparison, and collection scaffold (2026-10-23)

- [x] Read the full Project source file Morning.txt (992 lines) and consolidate its non-redundant requirements in docs/Morning-Report-and-Routine.md. This is a curated requirements spec, not a claim the linked original chat was fully recovered.
- [x] Document provider candidates and public plan tradeoffs in docs/Provider-Comparison.md. Preliminary evaluation pair: Alpaca Algo Trader Plus vs Massive Stocks Advanced; no provider selected and no account entitlements verified.
- [x] Implement an initial provider-neutral persistence scaffold: IScannerDiscoveryEventStore, JSONL append-only adapter, batch ingestion validation, idempotency key, receipt timestamp, and tests for restart deduplication/corrupt-log failure.
- [x] Add a first append-only raw market-bar observation store with source/receive timestamps, OHLCV validation, content-hash idempotency, and append-only correction links. Implemented in API; tests cover restart deduplication and corrections. Session-aware gap classification and corruption/retention policy remain open.
- [PARTIAL — HOSTED WORKERS IMPLEMENTED] The API hosts the explicit-symbol REST collector, Alpaca scanner discovery worker, and optional Alpaca minute-bar WebSocket worker. The collector uses persisted watermarks and bounded backfill chunks. Regular-session stream delivery and controlled outage/sleep/restart recovery tests remain open.
- [x] Verify .NET build/tests for the API host, provider adapters, raw-bar store, and store tests. Latest verified run 151: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012211560. Browser and live-provider tests remain open.
- [ ] Browser smoke test the Morning Brief and top-right Red Zone layout; user has manually confirmed the current visual look, but latest layout change still needs browser confirmation.
- [PARTIAL] The user procedure already exists at docs/Validation/5M-to-1M-User-Validation-Steps.md and repository-source comparison is documented. Exact installed-study input/version confirmation and matched-bar Permission/READY/runner state parity remain open; do not repeat prior OHLCV/DST tests.
- [ ] Resolve provider decision only after entitlement, scanner coverage, session behavior, historical backfill, retention/licensing, and representative-symbol comparison.


### Status correction — follow-up work after the prior checkpoint (2026-10-23)

The earlier workflow success recorded above was for its then-current branch head only. It does **not** establish CI status for the newer persistence scaffold and upper-right Red Zone commits.

- [x] Consolidate the supplied Morning.txt excerpts into docs/Morning-Report-and-Routine.md.
- [x] Add provider evidence and proof-of-fit checklist in docs/Provider-Comparison.md.
- [x] Add the provider-neutral JSONL discovery-event store and batch-ingestion validation scaffold; tests are added but their latest CI result must be checked.
- [x] Place Morning Brief left and Red Zone upper-right in the responsive wide-screen command workspace. User browser confirmation is still needed for this specific layout change.
- [x] Create docs/Validation/5M-to-1M-User-Validation-Steps.md with the exact installed-study evidence needed from the user.
- [x] Update the User Manual, Scanner Workbench, and New Chat Handoff.
- [ ] Verify .NET build/test workflow against the latest branch head after all changes.
- [ ] Browser-test the new top-right layout and export interaction.
- [x] Base persistence, timeframe/feed identity, cancellable API-hosted collection, durable watermarks, overlap, bounded REST backfill, and WebSocket reconnect/resubscription are implemented. Controlled restart/sleep/outage/recovery acceptance remains open in the separate recovery-test item.


### Collector lifecycle scaffold extension (2026-10-23)

- [x] Add ScannerDiscoveryPollingWorker with cancellation, retry-on-next-poll behavior, and RUNNING / DEGRADED / STOPPED / UNKNOWN health snapshots.
- [x] Add an offline test proving a successful empty poll is recorded as zero observations and cancellation stops the worker.
- [ ] Add explicit degraded-source/recovery test coverage and sleep/wake/backfill reconciliation after provider semantics are known.
- [x] The explicit-symbol REST collector and optional Alpaca WebSocket worker are registered as ASP.NET Core hosted services; Alpaca and Massive historical REST adapters exist. A configured symbol set and provider credentials are still required at runtime.


## Provider comparison UI / UPC CSV follow-up — 2026-10-23

- [DONE — STATIC UI ONLY] Added parallel Alpaca and Massive Stocks comparison cards to `frontend/index.html`, styled in `frontend/styles.css`, with a local comparison checklist action in `frontend/app.js`. User can set symbol, aggregation, and date; it marks both feeds NOT CONNECTED and shows required comparison fields. The button does not contact vendors and is not a live integration.
- [DONE — CSV REVIEW] Reviewed `data/StrategyReports_UPC_102226.csv`. It records four strategy/order events between 09:37 and 09:47 Arizona time and embedded isolated 1M OHLCV payloads. It is not a full OHLCV time series, so it cannot determine whether the reported earlier candles were absent.
- [RESOLVED] User confirms UPC screenshots/event date 2026-10-22 and Thinkorswim chart timezone Arizona.
- [OPEN] Obtain continuous 1M/5M UPC OHLCV or verify exact bar timestamps in TOS for 09:16–09:36. Do not fill missing bars.
- [OPEN] Implement real backend adapters for both vendors only after credentials and entitlement/licensing decisions; keep secrets out of frontend JS. Compare source timestamps, bars, quotes, missing intervals, reconnect and backfill.


## API host, REST adapters, and Morning Brief integration (2026-10-23)

- [DONE — IMPLEMENTED, NOT YET BUILD-VERIFIED] Added `src/MomentumCommandCenterV2.API` as an ASP.NET Core .NET 8 project and included it in `MomentumCommandCenterV2.sln`. It serves the existing `frontend/` workbench when run from the repository layout and exposes health/provider-status endpoints.
- [DONE — REST HISTORY ADAPTERS, NOT LIVE-VALIDATED] Added server-side Alpaca historical 1M/5M bars adapter and Massive historical aggregate adapter. Credentials are read from configuration/environment variables, never from frontend JavaScript. No API credentials were provided; no live provider request was performed.
- [DONE — FIRST COMPARISON ENDPOINT] Added `GET /api/market-data/compare` to request the same symbol/timeframe/window from both providers and report bar counts, first/latest bars, duplicate timestamps, and observed timestamp gaps. Gap count does not imply zero trading or a provider defect. Websocket, scanner/movers, quotes, pagination, durable bar storage, reconnect/backfill, and market-session-aware gap classification remain open.
- [PARTIAL — MORNING BRIEF] Added `GET /api/morning-brief` for SPY/QQQ/IWM 5M observation snapshots from configured providers, and connected the dashboard to provider status, the comparison endpoint, and Morning Brief. Sectors, breadth, movers, catalysts/news, validated market regime, Red Zone scoring, historical expectations, and Boring Overnight remain explicitly DATA? / NOT CALCULATED.
- [OPEN — REQUIRED BEFORE TRADING RELIANCE] Build/test the latest branch head, run live API requests with user's licensed credentials, verify provider entitlements and extended-hours coverage, compare both feeds against TOS, and test session/gap semantics. These commits are not a claim of build success, CI success, live-data parity, or production readiness.
- [OPEN] Add durable raw OHLCV persistence with source/receive timestamps, late/corrected bar handling, and restart recovery.
- [OPEN] Add websocket streaming, health state, reconnection and historical backfill before considering real-time operation.
- [OPEN] Continuous UPC OHLCV remains needed only to resolve whether the specific 09:16–09:36 chart intervals were truly missing. It is not a blocker to implementing provider adapters.


### CI verification — API adapters and dashboard wiring (2026-10-23)

- [VERIFIED] GitHub Actions run 147 at commit `ae8180866def759612fa7c1a515dc93586143056` completed successfully: solution restore, Release build, and test suite all passed. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011426478
- This validates compilation and existing automated tests only. It does not validate live provider credentials, API entitlements, response behavior against real accounts, browser rendering, data parity, or readiness for live trading.


### Raw market-bar persistence slice — 2026-10-23 [BUILD/TEST VERIFIED]

- [IMPLEMENTED — CI PENDING FOR LATEST COMMIT] Added `IMarketBarObservationStore` and `JsonlMarketBarObservationStore` to the API host. Provider-comparison requests append valid raw OHLCV observations to JSONL outside the repository by default (LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl); override with `MCC_MARKET_BAR_STORE_PATH`.
- Identical observations are idempotent by provider/symbol/source timestamp/content hash. Changed values append a new observation with a `SupersedesObservationId`; prior records are not overwritten. Records preserve source timestamp, receive timestamp, provider/feed and raw OHLCV.
- [VERIFIED — TESTS PASS, 2026-10-23] Added `tests/MomentumCommandCenterV2.Tests/JsonlMarketBarObservationStoreTests.cs` for idempotency across store restart and append-only correction history. GitHub Actions run 148 at commit `c702afa21fac9dfa5a31b18463cc4429aa4adc71` passed restore, Release build, and tests: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011865804.
- [OPEN] Add corruption recovery policy, disk/retention monitoring, session-aware gap semantics, and ensure write failures surface as a clear API health condition.


### Morning Brief and raw-bar persistence verification status

- Morning Brief now records observed SPY/QQQ/IWM 5M bars into the same append-only store and displays the observed first-to-last window change when data is available. These are observations, not a validated market-regime classification.
- The provider comparison UI shows newly persisted observation counts. REST history only; no WebSocket/live collector.


- [LATEST HEAD VERIFIED] GitHub Actions run 151 at `ae6ee7acc30e85a6268b5ea8fc40d86429d3461b` completed successfully for restore, Release build, and tests. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012059701. No live provider, browser, or entitlement validation is implied.


## Canonical dependency/status correction — 2026-10-23

Use `docs/Next-Steps-and-User-Inputs.md` as the single checklist for user actions versus work that can proceed without accounts.

- **Accounts:** no paid subscriptions are needed for the next development milestone. Free accounts are optional for live API smoke tests. One provider is enough to begin; both are useful for a same-window comparison. Do not select or pay for a production plan until proof-of-fit, account entitlements, license rights, premarket/small-cap coverage and TOS parity have been checked.
- **API:** the ASP.NET Core host, Alpaca/Massive historical REST adapters, comparison endpoint, partial Morning Brief and append-only JSONL raw-bar store are implemented. Latest CI run 151 passed restore, Release build and tests. No live provider calls, browser end-to-end test or license verification has been performed.
- **Offline work that remains actionable now:** adapter pagination/error/retry tests; hosted collector lifecycle and health; stream abstraction; recovery/backfill design and tests; store corruption/retention/error handling; fixture-driven Morning Brief/scanner/Boring Overnight calculations; session-aware gap semantics; ThinkScript source parity tests.
- **User evidence still required:** XRPN 5M screenshot listed in `docs/Validation/5M-to-1M-User-Validation-Steps.md`; UPC continuous bars or exact timestamp inspection only if the historical 09:16–09:36 gap question still needs resolution; review the API-hosted browser layout. These are not prerequisites for offline infrastructure work.
- **Morning Brief scope:** currently benchmark observation/freshness only. Broad movers, sector/breadth, sourced news/catalysts, validated regime, Red Zone qualification and live Boring Overnight remain unimplemented and must not be displayed as known.

## Export and opportunity-quality review — 2026-10-26

**Audit:** `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`

- [DONE — REPOSITORY DATA AUDIT] Found an aligned XRPN 1M/5M OHLCV pair for 2026-10-19: `data/StrategyReports_XRPN_102026_933.csv` (355 1M payload rows) and `data/StrategyReports_XRPN_102026_5m_offset_1027.csv` (81 5M payload rows).
- [DONE — DATA PROVENANCE] Corrected the October 22 1M report filename to `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`; it is not the October 2 1M pair. Do not pair it with the actual 2026-10-02 5M file.
- [PARTIAL — INTERNAL OHLCV CHECK] In the October 19 post-09:00 Arizona overlap, 27 complete 5M windows have all five 1M payload rows and their aggregated OHLCV matches the corresponding 5M payload. Thirty minute timestamps from 09:00–12:59 are absent from the 1M file; missing rows remain gaps, not synthesized bars.
- [BLOCKED — DIAGNOSTIC STATES] OHLCV payloads do not contain internal ThinkScript Permission, candidate, extension, READY, re-arm, or expiration states. READY-frequency root cause cannot be attributed from OHLCV alone. Existing October 2 screenshot remains available; do not ask user to re-upload it.
- [BLOCKED — OVERNIGHT WINDOW] Audited bars do not cover the full 07:00–20:00 Eastern GTC_EXT eligible window. `eligible-window-high-excursions.csv` previously contained placeholder values 1–20; those rows were removed, leaving a header-only input with zero observations. No target-touch rate or alternative limit recommendation may be derived from it.
- [AUDIT — OPPORTUNITY QUALITY] `OpportunityQualityAssessor` and unit tests exist, but the assessor is not wired to replay, V2SignalEngine, CLI, API, or UI. Required metrics are not derived from raw OHLCV. Keep provisional thresholds diagnostic-only; presentation must distinguish opportunity quality from the actual ThinkScript EXECUTION READY event.
- [NEXT — NO USER INPUT REQUIRED] Keep the October 19 pair as a coverage fixture; add regression checks for filename/header/payload date consistency where the ingestion workflow has file-level provenance; keep incomplete intervals explicit.
- [NEXT — USER/THINKORSWIM EVIDENCE] To diagnose a particular READY gate, obtain diagnostic states for the same symbol/date/bars as the OHLCV. For Boring Overnight, use a full eligible-window export with source-backed regular close; otherwise mark DATA_GAP.
- [NO PROMOTION] These exports are not proof of production timestamp semantics, indicator parity, fill probability, or strategy profitability.

## Opportunity-quality label safety and branch validation — 2026-10-26

- [DONE — SOURCE CHANGE, CI NOT YET CONFIRMED] Changed display labels from `READY — HIGH/MODERATE/BORING/EXTENDED` to `QUALITY — HIGH/MODERATE/LOW` and `TIMING — EXTENDED`. The labels now avoid conflating this diagnostic classification with the existing Thinkorswim `EXECUTION READY` event. Machine-readable enum values and eligibility precedence are unchanged.
- [DONE — TEST SOURCE UPDATED, CI NOT YET CONFIRMED] Updated the high-quality label assertion and added four negative-metric cases covering negative RVOL, ATR extension, recent range, and resistance distance; invalid metrics should fail closed to `DATA_UNKNOWN`.
- [OPEN — CI VERIFICATION] GitHub combined commit status returned no status records for the latest branch commit at audit time. Do not call the current branch build/test green until a workflow run tied to the latest commit is confirmed.
- [INTEGRATION CAUTION] Repository compare reports `feature/opportunity-quality-classification` as diverged from `master` (268 commits ahead and 152 behind in the compare result). Do not merge this branch wholesale into master without a deliberate file/history reconciliation. The changes in this work session remain on the feature branch.

## October upload uncertainty assessment — 2026-10-26 (supersedes prior preliminary note)

- [DONE — DATA PROVENANCE] Inspected the newly uploaded `StrategyReports_XRPN_10022026_1M_EntireDay_2.csv` by report header and embedded payload. It is actually **HPK 1M**, October 2, with 214 rows from 06:30–12:59 Arizona. Renamed it to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv` and removed the misleading XRPN-named path.
- [DATA QUALITY] Those 214 rows match all corresponding October 2 rows in the existing HPK multi-day file exactly. The upload is a duplicate/subset, not new XRPN data. The October 2 XRPN 5M file still has no verified same-symbol XRPN 1M partner.
- [PARTIAL — CHART PARITY] Existing October 19 parity worksheets have chart-entered values for 20 bars per timeframe: 1M has 10 MATCH and 10 ROUNDING ONLY; 5M has 9 MATCH and 11 ROUNDING ONLY. 48 1M and 13 5M rows remain unfilled. This supports sampled OHLCV transport mapping but not full chart/session parity.
- [PARTIAL — TIMESTAMP SEMANTICS] All 40 manually entered chart labels match the local clock digits represented by the payload timestamps. The worksheet records `ChartTimestampMeaning=MATCH`, not BAR_OPEN/BAR_CLOSE/decision availability; the timestamp convention is not fully documented.
- [BLOCKED — GAP CLASSIFICATION] October 2 HPK 1M has 214 of 390 expected minute timestamps, leaving 176 absent. October 19 XRPN has 30 absent timestamps in the 09:00–12:59 interval. Do not infer that missing rows are no-trade periods or chart gaps.
- [BLOCKED — READY ROOT CAUSE] The parity worksheets contain OHLCV chart values, not per-bar ThinkScript READY/Permission/candidate/extension/re-arm states. The October 2 XRPN diagnostic question still needs XRPN 1M and same-bar diagnostic state.
- [BLOCKED — BORING OVERNIGHT] No complete 07:00–20:00 Eastern GTC_EXT window or actual-fill dataset has been found. `eligible-window-high-excursions.csv` now contains only the header; its fabricated values were removed. It has zero verified observations.
- [CI] GitHub Actions run `40379797275` for commit `e7e59d4` completed successfully (restore, build, test). Documentation commits made after that run trigger newer runs; do not treat the current branch head as green until its own run completes.
- [INTEGRATION CAUTION] Keep branch-divergence caution in force; do not merge this feature branch wholesale into master.
- Detailed uncertainty table: `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`.

## Launch-week release gates — 2026-10-26

Use `docs/Release-Readiness-2026-10-26.md` as the concise launch-gate summary. This is a prioritization aid, not a declaration of release approval.

- [DONE — DOCS] Added explicit release-readiness assessment separating local prototype use from live-trading reliance.
- [DONE — CONTRACT CLARITY] Clarified in `docs/MCC-OHLCV-Export-Workflow.md` that the normalized CSV contract expects completed-bar availability timestamps, but raw `GetTime()[1]` semantics remain unverified until chart parity proves the mapping.
- [NEXT — NO USER INPUT] Add automated report provenance checks for filename/header/payload symbol and date consistency; add importer regression coverage for duplicate, missing, malformed, and partial-window rows.
- [NEXT — NO USER INPUT] Reconcile duplicated and legacy To-Do material; preserve one canonical Master To-Do and link domain-specific specifications rather than creating competing checklists.
- [BLOCKED — USER/THINKORSWIM] Obtain actual XRPN 1M for October 2 if that exact event remains the target; the new file was HPK and duplicates existing HPK rows.
- [BLOCKED — USER/THINKORSWIM] Obtain same-bar 1M/5M gate-state diagnostics for READY-frequency attribution.
- [BLOCKED — DATA SOURCE] Obtain full GTC_EXT eligible-window bars plus source-backed regular close for target reach research; actual fills are a separate data source.
- [CI STATUS] Latest confirmed feature-branch head `c1895641` passed workflow `40380631692`; the release-readiness note was then updated, so recheck the workflow for the resulting newest head before promotion.


## Follow-up update — 2026-10-26 (XRPN pair, Overnight Scanner, Alpaca)

- [DONE — OHLCV PAIR] data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv contains XRPN 1M data for 2026-10-02: 391 consecutive timestamps from 06:30–13:00 Arizona. Against data/StrategyReports_XRPN_10022026_5M_EntireDay.csv, 78 complete 5M windows aggregate exactly from the 1M rows. The same-symbol OHLCV pair is now available.
- [BLOCKED — READY ROOT CAUSE] The pair's strategy report payloads contain only OHLCV transport rows, not per-bar Permission, block code, candidate/trigger, extension danger, READY activation/expiration, re-arm, or expiration diagnostics. Data not found in sources. Need the diagnostic exports or exact repository path if they were uploaded separately. Do not attribute READY rarity from OHLCV alone.
- [NEXT — USER LOCAL ACTION] Run the documented Alpaca smoke test in src/MomentumCommandCenterV2.API/README.md and docs/Next-Steps-and-User-Inputs.md. Saved Alpaca credentials are intended for this test. Report sanitized API response/errors and timestamps only. User reports 87/87 tests pass; this is not live-provider verification.
- [NEXT — BORING OVERNIGHT] Initial chart-review shortlist from the 2026-10-26 scanner screenshot: TTE, RTO, NNN, OKE, AHR, FMS; CCI secondary. Request same 5M chart window across symbols with ETH enabled: 2026-10-26 regular session through 2026-10-27 20:00 Eastern. Full quantitative research still requires source-backed close and full next GTC_EXT window OHLCV with DATA_GAP intervals preserved.
- [RELEASE POSTURE] Local prototype / diagnostic only; no live-decision reliance until provider authentication/entitlements, timestamp semantics, persistence/recovery, installed-study parity and overnight research gates are closed. Do not merge the diverged feature branch wholesale.

## [Uncertainties]

- Study diagnostic exports are not present in the reviewed October 2 OHLCV files.
- Alpaca credentials are user-reported as saved; live auth is not yet verified.
- Screenshot-based overnight shortlist is provisional; catalyst/news clearance and full data coverage are unavailable.


## Correction and verification update — 2026-10-26

- [CORRECTED] Prior chart screenshot request for 2026-10-26–10-28 was invalid. User is providing AHR, FMS, NNN charts for completed historical dates 2026-10-21–10-23. Accept those for visual triage; do not ask them to retake based on the invalid request.
- [PASS — API HEALTH] /api/health reports OK, decision-support-only; order submission is not implemented.
- [PASS — TESTS, USER-REPORTED] Release test command succeeded: 87 passed, 0 failed, 0 skipped.
- [BLOCKED — LIVE PROVIDER] Both Alpaca and Massive report NOT_CONFIGURED. Compare endpoint returned zero bars and zero persisted observations. Alpaca error identifies missing MarketData:Alpaca:ApiKey and ApiSecret or ALPACA_API_KEY / ALPACA_API_SECRET. User's saved credentials are not available to this API process under its current configuration.
- [NEXT — LOCAL ACTION] API UserSecretsId is MomentumCommandCenterV2-API-Local. Run the two dotnet user-secrets set commands documented in NewChatHandoff.md and Next-Steps-and-User-Inputs.md, restart API, verify provider status, and rerun compare. Never send credentials in chat. Massive can remain unconfigured.
- [OPEN — XRPN] 1M/5M OHLCV alignment verified for 78 complete windows; per-bar Permission/block/candidate/extension-danger/READY/re-arm/expiration diagnostics remain missing from reviewed reports.

## [Uncertainties]

- Existing credential storage location is not known; project-scoped User Secrets should resolve this if credentials are already owned and valid.
- Screenshots are visual evidence only; full-window OHLCV and actual fill outcomes remain unverified.


## Final Overnight review and Visual Studio launch update — 2026-10-26

- [PARTIAL — VISUAL REVIEW ONLY] Inspected Project Sources screenshots for AHR, FMS, NNN. Their visible chart headers show 2026-10-19–2026-10-21, not the intended 2026-10-21–2026-10-23 sample. Do not label these as the requested date range. Visual-only observations and limits are recorded in docs/NewChatHandoff.md.
- [BLOCKED — DATE-CORRECT OVERNIGHT ANALYSIS] The intended Oct 21–23 screenshots were not identified in the inspected files. Data not found in sources for that exact window in this review. If they exist under different Project Sources names, identify the exact files; otherwise the correct-date visual review remains open. Do not ask the user to repeat already completed work without checking for the correct files first.
- [BLOCKED — TARGET RESEARCH] Screenshots do not establish full subsequent 07:00–20:00 Eastern GTC_EXT bars, target touch/reach percentages, or actual fills. Need source-backed prior regular close and full eligible window for each completed session, with missing bars preserved as DATA_GAP.
- [NEXT — USER / VISUAL STUDIO] User reports Alpaca secrets were saved successfully and the API is launched from Visual Studio. Stop/restart the API in Visual Studio so it reloads configuration; verify launch environment is Development and the API project's UserSecretsId is MomentumCommandCenterV2-API-Local. Recheck /api/providers/status and /api/market-data/compare on the same localhost process. A configured flag alone is insufficient; actual bars and source timestamps are required.
- [KEEP BLOCKED] Do not call today's Overnight qualified or optimize the +0.5% target from the inspected screenshots. The current target is a heuristic; validated touch-rate research and actual fills remain unavailable.


## 2026-10-26 — Overnight screenshot correction, TTE bubbles, Alpaca status fix

- [ACCEPTED — USER DATE CLARIFICATION] User confirms the three existing AHR/FMS/NNN screenshots are for 2026-10-21–2026-10-23. Previous assistant date challenge was incorrect; do not request the images again. Complete the visual analysis using the user's stated window while preserving screenshot-only limitations.
- [NEXT — 1M DISPLAY DIAGNOSTIC] TTE 1M chart has no visible bubbles, while FMS has many. The Library V14.5 source defaults `showStateBubbles=yes` but only draws bubbles for explicit transitions/events; no generic per-bar state bubble is present and the inspected bubble section does not plot a dedicated GET OUT/EOD bubble. Verify TTE's lower-pane 1M study input `showStateBubbles` and exact study/version against FMS before changing code. Missing bubbles alone do not mean HOLD or SELL.
- [PASS — ALPACA HISTORICAL SMOKE TEST] User's Visual Studio API returned 5 SPY 5M IEX bars and persisted 5 observations for 2026-10-23 13:30Z–13:50Z, zero duplicate timestamps, zero reported gaps, no error. This validates a successful historical request for that window only. Massive remains optional/unconfigured.
- [CODE CHANGE — PROVIDER STATUS] `/api/providers/status` was hardcoded to return `CONFIGURED_NOT_YET_VALIDATED` whenever credentials existed, even after a successful compare. Updated the interface/adapters and endpoint to expose last successful non-empty data validation and return `VALIDATED_DATA_RETURNED` when appropriate. Added a unit test that an empty response does not validate. Changes are on `feature/opportunity-quality-classification`; current-head CI and tests for these edits still need verification.
- [USER ACTION — AFTER UPDATED BUILD] Restart API in Visual Studio and repeat the SPY compare request; verify `/api/providers/status` changes to `VALIDATED_DATA_RETURNED`. If not, verify Visual Studio is running the updated feature-branch code and restarted process.


## 2026-10-26 — completed review work and next release priorities

- [DONE — VISUAL TRIAGE] Reviewed the user's three AHR/FMS/NNN chart screenshots for the user-confirmed 2026-10-21–2026-10-23 sample; no re-upload required. Detailed observations and limitations: docs/Validation/Overnight-Candidate-Review-20261026.md.
- [DONE — NEW CANDIDATE TRIAGE] Evaluated CCI and NICE scanner fields and chart screenshots. Both are WATCH / NOT READY; neither score overrides chart conditions, missing catalyst clearance, or unverified daily average volume. NICE's displayed 5D and AVG ON values both round to 0.70%, so the strict 5D > 20D gate requires unrounded values.
- [DONE — SOURCE FIX] Added a transition-only red EOD GET OUT bubble to docs/Charts/1M/MainStudy.txt. The state was already labeled GET OUT but EOD_GET_OUT was not included in the bubble event list. This is display-only; no entry/permission logic changed. [USER ACTION] Install the updated 1M study in Thinkorswim and verify the EOD bubble; this cannot be done from GitHub.
- [PASS — CI AT PRIOR HEAD] GitHub Actions .NET build/test succeeded on 4f5ed265e54e9a924935a58b8b3233b31b264279 (run 40414827751); user reports 88 tests passed. Check CI again after subsequent source/doc commits. ThinkScript is not compiled by the .NET workflow.
- [PASS — ALPACA HISTORICAL SMOKE TEST] SPY 5M IEX returned 5 bars/5 persisted observations, zero duplicate timestamps/gaps, no error; provider status now reports VALIDATED_DATA_RETURNED after successful non-empty fetch. This is limited to that request and process lifetime.
- [OPEN — CCI/NICE HARD GATES] Verify exact unrounded 5D > 20D average, 20D average daily volume >=500K, freshness, exclusion list and source-backed catalyst review. Until then keep both WATCH.
- [OPEN — QUANTITATIVE OVERNIGHT RESEARCH] Need source-backed prior regular close plus complete next 07:00–20:00 Eastern GTC_EXT bars; preserve missing intervals as DATA_GAP. Historical touch is not fill. No target optimization or READY claim from screenshots alone.
- [NOW — RELEASE READINESS] Prioritize current-head CI; install/verify 1M EOD bubble; finish market-data pagination/retry/error handling and session-aware gaps; collector lifecycle/restart/backfill/durable checkpoints; storage failure/rotation tests; fixture-driven Morning Brief/scanner/Boring Overnight; current-head parity/diagnostic review. Do not merge the diverged feature branch wholesale into master.


- [DONE — INITIAL PUBLIC CATALYST CHECK] CCI Q3 earnings/outlook release confirmed for Oct 21 after close; include recent post-earnings risk in manual review. NICE official IR event page lists no upcoming events, but full current-news/catalyst clearance remains OPEN. Sources and details: docs/Validation/Overnight-Candidate-Review-20261026.md.


## MCC readiness sprint — 2026-10-27

- [IMPLEMENTED — CI PENDING] Added Swagger/OpenAPI (`/swagger`) to the ASP.NET Core API and documented local launch / smoke-test flow in `docs/API-Swagger-Workflow.md`. Use the actual host/port printed by ASP.NET Core. Swagger is a test interface, not evidence of provider entitlement or production readiness.
- [IMPLEMENTED — CI PENDING] Added `POST /api/boring-overnight/evaluate` and `BoringOvernightEvaluator`. The evaluator applies deterministic hard gates: data freshness, excluded-list state, price > $5 and > SMA20, 20D average daily volume >= 500,000, ON win rate >= 60%, 20D average ON >= +0.10%, 5D average ON strictly greater than 20D average, catalyst clearance, 5M permission + bullish structure, and no 1M SELL/GET OUT/BLOCK state. Missing/unknown critical inputs return DATA?; hard-gate failures return BLOCKED; heat/extension warnings return WATCH. It does not discover candidates, source inputs, calculate a validated limit target, or submit orders.
- [ADDED TESTS — CI PENDING] Added tests for unknown catalyst/permission, volume-gate failure and extension warning.
- [DONE — RESEARCH POLICY] Added `docs/Validation/Boring-Overnight-Target-Policy-20261027.md`. Keep +0.5% as a comparison heuristic; do not call median ON % a 50% limit-touch/fill probability. Complete GTC_EXT windows and walk-forward target-touch research remain required.
- [OPEN — THINKSCRIPT ARTIFACT MISMATCH] User reports no bubbles after applying the prior fix, with no compile error. The repository `docs/Charts/1M/MainStudy.txt` is V14.4.10, while the uploaded local reference is `MomentumCommandCenterV3_2_1M_V14_5.txt`; these are not proven to be the same source. Prior bubble change did not resolve the installed chart. Do not claim fixed; identify and patch the exact installed V14.5 source, then verify a historical EOD transition bubble in TOS.
- [OPEN — CURRENT-HEAD CI] Run/inspect Release build and tests for the exact latest branch head after these changes; do not reuse prior green workflow status.
- [OPEN — END-TO-END INTEGRATION] Wire evaluator inputs to provider-backed and persisted scanner/catalyst/indicator data with timestamps/provenance. Until then, endpoint is a deterministic policy test harness, not an autonomous nightly workflow.


- [PATCH ARTIFACT CREATED — TOS USER VERIFICATION REQUIRED] Generated `/mnt/data/MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt` from the uploaded V14.5 source (not the repository's older V14.4.10 MainStudy). It adds a single bubble on transition into `EOD_GET_OUT`. Install this exact source in the lower 1M study and visually verify on a chart spanning the transition. Do not mark complete until verified; local artifact has not been committed as a canonical repository source yet.


## API usability / StrategyReport / exclusion watchlist — 2026-10-27

- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added `POST /api/strategy-report/upload` for multipart CSV upload, 10 MB limit, 10,000 data-row limit, quoted-field parsing, row-width warnings, full parsed rows plus 100-row preview, and likely timestamp/symbol/event column detection. It preserves what the report contains; it does not reconstruct continuous OHLCV or infer fills absent from the file. Parser unit tests added.
- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added local persisted MCC exclusion list at `GET/POST/DELETE /api/watchlists/exclusions`. Boring Overnight evaluation now derives the excluded flag from this list instead of trusting the request field. Store lives under API `App_Data/exclusions-watchlist.json`; ensure this directory is writable and protected by local deployment policy.
- [LIMITATION] This is MCC's local list, not a direct Thinkorswim watchlist integration. Data not found in sources for an official TOS API that lets MCC read or mutate the user's personal watchlists. If TOS watchlist membership must be authoritative, add a documented export/import workflow or supported connector after verifying the actual TOS export format.
- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added example-oriented summaries/descriptions for health, provider status, market data compare, Morning Brief, Boring Overnight evaluation, CSV upload and exclusion watchlist endpoints. Swagger enabled regardless of Development environment so local Release runs can use `/swagger`; do not expose unauthenticated Swagger/API publicly.
- [OPEN] Verify current-head workflow run 40538766766, then test CSV upload with a real StrategyReport export, add/remove exclusion symbol, and confirm the evaluator blocks it.
- [OPEN] Add an MCC frontend upload control and exclusion-watchlist management panel after API contracts are verified.
- [OPEN] Test JSONL persistence count semantics and continue Alpaca pagination/retry, collector recovery, and source-backed scanner wiring.
- [OPEN — THINKSCRIPT] Rebuilt the V14.5 file as a cleaned single-statement-per-line source, commented the wrapped prose fragments that caused invalid statements, and added the EOD transition bubble. This is a locally generated artifact, not yet compile-verified by Thinkorswim; user must test the exact file.


- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added API-backed frontend panels for MCC exclusions and StrategyReport CSV upload. Exclusions can be added/removed and refreshed from the persisted API list. StrategyReport upload renders row/column summary, warnings, first 100-row preview and offers full parsed JSON download. Existing "Working candidates" browser-local watchlist remains separate and must not be confused with exclusions.
- [CI INVESTIGATION] Workflow run 40538986271 failed because `WithOpenApi` was missing from the .NET 8 API project's references. Added `Microsoft.AspNetCore.OpenApi` 8.0.0 and re-triggered build. Newer workflow 40539484300 is in progress at latest UI/style commit; verify its result before calling the current head green.


## 2026-10-27 follow-up — parity conversion and Alpaca ingestion

- [IMPLEMENTED — CI PENDING] StrategyReport parser now detects embedded `MCC_OHLCV|v1|...` payloads and transforms them into the exact 17-column Chart Parity Template shape: SourceRow, Symbol, Timeframe, PayloadTimestampUtc, PayloadOpen/High/Low/Close/Volume, ChartTimestamp, ChartOpen/High/Low/Close/Volume, ChartTimestampMeaning, Finding. The dashboard can download the converted parity CSV. It only creates rows from explicit MCC payloads; ordinary StrategyReport rows are preserved but are not guessed into OHLCV.
- [IMPLEMENTED — CI PENDING] Alpaca adapter supports next_page_token pagination, up to 100 pages (safety stop), deduplication by timestamp, and up to 3 attempts for transient network/429/5xx/timeout responses. Bounds timestamps to requested UTC range. This is not streaming; it is bounded historical REST retrieval.
- [IMPLEMENTED — CI PENDING] Added `GET /api/market-data/bars` for one provider with explicit timeframe/window, normalized bars, feed, source timestamps, gap diagnostics, and newly-persisted observation count. Maximum 31-day request. Extended-hours availability still depends on feed, entitlement, symbol, and returned data; gaps must not be interpreted as no trades without session-aware logic.
- [IMPORTANT] Current-head CI is queued/in progress; earlier runs failed at compile because the StrategyReport result contract did not yet include ParityRows. The contract has been corrected and a parity conversion regression test added. Do not call the latest code green until the newest run passes.
- [OPEN] Full scanner/Boring Overnight cannot be declared live yet: Alpaca historical bars do not provide the Thinkorswim study's 5M permission / 1M execution state, current news/catalysts, official exclusion-list sync, or source-defined overnight metric definitions. Build an explicit data contract and source each missing input; return DATA? until complete. Do not substitute a lookalike indicator for authoritative TOS state.
- [OPEN] Background collector lifecycle/recovery, persisted cursor/watermark, backfill scheduling, retention, session-aware gap detection, and health/status telemetry remain unimplemented. Current /api/market-data/bars is request-driven collection, not a background collector.


- [IMPLEMENTED — CI PENDING] Added opt-in `MarketDataCollectionWorker`. Set `MarketData:Collector:Symbols` to a comma/semicolon-separated list (for example `SPY,QQQ,TTE,FMS`) to poll overlapping 1m/5m historical windows. Default interval 60 seconds, default lookback 20 minutes; bounded config ranges. Append-only storage deduplicates repeats and the overlapping lookback recovers recent bars after restarts. `GET /api/market-data/collector/status` reports enabled state, configured symbols, cycles, new observations, errors, and timestamps. Disabled when no symbols are configured. This is not a market-wide scanner or websocket feed.
- [OPEN] Validate worker under a real configured Alpaca key, restart API and confirm recent bars are backfilled without duplicate persistence; test 429/5xx retry and API cancellation.


- [IMPLEMENTED ARTIFACT — TOS VERIFICATION PENDING] Created `V14_5_Bubble_Stack_Replacement.txt`: one composite bubble per 1M bar with all simultaneous event names, including EOD transition, to eliminate overlapping separate bubbles. Apply to the current cleaned V14.5 study only; the Library source is stale. Thinkorswim compile and visual validation remain open.
- [CI FIX APPLIED — RECHECK REQUIRED] Removed the unused collector status constructor parameter that failed current-head build. Verify latest workflow before pulling.

## 2026-10-27 — current-head implementation audit and recovery/parity fixes

### Implemented in this checkpoint (CI still must be verified)
- StrategyReport parser corrected for actual Thinkorswim export structure: report preamble (title, Symbol, Work Time), semicolon-delimited order table, embedded MCC_OHLCV v1 payload, comma-grouped Unix-millisecond timestamps, and Windows CRLF line endings. Explicit payload rows are projected to the exact 17 canonical parity-template columns. Chart-side fields remain blank unless present in source; order price is never substituted for OHLCV.
- Added regression coverage for real-format semicolon StrategyReport conversion and for reports with no explicit payload, where parity rows must not be fabricated.
- Collector now persists per-provider/symbol/timeframe source-timestamp watermarks to collector-watermarks.json (override with MCC_MARKET_COLLECTOR_CHECKPOINT_PATH). It resumes from the watermark minus an overlap, writes checkpoints atomically after successful non-empty retrieval/persistence, and catches up long outages in bounded 30-day chunks.
- Collector's newlyPersistedObservations counter increments only by the store's returned append count. An identical already-seen bar returning zero is an idempotent no-op, not a persistence failure.
- Inspected user-fixed commit 0caf00004a837e269c175f7d4c34827120ac8fe6: it adds Swashbuckle.AspNetCore.SwaggerGen and Xunit imports. Prior handoff records workflow 40541586544 green for that SHA; this does not validate newer edits.

### Still open — do not overstate live readiness
- Current-head build/test status must be checked after commits 1c0b54e, bf62d3b, c7acc54, d1af047, and a69771c. Green CI on an earlier SHA does not validate this head.
- Collector recovery is bounded by provider REST limits (30-day chunks); very long outages catch up over repeated cycles. Test restart, checkpoint corruption, filesystem permissions, transient failures, duplicate bars, corrections and cancellation against a configured provider.
- Alpaca pagination/retries are already present. No websocket/streaming ingestion is implemented. Extended-hours coverage depends on feed entitlement and actual returned bars; validate with known premarket, RTH, postmarket and next-session samples.
- /api/market-data/bars reports timestamp gaps, not session-aware missing-bar truth. Closed sessions, halts and no-trade intervals must not be classified as data failures without session-aware rules.
- Provider-backed scanner and Boring Overnight remain incomplete. The evaluator still needs sourced daily/overnight metrics, timestamped catalyst clearance, authoritative 5M Permission/structure, 1M execution state, and freshness/exclusion inputs. Historical OHLCV alone cannot reconstruct TOS states or a news verdict. Missing inputs must remain DATA?.

### PR state
- Current open-PR search returned no open pull requests. PR #7 is closed and must remain unmerged. User reports cleaning up other branches/PRs; no merge action is required unless a fresh query finds an open PR.

### User actions after this update
1. Verify latest workflow for the exact feature branch head, then run dotnet test MomentumCommandCenterV2.sln --configuration Release. Record exact tested SHA and pass/fail/skip counts.
2. Upload a committed real-format StrategyReport through Swagger POST /api/strategy-report/upload. Verify parityRows is non-empty, exactly 17 columns match the canonical template, and timestamp/OHLCV values are correct. Do not substitute order execution price for OHLCV.
3. Configure MarketData:Collector:Symbols locally (e.g. SPY,QQQ,TTE,FMS). Inspect GET /api/market-data/collector/status before and after stopping/restarting the API; verify watermarks persist, advance, and identical bars do not inflate new-write counts.
4. Validate one completed symbol/date across premarket, regular session and postmarket using the configured Alpaca feed; compare returned timestamps to a source export. Do not mark coverage complete from one SPY window.
5. Keep missing catalyst/TOS-state/daily metrics as DATA? until a real source is wired and validated.

- [CORRECTION — PARITY TIMESTAMP SEMANTICS] The StrategyReport `Date/Time` column is an order-event timestamp and must not be copied into `ChartTimestamp`. The parser now leaves `ChartTimestamp` blank unless the source explicitly labels a chart timestamp, and records why in `ChartTimestampMeaning` / `Finding`. The regression test asserts this separation.

## 2026-10-27 — user validation findings and immediate fixes

- User's Release test run failed 2 StrategyReport parser tests because the new fixtures encoded literal backslash-n characters instead of newline characters. Fixed both fixtures to use actual C# newline escapes and fixed parser line-ending normalization before header-offset slicing. Relevant commits: e494810, 3e8f9c4, f617743. This is a code fix only; tests have NOT yet been rerun in this environment.
- User's collector status proves the configured Alpaca historical REST adapter did return data in the running process: configured provider Alpaca, 1 completed cycle, 69 newly persisted observations, zero cycle errors, watermarks across SPY/QQQ/TTE/FMS 1m/5m. This is evidence of successful REST retrieval/persistence for that cycle, not full session coverage or streaming.
- Dashboard provider card was misleading/stale: static HTML defaults to NOT CONNECTED and the prior JS only updated connection text, leaving latest observation/coverage/recovery at DATA NOT LOADED/Not measured/Not run. Updated frontend/app.js to populate current process-local provider validation, last validation time/count, configured Alpaca feed, and collector cycle/new-write/error summary from API endpoints. If API fetch fails, card explicitly says API NOT CONNECTED (static preview or API unavailable).
- Extended-hours validation procedure: use a completed market day and compare raw returned timestamps to an independent TOS/export source. For 2026-10-26, the Eastern 07:00–20:00 ET window maps to 2026-10-26T11:00:00Z through 2026-10-27T00:00:00Z. Example endpoint: /api/market-data/bars?symbol=SPY&timeframe=1m&from=2026-10-26T11%3A00%3A00Z&to=2026-10-27T00%3A00%3A00Z&providerName=Alpaca. Repeat for 5m and at least one individual equity such as TTE; inspect feed, barCount, first/latest source timestamps, gaps, and bar payload. Confirm the configured feed/entitlement and compare with TOS data before claiming coverage.
- The exact current-head workflow remains unverified by the GitHub connector's workflow/status wrappers; empty status arrays are not evidence of a green build. User must rerun dotnet test MomentumCommandCenterV2.sln --configuration Release after pulling latest feature branch, and verify Actions for that exact SHA.
- [PARSER FOLLOW-UP] The embedded payload test's Unix-millisecond epoch `1792430880000` is `2026-10-19T17:28:00Z` (verified), not 13:28Z. Corrected the expected timestamp. Grouped payload volume such as `8,210` is now normalized to `8210`. Commits `dab2bc8` and `ba5c92f`. These are source/test corrections; current-head Release test result is still pending.

## October 27, 2026 — latest evidence and current blockers

- [USER EVIDENCE RECEIVED] At 2026-10-27 17:53–18:26 UTC, `/api/providers/status` reported Alpaca `VALIDATED_DATA_RETURNED`, configured feed `iex`, and successful non-empty responses (3 bars, later 2 bars). This proves recent REST requests returned data, not complete session coverage or SIP entitlement.
- [USER EVIDENCE RECEIVED] Collector snapshot reports 2 cycles, 36 newly persisted observations, 0 errors in the reported last cycle, and durable per-symbol/timeframe watermarks. Restart/recovery still needs a controlled test.
- [PARTIAL — DATA REVIEW] Saved October 26 IEX samples report SPY 1m: 394 bars, 8 observed gaps; TTE 1m: 168 bars, 91 observed gaps; TTE 5m: 75 bars, 3 observed gaps. These are not enough to call the provider inaccurate: compare exact timestamps and OHLCV against the matching Thinkorswim export and distinguish exchange/session inactivity from missing observations.
- [SOURCE CHANGE] Added `POST /api/market-data/import-csv` to convert a conventional Alpaca/IEX OHLCV CSV into the canonical 17-column parity schema. It fills Payload* only; Chart* and ChartTimestamp remain blank until an independent chart export is supplied. Added parser tests; latest CI must verify the new endpoint and tests.
- [SOURCE CHANGE] Provider status fetches use `cache: no-store`; provider labels are clearer and the dashboard script URL is versioned to reduce stale-JavaScript issues. If the dashboard remains static, verify the browser's address is the same API host/origin and hard reload after pulling the branch.
- [OPEN — CRITICAL FOR SCANNER] The hosted collector only polls explicitly configured symbols (SPY, QQQ, TTE, FMS). There is no market-wide movers/discovery source, news/catalyst feed, quote/spread data, or validated candidate scoring. Do not present these four configured symbols as a market-wide scan. Scanner work needs a licensed discovery feed or a user-supplied candidate universe plus explicit stale-data and liquidity checks.
- [OPEN — CANDIDATE REVIEW] Overall overnight scan screenshot is available in this conversation. Six candidate chart images exist in the repository folder `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026`, but this session could not retrieve those PNG bytes through the connected repository reader. No candidate shortlist or limit-sell prices are claimed without chart evidence, actual entry assumptions, and risk levels.
- [OPEN — SOURCE FORMAT] The repository currently contains the saved API response text for SPY/TTE coverage and an XRPN Thinkorswim StrategyReport CSV; Data not found in sources for the claimed matching TTE Thinkorswim StrategyExport file. Upload the exact TTE StrategyExport and the six charts directly into the conversation to complete the comparison.
- [USER ACTION] Pull the latest `feature/opportunity-quality-classification` branch, run Release tests, test the dashboard on the API-served same origin, use Swagger to test both StrategyReport upload and Alpaca CSV import, and upload the TTE chart-export pair plus six candidate chart screenshots here.

- [SOURCE CHANGE — CI PENDING] Added `GET /api/scanner/universe`: bounded 1–50-symbol historical scan returning bar count, first/latest timestamps, latest-bar age, observed window change, observed range, and summed volume. It is explicitly user-universe-only, not market-wide discovery, and does not generate READY/BUY labels. This provides a transparent scan primitive while a licensed market-wide movers source remains unresolved.
- [DATA NOT FOUND IN SOURCES] `data/HowToTestExtendedHoursCoverage_5m.txt` exists but is empty. The SPY 5-minute response is not available in the repository and must be recaptured; do not infer SPY 5m coverage from SPY 1m.

- [DATA REVIEWED — INTERNAL CONSISTENCY PASS] Compared the 75 TTE 5m bars to aggregation of the saved TTE 1m bars in the same UTC five-minute buckets. All 75 available 5m bars matched the aggregate OHLCV values from the corresponding 1m rows; 0 mismatches observed. This is a consistency check between the saved IEX responses, not independent parity against Thinkorswim.

- [SOURCE CHANGE — CURRENT-HEAD CI PENDING] Connected the Scanner Workbench to `GET /api/scanner/universe` with symbols, timeframe, and local-time window controls. Results are descriptive historical data only. Latest verified .NET CI run passed at commit `7eb26118f360ba0d9cc0b2dbc274ca346bc99240`; later frontend/docs commits are running their own workflows and current-head status must be checked before release.


## 2026-10-27 follow-up — dashboard, import, parity, solution visibility

- Fixed a JavaScript syntax error in the StrategyReport upload handler: a literal `\\n` had been written between statements. This parse error prevented the browser from executing the entire `app.js`, which explains why provider status stayed at “NOT CONNECTED / DATA NOT LOADED” and the scanner submit appeared to do nothing. Updated the script URL to force a fresh download: `app.js?v=20261027-upload-js-fix`.
- Added an explicit Swagger OpenAPI multipart schema to `POST /api/market-data/import-csv`: required binary `file`, optional `symbol`, and optional `timeframe` (1m/5m). Without this, Swagger generated a request body with no upload fields even though the endpoint code accepted multipart form data.
- Added solution folders `Documentation` and `Data and Validation Fixtures` to `MomentumCommandCenterV2.sln`, with the files under `docs/` and `data/` listed as Solution Items. They are reference/test assets, not separate executable .NET projects; keeping them as solution items is the appropriate fit.
- Inspected the saved TTE StrategyReport parse result (77 explicit payload bars) against the saved IEX 5m response (75 bars). 74 timestamps overlap; 3 payload timestamps are absent from IEX; all 74 overlapping bars have at least one OHLCV mismatch. Median absolute close difference is about $0.02, maximum $0.23. Total volume over overlapping timestamps is 705,686.6302 in payload vs 45,441 in IEX (about 15.53x). This is a material parity failure; do not treat IEX-only as interchangeable with the Thinkorswim chart feed. Full method/results: `docs/Validation/Alpaca-IEX-vs-Thinkorswim-TTE-20261026.md`.
- Reviewed six chart screenshots in `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026/`. None shows a clean overnight entry at the captured end-of-day state: BNC has 2.68 ATR extension and no 1m execution environment; RTO has 5m permission but the 1m state is EOD GET OUT / no swing-buy environment; MFM, SXC, SAP, and PANW show 1m/5m GET OUT or permission NO and/or substantial extension. No buy shortlist is justified from these end-of-day snapshots. RTO is the strongest *watch-only* chart of this set, not an entry recommendation; price is around $22.89, with nearby chart reference around $22.96 and session high $23.23, but a limit-sell cannot be set responsibly without a valid entry, next-session setup, and risk/invalidation plan.
- The user's listed red-X Actions runs were older commits that failed at build with CS0173 nullable conditional errors in the scanner endpoint. Those errors were fixed; subsequent workflows were green through the branch head observed before these latest dashboard/Swagger/solution changes. The new commits must complete CI before release.
- Collector recovery UI: Provider comparison > Alpaca card > “Gap / recovery test” displays collector cycles, new observations, and last-cycle errors. It explicitly says restart recovery still needs a controlled test. To verify persistence, capture `/api/market-data/collector/status` before restart, restart the API, then check the same endpoint again: watermarks should remain at/near the previous source timestamps and advance after the next successful cycle. Counters may reset on process restart; the checkpoint file and watermark values are the persistence evidence.


## Update — 2026-10-27: parity import, scanner freshness, and overnight chart re-review

### CSV import failure: root cause found and code changed
- User's 400 response was reproducible from the checked-in fixture shape: `data/TTE_ExportAttempt.csv` is **JSON content**, not delimited CSV. It is an Alpaca historical-bars response with root metadata (`symbol`, `timeframe`, `provider`, `feed`) and a `bars` array. The existing parser searched only for a delimited OHLCV header, so the 400 was expected for this mislabeled JSON export.
- Updated `AlpacaCsvParityParser` to accept both ordinary comma-delimited OHLCV CSV and Alpaca historical-bars JSON responses; added a fixture-based JSON parsing test; clarified the Swagger description. JSON still populates only Payload* values; chart values remain blank until a separate chart export is supplied.
- Relevant commits: parser `3e9f831ba42759657342d78f010b871fda9fa25a`, test `2e95de4b4a8decbd4e02aabab28a684042ddd722`, Swagger description `952215474ad7d8c5144402df1c08b40f67cfc9b6`. These are source commits, not proof of passing tests; CI for the latest head must be checked.
- Endpoint separation: use `POST /api/market-data/import-csv` for Alpaca OHLCV CSV or Alpaca bars JSON. Use `POST /api/strategy-report/upload` for Thinkorswim StrategyReport CSV (the report format begins with `Strategy report` and has semicolon-delimited trade columns). Do not send a StrategyReport to the Alpaca OHLCV parser.
- Follow-up: rename the JSON fixture to `.json` in a later cleanup and update the solution item path, or store an actual CSV fixture with a truthful extension. Current endpoint accepts only a `.csv` filename even when its content is JSON; this compatibility choice is temporary and confusing.

### Scanner output and collector
- User retested scanner: FMS/TTE/QQQ/SPY status is `OBSERVED`, with reported freshness 138.39/138.39/83.39/133.39 minutes. Treat all as stale historical observations, not current candidates. Score alone must never turn these rows into trade-ready decisions. Need an explicit max-freshness gate and a visible `STALE` / `NOT ACTIONABLE` result when exceeded.
- The response itself explains that relative-volume baseline, quote/spread, catalyst, halt, sector, and Thinkorswim eligibility are absent. These are required gaps for market-wide discovery/overnight qualification, not just UI polish.
- Collector status before/after restart shows watermark values unchanged, counters resetting from cycles=6 to cycles=1, newly persisted observations=6, lastCycleErrors=0, recentErrors empty. This is consistent with process restart and continued successful polling. It is not a fully isolated proof that the checkpoint file was read from disk. Verify the exact checkpoint file exists, record its last-write time/content before restart, restart API, compare persisted JSON and endpoint watermarks, then wait for a new eligible bar and confirm watermark advancement. Do not expect source timestamps to advance while the provider returns no newer bars.

### Overnight screenshots: corrected interpretation
- Added `docs/Validation/Overnight-Chart-ReReview-20261027.md` and linked it under the solution's Documentation items.
- RTO 5M is constructive (permission YES, bullish structure/regime, ~0.43 ATR extension, RVOL ~5.16), but its newer 1M panel explicitly ends with GET OUT / 5M permission NO / ~5.7 ATR extension and a large last impulse bar with blocked/expired execution labels. Treat it as a conditional watchlist candidate, not an entry-ready buy. The mismatch is timeframe-specific; neither blindly discard 5M nor override 1M.
- ATR extension is not an automatic veto because the strategy prefers “boring.” It is a risk/chase penalty. Here RTO's 1M extension is extreme and the latest 1M state does not confirm entry.
- MFM: top 5M runner/permission positive but low displayed RVOL (~0.13); lower 1M permission NO, ~2.67 ATR extension and GET OUT. BNC: 5M extension ~2.68 ATR and lower 1M ~10.24 ATR/GET OUT. SXC: 5M permission NO after a high-RVOL spike/fade. SAP: bearish/permission NO/~3.87 ATR. PANW: 5M transitioning/~3.35 ATR, lower 1M permission NO/~5.37 ATR. No screenshot alone qualifies a Boring Overnight BUY.
- Screenshots are historical captures, not current market state. Boring Overnight still requires independent historical overnight stats, daily liquidity, exclusions, fresh prices, and catalyst/news clearance. Data not found in sources for a complete current gate check.

### Solution/project loadability
- Reviewed `MomentumCommandCenterV2.sln`: Documentation and Data and Validation Fixtures are standard Visual Studio Solution Folder entries, not executable projects. Their paths are relative to the solution root and the project-type GUID is the standard Solution Folder GUID. The latest checked-in solution references existing documentation/data files from the repository listing.
- GitHub CI is the authoritative repository build check available here; it does not reproduce a user's Visual Studio local SDK/IDE state. If Visual Studio still shows actual `.csproj` entries as “incompatible” or “cannot load,” capture the exact project names and the first Output > Solution/Project load error. Do not remove valid project references or retarget frameworks without that error. Verify installed .NET 8 SDK and Visual Studio version locally.

### Next actions
1. Pull `feature/opportunity-quality-classification`, stop old API processes, restart the API, and upload `data/TTE_ExportAttempt.csv` to `/api/market-data/import-csv`. Expected: 75 parity rows, Symbol TTE, Timeframe 5m, payload values populated, chart values blank. This expectation follows from the checked-in JSON fixture's reported barCount; the endpoint response must be confirmed by user.
2. Upload an actual Thinkorswim report to `/api/strategy-report/upload`, not the Alpaca import endpoint.
3. Confirm CI status on the latest commit; run `dotnet test MomentumCommandCenterV2.sln --configuration Release` locally.
4. Make stale-data a hard scanner gate before market-wide discovery work; then add universe discovery, quote/spread/liquidity checks, catalyst/news with timestamps, halt/sector context, exclusion integration, and independently validated overnight scoring.
5. For RTO, do not place an overnight order based on these snapshots. Re-evaluate only with fresh 5M/1M data and all Boring Overnight hard gates.



## 2026-10-27 — Scan & Analyze and Arizona-local time entry

### Implemented on `feature/opportunity-quality-classification`
- Renamed the scanner action to **Scan & Analyze (1M + 5M)**. The form now retrieves `GET /api/market-data/bars` twice for each supplied symbol, once with `timeframe=1m` and once with `timeframe=5m`, using the existing Alpaca provider selection. Calls are bounded to 1–25 unique symbols per run and run in batches of three symbols to avoid launching an unbounded burst of requests.
- Each symbol's row reports both timeframe bar counts, first-to-last close change, observed high-low range relative to the first close, summed volume, latest source timestamp and latest-bar age. Per-timeframe errors and empty results remain visible. The results explicitly say `OBSERVED · NOT QUALIFIED` when both timeframes return bars, or `INSUFFICIENT DATA` when either timeframe has no bars.
- The UI accepts and labels start/end as **Arizona local time** and converts those wall-clock values to UTC ISO timestamps before calling the API. Conversion uses `Intl.DateTimeFormat` with `America/Phoenix`, not the workstation's local timezone and not a hard-coded UTC offset. The summary reports both the entered Arizona values and converted UTC boundaries.
- Updated the browser script cache-buster so the API-served dashboard loads the new implementation.
- Commits: `cb6da905c5c9a385c24a7cf4a52e6e662f3a865a` (form labels/identity) and `f2baaa28355eb68e19b300dc501049474f842cbf` (dual-timeframe implementation). These commits are source changes, not proof of a passing CI run or local browser test.

### Contract clarification
- `GET /api/market-data/bars` is the correct existing source for normalized Alpaca OHLCV bars; it returns bars and metadata including provider/feed, requested UTC window, source timestamps, gaps, and persistence count. It does **not** return the internal Thinkorswim 5M Permission or 1M EXECUTION READY state.
- Scan & Analyze is a first integrated data-gathering and descriptive-analysis step. It is not yet the full Boring Overnight qualification pipeline. It does not yet calculate validated relative volume, ATR extension parity, overnight historical win rate/median/5D/20D metrics, quote spread, market-wide discovery, timestamped catalyst clearance, halt/sector context, or verified TOS states. Missing inputs must not be interpreted as passes.
- Freshness is displayed as age; this change deliberately does not invent a universal stale threshold. Qualification needs a documented policy that distinguishes intraday entry freshness from completed-session overnight research.
- The date/time controls are wall-clock values in `America/Phoenix`; API parameters and persisted provider timestamps remain UTC. Verify this on the user's Windows browser because source edits alone do not execute JavaScript.

### User verification sequence
1. Pull `feature/opportunity-quality-classification`; confirm the branch and commit.
2. Run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
3. Start the API and open its hosted dashboard (not `file://` preview).
4. Enter a small universe (e.g. `SPY,QQQ,TTE,FMS`), select a window with known bars, and confirm one row per symbol includes both 1M and 5M summaries.
5. Check a known Arizona time manually: on October 27, 2026, Phoenix is UTC−07:00, so Arizona 09:30 should be sent as 16:30Z. Confirm the status text shows the entered local time and the exact UTC values.
6. Try a symbol with no data or invalid provider conditions and confirm the result is `INSUFFICIENT DATA` or displays a request error rather than claiming qualification.
7. Verify CI for the exact resulting branch head before calling the change released.

### Remaining user input / decisions
- Define the intended historical session window for Boring Overnight (including the chosen after-hours boundary and treatment of incomplete current sessions). The current form defaults to the last six hours in Arizona local time; change this default only after the desired workflow is agreed.
- Confirm the symbol source for each run: manual comma-separated list / local watchlist first; market-wide discovery requires a selected and validated discovery feed.
- Agree on explicit freshness policies by workflow. Do not use the same arbitrary age threshold for an intraday entry and a completed historical overnight analysis.
- Supply/confirm the exact installed Thinkorswim 5M and 1M study versions and matching parity evidence before representing API-derived indicators as equivalent to their state labels.

### Release status
- Source implemented; CI/browser execution for commits above is **not yet verified in this work session**. Local test suite was reported by the user as 100 succeeded before these new changes; that prior run does not cover this implementation.


## 2026-10-27 — To-Do execution pass and dashboard Arizona timestamps

### Completed in this pass
- [DONE — SOURCE CHANGE] Scan & Analyze's REASON summary now formats each latest 1M/5M source timestamp in `America/Phoenix` and labels it “Arizona”; it no longer emits the latest bar timestamp as an ISO UTC string inside the reason text. UTC request bounds, API fields, and provider/persistence timestamps remain UTC by design. Commit: `fc17f9febce848abee86f35bdf99232a4f5172dc`.
- [DONE — IMPLEMENTATION SCAFFOLD] Added `docs/Implementation-Backlog-Scaffold-20261027.md`: workstream contracts, acceptance criteria, deterministic test cases, external blockers, and release gates covering dashboard/time, scanner discovery/qualification, providers/collector, Boring Overnight, TOS parity, extended continuation, Morning Report/Red Zone, import/parity, tests, and operating docs. This is explicit scaffolding, not a claim those blocked integrations are complete.
- [DONE — SOURCE AUDIT] Re-read the canonical Master To-Do, current handoff, API routes, and Scan & Analyze code. The dashboard's current Scan & Analyze still retrieves descriptive Alpaca OHLCV; it does not yet calculate validated qualification or TOS state parity.
- [DONE — DOCUMENTATION] Updated handoff to identify the timestamp fix, backlog scaffold, exact current branch state, and next validation actions.

### Per-workstream disposition (all canonical To-Do areas)
- [PARTIAL — 5M AUTHORITY] Source/screenshot evidence supports the observed PBLOCK=2 / EOD=0 revocation; no defect proven. Preserve Permission logic. Remaining: installed-study parity and explicit decision on persistent-latch semantics.
- [PARTIAL — 5M→1M HANDOFF] Source audit exists. Remaining same-bar installed-state tests for fresh/in-trend/continuation/confirmation/armed READY/expiry/re-entry; do not weaken the gate without evidence.
- [PARTIAL — 1M EXECUTION / RUNNER] Diagnostics and source references exist; complete installed-version reconciliation and transition tests remain evidence-dependent. No unsupported study logic changes made.
- [PARTIAL — BORING OVERNIGHT] Criteria and API evaluator scaffold exist; full independent historical edge/boringness, complete eligible window, event clearance and validated ranking are not connected. Unknown inputs must fail closed.
- [PARTIAL — EXTENDED CONTINUATION] Separate-state design and contracts scaffolded. Exact ATR-extension parity, live context and historical shadow results remain required before actionable ranking.
- [PARTIAL — SCANNER] Scan & Analyze collects both 1M and 5M OHLCV for a supplied list and summarizes it. Market-wide discovery, append-only discovery event store, explicit freshness policy, quote/spread/liquidity, catalyst/halt/sector context, exact extension, and TOS gates remain open. Provider choice/entitlement is a blocker for live discovery; no guessed provider endpoint added.
- [PARTIAL — C# / API / COLLECTOR] Historical REST adapters, bars endpoint, persistence/collector and status routes exist. Durable restart proof, correction/retention policy, session-aware gap classification, websocket/reconnect/backfill and production monitoring remain open.
- [PARTIAL — MORNING REPORT / RED ZONE] UI, report/export and API snapshot scaffolds exist. Persisted report snapshots and qualified-candidate ranking remain unconnected; demo/import data must remain visibly non-live.
- [PARTIAL — IMPORT / PARITY] Alpaca CSV/JSON import and StrategyReport separation exist. Rename the JSON fixture truthfully, add filename/header/payload provenance checks, and preserve gaps/partial windows. TOS state parity is still blocked by missing diagnostics.
- [PARTIAL — VALIDATION] Existing sampled OHLCV parity evidence is useful but not full-session parity. Add time conversion, freshness, evaluator, persistence/restart and deterministic report tests; run CI/tests at the exact new head.
- [PARTIAL — README / USER WORKFLOW / VS LOADABILITY] Handoff and canonical To-Do updated. If an actual C# project still fails to load, exact Visual Studio Output error remains necessary; solution folders are not projects.
- [HOLD — BROKER] Schwab API, credentials and order placement remain intentionally unimplemented. Manual Schwab workflow stays the boundary.

### Blockers retained rather than guessed
- Live scanner/provider: provider choice, credentials, entitlements and licensing.
- TOS parity: installed study version and same-bar internal diagnostics.
- Boring Overnight: complete source-backed regular close and eligible overnight-window OHLCV; session boundary decision; actual fills are a separate dataset.
- Catalyst clearance: source with known coverage, timestamps and latency.
- Freshness: separate approved policy for intraday execution and overnight research.
- Visual Studio: exact project name and first load error if an actual project remains incompatible.

### Immediate validation after pull
1. Confirm branch and exact head; run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
2. Open the dashboard served by the API and scan a known window. Confirm the REASON field's latest 1M and 5M timestamps display Arizona time, while the completion status still exposes UTC request boundaries for audit.
3. Verify a known timestamp on the Windows browser configured to a non-Arizona timezone; test both a winter date and a summer date.
4. Verify CI for the exact resulting head. Do not treat source changes or previous-head test counts as current-head verification.

### Release status
- Source/docs updated; local browser behavior and exact-head CI/test results are not verified by this source-editing pass. MCC remains a historical-data research workbench, not a live trading-ready scanner.


## 2026-10-28 — User validation feedback, sparse scanner results, provider date default

### User-confirmed verification of prior commit
- [USER VERIFIED] Pulled `feature/opportunity-quality-classification`, API-hosted dashboard starts, Scan & Analyze runs, 1M/5M times align, Arizona timestamp display looks correct, `dotnet test MomentumCommandCenterV2.sln --configuration Release` reports 100 passed, and CI shows all green for the prior head.
- [OPEN VALIDATION] User's “Summer” timestamp example was 2026-10-26, which is autumn; a true summer-date check (e.g. July) is still needed. Arizona does not observe daylight-saving time and remains UTC−07:00, but verify the timezone formatter with a real summer date.

### Implemented in this pass
- [DONE — SOURCE] Scan & Analyze filters invalid timestamps and sorts returned bars chronologically before summarizing.
- [DONE — SOURCE] Change and high-low range now require at least two valid bars. One bar no longer produces false `0.00% change` / `0.00% range`.
- [DONE — SOURCE] Status is `INSUFFICIENT DATA` unless each timeframe returns at least two valid bars. REASON explains missing/one-bar observations and shows a coverage warning. A bar count is an observation count, not proof of full-window coverage.
- [DONE — SOURCE] Provider Comparison Test Date defaults to the current Arizona calendar date when the field is empty; removed the stale static 2026-10-22 value. Date end includes the last millisecond of Arizona day.
- [DONE — DOCS] Implementation scaffold extended with PMI diagnosis boundaries and the next verification sequence.

### PMI case interpretation
- [USER REPORTED] PMI on 2026-10-26, Arizona 06:02–16:02, returned one 1M bar and one 5M bar, both timestamped 06:55 Arizona, volume 120. Latest bar age was about 3015.6 minutes.
- [CONCLUSION] Previous `+0.00%` / `0.00% range` was mathematically produced from the same lone observation as both first and last. It did not mean PMI was flat or that a decline had not occurred.
- [NOW CORRECTED] One bar is explicitly insufficient to calculate change/range; status and reason report the shortage instead of implying no movement.
- [STILL UNKNOWN] Why the provider returned only one bar. Data not found in sources to attribute this to a ticker issue, provider coverage, feed entitlement, request behavior, or API normalization. Inspect raw `GET /api/market-data/bars` payload and provider/feed/gap metadata before assigning a cause.

### Next checklist — do not skip
- [ ] Run PMI again on the same window; confirm both timeframes are `INSUFFICIENT DATA`, change/range unavailable, and the REASON names the one-bar coverage warning.
- [ ] Compare PMI response metadata (`provider`, `feed`, `barCount`, first/latest source timestamps, `gapCount`) with Provider Comparison results. Diagnose from returned evidence, not assumptions.
- [ ] Run a known liquid symbol for the same interval to check the multi-bar path.
- [ ] Verify Provider Comparison Test Date initially defaults to today's Arizona date, but preserves the user's changed selection.
- [ ] Verify an actual July date (not 2026-10-26) from a browser whose OS timezone is not Arizona.
- [ ] Run Release tests and CI at the exact source head containing the sparse-bar and provider-date changes.

### Status of all remaining unchecked items
Continue work without waiting for another confirmation when a task can be safely implemented from existing contracts. For items requiring a user choice, credentials/entitlements, licensed source, installed Thinkorswim study diagnostics, full source-backed overnight data, or proof from the user's environment: scaffold the interfaces, fail-closed behavior, tests and acceptance criteria; leave the specific external evidence item open. See `docs/Implementation-Backlog-Scaffold-20261027.md`. Do not relabel scaffolding as completed functionality.


- [DONE — SOURCE, 2026-10-28 follow-up] Scanner summary also excludes missing high/low fields from range inputs and requires finite first/last close values before calculating change. This prevents malformed/missing price fields from being displayed as a numeric movement result.
- [VALIDATION] This follow-up is source-only verified by inspection. The available GitHub combined-status lookup returned no status records for the latest source commit; do not treat the prior green CI as proof for this exact head.


## Verification checkpoint — Provider Comparison date and Swagger CI — 2026-10-28

- [DONE — SOURCE COMMITTED] Fixed the Swagger tag mapping to use ordinary `if/else` conditions instead of the invalid switch-pattern arm that failed CI with CS8780. Workflow run [40732946327](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40732946327) had failed in Build; Test was skipped. The user's 100/100 local test result does not supersede that CI failure.
- [DONE — SOURCE COMMITTED] Fixed Provider Comparison date initialization. `providerDate` is an HTML `type=date` input and must receive `YYYY-MM-DD`; the previous code supplied a datetime-local value (`YYYY-MM-DDTHH:mm`) that the browser rejects as invalid, leaving the field blank. Initialization is placed before later dashboard rendering, and the script cache query is bumped to `20261028-provider-date-and-swagger-fix`.
- [PENDING — CI] Verify build and all tests on the new branch head. Do not mark CI green until a new workflow run completes successfully.
- [PENDING — USER RUNTIME] Refresh dashboard and confirm Provider Comparison displays the current Arizona date on initial load and after refresh; verify a manually chosen date is preserved.
- [PENDING — DIAGNOSTIC] BrowserLink warning `$.noConflict is not a function` remains unconfirmed. Compare a run with Visual Studio Browser Link disabled or outside debugging before changing application code.
- [DONE — USER VERIFIED BEFORE THIS CHECKPOINT] Swagger workflow groups appeared in UI; Scan & Analyze Reason text wrapped without widening the page.


## Implementation checkpoint — fail-closed validation hardening — 2026-10-28

- [DONE — SOURCE IN THIS COMMIT] Boring Overnight evaluator now rejects blank/invalid symbols and non-finite numeric values (NaN, positive infinity, negative infinity) as DATA? before gate evaluation. This closes a correctness hole because NaN comparisons can all evaluate false and accidentally let a malformed snapshot appear eligible.
- [DONE — TESTS ADDED IN THIS COMMIT] Added evaluator regression cases for NaN/infinities and invalid symbols, plus raw market-bar store tests for non-finite prices, impossible OHLC relationships, and negative volume.
- [PENDING — CI] Run the Release build/test workflow for this exact commit before treating the new cases as passing.
- [NOT IMPLEMENTED / BLOCKED BY EVIDENCE] Do not claim autonomous Boring Overnight qualification: daily/overnight metric computation, complete GTC_EXT session coverage, session-aware gap classification, catalyst source, and authoritative TOS states still require implementation and/or source evidence. The evaluator continues to accept caller-supplied snapshots only.
- [NOT IMPLEMENTED / BLOCKED BY EXTERNAL SETUP] Live scanner discovery and provider proof remain blocked by provider selection, credentials/entitlements, coverage/licensing, and real-data smoke tests. The current collector is explicit-symbol historical REST polling, not a market-wide scanner or websocket stream.
- [USER/LOCAL VALIDATION OPEN] Confirm the Provider Comparison date after browser refresh; source initialization and cache-bust are committed, but browser behavior has not been independently observed here.
- [OPEN] BrowserLink $.noConflict is not a function remains unisolated. Compare with Browser Link disabled before adding application workarounds.


## Follow-up correction — validation hardening compile fix — 2026-10-28

- [FIX COMMITTED] The first validation-hardening CI run failed during build with CS0136 because a local variable named reasons conflicted with a later declaration in the evaluator. Renamed the early-return variable to invalidReasons in commit 65c83f7918200e413a5b20fb7942392357c7d193.
- [PENDING — CURRENT HEAD] Re-run build and tests on the exact latest head after this fix. The previous run 40737130070 is a known failure and must not be reported as passing.
- [NEXT] Verify the new evaluator tests for NaN/infinite values and invalid symbols, and the store tests for invalid OHLCV, once the workflow completes.


## Follow-up — batch validation is atomic — 2026-10-28

- [DONE — SOURCE COMMITTED] The raw-bar store now validates every bar in a batch before opening the append stream. A malformed later row can no longer leave earlier rows from that same batch partially persisted.
- [TEST ADDED] Invalid-OHLCV tests now submit a valid bar followed by the invalid bar and assert the store creates no file.
- [PENDING — CI] Confirm the latest branch-head workflow passes build and tests; earlier validation commit 4208441 failed build, and commit 65c83f7 fixes that compiler issue. Neither failure is considered green.


## Verification resolution — validation hardening CI green — 2026-10-28

- [DONE — CURRENT SOURCE HEAD VERIFIED] Commit 9c1b677682f7dd659d355669ddceb99194a7d5fb passed GitHub Actions run [40737545891](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40737545891).
- [PASS] Release build succeeded.
- [PASS] 108/108 tests passed, including the new Boring Overnight non-finite/symbol fail-closed tests and malformed OHLCV batch-validation tests.
- [DONE — SOURCE] The raw market-bar store validates the complete batch before opening its append stream; malformed later bars cannot leave earlier bars from that batch partially persisted.
- [STILL OPEN] Provider Comparison date must be confirmed in the user's browser after refresh; BrowserLink warning remains unisolated; live provider credentials/entitlements and source-backed overnight coverage remain external/data blockers.
- This verification applies to source commit 9c1b677682f7dd659d355669ddceb99194a7d5fb. Any later source or documentation commit requires checking its own CI run before calling that new head verified.




## Overnight Scanner review — 2026-10-28 (evidence correction)

- [CORRECTED] The earlier wording claiming all 11 individual ticker charts had been reviewed is not supported by the evidence accessible in this session. The GitHub image route did not return those image contents. Do not treat the earlier EVR-first / SXC-recovery chart-pattern claims as verified from today's source evidence.
- [DONE — REVIEW RECORDED] Added `docs/Validation/Overnight-Scanner-Review-20261028.md`, based on the readable scanner screenshot and the MCC table supplied by the user.
- [PRELIMINARY BORING OVERNIGHT ORDER] RTO, AMT, SXC for further chart review only. RTO has a reported 1.29% MCC range and 70% ON win rate; SXC has a 6.20% range and should not automatically be ranked first for a low-noise strategy.
- [DONE — USER CLARIFICATION APPLIED] The red 1M `EOD: GET OUT` state at 12:55 Arizona is intended live-dashboard behavior, not a surprise or automatic candidate disqualification. Keep 5M setup/permission distinct from 1M execution/exit state.
- [IMPORTANT] All supplied MCC rows say `OBSERVED · NOT QUALIFIED`; the snapshots are around 12:55–12:59 Arizona and are not current actionable entries.
- [BLOCKED — FRESH DATA] Current quote refresh, catalyst/news clearance, and next-session event risk were not verified. Catalyst status: Data not found in sources.
- [NEXT IMPLEMENTATION] MCC should expose a unified candidate-review record combining overnight statistics, 5M setup/permission, 1M execution state, sample completeness/freshness, extension/range, catalyst status, and a human-readable action. Do not fabricate weights; preserve raw fields and missing-input reasons.
- [NEXT IMPLEMENTATION] Add a freshness gate that marks stale bar windows and prevents old `OBSERVED` rows from being mistaken for current entries. Define age thresholds from the application's session/data contract before enforcing them.


## Overnight Scanner review and action checkpoint (2026-10-28)

- [DONE — EVIDENCE REVIEWED] Added docs/Validation/Overnight-Scanner-Review-20261028.md using the readable scanner screenshot and supplied MCC 1M/5M table. Linked individual chart images could not be inspected; no candle-pattern claims were fabricated.
- [PRELIMINARY RANKING] RTO first, AMT second, SXC third for further Boring Overnight chart review. None is READY. SXC's 6.20% intraday range conflicts with the low-noise objective; RTO has a 1.29% range and 70% ON win rate.
- [WATCH / TOO HOT] TRMD, EVR, MANH, OSW and STNG have current ON moves above the preferred below-3% boringness range; deprioritize for today's boring-first shortlist.
- [DATA STALE] MFM has only 8 bars and last observations around 12:25–12:28 Arizona; mark DATA? / not actionable.
- [IDENTITY CHECK] Confirm BNC instrument identity; scanner description reads “BNB Standard Corp”.
- [OPERATING RULE CLARIFIED] Red EOD: GET OUT at 12:55 Arizona is intentional for the live dashboard.
- [OPEN] All supplied MCC rows are OBSERVED · NOT QUALIFIED. Do not turn scanner rank into execution authorization.
- [NEXT] Verify current data, catalyst/event status, 20D volume, price/20D-SMA, maintained exclusion list, identity, and chart state before any Boring Overnight READY.
- [NEXT ENGINEERING] Boring Overnight remains snapshot-fed; daily/overnight metric calculation, GTC_EXT session collection, catalyst inputs and live scanner discovery are not fully wired end-to-end. Keep unknown/stale inputs fail-closed.
- [BLOCKED BY IMAGE RETRIEVAL] The linked individual symbol charts were not readable through the current GitHub image route. The ranking is limited to the accessible scanner screenshot and supplied MCC table.


## Unified candidate review scaffold — 2026-10-28

- [DONE — CONTRACT SCAFFOLD] Added docs/Architecture/Unified-Candidate-Review-Contract.md defining the unified candidate review record, precedence, data provenance, freshness behavior, reason codes, implementation seams, acceptance criteria and blockers.
- [NOT YET IMPLEMENTED] The contract is not yet wired to domain types, API, persistence, provider data or dashboard. Continue implementation from this contract; do not mark the feature complete based on documentation alone.


## RTO / AMT chart review — 2026-10-28

- [DONE — PROJECT SOURCES REVIEWED] Visually reviewed the directly uploaded RTO and AMT Thinkorswim screenshots; detailed observations are in `docs/Validation/RTO-AMT-Chart-Review-20261028.md`.
- [RTO — HISTORICAL SHORTLIST ONLY] 5M pane shows Runner Rising, Permission YES, RSI 61.1 and extension 0.49 ATR; lower 1M pane shows GET OUT / EOD GET OUT and a session of weakness followed by a late rebound. Do not treat the 5M green state as late-day overnight-entry authorization.
- [AMT — DEPRIORITIZE FOR THIS SESSION] 5M pane shows extension 2.03 ATR and the chart shows a large selloff/recovery from about $175.78 toward $184.02; lower 1M pane ends in GET OUT / EOD GET OUT. Not a boring-first entry from this chart evidence.
- [NO CANDIDATE PROMOTED] RTO remains first for further research, AMT deferred, SXC remains third/caution. No `READY` status established; hard gates and catalyst/data freshness remain unverified.
- [NOW — ENGINEERING] Add source-backed ATR/true-range and ATR-normalized extension calculations with explicit timeframe/lookback and tests; compare against TOS outputs before qualification integration. Preserve distinct 5M authority, 1M execution/EOD, RVOL, historical Boringness and freshness fields.
- [OPEN — STATE RECONCILIATION] The top 5M and lower 1M panes display differing Permission labels. Align exact bar timestamps and study inputs before asserting a bug or changing logic.
- [OPEN — END-TO-END] Boring Overnight remains caller-snapshot-fed; market-calendar-aware close/open pairing, complete GTC_EXT bars, catalyst provider, persistent decision snapshots, and scanner/Morning Report/MarketWatch integration are not verified as end-to-end.


## Signal semantics and implementation — 2026-10-28

- [DONE — CODE] Corrected `BoringOvernightEvaluator` on `feature/opportunity-quality-classification` to model `EodSignal` separately from clock/session state. `GET OUT` / `SELL` is explicitly an active exit signal: it blocks a fresh overnight entry and tells the operator to manage an existing position, but does not mean the trading day has ended. Commit: `89ad3ad03cf80552a973419f0665d54245f753a7`.
- [DONE — TESTS ADDED] Added tests covering `EodSignal=GET OUT` and `SELL` to ensure status is BLOCKED for a new entry and the reason is not misrepresented as market/session closure. Test commit: `fa03c32bbd5692e55fed5225b7fd87106e9dbef0`. CI/build still requires verification on this exact head.
- [RTO — RE-RANKED WITH SIGNAL SEMANTICS] Best of RTO/AMT/SXC for further boring-first review, not READY: supportive 5M trend/permission and modest 0.49 ATR extension, but active 1M/EOD GET OUT is an exit signal and RVOL 4.32 needs review.
- [AMT — DEFER] Active 1M/EOD GET OUT is an exit signal; extension ~2.03 ATR and large intraday selloff/recovery conflict with boring-first entry.
- [SXC — DATA GAP] No SXC chart was available in this review; the 6.20% scanner range is high relative to the low-noise objective. Keep WATCH/DEFER, not READY.
- [OPEN — ATR ENGINEERING] Implement explicit source-backed True Range/ATR/ATR-normalized extension from OHLCV with configurable timeframe/lookback and unit tests; do not integrate as authoritative until parity against selected TOS study output.
- [OPEN — END-TO-END P0] Historical close/open pairing, complete GTC_EXT eligible-window coverage, catalyst provider, persisted candidate decisions, and live scanner/Morning Report/MarketWatch integration remain incomplete. Unknown/stale inputs must fail closed.


## ATR implementation progress — 2026-10-28

- [DONE — SOURCE + TESTS ADDED] Added `BoringOvernightAtrCalculator` using explicit OHLCV bars, timeframe, period, Wilder smoothing, latest True Range, ATR%, and signed ATR extension from an explicitly supplied reference price. Missing history, mixed symbol/timeframe, duplicate timestamps and invalid OHLCV fail closed.
- [DONE — TESTS ADDED] Added tests for Wilder ATR smoothing, ATR%, signed extension, insufficient history, and mixed timeframes. Source commit: `ea8a27700f6609d1e78a7a9d93a3d192426448dc`; tests committed in `0c9be200270327ba70b4ef93a36f535be2b3422b`, with corrected smoothing expectation in `6f000252ba9724dda40687748a6905b17072ae0a`.
- [NOT YET QUALIFICATION-AUTHORITATIVE] The calculator is not yet wired into candidate evaluation. Its Wilder ATR and extension convention must be compared with the exact selected TOS study/timeframe/lookback before it is used as a gate.
- [OPEN — END-TO-END P0] Close/open overnight pairing, complete GTC_EXT coverage, catalyst evidence, persisted decisions, and scanner/Morning Report/MarketWatch integration remain incomplete.
- [CI UNKNOWN] GitHub status checks and commit-associated workflow lookup returned no records for the latest documentation head. Local build/test execution could not run in this environment because direct GitHub network access is unavailable. Do not mark tests as passed until CI or a local build confirms them.


## Time-gated Boring Overnight decision bubble — 2026-10-28

- [DONE — DECISION HELPER + TESTS ADDED] Added BoringOvernightHoldDecision and tests. Inside the configurable 12:30–12:55 local-time window, READY => green HOLD OVERNIGHT; known non-READY => red EOD GET OUT; missing/invalid evidence => yellow DATA? — NO HOLD. Outside the window it returns NO DECISION and does not emit either trading bubble. Timezone is an explicit input, not guessed.
- [DONE — DESIGN DOCUMENT] Added docs/Architecture/Boring-Overnight-Hold-Decision-Bubble.md with decision semantics, color/status mapping, qualification gate contract, production checklist, and precise user instructions.
- [PARTIAL — API HELPER NOT YET INTEGRATED] Helper accepts a supplied evaluator result; it is not yet wired to persisted source-backed candidate data.
- [DONE — THINKSCRIPT SOURCE UPDATED] Updated docs/Charts/1M/MainStudy.txt: 12:30–12:55 Arizona decision window; red EOD GET OUT only on a known chart-technical failure at the final window bar; yellow DATA? — NO HOLD when chart technical checks pass but required external qualification evidence is unavailable. The ThinkScript study deliberately does not emit green HOLD OVERNIGHT because the historical metrics/catalyst/exclusion/freshness evidence is not connected.
- [DONE — 5M LABEL CLARIFIED] docs/Charts/5M/MainStudy.txt now labels the clock-based control as ENTRY CUTOFF, not EOD GET OUT. The late-session entry cutoff logic remains in place.
- [DONE — TIMEZONE CONFIRMED] User selected Arizona time (America/Phoenix). The 1M study converts the window to Eastern clock inputs (15:30–15:55 ET during DST; 14:30–14:55 ET during standard time), because thinkScript time functions use Eastern time.
- [USER ACTION REQUIRED] Copy the updated docs/Charts/1M/MainStudy.txt into the existing 1M Thinkorswim study, save/compile, and validate the decision output on the 12:55 Arizona bar. Do not treat yellow DATA? as a failure; it indicates external qualification data is not yet wired.
- [OPEN P0] Validate ThinkScript compilation and same-bar 1M/5M alignment; wire complete qualification data into the server/API and decide how that authoritative result will reach the chart without manual stale inputs.
- [OPEN P0] Complete historical overnight metrics, GTC_EXT coverage, catalyst/exclusion/freshness evidence, persisted decisions, scanner/Morning Report/MarketWatch integration, and build/test verification.


## Dynamic Overnight bubble — implementation update 2026-10-28

- Removed the independent EOD clock-state override from the 1M `finalState`. The 3:55 PM Arizona cutoff still blocks intraday entries, but it no longer forces the 1M state to GET OUT or independently asserts an overnight decision.
- Added the 12:30–12:55 PM Arizona window; the 1M study evaluates its chart-visible technical blockers at the 12:55 bar. A known technical failure emits red EOD GET OUT. If technical checks do not fail, it emits yellow DATA? — NO HOLD because overnight history, catalyst, exclusions and data freshness are not available in ThinkScript.
- The 5M study's time-based label now says ENTRY CUTOFF ACTIVE/INACTIVE instead of EOD GET OUT/ACTIVE. The late-session entry cutoff behavior is preserved.
- Removed EodSignal as an evaluator input to avoid circular qualification: EOD GET OUT is now an output of the decision, not an independent input that makes the decision fail itself. 1M SELL/GET OUT/BLOCK execution state remains a legitimate gate.
- Arizona convenience overload and timezone test added to the server decision helper.
- Commits: 89e05e0f99fc78a72328b3f8bc52fc2c1b2283bb (1M study), 1fa0e0a49625af0ce970ddd2279f169bcfb62413 (label placement), e8c28953eec60b0ede81847587f247b2aefb360d (Arizona API overload), a4138830e7d96b28755b3a80648c091444687d61 (timezone test), 685994a2545998cdf2601d309eca505a7deb4042 (5M label), 6e28eed55b4f365ff5e38aa937ea6da4b164f9e6 (remove circular EOD input), 70d8b4befd218ae53475e9485c6bf44bee6a4c2a (test update).


## 2026-10-28 — Arizona 12:55 Boring Overnight bubble validation

- [DONE — USER-REPORTED RUNTIME VALIDATION] User installed the updated 1M and 5M studies successfully; both compiled.
- [DONE — USER-REPORTED 1M OUTCOME] At the 12:55 PM Arizona decision bar, SXC displays red `EOD GET OUT` when a defined chart-technical blocker is present. Evidence: [SXC screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212549.png).
- [DONE — USER-REPORTED 1M OUTCOME] RTO displays yellow `DATA? — NO HOLD` when the chart checks do not fail but external qualification evidence is unavailable. Evidence: [RTO screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212531.png).
- [EXPECTED / NOT A DEFECT] Green `HOLD OVERNIGHT` is not expected until the full qualification inputs are connected. The observed RTO DATA? result is fail-closed behavior, not permission to hold.
- [NEXT — IMPLEMENTATION, NO NEW USER INPUT REQUIRED] Connect the server-side decision helper to timestamped persisted candidate snapshots and provider-backed overnight metrics, exclusions, catalyst state and freshness. Keep unknown or stale evidence in DATA? and known technical/hard-gate failures blocked. Add API/scanner/report integration only after the authoritative input contract is complete.
- [OPEN — VERIFICATION LIMIT] The screenshots and runtime outcomes are reported by the user; no automated screenshot parsing or same-bar 1M/5M parity run was performed here. Repository CI/build status for the current head remains unverified until checked explicitly.


## CI recovery and working-state implementation — 2026-10-28

- [DONE — CI VERIFIED] Fixed the stale Wilder ATR test expectation: for the documented 2-period sequence, the seeded ATR is 2.5 and the next Wilder-smoothed value is 2.25. Implementation unchanged. Commit `96283fb20c054aee326205ff6d19c438f3033862`; GitHub Actions run `40818276331` completed successfully. This verifies the pipeline at that commit only; rerun after subsequent source changes.
- [NOW — OVERNIGHT FIRST] Replace caller-assembled qualification payloads with a reproducible, timestamped candidate snapshot contract that records symbol, as-of time, source/provider/feed, source timestamps, calculation version, required fields, missing/stale fields, and reason codes. Persist snapshots append-only; never overwrite history or allow missing evidence to pass READY.
- [NOW] Add a queryable read path over stored OHLCV observations and calculate overnight statistics only from explicitly paired, session-valid close/open observations. Preserve source/provider/feed and correction lineage. Reject incomplete windows; do not treat gaps as zero activity.
- [NOW] Add focused tests for snapshot round-trip, idempotency/corrections, stale/missing inputs, session boundaries, and hard-gate fail-closed behavior. Verify CI after every source commit.
- [OPEN — EXTERNAL INPUT REQUIRED] Live catalyst clearance cannot be completed without a selected/connected news or event source. Keep UNKNOWN => DATA? and provide a precise missing-evidence reason; do not fabricate clearance.
- [OPEN — EXTERNAL INPUT REQUIRED] A real market-wide scanner requires a selected, entitled provider and validated symbol universe. Existing bounded user-supplied-universe scans are not market-wide discovery.
- [OPEN — USER WORKSPACE REQUIRED] Thinkorswim installed-study parity and chart exports cannot be established by repository tests alone. Keep 5M authority and 1M execution gates unchanged until exact source/output comparisons are available.
- [OPEN — VALIDATION REQUIRED] GTC_EXT fill/outcome research, overnight target-touch probability, spread/slippage, and out-of-sample performance require actual timestamped trade/bar outcomes. No profitability claim is supported yet.
- [DONE — STATUS LABEL CONFIRMED BY USER] User observed red `EOD GET OUT` on SXC and yellow `DATA? — NO HOLD` on RTO. The green hold state is intentionally unavailable while external qualification evidence is missing.

### Immediate execution order
1. Build append-only candidate snapshot persistence + read/query contract.
2. Build validated overnight-stat calculation from real session-paired bars.
3. Connect evaluator to persisted snapshot and explicit freshness/provenance; maintain fail-closed semantics.
4. Add tests and keep CI green.
5. Add catalyst/exclusion evidence adapters only when sources are real and validated.
6. Integrate with scanner/Morning Brief; clearly label bounded versus market-wide discovery.
7. Finish TOS parity, GTC_EXT outcomes and out-of-sample research before actionable READY is enabled.

- [IMPLEMENTED — CI PENDING] Added append-only Boring Overnight candidate snapshot storage in `BoringOvernightCandidateSnapshotStore.cs` and endpoints `POST /api/boring-overnight/snapshots` / `GET /api/boring-overnight/snapshots`. Records include as-of/recorded timestamps, source/provider/feed, calculation version, field source timestamps and the evaluator request; API overwrites caller-supplied exclusion flag with MCC's local exclusion list before evaluation. Provenance is recorded, not independently verified. Commit `5de6613ea951d4d38ba160c1108a22ed4ae64870`; run `40818694414` queued at time of this note.
- [OPEN] Add automated tests for snapshot JSONL persistence, query ordering, validation and evaluator integration. Do not treat endpoint availability as a completed qualification pipeline: overnight statistics are not yet derived from validated session-paired bars.

- [DONE — CI VERIFIED] Added `BoringOvernightCandidateSnapshotStoreTests` covering provenance round-trip, case-insensitive symbol reads, non-UTC as-of rejection, and symbol mismatch rejection. CI passed for snapshot store and API source commits: run `40818681818` and run `40818694414`. The newer test commit `8139ef7eb6934fad423c41a29a28cfac4e4bc23b` has its own run queued; wait for that result before declaring current HEAD green.


### Persisted raw market-bar query — 2026-10-28
- [IMPLEMENTED — CI PENDING] Extended `IMarketBarObservationStore` with a read/query contract and implemented latest-version-per-source-timestamp retrieval in `JsonlMarketBarObservationStore`. Historical corrections remain append-only; queries return the latest correction for each provider/symbol/timestamp.
- [IMPLEMENTED — CI PENDING] Added `GET /api/market-data/stored-bars?symbol=RTO&fromUtc=2026-10-27T00:00:00Z&toUtc=2026-10-29T00:00:00Z&provider=Alpaca&limit=1000`. It returns raw persisted observations only; it does not infer market sessions or calculate overnight returns.
- [IMPLEMENTED — CI PENDING] Added tests for correction-aware query results, provider/window filtering, and invalid query inputs. CI for the interface-only intermediate commit failed because the interface was temporarily ahead of its implementation; subsequent commits add the implementation and tests. Verify latest HEAD before calling this green.
- [NEXT] Use the new read path as the foundation for a separately tested session calendar/overnight pairing service. Do not derive Overnight win-rate or return metrics until the regular-session close and next-session open definitions, timezone, eligible bar windows, and missing-session policy are explicit and tested.

- [IMPLEMENTED — CI PENDING] Added `BoringOvernightHistoryCalculator` and `POST /api/boring-overnight/history`. It calculates close-to-next-open returns only from persisted observations matched to caller-supplied exact UTC boundary timestamps. It returns per-pair provenance IDs and missing-boundary reason codes; if any requested pair is missing or history is short, it suppresses rolling metrics. It does not infer holiday calendars or establish catalyst clearance.
- [IMPLEMENTED — CI PENDING] Added tests for 20 explicit close/open pairs, missing-boundary fail-closed behavior, and duplicate next-session boundaries.
- [CI FAILURE DIAGNOSED] The first market-bar query test run failed only because the test expected the corrected bar's open to be 10.5, but the corrected fixture changes High/Close and leaves Open at 10.0. Corrected the assertion; new CI run is pending. No production query failure was indicated by this assertion.

- [TEST FIX COMMITTED — CI PENDING] Corrected two test-fixture expectations discovered by CI: the corrected market bar retains open=10.0, and the second missing-pair fixture lacks the previous-close timestamp. Commit `edef25ded7db56af767a71d8be037e32ef566f56`; workflow `40820294911` is running. Earlier failures are test expectation issues; continue checking the latest run rather than marking green prematurely.


### Swagger timestamp examples and Overnight request templates — 2026-10-29
- [IMPLEMENTED — CI PENDING] Standardized Swagger descriptions for timestamp-bearing market-data endpoints to show ISO-8601 UTC values ending in `Z`, with Arizona conversions for relevant examples. Updated `/api/market-data/stored-bars`, `/api/market-data/compare`, `/api/market-data/bars`, `/api/scanner/universe`, Overnight history/evaluation, and snapshot endpoints. API README now states the UTC convention.
- [IMPLEMENTED — TEMPLATE CREATED, CI PENDING] Added `docs/templates/boring-overnight-evaluate-with-history.template.json` with all required fields and 20 explicit boundary entries. Timestamps and dates are illustrative and MUST be checked/replaced against actual stored source bars before use; the template is not evidence that the store contains those observations.
- [IMPLEMENTED — INSTRUCTIONS ADDED, CI PENDING] Added `docs/templates/README-boring-overnight-api-templates.md` explaining UTC-to-Arizona conversion, how to fill boundaries from stored-bars, what to inspect in the response, the snapshot count=0 diagnostic, and the negative test.
- [NEXT P0] Replace illustrative timestamps in the template with exact timestamps from the user's actual provider/feed store. Add authoritative exchange calendar/session boundary generation and validation before treating computed returns as production-grade.
- [CI CHECK] At time of documentation, CI for the new UTC-description, template, and README commits still needs checking on latest HEAD. Do not mark the current branch green until its latest workflow passes.

- [DOCS REFINED] Template instructions explicitly identify the example `asOfUtc` value and require replacing it with the actual evaluation timestamp; the JSON remains parseable rather than using a non-date placeholder.


## Overnight dashboard bar workflow — 2026-10-29

- [IMPLEMENTED — CI PENDING] Replaced the static Boring Overnight checklist with an API-backed bar retrieval panel. It supports user-selected 1m/5m bars, Arizona-local From/To inputs converted to UTC, requests that extend past 1:00 PM Arizona, reports first/latest source timestamps, feed, bar count, gaps, and newly persisted observations, and reads saved input snapshots.
- [IMPORTANT LIMITATION] This dashboard panel fetches and displays source bars and persisted evaluator inputs; it does not auto-generate the 20 Overnight session boundary pairs or declare a candidate READY. Snapshot GET does not persist or return the evaluator's final decision. Stored bar observations do not currently carry an explicit timeframe identity, so the 1m/5m choice is valid for the direct fetch request but must not be used to claim that persisted-bars history is timeframe-isolated. Fix storage identity before automating historical evaluation.
- [NEXT P0] Add timeframe to persisted observation identity and query filters so 1m and 5m observations at the same source timestamp cannot overwrite or masquerade as each other.
- [NEXT P0] Add an authoritative exchange calendar/session-boundary service, then wire the dashboard to the evaluate-with-history endpoint without manually authored 20-pair JSON. Until then, exact session boundary input remains an API limitation and missing history must fail closed.


## 2026-10-29 — Remaining MCC work audit (six requested workstreams)

- [AUDIT ADDED] Created `docs/Validation/MCC-Remaining-Work-Audit-20261029.md` with implementation truth, blockers, and exact validation plan for all six requested workstreams.
- [5M→1M PARITY — PARTIAL, NOT CERTIFIED] Repository source comparison is complete and documented. 1M reconstructs 5M state and predicates differ in Permission/structural damage/major failure. Installed study and matched-bar behavioral parity remain unverified. No gate changes made.
- [RUNNER / EXIT — SOURCE AUDIT ONLY] 5M is authority for runner/waning/swing/failure; 1M contains short-term exit/invalidation behavior. Matched state-transition validation and installed-study parity remain open; no thresholds changed.
- [BROAD-MARKET DISCOVERY — NOT IMPLEMENTED] Discovery contracts, ingestion, JSONL event store, and worker scaffold exist, but there is no configured production discovery provider or hosted worker. Manual universe is not broad-market discovery.
- [COLLECTOR — PARTIAL] Configured-symbol REST polling, overlap, per-timeframe checkpoints, append-oriented JSONL observations, duplicate suppression, and correction retention exist. Persisted observation identity still omits timeframe; controlled outage/backfill/crash recovery and durable runtime behavior remain unvalidated.
- [MORNING BRIEF — SPECIFICATION ONLY] Requirements are documented; live benchmark health, breadth, broad movers, sector aggregates, sourced catalysts, and immutable report snapshots are not demonstrated end-to-end.
- [BROWSER / RELEASE — CI PASS, BROWSER OPEN] Dashboard code/docs workflows including run `40966465421` passed. No browser-level regression, accessibility, responsive, or release test was executed. Not release-ready.
- [USER LIVE TEST RESULT] RTO 0 bars/feed unknown; SPY Alpaca/IEX 2 bars (20:00Z, 20:54Z) in 20:00–23:55Z window. Confirms one SPY post-market observation only; RTO cause and complete session coverage remain unknown. The Requested bar coverage panel remains NOT LOADED because no request was run there.
- [RELEASE GATE] All six requested workstreams remain partially implemented, unverified, or not implemented end-to-end. See the new audit document for exact test steps and release disposition. Do not mark READY or release-ready based on the control test.


## 2026-10-29 — Engineering follow-up

- [PARTIAL] 5M→1M source audit: installed study files are already in GitHub. Installed 1M is V14.4.10; repository 1M is V14.4.11. Exact installed source and matched-bar parity remain open.
- [PARTIAL] Runner/exit: October 28 RTO/AMT screenshot review was completed. It is not a timestamp-aligned state tape. See `docs/Validation/Runner-Exit-State-Validation-20261029.md`.
- [IMPLEMENTED FOUNDATION] Alpaca SIP top-50 gainers/losers source, polling host, JSONL persistence, API endpoint, and scanner UI refresh control added. Entitlement/full-universe coverage not verified.
- [IMPLEMENTED] Persisted market-bar identity now includes timeframe and feed. Overnight history queries 1m only. Legacy rows stay UNKNOWN; new provider fetches are required to backfill correctly tagged history. Added regression tests.
- [PARTIAL] Morning Brief now includes SPY/QQQ/IWM, 11 sector ETF proxies, Alpaca mover observations, and timestamped unclassified news. Full-market breadth and catalyst classification remain DATA NOT FOUND IN SOURCES.
- [PARTIAL] Collector local restart/outage/backfill validation and browser regression remain.
- Runbooks: `docs/Validation/Collector-MorningBrief-Browser-Runbook-20261029.md` and `docs/Validation/Runner-Exit-State-Validation-20261029.md`.
- CI green for current code/test changes. Release remains NOT RELEASE-READY until local provider, collector, matched-bar, and browser evidence passes.


## 2026-10-29 — RTO runner evidence, collector reconciliation, and dashboard chart UX

### Verified from user's latest RTO chart and local API output
- [EVIDENCE RECEIVED] New RTO 5M/1M screenshot shows 5M status `5M STATE: GET OUT`, while the 1M status strip shows `1M STATE: EXECUTION EXPIRED`. The 1M pane does **not** show `GET OUT` or `EOD GET OUT` in this capture. Do not reuse the earlier Oct 28 screenshot description for this separate chart.
- [PARTIAL] The 5M pane contains a visible progression of permission/diagnostic bubbles and a later runner event. Exact event-to-event parity still requires timestamp-aligned 1M/5M CSV rows. The exported files were said to be committed under `data/validation/RTO`, but their exact filenames/content were not exposed by the repository file reads available in this pass: Data not found in sources. Do not claim the exports have been parsed.
- [PARTIAL — COLLECTOR] User confirmed repeated ingestion returned `newlyPersistedObservations: 0` with zero cycle errors, consistent with idempotent replay for the observations already present.
- [OPEN — COLLECTOR SYMBOL/WATERMARK RECONCILIATION] Status reports configured symbols SPY, QQQ, IWM, RTO, AMT, but the checkpoint contains only SPY, QQQ, TTE, FMS keys. There are no shown IWM/RTO/AMT watermarks; TTE/FMS keys appear to be retained from a prior symbol configuration. Verify current worker's configured symbol set and actual returned bars before calling per-symbol collection complete. Do not create watermarks for empty responses.
- [VERIFIED USER OUTPUT] Alpaca provider status reports configured/validated and one returned bar. This is a successful non-empty data validation, not a full coverage or scanner-entitlement proof.
- [USER CONFIRMED] Morning Brief market/sector/news content displayed substantial data. User requests each headline's URL be presented as a `News Link` hyperlink with the raw URL as hover tooltip.

### Dashboard changes committed
- [DONE — CODE] Morning Brief news headline URLs render as `News Link` anchors with the URL in the title tooltip; published timestamps display in Arizona time.
- [DONE — CODE] Discovery group labels render as ATG¹ / ATL¹; change is displayed as a percentage and source timestamp as Arizona local time. Added the abbreviation/field footnote to Candidate Discovery.
- [DONE — CODE] Added API-backed 1-minute close-price line charts to the focused-symbol chart area and Red Zone cards. Empty/failed responses display as unavailable; they are not rendered as flat/zero lines. Selecting a candidate tile focuses the matching symbol when that symbol exists in the main selector.
- [OPEN — BROWSER VALIDATION] Verify API response shape, price line rendering, card click-to-focus, hover tooltip, responsive layout, and no console errors in the local browser.

### Remaining RTO validation
- [NEXT] Locate/confirm the exact 1M and 5M export filenames in `data/validation/RTO`; parse both and report row counts, first/last timestamps, timezone convention, OHLCV validity, duplicate/gap counts.
- [x] Converted embedded epoch-millisecond timestamps to UTC and Arizona. They identify bar-start times. Grouped 1M bars by their 5-minute start buckets; all six 5M OHLCV candles match exactly. StrategyReport Date/Time event timestamps remain a separate clock and are not used as candle timestamps.
- [NEXT] Build the state tape from matched 5M/1M study event labels and assert only observed transitions. Keep 5M permission and 1M execution/exit state as separate state machines.
- [OPEN] Matched timestamp parity and runner/exit replay remain unvalidated. No change to ThinkScript trading gates is authorized by this screenshot alone.

## CI checkpoint — 2026-10-30

- [CI VERIFIED] GitHub Actions run **41095950730 / run 618** completed successfully for commit `7c7e20810b70f2a6620fb8f752844a15eed130c3`. This run includes the dashboard code, favicon, user manual, provider comparison note, browser runbook, and master To-Do updates committed through that SHA. The workflow is the repository's .NET build/test workflow; it does not execute browser UI tests.
- [BROWSER VALIDATION OPEN] Local browser acceptance remains outstanding for the new layout and favicon, and the BrowserLink `$.noConflict` error remains unresolved pending its injected script URL/stack trace.


## 2026-10-31 — Boring Overnight functionality and calculated limit

- [x] Clarified that 20 means 20 valid trading-session pairs, not calendar days; preserve 5D vs 20D comparison and explicit sample/gap counts.
- [x] Separated close-to-next-open Overnight edge from the user's 07:00–20:00 Eastern GTC_EXT sell opportunity.
- [x] Documented catalyst clearance semantics: validated source health/coverage and timestamped event review; unknown remains UNKNOWN.
- [x] Clarified 5M/1M contract is established; parity certification is a one-time exact study-hash/input configuration task, invalidated only by source/input changes.
- [x] Added a visible Calculated Limit Sell panel that fails closed as DATA? until target-distribution and ATR evidence are validated.
- [ ] Build authoritative exchange-calendar boundaries, including holidays/early closes; auto-pair regular-session first/last 1M bars.
- [ ] Build full GTC_EXT historical target-touch dataset; do not confuse a touch with an actual fill.
- [ ] Select target quantile from walk-forward results; no arbitrary ATR multiplier or fixed +0.5% target.
- [ ] Integrate source-backed reference close, historical target quantile, ATR context/parity, target-touch rate and coverage into Calculated Limit Sell.
- [ ] Connect validated catalyst source with coverage and health semantics.
- [ ] Certify exact installed 5M TOS study/hash/input parity once and reuse until changed; do not repeatedly ask the user to re-establish the architecture contract.
- [ ] Run exact-head Release build/test and browser acceptance after code changes.

Design note: docs/Validation/Boring-Overnight-Requirements-and-Limit-Policy-20261031.md.


### Sell-limit calculation engine — 2026-10-31

- Added BoringOvernightSellLimitCalculator. It takes the selected pre-decision historical quantile of per-session maximum favorable price movement during complete eligible sell windows, calculates a candidate limit from the current reference close, and reports historical touch rate plus target distance in ATR units.
- ATR is context only; no arbitrary ATR multiplier is used. The calculator requires at least 20 complete windows and exact ATR parity status MATCH before returning a numeric candidate. Incomplete windows return DATA?; unverified ATR parity returns NOT VERIFIED.
- Added unit tests for the nearest-rank quantile, in-sample touch rate, incomplete windows, short history, and ATR parity gate. CI/build has not yet been verified for this commit.
- This is the calculation engine only. The API does not yet assemble full eligible-window observations from persisted bars, and the dashboard does not yet call this calculator. The dashboard correctly remains DATA? until that integration and its source evidence exist.


## 2026-11-03 — CORRECTION: Boring Overnight review begins before the close

- [x] Correct the strategy definition: Overnight includes Day 1 (Buy Day) after-hours **and** Day 2 (Sell Day) premarket. The earlier 20:00 ET process-start recommendation is withdrawn; waiting until after after-hours would miss part of the measured opportunity.
- [x] Specify operator timing: begin actionable review during the final 60 minutes of Day 1 regular trading, before the close. Display in `America/Phoenix`: 1:00 PM Arizona during Eastern Standard Time; noon Arizona during Eastern Daylight Time. Derive from the actual exchange close and account for holidays/early closes.
- [x] Separate precomputed historical Overnight statistics from the final-hour refresh of current price/volume, current Overnight move, news/event risk, 5M structure/permission, and data freshness.
- [ ] Verify the exact historical Overnight return endpoint and boundary-bar inclusion against the actual source implementation and raw data. Preserve after-hours and premarket observations separately; distinguish endpoint return, full-window path/high/low, and executable fills.
- [ ] Implement/verify the runtime schedule using the exchange calendar and `America/Phoenix`; documentation alone does not change runtime behavior.
- [ ] Verify broker buy-order session eligibility separately. No automatic order submission is authorized by this workflow.

**Status note:** this is a strategy/operator timing correction, not a claim that the scanner, historical calculations, scheduler, or broker integration has been implemented. The earlier 20:00 ET recommendation must not be followed.


## 2026-11-03 — V14.5.1 Library source found and V14.5.2 rebased

- [x] Project Library search found and full-file read `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (1,260 lines, V14.5.1). The uploaded `CurrentStudy_CouldBeWrong.txt` is V14.5 (1,424 lines), not the same file; substantial differences exist in execution-window, reconstructed 5M, state and bubble logic.
- [x] Replaced the initial V14.5.2 draft with a V14.5.2 derived from the actual V14.5.1 Library source: `docs/Charts/1M/MainStudy_V14.5.2.txt` (1,298 lines). The uploaded V14.5 file remains unchanged.
- [x] V14.5.2 adds 5M V4.1.7 Permission parity (5M close > Daily VWAP, 5M close > EMA9, EMA9 >= EMA20, RSI >= floor) while retaining the separate 1M VWAP execution gate; it also adds DST-aware 3:55 PM Arizona EOD mapping.
- [ ] TOS compile and matched-bar parity remain mandatory; .NET CI does not compile ThinkScript.


## 2026-11-04 — operator measurements and next engineering sequence

### User-verified acceptance
- [x] Dashboard page load: 732 ms total; DOMContentLoaded: 638 ms. Pending localhost WebSocket is persistent and is not treated as a completed page-load request.
- [x] Morning Brief is deferred until modal open; observed modal request duration 6.49 s.
- [x] Fetch & Analyze bars: RTO 105 ms then 91 ms; EVR 102 ms. Timing alone does not establish cache/provider freshness.
- [x] 20-session capture: RTO 9.12 s; EVR 6.22 s. Preserve exact-boundary validation.
- [x] RTO capture appended a record labelled INPUT SNAPSHOT — NOT A DECISION.
- [x] EVR eight NEXT_SESSION_OPEN boundaries absent from provider response and store; nearest bars 1–7 minutes later. No imputation or timestamp shift.

### Snapshot evidence clarification
The compact table does not prove that all historical metrics were populated only when all 20 pairs were valid. Improve snapshot contract/UI to show expected/valid pair counts, historical-metrics status and per-field presence/status. Keep incomplete metrics null and current price, Daily liquidity, catalyst, 5M permission/structure and 1M state UNKNOWN unless source-backed.

### Daily market-data and liquidity — next implementation priority
- [ ] Inspect provider capabilities/configuration and add a separate Daily-bar contract; do not reuse 1m/5m assumptions.
- [ ] Implement source-backed Daily bars with provider/feed, retrievedAtUtc, source timestamps, adjustment mode, session date and completeness metadata.
- [ ] Calculate last eligible price, 20 completed regular-session SMA, and 20-session average regular-session volume from completed Daily bars only. Exclude current incomplete session; document corporate-action adjustment semantics. Ambiguous coverage/adjustment means DATA?.
- [ ] Test exactly 20 sessions, fewer than 20, duplicate/missing dates, current partial day, stale data, adjustment metadata and price/volume threshold boundaries.
- [ ] Expose Daily inputs/provenance in evaluator/snapshot. Do not promote READY until all other gates pass.

### Next work sequence
1. Daily market data, SMA20 and average volume.
2. Catalyst evidence/classification with CLEAR_WITH_SOURCE / RISK_PRESENT / UNKNOWN / STALE and source/publication/observed timestamps.
3. Snapshot completeness visibility and bars cache/freshness observability.
4. Official opening-print source/feed parity; IEX 09:30 minute-bar Open remains a proxy only.
5. Opportunity Quality current-data integration and Red Zone qualification.
6. GTC_EXT sell-window dataset, walk-forward target validation, actual fill/exit/P&L linkage.

### Operator inputs needed
- No further EVR missing-row export is needed.
- After Daily endpoint implementation, run local RTO and EVR smoke tests and provide response bodies plus provider/feed status. Include adjustment metadata if returned.
- For bars freshness diagnosis, provide one Fetch & Analyze response body per symbol and response headers if available. Do not repeatedly rerun without a defined cache-bypass test.
- Local build/browser acceptance remains operator-side; report exact output after pulling the branch.

[Uncertainties]
- The 91–105 ms timings do not establish cache, persisted-store, or fresh-provider origin. Data not found in sources.
- The pasted snapshot summary does not expose complete historical metrics or explicit 20/20 completeness. Data not found in sources.
- Daily adjustment behavior, entitlement and provider coverage remain unverified.


## 2026-11-04 — MCC-native recovery actions and workflow corrections

- [x] Production and prototype System Readiness are now collapsed by default behind a disclosure summary. The underlying readiness details remain available on demand. Browser visual acceptance is still required.
- [x] Focused Decision Explanation now displays `UPDATING TO <SYMBOL>` while requesting a new symbol assessment. A request sequence guard prevents a slower prior-symbol response from replacing the latest symbol's result. Browser acceptance is still required.
- [x] Timeframe Handoff now explicitly distinguishes entry review from exit management. The current live panel is not a position-specific exit tool because no broker-synchronized open-position state is available.
- [x] Corrected historical CSV qualification precedence: if any required custom-expression metric is unavailable, the row is `DATA?`, not `FAIL`. A row is `FAIL` only when all required metrics are present and at least one gate fails. Browser/test acceptance remains pending.
- [x] Renamed the existing 20-session history action to **Fetch / retry 20-session close/open history** and clarified that it re-fetches exact provider bars, persists returned observations, and re-audits all 40 required close/open timestamps. It cannot create a missing provider bar or substitute a nearby minute.
- [x] Added **Fetch 20-session GTC_EXT sell-window history** to the Calculated Limit Sell panel in production and prototype. It checks `/api/providers/status` first and makes no provider request unless the effective Alpaca feed is SIP. When SIP is effective, it invokes the existing capture endpoint and displays exact 780-minute window coverage, missing observations, and provenance.
- [ ] Current effective feed is IEX from local `secrets.json`; recent SIP access previously returned HTTP 403. Therefore the new GTC_EXT button is expected to stop at its preflight and report why; no SIP history capture has been performed by this UI change.
- [ ] EVR's eight exact 09:30 ET opens remain absent from the supplied provider response/store. Use the 20-session retry button after the UI update is deployed. It can populate them only if the provider returns the exact timestamps; nearest later bars remain diagnostic and cannot be imputed.
- [ ] Calculated Limit Sell remains DATA? / NOT CALCULATED. Do not connect the captured window highs to a numeric target until 20/20 exact SIP windows are complete, ATR parity is MATCH, and walk-forward target-touch/adverse-excursion validation is complete. Actual bar-high touches are not fills.
- [ ] The top-four Daily + catalyst button is documented as a post-import shortlist refresh, not a pre-entry or exit button. It queries only the four highest-scored imported CSV rows and cannot emit READY.
- [x] Exact-head GitHub Actions run [41916672903](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41916672903) passed on `3c27c787f7ffcb434be9893ca73ba58b8cad00fe`; workflow includes `node --check frontend/app.js`, .NET 8 restore, Release build and full tests.
- [ ] Local browser acceptance remains required for the 443-row import, Red Zone rendering, collapsed readiness, symbol loading state, and new GTC_EXT preflight/result feedback.


## 2026-11-04 — Dual top-four workflow and remaining completion plan

### Implemented in the latest UI pass
- [x] Added an explicit **MCC Top 4 / My Top 4** mode for Daily + catalyst retrieval in both production and prototype. MCC Top 4 selects the four highest-scored imported rows; My Top 4 allows exactly four user-selected imported symbols. The two sets are independent; My Top 4 selections and active mode persist in browser local storage. Evidence fetch targets only the active four and labels the group/symbols queried.
- [x] Preserved the imported qualification summary and historical gate counts. Manual shortlist selection does not change PASS/FAIL/DATA? counts or create READY.
- [x] Moved Morning Command Brief adjacent to the product title using a flex title row. Moved WebSocket stream status into System Readiness; prototype build badge is also inside System Readiness.
- [x] Moved Quick Start into the left prototype dock. Production page has no left aside/dock in its current information architecture; retain/restore a production-accessible Quick Start entry before release (do not leave the dialog unreachable).
- [ ] Browser acceptance: verify both shortlist modes, persistence after refresh, exactly four symbols per request, 443-row qualification counts unchanged, and both production/prototype header behavior.

### Eight requested completion workstreams — current dependency disposition
1. **SIP provider access and exact history recovery:** implementation supports configured-feed checks and refuses to mislabel IEX as SIP. External blocker: user Alpaca account entitlement and local runtime configuration. Exact EVR 09:30 ET opens were not returned by previous provider requests; no interpolation allowed. Once SIP is entitled and effective, retry exact timestamp recovery and verify provider response + persisted store.
2. **GTC_EXT to calculated sell-limit engine:** capture/audit endpoint and calculator exist. The UI exposes SIP-preflighted capture. Remaining code: connect persisted calculator observations to the limit calculator, return a fully provenance-bound result contract, and surface sample counts/quantile/ATR context. Gate on 20/20 complete SIP windows and exact reference closes; current IEX runtime has 0/20 complete windows for RTO/EVR.
3. **ATR parity and walk-forward validation:** ATR calculator and parity notes/tests exist; numeric parity is not proven. Requires user-provided Thinkorswim same-symbol/same-completed-bar OHLC/ATR14/extension values and exact timeframe/session settings. Do not connect a numeric limit or label validation passed until matched parity and out-of-sample walk-forward target-touch/adverse-excursion results are recorded.
4. **Position-aware entry/exit lifecycle:** core OpenPosition and ExitReason models plus runner/exit validation notes exist, but broker-synchronized open-position state and lifecycle UI are not complete. Requires a read-only broker position/order/fill data source and user-side Schwab app authorization/configuration; never request credentials in chat. Until then the timeframe handoff remains an entry/technical context panel, not a position-specific exit manager.
5. **Opportunity Quality live-input integration and ranking:** assessor/config/input models and snapshot adapter exist, and provisional reason codes are visible in Red Zone. Missing: live source adapters for complete candidate-pool inputs, versioned full-pool ranker, validated thresholds/calibration, stable rank/hysteresis, persisted source watermarks, and API/UI acceptance. Scanner score remains a preview and must not be called calibrated opportunity quality.
6. **Catalyst human-label validation:** automated Alpaca phrase triage and append-only human-review storage/API exist. Missing: operator-facing review UI, source-backed human labels, adjudication, a representative labelled sample, measured false-positive/false-negative rates, and versioned validation. No-news remains UNKNOWN; never emit CLEAR_WITH_SOURCE from phrase absence.
7. **Browser validation:** code/CI is not browser acceptance. Requires local pull/run and production + prototype browser checks for 443-row import, 168 PASS / 32 FAIL / 243 DATA?, Red Zone, top-four toggles, System Readiness, symbol loading, capture buttons and no console/network errors. Record screenshots and HAR/timings.
8. **Thinkorswim matched-bar and EOD acceptance:** requires the installed V14.5.2 1M and V4.1.7 5M studies compiled in Thinkorswim and exact matched completed-bar outputs across standard/daylight time, cutoff transitions and EOD. .NET tests cannot validate ThinkScript compilation or installed study state.

### User inputs required to unblock external dependencies
- Confirm Alpaca account entitlement includes SIP, then configure the local API's effective feed to SIP using the documented local secrets workflow; restart and share sanitized /api/providers/status output only. Never share keys/secrets.
- Supply one matched Thinkorswim capture/export for a symbol and exact completed timestamp showing 1M OHLC, VWAP, ATR14, ExtensionATR and 5M OHLC/permission fields, plus study versions/settings and session scope.
- For position lifecycle, configure a read-only Schwab developer app/authorization locally and provide sanitized position/order/fill payloads. No trading credentials or tokens in chat; no order placement is in scope.
- Perform the browser acceptance after pulling the branch and return screenshots plus any console/network errors. The user can choose four symbols in My Top 4 directly in MCC; no five-symbol shortlist will be truncated automatically.

[Uncertainties]
- SIP entitlement, official opening-auction print access, exact Thinkorswim installed versions/settings, and Schwab read-only integration status are not established by repository source alone.
- Browser runtime acceptance and real provider coverage remain unverified until tested in the user's local environment.


- [x] Added MCC-native catalyst article review queue to production and prototype qualification panels. After Daily + catalyst retrieval, returned articles show source link, headline, publication timestamp, provisional triage/category and human-label selector (MATERIAL_RISK / MATERIAL_EVENT / NON_MATERIAL / IRRELEVANT / DUPLICATE / UNCERTAIN), notes and append-only save to /api/market-data/catalyst-reviews. Human labels remain separate from automated triage and cannot clear candidates.
- [ ] Collect and adjudicate a representative article sample, double-review a subset, measure reviewer agreement and classifier precision/recall on a time-separated holdout. The UI is a label-collection mechanism, not validation completion.


 
## 2026-11-04 — SIP 403 diagnosis and sell-limit/ATR execution status

### Operator evidence
- [x] Recorded runtime failure: Alpaca returns HTTP 403 with `subscription does not permit querying recent SIP data` for recent SIP bars. This is a provider-plan entitlement rejection, not a missing symbol, timestamp, MCC parsing issue, or retryable transient.
- [x] Clarified feed choice: `IEX` is partial-exchange coverage and can support diagnostics where endpoints permit it; `SIP` is consolidated coverage and required for MCC qualification/live-movers/GTC_EXT research. The configured feed is an application setting, not an automatic upgrade. A SIP request cannot succeed until the Alpaca account has SIP market-data entitlement.
- [x] Shortened WebSocket readiness chip to compact states (WS OFF / WS OK / WS <state>), moved verbose counts, symbols and last error into its tooltip, and constrained the chip to its parent readiness card.
- [ ] Verify exact-head CI and browser fit in production and prototype after pull.
- [ ] User must enable/subscribe to Alpaca SIP market data in the Alpaca account. Do not keep retrying the same recent SIP request while entitlement remains denied.
- [ ] After entitlement is active, set effective `MarketData:Alpaca:Feed=sip` in local configuration (check environment-variable overrides too), restart API, verify `/api/providers/status` reports configured/effective SIP, then test one recent SIP bar request for SPY. Do not paste API keys or secrets.
- [ ] Only after the one-symbol request succeeds, retry exact EVR close/open boundaries and GTC_EXT capture for RTO/EVR; verify fetched and persisted exact timestamps, 20/20 complete windows and source provenance.

### GTC_EXT calculation pipeline
- [x] Exact-window capture and persisted-history audit exist. Each of 20 windows requires the exact prior regular close plus all 780 1-minute timestamps 07:00–19:59 ET. The service does not impute missing bars.
- [x] Sell-limit calculator exists and suppresses numeric output unless supplied ATR parity is MATCH and at least 20 complete eligible windows exist. It also withholds positive targets when the chosen historical quantile is flat/negative.
- [x] The calculation pipeline now has a fail-closed evidence orchestration API and Dashboard control. It reads persisted reference-close/provider bars and corrected V2.1 ATR evidence; numeric output remains blocked by the explicit walk-forward gate and unverified SIP entitlement.
- [x] Implemented `POST /api/boring-overnight/sell-limit`: it loads persisted history, verifies exact reference close, V2.1 ATR parity and same-source 5m OHLCV, and returns provenance/reason codes. The calculator is not invoked until all evidence gates, including walk-forward, pass; no caller-supplied MATCH flag is accepted.
- [ ] Add walk-forward evaluation using strictly prior eligible windows for each forecast and next-session observed highs/lows; include target touch, adverse excursion, sample count, missing-data exclusions and source identifiers. A bar high touch is not a fill. Do not optimize quantile/coefficients on the holdout.
- [x] UI shows DATA? / NOT CALCULATED until the orchestration API returns a fully validated result. History capture and sell-limit assessment are separate actions.

### ATR parity and walk-forward
- [x] Wilder ATR implementation exists with explicit period/timeframe and source bars; signed extension helper exists.
- [ ] Exact parity is unproven. Need identical symbol, timeframe, session scope, completed-bar timestamp, adjustment and Thinkorswim study settings; compare MCC ATR14 and extension against TOS values on the same bars.
- [ ] Confirm TOS ATR averaging method/initial seed and whether the strategy's ATR is 1M or 5M before choosing the production calculation source. No assumption is accepted.
- [ ] Add matched-fixture tests using user-exported OHLC/TOS values, establish numeric tolerance from observed platform precision, then run time-separated walk-forward validation. A unit test of MCC against itself is not parity evidence.

### Next operator action
1. Check Alpaca market-data plan and activate SIP entitlement.
2. Once active, configure SIP locally, restart API, and share sanitized `/api/providers/status` plus one SPY recent-bars response.
3. Export one same-timestamp Thinkorswim matched-bar fixture with OHLC, ATR14, extension, timeframe/session settings and study versions. This is required to finish ATR parity; no local code change can manufacture the platform's reference output.
4. After those two external prerequisites, implement and validate the integrated sell-limit endpoint and walk-forward results.


 
## 2026-11-04 — Follow-up execution attempt and Thinkorswim ATR specification

### Direct runtime access / data retrieval
- [x] Inspected the committed runtime configuration: `src/MomentumCommandCenterV2.API/appsettings.json` sets `MarketData:Alpaca:Feed=sip`. This is repository configuration only, not proof of the user's effective running configuration; environment variables, user secrets, and launch profile can override it.
- [x] Inspected `GET /api/providers/status`: it reports the running adapter's configured feed and configuration-provider source.
- [x] Inspected the existing MCC-native exact Overnight recovery action: `POST /api/boring-overnight/capture-history`, called by the UI button `overnightCapture20`. It fetches Alpaca 1-minute bars, generates the latest 20 calendar close/open boundaries, persists observations, and audits each exact boundary.
- [x] Inspected the existing GTC_EXT capture action: `POST /api/boring-overnight/capture-sell-window-history`, called by `overnightCaptureSellWindow`. The UI first checks effective feed and refuses to start unless it is SIP.
- [ ] **Not executed in this session:** this agent has no route to the user's `localhost:53360`. A local connection attempt from the isolated execution environment failed because no API listener is exposed there. Therefore current effective runtime feed, EVR exact opens, and persisted exact timestamp coverage remain unverified. Data not found in sources for a fresh runtime result.
- [ ] User must click `overnightCapture20` with EVR selected in MCC; then inspect all 20 exact boundary rows and persisted-bar audit. If the effective feed is IEX, the action may fetch IEX but cannot qualify consolidated SIP evidence; if configured SIP without entitlement it will fail with Alpaca 403. Do not label this retrieval successful until the API response reports exact boundaries and the persisted store confirms each timestamp.
- [ ] Add an MCC-native persisted-observation verification action that directly compares requested expected timestamps to the latest stored observations and reports present/missing, provider, feed, timeframe, content hash and source timestamp. Current capture response reports boundary coverage but no independent second readback acceptance report.

### Thinkorswim ATR specification — source-confirmed defaults, instance evidence still pending
- [x] Official thinkorswim ATR documentation specifies default length 14 and default average type Wilder's; length and average type are configurable: https://toslc.thinkorswim.com/center/reference/Tech-Indicators/studies-library/A-B/ATR
- [x] Official thinkScript `WildersAverage` documentation specifies first value = simple moving average, followed by recursive Wilder smoothing with alpha = 1/length: https://toslc.thinkorswim.com/center/reference/thinkScript/Functions/Tech-Analysis/WildersAverage
- [x] Official prefetch documentation states WildersAverage uses 7 * length bars of prefetch for range-independent results: https://toslc.thinkorswim.com/center/reference/thinkScript/tutorials/Advanced/Chapter-12---Past-Offset-and-Prefetch
- [ ] Verify the actual installed user's ATR study settings, aggregation/timeframe, extended-hours setting, price adjustment and chart data source. The built-in default is known; user overrides and the strategy's selected timeframe are not.
- [ ] MCC parity implementation must use sufficient warm-up history consistent with thinkScript prefetch (7*length bars), then compare on identical completed bars. Existing MCC ATR code seeds on the first supplied 14 true ranges; it can be range-dependent when the supplied history is too short. Do not claim parity solely because the recurrence matches.
- [ ] Obtain at least one matched Thinkorswim fixture (symbol, timeframe, exact bar timestamp, session, adjustment, OHLC, ATR14 and ExtensionATR) and compare multiple consecutive bars.
- [ ] Establish numeric tolerance from the actual displayed/export precision and observed parity differences; no tolerance is approved yet.
- [ ] Run chronological walk-forward target testing only after the historical capture is extended to enough sessions to provide an independent prior 20-window training sample plus subsequent test windows. Current 20-window audit alone cannot produce a meaningful 20-session rolling out-of-sample series.
- [ ] Record target touch, adverse excursion and data exclusions separately. A bar high touching the theoretical limit is not a fill.
- [ ] Actual fills must come from broker execution/order activity or a separately imported, source-identified execution ledger (order ID, client order ID, symbol, side, quantity, fill price, fill timestamp, fees, status). Do not derive fills from OHLCV touches. No verified broker-fill integration is established by the current source audit.
 
### Subscription-cost decision
- [x] Recorded that current Alpaca public docs list Basic as free with IEX and Algo Trader Plus at $99/month with all-US-exchange SIP coverage. Recent SIP requests require eligible subscription.
- [x] No verified public employee code, promotional code, or free trial was found in the sources checked. Alpaca Elite advertises free Algo Trader Plus market data but has a $100,000 deposit requirement; not recommended solely to avoid the $99 development fee.
- [ ] Keep development on IEX where technically appropriate; schedule paid SIP activation only for consolidated-feed acceptance and exact historical recovery. Never mark IEX as equivalent to SIP.


## 2026-11-05 — Calculated Limit Sell focus and EVR evidence

### Operator evidence reconciled
- [x] Confirmed the UI action **Fetch / retry 20-session close/open history** is the existing 20-pair capture workflow (POST /api/boring-overnight/capture-history); the earlier label “Capture 20-session Overnight history” was descriptive, not the visible button text.
- [x] Reviewed the 2026-11-05 result: 11/20 valid pairs, 9 missing next-session opens, provider exact-boundary coverage 31/40, persisted coverage 31/40, 3,759 bars fetched, 0 newly stored, effective Alpaca feed IEX. No 20-session rolling metrics are valid. Changing From/To is not indicated for a retry of the same rolling-window request; use a custom range only for a separately scoped diagnostic.
- [x] Confirmed the operator's appsettings.json and secrets.json both intentionally specify IEX. Runtime result agrees; do not describe this as an unknown secrets override. GTC_EXT SIP capture correctly refuses to run under effective IEX.
- [x] Parsed the EVR Thinkorswim strategy reports: 1M export contains 365 encoded MCC_OHLCV bar records with 21 gaps between adjacent records (not a continuous 1M tape); 5M export contains 79 distinct, contiguous 5-minute regular-session bars. These exports contain OHLCV encoded in strategy names and execution/P&L rows, but no Thinkorswim ATR14 output, ATR study settings, or independent ATR value. They therefore support a limited OHLCV inspection, not ATR parity certification.

### Calculated Limit Sell — current implementation status
- [x] Existing BoringOvernightSellLimitCalculator computes a nearest-rank quantile of each complete session's maximum eligible 07:00–19:59 ET price relative to its exact prior-session close. It handles flat/adverse samples without mislabelling them as missing data and refuses numeric output unless ATR parity status is MATCH.
- [x] Existing capture/audit APIs persist and audit source/feed-isolated 1-minute GTC_EXT observations, requiring all 780 exact minute timestamps and the exact previous regular-session close for each of 20 sessions. IEX is diagnostic only; SIP is required for consolidated target research.
- [x] **Fail-closed orchestration is implemented:** API/UI bind persisted 20-window history, exact reference close, corrected-study ATR parity and same-source 5m bars into a provenance result. The numeric calculator remains gated by the unimplemented walk-forward validation.
- [x] Added fail-closed `POST /api/boring-overnight/sell-limit` (supersedes the earlier proposed route name). It reads persisted bars, checks 20/20 history, exact reference close, ATR(14) Wilder V2.1 parity, 99 consecutive 5m bars and provider/TOS OHLCV equality. It does not accept a caller-provided MATCH flag. The walk-forward gate currently blocks calculator invocation.
- [x] Added Dashboard assessment UI for status, DATA? limit, reference close, ATR parity, target quantile and per-gate source/failure explanations. Walk-forward metrics are not yet available; no order placement or automatic sell.
- [ ] Capture more than the initial 20 windows before out-of-sample validation. Run chronological walk-forward: train on prior windows, evaluate only subsequent sessions; report target touch, time-to-touch and adverse excursion separately. In-sample quantile touch rate is not a fill probability.
- [ ] Actual fills must be joined from broker execution records with order ID, client ID, side, quantity, fill price/time, fees and status. OHLCV high touching a target is not a fill.

### EVR ATR parity data gap
- [ ] Obtain a Thinkorswim export containing the actual ATR(14) study output for the same 5-minute bars, including study name, inputs/average type, extended-hours setting, adjustment mode, timestamp and displayed precision. The supplied 5M StrategyReport is OHLCV-only and contains 79 bars; it is shorter than the 98-bar Thinkorswim Wilder prefetch context previously documented, so it cannot certify parity.
- [ ] Obtain at least 99 consecutive matching 5M OHLCV bars (98 prefetch bars plus the comparison bar) from the same feed/session/adjustment context and compare several consecutive completed bars, not a single rounded value. The 1M report's 365 encoded bars are sparse and are not a continuous ATR input series.

### Next operator actions
1. Keep the configured feed as IEX for now; do not expect GTC_EXT qualification from it. No paid-feed purchase or configuration change is authorized by these results.
2. Use **Fetch / retry 20-session close/open history** without changing From/To for a repeat of the same rolling request. For diagnosis only, record exact missing timestamp offsets from the boundary audit; do not accept nearest-minute substitutions.
3. For the sell-limit pipeline, first capture complete 20-session GTC_EXT history on an entitled SIP feed. Until then the calculated limit remains DATA? / NOT CALCULATED.
4. Export the actual Thinkorswim ATR14 value series and matching continuous 5M bars as specified above. The two supplied StrategyReports are not ATR-value exports.
5. After pulling any new code, run .\scripts\Get-Latest-And-Test.ps1 from a clean checkout on feature/opportunity-quality-classification, then restart MCC and verify the runtime API. PowerShell remains an accepted operator workflow.

[Uncertainties]
- The exact reason for the nine missing 09:30 open bars is not established by the summary; use the per-boundary provider/store nearest-offset audit to distinguish provider absence from storage mismatch.
- No complete 20-session GTC_EXT SIP dataset, current runtime SIP entitlement, Thinkorswim ATR14 values, or broker execution records were supplied.


## 2026-11-05 — EVR V2 ATR exports and Calculated Limit Sell readiness

### Verified from newly uploaded Strategy Reports
- [x] Audited `data/StrategyReports_EVR_11032026_5M_ATR.csv`: 81 V2 payloads; every row has the documented symbol/timeframe/epoch/OHLCV/volume/ATR14 shape; one 10-minute timestamp step, all other adjacent steps 5 minutes; ATR values 0.4962–2.0107.
- [x] Audited `data/StrategyReports_EVR_11042026_5M_ATR.csv`: 79 V2 payloads; all adjacent timestamps are 5 minutes; ATR values 0.7834–2.5238.
- [x] Recorded operator chart settings: extended-hours ON; corporate-action adjustment ON; chart timezone Phoenix local UTC-07:00; ATR length 14; Wilder average type. V2 script default is atrLength=14 and Wilder averaging.
- [ ] Verify at least 20 consecutive rows against Thinkorswim chart ATR plot and OHLCV; exported values alone do not establish parity.
- [ ] Resolve whether the 11/03 10-minute timestamp step is a true missing 5m bar or report-export omission. Never synthesize it.
- [x] Added V2 ATR parser/storage and tests against both full exports while retaining V1 reader compatibility.
- [x] Implemented server-calculated Wilder ATR series and timestamp-matched TOS parity audit; server derives status from stored observations, never from a caller flag.
- [x] Implemented fail-closed Calculated Limit Sell orchestration API/UI with provenance and per-gate reason codes. Numeric output remains blocked pending walk-forward validation.
- [ ] Obtain 20/20 complete GTC_EXT SIP windows (780 exact 1m bar starts/session plus exact prior close); current effective IEX feed is not qualification-usable. Verify SIP entitlement/config before changing runtime feed.
- [ ] Verify exact current reference close and adequate same-source 5m OHLCV warmup/anchor.
- [ ] Complete chronological walk-forward validation and integrate actual broker execution records separately.

**Readiness decision:** The 160 V2 rows across two dates are sufficient to start and test ATR parser/parity engineering. They are not sufficient to certify a numeric sell limit or complete end-to-end validation. Numeric output remains DATA? / NOT VERIFIED until every source, coverage, reference-close and parity gate passes. See `docs/Calculated-Limit-Sell-Implementation.md`.


## 2026-11-05 — ATR V2 aggregation selector and runtime feed confirmation
- [x] User confirmed extended-hours ON, corporate-action adjustment ON, Phoenix local timezone, ATR length 14 and Wilder average; original V2 compiles in Thinkorswim.
- [x] User supplied provider status: Alpaca configured/validated with effective IEX feed; appsettings and secrets both specify IEX; last validation returned 34 bars; Massive is not configured.
- [x] Added `chartAggregation` selector to ATR V2 study for 1m, 5m, 15m, 30m, 1h and 4h; timeframe label/payload are dynamic and export is blocked when selected input does not match actual chart aggregation.
- [ ] Compile revised study in Thinkorswim and validate each desired aggregation. ThinkScript cannot change host chart aggregation; set chart control and study input to the same value.
- [ ] Re-export and verify at least 20 consecutive rows at 5m; resolve the Nov 3 missing 5m interval.
- [ ] Keep IEX configuration unchanged until SIP entitlement is verified. GTC_EXT SIP qualification remains blocked; no SIP capture should be attempted against effective IEX.
- [ ] Implement V2 parser/tests, separate ATR persistence, server-side ATR calculation/parity, sell-limit orchestration API/UI, SIP history qualification, walk-forward and actual execution reconciliation.
