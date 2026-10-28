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
