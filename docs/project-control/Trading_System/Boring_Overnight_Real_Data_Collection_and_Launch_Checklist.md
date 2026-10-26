# Boring Overnight — Real-Data Collection and Launch Checklist

Status: Operator runbook; no fabricated values permitted.
Last updated: 2026-10-26 (Arizona time)

## 1. Known facts and immediate consequence

The user checked BMY and reports the Schwab ticket says GTC + Extended: active from 7:00 AM to 8:00 PM Eastern Time, up to 180 calendar days, limit orders only; orders placed after 8:00 PM ET, on weekends, or holidays activate the next trading day.

The operator has confirmed GTC_EXT is the intended sell qualifier and does not require EXTO 24h for this strategy. Use the reported GTC_EXT active window of 7:00 AM–8:00 PM ET for historical-window analysis; do not count highs from the 8:00 PM–7:00 AM ET gap as eligible-window observations.

Arizona stays on Mountain Standard Time year-round. Convert using named time zones:
- During Eastern Daylight Time: 3:00–5:00 PM Arizona = 6:00–8:00 PM ET.
- During Eastern Standard Time: 3:00–5:00 PM Arizona = 5:00–7:00 PM ET.

The user reports $0.00 commissions for intended eligible stocks and a $6.95 fee for OTCs; OTC symbols are excluded on two maintained lists. Do not apply the OTC fee to non-OTC symbols. Do not invent other charges.

## 2. Discard the invented sample

The previous eligible-window-high-excursions.csv values were fabricated. Do not use, cite, or run the calculator against them. Remove the invented rows from any operational input. A successful CLI run only proves the program executed; it does not validate its input.

The calculator expects an optional header named excursion_percent followed by one numeric percentage per observation, with at least 20 valid completed observations. Keep a detailed source ledger separately and generate the one-column calculator input from verified rows only.

## 3. Define one observation consistently

Maintain separate analyses. **Current heuristic benchmark:** baseline is the relevant regular-session close; the operator's chosen target is close × 1.005, motivated by observed median ON % often around 0.56–1.00%. This is not a fixed strategy rule. Measure touch/no-touch during the following GTC_EXT-eligible window. **Historical-reach research:** use eligible-window high excursions to estimate alternative target levels at a chosen empirical touch rate; select levels on one sample and evaluate on separate dates/symbols. **Actual-trade journal:** use real buy/sell fills to calculate realized outcomes. Never pool these questions or label a historical high-touch rate as a fill probability.

For each symbol/date, record:
1. Symbol and security type; confirm not OTC and not on either exclusion list.
2. Trade date and prior regular-session close, source and timestamp.
3. Baseline type, price, timestamp and time zone.
4. Actual order type/session: GTC_EXTENDED_7AM_8PM_ET or EXTO_24H; record eligibility evidence separately.
5. Session start/end in America/New_York and equivalent Arizona time, including daylight-saving handling.
6. Session high/low and timestamps, provider/feed/venue, bar interval and retrieval date.
7. Maximum high excursion percentage: ((session_high / baseline_price) - 1) * 100.
8. Maximum adverse excursion percentage: ((session_low / baseline_price) - 1) * 100.
9. Whether the high is a bar high, reported trade or executable quote; include spread/volume evidence if available.
10. Corporate actions, earnings/news, halt, unusual spread, missing bars and exclusions/notes.
11. Provenance: source file/screen, provider/feed entitlement and exact timestamps.

A session high touching the limit does not prove the sell order would fill. The calculator's historical touch rate is not a fill probability.

## 4. Step-by-step collection

### Step A — Verify order session
1. Use the operator-confirmed GTC_EXT sell qualifier and reported 7:00 AM–8:00 PM ET active window for the study.
2. If a particular symbol's actual order eligibility is uncertain, verify that symbol's ticket; do not infer eligibility from BMY or another ticker.
3. Keep the buy order's EXT qualifier separate from the sell order's GTC_EXT qualifier.
4. Do not require EXTO 24h for this workflow. Do not count 8:00 PM–7:00 AM ET highs as GTC_EXT-window evidence.

### Step B — Verify the data source
Use only historical data that demonstrably covers the chosen window. Regular-session-only data is insufficient. Do not assume IEX or an overnight ATS feed represents all Schwab-eligible trading. Verify extended-hours coverage, timestamp semantics, entitlement and missing intervals.

If thinkorswim chart/export shows the full historical interval, enable extended-hours display where available, set aggregation appropriate to the window, inspect date/time axes and export underlying data only if the platform supports it. If the required historical interval is not available, stop and select a verified data source. Do not estimate or invent missing values.

### Step C — Collect a small hand-verified pilot
1. Start with five completed symbol/date rows to prove source coverage, session mapping, close/target math, and timestamp handling. Include ordinary, down/flat, gap, and event cases where available; do not select only winners.
2. The pilot evaluates the current +0.5% heuristic as a benchmark, not a rule: target = relevant regular-session close × 1.005, rounded using the applicable price increment. Also retain the raw high-excursion values needed to test alternative history-derived targets.
3. Mark incomplete rows `DATA_GAP` / `INVALID_INCOMPLETE`; do not count them as misses or successes.
4. Check duplicates, splits/corporate actions, stale bars, missing intervals, and out-of-session highs.
5. Hand-calculate at least three values (or all rows if fewer than three are complete) and reconcile to the generated results.
6. Retain misses and down/flat cases. Once source/session semantics are proven, expand to a larger multi-week or multi-month sample. The existing percentile calculator's 20-observation minimum is not a requirement for the fixed-target pilot and does not mean 20 personal trades are required.

### Step D — Generate calculator input

Create eligible-window-high-excursions.csv with a header named excursion_percent and one numeric value per row. Example format only (not market data):

    excursion_percent
    0.42
    -0.18
    1.05

Replace every example number with calculated values from the verified ledger. One row equals one valid completed observation. Do not paste untraceable values. Keep the ledger and the generated file together.

### Step E — Sanity-check output
1. For the five-row pilot, verify the current +0.5% heuristic target calculation and record whether the eligible-window high reached it. Do not require 20 rows for this pilot.
2. For the separate percentile calculator, confirm at least 20 valid numeric observations.
3. Independently sort the percentile-calculator observations and calculate the empirical percentile. Current 70% desired touch rate maps to the 30th-percentile high-excursion threshold; confirm against code/tests before interpreting.
4. Independently verify the proposed limit, return percentage and tick rounding.
5. Hand-check at least one row from baseline and observed high.
6. If output suggests a very large return relative to the observed ON distribution, stop and verify units, baseline, session, sample mix and percentile.
7. Treat high-touch rate as a proxy, not a fill probability. Better fill evidence requires quote/trade sequence, spread, liquidity and order timing.
8. Run the CLI only against the verified file. The sample syntax below uses an illustrative entry price, not a current price or recommendation:

    dotnet run --project src/MomentumCommandCenterV2.Cli -- overnight-limit-sell AAPL 100 ./eligible-window-high-excursions.csv 70 0.10 0.00

Set the entry argument to the actual fill/reference price under evaluation. A 0.00 cost input is only a provisional assumption, not a statement that all-in execution costs are zero.

## 5. Costs

User-reported commission is $0.00 for intended eligible stocks; OTCs are excluded due to a reported $6.95 fee. The initial cost model may set direct commission to zero for eligible non-OTC stocks as a user-specified assumption. Track spread and slippage separately. Do not apply the OTC fee to eligible non-OTC observations, and add no other fee without support from account activity or current pricing evidence.

## 6. Remaining operator decisions / confirmations

1. **Manual decision remains operator-controlled:** no fixed automatic response when a sell remains unfilled. Review Market News, price/volume, liquidity, and thesis status; present a diagnosis and manual choices. No auto-cancel, reprice, or sell.
2. **Historical sample scope:** use a five-row OHLCSV pilot to validate source/session/calculation first, then broaden. No need to reconstruct unrecorded personal trades.
3. **OTC secondary list:** deferred per operator. Do not spend launch time reconciling it. Keep the canonical repository list append-oriented; later evaluate the $6.95 fee against actual opportunity economics.

Confirmed workflow: LMT EXT buy between 3:00–5:00 PM Arizona, Monday–Thursday; LMT GTC_EXT sell currently targeted at relevant regular-session close +0.5% as a heuristic; no new Friday entries; Thursday/weekend carry is allowed. Manual news-assisted review only; no automated order submission.

## 7. MCC launch checklist

### User actions
- [ ] Provide aligned 1M and 5M OHLCSV for a representative date/symbol where READY is rare after 09:00 Arizona, for the separate signal-frequency investigation.
- [ ] Provide a Boring Overnight OHLCSV sample with regular close and the following GTC_EXT-eligible window, if the platform export covers it. If not, say what it does cover; do not estimate missing intervals.
- [ ] Redact account and personal information from any ticket/screenshot.

### Engineering tasks
- [ ] Build repeatable historical-data import/generation with feed/session metadata and fail-closed coverage checks.
- [ ] Test holidays, early closes, DST changes, missing intervals, duplicate bars, invalid values and session mismatch.
- [ ] Preserve source ledger/provenance with generated input.
- [x] Implement a configurable reference-close target-touch evaluator (PR #5 merged and tests passed); it reports touch/no-touch and DATA_GAP for incomplete coverage. First-touch time still requires interval-level observations. Do not repurpose the percentile calculator for this question.
- [ ] Verify target rounding against the actual order price rules and hand calculations.
- [ ] Keep the percentile-based historical-reach calculator as a separate research tool only if its baseline/session definition matches the intended question.
- [ ] Wire the shared Schwab exclusion provider through all relevant qualification paths; secondary OTC-list reconciliation is deferred.
- [x] Operator specified desired approach: news-assisted diagnosis and manual decision for unfilled sells; no fixed automatic action and no automated order changes. Implementation remains outstanding.
- [ ] Validate broad labeled sample including losers, catalysts, low liquidity, gaps and no-fill cases.
- [ ] Missing/unverified catalyst coverage must produce DATA? or manual review, never confident READY.
- [ ] Keep this read-only/manual. No automated order submission.

## 8. Release status

Not ready for unattended or fully trusted Boring Overnight use. A successful build/run or high historical-touch percentage does not establish live eligibility, actual fills, profitability or safe weekend handling. Near-term use is read-only/manual decision support.

## [Uncertainties]
- Historical source/feed coverage for the actual GTC_EXT-eligible session is not established.
- The manual action remains case-specific and will be informed by current news/market context; the user does not want a fixed automatic response.
- A historical high-touch result is not evidence of an actual limit fill.
## 9. Clarification from the operator — actual Boring Overnight workflow (2026-10-26)

The earlier guidance over-focused on EXTO/24-hour execution and made data collection sound as if the operator needed a large personal trade history. That is not the intended strategy. Use this clarified workflow unless the operator changes it:

1. Candidate passes current MCC checks / the “Use the Overnight” watchlist criteria.
2. Operator submits a limit buy with EXT between 3:00 PM and 5:00 PM Arizona time, Monday through Thursday. Thursday entries are allowed; no new Friday entries.
3. After the buy, operator sets a limit sell with GTC_EXT at the relevant regular-session end-of-day close + 0.5%. The sell limit is based on the end-of-day close and can differ from the actual buy fill.
4. Sell may fill before 5:00 PM Arizona time, during the next morning's premarket session, or in the first minutes of regular trading. Operator reports premarket / early-open exits are common.
5. The intended sell order is GTC_EXT, with the user-reported Schwab session definition of 7:00 AM–8:00 PM ET. EXTO 24h is not a requirement for this clarified workflow. The inactive 8:00 PM–7:00 AM ET gap is not by itself a contradiction if the strategy does not require the sell to execute during that gap.
6. Weekend carry is allowed, including after Thursday entries, but requires an explicit manual risk plan.

The relevant UI reference is the user's existing “Use the Overnight” watchlist columns, where BMY's Overnight Score was reported as 33.49. Do not confuse that analytical watchlist/score with definitive Schwab order eligibility. The reported ticket qualifiers are LMT EXT buy and LMT GTC_EXT sell. Do not keep asking the user to prove EXTO eligibility unless the strategy changes to require 24-hour execution.

## 10. What the baseline choices mean for this strategy

The current workflow sets the sell limit from the end-of-day close, not necessarily from the buy fill. The +0.5% level is the operator's chosen heuristic, motivated by observed median ON % often around 0.56–1.00%; it is not a fixed strategy rule. There are three distinct research questions:

- **Reference-close benchmark study:** For each symbol/date, evaluate the current +0.5% heuristic as one candidate: relevant regular-session close × 1.005. Check whether the observed high reaches it during the next sell-eligible window covered by GTC_EXT. This can be researched without a personal trade if the historical data and coverage are verified. It is a target-touch study, not proof that an order would fill.
- **Actual-fill study:** For trades actually made, record actual buy fill price/time, sell limit price/time, sell fill price/time if sold, and any still-open/unfilled status. Calculate realized return from actual buy fill to actual sell fill. The operator's recent sample is too small to validate performance on its own.
- **Historical-reach alternative:** Use completed eligible-window high excursions to estimate alternative target levels at a selected historical touch rate. Select levels on a development sample and test them on separate dates/symbols. Maintain separate ReferenceCloseStudy, HistoricalReachResearch, and ActualTrades datasets. Never pool them or treat a high-touch rate as a fill probability.

For the reference-close benchmark, minimum fields are symbol, date, source-backed regular-session close, candidate target (close × 1.005, rounded according to the applicable valid-price rules), following sell-window start/end, observed high and timestamp, whether target was touched, source/feed, data coverage/missing intervals, catalyst/market notes, and provenance. Preserve the full high-excursion distribution for alternative-target research.

## 11. Simplified data collection path — no invented spreadsheet and no requirement for many personal trades

Do not ask the operator to produce 20 historical personal trades. Use two tracks:

**Track A — historical reference-close pilot (primary blocker):**
1. Select a small pilot set of candidate symbols and completed dates. Include all qualifying cases in the selected sample, not only winners.
2. Obtain actual historical daily close plus historical extended-hours/premarket data from a source whose coverage is verified for the required window.
3. For each symbol/date, evaluate the current +0.5% heuristic and retain the observed eligible-window high excursion; later use separate development/test samples to evaluate history-derived target alternatives.
4. Record source/feed and exact session coverage. If the source cannot cover the GTC_EXT-eligible window, mark the row DATA_GAP and do not count it.
5. Start with a small hand-verified pilot (for example, five symbol/date rows) to prove the workflow and calculations. Expand only after the source and session mapping are proven. The existing calculator's 20-observation minimum applies when using that calculator; it does not mean 20 personal trades are required.

**Track B — actual trade journal (secondary validation):**
For every future trade, preserve symbol/date, MCC score and decision output at entry, buy order and actual fill, reference EOD close used for target, target limit, sell order qualifier, sell fill/time or open status, and any manual intervention. This is prospective data collection. Do not reconstruct unknown values from memory as if verified. Add historical trades only where records/screenshots provide evidence.

**Important:** the current repository has two separate tools: the percentile calculator proposes a target from historical high excursions, while the configurable target-touch evaluator measures whether a chosen reference-close target was reached. Neither establishes actual fill probability. Do not feed target-touch binary results or arbitrary values into the percentile calculator.

## 12. User-confirmed policy and remaining engineering gates

- Buy order: LMT EXT, 3:00–5:00 PM Arizona time, Monday–Thursday; no new Friday entries.
- Sell order: LMT GTC_EXT; the current chosen heuristic is relevant regular-session EOD close +0.5%, not necessarily actual buy fill +0.5%. The target is open to historical optimization, not a fixed rule.
- Sell may fill same evening, next morning premarket, or shortly after regular open.
- GTC + Extended is acceptable; user-reported active window is 7:00 AM–8:00 PM ET. EXTO 24h is not a current requirement.
- Weekend carry is allowed, including after Thursday entries, with an explicit manual risk plan.
- Current checks include the “Use the Overnight” watchlist columns; BMY Overnight Score reported as 33.49. This score does not prove order eligibility or profitability.
- Do not use the small recent personal-trade sample as the sole validation dataset.

Engineering work: source-backed historical price/session ingestion; compare the +0.5% heuristic with historical-reach candidate targets; preserve provenance and gaps; exchange calendar/time-zone validation; enforce the canonical OTC exclusion list across candidate paths (secondary list reconciliation deferred); record actual fills prospectively; implement source-linked news-assisted manual review for unfilled positions; keep decision support read-only with no automated orders.


## 9. News-assisted unfilled-sell review (manual decision support)

When a sell remains unfilled, collect current source-linked news and price/liquidity context before suggesting an action. Each review must show: (1) verified news facts with publication/observation times and links; (2) interpretation and confidence; (3) effect on the original thesis; (4) current price, spread/volume and halt status when available; (5) actual entry fill/cost and current sell limit if known; (6) manual choices and risks. A verified adverse deal/catalyst can raise `URGENT_MANUAL_REVIEW`. A halt/pause alone does not mean the thesis is intact. Missing or conflicting coverage must be `NEWS_UNKNOWN`/`CONFLICTING`, never “no news.” Any lower sell limit must be assessed against actual entry economics; the goal can be the smallest profitable exit, but profit cannot be assumed from the close-based target. Never automatically cancel, replace, sell, or change an order.

## October 26 data-readiness update

- The latest uploaded October 2 1M file was mislabeled in its filename: its actual symbol is HPK, and its 214 rows duplicate the October 2 HPK subset in the existing multi-day report. It does not provide the missing XRPN 1M partner for the October 2 XRPN 5M report.
- The October 19 XRPN parity worksheets contain 20 chart-entered rows per timeframe; the rest are unfilled. Missing minute timestamps remain unresolved, not presumed no-trade.
- No target-reach calculation is justified until the source-backed regular-session close and complete next GTC_EXT-eligible window are available. Keep historical touch, actual fill, and realized P/L as separate datasets.
- See the detailed feature-branch audit: https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md
