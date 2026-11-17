# SLS: MCC Red Zone vs Thinkorswim (2026-11-17, ~09:00 AZ, standard time)

Operator screenshot: TOS charts (SLS 5 D 5m, SLS 1 D 5m, SLS Today 1m) beside the MCC Red Zone in INTRADAY MOMENTUM. MCC's live assessment was read at 16:04:33 UTC (09:04 AZ) from `GET /api/market-data/live-assessment?symbol=SLS&provider=Alpaca`. The TOS labels were read from the screenshot (TOS data box `11/17/26 9:00 AM`).

## Side by side

| Item | TOS (installed 5M / 1M studies) | MCC |
|---|---|---|
| Price | 19.935 (1D 5m) | 1M close $20.00 (latest 1m bar 08:59 AZ) |
| 5M permission | **YES** | **NO** (score 4/6, threshold 5) |
| 5M state | PREPARE TO BUY (5m chart); 1m chart's 5M STATE: PREPARE TO SELL | WAIT |
| 5M structure | NEUTRAL (5m chart); TRANSITIONING (1m chart) | structure bull: false |
| VWAP | **22.76** (`vwap(period = AggregationPeriod.DAY)`) | **20.31** (5m), 20.52 (1m card) |
| Extension | 3.45 ATR (5m), 11.21 ATR (1m) | 1.75 ATR (card); decision atrExtension 0.68 at 09:04 |
| RSI (5m) | 52 | 52.3 |
| RVOL | 0.01 (5m), 0.07 (1m) | 8.17× same-slot (different definition) |
| 1M execution | 1M EXECUTION ENVIRONMENT: NO | BLOCKED_BY_5M_PERMISSION |

## Cause 1: permission definitions differ (confirmed from source)

- **TOS** (`docs/Validation/Installed-5M-Study.txt`, "PERMISSION STATE"): a **latch**.
  - It is set when `price > dailyVWAP and price > ema9 and ema9 >= ema20 and rsi >= 50`.
  - It is released only by:
    - the EOD exit window;
    - persistent major failure (price below VWAP and EMA9, EMA9 < EMA20, EMA20 < SMA50);
    - structural damage on `permissionFailureBars = 2` consecutive bars (price < VWAP **and** EMA9 < EMA20).
  - Otherwise it keeps the previous bar's value.
- **MCC** (`LiveSymbolAssessmentService`, `permissionThreshold = 5`): a **stateless score**. At least 5 of 6 must pass on the current bar: close > VWAP, close > EMA9, EMA9 > EMA20, EMA20 > SMA50, RSI ≥ 50, RVOL ≥ 1.5.
- **At 09:04 AZ:** SLS close 20.00 was below MCC's VWAP (20.31) and EMA9 (20.20), while EMA9 > EMA20 still held.
  - TOS: no structural damage (EMA9 is still above EMA20), so the latch stays YES.
  - MCC: 4/6, so NO.
- MCC therefore blocks entries that TOS still permits, and the reverse can also happen: MCC can grant 5/6 without TOS's VWAP + EMA9 + RSI raw condition.

## Cause 2: VWAP session window (likely, not confirmed)

- **MCC:** VWAP uses regular-session bars only (09:30–16:00 ET) and IEX volume.
- **TOS:** `vwap(period = DAY)` includes extended-hours bars when the chart shows them. The 5 D 5m chart shows shaded extended-hours areas.
- SLS traded far above 20 earlier in the day (high 34.19). Pre-market inclusion would raise TOS's VWAP: 22.76 vs 20.31.
- Extension uses the same formula in both (|price − VWAP| / ATR14 of the chart's bars), so the extension gap follows from the VWAP gap.
- **Data not found in sources:** whether the TOS 5m chart has "Show extended-hours trading session" enabled.

## Not resolved

- Within TOS, the 5m chart reads 5M STATE: PREPARE TO BUY / STRUCTURE: NEUTRAL, while the 1m chart's 5M labels read PREPARE TO SELL / TRANSITIONING. The 1m study's secondary-aggregation 5M values differ from the 5m study at the same moment.
- RVOL: TOS shows the current partial bar's volume vs average (0.01); MCC shows a same-slot completed-bar ratio (8.17×). Different definitions; not comparable.

## Fix: TOS latch ported into MCC (2026-11-17, operator approval "Yes, port it")

- **Code:** `FiveMinutePermissionLatch` (Core). The study's PERMISSION STATE is ported line for line:
  - raw condition, structural damage over 2 bars, major failure over 2 bars, EOD window, latch;
  - block codes 0–8, from the study's permissionBlockCode.
- **Engine:** `V2SignalEngine` uses the latch value when the caller supplies it. The 6-condition score stays on the card as an alignment diagnostic only.
- **Callers:** the live assessment and the historical replay both run the latch over every completed 5m bar (`LiveSymbolTechnicalCalculator.PermissionSeries`). A unit test proves that series equals `Calculate()` exactly at every index.
- **Daily restart:** the latch starts at NO on each Eastern date, which models the operator's "1 D" 5m chart.
- **EOD window, as installed:** 18:55 ET in daylight time, 19:55 ET in standard time.
  - The study's comment calls this 15:55 AZ. That is true in daylight time only; in standard time 15:55 AZ is 17:55 ET.
  - The EOD window never falls inside the regular session either way.

### Replay on real bars (CLAUDE.md requirement)

SLS 5m IEX bars, 2026-10-27 to 2026-11-17 16:10 UTC (1,153 bars, fetched from MCC `/api/market-data/bars`, kept outside the repo). Today's regular-session rows:

| AZ bar | close | MCC VWAP | EMA9 | EMA20 | RSI | latch | transition | old score rule |
|---|---|---|---|---|---|---|---|---|
| 07:30 | 21.515 | 22.478 | 23.889 | 20.454 | 57.0 | no | NOT_GRANTED (code 4) | no (4) |
| 08:05 | 20.525 | 20.512 | 20.877 | 20.015 | 54.3 | no | NOT_GRANTED (code 5) | YES (5) |
| 08:35 | 20.225 | 20.290 | 19.994 | 19.839 | 53.8 | no | NOT_GRANTED (code 4) | YES (5) |
| 08:40 | 20.305 | 20.290 | 20.056 | 19.883 | 54.1 | **YES** | GRANTED | YES (6) |
| 08:55 | 20.000 | 20.307 | 20.201 | 20.015 | 52.3 | **YES** | HELD | **no (4)** |
| 09:00 | 20.250 | 20.306 | 20.211 | 20.037 | 53.3 | YES | HELD | YES (5) |
| 09:10 | 20.800 | 20.311 | 20.375 | 20.150 | 55.5 | YES | RAW_YES | YES (6) |

- **At the 08:55 bar** (the latest completed bar behind the operator's ~09:00 screenshot): the latch reads **YES (HELD)**, matching TOS **5M PERMISSION: YES**. The old rule read NO.
- **Where the old rule was looser:** it granted at 08:05 and 08:35 without the raw condition. At 08:05 the close was below EMA9; at 08:35 it was below VWAP.

### Still different from TOS

- **VWAP:** MCC uses regular-session IEX volume. TOS `vwap(DAY)` includes pre-market when the chart shows it. Pending the operator's chart-setting check.
- **Pre-market bars:** IEX had pre-market bars from 06:30 AZ. With no session bars yet, MCC's VWAP equals the close, so the latch cannot grant in pre-market. TOS may have.
- **Indicator seeding:** EMA and RSI seeding differ from TOS `ExpAverage` / `RSI`. The effect decays over 21 days of history and was not measured.

