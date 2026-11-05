# Sell-Limit Orchestration API — 2026-11-05

## Endpoint

`POST /api/boring-overnight/sell-limit`

Request:

```json
{
  "symbol": "EVR",
  "providerName": "Alpaca",
  "feed": "sip",
  "targetQuantilePct": 75
}
```

The API reads persisted observations only. It does not fetch provider bars, interpolate missing bars, infer executions, or submit orders.

## Fail-closed evidence gates

1. SIP feed is mandatory; IEX/partial feed cannot qualify the historical sample.
2. Exactly 20 complete 07:00–19:59 ET sell windows, each with the exact previous regular-session close and all 780 minute bars.
3. Exact latest completed regular-session 15:59 (or 12:59 early-close) 1m reference-close bar on the same provider/feed.
4. TOS provenance must be ATR(14) Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON, and MCC_OHLCV_ATR_V2.
5. Timestamp-matched server Wilder ATR parity must be `MATCH`.
6. At least 99 consecutive completed 5m TOS observations must exist.
7. The latest 99 TOS 5m OHLCV rows must exactly match persisted provider/feed 5m OHLCV at every timestamp.
8. Latest TOS 5m ATR must be completed and no more than 10 minutes old.
9. The existing quantile calculator must return a positive provisional candidate from the 20 complete sessions.

Any failed gate returns `BLOCKED` with reason codes and no numeric limit. When every gate passes, status is `PROVISIONAL`; this is still not a validated fill probability or broker order. Walk-forward validation and actual broker fills remain separate requirements.

## Source boundaries

- Historical Overnight window evidence: persisted provider 1m bars.
- Current reference close: exact prior-session 1m bar from the same provider/feed.
- Current ATR: imported Thinkorswim ATR V2.
- ATR parity: server Wilder calculation vs Thinkorswim ATR on the same imported TOS OHLCV.
- Provider/TOS 5m parity: exact OHLCV equality over the latest 99 bars.
- Broker fills: not consumed by this endpoint and never inferred from bar highs.

The endpoint is intentionally strict. A 403 SIP entitlement failure, IEX configuration, absent provider bars, old Thinkorswim export, mixed chart settings, missing timestamp, or any data mismatch leaves the result blocked.
