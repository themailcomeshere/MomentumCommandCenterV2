# Overnight Scanner Review — 2026-10-28

## Decision summary

**Review order: EVR, RTO, SXC.** This is a chart-review shortlist, not an entry recommendation. All supplied MCC rows say `OBSERVED · NOT QUALIFIED`. The supplied MCC bar snapshots end around 12:55 Arizona on the 5-minute series and 12:59 Arizona on the 1-minute series, whereas this review is later in the session. They do not establish a current live entry.

Interpret the red `EOD: GET OUT` state on the 1-minute dashboard as the user's intended 12:55 Arizona dashboard/exit behavior. It is not, by itself, a reason to disqualify a scanner candidate. Read 5-minute setup/permission separately from 1-minute execution/exit state.

## Top candidates

### 1. EVR — best bullish continuation structure in the supplied charts
- Scanner: Overnight Score 53.69; ON win 65%; Avg ON +0.37%; Median ON +0.46%; 5D ON +0.66%; ON Momentum +0.29%; Current ON +5.83%.
- MCC: +1.95% (5M); 4.98% range (5M), 5.10% range (1M); 77/279 bars; 5M/1M snapshots around 12:55/12:59 Arizona.
- Chart state: 5M Runner Rising, permission YES, bullish regime established, RSI 57.1, extension 1.88 ATR; 1M execution environment YES. Screenshot shows an abrupt upward repricing followed by a sustained consolidation near highs.
- Risk: abrupt price jump and large session range. Verify catalyst/news and ensure the post-move base is holding; don't chase an extended opening/impulse.
- Action: priority watch for a fresh, confirmed continuation or a clean base retest. No entry from stale snapshots.

### 2. RTO — best lower-range/less-extended chart to investigate
- Scanner: Overnight Score 63.98; ON win 70%; Avg ON +0.69%; Median ON +0.30%; 5D ON +1.60%; ON Momentum +0.91%; Current ON -0.17%.
- MCC: -0.26% (5M); 1.29% range on both timeframes; 77/261 bars; snapshots around 12:55/12:59 Arizona.
- Chart state: 5M Runner Rising, permission YES, structure TRANSITIONING, RSI 67.3, extension 1.6 ATR; 1M chart ends in `GET OUT`. Price recovers late from an intraday low, but structure is not established.
- Risk: a late recovery can fail; 1M execution state is not a buy signal.
- Action: watch for a fresh setup where 5M structure becomes established and the 1M execution state confirms. Do not buy solely because the range is low.

### 3. SXC — credible momentum, but high extension/volatility
- Scanner: Overnight Score 60.55; ON win 60%; Avg ON +0.58%; Median ON +0.10%; 5D ON +1.96%; ON Momentum +1.37%; Current ON +0.09%.
- MCC: +1.24% (5M); 6.20% range on both timeframes; 77/293 bars; snapshots around 12:55/12:59 Arizona.
- Chart state: 5M Runner Rising, permission YES, bullish regime established, RVOL 17.76, RSI 73.6, extension 3.87 ATR; 1M execution environment YES. The chart shows recovery from a selloff/base toward the highs and a large final volume bar.
- Risk: 6.20% range, elevated RSI, 3.87 ATR extension and end-of-window volume spike. This is not a “boring overnight” profile and may be vulnerable to a fade.
- Action: retain on the shortlist, but wait for a fresh session/setup and proof that the recovery level holds; avoid chasing the spike.

## Other symbols

| Symbol | Evidence | Decision |
|---|---|---|
| AMT | Scanner 60.03; MCC +0.83% (5M), 4.37%/4.36% ranges. Chart has a deep decline followed by a late rebound; 5M Runner Rising/permission YES, extension 2.03 ATR. | Secondary watch; require a hold/retest, not a chase. |
| BNC | Scanner 77.63 (highest of the listed set); MCC -1.57% (5M), 4.88%/4.86% ranges. Chart is weak/neutral; 5M Prepare to Buy but permission YES is not shown, and 1M ends `GET OUT`. | Reject for now. Scanner rank does not override weak intraday evidence. |
| MANH | Scanner 54.78; MCC -3.99% (5M), 5.43%/5.49% ranges. Chart shows bearish structure, 5M permission NO, extension 2.56 ATR. | Reject. |
| MFM | Scanner 61.15; only 8 bars on both timeframes, latest bars 12:25/12:28 Arizona, extension 4.07 ATR and bearish structure. | Reject: inadequate sample and stale data. |
| OSW | Scanner 53.58; MCC +2.26% (5M), 5.78%/6.00% ranges. 5M Runner Rising/permission YES, strong momentum, extension 2.46 ATR; sharp move and late volume spike. | Secondary momentum watch only; high chase risk. |
| RNG | Scanner 67.60; MCC -0.05% (5M), 4.08%/4.11% ranges. 5M permission NO, bearish structure, extension 5.47 ATR; 1M ends `GET OUT`. | Reject. |
| STNG | Scanner 54.20; MCC -2.62% (5M), 5.47% ranges. 5M Prepare to Buy/permission YES but RSI 71.2, extension 2.42 ATR; chart shows spike/fade and subsequent consolidation; 1M ends `GET OUT`. | Wait for reset and fresh confirmation. |
| TRMD | Scanner 55.78; MCC -1.11% (5M), 2.63%/2.64% ranges. 5M WAIT, permission NO, structure transitioning; 1M ends `GET OUT`. | Wait; no current qualified setup. |

## Scanner observations

The scanner screenshot shows BNC 77.63, RNG 67.60, RTO 63.98, MFM 61.15, SXC 60.55 and AMT 60.03 among the listed names. The scanner's overnight statistics and MCC's intraday setup states answer different questions; do not merge them into a single score without a defined, validated model.

## Explicit next steps for the trader

1. **Do not place an entry from these screenshots alone.** The MCC snapshots are stale relative to the review time and all rows are not qualified.
2. Put EVR, RTO and SXC on the first review tier; AMT and OSW second tier. Treat the others according to the table above.
3. Before any fresh entry, refresh the charts and confirm the 5M setup/permission, then the 1M execution state, current price, volume and stop/risk level. If 5M permission or 1M execution confirmation is absent, stand aside.
4. For SXC specifically, wait for a pullback/base hold or another clean continuation trigger; do not chase the large volume spike or assume the late rebound will persist overnight.
5. Check catalyst/news and overnight event risk before holding any candidate. Catalyst status is **Data not found in sources** for this review.
6. For “Boring Overnight” selection, prioritize consistency/low range and reliable history rather than the highest scanner score. RTO's lower range is notable, but its chart still needs fresh confirmation.

## Evidence and limitations

- Source screenshots: `src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/` (scanner plus 11 ticker charts).
- MCC intraday metrics are supplied in the review request; scanner values are read from the scanner screenshot.
- This review does not have current quote/news/catalyst verification or a full refreshed data feed. Do not treat the historical chart snapshot as a live signal.
- No trade was submitted and no order recommendation is implied.

## [Uncertainties]

- Current price/action after the screenshot timestamps is unknown.
- Catalyst/news clearance and next-session liquidity/event risk were not verified.
- The overnight score formula and thresholds are not independently validated here.
