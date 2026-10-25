# MCC V3 — Morning Report and Current Conditions Data Contract
**Status:** Draft specification based on `Morning.txt`; recommended first-release defaults documented below, live implementation remains open.  
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

- Initial target: one briefing at approximately 08:30 Arizona time. Generate it from an explicit data cut-off timestamp and show unavailable inputs.
- Recommended first-release delivery surface: a CLI-generated Markdown report/artifact. This fits the verified CLI-first repository and is easy to diff/test; defer a desktop/web UI until the core data pipeline is proven.
- Recommended Current Conditions cadence: recalculate market/industry/technical summaries every 5 minutes during the configured trading window. Consume news/catalyst updates as events when streaming is available; otherwise poll at 1–2 minute intervals only after provider limits are confirmed. Every output remains timestamped and freshness-aware.
- Proposed configurable trading window: 08:30–12:00 Arizona time, explicitly provisional rather than an assumed user preference. Use this only to measure move-before-window versus move-during-window and to filter timing context; do not suppress candidates until the user approves the window. Do not assume the window is an order/trading authorization.
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

## 9. Recommended first-release choices and remaining approvals

1. **Delivery:** CLI-generated Markdown report/artifact first; defer GUI selection until the report/data pipeline works. This is a recommendation, not a claim the CLI report is implemented.
2. **Market/industry data:** use Alpaca bars for the initial price/volume adapter, while clearly marking the IEX feed's limited single-exchange coverage. For a market-regime prototype, SPY/QQQ/IWM bars are straightforward, but breadth and VIX require separate data fields/providers or a defined universe-derived proxy. Do not label a three-ETF sample as full market breadth.
3. **Industry ranking:** a stock's industry label can be looked up, but ranking strongest/weakest industries requires a maintained ticker-to-industry classification, a defined constituent universe, time-aligned prices/returns, and a calculation window. A classification label alone does not establish industry momentum.
4. **News/catalysts:** yes, an API/feed is needed for automated source-backed news monitoring. Alpaca documents a News API and real-time news stream; historical news is described by Alpaca as provided by Benzinga. Before enabling it, verify the user's account access, coverage, licensing/usage rights, rate limits, and article timestamps. The account currently has a read-only News API probe implemented as `dotnet run --project src/MomentumCommandCenterV2.Cli -- alpaca-news-check AAPL`. It requests recent articles for the last 24 hours, prints returned article metadata, and never submits orders. Run it locally with the existing .NET User Secrets or environment variables. A successful response verifies endpoint access only; a zero-article response does not prove no news exists, and neither outcome proves complete coverage or catalyst clearance. The previous successful smoke test verified account authentication and one IEX latest-bar request; it did not test News API access. Until verified, show `UNKNOWN`/`DATA?`, not catalyst-clear. [Alpaca historical news documentation](https://docs.alpaca.markets/us/docs/historical-news-data) and [real-time news documentation](https://docs.alpaca.markets/us/docs/streaming-real-time-news).
5. **Refresh:** recommended market/industry calculations every 5 minutes during the configured window; news stream when available, otherwise a 1–2 minute polling target only if provider limits permit. This is a proposed cadence, not live functionality.
6. **Trading window:** use 08:30–12:00 Arizona as a configurable provisional default for development only. Ask the user to approve before this window affects candidate suppression or personalized timing conclusions.
7. **Prediction format:** qualitative scenarios first; no numerical probabilities until outcomes, sample sizes, point-in-time features, validation, and calibration are implemented.
8. **Alerts:** defer push/email alerts; first persist the state change and show it in the report. Decide channels and quiet hours only when the user interface is selected.

## [Uncertainties]
- The source brainstorming specifies the 08:30 Arizona target and panel concepts, but not approved providers, refresh cadence, exact trading-window boundaries, or delivery surface.
- Alpaca News API endpoints are documented publicly, but this account’s News API entitlement and usable coverage have not been tested. The successful smoke test only established account auth and one IEX bar request.
- Scenario probabilities are not supportable until outcome definitions, sufficient historical samples, and calibration are implemented and tested.