# Sell-Limit Orchestration API — current evidence gates

## Endpoint

`POST /api/boring-overnight/sell-limit`

Example request:

```json
{
  "symbol": "EVR",
  "providerName": "Alpaca",
  "feed": "sip",
  "targetQuantilePct": 75
}
```

The API reads persisted observations only. It does not fetch provider bars, interpolate missing bars, infer executions, or submit orders. It returns per-gate evidence. A numeric sell limit remains withheld until every independent gate passes.

## Fail-closed evidence gates

1. **SIP required.** IEX/partial-feed observations are diagnostic only and cannot qualify the historical sell-window sample.
2. **20/20 GTC_EXT history.** Each of 20 sessions must contain every 07:00–19:59 ET minute plus the exact previous regular-session close. Missing intervals remain missing.
3. **Exact current reference close.** Use the latest completed regular-session 15:59 ET (or 12:59 ET early-close) 1m bar from the same provider/feed.
4. **Corrected TOS study provenance.** ATR(14), Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON, payload V2.1 / study `MCC_OHLCV_ATR_V2.1`.
5. **ATR algorithm parity.** Server Wilder ATR versus TOS ATR must return `MATCH` over the latest 119 observations (99 warm-up + 20 compared bars). Any timestamp gap in that window returns `GAPS_PRESENT`, not MATCH. Maximum absolute error must remain within the 0.00005 export-rounding tolerance.
6. **Continuous 5m warm-up.** At least 99 consecutive completed 5m TOS observations are required for Wilder initialization.
7. **Same-source 5m OHLCV.** All 119 bars in the parity window must have no timestamp gaps and must exactly match persisted provider/feed OHLCV at every timestamp.
8. **Fresh completed ATR.** Latest TOS 5m ATR bar must be completed and no more than 10 minutes old.
9. **Walk-forward validation. IMPLEMENTED; runtime PASS still required.** The `BoringOvernightWalkForwardValidator` uses the earliest 20 complete chronological sell sessions as training and the next 20 complete unseen sessions as test. The history service retains 40 sessions and session highs/lows; orchestration enforces a hard `WALK_FORWARD_VALIDATION` gate. Pass requires 100% training/test coverage, a positive training target, test target-touch rate at least the empirical training touch rate, and maximum test adverse excursion no greater than the training adverse-excursion P95 bound. Insufficient or invalid evidence fails closed.
10. **Quantile calculation.** The training-only target quantile is calculated from the first 20 sessions. The unseen 20 test sessions must never be used to select or tune the target.

Any failed gate returns a blocked result with reason codes and no numeric limit. Walk-forward implementation does not mean its evidence gate has passed. A passing OHLCV bar touch is theoretical and is not an executable quote, fill probability, or broker fill.

## Current operator evidence — do not treat as qualified

Latest supplied parity responses:

| Symbol | Timeframe | Status | Observations | Compared | Gaps | Mean absolute error | Maximum absolute error |
|---|---|---|---:|---:|---:|---:|---:|
| EVR | 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.000026034 | 0.000046686 |
| EVR | 5m | `NOT_MATCH` | 163 | 20 | 1 | 0.001046717 | 0.001935736 |
| CBOE | 5m | `NOT_MATCH` | 165 | 20 | 6 | 0.000527 | 0.000965 |

All use `MCC-WILDER-ATR-PARITY-1.1`, ATR 14 Wilder and V2.1 provenance. EVR 1m numeric error is within tolerance but gaps block certification. EVR 5m and CBOE 5m exceed numeric tolerance. The cause of those 5m mismatches is not established. Re-run parity against persisted observations; do not re-import solely to recheck it.

## Dashboard

The Overnight workspace includes:
- Research target quantile input (1–99).
- **Assess sell-limit evidence gates** action.
- Status, reference close, ATR parity, DATA? / NOT CALCULATED state and per-gate explanations.

The UI reads the effective Alpaca feed from provider status. It does not change feed configuration and never submits an order.

## Source boundaries

- Historical Overnight windows: persisted provider 1m bars.
- Current reference close: exact latest completed regular-session 1m bar from the same provider/feed.
- ATR input: imported corrected Thinkorswim V2.1 ATR observation.
- ATR parity: server Wilder calculation versus TOS ATR on the same imported TOS OHLCV, seeded from full imported history.
- Provider/TOS 5m parity: exact OHLCV equality over all latest 119 parity-window bars.
- Walk-forward: theoretical OHLCV session highs/lows, chronological train/test split; never broker fills.
- Broker fills: not consumed by this endpoint and never inferred from bar highs.

The endpoint remains intentionally strict. SIP HTTP 403 entitlement denial, IEX qualification attempts, missing source timestamps, numeric ATR mismatch, non-matching provider/TOS OHLCV, incomplete warm-up, stale ATR, incomplete 20-session history or failed walk-forward validation leaves the result blocked.


## 2026-11-06 operator validation — latest state

- Release build/tests passed at source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`.
- API health is OK in `decision-support-only` mode; order submission remains NOT IMPLEMENTED.
- Alpaca is configured and validated on IEX. Massive is not configured. A one-bar provider validation is not proof of historical or extended-hours completeness, and IEX does not satisfy the SIP sell-window gate.
- Latest EVR 1m, EVR 5m, CBOE 5m and two-day CBOE 5m ATR imports all parsed successfully but appended zero observations and made zero corrections. Their source exports report respectively 48, 1, 5 and 6 timestamp gaps.
- EVR 1m's prior persisted parity result was numeric-within-tolerance but `GAPS_PRESENT`; EVR 5m and CBOE 5m prior results were `NOT_MATCH`. Re-query `GET /api/strategy-report/atr-parity?symbol={symbol}&timeframe={timeframe}` for EVR/1m, EVR/5m and CBOE/5m after API restart. No re-import is needed.
- The 82-observation EVR 5m export and 85-observation single-day CBOE export do not meet the 119-bar window requirement. The 165-observation CBOE two-day export still has six gaps and a 3h20m discontinuity; aggregate count does not establish continuity.
- Preserve the fixed 0.00005 tolerance, full-history Wilder seed, and fail-closed gap policy. Do not infer that gaps are no-trade intervals without independent evidence.

See `docs/Master-To-Do.md` and `docs/Validation/ATR-V2-Parity-Investigation-20261105.md` for the detailed current operator checklist and exact PowerShell parity queries.
