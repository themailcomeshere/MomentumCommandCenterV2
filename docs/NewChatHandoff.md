## Checkpoint update — local Alpaca configuration and XRPN evidence (2026-10-23)

- Added the API project's `.NET User Secrets` identity and documented local Alpaca setup commands in `src/MomentumCommandCenterV2.API/README.md`. Tracked `appsettings.json` now contains only the non-secret default feed (`iex`); do not place API keys there.
- Alpaca adapter credentials now resolve through standard ASP.NET Core configuration, so User Secrets and environment variables follow the same configuration pipeline. Added deterministic xUnit coverage for configured credentials, request headers, bar parsing, and the unconfigured-provider path. GitHub source edits do not execute tests; latest-head CI must be checked before claiming these tests pass.
- User supplied the requested XRPN 2026-10-02 screenshot. It shows the normal 5M study, Authority Diagnostic V2, readable time axis and target 12:30–13:00 Arizona window; user confirmed the 5M and 1M chart dates are correct and the top-left chart date should be ignored. Canonical To-Do and external-dependency checklist now mark this evidence received.
- Remaining user action: create the free Alpaca account, configure credentials locally with the README commands, run the API, and report sanitized status/errors/timestamps. Do not ask for credentials in chat. Keep Massive deferred and paid subscriptions on hold.
- Still open without provider credentials: pagination/retries, collector lifecycle/recovery, storage hardening, fixture-driven Morning Brief/scanner/Boring Overnight, session-aware gap semantics, ThinkScript parity and installed-study validation.

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
- Persistence is local JSONL only; no database, retention controls, collector loop, websocket, or automatic restart/backfill process yet. Store tests cover identical-observation deduplication across store restart and append-only correction history; CI run 148 passed at commit `c702afa21fac9dfa5a31b18463cc4429aa4adc71`.


The Morning Brief endpoint now persists the observed SPY/QQQ/IWM bars it fetches and reports each benchmark's observed first-to-last window change when enough bars exist. It still does not calculate sector breadth, catalyst/news, scanner movers, validated regime, Red Zone eligibility, historical probabilities, or Boring Overnight qualification.


### Latest verification checkpoint

- GitHub Actions run 151 completed successfully at `ae6ee7acc30e85a6268b5ea8fc40d86429d3461b`: solution restore, Release build, and test suite passed. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012059701.
- This is build/test verification only. Live API requests, browser rendering, account entitlements, market-data licensing, and parity with installed Thinkorswim studies remain unverified.


## Account dependencies and next work — 2026-10-23

- User confirmed the solution builds and runs locally. Do not ask them to repeat that setup.
- New canonical checklist: `docs/Next-Steps-and-User-Inputs.md`. It distinguishes free-account/live smoke-test needs, user-supplied TOS evidence, and offline implementation tasks.
- **Do not ask the user to buy both providers.** Neither account is required for provider-neutral coding, fixture tests, storage, collector lifecycle, or most Morning Brief contracts. One free account is enough to start a live smoke test; both are useful for direct side-by-side comparison. Paid plans remain on hold pending proof of fit and licensing/entitlement checks.
- Corrected stale To-Do/provider/API README entries: historical REST adapters and append-only JSONL raw-bar storage exist. Latest verified CI run 151 passed at code head `90ca6234a8bafc9230b6e4ef1996e569682c90a1`. Later documentation-only commits update dependency/status notes; check the latest Actions run before asserting current HEAD CI.
- Remaining work independent of accounts: pagination and resilient HTTP behavior; hosted collector/health lifecycle; streaming abstraction; reconnect/backfill/restart; raw-store corruption/retention/error health; fixture-based Morning Brief, scanner, and Boring Overnight pipelines; session-aware gap semantics; ThinkScript parity.
- User evidence that still matters: XRPN 5M transition screenshot per `docs/Validation/5M-to-1M-User-Validation-Steps.md`; UPC 09:16–09:36 continuous bars/exact timestamp inspection only if that historical gap question still needs resolution; review dashboard through API-hosted browser. None blocks offline engineering.
- Morning Brief remains only SPY/QQQ/IWM historical 5M observations plus explicit unknown placeholders. Do not claim broad movers, sector/breadth, news, validated regime, Red Zone or Boring Overnight are implemented.


## Checkpoint update — October 2 XRPN pair received; Alpaca smoke-test runbook — 2026-10-26

### XRPN October 2 OHLCV

- [DONE — OHLCV EXPORT RECEIVED] data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv is a genuine XRPN 1M export, not the previously misnamed HPK file. Report header: XRPN, 2026-10-02, 06:32–13:02 Arizona display range. Embedded payload has 391 consecutive minute timestamps from 06:30 through 13:00 Arizona (13:30–20:00 UTC); no missing one-minute payload timestamps were found.
- [DONE — TIMEFRAME CROSS-CHECK] Matched against data/StrategyReports_XRPN_10022026_5M_EntireDay.csv: 79 5M payload rows, same first/last payload timestamps. Of the 79 5M bars, 78 have a complete five-row 1M group; all 78 aggregated OHLCV groups match the corresponding 5M payload within numeric precision. The final 5M timestamp has no subsequent five-minute 1M group because both exports end at 13:00 Arizona. This validates exported OHLCV alignment for shared windows, not installed-study state parity.
- [BLOCKED — INTERNAL STUDY DIAGNOSTICS] The two repository CSVs expose the MCC_OHLCV transport payload only. No per-bar Permission, block-code, candidate/trigger, extension-danger, READY activation/expiration, re-arm, or expiration-state fields were found. Data not found in sources. If those diagnostics were uploaded separately, confirm their exact repository path; otherwise export the diagnostic study output for both timeframes.
- Keep the prior October 2 screenshot; do not ask the user to re-upload it. Pair its observed Permission revocation with the newly validated OHLCV only after same-bar diagnostic fields are available. Do not infer gate-state root cause from OHLCV alone.

### Overnight Scanner screenshot — 2026-10-26

- Screenshot reviewed: Screenshot 2026-10-26 143847.png. It is a ranked scanner view, not proof of Boring Overnight eligibility. Initial chart-review shortlist prioritizes lower displayed ON movement / steadier values over the highest raw Overnight Score: TTE, RTO, NNN, OKE, AHR, FMS, with CCI as a secondary comparison. Exclude BOLT and VISN from the first boringness batch because their displayed current ON moves are 6.22% and 33.8%, respectively; the screenshot alone cannot establish catalyst clearance, liquidity gate, source-backed close, or actual fill probability.
- Request same-window 5M chart screenshots with extended-hours bars enabled, covering Monday 2026-10-26 regular session and Tuesday 2026-10-27 from 07:00–20:00 Eastern. Capture the regular-session close and visible time axis for each symbol. Screenshots are visual triage only; quantitative target-touch research still requires timestamped OHLCV export and explicit DATA_GAP rows.

### Alpaca live API smoke test

- The user's saved Alpaca key/secret are the intended credentials for the first provider smoke test. Do not ask them to paste credentials. If /api/providers/status says Alpaca is not configured, re-enter them locally with the commands in src/MomentumCommandCenterV2.API/README.md.
- From the repository root, run: dotnet run --project src/MomentumCommandCenterV2.API. Open /api/health, then /api/providers/status. A configured status is only a configuration check.
- With the API still running, request /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z from the same local host/port printed by ASP.NET Core. This is 09:30–09:50 Eastern on Friday 2026-10-23. Check Alpaca request status, returned bar count, first/latest source timestamps, feed, duplicate timestamps and observed gaps. If Massive is unconfigured, its error is expected and does not invalidate an otherwise successful Alpaca response; record it separately.
- Run tests separately with dotnet test MomentumCommandCenterV2.sln --configuration Release. The user's reported 87/87 passing tests is good offline evidence, but it does not prove live authentication, feed entitlement, correct timestamps, or extended-hours coverage. Never include keys/secrets in the report.
- Share only sanitized results: health/status JSON, HTTP/error messages with tokens removed, provider/feed name, bar count, and source timestamps. Do not call Alpaca production-verified until authentication, entitlement, coverage and timestamp semantics are checked.

### Current branch / release posture

- Feature branch remains substantially diverged from master; do not merge it wholesale. Use scoped file/history reconciliation.
- MCC remains LOCAL PROTOTYPE / DIAGNOSTIC ONLY; live trading reliance is not approved. No orders were submitted by this work.

## [Uncertainties]

- OHLCV alignment is validated for 78 complete five-minute windows; the final endpoint bar has no complete subsequent 1M group by design of the export endpoint.
- Per-bar ThinkScript diagnostic fields were not present in the two XRPN OHLCV report files. Data not found in sources.
- The scanner screenshot has no source-backed news/catalyst or complete liquidity evidence; shortlist is for chart review, not a buy/hold recommendation.
- The local Alpaca credentials are user-reported as saved; actual API authentication has not yet been observed in this conversation.


### Handoff status after documentation edits — 2026-10-26

- Updated this handoff, docs/Next-Steps-and-User-Inputs.md, docs/Master-To-Do.md, docs/Release-Readiness-2026-10-26.md, and src/MomentumCommandCenterV2.API/README.md with the XRPN 1M/5M alignment result, the missing diagnostic-state evidence, the Overnight Scanner shortlist, and the exact Alpaca smoke-test procedure.
- The latest documentation edits have not been independently tested by a current-head GitHub Actions run in this session. Do not describe the latest branch head as CI-green until a run tied to the exact head completes.


## Correction and evidence update — 2026-10-26 (user response)

### Corrected chart-window request

- Retract the prior request to capture 2026-10-26 through 2026-10-28. It was an invalid historical-research instruction because those future sessions had not occurred when the screenshot request was made.
- User is capturing AHR, FMS, and NNN on 2026-10-21 through 2026-10-23. Accept this historical window for initial visual review; do not ask them to retake these screenshots merely to satisfy the previous erroneous range.
- This window provides recent historical shape only. It does not itself include the complete 07:00–20:00 Eastern GTC_EXT window for a subsequent session following each regular close. If initial chart review justifies further research, request a completed historical pair: prior regular-session close plus the entire next 07:00–20:00 Eastern window, with actual session dates explicitly named and all missing intervals preserved as DATA_GAP. Avoid future dates.

### Alpaca smoke test — actual result received

- /api/health returned OK, mode decision-support-only, orderSubmission NOT IMPLEMENTED, timestamp 2026-10-26T22:10:39Z.
- /api/providers/status reports Alpaca NOT_CONFIGURED and Massive NOT_CONFIGURED.
- /api/market-data/compare returned zero bars and zero persisted observations for both providers. Alpaca error: Set MarketData:Alpaca:ApiKey and ApiSecret (or ALPACA_API_KEY / ALPACA_API_SECRET). Massive error: Set MarketData:Massive:ApiKey or MASSIVE_API_KEY.
- Therefore, live-provider smoke test has NOT passed. Endpoint responds, but no provider returned market data.
- User reports dotnet test MomentumCommandCenterV2.sln --configuration Release completed successfully: build succeeded; 87 tests succeeded, 0 failed, 0 skipped. This is test evidence, not live provider evidence.

### Specific local credential fix

The API project UserSecretsId is MomentumCommandCenterV2-API-Local. Credentials saved under a different project/UserSecretsId or Windows account are not automatically visible to this API. In a terminal at repository root, set values locally without printing or sharing them:

1. dotnet user-secrets set "MarketData:Alpaca:ApiKey" "<YOUR_EXISTING_ALPACA_KEY>" --project src/MomentumCommandCenterV2.API
2. dotnet user-secrets set "MarketData:Alpaca:ApiSecret" "<YOUR_EXISTING_ALPACA_SECRET>" --project src/MomentumCommandCenterV2.API

Replace placeholders locally; never paste actual values into chat, source control, or screenshots. Restart API, recheck /api/providers/status, then rerun the same /api/market-data/compare request. If configured remains false, check same machine/user and target project. Do not create or buy Massive credentials for this test.

### Current user-reported status

- XRPN Oct 2 1M/5M OHLCV pair: aligned for 78 complete 5M windows; per-bar internal study diagnostics still missing from reviewed files.
- Overnight chart screenshots: user will provide AHR, FMS, NNN for Oct 21–23.
- Alpaca: credentials are not visible to this API process yet; actual authentication/data retrieval remains blocked.
- Tests: 87/87 passed according to user's terminal output.
- No live trading or order submission occurred.

## [Uncertainties]

- The reason saved Alpaca credentials are not visible is unknown until project-specific User Secrets are checked locally.
- The 10/21–10/23 chart screenshots may reveal visual behavior, but exact OHLCV coverage, GTC_EXT reach/touch rates, and actual fills cannot be established from screenshots alone.
- XRPN diagnostic fields may have been uploaded separately, but their exact path is not identified in reviewed branch files.


## Final Overnight review and Visual Studio credential update — 2026-10-26

### Screenshot date mismatch — do not mislabel evidence

- Three Project Sources screenshots were visually inspected: AHR, FMS, NNN.
- All three visible chart headers show 2026-10-19 through 2026-10-21, not the intended 2026-10-21 through 2026-10-23. Treat these screenshots only as a provisional visual read of Oct 19–21. Do not report them as the Final Analysis of the intended Oct 21–23 sample and do not infer the missing two dates.
- User had already been asked to capture Oct 21–23 after the assistant's earlier invalid future-date request. Do not ask the user to repeat work because of the assistant's mistake; ask only for the correct-date screenshots if they are not already present elsewhere in Project Sources.
- A chart screenshot can support qualitative visual observations, but not full GTC_EXT eligible-window coverage, exact target touch rates, actual fills, or a statistically validated target recommendation.

### Provisional chart observations (visual only; dates shown are Oct 19–21)

- **AHR:** The chart shows an early-session rise/spike toward the displayed high near 54.76, followed by a broad fade and lower prices into the right edge. The 5M status ribbon at the right edge reads 5M STATE: GET OUT, 5M PERMISSION: NO, bearish structure, extension about 1.71 ATR, and EOD GET OUT. This is consistent with deterioration by the displayed endpoint, not proof of overnight behavior or a sell-fill outcome.
- **FMS:** The chart shows a sharp downward gap/flush around the Tue boundary from roughly 22.0 to a low near 20.91, followed by a partial recovery and then relatively flat/lower consolidation near 21.5–21.7. The right-edge ribbon shows 5M STATE: GET OUT, 5M PERMISSION: NO, transitioning structure, RSI around 75.8 and extension around 6.46 ATR. The large gap and extreme displayed extension make this a poor example of a “boring” overnight without event/price-context investigation.
- **NNN:** The chart shows a rise from roughly 40.5 into a spike/high around 41.67, then a sustained fade toward about 40.6 by the right edge. The right-edge ribbon still shows permission YES / runner structure and rising runner momentum while extension is around 2.53 ATR and EOD is active. This demonstrates that a persistent 5M permission/runner state can coexist with intraday price deterioration and elevated extension; it does not prove overnight suitability.
- These observations are intentionally descriptive. The screenshots' exact dates/session boundaries and visual scale are not a substitute for exported bars and reference closes. No target-touch percentage is derived from them.

### Alpaca credentials and Visual Studio launch context

- User reports both MarketData:Alpaca:ApiKey and MarketData:Alpaca:ApiSecret were successfully saved to a .NET User Secrets store.
- User runs the application through Visual Studio and navigates to localhost endpoints; they are not launching the app with the previously provided dotnet run command.
- The next live-provider check must be made against the same Visual Studio-launched API process. User Secrets are loaded for the API project's UserSecretsId only when the app runs in Development configuration (the usual Visual Studio profile, but verify the launch profile/environment rather than assume).
- Stop/restart and re-run the API from Visual Studio after saving the secrets so the process reloads configuration. Then open /api/providers/status and /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z on the same localhost port.
- Success requires actual returned bars and source timestamps, not just configured: true. If still unconfigured, check the API project's exact UserSecretsId, Visual Studio launch profile's environment, and whether the secrets were set under the same Windows user. Do not share credentials or secret-store output containing values.
- Massive can remain unconfigured for the initial test.

### Immediate status / next steps

1. Assistant: keep the three chart screenshots marked as Oct 19–21; do not overstate them as Oct 21–23.
2. User: if Oct 21–23 screenshots exist in Project Sources, identify/attach the exact correct three files; otherwise provide the intended-date screenshots when convenient. No need to re-capture anything just to satisfy the earlier invalid date request.
3. User: restart the API in Visual Studio, verify /api/providers/status, then inspect /api/market-data/compare for real bars and source timestamps; send sanitized output only.
4. Assistant: after correct-date evidence and provider response are available, complete the date-correct visual review and then pursue full-window OHLCV / GTC_EXT target-touch research. Keep the launch recommendation blocked until evidence supports it.
5. Do not change the +0.5% reference target or claim it is optimized based on these screenshots. It remains a heuristic until a verified sample of full eligible windows is analyzed.

## [Uncertainties]

- Data not found in sources for the intended Oct 21–23 chart screenshots in the files inspected in this turn; the three inspected images visibly show Oct 19–21.
- It is not yet known whether the correct screenshots exist elsewhere in Project Sources but were not returned by the available file search.
- The visual chart endpoint status is not an independent provider/data export and does not establish order fills, catalyst clearance, or overnight returns.
- Whether Visual Studio is using Development and the same user-secrets profile remains to be confirmed from the actual running process.


## User correction, TTE/FMS chart review, and Alpaca validation — 2026-10-26

### Historical Overnight screenshots — accept user's date clarification

- User explicitly confirms the three AHR/FMS/NNN screenshots are for 2026-10-21 through 2026-10-23. Accept that user-provided date range as the intended study window; the previous response misread/misinterpreted the screenshots and incorrectly challenged the dates. Do not ask the user to re-provide those three images.
- Final analysis should use those screenshots as the 10/21–10/23 review set, with the limitation that screenshots are visual evidence and cannot establish exact target-touch rates or fills.

### Today's 1M chart screenshots — TTE and FMS

- TTE screenshot (2026-10-26): 5M ribbon shows `SELL - CONFIRMED WEAKNESS`, Permission NO, bearish structure, RSI about 53.1, extension about 3.58 ATR, EOD GET OUT. The lower 1M chart has no visible event bubbles in the displayed window, although state/metric labels are present.
- FMS screenshot (2026-10-26): 5M ribbon shows SELL/BREAKDOWN, Permission NO, bearish structure, RSI about 46.6, extension about 8.54 ATR, with very elevated displayed RVOL (about 15.47). The 1M chart has many historical event bubbles, including PREPARE TO SELL, SELL, EXECUTION EXPIRED, and extension-danger/candidate diagnostics. The right-edge 1M state is PREPARE TO SELL.
- No-bubble interpretation: absence of bubbles is not a HOLD or SELL signal by itself. In the Library's V14.5 1M source, `showStateBubbles` defaults to yes, but bubbles are plotted only for specific state/event transitions (e.g. PREPARE TO BUY, candidate events, READY, EXECUTION EXPIRED, PREPARE TO SELL, SELL); there is no generic per-bar bubble for every state and no dedicated GET OUT/EOD bubble in the inspected bubble section. Thus no qualifying bubble-producing transition in the displayed interval can produce a blank chart even while right-edge labels show a bearish/GET OUT state. However, because FMS displays many bubbles while TTE displays none, the screenshot alone cannot distinguish (a) TTE had no bubble-producing events, (b) `showStateBubbles` is off on TTE, or (c) the installed TTE study/version differs from FMS. Do not assert a single cause without checking the TTE study inputs/version.
- User walkthrough: on TTE's lower 1M pane, open Studies/Edit Studies, select the 1M Command Center study (not the 5M study), open its gear/settings, confirm `showStateBubbles = yes`, apply. Compare the exact 1M study name/version and inputs on TTE and FMS. If already yes and versions/settings match, do not change trading logic; report this and then use a temporary targeted diagnostic view to inspect state transitions. Do not change the 5M authority logic as a response to missing 1M bubbles.
- Caution: displayed extension is elevated on both; TTE ~3.58 ATR and FMS ~8.54 ATR. Their 5M permission is NO and structures are bearish. These are not fresh long-entry approvals. This is chart-state interpretation, not a personal position-management instruction.

### Alpaca smoke test — PASS for historical IEX bar retrieval

- User's Visual Studio-hosted API returned Alpaca configured true and status `CONFIGURED_NOT_YET_VALIDATED` from `/api/providers/status`.
- User's `/api/market-data/compare` request for SPY 5M from 2026-10-23 13:30Z to 13:50Z returned 5 Alpaca bars, 5 persisted observations, feed `iex`, first timestamp 13:30Z, latest timestamp 13:50Z, zero duplicate timestamps, zero reported gaps, and no error. This is actual successful historical market-data retrieval for that requested window and validates the configured credentials/feed path. It does not validate every symbol, every historical window, live streaming, SIP entitlements, or overnight-session coverage.
- Massive remains NOT_CONFIGURED; no need to configure it for this first successful Alpaca smoke test.
- Code review found `/api/providers/status` hardcoded `CONFIGURED_NOT_YET_VALIDATED` whenever credentials existed, independent of successful `/api/market-data/compare` requests. This made the status stale/misleading after successful data retrieval.
- Implemented a feature-branch fix: provider adapters now remember the timestamp and bar count of the last successful non-empty data response; `/api/providers/status` now reports `VALIDATED_DATA_RETURNED` when such a response has occurred in the current API process, and exposes `validated`, `lastSuccessfulDataValidationUtc`, and `lastSuccessfulDataValidationBarCount`. Empty successful responses do not mark validation. This is process-local state and resets when the API restarts; it is not a persistent health history.
- Added an Alpaca unit test for empty successful response not counting as data validation. The exact latest commit's CI status has not yet been verified; do not claim tests passed for these new code changes until GitHub Actions/local Release tests finish.

### Current next steps

1. User: check TTE lower 1M study's `showStateBubbles` setting and compare its exact study/version with FMS; return the setting/version result if bubbles remain absent.
2. User: after the status endpoint code update is available in the running build, restart the API in Visual Studio and repeat a small SPY compare request. `/api/providers/status` should then report `VALIDATED_DATA_RETURNED` after a successful non-empty response; if not, confirm the updated branch/build is actually running.
3. Assistant: check CI on the updated branch and address failures before promoting code.
4. Continue the date-correct 10/21–10/23 Overnight analysis from the three existing screenshots; do not request a duplicate upload.

## [Uncertainties]

- Exact TTE 1M study version and `showStateBubbles` input are not visible in the screenshot; these are the main unresolved causes of the blank bubble display.
- The provider validation state is intentionally in-memory and process-local; no durable provider-health history has been implemented.
- The successful SPY test proves the requested historical IEX bars were returned, not SIP entitlement, real-time stream, all symbols, or full GTC_EXT coverage.


## 2026-10-26 — CCI/NICE review, date-correct screenshot review, and 1M EOD bubble fix

### Historical screenshot set — COMPLETE for visual triage
- User confirms the three AHR/FMS/NNN screenshots represent the 2026-10-21–2026-10-23 sample. Accept the correction; do not ask for re-upload. Full visual notes are in docs/Validation/Overnight-Candidate-Review-20261026.md.
- AHR: visible spike then fade; right edge showed GET OUT / 5M Permission NO. Not boring at that snapshot.
- FMS: 5M Permission NO and EOD GET OUT; 1M extension about 6.45 ATR. Abnormally extended for a consistency-first overnight hold.
- NNN: best 5M permission of the three at the visible right edge (Permission YES / runner regime / execution environment YES), but 1M PREPARE and extension about 2.53 ATR. Scanner Boringness score 55.23 is below 60, so this is not a Boring Overnight READY.
- Screenshot-only triage is complete. Quantitative overnight target-touch research is still blocked on source-backed prior regular close and complete subsequent 07:00–20:00 Eastern GTC_EXT bars; missing intervals remain DATA_GAP and a touch is not a fill.

### New candidates CCI and NICE — WATCH, neither READY
- Scanner columns: CCI score 68.11 (AGGRESSIVE), ON Win 60%, AVG ON 1.24%, median 0.08%, 5D 2.73%, current ON 0.03%. Mean/median divergence warns that a few large gaps may dominate the average. CCI chart: 5M Permission NO, neutral structure, bullish regime not established, RVOL 0.04, extension 1.23 ATR.
- NICE score 60.47 (AGGRESSIVE), ON Win 75%, AVG ON 0.70%, median 0.60%, 5D 0.70%, current ON 0.55%. Rounded 5D and average values are equal; verify unrounded values against the strict 5D > 20D hard gate. NICE chart: 5M Permission YES but neutral structure, RVOL 9.48, extension 0.45 ATR, execution environment NO; price is below displayed VWAP/EMA9/EMA20/SMA50. Neither candidate qualifies from supplied evidence.
- Catalyst state and 20-day average daily share volume are not shown. Keep catalyst UNKNOWN; do not label READY. Full analysis and values are in docs/Validation/Overnight-Candidate-Review-20261026.md.

### TTE 1M bubble root cause and source fix
- User confirmed TTE and FMS use the same study/settings. Source inspection shows V14.5 only creates bubbles on transitions/events; the final EOD_GET_OUT state was label-only and was not included in stateBubbleEvent. Therefore an EOD/GET OUT label could appear without a bubble. This is a source-level omission, not evidence of a different 5M permission state.
- Updated docs/Charts/1M/MainStudy.txt to emit one red “EOD GET OUT” bubble on the transition into EOD_GET_OUT. This does not change entry gates, 5M authority, or state calculations; it only makes the EOD state transition visible. Source file is not automatically installed in Thinkorswim.
- User action: paste/update the 1M study from the repository source in Thinkorswim, save/apply it to TTE, then confirm one EOD GET OUT bubble appears when the chart enters the EOD state. Do not edit the 5M study. If other event bubbles remain absent outside EOD, use showHistoricalDebugBubbles=yes only for a tightly zoomed diagnostic window; turn it back off after debugging.

### Alpaca status and CI
- User's actual SPY 5M IEX compare returned 5 bars and 5 persisted observations for 2026-10-23 13:30Z–13:50Z, with zero duplicate timestamps, zero reported gaps and no error. The status endpoint then correctly returned VALIDATED_DATA_RETURNED after the provider-status code update; validation is process-local and resets on API restart.
- GitHub Actions .NET build/test workflow passed on commit 4f5ed265e54e9a924935a58b8b3233b31b264279 (run 40414827751). User also reports 88 tests passed. The subsequent ThinkScript text edit and documentation commits still require a current-head CI check; .NET CI cannot compile/validate ThinkScript semantics.
- Massive remains unconfigured and is not needed for the initial Alpaca path.

### Tonight / this week
- No supplied candidate is fully Boring Overnight READY. For tonight, only continue with manual source-backed catalyst checks and exact scanner hard gates; if any required field is unknown, choose WATCH / zero position.
- Near-term MCC work: current-head CI after the EOD bubble edit; install/verify the 1M EOD bubble; finish provider pagination/retries, collector lifecycle/recovery, durable data-quality states, Morning Brief and scanner contracts, and fixture-based Overnight calculations. Live stream, catalyst/news coverage, full GTC_EXT history, target-touch study, study parity, and realized outcomes remain release blockers.

## [Uncertainties]
- Exact unrounded scanner calculations, CCI/NICE daily average volume and catalyst status are not supplied.
- The user's date range for AHR/FMS/NNN is accepted, but visible chart labels/axes do not align perfectly; verify per-bar timestamps before quantitative research.
- No full GTC_EXT bars or actual fills for CCI/NICE/AHR/FMS/NNN were found.
- Thinkorswim installation of the updated 1M study must be done by the user and confirmed visually.


### CCI/NICE catalyst lookup — 2026-10-26
- CCI: company source confirms Q3 2026 results and full-year outlook update were released Oct 21 after close. Treat as a recent earnings/outlook event; review post-earnings volatility before any boring overnight classification. Source: https://crowncastle.gcs-web.com/news-releases/news-release-details/crown-castle-reports-third-quarter-2026-results-and-updates.
- NICE: official investor-relations event page currently lists no upcoming events, but that does not prove absence of company news or catalyst risk. Source: https://www.nice.com/investors.
- Current outcome remains WATCH / NOT READY for both. See docs/Validation/Overnight-Candidate-Review-20261026.md for values and caveats.


## Update — 2026-10-27 (API smoke test, Swagger, Boring Overnight, TOS bubble)

- User independently ran `GET /api/health`: status OK, decision-support-only, order submission NOT IMPLEMENTED.
- User independently ran `/api/providers/status`: Alpaca `VALIDATED_DATA_RETURNED`, last successful response 18 bars. This means a non-empty data response was returned during that API process; it is not proof of streaming or full coverage.
- User ran `/api/market-data/compare` for SPY 5m on 2026-10-23 13:30Z–13:50Z: Alpaca returned 5 IEX bars, timestamps 13:30Z and 13:50Z, no duplicates/gaps/error, but `persistedObservationCount=0` in this latest response. Massive remains unconfigured. Investigate why the API returns zero newly persisted observations (could be idempotent already-seen bars; verify store semantics before treating as a defect).
- User reports `dotnet test MomentumCommandCenterV2.sln --configuration Release`: 88 passed. This is user-reported test evidence; check CI against exact branch head after the latest commits.
- Added Swashbuckle and Swagger UI at `/swagger`; documented usage in `docs/API-Swagger-Workflow.md`. Local launch: `dotnet run --project src/MomentumCommandCenterV2.API --configuration Release`; open `/swagger` on the printed origin.
- Added `POST /api/boring-overnight/evaluate` plus deterministic hard-gate evaluator and unit tests. This is not yet connected to live scanner inputs. Missing critical data/catalyst/permission yields DATA?; hard failures BLOCKED; extension/current-day heat warnings WATCH. No orders are submitted.
- Limit target analysis is in `docs/Validation/Boring-Overnight-Target-Policy-20261027.md`. Do not declare +0.5% optimal or use median ON % as a fill probability without complete 07:00–20:00 Eastern GTC_EXT bars, walk-forward testing, and actual fill data.
- TOS bubble issue is NOT resolved. User applied the prior change without errors and sees no bubbles. Repository `docs/Charts/1M/MainStudy.txt` is V14.4.10, while the uploaded source reference is `MomentumCommandCenterV3_2_1M_V14_5.txt`. Exact installed source parity is not established; patch the exact V14.5 source and test on a historical EOD transition rather than re-editing the older reference blindly.
- Alpaca proof-of-fit remains historical REST only. No websocket, broad scanner/movers, or full GTC_EXT collector is implemented. Keep Massive deferred.
- Do not merge the divergent feature branch wholesale into master. Use a scoped PR after current-head CI and code review.


- Additional TOS follow-up: generated a corrected copy of the uploaded V14.5 source as `/mnt/data/MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It adds a one-time red EOD GET OUT bubble when `finalState` transitions into `EOD_GET_OUT`. This file is a local generated artifact, not yet committed to the repository. User must install this exact V14.5 file into the lower 1M study and inspect a chart containing the EOD transition; if the chart still shows no bubbles, collect a screenshot including the prior bar and confirm the installed study source/version. This remains unverified in TOS.


## Update — 2026-10-27: StrategyReport API, exclusions, Swagger examples, ThinkScript cleanup

- Latest branch head at handoff update: feature branch contains `POST /api/strategy-report/upload`; it accepts multipart/form-data field `file`, .csv only, maximum 10 MB, parses up to 10,000 valid rows, returns original headers, full parsed rows, first 100 preview rows, row warnings and likely timestamp/symbol/event columns. It does not infer absent bars/fills. Parser tests added; latest CI run must be verified.
- Added MCC local exclusion watchlist endpoints: `GET /api/watchlists/exclusions`, `POST /api/watchlists/exclusions` with `{ "symbol": "TTE", "reason": "Manual exclusion" }`, and `DELETE /api/watchlists/exclusions/TTE`. Persisted at API `App_Data/exclusions-watchlist.json`. Boring Overnight evaluation now reads this list and overrides caller-supplied `excluded` state.
- Important limitation: these endpoints do NOT sync to the Thinkorswim personal watchlist. No supported TOS API for personal watchlist CRUD has been verified in sources. Treat MCC's list as separate until export/import or another supported integration is established.
- Swagger is now enabled outside Development too, at `/swagger`, with example descriptions and a multipart file picker schema for StrategyReport. Only expose on a trusted local network; no authentication is implemented.
- The cleaned V14.5 ThinkScript file is available in the conversation attachment `MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It reflows statements, comments wrapped prose that was being parsed as code, and adds a red EOD transition bubble. Not TOS-compiled/visually verified; user reported earlier file errors and no bubbles. Test this exact file, don't assume success.
- User-reported prior local Release test: 91 passed before this new change set. GitHub Actions run 40538766766 for the current head was queued at the last check; no current-head pass claim.


## Final UI/CI checkpoint — 2026-10-27

- Added visible API-backed panels to the dashboard: MCC exclusion watchlist (add/remove/refresh) and StrategyReport CSV upload (parse, show count/columns/warnings/preview, download complete parsed JSON). Existing browser-local "Working candidates" list is separate and not the exclusion list.
- Swagger endpoint descriptions contain examples; Boring Overnight evaluator and exclusion add request bodies have OpenAPI examples, and StrategyReport upload exposes a multipart file picker schema. Swagger remains unauthenticated; keep API bound to localhost/trusted network only.
- First workflow run for the API changes failed at build because `WithOpenApi` extension was not referenced. Added `Microsoft.AspNetCore.OpenApi` package 8.0.0. Latest workflow run 40539484300 for commit `2c811c5da83cb5e807e9f95ef81eae05cb152de8` was in progress at last check. Verify this exact head after the next commit; do not reuse the user's earlier 91-test result as current-head proof.
- StrategyReport API returns all parsed rows up to 10,000 plus a 100-row preview. Parser unit tests were added. The front-end downloads the full response as JSON to avoid forcing a large report into the page.
- Exclusion watchlist writes `App_Data/exclusions-watchlist.json`; ensure local directory is writable and back it up if exclusions are important. This does not sync to TOS. User still needs to maintain TOS and MCC lists consistently or provide an export format for import.
- Cleaned ThinkScript file download: `MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It removes blank-line bloat and fixes wrapped header prose that caused ThinkScript to parse fragments as code, plus adds the EOD transition bubble. Not TOS-compiled or visually verified.


## 2026-10-27 — StrategyReport parity conversion and Alpaca reliability follow-up

- User fixed the previously broken commit. Their fix commit was `0caf00004a837e269c175f7d4c34827120ac8fe6` (“Fixed broken commit”); GitHub Actions run 40541586544 succeeded for that exact SHA. This was before the latest changes described below.
- StrategyReport upload now attempts an exact MCC Chart Parity Template conversion for rows containing explicit `MCC_OHLCV|v1|symbol|timeframe|unixMillis|open|high|low|close|volume` payloads. It returns `parityRows` with the 17 template columns and the dashboard offers “Download Parity Template CSV”. Non-MCC StrategyReport rows remain available in the ordinary parsed data and are not invented as chart OHLCV. A regression test has been added.
- Alpaca historical adapter now has bounded next-page-token pagination, up to 100 pages, 3 attempts for transient network/timeout/429/5xx errors, requested-window timestamp filtering, and timestamp deduplication.
- Added `GET /api/market-data/bars?symbol=TTE&timeframe=1m&from=...Z&to=...Z&providerName=Alpaca` to fetch normalized source bars, persist new observations, and report feed, timestamps, gaps, and newly persisted count. 31-day maximum. This is request-driven historical retrieval, not a background collector or websocket.
- CI caught a compile error because the result record was not updated with `ParityRows`; this was corrected in commit `dfaa0fa50e1aa85e3953c251a328ec68c2b46655`, and the regression test commit is `25f2b7eec9fc5a6289c23f29cc91fcb4534f95de`. Workflow run 40545469124 was queued at the last check. Verify current head before pulling; do not rely on the earlier green run.
- Remaining implementation: background collector and restart recovery, persistence/cursor watermark, session-aware coverage/gaps, real scanner candidate source, live computed daily/overnight statistics with explicit formula, and 5M/1M TOS state input. Do not claim live Boring Overnight qualification until required inputs are available and validated. Alpaca OHLCV alone cannot truthfully reconstruct the authoritative Thinkorswim 5M permission and 1M state.
- TTE duplicate bubbles: do not patch a different older study as though it were the installed V14.5. Latest installed V14.5 source/screenshot has not been located in the repo's current tracked study path. Existing V14.4.10 source already uses a composite state bubble plus optional debug bubbles; stacking likely comes from multiple enabled bubble-producing paths or multiple event sources on the same bar. Need exact current V14.5 source to safely consolidate every active AddChartBubble call into a single event bubble per bar and assign a non-overlapping vertical offset. User's uploaded Project Sources screenshot was not directly available from repository search.
