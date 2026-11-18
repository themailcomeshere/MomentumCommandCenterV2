# Intraday watchlist columns (MCC, 2026-11-16)

Custom Thinkorswim watchlist columns that show, per symbol, which of your intraday scan conditions are true right now. **Not yet compiled in Thinkorswim.**

## Two column sets (operator question 2026-11-18)

Thinkorswim keeps separate column sets for **scan Search Results** (Stock Hacker) and for **watchlists**. They do different jobs:

| | Intraday scan **Search Results** ("Intraday v1") | Intraday **Watchlist** |
|---|---|---|
| Job | Pick the few names worth watching out of many results | Monitor those few all session; this is the file you export to MCC **2.b** |
| Rows | Dozens to hundreds; every custom column runs on every row (slow columns show `loading`) | A handful |
| Sorting | **Sorted by → Custom Quotes** only lists plain-number columns: **5M RVOL**, **VWAP Dist %**, **Ext ATR**. **5M Trend** and **5M Trigger** are text labels and are greyed out there | Order you drag or sort in is the order MCC's **4 Red Zone** fills (first four, no Score column) |
| Columns | Symbol, Last, % Change, Volume, 5M RVOL, VWAP Dist %, 5M Trend, 5M Trigger, Ext ATR. Sorted by **5M RVOL** descending | The same nine, plus your installed 1M/5M study labels if you have them as columns |
| Never | ON / V3 columns (Overnight only) | ON / V3 columns |

MCC reads only **Symbol** and row order from an Intraday import; the other columns are for your eyes.

## Recommended column order (left to right)

1. **Symbol** (built in)
2. **Last** (built in)
3. **% Change** (built in, "Net Chng %"): scans 2A/D need +2% / +2.99%.
4. **Volume** (built in): scans 2A/Pivot need 500,000 for the day, D 250,000.
5. **VWAP Dist %** (`VWAP Dist %.txt`, 5 min): above VWAP or not.
6. **5M Trend** (`5M Trend.txt`, 5 min): 3/3 = close > EMA9 > EMA20 > SMA50.
7. **5M RVOL** (`5M RVOL.txt`, 5 min): 1.5x / 2.0x volume surge.
8. **5M Trigger** (`5M Trigger.txt`, 5 min): BREAKOUT / RECLAIM.
9. **Ext ATR** (`Ext ATR.txt`, 1 min): 2.0+ = too extended to chase (MCC's rule).
10. Optional: your installed 1M/5M study labels, if you already have them as columns.

**How to read a row:**
- **Momentum candidate:** green on VWAP Dist, 5M Trend 3/3, 5M RVOL and Trigger, with Ext ATR not red.
- **Wait for a pullback:** Ext ATR red.

## Install

1. Right-click a watchlist column header → **Customize…** → pick an unused **Custom** column → click its script icon.
2. Name it exactly as above and paste the file.
3. Set the **Aggregation** shown in the file header, with extended hours off.
4. Click **OK**.

Each column is deliberately small, to stay under Thinkorswim's complexity limit (`docs/rules/thinkscript.md` §8).
