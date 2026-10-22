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
