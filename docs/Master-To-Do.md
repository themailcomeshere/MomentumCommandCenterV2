# Momentum Command Center — Master To-Do

**Updated:** 2026-11-06  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261106.md](NewChatHandoff-20261106.md)  
**Detailed active backlog:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)  
**Latest ATR evidence analysis:** [ATR-Export-Coverage-Fix-20261106.md](Validation/ATR-Export-Coverage-Fix-20261106.md)  
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
- [x] GitHub Actions `build-and-test` passed on commit `d00dbd61ede861f6a9924165377a17e9d8001022`, including the current script/study source state. This validates repository .NET build/tests, not Thinkorswim compilation or PowerShell runner execution.

## Current blockers — P0

- [ ] EVR 5m remains `NOT_MATCH`: 325 observations, 1 gap in the 119-bar window, mean absolute error 0.0366528495 and maximum 0.0677729555. Row-level recurrence localizes the large divergence to 2026-11-05T14:05Z after a 17h05m source-history gap. Re-export all chart bars using the corrected study, then re-run parity.
- [ ] CBOE 5m is now numerically within tolerance (mean absolute error 0.0000200950; max 0.0000436336), but remains `GAPS_PRESENT` with 1 gap in the 119-bar window. It is not a MATCH.
- [ ] Compile the revised V2.1 ThinkScript in TOS and export all available chart bars (premarket, RTH and after-hours) within the selected date range. Do not filter export rows by time of day.
- [ ] Obtain at least 119 continuous completed 5m observations in the latest qualification window, with sufficient earlier history for 99 warm-up bars; no unexplained gaps may be imputed.
- [ ] Resolve EVR 1m's 13 gaps in the 119-observation comparison window or retain `GAPS_PRESENT`.
- [ ] Prove exact provider/TOS 5m OHLCV equality over the full 119-bar window and exact same-provider/feed prior RTH close.
- [ ] Keep SIP GTC_EXT qualification blocked until entitlement changes. Current effective feed is IEX; prior SIP access returned HTTP 403.
- [ ] After SIP access is confirmed, capture 40/40 exact GTC_EXT sessions (20 train + 20 unseen test; 780 exact minute bars/session plus exact prior close).
- [ ] Run the implemented walk-forward validator on qualified 40-session history and record all train/test metrics. No numeric sell limit until every independent gate passes.

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

## Immediate next action

Pull the latest branch and compile the revised Thinkorswim V2.1 study at [MCC_OHLCV_ATR_V2.txt](Charts/Flexible%20Grid/OHLCV/MCC_OHLCV_ATR_V2.txt). The export predicate now emits every available chart bar in the selected date range; the previous 07:00–19:59 ET filter omitted bars that still influenced TOS Wilder ATR.

Re-export EVR and CBOE 5m with extended hours ON and a sufficiently early start date, including premarket, regular-session and after-hours bars. Import once, then re-run parity. Do not change tolerance or impute missing bars. After import, run `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1` to capture the exact provider/TOS 119-bar comparison on the currently configured feed. It will skip SIP capture unless `/api/providers/status` confirms effective feed `sip`.

See [ATR export coverage diagnosis](Validation/ATR-Export-Coverage-Fix-20261106.md) for the exact recurrence break, updated study logic, and gate-by-gate status.

## [Uncertainties]

- The EVR recurrence discontinuity is localized to a long source-history gap; the exact omitted chart bars are not in the supplied CSV. The revised study has not yet been compiled/run.
- SIP entitlement and 40/40 GTC_EXT coverage remain unverified.
- Walk-forward runtime pass, provider/TOS equality, browser acceptance and regular-session WebSocket recovery remain unverified.
- Scanner full-pool missing-field count must be refreshed; 296 was the last recorded audit result.
- No actual broker execution ledger or broker-fill integration is verified.
