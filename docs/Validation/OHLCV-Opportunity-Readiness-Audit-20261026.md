# OHLCV export and opportunity-quality audit — 2026-10-26

**Branch reviewed:** `feature/opportunity-quality-classification`  
**Purpose:** Use the newly committed export files to advance the READY-frequency and Boring Overnight investigations without inventing missing bars or indicator states.

## Executive result

- **A usable aligned XRPN 1M/5M pair exists for October 19, 2026**, in `data/StrategyReports_XRPN_102026_933.csv` and `data/StrategyReports_XRPN_102026_5m_offset_1027.csv`. Both payloads cover approximately 06:30–13:00 Arizona time; the 5M file starts at 06:20.
- **The October 2 XRPN pair is still not aligned.** The new file uploaded as `data/StrategyReports_XRPN_10022026_1M_EntireDay_2.csv` actually has `Symbol: HPK` in its report header and all 214 payload rows are HPK 1M bars for October 2. I renamed it to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv`; it exactly duplicates the October 2 HPK rows in the existing HPK multi-day export. The companion `data/StrategyReports_XRPN_10022026_5M_EntireDay.csv` is XRPN 5M, so the symbol mismatch remains. Separately, the October 22 XRPN 1M file is correctly named `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`.
- The October 19 pair is sufficient to continue checking export structure and 1M/5M OHLCV consistency, but **not sufficient to diagnose READY gates**: the paired rows contain OHLCV, not the actual per-bar ThinkScript Permission, candidate, extension, READY, re-arm, or expiration states.
- **The current files do not support a valid Boring Overnight target-reach estimate.** They do not cover the full GTC_EXT-eligible window around the regular close, and `eligible-window-high-excursions.csv` contains placeholder values 1–20 rather than sourced historical observations.

## File-by-file findings

| File | Embedded payload date / range (Arizona) | Finding |
|---|---|---|
| `data/StrategyReports_XRPN_102026_933.csv` | 2026-10-19, 06:30–13:00; 355 1M payload rows | Paired 1M input; 31 non-one-minute intervals detected in this file. |
| `data/StrategyReports_XRPN_102026_5m_offset_1027.csv` | 2026-10-19, 06:20–13:00; 81 5M payload rows | Same symbol/date as the 1M file; regular five-minute timestamp spacing in this export. |
| `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv` | 2026-10-22, 06:30–13:00; 267 1M payload rows | Filename now matches the report header and embedded epoch date; the previous misleading filename was corrected. |
| `data/StrategyReports_XRPN_10022026_5M_EntireDay.csv` | 2026-10-02, 06:30–13:00; 79 5M payload rows | XRPN 5M bars; no same-symbol XRPN 1M partner is established by the audited files. |
| `data/StrategyReports_HPK_10022026_to_10032026_1M_EntireDay.csv` | 2026-10-01 06:30 through 2026-10-02 12:59; 422 1M payload rows | Two-day HPK file with many gaps; its October 2 subset exactly matches the 214 rows in the newly uploaded HPK file. Not an XRPN signal-parity pair. |
| `data/StrategyReports_UPC_102226.csv` | 2026-10-22, 09:37–09:47; 4 1M payload rows | Short strategy report; cannot settle the separate 09:16–09:36 UPC interval question. |
| `eligible-window-high-excursions.csv` | No symbol/date/source columns; values 1–20 | Placeholder/sample data, not eligible-window market observations. Exclude from research. |

Timestamps above are converted from the embedded epoch payloads to `America/Phoenix`. The Strategy Report `Date/Time` column is transport metadata; do not substitute it for the embedded payload timestamp. Timestamp open/close/availability semantics remain a separate validation gate.

## October 19 paired coverage check

For the overlapping post-09:00 Arizona segment:

- The 1M file has 211 payload rows timestamped from 09:00 through 13:00 Arizona.
- From 09:00 through 12:59, 30 of the 240 expected minute timestamps are absent from this file. This is an export-coverage observation, **not proof of zero trades or missing chart bars**.
- Of the 49 5M payload windows beginning from 09:00 through 12:55 Arizona, 27 have all five constituent 1M payload timestamps present.
- For those 27 complete windows, aggregation of the five 1M payload bars matches the corresponding 5M payload OHLCV exactly.
- The remaining windows are incomplete from the 1M export and are not reconstructed. No bars were synthesized.

These checks validate only internal consistency for complete windows. They do not prove that all expected bars were exported or that either export's timestamp means bar-open, bar-close, or decision-availability time.

## READY-frequency investigation: what this enables

Use October 19 as the first aligned pair for a **bar-coverage and timeline pilot**, not as a complete explanation of rare READY events.

The OHLCV export payloads do not include internal ThinkScript states. The chart-parity worksheets do contain manually entered chart values for a limited sample, but not READY/Permission state. Therefore these sources cannot by themselves count:
- candidate trigger and re-arm events;
- 5M raw Permission and latched Permission transitions;
- extension-danger blocks;
- READY activation and expiration;
- the specific reason a candidate failed after 09:00 Arizona.

The already documented October 2 diagnostic screenshot remains the evidence for the October 2 Permission revocation; it should not be conflated with the October 19 OHLCV pair. To attribute READY frequency to a specific gate, the next evidence must pair the same symbol/date/bar with the relevant 1M/5M diagnostic labels or a diagnostic export that records those states. The October 2 1M symbol mismatch is now documented; to reconstruct the XRPN event, obtain XRPN 1M data rather than using the HPK upload.

## Boring Overnight target research: current disposition

The files audited here cover daytime chart windows, not the full GTC_EXT-eligible interval. For October 19, the eligible order window described in the project is 07:00–20:00 Eastern, which corresponds to 04:00–17:00 Arizona while Eastern daylight time is in effect. The available XRPN data starts around 06:20–06:30 Arizona and ends at 13:00 Arizona, leaving the early eligible window and post-13:00 segment uncovered.

Accordingly:
- Do not compute a target-touch rate, median excursion, or recommended alternative limit from these files.
- Do not treat a historical high touch as a fill.
- Do not treat the placeholder `eligible-window-high-excursions.csv` as observations.
- Mark any future row whose full eligible window is not present as `DATA_GAP`.
- Keep reference-close target-touch research separate from actual fills and realized outcomes.

## Opportunity-quality implementation audit

The branch contains `OpportunityQualityAssessor`, its input/config/result models, and unit tests. The assessor:
- checks required metrics and 1M/5M timestamp freshness;
- lets existing eligibility dominate ranking;
- returns WATCH when the caller says the execution event is inactive;
- marks extension or close resistance as an entry-timing warning;
- emits deterministic quality classifications from supplied RVOL, momentum, and recent-range inputs.

It is **not wired** into the replay workflow, `V2SignalEngine`, CLI, API, or front end. Raw OHLCV replay does not calculate its required indicators, relative volume, recent range, resistance distance, or actual ThinkScript READY state. Its default thresholds are explicitly provisional and uncalibrated.

**UI wording safety change:** the assessor's current display labels are `QUALITY — HIGH`, `QUALITY — MODERATE`, `QUALITY — LOW`, and `TIMING — EXTENDED`, distinct from Thinkorswim `EXECUTION READY`. Source and tests have been updated; a later GitHub Actions run is required to verify the latest commits.

## Next steps, in order

1. Keep the October 19 1M/5M pair as the first aligned OHLCV coverage fixture; preserve its gaps and timestamp provenance.
2. [DONE] Rename the October 22 1M file to `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv` so its filename matches its embedded date.
3. Obtain or export diagnostic states for the same symbol/date/time as the OHLCV bars before attributing READY failures to a gate. The existing October 2 screenshot does not need to be re-uploaded.
4. Complete a full-window overnight export including the source-backed regular close and all available bars across the following 07:00–20:00 Eastern eligible window; otherwise retain `DATA_GAP`.
5. Add opportunity-quality replay/presentation only after the required metrics and existing execution-state snapshots are reproducible. Keep it diagnostic-only until out-of-sample comparison supports its value.

## [Uncertainties]

- The intended meaning of the export payload timestamp (bar-open, bar-close, or decision availability) is not settled by these files alone.
- Missing timestamps in the export are not classified as no-trade intervals versus export omissions.
- No full GTC_EXT eligible-window dataset or actual-fill dataset was found in the audited files.
- No internal READY/Permission diagnostic states are embedded in the paired October 19 OHLCV payloads.

## October 19 chart-parity worksheet results\n\nThe current parity worksheets have 20 manually chart-entered rows per timeframe. 1M: 10 `MATCH`, 10 `ROUNDING ONLY`, 48 unfilled rows. 5M: 9 `MATCH`, 11 `ROUNDING ONLY`, 13 unfilled rows. All 40 entered chart timestamps match the local clock digits represented by payload timestamps, but the worksheet's `ChartTimestampMeaning` cells say `MATCH`, not `BAR_OPEN`/`BAR_CLOSE`; semantics are therefore not fully documented. See `data/validation/XRPN/manifest.md` and `data/validation/XRPN/findings.md`.\n\n## Repository integration note

The GitHub compare result reports this feature branch as diverged from `master` (268 commits ahead, 152 behind). Treat the branch as a working baseline for this audit, not as a safe fast-forward/merge candidate. Reconcile history and file-level changes deliberately before merging.

The opportunity-quality display-label safety change and negative-metric test additions were committed on this branch after the audit. GitHub's combined status endpoint had no status records for the latest branch commit at the time of review; build/test verification is pending.

## New October 2 upload — verified symbol and coverage (2026-10-26)

The newly uploaded file was inspected by its report header and embedded OHLCV payload, not by filename alone.

- Original path: `data/StrategyReports_XRPN_10022026_1M_EntireDay_2.csv`.
- The report header says **Symbol: HPK**, work time 2026-10-02 06:32–13:01 Arizona; all 214 embedded payload rows are `HPK|1m` and resolve to 2026-10-02 06:30–12:59 Arizona.
- The paired file `data/StrategyReports_XRPN_10022026_5M_EntireDay.csv` says **Symbol: XRPN** and contains 79 `XRPN|5m` payload rows from 06:30 through 13:00 Arizona. This is still not a same-symbol pair.
- I corrected the 1M file's name to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv` and removed the misleading XRPN-named duplicate path.
- All 214 HPK 1M payload rows match the HPK rows at the same timestamps in `data/StrategyReports_HPK_10022026_to_10032026_1M_EntireDay.csv` exactly. Thus this upload confirms a clean duplicate/subset of the existing HPK data; it adds no new XRPN 1M evidence.
- The HPK 1M payload spans 390 expected minute timestamps from 06:30 through 12:59, but only 214 rows are present: **176 timestamps are absent**. This is not full one-minute coverage. Do not treat missing timestamps as no-trade bars.

## Explicit uncertainty assessment

| Uncertainty | Current assessment | Impact | What resolves it |
|---|---|---|---|
| October 2 symbol pairing | **Resolved: not aligned.** The new 1M upload is HPK; the October 2 5M report is XRPN. | The new file cannot diagnose XRPN's October 2 READY/Permission sequence. | Obtain the intended XRPN 1M export, or explicitly switch the diagnostic question to HPK with its matching 5M data. |
| Embedded timestamp meaning | **Unresolved.** Payload epochs convert consistently to Arizona local time, but the export alone does not state whether each timestamp is bar-open, bar-close, or decision-availability time. | Can shift bar alignment and causal replay; must not assert exact signal ordering until settled. | Compare a known timestamped chart bar / ThinkScript diagnostic bubble and document Thinkorswim's export convention. |
| Missing 1M rows | **Partially quantified; cause unresolved.** October 2 HPK: 214/390 minute timestamps present (176 absent). October 19 XRPN: 30 absent timestamps in the 09:00–12:59 interval. | Cannot infer zero trades, zero volume, or absent chart bars. Gaps invalidate strict parity windows that cross them. | Export bar-level coverage or confirm chart aggregation and export/report strategy behavior for a sample of missing timestamps. |
| READY/Permission cause | **Unresolved.** OHLCV has no internal gate values. The known screenshot shows Permission off at its observed point, but does not provide per-bar history or establish a general post-09:00 cause. | Cannot responsibly change READY logic or claim root cause. | Same-symbol/date 1M and 5M diagnostic exports including raw Permission, block code, candidate/trigger, extension danger, READY, re-arm, and expiration states. |
| Boring Overnight target reach | **Unresolved; current data is inadequate.** All audited bars end around 13:00 Arizona, before the 17:00 Arizona end of the GTC_EXT window during EDT; the high-excursion file is placeholder data. | No defensible target-touch rate or alternative limit can be recommended. | Source-backed regular-session close plus complete eligible-window OHLCV, timestamp provenance, and gap flags. |
| Actual order outcomes | **Data not found in sources.** Historical OHLCV does not provide actual fills, fees, order status, or slippage. | Target touch cannot be treated as execution or realized profit. | Actual order/fill exports and known fees; keep these rows separate from reference-close studies. |
| Current branch CI | **Pending, not failed and not green.** The latest GitHub Actions run was in progress at last check. | Recent label-safety/test changes remain unverified until the run concludes. | Recheck workflow run and inspect build/test results after completion. |
| Feature branch integration | **Known integration risk.** Earlier GitHub compare showed the branch diverged substantially from master. | Do not merge the branch wholesale or assume its commits are ready to promote. | Review changed files and reconcile branch history deliberately; prefer a PR with scoped changes and passing CI. |

## Recommended decision

The new October upload **does not close the XRPN October 2 data gap**. It is an HPK 1M export with substantial timestamp gaps and is an exact subset of existing HPK data. The best available same-symbol paired OHLCV fixture remains XRPN on October 19, and it is useful only for OHLCV coverage/aggregation checks—not READY gate attribution.

No signal rules or order behavior were changed as a result of this file review.
