# Momentum Command Center V3 — Consolidated Master To-Do
**Updated:** 2026-10-25 (Arizona time; complete Schwab exclusions supplied and captured; full .NET test suite passed; project-control docs synchronized to both Git branches)  
**Purpose:** Canonical master list. Consolidates the 2026-10-06 master TODO, the 2026-10-13 Boring Overnight Decision Column TODO, and subsequent documented project status. The dedicated Boring Overnight TODO remains a detailed companion; this master list must be updated whenever that companion changes.

## Status key
- [NOW] Current priority
- [NEXT] Next in sequence
- [HOLD] Deliberately deferred
- [VALIDATED] Substantially validated, not a claim of profitability
- [DONE] Completed and verified
- [UNVERIFIED] Must be checked against repository/current installed artifact

## 1. 5M authority / Permission — [NOW]
- [NOW / EVIDENCE RECEIVED 2026-10-23] Screenshot uploaded: `Screenshot 2026-10-23 154303.png`. User confirms the 1M and 5M chart dates are set to 10/02/2026; ignore the top-left chart's date. The screenshot includes the normal 5M study and 5M Authority Diagnostic V2 with a readable 5M time axis. Known transition remains: Permission 0 premarket; Permission 1 plus BUY at 8:25; Permission 0 by 12:45 Arizona time. Do not ask user to rediscover these timestamps.
- [EVIDENCE REVIEWED 2026-10-23] Re-reviewed the uploaded screenshot and diagnostic source. The 5M time axis includes 12:45; the visible red-bar diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, `State=0` around the transition. `PBLOCK=2` maps to persistent major failure. Do not ask the user to re-upload/re-enter this evidence.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.
- [NOW / ASSISTANT ACTION] Finish the code-level comparison of normal 5M V4.1.7 and Authority Diagnostic V2 around the 12:45 bar; determine whether the Permission latch behavior is intended under persistent major failure. User input is not currently required. Keep logic unchanged unless a concrete discrepancy is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- Verify exactly which 5M fields/states the 1M consumes.
- Verify fresh candidate, in-trend candidate, continuation, confirmation, event-armed READY, expiration, and re-entry paths obey the intended 5M gate.
- Verify RUNNER / WANING / SWING do not suppress legitimate 1M opportunities unless intended.
- Verify extension danger blocks chasing without falsely declaring the 5M thesis dead.
- Preserve architecture: 5M never references 1M; 1M may reference 5M.

## 3. 1M execution pipeline — [HOLD broad edits; audit targeted behavior]
- [SOURCE FOUND] Library contains `MomentumCommandCenterV3_2_1M_V14_5.txt`, labeled V14.5 — Event-armed execution opportunity, fresh-trigger memory + candidate re-arm + finite execution window. This is the best current 1M source to audit; screenshots alone cannot prove it is byte-for-byte identical to the user-installed study. Compare V14.5’s consumed 5M fields and transitions with the screenshot and prior candidate versions before proposing edits.
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

- [NEW 2026-10-23] Draft data contract created: `Boring_Overnight_Column_Data_Contract_v1.md`. It formalizes status precedence, hard gates, overnight-return/session definitions, required provenance/freshness, reason codes, ThinkScript/C# responsibilities, and a validation matrix. This is a draft: do not code undefined score point maps or unresolved session definitions as if approved.
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
- [USER PROVIDED 2026-10-25] Canonical current Schwab exclusion list is maintained in `Schwab_Exclusions_SourceOfTruth.md` (58 symbols). Treat it as append-oriented; do not remove entries without explicit review. Load it once through a shared exclusion service and apply it consistently to Boring Overnight, scanners, Morning Report, and MarketWatch.

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

## 7. C# V3 implementation — [NOW / documentation and import layer]
Repository supplied by user: https://github.com/themailcomeshere/MomentumCommandCenterV2.git
- [NOW] Verify actual repository branch, latest commit, solution/project tree, framework, namespaces, and build/test commands. The web retrieval attempted 2026-10-15 did not produce verifiable repository content; do not invent repository facts. Existing README is an architecture working spec, not a verified tree.
- [SOURCE REVIEW 2026-10-23] `ITradeImporter`, `TradeImportResult`, `SchwabTradeImporter.ImportFile()`, CSV parsing, fill-price parsing, Eastern timestamp conversion, FIFO matching, open-position detection, and unmatched-sell preservation already exist in the feature branch. Do not implement these a second time.
- [NEXT] Expand importer validation/tests for ImportFile behavior, duplicate/idempotency policy, malformed row handling, timestamp/DST edge cases, header/currency/number parsing, and actionable partial-failure diagnostics. Current importer throws on malformed input and returns an empty `Errors` collection on successful imports; row-level partial recovery/idempotency are not implemented or verified.
- [USER CONFIRMED 2026-10-25] `dotnet test MomentumCommandCenterV2.sln` succeeded on the feature branch: 53 total, 53 passed, 0 failed, 0 skipped; build succeeded. This is the user's local test report, not an independently executed CI run.
- Then build persistence layer; normalized trade/fill models and repository abstractions; analysis/market-data layers; state snapshots; decision logging; later web/MVC UI.
- Keep boundaries clean: raw import -> normalized application/domain models -> persistence -> bar/indicator calculations -> deterministic 5M authority -> 1M execution -> separate overnight decision -> logged outcome/backtest.
- Use pure, testable calculations/state transitions; make data source, time zone, session boundary, missing-data and freshness behavior explicit.
- C# should own/consume authoritative rolling ON stats, median/mean and 5D/20D metrics, consistency/volatility, scores, catalyst metadata, freshness, reason-coded decisions and outcome logs.
- Record candidate snapshots, status/reasons, intended and actual entry/exit, and outcomes for audit and threshold evaluation.
- Preserve `///` XML documentation with flowerboxing. Deliver complete coherent code and next-step instructions together.
- No claims that live brokerage integration, external event feeds, backtesting, automated orders, or production UI exist until verified in source/tests. No automated orders until separately specified and tested.
- Ensure any intentional divergence between C# and TOS behavior is documented and covered by comparison tests.


## 7B. MCC MarketWatch — persistent watchlist + evidence-based thesis tracking [NEW TODO]

### Objective
Build an MCC-native equivalent of Thinkorswim MarketWatch: a semi-permanent, user-maintained stock watchlist that supports both manual monitoring and ongoing automated evidence collection. The intent is not merely to show quotes; MCC should preserve *why* a symbol was added, what claim/catalyst motivated attention, how long it has been watched, what evidence emerges, and whether the original thesis is supported, contradicted, stale, or still unverified.

### Core user workflow
- Add a symbol at any time and keep it on the watchlist until the user removes/archives it.
- Record added-at timestamp, source of discovery, free-text rationale, original claim/rumor, source URLs or references, sector/theme/wave, and optional price/target/time horizon.
- Show elapsed watch age (e.g., “added 6 days ago”), last meaningful update, current price/return from add, and data freshness.
- Let the user append notes and attach later evidence without overwriting the original thesis or history.
- Support statuses such as `NEW`, `MONITORING`, `CONFIRMED`, `CONTRADICTED`, `UNVERIFIED`, `STALE`, `CATALYST_RISK`, and `ARCHIVED`. Status transitions must retain timestamps and reasons.

### Automated monitoring and evidence
- Ingest current/near-real-time and historical price/volume data through an explicit provider abstraction (Alpaca is the first provider candidate; actual entitlement, feed, and update frequency must be verified).
- Monitor configurable price/volume/volatility/technical conditions and surface meaningful changes, not just repetitive quote updates.
- Collect news/catalyst evidence through a separately identified news source/provider; quote APIs alone do not establish that a business deal is real.
- Track each evidence item with source, publication time, observed time, URL/reference, normalized claim, affected symbol, and confidence/provenance. Preserve conflicting evidence rather than silently choosing a side.
- Separate fact from interpretation. For example: “company press release confirms signed agreement” is evidence; “this implies +40% upside” is a target/analyst claim that needs its own source, author, timestamp, and horizon.
- Evaluate thesis claims with explicit outcomes: supported, contradicted, not yet verified, expired/stale, or conflicting evidence. Do not label rumors as facts or promise predictive accuracy.
- If no trustworthy source/feed is connected, show `DATA NOT FOUND IN SOURCES` / `UNVERIFIED` rather than inventing news or a price target.
- Show “added X days ago,” relevant developments since add, why the state changed, and links back to source material.

### UI and data model
- Watchlist overview with sortable/filterable symbol, age, thesis, state, last price, move since add, catalyst/news status, next review, and data freshness.
- Symbol detail timeline combining user notes, price/volume observations, news/evidence items, thesis edits, state changes, and alerts.
- Keep original thesis immutable as a versioned record; subsequent revisions are appended, not destructive edits.
- Suggested entities: `WatchlistItem`, `WatchlistThesisVersion`, `WatchlistNote`, `WatchlistEvidence`, `WatchlistObservation`, `WatchlistAlert`, and `WatchlistStateTransition`.
- Persist source metadata and timestamps (UTC internally; render in user-selected/local time). Handle symbol changes, delistings, stale feeds, duplicates, and provider failures explicitly.

### Alerts and user control
- Configurable event/condition alerts, with severity and deduplication/cooldown to prevent alert spam.
- Explain each alert: what changed, which source/observation supports it, and why it matters to the stored thesis.
- User can mute, snooze, mark reviewed, edit monitoring rules, archive, or remove a symbol. Preserve audit history when archived/removed according to retention rules.
- Never submit trades or infer authorization to trade from a watchlist alert.

### Implementation sequence
1. Specify use cases, watchlist fields, status machine, evidence provenance, freshness rules, and acceptance tests.
2. Implement persistent domain/application models and repository interfaces; unit-test state transitions, duplicate symbols, thesis versioning, stale data, and conflicting evidence.
3. Implement manual add/edit/note/archive workflow and an initial watchlist UI/API.
4. Integrate Alpaca market data behind a provider abstraction; verify account/feed entitlements and record source/time/freshness on every observation.
5. Add a news/catalyst provider only after source selection, licensing/access constraints, and reliability expectations are established. Never use an LLM-generated summary as the sole evidence of a deal or target.
6. Add deterministic rules and notifications, then validate against historical known winners, false rumors, failed catalysts, and stale/no-news cases.
7. Evaluate optional LLM summarization only as a cited interpretation layer over retrieved evidence, with explicit uncertainty and links; it must not invent source facts or automatically create a “confirmed” state.

### Acceptance criteria
- User can add a symbol with thesis/source and see it persist after restart.
- Watch age and price performance are computed from recorded timestamps and provider data, with missing/stale data clearly marked.
- Every automated catalyst assertion links to a retrieved source and shows publication/observation time.
- Unsupported rumors remain `UNVERIFIED`; contradictory evidence is visible and can move a thesis to `CONTRADICTED` or `CONFLICTING` without erasing history.
- Price targets are attributed to an identified source and horizon; MCC never fabricates a target.
- All state transitions and alerts are unit-tested; no watchlist feature submits orders.

### Dependencies / open decisions
- Select a news/catalyst source with acceptable coverage, cost, licensing, and API access. Alpaca market data alone is not enough to verify corporate deals or analyst targets.
- Decide whether watchlist storage/UI is local-first desktop/web UI or part of the eventual MCC web application, consistent with verified repository architecture.
- Decide default monitoring hours, alert channels, retention, and whether target prices are user-entered, source-reported, or both.

## 7A. Alpaca read-only provider smoke test — [IMPLEMENTED ON FEATURE BRANCH; USER CONFIRMED BUILD/RUN SUCCESS]
- [DONE IN BRANCH `feature/alpaca-readonly-provider-smoke-test`] Added `AlpacaOptions` with local environment-variable credentials and HTTPS endpoint validation.
- [DONE IN BRANCH] Added read-only `AlpacaApiClient`: `GET /v2/account` authentication check and latest stock-bar retrieval from Alpaca Market Data. No order-placement method exists.
- [DONE IN BRANCH] Added xUnit tests for missing credentials/no HTTP request, successful account response, unauthorized status handling, latest-bar parsing/symbol normalization, and missing-bar response.
- [DONE IN BRANCH] Added CLI command: `dotnet run --project src/MomentumCommandCenterV2.Cli -- alpaca-check AAPL`.
- [DONE IN BRANCH] Documented local setup and security guidance in README. Keys/secrets are environment variables, not `appsettings.json` or source control. Optional non-secret endpoint/feed overrides are supported.
- [USER CONFIRMED 2026-10-25] Full local test suite passed: 53/53 tests, no failures/skips, build succeeded. GitHub CI status was not reported; this is a user-confirmed local result.
- [USER CONFIRMED 2026-10-23] Real Alpaca smoke test succeeded: account authentication `OK`, account status `ACTIVE`, and latest AAPL IEX bar returned (`2026-10-23T19:59:00Z`, OHLC 347.69/347.87/347.54/347.69, volume 17,487). CLI confirmed no order was submitted. This verifies credential configuration, account endpoint, and one latest-bar request; it does not validate historical bars, streaming, entitlement breadth, or unit-test results.
- [DONE / USER CONFIRMED] Credentials are configured using .NET User Secrets (the `AddUserSecrets(typeof(Program).Assembly, optional: true)` overload fix was committed after a compile error). The user ran `alpaca-check AAPL` successfully: ACTIVE account, authentication OK, and one latest IEX bar returned. Secrets were not shared in chat. Keep keys out of source control; environment variables remain the deployment option.
- [HOLD] Do not add order placement, automated trading, or any order submission as part of provider verification.

## 8. Repository README / docs — [NEXT]
- [SOURCE CONTROL RULE] Personal Library documents are not branch-specific and are not automatically committed to GitHub. The four project-control documents must be copied into the repository and committed to both `master` and `feature/alpaca-readonly-provider-smoke-test` if they are to survive branch switches as source-controlled notes. Confirm each branch commit and compare file contents; do not infer synchronization from Library updates.
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
1. Finish the XRPN 10/2 12:45 Permission revocation audit by comparing normal 5M V4.1.7 with Authority Diagnostic V2 against the already uploaded screenshot and bubble values; do not ask the user to repeat them.
2. Validate Permission semantics without speculative code edits.
3. Audit 5M -> 1M handoff.
4. Trace 1M candidate/confirmation/READY/expiry/extension blocks and reconcile installed version.
5. Validate runner management and explicit exit behavior.
6. Specify Boring Overnight column contract; implement reliable ThinkScript subset and C# authoritative calculations; validate historically.
7. Implement `ITradeImporter` / `SchwabTradeImporter.ImportFile()` + `TradeImportResult`, tests, then persistence/analysis.
8. Verify repository source and refresh README from actual code.
9. Resume controlled scanner/trade validation and Daily Gainer Review.
10. Run `dotnet test MomentumCommandCenterV2.sln` locally and record the actual result. User has already confirmed the real read-only Alpaca smoke test; do not ask them to repeat it unless the code changes.
11. Specify and implement MCC MarketWatch persistent watchlist + source-backed thesis/evidence monitoring, starting with the data contract and tests before UI polish.


## 7C. Morning Report + continuously updated conditions — [SPECIFICATION TODO]
- [NEW 2026-10-23] Draft contract created: `MCC_Morning_Report_Data_Contract_v1.md`. It defines the three panels, scenario-prediction discipline, current-condition snapshots, provenance/freshness, acceptance tests, and open source/delivery decisions.
- [NEW from `Morning.txt`, reviewed 2026-10-23] Create a concise ~08:30 Arizona-time briefing with three panels: Morning Landscape (market/industry and overnight/premarket movers), Daily Predictions (scenario-level expectations, not individual-stock price predictions), and Current Conditions that refresh during the session.
- Include market regime/context (SPY, QQQ, IWM, breadth/VIX when data is available), strongest/weakest industries, premarket move versus move during the user's trading window, catalyst evidence, extension/chase risk, and opportunity types most/least aligned with the current environment.
- Treat predictions as measurable hypotheses with as-of time, evidence, confidence/sample count when statistically supported, and later outcome comparison. Never show invented probabilities or example values as live facts.
- Track divergence between morning expectations and later conditions; timestamp snapshots so the system can learn from expectation-versus-observation.
- Boring Overnight should consume catalyst, market/industry regime, historical overnight behavior, liquidity, extension, continuation/deterioration risk, and expected overnight range only when those data are actually available and freshness is visible.
- [RECOMMENDATION / NO USER BLOCKER FOR SPEC] Start with a timestamped Markdown/console report generated by the CLI and saved as a report artifact; this is testable and avoids prematurely choosing a web UI. Use Alpaca bars for the initial price/volume adapter, but label IEX coverage and feed timestamps. Alpaca documents a separate News API (historical news is described as Benzinga-provided); verify account access, coverage, licensing, and rate limits before relying on it. Industry classification/returns require a maintained ticker-to-industry mapping plus a defined universe and breadth calculation; a single ticker’s industry label is not the same as ranking industries. Use a 08:30 Arizona morning snapshot, refresh market/industry metrics every 5 minutes during the configured trading window, and process news/catalyst updates on arrival or at a 1–2 minute polling cadence if streaming is unavailable. Treat 08:30–12:00 Arizona as a proposed default window only; make it configurable and confirm it before using it to suppress opportunities. Missing/unverified feed => `DATA?`, never a confident live claim.

## 7D. Execution Intelligence / missed-fill learning — [FUTURE; DATA CONTRACT TODO]
- Capture signal time/price, proposed limit, order intent, actual fill/no-fill, price path after signal, subsequent maximum favorable/adverse excursion, and exit outcome.
- Learn fill probability versus limit discount and the trade-off between price improvement and missed opportunity; separately analyze entry and exit behavior.
- Do not present example fill probabilities as real until a sufficiently large, clean, timestamped dataset exists and the estimate is validated/calibrated.