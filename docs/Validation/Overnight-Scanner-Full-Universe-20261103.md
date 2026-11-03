# Overnight Scanner Full-Universe Review — 2026-11-03 (10:52 AM export)

**Source:** `data/ScannerExports/2026-11-03-WatchListScanner_1052am.csv`  
**Source blob:** `2026-11-03-WatchListScanner_1052am.csv` on `feature/opportunity-quality-classification`  
**Method:** Parsed the original UTF-8 CSV including BOM, two metadata rows, and header row 4. All 198 nonblank symbol rows were retained; no duplicate symbols were found.

## 1. Export integrity

- Metadata: `Watchlist Scanner`, blank row, `Results`.
- Header: row 4.
- Fields: Symbol, Description, ON WIN %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, Overnight Score.
- **198 data rows, 198 unique symbols, 0 duplicates.**
- The reported scanner result count is 248. This file contains 198 ticker rows: **50 rows are still missing**. Do not infer their identities.
- No current price or 20-day average daily volume fields are present.

The previous 9:01 AM export had 195 unique symbols. The 10:52 AM export adds three rows net; it is still not the complete 248-result export.

## 2. Historical-metric gates

Applied the five available checks:

1. ON win rate >= 60%.
2. 20-session average ON return >= +0.10%.
3. 5D ON return strictly greater than 20-session average ON return.
4. Current ON move < 3%.
5. Above 20 SMA = 1.

| Result | Rows |
|---|---:|
| Pass all five available checks | **187** |
| Known failures | **11** |
| Incomplete / unknown | **0** |

Known failures:
- Current ON move >=3%: HLF (7.00%), EVER (10.80%), XHR (3.72%), PHG (5.15%), JAN (3.09%), AHRT (3.12%), ALSN (3.90%).
- 5D ON return <= 20D average: CTRE, KVUE, POR.
- Above 20 SMA = 0: DG.

No other row failed these five checks. These are historical scanner gates only; they do not establish current price/volume eligibility, catalyst clearance, chart structure, or an actionable order.

## 3. Highest Overnight Score among historical-gate passes

These are the top 15 by the export's Overnight Score after applying the five historical gates. This score is **not** the separately specified Boringness Score and is not a final opportunity rank.

| Rank | Symbol | Overnight Score |
|---:|---|---:|
| 1 | LXU | 68.60 |
| 2 | OSW | 67.48 |
| 3 | MHK | 66.79 |
| 4 | PUK | 65.13 |
| 5 | EHC | 64.13 |
| 6 | EVR | 63.07 |
| 7 | AON | 62.90 |
| 8 | DANOY | 62.50 |
| 9 | JMKE | 62.09 |
| 10 | GEHC | 61.79 |
| 11 | WWW | 61.58 |
| 12 | VSXY | 61.05 |
| 13 | FNGD | 60.73 |
| 14 | TRN | 59.83 |
| 15 | MAR | 59.15 |

MHK, PUK, AON, DANOY and GEHC are present and pass the five historical checks. OSW and EVR also pass these five checks. **None is approved** until the remaining gates are supplied and reviewed.

## 4. Exclusion cross-check and local store correction

The canonical Schwab exclusion source on `master` contains 55 symbols. Exact-normalized comparison against all 198 scanner symbols found **zero overlaps**. OSW, EVR and MHK are not on the canonical list. This is not a live Schwab tradability guarantee.

The user supplied the local API response with only `TEST`. The feature branch's persisted JSON file also contained only `TEST`, confirming that the runtime store had not been seeded from the canonical list.

Corrective action on the feature branch:
- Seeded all 55 canonical symbols into `src/MomentumCommandCenterV2.API/App_Data/exclusions-watchlist.json`.
- Preserved the existing `TEST` entry.
- The tracked file now contains 56 entries.
- Copied the master canonical text list and source-of-truth documentation into `docs/project-control` and `src/MomentumCommandCenterV2.Infrastructure/Resources`.

**Runtime acceptance still required:** after pulling/restarting the API, `GET /api/watchlists/exclusions` should report 56 entries, including all 55 canonical symbols plus TEST. This has not yet been observed on the user's local process. The local Add/Remove API is not a live Schwab account sync.

## 5. Qualification disposition

**No final Boring Overnight candidate can yet be approved from this CSV.**

Missing gates for all 198 rows:
- Current price > $5.
- 20D average daily volume >= 500,000.
- Validated catalyst/event classification and current source timestamps.
- Current Daily trend and price structure.
- Current 5M authority/Permission and structure.
- 1M execution/timing evidence, after 5M authority is established.
- Point-in-time source provenance and immutable candidate decision snapshot.

The next step is not another TOS scan. It is MCC qualification after the export is imported: apply price, volume, exclusions, catalysts and chart evidence to the 187 historical-pass rows. The import table is the candidate data surface; Red Zone currently previews only four rows by raw Overnight Score, not by the validated Boring Overnight ranker.

## 6. Importer / Red Zone diagnosis and correction

The user's browser reported `Imported 195 local rows · Red Zone updated`, but the Red Zone did not visibly change.

Source inspection clarified the DOM roles:
- `#mcc-static-0278-div` is the static wrapper around the scanner results table.
- Dynamic scanner table rows are inserted into `tbody#scannerRows`; the wrapper itself is not replaced and should not be expected to mutate.
- Actual Red Zone cards are rendered into `#redZoneGrid`; the card count is also shown by `#morningTopCount`.

The importer already calls both `renderScanner()` and `renderMorningReport()`. Its success message previously claimed Red Zone updated without checking the rendered DOM. Corrected `frontend/app.js` so the success message reports:
- imported row count;
- actual scanner table row count;
- actual Red Zone card count and symbols.

The Red Zone subtitle now explicitly says an imported preview is the **top four by Overnight Score only**, not Boring Overnight qualification. This avoids representing the preview as an approved rank.

The code change passed CI in [run 41693652415](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41693652415). The browser acceptance remains pending; the user must verify the new on-screen counts and visible cards. If the message reports zero Red Zone cards or fewer table rows than imported, capture the exact status text and the contents of `#redZoneGrid` / `#scannerRows`.

## 7. Next steps

1. Obtain an export with 248 ticker rows. Current file has 198, so 50 remain absent.
2. Pull the feature branch and restart the API; verify exclusion endpoint count = 56 and confirm OSW/EVR/MHK are not excluded.
3. Import the 198-row file again. Read the new success text: expected scanner rows = 198 and Red Zone cards = 4 (unless manual pins/dismissals change that).
4. Apply price and 20D average-volume gates to the historical-pass rows.
5. Verify catalyst evidence and source timestamps.
6. Review current Daily and 5M charts; use 1M only for execution timing after 5M authority is established.

## [Uncertainties]

- The source file has 198 rows versus the reported 248. The 50 missing symbols are not present in the export.
- Price and 20D average volume are not in the file.
- Canonical exclusion membership is verified; live Schwab tradability is not.
- The new importer status message is code- and CI-verified, not yet browser-verified.
- Historical Overnight Score is not the Boringness Score or a validated top-four opportunity rank.
