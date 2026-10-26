# OHLCV export and opportunity-quality audit — 2026-10-26

**Branch reviewed:** `feature/opportunity-quality-classification`  
**Purpose:** Use the newly committed export files to advance the READY-frequency and Boring Overnight investigations without inventing missing bars or indicator states.

## Executive result

- **A usable aligned XRPN 1M/5M pair exists for October 19, 2026**, in `data/StrategyReports_XRPN_102026_933.csv` and `data/StrategyReports_XRPN_102026_5m_offset_1027.csv`. Both payloads cover approximately 06:30–13:00 Arizona time; the 5M file starts at 06:20.
- **The apparent October 2 full-day 1M/5M pair is not aligned.** The October 22 1M file is now correctly named `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`. It was previously named `StrategyReports_XRPN_10022026_1M_EntireDay.csv`, which incorrectly suggested October 2. The companion `data/StrategyReports_XRPN_10022026_5M_EntireDay.csv` is October 2. Do not combine them.
- The October 19 pair is sufficient to continue checking export structure and 1M/5M OHLCV consistency, but **not sufficient to diagnose READY gates**: the paired rows contain OHLCV, not the actual per-bar ThinkScript Permission, candidate, extension, READY, re-arm, or expiration states.
- **The current files do not support a valid Boring Overnight target-reach estimate.** They do not cover the full GTC_EXT-eligible window around the regular close, and `eligible-window-high-excursions.csv` contains placeholder values 1–20 rather than sourced historical observations.

## File-by-file findings

| File | Embedded payload date / range (Arizona) | Finding |
|---|---|---|
| `data/StrategyReports_XRPN_102026_933.csv` | 2026-10-19, 06:30–13:00; 355 1M payload rows | Paired 1M input; 31 non-one-minute intervals detected in this file. |
| `data/StrategyReports_XRPN_102026_5m_offset_1027.csv` | 2026-10-19, 06:20–13:00; 81 5M payload rows | Same symbol/date as the 1M file; regular five-minute timestamp spacing in this export. |
| `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv` | 2026-10-22, 06:30–13:00; 267 1M payload rows | Filename now matches the report header and embedded epoch date; the previous misleading filename was corrected. |
| `data/StrategyReports_XRPN_10022026_5M_EntireDay.csv` | 2026-10-02, 06:30–13:00; 79 5M payload rows | October 2 5M data, but no matching October 2 1M full-day file is established by the files audited here. |
| `data/StrategyReports_HPK_10022026_to_10032026_1M_EntireDay.csv` | 2026-10-01 06:30 through 2026-10-02 12:59; 422 1M payload rows | Two-day HPK file with many gaps; not an XRPN signal-parity pair. |
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

The export files do not include internal ThinkScript states. Therefore they cannot by themselves count:
- candidate trigger and re-arm events;
- 5M raw Permission and latched Permission transitions;
- extension-danger blocks;
- READY activation and expiration;
- the specific reason a candidate failed after 09:00 Arizona.

The already documented October 2 diagnostic screenshot remains the evidence for the October 2 Permission revocation; it should not be conflated with the October 19 OHLCV pair. To attribute READY frequency to a specific gate, the next evidence must pair the same symbol/date/bar with the relevant 1M/5M diagnostic labels or a diagnostic export that records those states. The October 2 1M file/date mismatch should be corrected if the intent is to reconstruct that exact event.

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

**UI wording risk:** result labels such as `READY — HIGH` and `READY — BORING` can be mistaken for the actual ThinkScript `EXECUTION READY` event even though the assessor is diagnostic-only. When this is surfaced, show the existing execution state and quality classification in separate fields; prefer a visibly distinct label such as `QUALITY: HIGH` and never allow the quality result to grant eligibility.

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
