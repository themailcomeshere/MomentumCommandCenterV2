# Morning Command Brief, Historical Replay, and Alpaca Streaming

**Branch:** `feature/opportunity-quality-classification`  
**Date:** 2026-10-30  
**Status:** UI/API implementation committed; CI and local browser/runtime acceptance pending.

## Morning Command Brief

The same partial API report is available in two locations:
1. Header button opens a modal.
2. Full-width collapsed section appears near the bottom of the dashboard.

Both display the current `GET /api/morning-brief` response. The current endpoint is intentionally partial: benchmark bars, Alpaca persisted top-mover observations, sector ETF proxy identifiers, and unclassified Alpaca news headlines. It does not yet provide validated breadth, sector money flow, catalyst classification, historical outcome probabilities, a validated Red Zone rank, or Boring Overnight qualification.

## Historical V2 replay

Use the Historical Decision Replay panel:
- Enter a ticker.
- Select the exact stored feed (IEX or SIP).
- Enter an explicit UTC timestamp including seconds and ending in Z.
- Leave "Simulate an existing position" unchecked for fresh-entry context, or check it to test the engine's existing position-management branch.

The API endpoint is:

`GET /api/market-data/replay?symbol=SPY&providerName=Alpaca&feed=iex&asOfUtc=2026-10-30T14:35:00Z&hasPosition=false`

Replay reads only persisted bars and excludes any bar whose expected close is after the as-of timestamp. It requires 50 completed 1m bars, 51 completed 5m bars, and adequate same-clock-slot RVOL history. It does not download missing history.

The same current `V2SignalEngine` and default `V2Config` are used. Historical configuration archival is not yet available, so this is not a guaranteed bit-for-bit reproduction of the engine/config that existed on the selected historical date.

The position checkbox only sets the existing boolean `hasPosition`. It does not simulate share quantity, side, entry price, partial fills, or a Schwab-sourced position.

## Optional Alpaca minute-bar WebSocket collector

The worker is disabled by default. Set these server-side settings:

```json
{
  "MarketData": {
    "Alpaca": {
      "StreamEnabled": true,
      "StreamSymbols": "SPY,RKLX,ASTX,XRPN",
      "Feed": "iex"
    }
  }
}
```

Use `iex` for Basic-plan access. Use `sip` only with appropriate SIP entitlement. Never place keys/secrets in frontend files or chat.

Verify:
1. `GET /api/market-data/stream/status` shows `connected=true`, `state=SUBSCRIBED`, and the expected symbols.
2. `lastMessageUtc` advances.
3. `lastBarUtc` and `persistedBars` advance during active market hours.
4. The worker reconnects after a deliberate network interruption.
5. Late `updatedBars` corrections are appended rather than overwriting prior observations.

The stream persists 1m bars and late corrections. The live V2 assessment uses the persisted 1m series when subscription is confirmed, at least 50 completed bars are available, the latest bar is fresh, and its source transport is WEBSOCKET. It falls back to REST otherwise. 5m permission remains REST-backed. This is a hybrid 1m WebSocket / 5m REST decision path, not a raw-tick signal engine.

## Stale market data

Live assessment freshness thresholds remain:
- 1m: 120 seconds after expected close.
- 5m: 360 seconds after expected close.

The response includes `freshnessDiagnostics` and a detailed stale reason. For each ticker, inspect which timeframe failed, age, expected close, source timestamp, and feed. A generic stale message alone cannot establish the reason for RKLX, ASTX, or XRPN.

## Schwab Trader API Individual — user setup required

The user must:
1. Register/log in at the Schwab Developer Portal.
2. Add the Individual Developer role to the portal profile (requires a Schwab brokerage account).
3. Request access to Trader API Individual / Accounts & Trading production.
4. Create an approved app and register its callback URL.
5. Put the resulting client ID/secret and OAuth configuration into server-side secret storage.

No brokerage credentials or tokens should be shared in chat. The API product page does not publish a clear monthly price; confirm costs/approval directly with Schwab.

Read-only account/position synchronization is not implemented yet. Required design: OAuth, secure refresh-token storage, account/position retrieval, normalization, snapshot timestamps, sync health, and fail-closed behavior when the account sync is stale or unavailable. Order endpoints remain out of scope and disabled.

