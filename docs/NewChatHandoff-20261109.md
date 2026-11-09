# CURRENT HANDOFF — 2026-11-09

**Repository:** themailcomeshere/MomentumCommandCenterV2  
**Working branch:** feature/opportunity-quality-classification  
**Current branch head:** `0ba7f2ca3babc349eaba40052c97c76e8215df28`  
**Dashboard:** frontend/index.html at http://localhost:53360/  
**Order submission:** NOT IMPLEMENTED; decision-support-only.

Continue on the feature branch. Do not switch to PrototypeIndex or modify master.

## 1. Operator-verified ATR runtime acceptance

The operator pulled/rebuilt/restarted and supplied the following actual runtime responses for `MCC-WILDER-ATR-PARITY-1.2`:

| Symbol | Status | Observations | Compared | Unresolved gaps | Verified no-bar intervals | MAE | Maximum error |
|---|---|---:|---:|---:|---:|---:|---:|
| EVR 5m | MATCH | 327 | 20 | 0 | 2 | 0.0000294330206093994 | 0.0000495349247567753 |
| CBOE 5m | MATCH | 338 | 20 | 0 | 1 | 0.0000203865947752492 | 0.0000426076307464691 |

Tolerance is 0.00005. Both values are inside tolerance. The two EVR gaps (61 + 4 scheduled slots) and one CBOE gap (31 slots) correspond exactly to operator-confirmed no-candle intervals. No bar was synthesized. ATR runtime acceptance is complete for these exact source windows; unrecognized future gaps remain blocking.

## 2. Catalyst EWTX conflict and current data-quality issue

The operator appended review `8405880abfd14b3f83c32e380ced7358` for EWTX article `117008554` and superseded:
- `5a89f75f1cbf4bedb75423dc964016b9` (MATERIAL_RISK)
- `b485426152ba4668a256ff284b527e9d` (MATERIAL_EVENT)

The subsequent validation result:
- raw reviews 5
- included unique reviews 1
- excluded 4
- conflicts 0
- training/holdout 0/0
- non-evaluable labels 2
- superseded reviews 2

This successfully resolves the conflict. The retained human label is MATERIAL_RISK and the saved rule prediction is UNCLASSIFIED, i.e. a false negative. Do not change the prediction to match the label.

However, the submitted new record used literal placeholders:
- source: `string`
- ruleSetVersion: `string`
- notes: `string`
- predictedCategories: `STRING`

This is not acceptable audit evidence. The review store was append-only, so the record must be corrected with another appended review, superseding review ID `8405880abfd14b3f83c32e380ced7358`. Preserve the original two superseded records and this weak intermediate record in the audit trail.

### New code change

Catalyst review submission now rejects placeholder values (`string`, `test`, `todo`, `placeholder`, `n/a`, `na`) in source, ruleset version, notes and predicted categories. A supersession requires a meaningful rationale of at least 12 characters. Validation excludes older saved records with missing/placeholder source, ruleset version or rationale, exposing `inadequateReviewEvidence` in `exclusionBreakdown`. Regression tests cover both append-time rejection and historical record exclusion.

### Operator correction after deployment

1. Pull the latest feature branch, build/test, stop the old API, and restart it.
2. Review the source article again if needed. Submit a new POST review for EWTX / 117008554:
   - label remains the human-adjudicated `MATERIAL_RISK` unless your source review changes it;
   - `predictedSeverity` remains `UNCLASSIFIED`;
   - `predictedCategories` should be `[]` unless actual categories were saved by triage;
   - `source` must name the actual publisher/provider;
   - `ruleSetVersion` must be the actual ruleset version used at triage time, not a placeholder;
   - `notes` must explain the article-level evidence and why MATERIAL_RISK is appropriate;
   - `supersedesReviewIds` must contain only `8405880abfd14b3f83c32e380ced7358`.
3. Re-query `GET /api/market-data/catalyst-reviews/validation`. Expect no conflict and the corrected record to be eligible only if all evidence fields are valid.
4. Keep ENOV and ARDT UNCERTAIN unless their original articles support independent source-backed adjudications. Do not force them into positive/negative classes.
5. Continue collecting source-verified positive and negative examples. Six eligible unique reviews is only the structural floor for a two-row chronological holdout, not statistical qualification.

## 3. Reliable update/build sequence

The operator reports this explicit sequence worked when the update script did not switch/pull correctly:

```powershell
git status --short --branch
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
.\scripts\Get-Latest-And-Test.ps1
```

Run from the repository root. Do not force-reset or discard uncommitted changes. The script runs restore, Release build, tests, and prints the source SHA. Record the local SHA and test totals.

## 4. Remaining project priorities

1. **Catalyst data quality and sample collection:** correct EWTX record metadata, then collect a substantially larger balanced sample with a chronological holdout containing both truth classes. No classifier promotion.
2. **Boring Overnight:** resolve official auction vs first eligible trade and exact scanner buy/sell endpoints. SIP remains parked behind the known HTTP 403; do not retry or substitute IEX.
3. **ThinkScript parity:** source review is not TOS compilation/runtime acceptance. Compile the relevant studies and run matched completed-bar permission/EOD checks across DST when scheduled.
4. **Dashboard/browser:** execute browser diagnostics and record screenshot/CDP evidence. Run stream collector during a confirmed regular session only.
5. **Opportunity Quality:** gather time-separated candidate snapshots/outcomes and calibrate ranking before treating it as operational.
6. **Walk-forward:** runtime qualification remains blocked by the known SIP entitlement issue and required complete eligible history.

## 5. CI status

- [x] Evidence-quality code and tests passed GitHub Actions on code/test commit `973aaf55482ea5c188071a4567d7b58fab019d92`: [run 42647886829](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42647886829). This run includes frontend syntax validation, Release build and full test suite.
- [ ] Documentation-only commits after that code/test commit are queued for exact latest branch-head CI: [run 42648083688](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42648083688).
- [ ] Operator local pull/build/test and API restart remain required before runtime verification of the new metadata guard.

## 6. Checklist state

- [x] EVR ATR parity MATCH, 0 unresolved gaps, 2 verified no-bar intervals.
- [x] CBOE ATR parity MATCH, 0 unresolved gaps, 1 verified no-bar interval.
- [x] EWTX conflicting review pair superseded; conflict count now zero.
- [x] Identified weak placeholder metadata in the superseding EWTX record.
- [x] Implemented placeholder rejection and historical evidence-quality exclusion with tests.
- [ ] Operator pull/build/test and runtime verification of evidence-quality guard.
- [ ] Append corrected EWTX review superseding the weak intermediate review.
- [ ] Collect balanced source-backed catalyst reviews and qualify a chronological holdout.
- [ ] Complete the independent open workstreams above.

## [Uncertainties]

- The actual article text/publisher identity and the exact ruleset version saved at the time of triage were not included in the supplied runtime payload. Use the actual values from the article and saved triage record; do not guess.
- CI for the new evidence-quality guard is pending at this handoff.
- No current catalyst precision/recall metrics exist.
