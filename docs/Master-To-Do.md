## Local Alpaca setup and XRPN evidence — 2026-10-23

- [DONE — SOURCE CHANGE, CI PENDING] Added API `UserSecretsId`, removed empty credential placeholders from tracked `appsettings.json`, documented local User Secrets commands, and standardized Alpaca credential resolution on ASP.NET Core configuration.
- [DONE — TESTS ADDED, CI PENDING] Added `AlpacaMarketDataAdapterTests` for configured credentials/request headers/bar parsing and missing-credentials behavior. These are fixture-based tests and do not contact Alpaca.
- [DONE — EVIDENCE RECEIVED] User supplied the XRPN 2026-10-02 screenshot showing the normal 5M study, Authority Diagnostic V2 and readable target time axis; top-left chart date is explicitly excluded per user clarification. Do not request it again.
- [USER ACTION — NEXT] Create a free Alpaca account, configure credentials locally using `src/MomentumCommandCenterV2.API/README.md`, run a small real API smoke test, and share only sanitized status/errors/timestamps. Do not share credentials. Massive and paid plans remain deferred.
- [OPEN] Check CI/build/test results at the latest branch head after these commits. A source edit or successful prior build does not establish current-head verification.

# Momentum Command Center V3 — Master To-Do

**Canonical location:** `docs/Master-To-Do.md`  
**Reviewed:** 2026-10-23 (Arizona time)  
**Branch:** `feature/opportunity-quality-classification`


## UPC extension-danger example — 2026-10-23

- [PARTIAL — EVIDENCE REVIEWED] Added `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md` for the user's UPC 5M/1M screenshots and event facts. The reported state is 5M `Perm=1`, `PBLOCK=0`, while 1M says `CANDIDATE BLOCKED: EXTENSION DANGER`, `IN-TREND CANDIDATE`, `EXT=2.35`. This is consistent with a valid higher-timeframe permission and a blocked 1M entry; do not weaken the gate.
- [DONE — USER CONFIRMED] Screenshots/event are from 2026-10-22 and Thinkorswim chart timezone is Arizona. This resolves the date/timezone question only; it does not classify the missing intervals or prove study-state parity.
- [OPEN] [PARTIAL] Reviewed `data/StrategyReports_UPC_102226.csv`: this is a strategy report covering 09:37–09:47, not a continuous OHLCV export; it cannot establish the reported 09:16–09:36 missing-bar intervals. Obtain a full bar export or exact timestamp inspection. Missing chart bars do not prove zero volume or no movement; never synthesize them.
- [OPEN] Confirm installed 1M source and `BLK=6` mapping. Study names alone do not prove installed source parity.

## Provider comparison refresh — 2026-10-23

- [DONE — RESEARCH DOCUMENTATION] Expanded `docs/Provider-Comparison.md` to compare free and paid tiers against ultimate MCC requirements. Free tiers are suitable for plumbing/prototyping only; they are not approved as live authority feeds.
- [RECOMMENDED SHORTLIST] Alpaca Algo Trader Plus ($99/mo public list price) versus Massive Stocks Advanced ($199/mo public list price), pending proof of fit, account entitlements, licensing, small-cap/premarket coverage and TOS comparison.
- [OPEN] Run proof-of-fit tests before subscription/provider selection. No provider is selected or connected.

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
- [DONE — USER EVIDENCE RECEIVED, 2026-10-23] The uploaded XRPN screenshot shows the 5M chart on 2026-10-02 with both the normal 5M study and Authority Diagnostic V2 visible and a readable time axis spanning approximately 12:00–13:35 Arizona; the 1M chart is also dated 2026-10-02 and spans the requested area. Per user clarification, ignore the date displayed on the separate top-left chart. The screenshot covers the requested 12:30–13:00 window. Use this as the evidence artifact for the Permission-revocation audit; do not request the same screenshot again.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- [DONE — SOURCE AUDIT ONLY, 2026-10-22] Compare 5M authority predicates with the 1M reconstructed 5M layer and document concrete raw-Permission, structural-damage, and major-failure differences in `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`. No ThinkScript edits made; installed-study parity and behavioral contract remain open.
- [NEXT] Validate fresh candidate, in-trend candidate, continuation, confirmation, event-armed READY, expiration, and re-entry paths against same-bar installed-study evidence and the intended 5M gate.
- [NEXT] Test whether RUNNER / WANING / SWING suppress legitimate 1M opportunities; source audit alone does not establish behavior.
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

## Extended momentum / continuation lane (user requirement, 2026-10-22)

- [RESEARCH REQUIREMENT] Do not treat a large current-day gain or an EXT warning as an automatic permanent rejection. Preserve the existing risk warning, but evaluate a separate **Extended Momentum / Continuation Watch** path for stocks already up roughly 10–15% when the user begins reviewing them.
- [DESIGN ONLY — NOT A BUY SIGNAL] This path should combine fresh price/volume continuation evidence, catalyst/news context with source and timestamp, tradability/liquidity/spread checks, current 5M Permission, and current 1M execution state. News sentiment or a strong narrative alone must never grant eligibility.
- [SEPARATE STATES] Distinguish (1) extended and not currently executable, (2) continuation remains credible and should be watched, and (3) a fresh, valid entry event exists under the unchanged 5M/1M gates. A continuation candidate can stay on a pullback/reclaim watchlist without being marked EXECUTION READY.
- [SAFETY] Never bypass the exact 1M extension formula or turn red EXT into a green/permission state. Any alternative entry policy needs explicit risk limits, gap/halts/spread safeguards, an auditable reason code, and historical/out-of-sample validation before it can affect actionable ranking.
- [OPEN] Determine whether the existing red In-trend candidate bubble is a diagnostic, setup classification, or actual eligibility state by checking the exact installed ThinkScript and chart examples. Do not claim it means technically safe to buy until its formula and semantics are verified.
- [OPEN] Test whether continuation logic should allow a separate re-entry trigger (for example a defined pullback/reclaim or consolidation breakout) rather than weakening the original EXECUTION READY gate.
- [TIME CONTEXT] User's 08:30 reference is Arizona local time. Arizona generally does not observe US daylight saving time, while US Eastern market hours do; therefore the US market open shifts between 07:30 and 08:30 Arizona time depending on the season. Label dashboard/session times with timezone and market-session context.

## Broker integration boundary (user preference, 2026-10-22)

- [DECISION] Schwab website/manual order entry is the preferred baseline. Do not implement Schwab API access, credential handling, or automated order submission merely for convenience.
- [POSSIBLE FUTURE UX] Consider a clearly labeled Open Schwab / open trade-ticket handoff only if a legitimate, supported URL workflow is verified. Do not imply that it can prefill symbol, quantity, account, or order type until confirmed.
- [NOT APPROVED] Do not implement a one-click Buy Maximum Amount control. It is too easy to submit a mistaken oversized order, and broker-side buying power/margin/order constraints cannot be safely inferred from MCC's local data. If a sizing helper is later requested, make it a non-submitting calculator with explicit custom/max budget choice, visible price and share calculation, reserve/cushion, and a final manual review in Schwab.

## Dashboard feedback and scanner feed decision (2026-10-22)

- [PARTIAL — UI FEEDBACK SLICE] The static dashboard now has an explicit system-readiness panel and a local feedback notebook so the user can review layout and behavior before provider integration. Notes persist only in browser local storage and can be exported as JSON; they are not uploaded automatically.
- [NOW] Use dashboard feedback to prioritize UI changes. Keep demo/unknown labels visible; never make mock data look live.
- [BLOCKED — PROVIDER NOT SELECTED] Thinkorswim is not connected as a live scanner/API feed. It is the source of chart-study behavior and validation evidence. No live scanner provider/feed is selected or verified in repository sources.
- [NEXT] Compare eligible feed/provider options against real-time 1M/5M OHLCV, scanner API coverage, timestamps/session/ETH behavior, historical data, entitlements, licensing, rate limits, and outage/reconnect behavior before implementing an adapter.
- [HOLD] Do not bind the dashboard to an assumed Thinkorswim endpoint or a guessed third-party API.

## Scanner integration readiness and EXT prequalification (2026-10-22)

- [NOW] Define a provider-neutral scanner-event contract before wiring any provider: normalized symbol, scanner group, source/event ID when available, first-seen and observed timestamps, data-as-of timestamps, raw-field provenance, schema/version, freshness, current status/reason codes, and evaluator/study version. Discovery is not an entry signal.
- [NOW] Add extension qualification to the scanner design. The user's reported problem is that red-state 1M EXT / extension-danger often coincides with no EXECUTION READY bubble, wasting chart-review time. Keep this as a measured workflow hypothesis until timestamped examples quantify it.
- [NOW] Default actionable scanner queue must deprioritize/filter candidates whose current extension exceeds the exact versioned 1M study envelope. Preserve them in a recoverable WATCH_PULLBACK list; do not permanently discard a symbol merely because it is extended now. Pullback requalification requires fresh data, a new valid event, and unchanged 5M authority / 1M execution gates.
- [NOW] Distinguish extension warning from extension danger. Never substitute a guessed percentage threshold or the provisional OpportunityQualityAssessor threshold for the exact ThinkScript ATR-extension definition. Missing/stale extension inputs => DATA?, never “not extended” or ACTIONABLE.
- [BLOCKER] Before live scanner gating, complete exact-study indicator parity for ATR/extension and related gates; classify the 31 irregular 1M intervals; finish full-session 1M-to-5M reconciliation; repeat RTH/extended-hours and Arizona/Eastern DST tests. See docs/Validation/MCC-OHLCV-20261020.md.
- [PARTIAL — CONTRACT SCAFFOLD COMMITTED 2026-10-22] Added `ScannerDiscoveryEvent` and `IScannerDiscoverySource` in Application. This defines a provider-neutral DTO/source boundary only; append-only storage, freshness/deduplication validation, qualification workflow, tests, and provider adapter remain open. No build/test result has been verified for this commit.
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
- [USER-CONFIRMED DONE LOCALLY — 2026-10-22; REPOSITORY EVIDENCE SYNC PENDING] User reports that all 31 irregular 1M intervals were reviewed and classified locally, full-session 1M-to-5M reconciliation was completed, and RTH/extended-hours plus Arizona/Eastern DST checks were completed; observed outcomes were MATCH or ROUNDING ONLY. Do not ask the user to repeat these tests. Detailed local row-level results/settings/provenance are not in the current repo; see `docs/Validation/MCC-OHLCV-20261020.md`. If auditability is needed, sync the completed artifacts/manifest rather than redoing the work.
- [NEXT] Reconcile the user-confirmed local results into repository provenance: exact files and hashes where available, chart/study version, date/timezone, RTH/ETH settings, DST boundary dates, row/window counts, and result summary. Do not fabricate missing metadata.
- [NEXT] Investigate native Thinkorswim chart-data export as the preferred transport if it exposes the same timestamp/OHLCV fields without strategy-order timing semantics.
- [HOLD] Do not mark the export production-ready, connect it to replay as authoritative market data, or calibrate signal/opportunity logic until the external chart-parity items are complete.


## Morning Report / Morning Routine / Dashboard workflow (2026-10-22)

- [NOW — PRODUCT WORKFLOW] User wants to sit down around 08:30 Arizona time and see a dated Morning Report with an automatically populated/re-ranked watchlist, top 1–4 ranked candidate “Red Zone” multi-pane, and a Boring Overnight decision.
- [DOC ADDED] `docs/User-Manual.md` is the user manual and workflow specification. It distinguishes existing prototype behavior from planned functionality and documents launch, data readiness, watchlist, Red Zone, Boring Overnight, continuation, manual Schwab, and feedback flows.
- [IMPORTANT — NOT IMPLEMENTED] Current Dashboard is static HTML/CSS/JS opened via `frontend/index.html` in a browser, not an .exe. It uses demo fixtures/local CSV and browser-local watchlist/feedback. No live feed/scanner, automatic ranking, Morning Report generator, live Red Zone, live Boring Overnight calculator, C# engine wiring, or broker integration is present.
- [SOURCE AUDIT — REPOSITORY ONLY] `docs/Charts/1M/MainStudy.txt` defines `extensionATR = AbsValue(price - dailyVWAP) / ATR(14)`, warning at 1.50 ATR and danger at 2.00 ATR by default; bullish extension danger is price above Daily VWAP and at/above danger threshold. `inTrendExecutionCandidate = executionCandidate AND NOT freshTrigger`; its event is transition/re-arm gated. Final READY separately requires an active execution window, 5M execution permission, and no bullish extension danger. This establishes repository source semantics, not that the installed Thinkorswim study matches.
- [NOW] Continue audit of the exact 1M/5M installed-study parity and the bubble's real chart examples before any rule changes. Do not reinterpret IN-TREND CANDIDATE as READY or weaken extension thresholds without validation.
- [DONE — SPECIFICATION] Define provider-neutral ingestion, persistence, freshness, recovery, report snapshot, and validation requirements in `docs/Overnight-Collection-Contract.md`.
- [BLOCKED — PROVIDER / ENTITLEMENT] Choose and verify a supported market-data/scanner provider and licensing before implementing its adapter, exact freshness thresholds, and backfill/reconnect behavior.
- [NEXT] Implement append-only event/observation persistence, idempotent ingestion, health monitoring, recovery tests, deterministic report generation, ranking, Red Zone, and Boring Overnight wiring after provider selection.
- [NEXT] Decide desktop delivery only after app integration architecture is known. Browser launch is current reality; an .exe is not built or required yet.
- [USER HISTORY GAP] Repository search and available project handoff do not contain the full prior Morning Routine conversation. The manual documents a proposed workflow from known requirements, but the user should supply or point to the prior routine if exact historical steps/metrics must be restored.


## Overnight scanner / persistent Morning Report requirement (2026-10-22)

User wants to leave the released/deployed MCC program open overnight so scanner discoveries/data are collected before the 08:30 Arizona Morning Report. Do not mark this satisfied by an open browser tab or a static preview.

- [x] Add preview-only Morning Command Brief and responsive ranked 1–4 Red Zone layout to the static UI; include explicit “overnight collection NOT RUNNING” status and JSON report-preview export.
- [ ] Select a supported/licensed scanner and market-data provider; document actual APIs, entitlements, timestamp/session conventions, rate limits, and terms. Provider is currently unselected.
- [x] Write the provider-neutral collection contract in `docs/Overnight-Collection-Contract.md`; it deliberately leaves provider-dependent thresholds unspecified.
- [ ] Implement a long-running collection service that works independently of the browser tab; handle startup, reconnect, rate limits, outages, and graceful shutdown.
- [ ] Persist raw discovery events and timestamped market observations with source/event IDs, source and receive times, session/timezone, schema/version, raw fields, and ingestion disposition.
- [ ] Implement collector health states RUNNING / DEGRADED / STOPPED / UNKNOWN, last-success timestamp, freshness checks, gap/duplicate detection, and no synthetic bars.
- [ ] Implement recovery tests for machine sleep/wake, network/provider outage, authentication expiry, replayed/duplicate events, session boundaries, and process restart.
- [ ] Implement deterministic, versioned Morning Report generation from persisted data; include provenance/freshness per candidate and exportable report snapshots.
- [ ] Connect the live service to scanner discovery, deduplication, automatic re-ranking, continuation watch, and the Red Zone; show only up to four ranked candidates and never treat rank as permission.
- [ ] Wire Boring Overnight calculations and hard gates to persisted, validated inputs; unknown catalyst/data remains DATA? / manual review.
- [ ] Define packaging and startup: browser/local service vs desktop app/installer. Do not claim .exe/background collection exists until built, deployed, and tested.
- [ ] Keep provider secrets out of frontend, localStorage, repository, and logs.
- [x] Verify the latest branch-head .NET workflow after the UI changes; head `bcb1ef5027859cd4448daa08191df83e0e6eb871`, workflow run 91, conclusion `success` (checked 2026-10-23 UTC).
- [ ] Run a browser-based end-to-end smoke test of the Morning Command Brief and JSON export; structural checks are not a browser test.


## Morning Routine recovery, provider comparison, and collection scaffold (2026-10-23)

- [x] Read the full Project source file Morning.txt (992 lines) and consolidate its non-redundant requirements in docs/Morning-Report-and-Routine.md. This is a curated requirements spec, not a claim the linked original chat was fully recovered.
- [x] Document provider candidates and public plan tradeoffs in docs/Provider-Comparison.md. Preliminary evaluation pair: Alpaca Algo Trader Plus vs Massive Stocks Advanced; no provider selected and no account entitlements verified.
- [x] Implement an initial provider-neutral persistence scaffold: IScannerDiscoveryEventStore, JSONL append-only adapter, batch ingestion validation, idempotency key, receipt timestamp, and tests for restart deduplication/corrupt-log failure.
- [x] Add a first append-only raw market-bar observation store with source/receive timestamps, OHLCV validation, content-hash idempotency, and append-only correction links. Implemented in API; tests cover restart deduplication and corrections. Session-aware gap classification and corruption/retention policy remain open.
- [ ] Add API-hosted collector health state and a cancellable long-running streaming/polling worker. A polling-worker scaffold exists but is not wired to a real provider source; Alpaca/Massive historical REST adapters now exist. Live stream binding, recovery/backfill, and operational health remain open.
- [x] Verify .NET build/tests for the API host, provider adapters, raw-bar store, and store tests. Latest verified run 151: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012211560. Browser and live-provider tests remain open.
- [ ] Browser smoke test the Morning Brief and top-right Red Zone layout; user has manually confirmed the current visual look, but latest layout change still needs browser confirmation.
- [ ] Validate source differences against installed Thinkorswim studies using the exact user procedure in docs/Validation/5M-to-1M-User-Validation-Steps.md (to be created); do not ask user to repeat prior OHLCV tests already confirmed done.
- [ ] Resolve provider decision only after entitlement, scanner coverage, session behavior, historical backfill, retention/licensing, and representative-symbol comparison.


### Status correction — follow-up work after the prior checkpoint (2026-10-23)

The earlier workflow success recorded above was for its then-current branch head only. It does **not** establish CI status for the newer persistence scaffold and upper-right Red Zone commits.

- [x] Consolidate the supplied Morning.txt excerpts into docs/Morning-Report-and-Routine.md.
- [x] Add provider evidence and proof-of-fit checklist in docs/Provider-Comparison.md.
- [x] Add the provider-neutral JSONL discovery-event store and batch-ingestion validation scaffold; tests are added but their latest CI result must be checked.
- [x] Place Morning Brief left and Red Zone upper-right in the responsive wide-screen command workspace. User browser confirmation is still needed for this specific layout change.
- [x] Create docs/Validation/5M-to-1M-User-Validation-Steps.md with the exact installed-study evidence needed from the user.
- [x] Update the User Manual, Scanner Workbench, and New Chat Handoff.
- [ ] Verify .NET build/test workflow against the latest branch head after all changes.
- [ ] Browser-test the new top-right layout and export interaction.
- [ ] After provider selection, extend persistence to market bars and implement cancellable collector, health reporting, reconnection/backfill, and restart/sleep recovery.


### Collector lifecycle scaffold extension (2026-10-23)

- [x] Add ScannerDiscoveryPollingWorker with cancellation, retry-on-next-poll behavior, and RUNNING / DEGRADED / STOPPED / UNKNOWN health snapshots.
- [x] Add an offline test proving a successful empty poll is recorded as zero observations and cancellation stops the worker.
- [ ] Add explicit degraded-source/recovery test coverage and sleep/wake/backfill reconciliation after provider semantics are known.
- [ ] Wire the worker into a real host and implement a provider adapter; no configured source exists, so the worker is not currently running.


## Provider comparison UI / UPC CSV follow-up — 2026-10-23

- [DONE — STATIC UI ONLY] Added parallel Alpaca and Massive Stocks comparison cards to `frontend/index.html`, styled in `frontend/styles.css`, with a local comparison checklist action in `frontend/app.js`. User can set symbol, aggregation, and date; it marks both feeds NOT CONNECTED and shows required comparison fields. The button does not contact vendors and is not a live integration.
- [DONE — CSV REVIEW] Reviewed `data/StrategyReports_UPC_102226.csv`. It records four strategy/order events between 09:37 and 09:47 Arizona time and embedded isolated 1M OHLCV payloads. It is not a full OHLCV time series, so it cannot determine whether the reported earlier candles were absent.
- [RESOLVED] User confirms UPC screenshots/event date 2026-10-22 and Thinkorswim chart timezone Arizona.
- [OPEN] Obtain continuous 1M/5M UPC OHLCV or verify exact bar timestamps in TOS for 09:16–09:36. Do not fill missing bars.
- [OPEN] Implement real backend adapters for both vendors only after credentials and entitlement/licensing decisions; keep secrets out of frontend JS. Compare source timestamps, bars, quotes, missing intervals, reconnect and backfill.


## API host, REST adapters, and Morning Brief integration (2026-10-23)

- [DONE — IMPLEMENTED, NOT YET BUILD-VERIFIED] Added `src/MomentumCommandCenterV2.API` as an ASP.NET Core .NET 8 project and included it in `MomentumCommandCenterV2.sln`. It serves the existing `frontend/` workbench when run from the repository layout and exposes health/provider-status endpoints.
- [DONE — REST HISTORY ADAPTERS, NOT LIVE-VALIDATED] Added server-side Alpaca historical 1M/5M bars adapter and Massive historical aggregate adapter. Credentials are read from configuration/environment variables, never from frontend JavaScript. No API credentials were provided; no live provider request was performed.
- [DONE — FIRST COMPARISON ENDPOINT] Added `GET /api/market-data/compare` to request the same symbol/timeframe/window from both providers and report bar counts, first/latest bars, duplicate timestamps, and observed timestamp gaps. Gap count does not imply zero trading or a provider defect. Websocket, scanner/movers, quotes, pagination, durable bar storage, reconnect/backfill, and market-session-aware gap classification remain open.
- [PARTIAL — MORNING BRIEF] Added `GET /api/morning-brief` for SPY/QQQ/IWM 5M observation snapshots from configured providers, and connected the dashboard to provider status, the comparison endpoint, and Morning Brief. Sectors, breadth, movers, catalysts/news, validated market regime, Red Zone scoring, historical expectations, and Boring Overnight remain explicitly DATA? / NOT CALCULATED.
- [OPEN — REQUIRED BEFORE TRADING RELIANCE] Build/test the latest branch head, run live API requests with user's licensed credentials, verify provider entitlements and extended-hours coverage, compare both feeds against TOS, and test session/gap semantics. These commits are not a claim of build success, CI success, live-data parity, or production readiness.
- [OPEN] Add durable raw OHLCV persistence with source/receive timestamps, late/corrected bar handling, and restart recovery.
- [OPEN] Add websocket streaming, health state, reconnection and historical backfill before considering real-time operation.
- [OPEN] Continuous UPC OHLCV remains needed only to resolve whether the specific 09:16–09:36 chart intervals were truly missing. It is not a blocker to implementing provider adapters.


### CI verification — API adapters and dashboard wiring (2026-10-23)

- [VERIFIED] GitHub Actions run 147 at commit `ae8180866def759612fa7c1a515dc93586143056` completed successfully: solution restore, Release build, and test suite all passed. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011426478
- This validates compilation and existing automated tests only. It does not validate live provider credentials, API entitlements, response behavior against real accounts, browser rendering, data parity, or readiness for live trading.


### Raw market-bar persistence slice — 2026-10-23 [BUILD/TEST VERIFIED]

- [IMPLEMENTED — CI PENDING FOR LATEST COMMIT] Added `IMarketBarObservationStore` and `JsonlMarketBarObservationStore` to the API host. Provider-comparison requests append valid raw OHLCV observations to JSONL outside the repository by default (LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl); override with `MCC_MARKET_BAR_STORE_PATH`.
- Identical observations are idempotent by provider/symbol/source timestamp/content hash. Changed values append a new observation with a `SupersedesObservationId`; prior records are not overwritten. Records preserve source timestamp, receive timestamp, provider/feed and raw OHLCV.
- [VERIFIED — TESTS PASS, 2026-10-23] Added `tests/MomentumCommandCenterV2.Tests/JsonlMarketBarObservationStoreTests.cs` for idempotency across store restart and append-only correction history. GitHub Actions run 148 at commit `c702afa21fac9dfa5a31b18463cc4429aa4adc71` passed restore, Release build, and tests: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011865804.
- [OPEN] Add corruption recovery policy, disk/retention monitoring, session-aware gap semantics, and ensure write failures surface as a clear API health condition.


### Morning Brief and raw-bar persistence verification status

- Morning Brief now records observed SPY/QQQ/IWM 5M bars into the same append-only store and displays the observed first-to-last window change when data is available. These are observations, not a validated market-regime classification.
- The provider comparison UI shows newly persisted observation counts. REST history only; no WebSocket/live collector.


- [LATEST HEAD VERIFIED] GitHub Actions run 151 at `ae6ee7acc30e85a6268b5ea8fc40d86429d3461b` completed successfully for restore, Release build, and tests. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012059701. No live provider, browser, or entitlement validation is implied.


## Canonical dependency/status correction — 2026-10-23

Use `docs/Next-Steps-and-User-Inputs.md` as the single checklist for user actions versus work that can proceed without accounts.

- **Accounts:** no paid subscriptions are needed for the next development milestone. Free accounts are optional for live API smoke tests. One provider is enough to begin; both are useful for a same-window comparison. Do not select or pay for a production plan until proof-of-fit, account entitlements, license rights, premarket/small-cap coverage and TOS parity have been checked.
- **API:** the ASP.NET Core host, Alpaca/Massive historical REST adapters, comparison endpoint, partial Morning Brief and append-only JSONL raw-bar store are implemented. Latest CI run 151 passed restore, Release build and tests. No live provider calls, browser end-to-end test or license verification has been performed.
- **Offline work that remains actionable now:** adapter pagination/error/retry tests; hosted collector lifecycle and health; stream abstraction; recovery/backfill design and tests; store corruption/retention/error handling; fixture-driven Morning Brief/scanner/Boring Overnight calculations; session-aware gap semantics; ThinkScript source parity tests.
- **User evidence still required:** XRPN 5M screenshot listed in `docs/Validation/5M-to-1M-User-Validation-Steps.md`; UPC continuous bars or exact timestamp inspection only if the historical 09:16–09:36 gap question still needs resolution; review the API-hosted browser layout. These are not prerequisites for offline infrastructure work.
- **Morning Brief scope:** currently benchmark observation/freshness only. Broad movers, sector/breadth, sourced news/catalysts, validated regime, Red Zone qualification and live Boring Overnight remain unimplemented and must not be displayed as known.

## Export and opportunity-quality review — 2026-10-26

**Audit:** `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`

- [DONE — REPOSITORY DATA AUDIT] Found an aligned XRPN 1M/5M OHLCV pair for 2026-10-19: `data/StrategyReports_XRPN_102026_933.csv` (355 1M payload rows) and `data/StrategyReports_XRPN_102026_5m_offset_1027.csv` (81 5M payload rows).
- [DONE — DATA PROVENANCE] Corrected the October 22 1M report filename to `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`; it is not the October 2 1M pair. Do not pair it with the actual 2026-10-02 5M file.
- [PARTIAL — INTERNAL OHLCV CHECK] In the October 19 post-09:00 Arizona overlap, 27 complete 5M windows have all five 1M payload rows and their aggregated OHLCV matches the corresponding 5M payload. Thirty minute timestamps from 09:00–12:59 are absent from the 1M file; missing rows remain gaps, not synthesized bars.
- [BLOCKED — DIAGNOSTIC STATES] OHLCV payloads do not contain internal ThinkScript Permission, candidate, extension, READY, re-arm, or expiration states. READY-frequency root cause cannot be attributed from OHLCV alone. Existing October 2 screenshot remains available; do not ask user to re-upload it.
- [BLOCKED — OVERNIGHT WINDOW] Audited bars do not cover the full 07:00–20:00 Eastern GTC_EXT eligible window. `eligible-window-high-excursions.csv` is placeholder values 1–20, not historical observations. No target-touch rate or alternative limit recommendation may be derived from it.
- [AUDIT — OPPORTUNITY QUALITY] `OpportunityQualityAssessor` and unit tests exist, but the assessor is not wired to replay, V2SignalEngine, CLI, API, or UI. Required metrics are not derived from raw OHLCV. Keep provisional thresholds diagnostic-only; presentation must distinguish opportunity quality from the actual ThinkScript EXECUTION READY event.
- [NEXT — NO USER INPUT REQUIRED] Keep the October 19 pair as a coverage fixture; add regression checks for filename/header/payload date consistency where the ingestion workflow has file-level provenance; keep incomplete intervals explicit.
- [NEXT — USER/THINKORSWIM EVIDENCE] To diagnose a particular READY gate, obtain diagnostic states for the same symbol/date/bars as the OHLCV. For Boring Overnight, use a full eligible-window export with source-backed regular close; otherwise mark DATA_GAP.
- [NO PROMOTION] These exports are not proof of production timestamp semantics, indicator parity, fill probability, or strategy profitability.

## Opportunity-quality label safety and branch validation — 2026-10-26

- [DONE — SOURCE CHANGE, CI NOT YET CONFIRMED] Changed display labels from `READY — HIGH/MODERATE/BORING/EXTENDED` to `QUALITY — HIGH/MODERATE/LOW` and `TIMING — EXTENDED`. The labels now avoid conflating this diagnostic classification with the existing Thinkorswim `EXECUTION READY` event. Machine-readable enum values and eligibility precedence are unchanged.
- [DONE — TEST SOURCE UPDATED, CI NOT YET CONFIRMED] Updated the high-quality label assertion and added four negative-metric cases covering negative RVOL, ATR extension, recent range, and resistance distance; invalid metrics should fail closed to `DATA_UNKNOWN`.
- [OPEN — CI VERIFICATION] GitHub combined commit status returned no status records for the latest branch commit at audit time. Do not call the current branch build/test green until a workflow run tied to the latest commit is confirmed.
- [INTEGRATION CAUTION] Repository compare reports `feature/opportunity-quality-classification` as diverged from `master` (268 commits ahead and 152 behind in the compare result). Do not merge this branch wholesale into master without a deliberate file/history reconciliation. The changes in this work session remain on the feature branch.

## October upload uncertainty assessment — 2026-10-26 (supersedes prior preliminary note)

- [DONE — DATA PROVENANCE] Inspected the newly uploaded `StrategyReports_XRPN_10022026_1M_EntireDay_2.csv` by report header and embedded payload. It is actually **HPK 1M**, October 2, with 214 rows from 06:30–12:59 Arizona. Renamed it to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv` and removed the misleading XRPN-named path.
- [DATA QUALITY] Those 214 rows match all corresponding October 2 rows in the existing HPK multi-day file exactly. The upload is a duplicate/subset, not new XRPN data. The October 2 XRPN 5M file still has no verified same-symbol XRPN 1M partner.
- [PARTIAL — CHART PARITY] Existing October 19 parity worksheets have chart-entered values for 20 bars per timeframe: 1M has 10 MATCH and 10 ROUNDING ONLY; 5M has 9 MATCH and 11 ROUNDING ONLY. 48 1M and 13 5M rows remain unfilled. This supports sampled OHLCV transport mapping but not full chart/session parity.
- [PARTIAL — TIMESTAMP SEMANTICS] All 40 manually entered chart labels match the local clock digits represented by the payload timestamps. The worksheet records `ChartTimestampMeaning=MATCH`, not BAR_OPEN/BAR_CLOSE/decision availability; the timestamp convention is not fully documented.
- [BLOCKED — GAP CLASSIFICATION] October 2 HPK 1M has 214 of 390 expected minute timestamps, leaving 176 absent. October 19 XRPN has 30 absent timestamps in the 09:00–12:59 interval. Do not infer that missing rows are no-trade periods or chart gaps.
- [BLOCKED — READY ROOT CAUSE] The parity worksheets contain OHLCV chart values, not per-bar ThinkScript READY/Permission/candidate/extension/re-arm states. The October 2 XRPN diagnostic question still needs XRPN 1M and same-bar diagnostic state.
- [BLOCKED — BORING OVERNIGHT] No complete 07:00–20:00 Eastern GTC_EXT window or actual-fill dataset has been found. `eligible-window-high-excursions.csv` now contains only the header; its fabricated values were removed. It has zero verified observations.
- [CI] GitHub Actions run `40379797275` for commit `e7e59d4` completed successfully (restore, build, test). Documentation commits made after that run trigger newer runs; do not treat the current branch head as green until its own run completes.
- [INTEGRATION CAUTION] Keep branch-divergence caution in force; do not merge this feature branch wholesale into master.
- Detailed uncertainty table: `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`.

## Launch-week release gates — 2026-10-26

Use `docs/Release-Readiness-2026-10-26.md` as the concise launch-gate summary. This is a prioritization aid, not a declaration of release approval.

- [DONE — DOCS] Added explicit release-readiness assessment separating local prototype use from live-trading reliance.
- [DONE — CONTRACT CLARITY] Clarified in `docs/MCC-OHLCV-Export-Workflow.md` that the normalized CSV contract expects completed-bar availability timestamps, but raw `GetTime()[1]` semantics remain unverified until chart parity proves the mapping.
- [NEXT — NO USER INPUT] Add automated report provenance checks for filename/header/payload symbol and date consistency; add importer regression coverage for duplicate, missing, malformed, and partial-window rows.
- [NEXT — NO USER INPUT] Reconcile duplicated and legacy To-Do material; preserve one canonical Master To-Do and link domain-specific specifications rather than creating competing checklists.
- [BLOCKED — USER/THINKORSWIM] Obtain actual XRPN 1M for October 2 if that exact event remains the target; the new file was HPK and duplicates existing HPK rows.
- [BLOCKED — USER/THINKORSWIM] Obtain same-bar 1M/5M gate-state diagnostics for READY-frequency attribution.
- [BLOCKED — DATA SOURCE] Obtain full GTC_EXT eligible-window bars plus source-backed regular close for target reach research; actual fills are a separate data source.
- [CI STATUS] Latest confirmed feature-branch head `c1895641` passed workflow `40380631692`; the release-readiness note was then updated, so recheck the workflow for the resulting newest head before promotion.
