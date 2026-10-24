# MCC V3 — Boring Overnight Decision Column Data Contract
**Status:** Draft specification for implementation and validation; not a profitability claim.  
**Last updated:** 2026-10-23

## 1. Purpose and boundaries

The decision column must answer two separate questions:
1. Does this symbol have a historically attractive, relatively boring overnight profile?
2. Is it qualified *today* to be considered for an overnight hold?

A high historical score must never override a current-day blocker. A large intraday move is not proof of overnight quality. A zero-position state is valid. This component is decision support only and must not submit orders.

## 2. Decision pipeline and precedence

Evaluate in this order and preserve all failed reasons, not just the first one:

1. **Data integrity / freshness** — required fields available, correctly dated, and within source-specific freshness limits. If required data is missing, stale, or internally inconsistent: `DATA?` unless a higher-priority known hard blocker independently proves `BLOCKED`.
2. **Hard eligibility gates** — price, liquidity, historical overnight edge, trend, exclusion list, and event/catalyst clearance.
3. **Historical profile** — Overnight Edge metrics and Boringness Score, each with explicit sample count and window.
4. **Current-day qualification** — overnight move, intraday extension, unusual volume, fresh catalyst, deterioration and 5M structure where available.
5. **MCC 5M permission / structure** — consume an explicitly documented authoritative output; do not reimplement or silently approximate the TOS state machine in this column.
6. **1M entry timing** — optional separate stage; not a requirement for deciding whether a symbol belongs on an overnight watchlist. Entry readiness is not overnight thesis quality.
7. **Final status + reason codes + as-of timestamp.**

Status precedence: `BLOCKED` for a known disqualifying condition; otherwise `DATA?` when required data cannot establish eligibility; otherwise `WATCH` when the historical profile is acceptable but current conditions or event clearance require review; `READY` only when every required gate is affirmatively passed. `READY` is not an order instruction.

## 3. Initial hard gates (provisional; preserve until tested)

| Gate | Initial rule | Data requirements / notes |
|---|---|---|
| Price | Last eligible price >= $5.00 | Timestamped quote/trade; source and session explicit. |
| Liquidity | 20-session average daily volume >= 500,000 shares | Use completed regular sessions; document split/corporate-action adjustment. |
| ON win rate | >= 60% | Define a successful ON return and sample count before calculation. |
| 20D mean ON return | >= +0.10% | Use completed overnight intervals only. |
| 5D vs 20D mean | 5D mean ON return > 20D mean ON return | This is the existing cheat-sheet rule; validate it rather than silently changing it. |
| Trend | Price above 20D SMA | Specify which price/session close and same-day treatment. |
| Exclusions | Not on the maintained Schwab exclusion list | Load a canonical maintained list; existing examples are not exhaustive. |
| Catalyst | No known material event incompatible with the overnight strategy | Requires a separate evidence-backed event source or explicit manual clearance. Unknown is not clear. |

Any gate with unavailable inputs must not default to pass. Corporate actions and symbol changes require careful handling to avoid false returns.

## 4. Overnight interval and calculation definitions

Before production calculations, freeze the session contract and use it consistently in ThinkScript, C#, tests, and reports:

- **Timezone:** store timestamps in UTC; render session labels in America/Phoenix and exchange-local time as needed. Arizona does not observe daylight saving time; exchange-local timestamps do.
- **Overnight interval:** must be explicitly chosen. Candidate definition to validate: previous regular-session close to current regular-session open, using split-adjusted prices. Do not mix premarket highs/lows with close-to-open return unless a metric explicitly says so.
- **ON return:** `(current regular-session open / previous regular-session close - 1) * 100` under the candidate interval above.
- **ON win:** ON return > 0. Confirm whether zero return is a loss/non-win and keep that convention stable.
- **5D / 20D windows:** last 5 / 20 completed eligible overnight intervals; do not include a partial current interval in historical scores.
- **Mean ON return:** arithmetic mean of ON returns over the eligible window.
- **Median ON return:** median over the same eligible observations; robust to a single outlier.
- **Consistency:** percentage of eligible intervals satisfying the adopted positive-return criterion. The current score asks for consistency, but its exact definition needs validation; proposed initial definition is the fraction of ON returns > 0.
- **Overnight volatility:** definition still open. Do not substitute an unlabelled proxy. Candidate: standard deviation of eligible ON returns over the 20-interval window.
- **Sample count:** every historical metric must include N. If fewer than 20 eligible intervals exist, mark the 20D metrics insufficient; do not fabricate missing history or silently shorten the window while labeling it 20D.
- **Current ON move:** define separately from historical ON return; capture its reference price and timestamp. Do not compare values computed from different session boundaries.

## 5. Boringness score — initial weights

Total 100 points; each component must have a documented mapping from raw metric to points. The existing weighting is retained, but the point maps are **not yet specified** and must not be guessed in code.

| Component | Max points |
|---|---:|
| ON win rate | 25 |
| Median ON return | 20 |
| 20D mean ON return | 15 |
| 5D mean ON return | 10 |
| Consistency | 10 |
| Overnight volatility | 10 |
| Average volume | 5 |
| Price | 5 |

Bands: 90–100 `PERFECT BORING`; 80–89 `VERY GOOD`; 70–79 `POSSIBLE`; 60–69 `AGGRESSIVE`; below 60 `REJECT / NOT BORING ENOUGH`. These are descriptive bands, not probabilities. A score cannot override any hard gate. Keep the raw metrics beside the score so the score is auditable.

Existing preferred ranges to preserve pending validation:
- Current ON move < 3%; at or above 3% adds review/penalty.
- 20D mean ON return >= +0.10% and < 2.0%.
- 5D mean ON return positive and < 5.0%.
- Consistency >= 70% preferred; >= 80% excellent.
- Median positive and reasonably close to mean; the quantitative meaning of “reasonably close” is unresolved.

## 6. Current-day qualification

Required checks and evidence:
- Current ON move and gap size, including reference prices and timestamp.
- Intraday move versus defined baseline; do not label “extended” without a specified measure and threshold.
- Relative/unusual volume versus a defined time-of-day baseline, not a full-day average compared to an incomplete session.
- Fresh company or sector catalyst, with source URL/reference, publication time, observed time, category, and status.
- Earnings, M&A, regulatory/government decisions, corporate actions, material company news, and sector/regime shocks.
- 5M structure/permission and extension danger where available; missing state is unknown, not bullish.
- Price/volume deterioration compared with normal behavior.

Catalyst status must distinguish at least `CLEAR_WITH_SOURCE`, `RISK_PRESENT`, `UNKNOWN`, and `STALE`. Without a trustworthy event feed, an operator's explicit manual review may provide a timestamped clearance; otherwise status remains `DATA?` or `WATCH`, never silently `READY`.

## 7. Reason codes (initial vocabulary)

- `DATA_PRICE_MISSING`, `DATA_PRICE_STALE`
- `DATA_VOLUME_MISSING`, `DATA_HISTORY_INSUFFICIENT`, `DATA_SESSION_BOUNDARY_UNKNOWN`
- `DATA_CATALYST_FEED_UNAVAILABLE`, `DATA_CATALYST_REVIEW_REQUIRED`, `DATA_5M_STATE_UNAVAILABLE`
- `GATE_PRICE_BELOW_MIN`, `GATE_AVG_VOLUME_BELOW_MIN`, `GATE_ON_WIN_RATE_BELOW_MIN`
- `GATE_ON_MEAN_BELOW_MIN`, `GATE_5D_NOT_ABOVE_20D`, `GATE_BELOW_20D_SMA`, `GATE_SCHWAB_EXCLUSION`
- `BLOCK_MATERIAL_CATALYST`, `BLOCK_CURRENT_EXTENSION`, `BLOCK_ABNORMAL_VOLUME`, `BLOCK_5M_PERMISSION`, `BLOCK_STRUCTURE_DETERIORATION`
- `WATCH_CURRENT_ON_MOVE_HIGH`, `WATCH_SCORE_BAND`, `WATCH_CATALYST_UNKNOWN`, `WATCH_METRIC_NEAR_THRESHOLD`
- `READY_ALL_GATES_PASSED`

Reason codes are multi-valued; preserve all applicable reasons with a primary reason for compact display. Names may evolve, but changes must be versioned and tested.

## 8. Suggested column output contract

Because a single ThinkScript custom quote has limited display space, compact output should prioritize status + score + primary reason, while detailed metrics live in labels/chart study or MCC UI. Proposed compact shape:

`READY | 86 | ON+0.42% | N=20`

or

`WATCH | 82 | ON MOVE 3.4%`

or

`BLOCKED | PRICE < $5`

or

`DATA? | CATALYST UNKNOWN`

Exact syntax is open. Do not show a score without a status, and do not hide data freshness/sample count in the detailed view. Color is supplementary; text/reason must remain unambiguous.

## 9. Data contract / provenance

Every evaluation should carry:
- symbol and stable security identifier where available;
- evaluation timestamp UTC, session date, session boundary/version;
- source/provider and feed (including Alpaca feed such as IEX), retrieved timestamp, source event/publication timestamp where applicable;
- current price and quote/bar timestamp;
- daily volume history, ON-return observations and each window's sample count;
- metric-definition version and scoring-rule version;
- exclusion-list source/version;
- catalyst state, evidence references, and last review time;
- 5M state/source timestamp and 1M timing state if included;
- final status, all reason codes, score components, total score, and decision-engine version.

No historical values, event facts, scores, or targets may be backfilled from guesses. If a source does not supply a field, record unavailable and the reason.

## 10. ThinkScript vs C# responsibilities

**ThinkScript:** lightweight display and calculations only where chart/session behavior can be verified; show clearly labelled proxies; cannot be the authoritative catalyst-clearance system or full rolling-history store.

**C# V3:** authoritative rolling ON history, mean/median, win rate, consistency, volatility, sample counts, score point maps, exclusion management, catalyst evidence, freshness, reason-coded decision, snapshot history, and comparison tests. Use one pure calculation service so the same input fixture always yields the same output.

The two implementations must be compared on identical symbol/date/session inputs. Differences must be explained, not patched with hidden per-symbol exceptions.

## 11. Validation matrix and acceptance criteria

Test at minimum:
1. Every hard gate fails independently and returns the correct blocker.
2. Missing/stale price, volume, history, catalyst feed, or 5M state never produces false `READY`.
3. ON return calculation across weekends, holidays, daylight-saving transitions, early closes, splits, and missing bars.
4. Exactly 5 and 20 completed intervals; insufficient history; zero returns; outlier return; median/mean divergence.
5. Current ON move just below, exactly at, and above 3%.
6. 5D mean equal to and either side of the 20D mean.
7. Price exactly at $5; volume exactly at 500,000; win rate exactly at 60%; mean exactly at +0.10%; price exactly on 20D SMA.
8. Excluded ticker; ticker removed from exclusion list; symbol change/delisting.
9. Known winner, known loser, catalyst spike, overextended mover, rumor contradicted by later source, and no-news/no-feed case.
10. Score-band boundaries 59/60/69/70/79/80/89/90/100 and hard gate overriding a 100 score.
11. Same fixture produces identical C# result and any claimed ThinkScript result, accounting for documented proxy differences.
12. Audit history records rule version, raw inputs, data sources, freshness, score components, status, and reasons.

Measure false-positive READY rate, missed qualified candidates, ON win rate, mean/median ON return, adverse excursion, spread/slippage, and outcomes by market regime. A passing test suite demonstrates implementation correctness, not profitability.

## 12. Decisions still required before production

1. Confirm exact ON session boundaries and treatment of premarket/after-hours, holidays, early closes, and corporate actions.
2. Define the numeric point maps for all eight Boringness components.
3. Define overnight-volatility and “median reasonably close to mean” thresholds.
4. Confirm how catalyst clearance is supplied (provider, cost/licensing, coverage, or manual review) and how stale news is treated.
5. Confirm whether `READY` means “eligible for overnight watchlist” or “eligible for a separate entry review”; recommended interpretation is eligible for watchlist/review only.
6. Supply/reconcile the complete current Schwab exclusion lists; examples in prior notes are not exhaustive.
7. Provide a representative set of symbols/dates labelled as known good, bad, catalyst-spike, overextended, and excluded for validation.

## [Uncertainties]
- The proposed close-to-open ON interval and positive-return consistency definition are candidates to confirm, not established final rules.
- Component point maps and several thresholds are not present in the source material; no mapping is invented here.
- Data provider entitlements, news-source availability/licensing, and the complete exclusion lists have not been verified.