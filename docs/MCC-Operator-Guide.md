# MCC Operator Guide — daily use

**Updated:** 2026-11-16 (code head `703bf03`). This is the single page for using MCC day to day. Step numbers match the blue badges on the Dashboard. The detailed map is in [MCC-Workflow-Steps.md](Engineering/MCC-Workflow-Steps.md).

**Times are Arizona time.** In **standard time** (early November to mid-March, now) the regular session is **07:30–14:00 AZ**. In **daylight time** subtract one hour from every time below (06:30–13:00 AZ).

**Golden rules:**
- MCC never places orders. You act in Schwab/Thinkorswim.
- READY is decision support, and it stays **shadow** (logged, no sound) until you've reviewed 10 sessions.
- Missing data shows as DATA?, never as a pass.

---

## Start and stop (from `D:\source\repos\MomentumCommandCenterV2`, PowerShell)

| When | Command |
|---|---|
| After Claude pushes changes (stop first, the build is locked while MCC runs) | `.\scripts\MCC.ps1 stop` then `.\scripts\Get-Latest-And-Test.ps1` (note the SHA and test count) |
| Start | `.\scripts\MCC.ps1 start`, then about 15 s later `.\scripts\MCC.ps1 warmup` |
| Open | `http://localhost:53360/`, then **Ctrl+F5** after any update |
| Status / stop | `.\scripts\MCC.ps1 status` · `.\scripts\MCC.ps1 stop` |
| Backup (MCC stopped) | `.\scripts\Backup-MccData.ps1` (goes to `D:\source\repos\MCC-Backups`) |

**Header (one slim row, pinned at the top):**
- title, **Morning Command Brief**, **History export**, the market clock;
- **Market chip:**
  - **▲ RISK-ON** (green): SPY and QQQ both trade above VWAP with EMA9 above EMA20.
  - **▼ RISK-OFF** (red): both are below VWAP with EMA9 below EMA20.
  - **◆ MIXED** (amber): anything in between.
  - **· closed** outside the regular session.
  - **· DATA?** when the data is stale.
  - Hover over the chip to see the inputs. It is awareness only, not a signal.
- **⚑ event flag** (shown only when one applies):
  - CPI, FOMC or the Jobs report **today** (red) or **next session** (amber; an overnight hold is exposed to it).
  - Times are shown in Arizona time;
- in BORING OVERNIGHT, one buy-window countdown: Buy window in → Buy window · closes in → Late window (LMT EXT) · ends in → CLOSED;
- **SYSTEM READINESS** (click to drop down the details).

Switch workspaces with the left **WORKSPACE** toggle: **INTRADAY MOMENTUM** or **BORING OVERNIGHT**. Each workspace shows only its own panels and step numbers.

---

## A. INTRADAY MOMENTUM (regular hours, 07:30–14:00 AZ)

**Before the open (about 07:15 AZ):**
1. Start MCC (above). Open the Dashboard and set WORKSPACE = **INTRADAY MOMENTUM**.
2. **Morning Command Brief** (button in the header): optional market lay of the land in a pop-up. It is awareness only; MCC's decisions do not use it.

**From 07:35 AZ (first completed 5-minute bar):**

3. **2.a Automatic momentum scan:**
   - The status line should read `COMPLETED`; it runs every 2 minutes, or click **Scan now**.
   - Check **Sources, feeds and limits** for any FAILED source.
   - The table shows symbols passing your scans 1 / 2A / 2B / PIV / D. Hover a ✓ / · / ? chip for the conditions.
   - Order: scans passed, then **From open** (% move since today's open). A **CHASE** row (red tag, dimmed) ranks last: **GAP_FADING** = up 10%+ on the day but below today's open (the move happened before the open, like SLS on 2026-11-17); **OFF_HIGH** = 10%+ below today's high.
   - Click **Feed Red Zone** to send the top 4 to the Red Zone.
4. **2.b Import TOS CSV** (bar below 2.a):
   - After your TOS scans, export the TOS Watchlist and import it here.
   - This records the side-by-side comparison with 2.a; once per session is enough.
   - The import takes over the Red Zone; click **Feed Red Zone** again to switch back.
5. **Exclusions** (INTRADAY MOMENTUM: left-dock button, the number = excluded symbols): opens the list in a pop-up; add a symbol you never want suggested.
   - **Catalysts** (left-dock button below it, in both workspaces): the list of pending binary events, such as trial readouts or FDA decisions.
   - **Earnings** (left-dock button below Catalysts, in both workspaces): this week's reports from the Nasdaq calendar, Monday to Friday with today highlighted; companies of $40B+ plus your symbols (imported scan, READY Symbols box, Red Zone cards), each tagged **before open** / **after close** / **time n/a**. The badge counts your symbols reporting this week. **‹ Prev / This week / Next ›** move between weeks; **Show every company** lists all reports. A report between the buy-day close and the sell-day open **BLOCKS** Boring Overnight READY (gate EARNINGS).
     - Add the symbol, the event, and the expected window as the source states it.
     - Listed symbols show **⚑ Binary catalyst pending** on Red Zone cards and can never reach READY in BORING OVERNIGHT.
     - Click **Remove (readout public)** once the result is out.
6. **4 Red Zone:**
   - Click **Enable sound alerts** once.
   - Each card's banner colour is its state:
     - **WAIT** grey;
     - **PREPARE TO BUY** yellow;
     - **EXECUTION READY** solid green;
     - **RUNNER** green outline;
     - **RUNNER WANING / PREPARE TO SELL** yellow;
     - **SELL / GET OUT** solid red;
     - **DATA?** dashed.
   - **4.1 Rank quality pool · top 50**: a live quality ranking of the imported list.
   - **4.2 Add a symbol → Pin to Red Zone**: keep a symbol on screen.
   - Each card's gate line reads **5M Permission: YES (TOS latch since h:mm AZ)** or **NO · reason**. This is the same latch as your TOS 5m study: once granted, it stays YES through a pullback until 2 bars close below VWAP with EMA9 under EMA20. The **V2 5M score n/6** above it is alignment only.
   - **Mode: Automatic / Manual**: switch between the automatic top 4 and your pins.
7. **5 Focused symbol:** click a card to load its chart, classification, decision explanation and 5M→1M handoff.
8. **6 Act in Schwab/TOS:**
   - **EXECUTION READY** → limit buy.
   - After you buy, click **Mark holding** on the card. Sell-side states then appear:
     - **RUNNER WANING / PREPARE TO SELL** → plan your limit sell;
     - **SELL / GET OUT** → sell.
   - Click the button again after you sell (or **Clear** on its Position watch tile).
   - **Position watch** (top of Main Chart, INTRADAY MOMENTUM): one gold tile per held stock with its state, last price, data age, **Focus** and **Clear**. Held Red Zone cards and a held Main Chart symbol get a gold outline.
   - When IEX has had no trade in the last 2 minutes but the 5M bar is fresh, a held stock shows a **5M ONLY** state instead of STALE: **HOLDING · 5M TREND INTACT** (permission YES), **HOLDING · 5M PERMISSION LOST** (plan your sell) or **HOLDING · 5M STRUCTURE BROKEN** (2 bars below VWAP with EMA9 < EMA20; plan your exit). The line says how long IEX has been quiet; check TOS 1M.

**Thin data:** a card's freshness line showing **1M data x/15 min THIN** means fewer than 80% of the last 15 minutes had an IEX trade. MCC then shows **PREPARE TO BUY** instead of EXECUTION READY, because its 1-minute picture rests on a few prints; check TOS 1M. Sell signals are not affected.

**Audit a signal:** click **Signal log** in the left dock (either workspace; the badge counts today's EXECUTION READY lines). Pick the **Date (Eastern)**, type a **Symbol** or leave it blank, and untick **Signals only** to also see WAIT and data-only changes; hover a row for the reason. The **Quality** column shows the grade at that moment.

**Hot or boring? (INTRADAY MOMENTUM):** when a no-position card or the Main Chart shows EXECUTION READY, MCC adds a grade: **QUALITY HIGH** (hot: 5M RVOL 2.5x+, momentum and range OK), **QUALITY MODERATE** (RVOL 1.5–2.5x), **QUALITY LOW** (ready but not exciting: RVOL under 1.5x, 1M momentum not confirmed, or the last 30 1M bars span under 1 ATR) or **EXTENDED** (late: 1.5+ ATR extended or a prior high within 0.5 ATR). The reason follows the step hint on the card. The grade is provisional and never changes EXECUTION READY; held stocks show the holder state instead. The same data is at `http://localhost:53360/api/market-data/signal-log?symbol=MWH` (add `&date=YYYY-MM-DD`). It lists every state change MCC showed for that symbol, with the time, price, coverage and reason. Only symbols the Dashboard was assessing are logged.

**Keep TOS open beside MCC:**
- MCC's 1M/5M states aren't yet proven to match your installed TOS studies (To-Do §5).
- MCC's live data is IEX, a partial feed, so volume figures differ from TOS.

---

## B. BORING OVERNIGHT (buy 13:00–14:00 AZ; late window to 15:00 AZ is observe-only; sell next day)

**About 12:30 AZ (prepare):**
1. Set WORKSPACE = **BORING OVERNIGHT**. In TOS, run your **Overnight scanner** (optionally with the V3 Edge / V3 Boringness / V3 Status columns) and export it to CSV.
2. **1 Import TOS CSV** (bar at the top): choose the export.
3. **2.1 Red Zone (BORING OVERNIGHT · HISTORICAL SHORTLIST):** the top 4 passing:
   - win ≥ 60%;
   - average ≥ 0.10%;
   - median ≥ average;
   - 5D > 20D;
   - today's gap under 3%;
   - above the 20-day SMA.
4. **Exclusions**, **Catalysts** and **Earnings** (BORING OVERNIGHT: left-dock buttons): check that nothing on the shortlist should be excluded or has a pending readout. A ⚑ card is blocked at READY (gate PENDING_BINARY_CATALYST).
5. **2.3 Candidate qualification:** read the **Boring** chips (your Top 4) and the counts row. The one-row heading has **Boring Top 4 / My Top 4**, **Choose My Top 4** (drop-down picker), **Fetch Daily + catalyst** and **ⓘ**.
6. **Optional, closed by default (click to open; MCC remembers):** **All imported rows** (the full table), **2.4 Daily Data Audit**, **2.5 Top 4 evidence**, **2.6 Catalyst review queue and label guide**. None is needed for READY: step 4 fetches its own 20-session history, Daily data and news, and READY cards label articles inline.
7. In **BORING OVERNIGHT · WORKFLOW**, only the four step bubbles and **4 READY check** are open; **3 Single-symbol diagnostics** (3.2–3.7) and **6.2 Thinkorswim Daily OHLC reference** are closed-by-default toggles. **Fetch Daily + catalyst** (2.3 heading) is optional too: every **Check READY now** fetches its own 20-session history, Daily data and news.

**13:00–14:00 AZ (buy window): 4 READY check**

8. **Pick and check:**
   - the **Symbols** box is filled from your active Top 4 group after the import (or click **Use Top 4**); edit it if you want other symbols, then click **Check READY now**;
   - **Re-check every 2 min during the buy windows** is on by default (MCC remembers if you untick it). It runs only inside the buy windows and skips any card you are typing in, so your notes are never wiped.
9. **Catalyst, on each card:**
   - for every article listed, open it, choose a label, write 40+ characters of facts and click **Save label**;
   - to fix an earlier label, open **Change label · LABEL · headline** under the article list, choose the new label, write the facts and click **Save label** (the newest label counts);
   - then tick the sources you checked and click **Sign off: nothing blocking**. Good news (an upgrade, a target raise) is not blocking: say so in the note.
10. **Read the card label:**
    - **READY (SHADOW — not yet validated)**: every gate passes;
    - **· LIMIT UNTESTED**: the limit is your +0.5% heuristic, not a tested target;
    - **WAITING FOR BUY WINDOW** (blue): outside the buy windows, and the only missing inputs are ones the live session fills (1-minute state, extension, volume, a live trade for the sell limit). Nothing is wrong; check again in the window;
    - **NOT READY / BLOCKED / DATA?**: read the red or grey gates; the arrows (→) say what to do. **BLOCKED** always shows first (for example negative news you labelled MATERIAL_RISK). **DATA?** now means a real gap (for example missing history).
11. **Act in Schwab:**
    - buy with a limit order;
    - then place the **GTC_EXT limit sell** at the card's limit, re-checked against your actual fill.

**14:00–15:00 AZ (late window): observe only, do not buy** (operator decision 2026-11-17)

12. The card shows **NOW IN LATE WINDOW**. Buy in the final hour (13:00–14:00 AZ) instead.
    - A late READY reads **READY · LATE — OBSERVE ONLY (do not buy)** on an amber card, never plays the sound, and is logged with its Day 2 outcome. The shadow log marks it **LATE (observe only)**.
    - To record a late check anyway, type the after-hours price you see in TOS into **Your after-hours price** and click **Re-check with this price** (the free feed has no after-hours trades). Above the regular close the card shows **WATCH — AFTER-HOURS UP x%**.
    - After about 20 logged late signals, compare their Day 2 outcomes with final-hour signals before deciding whether late READY should become actionable.

**After 14:16 AZ (research, optional, about 5 minutes):**

13. Click **History export** in the Dashboard header (next to Morning Command Brief). It exports 2-year overnight history and weekday statistics for your imported scan to `D:\source\repos\MCC-Data` (about a minute).
    - **Amber:** not run today. **Blue:** running. **Green "History ✓ time":** done today.
    - Hover for the summary. The script `.\scripts\Export-OvernightHistory.ps1` does the same and also counts. Research only.
14. V3 parity, until done: open `http://localhost:53360/api/boring-overnight/v3-column?symbol=PRU` and compare with the TOS V3 columns.

**Next day, 05:00–18:00 AZ (GTC_EXT sell window):** the limit order works on its own. The shadow log (**4 READY → Shadow log and Day 2 outcomes**) records whether the limit was touched.

---

## C. Thinkorswim scanners and watchlist columns: what each one is for

| Tool | Still needed? | How to use it |
|---|---|---|
| **TOS Overnight scanner** | **Yes.** MCC has no full-market daily scan (TOS scans about 15,500 stocks) | Show the eight Overnight columns MCC imports: **ON Win %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, and Overnight Score**, plus **V3 Edge / V3 Boringness / V3 Status**. Without the eight, the import works but every historical gate is DATA? and Boring Top 4 stays empty (MCC now says so at import and on Fetch). TOS exports whatever column set is showing, so switch the scan results to this set before exporting. Add the **V3 Rank** column and set **Sorted by → Custom Quotes → V3 Rank → Descending**: CANDIDATEs come first, best Boringness on top (300–400 = CANDIDATE, 200s WATCH, 100s BLOCKED). The labelled V3 columns can't be chosen in "Sorted by", because Thinkorswim greys out text-label columns. Export, then use **1 Import TOS CSV** in BORING OVERNIGHT. You don't need to add the symbols to a watchlist. |
| **TOS intraday scans 1, 2A, 2B, Pivot, D** | **For now, as the benchmark.** MCC 2.a runs the same rules automatically on free data | Keep running them until the 5-session comparison (step 2.b) shows how much MCC misses. Then decide whether to keep them. |
| **TOS intraday watchlist** | Yes, for charts | Columns, left to right: Symbol, Last, % Change, Volume, **VWAP Dist %**, **5M Trend**, **5M RVOL**, **5M Trigger**, **Ext ATR**. Files and install steps: `docs/Column Formulas/Intraday/README.md`. A candidate row is green across VWAP Dist / Trend 3/3 / RVOL / Trigger with Ext ATR not red. |
| **Scan aggregation** | — | Each **Study** row in a TOS scan has an aggregation button at its right: `D` = daily bars, `5m` = 5-minute bars. All your intraday scans, including Scanner 1, use `5m` (confirmed 2026-11-16). |

**What the history says (2026-11-16):**
- When your scanner's rules fired over the past 2 years, the next night won about **53%**, not the 60–80% the 20-night columns show.
- Treat each pick as a small edge, not a near-certainty.
- Details: [Overnight-History-Findings-20261116.md](Research/Overnight-History-Findings-20261116.md).

## Weekly

- Back up: `MCC.ps1 stop`, then `Backup-MccData.ps1`.
- Label a catalyst batch (To-Do §7).
- Review the shadow log, and the 2.a vs TOS comparison line (5 sessions).
