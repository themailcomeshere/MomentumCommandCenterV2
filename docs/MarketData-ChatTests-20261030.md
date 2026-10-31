# MarketData ChatTests — 2026-10-30

## Purpose

This note records the four uploaded live-assessment JSON captures and the stream-status response supplied during the MCC review. It distinguishes post-close stale results from the earlier in-session RKLX stale observation.

## Uploaded JSON evidence

All four files are in `data/ChatTests/`:

| Symbol | Evaluated UTC | 1M source UTC | 1M age / limit | 5M source UTC | 5M age / limit | 1M source |
|---|---|---|---:|---|---:|---|
| SPY | 20:08:55 | 19:59 | 536s / 120s | 19:55 | 536s / 360s | REST |
| RKLX | 20:11:51 | 19:59 | 711s / 120s | 19:55 | 711s / 360s | REST |
| ASTX | 20:12:21 | 20:02 | 561s / 120s | 20:00 | 441s / 360s | REST |
| XRPN | 20:12:46 | 19:59 | 766s / 120s | 19:55 | 766s / 360s | REST |

Each returned `evaluationStatus=STALE_MARKET_DATA`; each lists both timeframes as stale. No V2 permission or execution decision was evaluated. These four captures were generated after the regular-session close on 2026-10-30 (20:00 UTC / 1:00 PM Arizona). The post-close timing explains why bars can stop advancing, but does not explain the earlier in-session RKLX failure below.

## Separate RKLX in-session observation

The user also supplied an RKLX card showing:

- 1M: FRESH, 22 seconds
- 5M: STALE, 982 seconds, limit 360 seconds
- Last 5M source: 19:05 UTC; expected close: 19:10 UTC
- Assessment was around 19:26 UTC on 2026-10-30, before the 20:00 UTC regular-session close.

This is a genuine in-session 5M freshness failure in the supplied sample. The exact cause is not established by the available JSON. Possible causes such as IEX trade sparsity, provider response staleness, or collector/provider timing must not be asserted without a same-time REST response and per-symbol stream diagnostics. **Data not found in sources.**

## WebSocket status supplied by user

At 19:40:01 UTC:

- enabled/configured/connected: true
- state: SUBSCRIBED
- feed: IEX
- configured symbols: SPY, RKLX, ASTX, XRPN
- persistedBars: 2
- lastError: null

This proves connection and initial subscription acknowledgement, not that each symbol has fresh WebSocket bars or that the assessment selected WebSocket. All four uploaded assessments report 1M `sourceTransport=REST`.

The WebSocket worker starts with the configured symbols and only stores bars received after it starts. The live assessment requires at least 50 completed 1M bars, a fresh latest bar, and a WebSocket-sourced latest observation. Existing persisted REST history can supply the older bars; a new WebSocket bar must become the latest observation before the source switches. 5M permission remains REST-backed.

## Implemented follow-up

- Added dynamic `POST /api/market-data/stream/subscribe?symbol=XYZ`.
- MCC requests subscriptions for the selected symbol and Red Zone candidates. `StreamSymbols` is now an initial seed, not a per-ticker maintenance list.
- Dynamic subscriptions are retained in API memory and re-sent after reconnect; they are not persisted across API restarts. The front end re-requests visible symbols after reload.
- Stream status now exposes acknowledged `subscribedSymbols`, per-symbol received bar counts, last bar timestamps, and separate received/persisted totals.
- The live assessment remains REST fallback until the WebSocket latest-bar/history/freshness gates pass. 5M remains REST.
- Added opt-in Red Zone sound alert for a newly appearing ticker in the four visible priority slots. The user must enable sound with a click because browsers require a user gesture. Live Alpaca discovery refreshes every 30 seconds while the Live Discovery scanner group is selected. The Red Zone section occupies the full available row and uses a 4-column desktop layout.

## Required runtime acceptance

During regular market hours, after deploying the changes:

1. Select SPY, RKLX, ASTX, and XRPN or refresh Alpaca market movers.
2. Open `/api/market-data/stream/status`.
3. Verify all four appear in `subscribedSymbols`.
4. Verify `barsBySymbol` advances independently for each symbol.
5. Check each `/api/market-data/live-assessment?symbol=...` response.
6. Confirm `oneMinute.sourceTransport=ALPACA_WEBSOCKET_LATEST_BAR` only after its guards pass.
7. Confirm 5M remains fresh during regular hours; a stale 5M response still blocks V2 evaluation.
8. If RKLX 5M remains stale while SPY advances, capture the same-time 5M REST bar endpoint and stream diagnostics. Do not relax freshness thresholds to hide missing bars.

## Interpretation

The uploaded JSONs prove stale assessments at their captured timestamps. They do not prove that WebSocket is broken. The separate RKLX observation demonstrates an in-session 5M coverage issue that remains unresolved. A fresh in-session capture after dynamic subscriptions is required to distinguish provider/feed coverage from an ingestion/assessment issue.


## RKLX IEX sparse-bar follow-up — 2026-10-30

User supplied 1M and 5M requests for 19:00–19:35 UTC. Both identify Alpaca/IEX and return four bars. The 1M source timestamps are 19:00, 19:08, 19:25, and 19:35 UTC. The 5M timestamps are 19:00, 19:05, 19:25, and 19:35 UTC; no 5M bars were returned at 19:10, 19:15, or 19:20.

The in-session stale observation around 19:26 UTC is explained by completed-bar timing:
- The 1M bar timestamped 19:25 completed at 19:26 and could be fresh.
- The 5M bar timestamped 19:25 was still forming and is correctly excluded.
- The last completed 5M bar was timestamped 19:05 and completed at 19:10. At 19:26 it was about 16 minutes old, beyond the 360-second limit.

This is a sparse returned IEX data interval, not evidence of a faulty freshness threshold. The wider three-day assessment history and the bounded 19:00–19:35 query are consistent over their overlapping timestamps; they are different request windows.

The 22:50 UTC stream-status snapshot was after the 20:00 UTC regular-session close. It showed a connected/subscribed socket but zero received/persisted bars. That does not prove failure during market hours. Regular-session delivery remains unverified.

Data does not establish whether the missing bars represent no IEX-eligible prints or upstream/provider coverage behavior. Do not infer that no trades occurred on other venues, do not treat missing bars as zero volume, and do not synthesize bars.

Next: run the stream-status before/after test during regular hours; verify per-symbol bar counts and timestamps advance; then confirm the assessment switches to `ALPACA_WEBSOCKET_LATEST_BAR` only after the 50-bar, freshness, and provenance requirements are met.
