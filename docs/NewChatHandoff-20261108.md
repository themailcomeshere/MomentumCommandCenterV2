# CURRENT HANDOFF — 2026-11-08

**Repository:** themailcomeshere/MomentumCommandCenterV2  
**Working branch:** feature/opportunity-quality-classification  
**Documentation commits:** Master To-Do 5f4ab9b13a2c8e9d5a5b6599f53d4046eac88e3c; operator evidence 8d578c2e95657c84e0e60964d1ac3e9e4b39c8c7  
**Pre-documentation code head:** 2c9f126067986699ef82b76cb6996d51978433dd  
**GitHub Actions:** [42519967088 — success](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42519967088) on pre-documentation code head.  
**Dashboard:** frontend/index.html at http://localhost:53360/  
**Order submission:** NOT IMPLEMENTED; decision-support-only.

Continue on the feature branch. Do not switch to PrototypeIndex or modify master.

## 1. Operator-verified functionality

- Swagger ATR import file picker: operator confirmed fixed.
- Read-only persisted coverage endpoint works for EVR and CBOE:
  - GET /api/strategy-report/atr-session-coverage?symbol=EVR&timeframe=5m
  - GET /api/strategy-report/atr-session-coverage?symbol=CBOE&timeframe=5m
- Coverage is filtered to V2.1 / ATR14 Wilder / Phoenix / extended-hours / corporate-action-adjusted provenance and reports expected 5-minute slots in the 04:00–20:00 ET session.
- It does not establish whether an absent slot had a TOS candle or eligible trade. No bars are imputed.
- Code/schema + endpoint commit 5720a6e046d621f69813bb83554d6894f30f6a62 passed [run 42519899439](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42519899439). Pre-documentation code head 2c9f126067986699ef82b76cb6996d51978433dd passed [run 42519967088](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42519967088).

## 2. ATR source continuity — current evidence

All times below are Eastern Standard Time.

| Symbol | Date | First → last observed ET | Leading missing | Trailing missing |
|---|---|---|---:|---:|
| EVR | Nov 2 | 09:30 → 16:00 | 66 (04:00–09:25) | 47 (16:05–19:55) |
| EVR | Nov 3 | 09:00 → 16:00 | 60 (04:00–08:55) | 47 (16:05–19:55) |
| EVR | Nov 4 | 09:00 → 16:00 | 60 (04:00–08:55) | 47 (16:05–19:55) |
| EVR | Nov 5 | 00:00 → 15:55 | 0 | 48 (16:00–19:55) |
| CBOE | Nov 4 | 00:45 → 18:40 | 0 | 15 (18:45–19:55) |
| CBOE | Nov 5 | 09:30 → 16:05 | 66 (04:00–09:25) | 46 (16:10–19:55) |

These leading/trailing counts are distinct from interior parity gaps:
- EVR Nov 5: 4 missing slots, 09:10–09:25 ET.
- CBOE Nov 4: 31 missing slots, 16:05–18:35 ET.

The prior EVR 5m and CBOE 5m parity responses were GAPS_PRESENT. Their prior MAE/max error were within 0.00005 tolerance, but continuity did not qualify. The latest operator message did not include fresh numeric parity values.

### Required to finish ATR continuity

1. Open EVR 5m Nov 5 and CBOE 5m Nov 4 in Thinkorswim. Verify 5m aggregation, chart timezone America/Phoenix, extended hours ON, corporate-action adjustment ON, and corrected V2.1 payload study.
2. Use crosshair/data-box timestamps to inspect EVR 04:00–09:00 ET and 09:10–09:25 ET; inspect CBOE 16:05–18:35 ET. Record actual candle presence and exact first/last timestamps.
3. Clarify prior chart observations “EVR 05:21” and “CBOE 06:15” with AM/PM, exact date and displayed timezone. Do not map them to gaps until anchored.
4. Compile corrected V2.1 ThinkScript in TOS. If a chart candle exists but is absent from export, export the full affected interval, verify embedded payload timestamps, then import only genuine new/corrected rows.
5. If no chart candle exists, record no-bar behavior. Do not create zero-volume, interpolated or synthetic observations.
6. Re-run parity after a real correction. Numeric tolerance and timestamp continuity are independent gates. Do not relax either gate to obtain MATCH.

Do not re-import previously inspected unchanged CSVs.

## 3. Catalyst validation — current evidence

Operator's current endpoint response:
- INSUFFICIENT_LABELS
- raw reviews 3; included unique 1; excluded 2; conflicts 0
- training 0; holdout 0
- minimum: 6 unique, non-conflicting reviews with saved triage predictions for a two-observation 20% chronological holdout. This is a structural minimum only, not statistical qualification.

| Symbol | Article ID | Human label | Saved prediction | Validation |
|---|---|---|---|---|
| ENOV | 116604852 | UNCERTAIN | MATERIAL | Excluded; retain UNCERTAIN |
| ARDT | 116604839 | UNCERTAIN | MATERIAL | Excluded; retain UNCERTAIN |
| EWTX | 117008554 | MATERIAL_RISK | UNCLASSIFIED | Included; false negative |

One eligible observation cannot create training or holdout. Do not fabricate labels or relabel ENOV/ARDT based on a different article/source.

### Required to finish catalyst validation

1. In Dashboard Overnight Qualification, fetch and open actual source articles/filings.
2. Save one review per unique symbol/article with a source-verified label and rationale. Include positive truth labels MATERIAL_RISK/MATERIAL_EVENT and negative truth labels NON_MATERIAL/IRRELEVANT. Use UNCERTAIN if evidence is insufficient.
3. Each review must capture the saved triage prediction. Do not count missing-prediction records, duplicates, uncertain labels or conflicts.
4. Re-query GET /api/market-data/catalyst-reviews/validation after each batch and preserve JSON.
5. Reach at least six eligible unique reviews before a structural split can form; then continue to a substantially larger representative set. Ensure the later chronological holdout contains both positive and negative truth classes. Double-review a subset and assess reviewer agreement before classifier promotion.
6. This evaluates fixed keyword triage; it does not train a model. UNKNOWN/no-news remains UNKNOWN and never CLEAR.

Workflow: docs/Catalyst-Human-Review-Workflow.md.

## 4. PowerShell verification after pulling

```powershell
$base = "http://localhost:53360"

Invoke-RestMethod "$base/api/health" | ConvertTo-Json -Depth 10

$evrCoverage = Invoke-RestMethod "$base/api/strategy-report/atr-session-coverage?symbol=EVR&timeframe=5m"
$cboeCoverage = Invoke-RestMethod "$base/api/strategy-report/atr-session-coverage?symbol=CBOE&timeframe=5m"
$evrCoverage | ConvertTo-Json -Depth 12
$cboeCoverage | ConvertTo-Json -Depth 12

$evrParity = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&warmupBars=99&windowBars=119"
$cboeParity = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m&warmupBars=99&windowBars=119"
$evrParity | Select-Object status,sourceObservationCount,comparedBarCount,timestampGapCount,timestampGaps,meanAbsoluteError,maximumAbsoluteError | ConvertTo-Json -Depth 12
$cboeParity | Select-Object status,sourceObservationCount,comparedBarCount,timestampGapCount,timestampGaps,meanAbsoluteError,maximumAbsoluteError | ConvertTo-Json -Depth 12

$reviews = Invoke-RestMethod "$base/api/market-data/catalyst-reviews?limit=20"
$reviews | ConvertTo-Json -Depth 12
Invoke-RestMethod "$base/api/market-data/catalyst-reviews/validation" | ConvertTo-Json -Depth 12
```

Do not import unchanged files. Do not retry SIP; known HTTP 403 entitlement remains parked. IEX is not a substitute for consolidated/SIP qualification. Numeric sell-limit output remains fail-closed while independent gates are incomplete.

## 5. Checklist

- [x] Swagger multipart file picker fixed and operator verified.
- [x] Read-only EVR/CBOE session coverage endpoint operator verified.
- [x] Per-session leading/trailing coverage reconciled against known interior parity gaps.
- [x] Six-eligible-review structural minimum documented and enforced in code.
- [x] Three catalyst reviews reconciled: two UNCERTAIN excluded, one eligible false negative.
- [ ] Inspect exact TOS chart candles in all requested intervals.
- [ ] Compile corrected V2.1 ThinkScript in TOS.
- [ ] Correct export only if TOS shows payload omissions; re-run ATR parity to MATCH.
- [ ] Collect representative source-verified positive/negative catalyst reviews and achieve meaningful chronological holdout metrics.
- [ ] Double-review a subset and assess agreement before classifier promotion.

## [Uncertainties]

- Actual TOS candle presence in absent intervals: Data not found in sources.
- Fresh numeric ATR MAE/max-error from latest operator recheck: Data not found in sources.
- Prior 05:21 / 06:15 observations lack complete date/timezone/AM-PM anchors.
- Catalyst metrics remain unavailable because only one review is eligible.
