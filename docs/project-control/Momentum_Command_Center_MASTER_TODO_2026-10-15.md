# Momentum Command Center V3 — Consolidated Master To-Do
**Updated:** 2026-10-26 (Arizona time; PR #5 fixed-target evaluator merged and CI passed; operator corrections to target research, READY-frequency investigation, and news-assisted exit review incorporated)  
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
- [DONE / SOURCE AUDIT 2026-10-25] Normal V4.1.7 and Authority Diagnostic V2 Permission definitions match exactly (132-line normalized comparison). `PBLOCK=2` means persistent major failure and the normal latch explicitly clears Permission to 0. No normal-5M-versus-diagnostic source discrepancy found. The next audit is the V14.5 1M reconstructed-permission proxy, whose raw permission, structural-damage, and major-failure definitions differ from authoritative 5M logic; do not change it until the XRPN same-bar effect is validated.

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
- [USER PROVIDED 2026-10-25] Canonical current Schwab exclusion list is maintained in `Schwab_Exclusions_SourceOfTruth.md` (55 symbols). Treat it as append-oriented; do not remove entries without explicit review. Load it once through a shared exclusion service and apply it consistently to Boring Overnight, scanners, Morning Report, and MarketWatch.

### Implementation sequence
- [DONE / DRAFT CONTRACT] Data contract now includes provisional configurable score maps (`BORING-0.1-PROVISIONAL`), close-to-open ON interval, volatility thresholds, and status precedence. These are engineering defaults to validate, not approved/proven thresholds.
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
- [DONE / REPOSITORY VERIFIED 2026-10-25] Repository is `themailcomeshere/MomentumCommandCenterV2`, default branch `master`, target framework .NET 8, with solution `MomentumCommandCenterV2.sln`, CLI project `src/MomentumCommandCenterV2.Cli`, Infrastructure project `src/MomentumCommandCenterV2.Infrastructure`, and xUnit project `tests/MomentumCommandCenterV2.Tests`. PR #1 merged to `master` at `8e9a441fb77a6ea8df216bc4950a19dad2216355`; post-merge GitHub Actions build and test job passed (run 40235261498). Keep README architecture claims separate from code actually implemented.
- [SOURCE REVIEW 2026-10-23] `ITradeImporter`, `TradeImportResult`, `SchwabTradeImporter.ImportFile()`, CSV parsing, fill-price parsing, Eastern timestamp conversion, FIFO matching, open-position detection, and unmatched-sell preservation already exist in the feature branch. Do not implement these a second time.
- [MERGED 2026-10-25] Canonical machine-readable Schwab exclusions at `src/MomentumCommandCenterV2.Infrastructure/Resources/schwab-exclusions.txt`, shared `SchwabExclusionList` provider, and xUnit tests for all 55 symbols, casing/whitespace, near-matches, and null/blank input are on `master`. PR #1 post-merge CI passed. Provider integration into Boring Overnight/scanners/watchlist is still outstanding.
- [NEXT] Expand importer validation/tests for ImportFile behavior, duplicate/idempotency policy, malformed row handling, timestamp/DST edge cases, header/currency/number parsing, and actionable partial-failure diagnostics. Current importer throws on malformed input and returns an empty `Errors` collection on successful imports; row-level partial recovery/idempotency are not implemented or verified.
- [BASELINE USER TEST 2026-10-25] Earlier local baseline was 53/53 before the exclusion provider/tests. The merged PR's post-merge GitHub Actions build and test job passed; follow-up PR #2 adds a nullable-symbol regression test and CLI input diagnostics, with its CI run pending at the time of this edit.
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
- [USER CONFIRMED 2026-10-25] Baseline local test suite passed 53/53 before the new Schwab exclusion provider/tests were added. Re-run the suite to validate the new code; GitHub CI status was not reported.
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

## 12. Launch-readiness checkpoint — 2026-10-25

- [DONE / VERIFIED 2026-10-25] PR #1 was merged to `master` as merge commit `8e9a441fb77a6ea8df216bc4950a19dad2216355`. The post-merge GitHub Actions `.NET tests` run completed successfully on that merge commit (run 40235261498). The merge is complete; later fixes should use a new branch/PR.
- [DONE / MERGED] Provisional Boring Overnight evaluator, shared Schwab exclusion-list provider/tests, read-only Alpaca News API probe, and historical limit-sell calculation core are now on `master`. This is not equivalent to a production-ready or profitable overnight workflow.
- [NOW / BLOCKER] The `overnight-limit-sell` CLI needs a real input CSV at the supplied path. `eligible-window-high-excursions.csv` is an example filename, not a generated dataset. Required schema: optional first-row header `excursion_percent`, then one numeric percentage per row; minimum 20 valid completed observations. Every value must be the maximum high excursion from the reference entry/close during the same order-eligible window being evaluated. Do not substitute close-to-open returns, fabricated values, or a mismatched Alpaca BOATS session.
- [NOW / BLOCKER] Obtain/verify the user's actual Schwab order type and eligible hours (including whether overnight trading is enabled for the relevant security/account), then source historical bars that cover exactly that window. Record provider, feed, timestamps/time zone, missing intervals, corporate-action handling, and sample construction. Historical high-touch is not actual fill probability.
- [DONE / MERGED 2026-10-25] PR #2 merged to master as merge commit 2da69196ae098de5e28d24803714e8400826f999. The nullable-symbol fix, actionable missing-CSV diagnostics, and null-symbol regression test are merged. Post-merge CI run 40260494777 passed.
- [NEXT] Implement session-aware data acquisition and CSV generation with provenance; fail closed when the provider cannot cover the intended window. Add tests for holidays, DST, early closes, gaps, duplicates, invalid samples, and feed/session mismatch.
- [NEXT] Wire Schwab exclusion provider consistently into all candidate qualification paths (Boring Overnight, scanners, Morning Report, MarketWatch); current provider existence alone does not prove end-to-end integration.
- [NEXT] Implement persistent candidate/watchlist evidence, morning report data pipeline, catalyst freshness/coverage policy, and historical labeled validation. Alpaca News API returning articles proves endpoint access only, not complete catalyst coverage.
- [NEXT] Finish 5M-to-1M authority/handoff and V14.5 proxy divergence validation before changing ThinkScript. Do not claim readiness based on successful compilation alone.
- [USER ACTION REQUIRED] Run the CLI from the repository root after placing the verified CSV there, or pass an absolute CSV path. The CLI resolves relative paths from the current working directory. No valid source dataset is currently present in repository evidence: **Data not found in sources.**
- [USER ACTION REQUIRED] Before relying on a limit-sell reference, confirm actual Schwab order eligibility, actual entry fill price, estimated commissions/fees/spread/slippage, and whether overnight/extended limit orders are accepted for the symbol. Do not place an order based solely on the provisional calculator.
- [RELEASE GATE] MCC may be used this week for read-only smoke tests and manually reviewed decision support only. Boring Overnight automation is not launch-ready until source coverage, catalyst status, history, exclusion wiring, and session rules are verified. No automated order submission is in scope.


## 13. Boring Overnight trade-readiness plan — 2026-10-25 (operator workflow clarified in §15)

- [USER WORKFLOW] Research candidates during the afternoon; buy with LMT EXT between 3:00–5:00 PM Arizona, Monday–Thursday; no Friday entries. After the relevant regular-session close is known, set LMT GTC_EXT sell at close × 1.005. The target may differ from the buy fill.
- [SESSION RULE] The operator confirmed GTC_EXT as the intended sell qualifier, with the displayed 7:00 AM–8:00 PM ET active window. EXTO 24h is not required for this strategy. Do not count prices from 8:00 PM–7:00 AM ET as sell-window observations. Morning premarket and early regular-open observations can fall inside the stated active window.
- Arizona does not observe daylight saving time. During Eastern Daylight Time, 3:00–5:00 PM Arizona = 6:00–8:00 PM ET; during Eastern Standard Time, it = 5:00–7:00 PM ET. Use named time zones and an exchange calendar.
- [WEEKEND RISK] Thursday entries and weekend carry are allowed, but GTC does not guarantee an exit. A manual stale-position/unfilled-exit action and review time remain to be specified.
- [COST POLICY] User reports $0.00 commissions for intended eligible non-OTC stocks and a $6.95 OTC fee; OTCs are excluded on two maintained lists. Do not add the OTC fee to eligible non-OTC trades or invent additional fees. Track spread/slippage separately.
- [CRITICAL DATA GAP] Data not found in sources for a verified historical dataset covering the actual GTC_EXT window with source/feed provenance. Do not treat the prior fabricated CSV or the synthetic unit-test fixtures as market evidence.
- [TWO DISTINCT RESEARCH QUESTIONS] Evaluate the operator's current +0.5% heuristic (close × 1.005; chosen because observed median ON % often falls around 0.56–1.00%) against alternative history-derived target levels using eligible-window high excursions and a stated desired historical touch rate. This is a candidate benchmark, not a fixed rule. Compare out-of-sample; neither historical touch metric proves actual fills.
- [VALIDATION GATE] Include ordinary, down/flat, gap, catalyst, low-liquidity, excluded-symbol and no-touch cases. Track high-touch versus executable quote/trade evidence, actual fills when available, net returns, adverse excursion, spread/slippage, and weekend exposure.
- [RELEASE SCOPE] Read-only, manually reviewed decision support only. No automated order submission.

## 14. Launch checklist and real-data collection — 2026-10-26 (partially superseded by §15)

- [USER CONFIRMED] User checked BMY order ticket and reports the qualifier is GTC + Extended. Schwab's displayed definition: active 7:00 AM–8:00 PM ET, up to 180 calendar days, limit orders only; orders entered after 8:00 PM ET, on weekends, or holidays activate the next trading day.
- [USER COST POLICY] User says intended eligible non-OTC stocks have $0.00 commissions and OTCs cost $6.95, so OTCs are excluded on two maintained lists. Do not add the OTC charge to eligible non-OTC trades or invent other fees.
- [DATA BLOCKER] Prior eligible-window-high-excursions.csv values were fabricated and must be discarded. Never run the calculator against them.
- [DATA COLLECTION] Historical highs estimate whether a threshold was touched, not whether a limit order would fill. Record source/feed, session coverage, timestamp, spread/volume when available, missing intervals, and provenance.
- [RELEASE SCOPE] Read-only/manual decision support only until source-backed data, correct strategy-specific evaluation, consistent exclusions, and manual open-position/weekend handling are in place. No automated orders.
- [SUPERSEDED BY §15] The earlier requests to choose EXTO versus GTC_EXT and to decide whether weekend carry is allowed are superseded by the operator's explicit workflow below. Only the exact manual action for an unfilled position remains to be specified.

## 15. Operator clarification — actual Boring Overnight strategy and corrected research path (2026-10-26)

- [USER CONFIRMED] The strategy uses the “Use the Overnight” watchlist columns/current MCC checks; BMY Overnight Score reported as 33.49. This is an analytical signal, not proof of broker order eligibility.
- [USER CONFIRMED] Buy: limit order with EXT, 3:00–5:00 PM Arizona time, Monday–Thursday. No Friday new entries.
- [USER CONFIRMED] Sell: limit order with GTC_EXT at the relevant regular-session end-of-day close +0.5%. The target is based on the close and may differ from the actual buy fill. Reported fills commonly occur the next morning during premarket or the first minutes of regular trading; some occur the same evening.
- [USER CONFIRMED] GTC + Extended / GTC_EXT is the intended sell session; user-reported session definition is 7:00 AM–8:00 PM ET. Do not block this workflow solely because EXTO 24h is not used; 24-hour execution is not a requirement under this clarified strategy.
- [USER CONFIRMED] Weekend carry is allowed, including after Thursday entries, with an explicit manual risk plan. No Friday new entries remains in force.
- [RESEARCH CORRECTION] The +0.5% sell target is the operator's current heuristic, not a fixed strategy rule or random choice. It was selected because many observed median ON % values fall around 0.56–1.00%. Treat it as a baseline candidate to test against historical-reach-derived targets, not as an approved invariant. For each symbol/date, evaluate both the current +0.5% target and alternative target levels estimated from eligible-window historical highs; compare out-of-sample. Do not require 20 historical personal trades. Start with a small hand-verified OHLCSV pilot to prove source/session/calculation correctness, then expand.
- [RESEARCH CORRECTION] Keep historical reference-close study separate from actual-fill trade records. The first measures historical target touches; the second measures realized outcomes from actual buy/sell fills. Do not pool these samples.
- [CALCULATOR CAUTION] The percentile-based overnight-limit-sell CLI and the configurable +0.5% target-touch evaluator answer different questions. Keep the percentile calculator for candidate historical-reach levels; use the touch evaluator to evaluate a chosen target such as +0.5%. Neither is a fill-probability model.
- [USER ACTION] No need to reconstruct unrecorded historical trades from memory. For future trades, preserve order ticket, actual fills, reference close, target, timestamps, MCC output, and final outcome. Historical rows require source evidence; otherwise label unknown.
- [ENGINEERING] Build session-aware historical ingestion with feed coverage/provenance; compare +0.5% heuristic with historical-reach candidate targets; source/session validation and exchange-calendar handling; enforce the canonical OTC exclusion list across candidate paths; create news-assisted open-position/weekend review; no automated orders.


## 16. Branch and delivery discipline — 2026-10-26

- Keep `master` as the canonical, releasable source of truth. Do not create branches merely to edit one or two low-risk documentation paragraphs when an existing documentation PR/branch can carry the related changes.
- Use one short-lived branch per coherent code change when isolation, tests, review, or rollback matters (for example, a new evaluator plus its tests). Avoid multiple branches that duplicate the same task or leave competing versions of a document.
- Before creating a branch, inspect existing branches and open PRs; update the existing task branch/PR when it is the same deliverable. After merge, verify the change is on `master`; do not assume closed branches were deleted.
- Treat old feature branches as archival until their diff and merge status are checked. Do not delete branches without checking whether they contain unmerged work.


## 17. Near-term launch critical path — 2026-10-26

Goal: get the safest useful MCC workflow into manual use this week; do not call the full system production-ready simply because the CLI or build succeeds.

### P0 — before relying on any Boring Overnight candidate
1. [MERGED PR #5; TESTS PASSED] Added per-observation configurable reference-close target-touch evaluator (default target +0.5%) with source/feed provenance, complete-coverage requirement, eligible-window bounds, high timestamp, touch/no-touch, and DATA_GAP behavior. +0.5% is a candidate benchmark, not a fixed rule. GitHub Actions .NET tests passed for the code commit. This is calculation logic only, not historical ingestion or CLI/UI integration.
2. [ENGINEERING] Keep the percentile-based OvernightLimitSellCalculator separate. It answers a different question: what price threshold is suggested by a selected percentile of historical high excursions? Compare that candidate with the operator's current +0.5% heuristic; do not label either a fill probability.
3. [BLOCKER] Verify an actual historical source/feed covers the next GTC_EXT sell window (reported active 7:00 AM–8:00 PM ET) for the chosen symbols/dates. Until verified, status is DATA_GAP; Data not found in sources. Do not manufacture a pilot dataset.
4. [ENGINEERING] Trace both maintained OTC exclusion lists and wire the shared Schwab exclusion provider into every Boring Overnight/scanner/Morning Report/MarketWatch qualification path. Verify the OTC-fee policy is applied only to excluded OTCs.
5. [OPERATOR + ENGINEERING] Define visible/manual handling for an open position whose sell has not filled, including Thursday/weekend carry. No automatic cancel, replace, or sell.
6. [OPERATOR WORKFLOW] Until P0 is complete, use MCC only as read-only candidate triage; verify the ticket, data freshness, catalyst status, spread/liquidity, and position risk manually. Do not infer an order instruction from READY or score alone.

### P1 — urgent MCC usability issue
- [SOURCE AUDIT / INSTALLED VERSION UNVERIFIED] User reports EXECUTION READY signals are rare after 9:00 Arizona time and this materially reduces the useful scanning window; this is not a claim that source contains a hard 09:00 cutoff. Existing evidence is already available: on 2026-10-02, the 5M diagnostic showed Permission=0 by 12:45 Arizona, with PBLOCK=2 mapping to persistent major failure. The 1M V14.5 Library source explicitly defines READY as a short-lived event-armed window, not a persistent bullish state; it requires 5M execution permission, candidate environment/momentum, fresh trigger/re-arm rules, finite window, and no invalidation/extension danger. The unresolved question is why these gates seldom align after 09:00 Arizona in real use. [NEXT] Use aligned same-symbol 1M/5M OHLCSV exports plus existing diagnostic evidence to count candidate triggers, 5M permission revocations, extension blocks, re-arm failures, READY transitions, and expirations across 08:30–close Arizona. Do not ask user to repeat the 10/02 screenshot or infer a hard clock cutoff.
- [AUDIT] Finish targeted 5M authority → 1M handoff comparison on aligned symbol/date/timestamps. The 5M authority and diagnostic sources matched; 1M proxy differences remain unvalidated.
- [INTEGRATION] Wire the shared Schwab exclusion provider consistently and add tests proving excluded symbols cannot be marked eligible in each qualification path.
- [DATA] Choose/verify the historical market-data provider, feed entitlement, extended-hours coverage, timestamps, and licensing/access. Do not assume Alpaca IEX or BOATS reproduces Schwab's full eligible window.

### Launch boundary
- [POSSIBLE THIS WEEK] Read-only/manual decision support with explicit unknown/stale/catalyst flags and a manual order checklist, if current quote/source evidence and exclusion checks are independently verified.
- [NOT READY] Trusted historical-reach recommendations, automatic candidate READY, unattended position management, or automated orders until the source-backed validation and safety gates above pass.


## 18. Operator correction and launch decisions — 2026-10-26

### Historical-reach limit research
- [USER CORRECTION] The +0.5% sell limit is the operator's chosen heuristic, not a rule. It was motivated by observed median ON % commonly around 0.56–1.00%. Research must compare this heuristic with history-derived limit candidates, not treat +0.5% as ground truth.
- [NEXT INPUT] For the READY-after-09:00 investigation, request aligned 1M and 5M OHLCSV exports for a representative symbol/date, covering at least 08:30 Arizona through close, with extended-hours bars enabled if supported. Include date/time, OHLC, volume, symbol and timeframe metadata. Pair with study diagnostic screenshots or per-bar study output; OHLCV alone cannot reveal internal ThinkScript gates.
- [NEXT INPUT] For Boring Overnight historical-reach testing, export one symbol across several complete dates if possible, including regular-session close and the entire following GTC_EXT-eligible window (7:00 AM–8:00 PM ET as reported by operator). Include timestamps and source/feed; if export truncates extended hours or omits the relevant session, mark DATA_GAP. These are two distinct data requests and should not be conflated.
- [ENGINEERING] Pilot comparison should compute the +0.5% heuristic touch rate, empirical candidate target levels, observed excursion distribution, and coverage quality. Keep source provenance; split target-selection data from out-of-sample evaluation. No invented observations, and no claim of actual fill probability from highs alone.

### News-assisted unfilled-position / weekend review
- [USER DIRECTION] When a sell remains unfilled, MCC should check current Market News and provide a diagnosis. A newly confirmed adverse deal change may justify urgent manual exit review; a trading pause with thesis still intact may justify considering a lower sell limit. News informs the operator; it is not an automatic order trigger.
- [SPEC] Each review captures symbol, actual position/fill details if known, existing sell limit/order status, source-linked news items, publication and observation times, catalyst type, directional impact, evidence confidence, contradictory reports, price/volume/volatility and liquidity context, and source freshness.
- [SPEC] Output separates verified facts and links, interpretation, risk to the original thesis, and suggested manual choices (leave order working, review a lower limit, prioritize exit, or gather more evidence). No unsupported analyst target or unverified corporate deal.
- [SAFETY] No automatic cancel, replace, sell, or price change. Verified material adverse catalyst => URGENT_MANUAL_REVIEW. Missing or conflicting news => NEWS_UNKNOWN/CONFLICTING, not “no news.” A trading halt is a state to investigate, not proof conditions remain favorable.
- [SOURCE VERIFIED 2026-10-26] Master includes `AlpacaApiClient.GetLatestNewsAsync()` and CLI command `alpaca-news-check <SYMBOL>`; tests cover article parsing, denied access, and invalid limits. This is a read-only 24-hour latest-news fetch, not a complete catalyst monitor or position-diagnosis workflow. [NEXT] Extend the CLI/report to show summary, updated timestamp, source URL, symbols and freshness; then build news-assisted manual exit review with actual position/order context, contradictory-source handling, quote/liquidity context and explicit `NEWS_UNKNOWN` state. No automated order changes.
- [OPERATOR DECISION] No fixed rule for unfilled sells. Use news-assisted diagnosis and a manual decision, including the smallest profitable exit where appropriate. Profitability must use actual entry cost/fill and applicable fees/slippage; do not assume the reference-close target is profitable.

### OTC fee policy
- [USER CLARIFICATION] Do not spend current launch time reconciling the secondary OTC list. Keep the canonical repository list append-oriented and enforce it where already supported; revisit the $6.95 fee versus opportunity-cost decision later using actual trade economics. Do not silently add/remove exclusions.
