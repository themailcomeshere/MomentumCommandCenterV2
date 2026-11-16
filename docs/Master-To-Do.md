# Momentum Command Center — Master To-Do

**Updated:** 2026-11-16. Automatic scanner on free data (§2b). Red Zone state colours and alerts added (§2b). Operator walkthrough: the intraday workflow is the daily core. Added step numbers to the Dashboard, colour-coded ranking reasons, and a numbered workflow map with gap analysis (§2b). Boring Overnight READY runs in shadow mode.
**Working branch:** `feature/opportunity-quality-classification` (do not modify `master`)
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/` (PrototypeIndex is retired)
**Current handoff:** [NewChatHandoff-20261114.md](NewChatHandoff-20261114.md)
**Full dated history (Nov 6 – Nov 13 morning):** [Master-To-Do-History-20261113.md](archive/Master-To-Do-History-20261113.md) · older: [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)
**Detail:** [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md) · [P2/P3 Execution Plan](Engineering/P2-P3-Execution-Plan-20261110.md) · [Interactive Windows Operation](Engineering/Interactive-Windows-Operation.md) · [Boring Overnight Control Contract](Engineering/Boring-Overnight-Control-Contract-20261113.md) · [Provider Runtime Inventory](Engineering/Market-Data-Provider-Runtime-Inventory-20261113.md) · [Technical Workflow](Technical-Workflow.md) · [Runtime Acceptance Runbook](Runtime-Acceptance-Runbook.md) · [Catalyst Human Review](Catalyst-Human-Review-Workflow.md)

This is the authoritative checklist. Each open item appears once. Every item is tagged **[Repo]** (engineering we can do in the repository now) or **[Operator]** (needs operator, Thinkorswim, browser, provider or entitlement evidence — never mark complete from code/CI alone).

---

## Current verification state

- [x] `4c743a1`: operator Get-Latest + Release build + tests PASSED, 302/302; CI run `43493475779` SUCCESS.
- [x] `e7c0810`: CI run `43504399098` SUCCESS (JS syntax, .NET build, tests). Local Release tests run by Claude: 344/344 passed.
- [x] `d6d52ab` (JSONL startup warm-up): local Release tests run by Claude: 345/345 passed. Exact-head CI run `43551329653` SUCCESS.
- [x] `6a547c3` (DATA? reasons + `MCC.ps1 warmup`): local Release tests run by Claude: 351/351 passed. Exact-head CI run `43630951781` SUCCESS.
- [x] `5f1d5d1` (delayed-SIP Daily + Massive cross-check): local 362/362. Exact-head CI run `43634828257` SUCCESS.
- [x] `0596df6` (catalyst review queue): local 363/363. Exact-head CI run `43635497154` SUCCESS.
- [x] `e48b4d2` (delayed-SIP 1m for GTC_EXT history): local 366/366. Exact-head CI run `43636134528` SUCCESS.
- [x] `0b4fbc1` (NO_TRADE minutes): local 369/369. Exact-head CI run `43636822164` SUCCESS.
- [x] `b3ca453` (Daily tolerance): local 370/370. Exact-head CI run `43642242199` SUCCESS.
- [x] `02e88a3` (READY backend `8365f7e` + Dashboard panel; ATR informational `4eafc9b`): local 392/392. Exact-head CI run `43644529920` SUCCESS.
- [x] `6fe6446` (latest code head, close/open history on SIP): local Release tests run by Claude: **398/398** passed. Exact-head CI run `43646185327` SUCCESS.
- [ ] **[Operator]** Pull the latest head, run `.\scripts\Get-Latest-And-Test.ps1`, and record the SHA and totals. Expect 398+ tests.
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

The full, click-by-click sequence is in the [current handoff](NewChatHandoff-20261114.md).

**Weekend (market closed; all of these work after hours):**
1. Answer the five open READY questions in [READY semantics](Engineering/Boring-Overnight-READY-Semantics-20261114.md).
2. Pull, run `.\scripts\Get-Latest-And-Test.ps1`, then `.\scripts\MCC.ps1 start`, `start` again (refused), `status`, and about 15 s later `.\scripts\MCC.ps1 warmup`.
3. Dashboard (Ctrl+F5). The **Rank quality pool · top 50** button exists **only in INTRADAY MOMENTUM** (it is hidden in Boring Overnight, `styles.css:1728`), so switch the WORKSPACE first. Run the ranking (after hours every row is DATA? with `DataStale, OutsideRegularSession`); do the exclusion test; Field availability export (Boring Overnight); Network order.
4. Daily audit: confirm the panel now shows `Alpaca / sip · delayed historical SIP` and `QUALIFICATION USABLE` for RTO, EVR, CRBG.
5. Catalyst batch 1: label the 60-row sheet `Documents\MCC-Reviews\catalyst-queue-20261114-200253.csv`, dry-run, then `-Submit` ([sample plan](Validation/Catalyst-Sample-Plan-20261114.md)).
6. Optional: create a free Massive account, add the key to user secrets, and run `GET /api/market-data/daily/cross-check?symbol=RTO`.
7. `MCC.ps1 stop`, `Backup-MccData.ps1`, staged `Restore-MccData.ps1`, sign-out test, then `New-ReleaseEvidence.ps1` and commit the bundle.

**Monday regular session (07:30–14:00 Arizona in standard time; the buy window is 13:00–14:00):**
8. Ranking again (Intraday Momentum): rows should classify instead of DATA?.
9. CRBG live assessment and **Fetch / retry 20-session** capture (Section 2), including the capture-history POST Network check.

---

## 1. Performance — JSONL store

Profile on a copy of the operator's file (138.6 MB, 276,441 lines, 254,048 keys), isolated process, 2026-11-13: **~1.5 s total** (read 0.2 s, deserialize 1.1 s, index 0.2 s). The 23.3 s measured inside the API on 2026-11-12 therefore comes from startup contention, not file size. The old loop made one async thread-pool hop per 4 KiB buffer (~34k hops).

- [x] **[Repo]** Attribute the remaining cold-rebuild time: added `jsonlIndexReadElapsedMs`, `jsonlIndexOpenElapsedMs` and `jsonlIndexGateWaitBeforeLoadMs` to `performanceDiagnostics` (`8445c4e`).
- [x] **[Repo]** First cold-load optimization: a synchronous sequential read with 1 MiB buffers and no per-line allocations. Format, append semantics, corrections and latest-observation selection are unchanged; all store tests pass.
- [x] **[Operator]** Restart measurement on `8445c4e` or later (2026-11-13): cold rebuild **7.3 s** (`jsonlIndexLastLoadElapsedMs` 7306.1; read 900.4; deserialize 5367.8; gate wait 0), down from 23.3 s. Deserialization dominates, about 2.5× slower than in an isolated process, which points to CPU contention from the other startup workers.
- [x] **[Repo]** Tested a source-generated JSON context on the real file: 1.9 s vs 2.0 s with identical results, so no meaningful gain and not adopted. Added `JsonlMarketBarIndexWarmupService` (`d6d52ab`): the index now builds in the background at API startup, so a Dashboard request no longer pays for it. `performanceDiagnostics` adds `jsonlIndexLoadTrigger` (`STARTUP_WARMUP` / `READ` / `APPEND`) and `jsonlIndexLoadCompletedAtUtc`. Isolated in-API run on a copy of the real file: `STARTUP_WARMUP`, 2.4 s, 278,445 lines.
- [ ] **[Operator]** After the next restart, confirm `jsonlIndexLoadTrigger = STARTUP_WARMUP` on the first EVR request and that the API log contains `JSONL market-bar index ready`. Record `totalElapsedMs` for the first request; it should be well under 1 s if the warm-up finished first. Run `.\scripts\MCC.ps1 warmup`. Pre-check by Claude on the operator machine, 2026-11-14, launcher start on `a52fe97`+DIRTY (the `6a547c3` changes), Development secrets, real file: `trigger STARTUP_WARMUP, total 2230 ms (read 215, deserialize 1740, index 262), 347524 lines, 325119 keys`; first EVR request `totalElapsedMs` 387.2, load count 1, gate wait 0.1 ms.
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
- [ ] **[Operator]** Network check of the three controls as listed in the control contract (bars → snapshots; snapshots only; capture-history POST). Pre-check by Claude in the app browser pane, 2026-11-14, symbol RTO: **Fetch & analyze bars** → `GET bars?symbol=RTO…` first, `GET snapshots?limit=50&symbol=RTO` last (background `clock`, `live-assessment`, `stream/status` in between); **Refresh saved evaluations** → only `GET snapshots`. No console errors. The capture-history POST was not exercised, to avoid writing an after-hours capture into the evidence store.
- [ ] **[Operator]** Intraday mode, hard refresh: the Main Chart cards fill on the first load instead of waiting ~15 s.
- [ ] **[Operator]** Full workflow acceptance: import row reconciliation, import placement, shortlist cards, top-20, four **distinct** Boring Top 4 Daily/catalyst requests with the per-symbol evidence table, and candidate review.
- [ ] **[Operator]** Calculated Limit Sell browser disclosure: Network shows `GET /api/providers/status` then `POST /api/boring-overnight/sell-limit`; every gate and stage timing is rendered in the quick card.
- [ ] **[Operator]** Evaluation clock: observe OPEN at the scheduled final-hour start and closed/countdown at the buy deadline.
- [ ] **[Operator]** Compare the displayed final-60-minute window, buy deadline and next-session GTC_EXT 7:00–20:00 ET window with the operator's actual order ticket (standard time, daylight time, early close).

### READY definition and V3 study
Decision record: [Boring-Overnight-READY-Semantics-20261114.md](Engineering/Boring-Overnight-READY-Semantics-20261114.md).
- [x] **[Operator]** Freeze buy time and exit (2026-11-14): buy **any time in the final regular hour** (15:00–16:00 ET; 13:00–14:00 AZ in standard time; the hour before an early close). Exit is a **GTC_EXT limit sell, Day 2 07:00–20:00 ET**; a touch is not a fill. `MarketSessionClock` already implements both windows.
- [x] **[Operator]** Official opening-auction data: **not required** (2026-11-14). The exit is a limit in the extended window; the 09:30 close/open capture stays a separately named research series.
- [x] **[Operator]** Historical SIP minute bars for the 40-window sell history: **allowed** (2026-11-14). Implemented in `e48b4d2`.
- [x] **[Operator]** No-trade minutes: **NO_TRADE accepted with safeguards** (2026-11-14, Claude's recommendation). Implemented in `0b4fbc1`.
- [x] **[Operator]** Remaining questions answered (2026-11-14), recorded as decisions 6–9:
  - Unfilled limit: the GTC_EXT order carries over; measurement stays Day 2 only.
  - Catalyst: an operator attestation counts, under rules; zero news is never CLEAR.
  - Daily tolerance: ±$0.01 / ±0.5%.
  - READY: a clear go signal; size stays the operator's decision.
- [x] **[Operator]** READY rules confirmed (2026-11-14, Claude's recommendations): earnings/guidance and FDA/clinical events block, while M&A, contract and analyst events pass with sign-off plus a warning; shadow mode is open-ended with a minimum of 10 sessions and the operator switches it live; sign-off by ticking sources; Thinkorswim ATR gates informational.
- [x] **[Repo]** READY built:
  - `8365f7e`: `BoringOvernightReadyEvaluator`, `ReadyCatalystGate`, `CatalystAttestationStore`, `ReadyShadowLogStore` and summary; endpoints `ready/evaluate`, `ready/shadow-log`, `catalyst-attestations`; 21 tests.
  - `02e88a3`: Dashboard **READY check** panel in the Overnight workspace.
  - The limit is the latest trade (at most 2 min old) × the tested target %, because the day's close does not exist during the buy window.
  - Browser check, 2026-11-14 (Saturday), RTO and CRBG: BUY_WINDOW NOT_YET, QUALIFICATION DATA?, catalyst needs sign-off, sell limit walk-forward FAIL; no console errors.
- [x] **[Operator]** Close/open history on delayed SIP: **approved** 2026-11-14.
- [x] **[Repo]** `6fe6446`: capture-history uses delayed SIP for the 20 pairs, while live 1M/5M stays on IEX and is checked against that feed. The current interval is judged as of now − 16 min. Bug fix: `currentOvernightPct` was never filled (it is now the latest completed pair's return). 6 tests. Runtime 2026-11-14: RTO 20/20 (win rate 60%, 20D +0.44%, 5D +0.12%) and CRBG 20/20 (80%, +0.72%, +0.68%). Both fail "5D > 20D"; the remaining DATA? inputs are live-session fields only.
- [ ] **[Operator]** In-session READY run (Monday 13:00–14:00 AZ): check READY for your candidates, sign off the catalyst check, and confirm the cards. Shadow sessions start counting from that day.
- [ ] **[Repo]** Build the V3 Boring Overnight watchlist column/study. Keep the Overnight Edge and Boringness scores separate; TOS compilation and parity are required. Keep the Overnight Edge and Boringness scores separate; TOS compilation and parity are required.
- [ ] **[Operator]** Validate Boring Winner on chronological source-qualified samples and confirm the TOS Daily aggregation and extended-hours semantics. It remains RESEARCH ONLY.

## 2a. Repository location (moved 2026-11-16)

- [x] **[Repo]** Copied the repository to `D:\source\repos\MomentumCommandCenterV2` (bin/obj excluded) and `MCC-Backups` (3 backups, 1.93 GB) and `MCC-Reviews` to `D:\source\repos\`. SHA-256 check: 1,600 repo files, 51 backup files and 1 review file identical. Same HEAD `1e48dd7`, clean status, `git fsck` OK, 423/423 tests from D:. Not copied: 5 locked Visual Studio index files in `.vs` (rebuilt by Visual Studio). `Backup-MccData.ps1` and `New-CatalystReviewQueue.ps1` now default to the `MCC-Backups` / `MCC-Reviews` folders next to the repository folder.
- [x] **[Operator]** D: tested by the operator 2026-11-16 ("D: works"); Visual Studio now has the D: solution open.
- [x] **[Repo]** 2026-11-16: `C:\Users\jampo\OneDrive\Documentos\MCC-Backups` and `\MCC-Reviews` sent to the Recycle Bin (operator approval). Not permanently deleted; OneDrive keeps its own recycle-bin copy.
- [ ] **[Operator]** Remove `C:\Users\jampo\source\repos\MomentumCommandCenterV2`: the Recycle Bin move failed with "in use" because the Claude session working in that folder holds it open (about 4 files had already gone to the Recycle Bin). Close that session, then delete the folder in File Explorer (it goes to the Recycle Bin). Empty the Recycle Bin to free the space on C:.

## 2b. Intraday workflow alignment (operator walkthrough 2026-11-16)

Numbered map and gap analysis: [MCC-Workflow-Steps.md](Engineering/MCC-Workflow-Steps.md).
- [x] **[Repo]** (`6b74cdf`, CI `43889621585` SUCCESS, 398/398) Step numbers on the Dashboard for both modes (INTRADAY 0–7, OVERNIGHT 0–6). They are `data-step-*` attributes; each mode shows only its own badges. Verified in the browser pane (static preview): 18 intraday and 21 overnight badges render, in page order.
- [x] **[Repo]** (`037a9ce`, CI `43894502837` SUCCESS, 398/398) TOS CSV import available in INTRADAY MOMENTUM (2026-11-16, operator choice). It is one shared **Import a Thinkorswim CSV** bar (Intraday 2.a / Overnight 1); the latest import feeds the Red Zone, ranking pool, shortlist and Daily audit. **Bug fixed:** the Intraday Red Zone filtered imports with the Boring Overnight rules, so a normal watchlist gave 0 cards; it now takes the first four by Score, or in TOS watchlist order. Browser-pane check with a synthetic 5-row watchlist: Red Zone shows the first 4 in order, Overnight still shows its own shortlist rules.
- [x] **[Repo]** Both layouts reordered by step number with per-mode CSS `order` (2026-11-16). Intraday reads 1.2 → 2.a → 2.b–2.c (Discovery tools, collapsed) → 3 → 4 Red Zone → 5 → 7. Overnight reads 1 → 2.1–2.6 → 3 → 4 READY → 6. Intraday-only panels are hidden in Overnight. **Also fixed:** the Red Zone kept the Overnight heading after switching back to Intraday. Browser-pane check: block and badge order correct in both modes, no script errors.
- [x] **[Repo]** (`8a27531`, CI `43909053318` SUCCESS, 410/410) Red Zone state colours matching the installed TOS study labels: WAIT grey, PREPARE TO BUY yellow, EXECUTION READY green, RUNNER green, RUNNER WANING / PREPARE TO SELL yellow, SELL / GET OUT red, DATA? dashed. Each card has a coloured state banner with the step 6.a/6.b/6.c hint, a legend, and a transition log in AZ time. Distinct sounds for PREPARE TO BUY, EXECUTION READY, RUNNER WANING / PREPARE TO SELL and SELL / GET OUT. No sound on first load or when leaving DATA?. **Found while building:** the live engine always ran with `hasPosition=false`, so sell-side states could never appear. Fix: the assessment also returns `heldPositionDecision`, used when the operator clicks **Mark holding** on a card (browser only; no broker data). SWING RECLAIM / SWING ZONE (orange) and EXECUTION EXPIRED have no equivalent in the V2 engine and are not shown. Browser-pane check with a stubbed API: colours, the HOLDING switch and 3 transitions with the right sounds; no script errors.
- [ ] **[Operator]** Live check of the state colours during the regular session (07:30–14:00 AZ, standard time): compare at least one card's MCC state with the TOS 1M/5M labels on the same symbol, and test **Mark holding** on a position you hold.
- [x] **[Repo]** (`a942074`, CI `43913313796` SUCCESS, 423/423) Automatic scanner on free data (operator decision 2026-11-16: build free first, compare with TOS). Runs scans 1, 2A, 2B, Momentum Pivot and D as written, every 2 min during the session. Universe: Alpaca most-actives (volume, trades) + top gainers. Prices: IEX. Prior close: delayed SIP Daily. 5m conditions: IEX bars; volume conditions labelled approximate. Dashboard panel at Intraday **2.a**, with a **Feed Red Zone** toggle and a TOS side-by-side comparison recorded on each Intraday TOS import. Design and limits: [MCC-Auto-Scanner-20261116.md](Engineering/MCC-Auto-Scanner-20261116.md). Browser-pane check with a stubbed API: table, Red Zone feed, comparison post, import takes the Red Zone back, panel hidden in Overnight; no script errors. **Not run against live Alpaca data.**
- [ ] **[Operator]** First live session: confirm the panel fills at about 07:35 AZ (standard time), with no FAILED source under **Sources, feeds and limits** (most-actives on the free plan is unconfirmed). Confirm Scanner 1's timeframe (daily or 5m).
- [ ] **[Operator]** Side-by-side: import your TOS watchlist in INTRADAY MOMENTUM once per session for 5 sessions; MCC records the overlap automatically.
- [ ] **[Operator]** After 5 sessions, decide: stay free, buy live consolidated data (Alpaca Algo Trader Plus $99/month or Massive Stocks Advanced $199/month, prices checked 2026-11-14), and/or approve the proposed rule improvements (merge scans, same-time-of-day RVOL, time-adjusted volume, unified thresholds).

## 3. Opportunity Quality

- [x] **[Repo]** Full-pool fixtures (`b5a3283`): 11 synthetic candidates pass through the real adapter → assessor → assessment JSON → mapper → ranker path. They cover every classification plus excluded, incomplete, stale and failed. Order stays identical across 20 shuffles, and every row carries provenance.
- [x] **[Repo]** Rank endpoint gaps fixed: exclusion-watchlist symbols were ignored and are now hard-BLOCKED with `EXCLUDED_WATCHLIST`. Each row carries provider, feed, 1m/5m source timestamps and `evaluatedAtUtc`. The response adds `excludedCount`, `dataUnknownCount` and `exclusionListChecked`. The Dashboard cards show the provenance.
- [x] **[Repo]** Scanner field-availability report: after each import or restore, the Dashboard shows available, missing and denominator for all eight fields, the rows with missing fields, the source file, the import timestamp, and a JSON export. A subscription-limit message, blank, `DATA?` or `n/a` counts as unavailable. Logic verified in the browser.
- [ ] **[Operator]** Import the current scanner CSV and export the availability JSON. This replaces the stale "296 missing rows" figure.
- [x] **[Repo]** DATA? rows no longer say `DataMissing` for every cause (`6a547c3`). `OpportunityQualityUnavailableReasons` maps `STALE_MARKET_DATA` → `DataStale`, adds the new `OutsideRegularSession` reason outside regular hours, and keeps `DataMissing` for insufficient history, missing RVOL baseline and unknown statuses. 6 tests.
- [ ] **[Operator]** Run the ranking and confirm the provenance line on each card and BLOCKED for any excluded symbol. The **Rank quality pool · top 50** button is in the Red Zone header **in INTRADAY MOMENTUM mode only**; it is hidden in Boring Overnight (`styles.css:1728`). On 2026-11-14 the operator could not find it because the handoff had them switch to Boring Overnight first. Results appear directly under it. The exclusion list is the collapsible **EXCLUSIONS · API-BACKED — MCC exclusion watchlist** panel lower on the page. Operator run 2026-11-14 03:46 UTC (Friday after the close, JSON pasted): 50 candidates, `exclusionListChecked` true, 0 excluded, 50 DATA? (`dataFresh` false; last 1m bar 20:59 UTC). 48 rows carry `Alpaca / iex` and 1m/5m timestamps; SBGSY and SMERY have no feed or timestamps (no IEX bars). Pre-check by Claude on `6a547c3` code, 2026-11-14: pool CRBG, EVR, GGAL, SBGSY → GGAL (already on the list) `BLOCKED`, `QUALITY — EXCLUDED`, reasons `EXCLUDED_WATCHLIST, DataStale, OutsideRegularSession`; CRBG/EVR `DataStale, OutsideRegularSession`; SBGSY `DataMissing, OutsideRegularSession`. **Operator exclusion test (reported 2026-11-16):** the added symbol's reasons included `EXCLUDED_WATCHLIST`; after removing it and re-ranking, that reason was gone. A regular-session run is still needed for non-DATA? classifications.
- [x] **[Repo]** Colour-coded reason chips on ranking cards (2026-11-16, operator request): red = blocks (`EXCLUDED_WATCHLIST`, `EntryNotEligible`), grey = data missing/stale/after hours, amber = caution, green = supportive, plus a legend; the card title is coloured by status. Tone mapping and computed colours checked in the browser pane.
- [ ] **[Operator]** Confirm the colours on a live ranking (INTRADAY MOMENTUM → Red Zone → **Rank quality pool · top 50**).
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
- [x] **[Repo]** Daily source path (operator decision 2026-11-14: delayed historical SIP for Daily, plus Massive free as cross-check). `MarketData:Alpaca:DailyFeed=sip_delayed` requests completed Daily bars on SIP with end ≥ 16 min old. A SIP refusal falls back to IEX, is labelled (`sipRequestStatus`) and is never usable. Also new: `MassiveDailyMarketDataClient`, `DailySourceCrossCheck` and `GET /api/market-data/daily/cross-check` (both split-adjusted). The Daily audit shows the SIP status (`5f1d5d1`, 11 tests, CI `43634828257`).
- [x] **[Repo]** Runtime evidence (Claude, operator machine, 2026-11-14): RTO, EVR, CRBG → `feed=sip`, `CONSOLIDATED_SIP`, `SIP_DELAYED_HISTORICAL_OK`, 20/20, fresh, `qualificationUsable=true`, on the free account. Against the independent S&P Global/Stock Analysis reference (15 overlapping sessions), volume ratio is 1.0000–1.0011, O/H/L within 0.005, and 14/15 closes exact. 2026-11-06 closes differ (RTO −0.02, EVR +0.18); that reference row was captured 53 min after that close. Evidence: `artifacts/daily-data-qualification/20261114T195621Z-sip-delayed/`.
- [x] **[Repo]** Delayed historical SIP 1m bars for the GTC_EXT sell-window history (`e48b4d2`, 3 tests). `AllowDelayedHistoricalSipMinutes=true`; every request ends ≥ 16 min before now, and anything newer is refused without a request. The sell-window capture uses it for `feed=sip` on an IEX configuration. `/api/providers/status` exposes `delayedHistoricalSipMinutes`; the Dashboard capture and sell-limit controls use SIP when it is enabled.
- [x] **[Repo]** Runtime (Claude, operator machine, 2026-11-14): the RTO capture fetched and persisted 15,201 SIP 1m bars in 3.8 s, `CONSOLIDATED_SIP`, with reference close present for all 40 windows. **0/40 complete windows**: only 302–424 of 780 minutes per window are present. On 2026-11-12, regular hours were nearly full, but 07h–08h had 5 bars, 16h had 1 and 17h–19h had 0. Alpaca omits minutes without trades. Evidence: `artifacts/gtc-ext-sell-window/20261114T201053Z-sip-delayed/`.
- [x] **[Repo]** NO_TRADE classification (`0b4fbc1`, operator decision 2026-11-14). On consolidated SIP, a minute with no bar is `NO_TRADE`: counted, never priced. It applies only when the window's exact reference close is present and the sell session's regular session has bars; otherwise the minute stays `MISSING`. IEX empty minutes are always `MISSING`. Each window reports `noTradeIntervalCount` and `noTradeRule`; complete means zero MISSING. 4 tests.
- [x] **[Repo]** Runtime (Claude, operator machine, 2026-11-14): RTO re-audit **40/40 complete**, qualification usable. 14,910 minutes with a bar, 16,290 NO_TRADE, 0 MISSING; the Dashboard capture button shows the same. Sell limit at P50 is still **BLOCKED**. Passing: SIP, 40/40 history, reference close 23.90. Failing: the TOS ATR gates (no Thinkorswim ATR observations saved) and walk-forward adverse excursion (unseen max 5.85% vs training P95 3.36%). Touch rate passed: target +0.77%, test 65% (13/20) vs training 55%. Research only.
- [x] **[Operator]** TOS ATR is no longer required for the sell limit (2026-11-14, `4eafc9b`); it stays as informational context. Still required: the 40-window SIP history and the walk-forward gates. RTO and CRBG currently fail walk-forward.
- [x] **[Operator]** Daily parity tolerance: **±$0.01 per OHLC price, ±0.5% volume** (2026-11-14, Claude's recommendation). Applied in `DailySourceCrossCheck` and `Test-DailyMarketDataQualification.ps1`. Fresh reference download, RTO and EVR, 2026-11-03 … 11-13 (18 sessions): prices within $0.005 and volume within 0.021%, so all pass. The old 2026-11-06 close difference is gone; the old reference row was captured before the final close.
- [ ] **[Operator]** Optional cross-check: create a free Massive account (EOD only, 5 calls/min, 2 years), set `MarketData:Massive:ApiKey` in user secrets, run `/api/market-data/daily/cross-check?symbol=RTO` and report the verdict.
- [ ] **[Operator]** Review Alpaca's market-data terms for storing and displaying historical SIP Daily bars on the free plan (not found in repository sources).
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
- [x] **[Repo]** Sample tooling (`0596df6`): `catalyst-assessment?lookbackHours=` (1–720, default 24 unchanged); `scripts/New-CatalystReviewQueue.ps1` (many symbols, skips reviewed articles, deterministic stratified sample); `scripts/Submit-CatalystReviews.ps1` (local dry run, `-Submit` appends). Plan and targets: [Catalyst-Sample-Plan-20261114.md](Validation/Catalyst-Sample-Plan-20261114.md).
- [ ] **[Operator]** Batch 1: label `Documents\MCC-Reviews\catalyst-queue-20261114-200253.csv` (60 articles, 51 symbols, 14 days; predicted RISK 11 / MATERIAL 25 / UNCLASSIFIED 24), then submit. Targets: ≥ 120 evaluable labels, ≥ 40 positive, ≥ 40 negative, ≥ 3 weekly batches. The current store has 7 records and no negatives.
- [ ] **[Repo]** Blind re-review mode for intra-rater agreement, so a second label by the same reviewer is stored separately and not as a conflict.
- [ ] **[Repo — blocked]** Per-class precision/recall, FP/FN and coverage, once that sample exists.

## 8. Runtime, browser and interactive Windows deployment

Decision (2026-11-11): **interactive Windows only**. Details: [Interactive-Windows-Operation.md](Engineering/Interactive-Windows-Operation.md).

- [x] **[Repo]** Durable-path inventory: nine stores, their `MCC_*_PATH` overrides, secrets, browser storage keys and logs (`scripts/lib/MccDataInventory.ps1`).
- [x] **[Repo]** Launcher `scripts/MCC.ps1 start|status|stop|open`: single instance, visible startup failure, per-start log files, graceful stop through the token-guarded loopback `POST /api/admin/shutdown` (`LocalShutdownPolicy`, 8 tests). `/api/health` now reports PID, start time and `sourceSha`. Verified in an isolated PS 5.1 run.
- [x] **[Repo]** `Backup-MccData.ps1` / `Restore-MccData.ps1`: SHA-256 manifest; staged restore by default; `-ToLive` moves the live files aside first; secrets are never copied. Tamper detection and live restore were verified in a temp data root.
- [ ] **[Operator]** On the real machine: start, a refused second start, status, a graceful stop. Then sign out and back in, start again, and confirm the watermarks resume with no duplicates. Note from 2026-11-13: `status` reported `source SHA UNKNOWN - graceful stop False`. That output means the running API was started outside the launcher (Visual Studio or `dotnet run`), so the launcher itself has not yet run on this machine. Stop that API first, then use `MCC.ps1 start`. Pre-check by Claude on this machine, 2026-11-14 (Development secrets, real data): start OK with SHA, second start refused ("already running"), status RUNNING with PID and both log paths, `stop` → "stopped gracefully", status NOT RUNNING. The sign-out test and the operator's own run are still open.
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

- **Recent SIP:** recent-SIP HTTP 403 is established; live, stream and intraday decisions stay on IEX. Delayed historical SIP is allowed for completed Daily bars and for the GTC_EXT sell-window 1m history (2026-11-14).
- **Schwab read-only API:** blocked on developer approval, app registration, callback and read-only scopes. Never order endpoints.
- **Broker execution reconciliation ledger:** only from real redacted broker order/fill records. Never infer fills from OHLCV.

---

## Guardrails (apply to every item)

- Keep Alpaca on IEX for intraday/live/stream data; IEX_PARTIAL is diagnostic only and never consolidated coverage. Exceptions: completed Daily bars (`DailyFeed=sip_delayed`) and GTC_EXT sell-window 1m history (`AllowDelayedHistoricalSipMinutes`) via delayed historical SIP.
- No imputation of missing bars, sessions, prices, timestamps, ATR observations, reference closes or catalyst facts; no nearest-bar substitution.
- Zero news is UNKNOWN, not CLEAR. MATERIAL_EVENT_REVIEW / UNCLASSIFIED map to RISK. READY may pass the catalyst gate only on a recorded, expiring operator attestation (`CATALYST: OPERATOR-ATTESTED`), never by relabelling UNKNOWN as CLEAR.
- Overnight 1M ATR14 is a 1-minute simple ATR, not daily ATR. Overnight RVOL is NOT USED.
- Boring Winner PASS and research defaults are research-only — never READY, a quote, or a trade instruction.
- MCC is decision-support-only; order submission is NOT IMPLEMENTED. The new shutdown endpoint only stops the local process.

## Recently completed (summary — details in commits and the archive)

- **Nov 14, after midnight UTC:** `6fe6446` close/open history on delayed SIP (RTO/CRBG 20/20); currentOvernightPct fix.
- **Nov 14 late night:** READY built in shadow mode (`4eafc9b` ATR informational, `8365f7e` backend, `02e88a3` Dashboard panel); IEX close/open history gap found (14/20, 13/20).
- **Nov 14 night:** READY decisions 6–9 recorded; Daily tolerance ±$0.01 / ±0.5% applied; fresh reference download confirms SIP Daily parity on 18 sessions.
- **Nov 14 evening:** `0b4fbc1` NO_TRADE minutes: RTO GTC_EXT history 40/40 complete on SIP. First walk-forward run: touch rate pass, adverse excursion fail; sell limit still blocked on the TOS ATR gates.
- **Nov 14 late afternoon:** `e48b4d2` delayed-SIP 1m for the GTC_EXT sell history; RTO 15,201 SIP bars captured, 0/40 complete because no-trade minutes have no bar.
- **Nov 14 afternoon:** READY buy/exit/auction decisions frozen; `5f1d5d1` delayed-SIP Daily (20/20 consolidated on the free account) + Massive cross-check; `0596df6` catalyst review queue; first 60-article review sheet generated.
- **Nov 14:** `6a547c3` DATA? reasons (`DataStale` / `OutsideRegularSession`) and `MCC.ps1 warmup`; Claude pre-checked launcher, warm-up (2.2 s background, first request 387 ms), exclusion BLOCKED and Network order on the operator machine.
- **Nov 13 evening:** `d6d52ab` JSONL index warm-up at startup; operator cold rebuild measured at 7.3 s (from 23.3 s).
- **Nov 13 afternoon:** `4ac53f7` capture mapper + 26 tests · `8445c4e` JSONL timing attribution and buffered read · `b5a3283` Opportunity Quality pool fixtures, exclusion blocking, provenance, field availability, startup ReferenceError fix · `e7c0810` launcher, graceful shutdown, inventory, backup/restore · docs: control contract, provider inventory, release evidence script.
- **Nov 13 morning:** after-hours technical gating confirmed on CRBG; boundary audit `PENDING_NOT_YET_AVAILABLE`; Overnight qualification hidden in Intraday mode.
- **Nov 12:** current Overnight interval separated from 20 historical pairs; technical-input wiring with feed-provenance guard and staleness withholding; capture `calc` fix; catalyst mapping; saved-evaluation recalculation; JSONL stage instrumentation measured.
- **Nov 10–11:** in-memory JSONL index (warm 12 s → 0.4 s); Calculated Limit Sell quick-action fix; Main Chart five metrics accepted; calendar-backed final-hour window; interactive-Windows decision.

## Uncertainties

- The operator's 7.3 s measurement came from an API not started by the launcher; the exact SHA it ran is Data not found in sources. Because the new read timing was present, it must have been built from `8445c4e` or later.
- The warm-up was measured once with live workers on the operator machine, after hours (2.2 s). During the regular session, with the stream and collector busier, it may take longer; a request that arrives before it finishes waits on the gate, and the wait is reported.
- Claude's 2026-11-14 pre-checks ran on the operator machine but are not operator sign-off; the [Operator] items stay open until the operator repeats them.
- The `calc is not defined` browser fix is inferred, not explicitly confirmed.
- The launcher was run by Claude with the operator's Development secrets and real data on 2026-11-14. Backup/restore were verified only with temp data, not the real 315 MB data set.
- Consolidated SIP Daily history exists (RTO, EVR, CRBG) and SIP 1m GTC_EXT history exists for RTO (2026-11-14). There is no persisted TOS ATR evidence or TOS compile record. That a missing minute means "no trade" is inferred from the hourly pattern; Alpaca does not label it.
- Massive daily timestamps are assumed to be Eastern-midnight milliseconds (tested with fixtures only). The Massive client has not run against the live API because no key is configured.
- With `DailyFeed=sip_delayed`, Daily qualification fields are now populated wherever they are used (Boring Overnight evaluation, capture technicals). READY is still not wired, and the sell limit stays fail-closed on `HISTORY_40_OF_40`.
