# Overnight Scanner Full-Universe Review — 2026-11-03

**Source:** `data/ScannerExports/2026-11-03-WatchListScanner_901am.csv`  
**Source blob:** `962af9e1cfef24b93362c5d4d00f8de62abc50df`  
**Branch at review:** `feature/opportunity-quality-classification`  
**Method:** Parse the original CSV with its two metadata preamble rows and header on line 3. No rows were dropped or deduplicated.

## 1. Export integrity

- 2 metadata lines: `Watchlist Scanner`, `Results`.
- Header row: `Symbol,Description,ON WIN %,AVG ON %,Median ON %,5D ON %,ON Momentum,Current ON %,Above 20 SMA,Overnight Score`.
- **195 data rows, 195 unique symbols, 0 duplicate symbols.**
- The user-reported scanner result count was 248. The uploaded file contains 195, so it is not a complete 248-row export. The missing 53 rows are not in this file; do not infer which symbols are missing.
- The export contains no current price or 20-day average volume fields.

### Loading values by column

| Field | `loading` cells |
|---|---:|
| ON WIN % | 22 |
| AVG ON % | 25 |
| Median ON % | 18 |
| 5D ON % | 14 |
| ON Momentum | 17 |
| Current ON % | 15 |
| Above 20 SMA | 22 |
| Overnight Score | 17 |

The UI importer finds the Symbol header within the first 25 nonblank rows, handles the UTF-8 BOM and the two-line preamble, and maps `Overnight Score` to the imported score. Commit `649361834984292eab66fa577e960237d6ff1e7e` extends the importer to preserve all seven Overnight metric fields in each imported candidate's `overnightMetrics` object and puts a compact metric summary in the existing reason column. Source-level parser execution against this CSV found header row 3, comma delimiter, 195 imported rows, 195 unique symbols, and zero duplicates. Browser upload/Red Zone rendering remains unverified.

## 2. Historical-metric triage across all 195 rows

Applied only the available historical scanner checks:

1. ON win rate >= 60%.
2. 20D average ON return >= +0.10% (the `AVG ON %` field).
3. 5D ON return strictly greater than `AVG ON %`.
4. Current ON move < 3%.
5. Above 20 SMA = 1.

| Result | Rows | Meaning |
|---|---:|---|
| Pass all five available gates | **107** | All five fields present and all five conditions pass |
| Incomplete, no known gate failure | **81** | At least one of the five required fields is `loading` |
| Known gate failure | **7** | At least one available field definitively fails |

Known failures:
- Current ON move >=3%: EVER (10.80%), XHR (3.72%), PHG (5.15%), JAN (3.09%), AHRT (3.12%), ALSN (3.90%).
- 5D ON does not exceed 20D average: CTRE (0.20% versus 0.20%).

### Highest Overnight Score among the 107 numeric-pass rows

These are scanner-metric research candidates, **not READY/Boring Overnight approvals**. The scanner's Overnight Score is not the separately specified Boringness Score.

| Symbol | Overnight Score | ON Win % | 20D Avg ON % | 5D ON % | Current ON % |
|---|---:|---:|---:|---:|---:|
| MHK | 66.79 | 65 | 0.22 | 2.17 | -1.24 |
| PUK | 65.13 | 75 | 0.12 | 1.43 | 1.64 |
| AON | 62.90 | 70 | 0.44 | 1.37 | 0.08 |
| DANOY | 62.50 | 60 | 0.58 | 1.73 | 0.27 |
| GEHC | 61.79 | 60 | 0.53 | 1.96 | -0.16 |
| WWW | 61.58 | 75 | 0.43 | 0.97 | 0.14 |
| VSXY | 61.05 | 70 | 0.56 | 1.08 | 2.32 |
| TRN | 59.83 | 60 | 0.37 | 1.64 | 0.36 |
| NAC | 55.50 | 75 | 0.26 | 0.45 | 0.20 |
| LAMR | 55.18 | 75 | 0.31 | 0.43 | -0.29 |
| VRTX | 55.17 | 70 | 0.44 | 0.66 | 0.96 |
| XPOF | 54.88 | 60 | 0.47 | 1.25 | 1.26 |
| FCPT | 54.87 | 75 | 0.36 | 0.48 | 0.86 |
| SWX | 54.79 | 75 | 0.29 | 0.51 | 0.13 |
| OPCH | 54.70 | 80 | 0.15 | 0.39 | -0.03 |

## 3. OSW / EVR / MHK exclusion assessment

The current user-provided canonical Schwab exclusion list has 55 symbols. A symbol-by-symbol comparison against all 195 scanner rows found **zero overlaps**: none of the 195 symbols is on the canonical 55-list. OSW, EVR and MHK are therefore also absent. This only means they are not on the supplied canonical list; it does not prove current broker tradability.

The feature branch still uses a separate local `App_Data/exclusions-watchlist.json` store. The store returns an empty list when the file is absent and is not seeded from the 55-symbol canonical source. The local runtime file is not available in this repository review. Therefore live exclusion status remains unverified.

| Symbol | Canonical 55-list match | Scanner row status | Prior chart evidence |
|---|---|---|---|
| OSW | No | Current ON % = loading | Nov 2 5M WAIT / Permission NO / BEARISH; no Nov 3 chart supplied |
| EVR | No | ON Win % = loading | Nov 2 5M WAIT / Permission NO / BEARISH; no Nov 3 chart supplied |
| MHK | No | Numeric gates pass; current ON = -1.24% | Nov 2 5M WAIT / Permission NO / NEUTRAL; no Nov 3 chart supplied |

Previous public-source checks found OSW, EVR and MHK above $5 and their reported 20D average volumes above 500K. Those checks were secondary-source cross-checks from the prior review, not values in this CSV or current MCC authoritative data. All three had recent earnings catalysts in the previous review. No new Nov 3 Daily/5M/1M chart evidence or validated catalyst-clear source is present here.

## 4. Qualification decision

**No symbol is approved as a Boring Overnight BUY/HOLD from this export alone.**

The 107 numeric-pass rows are only the first filter. The next gates are still missing for the full universe:
- current price >$5;
- authoritative 20D average volume >=500K;
- current canonical exclusion list wired into this branch/runtime;
- validated catalyst/event status and source freshness;
- current Daily trend and current 5M permission/structure;
- current 1M execution/timing where relevant;
- complete source provenance and immutable point-in-time candidate snapshots.

Do not convert `loading` to zero, pass, or fail. Missing data remains DATA?/WATCH.

## 5. Next data and UI acceptance

1. Re-export the scanner with all 248 results and verify the resulting file has 248 ticker rows after the two metadata lines and header. Preserve the original preamble.
2. Upload current Daily, 5M and 1M charts for the highest-ranked candidates after price/volume/exclusion filtering. MHK, PUK, AON, DANOY and GEHC are the first scanner-metric review tier; this is not a final buy list.
3. Test browser import with the 195-row file first, verify the displayed imported count is 195 and Red Zone updates; then repeat with the true 248-row export.
4. Do not claim the importer has retained all Overnight metric columns; its current UI mapping only retains symbol, score and optional generic status fields.

## [Uncertainties]

- Why the TOS export has 195 rows when the scanner displayed 248 is unknown.
- Current price/20D volume for the remaining 195-symbol universe is not in the CSV.
- Current live Schwab exclusion state and the feature branch's local App_Data file are unavailable.
- Current Nov 3 chart states and validated catalyst coverage are unavailable.
- The 55-symbol canonical exclusion list is a user-maintained list, not a live broker-tradability feed.
