## 2026-11-05 — V2 import pipeline and ATR alignment defect

Branch: `feature/opportunity-quality-classification`

### Implemented in the application

- `StrategyReportAtrV2Parser`: dedicated parser for the 11-field `MCC_OHLCV_ATR|v2|` payload. Supports 1m/5m/15m/30m/1h/4h, parses epoch milliseconds and invariant decimals, rejects malformed payloads/duplicate timestamps/mixed symbols or timeframes, and reports gaps without filling them. Existing V1 parser is unchanged.
- `JsonlThinkorswimAtrObservationStore`: separate append-only store at `MCC_TOS_ATR_STORE_PATH` or the local application data default. Persists chart provenance and corrections. It does not merge with provider OHLCV.
- API:
  - `POST /api/strategy-report/atr-import` — multipart CSV upload plus explicit metadata: `atrPeriod`, `atrAverageType=Wilder`, `chartTimezone`, `extendedHours`, `corporateActionAdjustment`.
  - `GET /api/strategy-report/atr-observations?symbol=EVR&timeframe=1m`
  - `GET /api/strategy-report/atr-parity?symbol=EVR&timeframe=1m`
- Dashboard StrategyReport upload detects V2 payloads, persists them separately, and displays import counts, gaps, provenance and parity status. V1 files continue through the existing parse-only route.
- `ThinkorswimAtrParityService` version `MCC-WILDER-ATR-PARITY-1.0` uses 99 warm-up observations, at least 20 comparison bars, and a 0.00005 absolute tolerance (half of the four-decimal exported ATR precision). It compares the server Wilder formula with the TOS ATR on the same imported TOS OHLCV bars. It does not validate Alpaca/SIP feed parity.
- CI build and tests passed on `e87143af82f618b13c8aa07d826757665ae7b63f` (run [42116898016](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42116898016)). Later source/test/UI edits require a new exact-head CI run.

### Critical source defect found from the November 3 exports

The V2 ThinkScript originally used:

```thinkscript
def trueRange = TrueRange(high, close[1], low);
```

Thinkorswim's documented `TrueRange(high, close, low)` already references the previous close. Passing `close[1]` therefore shifts the close reference back one additional bar. The source has been corrected to:

```thinkscript
def trueRange = TrueRange(high, close, low);
```

Official reference: https://toslc.thinkorswim.com/center/reference/thinkScript/Functions/Tech-Analysis/TrueRange

Measured against the November 3 1m export after 99 warm-up observations:
- Correct standard previous-close Wilder calculation vs exported ATR: mean absolute error ~0.06370; maximum ~0.14788.
- Reproduction of the extra previous-close shift vs exported ATR: mean absolute error ~0.00002545; maximum ~0.00004974.

The old export is a regression fixture for the defect, not a valid parity certification. The corrected source has not yet been compiled and re-exported by the operator.

### Actual export coverage

| Export | Numbered Strategy Report rows | ATR payload observations | Timestamp gaps | Parity readiness |
|---|---:|---:|---:|---|
| EVR 1m, Nov 3 | 692 | 346 | 48 | Enough rows for a diagnostic; old study formula is misaligned, so NOT_MATCH |
| EVR 5m, Nov 3 | 162 | 81 | 1 | INSUFFICIENT_WARMUP; below 99 warm-up observations |

Each source bar creates a transport-close order row and an ATR payload order row; the payload observation count is the number of source bars. Gap duration is missing interval time, not the full elapsed time between the two observed bars.

### Required operator sequence

1. Pull the latest `feature/opportunity-quality-classification` branch.
2. Replace the Thinkorswim study with the corrected `MCC_OHLCV_ATR_V2.txt`; compile it. Verify selected aggregation and host chart both match.
3. Keep ATR length 14 and Wilder. Confirm Phoenix timezone, extended-hours ON and corporate-action adjustment ON.
4. Export EVR 1m and 5m again. For the 5m export include at least 99 completed warm-up bars plus at least 20 comparison bars (119 or more observations total). Include the corrected-study bars only.
5. In MCC Dashboard → StrategyReport upload, upload each CSV separately. The import panel defaults to the known chart settings but allows explicit confirmation.
6. Inspect import counts and gap samples. Query/inspect the parity status shown by the panel or:
   - `GET /api/strategy-report/atr-parity?symbol=EVR&timeframe=1m`
   - `GET /api/strategy-report/atr-parity?symbol=EVR&timeframe=5m`
7. Require `MATCH` on at least 20 timestamp-matched bars before calling ATR parity verified. A mismatch, insufficient warm-up, missing timestamp or mixed provenance blocks numeric sell-limit output.

### Sell-limit and other remaining blockers

The import and ATR algorithm-parity diagnostic are implemented, but sell-limit orchestration is not yet production-ready. It still needs:
- Complete 20/20 exact GTC_EXT SIP history and a verified effective SIP entitlement.
- Exact prior-session reference close and current verified close.
- Same-source continuous 5m warm-up history and corrected-study ATR `MATCH`.
- Provenance-bound quantile calculation and fail-closed UI explanation.
- Time-separated walk-forward target validation, target-touch/adverse-excursion outcomes.
- Actual broker fills, quantities, fees and timestamps kept separate from theoretical OHLCV touches.

Current effective Alpaca feed remains IEX; prior SIP access returned 403. Do not change the feed and assume the entitlement is active. No numeric sell limit is approved by this implementation.

## 2026-11-05 — ThinkScript compiler error correction

The first aggregation-selector revision did not compile in Thinkorswim. User reported `AggregationPeriod.ONE_MIN` unknown, string-parameter incompatibilities, and `Expected double`.

Correction committed in `39cacef3a68f88e2e3fc33467d260ac00f175b37`:
- Use `AggregationPeriod.MIN` for one-minute aggregation.
- Remove `def timeframeLabel`: ThinkScript `def` is numeric and cannot store strings.
- Inline the conditional timeframe string in `AddLabel` and `AddOrder` expressions.
- Keep the selected-vs-host-chart aggregation guard and supported 1m/5m/15m/30m/1h/4h choices.

This is a source correction only. Thinkorswim compilation and Strategy Report payload validation remain unverified. GitHub .NET CI does not compile ThinkScript.

## Application implementation status — explicitly open

The requested pipeline is not implemented end-to-end. Existing components are:
- `StrategyReportCsvParser` and `ThinkorswimOhlcvStrategyReportReader` support the V1 OHLCV transport, not the V2 ATR payload.
- `BoringOvernightAtrCalculator` calculates Wilder ATR from supplied bars but its seed/prefetch behavior has not been matched to TOS.
- `BoringOvernightSellLimitCalculator` calculates a research quantile from supplied observations but is not wired to a verified live reference close, persisted TOS ATR, complete SIP windows, or broker fills.

Remaining implementation checklist:
- [ ] V2 11-field Strategy Report parser, strict validation, V1 compatibility and automated fixtures.
- [ ] Separate append-only, idempotent Thinkorswim ATR observation store and import API.
- [ ] Versioned server Wilder ATR seed/warm-up contract.
- [ ] Matched-timestamp ATR parity service with evidence-based tolerance.
- [ ] Provenance-bound sell-limit orchestration API and fail-closed gate aggregation.
- [ ] UI for import status, exact missing timestamps, ATR parity, history coverage, quantile/sample size and calculation provenance.
- [ ] Effective SIP entitlement plus 20/20 exact GTC_EXT windows. Current effective feed evidence is IEX; SIP is not established.
- [ ] Time-separated walk-forward evaluation, target touches and adverse excursion.
- [ ] Separate actual broker fills/fees/quantities from theoretical OHLCV touches.

No numeric limit is valid until all required evidence gates pass. Keep `DATA?`/`NOT VERIFIED` rather than promoting partial research calculations.

# Calculated Limit Sell — ATR V2 handoff (2026-11-05)

Branch: feature/opportunity-quality-classification

## Thinkorswim export finding
The existing docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_V1.txt is a strategy-report transport for completed-bar OHLCV only. Its payload contains symbol, aggregation, epoch timestamp, O/H/L/C and volume. It does not export ATR or chart study metadata. Replacing V1 in place would risk breaking the existing report reader/parser contract.

## New artifact committed
Added docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt (commit fd43b9d3c88f0cb73a652ec70f37dfc689c2ebc6).
- Separate versioned 5-minute strategy-report transport.
- Adds ThinkScript Wilder ATR(14) value for the same completed source bar.
- Includes 5m chart guard and configurable date/time window.
- V1 remains unchanged.
- V2 is a DRAFT until compiled and verified in Thinkorswim. The repository cannot establish chart extended-hours or adjustment settings; record those manually.
- Do not use the V2 payload until Strategy Report preserves the complete dynamic order name and ATR values match the chart plot for consecutive completed bars.

## Remaining application work — NOT IMPLEMENTED
- Update ThinkorswimOhlcvStrategyReportReader to recognize V2 as a separate schema, preserve V1 compatibility, and add parser fixtures/tests.
- Build persisted-data sell-limit orchestration API: exact 20/20 SIP GTC_EXT windows, exact persisted reference close, continuous same-source 5m bars, ATR warm-up, TOS comparison and server-derived parity result.
- Add API/UI provenance and fail-closed reason states.
- Add chronological walk-forward validation and separate broker execution reconciliation.

Do not claim the calculation pipeline is complete. No numeric sell limit is valid while SIP coverage and ATR parity are unverified.

## Operator instructions for ATR export
1. In Thinkorswim, add MCC_OHLCV_ATR_V2 to an EVR 5-minute chart.
2. Load at least 99 consecutive completed 5-minute bars before the first comparison bar.
3. Set the desired date/time range and wait for all chart bars to load.
4. Record chart symbol, aggregation, timezone, extended-hours on/off, corporate-action adjustment mode, and ATR study settings (period 14, average type Wilder's).
5. Export the Strategy Report. Confirm the order-name payload includes all fields through ATR14 and compare at least 20 consecutive rows with the chart's OHLCV and ATR plot.
6. If Thinkorswim rejects the script or truncates payload names, stop; do not import partial/corrupt rows. Send the compile error or report sample for correction.

## [Uncertainties]
ThinkScript compilation and report field preservation are unverified. Exact equivalence of the exported Wilder ATR seed/prefetch behavior to the user's installed ATR study must be tested against chart values. No SIP GTC_EXT sample or current reference close is available.


## 2026-11-05 — selectable chart aggregation added
- Updated `MCC_OHLCV_ATR_V2.txt` with a `chartAggregation` input, supported periods 1m/5m/15m/30m/1h/4h, dynamic timeframe label and payload, and a fail-closed chart/input mismatch guard.
- Important: ThinkScript cannot change the host chart's aggregation. Select the period in the study input AND set the chart's aggregation control to the same period. The study now blocks export if they differ, so source-code edits are no longer needed when switching among supported periods.
- ATR length remains 14 and Wilder remains the average type. Date/time filters and manual ETH/adjustment recording remain.
- The updated script has not yet been recompiled in Thinkorswim after this change; validate every aggregation you intend to use before importing its reports.
