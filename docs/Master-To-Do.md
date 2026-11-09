# Momentum Command Center — Master To-Do

**Updated:** 2026-11-09 (operator validation recheck, EWTX rationale hardening, MCC Top-4 distinct-symbol fix)  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261109.md](NewChatHandoff-20261109.md)  
**Detailed engineering acceptance criteria:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Catalyst review procedure:** [Catalyst-Human-Review-Workflow.md](Catalyst-Human-Review-Workflow.md)  
**Technical workflow:** [Technical-Workflow.md](Technical-Workflow.md)  
**Runtime acceptance:** [Runtime-Acceptance-Runbook.md](Runtime-Acceptance-Runbook.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)

This is the current authoritative checklist. The detailed backlog contains implementation boundaries and acceptance criteria. Older historical checkbox states are archived and do not override the current statuses below. Continue on the feature branch; do not modify `master`. Main Dashboard only; do not return to PrototypeIndex.

## Current operator evidence — 2026-11-09

- [x] Operator confirmed the restored six-label catalyst legend looks good.
- [x] Operator deployed the previous rationale guard and supplied validation: raw reviews 6; included unique 0; excluded 6; conflicts 0; training/holdout 0/0; non-evaluable labels 2; superseded reviews 3; inadequate evidence 1.
- [x] This confirms the earlier assistant-appeal rationale is excluded. The validator is correctly withholding metrics.
- [x] Operator supplied new EWTX review `fbc839fc9c2740c1ad87e6bc641e2a77`. It is not an acceptable correction: generic confirmation, not article-specific facts; supersedes `8405880...` rather than weak review `f496393...`; and uses the human-validation version as its `ruleSetVersion`.
- [x] API/historical validator/UI now reject the observed generic confirmation phrases as well as short, placeholder and assistant-appeal rationales. Regression test includes the exact submitted notes. This is a heuristic, not proof of factual accuracy.
- [x] MCC Top 4 now sorts rows by score and deduplicates by ticker, preserving the highest-scoring row per symbol. Duplicate rows can no longer occupy multiple Top-4 slots.
- [ ] Operator pull/build/test/restart and runtime verification of the distinct-symbol Top 4 are pending.
- [ ] EWTX still needs an independently researched correction superseding both active weak reviews `f496393...` and `fbc839...`; see P1.2 below.

## P0 — Data integrity and sell-limit qualification

### ATR V2 / Thinkorswim parity

- [x] ATR V2 StrategyReport import, provenance capture and persisted observations.
- [x] Wilder ATR(14) recurrence and 5m parity comparison with 99-bar warm-up and 20-bar comparison window.
- [x] Timestamp-gap diagnostics scoped to expected 04:00–20:00 ET chart-session slots.
- [x] Exact operator-confirmed no-bar intervals encoded; no synthetic bars or imputation.
- [x] Operator runtime parity: EVR 5m MATCH (327 observations, 20 compared, 0 unresolved gaps, 2 verified no-bar intervals; MAE 0.0000294330, max error 0.0000495349).
- [x] Operator runtime parity: CBOE 5m MATCH (338 observations, 20 compared, 0 unresolved gaps, 1 verified no-bar interval; MAE 0.0000203866, max error 0.0000426076).
- [ ] Compile the revised ThinkScript in Thinkorswim and retain compile/runtime evidence for the exact current source.
- [ ] Complete independent 1m continuity; EVR 1m previously had 13 gaps. Keep 1m independent of the 5m authority gate.
- [ ] Revalidate any future, previously unrecognized timestamp gaps against actual source/chart evidence. Existing verified no-bar windows do not authorize blanket gap suppression.

### Provider/TOS OHLCV parity and sell-limit gate

- [x] Provider comparison orchestration and fail-closed sell-limit gates are implemented.
- [x] Evidence runner distinguishes configured, response and row-level feed identity.
- [ ] Obtain 119 exact provider/TOS 5m OHLCV matches, including exact volume, and the exact previous RTH close from the same qualified feed.
- [ ] Keep sell-limit numeric output blocked until ATR, source provenance, continuity, exact provider/TOS comparison, reference close and all freshness/session gates pass.
- [x] Walk-forward validator implementation exists.
- [ ] Runtime qualify the validator only after a complete source-qualified 40-session dataset exists; report all train/test counts, exclusions and metrics.

### SIP / GTC_EXT history — parked external blocker

- [x] SIP HTTP 403 entitlement denial is established by repeated operator evidence; effective Alpaca feed is IEX.
- [x] IEX is diagnostic-only and is not substituted for SIP.
- [ ] PARKED: no further SIP request, entitlement check, configuration change or capture unless the operator independently reports a genuine entitlement change.
- [ ] If entitlement changes, collect 40 complete sell sessions (20 training + 20 unseen test), each with 780 exact 1m bar-start timestamps and exact previous RTH close; verify source/feed and persisted read-back.
- [ ] Only then run walk-forward runtime qualification. No nearest-bar substitution or imputation.

## P1 — Market data, strategy correctness and dashboard

### Daily market data and liquidity

- [x] Daily API returns completed-session close, SMA20, ADV20, expected/missing session dates, freshness and provenance.
- [x] Daily Dashboard audit panel and PowerShell qualification/evidence runner implemented.
- [x] 2026-11-06 RTO/EVR run passed internal 20-session arithmetic, coverage and freshness checks.
- [x] Same run failed independent consolidated OHLCV parity for both symbols; Alpaca IEX correctly returned `IEX_PARTIAL`, `qualificationUsable=false`, and null qualification fields.
- [ ] Do not repeat the unchanged IEX comparison. Find a genuinely independent, appropriately licensed source/feed or keep qualification blocked.
- [ ] Capture browser screenshot showing the Daily audit UI matches its API evidence.
- [ ] Recheck corporate-action adjustment equivalence before any new independent comparison.

### Catalyst human review and classifier validation

- [x] Ticker-scoped news retrieval and deterministic keyword triage exist; no automatic CLEAR is permitted.
- [x] Append-only human review store, saved prediction capture, conflict diagnostics and explicit supersession exist.
- [x] Chronological holdout validation endpoint and six-label Dashboard legend exist.
- [x] Source/placeholder/short/assistant-appeal rationale guards and historical evidence exclusion exist.
- [x] Latest operator validation correctly reports zero eligible reviews and zero conflicts.
- [x] Generic EWTX affirmation submitted by operator is now explicitly rejected by the strengthened rationale heuristic.
- [ ] Pull/build/restart latest branch and verify generic rationale rejection at runtime.
- [ ] Independently open the original EWTX source article; identify concrete facts, original publisher, and the actual triage ruleset version saved with the prediction. Data not found in sources for the article body and exact saved triage version.
- [ ] Append a source-backed EWTX correction superseding both currently active weak records `f496393535df431ab275231b57f8530a` and `fbc839fc9c2740c1ad87e6bc641e2a77`. Preserve prediction `UNCLASSIFIED` and categories `[]`; independently choose the label.
- [ ] Re-query validation; require conflict=0 and inadequateReviewEvidence=0 for the active EWTX record.
- [ ] Keep ENOV/ARDT and other unsupported articles UNCERTAIN; do not relabel to increase sample size.
- [ ] Collect a substantially larger source-backed sample across MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL and IRRELEVANT. Six eligible records is only a structural floor.
- [ ] Double-review a subset, measure reviewer agreement, and ensure both truth classes occur in the chronological holdout.
- [ ] Measure per-class precision/recall, false positives/negatives, source coverage and time-window coverage. No classifier promotion until representative evidence supports it.

### Opportunity Quality ranking

- [x] Deterministic Opportunity Quality assessment and explicit-symbol API ranking endpoint (1–50 symbols).
- [x] Dashboard top-50 diagnostic ranking control wired to imported scanner-score candidates.
- [x] Candidate results disclose provisional classification/reasons; DATA? and BLOCKED are not promoted.
- [ ] Audit current scanner-field availability by symbol and report missing-field denominators/source timestamps; last historical audit count of 296 missing custom-expression rows is stale.
- [ ] Expand beyond bounded top-50 prefilter only after candidate-pool completeness is evidenced.
- [ ] Calibrate thresholds/rank stability using time-separated candidate snapshots and outcomes; current ranking is diagnostic, not a calibrated probability or market-wide discovery engine.
- [ ] Add full-pool complete/incomplete/stale/excluded deterministic fixtures and UI acceptance.

### Overnight boundary / Boring Overnight

- [x] 09:30 1m open is explicitly labelled `ONE_MINUTE_BAR_OPEN_PROXY`; `officialOpeningAuctionVerified=false`.
- [x] Close-to-next-open endpoint is documented as spanning Day 1 after-hours and Day 2 premarket; it does not reconstruct path/extrema.
- [ ] Verify official auction print versus first eligible observed trade from a properly entitled source. IEX cannot certify consolidated/auction coverage.
- [ ] Resolve exact historical buy/sell endpoints against the original Thinkorswim Overnight scanner formula.
- [ ] Implement separate V3 Boring Overnight watchlist column only after endpoint/session semantics and source-backed metric fields are frozen. Keep it distinct from MCC Opportunity Quality.
- [ ] Keep Overnight Edge and Boringness scores separate; validate score weights and hard gates against known fixtures before use.

### Thinkorswim 5m/1m authority and EOD

- [ ] Compile exact current 5m/1m studies in TOS.
- [ ] Verify 5m Permission remains authoritative and 1m is execution timing only.
- [ ] Export matched completed-bar state labels; OHLCV-only rows cannot prove Permission/READY/RUNNER/WANING/SWING/GET OUT.
- [ ] Verify exact Arizona-local EOD cutoff using bars immediately before, at and after cutoff, including DST/standard-time cases.

### Dashboard, browser, WebSocket and collector

- [x] Main Dashboard is the only supported UI; PrototypeIndex is retired.
- [x] Edge discovery/launch diagnostics and browser CDP capture script implemented.
- [x] Regular-session stream/collector evidence script implemented.
- [x] Dashboard load/API latency instrumentation work was started; prior measurement showed a 10.24s live-assessment request and a pending WebSocket connection, so no overall performance pass is claimed.
- [ ] Verify MCC Top 4 selects/fetches four distinct tickers after latest pull; if the imported file has fewer than four unique symbols, show the source-data limitation.
- [ ] Verify the post-fetch status lists four distinct queried symbols and the evidence table shows each result. The human-review queue lists only symbols with returned articles, not every symbol queried.
- [ ] Execute browser diagnostics on operator machine; retain screenshot, console exceptions and sanitized network evidence.
- [ ] Complete full Dashboard acceptance: import/parity, Red Zone, exclusions, Morning Brief, replay, sell-limit gate explanations, responsive layout and CDP console/network health.
- [ ] During a confirmed regular session, capture 5–10 minutes of SPY/RKLX/ASTX/XRPN stream/collector data. Verify per-symbol subscriptions, completed bars, timestamps, received/persisted deltas and watermarks.
- [ ] Exercise restart, sleep/wake, network/provider failure, duplicate/replayed events, session boundaries and idempotent recovery.
- [ ] Measure document/load/DOMContentLoaded/largest request/Fetch & Analyze durations from browser Network/HAR; compare to a baseline before claiming improvement.

## P2 — Integrations, execution and deployment

- [ ] Implement a broker execution reconciliation ledger from actual broker order/fill records, including partial fills, quantities, fees, timestamps and cancel/correction lineage. Never infer fills from OHLCV.
- [ ] Schwab read-only integration is blocked on developer approval, app registration, callback and secure OAuth setup. No order submission.
- [ ] Document provider entitlement, scanner universe coverage, licensing, rate limits, retention and symbol/session coverage before production-source selection. Massive remains unconfigured.
- [ ] Define packaging/startup, API lifecycle, local data backup/restore, log rotation, health monitoring and unattended collector recovery before choosing installer technology.
- [ ] Keep order routing explicitly NOT IMPLEMENTED. MCC remains decision-support-only.

## P3 — Release governance and operating workflow

- [x] Main feature branch and technical workflow documented.
- [x] Reliable pull/build/test sequence recorded in the handoff.
- [ ] Operator must run the exact latest branch-head Release build/test and restart local API before runtime acceptance of new code.
- [ ] Every code-bearing branch head must have passing CI; ThinkScript compilation, browser acceptance and operator runtime evidence are separate gates.
- [ ] Update this checklist and the current handoff after each material implementation/evidence change.
- [ ] Never mark external, operator-side or source-dependent work complete from code/CI alone.

## Immediate operator sequence

1. Run the PowerShell sequence in the current handoff from the repository root. Do not reset or discard local changes.
2. Confirm the script reports the latest feature-branch SHA and all Release tests pass; restart the API from that build.
3. In the Dashboard, confirm MCC Top 4 contains four distinct tickers. Click Fetch Daily + catalyst; verify four distinct queried symbols and inspect the per-symbol table. The article review queue may contain only EWTX if the other three returned no articles; no-news is UNKNOWN, not CLEAR.
4. Query catalyst validation again. Expected from current six-record history: zero conflicts, zero included until EWTX is corrected; new generic record should be counted as inadequate evidence after deploying the latest guard.
5. Independently review the EWTX article. Verify the original publisher and saved triage ruleset; append a correction superseding both weak active review IDs. Do not invent source facts or alter the saved prediction.
6. Continue the remaining P0/P1 runtime and evidence tasks above. Do not request another SIP entitlement check.

## [Uncertainties]

- The exact current contents of the user's imported scanner CSV are not available here. The duplicate-row Top-4 defect is fixed, but the import may still contain fewer than four unique symbols.
- Data not found in sources for the EWTX article body, confirmed original publisher, and exact saved prediction ruleset version.
- The latest code changes have individual passing CI runs; exact latest documentation branch-head CI is still in progress at handoff time.
- SIP remains blocked by the previously confirmed HTTP 403 entitlement response; no change in entitlement is evidenced.
- Browser/CDP acceptance, local runtime after latest commit, Thinkorswim compilation, and regular-session collector delivery remain unverified.
