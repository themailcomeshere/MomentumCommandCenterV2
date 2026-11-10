# Boring Overnight Scanner Analysis — November 10, 2026 · 1:44 PM export

## Source and scope

- Source: data/ScannerExports/2026-11-10-WatchListScanner_1344.csv
- 50 symbols.
- Available fields: ON WIN %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, Overnight Score.
- This is a research snapshot, not a live quote, liquidity report, catalyst audit or validated execution signal.

## Top 3 boring-profile research candidates

This prioritization avoids the largest raw Overnight Scores. It favors a meaningful historical win rate, positive typical (median) return, average reasonably close to median, moderate recent momentum and a modest current overnight gap.

| Rank | Symbol | ON Win | AVG ON | Median ON | 5D ON | Momentum | Current ON | Score |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | **CRBG** | 80% | 0.56% | 0.66% | 1.04% | 0.48 | 0.08% | 66.95 |
| 2 | **FG** | 75% | 0.83% | 0.94% | 2.13% | 1.30 | 0.00% | 78.22 |
| 3 | **IBN** | 70% | 0.65% | 0.76% | 1.91% | 1.26 | 0.73% | 71.63 |

### 1. CRBG — strongest consistency profile

- Highest win rate of this trio at 80%.
- Average (0.56%) and median (0.66%) are close.
- 5D ON is 1.04%, momentum 0.48, and current ON is only +0.08%.
- Best first candidate for evidence review based on the supplied metrics.

### 2. FG — strongest combination of hit rate and typical return

- 75% win rate, 0.94% median and 0.83% average.
- Current ON is 0.00%; recent 5D average is 2.13% with momentum 1.30.
- Recent performance is stronger than the longer-run average, but not as extreme as the obvious outliers.

### 3. IBN — balanced third candidate

- 70% win rate, 0.76% median and 0.65% average.
- 5D ON 1.91%, momentum 1.26, current ON +0.73%.
- Current gap is positive but moderate relative to the hot names in this export.

## Close alternatives

- **HDB:** 65% win, 0.87% median, 0.61% average, 2.31% 5D, 1.69 momentum, +0.81% current. Strong alternative to IBN; slightly lower win rate and higher recent acceleration.
- **LYSDY:** 60% win, 1.99% median, 1.31% average, 2.12% 5D, 0.81 momentum, +1.29% current. Highest median among these candidates, but lower win rate and ADR/liquidity considerations keep it outside the top three.
- **VIRT:** 60% win, 0.75% median, 0.68% average, 2.02% 5D, 1.33 momentum, -0.11% current. Consistent aggregates, but lower win rate than the top three.

## Outliers deliberately not prioritized

| Symbol | Current ON | 5D ON | Momentum | Why it is not a boring-first candidate |
|---|---:|---:|---:|---|
| SMX | +92.41% | 23.19% | 16.96 | Extreme current gap and momentum; event/corporate-action/halts/liquidity review required |
| CSQR | +11.98% | 3.01% | 2.39 | Extreme current gap |
| AMRC | +5.79% | 2.88% | 2.12 | Large current gap |
| TTWO | +5.23% | 1.93% | 1.24 | Large current gap despite moderate momentum |
| EWTX | -3.94% | 3.49% | 2.45 | Large negative current gap |
| JKS | -3.24% | 2.30% | 1.23 | Large negative current gap |
| CENX | -3.06% | 2.55% | 2.27 | Large negative current gap |

SMX, CSQR, AMRC and TTWO should be event/outlier investigations, not promoted because of raw score.

## MCC diagnosis workflow

1. Switch to **BORING OVERNIGHT**.
2. Import this CSV at the top of **Overnight Candidate Workspace**.
3. Review **Boring Overnight Shortlist**. Automatic display uses the existing imported historical checks, excludes extreme absolute current ON gaps (absolute value >= 3%), and sorts by median ON, win rate, average/median consistency, then lower absolute momentum. This is a display heuristic, not a new qualification gate.
4. In **Candidate qualification**, the automatic group is now **Boring Top 4**. Click **Fetch Daily + catalyst for Boring Top 4**. This is the same Candidate qualification action previously labeled "Fetch Daily + catalyst for MCC Top 4"; its automatic cohort is now boring-profile rather than highest raw score. **My Top 4** remains the manual override.
5. Run **Daily Data Audit** for the populated Boring Top 20. Check missing sessions, feed, freshness, adjustment, observed close/SMA20/volume and qualification status. Missing Sessions NONE is green; a complete session list alone does not make IEX consolidated SIP.
6. Open CRBG, FG and IBN in Main Chart and review timestamps/source. Inspect the full catalyst review queue and exclusions.
7. Do not treat any candidate as qualified or READY based on this CSV or shortlist alone.

## Important data limitations

The export does not include raw per-session returns, current bid/ask, spread, dollar volume, RVOL, halts, catalyst/news coverage, current executable quote or actual fills. Data not found in sources. IEX is partial coverage and cannot satisfy the SIP qualification requirement. MCC submits no order.

## [Uncertainties]

- The top-three ordering is a transparent human research prioritization, not a statistically validated predictive model.
- CRBG, FG and IBN still require Daily Data Audit, catalyst review, exclusion checks, price/volume checks, chart review and independent quote/spread/liquidity verification.
- Aggregate metrics do not reveal the underlying distribution or exact observation count behind each statistic.
