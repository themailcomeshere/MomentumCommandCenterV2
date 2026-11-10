# Overnight Watchlist Scanner — November 10, 2026 (11:38 AM AZ)

**Source file:** `2026-11-10-WatchListScanner_1138.csv`  
**Rows:** 50  
**Sort:** Overnight Score descending  
**Scope:** Analysis of the supplied snapshot only. This is not a live quote, qualification result, or trade recommendation.

## Executive read

The top 10 are all marked Above 20 SMA. Their historical ON WIN % ranges from 60–65%; their mean AVG ON % is 1.86%, but their mean Median ON % is only 0.47%. That mean/median gap is a warning that positive historical averages may be skewed by occasional large gaps.

SMX is a major outlier: 92.41% Current ON %, 23.19% 5D ON %, and 16.96 percentage points ON Momentum. Its 6.23% AVG ON % contrasts with a 0.66% median. It dominates the score (278.51) but should be treated as an exceptional/event-driven observation, not as the cleanest repeatable candidate.

Among the remaining nine, WRBY, SEZL, ETON, IRWD and STEP lead the score ranking. WRBY has the strongest balanced profile among the non-SMX names: 65% win rate, 1.81% average, 0.54% median, 6.04% 5D mean and +4.23 momentum. SEZL has stronger recent acceleration but a much lower 0.16% median. ETON and OPRT have negative current gaps; IRWD and STEP are exactly 0.00% current gaps in this snapshot.

## Top 10

| Rank | Symbol | ON Win % | Avg ON % | Median ON % | 5D ON % | Momentum | Current ON % | Above 20 SMA | Score |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | SMX | 60 | 6.23 | 0.66 | 23.19 | 16.96 | 92.41 | Yes | 278.51 |
| 2 | WRBY | 65 | 1.81 | 0.54 | 6.04 | 4.23 | -1.19 | Yes | 108.22 |
| 3 | SEZL | 60 | 1.28 | 0.16 | 6.21 | 4.93 | 0.68 | Yes | 103.63 |
| 4 | ETON | 60 | 1.57 | 0.20 | 5.70 | 4.13 | -1.12 | Yes | 99.00 |
| 5 | IRWD | 60 | 1.26 | 0.70 | 5.08 | 3.82 | 0.00 | Yes | 97.85 |
| 6 | STEP | 65 | 1.24 | 0.51 | 4.73 | 3.49 | 0.00 | Yes | 94.91 |
| 7 | ACHC | 65 | 1.23 | 0.37 | 4.70 | 3.47 | 0.00 | Yes | 93.20 |
| 8 | OPRT | 65 | 1.21 | 0.27 | 4.61 | 3.40 | -0.95 | Yes | 91.24 |
| 9 | ULCC | 60 | 1.62 | 0.78 | 4.19 | 2.57 | 0.96 | Yes | 89.66 |
| 10 | ARDT | 65 | 1.16 | 0.51 | 3.97 | 2.81 | 0.61 | Yes | 87.28 |

## Candidate-by-candidate interpretation

### 1. SMX — exceptional outlier; investigate first, do not trust the rank at face value
- The 92.41% current gap and 23.19% five-day average are far outside the rest of this file.
- Its 6.23% 20-session average is almost ten times its 0.66% median. That is a strong skew/outlier warning: a few very large gaps may be driving the mean and score.
- 60% win rate is not exceptional relative to this list.
- MCC diagnosis priority: verify split/corporate-action/news context, actual tradable price, spread, liquidity, halts and the full 20-return distribution. Do not infer repeatability from its score.

### 2. WRBY — strongest balanced non-outlier profile in the top 10
- Best non-SMX 20-session average (1.81%) and robust 65% win rate.
- 5D average 6.04% and +4.23 momentum show strong recent acceleration.
- Current gap is -1.19%, so the latest observation is not confirming the recent positive trend.
- MCC diagnosis priority: check whether the negative current gap is a normal pullback or a catalyst/price-dislocation event; inspect daily history and catalyst evidence.

### 3. SEZL — strong acceleration, weak median
- +4.93 momentum and 6.21% 5D average indicate pronounced recent strengthening.
- 60% win rate and only 0.16% median are weak consistency signals compared with the higher average.
- Current gap is +0.68%.
- MCC diagnosis priority: inspect the individual 20 gaps and determine whether recent strength is concentrated in a few observations.

### 4. ETON — high average and acceleration, but negative current gap and low median
- 1.57% average, 5.70% 5D mean and +4.13 momentum.
- Median is only 0.20%; current gap is -1.12%.
- MCC diagnosis priority: investigate the current negative gap and any issuer-specific catalyst; require history/catalyst coverage rather than relying on its score.

### 5. IRWD — best median in this top 10 after ULCC, but current gap flat
- 0.70% median is the second-highest in this top 10; 1.26% average and 60% win rate.
- Recent 5D mean 5.08%, momentum +3.82.
- Current gap exactly 0.00%.
- MCC diagnosis priority: compare the current flat gap with its daily history and catalyst state; verify that the apparently strong 5D period is not driven by one or two large gaps.

### 6. STEP — relatively balanced, positive recent acceleration
- 65% win rate, 1.24% average and 0.51% median.
- 5D mean 4.73%, momentum +3.49.
- Current gap exactly 0.00%.
- MCC diagnosis priority: a useful comparison/control candidate against WRBY and IRWD; validate daily completeness, catalyst coverage, liquidity and current spread.

### 7. ACHC — similar to STEP, slightly lower median
- 65% win rate and 1.23% average; median 0.37%.
- 5D mean 4.70%, momentum +3.47.
- Current gap exactly 0.00%.
- MCC diagnosis priority: inspect catalyst/news and compare 20-return dispersion with STEP. The scanner fields alone do not establish a current setup.

### 8. OPRT — strong recent acceleration but negative current gap and low median
- 65% win rate, 1.21% average and 0.27% median.
- 5D mean 4.61%, momentum +3.40.
- Current gap -0.95%.
- MCC diagnosis priority: resolve why the latest gap is negative despite recent strength; check catalyst and liquidity before considering it further.

### 9. ULCC — strongest median in the top 10; lower recent acceleration
- 0.78% median is the highest in the top 10; 1.62% average and 60% win rate.
- 5D mean 4.19%, momentum +2.57, lower than the other top-ranked non-SMX names.
- Current gap +0.96%.
- MCC diagnosis priority: strongest candidate in this top 10 for further investigation of typical-gap consistency, but airline/event and liquidity context still matter. The CSV does not contain those data.

### 10. ARDT — steady-looking scanner metrics, but not enough evidence to qualify
- 65% win rate, 1.16% average and 0.51% median.
- 5D mean 3.97%, momentum +2.81.
- Current gap +0.61%.
- MCC diagnosis priority: compare its history/catalyst/liquidity evidence against STEP and ACHC; its lower 20-session average is offset by a 65% win rate and positive current gap.

## What the file does and does not establish

- All 50 rows have Above 20 SMA = 1. This field does not differentiate the candidates in this snapshot.
- Top-10 mean win rate = 62.5%; mean AVG ON = 1.86%; mean Median ON = 0.47%; mean 5D ON = 6.84%; mean momentum = 4.98 percentage points. These averages are heavily affected by SMX.
- Excluding SMX, the remaining top-nine mean AVG ON is approximately 1.37%; mean Median ON approximately 0.45%; mean 5D ON approximately 5.02%; mean momentum approximately 3.65 points.
- Overnight Score is a deterministic ranking heuristic, not a probability or expected return. The formula audit explains its components and limitations.
- This CSV does not include per-session raw returns, source/feed provenance, bid/ask spread, dollar volume, RVOL, news/catalyst, halt status, executable prices, or actual GTC_EXT fills. **Data not found in sources.**
- A static scanner row is not an MCC qualification result. No symbol should be labelled READY based on this file.

## MCC diagnosis workflow for this snapshot

Use **BORING OVERNIGHT** mode for the Overnight research workflow. Do not use Intraday 5M Permission / 1M Execution cards as Overnight qualification gates; those are NOT USED in this mode.

1. **Update the Dashboard with this exact scanner snapshot.** Import `2026-11-10-WatchListScanner_1138.csv` through the Overnight Scanner CSV import. Confirm 50 rows were parsed and that the import timestamp is the intended snapshot. The CSV has 50 rows, not the 419-row population described in the prior November 9 screenshot.
2. **Review the imported qualification table.** Check HISTORY, PRICE, 20D AVG VOL, exclusions and DATA? statuses independently. A populated scanner metric does not prove MCC has complete underlying Daily/market data.
3. **Populate the research cohort.** Use **Use Overnight Top 20** in the Daily data audit. It fills the top 20 unique imported symbols by Overnight Score; it does not launch requests by itself.
4. **Run Daily validation deliberately.** Click **Validate Daily data**. Review per-symbol source/feed, expected versus returned sessions, freshness, adjustment provenance and missing dates. Current Alpaca configuration is IEX. The sell-limit response explicitly confirms IEX is partial and cannot pass the SIP_REQUIRED historical GTC_EXT gate. Do not treat IEX results as consolidated qualification or switch feeds silently.
5. **Fetch catalyst evidence for the shortlist.** In the Dashboard use **Fetch Daily + catalyst for MCC Top 4** or the manual **My Top 4** path. For a first diagnostic cohort from this file, compare WRBY, SEZL, IRWD and ULCC; include SMX separately as an outlier investigation rather than allowing its score to dominate the cohort. This is a research cohort suggestion, not a qualification decision.
6. **Read each candidate's full evidence row.** Inspect Daily source/completeness/freshness, catalyst coverage/status, exclusions, price and 20D average volume. Missing articles or incomplete source coverage remain UNKNOWN/DATA?, not CLEAR/PASS.
7. **Open Main Chart for each finalist.** In BORING OVERNIGHT mode MCC uses historical 5-minute bars. Inspect the actual chart and current source/timestamp context. Confirm live executable quote/spread independently; an old IEX bar is not a current quote.
8. **Run Calculated Limit Sell only as an evidence diagnostic.** The current result on RKLX is correctly BLOCKED because IEX is not SIP and required TOS ATR/parity/walk-forward evidence is absent. The numeric sell limit is withheld. The 50th-percentile default is a research quantile, not a relaxation of the gates. A BLOCKED response is expected until the required source evidence exists.
9. **Keep the final state honest.** Historical scanner rank is not qualification; Boring Winner is RESEARCH ONLY; no order submission is implemented. Do not place an order based solely on this report.

## Suggested first-pass MCC cohort

- **Balanced baseline:** WRBY, IRWD, STEP, ULCC.
- **Momentum/acceleration comparison:** SEZL, ETON, ACHC, OPRT.
- **Outlier investigation:** SMX, isolated from the ordinary cohort.
- **Additional comparison:** ARDT.

This grouping is only a practical diagnostic sequence. It does not establish which symbol is tradable.

## [Uncertainties]

- The CSV does not identify its data-source timestamp per symbol or provide the raw 20 daily-gap observations, so mean-versus-median skew is a clue, not a reconstructable outlier analysis.
- Current prices, spread, volume, RVOL, catalysts, news, halts and GTC_EXT execution eligibility are not in this file.
- Thinkorswim Daily aggregation/session settings still require matched export verification before treating its gap fields as equivalent to a particular market-session definition.
