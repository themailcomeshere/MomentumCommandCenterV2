## 2026-11-05 Arizona — Wilder ATR parity seed correction

- [x] Investigated the imported EVR 1m API result (`NOT_MATCH`, mean 0.000029, max 0.000061). The endpoint was trimming the source to the latest 119 bars before calculating Wilder ATR. That re-seeded the recursive ATR at the start of the 119-bar window rather than carrying the full chart-history seed forward.
- [x] Reproduced the exact API values from the committed 346-row EVR 1m V2.1 export: full-history calculation compared on the latest 20 gives mean absolute error 0.000026034 / max 0.000046686; a fresh seed from only the latest 119 gives 0.000028985 / 0.000060665, matching the operator's reported 0.000029 / 0.000061 after rounding.
- [x] Updated `ThinkorswimAtrParityService` to calculate Wilder ATR from all imported observations while applying the requested 119-bar (99 warm-up + 20 comparison) window only to the comparison slice and timestamp-gap qualification.
- [x] Updated `/api/strategy-report/atr-parity` to pass full history to the service. Timestamp gaps are counted only inside the latest requested comparison window; missing bars remain un-imputed.
- [x] Bumped parity implementation version to `MCC-WILDER-ATR-PARITY-1.1` and added a regression test proving the full-history seed is used while only the latest 20 bars are compared.
- [ ] Local API/browser re-test is still required. Expected diagnostic for the imported EVR 1m sample, if the stored series is unchanged: numeric values within tolerance but 13 gaps in latest 119, therefore `GAPS_PRESENT`, not `MATCH`. EVR 5m remains `INSUFFICIENT_WARMUP` with 81/82 observations.

[Uncertainties]
The correction is committed but CI and local API response have not yet been verified. The 13 gaps in the 1m comparison window still prevent a parity MATCH, even if numeric ATR errors are within rounding tolerance.

---

## 2026-11-05 Arizona — Main Dashboard promotion and ATR import verification

### Main Dashboard is now authoritative
- [x] Migrated the approved PrototypeIndex UI into `frontend/index.html`: fixed left symbol/workspace dock, Intraday Momentum ↔ Boring Overnight vertical switch, responsive top bar/readiness layout, integrated opportunity classification card, completed 1M close timestamp, Daily VWAP, ATR14, RVOL and ATR-extension metrics.
- [x] Preserved main-index-only features during the migration, including all five StrategyReport ATR provenance controls, calculated sell-limit evidence controls, current Quick Start content, current Morning Brief, and the cache-busted `app.js?v=20261105-atr-import-control-recovery-v3` reference.
- [x] Added the workspace-toggle bridge so the vertical switch drives the existing `#mode` state. The symbol field continues to drive the existing `#symbol` state.
- [x] Removed duplicate static element IDs introduced by the prototype chart metric markup.
- [x] Removed `frontend/prototypeIndex.html`. Do not test or develop against a second HTML entry point. `frontend/index.html` is the only Dashboard page for all future UI work and acceptance testing.

### Operator-confirmed ATR import results
- [x] EVR 1m Nov 3 V2.1: 346 observations appended; 48 timestamp gaps overall; parity `NOT_MATCH`; 20 matched timestamps; mean absolute error 0.000029; max absolute error 0.000061. The max exceeds the 0.00005 tolerance, so 1m parity is not certified.
- [x] EVR 5m Nov 3 V2.1: 81 observations appended; one 5-minute gap; `INSUFFICIENT_WARMUP` because 99 warm-up observations are required before the 20-bar comparison.
- [x] EVR 5m Nov 4 V2.2: 82 observations recognized as identical; 0 appended, 82 identical skipped, 0 corrections. This is correct idempotent import behavior, not a missing import. It remains below the 119 observations required for the 99+20 validation window.
- [x] Gaps remain explicit; no bars were synthesized. Numeric sell-limit output remains withheld.

### Next work / operator acceptance
1. Pull `feature/opportunity-quality-classification`, run `.\\scripts\\Get-Latest-And-Test.ps1`, restart the API, then use only the root Dashboard page (`/`, served from `frontend/index.html`).
2. Confirm the left dock, vertical workspace switch, editable focused-symbol field, redesigned readiness header, and six opportunity metrics render. Test both workspaces and Reset.
3. Confirm all five ATR import controls remain visible and the two previously imported Nov 3 reports remain persisted; do not re-import unless deliberately testing idempotency.
4. Export at least 119 consecutive 5m V2.1 observations for EVR (99 warm-up + 20 comparisons), with no intraday gaps in the latest parity window. Inspect embedded payload epochs, including any after-hours bars.
5. Investigate the 1m parity error and source gaps. Do not relax the 0.00005 tolerance or interpolate missing bars to force a match.
6. Continue provider/TOS OHLCV parity, full-session coverage, 20/20 SIP GTC_EXT qualification (only after entitlement changes), walk-forward runtime validation, and actual broker-fill reconciliation.

[Uncertainties]
- The new main-index layout is committed but still requires browser/runtime acceptance on the operator's local API host.
- The 1m parity discrepancy's cause is not diagnosed by this import response; provider-feed differences are not tested by the current TOS-import parity comparison.
- The cause of the 1m and 5m source timestamp gaps is unknown. No interpolation is permitted.

---

# Handoff update — November 5, 2026: ATR control recovery and V2.2

- The current source HTML has all five ATR metadata controls, but the operator's running page reports all five missing. The exact static-file/cache cause is not proven.
- New defensive bootstrap in `frontend/app.js` dynamically recreates missing controls before the submit handler runs. Defaults match the operator's confirmed study settings: ATR 14 / Wilder / America-Phoenix / ETH ON / corporate-action adjustment ON. Existing controls are preserved. Cache key bumped to `20261105-atr-import-control-recovery-v3`.
- New report `StrategyReports_EVR_11042026_5M_ATR_v2_2.csv`: 82 source observations from 09:05 ET to 16:00 ET; one 15-minute gap 09:15–09:30 ET; one source timestamp at 16:00 ET, none later. This does not establish sustained after-hours coverage.
- 82 bars are below the 119 required for a 99-bar warm-up plus 20 ATR comparisons. No 5m parity certification yet.
- Next: pull branch, run local test script, restart API, hard-refresh, confirm controls appear, retry both Nov 3 imports, export a new 5m report, inspect embedded source epochs for bars after 16:00 ET.
- Do not retry SIP while prior HTTP 403 entitlement failure remains.
- Walk-forward is 20 train + 20 unseen test; theoretical OHLCV touches are not broker fills.


---

# Handoff update — November 5, 2026 (StrategyReport import, after-hours window, walk-forward)

**Branch:** `feature/opportunity-quality-classification`

## Implemented
- Dashboard ATR import handler now validates all required ATR metadata DOM controls before dereferencing them; missing controls are named in the visible error. Dashboard HTML cache-bust key updated to load the guarded app.js.
- New November 4 EVR 5m V2.1 report contains 82 payloads from 09:05–16:00 Eastern (07:05–14:00 Arizona), with one 15-minute gap from 09:15–09:30 ET. No payload after 16:00 ET.
- Root cause for the 16:00 ET cutoff: ThinkScript study filtered source bars using `startTime=0600`, `endTime=1600`. Extended-hours chart setting ON cannot override the script's time filter.
- ThinkScript defaults changed to `startTime=0700`, `endTime=2000`; official `SecondsFromTime` docs specify EST/Eastern clock, not Phoenix chart display time. User must reload/compile and export again to verify after-hours inclusion.
- Implemented walk-forward validator: 20 chronological training sessions + 20 later unseen test sessions. Target quantile is selected from training only; test touch rate and adverse excursion are measured separately. Test touch rate must meet training empirical touch rate; max test adverse excursion must not exceed training adverse-excursion P95; 100% session coverage required.
- Expanded GTC_EXT history service to 40 sessions, capturing lowest and highest eligible prices. Sell-limit calculator receives only the 20 training sessions. Orchestrator adds hard `WALK_FORWARD_VALIDATION` gate.
- Added tests for pass, target-touch failure, adverse-excursion failure, insufficient sample and incomplete coverage.
- Added `docs/Validation/Walk-Forward-Validation-Design-20261105.md`.

## Operator verification still required
1. Pull branch and run `.scripts\Get-Latest-And-Test.ps1`. Confirm C# build/tests and frontend JS validation pass.
2. Ctrl+F5 Dashboard. Re-import Nov 3 1m and 5m V2.1 files. Record full import response, persisted counts, parity status.
3. Pull updated ThinkScript, set chartAggregation and actual chart to 5m, confirm ETH ON / corporate-action adjustment ON, compile, export new report.
4. Confirm new payload has bars after 16:00 ET. If gaps persist, send the full new StrategyReport; do not interpolate.
5. Do not retry SIP while prior entitlement 403 remains unchanged.
6. A passing walk-forward gate is not sufficient by itself: SIP GTC_EXT, exact close, ATR parity, continuous 99-bar warm-up and exact provider/TOS 5m OHLCV gates remain mandatory.

## Key policy
Walk-forward v1.0 details and formulas are in `docs/Validation/Walk-Forward-Validation-Design-20261105.md`. OHLCV high/low touches are theoretical observations, not broker fills. Actual fills require a separate broker execution ledger.

[Uncertainties]
CI and local runtime acceptance for the newly committed validator/import guard are not yet verified. The Nov 4 report's missing after-hours payload is explained by the old 16:00 ET study filter; actual post-close bar availability must be checked after the revised study is loaded.

---

# November 5 — ATR V2 ThinkScript compile-error correction

- User reported `AggregationPeriod.ONE_MIN` unknown, `Expected double`, and incompatible string parameters.
- Root causes: ThinkScript's one-minute aggregation enum is `AggregationPeriod.MIN`; `def` is numeric and cannot hold string labels.
- Corrected `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`: replaced `ONE_MIN` with `MIN`; removed string-valued `def timeframeLabel`; embedded the conditional timeframe string directly in label and order-name expressions.
- Commit `39cacef3a68f88e2e3fc33467d260ac00f175b37` (subsequent Master To-Do documentation commit `56188b80ec61210a0ee5f94cef6b9712544ebba3`).
- GitHub Actions `build-and-test` for the ThinkScript/documentation commit was still in progress at last check. .NET CI cannot compile ThinkScript.
- **Not yet done:** Thinkorswim compile/export validation; V2 parser/tests; separate ATR persistence; production Wilder ATR seed parity; matched-bar parity service; sell-limit orchestration API/UI; SIP entitlement/history; walk-forward validation; broker-fill reconciliation.
- Existing V1 parser and research calculators are not completion of those deliverables. Full status is tracked in `docs/Master-To-Do.md`.

## Immediate operator validation
1. Pull the feature branch and open the corrected `MCC_OHLCV_ATR_V2.txt`.
2. Compile in Thinkorswim. If it still errors, send the complete new compiler output; do not edit the study manually.
3. On EVR 5m, set `chartAggregation=FIVE_MIN`, confirm chart aggregation is 5m and the label says MATCH.
4. Export a short date range and verify payload contains 11 pipe-delimited fields: marker, version, symbol, timeframe, epoch milliseconds, O, H, L, C, volume, ATR.
5. Do not use the export for a numerical sell limit yet.

# Handoff — November 5, 2026 (updated)

Branch: feature/opportunity-quality-classification

Priority: Calculated Limit Sell. Contract: `docs/Calculated-Limit-Sell-Implementation.md`. Master checklist: `docs/Master-To-Do.md`.

## Operator-confirmed Thinkorswim settings
- Extended-hours ON.
- Corporate-action adjustment ON.
- Chart timezone Phoenix local UTC-07:00.
- ATR length 14, Wilder average.
- User confirms the original V2 study compiles without errors.

## V2 aggregation selector change
- Updated `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`.
- Added `chartAggregation` input and support for 1m, 5m, 15m, 30m, 1h, 4h.
- Payload timeframe is dynamic; mismatch between selected input and actual chart aggregation blocks export and is shown in labels.
- ThinkScript cannot change the host chart aggregation. Set both the study input and chart aggregation control to the same period. No source edit is needed to switch among supported periods.
- This revision still needs Thinkorswim compilation and validation at each selected aggregation.

## Provider status
User supplied `/api/providers/status`: Alpaca configured/validated, effective feed IEX, both appsettings and secrets specify IEX; Massive not configured. 34 bars in last successful validation. This does not establish SIP entitlement. Do not change feed blindly; GTC_EXT qualification remains blocked pending SIP entitlement/effective feed verification.

## ATR exports and calculated sell limit
- Nov 3 export: 81 records, one missing 5m timestamp interval; ATR14 present on every row.
- Nov 4 export: 79 contiguous 5m records; ATR14 present on every row.
- 160 records over two dates are enough to start parser/parity implementation, not enough for 20-session sell-limit qualification or 98-bar warmup for every comparison.
- Still missing: V2 reader and tests, separate TOS ATR persistence, server ATR series/parity, orchestration API/UI, 20/20 SIP GTC_EXT sessions, exact reference close, walk-forward tests and actual broker execution linkage.

## Next actions
1. Compile the updated aggregation-selector study in Thinkorswim.
2. Test 5m first, then each additional aggregation you intend to use. Set both input and chart controls identically; confirm the label says MATCH and payload timeframe matches.
3. Re-export EVR 5m and compare at least 20 consecutive rows with chart OHLCV and ATR.
4. Diagnose the missing November 3 5m timestamp.
5. Verify SIP entitlement without changing IEX configuration until confirmed.
6. Continue V2 parser/storage/parity implementation, then orchestration API/UI with fail-closed status.
7. Run `.\\scripts\\Get-Latest-And-Test.ps1`, restart MCC and browser-test after code implementation.

[Uncertainties]
The operator has supplied V2.1 payload exports, confirming the revised study executed. Full ATR parity remains unverified because the latest 1m window has 13 intraday gaps and the 5m report has only 81 observations. Prior recent-history SIP requests returned HTTP 403 due subscription entitlement; do not ask the operator to repeat them. GTC_EXT completeness and broker fills remain unavailable.


# Handoff update — November 5, 2026 (latest V2.1 exports and candidate review)

The operator confirmed the aggregation-selector change works and supplied corrected V2.1 EVR Strategy Reports:
- `data/ScannerExports/StrategyReports_EVR_11032026_1M_ATR_v2_1.csv`: 346 ATR payload observations, 48 intraday gaps overall, 13 intraday gaps in latest 119.
- `data/ScannerExports/StrategyReports_EVR_11032026_5M_ATR_v2_1.csv`: 81 ATR payload observations, one 10-minute jump; insufficient for 99 warm-up + 20 comparison bars.
- The 1m last 20 post-warm-up numeric ATR values have mean absolute error ~0.00002603 and max ~0.00004669 against server Wilder calculation, within 0.00005 export tolerance. The latest 119 rows still contain 13 intraday gaps, so this is not certified MATCH. This was computed from the committed CSVs; Dashboard import/API verification remains to be done.

New code:
- `AtrTimestampGapDetector.cs` now excludes scheduled overnight/weekend date transitions from intraday gap counts in parser and parity service.
- Same-New-York-date missing intervals still block MATCH.
- Full-session completeness (detecting a completely absent trading session) remains an open item.
- Added regression tests for overnight/weekend transitions.
- Latest code commit: `c9003fd24d3b39817a874304195e01619c79de21`; CI [42121843682](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42121843682) succeeded: build succeeded and 225/225 tests passed.

SIP: Prior recent-history SIP requests returned HTTP 403 due subscription entitlement. IEX preflight rejection is a separate known limitation. Do not repeat SIP requests or ask the operator to repeat them; do not run GTC_EXT capture while entitlement remains denied. Continue IEX-only diagnostic development.

Button location:
Dashboard → Workspace dropdown → Boring Overnight → Calculated Limit Sell → Research target quantile (%) → **Assess sell-limit evidence gates**. HTML ID: `overnightCalculateSellLimit`. Do not click the separate GTC_EXT history capture button while SIP is blocked.

Candidate review:
`docs/Validation/Overnight-Candidate-Review-20261105.md` documents AVNT, BLMN, EVER, BLK and CBOE. CBOE is the preferred addition to the next ordinary Overnight review; BLK has the best historical win rate; AVNT is a post-earnings momentum watch; EVER is secondary; BLMN is excluded for the Nov 5→Nov 6 routine overnight because it reports earnings premarket Nov 6. These are research priorities, not READY decisions.

Next operator actions:
1. Pull latest branch and run `.\scripts\Get-Latest-And-Test.ps1` after CI is green.
2. Import the two V2.1 exports through Dashboard → StrategyReport; verify counts and parity states.
3. Export a longer 5m V2.1 history with at least 119 observations, allowing multiple sessions; no intraday gaps within a New York date in the comparison window.
4. Use the sell-limit evidence button only for diagnosis; numeric limit remains blocked by walk-forward validation and SIP GTC_EXT history.
