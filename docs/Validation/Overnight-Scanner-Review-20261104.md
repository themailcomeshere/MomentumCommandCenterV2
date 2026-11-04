# Overnight Scanner Review — 2026-11-04

**Source file:** `data/ScannerExports/2026-11-04-OvernightWatchListScanner_1244.csv`  
**Source format:** Thinkorswim Watchlist Scanner CSV  
**Scope:** Scanner history + current overnight move + public company releases/news. This is not a current technical chart review or a READY decision.

## 1. Import and data-quality audit

| Measure | Result |
|---|---:|
| CSV data rows / unique symbols | 443 |
| Rows with all eight numeric scanner fields | 200 |
| Rows with unavailable custom-expression fields | 243 |
| Valid rows passing the five existing historical gates | 168 |
| Valid rows failing at least one gate | 32 |

The 243 unavailable rows contain Thinkorswim's literal `custom expression subscription limit exceeded` response. They are **UNKNOWN**, not FAIL. Do not fill them with zero, reuse another ticker's values, or treat the file as a complete 443-row ranking.

Among the 200 complete rows, all pass ON win rate >=60%, AVG ON >=0.10%, 5D ON > AVG ON, and Above 20 SMA = 1. The 32 complete rows that do not pass the existing historical filter fail only the Current ON <3% condition.

The top 25 historically qualifying symbols do not match any symbol in the feature branch's checked-in Schwab exclusion seed. This is not a substitute for checking the running API's exclusion endpoint and actual account tradability.

## 2. Highest Overnight Score among historical-gate passes

All percentages are from the CSV. "Score" is the scanner's supplied Overnight Score, not MCC's calibrated Opportunity Quality score.

| Rank | Symbol | Score | ON win | Avg ON | Median ON | 5D ON | Current ON | Read |
|---:|---|---:|---:|---:|---:|---:|---:|---|
| 1 | ADT | 109.72 | 60% | 2.03% | 0.20% | 6.77% | 0.20% | Large mean/median gap; corporate acquisition dominates risk |
| 2 | GPK | 87.21 | 60% | 0.90% | 0.52% | 4.20% | 2.39% | Strong recent scanner momentum; Q3 margins/earnings need digestion |
| 3 | HLF | 83.63 | 80% | 0.82% | 0.42% | 2.94% | 2.16% | Best win-rate of top four; earnings and leadership change just occurred |
| 4 | EVER | 73.70 | 60% | 1.01% | 0.28% | 3.09% | 0.00% | Current move not extended; strong Q3 growth but post-earnings volatility |
| 5 | EHC | 69.80 | 60% | 0.82% | 0.29% | 2.69% | 2.08% | Positive guidance revision; useful post-earnings alternative |
| 6 | DANOY | 69.78 | 65% | 0.74% | 0.81% | 1.92% | 2.92% | Median exceeds mean; current move is close to the 3% cutoff; ADR |
| 7 | CRBG | 65.10 | 75% | 0.42% | 0.51% | 1.25% | 1.51% | Consistent-looking historical distribution; earnings just reported |
| 8 | AHRT | 64.95 | 70% | 0.54% | 0.57% | 1.42% | 1.52% | Q3 and balance-sheet transformation news just reported |
| 9 | FTDR | 64.11 | 65% | 0.45% | 0.53% | 1.63% | 2.73% | Q3 earnings scheduled before the November 5 open; avoid holding through it |
| 10 | HTHT | 64.08 | 65% | 0.40% | 0.44% | 1.71% | 1.80% | Q3 results scheduled November 16; ADR and future-event risk |

## 3. News and catalyst assessment

### ADT — acquisition is the dominant event

ADT announced an all-cash acquisition by Thrive Holdings at $10.00 per share, a stated 45.6% premium to the unaffected October 27 close of $6.87. The deal is expected to close in Q1 2027, subject to shareholder and antitrust approvals; a 30-day go-shop period runs to November 28. ADT reported Q3 results on November 3, with revenue +4% and GAAP EPS +18% year over year, but withdrew prior guidance in light of the pending transaction.

**Overnight interpretation:** The scanner's 2.03% mean vs 0.20% median and 6.77% 5D mean are not a reason to overlook a fixed-price merger event. The $10 cash consideration can cap upside while regulatory, shareholder and superior-proposal risk can create discontinuous downside. **Do not treat ADT as a boring overnight candidate.** Keep it on event/arbitrage watch only.

Sources: [ADT Q3 2026 results](https://newsroom.adt.com/financial/adt-reports-third-quarter-2026-financial-results), [ADT acquisition terms](https://newsroom.adt.com/financial/adt-to-be-acquired-by-thrive-holdings-in-15-3-billion-transaction).

### GPK — recent earnings show margin and leverage pressure

GPK reported Q3 sales of $2.192B, approximately flat year over year, with volume/mix +1%. Net income was $22M versus $142M a year earlier; adjusted EBITDA was $289M versus $383M, and adjusted EBITDA margin fell to 13.2% from 17.5%. The company cited $83M of higher commodity/operating cost inflation and $15M of unfavorable price impact. Net leverage was 4.8x. FY 2026 guidance was reaffirmed.

**Overnight interpretation:** Scanner momentum is strong, but this is a fresh earnings repricing with a sharp adjusted EBITDA decline and elevated leverage. Treat as post-earnings watch, not a clean overnight hold. Require the live 5M structure and 1M timing to agree; do not infer strength from the CSV score.

Sources: [GPK Q3 2026 results](https://investors.graphicpkg.com/news-events/press-releases/detail/348/graphic-packaging-holding-company-reports-third-quarter-2026-financial-results), [GPK Q3 earnings-call takeaways](https://www.fool.com/earnings/call-transcripts/2026/11/03/graphic-packaging-gpk-q3-2026-earnings-call-transcript/).

### HLF — good historical win rate, but event just passed

HLF reported Q3 net sales +6.0% year over year, adjusted EBITDA margin +70 basis points, and a 2.0x net leverage ratio. The release also identifies an interim CEO and CFO effective November 1, following a planned leadership transition.

**Overnight interpretation:** The 80% historical win rate is attractive, but only 20 observations are implied by the scanner's 5-point increments; it is not a robust sample by itself. Earnings and leadership transition are fresh catalysts. Wait for the post-earnings range/volume and chart structure to settle; current ON is already +2.16%.

Sources: [HLF Q3 results (SEC exhibit)](https://www.sec.gov/Archives/edgar/data/1180262/000121390026114951/ea031037101ex99-1.htm), [HLF investor release](https://ir.herbalife.com/news-events/press-releases/detail/960/herbalife-achieves-third-quarter-net-sales-growth-of-6-0).

### EVER — strongest post-earnings growth profile among the top four

EVER reported Q3 revenue +25% to $218.1M, adjusted EBITDA +41% to $35.5M, operating cash flow $40.5M, and $229.1M cash with no debt. GAAP net income was $18.2M versus $18.9M a year earlier. The company exceeded Q3 guidance, gave a strong Q4 outlook, and had separately authorized a $50M share repurchase.

**Overnight interpretation:** Of the top four, EVER has the most constructive operating-results narrative and a current ON move of 0.00%, so the CSV does not show it already extended overnight. However, the release was November 2; a post-earnings reaction can continue or reverse. It is a **watch candidate for live chart review**, not a buy signal.

Sources: [EVER Q3 results (SEC exhibit)](https://www.sec.gov/Archives/edgar/data/1640428/000119312526443633/ever-ex99_1.htm), [EVER investor quarterly results](https://investors.everquote.com/financials/quarterly-results/default.aspx).

### EHC — strongest positive post-earnings alternative

EHC reported Q3 revenue +8.9%, adjusted EPS +23.6%, and operating cash flow +33.7%, then raised full-year revenue, EBITDA and adjusted EPS guidance. Results were released October 28.

**Overnight interpretation:** The release is less immediate than the November 2–3 events affecting the top four. It has a 60% historical win rate, 0.82% mean, 0.29% median and 2.08% current ON move. Worth chart review after the earnings reaction, but the mean/median gap indicates some positive outcomes may be skewing the mean.

Source: [EHC Q3 2026 results](https://investor.encompasshealth.com/news/news-details/2026/Encompass-Health-reports-results-for-third-quarter-2026/default.aspx).

### DANOY — good median, but nearly at the current-move limit

Danone's Q3 like-for-like sales rose 4.0%, with volume/mix +0.4% and price +3.6%; 2026 guidance was maintained at 3–5% like-for-like growth. The company noted weak Mizone water performance in China and continuing North American dairy execution work.

**Overnight interpretation:** Median ON (0.81%) exceeds average ON (0.74%), which is a favorable consistency feature. But current ON is 2.92%, only 0.08 percentage points below the hard 3% cap. It is also an ADR, so confirm the actual U.S. listing's price/volume and overnight session liquidity rather than assuming the Paris primary listing's liquidity carries over.

Sources: [Danone Q3 release](https://www.danone.com/investors/publications-and-events/press-releases.html), [Reuters Q3 summary](https://www.reuters.com/business/retail-consumer/higher-prices-lift-danone-q3-sales-mizone-china-weighs-2026-10-28/).

### CRBG — strong reported quarter, but immediate post-earnings risk

CRBG reported Q3 net income of $1.4B, adjusted after-tax operating income of $545M, and operating EPS growth of 27%. Premiums/deposits were $8.8B; it returned $295M to shareholders. The results were released November 2.

**Overnight interpretation:** Historical median (0.51%) slightly exceeds mean (0.42%), and win rate is 75%. This is one of the more consistent-looking scanner profiles, but it is still immediately post-earnings. Wait for the market's post-release repricing and check 5M/1M evidence.

Source: [CRBG Q3 2026 results](https://investors.corebridgefinancial.com/news/news-details/2026/Corebridge-Financial-Announces-Third-Quarter-2026-Results/default.aspx).

### AHRT — recent results and transformation

AH Realty Trust (NYSE: AHRT; not AHR) reported Q3 adjusted FFO of $0.16 per diluted share and raised FY 2026 adjusted FFO guidance 11.5% to $0.57–$0.59. It reported office same-store cash NOI growth of 5.2% and retail same-store cash NOI growth of 6.8%. It also announced asset dispositions, debt reduction, refinancing and a simplified capital structure, with no material debt maturities until 2029.

**Overnight interpretation:** The results and transformation update are very recent (November 2), so event repricing remains relevant. The 70% historical win rate and median 0.57% vs mean 0.54% are relatively balanced. It merits post-event chart review, but verify its liquidity and current technical structure.

Sources: [AHRT Q3 results](https://ir.ahrealtytrust.com/news-releases/news-release-details/ah-realty-trust-reports-third-quarter-2026-results), [AHRT financing and disposition update](https://ir.ahrealtytrust.com/news-releases/news-release-details/ah-realty-trust-completes-dispositions-and-financing).

### FTDR — earnings before the next regular open

Frontdoor scheduled its Q3 release and conference call for November 5 at 7:30 a.m. Central / 8:30 a.m. Eastern.

**Overnight interpretation:** Do not carry an overnight position through the scheduled earnings release. This is a known event risk immediately ahead of the next regular session.

Source: [Frontdoor Q3 release announcement](https://investors.frontdoorhome.com/news-releases/news-release-details/frontdoor-inc-announce-third-quarter-2026-results).

### HTHT — next earnings November 16

H World Group filed that Q3 2026 results are scheduled for November 16, after Hong Kong trading and before the U.S. market opens.

**Overnight interpretation:** The event is not tomorrow, but it is a known future catalyst. Current ON is 1.80%; the ADR's actual U.S. listing liquidity and current technical state remain unverified.

Source: [H World Group Form 6-K / earnings schedule](https://www.stocktitan.net/sec-filings/HTHT/6-k-h-world-group-ltd-current-report-foreign-issuer-122a3707f2df.html).

## 4. Broader market backdrop — November 4

The post-midterm session was strongly risk-on in the cited report: the S&P 500 rose 1.8%, the Dow gained 2.9%, and Nasdaq rose 1.9%; Brent crude fell 8.6% to $94.70 and the 10-year Treasury yield was 4.89%. The same report described sector dispersion: healthcare and discount retail rallied, while energy and defense names fell. A separate market recap of the prior session reported the Nasdaq and S&P up modestly, Dow lower, technology/industrials/communications stronger and materials weaker.

**Implication:** Broad index strength does not remove single-name event risk. GPK is particularly exposed to the materials/industrial cost and margin narrative; healthcare-linked EHC/HLF/CRBG/AHRT may see a supportive sector tape, but their own earnings and company-specific events dominate. Treat the post-election macro move as context, not as a substitute for ticker-specific permission.

Sources: [AP market report, November 4](https://apnews.com/article/4d12764c0b3ce38ffde3077712811182), [Zacks market recap, November 4](https://www.zacks.com/stock/news/3022827/stock-market-news-for-nov-4-2026).

## 5. Practical shortlist and decision

**Priority for manual chart review (not trade approval):**
1. **EVER** — strongest operating-growth narrative among the top four; current ON 0.00%; still post-earnings.
2. **EHC** — positive Q3 and raised guidance; earnings reaction is a week old; check whether current 5M structure confirms.
3. **CRBG** — historical median > mean and 75% win rate; just reported earnings, so wait for reaction.
4. **AHRT** — historical median > mean and recent raised FFO guidance/deleveraging; recent transformation/earnings event means manual review.
5. **HTHT** — less immediate earnings catalyst; ADR liquidity and November 16 event still require checks.

**Do not prioritize as boring holds without special event handling:**
- ADT: pending acquisition/go-shop/regulatory conditions.
- GPK: fresh earnings, sharply lower adjusted EBITDA and 4.8x net leverage.
- HLF: just-reported earnings and leadership change.
- FTDR: earnings before the November 5 regular open.
- DANOY: current ON 2.92%, immediately below the 3% gate, and ADR-specific liquidity needs verification.

No symbol is READY. The CSV does not contain last price, validated 20-session average daily volume, catalyst clearance, exclusions runtime confirmation, 5M Permission, 1M execution, or a completed GTC_EXT sell outcome. The current API's effective Alpaca feed is IEX, so Daily fields are diagnostic and cannot satisfy SIP qualification.

## 6. How to reproduce this in MCC

Follow [Overnight Scanner → Boring Overnight: Operator Workflow](../Overnight-Scanner-to-Boring-Overnight-Workflow.md). In brief: import this CSV; verify row count and parse diagnostics; use Automatic mode for the scanner-score top-four preview or Manual mode for ordered pins; read the current 5M/1M and Opportunity Quality diagnostic separately; run 20-session capture per serious candidate; review Daily/catalyst evidence; and leave any missing source or event review as DATA?/manual review. Begin the actionable review in the final 60 minutes before the actual regular-session close, using the exchange calendar and Arizona display time.

## [Uncertainties]

- The CSV's file name includes `1244`, but the export's timezone is not encoded in the file. Treat the metrics as the captured scan snapshot, not as an exact exchange-time quote.
- The 243 Thinkorswim custom-expression failures prevent a complete universe ranking. The 200 valid rows are a partial subset.
- The CSV's 20-session historical sample is short; the score is not a probability of profit. Mean/median gaps suggest skew in some names.
- No validated Daily SIP price/volume, current 5M/1M chart state, broker eligibility, or GTC_EXT complete SIP data was available for these symbols.
- Public-news searches are targeted, not exhaustive. "No result found" is not proof that no catalyst exists.
