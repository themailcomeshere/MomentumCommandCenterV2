# Momentum Command Center V3 — Master To-Do

**Canonical location:** `docs/Master-To-Do.md`  
**Reviewed:** 2026-10-16 (Arizona time)  
**Branch:** `feature/opportunity-quality-classification`

## Status and migration note

This file preserves the prior consolidated To-Do text below, after reviewing all 180 lines. Completed repository tasks are marked in the current status addendum at the end; unresolved ThinkOrSwim/chart validation tasks remain open because source-controlled files do not prove which study is installed in the user's TOS workspace. The prior reference to a Boring Overnight TODO under `/Trading System/` is not present in this repository; the criteria are now documented in `docs/BoringOvernight.md`. The current repository's `docs/Charts/1M/` and `docs/Charts/5M/` studies are the source-controlled references; do not infer that they match the installed study without comparison.

# THIS IS A .MD FILE

# Momentum Command Center V3 — Consolidated Master To-Do
**Updated:** 2026-10-15 (Arizona time)  
**Purpose:** Canonical master list. Consolidates the 2026-10-06 master TODO, the 2026-10-13 Boring Overnight Decision Column TODO, and subsequent documented project status. The dedicated Boring Overnight TODO remains a detailed companion; this master list must be updated whenever that companion changes.

## Status key
- [NOW] Current priority
- [NEXT] Next in sequence
- [HOLD] Deliberately deferred
- [VALIDATED] Substantially validated, not a claim of profitability
- [DONE] Completed and verified
- [UNVERIFIED] Must be checked against repository/current installed artifact

## 1. 5M authority / Permission — [NOW]
- [NOW] Reconstruct the known XRPN 10/2 Permission `1 -> 0` transition at 12:45 Arizona chart time. Known facts already supplied: Permission 0 premarket; Permission 1 plus BUY at 8:25; Permission 0 by 12:45. Do not ask user to rediscover these transitions.
- [EVIDENCE REVIEWED, 2026-10-22] First screenshot (`Screenshot 2026-10-22 161411.png`) showed the normal 5M study and Authority Diagnostic V2 working on morning bars, but not the target transition.
- [TARGET WINDOW CAPTURED — BRANCH INDICATED, 2026-10-22] The follow-up Project Source screenshot (`Screenshot 2026-10-22 161705.png`) shows XRPN 10/2/26 5M around 12:10–12:55 Arizona chart time, with both studies visible. Around the Permission revocation, red diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, and `EOD=0`; the displayed conditions include `VWAP=0`, `EMA9=0`, and `EMA9>20=0`. In Authority Diagnostic V2, `PBLOCK=2` is the persistent-major-failure branch, so the screenshot strongly indicates that branch caused the revocation rather than EOD. Verify the exact ordering/priority in source before changing logic; do not infer a defect from the revocation alone.
- [AUDIT RESULT — 2026-10-22] The follow-up screenshot indicates `PBLOCK=2` (persistent major failure) with `EOD=0` on the revoked bars. `docs/Charts/5M/MainStudy.txt` confirms `majorFailureRaw = price < dailyVWAP AND price < ema9 AND ema9 < ema20 AND ema20 < sma50`; `majorFailurePersistent` requires this condition on `majorFailureBars` bars (default 2). The Permission latch checks EOD first, then persistent major failure, then persistent structural damage, and recovers only when `bullishPermissionRaw` is true. This matches the observed PBLOCK mapping and does not by itself demonstrate a logic defect. Preserve Permission logic; proceed to the 5M→1M handoff audit.
- [USER ACTION REQUIRED] On the same XRPN 10/2/26 5M chart, zoom/pan the **5M chart itself** to show approximately 12:30–1:00 PM Arizona, with the normal 5M state bubbles, Authority Diagnostic V2 bubbles, and readable time axis all visible together. Send that screenshot. Keep the 1M pane if convenient, but the 5M transition is the evidence needed.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- [NOW] Audit the 5M→1M handoff in source: list the 5M fields/states reconstructed by the 1M study, compare the gate and runner/waning/swing interactions against the intended contract, and record any concrete mismatches before editing code.
- Verify fresh candidate, in-trend candidate, continuation, confirmation, event-armed READY, expiration, and re-entry paths obey the intended 5M gate.
- Verify RUNNER / WANING / SWING do not suppress legitimate 1M opportunities unless intended.
- Verify extension danger blocks chasing without falsely declaring the 5M thesis dead.
- Preserve architecture: 5M never references 1M; 1M may reference 5M.

## 3. 1M execution pipeline — [HOLD broad edits; audit targeted behavior]
- [UNVERIFIED] Reconcile current user-installed study with Library copies: `V14_4_9_AggressiveSetupHandoff_CompositeBubble` (known compile-clean diagnostic baseline, duplicate files) and `V14_4_10_AggressiveAuditOnly` (later audit-only files, duplicate copies). Filename/modified date alone does not establish production baseline.
- Trace actual transitions and bars before editing; avoid version churn that changes only labels or does not address a demonstrated defect.
- Validate fresh execution candidate, in-trend candidate, continuation candidate, confirmation event, event-armed opportunity, finite READY window, READY event, candidate-block reasons, execution expiration, extension warning/danger, swing reclaim/re-entry.
- Preserve negative test: opportunity can exist while READY is blocked by extension danger. This is not automatically a broken bullish thesis.
- Validate historical debug on scrolled bars; use per-bar bubbles for history rather than relying on right-edge AddLabel values.
- Distinguish `NO NEW SIGNAL` from HOLD or EXIT. Missing signal bubbles are not themselves a SELL signal.

## 4. Runner management — [NEXT after authority/handoff validation]
- Validate RUNNER RISING -> RUNNER; RUNNER -> RUNNER RISING; RUNNER -> RUNNER WANING; WANING -> SWING ZONE; SWING recovery; and WANING/SWING -> actual thesis failure.
- Runner waning is caution, not automatic SELL. Preserve goal of holding valid runners rather than repeatedly taking tiny profits.
- Define and test explicit position-management/exit behavior separately from entry readiness.

## 5. Boring Overnight decision column / chart study — [NOW: specification first]
Detailed companion: `docs/BoringOvernight.md`. Preserve its complete criteria and update it alongside this master.

### Required pipeline
1. Overnight Edge Score (historical overnight profitability/edge).
2. Boringness Score (consistency and controlled risk/return distribution).
3. Event/Catalyst Risk (earnings, M&A, regulatory/government decisions, corporate actions, material news, sector/regime shocks).
4. Current-Day Qualification (abnormal intraday extension, unusual volume, fresh catalyst spike, excessive ON move, deterioration from normal behavior).
5. 5M structure/permission where appropriate.
6. 1M entry timing where appropriate.
7. Decision status: `READY`, `WATCH`, `BLOCKED`, `DATA?`, plus reason codes and data freshness.
8. Zero position is valid; never silently treat missing/stale data as READY.

### Initial gates to preserve pending validation
- Price >= $5.
- 20-day average volume >= 500,000 shares/day.
- Overnight win rate >= 60%.
- 20-day average ON return >= +0.10%.
- 5-day average ON return > 20-day average ON return under current cheat-sheet rule.
- Price above 20-day SMA.
- Not on the maintained Schwab exclusion list.
- No material event/catalyst risk incompatible with boring overnight holding.

### Initial Boringness score (100 points)
- ON win % 25; median ON % 20; 20D average ON % 15; 5D average ON % 10; consistency 10; overnight volatility 10; average volume 5; price 5.
- Bands: 90–100 PERFECT BORING; 80–89 VERY GOOD; 70–79 POSSIBLE; 60–69 AGGRESSIVE; below 60 REJECT / NOT BORING ENOUGH. Score never overrides hard gates.
- Preferred ranges: current ON move <3% (>=3% increases penalty/review); 20D average +0.10% to <2.0%; 5D average positive to <5.0%; consistency >=70% preferred and >=80% excellent; median should be positive and reasonably close to mean.
- Penalize abnormal intraday moves/volume, sharp extension, fresh news/earnings/catalyst, M&A/regulatory/government decisions, corporate actions, sector/regime shocks, deteriorating 5M structure.
- Existing exclusion list includes `SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE`; other maintained lists contain additional exclusions. Reconcile all current sources; do not assume this subset is exhaustive.

### Implementation sequence
- [NOW] Write column specification/data contract: field definitions, windows, session boundaries, source, freshness, missing-data handling, reason codes, extended-hours assumptions.
- Implement only calculations ThinkScript can reliably support; label proxies honestly. Split into coherent columns if one column truncates critical information.
- ThinkScript cannot establish catalyst-clear status reliably or retain full authoritative rolling history. Missing event data => DATA? / manual review, never READY.
- Validate known winners, losers, catalyst spikes, overextended candidates, and all hard-gate failures. Compare same-symbol/same-date ThinkScript results against independently calculated C# results.
- Add chart study only if it explains information not usefully shown by the column.
- Track false positives, missed opportunities, ON win rate, average/median return, adverse excursion, spread/slippage. Do not infer profitability from compilation or a few chart examples.
- No automatic order placement until a separate risk/order-control workflow is specified and tested.

## Scanner integration readiness and EXT prequalification (2026-10-22)

- [NOW] Define a provider-neutral scanner-event contract before wiring any provider: normalized symbol, scanner group, source/event ID when available, first-seen and observed timestamps, data-as-of timestamps, raw-field provenance, schema/version, freshness, current status/reason codes, and evaluator/study version. Discovery is not an entry signal.
- [NOW] Add extension qualification to the scanner design. The user's reported problem is that red-state 1M EXT / extension-danger often coincides with no EXECUTION READY bubble, wasting chart-review time. Keep this as a measured workflow hypothesis until timestamped examples quantify it.
- [NOW] Default actionable scanner queue must deprioritize/filter candidates whose current extension exceeds the exact versioned 1M study envelope. Preserve them in a recoverable WATCH_PULLBACK list; do not permanently discard a symbol merely because it is extended now. Pullback requalification requires fresh data, a new valid event, and unchanged 5M authority / 1M execution gates.
- [NOW] Distinguish extension warning from extension danger. Never substitute a guessed percentage threshold or the provisional OpportunityQualityAssessor threshold for the exact ThinkScript ATR-extension definition. Missing/stale extension inputs => DATA?, never “not extended” or ACTIONABLE.
- [BLOCKER] Before live scanner gating, complete exact-study indicator parity for ATR/extension and related gates; classify the 31 irregular 1M intervals; finish full-session 1M-to-5M reconciliation; repeat RTH/extended-hours and Arizona/Eastern DST tests. See docs/Validation/MCC-OHLCV-20261020.md.
- [NEXT] Implement provider-neutral scanner/discovery contracts and append-only first-seen history in Application/Core with deterministic tests for freshness, deduplication, missing data, and state transitions. Keep provider-specific code out of Core.
- [NEXT] Implement extension prequalification only after its inputs and thresholds have parity tests. Verify warning/danger behavior, pullback requalification, stale-data handling, and that scanner filtering never changes the 5M thesis.
- [BLOCKED — PROVIDER DECISION] Choose a provider only after API coverage, licensing, rate limits, latency, credentials, and failure behavior are specified. Data not found in sources: a selected live scanner provider and verified access contract.
- [NEXT] Run shadow mode against timestamped scanner discovery, first 5M Permission, first 1M READY, extension blocks, pullback/re-entry, and outcomes. Measure chart reviews avoided and valid opportunities hidden before enabling hard exclusion by default.
- [HOLD] Do not connect live scanner results to orders or let scanner/quality ranking override 5M authority or 1M READY.

## 6. Scanners / daily workflow — [PARTIAL: local workbench implemented]
- [DONE] Add a local scanner/watchlist workbench with four scanner groups, candidate statuses/reasons, local CSV import, and browser-local watchlist persistence. See `docs/Scanner-and-Watchlist-Workbench.md`.
- [HOLD] Reconnect the Command Center to a live scanner workflow until 5M/1M behavior is sufficiently validated and a provider-neutral scanner contract exists.
- Scanner groups: A Momentum Hunter; B Breakout Trigger; C Reclaim/Continuation; D Market Movers/Discovery (daytime, not Overnight).
- Maintain regular-session default and separate overnight workflow explicitly.
- Daily Gainer Review after 6 PM Arizona time: regular-session biggest intraday gainers only, exclude overnight holds; record scanner appearances/timing, chart states, news/catalysts, Schwab tradability/exclusions, and miss classification.
- Preserve timestamped scanner screenshots and actual trades for V2/V3 validation.
- Keep Schwab-unavailable/undesired securities excluded across relevant scans; reconcile maintained lists rather than replacing them with a partial list.

[NOW] Scanner discovery timestamp and missed-move audit
	• Record the earliest known timestamp each symbol appears in each scanner, including scanner/group name and the scan result at that time.
	• Record when the symbol is first added to the working watchlist, if different from its scanner appearance.
	• Preserve timestamped scanner snapshots or append-only candidate logs so discovery history is not lost when scanner results change.
	• For each candidate, correlate discovery time with:
		○ First 5M Permission=1 and its EXT value.
		○ First 5M BUY / PREPARE TO BUY / runner state.
		○ First 1M EXECUTION READY.
		○ READY expiration and candidate-block reasons, including extension danger.
		○ Subsequent runner waning, pullback, swing recovery, or thesis failure.
	• Calculate the price move already realized between the earliest recorded discovery and first actionable READY, with a clearly defined reference price and session boundary.
	• Classify missed opportunities as discovery too late, qualification too late, execution blocked/expired, no valid setup, data unavailable, or unknown.
	• Track scanner misses and false positives across historical sessions before changing scan thresholds or signal logic.
	• Keep regular-session DGR separate from Boring Overnight tracking.

From <https://chatgpt.com/g/g-p-6ad0f6bcb72c81918e3cdd194cda6630-momentum-command-center-v3/c/6ad0f88f-d89c-83e8-9eeb-9923a03fc1d2> 





## 7. C# V3 implementation — [NEXT / data and replay foundation]
Repository supplied by user: https://github.com/themailcomeshere/MomentumCommandCenterV2.git
- [DONE] Verify the actual repository branch, latest commit, solution/project tree, framework, namespaces, and build/test commands directly against the repository. The working branch is `feature/opportunity-quality-classification`; it targets .NET 8.
- [DONE] `ITradeImporter`, `SchwabTradeImporter.ImportFile()`, and `TradeImportResult` exist in the current solution.
- [PARTIAL] Importer validation and xUnit tests exist. Expand coverage for any uncovered duplicate/idempotency, partial-failure, and diagnostic cases when importer work resumes.
- [NEXT] Persistence, provider-neutral indicator snapshots, decision logging, market-data parity, and later production UI remain future work. This milestone adds raw-bar replay primitives only.
- Keep boundaries clean: raw import -> normalized application/domain models -> persistence -> bar/indicator calculations -> deterministic 5M authority -> 1M execution -> separate overnight decision -> logged outcome/backtest.
- Use pure, testable calculations/state transitions; make data source, time zone, session boundary, missing-data and freshness behavior explicit.
- C# should own/consume authoritative rolling ON stats, median/mean and 5D/20D metrics, consistency/volatility, scores, catalyst metadata, freshness, reason-coded decisions and outcome logs.
- Record candidate snapshots, status/reasons, intended and actual entry/exit, and outcomes for audit and threshold evaluation.
- Preserve `///` XML documentation with flowerboxing. Deliver complete coherent code and next-step instructions together.
- No claims that live brokerage integration, external event feeds, backtesting, automated orders, or production UI exist until verified in source/tests. No automated orders until separately specified and tested.
- Ensure any intentional divergence between C# and TOS behavior is documented and covered by comparison tests.

## 8. Repository README / docs — [PARTIAL]
- [DONE] README and architecture/data contracts have been refreshed from verified repository contents. The original README is preserved at `docs/archive/README_ORIGINAL.md`.
- [PARTIAL] README now documents .NET 8 architecture, replay command, demo UI, safety boundaries, and core roadmap. Provider configuration, scanner interfaces, and deployment workflow remain unimplemented and are not claimed.
- Keep source-of-truth boundaries explicit: current TOS studies are live behavioral reference under validation; C# is intended to make logic/data/decisions reproducible and testable.

## 9. Historical validation / diagnostics — [NEXT]
- Resolve known XRPN 10/2 Permission revocation before collecting new-day evidence for that question.
- Then use controlled sessions to gather transition evidence, not assume production readiness.
- Historical diagnostics must show per-bar state accurately when scrolled; distinguish current/right-edge state from historical bar state.
- [DOC ADDED] Use `docs/Charts/1M/DiagnosticStudy.txt` as the audit procedure for historical 1M READY bubbles. It documents the existing per-bar diagnostics in `MainStudy.txt`; it does not alter signal logic or claim the installed Thinkorswim study matches the repository source.
- Compare scanner candidates, signals, fills, exits, and outcomes; classify false positives, missed trades, invalid entries, exits too early, and missed runner continuation.
- Validate Arizona/Eastern session conversions and EOD logic across DST/standard-time boundaries.

## 10. User interaction / operating rules
- Correctness and thoroughness over expediency. Review all relevant Library items, not just the newest one.
- On screenshot analysis, identify date/session boundary and timeframe; explicitly separate observed facts from inference and unknowns.
- For study changes, use handoff heading “1. What I changed”; explain screenshot purpose, exact Study/Library artifact, user actions, and next steps.
- Do not request screenshots or transitions already provided. Do not misstate current Arizona time or tell user to wait for a future session when already in it.
- Do not create new study versions without a demonstrated defect and explicit test purpose; confirm compilation and chart effect before calling it successful.
- Trading signals are evidence, not guarantees. `EXECUTION READY` does not replace position sizing, risk limits, or exit rules. Distinguish HOLD, EXIT, and NO NEW SIGNAL.

## 11. Immediate order of operations
1. [NOW] Build the current branch and run the full xUnit suite; GitHub file edits do not execute .NET.
2. [NOW] Reconcile the repository's 1M/5M studies with the actually installed ThinkOrSwim versions; do not infer identity from filenames.
3. [NEXT] Define timestamp open/close semantics, session/extended-hours policy, and DST behavior for historical bars.
4. [NEXT] Add sanitized real historical fixtures and golden indicator-parity tests against the source studies.
5. [NEXT] Connect replay frames to V2 signal evaluation without changing existing signal rules; log transitions and opportunity assessments.
6. [NEXT] Investigate the known XRPN 10/2 Permission revocation from existing chart evidence; keep this separate from speculative edits and replay-foundation work.
7. [NEXT] Continue targeted 5M/1M handoff, READY expiration/extension, runner, and exit-behavior validation.
8. [NEXT] Resume scanner-discovery correlation, trade-quality analytics, and V1/V2 historical validation after data parity is trustworthy.
9. [HOLD] Production UI/provider/news integration and any order automation until their contracts and safety requirements are separately specified.

---

## 12. Repository and documentation status update (2026-10-16)

### Done in this branch
- [DONE] Inspect the current branch and latest commits, including separated Models, development rules, chart-study source files, and documentation additions.
- [DONE] Add XML flowerboxing with purpose/when-called context to current classes and methods; a follow-up source audit found no unflowerboxed method signatures in the inspected C# files.
- [DONE] Add raw timestamped OHLCV bar contract and deterministic replay foundation; replay emits frames at 1M timestamps and attaches only 5M bars with timestamp <= the frame timestamp.
- [DONE] Add CSV ingestion with explicit timezone-offset requirement, invariant numeric parsing, and duplicate/malformed OHLCV validation.
- [DONE] Add CLI replay command and synthetic CSV fixture.
- [DONE] Add unit tests for no-look-ahead, duplicate bars, and invalid OHLC.
- [DONE] Add `docs/Market-Data-and-Replay-Contract.md`.
- [DONE] Expand `docs/BoringOvernight.md` with Design vs Use, gates, score components, ranges, catalyst risk, and limitations.
- [DONE] Populate `docs/NewChatHandoff.md`.
- [DONE] Scaffold static front end under `frontend/`; it is demo-only and not connected to market data or a broker.
- [DONE] Put development rules in `docs/rules/abpDevRules.md` (already moved by user).

### Still open / not verified by this branch
- [NOW] Run the local build and all tests after the new replay/adapter/CLI changes; GitHub file operations do not run .NET.
- [DONE] Add XML flowerboxing to the current C# class/type and method declarations identified during the repository audit; continue enforcing this on all new and changed code.
- [NOW] Reconcile the source-controlled chart studies against the actual installed 1M/5M ThinkOrSwim studies; filenames and repository state do not establish runtime version.
- [NEXT] Add sanitized historical 1M/5M fixtures and provenance.
- [NEXT] Define bar timestamp open/close convention, session calendar, extended-hours policy, regular-session VWAP reset, and DST handling.
- [NEXT] Port TOS indicator formulas with golden parity tests before connecting calculated values to the signal engine.
- [NEXT] Connect replay frames to existing V2 signal evaluation without changing signal behavior; persist transitions and opportunity assessments.
- [NEXT] Add daily trading-session grouping, MFE/MAE, hold-time/entry-to-peak/exit-giveback, runner/early-exit/missed-runner analysis, limit-price analysis, scanner events, and V1/V2 historical validation.
- [NEXT] Reconcile all Schwab exclusion lists; the documented list is not guaranteed exhaustive.
- [HOLD] Live market-data provider, news/catalyst feed, production front-end/API, and broker connectivity until the contracts and provider are chosen.
- [OUT OF SCOPE] Automated order placement and autonomous Schwab submission.

### Line-by-line disposition of the prior checklist

- Sections 1–4 (5M permission, 5M→1M handoff, 1M execution, runner management): retained as unresolved TOS validation work. No speculative study edits were made.
- Section 5 (Boring Overnight): strategy criteria migrated into `docs/BoringOvernight.md`; implementation and historical validation remain open. Its former companion path under `/Trading System/` did not exist in this repository.
- Section 6 (scanners/Daily Gainer Review): retained as future scanner-event and discovery-timestamp work; no scanner data source is connected.
- Section 7 (C# V3 implementation): repository was inspected; importer/trade-analysis work already exists in the solution. Market-data/replay is now a first slice, not a complete backtest. Remaining persistence, historical market correlation, event sourcing, and provider work stay open.
- Section 8 (README/docs): README and the replay contract were updated from verified repository content; this master list and handoff now exist under `docs/`.
- Section 9 (historical validation): retained. Replay primitives exist but no source-study parity, fills, or P&L simulation is claimed.
- Section 10 (user interaction/operating rules): retained as operating constraints; development-specific rules are in `docs/rules/abpDevRules.md`.
- Section 11 (immediate order): reprioritized to build/test this milestone first, then TOS version/behavior audit, then data/session/indicator parity, then signal/replay integration, and later trade/scanner analytics. The historical XRPN 10/2 revocation remains a separate unresolved investigation requiring chart evidence; it is not solved by this C# replay slice.
- The old README-derived trade importer and statistics bullets remain relevant background; implemented functionality is described in `README.md`, while market-dependent performance judgments remain unverified until historical bars are available.


- [DOC ADDED] `docs/Research/ClearStreet-Comparison.md` records relevant product patterns (unified decision surface, multi-factor discovery, transition alerts, risk context, API contracts) and explicitly excludes broker infrastructure/automated execution from MCC scope.
- [RULES UPDATED] `docs/rules/thinkscript.md` now reflects MCC's 5M-authority/1M-execution hierarchy, historical-bar semantics, formula-fidelity requirements, and validation gates; generic `AddOrder()` advice no longer implies orders belong in a chart study.

## 13. Boring Overnight source reconciliation

The original master list and `docs/BoringOvernight.md` agree on the initial 100-point weights, hard gates, score bands, preferred ranges, “too hot” penalties, event/catalyst caution, 5M/1M review, and zero-position principle. The exclusions list is explicitly partial pending reconciliation. Overnight Edge and Boringness must remain separate; no final score should override a failed/unknown hard gate.

## 14. Current execution checkpoint (2026-10-18)

- [BLOCKED HERE / STILL OPEN] Attempted to clone the current branch and run `dotnet test MomentumCommandCenterV2.sln`; this environment could not resolve `github.com`, so the clone failed before restore/build/test. This is an environment/network limitation, not a code failure and not a passing test result.
- [NOW] Use the repository's GitHub Actions `.NET build and test` workflow to validate the current branch, or run the documented Release restore/build/test commands locally. Confirm the run corresponds to the current branch head before marking this item DONE.
- [NEXT] Once build/test is green, reconcile the installed Thinkorswim 1M/5M studies against `docs/Charts/1M/MainStudy.txt` and `docs/Charts/5M/MainStudy.txt`; then capture sanitized historical bars with provenance before indicator parity work.
- No signal logic, ThinkScript thresholds, or replay semantics were changed during this checkpoint.

## 15. Opportunity classification: practical verification and news boundary (2026-10-18)

- [DOC ADDED] `docs/Opportunity-Classification-Verification.md` defines a shadow-mode workflow, required input provenance, outcome comparisons, acceptance gates, and the boundary between a news provider, web search, and an LLM.
- [USER-REPORTED PASS] User reports all unit tests pass. This is recorded as user-reported; no local build/test execution was possible in the prior environment attempt. Do not confuse unit-test success with live market-data parity or profitability.
- [NOW] User is handling sanitized historical 1M/5M fixtures and provenance, plus session/timezone definitions.
- [NOW] Implement MCC-owned OHLCV export contract/parser and preserve the legacy `kg_EveryTickSOHLCP` report as a rejected negative fixture. Complete Thinkorswim compilation, timestamp/order-name verification, native-export comparison, and consecutive 1M/5M chart parity before treating any export as production-ready.
- [NEXT] Implement golden indicator-parity tests against exact versioned ThinkScript studies, then connect replay to the existing V2 signal engine without changing rules.
- [NEXT] Build shadow-mode opportunity snapshots and outcome linkage only after timestamp/session/indicator parity. Keep classification diagnostic-only until out-of-sample evaluation demonstrates value over the READY-only baseline.
- [HOLD] News/catalyst provider selection and AI-assisted headline classification until coverage, licensing, latency, costs, provenance, and unknown/stale behavior are specified. Current repository has no verified live-news or per-symbol ChatGPT/Google query integration.

## 16. MCC-owned OHLCV export checkpoint (2026-10-20)

- [DONE] Add the uniquely named `MCC_OHLCV_V1` strategy artifact with explicit date, Eastern-time session, aggregation, actual-volume payload, visible diagnostics, and simulated close/reopen report transport.
- [DONE] Add the strategy-report parser and reject legacy `kg_EveryTickSOHLCP` rows, transport-only reports, malformed OHLC, invalid timestamps, negative volume, duplicates, and symbol/timeframe mismatches.
- [DONE] Validate the repository against successful 1M and 5M report shapes. `StrategyReports_XRPN_102026_933.csv` contains 355 1M payload rows; `StrategyReports_XRPN_102026_5m_offset_1027.csv` contains 81 5M payload rows.
- [DONE] Enforce that `aggregationLabel` matches the actual Thinkorswim chart aggregation; a mismatch must emit no payload rows and show a diagnostic mismatch state.
- [DONE] Add dependency-free PowerShell validation scripts under `tools/` for single-report structure, 1M/5M export comparison, and report-versus-payload timestamp offsets.
- [DONE] Add `tools/New-MccChartParityTemplate.ps1` and generate first-20-row 1M/5M comparison templates under `data/validation/`.
- [DONE] Add a local scanner/watchlist front-end workbench without claiming live provider, news, broker, or order connectivity.
- [DONE] Record automated 1M/5M validation results in `docs/Validation/MCC-OHLCV-20261020.md`: 52 complete 5M windows reconstructed from 1M rows match OHLCV; 29 windows are incomplete; report timestamps are transport metadata.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 1M sample contains 20 consecutive bars (13:28–13:47 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; rows with precision differences are labeled `ROUNDING ONLY`. See `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv`.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 5M sample contains 20 consecutive bars (13:20–14:55 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; precision differences are labeled `ROUNDING ONLY` where applicable. See `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Constituent 1M-to-5M reconstruction is independently possible only for the overlapping complete 1M windows in the supplied 1M sample; the full-session reconciliation remains open.
- [VALIDATED] The `_UPDATED_3.csv` 1M and 5M files provide 20 consecutive payload rows each and now include independent chart OHLCV values and row-level findings. This completes the requested manual chart sample for those two windows; it does not by itself classify the separate 31 gaps in the broader 1M export.
- [DONE — SAMPLE PARITY] Independent chart OHLCV values are supplied for 20 consecutive 1M bars and 20 consecutive 5M bars in the `_UPDATED_3.csv` files. The broader 1M export's 31 gaps remain unclassified and require separate evidence.
- [VALIDATED — OVERLAP CHECK] The three complete shared 5M windows beginning 13:30, 13:35, and 13:40 UTC reproduce the 5M payload OHLCV exactly when their five constituent 1M payload bars are aggregated. The 13:45 window is not fully testable from this 1M sample because 13:48 and 13:49 are outside its range. Details: `docs/Validation/MCC-OHLCV-20261020.md`.
- [DONE — CLARIFIED] The raw Strategy Reports remain payload-only and are not completed parity evidence. The completed manual comparison evidence is in `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv` and `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Do not revert to the original templates or treat `_UPDATED_2.csv` as the latest completed sample.
- [NEXT] Classify the 1M export's 31 gaps and determine whether they are chart/session gaps or export omissions. Do not fill them until classified.
- [NEXT] Repeat parity under RTH and extended-hours settings and across an Arizona/Eastern DST boundary; preserve a provenance manifest for each run.
- [NEXT] Investigate native Thinkorswim chart-data export as the preferred transport if it exposes the same timestamp/OHLCV fields without strategy-order timing semantics.
- [HOLD] Do not mark the export production-ready, connect it to replay as authoritative market data, or calibrate signal/opportunity logic until the external chart-parity items are complete.
