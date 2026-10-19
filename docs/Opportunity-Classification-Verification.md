# Opportunity Classification: Operational Verification and News/Data Boundaries

**Status:** Design and verification checklist; not a live-data implementation.
**Scope:** Define how MCC can establish whether an Opportunity Classification is useful in actual practice without confusing a diagnostic score with a trade recommendation.

## 1. Current verified capability boundary

The repository documents a deterministic, pure `OpportunityQualityAssessor` and unit tests for its input precedence and classifications. Its input includes measurements such as recent range and relative volume, but the assessor does not calculate trustworthy values from raw market bars itself.

The assessor is not wired into `V2SignalEngine`, market-data ingestion/replay, the CLI, or the front end. The current market-data layer reads local timestamped OHLCV and emits replay frames; it does not calculate indicators, detect news, or simulate fills.

Therefore, **today the application does not establish that a live stock deserves a classification in real time**. Unit tests establish deterministic behavior for supplied inputs, not market-data correctness, profitability, or live execution quality.

## 2. Practical verification loop

### Phase A — Freeze the baseline

- Preserve current 5M authority, 1M execution gates, READY lifetime, and position lifecycle behavior.
- Record the strategy/study version, classification config version, and test-data provenance.
- Keep opportunity quality diagnostic-only. Do not route it to order placement or let it override a hard gate.

### Phase B — Build trusted timestamped inputs

For each symbol and evaluation time, record:
- Timestamped 1M and 5M completed bars and their source, timezone, and availability time.
- 5M permission/authority and 1M candidate/READY state, including READY start, expiry, and block reasons.
- Indicator values only after formulas, warm-up, aggregation, session reset, and missing-data behavior have parity tests against the versioned ThinkScript source.
- Relative volume with a defined time-of-day baseline; recent range/ATR with a defined lookback; resistance/swing-high distance with an explicit calculation.
- Spread/liquidity only from a reliable source. Never infer spread or catalyst clearance from OHLCV alone.
- Any news/event evidence with publisher/source, headline, publication time, retrieval time, URL/identifier, and symbol-match confidence. Missing news data is UNKNOWN, not “no catalyst.”

Every input must be no newer than the decision timestamp. Distinguish event publication time from ingestion/retrieval time and avoid look-ahead.

### Phase C — Run a shadow classification

- Evaluate every candidate, including ones not traded, at the time the decision would have been made.
- Persist the immutable input snapshot, all component values, classification, reason codes, freshness flags, config version, and source timestamps.
- Show the classification beside—not in place of—the existing 5M/1M signals.
- Do not change behavior or claim a candidate was tradable merely because it was labelled READY.

### Phase D — Compare with outcomes

For each candidate, link subsequent historical bars and actual fills when available. Measure:
- MFE and MAE from a defined reference and horizon.
- Return at fixed horizons, time to favorable/adverse movement, and realized outcomes where actual fill/exit records exist.
- High-classification false positives, high-quality candidates missed, lower-classification winners, and results by symbol, time of day, market regime, and news/event condition.
- Sensitivity to spread/slippage only when reliable data is available.

Use time-separated development and validation periods. Calibrate bands on one period and evaluate on unseen later sessions. Compare against the existing READY-only baseline. Do not tune on a handful of examples. A high score is not a win probability unless separately calibrated and validated as one.

### Phase E — Define operational acceptance gates

Do not promote the classification from diagnostic-only until:
- Data timestamp, session, indicator, and freshness parity checks pass.
- Missing/stale data always fails closed to UNKNOWN/BLOCKED as appropriate.
- Existing eligibility and READY expiry remain authoritative.
- Historical evaluation demonstrates a measurable benefit over the READY-only baseline across multiple symbols and market conditions, with sample sizes and uncertainty reported.
- Shadow-mode results and any failure cases are reviewed before the UI presents the classification as an operational priority.

No universal numeric acceptance threshold is specified here; it must be agreed before evaluating results, not chosen after seeing them.

## 3. News and AI integration answer

The current repository has **no verified live market-news/catalyst integration**, and this code path does not query ChatGPT or Google for every symbol. There is no evidence that it calls an AI search response behind the scenes.

A future news integration should be an explicit provider adapter, not an implicit AI oracle:
1. A scheduled or event-driven adapter obtains headlines from a selected provider/API.
2. Normalize symbol/entity, source, headline, published time, retrieved time, and URL/ID.
3. Deduplicate and preserve provenance; mark feed failures and stale coverage as UNKNOWN.
4. Optionally ask an LLM to classify supplied headlines into structured, cited categories (e.g. earnings, offering, FDA/regulatory, merger, halt, analyst action). The LLM must not invent events and must return source IDs/evidence.
5. Store the raw evidence and structured result so a decision can be reproduced without re-querying the model.
6. Keep news classification as a risk/context input, not a substitute for 5M authority or 1M execution. No “catalyst clear” value unless coverage/completeness rules justify it.

Google search, a market-news API, and an LLM are different components. Google/another search engine can retrieve public pages; a news provider can offer structured feed data; an LLM can classify evidence that it is actually given. None is currently established as connected to MCC. Choose a provider only after defining coverage, latency, licensing, cost, historical availability, and outage behavior.

## 4. Current status

- Unit tests: user reports all pass; this document does not independently execute them.
- Live quotes/news, indicator parity, historical outcome linkage, fill simulation, and production alerts: not established as implemented.
- Automated orders and autonomous brokerage submission remain out of scope.
