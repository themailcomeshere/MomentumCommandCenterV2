# Momentum Command Center — Master To-Do

**Updated:** 2026-11-06  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261106.md](NewChatHandoff-20261106.md)  
**Detailed active backlog:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)

This is the sole active execution checklist. Historical repeated checkboxes were moved verbatim to the archive and consolidated into the detailed backlog. Do not use historical unchecked boxes as current task status. Keep this file concise; put technical acceptance criteria in the active backlog and specialized runbooks.

## Verified at this checkpoint

- [x] Operator reports Release build and tests PASS at local source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- [x] Operator restarted API and re-ran persisted ATR parity for EVR 1m, EVR 5m and CBOE 5m.
- [x] API health is `OK`; mode is `decision-support-only`; order submission is `NOT IMPLEMENTED`.
- [x] Alpaca is configured/validated on IEX. One validation bar was returned. Massive is not configured.
- [x] Latest ATR import retries were idempotent: zero new observations and zero corrections. Do not re-import solely to re-run parity.
- [x] ATR parity version `MCC-WILDER-ATR-PARITY-1.1` and full-history Wilder seed correction are present and running in the restarted local API.
- [x] Walk-forward validator and hard orchestration gate are implemented. Runtime pass remains unproven.
- [x] Main Dashboard is `frontend/index.html`. Do not use or recreate PrototypeIndex.
- [x] Supporting documentation, legacy handoffs and audit files were reconciled; a repeatable ATR evidence-export script was added.

## Current blockers — P0

- [ ] Diagnose EVR 5m `NOT_MATCH` (mean absolute error 0.0010467166; max 0.0019357350) from exact persisted OHLCV/ATR rows. Root cause is unknown.
- [ ] Diagnose CBOE 5m `NOT_MATCH` (mean absolute error 0.0005274676; max 0.0009650641) from exact persisted OHLCV/ATR rows. Root cause is unknown.
- [ ] Obtain corrected, continuous V2.1 5m TOS history with at least 119 completed observations in the qualification window and no unexplained gaps.
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

Run:

```powershell
.scriptsalidationExport-AtrParityEvidence.ps1
```

The script saves health/provider status, EVR/CBOE 1m/5m parity responses, full persisted TOS observation rows, local Git SHA when available, manifest and SHA-256 hashes under `artifacts/atr-parity-evidence/<timestamp>/`. Zip that output folder and provide it. This is the next required evidence to diagnose the 5m numeric mismatches. It does not include provider OHLCV or establish provider/TOS equality.

Then follow [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md) in priority order.

## [Uncertainties]

- Numeric mismatch root causes and the meaning of missing TOS timestamps are not established.
- SIP entitlement and 40/40 GTC_EXT coverage remain unverified.
- Walk-forward runtime pass, provider/TOS equality, browser acceptance and regular-session WebSocket recovery remain unverified.
- Scanner full-pool missing-field count must be refreshed; 296 was the last recorded audit result.
- No actual broker execution ledger or broker-fill integration is verified.
