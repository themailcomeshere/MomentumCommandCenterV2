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
