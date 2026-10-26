# Validation Findings
# The analysis and conclusion. The manifest tells us what was tested; the findings explain what the evidence demonstrates and what remains unknown.

## 1. Summary

- **Run ID:** [Manifest run ID]
- **Review status:** `IN_PROGRESS`
- **Overall outcome:** `NOT YET APPROVED`
- **Authoritative replay eligibility:** `BLOCKED`

## 2. Source Integrity

- Source files reviewed: [List exact paths]
- Hashes verified: [Yes/No]
- Changes to original source files: [None, or document changes]
- Missing or unavailable evidence: [List]

## 3. Irregular 1M Interval Classification

For each flagged interval, record:

| Interval start | Interval end | Chart bar observed? | Export payload present? | Classification | Evidence/reference | Confidence/notes |
|---|---|---|---|---|---|---|
| [Timestamp] | [Timestamp] | [Yes/No/Unknown] | [Yes/No] | [Approved category] | [Chart capture/file reference] | [Explanation] |

Allowed classifications:

- `Genuine session gap`
- `Genuine chart gap`
- `Export omission`
- `Unresolved`

Do not classify an interval solely from the timestamp difference. Record the supporting evidence.

## 4. 1M-to-5M Reconstruction

For each 5M window, record:

- 5M bar timestamp
- Number of expected 1M constituent bars
- Number of observed 1M constituent bars
- Calculated OHLCV, when the window is complete
- Independent 5M chart OHLCV
- Comparison outcome: `MATCH`, `ROUNDING ONLY`, `MISMATCH`, or `INCOMPLETE`
- Notes and source references

Do not calculate a definitive reconstructed 5M bar when constituent 1M bars are missing.

## 5. Session-Setting Results

Record results separately for each tested configuration:

- RTH settings and outcome
- ETH settings and outcome
- Bars present in one configuration but absent in the other
- Any unexplained discrepancies

## 6. DST Boundary Results

- Before-transition trading date:
- After-transition trading date:
- Arizona/chart timezone:
- Eastern/chart timezone:
- Verified timestamp relationship:
- Alignment result:
- Unresolved timestamp issues:

## 7. Defects and Limitations

List each unresolved issue, its impact, and the additional evidence needed to resolve it.

## 8. Final Decision

- [ ] Interval classifications reviewed
- [ ] Reconstruction results reviewed
- [ ] RTH/ETH behavior reviewed
- [ ] DST behavior reviewed
- [ ] Source provenance verified
- [ ] Replay eligibility behavior verified

**Decision:** `BLOCKED`, `DIAGNOSTIC ONLY`, or `AUTHORIZED`

**Rationale:** [Evidence supporting the decision]

**Reviewed by / date:** [Record when applicable]

## 9. Evidence-backed update — 2026-10-26

- **October 19 sampled chart parity:** 1M: 20 chart-entered rows (10 `MATCH`, 10 `ROUNDING ONLY`); 5M: 20 chart-entered rows (9 `MATCH`, 11 `ROUNDING ONLY`). These are sample-level results only; 48 1M and 13 5M rows in the current sheets remain without chart values.
- **Timestamp label check:** all 40 chart-entered rows use local chart clock digits matching the payload's Arizona-local minute label. The sheet's `ChartTimestampMeaning` field currently contains `MATCH`, not a documented semantic such as `BAR_OPEN` or `BAR_CLOSE`; timestamp convention is therefore only partially established.
- **Interval classification:** the current sheets leave missing intervals unclassified. Do not call them export omissions or chart gaps without corresponding chart evidence.
- **October 2 symbol mismatch:** the new file at the old `...XRPN..._2.csv` path reported HPK and duplicated existing HPK rows exactly. It has been renamed to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv`. The XRPN 5M October 2 export still lacks an aligned XRPN 1M partner.
- **Decision:** `DIAGNOSTIC ONLY / BLOCKED`. The sampled parity evidence supports the transport's OHLCV mapping for the entered rows, with documented rounding-only differences. It does not authorize full replay, prove missing-bar semantics, prove study-state parity, or diagnose READY frequency.
- **Remaining missing evidence:** exact Thinkorswim chart/study/session configuration; source-file byte hashes; chart observations for gaps; same-bar 1M/5M diagnostic state for READY gates; full GTC_EXT window and actual order fills for Boring Overnight.
