# Live Symbol Assessment — MCC

**Date:** 2026-10-30  
**Status:** Implemented in `feature/opportunity-quality-classification`; runtime/browser validation pending.

## Endpoint

`GET /api/market-data/live-assessment?symbol=XYZ&providerName=Alpaca`

Returns provider/feed, evaluation timestamp, overall data status, separate 1m and 5m source timestamps and freshness, technical indicator values, recent 1m chart bars, V2 score/permission/execution state, and explicit evaluation reason.

## Source retrieval and persistence

- 1m historical bars: 3 calendar-day lookback.
- 5m historical bars: 21 calendar-day lookback.
- Only completed bars are used for indicators and decisions.
- Returned observations are appended to the existing market-bar observation store.
- Per-symbol/provider response cache: 30 seconds.
- Browser refresh cadence: 60 seconds while the page is visible.
- Current implementation uses Alpaca REST historical bars; it is not a websocket or tick stream.

## Indicators

Calculated independently for 1m and 5m:
- EMA 9
- EMA 20
- SMA 50
- RSI 14 (Wilder smoothing)
- MACD 12/26 with 9-period signal
- ATR 14 (Wilder smoothing)
- Regular-session VWAP (volume-weighted typical price, regular-session bars only)

Extension is the non-negative 1m close-to-EMA9 distance in 1m ATR units:
`max(0, (1m close - 1m EMA9) / 1m ATR14)`.

## 5m relative volume

Current 5m bar volume is divided by average volume for the same 5-minute regular-session clock slot across up to ten prior sessions. At least five prior session samples are required. This is an MCC-calculated same-time-of-day baseline, not a provider-supplied consolidated RVOL field. No baseline means no 5M permission evaluation.

## V2 decision evaluation

The existing `V2SignalEngine` receives:
- current completed 1m BarSnapshot;
- current completed 5m BarSnapshot;
- previous completed 5m BarSnapshot;
- regular-session market phase;
- evaluation timestamp.

The service invokes the engine with `hasPosition=false`. Output includes V2 5M score/permission, 5M structure, 1M structure/momentum, entryAllowed, runnerAllowed, state/action, extension and reason. The 1M execution label is entry-context only; this does not evaluate an existing position.

## Fail-closed requirements

- Minimum 50 completed 1m bars.
- Minimum 51 completed 5m bars to provide current and previous 5m snapshots.
- 1m source freshness: no more than 120 seconds after expected close.
- 5m source freshness: no more than 360 seconds after expected close.
- Minimum five same-clock-slot prior 5m RVOL samples.
- Intraday V2 decision is evaluated only during 09:30–16:00 Eastern on weekdays.
- Insufficient history, stale bars, insufficient RVOL baseline or outside-session requests return explicit status/reason and no decision object.

## UI behavior

- Every visible Red Zone candidate (manual or scanner-derived) requests this endpoint.
- Red Zone displays V2 score, extension, 1m/5m freshness, 5M permission, 1M execution state/reason and a 1m sparkline.
- Main Chart consumes the same endpoint and renders up to 180 completed 1m bars.
- Main Chart and Red Zone refresh every 60 seconds.
- Server-side cache reduces duplicate requests.
- Manual pins remain browser-local; market data assessment is independent of scanner-row membership.

## Known boundaries

- REST polling is near-live but not tick-level streaming.
- Alpaca feed/entitlement governs source coverage.
- No NBBO spread, order book, halt, catalyst/news classification, Thinkorswim studies, broker positions or order execution.
- Existing V2 thresholds are used as-is; this implementation does not recalibrate them.
- Browser runtime acceptance and latest-head CI must be completed before treating the implementation as validated.
