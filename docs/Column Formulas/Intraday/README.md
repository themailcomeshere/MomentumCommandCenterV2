# Intraday watchlist columns (MCC, 2026-11-16)

Custom Thinkorswim watchlist columns that show, per symbol, which of your intraday scan conditions are true right now. **Not yet compiled in Thinkorswim.**

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
