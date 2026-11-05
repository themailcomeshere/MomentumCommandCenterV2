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

The API reads persisted observations only. It does not fetch provider bars, interpolate missing bars, infer executions, or submit orders. It returns a per-gate evidence result. A numeric limit is deliberately withheld until walk-forward validation is implemented.

## Fail-closed evidence gates

1. **SIP required.** IEX/partial feed cannot qualify the historical sample.
2. **20/20 GTC_EXT history.** Every 07:00–19:59 ET minute plus the exact previous regular-session close must be present for all 20 sessions.
3. **Exact current reference close.** Latest completed regular-session 15:59 (or 12:59 early-close) 1m bar from the same provider/feed.
4. **Corrected TOS study provenance.** ATR(14) Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON, payload V2.1 / study version `MCC_OHLCV_ATR_V2.1`.
5. **ATR algorithm parity.** Timestamp-matched server Wilder ATR vs TOS ATR must return `MATCH` with at least 20 compared bars after 99 warm-up observations.
6. **Continuous 5m warm-up.** At least 99 consecutive completed 5m TOS observations.
7. **Same-source 5m OHLCV.** Latest 99 TOS 5m rows must exactly match persisted provider/feed 5m OHLCV at every timestamp.
8. **Fresh completed ATR.** Latest TOS 5m ATR bar must be completed and no more than 10 minutes old.
9. **Walk-forward validation.** Currently NOT IMPLEMENTED. The API always reports this gate as failed; no numeric sell limit is issued.
10. **Quantile calculation.** After all prior gates pass, the existing calculator may produce a provisional research candidate from the complete 20-session sample. It is not an executable quote or validated fill probability.

Any failed gate returns `BLOCKED` with reason codes and no numeric limit. The current implementation is intentionally blocked at gate 9 even when all upstream evidence gates pass. Once walk-forward validation exists, a result can be marked `PROVISIONAL` for human review; it will not place an order.

## Dashboard

The Overnight workspace includes:
- Research target quantile input (1–99).
- **Assess sell-limit evidence gates** action.
- Status, reference close, ATR parity, DATA? / NOT CALCULATED state and per-gate explanations.

The UI reads the effective Alpaca feed from provider status. It does not change feed configuration. It never submits an order.

## Source boundaries

- Historical Overnight windows: persisted provider 1m bars.
- Current reference close: exact latest completed regular-session 1m bar from the same provider/feed.
- ATR input: imported corrected Thinkorswim V2.1 ATR observation.
- ATR parity: server Wilder calculation vs TOS ATR on the same imported TOS OHLCV.
- Provider/TOS 5m parity: exact OHLCV equality over the latest 99 bars.
- Broker fills: not consumed by this endpoint and never inferred from bar highs.

The endpoint is intentionally strict. A 403 SIP entitlement failure, IEX configuration, absent provider bars, V2.0 legacy export, mixed chart settings, missing timestamp, non-matching OHLCV or incomplete warm-up leaves the result blocked.
