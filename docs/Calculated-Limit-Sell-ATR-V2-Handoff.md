# Momentum Command Center — ATR V2 and Sell-Limit Engineering Handoff

**Current branch:** `feature/opportunity-quality-classification`  
**Latest code commit with green CI:** `c990faf973fb9e3a14ec948386e210b9eb8b4fc5`  
**GitHub Actions:** [run 42119562504](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42119562504) — build succeeded; 223/223 tests passed, including frontend JavaScript syntax validation.

## 0. November 5, 2026 — latest operator exports and engineering update

The operator supplied corrected V2.1 files:
- `data/ScannerExports/StrategyReports_EVR_11032026_1M_ATR_v2_1.csv`
- `data/ScannerExports/StrategyReports_EVR_11032026_5M_ATR_v2_1.csv`

Observed payload counts and diagnostics:
- 1m: 346 payload observations; 48 intraday timestamp gaps across the full file; 13 intraday gaps in the latest 119 observations.
- 1m numeric Wilder comparison: 20 post-warm-up observations; mean absolute error ~0.00002603; maximum absolute error ~0.00004669, within the 0.00005 four-decimal export tolerance. This is a local recomputation from the supplied CSV, not a Dashboard import or an API response. Because the latest 119-row window has intraday gaps, it is not a certified `MATCH`.
- 5m: 81 observations and one 10-minute timestamp jump. This is below the 99-row warm-up and cannot produce 20 comparison bars.

Code update:
- Added `AtrTimestampGapDetector` and wired it into the V2 parser and Wilder parity service.
- A scheduled overnight/weekend date transition in America/New_York no longer counts as a missing intraday bar. Missing intervals within the same New York date still count and block `MATCH`.
- Full-session completeness is not yet implemented. A missing entire trading session needs its own coverage check and must not be silently inferred as complete.
- Added regression tests for overnight/weekend transitions and intraday gaps.
- Latest code commit: `c9003fd24d3b39817a874304195e01619c79de21`. CI run [42121843682](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42121843682) was queued when this handoff was updated; verify its final result before treating the commit as tested.

Dashboard button location:
1. Workspace dropdown → **Boring Overnight**.
2. Scroll to **Calculated Limit Sell**.
3. Enter a value in **Research target quantile (%)**.
4. Click **Assess sell-limit evidence gates** (HTML ID `overnightCalculateSellLimit`).
5. Read the gate results immediately below the button.

SIP status:
- Prior recent-history SIP access returned HTTP 403 because the subscription did not permit the requested data. Separate IEX preflight attempts were correctly rejected for GTC_EXT qualification.
- This is a known external entitlement blocker, not an uncompleted operator setup step. Do not ask the operator to repeat SIP requests or click **Fetch 20-session GTC_EXT sell-window history** unless entitlement/provider access has materially changed.
- Continue development using IEX only for diagnostics. Do not qualify GTC_EXT from IEX.

Candidate review:
- See `docs/Validation/Overnight-Candidate-Review-20261105.md`.
- CBOE is the preferred addition to the next ordinary Overnight review; BLK has the strongest historical win rate; AVNT is a post-earnings momentum watch; EVER is secondary; BLMN is excluded from a routine Nov 5→Nov 6 overnight because earnings are scheduled premarket Nov 6.
- No candidate is marked READY.

## 1. What is implemented

### Thinkorswim ATR import

- `StrategyReportAtrV2Parser` supports the 11-field `MCC_OHLCV_ATR|v2|` and `MCC_OHLCV_ATR|v2.1|` payloads.
- V2.0 legacy and V2.1 corrected formula exports are recorded as separate study versions. The orchestrator and default parity query use V2.1 only; old V2.0 data cannot contaminate corrected-study parity.
- Supports 1m, 5m, 15m, 30m, 1h and 4h; validates symbol/timeframe consistency, epoch timestamps, invariant decimals, malformed payloads, duplicate timestamps and gaps.
- V1 Strategy Report parsing remains unchanged.

### Separate persistence and APIs

- Append-only JSONL store for Thinkorswim ATR observations, chart provenance, import batch/hash and correction linkage. It is separate from Alpaca/provider OHLCV.
- `POST /api/strategy-report/atr-import`
- `GET /api/strategy-report/atr-observations?symbol=EVR&timeframe=5m`
- `GET /api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&studyVersion=MCC_OHLCV_ATR_V2.1`
- Dashboard StrategyReport upload imports V2 data, displays counts/gaps/provenance and requests parity status.

### ATR parity diagnostic

- Version: `MCC-WILDER-ATR-PARITY-1.0`.
- Server calculation uses standard Wilder True Range on the exact imported TOS OHLCV rows.
- Warm-up: 99 observations; minimum comparison sample: 20 matched timestamps.
- Absolute tolerance: 0.00005, half of one unit at the V2 export's four-decimal ATR precision.
- Reports mean/max/P90 absolute error, mean relative error, compared count and timestamp gaps.
- This validates the ATR algorithm on imported TOS bars; it does not by itself validate Alpaca SIP OHLCV.

### Sell-limit evidence orchestration

- `POST /api/boring-overnight/sell-limit`
- Dashboard has a target-quantile input and an evidence-assessment button. It displays status and each pass/fail gate.
- The service checks persisted 20/20 GTC_EXT history, exact latest regular-session 1m reference close, V2.1 ATR(14) Wilder provenance, gap-free ATR parity across the latest 119 observations, 99 consecutive 5m warm-up observations, exact provider/TOS 5m OHLCV equality across all 119 parity-window bars, and completed/fresh ATR.
- **The numeric limit is deliberately blocked by the `WALK_FORWARD_VALIDATION` gate.** Time-separated target-touch/adverse-excursion validation is not implemented. The endpoint does not submit orders.

## 2. Important ThinkScript defect discovered and corrected

The November 3 exports were generated by a source formula that used:

```thinkscript
def trueRange = TrueRange(high, close[1], low);
```

Thinkorswim's documented `TrueRange(high, close, low)` function already references the previous close. Passing `close[1]` shifts the previous-close reference back an additional bar.

The corrected source now uses:

```thinkscript
def trueRange = TrueRange(high, close, low);
```

The corrected payload marker is `MCC_OHLCV_ATR|v2.1|`; legacy `v2` payloads are stored as `MCC_OHLCV_ATR_V2.0`, corrected payloads as `MCC_OHLCV_ATR_V2.1`.

Official reference: https://toslc.thinkorswim.com/center/reference/thinkScript/Functions/Tech-Analysis/TrueRange

Measured on the November 3 1m export after 99 warm-up observations:
- Standard previous-close Wilder ATR vs exported ATR: mean absolute error ~0.06370; maximum ~0.14788.
- Reproducing the old extra previous-close shift: mean absolute error ~0.00002545; maximum ~0.00004974.

The old export is a regression fixture for the defect, not a valid parity certification. The corrected ThinkScript V2.1 has not yet been compiled and re-exported by the operator.

## 3. Actual November 3 export coverage

| Export | Numbered Strategy Report rows | ATR payload observations | Timestamp gaps | Status |
|---|---:|---:|---:|---|
| EVR 1m | 692 | 346 | 48 | Old V2.0 formula; enough rows for diagnostic, but not valid corrected parity |
| EVR 5m | 162 | 81 | 1 | Old V2.0 formula and below the 99-observation warm-up |

Each source bar creates a transport-close order row and an ATR payload row. Use payload observation count, not numbered order row count, as the chart-bar count.

## 4. Required operator actions

1. Pull the latest `feature/opportunity-quality-classification` branch and run:
   ```powershell
   .\scripts\Get-Latest-And-Test.ps1
   ```
2. Open the updated `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt` study in Thinkorswim, replace the previous version and compile it. The payload must contain `v2.1`.
3. Confirm chart aggregation equals the study input; ATR length 14; average type Wilder; chart timezone Phoenix; extended-hours ON; corporate-action adjustment ON.
4. Export EVR 1m and 5m again. For 5m include at least 119 consecutive completed observations: 99 warm-up + 20 comparison bars. No timestamp gap is allowed in this latest 119-bar parity window.
5. Upload both CSVs separately through Dashboard → StrategyReport. The importer detects V2.0 vs V2.1 from the payload marker. Only V2.1 is used for sell-limit parity.
6. Confirm the 1m and 5m parity results. Both must show `MATCH` on at least 20 compared bars.
7. In the Overnight workspace, choose a research target quantile and click **Assess sell-limit evidence gates**. The UI will show the remaining failures. A numeric limit remains blocked until walk-forward validation is implemented.

### SIP data

The effective Alpaca feed last reported IEX. Prior SIP historical access returned HTTP 403. Do not switch configuration to SIP and assume entitlement. First confirm the Alpaca account actually permits SIP historical requests. The capture and sell-limit qualification gates require SIP; IEX cannot qualify the GTC_EXT sample.

## 5. Remaining engineering work

1. Implement time-separated walk-forward validation with per-session theoretical target touches and adverse excursions. This is the current explicit gate preventing numeric sell-limit output.
2. Keep theoretical OHLCV touches separate from actual broker fills, quantities, fees and timestamps.
3. Verify complete 20/20 SIP GTC_EXT windows and exact reference-close coverage on a confirmed SIP entitlement.
4. Re-import corrected V2.1 reports and establish actual platform parity.
5. Complete provider-feed parity for current 5m bars and ensure the latest 119 provider/TOS bars match exactly (99 warm-up + 20 comparison).
6. Complete Daily qualification and catalyst/news classification, Opportunity Quality calibration and full-pool scanner handling.
7. Complete runtime/browser acceptance: WebSocket recovery, collector schedule, UI state transitions, EOD/early-close behavior, page-load performance and console/network errors.
8. Keep broker execution integration separate; the sell-limit endpoint is research-only and never submits an order.

## 6. Final workflow direction

The intended end state is MCC as the standalone analysis and decision-support application. Thinkorswim is currently needed as an independent reference to validate the corrected ATR implementation. Once server-side ATR parity is established against SIP provider bars, MCC should calculate ATR itself and no longer require routine Thinkorswim exports or an open TOS chart for MCC analysis.

MCC does not currently route broker orders. A separate broker platform or execution integration will still be needed to place and manage trades.

## [Uncertainties]

- Thinkorswim V2.1 compilation and actual chart/export parity are not yet operator-verified.
- SIP entitlement and 20/20 history coverage are not established.
- Walk-forward validation and actual broker fill reconciliation remain incomplete.
- Browser acceptance has not been performed against the user's local runtime.
