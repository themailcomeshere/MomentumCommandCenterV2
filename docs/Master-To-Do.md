# Momentum Command Center — Master To-Do

**Updated:** 2026-11-06  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261106.md](NewChatHandoff-20261106.md)  
**Detailed active backlog:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)  
**Latest ATR evidence analysis:** [ATR-Export-Coverage-Fix-20261106.md](Validation/ATR-Export-Coverage-Fix-20261106.md)  
**Latest provider/sell-limit evidence:** [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md)  
**Earlier evidence archive:** [ATR-Evidence-Export-Analysis-20261106.md](Validation/ATR-Evidence-Export-Analysis-20261106.md)

This is the sole active execution checklist. Historical repeated checkboxes were moved verbatim to the archive and consolidated into the detailed backlog. Do not use historical unchecked boxes as current task status. Keep this file concise; put technical acceptance criteria in the active backlog and specialized runbooks.

## Verified at this checkpoint

- [x] Operator reports Release build and tests PASS at local source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- [x] Operator restarted API and re-ran persisted ATR parity for EVR 1m, EVR 5m and CBOE 5m.
- [x] API health is `OK`; mode is `decision-support-only`; order submission is `NOT IMPLEMENTED`.
- [x] Alpaca is configured/validated on IEX. The latest captured provider-status evidence reported 8 validation bars. Massive is not configured.
- [x] Latest ATR import retries were idempotent: zero new observations and zero corrections. Do not re-import solely to re-run parity.
- [x] ATR parity version `MCC-WILDER-ATR-PARITY-1.1` and full-history Wilder seed correction are present and running in the restarted local API.
- [x] Walk-forward validator and hard orchestration gate are implemented. Runtime pass remains unproven.
- [x] Main Dashboard is `frontend/index.html`. Do not use or recreate PrototypeIndex.
- [x] Supporting documentation, legacy handoffs and audit files were reconciled; a repeatable ATR evidence-export script was added.
- [x] Operator successfully ran the exporter; ZIP captured health/provider status, all four parity endpoints, persisted TOS observations, manifest and SHA-256 hashes at source SHA `9dd2c224d8f1373c7935eb3f94ed5fddea837e5f`.
- [x] Row-level recurrence was analyzed for the expanded EVR/CBOE 5m exports; the EVR mismatch is localized to a recurrence discontinuity across a long source-history gap.
- [x] Identified and corrected the V2.1 study's export predicate: ATR was calculated on all chart bars while the Strategy Report omitted bars outside 07:00–19:59 ET. The study source now exports every available chart bar in the selected date range.
- [x] GitHub Actions `build-and-test` passed on commit `d00dbd61ede861f6a9924165377a17e9d8001022`, including the original qualification runner/study source state. It does not validate subsequent runner edits, Thinkorswim compilation, or local PowerShell execution.
- [x] Reviewed operator evidence ZIP `20261106T105332.zip`: API healthy; effective Alpaca feed IEX; no SIP request or 40-session capture occurred.
- [x] Latest imports persisted 327 EVR and 338 CBOE 5m observations; 2 EVR and 9 CBOE rows appended, with no corrections.
- [x] Latest EVR/CBOE 5m numeric ATR errors are within 0.00005, but parity remains `GAPS_PRESENT` (3 EVR and 1 CBOE gap in the 119-observation qualification window).
- [x] Analyzed row-level IEX/TOS comparison: 0/119 exact OHLCV rows for each symbol; IEX returned 104 EVR and 109 CBOE bars. These results are diagnostic only, not SIP qualification.
- [x] Updated qualification runner to report comparison mode, configured/response/row-level feed identity, and per-field mismatch counts. This runner edit has not yet been executed locally.
- [x] Added [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md) with import provenance, parity, IEX comparison and gate interpretation.

## Current blockers — P0

- [ ] Compile the revised V2.1 ThinkScript in Thinkorswim and export full chart history. The revised source removes the 07:00–19:59 ET export filter, but TOS compilation/run is not yet evidenced.
- [ ] Resolve EVR 5m parity: latest status `GAPS_PRESENT`, 3 gaps in the 119-observation window; MAE 0.0000294330, max 0.0000495349. Numeric values are within tolerance, but continuity is not certified.
- [ ] Resolve CBOE 5m parity: latest status `GAPS_PRESENT`, 1 gap in the 119-observation window; MAE 0.0000203866, max 0.0000426076. Numeric values are within tolerance, but continuity is not certified.
- [ ] Obtain 119 continuous completed 5m observations for each required symbol, plus at least 99 earlier warm-up bars. Investigate every gap against source/chart evidence; never impute.
- [ ] Resolve EVR 1m's 13 gaps in its prior 119-observation window or retain `GAPS_PRESENT`; 1m does not replace the independent 5m gate.
- [ ] Prove exact provider/TOS 5m OHLCV equality over all 119 timestamps and capture the exact previous RTH close from the same provider/feed. Latest IEX diagnostic returned 0/119 exact rows for EVR and CBOE; it does not establish SIP behavior.
- [ ] Keep SIP qualification blocked until account entitlement changes. Current effective feed is IEX; this run made no SIP request.
- [ ] After SIP access is confirmed, capture 40/40 exact SIP GTC_EXT sessions (20 chronological training + 20 unseen test; 780 exact minute bars/session plus exact prior close).
- [ ] Run the implemented walk-forward validator on qualified 40-session history and record all train/test metrics. Runtime PASS is unproven.
- [ ] Keep numeric sell-limit output fail-closed until every independent gate passes.

## Remaining engineering and acceptance — P1

- [ ] Complete Daily data source/adjustment parity and operator smoke tests for RTO/EVR; incomplete sessions remain DATA?.
- [ ] Build and validate human-adjudicated catalyst labels and holdout precision/recall; keyword triage must never claim CLEAR.
- [ ] Implement/validate a full-pool Opportunity Quality ranker; scanner custom-expression missingness, calibration and rank stability remain unresolved.
- [ ] Implement the separate V3 Boring Overnight Thinkorswim watchlist column from its design specification; MCC Opportunity Quality is not that feature.
- [ ] Resolve exact 09:30 next-session opens and define official auction print versus first observed eligible trade. A 1m bar Open is only a proxy.
- [ ] Compile and validate Thinkorswim 5m/1m authority, matched-state replay, and exact Arizona EOD cutoff behavior.
- [ ] Complete regular-session WebSocket/collector delivery and recovery acceptance using per-symbol completed/persisted bar deltas and freshness/provenance.
- [ ] Complete browser acceptance on the main Dashboard: import/parity, Red Zone, exclusions, Morning Brief, replay, sell-limit gate explanations, responsive layout and console/network health.
- [ ] Measure Dashboard load and Fetch & Analyze latency with Network/HAR evidence; no unmeasured performance claims.

## External dependencies — P2

- [ ] Build a separate actual broker execution reconciliation ledger from broker order/fill records, quantities, fees and timestamps. Never infer fills from OHLCV.
- [ ] Schwab read-only integration remains blocked on developer approval, app registration, callback and secure OAuth setup. No order routing.
- [ ] Confirm provider entitlement, scanner universe coverage, licensing, rate limits and retention before production-source selection.
- [ ] Define packaging/startup, local data backup/restore, API lifecycle and unattended collector recovery before choosing installer technology.

## Immediate execution order

1. Pull `feature/opportunity-quality-classification` and run the local Release build/tests before restarting the API.
2. Compile the revised Thinkorswim V2.1 study. Set 5m chart/study aggregation, ATR14 Wilder, Phoenix timezone, extended hours ON, corporate-action adjustment ON, and a date range that actually supplies at least 99 warm-up bars before the latest 119-bar window.
3. Export EVR and CBOE with all available premarket, RTH and after-hours chart bars. Confirm embedded epoch timestamps; do not use Strategy Report order-event timestamps.
4. Import once, inspect append/identical/correction counts and per-row provenance, then rerun parity. Do not relax 0.00005 tolerance or impute.
5. Inspect every remaining gap in TOS. If a bar is legitimately absent, record the source/session rule and ensure parity's continuity contract handles that rule explicitly; otherwise correct the export and re-import.
6. Run the updated `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1`. Verify its new feed-identity and mismatch-breakdown fields. With IEX it must remain diagnostic-only and skip SIP/40-session work.
7. Obtain/confirm SIP entitlement outside MCC. Only after that, configure SIP, restart, verify effective feed, run one recent SPY SIP smoke request, and then capture/audit 40 GTC_EXT sessions.
8. Complete the 20-train/20-unseen-test walk-forward runtime validation. Preserve every exclusion and metric; no numeric sell limit before all gates pass.

The latest provider comparison and detailed evidence interpretation are in [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md).

## [Uncertainties]

- Remaining TOS timestamp gaps may be legitimate no-trade intervals or export omissions; supplied CSVs cannot classify them.
- IEX/TOS OHLCV differences do not establish which source is wrong and do not predict SIP results.
- The revised ThinkScript and latest PowerShell runner edit have not been compiled/executed in their target runtimes.
- SIP entitlement, 40/40 GTC_EXT coverage, walk-forward runtime pass, exact prior close, browser acceptance and regular-session WebSocket recovery remain unverified.
- Scanner full-pool missing-field count must be refreshed; 296 was the last recorded audit result.
- No actual broker execution ledger or broker-fill integration is verified.
