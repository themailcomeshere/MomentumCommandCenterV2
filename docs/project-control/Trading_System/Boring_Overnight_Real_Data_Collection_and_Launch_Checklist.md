# Boring Overnight — Real-Data Collection and Launch Checklist

Status: Operator runbook; no fabricated values permitted.
Last updated: 2026-10-26 (Arizona time)

## 1. Known facts and immediate consequence

The user checked BMY and reports the Schwab ticket says GTC + Extended: active from 7:00 AM to 8:00 PM Eastern Time, up to 180 calendar days, limit orders only; orders placed after 8:00 PM ET, on weekends, or holidays activate the next trading day.

This is not proof of 24-hour overnight execution. The order is inactive during the 8:00 PM–7:00 AM ET gap. If the strategy requires sell execution in that gap, inspect thinkorswim Public → 24 Hour Trading and the sell ticket for EXTO 24h eligibility for that symbol and account. Do not assume BMY eligibility applies to another ticker. If EXTO is unavailable, constrain the strategy and historical windows to the sessions the actual order covers.

Arizona stays on Mountain Standard Time year-round. Convert using named time zones:
- During Eastern Daylight Time: 3:00–5:00 PM Arizona = 6:00–8:00 PM ET.
- During Eastern Standard Time: 3:00–5:00 PM Arizona = 5:00–7:00 PM ET.

The user reports $0.00 commissions for intended eligible stocks and a $6.95 fee for OTCs; OTC symbols are excluded on two maintained lists. Do not apply the OTC fee to non-OTC symbols. Do not invent other charges.

## 2. Discard the invented sample

The previous eligible-window-high-excursions.csv values were fabricated. Do not use, cite, or run the calculator against them. Remove the invented rows from any operational input. A successful CLI run only proves the program executed; it does not validate its input.

The calculator expects an optional header named excursion_percent followed by one numeric percentage per observation, with at least 20 valid completed observations. Keep a detailed source ledger separately and generate the one-column calculator input from verified rows only.

## 3. Define one observation consistently

Preferred for evaluating the actual strategy: actual filled buy price and maximum price observable during the exact session(s) in which the intended sell order is eligible. If actual fills are unavailable, label the baseline REFERENCE_CLOSE, not ACTUAL_FILL. Never blend these baselines without retaining their type.

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
1. In thinkorswim, open Public → 24 Hour Trading.
2. Search BMY and one other intended non-OTC candidate; record overnight eligibility for each.
3. Open the buy ticket and record exact order type, time-in-force and session qualifier.
4. Open the sell ticket and separately record its exact order type, time-in-force and session qualifier. Do not infer sell eligibility from buy eligibility.
5. Distinguish GTC + Extended / EXT 13h from EXTO 24h. Save redacted screenshots; do not submit a test order to discover eligibility.
6. If the sell is GTC + Extended only, use 7:00 AM–8:00 PM ET as its active window. Do not count 8:00 PM–7:00 AM ET highs as executable-window evidence.

### Step B — Verify the data source
Use only historical data that demonstrably covers the chosen window. Regular-session-only data is insufficient. Do not assume IEX or an overnight ATS feed represents all Schwab-eligible trading. Verify extended-hours coverage, timestamp semantics, entitlement and missing intervals.

If thinkorswim chart/export shows the full historical interval, enable extended-hours display where available, set aggregation appropriate to the window, inspect date/time axes and export underlying data only if the platform supports it. If the required historical interval is not available, stop and select a verified data source. Do not estimate or invent missing values.

### Step C — Collect a small hand-verified pilot
1. Start with five completed symbol/date rows to prove source coverage, session mapping, close/target math, and timestamp handling. Include ordinary, down/flat, gap, and event cases where available; do not select only winners.
2. The first pilot evaluates the strategy's fixed target: target = relevant regular-session close × 1.005, rounded using the actual valid-price rules. Record whether the target was touched in the following GTC_EXT-eligible window, and the first touch timestamp if the source supports it.
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
1. Confirm at least 20 valid numeric observations.
2. Independently sort observations and calculate the empirical percentile. Current 70% desired touch rate maps to the 30th-percentile high-excursion threshold; confirm against code/tests before interpreting.
3. Independently verify the proposed limit, return percentage and tick rounding.
4. Hand-check at least one row from baseline and observed high.
5. If output suggests a very large return while the strategy target is +0.5%, stop and verify units, baseline, session, sample mix and percentile.
6. Treat high-touch rate as a proxy, not a fill probability. Better fill evidence requires quote/trade sequence, spread, liquidity and order timing.
7. Run the CLI only against the verified file. The sample syntax below uses an illustrative entry price, not a current price or recommendation:

    dotnet run --project src/MomentumCommandCenterV2.Cli -- overnight-limit-sell AAPL 100 ./eligible-window-high-excursions.csv 70 0.10 0.00

Set the entry argument to the actual fill/reference price under evaluation. A 0.00 cost input is only a provisional assumption, not a statement that all-in execution costs are zero.

## 5. Costs

User-reported commission is $0.00 for intended eligible stocks; OTCs are excluded due to a reported $6.95 fee. The initial cost model may set direct commission to zero for eligible non-OTC stocks as a user-specified assumption. Track spread and slippage separately. Do not apply the OTC fee to eligible non-OTC observations, and add no other fee without support from account activity or current pricing evidence.

## 6. Remaining operator decisions / confirmations

1. **Weekend/open-position handling:** Thursday entries and weekend carry are allowed by the operator, but the precise manual response to an unfilled sell is not yet specified. Choose a review time and what the operator will do if the position remains open; do not automate cancel/replace.
2. **Historical sample scope:** use the five-row pilot to validate data/session/calculation first, then broaden the sample. The operator does not need to reconstruct unrecorded personal trades.
3. **Per-symbol tradability/exclusions:** the maintained OTC lists must be synchronized and checked before a candidate is treated as eligible. The canonical Schwab exclusion list in the repository is not proof that both operator-maintained OTC lists are synchronized.

Confirmed strategy: LMT EXT buy between 3:00–5:00 PM Arizona, Monday–Thursday; LMT GTC_EXT sell at relevant regular-session close +0.5%; GTC_EXT is the intended sell session; no new Friday entries; Thursday/weekend carry is allowed with a manual risk plan. Do not require EXTO 24h for this workflow. Manually review each candidate and order; no automated order submission.

## 7. MCC launch checklist

### User actions
- [ ] Define the manual review/action plan for an unfilled sell, especially after a Thursday entry and across weekends.
- [ ] If using a broker/data screenshot or export, redact account and personal information.
- [ ] Confirm the two OTC exclusion lists are synchronized; resolve any disagreement without deleting exclusions silently.
- [ ] If available, provide historical chart/export data only after the required session coverage is visible; otherwise report that export is unavailable rather than estimating values.

### Engineering tasks
- [ ] Build repeatable historical-data import/generation with feed/session metadata and fail-closed coverage checks.
- [ ] Test holidays, early closes, DST changes, missing intervals, duplicate bars, invalid values and session mismatch.
- [ ] Preserve source ledger/provenance with generated input.
- [ ] Implement a separate fixed-target reference-close evaluator: target = regular-session close × 1.005; report touch/no-touch, first-touch time when supported, and coverage gaps. Do not repurpose the excursion-percentile calculator for this question.
- [ ] Verify target rounding against the actual order price rules and hand calculations.
- [ ] Keep the percentile-based historical-reach calculator as a separate research tool only if its baseline/session definition matches the intended question.
- [ ] Wire the shared Schwab exclusion provider through all qualification paths.
- [ ] Define stale position, unfilled sell, Thursday and weekend behavior before showing READY.
- [ ] Validate broad labeled sample including losers, catalysts, low liquidity, gaps and no-fill cases.
- [ ] Missing/unverified catalyst coverage must produce DATA? or manual review, never confident READY.
- [ ] Keep this read-only/manual. No automated order submission.

## 8. Release status

Not ready for unattended or fully trusted Boring Overnight use. A successful build/run or high historical-touch percentage does not establish live eligibility, actual fills, profitability or safe weekend handling. Near-term use is read-only/manual decision support.

## [Uncertainties]
- Historical source/feed coverage for the actual GTC_EXT-eligible session is not established.
- The exact manual action for an unfilled position across Thursday/weekend is not yet specified.
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

The strategy deliberately sets the sell limit from the end-of-day close, not necessarily from the buy fill. There are two distinct return questions:

- **Reference-close / strategy-rule study (best first research sample):** For each symbol/date, set target sell limit = relevant regular-session close × 1.005. Check whether the observed high reaches that target during the next sell-eligible window covered by GTC_EXT. Record whether the target was touched and when, if timestamps are available. This can be researched without the operator having personally traded that symbol/date, provided historical prices and session coverage are real and verified. It is a target-touch study, not proof that an order would fill.
- **Actual-fill study:** For trades actually made, record actual buy fill price/time, sell limit price/time, sell fill price/time if sold, and any still-open/unfilled status. Calculate realized return from actual buy fill to actual sell fill. The operator's recent sample is too small to validate performance on its own.
- **Both, kept separate:** Maintain two tabs/files, such as ReferenceCloseStudy and ActualTrades. Never pool observations into one percentile distribution. The first evaluates historical strategy-rule target touches across more symbol/dates; the second measures actual execution and realized outcomes. Compare them later, but do not treat one as the other.

For the reference-close study, minimum fields are symbol, date, source-backed regular-session close, target limit (close × 1.005, rounded according to the actual order's valid price rules), following sell-window start/end, observed high and timestamp, whether target was touched, source/feed, data coverage/missing intervals, catalyst/market notes, and provenance.

## 11. Simplified data collection path — no invented spreadsheet and no requirement for many personal trades

Do not ask the operator to produce 20 historical personal trades. Use two tracks:

**Track A — historical reference-close pilot (primary blocker):**
1. Select a small pilot set of candidate symbols and completed dates. Include all qualifying cases in the selected sample, not only winners.
2. Obtain actual historical daily close plus historical extended-hours/premarket data from a source whose coverage is verified for the required window.
3. For each symbol/date, calculate target = close × 1.005; inspect the next eligible sell window; record whether the high touched target and when.
4. Record source/feed and exact session coverage. If the source cannot cover the GTC_EXT-eligible window, mark the row DATA_GAP and do not count it.
5. Start with a small hand-verified pilot (for example, five symbol/date rows) to prove the workflow and calculations. Expand only after the source and session mapping are proven. The existing calculator's 20-observation minimum applies when using that calculator; it does not mean 20 personal trades are required.

**Track B — actual trade journal (secondary validation):**
For every future trade, preserve symbol/date, MCC score and decision output at entry, buy order and actual fill, reference EOD close used for target, target limit, sell order qualifier, sell fill/time or open status, and any manual intervention. This is prospective data collection. Do not reconstruct unknown values from memory as if verified. Add historical trades only where records/screenshots provide evidence.

**Important:** if the current CLI accepts only excursion_percent and computes a percentile threshold from excursion samples, it may not answer the fixed-target question “did high reach EOD close × 1.005?” Do not feed target-touch binary results or arbitrary excursion values into it. Inspect implementation/tests and add a separate fixed +0.5% historical target-touch evaluator if the current calculator's semantics do not match the strategy.

## 12. User-confirmed policy and remaining engineering gates

- Buy order: LMT EXT, 3:00–5:00 PM Arizona time, Monday–Thursday; no new Friday entries.
- Sell order: LMT GTC_EXT at the relevant regular-session EOD close +0.5%, not necessarily actual buy fill +0.5%.
- Sell may fill same evening, next morning premarket, or shortly after regular open.
- GTC + Extended is acceptable; user-reported active window is 7:00 AM–8:00 PM ET. EXTO 24h is not a current requirement.
- Weekend carry is allowed, including after Thursday entries, with an explicit manual risk plan.
- Current checks include the “Use the Overnight” watchlist columns; BMY Overnight Score reported as 33.49. This score does not prove order eligibility or profitability.
- Do not use the small recent personal-trade sample as the sole validation dataset.

Engineering work: source-backed historical price/session ingestion; fixed-target +0.5% touch study if current calculator is not semantically suitable; preserve provenance and gaps; exchange calendar/time-zone validation; synchronize and enforce both OTC exclusion lists across candidate paths; record actual fills prospectively; define manual weekend/open-position plan; keep decision support read-only with no automated orders.
