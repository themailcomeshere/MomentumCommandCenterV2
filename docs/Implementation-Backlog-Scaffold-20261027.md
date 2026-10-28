# V3 implementation backlog — executable scaffolding

**Updated:** 2026-10-27  
**Branch:** `feature/opportunity-quality-classification`  
**Purpose:** Turn the canonical Master To-Do into implementation contracts, explicit acceptance criteria, and blockers. This is a design/scaffold artifact, not a claim that blocked integrations or live validation are complete.

## Execution rules

- Treat `docs/Master-To-Do.md` as canonical priority/status; this file is the technical companion, not a competing To-Do list.
- Missing, stale, conflicting, or unproven inputs fail closed to `DATA?`, `STALE`, `BLOCKED`, or `MANUAL_REVIEW`. Never coerce missing values to zero or a pass.
- Preserve Thinkorswim semantics: 5M authority is upstream; 1M execution may consume 5M state, never the reverse. Extension danger blocks chasing, not the underlying thesis.
- Every actionable result must carry source/provider, symbol, timeframe, data-as-of/source timestamps, receive timestamp, schema/version, evaluator version, reason codes, and a freshness classification.
- Never place orders or connect to Schwab API without explicit future approval. Manual Schwab order entry remains the boundary.
- Use Arizona local time (`America/Phoenix`) for user-entered/displayed wall-clock times. Keep API/provider/persistence timestamps in UTC and label both when shown together.

## Workstream contracts and acceptance criteria

### A. Dashboard time and Scan & Analyze
**Current implementation:** Scan & Analyze requests Alpaca 1M and 5M bars for the supplied symbols. The reason summary now renders latest source timestamps in Arizona time.  
**Acceptance:** Arizona timestamp labels must be consistent in every dashboard table/report/reason surface; API ISO values remain unchanged; test Arizona-to-UTC conversion on winter and summer dates and from a browser configured to a non-Arizona timezone. Verify empty results, request errors, malformed timestamps, and 1M/5M asymmetry.  
**Known limit:** OHLCV summaries are not TOS Permission/READY states or Boring Overnight qualification.

### B. Scanner discovery and qualification
Implement in layers:
1. Normalize provider events to `ScannerDiscoveryEvent`: stable event ID when supplied, normalized symbol/group, source, first-seen UTC, observed UTC, data-as-of UTC, raw-field provenance, schema version, status/reason codes, evaluator version.
2. Persist append-only; deduplicate idempotently without rewriting first-seen time. Preserve corrections as new observations.
3. Add explicit freshness policy by workflow, not one universal age. Until policy is approved, keep observations descriptive and never mark them actionable.
4. Add provider-neutral qualification stages: data completeness/freshness; exclusions; liquidity/price; relative volume against a named baseline; exact versioned extension calculation; 5M permission; 1M execution event; catalyst/market context; final state and reasons.
5. Keep separate queues: `ACTIONABLE_CANDIDATE`, `WATCH_PULLBACK`, `FILTERED`, `BLOCKED`, `DATA_UNKNOWN`. Requalification requires fresh data and a new valid event; do not change source-study states.
6. Add shadow-mode comparison and false-positive/false-negative tracking before enabling a gate.
**Acceptance tests:** duplicate delivery, out-of-order/corrected data, stale/missing timestamps, unknown provider, invalid symbol, missing quote/spread, missing catalyst status, exclusion hit, extension warning/danger, 5M permission false, READY expired, pullback re-entry, restart/recovery, and deterministic repeated evaluation.

### C. Provider adapters and operational reliability
**Existing:** historical REST adapters for Alpaca and Massive, provider comparison, raw-bar persistence, collector lifecycle/checkpoint scaffold.  
**Blocked:** credentials, subscription entitlements/licensing, feed coverage, and the explicit provider choice for live scanner discovery.  
**Next implementation:** provider capability manifest; connection/last-success/data-as-of status; request throttling; retry/backoff; cancellation; reconnect and backfill; durable append-only observations; retention/disk health; correction policy; per-symbol/timeframe watermarks; session-aware gaps. Keep secrets server-side.  
**Acceptance:** fixture-based adapter contract tests plus controlled licensed live smoke test; restart with checkpoint file hash/time captured before and after; prove watermark reload and advancement only when a newer eligible bar exists. A successful HTTP response alone does not prove completeness or live readiness.

### D. Boring Overnight evaluator
**Current:** documented criteria and API evaluator scaffold exist; dashboard does not calculate complete scores.  
**Contract:** separate historical edge, boringness/consistency, liquidity, current-day abnormality, event/catalyst risk, exclusion status, 5M structure, 1M timing, data freshness, and final decision. Output `READY`, `WATCH`, `BLOCKED`, or `DATA?` with hard-gate reasons and per-input provenance. A zero-position decision is valid.  
**Inputs required:** source-backed regular close and full defined overnight window; completed session policy; daily volume/price history; ON win rate, mean, median, 5D/20D averages and volatility/consistency; maintained exclusion list; timestamped event/news status; current price and session context.  
**Acceptance:** independently calculate the same-symbol/same-date metrics in C# and spreadsheet/Python fixture; test each hard-gate failure individually; ensure catalyst unknown and incomplete session cannot return READY. Validate winners, losers, gap/spike days, extended candidates, and false positives. No profitability claims from compilation or a handful of charts.

### E. Thinkorswim 5M authority and 1M execution parity
**Preserve:** 5M source is authoritative; 1M reconstruction must not invent independent 5M semantics. Keep PRAW, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment separate.  
**Open evidence:** installed study version parity; same-bar per-state diagnostics; exact extension/ATR formula parity; 31 irregular interval classification where relevant; full-session and ETH/RTH reconciliation; DST/session boundary tests.  
**Acceptance:** golden fixtures for Permission transitions and reason codes; candidate/continuation/confirmation/event-armed READY/expiry/re-arm/re-entry; extension danger blocks entry without erasing thesis; runner rising/waning/swing/recovery/failure; historical bubble state validated on the actual bar. Do not edit study gates based only on OHLCV or missing bubbles.

### F. Extended momentum / pullback continuation
Implement as a separate watch workflow, not a relaxed original gate. Inputs: fresh OHLCV, volume baseline, quote/spread/liquidity, source-timestamped catalyst/news, halt status, current 5M Permission, current 1M execution state, exact extension measure, and re-entry trigger.  
**Acceptance:** extended-but-not-executable, credible continuation watch, and valid fresh entry are distinct states; missing any required input yields DATA?/BLOCKED; original red EXT remains red; shadow-test both efficiency and missed-opportunity rate. News narrative alone never grants eligibility.

### G. Morning Report / collector / Red Zone
**Existing:** Morning Brief endpoint and UI scaffold, provider status, historical comparisons, collector status, local report export, Red Zone layout.  
**Next:** persist dated report snapshots; deterministic ranking from qualified candidates only; populate top 1–4 tiles with provenance, freshness, 5M/1M gates and reasons; add benchmark/regime only after independently defined calculations; alert on collector failure/staleness; export exact snapshot used for the report.  
**Acceptance:** repeatable report for a fixed input snapshot, restart recovery, no live-looking demo values, no candidate promotion from score alone, and explicit unavailable fields. Browser-local notes are not backend persistence.

### H. Import, parity, and evidence provenance
Separate Alpaca OHLCV CSV/JSON from TOS StrategyReport. Rename JSON fixture with a truthful extension and update solution references when safe. Add filename/header/payload symbol/date checks; preserve partial rows and gaps; never synthesize bars. Store provenance manifest with source filename/hash where available, study/version, symbol/date, timezone, RTH/ETH, timestamp semantics, counts and comparison result.  
**Acceptance:** malformed/duplicate/partial/wrong-symbol/wrong-date/empty import tests; continuous OHLCV versus StrategyReport separation; complete versus incomplete 1M-to-5M window tests; timestamp convention documented only after evidence.

### I. Validation and test architecture
Add a machine-readable validation manifest for known fixtures and expected results. Tests must be deterministic, offline by default, and clearly distinguish unit/fixture tests from live provider smoke tests. Add tests for time conversion, UTC serialization, stale/missing data, exclusion decisions, evaluator hard gates, persistence idempotency, restart recovery, provider failures, and report reproducibility.  
**Acceptance:** run `dotnet test MomentumCommandCenterV2.sln --configuration Release` and CI at exact head; record commit SHA and result. No claims based on prior-head tests.

### J. Solution, documentation, and operating workflow
Keep one canonical Master To-Do and one New Chat Handoff; domain docs remain linked companion specifications. Solution folders are organizational only. Diagnose actual incompatible project loads from exact Visual Studio Output errors before retargeting frameworks. Maintain a user runbook for branch/pull, build/test, API launch, health/provider status, import endpoints, scan, log sanitization, and known demo/live boundaries.

## Explicit external blockers (do not guess around them)

- **Provider/live scanner:** user choice, credentials, plan entitlements, feed licensing, and live smoke-test access.
- **TOS parity:** installed 1M/5M source version and same-bar diagnostic states for the target examples.
- **Overnight stats:** complete source-backed regular close and eligible overnight-window bars plus a decided session boundary; actual fills are a separate source.
- **Catalyst clearance:** a selected source with coverage, timestamp, and acceptable latency; absent data is not “clear.”
- **Freshness thresholds:** separate agreed policy for intraday entry versus completed overnight research.
- **Schwab:** no API/order integration approved; manual entry only.
- **Visual Studio load error:** exact project name and first Output error if an actual C# project remains incompatible.

## Release gate

Do not label MCC production/live-trading ready until: exact-head CI/tests pass; provider access and entitlements are verified; full-session OHLCV parity and gaps are classified; TOS state parity is demonstrated; scanner qualification passes fixture and shadow-mode tests; overnight evaluator is validated on independent data; restart/reconnect/backfill are verified; and missing/stale/catalyst-unknown inputs fail closed. Until then the deliverable is a local research workbench with historical API adapters and explicitly bounded analysis.


## 2026-10-28 — Sparse-bar correctness and provider-date default

### Implemented
- Scan & Analyze now filters malformed source timestamps and sorts valid bars before calculating summaries.
- A change and high-low range are calculated only when at least two valid bars exist. A single returned bar is no longer represented as `0.00% change` and `0.00% range`.
- The result status is now `INSUFFICIENT DATA` unless both 1M and 5M have at least two valid bars. Reason text explicitly flags a timeframe that returned only one bar or no bars and explains that change/range cannot be calculated.
- Provider comparison's Test Date no longer contains a stale hard-coded date. When empty, it defaults to the current calendar date in `America/Phoenix`, independent of the workstation timezone.
- The provider comparison's Arizona end-of-day bound includes the final millisecond.

### PMI observation (user-reported)
- Requested symbol PMI, 2026-10-26 Arizona 06:02–16:02; only one bar was returned per timeframe, timestamp 06:55 Arizona, volume 120.
- The prior 0.00% result was an arithmetic artifact: first bar and last bar were the same single observation. It did **not** establish that PMI did not move.
- New UI behavior correctly marks this result insufficient and avoids inventing movement. The reason for the provider returning one bar is not established from current sources; inspect the API payload's provider/feed, raw bars, request window, and provider status before diagnosing ticker support, entitlement, or historical coverage.

### Verification supplied by user
- User reports pull, API-hosted dashboard, Scan & Analyze, Arizona display, 100 tests, and exact-head CI green all succeeded after the previous change set.
- User's so-called “Summer” example used 2026-10-26, which is autumn, not a summer date. A true summer-date check remains open. Arizona stays on MST (UTC−07:00) year-round, but test at least a July date to validate the code path.
- New source changes in this section have not yet been run by the user or verified by a new CI run.

### Next validation
- Re-run PMI for the same 10-hour window and confirm 1M/5M show `INSUFFICIENT DATA`, score/change as unavailable, and a reason coverage warning.
- Repeat on a known liquid symbol and verify multiple bars and nonzero changes when source bars support them.
- Inspect `GET /api/market-data/bars` response for PMI and compare provider/feed, first/latest timestamps, barCount and gapCount with Provider Comparison. Do not attribute the one-bar response to a provider limitation until those observations are reviewed.
- Confirm Test Date defaults to today's Arizona date on initial page load, while preserving a date the user selects during the session.


## Implementation checkpoint — fail-closed validation hardening — 2026-10-28

- [DONE — SOURCE IN THIS COMMIT] Boring Overnight evaluator now rejects blank/invalid symbols and non-finite numeric values (NaN, positive infinity, negative infinity) as DATA? before gate evaluation. This closes a correctness hole because NaN comparisons can all evaluate false and accidentally let a malformed snapshot appear eligible.
- [DONE — TESTS ADDED IN THIS COMMIT] Added evaluator regression cases for NaN/infinities and invalid symbols, plus raw market-bar store tests for non-finite prices, impossible OHLC relationships, and negative volume.
- [PENDING — CI] Run the Release build/test workflow for this exact commit before treating the new cases as passing.
- [NOT IMPLEMENTED / BLOCKED BY EVIDENCE] Do not claim autonomous Boring Overnight qualification: daily/overnight metric computation, complete GTC_EXT session coverage, session-aware gap classification, catalyst source, and authoritative TOS states still require implementation and/or source evidence. The evaluator continues to accept caller-supplied snapshots only.
- [NOT IMPLEMENTED / BLOCKED BY EXTERNAL SETUP] Live scanner discovery and provider proof remain blocked by provider selection, credentials/entitlements, coverage/licensing, and real-data smoke tests. The current collector is explicit-symbol historical REST polling, not a market-wide scanner or websocket stream.
- [USER/LOCAL VALIDATION OPEN] Confirm the Provider Comparison date after browser refresh; source initialization and cache-bust are committed, but browser behavior has not been independently observed here.
- [OPEN] BrowserLink $.noConflict is not a function remains unisolated. Compare with Browser Link disabled before adding application workarounds.


## Follow-up correction — validation hardening compile fix — 2026-10-28

- [FIX COMMITTED] The first validation-hardening CI run failed during build with CS0136 because a local variable named reasons conflicted with a later declaration in the evaluator. Renamed the early-return variable to invalidReasons in commit 65c83f7918200e413a5b20fb7942392357c7d193.
- [PENDING — CURRENT HEAD] Re-run build and tests on the exact latest head after this fix. The previous run 40737130070 is a known failure and must not be reported as passing.
- [NEXT] Verify the new evaluator tests for NaN/infinite values and invalid symbols, and the store tests for invalid OHLCV, once the workflow completes.


## Follow-up — batch validation is atomic — 2026-10-28

- [DONE — SOURCE COMMITTED] The raw-bar store now validates every bar in a batch before opening the append stream. A malformed later row can no longer leave earlier rows from that same batch partially persisted.
- [TEST ADDED] Invalid-OHLCV tests now submit a valid bar followed by the invalid bar and assert the store creates no file.
- [PENDING — CI] Confirm the latest branch-head workflow passes build and tests; earlier validation commit 4208441 failed build, and commit 65c83f7 fixes that compiler issue. Neither failure is considered green.
