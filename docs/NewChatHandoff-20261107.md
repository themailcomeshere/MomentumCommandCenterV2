# CURRENT HANDOFF — 2026-11-07 evening

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Working branch:** `feature/opportunity-quality-classification`  
**Reviewed branch head:** `b212caf0c2d1df0393fdcc05b5f872c0966aeb67`  
**GitHub Actions:** [Run 42340443270 — success](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42340443270)  
**Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Order submission:** NOT IMPLEMENTED; decision-support-only.

This handoff supersedes the November 6 continuation for current ATR/catalyst evidence. Keep work on the feature branch. Do not switch to PrototypeIndex. Do not modify `master` without explicit direction.

## 1. Latest operator ATR parity output

Run parameters: `warmupBars=99&windowBars=119`.

| Symbol | Timeframe | Status | Persisted observations | Compared bars | Timestamp gaps |
|---|---|---|---:|---:|---:|
| EVR | 5m | `GAPS_PRESENT` | 327 | 20 | 2 |
| CBOE | 5m | `GAPS_PRESENT` | 338 | 20 | 1 |

### EVR — 2026-11-05

- 05:10Z → 14:05Z = 00:10 → 09:05 EST.
- 61 expected 5m slots within 04:00–20:00 ET: 04:00 through 09:00 ET.
- 14:05Z → 14:30Z = 09:05 → 09:30 EST.
- 4 expected slots: 09:10, 09:15, 09:20, 09:25 ET.
- Committed export's first November 5 payload row is 14:05Z (07:05 Arizona / 09:05 ET).

### CBOE — 2026-11-04

- 21:00Z → 23:40Z = 16:00 → 18:40 EST.
- 31 expected slots: 16:05 through 18:35 ET.
- Committed export contains 21:00Z followed by 23:40Z.

The operator reports seeing chart bars beginning at **05:21 for EVR** and **06:15 for CBOE**. AM/PM, exact chart date for each displayed time, and axis timezone have not yet been confirmed. Do not assume these observations correspond to a specific API gap until the chart crosshair/axis confirms it.

**Interpretation:** the gap detector counts scheduled 5m slots in the 04:00–20:00 ET chart window. It cannot prove a TOS candle existed. If TOS visibly has a candle absent from the CSV, that is an export omission. If TOS has no candle, preserve that as no-bar/no-eligible-print evidence; do not synthesize bars. No tolerance changes or imputation.

Detailed record: [ATR and Catalyst Operator Evidence — 2026-11-07](Validation/ATR-and-Catalyst-Operator-Evidence-20261107.md).

## 2. Immediate ATR actions

1. Pull/build the exact feature branch and restart the local API from that build. The exact reviewed head has green CI, but the current local API process SHA is not known from the supplied endpoint output.
2. Open EVR 5m for November 5 and CBOE 5m for November 4 in Thinkorswim. Verify 5m aggregation, extended hours ON, corporate-action adjustment ON, America/Phoenix timezone, and the corrected V2.1 payload study compiled.
3. For EVR, inspect 04:00–09:00 ET and 09:10–09:25 ET. Record the exact first/last visible candle timestamps and clarify whether 05:21 is AM or PM, which date it belongs to, and the displayed timezone.
4. For CBOE, inspect 16:05–18:35 ET on November 4. Record whether candles exist and their exact timestamps. Clarify whether 06:15 belongs to that date/session or another chart period.
5. Use crosshair/data-box timestamps, not visual estimates from the compressed chart axis.
6. If chart candles exist but are absent from the Strategy Report, compile the corrected study, export only the affected symbol/date, inspect embedded epoch payloads, then import genuinely new/corrected rows.
7. If no chart candles exist, do not create zero-volume rows. Document the TOS no-bar behavior; determine whether the parity gate should distinguish absent chart candles from omitted export rows before changing the detector.
8. Re-query EVR/CBOE parity after any new import. Require numeric tolerance AND resolved continuity; keep sell-limit output blocked until every independent gate passes.

No unchanged CSV re-import is needed. The latest committed exports and persisted rows were already inspected.

## 3. Catalyst validation — current result

The operator's local endpoint returned:
- `INSUFFICIENT_LABELS`
- raw reviews 0; included unique reviews 0; excluded 0; conflicts 0
- training 0; holdout 0
- confusion matrices empty; precision/recall/specificity/accuracy null.

This means the local human-review store currently contains no saved reviews. It is not a measured classifier failure and not a pass. No labels may be invented.

### Existing implementation

- Append-only review store.
- Dashboard submission captures saved triage prediction severity/categories.
- `GET /api/market-data/catalyst-reviews/validation` provides de-duplication and chronological later-20% holdout metrics.
- Positive human labels: `MATERIAL_RISK`, `MATERIAL_EVENT`.
- Negative human labels: `NON_MATERIAL`, `IRRELEVANT`.
- `DUPLICATE` and `UNCERTAIN` are excluded.
- Saved predictions `RISK`/`MATERIAL` are positive; `UNCLASSIFIED` is negative.
- This measures fixed keyword triage; it does not train a model and cannot emit catalyst CLEAR.

### Operator data-collection steps

1. In the main Dashboard, select/import the active scanner group in Overnight Qualification.
2. Click **Fetch Daily + catalyst** for the active review group.
3. Open the source article/filing for each returned item.
4. Assign one label per symbol/article and add a short rationale. Use UNCERTAIN when evidence is ambiguous.
5. Include both material and non-material examples; do not submit only positive catalyst stories.
6. Save each human review.
7. Query the validation endpoint again after each batch and retain its JSON.
8. Continue until the chronological holdout has both positive and negative truth classes. That is only a minimum structural requirement, not statistical confidence. Add representative difficult cases and double-review a subset before considering promotion.
9. Never treat no news, a missing feed or no article as a human negative label. Those remain UNKNOWN.

Workflow: [Catalyst Human Review Workflow](Catalyst-Human-Review-Workflow.md).

## 4. Next command sequence

After pulling and building:

```powershell
$base = "http://localhost:53360"

# Confirm the API is the rebuilt instance; do not paste secrets.
Invoke-RestMethod "$base/api/health" | ConvertTo-Json -Depth 10
Invoke-RestMethod "$base/api/providers/status" | ConvertTo-Json -Depth 10

# Re-read existing ATR observations; do not re-import unchanged CSVs.
$evr = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&warmupBars=99&windowBars=119"
$cboe = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m&warmupBars=99&windowBars=119"
$evr | Select-Object status,sourceObservationCount,comparedBarCount,timestampGapCount,timestampGaps,meanAbsoluteError,maximumAbsoluteError |
    ConvertTo-Json -Depth 10
$cboe | Select-Object status,sourceObservationCount,comparedBarCount,timestampGapCount,timestampGaps,meanAbsoluteError,maximumAbsoluteError |
    ConvertTo-Json -Depth 10

# Inspect human-review sample after saving real article adjudications.
Invoke-RestMethod "$base/api/market-data/catalyst-reviews/validation" |
    ConvertTo-Json -Depth 12
```

Do not retry SIP: the known HTTP 403 entitlement restriction remains unchanged. Do not use IEX as a substitute for consolidated/SIP qualification. Do not publish a calculated numeric sell limit while ATR continuity, provider parity, exact reference close, eligible-window coverage and walk-forward runtime gates remain incomplete.

## 5. Completed in this handoff

- Re-read the feature-branch README, current Master To-Do, active handoff, ATR/catalyst validation notes, review workflow, relevant implementation and tests.
- Inspected the committed EVR/CBOE Strategy Report exports around the reported gap dates.
- Verified exact-head GitHub Actions success on `b212caf0c2d1df0393fdcc05b5f872c0966aeb67`.
- Recorded the latest operator ATR output and zero-label catalyst result in the dated evidence note and Master To-Do.
- No application code was changed because the remaining ATR determination requires TOS chart evidence, and human validation requires genuine human-adjudicated source articles.

## [Uncertainties]

- The chart observations “05:21” and “06:15” lack AM/PM, exact chart-date and displayed-timezone confirmation.
- No source proves whether missing ATR timestamps correspond to actual TOS candles or no-trade/no-bar periods.
- The current local catalyst-review store has zero records; no article-level human labels are present.
- Latest pasted ATR response omitted MAE/max-error fields; numeric error metrics are not restated here.
- The local API process SHA and current local browser acceptance are not verified.


## 6. Latest operator update — 2026-11-08 UTC (local Nov 7 evening)

### Verified local runtime
- Local source SHA verified by operator: `bb6a04aedc6f3cbfda1250a7cc9d8d676594419e`.
- API health: `OK`; mode `decision-support-only`; order submission `NOT IMPLEMENTED`.

### ATR inspect exports: wrong coverage windows
Both new files were V2.1 / ATR14 Wilder / Phoenix / extended-hours ON / corporate-action adjustment ON and imported idempotently:
- CBOE: 82 observations, 06:10Z (01:10 ET) through 20:55Z (15:55 ET), 2 interior gaps, 0 appended, 82 identical.
- EVR: 78 observations, 14:30Z (09:30 ET) through 20:55Z (15:55 ET), 0 interior gaps, 0 appended, 78 identical.

Neither file covers the requested interval:
- EVR starts 09:30 ET; requested ranges were 04:00–09:00 and 09:10–09:25 ET.
- CBOE ends 15:55 ET; requested range was 16:05–18:35 ET.
The parser's prior `gapCount` only counted interior discontinuities. A zero does not prove the absent leading/trailing part of a session was inspected.

### Code committed to fix missing diagnostics
- Import parser now returns per-Eastern-session leading/trailing missing scheduled slots (04:00–20:00 ET), separately from interior gaps. It explicitly does not claim those scheduled slots contained actual candles.
- Regression test verifies 09:30–15:55 ET 5m partial export reports 66 leading and 48 trailing scheduled slots.
- Catalyst validation now returns mutually exclusive exclusion reasons: missing saved prediction, non-evaluable label, duplicate same-label review, conflicting-article review.
- Dashboard now displays the exclusion breakdown after validation refresh.
- Tests added for ATR partial coverage and catalyst exclusion breakdown.

Latest code commit: `65ba3b8e42704d722d29b10265b5f8d4606e9d55` (Dashboard display). GitHub Actions for ATR coverage and catalyst diagnostics/tests passed through `72a8cafd04df931d72726c628f38faaed3f9fd77`; latest Dashboard workflow was still in progress when the evidence note was updated. Verify latest-head CI before assuming it is green.

### Catalyst review result
The operator saved three records; endpoint reports raw=3, included=1, excluded=2, conflicts=0. The prior endpoint did not reveal the two exclusion reasons. After pulling latest code, query validation and retrieve the three saved records. Do not assume they are duplicate or uncertain until the new breakdown says so.

### Immediate operator sequence
1. Pull `feature/opportunity-quality-classification`, build/test, restart API. The currently verified local SHA predates the new diagnostics.
2. Re-query ATR import/parity; inspect per-session coverage output.
3. Generate a genuinely new EVR export spanning 04:00–09:25 ET Nov 5 and CBOE export spanning 16:00–18:40 ET Nov 4. Verify payload first/last timestamps before import. Do not re-import the two prior inspect CSVs.
4. Query `GET /api/market-data/catalyst-reviews/validation`; copy `exclusionBreakdown`.
5. Query `GET /api/market-data/catalyst-reviews?limit=20`; provide label, symbol, articleId, predictedSeverity and reviewedAtUtc for all three. Article URLs can be omitted.
6. Continue unique, non-conflicting, evaluable human reviews across both positive and negative truth classes. No holdout metrics yet.

Detailed evidence: [ATR and Catalyst Operator Evidence](Validation/ATR-and-Catalyst-Operator-Evidence-20261107.md).


## 7. Operator update — 2026-11-08 (latest source and catalyst evidence)

### ATR CSV audit
- EVR Nov 4 file: 82 payload bars, 09:05–16:00 ET, one interior gap 09:15→09:30 ET (2 slots); incomplete 04:00–20:00 ET.
- CBOE Nov 4 file: 82 payload bars, 01:10–15:55 ET, interior gaps 01:10→02:35 (16), 02:35→09:00 (76), 09:00→09:25 (4); incomplete 04:00–20:00 ET.
- Use the embedded V2.1 epoch for bar time. Strategy Report Date/Time is an order-event timestamp.
- Code defect fixed: ATR parser already computed sessionCoverage, but the import API response omitted it. The route now returns sessionCoverage. Regression test covers 09:30–15:55 ET => 66 leading + 48 trailing scheduled 5m slots.
- Commits: `abea490af4864613e1f1b60a59d0a7d0194b8816`, `9376d94374bc4840cf1e410fcf09fe8ffb81f349`.

### Catalyst
- ENOV / article 116604852: UNCERTAIN, predicted MATERIAL — correctly excluded.
- ARDT / article 116604839: UNCERTAIN, predicted MATERIAL — correctly excluded.
- EWTX / article 117008554: MATERIAL_RISK, predicted UNCLASSIFIED — included false negative.
- Totals: raw 3, eligible 1, excluded 2, conflicts 0, train/holdout 0/0. No classifier validation pass.
- Alternative primary sources for human inspection only: ENOV official IR Form 8-K https://ir.enovis.com/static-files/5a472069-e8f5-4e5d-9f15-374cbe655d6b ; ARDT SEC Form 8-K https://www.sec.gov/Archives/edgar/data/1756655/000162828026070163/ardt-20261104.htm . Do not relabel the original records unless the source/event match is verified by the reviewer.

### Next
1. Pull feature branch and run `.scripts\Get-Latest-And-Test.ps1`; verify latest SHA and full test result.
2. Restart API and import a partial-session test export only if needed to verify `sessionCoverage` appears in the response. Do not re-import the existing CSVs unchanged.
3. Inspect TOS chart timestamps in actual gaps to classify export omission versus no-bar behavior. Do not synthesize observations.
4. Add additional unique source-verified catalyst reviews across both truth classes; keep inaccessible/ambiguous articles UNCERTAIN.
5. Re-run parity only after genuinely new/corrected TOS payload observations are imported.

**Unresolved:** full extended-hours TOS source continuity, provider SIP entitlement, 99 consecutive-bar parity gates, and catalyst holdout qualification. Order submission remains NOT IMPLEMENTED.


---

# Current handoff addendum — 2026-11-08 (branch head 71c76c853726bbaf35f932b08efa04b4f2289beb)

## Verified state
- Branch: `feature/opportunity-quality-classification`; do not modify master.
- CI: exact prior code/test head `6234b3959ec8de315cbca2ffbd04719d74c4149f`, Actions run [42516614630](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42516614630), conclusion **success**. The current documentation-only commit has not yet been run through CI.
- API import route returns `sessionCoverage` now. It is visible in the JSON response to Swagger `POST /api/strategy-report/atr-import`; no unchanged EVR/CBOE file needs to be re-imported.
- The prior EVR/CBOE inspect exports were idempotent (zero new observations). Source continuity remains unresolved.
- Catalyst records: ENOV and ARDT are UNCERTAIN; EWTX is MATERIAL_RISK vs saved UNCLASSIFIED prediction (one false negative). Included=1, excluded=2, training=0, holdout=0. No validation pass.

## What is required to finish ATR source continuity
The remaining evidence cannot be produced by code or inferred from the CSV. It must come from the Thinkorswim chart itself:
1. EVR 5m, 2026-11-05: inspect 04:00–09:00 ET and 09:10–09:25 ET.
2. CBOE 5m, 2026-11-04: inspect 16:05–18:35 ET.
3. On each chart, confirm America/Phoenix timezone, extended hours ON, corporate-action adjustment ON, 5m aggregation, and corrected V2.1 study.
4. Use the crosshair/data box to record exact timestamps of bars actually visible. Record “no bar” explicitly where appropriate. Do not infer from compressed axis labels.
5. Only if a visible chart bar is missing from the export: export that interval, inspect the embedded `MCC_OHLCV_ATR|v2.1` epoch payload, then import the genuinely new/corrected rows. Do not re-import the unchanged inspect CSVs.
6. Re-run parity with 99 warm-up bars and require both numeric tolerance and continuity. No imputation; no IEX substitution for SIP/TOS source truth.

## What is required to finish catalyst validation
1. Review original ENOV article 116604852 and ARDT article 116604839 independently. Keep UNCERTAIN unless source/event identity is verified; official IR/SEC alternatives are only review aids.
2. Add source-verified material-positive and non-material/irrelevant-negative reviews, with rationale and original source identity. Include triage misses and false alarms; do not label missing news as negative.
3. Six eligible unique records is merely the structural minimum to yield a two-record 20% chronological holdout. The holdout must contain both truth classes. A substantially larger representative sample and double-review agreement are needed before promotion.
4. Re-query `GET /api/market-data/catalyst-reviews/validation` after each batch and preserve the JSON/exclusion breakdown.

## Operator commands after pulling/building
```powershell
$base = "http://localhost:53360"
Invoke-RestMethod "$base/api/health" | ConvertTo-Json -Depth 8
Invoke-RestMethod "$base/api/market-data/catalyst-reviews/validation" | ConvertTo-Json -Depth 12
$evr = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&warmupBars=99&windowBars=119"
$cboe = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m&warmupBars=99&windowBars=119"
$evr | ConvertTo-Json -Depth 12
$cboe | ConvertTo-Json -Depth 12
```

To view `sessionCoverage` without creating observations, use the API's existing import response only when a genuinely new export needs importing. Swagger endpoint: `POST /api/strategy-report/atr-import`. There is currently no documented read-only persisted session-coverage endpoint. Do not import the same EVR/CBOE files merely to see this field.

## [Uncertainties]
- No TOS chart crosshair evidence was supplied for the missing intervals; source omission versus no-bar behavior is unresolved.
- Current catalyst eligible sample is one; holdout qualification is impossible.
- SIP entitlement remains HTTP 403; current entitlement state is not re-verified.
- Current local API process SHA/restart and runtime endpoint responses are not verified by the assistant.


### Catalyst validation safeguard added — 2026-11-08

The validator now requires **six eligible unique reviews** before creating a chronological 20% holdout. With fewer than six it returns `INSUFFICIENT_LABELS`, zero train/holdout counts and a note stating the required minimum. This prevents the previous behavior in which two eligible records could produce a one-row holdout and superficially available metrics. Six is only the mathematical floor for a two-observation holdout; it is not a statistically reliable sample. A 10-record regression still verifies 8 training / 2 holdout metrics. The two-record exclusion test now verifies the insufficient-label response.

- Code commit: `23cc4f647f4cbc4a0920c76a086b3391e3d26891`
- Test commit: `545fd4f330d5e0e47d7d0972e77aa81151791d66`
- Latest CI run: [42518620507](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42518620507) — queued at handoff update; verify before claiming pass.


## 2026-11-08 follow-up — runtime output and API usability

User reports Swagger `POST /api/strategy-report/atr-import` displayed only Execute. Root cause: route consumes `HttpRequest.ReadFormAsync` but had no OpenAPI multipart request schema. Added explicit form schema with required `file`, `atrPeriod`, `atrAverageType`, `chartTimezone`, `extendedHours`, and `corporateActionAdjustment` fields. Also added read-only persisted coverage endpoint:

`GET /api/strategy-report/atr-session-coverage?symbol=EVR&timeframe=5m`

Use same endpoint with `symbol=CBOE`. No import is performed and no rows are changed.

### Runtime parity supplied by operator
- EVR: 327 source observations; 20 compared bars; MAE 0.0000294330; max error 0.0000495349 (inside 0.00005 tolerance); two timestamp gaps; status GAPS_PRESENT.
- EVR gaps: Nov 5 04:00–09:00 ET (61 scheduled 5m slots) and 09:10–09:25 ET (4 slots).
- CBOE: 338 source observations; 20 compared bars; MAE 0.0000203866; max error 0.0000426076 (inside tolerance); one timestamp gap; status GAPS_PRESENT.
- CBOE gap: Nov 4 16:05–18:35 ET (31 scheduled 5m slots).
- Numeric parity passes the configured tolerance for both symbols; timestamp continuity does not. Keep both unqualified.

The prior catalyst endpoint response note says minimum two; that is stale code. Current branch code requires six eligible unique reviews. The current three-review dataset still yields one eligible review, so remains insufficient.

### CI and build failure diagnosis
Run 42518610557 failed because the test still expected `INSUFFICIENT_HOLDOUT_CLASS_COVERAGE` after the new six-review minimum changed the correct status to `INSUFFICIENT_LABELS`. Test expectation was corrected in commit `545fd4f`; run 42518620507 passed. The subsequent import-schema/coverage endpoint commits are under CI run [42519899439](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42519899439), pending at this note.

### Next operator steps
1. Pull latest feature branch and rerun `.\scripts\Get-Latest-And-Test.ps1`. If local build still fails, capture the six actual compiler error lines (CSxxxx plus file/line); the earlier GitHub CI failure was a test assertion, not a build error.
2. Restart API. Swagger should show a multipart form with a file picker and five metadata controls. The endpoint requires all fields.
3. Call read-only session coverage endpoint for EVR and CBOE; do not import old CSVs.
4. Inspect TOS charts for the exact gaps above and report each visible candle timestamp or confirm no candle. Only export/import genuinely missing TOS rows.
5. Collect additional catalyst reviews from independently verified source articles. Keep uncertain articles uncertain.

[Uncertainties]
- Chart evidence still does not establish whether missing ATR slots correspond to chart bars or no-bar intervals.
- Local build's six compiler errors are not included in the user's message, so exact local error cause cannot be diagnosed from the supplied summary alone.
- Latest Swagger/coverage endpoint CI was pending at this handoff revision.
