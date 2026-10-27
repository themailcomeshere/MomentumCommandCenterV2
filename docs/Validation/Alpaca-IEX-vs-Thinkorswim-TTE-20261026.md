# TTE Alpaca/IEX vs Thinkorswim StrategyReport — 2026-10-26

## Scope and method

Compared the 77 explicit `MCC_OHLCV|v1` payload rows parsed from `data/StrategyReportUploadResponse.txt` (uploaded Thinkorswim StrategyReport: `StrategyReports_TTE_5M_10262026.csv`) against the saved Alpaca historical-bars response in `data/HowToTestExtendedHoursCoverage_TTE_5m.txt`.

The source timestamps were normalized to UTC and matched by exact five-minute timestamp. For matched timestamps, compared open, high, low, close, and volume. The provider response declares `feed: iex`.

## Findings

- StrategyReport payload rows: **77**.
- Alpaca/IEX five-minute bars: **75**.
- Exact timestamps represented in both: **74**.
- StrategyReport timestamps absent from the saved IEX response: **3** — 2026-10-26 13:55Z, 16:40Z, and 17:15Z.
- Matched timestamps with at least one OHLCV mismatch: **74 of 74**.
- Median absolute close difference on matched timestamps: approximately **$0.02**; maximum observed absolute close difference: **$0.23**.
- Sum of payload volumes on the 74 matched timestamps: **705,686.6302**.
- Sum of IEX volumes on the same 74 timestamps: **45,441**. Payload volume is approximately **15.53×** the IEX volume over those matched timestamps.

## Interpretation

This is a material parity failure, not a minor rounding difference. The saved response is explicitly IEX-only, while the Thinkorswim chart/strategy payload appears to represent a broader chart data stream. An IEX-only feed is not a valid substitute for consolidated SIP-style OHLCV parity unless the user intentionally wants IEX-only bars.

Potential causes include different market-data universes/exchange coverage, aggregation/session settings, and/or payload timestamp semantics. The evidence proves the saved files disagree; it does not, by itself, isolate one root cause. The three missing timestamps may reflect absent IEX bars, session/aggregation behavior, or differences in how the Thinkorswim strategy emits payloads.

## Required next validation

1. Confirm the Thinkorswim chart is configured for the same session window (including extended hours) and five-minute aggregation.
2. Repeat the API request using an entitled consolidated feed if available; do not assume an IEX subscription provides consolidated-market volume.
3. Preserve the exact API response and chart export as separate immutable files.
4. Re-run the timestamp/OHLCV comparison after matching feed, session, and aggregation settings.
5. Keep ChartTimestamp blank in the parity template until a chart-bar timestamp is explicitly supplied; StrategyReport `Date/Time` remains an order-event timestamp.

## Decision

**Do not mark TTE parity as validated.** The current IEX response is suitable only as a separate IEX-feed observation dataset, not as a parity replacement for the supplied Thinkorswim StrategyReport.
