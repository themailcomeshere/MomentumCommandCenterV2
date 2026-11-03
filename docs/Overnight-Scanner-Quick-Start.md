## 2026-11-03 — Scanner CSV import retry

The synced export is now present at `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`. It contains two metadata preamble lines (`Watchlist Scanner`, `Results`) followed by the actual comma-delimited header on line 3. The importer previously assumed line 1 was the header, which caused the reported missing Symbol/Ticker error.

`frontend/app.js` now searches the first 25 nonblank rows for a normalized Symbol/Ticker header, then imports the data rows after it. The uploaded file was parsed against the same delimiter/header rules: comma delimiter, header row 3, **50 nonblank ticker rows**, first three MHK/OSW/EVR with Overnight Scores 73.42/70.97/68.25. Browser interaction has not yet been exercised, so final UI acceptance remains pending. Do not re-upload or edit the CSV to remove its preamble.

Current importer fix commit: `b05c63713452cbbbb5632a87c639f655ad027e73`. GitHub Actions run 41549757022 is still running at the time this note was added.

# Overnight Scanner — Quick Start

**Purpose:** Turn the Thinkorswim Overnight scan into a consistent shortlist, evidence review, and MCC Red Zone workflow. This is a candidate-research workflow, not an automatic trade authorization.

## 1. Run and preserve the scan

1. Open the Thinkorswim Overnight scan using the approved scan filters and current study/column version.
2. Sort `Overnight Score` descending.
3. Record the scan date/time and session context. Preserve the full result count and visible result count. If the screen says `Showing 50 of 248`, the screenshot documents the first 50 sorted rows, not all 248.
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

The Dashboard's Scanner & Watchlist workbench accepts a CSV with a `Symbol`, `Ticker`, or `Symbol/Ticker` column. The importer handles UTF-8 BOMs, comma/semicolon/tab delimiters, and quoted headers/fields. Optional columns are `Score` (also `Overnight Score`), `Status`, `Reason`, `Extension`/`Ext`, and `Freshness`/`Data_As_Of`. Keep the original TOS export unchanged. If the import is rejected, the UI reports the normalized headers it detected so the source format can be diagnosed.

Example:

```csv
Symbol,Score,Status,Reason,Freshness
OSW,70.97,WATCH,Record Q3 and raised guidance; verify catalyst risk,2026-11-02
EVR,68.25,WATCH,Record Q3; verify event and structure,2026-11-02
MHK,73.42,WATCH,Current ON move above preferred range,2026-11-02
```

Importing the CSV now immediately refreshes both the scanner table and Red Zone. Red Zone displays up to four priority candidates, and each card requests current MCC market assessment data. Pin a candidate to retain it; dismiss a candidate to remove it from the current Red Zone view. These choices are browser-local.

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

- The complete 248-symbol scan export was not supplied; this review is based on the visible first 50 rows in the screenshot.
- The exact Thinkorswim scan formulas/column implementation behind `Overnight Score` were not supplied in this upload.
- Price and 20-day average volume for the shortlist were not visible in the screenshot; those hard gates remain unverified.
- Catalyst clearance requires current per-symbol review and cannot be inferred from the scan columns.