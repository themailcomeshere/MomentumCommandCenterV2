# MCC workflow steps — numbered map (INTRADAY MOMENTUM and BORING OVERNIGHT)

**Created:** 2026-11-16. The step numbers appear on the Dashboard as small blue badges. Each workspace mode shows only its own numbers, set by `data-step-intraday` / `data-step-overnight` attributes in `frontend/index.html`.
**Times:** Arizona, **standard time** (Nov–Mar): regular session 07:30–14:00 AZ. In daylight time it is 06:30–13:00 AZ.

How to read a number: `2` is a phase, `2.1` a step inside it, and `2.a` / `2.b` are alternative ways to do the same step.

---

## INTRADAY MOMENTUM

| Step | What | Where on the Dashboard | When (AZ) | Status |
|---|---|---|---|---|
| **0** | **Setup and health** | | | |
| 0.1 | Choose the workspace | Left toggle **WORKSPACE** | 07:30 | Built |
| 0.2 | Market clock (session, time left) | Header **Market session clock** | always | Built |
| 0.3 | Data and system health | Header **SYSTEM READINESS** | always | Built |
| **1** | **Morning context** | | | |
| 1.1 | Morning brief | Header button **Morning Command Brief** | 07:30 | Partial: snapshot only, no validated regime or probabilities |
| 1.2 | Market snapshot | **MORNING REPORT · TARGET WORKFLOW**, bottom of the page | 07:30 | Partial |
| **2** | **Discover candidates** (your TOS routine: scanners 1, 2A, 2B, Momentum Pivot, D → Watchlist) | | | |
| 2.a | Alpaca top movers / API scan of typed symbols | **SCANNER WORKBENCH** | 07:30 onward | Partial: top-50 movers list only; your TOS scanner formulas are not run by MCC |
| 2.b | Pin a symbol by hand | Red Zone **Add a symbol** | any time | Built |
| 2.c | Working watchlist (browser-only list) | **WATCHLIST · Working candidates** | any time | Built (local to this browser) |
| 2.d | Import your TOS Watchlist CSV | *Not reachable in this mode* (the only import is Boring Overnight step 1) | — | **Missing in Intraday** |
| **3** | **Filter and rank** | | | |
| 3.1 | Exclusion watchlist | **EXCLUSIONS · API-BACKED** | as needed | Built |
| 3.2 | Rank quality pool (top 50 imported rows) | Red Zone button **Rank quality pool · top 50** | during session | Built; depends on 2.d, which is missing here |
| **4** | **Red Zone: best 1–4 symbols, auto-refreshing** | **TOP CANDIDATES · MULTI-PANE MONITOR** | during session | Partial: automatic mode picks from imported scanner score or Alpaca discoveries; cards refresh every 15 s |
| **5** | **Focus one symbol and read its state** | | | |
| 5.1 | Symbol chart | **FOCUSED SYMBOL** | during session | Built (Alpaca IEX 1M/5M) |
| 5.2 | Live classification (5M permission, 1M execution, state) | **LIVE OPPORTUNITY CLASSIFICATION** | during session | Built; states shown as text, not TOS colours |
| 5.3 | Why this classification? | **DECISION EXPLANATION** | during session | Built |
| 5.4 | 5M authority → 1M execution handoff | **TIMEFRAME HANDOFF** | during session | Built |
| **6** | **Act (outside MCC: Schwab web or app)** | | | |
| 6.a | EXECUTION READY → place a limit buy | Schwab | during session | Manual by design |
| 6.b | RUNNER WANING / PREPARE TO SELL → place a limit sell | Schwab | during session | Manual by design |
| 6.c | SELL - BREAKDOWN / GET OUT → sell now | Schwab | during session | Manual by design |
| **7** | **Records and diagnostics (not daily)** | | | |
| 7.1 | Upload TOS StrategyReport CSV | **THINKORSWIM · CSV** | after session | Built |
| 7.2 | Provider comparison | **DATA PROVIDER DIAGNOSTICS** | rarely | Built |
| 7.3 | Dashboard notes | **PRODUCT FEEDBACK** | any time | Built |

**Page order today (top to bottom):** 0.1, 1.1, 0.2, 0.3, **5.1, 5.2, 4, 3.2, 2.b, 5.3, 5.4, 2.a, 2.c, 3.1**, 7.1, 7.2, **1.2**, 7.3.

## BORING OVERNIGHT

| Step | What | Where on the Dashboard | When (AZ) | Status |
|---|---|---|---|---|
| **0** | **Setup** (same header; the clock shows the buy window and the next GTC_EXT sell window) | 0.1 / 0.2 / 0.3 | | Built |
| **1** | **Import the TOS Overnight scanner CSV** | Top of **Overnight Candidate Workspace** | before 13:00 | Built |
| **2** | **Shortlist and qualify** | | | |
| 2.1 | Historical shortlist | Red Zone (labelled **BORING OVERNIGHT · HISTORICAL SHORTLIST** in this mode) | after import | Built |
| 2.2 | Candidate qualification (Boring Top 4 / My Top 4) | **BORING OVERNIGHT · QUALIFICATION** | after import | Built |
| 2.3 | Exclusion watchlist | **EXCLUSIONS** | as needed | Built |
| 2.4 | Daily data audit (SIP, 20/20) | **Daily Data Audit** | after import | Built |
| 2.5 | Daily + catalyst evidence for the Top 4 | **Top 4 live evidence** | after import | Built |
| 2.6 | Human catalyst review | **Human catalyst review queue** | after 2.5 | Built |
| **3** | **Per-symbol evidence** | | | |
| 3.1 | Symbol chart | **FOCUSED SYMBOL** | any time | Built |
| 3.2 | Fetch and analyse bars | **Fetch & analyze bars** | any time | Built |
| 3.3 | 20-pair close/open history (delayed SIP) | **Fetch / retry 20-session close/open history** | any time | Built |
| 3.4 | Boundary audit | **Show exact boundary coverage audit** | optional | Built |
| 3.5 | 40-window GTC_EXT sell history (delayed SIP) | **Fetch 40-session GTC_EXT sell-window history** | any time | Built |
| 3.6 | Sell-limit evidence gates | **Assess sell-limit evidence gates** | any time | Built |
| 3.7 | Saved input snapshots | **Saved Overnight input snapshots** | optional | Built |
| **4** | **READY check** (runs 3.3, the catalyst gate and the sell limit itself) | **BORING OVERNIGHT · READY SIGNAL** | **13:00–14:00** | Built, shadow mode |
| **5** | **Act (Schwab)**: buy in the final hour; GTC_EXT limit sell at MCC's limit | Schwab | 13:00–14:00 | Manual by design |
| **6** | **Optional research** | | | |
| 6.1 | Ticker vs SPY control test | **Optional diagnostic: compare this ticker with SPY** | rarely | Built |
| 6.2 | TOS Daily OHLC parity | **Thinkorswim Daily OHLC reference** | rarely | Built |
| 6.3 | Provider comparison | **DATA PROVIDER DIAGNOSTICS** | rarely | Built |

**Page order today:** 0.x, **3.1, 2.1, 1, 4**, 3.2–3.4, **6.1**, 3.5–3.7, **2.2, 2.6, 2.4**, 6.2, **2.5, 2.3**, 6.3.

---

## Layout problems the numbering exposes

1. **Intraday has no import (2.d missing).** The only TOS CSV import is Boring Overnight step 1, yet the Red Zone says "CSV imports update Red Zone immediately", and 3.2 ranks "imported" rows. In INTRADAY MOMENTUM you cannot get your TOS watchlist into MCC.
2. **Intraday runs backwards.** The page opens on 5 (focus a symbol), then 4 (Red Zone). Discovery (2.a, 2.c) is near the bottom, and the morning snapshot (1.2) is second to last. Your routine is 1 → 2 → 4 → 5.
3. **The Red Zone is not first on screen.** Your design is NFL-Red-Zone style: the top 1–4 symbols front and centre, refreshing themselves. Today the single focused-symbol chart sits above it.
4. **Overnight is scattered.** READY (4) sits above the evidence steps (3), and qualification (2.2–2.6) sits below the evidence steps. The SPY control test (6.1) sits between 3.4 and 3.5.
5. **Intraday-only panels show in Boring Overnight.** The decision explanation and the five intraday metric cards remain visible, with nothing to say in that mode.

## Your desired workflow vs MCC today

| You want | MCC today | Gap |
|---|---|---|
| MCC scans for you (scanners 1, 2A, 2B, Momentum Pivot, D) and builds the watchlist | Candidates come from a TOS CSV import (Overnight mode only), Alpaca's top-50 movers list, or symbols you type | Your scanner formulas (`docs/Scanners/`) are **not** implemented in MCC, and MCC has no market-wide universe to run them over |
| Red Zone = best 4 right now, auto-refreshing | Automatic mode exists; it ranks the imported scanner score or Alpaca discoveries, and cards refresh every 15 s | Good mechanics; the input quality is limited by the gap above |
| Clear colour-coded states: heating up / EXECUTION READY / winding down / sell now | MCC computes 9 states (`SignalState`: NoTrade, PrepareBuy, Buy, Runner, RunnerWaning, PrepareSell, ConfirmedWeakness, Breakdown, Exit) and shows them as text, with 3 generic colours | Your TOS studies colour each state. 1M: WAIT grey, PREPARE yellow, EXECUTION READY green, EXECUTION EXPIRED yellow, SWING RECLAIM orange, PREPARE TO SELL yellow, all SELL/GET OUT states red. 5M: RUNNER RISING/RUNNER green, RUNNER WANING yellow, SWING ZONE orange. MCC does not mirror those colours, and does not alert per state change |
| States match your installed TOS studies | MCC's V2 engine reproduces the studies' logic from Alpaca bars | Parity with the installed 1M/5M studies is **not evidenced** (To-Do §5) |
| Live data good enough to trade on | Live 1M/5M bars are **IEX** (partial exchange coverage); recent SIP is refused (HTTP 403) | IEX volume and RVOL differ from TOS. A full-market live scanner needs a live consolidated feed. Data not found in sources for a free option: the reviewed paid options are Alpaca Algo Trader Plus ($99/month) and Massive Stocks Advanced ($199/month), prices checked 2026-11-14 |

**What drifted:** most recent work (2026-11-13 → 11-14) went into Boring Overnight (SIP history, sell limit, READY). Your daily core is INTRADAY MOMENTUM at 07:30 AZ, and its biggest gaps (import, layout, state colours, automatic scanning) were not worked on in that period.

## Proposed order of work (the operator decides)

1. **Intraday import (2.d):** make the TOS watchlist CSV import available in INTRADAY MOMENTUM. This is small and unblocks Red Zone ranking today.
2. **Reorder both layouts by step number:** Intraday 0 → 1 → 2 → 3 → 4 (Red Zone at the top) → 5 → 7; Overnight 1 → 2 → 3 → 4 → 6. Hide intraday-only panels in Overnight.
3. **State colours and alerts on Red Zone cards:** mirror the TOS colours above, and add per-transition alerts (e.g. a sound when a symbol turns PREPARE TO BUY or EXECUTION READY, a different one for SELL).
4. **Automatic scanning:** implement scanners 1, 2A, 2B, Momentum Pivot and D as MCC rules. This first needs a decision on the live data source, because IEX cannot reproduce TOS volume-based scans.
5. **1M/5M parity evidence** against the installed studies.

Boring Overnight keeps running in shadow mode in the meantime.
