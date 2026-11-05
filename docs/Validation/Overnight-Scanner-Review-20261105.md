# Overnight Scanner Review — 2026-11-05

## Source files

- `data/ScannerExports/2026-11-05-WatchListScanner.csv`
- `data/ScannerExports/2026-11-05-WatchListScanner_Updated.csv`

Both exports contain the same 10 scanner columns: Symbol, Description, ON WIN %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, and Overnight Score.

## Completeness comparison

| Measure | Original | Updated |
|---|---:|---:|
| Data rows | 491 | 495 |
| Rows with all eight metric fields usable | 192 | 199 |
| Rows with at least one unavailable metric | 299 | 296 |
| Usable-row change | — | +7 |
| Duplicate symbols within usable rows | 0 | 0 |

The updated export has 11 symbols that were not usable in the original: AIR, CATY, COF, CPAY, ESS, FFIN, GBX, GEHC, GEN, GFI, and GILD. Four previously usable symbols are no longer fully usable: AIG, AMLX, BNS, and COTY. Net usable rows increased by seven.

The unavailable metric values are the Thinkorswim custom-expression subscription-limit message, plus one `loading` value in the updated export. Do not convert those values to zero or use them in ranking. The export does not establish why individual rows are missing beyond the literal reported status.

## Updated top 20 by Overnight Score

| Rank | Symbol | Overnight Score | ON Win % | 5D ON % | Current ON % |
|---:|---|---:|---:|---:|---:|
| 1 | BAYRY | 85.90 | 65.0 | 3.73 | 2.96 |
| 2 | EWTX | 85.50 | 70.0 | 3.84 | 14.58 |
| 3 | ENOV | 84.12 | 60.0 | 4.04 | 16.42 |
| 4 | ARDT | 83.85 | 70.0 | 3.46 | 12.78 |
| 5 | ACHC | 83.59 | 60.0 | 4.12 | 15.29 |
| 6 | CPAY | 71.66 | 65.0 | 2.55 | 8.61 |
| 7 | BOBS | 69.97 | 70.0 | 1.82 | 3.66 |
| 8 | AVNT | 68.02 | 60.0 | 2.67 | 0.25 |
| 9 | GFI | 67.29 | 60.0 | 2.27 | 4.46 |
| 10 | BLMN | 66.83 | 70.0 | 1.52 | 0.46 |
| 11 | EVER | 66.76 | 60.0 | 2.40 | -0.49 |
| 12 | BLK | 66.70 | 85.0 | 0.81 | 0.93 |
| 13 | CBOE | 66.01 | 75.0 | 1.14 | 1.85 |
| 14 | ACEL | 65.83 | 60.0 | 2.18 | 0.45 |
| 15 | CI | 65.67 | 70.0 | 1.61 | 4.12 |
| 16 | DANOY | 65.53 | 65.0 | 1.50 | 0.92 |
| 17 | CNC | 65.44 | 75.0 | 1.33 | 0.60 |
| 18 | EW | 65.26 | 80.0 | 1.34 | 1.31 |
| 19 | ASB | 64.76 | 80.0 | 1.00 | 1.21 |
| 20 | D | 64.20 | 75.0 | 1.26 | 1.55 |

The top five symbols are unchanged between the original and updated files: BAYRY, EWTX, ENOV, ARDT and ACHC. The updated file moves CPAY into rank 6 and GFI into rank 9; COTY is not fully usable in the updated file.

## Interpretation and limits

This is a ranking of the supplied Overnight Score values only. It is not an MCC qualification or a buy recommendation. The scanner exports do not contain the full set of independent decision inputs required by MCC:

- Verified current price and complete 20-session SIP average daily volume.
- Catalyst/news coverage and human review.
- Current completed 5-minute Permission and structure evidence.
- Current completed 1-minute execution evidence.
- MCC exclusion-list result.
- Current provider/feed provenance and data freshness.

Therefore no candidate is marked READY from these files. Rows with missing custom-expression values remain DATA?/UNKNOWN; they are not assigned a zero score or silently dropped from the full-universe evidence audit.

## Engineering implications

1. Preserve all imported rows, including partial rows, and expose a per-field availability state.
2. Keep scanner ranking separate from qualification; do not let a high Overnight Score bypass current market-data, catalyst, exclusion or execution gates.
3. Add an import comparison showing symbols newly complete, newly incomplete and unchanged between scanner refreshes.
4. Use the updated export as the latest scanner snapshot, but retain the original file as an immutable comparison input.


## Related candidate review

The November 5 Red Zone shortlist and CBOE comparison are documented in [Overnight-Candidate-Review-20261105.md](Overnight-Candidate-Review-20261105.md). CBOE is the preferred addition to the next ordinary Overnight review; BLMN is excluded from a routine November 5→6 hold because of its November 6 premarket earnings event.
