# Validation Run Manifest
## 	The run's identity card and chain of custody. It records what was captured, from where, under which settings, and which exact files were used.

## 1. Run Identity

#This is a proposed Run ID
# **Run ID:** `XRPN_20261019_1M5M_RTH_01`
- **Status:** `IN_PROGRESS`
- **Symbol:** `XRPN`
- **Trading date:** `2026-10-19`
- **Time range:** Record the actual range covered by the chart bars
- **Purpose:** Classify irregular 1M intervals and validate 1M-to-5M OHLCV reconstruction

## 2. Chart Configuration

- **Platform:** [Record platform]
- **Chart/study:** [Exact study name and version]
- **1M session setting:** [Exact configured value]
- **5M session setting:** [Exact configured value]
- **Chart timezone:** [Exact configured timezone]
- **Extended-hours setting:** [Enabled/disabled]
- **Other relevant settings:** [Record settings that affect bars or timestamps]

## 3. Source Files

For each source file, record:

| Property | Value |
|---|---|
| File path | [Repository-relative path] |
| Source type | [Raw 1M export / raw 5M export / chart parity capture] |
| Capture time | [Timestamp and timezone] |
| Source SHA-256 | [Hash of exact file bytes] |
| Notes | [Any known limitations] |

## 4. Derived Files

| Property | Value |
|---|---|
| Parity file(s) | [Exact filenames] |
| Classification file | [Exact filename, if separate] |
| Findings file | `findings.md` |
| Analysis script/version | [Exact script and commit, if applicable] |

## 5. Timestamp Interpretation

- **Payload timestamp field:** [Exact field or encoding]
- **Timestamp unit:** [Verified unit]
- **Timezone/offset interpretation:** [Evidence-backed interpretation]
- **Chart timestamp interpretation:** [Evidence-backed interpretation]
- **Known ambiguity:** [State explicitly; do not guess]

## 6. Validation Scope

- [ ] Source files preserved unchanged
- [ ] Source hashes recorded
- [ ] Chart settings recorded
- [ ] 1M and 5M ranges overlap
- [ ] Irregular intervals included
- [ ] Classification evidence recorded
- [ ] Complete and incomplete 5M windows distinguished
- [ ] Results reviewed

## 7. Authorization

- **Authoritative replay eligibility:** `BLOCKED`
- **Reason:** Validation gates have not yet been demonstrated complete.
- **Approval reference:** [Commit, review, or decision record when authorized]

## 8. October 19 source/parity evidence reviewed — 2026-10-26

- **Run status remains:** `IN_PROGRESS`; authoritative replay eligibility remains `BLOCKED`.
- **Reviewed 1M source exports:** `data/validation/XRPN/10192026/source/StrategyReports_XRPN_102026_1m_928-947.csv`, `.../StrategyReports_XRPN_102026_1m_1328to1347.csv`, and `.../StrategyReports_XRPN_102226_1m_10192026.csv`.
- **Reviewed 5M source exports:** `.../StrategyReports_XRPN_102026_5m_0920to1055.csv`, `.../StrategyReports_XRPN_102026_5m_1320to1455.csv`, and `.../StrategyReports_XRPN_102226_5m_10192026.csv`.
- **Reviewed chart-entry sheets:** `data/validation/XRPN/10192026/parity/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_4.csv` and corresponding `XRPN_5m...` file.
- **1M chart-entry sheet:** 20 of 68 source rows have chart values entered. Of those 20, 10 are marked `MATCH` and 10 `ROUNDING ONLY`; 48 rows have no chart values.
- **5M chart-entry sheet:** 20 of 33 source rows have chart values entered. Of those 20, 9 are marked `MATCH` and 11 `ROUNDING ONLY`; 13 rows have no chart values.
- Chart timestamps entered for those 20 rows match the local clock digits represented by the payload timestamps. This is evidence for the sampled chart labels, not enough to settle bar-open vs. bar-close/decision-availability semantics across all settings.
- **Not recorded in source files:** exact installed study version for the parity session, chart timezone/session/extended-hours settings, source-file SHA-256 checksums, chart evidence for every missing minute, and per-bar READY/Permission state.
- **October 2 correction:** the newly uploaded file was HPK 1M, not XRPN 1M; it was renamed to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv`. Its 214 rows exactly duplicate the October 2 portion of the existing HPK multi-day report. It does not close the XRPN October 2 pairing gap.
