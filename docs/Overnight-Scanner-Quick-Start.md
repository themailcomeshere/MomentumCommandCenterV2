## 2026-11-03 — current scan export and process timing

The November 3 10:52 AM export `data/ScannerExports/2026-11-03-WatchListScanner_1052am.csv` contains the `Watchlist Scanner` / `Results` preamble, a blank row, the header on row 4, and **198 unique ticker rows**. The user-reported scan count was 248; 50 rows are not present in this file. Preserve the original export and verify the full result count on the next export.

The importer preserves all seven Overnight metrics in each candidate. Imported rows are saved in browser localStorage and restored after dashboard reload. The new Boring Overnight Qualification panel evaluates the five available historical gates and reads the local MCC exclusion API. On the 10:52 file: 187 pass, 11 fail, 0 unknown; none is READY because price, 20D average volume, catalyst and current chart evidence remain DATA?. The Red Zone preview shows up to four highest Overnight Scores among historical-gate passes only. This is not the final Boringness rank or trade approval. Code/CI passed; browser acceptance remains pending.

**Corrected Overnight timing:** The strategy includes Day 1 after-hours and Day 2 premarket. Do not wait until 20:00 ET. Begin actionable candidate review in the final 60 minutes before the regular-session close. Evaluation starts at actual close minus 60 minutes; the buy deadline shown by the prototype clock is the regular-session close. This normally appears as noon Arizona during EDT and 1:00 PM Arizona during EST, with early closes handled by the exchange calendar. Broker order qualifier/eligibility remains a separate confirmation.

# Overnight Scanner — Quick Start

**Purpose:** Turn the Thinkorswim Overnight scan into a consistent shortlist, evidence review, and MCC Red Zone workflow. This is a candidate-research workflow, not an automatic trade authorization.

## 1. Run and preserve the scan

Run the actionable final review during the final 60 minutes of the regular session. The exact start and close are calendar-derived; do not hardcode an Arizona time or wait for after-hours to end.

1. Open the Thinkorswim Overnight scan using the approved scan filters and current study/column version.
2. Sort `Overnight Score` descending.
3. Record the scan date/time and session context. Preserve the result count displayed for that specific scan and the number of rows actually exported. Counts vary by scan snapshot; never carry yesterday's count forward as today's expected row count.
4. Export the results to CSV if available. Keep the original export unchanged; do not strip columns or overwrite the source file.
5. Review the highest-ranked names as candidates. Do not assume the top score is a READY overnight position.

## 2. Apply the Boring Overnight review gates

Use `docs/BoringOvernight.md` as the strategy definition. The current provisional gates are:

- Overnight win rate at least 60%; 70%+ preferred.
- 20-session average overnight return at least +0.10%.
- 5-session average overnight return strictly greater than the 20-session average under the current cheat-sheet rule.
- Median overnight return positive and reasonably close to the mean; a high mean with a weak median can be an outlier effect.
- Price above $5.
- 20-session average daily volume at least 500,000 shares.
- Price above the 20-day SMA.
- Not on the maintained Schwab exclusion/unavailable lists.
- No incompatible earnings, M&A, regulatory, corporate-action, or other material catalyst risk.

Current overnight move is a *penalty/review input*, not a standalone qualification. A current ON move around/above 3% is increasingly hot and deserves additional scrutiny. The preferred historical ranges and score bands are documented in `docs/BoringOvernight.md`.

**Missing or stale data is not a pass.** If price, volume, exclusion status, catalyst status, session boundaries, or required history is unavailable, mark the candidate DATA? / MANUAL REVIEW. Do not silently fill a missing field.

## 3. Research news and catalysts

For each serious candidate:

1. Search company investor relations and SEC filings first for earnings, guidance, buybacks, dividends, acquisitions, financing, management changes, and corporate actions.
2. Search reputable financial news for sector, industry, regulatory, geopolitical, and company-specific developments.
3. Record the article/filing URL, publication time, event date, and whether the information is already reflected in the overnight move.
4. Treat an earnings reaction or other fresh material event as a catalyst-risk review, even when the news is positive. Positive news does not automatically make an overnight position boring.
5. If event status cannot be verified, leave it UNKNOWN / MANUAL REVIEW. Do not label it catalyst-clear.

## 4. Upload charts for the final shortlist

Upload the same chart set for each of the final 2–4 candidates:

- **Daily:** show price, 20-day SMA, recent range, and enough history to assess structure.
- **5-minute:** show the current session, VWAP/EMA structure, volume, and the close into the overnight period.
- **1-minute:** only when evaluating a specific entry/exit or intraday timing; it is not a substitute for overnight historical qualification.

Include the symbol, date, timeframe, and visible study/version where possible. If the chart is cropped so that the price scale, date, or relevant study values are missing, that evidence cannot be confirmed.

## 5. Put candidates into MCC Red Zone

### Thinkorswim CSV import

**Exact location in MCC:** open the local MCC Dashboard and scroll to **Scanner Workbench → Candidate discovery**. Click **Import CSV**. This is not a new Thinkorswim scan and does not require editing TOS filters.

After import, scroll below the Scanner Workbench to **BORING OVERNIGHT · QUALIFICATION → Candidate qualification**. This table shows every imported symbol, the five historical gates, Schwab exclusion result, and the still-missing current price / 20D average volume fields. It is the place to review the candidate qualification pipeline; the Red Zone above is only a four-card preview.

The Dashboard's Scanner & Watchlist workbench accepts a CSV with a `Symbol`, `Ticker`, or `Symbol/Ticker` column. The importer handles UTF-8 BOMs, comma/semicolon/tab delimiters, and quoted headers/fields. It maps `Overnight Score` to Score and preserves `ON WIN %`, `AVG ON %`, `Median ON %`, `5D ON %`, `ON Momentum`, `Current ON %`, and `Above 20 SMA` as structured `overnightMetrics` on each candidate. The existing reason column displays a compact summary of those values. Optional generic columns remain `Status`, `Reason`, `Extension`/`Ext`, and `Freshness`/`Data_As_Of`. Keep the original TOS export unchanged. If the import is rejected, the UI reports the normalized headers it detected so the source format can be diagnosed.

Example:

```csv
Symbol,Score,Status,Reason,Freshness
OSW,70.97,WATCH,Record Q3 and raised guidance; verify catalyst risk,2026-11-02
EVR,68.25,WATCH,Record Q3; verify event and structure,2026-11-02
MHK,73.42,WATCH,Current ON move above preferred range,2026-11-02
```

Importing the CSV refreshes the scanner table, Red Zone, and the Boring Overnight Qualification panel. The import is persisted in this browser and restored on reload. The dynamic scanner rows live in `tbody#scannerRows`; `#mcc-static-0278-div` is only the surrounding static table wrapper. The actual Red Zone cards live in `#redZoneGrid`. The import status now reports actual rendered row/card counts and symbols. Red Zone shows up to four highest Overnight Scores among the five historical-gate passes; price, liquidity, catalyst and chart gates remain separate DATA? requirements. Pin a candidate to retain it; dismiss a candidate to remove it from the current Red Zone view. These choices are browser-local.

### What updates automatically

- **After CSV import:** Red Zone now re-renders immediately from the imported candidate rows.
- **Market assessment cards:** refreshed every 15 seconds while the page is visible. This updates current 1M/5M assessment, freshness, extension, participation, and price line; it does not change the imported Overnight Score.
- **Alpaca live discovery:** persisted top-gainers/losers are fetched every 30 seconds only when the `Alpaca live discovery` scanner group is selected. The source is SIP-based and separate from the Thinkorswim Overnight scan.
- **Thinkorswim Overnight scan:** not automatically connected to MCC. A new TOS scan must be imported/exported again; MCC does not have a Thinkorswim scanner API connection.

Current Alpaca account entitlement denies recent SIP requests. Therefore the Alpaca live-discovery source may report degraded health or stop producing fresh discoveries. Do not present old persisted discoveries as current. The current IEX OHLCV feed does not make the separate SIP-based movers endpoint available.

## 6. Interpret Red Zone correctly

Red Zone is a live technical-monitoring surface, not the Boring Overnight qualification engine.

- Its 5M Permission / 1M execution assessment is based on MCC market-data evidence.
- It does not certify the TOS overnight win rate, 5D/20D overnight returns, 20-session average volume, catalyst clearance, or exclusion status.
- A strong 5M score or EXECUTION READY label does not turn a candidate into an overnight READY position.
- Imported scanner scores remain imported ranking values; MCC does not recalculate or validate them.
- Missing or stale market assessment data must remain DATA? / UNKNOWN.

## 7. Final decision and record

For each candidate, retain:

- scan snapshot and timestamp;
- overnight statistics and sample count;
- current overnight move and price-vs-20SMA status;
- price and average volume;
- exclusion-list result;
- catalyst/news links and publication times;
- daily/5M/1M chart evidence;
- MCC 5M Permission and 1M execution state, when relevant;
- final disposition: READY, WATCH, BLOCKED/REJECT, DATA?, or ZERO POSITION, with reasons.

Use READY only when every required hard gate and evidence requirement is affirmatively satisfied. Otherwise use WATCH, BLOCKED, or DATA?. Zero position is valid.

## 8. Current system boundary

The Thinkorswim Overnight scan is the candidate discovery source in this workflow. MCC's API-backed Red Zone adds live technical assessment and monitoring, but it does not currently ingest Thinkorswim scans automatically or calculate a validated complete-market Overnight ranking. The separate Alpaca discovery service is limited to top gainers/losers and currently depends on SIP entitlement.

Related documents:
- `docs/BoringOvernight.md` — thresholds, score policy, hard gates, and research methodology.
- `docs/Overnight-Collection-Contract.md` — desired persistent overnight collection and Morning Brief architecture.
- `docs/Scanner-and-Watchlist-Workbench.md` — scanner and Red Zone implementation boundary.
- `docs/Active-Work-Checklist-20261102.md` — current implementation and validation tasks.

## [Uncertainties]

- The November 3 10:52 AM export contains 198 rows. This is the current file's row count, not a fixed expected count; the user confirmed all 198 rows imported and rendered.
- The exact Thinkorswim scan formulas/column implementation behind `Overnight Score` were not supplied in this upload.
- Price and 20-day average volume are not columns in the November 3 CSV; those hard gates remain unverified for the 198-row imported universe.
- Catalyst clearance requires current per-symbol review and cannot be inferred from the scan columns.

## 2026-11-04 — after-import data actions

1. The qualification summary's **Unknown / incomplete history** count includes rows with missing required custom-expression outputs. These are DATA?, not failed stocks. A historical FAIL requires all required metrics to be present and at least one gate to fail.
2. After reviewing the imported score order, use **Fetch Daily + catalyst for top 4** to retrieve adjusted Daily metrics and the preceding 24 hours of ticker news for the four highest-scored imported rows only. This is not the same as a manual chart-review shortlist and does not provide 1M/5M refresh or catalyst clearance.
3. For missing exact close/open timestamps, use **Fetch / retry 20-session close/open history**. Inspect the exact-boundary audit; no neighboring minute is substituted.
4. Use **Fetch 20-session GTC_EXT sell-window history** only after the provider status confirms effective SIP. IEX stops at preflight and is diagnostic only.
5. Do not expect a numeric Calculated Limit Sell until complete SIP windows, ATR parity MATCH and walk-forward validation are available.
