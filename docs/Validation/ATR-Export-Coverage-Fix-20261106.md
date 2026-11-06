# ATR Export Coverage Diagnosis and V2.1 Study Correction — 2026-11-06

**Branch:** `feature/opportunity-quality-classification`  
**New source exports:**  
- `data/ScannerExports/StrategyReports_EVR_11022026_to_11052026_5M_ATR.csv`
- `data/ScannerExports/StrategyReports_CBOE_11022026_to_11052026_5M_ATR.csv`

## 1. Latest import evidence

| Symbol | Report rows | ATR observations | Import appended | Identical skipped | Corrections | Parser gap count |
|---|---:|---:|---:|---:|---:|---:|
| EVR 5m | 650 | 325 | 162 | 163 | 0 | 3 |
| CBOE 5m | 658 | 329 | 164 | 165 | 0 | 10 |

Both exports identify V2.1, ATR(14) Wilder, Phoenix chart timezone, extended hours ON and corporate-action adjustment ON.

Latest operator-supplied parity responses:

| Symbol | Status | Source observations | Compared bars | Gaps in 119-bar window | Mean absolute error | Maximum absolute error |
|---|---|---:|---:|---:|---:|---:|
| EVR 5m | `NOT_MATCH` | 325 | 20 | 1 | 0.0366528495 | 0.0677729555 |
| CBOE 5m | `GAPS_PRESENT` | 329 | 20 | 1 | 0.0000200950 | 0.0000436336 |
| EVR 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.0000260340 | 0.0000466859 |
| CBOE 1m | `NO_DATA` | 0 | 0 | 0 | — | — |

The tolerance remains 0.00005. CBOE's compared numeric values are within tolerance, but one gap prevents MATCH. EVR's numeric error is far outside tolerance.

## 2. Row-level recurrence diagnosis

The two CSVs were parsed from their explicit V2.1 payloads. For each consecutive exported pair, true range was recomputed from current high/low and the previous exported close. The exported ATR recurrence was checked as:

`ATR[t] = (13 * ATR[t-1] + TR[t]) / 14`

### EVR

- 325 observations, 3 timestamp discontinuities in the full export.
- A large recurrence discontinuity occurs at **2026-11-05T14:05:00Z**.
- Previous exported observation: **2026-11-04T21:00:00Z**, ATR **0.6977**, close **311.03**.
- Current observation: **2026-11-05T14:05:00Z**, OHLC all **313.50**, ATR **6.1914**.
- Current-bar TR recomputed from the previous exported close is **2.47**.
- Wilder recurrence from the previous exported ATR and that TR predicts approximately **0.8243**, not 6.1914. The recurrence residual is **+5.3671071**.
- Across the remaining consecutive exported observations, recurrence residuals return to the four-decimal rounding scale (maximum absolute residual about **0.00009286**).
- The resulting difference against the server's independently recomputed ATR decays across the final 20 comparison bars: absolute error starts at **0.06777296** and ends at **0.01657418**.

This localizes the large numeric mismatch to the state transition across the long missing-history interval, rather than a persistent formula error on every subsequent bar.

### CBOE

- 329 observations, 10 timestamp discontinuities in the full export.
- Large recurrence residuals also occur at long discontinuities, including approximately **-0.0949214** at 2026-11-03T13:05:00Z and **+0.0841929** at 2026-11-04T14:00:00Z.
- On consecutive exported bars after those transitions, recurrence residuals return to the four-decimal rounding scale.
- The latest 20 numeric comparisons are within the 0.00005 tolerance, but one missing timestamp in the latest 119-bar qualification window keeps status at `GAPS_PRESENT`.

## 3. Proven export-design mismatch

The committed V2.1 ThinkScript computes:

- `trueRange = TrueRange(high, close, low)`
- `atrSeries = MovingAverage(AverageType.WILDERS, trueRange, atrLength)`

Those calculations run on the full host-chart bar sequence.

However, the previous `exportBar` predicate also applied a 07:00–19:59 ET time-of-day filter. That meant the Strategy Report could omit chart bars outside the export window even though those bars still updated the ATR series. The next exported observation could therefore carry a TOS ATR state that cannot be reconstructed from the preceding exported row. The EVR recurrence jump is direct evidence of such a state discontinuity across a long source-history gap; the exact omitted chart bars are not present in these CSVs and cannot be identified from this export alone.

## 4. Implemented study correction

Updated `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`:

- Removed the 07:00–19:59 ET time-of-day export filter.
- Retained date-range filtering only.
- The Strategy Report now exports every available host-chart bar in the selected date range, including premarket, regular-session and after-hours bars when the chart has extended hours enabled.
- Kept the V2.1 payload and Wilder formula unchanged.
- Moved the strategy's 07:00–19:59 ET eligible-window selection downstream, after full-series ATR parity.

This is a source-file change only. It has **not** been compiled in Thinkorswim or run against a fresh export yet.

## 5. Exact next TOS export procedure

1. Pull the latest `feature/opportunity-quality-classification` branch.
2. Open `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt` and replace the existing ThinkScript study with the updated source.
3. Compile it in Thinkorswim. Confirm the label shows `chart=MATCH` and full chart-history export enabled.
4. Set chart aggregation and study `chartAggregation` to 5m; ATR length 14; Wilder; Phoenix timezone; extended hours ON; corporate-action adjustment ON.
5. Choose a start date early enough to export at least 99 completed 5m bars before the latest 119-bar qualification window. Use the same date range for EVR and CBOE.
6. Confirm the Strategy Report includes all available premarket, regular-session and after-hours chart bars in that date range. Do not filter by time of day.
7. Export EVR and CBOE separately, preserving the explicit payload epoch and full decimal precision.
8. Import both CSVs once. Existing timestamps should be idempotent; newly included source bars should append. Record append/identical/correction counts.
9. Re-run parity without changing tolerance. A valid `MATCH` requires all 119 qualification observations to be continuous under the declared chart/session rules and all 20 compared ATR values within tolerance.
10. Preserve unexplained gaps. Do not impute zero-volume bars or synthetic OHLCV.

## 6. Other sell-limit gates — status and actual dependency

| Gate | Current status | What blocks completion |
|---|---|---|
| ATR parity MATCH | BLOCKED | EVR numeric mismatch; CBOE and EVR have a qualification-window gap |
| Continuous qualified 119-bar TOS history | BLOCKED | Current exports contain gaps; revised full-chart export has not yet been compiled/run |
| Exact provider/TOS 5m OHLCV equality | NOT VERIFIED | Provider bars for the exact 119 TOS timestamps are not in the supplied evidence |
| Exact prior regular-session close | NOT VERIFIED | No exact same-provider/feed reference-close record supplied |
| Confirmed SIP entitlement | BLOCKED / NOT CONFIRMED | Prior SIP request returned HTTP 403; do not retry until account entitlement/access changes |
| 20/20 SIP GTC_EXT sessions | NOT CAPTURED | Requires confirmed SIP access and 40 complete sessions, each with 780 exact 1m bars plus exact prior close |
| 20-train / 20-unseen-test walk-forward | IMPLEMENTED, RUNTIME NOT QUALIFIED | Requires the 40 complete source-qualified SIP sessions; current test fixtures are synthetic, not market evidence |

The current API's Alpaca IEX validation is not a substitute for SIP entitlement or consolidated coverage. No local API process or user credentials are accessible from this repository update, so no provider capture, SIP entitlement test, 40-session capture, or runtime sell-limit calculation was executed here.

## 7. [Uncertainties]

- The exact bars omitted from the old time-filtered Strategy Report are unavailable in the supplied CSVs. The study predicate proves that time-filtered export could omit bars used by ATR; the row-level EVR recurrence jump localizes the state break but cannot enumerate the omitted bars.
- The revised ThinkScript has not yet been compiled or run in Thinkorswim.
- No provider/TOS bar equality, prior-close equality, SIP entitlement, 40-session completeness, or runtime walk-forward PASS is evidenced.
- No broker execution/fill integration exists; OHLCV touches are not fills.
