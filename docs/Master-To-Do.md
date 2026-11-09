## Boring Overnight UI implementation — 2026-11-09

- [x] Added Previous Regular-Session Close, Latest Price (identified as extended-hours when latest 1M bar is outside RTH), and Calculated Limit Sell to the focused-symbol panel.
- [x] Price fields show Phoenix source timestamps; latest 1M close is not represented as a live quote. Exact 16:00 ET wording is used only when the 15:59 ET bar is present.
- [x] Added focused-symbol sell-limit action reusing the existing evidence-gated API and quantile. No duplicate calculator and no order submission.
- [x] Boring Overnight Red Zone now uses historical 5M bars and marks intraday 5M/1M gates NOT USED rather than displaying Intraday stale status.
- [x] Manual pinned symbols are polled during Automatic mode; sound requires a known false-to-true EXECUTION READY transition and enabled sound.
- [x] Scanner Workbench Symbols auto-populates with up to 20 unique imported Overnight symbols sorted by Overnight Score; manual Use Overnight Top 20 action is available. It does not submit a scan.
- [x] Removed obsolete Red Zone static chip, redundant label ID, and obsolete explanatory paragraph. Preserved `.gate-warning` and Ticker placeholder.
- [x] Decision Explanation and Timeframe Handoff are collapsible with pointer cursor. Timeframe Handoff is hidden in Boring Overnight mode.
- [ ] Operator build/test, browser verification, and API/runtime acceptance remain required.

## Operator evidence and implementation — 2026-11-09 (Overnight top-20, mode isolation, TOS formula engine)

- [x] Operator confirmed all eight scanner columns display after prior deployment; some HISTORY values remain DATA? while PRICE and 20D AVG VOL are populated. These evidence fields remain independent.
- [x] Added automatic population of Daily data audit Symbols with the top 20 unique imported Overnight symbols, sorted by Overnight Score, on import and restored import.
- [x] Added a manual **Use Overnight Top 20** action; audit accepts up to 20 symbols and runs no more than four Daily requests concurrently. Population does not automatically launch API requests.
- [x] Made EXCLUSIONS disclosure summary visibly interactive with pointer cursor, hover affordance and keyboard focus outline.
- [x] Persisted workspace mode and restore it before startup requests; Boring Overnight no longer starts the Intraday live-assessment path.
- [x] In Boring Overnight mode, Main Chart uses 5-minute historical bars over a rolling five-day window, updates on selected-symbol changes, and refreshes while visible. Intraday permission/execution cards are hidden and status explicitly says NOT USED.
- [x] Added versioned `MCC-TOS-DAILY-GAP-1.0` calculator reproducing all eight supplied Thinkorswim scanner formulas from explicit TOS daily OHLC bars.
- [x] Added append-only TOS daily reference store, import/read API endpoints, CSV import UI, provenance fields (aggregation, extended-hours, adjustment, timezone), and per-column displayed-precision parity results.
- [x] Added regression tests for all eight fields, score decomposition, insufficient history, duplicate dates and source metadata validation.
- [ ] CI for this code set and operator local build/runtime acceptance are pending.
- [ ] Import matched TOS OHLC exports and compare with scanner CSV for representative symbols; until then formula parity is implemented but empirical per-symbol parity is not yet demonstrated.
- [ ] Exact V3 trade-window Boring Overnight study remains blocked on frozen Arizona-local buy timestamp, sell/measurement endpoint, and official auction vs first eligible trade semantics. Do not substitute daily-gap scanner formula for executable trade return.
- [ ] BAYRY Daily response contract diagnosis remains open.
- [ ] Remaining P0/P1/P2/P3 checklist below remains current; do not mark unresolved external data/entitlement/operator evidence tasks complete.

## Operator evidence and implementation — 2026-11-09 (Overnight formulas + BAYRY)

- [x] Operator supplied all eight active Overnight Scanner ThinkScript columns and the inactive Boring Winner formula in `docs/Column Formulas`; reviewed exact source.
- [x] Documented formula definitions, Overnight Score weighting, Boring Winner gates, screenshot candidates, and a step-by-step MCC workflow in [Overnight Scanner Formula Audit and MCC Workflow](Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md).
- [x] Main Dashboard Overnight qualification table now displays all eight imported scanner metrics (ON Win %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, Overnight Score).
- [x] Historical row status now requires all eight source columns to be present; a missing Median/Momentum/Score field remains DATA?, not PASS or FAIL.
- [x] API-backed EXCLUSIONS section is now collapsible via native details/summary; its refresh/add/remove controls and API behavior remain intact.
- [x] Operator verified BAYRY stream subscription and live assessment both return 200 OK. Daily history request still returns 502 after CSV import and after clearing Red Zone; UI detail: `Alpaca Daily response did not contain a bars array.`
- [ ] Diagnose BAYRY's HTTP-200 upstream payload shape (or unexpected response envelope) and handle valid empty/unsupported-symbol responses explicitly. Current retry handles transient transport/status failures only; it cannot repair a successful response with a missing/wrong bars shape.
- [ ] Operator pull/build/test/restart and runtime verify the collapsible exclusions panel and all-eight-column table.
- [ ] Confirm TOS daily aggregation/session setting: formulas are exactly `open / close[1] - 1`, but code does not specify whether daily `open` reflects regular-hours only or includes extended hours. Match exported TOS bars to MCC data before declaring overnight-window parity.
- [ ] Keep Boring Winner inactive pending approval and chronological source-qualified validation; no TOS V3 Boring Overnight study has yet been compiled.

## Operator evidence — 2026-11-09 (Top-4, Daily provider, catalyst validation)

- [x] Operator confirms MCC Top 4 made requests for exactly four symbols; the article queue currently shows only EWTX.
- [x] Clarified that article cards are article-driven, not a four-symbol query roster. No returned article for another symbol is UNKNOWN, not CLEAR.
- [x] Added a separate four-symbol evidence table so every selected ticker shows Daily outcome, feed/completeness, Daily error, catalyst outcome, article count, catalyst coverage and catalyst error.
- [x] Operator reports BAYRY Daily API returned HTTP 502. Added bounded three-attempt retry for transient network/timeout/429/5xx responses and preserved provider HTTP status in exceptions. API maps provider 422 to 422, 429 to 503, and upstream 5xx to 502. Runtime retry behavior still needs operator verification.
- [x] Operator catalyst validation now shows raw 7, eligible 0, excluded 7, conflicts 0, non-evaluable 2, superseded 3, inadequate evidence 2. No classifier metrics exist; this is correct fail-closed behavior.
- [x] Latest EWTX assessment response establishes the actual triage ruleset as `MCC-CATALYST-TRIAGE-1.0` for the newly returned investor-conference article 117314249. It does not establish the ruleset stored with the older sale-exploration article 117008554.
- [ ] The original EWTX sale-exploration article still requires source-backed adjudication; do not reuse the new conference article's metadata/label.

## Overnight engineering audit — 2026-11-09

- [x] Reviewed current Boring Overnight API, history calculator, evaluator, calendar, Dashboard, source scanner CSV and V3 column requirements.
- [x] Confirmed the current 20-pair calculator computes prior regular-session close to next session 09:30 one-minute bar Open. It includes Day 1 after-hours and Day 2 premarket only in the *net endpoint interval*; it does not calculate the operator's actual buy-time entry or any path/extrema.
- [x] Confirmed the 09:30 one-minute Open is explicitly a proxy, not a verified official opening-auction print.
- [x] Confirmed the current calendar is an MCC-maintained holiday/early-close ruleset, not a live official exchange-calendar source.
- [x] Preserved the scanner CSV's custom-expression subscription-limit messages as missing data, not historical FAIL. Latest operator import: 495 rows, 185 PASS / 14 FAIL / 296 DATA?, exclusions 56 loaded / 0 matches.
- [ ] Exact intended Arizona-local buy-time and sell/measurement endpoints must be frozen against the original TOS scanner formula and operator's confirmed order workflow before replacing the current close/open research metric. The current calculator must not be relabelled as actual trade-window return.
- [ ] Official auction print vs first eligible observed trade remains unverified; Alpaca minute-bar Open cannot certify the auction print. Obtain an authoritative auction/eligible-trade source with entitlement and timestamp semantics.
- [ ] Complete independent RTO/EVR OHLCV parity; prior IEX comparison failed and IEX remains partial-exchange diagnostic data.
- [ ] Qualified independent Daily source remains open. Alpaca SIP entitlement and corporate-action/adjustment equivalence must be established; BAYRY 502 requires operator runtime retry/error verification.
- [ ] Catalyst representative dataset, reviewer agreement and classifier qualification remain open; do not synthesize labels.
- [ ] Opportunity Quality historical calibration remains open; no time-separated outcome-labelled candidate dataset is present in reviewed sources.
- [ ] Thinkorswim V3 Boring Overnight column/chart deliverable remains unimplemented/uncompiled. Do not claim it is done by writing a draft script without TOS compilation and parity.
- [ ] 5M/1M installed-study authority and EOD verification remain operator-side evidence gates.

- [x] Daily transient retry, per-symbol evidence table, and status mapping code/tests passed Release CI run [42659428224](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42659428224) on code commit `59719e6263e7e51a3eafaacf86a0507817f3e2ef`.

## Dashboard runtime follow-up

- [x] Added all-four-symbol live evidence display independently of the catalyst article cards.
- [x] Added transient retry and accurate upstream status mapping for Daily requests.
- [ ] Operator must pull latest branch, build/test, restart API, and repeat MCC Top 4 fetch; verify all four rows populate and inspect BAYRY outcome.
- [ ] Full browser/CDP acceptance, full Dashboard acceptance, regular-session WebSocket proof, collector recovery, and HAR performance measurement remain open.

---

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
- [x] Four-symbol evidence table implemented; [ ] verify MCC Top 4 selects/fetches four distinct tickers after latest pull; if the imported file has fewer than four unique symbols, show the source-data limitation.
- [x] Separate four-symbol evidence table implemented; [ ] verify the post-fetch status lists four distinct queried symbols and the evidence table shows each result. The human-review queue lists only symbols with returned articles, not every symbol queried.
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
