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
