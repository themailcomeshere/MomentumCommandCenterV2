# Momentum Command Center V3 — Boring Overnight Decision Column / Chart Study TODO

**Status:** Planned — not implemented or validated yet  
**Related existing Library item:** `/Trading System/Boring_Overnight_Cheat_Sheet.md`  
**Purpose:** Move Boring Overnight decisions from ad hoc manual approval into a repeatable, explainable, testable workflow.

## Problem / design goal

The HEQ position entered from the 2026-10-09 `EXECUTION READY` signal illustrates the cost of relying on an intraday execution signal without a separate overnight-position decision process. Intraday momentum states and overnight suitability are different questions. A stock can be a poor intraday momentum trade but a reasonable overnight candidate, or vice versa.

V3 should not require the user to ask ChatGPT for final approval for every overnight candidate. The system should report a consistent decision, the evidence behind it, and explicit reasons for any block or missing data. **Zero position is a valid outcome.**

## Deliverable order

1. **Specify and implement a Boring Overnight Decision watchlist column first.** This is the primary sortable triage surface across scanner candidates.
2. Validate its calculations and decision states against known examples and historical observations.
3. Add a companion chart study only if it adds useful explanatory context that the column cannot provide.
4. Integrate the authoritative historical statistics and decision log into MomentumCommandCenterV3 C# where ThinkScript cannot reliably calculate or retain the complete data set.

Do not create duplicate or conflicting definitions of the same score in the column, chart study, and C# application. Document which layer is authoritative for each metric.

## Proposed decision outputs

- `READY` — all required historical, liquidity, current-behavior, event-risk, and chart/session checks pass using sufficiently fresh data.
- `WATCH` — historically promising, but not currently qualified; identify the condition preventing readiness.
- `BLOCKED` — a hard gate or material risk condition failed; identify the reason.
- `DATA?` — required data is missing, stale, unsupported, or cannot be calculated reliably.

Missing or stale data must **never** silently become `READY`. A status must be accompanied by a compact reason code or explanation wherever the platform's column limitations allow.

## Decision pipeline

1. **Overnight Edge Score** — historical ability to produce positive overnight returns.
2. **Boringness Score** — consistency, controlled return distribution, low relative volatility, liquidity, and price suitability.
3. **Event / Catalyst Risk** — earnings, M&A, regulatory/government decisions, major corporate actions, material news, and regime-changing catalysts.
4. **Current-Day Qualification** — abnormal intraday extension, unusual volume, a fresh catalyst spike, excessive current overnight move, or deterioration relative to normal behavior.
5. **5M chart / structure** — current structure and permission checks where appropriate.
6. **1M chart / timing** — entry timing confirmation where appropriate; must not replace the 5M authority layer.
7. **Overnight Decision** — `READY`, `WATCH`, `BLOCKED`, or `DATA?`, with reason(s).
8. **Zero position** — remain out when no candidate survives.

Keep Overnight Edge Score and Boringness Score separate until their individual components have been tested and validated. A historically high average overnight return is not equivalent to a boring, repeatable edge.

## Existing Boring Overnight rules to preserve for initial implementation

These are current project design thresholds, not proof of profitability. Preserve them initially; tune only after testing against actual outcomes.

### Hard gates / rejection conditions

- Price >= $5.
- 20-day average volume >= 500,000 shares/day.
- Overnight win rate >= 60%.
- 20-day average overnight return >= +0.10%.
- 5-day average overnight return must be greater than the 20-day average overnight return under the current cheat-sheet rule.
- Price above the 20-day SMA.
- Symbol is not on the Schwab-excluded securities list.
- No material event/catalyst risk incompatible with a boring overnight hold.

Current complete user-provided list (55 symbols) is in `../Schwab_Exclusions_SourceOfTruth.md` (canonical repository path: `docs/project-control/Schwab_Exclusions_SourceOfTruth.md`). It is append-oriented; do not remove entries without explicit review.

### Boringness score weights (100 points)

| Component | Weight | Desired behavior |
|---|---:|---|
| Overnight win % | 25 | Higher is better, subject to >=60% hard floor |
| Median overnight % | 20 | Positive and reasonably consistent with average |
| 20-day average overnight % | 15 | Positive but do not reward extreme gaps |
| 5-day average overnight % | 10 | Positive / improving but controlled |
| Overnight consistency | 10 | Higher is better |
| Overnight volatility | 10 | Lower is better relative to return earned |
| Average volume | 5 | Higher liquidity |
| Price | 5 | Prefer established stocks above $5 |

Ranking bands: 90–100 `PERFECT BORING`; 80–89 `VERY GOOD`; 70–79 `POSSIBLE`; 60–69 `AGGRESSIVE`; below 60 `REJECT / NOT BORING ENOUGH`. Ranking bands do not override hard gates.

### Preferred ranges / penalties

- Current overnight move: small positive to <3%; a move >=3% should increasingly reduce Boringness and may trigger review/blocking depending on context.
- 20-day average overnight return: +0.10% to <2.0% preferred.
- 5-day average overnight return: positive to <5.0% preferred; current cheat sheet expects 5-day average to exceed the 20-day average.
- Median overnight return should be positive and reasonably close to the mean; a high average paired with a tiny median suggests outlier-driven results.
- Overnight consistency: >=70% preferred; >=80% excellent.
- Penalize unusually large intraday percentage moves, abnormal volume spikes, sharp extension away from normal structure, fresh news/earnings/catalyst spikes, M&A or regulatory/government decision risk, major corporate actions, sector/regime shocks, and materially deteriorating 5M structure.

## Column requirements

At minimum, expose or make available:

- Decision status: `READY`, `WATCH`, `BLOCKED`, or `DATA?`.
- Overnight Edge Score and/or its qualification state, where data is available.
- Boringness Score and rank band.
- Current overnight percentage move.
- Current-day abnormal extension / unusual-volume warning.
- Hard-gate failure(s) and a compact reason code.
- Data quality/freshness state.
- Exclusion-list status.

If ThinkScript column display limitations prevent multiple fields, split into a small coherent set of columns rather than truncating critical status/reason data. Make the watchlist aggregation and extended-hours assumptions explicit; do not silently mix daily, overnight, and intraday periods.

## Technical responsibility boundaries

### thinkorswim watchlist column

Use for fast candidate triage and calculations supported reliably by ThinkScript: price, moving averages, current price/volume behavior, and limited session-aware signals. State clearly when any value is only a proxy. Do not imply ThinkScript alone can establish that no material news or upcoming event exists.

### thinkorswim chart study

Optional follow-on. Should explain the current-day qualification: normal structure, extension, volume anomaly, and relevant entry/permission checks. It must distinguish overnight logic from the intraday Momentum Command Center state machine and must not conflate 5M authority with 1M timing.

### MomentumCommandCenterV3 C# application

Own or consume the authoritative rolling historical overnight dataset and calculations that require robust history: overnight win rate, median/mean overnight returns, rolling 5D/20D metrics, consistency, volatility, scores, event/catalyst metadata, data freshness, and decision logging. Record candidate snapshot, status, reasons, intended entry, actual entry/exit, and outcome so decisions can be audited and thresholds evaluated.

### Event / catalyst data

ThinkScript alone cannot reliably determine whether a symbol has no material upcoming news. Integrate an external/current event source in the application or treat the status as `DATA?` / manual review required. Do not label a candidate catalyst-clear without evidence.

## Validation plan / acceptance criteria

1. Test historically consistent overnight winners and overnight losers.
2. Include candidates that looked attractive historically but were too extended or had a fresh catalyst on entry day.
3. Include known hard-gate failures: low price, volume below 500K, win rate below 60%, below 20 SMA, excluded Schwab symbol, or incompatible event risk.
4. Verify that missing/stale historical or catalyst data yields `DATA?` or `WATCH`, never `READY`.
5. Compare column results with independently computed C# results on the same dates/symbols; document intentional differences.
6. Track false positives, missed opportunities, overnight win rate, average/median return, adverse excursion, and transaction/spread effects before tuning weights or thresholds.
7. Do not treat a successful compile or a few attractive chart examples as validation of profitability.
8. Do not permit the decision output to place orders automatically until a separately defined and tested order/risk-control workflow exists.

## Next concrete development task

Write the column specification and data contract first: define each field, calculation window, session boundary, data source, missing-data behavior, and reason code. Then implement the subset that ThinkScript can calculate accurately; identify the remaining fields that must come from MCC V3. Do not start by writing a monolithic ThinkScript that pretends to calculate the entire pipeline.

## Related source of truth

See `/Trading System/Boring_Overnight_Cheat_Sheet.md` for the existing Boringness weights, preferred ranges, hard gates, current-day Too Hot penalty, and overall overnight pipeline. Keep this TODO synchronized with that cheat sheet when rules change.

---

## 2026-10-23 implementation contract follow-up

A separate draft specification now exists in the Library as `Boring_Overnight_Column_Data_Contract_v1.md`. Treat it as the data-contract companion to this TODO, not as approval to implement unresolved definitions. It formalizes status precedence, hard gates, session/return definitions, sample counts, provenance/freshness, reason codes, C# vs ThinkScript responsibilities, and validation cases.

Open decisions. For the first prototype, use the recommendations below as provisional defaults and keep them configurable; do not block all progress waiting for preference confirmation:
- Proposed overnight return: prior regular-session close (16:00 America/New_York, using the official exchange calendar for early closes) to next regular-session open (09:30 America/New_York). Keep premarket/after-hours movement separate. Handle weekends/holidays, DST, corporate actions, missing bars, and early closes explicitly.
- [DRAFTED] Provisional configurable point maps for all eight Boringness components are in `Boring_Overnight_Column_Data_Contract_v1.md` under `BORING-0.1-PROVISIONAL`. Implement as versioned rules and validate against labeled historical cases before freezing.
- Provisional volatility metric: standard deviation of daily overnight returns over the same 20-session window. Define the median-to-mean comparison as a scored feature with a configurable tolerance; do not hard-reject on an unvalidated tolerance.
- Choose/verify a catalyst/news source or use timestamped manual review; unknown catalyst status must not silently pass.
- [DONE 2026-10-25] User supplied the current 55-symbol Schwab exclusion list. Canonical source: `docs/project-control/Schwab_Exclusions_SourceOfTruth.md`. A shared `SchwabExclusionList` provider and tests have now been added on `feature/alpaca-readonly-provider-smoke-test`; the new code has not yet been rerun locally. Next: run the full test suite, then wire this provider into candidate qualification/scanners/watchlist and add end-to-end exclusion tests. Treat future additions as append-only unless user explicitly authorizes removal.
- Supply known-good, known-bad, catalyst-spike, overextended, and excluded symbol/date fixtures for validation.

No column/study implementation should claim profitability from compilation or a few chart examples. Keep no-auto-order behavior.


## 2026-10-23 source review and next actions

- The current Library contains `MomentumCommandCenterV3_2_1M_V14_5.txt`, labeled V14.5 (event-armed opportunity, fresh-trigger memory, candidate re-arm, finite execution window). Audit this source before requesting any new chart inputs; the screenshot does not prove the locally installed study matches the Library byte-for-byte.
- Recommended implementation defaults: regular-session close to next regular-session open in `America/New_York`; premarket/after-hours tracked separately; 20-session standard deviation of overnight returns as the provisional volatility metric; all score mappings configurable until historical validation.
- Do not ask the user to repeat screenshot/bubble evidence already provided. Complete exclusion list is now supplied. If representative labelled validation fixtures cannot be recovered from existing sources, ask for a small set only after source review.

## 2026-10-25 — Limit-sell CLI failure and launch-readiness gate

### Why the user's first command failed

The command's CSV path is an input, not a generated output. The relative path `./eligible-window-high-excursions.csv` resolves from the current working directory (in the reported run, the repository root). That file was not present. The calculator therefore had no historical sample and correctly did not calculate a price. Do not populate it with invented values or use median close-to-open returns as a substitute.

Expected input:
- Optional header: `excursion_percent`
- One finite numeric percentage per subsequent row.
- At least 20 completed observations.
- Each observation must be the maximum price excursion above the relevant reference entry/close during the exact session in which the intended sell limit would be eligible.
- Source, feed, session definition, time zone, missing-bar treatment, and observation dates must be documented outside the raw numeric column until the provenance-aware importer is implemented.

### Follow-up code change

A follow-up fix is merged in PR #2: nullable-symbol handling, actionable missing-file diagnostics, and a null-symbol regression test. Its post-merge CI passed.

### Before using this calculator

1. Use the operator-confirmed GTC_EXT sell qualifier and reported active window of 7:00 AM–8:00 PM ET for this strategy; EXTO 24h is not required. Verify individual symbol eligibility only when that symbol's ticket is uncertain.
2. Verify a historical data feed actually covers the GTC_EXT-eligible window. Do not assume Alpaca IEX or BOATS is equivalent to Schwab's full eligible price stream.
3. Generate at least 20 valid, completed historical eligible-window high excursions with dates and provenance.
4. Confirm the entry price is the actual average fill price; set realistic round-trip costs and desired net return.
5. Review the reported empirical high-touch rate and remember that touching a price is not proof of a live limit fill.
6. Keep manual order placement and risk review separate. The calculator never submits orders.

**Current status:** the calculation core is merged and CI passed on the merge commit. Automated session-aware data acquisition and historical sample generation are not implemented. **Data not found in sources** for verified high excursions matching the actual Schwab order window. Until that dependency is resolved, this command is a calculator engine test, not a ready-to-use live limit recommendation.


## 2026-10-25 — User workflow confirmed; broker/session alignment required

### User's intended routine
- Research 1:00–3:00 PM Arizona time.
- Candidate limit-buy window 3:00–5:00 PM Arizona time, Monday–Thursday; after actual fill, user sets a limit sell at regular-session close +0.5%, currently described as GTC + Extended.
- Priority is manually reviewed Boring Overnight decision support; no automated orders.

### Confirmed sell-session definition
The operator has confirmed GTC_EXT as the intended sell qualifier, with the ticket's reported active window 7:00 AM–8:00 PM Eastern. EXTO 24h is not a requirement for this workflow. Do not re-open the EXTO decision unless the strategy changes. Official background references:
- https://www.schwab.com/stocks/extended-hours-trading
- https://international.schwab.com/investment-products/extended-hours-trading
- https://www.schwab.com/learn/story/how-does-extended-hours-trading-work

### Time conversion
Arizona remains on Mountain Standard Time year-round. During Eastern Daylight Time, 3:00–5:00 PM Arizona = 6:00–8:00 PM Eastern. During Eastern Standard Time, it = 5:00–7:00 PM Eastern. Convert with named time zones and the actual date; never hardcode an offset.

### ADR fees and calculator costs
ADR pass-through fees are depositary fees for certain ADR holdings, not specifically overnight trading fees; amounts/timing vary. The calculator's cost input is a user-supplied round-trip execution estimate for Schwab in this workflow, not Alpaca API/data subscription costs and not automatically retrieved from a broker. Include actual transaction charges and spread/slippage without double-counting; verify against Schwab confirmations/pricing guide: https://www.schwab.com/legal/schwab-pricing-guide-for-individual-investors.

### Data needed before valid excursion CSV generation
1. Use the already confirmed LMT EXT buy and LMT GTC_EXT sell qualifiers. Check an individual symbol's eligibility only if that ticket is uncertain.
2. Identify the actual historical feeds/entitlements available. Alpaca IEX is single-exchange, SIP is consolidated coverage subject to plan, and BOATS is an overnight ATS feed. BOATS alone does not cover the entire after-hours + overnight + premarket + regular-session lifecycle, and no feed is assumed to reproduce Schwab executable quotes/fills.
4. Implement session-aware minute bars and, where available, quotes/trades with named time zones, exchange calendar, holiday/early-close/DST handling, timestamps, feed provenance, missing-interval checks, and corporate-action treatment.
5. For each observation, preserve symbol/date, reference entry price, window start/end, provider/feed, full coverage/missing-data flags, max favorable/adverse excursion, spread/quote availability, target-touch time, and whether there is actual fill evidence or only a touch proxy.
6. Generate the CSV only after coverage checks pass. Fail closed for missing intervals, mismatched session, stale feed, invalid data, or fewer than 20 completed observations.
7. Use at least a multi-month, broad-universe out-of-sample validation to evaluate touch rate, executable quote touch, no-fill rate, net return after costs, adverse excursion, catalyst nights, and market regimes. Twenty samples validate only basic code-path behavior, not strategy reliability.
8. Review the actual CSV behind the user's AAPL output. Until its provenance and window are verified, that result is only a calculation output. Data not found in sources for a verified Schwab-window-matched sample.

### Weekend/unfilled-position handling — operator preference recorded
Thursday entries and weekend carry are allowed. The operator does not want a predetermined automatic response. For an unfilled sell, review source-linked Market News plus price/volume/liquidity and the original thesis, then present manual options (leave working, consider a lower limit, prioritize an exit, or gather more evidence). No automatic cancel, reprice, or sell. See `Boring_Overnight_Unfilled_Sell_News_Review_v0_1.md`.

### Release gate
The +0.5% target is the operator's chosen heuristic (motivated by observed median ON % often around 0.56–1.00%), not a fixed strategy rule or a near-guarantee. It must be compared with history-derived targets. The first usable release should be read-only/manual review with order-eligibility, catalyst, data freshness, liquidity, cost, and weekend-risk gates. It must not submit orders.


## 2026-10-26 — Operator corrections and launch additions

- The +0.5% limit is a current heuristic, not a rule. The research goal is to derive candidate targets from historical eligible-window highs, compare those against +0.5%, and evaluate out-of-sample. Keep reference-close study and actual-fill trade records separate.
- Do not request the user's secondary OTC list for current launch work. Keep the canonical 55-symbol repository list append-oriented and focus on wiring the shared provider where relevant; revisit the $6.95 fee/opportunity-cost trade-off later using actual economics.
- When an overnight sell remains unfilled, use source-linked Market News plus price/liquidity context to diagnose the thesis and present manual options. A confirmed adverse deal change can trigger urgent manual review; a halt/pause alone is not proof the thesis is intact. Missing/conflicting news must be explicit. Never auto-cancel, replace, sell, or change the limit.
- Historical-data request: for the first Boring Overnight pilot, an OHLCSV export is useful only if timestamps and coverage include the relevant regular close and following GTC_EXT-eligible sell window. If the export does not contain extended-hours bars for the full interval, mark DATA_GAP. High-touch is not fill probability.


- [2026-10-26 STATUS] Source-linked news-assisted review is now specified in `Boring_Overnight_Unfilled_Sell_News_Review_v0_1.md`. Master already has the read-only Alpaca News API and `alpaca-news-check`; PR #6 improved the output and passed CI. The full position-aware diagnosis remains unimplemented.
- [2026-10-26 STATUS] Existing `BoringOvernightEvaluator` already injects and checks the canonical `SchwabExclusionList`. End-to-end scanner/Morning Report/MarketWatch qualification paths remain future integration work. Secondary OTC list reconciliation is deferred by the operator.
