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
- [NOW] Determine which actual branch caused 12:45 revocation: persistent structural damage, persistent major failure, EOD, or another branch. Use existing chart/debug evidence; do not guess.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- Verify exactly which 5M fields/states the 1M consumes.
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
Detailed companion: `/Trading System/MomentumCommandCenterV3_Boring_Overnight_Decision_Column_TODO.md` (2026-10-13). Preserve its complete criteria and update it alongside this master.

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

## 6. Scanners / daily workflow — [HOLD until core behavior stabilizes]
- Reconnect the Command Center to the scanner workflow after 5M/1M behavior is sufficiently validated.
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





## 7. C# V3 implementation — [NOW / documentation and import layer]
Repository supplied by user: https://github.com/themailcomeshere/MomentumCommandCenterV2.git
- [NOW] Verify actual repository branch, latest commit, solution/project tree, framework, namespaces, and build/test commands. The web retrieval attempted 2026-10-15 did not produce verifiable repository content; do not invent repository facts. Existing README is an architecture working spec, not a verified tree.
- [NEXT] Implement `ITradeImporter` and `SchwabTradeImporter.ImportFile()`; define `TradeImportResult` in `Application.Models`.
- [NEXT] Add validation and tests for supported file formats, headers, malformed rows, duplicates/idempotency, date/time/number/currency parsing, partial failures, and actionable import diagnostics.
- Then build persistence layer; normalized trade/fill models and repository abstractions; analysis/market-data layers; state snapshots; decision logging; later web/MVC UI.
- Keep boundaries clean: raw import -> normalized application/domain models -> persistence -> bar/indicator calculations -> deterministic 5M authority -> 1M execution -> separate overnight decision -> logged outcome/backtest.
- Use pure, testable calculations/state transitions; make data source, time zone, session boundary, missing-data and freshness behavior explicit.
- C# should own/consume authoritative rolling ON stats, median/mean and 5D/20D metrics, consistency/volatility, scores, catalyst metadata, freshness, reason-coded decisions and outcome logs.
- Record candidate snapshots, status/reasons, intended and actual entry/exit, and outcomes for audit and threshold evaluation.
- Preserve `///` XML documentation with flowerboxing. Deliver complete coherent code and next-step instructions together.
- No claims that live brokerage integration, external event feeds, backtesting, automated orders, or production UI exist until verified in source/tests. No automated orders until separately specified and tested.
- Ensure any intentional divergence between C# and TOS behavior is documented and covered by comparison tests.

## 8. Repository README / docs — [NEXT]
- Existing Library docs: `MomentumCommandCenterV3_CSharp_README.md` and commit-stamped copy. Both are conservative working specifications, not source-derived inventories.
- After direct repo inspection, update README with verified project names, target frameworks, namespaces, entry points, configuration, models/services, scanner/data interfaces, tests, build/test commands, and deployment workflow.
- Keep source-of-truth boundaries explicit: current TOS studies are live behavioral reference under validation; C# is intended to make logic/data/decisions reproducible and testable.

## 9. Historical validation / diagnostics — [NEXT]
- Resolve known XRPN 10/2 Permission revocation before collecting new-day evidence for that question.
- Then use controlled sessions to gather transition evidence, not assume production readiness.
- Historical diagnostics must show per-bar state accurately when scrolled; distinguish current/right-edge state from historical bar state.
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
1. Audit XRPN 10/2 12:45 Permission revocation from existing evidence.
2. Validate Permission semantics without speculative code edits.
3. Audit 5M -> 1M handoff.
4. Trace 1M candidate/confirmation/READY/expiry/extension blocks and reconcile installed version.
5. Validate runner management and explicit exit behavior.
6. Specify Boring Overnight column contract; implement reliable ThinkScript subset and C# authoritative calculations; validate historically.
7. Implement `ITradeImporter` / `SchwabTradeImporter.ImportFile()` + `TradeImportResult`, tests, then persistence/analysis.
8. Verify repository source and refresh README from actual code.
9. Resume controlled scanner/trade validation and Daily Gainer Review.

---

## 12. Repository and documentation status update (2026-10-16)

### Done in this branch
- [DONE] Inspect the current branch and latest commits, including separated Models, development rules, chart-study source files, and documentation additions.
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
- [NOW] Complete flowerboxing audit across all pre-existing classes and methods; new replay files have purpose/when-called comments, but legacy code still needs review.
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

## 13. Boring Overnight source reconciliation

The original master list and `docs/BoringOvernight.md` agree on the initial 100-point weights, hard gates, score bands, preferred ranges, “too hot” penalties, event/catalyst caution, 5M/1M review, and zero-position principle. The exclusions list is explicitly partial pending reconciliation. Overnight Edge and Boringness must remain separate; no final score should override a failed/unknown hard gate.
