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
- Per-symbol/provider response cache: 10 seconds.
- Browser refresh cadence: 15 seconds while the page is visible.
- 5m permission bars use Alpaca REST historical bars. The 1m execution series uses the confirmed Alpaca WebSocket-persisted series when the stream is subscribed, at least 50 completed bars are available, the latest completed bar is fresh, and its stored SourceTransport is WEBSOCKET; otherwise 1m falls back to REST. The dashboard is not a raw tick stream.

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


## V2 score interpretation and user-facing explanation

The live response now includes `fiveMinute.scoreBreakdown`:
- Six criteria are returned individually with `true`, `false`, or `null` (unavailable).
- The total score is only populated when the V2 engine actually evaluates the symbol.
- Maximum score: 6. Minimum 5/6 is required for 5M long permission.
- 0–2 is low alignment (red); 3–4 is partial alignment (amber); 5–6 is strong alignment (green).
- A green score is not synonymous with an entry. The UI only labels **EXECUTION READY** when `decision.entryAllowed=true`; otherwise it shows 5M permission and the need to wait for 1M confirmation.

Criteria use the same default `V2Config` values as the engine: close > VWAP, close > EMA9, EMA9 > EMA20, EMA20 > SMA50, RSI >= 50, and same-slot RVOL >= 1.50.

## UNKNOWN diagnosis

When no decision object is returned, the UI shows the server's exact `evaluationStatus` and `evaluationReason`. Typical fail-closed causes are:
- `INSUFFICIENT_HISTORY`: fewer than 50 completed 1m bars or 51 completed 5m bars.
- `STALE_MARKET_DATA`: latest completed bar is outside the freshness limit.
- `RVOL_BASELINE_INSUFFICIENT`: fewer than five prior observations for the same 5m regular-session clock slot.
- `OUTSIDE_REGULAR_SESSION`: the intraday engine is not evaluated outside US regular hours.
- Request/provider errors: credentials, entitlement, symbol support, network, or provider response errors.

A symbol-specific cause must be read from that symbol's response; do not infer it from another ticker's status.

## Refresh and streaming decision

The dashboard refreshes every 15 seconds while visible; the API cache is 10 seconds. When the optional Alpaca WebSocket worker is confirmed subscribed and has at least 50 completed 1m bars with a fresh latest observation tagged WEBSOCKET, the live assessment uses that stored 1m series. Otherwise it falls back to REST. The 5m permission series remains REST. The worker handles auth, subscription acknowledgement, reconnects, normal bars, and late updatedBars corrections. The engine consumes completed bars, not raw trade ticks.

## Replay status

The former Replay point slider changed between five hard-coded illustrative snapshots. It did not replay historical market data or recompute V2 decisions. It has been removed from the active controls. True historical decision replay remains open work.


## Freshness diagnostics update — 2026-10-30

The live-assessment response now includes `freshnessDiagnostics` with separate 1m and 5m objects:
- `isFresh`
- `ageSeconds`
- `limitSeconds` (120 for 1m; 360 for 5m)
- `sourceTimestampUtc`
- `expectedCloseUtc`
- `staleTimeframes` array

When stale, `evaluationReason` identifies each failing timeframe and reports its measured age, configured limit, source timestamp, expected close, and feed. The calculation uses expected close (bar timestamp + timeframe). This distinguishes an old 1m source bar from an old 5m source bar rather than collapsing both into one generic message.

A stale reason is not a low score. It means no V2 permission or execution decision was evaluated.

## Historical V2 replay endpoint

`GET /api/market-data/replay?symbol=SPY&providerName=Alpaca&feed=iex&asOfUtc=2026-10-30T14:35:00Z&hasPosition=false`

- Requires explicit UTC `asOfUtc` ending in Z and an explicit feed; this prevents mixing IEX and SIP observations.
- Reads only the latest persisted observation per source timestamp from the current API instance's raw-bar store.
- Uses only 1m/5m bars whose expected close is at or before the as-of timestamp.
- Requires at least 50 completed 1m and 51 completed 5m bars, plus at least five same-time-slot RVOL samples for both current and previous 5m bars.
- Calls the same `V2SignalEngine` with its current default `V2Config`.
- Optional `hasPosition=true` exercises the existing boolean position-management branch. It does not import or infer an actual brokerage position.
- Missing persisted history returns an explicit status; the endpoint does not silently backfill or invent bars.
- Historical config/version archival is not implemented. Replay results therefore represent the current engine/config applied to old data, not necessarily the exact historical code/config that existed at that time.

## Optional Alpaca WebSocket minute-bar collector

Configuration (server-side only):
- `MarketData:Alpaca:StreamEnabled=true`
- `MarketData:Alpaca:StreamSymbols=SPY,RKLX,ASTX,XRPN`
- Existing `MarketData:Alpaca:Feed` selects `iex` or `sip`; the feed must be entitled.

Status:
- `GET /api/market-data/stream/status`
- Worker is disabled by default.
- Authenticates and waits for the authenticated response.
- Sends minute-bar subscriptions and waits for server subscription acknowledgement.
- Persists both normal minute bars (`b`) and late updated bars (`u`) using the append-only observation store.
- Reconnects with exponential backoff and reports last message/bar, persisted count, and last error.
- It does not process raw trade ticks or submit orders.

Important: the live assessment consumes a WebSocket-sourced 1m latest bar only when the subscription is confirmed, at least 50 completed bars are present, the bar is fresh, and the stored transport provenance is WEBSOCKET. Otherwise it falls back to REST. The 5m permission bars remain REST-backed. REST/WebSocket parity and runtime acceptance remain open.
