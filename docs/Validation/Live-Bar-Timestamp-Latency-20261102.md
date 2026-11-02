# Live 1-Minute Bar Timestamp and Latency — 2026-11-02

## User observation

The user reports that the Main Chart's "Latest completed 1M bar close" appears at least one minute behind Thinkorswim.

## Source-code finding

This is partly a timestamp-display ambiguity, not yet evidence of a one-minute provider delay:

- Alpaca minute-bar `MarketBar.Timestamp` is treated by MCC as the **bar start**.
- `LiveSymbolAssessmentService.CompletedBars` includes a bar only when `bar.Timestamp + timeframe <= now`; the current in-progress minute is deliberately excluded.
- The API already returns both `oneMinute.sourceTimestampUtc` (bar start) and `oneMinute.completedAtUtc` (bar start + one minute), as well as `evaluatedAtUtc` and `ageSeconds`.
- Before this fix, the Main Chart printed `sourceTimestampUtc` beneath a metric labelled "Latest completed 1M bar close". That timestamp is the beginning of the candle, so it naturally appears one minute earlier than its close time.
- The live-assessment server cache is 10 seconds and the visible UI refresh interval is 15 seconds. Those settings alone do not explain a full 60-second lag; provider publication latency and the actual returned timestamp must still be measured.

## Change made

The Main Chart now displays the completed bar's close time from `completedAtUtc`, and separately shows the bar's start-to-close interval in Arizona time. The visible metric note explicitly says that the current in-progress minute is excluded. Both production and prototype pages use a new cache-busting version for the shared script.

This is a display correction. It does not change the API's bar-selection policy, freshness limits, technical indicators, V2 decisions, or feed.

## How to determine whether real data latency remains

During regular market hours, for the same symbol, inspect one live-assessment response and record:

| Field | Meaning |
|---|---|
| `evaluatedAtUtc` | Server evaluation time |
| `oneMinute.sourceTimestampUtc` | Start timestamp of the selected 1M bar |
| `oneMinute.completedAtUtc` | Expected close time of that bar |
| `oneMinute.ageSeconds` | Server time minus expected close time |
| `oneMinute.feed` | Actual feed tagged on the selected bar |
| `oneMinute.sourceTransport` | REST or validated WebSocket source |

Calculate actual completed-bar age as `evaluatedAtUtc - completedAtUtc`. A small positive age is normal. A value near or above 60 seconds during an active, liquid regular-session symbol indicates a real provider/collection delay that needs investigation. Compare only completed candles on both platforms; a live, still-forming Thinkorswim candle is not equivalent to MCC's last completed candle.

For Thinkorswim, use the Data Box/crosshair on the same candle and distinguish the candle's start label from its close time. Do not compare the latest live AddLabel value with a historical MCC candle.

## Current INO comparison supplied by user

| Metric | Thinkorswim | MCC |
|---|---:|---:|
| Close | 2.06 | 2.05500 |
| Daily VWAP | 1.89 | 1.87031 |
| ATR14 | 0.02 | 0.01429 |
| Extension | 24.6 ATR (latest/live label, not historical per-bar reading) | Not supplied for the matched timestamp |

The close values are consistent at two-decimal display precision: 2.055 rounds to 2.06. The VWAP and ATR14 readings are not yet an exact parity result because the Thinkorswim values were supplied at two decimals and the exact matched timestamp/date was not included. The TOS 24.6 value was identified as a live/latest label, so it must not be compared with a historical MCC candle.

For historical extension, read the **ExtensionATR plot value in the Data Box on the selected candle**, not the `AddLabel` value, which reports the latest bar.

## Acceptance status

- [x] Correct Main Chart timestamp to display completedAtUtc and the bar interval.
- [x] State that the in-progress minute is excluded.
- [ ] Capture a same-time live assessment JSON during regular market hours and calculate actual completed-bar age.
- [ ] If age is >=60 seconds for a liquid symbol, inspect provider publication, REST/WebSocket source, feed and persisted bar timestamps before changing refresh cadence or freshness thresholds.
- [ ] Capture exact-date, exact-completed-bar Thinkorswim and MCC values to five decimals for Close, Daily VWAP and ATR14; read historical ExtensionATR from the Data Box.

## [Uncertainties]

- No exact date or timestamp was supplied for the user's latest INO comparison.
- No raw API response accompanied the one-minute-delay report, so actual provider lag cannot yet be measured.
- Thinkorswim VWAP and ATR14 values are rounded to two decimals; exact parity remains unproven.
