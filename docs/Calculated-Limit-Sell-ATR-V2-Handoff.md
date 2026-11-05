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
