# Momentum Command Center — Master To-Do

**Consolidated:** 2026-11-13 — one deduplicated, prioritized list replacing the dated append-only sections.
**Working branch:** `feature/opportunity-quality-classification` (do not modify `master`)
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/` (PrototypeIndex is retired)
**Full dated history (Nov 6 – Nov 13):** [Master-To-Do-History-20261113.md](archive/Master-To-Do-History-20261113.md) · older: [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)
**Detail:** [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md) · [P2/P3 Execution Plan](Engineering/P2-P3-Execution-Plan-20261110.md) · [Technical Workflow](Technical-Workflow.md) · [Runtime Acceptance Runbook](Runtime-Acceptance-Runbook.md) · [Catalyst Human Review](Catalyst-Human-Review-Workflow.md)

This is the authoritative checklist. Each open item appears once; older duplicate wordings live in the archive. Every item is tagged **[Repo]** (engineering we can do in the repository now) or **[Operator]** (needs operator, Thinkorswim, browser, provider or entitlement evidence — never mark complete from code/CI alone).

---

## Current verification state

- [x] Exact head `4c743a159a9a944fc64524a745d8b3452e47cbc9`: operator Get-Latest + Release build + tests **PASSED** — 302 total / 302 passed / 0 failed / 0 skipped (tests 7.6s, build 8.0s).
- [x] Exact-head GitHub Actions run `43493475779` on `4c743a1`: **SUCCESS**.
- [x] This closes every earlier "run exact-head CI / Release build/tests" item for code through `4c743a1` (boundary-audit correction, mode isolation, technical staleness, feed-provenance guard, current-interval separation, capture `calc` fix, catalyst mapping, JSONL instrumentation).
- Browser, runtime, TOS compilation and data-source evidence remain separate gates (below).

## Priority directive (2026-11-11)

Work in this order; Schwab and broker reconciliation are parked at the bottom.

1. Performance diagnosis and JSONL cold-index optimization
2. Boring Overnight workflow and candidate evidence review
3. Opportunity Quality data-quality/coverage audit and deterministic fixtures
4. Thinkorswim study/formula parity using available operator exports
5. Runtime/browser/collector acceptance and interactive Windows deployment/recovery
6. Exact-head CI/release evidence for every code-bearing head

---

## Next operator actions

1. During a **regular session**, run a focused-symbol refresh (CRBG or another test symbol) and capture `GET /api/market-data/live-assessment?symbol=SYMBOL&providerName=Alpaca`. Report `intradayBarsUsable`, `v2DecisionUsable`, evaluation status, source timestamps, ages/limits and the five technical evaluator fields.
2. Run the Overnight 20-session capture and inspect the final NEXT_SESSION_OPEN row of the exact-boundary audit: expect `PENDING_NOT_YET_AVAILABLE` before `endpointAvailableAtUtc`; retain `auditStatus`, `endpointAvailableAtUtc`, `auditedAtUtc`.
3. Switch to Intraday Momentum and confirm the whole `BORING OVERNIGHT · QUALIFICATION` section is absent; switch back and confirm it returns.
4. Restart the API, then record the first and second EVR live-assessment `performanceDiagnostics` separately (cold vs warm).
5. Keep IEX qualification blocked. SIP entitlement/coverage and TOS parity are separate unresolved gates.

---

## 1. Performance — JSONL store

Latest measurement (2026-11-12, fresh process): 130.8 MB file, 261,010 lines, 243,384 keys; cold rebuild **23.3s** total (deserialize 4.8s, index insertion 1.1s — ~17.4s not yet attributed). Index built once per process; warm assessments 240–586 ms. The original ~51.8s latency is fixed for these samples only.

- [ ] **[Repo]** Attribute the remaining ~17.4s of cold rebuild (file I/O, line splitting, key construction, allocation, lock/contention) with finer stage timings.
- [ ] **[Repo]** Design a cold-load optimization only after that profile. Preserve append-only audit history, correction lineage, idempotent duplicates, provider/feed/timeframe isolation and exact latest-observation selection. No compaction, database migration or snapshot/cache format without a measured design plus migration/recovery tests.
- [ ] **[Operator]** Measure host-startup vs first-request cold load, warm reads and append/read-back persistence separately on the operator's current file.
- [ ] **[Operator]** Verify the 20s live-assessment timeout: a timed-out symbol does not block a later retry or another symbol, and the in-flight entry is removed. If timeouts recur, use `performanceDiagnostics`; the timeout is not a backend fix.
- [ ] **[Operator]** Browser Network/HAR: document load, DOMContentLoaded, largest request, Fetch & Analyze durations against a baseline.

## 2. Boring Overnight workflow

### Capture history, current interval and technical inputs
- [ ] **[Repo]** Unit tests for current-interval state transitions: PENDING before 09:31 ET, MISSING after endpoint when the exact open is absent, OBSERVED only with exact close and open. No nearest-bar substitution.
- [ ] **[Repo]** API-level regression tests for full capture-history technical mapping (5M permission/structure, 1M state, extension ATR, same-slot RVOL, per-field timestamps, stale/missing). Policy-level feed-mismatch/staleness tests already exist.
- [ ] **[Operator]** Regular-session capture: fresh matched bars set `intradayBarsUsable=true`; V2 decision fields only when status is `EVALUATED`. (After-hours nulling is already confirmed by the Nov 13 CRBG capture.)
- [ ] **[Operator]** Verify future-endpoint audit status in a capture made after pulling the boundary correction (see Next operator actions #2).
- [ ] **[Operator]** Browser: historical 20 pairs and current interval render as separate sections; technical provenance, matched-feed flag, raw bar-start timestamps, stale and mismatch states are visible.
- [ ] **[Operator]** Confirm the `Capture failed: calc is not defined` error is gone in the browser. The Nov 13 CRBG capture persisted a snapshot, which suggests it is fixed, but no explicit confirmation was recorded.
- [ ] **[Operator]** Re-run CRBG (or another symbol) with coverage that returns all 20 exact close/open boundaries; current IEX result is 13/20 with 7 exact opens missing.

### Workflow and UI contract
- [ ] **[Repo]** Trace and document the frontend handler/endpoint side effects of **Fetch & analyze bars** (persistence, whether it triggers Refresh saved evaluations, date-range defaults).
- [ ] **[Operator]** Single-symbol end-to-end run recording the Network sequence for all three actions, plus symbol, interval, From/To, provider/feed, returned/persisted bar counts, missing intervals, valid/missing pair counts, Daily/catalyst errors and snapshot as-of.
- [ ] **[Operator]** Refresh saved evaluations returns one recalculated evaluation per snapshot and the visible status matches the API.
- [ ] **[Operator]** Full workflow acceptance: import row reconciliation, import placement, shortlist cards, top-20, four **distinct** Boring Top 4 Daily/catalyst requests with the per-symbol evidence table (show the source limitation if fewer than four unique symbols), candidate review.
- [ ] **[Operator]** Calculated Limit Sell browser disclosure: Network shows `GET /api/providers/status` then `POST /api/boring-overnight/sell-limit`; every PASS/BLOCKED gate and stage timing is rendered in the quick card. (API path already returns the full BLOCKED gate set.)
- [ ] **[Operator]** Evaluation clock: observe OPEN at the scheduled final-hour start and closed/countdown at the buy deadline on the operator's machine.
- [ ] **[Operator]** Compare displayed final-60-minute window, buy deadline and next-session GTC_EXT 7:00–20:00 ET window to the operator's actual order ticket for a standard-time, daylight-time and early-close date. Do not infer fills or auction prints from bars.

### READY definition and V3 study (blocked on frozen semantics)
- [ ] **[Repo]** Wire a source-qualified Boring Overnight READY workflow and false→true alert transition. Never substitute Boring Winner PASS for READY; no sound until READY is validated.
- [ ] **[Operator]** Freeze the Arizona-local buy time, sell/measurement endpoint and official-auction vs first-eligible-trade semantics against the original TOS scanner formula and order workflow. The current close→09:30 1m Open metric is a proxy (`ONE_MINUTE_BAR_OPEN_PROXY`) and must not be relabelled as trade-window return.
- [ ] **[Operator]** Decide whether official opening-auction data is required; if so, obtain an entitled source with timestamp semantics.
- [ ] **[Repo]** After semantics are frozen: separate V3 Boring Overnight watchlist column/study, distinct from Opportunity Quality; keep Overnight Edge and Boringness scores separate and validate weights/gates against fixtures. TOS compilation and parity required before calling it done.
- [ ] **[Operator]** Validate Boring Winner on chronological source-qualified samples and confirm TOS daily aggregation / extended-hours semantics for `open / close[1] - 1`. Remains RESEARCH ONLY.

## 3. Opportunity Quality

- [ ] **[Repo]** Full-pool API/UI fixtures for complete, incomplete, stale, excluded and mixed candidate populations; assert deterministic ranks, DATA?/BLOCKED semantics and per-symbol provenance.
- [ ] **[Repo]** Scanner custom-field availability report with numerator, denominator, capture timestamp and source feed per symbol. The historical 296-missing-row count is stale.
- [ ] **[Repo]** Expand beyond the bounded top-50 prefilter only after candidate-pool completeness is evidenced.
- [ ] **[Operator]** Collect time-separated candidate snapshots with outcomes for threshold/rank calibration. Ranking is diagnostic and uncalibrated until then.

## 4. Candidate evidence review

- [ ] **[Operator]** Source-backed Daily/catalyst/price/volume/exclusion review for CRBG, FG, IBN, HDB. 20/20 FRESH on IEX is still `IEX_PARTIAL`; zero articles is UNKNOWN, not CLEAR.
- [ ] **[Operator]** Human-review the CRBG Wells Fargo analyst-action article (MATERIAL_EVENT_REVIEW → RISK) and INGM's two UNCLASSIFIED articles.
- [ ] **[Operator]** Reconcile MCC Top 4 against scanner-score ranking with candidate-specific evidence; do not replace MCC selection from research notes alone. HSAI catalyst and FB identity remain unverified.
- [ ] **[Operator]** BAYRY, LYSDY, BNPQY: keep qualification blocked (0/20 on IEX). Only a genuinely supported source with documented feed/adjustment/session provenance can unblock; no retrying unchanged IEX, no backfill.
- [ ] Top-10 scanner diagnoses (SMX, ADXN outliers etc.) remain research-only.

## 5. Thinkorswim parity

- [ ] **[Operator]** Compile the exact current ATR V2 / 5m / 1m studies in TOS; retain compiler result and chart settings (Wilder ATR14, Phoenix TZ, extended hours ON, adjustment ON, `MCC_OHLCV_ATR_V2.1`).
- [ ] **[Operator]** Import matched TOS Daily OHLC exports and compare to the scanner CSV (formula parity is implemented; per-symbol empirical parity is not shown). No dedicated Boring Winner Daily OHLC export study exists yet.
- [ ] **[Operator]** Reconcile Daily range % and 14-session simple ATR % proxies against the exact TOS Daily study (not Wilder parity today).
- [ ] **[Operator]** Independent 1m continuity (EVR 1m had 13 gaps); keep 1m separate from the 5m authority gate. Revalidate any new gaps against source/chart evidence — no blanket suppression.
- [ ] **[Operator]** Verify 5m Permission is authoritative and 1m is timing only; export matched completed-bar state labels (OHLCV rows cannot prove Permission/READY/RUNNER/WANING/SWING/GET OUT).
- [ ] **[Operator]** Verify the Arizona-local EOD cutoff with bars before/at/after, including DST and standard time.

## 6. Data sources, parity and sell-limit gate

- [ ] **[Operator]** Find a genuinely independent, licensed Daily/OHLCV source (or keep qualification blocked); recheck corporate-action adjustment equivalence first. Do not repeat the unchanged IEX comparison (RTO/EVR already failed).
- [ ] **[Operator]** 119 exact provider/TOS 5m OHLCV matches (including volume) plus exact previous RTH close from the same qualified feed.
- [ ] **[Operator]** Runtime-qualify walk-forward only after a complete source-qualified 40-session dataset exists; report train/test counts, exclusions and metrics.
- [ ] **[Operator]** RKLX price-source acceptance: compare Yahoo post-market reference and Alpaca latest IEX trade timestamps to an independently observed quote.
- [ ] **[Operator]** Scanner symbols with zero IEX observations need an entitled consolidated source or a clearly labelled alternate feed; never change the effective feed silently.
- [ ] **[Operator]** Screenshot showing the Daily audit UI matches its API evidence.
- [ ] **[Repo]** Document provider entitlement, scanner-universe coverage, licensing, rate limits, retention and symbol/session coverage before production-source selection (Massive is NOT_CONFIGURED).
- Sell limit stays **fail-closed** until SIP feed, 40/40 GTC_EXT windows, ATR settings/parity/99-bar warm-up, 119 OHLCV matches, current ATR freshness and walk-forward all pass. Latest CRBG (2026-11-12): only EXACT_REFERENCE_CLOSE passes.

## 7. Catalyst classifier

- [ ] **[Operator]** EWTX: open the original sale-exploration article (117008554), confirm publisher and the triage ruleset saved with the prediction, then append a source-backed correction superseding both weak active reviews `f496393535df431ab275231b57f8530a` and `fbc839fc9c2740c1ad87e6bc641e2a77`. Keep prediction `UNCLASSIFIED`/`[]`; re-query validation expecting conflict=0 and inadequateReviewEvidence=0.
- [ ] **[Operator]** Verify generic-rationale rejection at runtime on the latest build.
- [ ] **[Operator]** Keep ENOV/ARDT and other unsupported articles UNCERTAIN; never relabel to grow the sample.
- [ ] **[Operator]** Build a larger source-backed sample across MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT; double-review a subset for agreement; ensure both truth classes in the chronological holdout.
- [ ] **[Repo]** After the sample exists: per-class precision/recall, FP/FN, source and time-window coverage. No classifier promotion before then.

## 8. Runtime, browser and interactive Windows deployment

Decision (2026-11-11): **interactive Windows only** — MCC runs while the operator is logged in. No Windows Service, no unattended-after-sign-out mode.

- [ ] **[Repo]** Finish the durable-path inventory: exact Boring Overnight / Morning Brief filenames, remaining stores, logs, imported source files and all path/environment overrides.
- [ ] **[Repo]** User-session launcher: start/status/graceful-stop, browser launch, single-instance protection, visible startup failures (graceful-shutdown mechanism before the stop command).
- [ ] **[Repo]** After the inventory: data-root separation, manifest/hash-backed backup, staged restore with integrity checks, redacted rolling logs, readiness/health and collector-watermark monitoring.
- [ ] **[Operator]** Interactive start, second-start (single instance), graceful stop; sign-out stops the API; next start resumes collector watermarks without duplicates.
- [ ] **[Operator]** Recovery: crash/restart, sleep/wake, network/provider failure, disk-full/write failure, duplicate stream replay, session boundaries, backup during writes, restore to a separate location.
- [ ] **[Operator]** Browser/CDP diagnostics on the operator machine: screenshot, console exceptions, sanitized network evidence.
- [ ] **[Operator]** Full Dashboard acceptance: import/parity, Red Zone, exclusions panel, Morning Brief, replay, sell-limit explanations, responsive layout, console/network health.
- [ ] **[Operator]** WebSocket/collector coverage beyond the single 10-minute SPY/RKLX/ASTX/XRPN sample.

## 9. Release governance

- [ ] **[Repo]** Immutable per-release evidence bundle: exact SHA, workflow URL/status, local test totals, runtime SHA, browser diagnostics, TOS compile record, data provenance, backup/restore status, blockers.
- Standing rules: every code-bearing head needs passing CI; operator local Release build/tests, API runtime SHA, browser acceptance, TOS compilation and source/entitlement evidence are separate gates. Update this file and the current handoff after each material change.

## 10. Parked (lowest priority — do not start)

- **SIP / GTC_EXT history:** HTTP 403 entitlement denial established; effective feed is IEX. No further SIP request, entitlement check or config change unless the operator reports a genuine entitlement change. If it changes: 40 complete sell sessions (20 train + 20 test), 780 exact 1m bar starts each, exact previous RTH close, then walk-forward.
- **Schwab read-only API:** blocked on developer approval, app registration, callback and read-only scopes. Do not request exports or begin OAuth work. Never order endpoints.
- **Broker execution reconciliation ledger:** only from real redacted broker order/fill records (partial fills, fees, cancel/correction lineage, unmatched exceptions). Never infer fills from OHLCV.

---

## Guardrails (apply to every item)

- Keep Alpaca on IEX; IEX_PARTIAL is diagnostic only and never consolidated coverage.
- No imputation of missing bars, sessions, prices, timestamps, ATR observations, reference closes or catalyst facts; no nearest-bar substitution.
- Zero news is UNKNOWN, not CLEAR. MATERIAL_EVENT_REVIEW / UNCLASSIFIED map to RISK.
- Overnight 1M ATR14 is a 1-minute simple ATR, not daily ATR. Overnight RVOL is NOT USED.
- Boring Winner PASS and research defaults (e.g. 50th-percentile quantile) are research-only — never READY, a quote, or a trade instruction.
- MCC is decision-support-only; order submission is NOT IMPLEMENTED.

## Recently completed (summary — details in the archive)

- **Nov 13:** after-hours technical gating confirmed on CRBG; boundary audit `PENDING_NOT_YET_AVAILABLE` until endpoint availability with `endpointAvailableAtUtc`/`auditedAtUtc`; Overnight qualification hidden in Intraday mode; operator capture instructions source-verified.
- **Nov 12:** current Overnight interval separated from 20 historical pairs; source-qualified technical-input wiring with strict feed-provenance guard and staleness withholding; capture `calc` fix; catalyst status mapping and DataFresh fix; saved-evaluation recalculation; JSONL stage instrumentation measured.
- **Nov 10–11:** in-memory JSONL index (warm assessment 12s → 0.4s); Calculated Limit Sell quick-action fix; Boring Overnight Main Chart five metrics accepted; calendar-backed final-hour window; Daily parser/retry; Boring Overnight shortlist and Top 4; interactive-Windows deployment decision.

## Uncertainties

- ~17.4s of the 23.3s cold rebuild is not yet attributed to a measured stage.
- The `calc is not defined` browser fix is inferred, not explicitly confirmed.
- No consolidated SIP history, persisted TOS ATR evidence or TOS compile record exists for any symbol.
- Fetch & analyze bars endpoint side effects are not yet documented from source.
