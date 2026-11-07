# Momentum Command Center — Master To-Do

**Updated:** 2026-11-07 (exact ATR gap slots + catalyst validation + Opportunity Quality pool API)  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261106.md](NewChatHandoff-20261106.md)  
**Detailed active backlog:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)  
**Latest ATR evidence analysis:** [ATR-Export-Coverage-Fix-20261106.md](Validation/ATR-Export-Coverage-Fix-20261106.md)  
**Exact ATR gap/session-slot analysis:** [ATR-Gap-Session-Slot-Analysis-20261107.md](Validation/ATR-Gap-Session-Slot-Analysis-20261107.md)  
**Catalyst and Opportunity Quality validation:** [Catalyst-and-Opportunity-Quality-Validation-20261107.md](Validation/Catalyst-and-Opportunity-Quality-Validation-20261107.md)  
**Latest provider/sell-limit evidence:** [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md)  
**Daily data qualification:** [Daily-Data-Qualification-20261106.md](Validation/Daily-Data-Qualification-20261106.md)  
**Earlier evidence archive:** [ATR-Evidence-Export-Analysis-20261106.md](Validation/ATR-Evidence-Export-Analysis-20261106.md)  
**Browser/stream acceptance:** [Browser-and-Stream-Collector-Acceptance-20261106.md](Validation/Browser-and-Stream-Collector-Acceptance-20261106.md)

This is the sole active execution checklist. Historical repeated checkboxes were moved verbatim to the archive and consolidated into the detailed backlog. Do not use historical unchecked boxes as current task status. Keep this file concise; put technical acceptance criteria in the active backlog and specialized runbooks.

## Engineering continuation — 2026-11-07 (operator exact gaps + feature implementation)

### P0 — ATR continuity and TOS authority/EOD

- [x] Converted the operator's exact gaps to Eastern time and inspected the committed EVR/CBOE Strategy Report exports. EVR: 00:10→09:05 ET and 09:05→09:30 ET; CBOE: 16:00→18:40 ET. No new export/import is justified.
- [x] Corrected ATR gap diagnostics to count only expected 5-minute timestamp slots inside the 04:00–20:00 ET chart session. The first EVR gap is 61 expected chart-session slots, not 106 wall-clock slots; the second is 4; CBOE is 31. Added first/last expected missing UTC timestamps.
- [x] Added regression tests for all three real intervals and the one-minute synthetic gap. CI passed on code/test commit `028796bb4915ab8b118f7f5550eb8acd425aa250`: Release build, 0 warnings/errors, 239/239 tests.
- [x] Source audit: 1M V14.5.2 reconstructs 5M permission with 5M close above Daily VWAP, 5M close above EMA9, EMA9 >= EMA20 and RSI; separate 1M VWAP execution gate remains. Both 1M V14.5.2 and 5M MainStudy use 17:55 ET in standard time / 18:55 ET in daylight time for 15:55 Arizona.
- [ ] Inspect actual TOS chart candle presence at EVR 04:00–09:00 ET, EVR 09:10–09:25 ET and CBOE 16:05–18:35 ET. The export alone cannot distinguish no-trade/no-bar from omitted export rows.
- [ ] Compile both studies in TOS and run matched completed-bar permission/EOD checks on both sides of DST. Source inspection and GitHub CI are not ThinkScript compilation or runtime parity.

### P1 — Overnight boundary and Boring Overnight

- [x] API/history output now labels the 09:30 reference as `ONE_MINUTE_BAR_OPEN_PROXY` and `officialOpeningAuctionVerified=false`.
- [x] Documentation now states the previous RTH close → next RTH 09:30 bar-open net return spans Day 1 after-hours and Day 2 premarket, but is endpoint-only and does not reconstruct the path/extrema.
- [ ] Verify the official auction print versus first eligible observed trade. Current IEX evidence cannot certify consolidated/auction coverage; SIP remains parked under the known HTTP 403 blocker.
- [ ] Resolve the precise historical Overnight buy/sell endpoints against the existing TOS scanner formula/source before promoting the close/open proxy as authoritative.
- [ ] Implement the separate V3 Boring Overnight watchlist column only after endpoint/session semantics and source-backed metric fields are frozen. The data contract remains the specification; no guessed proxy column should be promoted.

### P1 — Catalyst validation

- [x] Human review records now persist the exact triage severity/categories present at review time.
- [x] Added chronological holdout validation endpoint: `GET /api/market-data/catalyst-reviews/validation`. It excludes uncertain/duplicate/legacy-predictionless/conflicting records and reports confusion matrix, precision, recall, specificity and accuracy.
- [x] Added frontend capture of prediction fields and displays validation status after a review is saved.
- [ ] Collect human-adjudicated examples across both positive and negative classes; inspect the endpoint until the later time-based holdout contains both. No precision/recall evidence is available yet. Unknown news remains UNKNOWN and never CLEAR.

### P1 — Opportunity Quality full-pool ranking

- [x] Added deterministic diagnostic pool ordering and tests.
- [x] Added `GET /api/market-data/opportunity-quality/rank?symbols=...` for 1–50 unique explicit symbols with assessment concurrency capped at four. All supplied candidates are returned; DATA? and BLOCKED sort after diagnostic quality classes.
- [x] Wired the endpoint into the main Dashboard with a **Rank quality pool · top 50** control. It uses the top 50 imported scanner-score candidates as a disclosed prefilter; it does not change Red Zone's four-card selection. Current API ranking is not market-wide discovery and is not a calibrated numeric score.
- [ ] Calibrate with time-separated candidate snapshots/outcomes before changing provisional thresholds or calling the rank operational.

- [x] Opportunity Quality pool API + main Dashboard top-50 control passed GitHub Actions on `69c1b39562b62dc30e4a43b1a6227438120c31b2`: frontend JavaScript syntax PASS, Release build 0 warnings/errors, 243/243 tests PASS. This is CI verification, not operator browser acceptance.

### P1 — Full Dashboard/browser and regular-session collector

- [x] Edge executable discovery/launch handling fix is committed and its code CI passed on the earlier code commit.
- [ ] Pull and execute the corrected browser diagnostics on the operator machine; save screenshot/CDP evidence and inspect errors.
- [ ] Run the stream/collector capture during a confirmed regular session; Friday evening attempts were correctly rejected by the session guard.

### P2 — Walk-forward

- [x] Validator implementation exists.
- [ ] Runtime-qualified 40-session history and walk-forward remain parked behind the known SIP HTTP 403 entitlement blocker. Do not retry SIP or request another entitlement check unless the operator independently reports changed access.

**Detailed exact-gap analysis:** [ATR-Gap-Session-Slot-Analysis-20261107.md](Validation/ATR-Gap-Session-Slot-Analysis-20261107.md).

## Latest operator evidence — browser/stream attempts and ATR parity (2026-11-06 evening)

- [x] Recorded the browser diagnostics failure: the script itself was found and executed, but `Start-Process` could not locate/launch the resolved Edge executable. This is an executable-discovery/launch-path problem, not a missing diagnostics-script path.
- [x] Hardened `Capture-DashboardBrowserDiagnostics.ps1`: validates an explicitly supplied browser path as an existing `msedge.exe` file; searches standard Edge install roots, Windows App Paths registry entries and all `Get-Command msedge.exe` results; resolves to an absolute path; prints the exact executable; and returns a specific launch error if Start-Process fails.
- [ ] Re-run browser diagnostics after pulling this fix. No screenshot/CDP evidence has been captured yet; browser acceptance remains open.
- [x] Recorded both stream collector attempts as correctly blocked by the regular-session guard: operator time 2026-11-06 18:52 ET was outside 09:30–16:00 ET. This is expected behavior, not a collector test pass or a script defect.
- [ ] Run the 10-minute stream/collector capture during a confirmed regular session. Next weekday opportunity: Monday 2026-11-09, 09:30 ET / 07:30 Arizona, subject to actual exchange session status. Do not run during holidays or after an early close.
- [x] Re-read local ATR parity after the session-aware gap-detector deployment, using existing persisted observations only. EVR 5m: `GAPS_PRESENT`, 327 observations, 20 compared bars, 2 timestamp gaps, MAE 0.0000294330, max error 0.0000495349. CBOE 5m: `GAPS_PRESENT`, 338 observations, 20 compared bars, 1 timestamp gap, MAE 0.0000203866, max error 0.0000426076. Both numeric comparisons remain within 0.00005; neither is MATCH because continuity is unresolved.
- [x] Confirmed revised gap detector removed one EVR gap relative to the earlier 3-gap response. Remaining gaps: EVR 2, CBOE 1. Do not impute bars or re-import unchanged CSVs.
- [ ] Inspect the exact remaining source timestamps against the TOS chart and declared 04:00–20:00 ET chart-session contract; determine whether these are omitted chart bars or legitimate no-eligible-print intervals. Compile the revised ThinkScript in TOS; it is not yet verified.

## ATR diagnostic improvement — 2026-11-06 evening

- [x] Added exact timestamp-gap details to the ATR parity response: previous bar UTC, current observed bar UTC, expected next bar UTC, first observed bar after the gap, estimated missing interval count and gap duration.
- [x] Added a regression assertion that verifies exact gap timestamps and missing-interval count for a synthetic 1m gap. The existing fail-closed `GAPS_PRESENT` status is unchanged.
- [x] GitHub Actions passed the browser executable-discovery fix on `50945206dd3ac207ab96efbcb9a42911cc13d335` and the ATR gap-detail implementation/tests on `5bc2ac57f5f83e00f77930e7ae13374d556828a4`: Release build, 0 warnings/errors, 236/236 tests. The subsequent documentation-only head's CI was still running at the time of this update.
- [ ] After pulling/building/restarting the API, re-read EVR/CBOE parity and inspect `timestampGaps` in the JSON response. Use those exact UTC intervals to inspect the TOS chart; do not infer that a missing interval was a trade or a bar.

## Verified at this checkpoint

- [x] Operator reports Release build and tests PASS at local source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- [x] Operator restarted API and re-ran persisted ATR parity for EVR 1m, EVR 5m and CBOE 5m.
- [x] API health is `OK`; mode is `decision-support-only`; order submission is `NOT IMPLEMENTED`.
- [x] Alpaca is configured/validated on IEX. The latest captured provider-status evidence reported 8 validation bars. Massive is not configured.
- [x] Earlier repeat imports were idempotent (zero new observations/corrections); the newer 2026-11-06 broad exports subsequently appended 2 EVR and 9 CBOE observations. Do not re-import identical files solely to re-run parity.
- [x] ATR parity version `MCC-WILDER-ATR-PARITY-1.1` and full-history Wilder seed correction are present and running in the restarted local API.
- [x] Walk-forward validator and hard orchestration gate are implemented. Runtime pass remains unproven.
- [x] Main Dashboard is `frontend/index.html`. Do not use or recreate PrototypeIndex.
- [x] Supporting documentation, legacy handoffs and audit files were reconciled; a repeatable ATR evidence-export script was added.
- [x] Operator successfully ran the exporter; ZIP captured health/provider status, all four parity endpoints, persisted TOS observations, manifest and SHA-256 hashes at source SHA `9dd2c224d8f1373c7935eb3f94ed5fddea837e5f`.
- [x] Latest persisted EVR/CBOE 5m observations were independently rechecked across 663 adjacent pairs; maximum Wilder recurrence residual is 0.0000928571 for both, consistent with four-decimal rounding. No large recurrence discontinuity remains in this import.
- [x] Corrected a gap-detector false positive for same-date 00:00–00:10 ET intervals wholly inside the scheduled 20:00–04:00 ET closure; added tests for overnight closure, premarket-overlap, and after-hours gaps. GitHub Actions passed Release build and 236/236 tests on `151cb97976121600833c8927bddf4cf175567b7b`; local API parity re-read is pending.
- [x] Identified and corrected the V2.1 study's export predicate: ATR was calculated on all chart bars while the Strategy Report omitted bars outside 07:00–19:59 ET. The study source now exports every available chart bar in the selected date range.
- [x] GitHub Actions `build-and-test` passed on commit `d00dbd61ede861f6a9924165377a17e9d8001022`, including the original qualification runner/study source state.
- [x] Latest ATR session-boundary detector and regression tests passed GitHub Actions on `151cb97976121600833c8927bddf4cf175567b7b`: Release build, 0 warnings/errors, 236/236 tests. It does not validate subsequent runner edits, Thinkorswim compilation, or local PowerShell execution.
- [x] Reviewed operator evidence ZIP `20261106T105332.zip`: API healthy; effective Alpaca feed IEX; no SIP request or 40-session capture occurred.
- [x] Latest imports persisted 327 EVR and 338 CBOE 5m observations; 2 EVR and 9 CBOE rows appended, with no corrections.
- [x] Latest EVR/CBOE 5m numeric ATR errors are within 0.00005, but parity remains `GAPS_PRESENT` (3 EVR and 1 CBOE gap in the 119-observation qualification window).
- [x] Analyzed row-level IEX/TOS comparison: 0/119 exact OHLCV rows for each symbol; IEX returned 104 EVR and 109 CBOE bars. These results are diagnostic only, not SIP qualification.
- [x] Updated qualification runner to report comparison mode, configured/response/row-level feed identity, and per-field mismatch counts. This runner edit has not yet been executed locally.
- [x] Added [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md) with import provenance, parity, IEX comparison and gate interpretation.

## Current blockers — P0

- [ ] Compile the revised V2.1 ThinkScript in Thinkorswim. Inspect the already committed EVR/CBOE exports and persisted rows first; only create a genuinely new export if a specific omission is proven after compilation. TOS compilation/run is not yet evidenced.
- [ ] Re-read EVR 5m parity after the session-aware gap-detector code is built and restarted. Expected to remove one known 00:00–00:10 ET scheduled-closure false positive; premarket-overlapping gaps remain unresolved. Prior MAE 0.0000294330 / max 0.0000495349 were within tolerance.
- [ ] Re-read CBOE 5m parity after the session-aware gap-detector code is built and restarted. The 16:00–18:40 ET after-hours gap remains; prior MAE 0.0000203866 / max 0.0000426076 were within tolerance.
- [ ] Investigate remaining EVR/CBOE gaps against actual TOS chart/source bars and the 04:00–20:00 ET ETH session contract. Do not re-upload/re-import the existing CSVs; only request a new corrected export if a specific omission is demonstrated after compiling the revised study. Never impute.
- [ ] Resolve EVR 1m's 13 gaps in its prior 119-observation window or retain `GAPS_PRESENT`; 1m does not replace the independent 5m gate.
- [ ] Prove exact provider/TOS 5m OHLCV equality over all 119 timestamps and capture the exact previous RTH close from the same provider/feed. Latest IEX diagnostic returned 0/119 exact rows for EVR and CBOE; it does not establish SIP behavior.
- [x] Confirmed repeated recent-SIP HTTP 403 entitlement denial; current effective feed remains IEX. Park SIP-dependent work. Do not ask the operator to retry SIP, reconfigure it, or repeat SIP smoke/capture requests. Resume only if the operator independently reports an actual entitlement change.
- [ ] 40-session GTC_EXT qualification is PARKED behind the known SIP entitlement blocker; no further SIP requests. If access genuinely changes later, the contract remains 40 complete sessions (20 training + 20 unseen test; 780 exact minute bars/session plus exact prior close).
- [ ] Run the implemented walk-forward validator on qualified 40-session history and record all train/test metrics. Runtime PASS is unproven.
- [ ] Keep numeric sell-limit output fail-closed until every independent gate passes.

## Remaining engineering and acceptance — P1

- [x] Hardened Daily API: empty history returns incomplete data instead of throwing; response now includes the exact 20 expected dates, missing dates and session-level OHLCV rows.
- [x] Added the selectable-symbol **Daily data audit** panel with provider/feed, adjustment, latest session, retrieval timestamp, close, SMA20, average volume, freshness and missing-session provenance; operator verified RTO, EVR and CBOE.
- [x] Added Daily qualification runner and 20-session independent Stock Analysis / S&P Global reference CSVs for RTO and EVR (2026-10-12 through 2026-11-06).
- [x] Run `scripts/validation/Test-DailyMarketDataQualification.ps1`; evidence `artifacts/daily-data-qualification/20261106T215302Z` confirms 20/20 sessions, zero missing, freshness and internal arithmetic for RTO/EVR.
- [x] Confirm fail-closed behavior: IEX produces `DATA? · IEX_PARTIAL` and null qualification metrics for both symbols.
- [ ] Resolve independent OHLCV parity. The run FAILED for both symbols: numerous OHLC differences and all 20 volume rows mismatch against consolidated independent reference data. Do not treat IEX partial volume as qualified ADV.
- [x] Operator verified the visible **Daily data audit** panel for RTO,EVR,CBOE: 3 returned, 0 API errors, 0 qualification-usable, all diagnostic-only / DATA? / IEX_PARTIAL, 20/20 sessions, zero missing, FRESH.
- [ ] Capture a browser screenshot and CDP console/network evidence; the supplied text did not include a screenshot or browser diagnostics.
- [ ] Build and validate human-adjudicated catalyst labels and holdout precision/recall; keyword triage must never claim CLEAR.
- [ ] Implement/validate a full-pool Opportunity Quality ranker; scanner custom-expression missingness, calibration and rank stability remain unresolved.
- [ ] Implement the separate V3 Boring Overnight Thinkorswim watchlist column from its design specification; MCC Opportunity Quality is not that feature.
- [ ] Resolve exact 09:30 next-session opens and define official auction print versus first observed eligible trade. A 1m bar Open is only a proxy.
- [ ] Compile and validate Thinkorswim 5m/1m authority, matched-state replay, and exact Arizona EOD cutoff behavior.
- [ ] Run `scripts/validation/Test-RegularSessionStreamCollector.ps1` during a confirmed regular session. Verify per-symbol acknowledged subscriptions, completed-bar count/lastBarUtc advancement, global received/persisted deltas, collector cycles/watermarks/errors, and post-capture assessment provenance.
- [ ] Complete full browser acceptance on the main Dashboard: import/parity, Red Zone, exclusions, Morning Brief, replay, sell-limit gate explanations, responsive layout and CDP console/network health. Daily data audit UI behavior is now user-verified; this is not full Dashboard acceptance.
- [ ] Measure Dashboard load and Fetch & Analyze latency with Network/HAR evidence; no unmeasured performance claims.

## External dependencies — P2

- [ ] Build a separate actual broker execution reconciliation ledger from broker order/fill records, quantities, fees and timestamps. Never infer fills from OHLCV.
- [ ] Schwab read-only integration remains blocked on developer approval, app registration, callback and secure OAuth setup. No order routing.
- [ ] Confirm provider entitlement, scanner universe coverage, licensing, rate limits and retention before production-source selection.
- [ ] Define packaging/startup, local data backup/restore, API lifecycle and unattended collector recovery before choosing installer technology.

## Immediate execution order

1. Do not repeat the same IEX Daily comparison and do not retry SIP. The user has confirmed the SIP HTTP 403 multiple times. Daily audit UI was verified for RTO/EVR/CBOE; independent parity remains unresolved (RTO/EVR failed, CBOE reference absent).
2. Compile the revised Thinkorswim V2.1 study. Set 5m chart/study aggregation, ATR14 Wilder, Phoenix timezone, extended hours ON, corporate-action adjustment ON, and a date range that actually supplies at least 99 warm-up bars before the latest 119-bar window.
3. Do not re-upload or re-import the already committed EVR/CBOE ScannerExports. The persisted 327 EVR and 338 CBOE observations and source CSVs are already available. Inspect those first. Only after the revised study is compiled and a specific export defect is demonstrated should a genuinely new, corrected export be requested; never ask for the same files again.
4. Do not re-import the same committed CSVs. Re-read parity after the detector code is deployed. If and only if a new corrected export is justified by chart evidence, import that new file once and record append/identical/correction counts. Do not relax 0.00005 tolerance or impute.
5. Inspect every remaining gap in TOS. If a bar is legitimately absent, record the source/session rule and ensure parity's continuity contract handles that rule explicitly; otherwise correct the export and re-import.
6. Run the updated `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1`. Verify its new feed-identity and mismatch-breakdown fields. With IEX it must remain diagnostic-only and skip SIP/40-session work.
7. Keep SIP-dependent work parked. No further SIP request, configuration change, smoke test, or GTC_EXT capture is planned unless the operator explicitly reports that Alpaca entitlement has changed.
8. Complete the 20-train/20-unseen-test walk-forward runtime validation. Preserve every exclusion and metric; no numeric sell limit before all gates pass.

The latest provider comparison and detailed evidence interpretation are in [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md).



## Operator evidence update — Daily qualification, 2026-11-06 21:53 UTC

- [x] Inspected committed run `artifacts/daily-data-qualification/20261106T215302Z` (raw API responses, per-symbol summaries, combined summary and reference CSVs).
- [x] Confirmed internal arithmetic and 20-session coverage pass for RTO and EVR.
- [x] Confirmed independent parity FAIL for both; API values are internally consistent but IEX differs from consolidated reference OHLCV, especially volume.
- [x] Kept qualification fail-closed: feed IEX, `IEX_PARTIAL`, qualification values null, DATA?.
- [x] SIP access remains denied by repeated HTTP 403. No further SIP requests or configuration changes are planned; this is a known external blocker.
- [ ] Find an independent Daily data source that does not require the denied SIP entitlement, or retain IEX values as diagnostic-only. RTO/EVR comparison already failed; CBOE reference is absent. Do not repeat the unchanged IEX comparison.

The current Daily task is no longer “script not run”; it is “script run, independent parity failed, SIP/source qualification outstanding.” See [Daily data qualification](Validation/Daily-Data-Qualification-20261106.md).



## ATR recurrence follow-up — 2026-11-06

- [x] Independently recomputed Wilder recurrence across all adjacent persisted observations: 326 EVR pairs and 337 CBOE pairs.
- [x] Confirmed maximum absolute recurrence residual 0.0000928571 for both, consistent with four-decimal ATR rounding; no residual exceeded 0.0002.
- [ ] Re-read parity after the session-aware detector build/restart. The known EVR 00:00–00:10 ET closure false positive is now excluded; premarket-overlapping EVR and after-hours CBOE gaps remain unresolved until checked against actual TOS chart bars.
- [ ] Compile revised V2.1 ThinkScript in TOS and inspect existing exported/persisted rows. A new broad export is only needed if a specific omission is proven; do not repeat the same ScannerExports.



## Operator browser/runtime update — 2026-11-06

- [x] Main Dashboard confirmed; use `frontend/index.html` only. `prototypeIndex.html` is not an alternate acceptance target.
- [x] Visible section label confirmed as **Daily data audit**.
- [x] Operator ran Daily audit for RTO,EVR,CBOE: 3 checked, 0 API errors, 0 qualification-usable, 3 diagnostic-only; all 20/20, zero missing, FRESH, IEX_PARTIAL / DATA?.
- [x] Clarified Overnight fail-closed semantics: historical scanner PASS is only shortlist evidence; unknown/missing/stale price, liquidity, catalyst, session, exclusion or chart gates cannot become PASS/READY. Unknown catalyst is not CLEAR; Final READY stays unavailable until all required gates pass.
- [x] Added `scripts/validation/Capture-DashboardBrowserDiagnostics.ps1` for CDP console/network capture and Dashboard screenshot.
- [x] Added `scripts/validation/Test-RegularSessionStreamCollector.ps1` for 10-minute, 15-second polling of API health, provider, WebSocket stream and historical collector status during regular hours.
- [ ] Run both PowerShell scripts and commit their timestamped evidence directories.
- [ ] Full Dashboard browser acceptance remains open beyond the verified Daily audit panel.
- [ ] Regular-session WebSocket delivery and collector progress remain unverified until the new script is run.

**SIP policy:** the repeated HTTP 403 is accepted as a known external blocker. Do not ask for or initiate another SIP request. Keep SIP-dependent qualification parked unless the operator independently reports changed entitlement.

## [Uncertainties]

- Remaining TOS timestamp gaps may be legitimate no-trade intervals or export omissions; supplied CSVs cannot classify them.
- IEX/TOS OHLCV differences do not establish which source is wrong and do not predict SIP results.
- The revised ThinkScript and latest PowerShell runner edit have not been compiled/executed in their target runtimes.
- SIP entitlement, 40/40 GTC_EXT coverage, walk-forward runtime pass, exact prior close, browser acceptance and regular-session WebSocket recovery remain unverified.
- Scanner full-pool missing-field count must be refreshed; 296 was the last recorded audit result.
- No actual broker execution ledger or broker-fill integration is verified.
