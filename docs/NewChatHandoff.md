# Momentum Command Center — New Chat Handoff

**Updated:** 2026-10-23
**Repository:** [MomentumCommandCenterV2](https://github.com/themailcomeshere/MomentumCommandCenterV2)
**Working branch:** `feature/opportunity-quality-classification`
**Canonical task list:** `docs/Master-To-Do.md`
**Purpose:** Recovery guide for a new ChatGPT conversation or interrupted project session. Treat the repository and this document as the working record; never invent missing facts.

## New user requirements — extended momentum and Schwab (2026-10-22)

- User prefers manual Schwab website order entry; no broker API, credentials, or automated order placement. A future "Open Schwab" link is only a possibility after a supported workflow is verified. Do not build one-click Buy Maximum Amount. If requested later, a non-submitting position-size calculator is the safer starting point.
- User wants MCC to account for stocks already up around 10–15% when they sit down to trade, especially when continuation/news context is strong. Do not automatically discard these candidates solely because they are extended; add an **Extended Momentum / Continuation Watch** research lane.
- This is not permission to override EXT, 5M Permission, or the 1M EXECUTION READY gate. Separate "extended/no entry now", "continuation candidate worth monitoring", and "fresh valid entry event". News must be timestamped and sourced, and cannot alone create eligibility.
- Audit the exact installed ThinkScript to determine what the red "In-trend candidate" bubble means. Do not describe it as technically safe to buy until its exact formula is verified.
- Evaluate a separate validated pullback/reclaim or consolidation-breakout re-entry condition, with spread/liquidity/gap/halts safeguards and out-of-sample tests, rather than weakening the original entry gate.
- Timezone: the user references 08:30 Arizona time. Arizona generally stays on MST while Eastern market hours shift with DST, so the US regular-session open is generally 07:30 Arizona time during Eastern daylight time and 08:30 during Eastern standard time. Dashboard labels should include timezone/session context.


## UPC extension-danger evidence review (2026-10-23)

- User supplied UPC screenshots and facts for a reported 2026-10-22 09:25 event. Reported 5M `Perm=1` and `PBLOCK=0`; 1M `EXT=2.35`, `CANDIDATE BLOCKED: EXTENSION DANGER`, and `IN-TREND CANDIDATE`. This supports keeping the 5M permission and 1M entry gate separate; no logic change is justified.
- Evidence review: `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md`.
- Screenshot headers appear to show 2026-10-23 around 11:26 AM, conflicting with the reported event date. Timezone is missing. Reported missing bars cannot be interpreted as zero movement/volume. Resolve exact bar timestamps and data before diagnosing gaps.
- The exact installed 1M source and `BLK=6` mapping remain unverified. Do not infer source parity from study names.

## Provider comparison refresh (2026-10-23)

- `docs/Provider-Comparison.md` now compares public free and paid tiers against MCC's target. Recommended paid finalists to proof-test: Alpaca Algo Trader Plus ($99/month) and Massive Stocks Advanced ($199/month); no purchase/provider decision made. Free tiers are for plumbing tests, not live authority.


## Provider comparison UI and UPC CSV update (2026-10-23)

- User confirmed UPC screenshot/event date is 2026-10-22 and TOS chart timezone is Arizona.
- Reviewed `data/StrategyReports_UPC_102226.csv`: strategy/order events are 09:37, 09:40, 09:41, 09:47 Arizona; it is not a continuous OHLCV export and cannot establish whether earlier bars were absent.
- Added offline comparison cards for Alpaca and Massive to `frontend/index.html`, CSS in `frontend/styles.css`, and a local checklist action in `frontend/app.js`. These are UI-only placeholders; no provider API calls/adapters/credentials are wired. The comparison button only prepares a test checklist and must not be described as a real provider comparison.
- Evidence review: `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md`; canonical To-Do updated. Still need full UPC OHLCV or exact TOS timestamp evidence, then build backend provider adapters and compare historical/stream data after entitlement/licensing review.

## 1. Mission and architectural boundaries

MCC is a C#/.NET 8 research and trading decision-support prototype, not an auto-trader. Do not implement automatic order placement or Schwab order submission.

- **5M is the authority/permission timeframe.** It establishes whether the broader setup has permission.
- **1M is the execution-timing timeframe.** It may consume 5M context; 5M must never depend on 1M.
- Keep data confidence, setup/entry eligibility, opportunity quality, entry timing, and position management as distinct dimensions.
- Scanner discovery is a candidate-universe input, not a trade signal. A scanner must not create or override 5M Permission or 1M `EXECUTION READY`.
- Opportunity-quality classifications remain diagnostic-only until exact input parity and out-of-sample validation support further use.
- Preserve raw data, source provenance, timezone offsets, and versioned study/config identities. Never synthesize missing bars or silently fill gaps.

## 2. User workflow and collaboration rules

- No fabrication. If data is missing, state: **Data not found in sources.**
- Flag assumptions and unknowns in a final `[Uncertainties]` section for complex responses.
- Be direct and thorough; execute the next task without unnecessary approval requests.
- For any multi-step work, report what changed, what is verified, what remains open, and exactly what the user needs to do (if anything).
- Do not claim build/tests/CI passed without evidence tied to the current branch head.
- Follow `docs/rules/abpDevRules.md`: flowerbox classes and methods with purpose/when-called comments; preserve clean layer dependencies.
- Update this handoff and `docs/Master-To-Do.md` when the project checkpoint materially changes.

## 3. Current project state

**New documentation:** [5M→1M handoff audit](Validation/5M-to-1M-Handoff-Audit-20261022.md) · [Overnight collection contract](Overnight-Collection-Contract.md) · [Canonical Master To-Do](Master-To-Do.md)

### Latest completed work (2026-10-23)

- Added `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`: source comparison confirms the 1M layer reconstructs rather than consumes the actual 5M Permission state, and records concrete raw-Permission, structural-damage, and major-failure predicate differences. No ThinkScript logic was changed; installed-study parity remains open.
- Added `docs/Overnight-Collection-Contract.md`: provider-neutral contract for event envelopes, append-only persistence, idempotency, freshness/health, sleep/restart/outage recovery, deterministic Morning Report snapshots, and required tests. Provider-specific thresholds remain undefined until provider selection.
- Latest branch-head .NET workflow was checked: head `bcb1ef5027859cd4448daa08191df83e0e6eb871`, run 91, conclusion `success`. This does not replace a browser-based end-to-end smoke test.

### Architecture/code already present

- Core market bar and replay models, CSV parsing, replay frame sequencing, V2 signal engine, and opportunity-quality diagnostic models/assessor.
- Strategy Report parser for the MCC-owned `MCC_OHLCV` payload, with validation/rejection paths for malformed/legacy rows.
- ThinkScript references in `docs/Charts/1M/` and `docs/Charts/5M/`, including Authority Diagnostic V2 for the 5M Permission audit.
- Static front-end under `frontend/` is a local/demo workbench; its sample rows are not live scanner output and are not calculated from the C# engine. The dashboard now includes a readiness panel and browser-local feedback notebook with JSON export.
- Application-layer scanner contract scaffold was committed on 2026-10-22: `ScannerDiscoveryEvent.cs` and `IScannerDiscoverySource.cs`. These are contracts only—not a concrete provider, polling job, storage layer, UI wiring, or live integration.
- Scanner candidate dispositions are presentation states (`ActionableQueue`, `WatchPullback`, `FilteredExtended`, `Blocked`, `DataUnknown`), not trade signals.

### Market-data validation status — important reconciliation

On 2026-10-22, the user clarified that they completed the following **locally** and saw only `MATCH` or `ROUNDING ONLY`: (1) all 31 irregular 1M intervals, (2) full-session 1M-to-5M reconciliation, (3) RTH/extended-hours session-setting checks, and (4) Arizona/Eastern DST checks. The user intentionally did not commit all detailed comparison work because they considered it wasteful.

Treat these checks as **USER-CONFIRMED DONE** for task planning. Do not ask the user to repeat them. However, detailed local comparison rows, run dates/settings, classifications, hashes, and provenance are not all present in the current repository; independent auditability is therefore incomplete. The efficient follow-up is to sync already-completed local artifacts or a concise manifest/summary if available—not rerun the analysis.

`docs/Validation/MCC-OHLCV-20261020.md` and `docs/Master-To-Do.md` were updated to reflect this distinction. The 20-row `_UPDATED_3.csv` 1M and 5M samples are already committed and documented as `MATCH`, with precision-only differences identified as `ROUNDING ONLY`. The `_UPDATED_4.csv` files contain later appended source payload rows whose chart comparison fields were observed blank in the repository; do not treat those appended rows as completed chart parity without reconciling the local copy.

### What remains a validation blocker

- Reconcile the user's completed local interval/full-session/session/DST results into repo evidence where practical (exact files, chart/study version, run dates, settings, row/window counts and hashes where available). Do not invent missing metadata.
- Verify build and tests at the current branch head. Older CI runs or user-reported local passes do not prove the current head passes.
- Complete golden indicator parity for the exact versioned ThinkScript definitions used by the C# application, especially ATR/extension and any other metrics that will gate scanner candidates.
- Keep replay authority and live scanner gating blocked until the required evidence and indicator parity are reconciled.

## 4. XRPN 2026-10-02 5M Permission audit — result

The user uploaded a screenshot showing the normal 5M study and Authority Diagnostic V2 around the Permission transition near 12:45 Arizona chart time. Red diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, and `EOD=0`; visible conditions include price below VWAP and EMA9 and EMA9 below EMA20.

Source review in `docs/Charts/5M/MainStudy.txt` confirms the `PBLOCK=2` mapping to persistent major failure. The major-failure raw condition is price below Daily VWAP, price below EMA9, EMA9 below EMA20, and EMA20 below SMA50; persistence requires two bars by default. The Permission latch checks EOD, then persistent major failure, then persistent structural damage, and recovers when the bullish raw condition is true.

Conclusion: the screenshot and source agree that persistent major failure explains the observed Permission revocation. No Permission code change was justified by this evidence. The Master To-Do records the audit and the next handoff task.

## 5. Scanner integration decision and EXT problem

**Decision: start the provider-neutral scanner foundation; do not connect a live provider yet. Thinkorswim is not currently connected as a live scanner/API feed.**

The user reports that red-state `EXT`/extension-danger often coincides with no `EXECUTION READY`, causing wasted chart reviews. Treat the workflow issue as credible, but do not invent a measured frequency or alter the existing gate without data.

Required design:

- Filter/deprioritize currently overextended candidates from the default actionable queue using the exact versioned 1M study's ATR-extension formula and thresholds.
- Keep them in a recoverable `WATCH_PULLBACK` list; do not permanently discard a symbol solely because it is extended now.
- Requalification requires fresh data, a valid new event, and the existing 5M authority and 1M execution gates.
- Distinguish extension warning from danger. Never substitute guessed percentage thresholds or provisional opportunity-assessor thresholds for the exact study rules.
- Missing/stale extension metrics must produce `DATA?`, not an assumption that the candidate is not extended.
- Run shadow mode and measure chart reviews avoided, valid opportunities hidden, and later pullback/READY events before hard-exclusion becomes default.

Provider choice, verified API access, licensing, rate limits, and live credentials are not established in sources. Thinkorswim is currently the chart-study reference/validation environment, not an integrated feed. Do not claim a provider is integrated. No automatic orders.

## 6. Scanner commits and documents

Recent commits on `feature/opportunity-quality-classification`:

- `5c3f45d0a82202aa554550e7a991f0dc251629c6` — add `src/MomentumCommandCenterV2.Application/Models/ScannerDiscoveryEvent.cs`.
- `719db8631a1d3629cfccf98933817f8fb7afa9fd` — add `src/MomentumCommandCenterV2.Application/Interfaces/IScannerDiscoverySource.cs`.
- `59a516436994c9ac869c46ac48ee629d712731c1` — document scanner contract and EXT qualification in `docs/Scanner-and-Watchlist-Workbench.md`.
- `ec4029b2807a64727ad0de88a7d4dd5fa32dbb40` — add scanner integration and EXT prequalification items to Master To-Do.
- `7616548fb9779ee74538a6397fb833125cd93d02` — mark contract scaffold as partial; remaining acceptance work stays open.
- `eccc9c0cb1a6d0147778b6b1e15f547bfe400e03` — reconcile validation doc with user's locally completed MATCH/ROUNDING results.
- `1e057315783119059f599ef4026a09f654353b8b` — update Master To-Do to mark interval/session/DST checks user-confirmed done and evidence sync pending.

These commit hashes are the observed GitHub responses from the tool actions; they are not a claim that CI passed.

## 7. Recommended execution order from this checkpoint

1. [NOW] Inspect current branch head and workflow status; run or trigger build/tests for the exact head if supported. Record pass/fail and SHA accurately.
2. [NEXT] Audit exact 1M extension/ATR implementation and define the scanner qualification input contract from the versioned study. Confirm the C# app can calculate these inputs with golden parity tests; otherwise keep scanner extension gating blocked.
3. [NEXT] Implement deterministic provider-neutral scanner qualification and append-only discovery history. Add tests for first-seen preservation, deduplication, source timestamps, stale/missing values, reason codes, and transitions between actionable, pullback-watch, filtered, blocked, and data-unknown.
4. [NEXT] Add extension warning/danger tests, pullback requalification tests, and prove filtering never changes 5M Permission or 1M READY.
5. [NEXT] Reconcile completed local interval/session/DST artifacts into a compact manifest if available; do not repeat the tests solely to recreate already completed work.
6. [BLOCKED UNTIL CHOSEN] Implement one live provider adapter only after the user/provider choice and verified access requirements are known.
7. [NEXT] Run shadow mode on timestamped data and measure both efficiency gains and false-negative risk.
8. [NOW] Use the updated local dashboard for UI feedback; export feedback JSON before changing browsers/devices because notes are browser-local.
9. Keep the local UI clearly marked demo until it consumes the validated application contract; no order execution.

## 8. Canonical source map

- `README.md` — project overview and roadmap.
- `docs/Master-To-Do.md` — canonical task checklist and next-task authority.
- `docs/NewChatHandoff.md` — recovery guide (this file).
- `docs/Scanner-and-Watchlist-Workbench.md` — local workbench and scanner integration status.
- `docs/Validation/MCC-OHLCV-20261020.md` — market-data export/parity status and user-confirmed local validation note.
- `data/validation/XRPN/10192026/parity/` — 1M/5M parity templates, including `_UPDATED_3` and `_UPDATED_4` versions.
- `data/validation/XRPN/findings.md` and `data/validation/XRPN/manifest.md` — intended detailed evidence/provenance record; currently still contain incomplete template fields.
- `docs/Charts/1M/MainStudy.txt`, `docs/Charts/5M/MainStudy.txt` — versioned study logic.
- `docs/Charts/5M/DiagnosticStudy.txt` — Authority Diagnostic V2.
- `docs/Market-Data-and-Replay-Contract.md` — raw bar, timestamp, replay, and parity requirements.
- `docs/Opportunity-Classification-Verification.md` and `docs/Opportunity-Quality-and-Ranking.md` — diagnostic-only quality assessment and validation protocol.
- `docs/rules/abpDevRules.md` — code/comment/layering conventions.

## 9. Current user action

No immediate user action is required to continue code/document work. If already-completed local validation artifacts can be synced without repeating analysis, that would improve auditability; otherwise keep them recorded as user-confirmed completion with evidence-sync caveat. Provider selection will eventually require user input unless a provider is already specified in a source.

## [Uncertainties]

- Detailed local records supporting the 31-interval, full-session, RTH/ETH, and DST checks are not present in the current repository state. The user's completion report is authoritative for planning but not independently reproducible from the committed files.
- Build/test status at the current branch head is not verified here.
- Exact scanner provider and its API/entitlement/licensing details are not specified; Thinkorswim is not integrated as a live feed.
- Live scanner extension gating remains blocked until exact indicator parity is implemented and tested.
- Dashboard feedback is local to the browser until manually exported; there is no server-side feedback storage.

## Morning Report / Dashboard workflow checkpoint (2026-10-22)

- User's target is to open MCC around 08:30 Arizona time and see a Morning Report, automatically populated and re-ranked watchlist, top 1–4 ranked candidate “Red Zone” tiles, and Boring Overnight decision support.
- New user manual: `docs/User-Manual.md`. Canonical To-Do remains `docs/Master-To-Do.md`.
- Current UI reality: `frontend/index.html` is opened in a browser; it is not an .exe. The page is a static demo/import workbench with localStorage watchlist/feedback. No live data/scanner provider, automatic ranking, generated Morning Report, Red Zone tiles, live Boring Overnight scoring, application-engine wiring, or broker API is connected.
- Repository source audit: `docs/Charts/1M/MainStudy.txt` sets extension warning/danger defaults at 1.50/2.00 ATR using absolute price-to-Daily-VWAP distance divided by ATR(14); bullish danger is specifically price above VWAP and extension at/above danger. `inTrendExecutionCandidate` means `executionCandidate AND NOT freshTrigger`; event is transition/re-arm gated. Final READY separately requires an active execution window, 5M execution permission, and no bullish extension danger. It is a candidate classification, not itself READY. This audit does not prove the installed Thinkorswim study matches repository source.
- Morning workflow/manual records the target Morning Report sections, up-to-four split-screen “Red Zone”, ranked watchlist, Boring Overnight pipeline, extended continuation watch, manual Schwab boundary, and acceptance criteria. All unimplemented features are labeled as planned.
- Search of the repository and available project handoff notes did not recover the full earlier Morning Routine conversation. Do not pretend the proposed routine is an exact reproduction. If exact details matter, ask the user to paste or point to the earlier conversation.
- Next: continue installed-study/source parity audit; preserve current trading rules; define provider-neutral Morning Report and scanner data contracts; choose a verified data/scanner source; implement automated watchlist and ranking, Red Zone, and Boring Overnight only with provenance/freshness and validation. Decide .exe packaging after integration architecture is settled.


## Morning Report UI and overnight collector checkpoint (2026-10-22)

- User wants to leave released/deployed MCC open overnight and see collected scanner data in the 08:30 Arizona Morning Report. This is a requirement, not a current capability: the static browser workbench has no live provider, polling/streaming collector, persistent market-data event store, or background service. Keeping the tab open today does not collect data.
- UI updates:
  - `frontend/index.html`: Morning Command Brief section, explicit overnight collector NOT RUNNING status, scanner/watchlist/top-candidate/Boring Overnight status cards, responsive Red Zone placeholder.
  - `frontend/app.js`: locally rank demo/import scanner rows by supplied score for display only, render up to four Red Zone cards with unknown 5M/1M gates, update watchlist count, and export a JSON report preview.
  - `frontend/styles.css`: responsive Morning Report and 1–4 candidate tiles.
- The Morning Report is intentionally labeled preview-only. The displayed generated timestamp is the browser's local clock, not a market-data timestamp. The JSON export records liveScannerConnected=false, overnightCollectionRunning=false, provider=NOT_SELECTED, and Boring Overnight NOT_CALCULATED.
- Documentation updated: `docs/User-Manual.md`, `docs/Master-To-Do.md`, `docs/Scanner-and-Watchlist-Workbench.md`. New To-Do work requires a supported provider, long-running collector independent of browser tab, persisted raw timestamped events/observations, freshness/gap/duplicate checks, restart/outage/sleep recovery, versioned Morning Report, real ranked Red Zone, and Boring Overnight integration. The provider-neutral contract is now written in `docs/Overnight-Collection-Contract.md`; provider-specific adapter and operational thresholds remain blocked on provider selection.
- Do not claim live operation or .exe/background service until implemented and tested. Next engineering step is provider selection and data-ingestion/persistence architecture, not adding fake live-looking demo values.


## Latest checkpoint — Morning routine, provider research, collection scaffold (2026-10-23)

- Latest previously checked branch head `46c28fd4f56bc7db3b192a4b9a05b617bb42350a` had a successful .NET build-and-test workflow (run 39905346096). Subsequent commits in this checkpoint require a fresh CI check; do not assume they passed until verified.
- Read the complete Project source file Morning.txt (992 lines). Consolidated its selected excerpts into `docs/Morning-Report-and-Routine.md`. It covers the 08:30 Arizona briefing, market/industry context, scenario expectations, continuous conditions, contextual ranking, catalyst maturation risk, Boring Overnight, journey/outcome learning, personalized timing, and execution intelligence. Illustrative numbers in that file are not real outputs.
- Added `docs/Provider-Comparison.md` comparing Alpaca, Massive, Twelve Data, and Schwab evidence. Preliminary proof-of-fit candidates are Alpaca Algo Trader Plus and Massive Stocks Advanced. No provider is selected; account entitlements and contract rights are unverified.
- Added an initial provider-neutral persistence scaffold:
  - `src/MomentumCommandCenterV2.Application/Interfaces/IScannerDiscoveryEventStore.cs`
  - `src/MomentumCommandCenterV2.Application/Services/ScannerDiscoveryIngestionService.cs`
  - `src/MomentumCommandCenterV2.Infrastructure/Persistence/JsonlScannerDiscoveryEventStore.cs`
  - `tests/MomentumCommandCenterV2.Tests/JsonlScannerDiscoveryEventStoreTests.cs`
- Scaffold validates event fields/timestamps, stores discovery envelopes in append-only JSONL, retains receipt time, and deduplicates using source event ID or a deterministic fallback key. It is not a background worker/provider adapter and does not yet persist OHLCV bars.
- Updated Dashboard layout so Morning Brief sits left and Red Zone sits upper-right on wide viewports; the two-column workspace collapses responsively. User had approved the earlier look; the latest position change still needs browser confirmation.
- Created `docs/Validation/5M-to-1M-User-Validation-Steps.md` with exact steps and evidence needed from the installed Thinkorswim studies. User need not repeat the already completed OHLCV/DST work.
- Updated To-Do, User Manual, and Scanner Workbench with this status.


### Polling worker scaffold (2026-10-23)

- Added `src/MomentumCommandCenterV2.Application/Services/ScannerDiscoveryPollingWorker.cs`: cancellable generic poll loop, durable ingestion before success state, retry on next interval after errors, and health snapshot (RUNNING / DEGRADED / STOPPED / UNKNOWN). It is not hosted or running because no provider adapter is configured.
- Added `tests/MomentumCommandCenterV2.Tests/ScannerDiscoveryPollingWorkerTests.cs` for empty-batch handling and graceful cancellation.
- The event store contract import fix passed CI at commit `38b3f3ad248bd020ac48f377ce86421f7753f76d`; the latest worker/test commit needs its own CI result.


## API host, provider adapters, and Morning Brief — 2026-10-23

- Added project `src/MomentumCommandCenterV2.API` and included it in the solution. The API serves the existing dashboard under `frontend/` from the repository's normal directory layout.
- Added normalized provider contracts and first REST historical-bar adapters:
  - `src/MomentumCommandCenterV2.API/MarketData/AlpacaMarketDataAdapter.cs`
  - `src/MomentumCommandCenterV2.API/MarketData/MassiveMarketDataAdapter.cs`
  - `src/MomentumCommandCenterV2.API/MarketData/MarketDataContracts.cs`
- Credentials are read server-side from .NET configuration or `ALPACA_API_KEY`, `ALPACA_API_SECRET`, `ALPACA_DATA_FEED`, and `MASSIVE_API_KEY`. No credentials were supplied; no live requests or plan-entitlement checks have occurred.
- Endpoints: `/api/health`, `/api/providers/status`, `/api/market-data/compare`, and `/api/morning-brief`.
- Dashboard provider comparison now requests both adapters for the same UTC window. Morning Brief fetches only SPY/QQQ/IWM 5M observations from configured providers and explicitly labels sectors, movers, catalysts, historical expectations, Red Zone, and Boring Overnight as DATA? / NOT CALCULATED.
- Start with `dotnet run --project src/MomentumCommandCenterV2.API`. Keep keys out of Git, browser code, localStorage, and logs.
- **Not yet verified:** latest solution build/test/CI, endpoint smoke test, browser test, provider response parsing against entitled accounts, licensing, timestamp/session parity, pre/post-market coverage, pagination, WebSockets, reconnect/backfill, and durable OHLCV storage. Do not rely on this as a replacement for TOS yet.
- Continuous UPC OHLCV is not a prerequisite for the provider implementation. It is still the evidence needed to resolve the separate UPC chart-gap question for 09:16–09:36 on 2026-10-22.


### CI checkpoint — 2026-10-23

- GitHub Actions run 147 completed successfully at commit `ae8180866def759612fa7c1a515dc93586143056`. Restore, Release build, and tests passed: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011426478
- This is not live provider validation or browser testing. The subsequent To-Do/handoff documentation-only updates should still be followed by a check of the newest workflow run before claiming latest-head validation.


### Raw OHLCV persistence extension — 2026-10-23

- Added `IMarketBarObservationStore` and `JsonlMarketBarObservationStore` under the API project. The comparison endpoint persists each provider's valid raw bars before returning comparison metrics.
- Default file: LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl. Override with environment variable `MCC_MARKET_BAR_STORE_PATH`.
- Append-only records include observation ID, received timestamp, source timestamp, provider, symbol, bar, content hash, and optional superseded observation ID. Identical observations are skipped; changed observations at the same provider/symbol/timestamp are appended as corrections.
- Persistence is local JSONL only; no database, retention controls, store-specific tests, collector loop, websocket, or automatic restart/backfill process yet.
