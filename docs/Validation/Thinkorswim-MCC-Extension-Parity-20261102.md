# Thinkorswim / MCC Extension Parity — 2026-11-02

## Goal

Compare the same completed 1-minute INO candle in Thinkorswim and MCC. Record unrounded inputs; rounded dashboard text is insufficient for a low-priced symbol.

## Thinkorswim: expose ATR14 and extension

On the 1-minute INO chart, add a lower study with these ThinkScript plots:

```thinkscript
declare lower;

plot ATR14 = Average(TrueRange(high, close, low), 14);
ATR14.SetDefaultColor(Color.CYAN);
ATR14.SetLineWeight(2);

def DailyVWAP = vwap(period = AggregationPeriod.DAY);
plot ExtensionATR =
    if ATR14 > 0
    then AbsValue(close - DailyVWAP) / ATR14
    else Double.NaN;
ExtensionATR.SetDefaultColor(Color.YELLOW);
ExtensionATR.SetLineWeight(2);

AddLabel(yes, "ATR14: " + AsText(ATR14), Color.CYAN);
AddLabel(yes, "EXT: " + AsText(ExtensionATR), Color.YELLOW);
```

The built-in VWAP study is already on the chart as `VWAP (-2.0, 2.0, DAY)`. Its center VWAP plot is the Daily VWAP line; the -2 and +2 values are deviation bands, not ATR inputs.

For the historical 11:26 candle, enable Thinkorswim's Data Box and move the crosshair onto that exact 1-minute candle. Read:
- Candle close from the bar's OHLC data
- Daily VWAP from the built-in VWAP plot
- ATR14 from the `ATR14` plot
- Extension from `ExtensionATR`

The `AddLabel` values show the latest/live bar; they are not historical values. For the historical candle, hover the exact 1-minute bar and read the **ExtensionATR plot** in the Data Box. Do not use the live EXT label as the historical candle's extension.

## MCC values

The Main Chart displays:
- 1M Close
- Daily VWAP
- ATR14
- 5M Relative Volume
- ATR Extension

The close, VWAP, and ATR14 are now displayed to five decimal places. The detailed reason/source panel also prints those 1M inputs to five decimal places. This is deliberate: for INO around $2, three-decimal rounding can materially distort a ratio check.

MCC calculation:

`Abs(1M close - 1M session Daily VWAP) / 1M ATR14`

MCC Daily VWAP is currently computed from regular-session 1-minute bars using volume-weighted typical price `(high + low + close) / 3`. MCC ATR14 is the simple 14-bar average of True Range. Compare those definitions to the Thinkorswim study and session settings.

## User's November 2 observation

At the reported 11:26am bar:
- Close: both displayed approximately $2.00
- MCC Daily VWAP: $1.838 (previously displayed to 3 decimals)
- MCC ATR14: $0.013 (previously displayed to 3 decimals)
- MCC extension: 11.89 ATR
- Thinkorswim extension: 22.92 ATR

These values are not enough to identify the cause. The previous MCC display rounded close, VWAP, and ATR14; the exact timestamp/date and full precision inputs have not been captured. A rough calculation from rounded values is not a valid parity test.

## Comparison worksheet

For the exact same symbol, date, and completed 1M bar timestamp, record:

| Input | Thinkorswim | MCC |
|---|---:|---:|
| Exact candle timestamp | | |
| Close (5+ decimals) | | |
| Daily VWAP (5+ decimals) | | |
| ATR14 (5+ decimals) | | |
| `Abs(close - VWAP) / ATR14` | | |
| Feed / session scope | | |

Do not adjust V2 thresholds until the inputs match. If close matches but VWAP does not, investigate feed, RTH/extended-hours inclusion, session reset, and VWAP source. If VWAP matches but ATR14 does not, compare the exact 14 true-range observations and average type. If both inputs match but extension differs, investigate formula/rounding or timestamp alignment.

## [Uncertainties]

- The exact calendar date and timezone-qualified timestamp of the reported 11:26 bar were not supplied.
- The previous displayed MCC prices were rounded to three decimals (close to two), so the reported 11.89 cannot be recomputed from the displayed inputs.
- Thinkorswim session/extended-hours VWAP settings must be confirmed against MCC's regular-session-only VWAP.
- Numerical parity has not yet been established.


## November 2 follow-up — latest values and completed-bar timing

The user later supplied:

| Input | Thinkorswim | MCC |
|---|---:|---:|
| Close | 2.06 | 2.05500 |
| Daily VWAP | 1.89 | 1.87031 |
| ATR14 | 0.02 | 0.01429 |
| Extension | 24.6 ATR (latest/live label, not per-candle) | Not supplied for the same exact timestamp |

The close is consistent at two-decimal display precision: MCC 2.05500 rounds to 2.06. TOS VWAP and ATR14 are only shown to two decimals in the supplied comparison, so the input mismatch cannot yet be quantified precisely. The 24.6 ATR value came from the live/latest label, not the historical bar; it is not a valid historical parity value.

The user also observed an apparent one-minute Main Chart delay. Source review found that the API already returns both bar-start `sourceTimestampUtc` and bar-close `completedAtUtc`, but the UI had displayed the bar-start timestamp under a "Latest completed 1M bar close" label. The UI now displays the expected completed/close time and separately shows the start-to-close interval. See [Live 1-Minute Bar Timestamp and Latency](Live-Bar-Timestamp-Latency-20261102.md).

This display correction does not establish provider latency or TOS parity. The API intentionally excludes the in-progress minute. During regular hours, compare the same completed candle and inspect API `evaluatedAtUtc`, `oneMinute.completedAtUtc`, `oneMinute.ageSeconds`, `oneMinute.feed`, and `oneMinute.sourceTransport`.

## Updated acceptance

- [x] User supplied the latest rounded TOS/MCC values; recorded above.
- [x] Corrected the UI's displayed timestamp to use the completed-bar close time.
- [ ] Capture TOS Close, VWAP, ATR14 and historical ExtensionATR to five decimals on the exact same completed candle as MCC.
- [ ] Confirm matching session scope (MCC currently uses regular-session HLC3 volume-weighted VWAP) and actual bar timestamp.
- [ ] Do not change extension thresholds or V2 signal rules to force numeric parity.


## 1M V14.5 — reconstructed 5M Permission audit (2026-11-02)

The current Library 1M baseline is `MomentumCommandCenterV3_2_1M_V14_5.txt`. Keep the normal 5M authority study at `MCC_V3_2_5M_V4_1_7.txt`. The user's installed 1M study hash has not been independently captured, so the Library baseline is not proof of byte-for-byte installation.

A source comparison found one material parity gap:

- Normal 5M V4.1.7 `bullishPermissionRaw` requires 5M price above Daily VWAP, 5M price above EMA9, EMA9 >= EMA20, and RSI >= 50.
- 1M V14.5 reconstructs 5M raw permission from 5M EMA9 > EMA20, 5M close > EMA9, 5M RSI >= 50, and not-bearish-stack. It does **not** include the 5M close-above-Daily-VWAP condition.
- V14.5's shared `fiveExecutionPermission` separately requires the current 1M price above Daily VWAP. That is an additional 1M execution filter, not a substitute for the missing 5M VWAP condition.

Therefore V14.5's reconstructed 5M Permission can disagree with the normal V4.1.7 5M authority on a bar where 1M price and 5M close are on opposite sides of Daily VWAP. This is a targeted parity defect, not a reason to redesign the 1M event/continuation pipeline.

**Disposition:** retain V14.5 as the current baseline for now; do not install an uncompiled replacement or alter thresholds. The next 1M revision should make its reconstructed 5M Permission agree with V4.1.7 on the same completed 5M bar, including the 5M Daily VWAP condition, while retaining the independent 1M VWAP execution gate. Compile in thinkorswim and compare same-symbol/same-timestamp permission/state outputs before promoting the revision. Do not call parity certified until that test passes.
