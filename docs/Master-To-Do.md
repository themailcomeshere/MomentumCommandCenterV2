# Momentum Command Center — Master To-Do

**Updated:** 2026-11-13 (afternoon) — repository work pass: capture-mapping tests, JSONL profiling, Opportunity Quality pool fixtures, field availability, launcher, backup/restore, release evidence.
**Working branch:** `feature/opportunity-quality-classification` (do not modify `master`)
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/` (PrototypeIndex is retired)
**Current handoff:** [NewChatHandoff-20261113.md](NewChatHandoff-20261113.md)
**Full dated history (Nov 6 – Nov 13 morning):** [Master-To-Do-History-20261113.md](archive/Master-To-Do-History-20261113.md) · older: [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)
**Detail:** [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md) · [P2/P3 Execution Plan](Engineering/P2-P3-Execution-Plan-20261110.md) · [Interactive Windows Operation](Engineering/Interactive-Windows-Operation.md) · [Boring Overnight Control Contract](Engineering/Boring-Overnight-Control-Contract-20261113.md) · [Provider Runtime Inventory](Engineering/Market-Data-Provider-Runtime-Inventory-20261113.md) · [Technical Workflow](Technical-Workflow.md) · [Runtime Acceptance Runbook](Runtime-Acceptance-Runbook.md) · [Catalyst Human Review](Catalyst-Human-Review-Workflow.md)

This is the authoritative checklist. Each open item appears once. Every item is tagged **[Repo]** (engineering we can do in the repository now) or **[Operator]** (needs operator, Thinkorswim, browser, provider or entitlement evidence — never mark complete from code/CI alone).

---

## Current verification state

- [x] `4c743a1`: operator Get-Latest + Release build + tests PASSED, 302/302; CI run `43493475779` SUCCESS.
- [x] `e7c0810` (latest code head of this pass): CI run `43504399098` SUCCESS (JS syntax, .NET build, tests). Local Release tests run by Claude: **344/344** passed.
- [ ] **[Operator]** Pull the latest head, run `.\scripts\Get-Latest-And-Test.ps1`, and record the SHA and totals. Expect 344+ tests.
- Browser, runtime, TOS compilation and data-source evidence remain separate gates (below).

## Priority directive (2026-11-11)

1. Performance diagnosis and JSONL cold-index optimization
2. Boring Overnight workflow and candidate evidence review
3. Opportunity Quality data-quality/coverage audit and deterministic fixtures
4. Thinkorswim study/formula parity using available operator exports
5. Runtime/browser/collector acceptance and interactive Windows deployment/recovery
6. Exact-head CI/release evidence for every code-bearing head

Schwab and broker reconciliation stay parked at the bottom.

---

## Next operator actions

The full, click-by-click sequence is in the [current handoff](NewChatHandoff-20261113.md).

1. Pull, then run `.\scripts\Get-Latest-And-Test.ps1`. Start MCC with the new launcher: `.\scripts\MCC.ps1 start`.
2. Run `.\scripts\MCC.ps1 status` and confirm the source SHA. Then run the first and second EVR live assessment and record the new JSONL timings: `jsonlIndexReadElapsedMs`, `jsonlIndexGateWaitBeforeLoadMs`, `jsonlIndexLastLoadElapsedMs`.
3. Hard-refresh the Dashboard (cache key `20261113-field-availability-pool-provenance-v2`). Import the scanner CSV, open **Field availability**, and export the JSON.
4. Run the Opportunity Quality ranking and confirm each card shows its source, feed and timestamps, and that any excluded symbol is BLOCKED.
5. During the regular session, run the CRBG capture and the live-assessment checks (Section 2).
6. Run `.\scripts\MCC.ps1 stop`, `.\scripts\Backup-MccData.ps1`, then a staged `.\scripts\Restore-MccData.ps1`.
7. Run `.\scripts\New-ReleaseEvidence.ps1` and commit the bundle it writes.

---

## 1. Performance — JSONL store

Profile on a copy of the operator's file (138.6 MB, 276,441 lines, 254,048 keys), isolated process, 2026-11-13: **~1.5 s total** (read 0.2 s, deserialize 1.1 s, index 0.2 s). The 23.3 s measured inside the API on 2026-11-12 therefore comes from startup contention, not file size. The old loop made one async thread-pool hop per 4 KiB buffer (~34k hops).

- [x] **[Repo]** Attribute the remaining cold-rebuild time: added `jsonlIndexReadElapsedMs`, `jsonlIndexOpenElapsedMs` and `jsonlIndexGateWaitBeforeLoadMs` to `performanceDiagnostics` (`8445c4e`).
- [x] **[Repo]** First cold-load optimization: a synchronous sequential read with 1 MiB buffers and no per-line allocations. Format, append semantics, corrections and latest-observation selection are unchanged; all store tests pass.
- [ ] **[Operator]** Fresh API restart, then the first EVR request: report total, read, deserialize, index and gate-wait times. The expected total is close to the ~1.5 s isolated figure. If it is still high and gate-wait is low, the remaining cost is CPU contention from other startup work (scanner discoveries, 167 MB). The next step would then be preloading the index in the background.
- [ ] **[Operator]** Measure warm reads and append/read-back persistence separately on the operator's current file.
- [ ] **[Operator]** Verify the 20s live-assessment timeout: a timed-out symbol does not block a later retry or another symbol.
- [ ] **[Operator]** Browser Network/HAR: document load, DOMContentLoaded, largest request, Fetch & Analyze durations against a baseline.

## 2. Boring Overnight workflow

### Capture history, current interval and technical inputs
- [x] **[Repo]** Current-interval state-transition tests: PENDING before the exact 09:30 bar completes, PENDING exactly one second before availability, OBSERVED at availability, MISSING for each missing-bar combination, and a nearest-bar substitution is rejected (`4ac53f7`).
- [x] **[Repo]** Capture-history technical mapping extracted to `BoringOvernightCaptureInputMapper` with 26 tests: full mapping, provider/feed mismatch, unknown feed, stale/unknown freshness, non-EVALUATED statuses, Daily range/ATR fail-closed rules. A JSON `null` feed label is now treated as missing (it was previously `""`).
- [ ] **[Operator]** Regular-session capture: fresh matched bars set `intradayBarsUsable=true`; V2 decision fields appear only when the status is `EVALUATED`.
- [ ] **[Operator]** Check the future-endpoint audit status in a capture made after pulling the boundary correction. The final NEXT_SESSION_OPEN row should read `PENDING_NOT_YET_AVAILABLE` before `endpointAvailableAtUtc`.
- [ ] **[Operator]** Browser: historical 20 pairs and current interval render as separate sections; technical provenance, matched-feed flag, raw bar-start timestamps, stale and mismatch states are visible.
- [ ] **[Operator]** Confirm the `Capture failed: calc is not defined` error is gone. The Nov 13 CRBG capture suggests it is, but nobody has confirmed it explicitly.
- [ ] **[Operator]** Re-run CRBG (or another symbol) with coverage that returns all 20 exact close/open boundaries. The current IEX result is 13/20.

### Workflow and UI contract
- [x] **[Repo]** Traced **Fetch & analyze bars**: `GET /api/market-data/bars` (appends bars to `market-bars.jsonl`), then automatically `GET /api/boring-overnight/snapshots`. It never creates a snapshot. See the [control contract](Engineering/Boring-Overnight-Control-Contract-20261113.md). Note: the default From of 13:00 AZ is 15:00 ET during standard time.
- [x] **[Repo]** Fixed a Dashboard startup `ReferenceError`. `liveAssessmentInFlight` was declared after the first `render()`, so the first Intraday assessment after every page load failed until the 15 s refresh (`b5a3283`).
- [ ] **[Operator]** Network check of the three controls as listed in the control contract (bars → snapshots; snapshots only; capture-history POST).
- [ ] **[Operator]** Intraday mode, hard refresh: the Main Chart cards fill on the first load instead of waiting ~15 s.
- [ ] **[Operator]** Full workflow acceptance: import row reconciliation, import placement, shortlist cards, top-20, four **distinct** Boring Top 4 Daily/catalyst requests with the per-symbol evidence table, and candidate review.
- [ ] **[Operator]** Calculated Limit Sell browser disclosure: Network shows `GET /api/providers/status` then `POST /api/boring-overnight/sell-limit`; every gate and stage timing is rendered in the quick card.
- [ ] **[Operator]** Evaluation clock: observe OPEN at the scheduled final-hour start and closed/countdown at the buy deadline.
- [ ] **[Operator]** Compare the displayed final-60-minute window, buy deadline and next-session GTC_EXT 7:00–20:00 ET window with the operator's actual order ticket (standard time, daylight time, early close).

### READY definition and V3 study (blocked on frozen semantics)
- [ ] **[Repo — blocked]** Wire a source-qualified Boring Overnight READY workflow and alert transition. This is blocked until the item below freezes the semantics. Never substitute Boring Winner PASS; no sound until READY is validated.
- [ ] **[Operator]** Freeze the Arizona-local buy time, the sell/measurement endpoint, and the choice between the official auction print and the first eligible trade, against the TOS scanner formula and the order workflow.
- [ ] **[Operator]** Decide whether official opening-auction data is required. If it is, obtain an entitled source.
- [ ] **[Repo — blocked]** Build the V3 Boring Overnight watchlist column/study after the semantics are frozen. Keep the Overnight Edge and Boringness scores separate; TOS compilation and parity are required.
- [ ] **[Operator]** Validate Boring Winner on chronological source-qualified samples and confirm the TOS Daily aggregation and extended-hours semantics. It remains RESEARCH ONLY.

## 3. Opportunity Quality

- [x] **[Repo]** Full-pool fixtures (`b5a3283`): 11 synthetic candidates pass through the real adapter → assessor → assessment JSON → mapper → ranker path. They cover every classification plus excluded, incomplete, stale and failed. Order stays identical across 20 shuffles, and every row carries provenance.
- [x] **[Repo]** Rank endpoint gaps fixed: exclusion-watchlist symbols were ignored and are now hard-BLOCKED with `EXCLUDED_WATCHLIST`. Each row carries provider, feed, 1m/5m source timestamps and `evaluatedAtUtc`. The response adds `excludedCount`, `dataUnknownCount` and `exclusionListChecked`. The Dashboard cards show the provenance.
- [x] **[Repo]** Scanner field-availability report: after each import or restore, the Dashboard shows available, missing and denominator for all eight fields, the rows with missing fields, the source file, the import timestamp, and a JSON export. A subscription-limit message, blank, `DATA?` or `n/a` counts as unavailable. Logic verified in the browser.
- [ ] **[Operator]** Import the current scanner CSV and export the availability JSON. This replaces the stale "296 missing rows" figure.
- [ ] **[Operator]** Run the ranking and confirm the provenance line on each card and BLOCKED for any excluded symbol.
- [ ] **[Repo — blocked]** Expand beyond the top-50 prefilter only after candidate-pool completeness is evidenced.
- [ ] **[Operator]** Collect time-separated candidate snapshots with outcomes for calibration. The ranking remains diagnostic and uncalibrated.

## 4. Candidate evidence review

- [ ] **[Operator]** Source-backed Daily/catalyst/price/volume/exclusion review for CRBG, FG, IBN, HDB. 20/20 FRESH on IEX is still `IEX_PARTIAL`; zero articles is UNKNOWN.
- [ ] **[Operator]** Human-review the CRBG Wells Fargo analyst-action article and INGM's two UNCLASSIFIED articles.
- [ ] **[Operator]** Reconcile MCC Top 4 against scanner-score ranking with candidate-specific evidence. HSAI catalyst and FB identity remain unverified.
- [ ] **[Operator]** BAYRY, LYSDY, BNPQY: keep qualification blocked (0/20 on IEX). No retrying unchanged IEX, no backfill.
- [ ] Top-10 scanner diagnoses (SMX, ADXN outliers etc.) remain research-only.

## 5. Thinkorswim parity

- [ ] **[Operator]** Compile the exact current ATR V2 / 5m / 1m studies in TOS and retain the compiler result and chart settings.
- [ ] **[Operator]** Import matched TOS Daily OHLC exports and compare with the scanner CSV. No dedicated Boring Winner Daily OHLC export study exists yet.
- [ ] **[Operator]** Reconcile the Daily range % and 14-session simple ATR % proxies against the exact TOS Daily study.
- [ ] **[Operator]** Independent 1m continuity (EVR 1m had 13 gaps); revalidate any new gaps against source evidence.
- [ ] **[Operator]** Verify that 5m Permission is authoritative and 1m is for timing only. Export matched completed-bar state labels.
- [ ] **[Operator]** Verify the Arizona-local EOD cutoff with bars before, at and after it (DST and standard time).

## 6. Data sources, parity and sell-limit gate

- [x] **[Repo]** Provider runtime inventory: wired endpoints, feeds, retry policy, runtime evidence and the gaps that block production-source selection. See [Market-Data-Provider-Runtime-Inventory-20261113.md](Engineering/Market-Data-Provider-Runtime-Inventory-20261113.md).
- [ ] **[Operator]** Find a genuinely independent, licensed Daily/OHLCV source, or keep qualification blocked. Recheck corporate-action adjustment first.
- [ ] **[Operator]** Obtain 119 exact provider/TOS 5m OHLCV matches plus the exact previous RTH close from the same qualified feed.
- [ ] **[Operator]** Runtime-qualify walk-forward only after a complete source-qualified 40-session dataset exists.
- [ ] **[Operator]** RKLX price-source acceptance against an independently observed quote.
- [ ] **[Operator]** Scanner symbols with zero IEX observations need an entitled source or a clearly labelled alternate feed.
- [ ] **[Operator]** Screenshot showing the Daily audit UI matches its API evidence.
- [ ] **[Operator]** Review the account's licensing, retention and runtime rate-limit terms. These were not found in the repository sources.
- The sell limit stays **fail-closed** until every gate passes. Latest CRBG (2026-11-12): only EXACT_REFERENCE_CLOSE passes.

## 7. Catalyst classifier

- [ ] **[Operator]** EWTX: open the original article (117008554), then append a source-backed correction that supersedes `f496393535df431ab275231b57f8530a` and `fbc839fc9c2740c1ad87e6bc641e2a77`. Re-query and expect conflict=0 and inadequateReviewEvidence=0.
- [ ] **[Operator]** Verify generic-rationale rejection at runtime on the latest build.
- [ ] **[Operator]** Keep ENOV/ARDT and other unsupported articles UNCERTAIN.
- [ ] **[Operator]** Build a larger source-backed sample across all four truth classes; double-review a subset; make sure both truth classes appear in the holdout.
- [ ] **[Repo — blocked]** Per-class precision/recall, FP/FN and coverage, once that sample exists.

## 8. Runtime, browser and interactive Windows deployment

Decision (2026-11-11): **interactive Windows only**. Details: [Interactive-Windows-Operation.md](Engineering/Interactive-Windows-Operation.md).

- [x] **[Repo]** Durable-path inventory: nine stores, their `MCC_*_PATH` overrides, secrets, browser storage keys and logs (`scripts/lib/MccDataInventory.ps1`).
- [x] **[Repo]** Launcher `scripts/MCC.ps1 start|status|stop|open`: single instance, visible startup failure, per-start log files, graceful stop through the token-guarded loopback `POST /api/admin/shutdown` (`LocalShutdownPolicy`, 8 tests). `/api/health` now reports PID, start time and `sourceSha`. Verified in an isolated PS 5.1 run.
- [x] **[Repo]** `Backup-MccData.ps1` / `Restore-MccData.ps1`: SHA-256 manifest; staged restore by default; `-ToLive` moves the live files aside first; secrets are never copied. Tamper detection and live restore were verified in a temp data root.
- [ ] **[Operator]** On the real machine: start, a refused second start, status, a graceful stop. Then sign out and back in, start again, and confirm the watermarks resume with no duplicates.
- [ ] **[Operator]** Back up the real data set (~315 MB) and do a staged restore.
- [ ] **[Operator]** Recovery drills: crash (kill the PID), sleep/wake, network/provider failure, disk full, duplicate stream replay, session boundaries.
- [ ] **[Operator]** Browser/CDP diagnostics on the operator machine: screenshot, console exceptions, sanitized network evidence.
- [ ] **[Operator]** Full Dashboard acceptance: import/parity, Red Zone, exclusions, Morning Brief, replay, sell-limit explanations, responsive layout.
- [ ] **[Operator]** WebSocket/collector coverage beyond the single 10-minute sample.

## 9. Release governance

- [x] **[Repo]** `scripts/New-ReleaseEvidence.ps1`: an immutable per-SHA bundle (`docs/Validation/Release-Evidence/<sha>.json/.md`) recording exact-head CI, local tests, the running API SHA, operator evidence files with their hashes, and blockers. A missing gate is reported as NOT_PROVIDED, never PASS. Verified against `e7c0810` (CI PASS, tests 344/344).
- [ ] **[Operator]** Generate and commit a bundle after each operator verification run, attaching the browser, TOS and backup evidence files.
- Standing rules: every code-bearing head needs passing CI. Local Release tests, the API runtime SHA, browser acceptance, TOS compilation and source/entitlement evidence are separate gates. Update this file and the handoff after each material change.

## 10. Parked (lowest priority — do not start)

- **SIP / GTC_EXT history:** HTTP 403 entitlement denial is established; the effective feed is IEX. No further SIP request unless the operator reports an entitlement change.
- **Schwab read-only API:** blocked on developer approval, app registration, callback and read-only scopes. Never order endpoints.
- **Broker execution reconciliation ledger:** only from real redacted broker order/fill records. Never infer fills from OHLCV.

---

## Guardrails (apply to every item)

- Keep Alpaca on IEX; IEX_PARTIAL is diagnostic only and never consolidated coverage.
- No imputation of missing bars, sessions, prices, timestamps, ATR observations, reference closes or catalyst facts; no nearest-bar substitution.
- Zero news is UNKNOWN, not CLEAR. MATERIAL_EVENT_REVIEW / UNCLASSIFIED map to RISK.
- Overnight 1M ATR14 is a 1-minute simple ATR, not daily ATR. Overnight RVOL is NOT USED.
- Boring Winner PASS and research defaults are research-only — never READY, a quote, or a trade instruction.
- MCC is decision-support-only; order submission is NOT IMPLEMENTED. The new shutdown endpoint only stops the local process.

## Recently completed (summary — details in commits and the archive)

- **Nov 13 afternoon:** `4ac53f7` capture mapper + 26 tests · `8445c4e` JSONL timing attribution and buffered read · `b5a3283` Opportunity Quality pool fixtures, exclusion blocking, provenance, field availability, startup ReferenceError fix · `e7c0810` launcher, graceful shutdown, inventory, backup/restore · docs: control contract, provider inventory, release evidence script.
- **Nov 13 morning:** after-hours technical gating confirmed on CRBG; boundary audit `PENDING_NOT_YET_AVAILABLE`; Overnight qualification hidden in Intraday mode.
- **Nov 12:** current Overnight interval separated from 20 historical pairs; technical-input wiring with feed-provenance guard and staleness withholding; capture `calc` fix; catalyst mapping; saved-evaluation recalculation; JSONL stage instrumentation measured.
- **Nov 10–11:** in-memory JSONL index (warm 12 s → 0.4 s); Calculated Limit Sell quick-action fix; Main Chart five metrics accepted; calendar-backed final-hour window; interactive-Windows decision.

## Uncertainties

- The ~1.5 s isolated cold load was measured on the operator's machine with a copy of the file. Whether the API's in-process cold load now matches depends on startup contention, which only an operator restart measurement can show.
- The `calc is not defined` browser fix is inferred, not explicitly confirmed.
- The launcher and backup were verified with isolated ports and temp data, not with the operator's Development secrets or the real 315 MB data set.
- No consolidated SIP history, persisted TOS ATR evidence or TOS compile record exists for any symbol.
