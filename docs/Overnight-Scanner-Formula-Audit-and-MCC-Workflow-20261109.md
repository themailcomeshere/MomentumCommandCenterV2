# Overnight Scanner — source formula audit and MCC operating procedure

**Reviewed:** 2026-11-09  
**Branch:** `feature/opportunity-quality-classification`  
**Source of truth:** the Thinkorswim column files in [docs/Column Formulas](Column%20Formulas/). The scanner screenshot supplied 2026-11-09 shows the top 50 of 419 results sorted by Overnight Score descending.

## 1. Exact formulas and their meaning

All eight active columns use the ThinkScript daily aggregation series. Define:

`r[t] = open[t] / close[t-1] - 1`

This is the current daily bar's Open relative to the previous daily bar's Close. The code itself does not specify a clock-time entry, 7:55 PM Arizona entry, after-hours low/high, premarket low/high, or a sell-window endpoint. Its real-world session interpretation therefore depends on the Thinkorswim aggregation/session settings and must be confirmed against exported TOS bars. Do not silently equate it with a custom buy-at-time return.

| Scanner order | Column | Exact computation | Interpretation |
|---|---|---|---|
| 1 | ON WIN % | `Average(if r > 0 then 1 else 0, 20) * 100` | Percent of the last 20 daily-bar gaps that were positive. A zero return is not a win. |
| 2 | AVG ON % | `Average(r, 20) * 100` | Arithmetic mean of 20 daily-bar open/previous-close returns. |
| 3 | Median ON % | `Median(r, 20) * 100` | Median of the same 20 returns; less sensitive to extreme gaps than the mean. |
| 4 | 5D ON % | `Average(r, 5) * 100` | Arithmetic mean of the most recent five returns. |
| 5 | ON Momentum | `(Average(r,5) - Average(r,20)) * 100` | Short-window mean minus 20-window mean, expressed in percentage points. Positive means recent gaps are stronger than the 20-session average. |
| 6 | Current ON % | `(open / close[1] - 1) * 100` | Current daily bar's open/previous daily close gap. |
| 7 | Above 20 SMA | `if close > Average(close,20) then 1 else 0` | Binary close-above-simple-20-daily-close-average flag. |
| 8 | Overnight Score | Formula below | Deterministic weighted sum; not a probability. |

### Exact Overnight Score

`ONScore = win20*50 + avg20*1000 + median20*1000 + (avg5-avg20)*1000 + aboveSMA*10`

Where `win20` and all return terms are decimal fractions, not already-percent-scaled display values. The 5D ON % display is not added independently; its contribution is embedded in ON Momentum. Above-20-SMA contributes exactly 10 points. A one-percentage-point change in AVG ON or Median ON contributes 10 score points; a one-percentage-point change in momentum contributes 10 points; a 1 percentage-point increase in win rate contributes 0.5 score points.

The score is a ranking heuristic. It has no calibration to the probability of a profitable trade, does not include spread, liquidity, RVOL, catalyst risk, price, tradability or execution slippage, and should not be interpreted as a return forecast.

## 2. Boring Winner — active research formula (not a production gate)

The supplied Boring Winner formula is not currently used in the scanner. It returns 1 only when all are true:

- 20-session win rate >= 60%
- 20-session average return is between +0.10% and +3.00%, inclusive
- At least 50% of the last 20 returns are in the +0.10% to +3.00% interval
- 20-session sample standard deviation <= 3.00%
- Current daily close is above its 20-day simple average

It is a candidate hard-gate definition, not a validated model. It has not been promoted into MCC or a Thinkorswim V3 study. MCC now calculates this separately from Overnight Score when a source-qualified Thinkorswim daily OHLC import is present. The result is labelled RESEARCH ONLY and does not affect READY. Keep it separate from Overnight Score until thresholds are tested on source-qualified, point-in-time data.

## 3. Screenshot — candidate triage, not trade recommendations

The screenshot shows 50 of 419 rows, sorted by Overnight Score. The top visible names and their displayed metrics:

| Rank | Symbol | Win % | Avg ON % | Median ON % | 5D ON % | Momentum | Current ON % | Above SMA | Score |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | STAK | 65 | 5.44 | 1.90 | 5.69 | 0.25 | 3.99 | 1 | 118.36 |
| 2 | WRBY | 70 | 1.89 | 0.59 | 6.27 | 4.37 | 1.69 | 1 | 113.51 |
| 3 | SEZL | 60 | 1.26 | 0.16 | 6.11 | 4.85 | 2.11 | 1 | 102.70 |
| 4 | IRWD | 60 | 1.24 | 0.70 | 5.37 | 4.14 | 2.51 | 1 | 100.74 |
| 5 | ACHC | 60 | 1.25 | 0.44 | 4.93 | 3.68 | 2.45 | 1 | 98.72 |
| 6 | STEP | 65 | 1.22 | 0.51 | 4.85 | 3.63 | 2.40 | 1 | 96.12 |
| 7 | EWTX | 70 | 1.20 | 0.37 | 4.60 | 3.40 | 1.23 | 1 | 94.72 |
| 8 | OPRT | 65 | 1.26 | 0.27 | 4.88 | 3.62 | 3.23 | 1 | 93.94 |
| 9 | GPK | 65 | 1.04 | 0.60 | 3.54 | 2.50 | 2.41 | 1 | 83.87 |
| 10 | ULCC | 60 | 1.58 | 0.47 | 3.86 | 2.28 | 5.52 | 1 | 83.37 |
| 11 | EVMN | 65 | 1.35 | 0.62 | 3.42 | 2.07 | 2.56 | 1 | 82.87 |
| 12 | ARDT | 60 | 1.15 | 0.42 | 3.61 | 2.46 | 3.06 | 1 | 82.78 |
| 13 | BAYRY | 60 | 1.11 | 1.05 | 2.86 | 1.75 | 2.15 | 1 | 81.63 |
| 14 | VCEL | 75 | 1.00 | 0.19 | 3.22 | 2.22 | 2.53 | 1 | 81.55 |
| 15 | CALY | 75 | 0.98 | 0.58 | 2.79 | 1.81 | 1.01 | 1 | 81.13 |

The score arithmetic is internally consistent with the supplied formula to display rounding. Example STAK: 65% win rate contributes 32.5; 5.44% average contributes 54.4; 1.90% median contributes 19; 0.25 percentage-point momentum contributes 2.5; above-SMA contributes 10; total 118.4, displayed 118.36 due to underlying unrounded values.

### What stands out

- WRBY, SEZL and IRWD have large positive momentum (4.37, 4.85 and 4.14), meaning their recent five-session average gap is materially above their 20-session mean. This is acceleration, not proof the next gap will continue.
- STAK ranks first primarily because its 20-session average (5.44%) and median (1.90%) are much higher than most of the list, while its momentum is only 0.25. Its average/median spread suggests skew/outlier influence; inspect all 20 observations before trusting the mean.
- EWTX and ARDT appear in this high-score set. Both require the catalyst and source review already tracked in MCC; a high score does not neutralize news risk.
- BAYRY ranks 13th with moderate gap statistics. Its MCC Daily API error is a separate data-quality issue; do not treat the TOS scanner row as proof MCC's Daily source is complete.
- ULCC's current ON gap (5.52%) is materially above its recent average. Treat it as an event/extension requiring inspection, not an automatic advantage.
- The screenshot alone does not show bid/ask spread, average dollar volume, relative volume, news, borrow/halts, actual executable prices, or complete history provenance. These are unresolved for every row.

No candidate can be called READY from this screenshot. It is a ranking view, not a completed MCC qualification run.

## 4. Exact MCC operator workflow for each candidate

1. **Import the same scanner snapshot.** In the main MCC Dashboard, use the Overnight Scanner CSV import. Confirm parsed row count and the import timestamp. Do not use PrototypeIndex.
2. **Check source completeness before ranking.** Review PASS / FAIL / DATA? separately. DATA? means required source/history is absent or incomplete; it is not a historical FAIL. The 2026-11-09 import was 495 rows: 185 PASS, 14 FAIL, 296 DATA?; 56 exclusions loaded, 0 matching imported rows.
3. **Confirm exclusion state.** Expand the collapsible EXCLUSIONS · API-BACKED section only when needed. It is MCC's persisted exclusion list, not automatically synchronized with the Thinkorswim personal watchlist. Check both lists.
4. **Choose the cohort.** MCC Top 4 selects the highest-scoring imported unique symbols; My Top 4 is the manual comparison group. Confirm four distinct tickers; duplicate CSV rows must not consume slots.
5. **Fetch live Daily + catalyst.** Click **Fetch Daily + catalyst for MCC Top 4** (or the My Top 4 equivalent). Read the four-symbol evidence table, not just the article cards. A symbol with no returned article remains UNKNOWN, not CLEAR. Daily source/feed/completeness/freshness errors are independent from catalyst results.
6. **Check the Daily gates.** For qualification, require 20/20 expected sessions, fresh data, consolidated SIP source, and confirmed corporate-action adjustment equivalence. IEX is partial-exchange diagnostic data and cannot pass consolidated qualification. A 200 HTTP response alone is not proof of valid bars.
7. **Review catalysts.** Open every returned article, verify publisher, timestamp, facts and source. Use the six-label legend. If article coverage is incomplete, the state remains UNKNOWN/UNCERTAIN. Human review must cite article-specific evidence; do not copy assistant suggestions as rationale.
8. **Run the Overnight qualification panel.** Inspect history, exclusion, price, 20D average volume, catalyst/coverage and final decision for each candidate. Treat DATA? as blocked from qualification; do not replace missing values with zero.
9. **Inspect the chart and execution context.** Open each finalist in the main chart. Confirm actual quote/spread, liquidity, relevant extended-hours prints, current catalyst context and the operator's intended Arizona-local entry/sell process. 5M Permission is authoritative for the intraday strategy; 1M is execution timing only. Neither is currently a broker order signal.
10. **Keep the result in its proper class.** Scanner score = historical ranking heuristic. Historical PASS = data/threshold checks passed. READY requires all currently specified gates and is still fail-closed. No automatic order submission exists.

## 5. Required development before MCC can reproduce the TOS scanner exactly

- Implement a versioned Daily-gap history model that ingests exact TOS daily-bar OHLC observations, preserves source/session settings and calculates all eight formulas without silently substituting provider bars.
- Prove whether the TOS daily `open` includes regular-session only or extended-hours aggregation under the user's actual scanner settings. The ThinkScript formulas do not encode this setting. Compare date-aligned TOS exports against the selected MCC source before asserting parity.
- Add independent per-column parity tests, score decomposition, missing-history behavior and rounding/display tests. Do not compute a partial 20-sample statistic as if it were complete.
- Implement the separate V3 Boring Overnight column/study only after its intended buy/sell endpoints, time zone, extended-hours inclusion and official auction/first eligible trade semantics are frozen.
- Add liquidity/spread/volume/catalyst gates as separate evidence fields; do not silently alter the existing Overnight Score formula.
- Keep Boring Winner out of production qualification until separately approved, source-qualified and validated on chronological out-of-sample data.

## [Uncertainties]

- Thinkorswim's exact daily aggregation session boundaries and extended-hours treatment used by this scanner are not established by the text formulas alone. The formulas are exact; the session interpretation needs chart/scanner settings and matched exports.
- The screenshot is a static top-50 view. It does not establish the full 419-row data quality, current market quotes, catalyst coverage, or execution conditions.
- Data not found in sources for a complete per-symbol 20-session raw return series, bid/ask spread, RVOL, dollar-volume evidence, and independently verified outcomes.


## 6. MCC implementation added after operator verification

### Daily audit top-20 workflow
- After a scanner CSV is imported or restored, MCC now fills **Daily data audit → Symbols** with the top 20 unique symbols sorted by imported Overnight Score.
- The **Use Overnight Top 20** button can refill the field on demand.
- The input supports up to 20 symbols; validation runs at most four Daily requests concurrently to avoid launching 20 simultaneous provider calls.
- Filling the field does not automatically issue requests. Click **Validate Daily data** when ready. The existing qualification restrictions still apply: IEX is partial diagnostic data, not consolidated SIP qualification.

### Overnight chart behavior
- MCC persists the selected workspace in browser local storage and restores it before startup chart requests.
- In Boring Overnight mode, the Main Chart requests historical 5-minute bars over a rolling five-day window and redraws on symbol changes and the normal 15-second visible-page refresh.
- The chart does not request the intraday `live-assessment` endpoint in Overnight mode. Intraday 5M permission and 1M execution are explicitly labelled **NOT USED** and their metric cards are hidden.
- The Overnight chart displays the most recently returned historical bars. It does not claim that an old after-hours observation is a current quote or executable price.
- This separates an Overnight research chart from the intraday freshness/permission state. It does not convert IEX into complete extended-hours coverage.

### TOS daily OHLC import and calculation
- API: `POST /api/overnight-scanner/thinkorswim-daily-bars`; history: `GET /api/overnight-scanner/thinkorswim-daily-bars?symbol=WRBY&limit=50`.
- Storage is append-only JSONL under LocalApplicationData/MomentumCommandCenterV2/tos-daily-gap-reference.jsonl (override with `MCC_TOS_DAILY_GAP_PATH`). Each import retains source, aggregation, extended-hours flag, corporate-action adjustment flag, chart timezone, raw bars, calculation version, fingerprint and calculation.
- Dashboard import requires one symbol and CSV headers `Date, Open, High, Low, Close`. The symbol and the actual TOS session/adjustment settings are entered explicitly. At least 21 valid daily OHLC bars are required for 20 daily-gap returns.
- Calculator version: `MCC-TOS-DAILY-GAP-1.0`. It calculates ON Win %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA and Overnight Score directly from the imported TOS bars. It emits no rolling metrics for fewer than 20 gap observations.
- Dashboard compares each calculated field to the matching imported scanner CSV field at displayed precision and reports MATCH/MISMATCH/NO SCANNER VALUE.
- This is **reference arithmetic parity**, not yet proof that the TOS aggregation settings were selected correctly or that another provider has equivalent extended-hours bars. The operator must select the actual settings; defaults are deliberately unchecked.

### Exclusions toggle affordance
The disclosure summary now uses a pointer cursor, hover background, and keyboard-visible focus outline. Click the heading area or disclosure marker to expand/collapse.

## 7. Still blocked — do not infer the missing trade semantics

The supplied active scanner formulas are fully specified and the independent calculator is implemented. They are daily-bar gap formulas, not an intraday entry/sell-window strategy. A separate V3 Boring Overnight trade column still requires the operator's exact Arizona-local buy timestamp and sell/measurement endpoint, the bar/session convention, and the official-auction versus first-eligible-trade rule. The new daily-gap calculator intentionally does not guess those values.

The inactive Boring Winner remains inactive pending approval and chronological source-qualified validation.


## November 10 — Boring Winner daily OHLC export clarification

The Boring Winner calculator consumes one row per completed Daily bar, with Date/Open/High/Low/Close. The ordinary ATR V2 1M/5M StrategyReport is not its input. A StrategyReport is acceptable only if it exports those exact daily OHLC rows and no order-event rows. Use Daily aggregation, Extended Hours OFF, corporate-action adjustment ON, chart timezone America/Phoenix, and at least 21 completed daily bars (40–60 preferred). This reproduces the regular-session open versus previous regular-session close gap. Session semantics still require empirical TOS parity; Boring Winner remains RESEARCH ONLY.
