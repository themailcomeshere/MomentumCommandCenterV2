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

The labels show the latest bar; use the Data Box/crosshair for the historical 11:26 bar.

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
