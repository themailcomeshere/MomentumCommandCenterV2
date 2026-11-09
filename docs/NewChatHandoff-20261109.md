# CURRENT HANDOFF — 2026-11-09 (operator recheck + Top-4 deduplication)

## Operator update — 2026-11-09 (latest supplied validation + Top-4 fix)

### Operator evidence

- Label legend: operator confirmed it looks good.
- Catalyst validation after deployment: raw 6, included 0, excluded 6, conflicts 0, training/holdout 0/0; non-evaluable=2, superseded=3, inadequateReviewEvidence=1. The rationale guard correctly excluded the old assistant-appeal record.
- New review `fbc839fc9c2740c1ad87e6bc641e2a77` is not a valid correction yet. Notes are generic affirmation, not article facts. It supersedes `8405880...` but not the currently weak `f496393...`; its ruleSetVersion is `MCC-CATALYST-HUMAN-VALIDATION-1.0`, not verified prediction provenance. Do not count it.

### New code changes on this branch

- API and historical validation now reject additional generic confirmation language matching the supplied EWTX notes; the Dashboard form mirrors the check. A regression test uses the exact submitted note. This is a heuristic quality screen; it cannot prove factual accuracy.
- MCC Top 4 now deduplicates scanner imports by ticker after score ordering, preserving the highest-scoring row for each ticker. Four repeated rows for one ticker can no longer result in four requests to the same symbol / one symbol in the evidence map.

### Operator next steps

1. Pull/build/test using the standard sequence below; restart the API from the newly built output.
2. Verify MCC Top 4 lists four distinct tickers. Click Fetch Daily + catalyst and verify the status names four distinct tickers and reports four queried symbols. If the import contains fewer than four unique tickers, correct the source import; do not invent symbols.
3. Independently open the original EWTX article and record specific verifiable facts. Determine the label yourself; the prior assistant label is not evidence.
4. Verify the original publisher and actual saved triage ruleset version. The supplied review payload does not establish either. Do not use the human-validation version as the prediction ruleset.
5. Append a correction superseding both active weak EWTX records: `f496393535df431ab275231b57f8530a` and `fbc839fc9c2740c1ad87e6bc641e2a77`. Keep saved prediction `UNCLASSIFIED` and categories `[]`; do not modify audit history.
6. Re-query validation. Expected: no conflict, inadequateReviewEvidence=0 for the active EWTX review, with all other source-unsupported/UNCERTAIN labels still excluded.
7. Continue collecting independently sourced positive and negative examples. Do not promote the classifier until a substantially larger balanced sample, both truth classes in chronological holdout, and reviewer agreement are demonstrated.


**Repository:** themailcomeshere/MomentumCommandCenterV2  
**Working branch:** feature/opportunity-quality-classification  
**Current branch:** [feature/opportunity-quality-classification](https://github.com/themailcomeshere/MomentumCommandCenterV2/tree/feature/opportunity-quality-classification). The evidence-quality code/test commit is recorded in §5.  
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

## 5. Catalyst label legend restored and rationale quality tightened

The dashboard now displays the six-label guide immediately above the human review queue:
- `MATERIAL_RISK`: specific material downside exposure or adverse risk.
- `MATERIAL_EVENT`: significant event, not inherently adverse.
- `NON_MATERIAL`: correct issuer, but no meaningful overnight impact expected.
- `IRRELEVANT`: not actually the issuer/security (including ticker collision).
- `DUPLICATE`: repeated coverage, not a new independent example.
- `UNCERTAIN`: insufficient verifiable evidence; not a positive/negative truth label.

The operator's latest record `f496393535df431ab275231b57f8530a` contains notes: "Rationale is because you said it should be MATERIAL_RISK". The prior deployed validator counted it as the one included review because the text was long enough. That is not an independent human adjudication. The code now requires at least 40 characters of article-specific facts and reasoning, and rejects obvious appeals to an assistant's recommendation. Historical validation applies the same rule, so after deployment this record should be counted under `inadequateReviewEvidence`, and the included unique count should fall from 1 to 0 unless another valid record exists.

Important: the field `ruleSetVersion` should identify the actual automated triage ruleset that produced `UNCLASSIFIED`, not the human-validation schema. The submitted `MCC-CATALYST-HUMAN-VALIDATION-1.0` may therefore be incorrect; inspect the saved assessment's actual ruleset version and do not guess.

### Correct the EWTX review after deployment

1. Pull and test the latest feature branch, restart the API.
2. Open the original Seeking Alpha article via its TradingView syndication page. Read the article body; headline alone is insufficient.
3. Independently decide whether the article describes a specific material downside risk. Do not use my prior recommendation as the reason.
4. Append a new review for EWTX / 117008554, superseding only `f496393535df431ab275231b57f8530a`. Preserve `predictedSeverity=UNCLASSIFIED` and `predictedCategories=[]` because those are the saved triage outputs provided.
5. Set `source` to the original publisher/provider where known (the article is syndicated through TradingView; verify whether Seeking Alpha is the actual publisher). Set `ruleSetVersion` to the saved triage version.
6. Write notes with at least 40 characters of article-specific facts and why they support the label.
7. Re-query validation. Expect zero conflicts, and verify `inadequateReviewEvidence=0` for the corrected active record.

## 6. CI status

- [x] Evidence-quality guard + tests passed: [run 42651528472](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42651528472).
- [x] Generic rationale rejection + regression fixture passed: [run 42655128579](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42655128579).
- [x] Top-4 distinct-symbol deduplication frontend commit passed: [run 42655221198](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42655221198).
- [x] Latest workflow-doc update commit passed: [run 42655348102](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42655348102).
- [ ] Exact latest documentation-only branch head `364ac31a60625a83024ef84d8ed6422742b3931e` has not yet received its own CI run. All code-bearing commits listed above passed.
- [ ] Operator local pull/build/test and API restart remain required before runtime verification of the newest guard and Top-4 behavior.

## 7. Checklist state

- [x] Current Master To-Do was consolidated into a single current-state checklist; detailed engineering acceptance criteria remain in Active-Engineering-Backlog-20261106.md.
- [x] Latest operator validation, generic-rationale rejection, and distinct-symbol Top-4 fix are recorded in the Master To-Do and this handoff.
- [ ] Operator runtime verification and independent EWTX source adjudication remain open.


- [x] EVR ATR parity MATCH, 0 unresolved gaps, 2 verified no-bar intervals.
- [x] CBOE ATR parity MATCH, 0 unresolved gaps, 1 verified no-bar interval.
- [x] EWTX conflicting review pair superseded; conflict count now zero.
- [x] Identified weak placeholder metadata in the superseding EWTX record.
- [x] Implemented placeholder rejection and historical evidence-quality exclusion with tests.
- [x] Restored dashboard label legend and enforced 40-character article-specific rationale; reject assistant-appeal rationales.
- [ ] Operator pull/build/test and runtime verification of the updated article-specific rationale guard.
- [ ] Append corrected EWTX review superseding weak intermediate review `f496393535df431ab275231b57f8530a`.
- [ ] Collect balanced source-backed catalyst reviews and qualify a chronological holdout.
- [ ] Complete the independent open workstreams above.

## [Uncertainties]

- The actual article text/publisher identity and the exact ruleset version saved at the time of triage were not included in the supplied runtime payload. Use the actual values from the article and saved triage record; do not guess.
- CI for the new evidence-quality guard is pending at this handoff.
- No current catalyst precision/recall metrics exist.
