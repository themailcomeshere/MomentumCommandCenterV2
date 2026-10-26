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

### Step C — Collect a pilot
1. Collect at least 20 completed symbol/date observations using the same baseline and eligible-session definition.
2. Use a multi-week or multi-month span, not handpicked winners. Include ordinary, down, gap and event days; tag material events.
3. Mark incomplete rows INVALID_INCOMPLETE and do not include them in calculator input.
4. Check duplicates, splits/corporate actions, stale bars, missing intervals and out-of-session highs.
5. Hand-calculate at least three excursion values and reconcile to the generated values to two decimal places.
6. Retain all valid rows, including zero and negative excursions; do not clip losses or keep only successful target touches.

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

## 6. User decisions still required

1. Sell session: is 7:00 AM–8:00 PM ET under GTC + Extended acceptable, or does the strategy require EXTO 24h? Verify symbol/account eligibility if EXTO is required.
2. Weekend risk: are Thursday entries allowed if the sell remains unfilled? Can positions be carried over a weekend? Define latest entry and manual review/cancel/replace deadline. No automated order management is assumed.
3. Baseline: actual-fill observations only, or a separately labeled reference-close study?
4. Scope: start with a one-ticker pilot, then a broader symbol/date sample. One ticker cannot validate the whole universe.

Until answered, do not assume sell coverage from 8:00 PM to 7:00 AM ET; no new Friday entries; manually review every candidate and order; no automated order submission.

## 7. MCC launch checklist

### User actions
- [ ] Confirm BMY eligibility in Public → 24 Hour Trading; save redacted evidence.
- [ ] Record exact buy and sell qualifiers and whether sell is GTC + Extended or EXTO 24h.
- [ ] Decide whether only 7:00 AM–8:00 PM ET is acceptable or 24-hour execution is required.
- [ ] Decide Thursday/weekend carry policy and unfilled-exit handling.
- [ ] Replace fabricated CSV with source-backed ledger and generated calculator input.
- [ ] Run the pilot CLI and provide output plus ledger, with personal/account information redacted.
- [ ] Confirm the two OTC exclusion lists are synchronized and specify which is canonical if they disagree.

### Engineering tasks
- [ ] Build repeatable historical-data import/generation with feed/session metadata and fail-closed coverage checks.
- [ ] Test holidays, early closes, DST changes, missing intervals, duplicate bars, invalid values and session mismatch.
- [ ] Preserve source ledger/provenance with generated input.
- [ ] Verify percentile and tick rounding against tests and hand calculations.
- [ ] Wire the shared Schwab exclusion provider through all qualification paths.
- [ ] Define stale position, unfilled sell, Thursday and weekend behavior before showing READY.
- [ ] Validate broad labeled sample including losers, catalysts, low liquidity, gaps and no-fill cases.
- [ ] Missing/unverified catalyst coverage must produce DATA? or manual review, never confident READY.
- [ ] Keep this read-only/manual. No automated order submission.

## 8. Release status

Not ready for unattended or fully trusted Boring Overnight use. A successful build/run or high historical-touch percentage does not establish live eligibility, actual fills, profitability or safe weekend handling. Near-term use is read-only/manual decision support.

## [Uncertainties]
- EXTO 24h eligibility for BMY or other candidates is not yet established.
- Historical source/feed coverage for the actual Schwab-eligible session is not established.
- Weekend policy and actual-fill versus reference-close baseline remain user decisions.