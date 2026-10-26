# Overnight Candidate Review — 2026-10-26

**Scope:** user-supplied Overnight Scanner column screenshot, CCI and NICE Thinkorswim screenshots, and the user's date correction for the three AHR/FMS/NNN screenshots. This is visual/rule-based triage, not a profitability finding or order instruction.

## Decision summary

- **CCI — WATCH / NOT READY.** Scanner score 68.11 is in the documented AGGRESSIVE band (60–69), ON Win 60% is exactly the hard floor, and the visible mean/median split is concerning: AVG ON 1.24% versus Median ON 0.08%. 5D ON 2.73% exceeds AVG ON 1.24%; current ON 0.03% and Above 20 SMA=1 look acceptable from the shown columns. The chart screenshot shows 5M Permission NO, Structure NEUTRAL, Bullish Regime NOT ESTABLISHED, RVOL 0.04, Extension 1.23 ATR, and EOD ACTIVE. The chart does not support an Overnight READY decision. A public company release confirms CCI reported Q3 2026 results and updated its 2026 outlook on Oct 21 after the close, making this a recent earnings/outlook reaction to review rather than assume clear. See https://crowncastle.gcs-web.com/news-releases/news-release-details/crown-castle-reports-third-quarter-2026-results-and-updates. 20-day average daily share volume, source freshness and unrounded inputs are not shown.
- **NICE — WATCH / NOT READY.** Scanner score 60.47 is also AGGRESSIVE; ON Win 75%, AVG ON 0.70%, Median ON 0.60%, 5D ON 0.70%, Current ON 0.55%, ON Momentum 0.00%, Above 20 SMA=1. The displayed 5D and average ON figures are equal after rounding; the hard rule requires 5D > 20D average, so the unrounded values must be checked before claiming a pass. NICE's chart shows 5M Permission YES but Structure NEUTRAL, RVOL 9.48, Extension 0.45 ATR, Execution Environment NO and Swing Buy Environment NO. Price 122.41 is below the displayed VWAP 122.66, EMA9 122.66, EMA20 122.76 and SMA50 122.55. High current-bar relative volume and the lack of an execution environment mean the chart does not establish a ready entry/overnight qualification. Catalyst status and 20-day average daily volume are not shown.
- **Neither is qualified as a Boring Overnight tonight from these sources.** If forced to keep one for manual follow-up, NICE has the less skewed historical ON mean/median pair, but it has a near-bottom AGGRESSIVE score and an unresolved 5D-vs-average hard gate. That is a research watchlist distinction, not a trade recommendation. Zero position remains valid.

## Scanner values captured

| Symbol | ON Win % | AVG ON % | Median ON % | 5D ON % | ON Momentum | Current ON % | Above 20 SMA | Overnight Score |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| CCI | 60.0 | 1.24 | 0.08 | 2.73 | 1.49 | 0.03 | 1.0 | 68.11 |
| TTE | 75.0 | 0.47 | 0.72 | 0.85 | 0.38 | 0.14 | 1.0 | 63.19 |
| NICE | 75.0 | 0.70 | 0.60 | 0.70 | 0.00 | 0.55 | 1.0 | 60.47 |
| FMS | 75.0 | 0.25 | 0.61 | 0.64 | 0.39 | 0.31 | 1.0 | 59.99 |
| AHR | 75.0 | 0.39 | 0.40 | 0.52 | 0.13 | 0.00 | 1.0 | 56.75 |
| NNN | 75.0 | 0.29 | 0.23 | 0.54 | 0.25 | 0.24 | 1.0 | 55.23 |
| OKE | 65.0 | 0.35 | 0.62 | 0.56 | 0.21 | 0.00 | 1.0 | 54.34 |

The screenshot's AVG ON % column is treated as the project's 20D/longer-window average for this first-pass comparison; verify exact study definition and unrounded values before using it as a hard gate.

## Three historical chart screenshots — user-confirmed 2026-10-21 to 2026-10-23 sample

The user explicitly confirms these three screenshots represent the requested Oct 21–23 sample. Accept that correction and do not request re-upload. The chart-axis/header text is not fully aligned with that user-described sample range; retain this as a metadata caveat rather than overruling the user's clarification.

- **AHR:** sharp upward extension/spike (approximately 54.76 in the visible chart) followed by fading price action; the chart's right edge showed GET OUT / 5M Permission NO / weak or bearish structure in the supplied review. Not a boring, stable overnight profile at that snapshot.
- **FMS:** strong move/flush and rebound, with the displayed 5M Permission NO and EOD GET OUT. The visible 1M extension was approximately 6.45 ATR and RSI around 75. This is materially extended/abnormal for a consistency-first overnight hold, despite ON win 75% and 5D average above the displayed average.
- **NNN:** the strongest chart-state permission of the three at the visible right edge: 5M Permission YES, runner/bullish regime established, execution environment YES, 1M PREPARE and 5M PREPARE TO BUY. However extension was approximately 2.53 ATR, beyond the source study's 2.0 ATR danger threshold. Treat as a chart-review candidate, not automatically READY. Its scanner score 55.23 is below 60 (REJECT / NOT BORING ENOUGH band), so the historical chart state does not rescue the Boringness score.

## Shared pipeline blockers

1. Scores do not override hard gates. Need exact 20D average, 5D > 20D check, daily average volume >=500K, price >=$5, above 20SMA, Schwab exclusion list, fresh data and catalyst review.
2. CCI has a recent Oct 21 earnings/outlook release; treat current price/volume behavior as potentially catalyst-affected and do not assume the move is boring. NICE's IR page lists no upcoming event, but a complete timestamped news/catalyst review is still required; event-calendar absence is not clearance.
3. No complete source-backed prior regular close plus next eligible GTC_EXT 07:00–20:00 Eastern OHLCV set is attached for these names. Therefore no target-touch percentage, target optimality, actual fill, or realized-return claim can be made. Data not found in sources.
4. Chart screenshots are snapshots and do not prove full-window bars, absence of gaps, fills, or expectancy.

## Next steps

- Before any overnight decision: review source-backed earnings/news/corporate actions for CCI/NICE and each finalist; verify exact unrounded 20D/5D values and daily average volume; confirm chart/session timestamp and freshness; re-check 5M/1M immediately before the decision.
- For quantitative research: import source-backed regular close and the complete following GTC_EXT window for each date/symbol; preserve missing intervals as DATA_GAP; count historical touches separately from fills.
- Keep CCI and NICE on WATCH pending the missing hard-gate and catalyst evidence. Do not change thresholds to force a candidate.

## [Uncertainties]

- Exact unrounded scanner values and the exact meaning/window of AVG ON % are not shown.
- CCI's recent Oct 21 earnings/outlook release is verified from company sources; the effect on current overnight volatility still needs review. NICE's official IR page shows no upcoming events, but complete news/catalyst clearance, daily average share volume, and source freshness are not established.
- The user-confirmed Oct 21–23 screenshot sample range conflicts with some visible chart header/axis labels; user correction is accepted, but exact per-bar dates should be verified before quantitative use.
- No full GTC_EXT OHLCV, target touches, or actual fills for these candidates were found in the reviewed sources.

## Public catalyst source check — 2026-10-26

- CCI: Crown Castle reported Q3 2026 results and updated its full-year outlook on October 21, 2026 after the close. This is a recent company event, so the boringness review must explicitly consider post-earnings repricing/volatility; no claim is made about the current reaction's direction or magnitude. Source: https://crowncastle.gcs-web.com/news-releases/news-release-details/crown-castle-reports-third-quarter-2026-results-and-updates.
- NICE: the official investor-relations events page currently lists no upcoming events. This does not establish that the stock is catalyst-free; continue a current-news/company-event review. Source: https://www.nice.com/investors.
