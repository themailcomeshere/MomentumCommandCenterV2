# Overnight Candidate Review — 2026-11-05

## Scope and source boundary

This review evaluates the five symbols discussed by the operator: AVNT, BLMN, EVER, BLK and CBOE. Scanner metrics are from the committed updated November 5 WatchList Scanner export. News/catalyst checks are current through November 5, 2026.

This is a candidate-prioritization review, not a trade authorization. The scanner file does not contain current price, bid/ask spread, current SIP liquidity, current 5m Permission, current 1m execution evidence, or MCC exclusion-list status. No candidate is marked READY.

## Scanner metrics

| Symbol | Overnight Score | ON Win % | AVG ON % | Median ON % | 5D ON % | ON Momentum | Current ON % |
|---|---:|---:|---:|---:|---:|---:|---:|
| AVNT | 68.02 | 60 | 0.30 | 0.13 | 2.67 | 2.37 | 0.25 |
| BLMN | 66.83 | 70 | 0.83 | 0.67 | 1.52 | 0.69 | 0.46 |
| EVER | 66.76 | 60 | 0.99 | 0.28 | 2.40 | 1.41 | -0.49 |
| BLK | 66.70 | 85 | 0.68 | 0.61 | 0.81 | 0.13 | 0.93 |
| CBOE | 66.01 | 75 | 0.74 | 0.71 | 1.14 | 0.40 | 1.85 |

All five were above their 20 SMA in the scanner export. This binary field does not replace current price/structure validation.

## Candidate assessment

### 1. CBOE — strongest alternative to the current Red Zone four

- 75% historical ON win rate; mean +0.74%, median +0.71%.
- Current ON +1.85%, strongest current ON reading among these five.
- 5D ON +1.14%; momentum +0.40%.
- Company reported Q3 on October 30: net revenue +18% year over year, adjusted EPS +36%, and raised its 2026 organic revenue growth target to high teens.
- On November 5, reported market coverage showed CBOE up 2.28% while the S&P 500 fell 4.36%, indicating relative strength on that session.

**Assessment:** I prefer CBOE over EVER and BLMN for the next ordinary overnight review. It combines a strong median, above-average win rate and strong current ON reading. The November 5 relative-strength observation is supportive but must not replace a fresh chart and spread check.

Sources:
- https://ir.cboe.com/news/news-details/2026/Cboe-Global-Markets-Reports-Results-for-Third-Quarter-2026/default.aspx
- https://www.marketwatch.com/data-news/cboe-global-markets-inc-stock-outperforms-competitors-on-strong-trading-day-273eec38-cc50c72be3a6

### 2. BLK — best historical consistency

- 85% ON win rate, highest of the five.
- Mean +0.68%, median +0.61%.
- Current ON +0.93%, but 5D ON +0.81% and momentum only +0.13%.

**Assessment:** Strongest historical win-rate profile. Less recent acceleration than CBOE, AVNT, EVER or BLMN. Keep it on the shortlist, but do not equate the 85% historical win rate with a current-session probability.

Source:
- https://www.blackrock.com/corporate/newsroom/media/press-releases/blackrock-reports-third-quarter-2026

### 3. AVNT — strongest recent scanner momentum, with post-earnings volatility

- 5D ON +2.67% and momentum +2.37%, highest of these five on both fields.
- Historical ON mean +0.30% and median +0.13%; ON win rate 60%.
- Q3 results were released November 4: sales +10.5%, adjusted EPS +23% and above guidance; full-year adjusted EPS guidance was raised.

**Assessment:** A momentum candidate, but its historical overnight mean/median are weak relative to CBOE, BLK and BLMN. It reported earnings immediately before this scan, so treat the first post-earnings sessions as a volatility/price-discovery period. Require especially clean current 5m structure and spread.

Sources:
- https://www.sec.gov/Archives/edgar/data/1122976/000112297626000145/avnt-20260930xnewsrelease.htm
- https://www.avient.com/investor-center/news/avient-announces-third-quarter-2026-results-ahead-expectations-increases-full-year-adjusted-eps-guidance-range

### 4. EVER — positive recent fundamental catalyst, but weak current ON reading

- Historical ON mean +0.99%, second highest of these five.
- Median only +0.28%, versus mean +0.99%; results may be influenced by a smaller number of outsized nights.
- 5D ON +2.40%, momentum +1.41%.
- Current ON is -0.49%.
- Q3 results were released November 2: revenue +25% year over year, adjusted EBITDA +41%, record results and positive Q4 outlook.

**Assessment:** Keep as a secondary candidate, not a first choice while current ON is negative. The mean/median divergence deserves scrutiny rather than treating the 0.99% mean as typical.

Sources:
- https://investors.everquote.com/financials/quarterly-results/default.aspx
- https://www.globenewswire.com/news-release/2026/11/02/3393721/0/en/everquote-announces-third-quarter-2026-financial-results.html

### 5. BLMN — exclude from a routine overnight hold because of scheduled earnings

- ON win 70%; mean +0.83%; median +0.67%.
- 5D ON +1.52%; current ON +0.46%.
- Q3 earnings are scheduled for November 6 at approximately 6:30 a.m. ET, before the regular session.

**Assessment:** The scanner metrics are respectable, but the earnings release falls inside the overnight holding period from November 5 to November 6. That creates a binary event gap risk. Exclude it from the ordinary overnight shortlist unless the strategy explicitly intends to hold through earnings and has a separately validated event-risk rule.

Sources:
- https://www.businesswire.com/news/home/20261016005321/en/Bloomin-Brands-Inc.-to-Host-Fiscal-2026-Third-Quarter-Earnings-Conference-Call-at-8-00-AM-ET-on-November-6-2026
- https://www.stocktitan.net/news/BLMN/

## Recommended shortlist for the next ordinary overnight review

1. **CBOE** — preferred addition; best combined current ON reading and median consistency among these five.
2. **BLK** — highest historical win rate.
3. **AVNT** — strongest recent momentum, but post-earnings volatility and weaker historical average/median.
4. **EVER** — secondary; positive Q3 catalyst but negative current ON and a large mean/median gap.
5. **BLMN** — do not use for a routine overnight hold into its November 6 premarket earnings release.

This is a research ordering, not an instruction to buy. All candidates remain subject to current price, spread/liquidity, exclusion, catalyst, 5m Permission and 1m execution checks. SIP-based qualification remains blocked by the previously documented Alpaca 403 and is not re-requested here.

## Operator UI location

Dashboard → Workspace dropdown → **Boring Overnight** → scroll to **Calculated Limit Sell**. The **Assess sell-limit evidence gates** button is directly below the **Research target quantile (%)** input. HTML ID: `overnightCalculateSellLimit`.

Do not use **Fetch 20-session GTC_EXT sell-window history** while SIP remains unavailable.

## [Uncertainties]

- Scanner values are from the November 5 export, not a live quote or a live market-data feed.
- Catalyst summaries do not establish the absence of every possible news item; MCC's own catalyst/news gate must still run when available.
- Current 5m/1m chart structure, spreads, current SIP liquidity and exclusion status were not present in the supplied scanner file.
- No candidate is qualified or marked READY by this review.
