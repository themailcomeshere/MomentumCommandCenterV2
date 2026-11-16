# MCC Operator Guide — daily use

**Updated:** 2026-11-16 (code head `0962645`). This is the single page for using MCC day to day. Step numbers match the blue badges on the Dashboard. The detailed map is in [MCC-Workflow-Steps.md](Engineering/MCC-Workflow-Steps.md).

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

Switch workspaces with the left **WORKSPACE** toggle: **INTRADAY MOMENTUM** or **BORING OVERNIGHT**. Each workspace shows only its own panels and step numbers.

---

## A. INTRADAY MOMENTUM (regular hours, 07:30–14:00 AZ)

**Before the open (about 07:15 AZ):**
1. Start MCC (above). Open the Dashboard and set WORKSPACE = **INTRADAY MOMENTUM**.
2. **1.2 Morning Command Brief** (collapsed panel at the top): expand it for the market snapshot if you want it.

**From 07:35 AZ (first completed 5-minute bar):**

3. **2.a Automatic momentum scan:**
   - The status line should read `COMPLETED`; it runs every 2 minutes, or click **Scan now**.
   - Check **Sources, feeds and limits** for any FAILED source.
   - The table shows symbols passing your scans 1 / 2A / 2B / PIV / D. Hover a ✓ / · / ? chip for the conditions.
   - Click **Feed Red Zone** to send the top 4 to the Red Zone.
4. **2.b Import TOS CSV** (bar below 2.a):
   - After your TOS scans, export the TOS Watchlist and import it here.
   - This records the side-by-side comparison with 2.a; once per session is enough.
   - The import takes over the Red Zone; click **Feed Red Zone** again to switch back.
5. **3 Exclusions** (collapsed): add a symbol you never want suggested.
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
   - **Mode: Automatic / Manual**: switch between the automatic top 4 and your pins.
7. **5 Focused symbol:** click a card to load its chart, classification, decision explanation and 5M→1M handoff.
8. **6 Act in Schwab/TOS:**
   - **EXECUTION READY** → limit buy.
   - After you buy, click **Mark holding** on the card. Sell-side states then appear:
     - **RUNNER WANING / PREPARE TO SELL** → plan your limit sell;
     - **SELL / GET OUT** → sell.
   - Click the button again after you sell.

**Keep TOS open beside MCC:**
- MCC's 1M/5M states aren't yet proven to match your installed TOS studies (To-Do §5).
- MCC's live data is IEX, a partial feed, so volume figures differ from TOS.

---

## B. BORING OVERNIGHT (buy 13:00–14:00 AZ, late window to 15:00 AZ; sell next day)

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
4. **2.2 Exclusions:** check that nothing on the shortlist should be excluded.
5. **2.3 Qualification table:** all candidates with reasons.
6. **2.4 Daily Data Audit:**
   - click **Use Boring Top 20**, then **Validate Daily data**;
   - expect `sip` / 20/20 / `QUALIFICATION USABLE`.
7. **2.5 Top 4 live evidence → 2.6 Human catalyst review queue:** news for the active top 4.

**13:00–14:00 AZ (buy window): 4 READY check**

8. **Pick and check:**
   - type your picks into **Symbols** (e.g. `PRU,AFL`) and click **Check READY now**;
   - tick **Re-check every 2 min during the buy windows**.
9. **Catalyst, on each card:**
   - for every article listed, open it, choose a label, write 40+ characters of facts and click **Save label**;
   - then tick the sources you checked and click **Sign off: nothing material**.
10. **Read the card label:**
    - **READY (SHADOW — not yet validated)**: every gate passes;
    - **· LIMIT UNTESTED**: the limit is your +0.5% heuristic, not a tested target;
    - **NOT READY / BLOCKED / DATA?**: read the red or grey gates; the arrows (→) say what to do.
11. **Act in Schwab:**
    - buy with a limit order;
    - then place the **GTC_EXT limit sell** at the card's limit, re-checked against your actual fill.

**14:00–15:00 AZ (late window, optional):**

12. The card shows **NOW IN LATE WINDOW**.
    - The free data feed has **no after-hours trades**, so type the after-hours price you see in TOS into **Your after-hours price** and click **Re-check with this price**.
    - READY needs that price at or below the regular close; otherwise the card shows **WATCH — AFTER-HOURS UP x%**.
    - Use a **LMT EXT** order.

**After 14:16 AZ (research, optional, about 5 minutes):**

13. `.\scripts\Export-OvernightHistory.ps1` (MCC running). It reads your newest `*WatchListScanner*.csv` from Documents, archives it to `D:\source\repos\MCC-Data\scanner-exports`, and writes 2-year overnight history plus weekday statistics to `D:\source\repos\MCC-Data\overnight-history\<time>\`. Research only.
14. V3 parity, until done: open `http://localhost:53360/api/boring-overnight/v3-column?symbol=PRU` and compare with the TOS V3 columns.

**Next day, 05:00–18:00 AZ (GTC_EXT sell window):** the limit order works on its own. The shadow log (**4 READY → Shadow log and Day 2 outcomes**) records whether the limit was touched.

---

## Weekly

- Back up: `MCC.ps1 stop`, then `Backup-MccData.ps1`.
- Label a catalyst batch (To-Do §7).
- Review the shadow log, and the 2.a vs TOS comparison line (5 sessions).
