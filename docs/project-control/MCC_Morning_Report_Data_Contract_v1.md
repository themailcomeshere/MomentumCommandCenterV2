# MCC V3 — Morning Report and Current Conditions Data Contract
**Status:** Draft specification based on `Morning.txt`; source selection and live implementation remain open.  
**Last updated:** 2026-10-23

## 1. Purpose

Deliver a concise briefing for the user's approximately 08:30 Arizona-time trading session, then update the Current Conditions view as the session develops. The report is not a static market essay and must not pretend to predict individual stock prices. It should preserve the initial expectation, record observed conditions later, and measure divergence.

The Morning Report has three panels:
1. **Morning Landscape** — what has happened.
2. **Daily Predictions / Scenario Expectations** — which kinds of setups appear more or less aligned with observed conditions, with evidence and uncertainty.
3. **Current Conditions** — what is true now and how it has changed since the morning snapshot.

The watchlist should already be sorted when the report is generated only if the watchlist/ranking service has actually run successfully. If it has not, display its state as unavailable rather than implying it is running.

## 2. Panel A — Morning Landscape

### Market context
Display available, timestamped values for:
- SPY, QQQ, and IWM direction and percentage change;
- market breadth;
- VIX / volatility context;
- summarized market regime (`STRONG`, `NORMAL`, `WEAK`, or `UNKNOWN`) with rule/version and supporting observations.

Do not calculate a composite regime when its required inputs are missing. Show which inputs are missing and when each was last updated.

### Industry / money flow
Display strongest and weakest industries/sectors with:
- sector/industry name and classification source;
- current percentage change and observation timestamp;
- relative strength or momentum measure, including its calculation window;
- whether strength is accelerating or fading, only when multiple time-stamped observations support the comparison.

If industry mapping, breadth, or sector data is unavailable from the chosen provider, mark the panel `DATA?` and do not fabricate rankings.

### Overnight / premarket movers
For each material mover show:
- symbol, current/last price, percentage move and reference price;
- the portion of the move occurring overnight/premarket versus during the user's defined trading window, if reliable intraday bars support the split;
- volume and relative-volume context with a defined baseline;
- catalyst category, source, publication timestamp and verification state;
- extension/chase-risk flag and reason, not merely the raw gain percentage.

Do not call a move “mostly premarket” without calculating it from timestamped bars and a defined reference price.

## 3. Panel B — Daily Predictions as scenario hypotheses

Do not issue unsupported statements such as “symbol X will rise today.” Summarize the environment and the types of setups that may be favored or challenged, based on observable evidence and validated historical outcomes.

Candidate scenario dimensions from `Morning.txt`:
- momentum environment;
- expected volatility regime;
- breakout and continuation environment;
- mean-reversion risk;
- fresh catalyst + high RVOL + strong industry + bullish 5M structure;
- extended overnight runners with declining RVOL / weakening momentum;
- opportunity map: highest-quality opportunity type and patterns to avoid chasing.

### Required discipline
- Initially present these as qualitative hypotheses (`FAVORABLE`, `MIXED`, `UNFAVORABLE`, `UNKNOWN`) unless calibrated statistical models support probabilities.
- A numeric probability requires a defined outcome, horizon, sample count, point-in-time features, validation period, and calibration results. Example numbers in brainstorming material are illustrative, not live facts.
- Every expectation must include `as_of`, supporting features/sources, rule or model version, and confidence/sample-size context where available.
- Store the morning snapshot immutably so later conditions and outcomes can be compared against it.
- Do not use later-day information to rewrite what the system knew at 08:30; avoid look-ahead leakage in training/evaluation.

## 4. Panel C — Current Conditions (continuously refreshed)

The `Morning.txt` example names these dimensions:
- Market
- Momentum
- Volatility
- Sector concentration
- Fresh catalysts
- Chase risk

Each dimension must have a value/status, supporting data, timestamp, and freshness state. Current Conditions must show changes relative to the initial snapshot, for example `CHASE RISK: INCREASING`, only when the metric has actually changed according to defined rules.

A later observation should answer:
- What changed since the last update?
- Which input or rule caused the state change?
- Is this a market-wide, industry-wide, or company-specific change?
- Does this change affect the scenario expectations or the ranked opportunities?
- Is the change supported by fresh evidence or is the feed stale/unavailable?

Persist timestamped snapshots and state transitions; do not store only the latest state.

## 5. Connection to watchlist and Boring Overnight

- Consume the persistent MCC watchlist only after its retrieval/sort succeeds; show sort timestamp, ranking version, and missing-data warnings.
- Boring Overnight should incorporate catalyst, historical overnight behavior, liquidity, industry, market regime, extension, continuation/deterioration risk, and expected overnight range only when each input is available and freshness is visible.
- A strong current mover is not automatically a good overnight candidate. Historical overnight edge and current-day qualification remain separate.
- Catalyst classifications must distinguish evidence from interpretation. The `Morning.txt` “pump → dump” idea is a probabilistic risk state, not a factual label: combine catalyst strength, momentum fading, extreme extension, weakening buyers, and increasing selling only where observable data support each component.
- A catalyst-free state cannot be assumed when no news feed is connected. Use `UNKNOWN` / `DATA?` and surface the limitation.

## 6. Data contract and provenance

Each report generation and each refresh should persist:
- report ID, report type, generated-at UTC, rendered Arizona time, trading date, and data cut-off time;
- source/provider/feed and request/observation timestamp for every field;
- market, sector, volatility, price/volume, and catalyst observations with units and calculation windows;
- missing/stale flags and source errors;
- scenario hypothesis, supporting features, rule/model version, sample size and calibration metadata when applicable;
- watchlist/ranking snapshot ID and Boring Overnight evaluation snapshot IDs, if consumed;
- all state changes since the previous snapshot and the reason for each change.

No sample values in `Morning.txt` (for example, sample sector ranks, percentage changes, or condition colors) should appear as live market facts. They are mockups only.

## 7. Refresh and history behavior

- Initial target: one briefing around 08:30 Arizona time, subject to user confirmation and data-provider timing.
- Current Conditions refresh cadence is unresolved and must be configurable; do not claim real-time behavior until a working refresh mechanism and provider limits are verified.
- Store the initial morning snapshot plus subsequent periodic/event-driven snapshots.
- Capture expectation-versus-observation divergence for later evaluation. Keep original predictions immutable; append later revisions and outcomes.
- Respect market holidays, early closes, premarket/regular/after-hours session boundaries, and Arizona versus Eastern time differences.

## 8. Acceptance tests

1. Report generates with complete data and clearly labels all values with their source/as-of time.
2. Missing breadth, VIX, sector mapping, catalyst feed, or watchlist service yields visible `DATA?` rather than invented values.
3. Premarket and regular-session movement are split correctly at configured boundaries.
4. A stale feed cannot be rendered as current without a stale indicator.
5. Daily Predictions use qualitative hypotheses until the required outcome definitions and calibration exist.
6. Numeric probabilities require sample count, horizon, outcome definition, model version, and validation/calibration metadata.
7. Current Conditions records timestamped changes and explains the contributing observations.
8. Morning expectation is not overwritten by later observations; no look-ahead data leaks into historical evaluation.
9. Catalyst facts link to sources; rumors remain unverified; conflicting reports remain visible.
10. Boring Overnight consumes only available, timestamped inputs and does not equate “big mover” with “good overnight hold.”
11. No invented example values from the source brainstorm are displayed as live data.

## 9. Open decisions / user input

1. Confirm whether the briefing should be a local CLI artifact, a local UI panel, or part of the eventual web UI. The current repository/runtime target needs to be verified before choosing implementation details.
2. Approve providers for market breadth, VIX, sector/industry classification and returns, news/catalysts, and any analyst/target data. Data not found in sources for approved live providers/entitlements.
3. Confirm the precise Arizona trading window used to separate premarket movement from movement during the user's trading time.
4. Set Current Conditions refresh cadence and whether alerts are desired, including channel and quiet hours.
5. Confirm whether the first release should use only qualitative scenario labels or wait for validated historical probabilities; recommendation is qualitative labels first.

## [Uncertainties]
- The source brainstorming specifies the 08:30 Arizona target and panel concepts, but not approved providers, refresh cadence, exact trading-window boundaries, or delivery surface.
- No live market/news data source or entitlement was verified while drafting this contract.
- Scenario probabilities are not supportable until outcome definitions, sufficient historical samples, and calibration are implemented and tested.