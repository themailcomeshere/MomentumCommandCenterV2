# MCC workflow steps — numbered map (INTRADAY MOMENTUM and BORING OVERNIGHT)

**Created:** 2026-11-16. The step numbers appear on the Dashboard as small blue badges. Each workspace mode shows only its own numbers, set by `data-step-intraday` / `data-step-overnight` attributes in `frontend/index.html`.
**Times:** Arizona, **standard time** (Nov–Mar): regular session 07:30–14:00 AZ. In daylight time it is 06:30–13:00 AZ.

How to read a number: `2` is a phase, `2.1` a step inside it, and `2.a` / `2.b` are alternative ways to do the same step.

---

## INTRADAY MOMENTUM (layout reordered 2026-11-16; the page now reads in this order)

| Step | What | Where on the Dashboard | When (AZ) | Status |
|---|---|---|---|---|
| **0** | **Setup and health** | | | |
| 0.1 | Choose the workspace | Left toggle **WORKSPACE** | 07:30 | Built |
| 0.2 | Market clock | Header **Market session clock** | always | Built |
| 0.3 | Data and system health | Header **SYSTEM READINESS** | always | Built |
| **1** | **Morning context** | | | |
| 1.1 | Morning brief (dialog) | Header button **Morning Command Brief** | 07:30 | Partial |
| 1.2 | Morning snapshot (collapsed panel) | **Morning Command Brief** panel, top of page | 07:30 | Partial |
| **2** | **Load candidates** (choose any) | | | |
| 2.a | Import your TOS Watchlist CSV | **Import a Thinkorswim CSV** bar, near the top | after your TOS scans, any time | **Built 2026-11-16** (shared with Overnight step 1) |
| 2.b | Alpaca top movers / API symbol scan | **Discovery tools** (collapsed) → **SCANNER WORKBENCH** | optional | Partial: top-50 movers only |
| 2.c | Local watchlist | **Discovery tools** → **WATCHLIST** | optional | Built (browser only) |
| **3** | **Exclusions** | **EXCLUSIONS · API-BACKED** (collapsed) | as needed | Built |
| **4** | **Red Zone: best 1–4, auto-refreshing** | **TOP CANDIDATES · Red Zone** | during session | Built: automatic mode takes the first four imported symbols (by a Score column if present, otherwise TOS watchlist order); cards refresh every 15 s |
| 4.1 | Live ranking of the imported pool | **Rank quality pool · top 50** | during session | Built |
| 4.2 | Pin a symbol | **Add a symbol** | any time | Built |
| **5** | **Focus one symbol and read its state** | | | |
| 5.1 | Symbol chart | **FOCUSED SYMBOL** | during session | Built |
| 5.2 | Live classification | **LIVE OPPORTUNITY CLASSIFICATION** | during session | Built; states shown as text, not TOS colours |
| 5.3 | Why this classification? | **DECISION EXPLANATION** | during session | Built |
| 5.4 | 5M authority → 1M execution | **TIMEFRAME HANDOFF** | during session | Built |
| **6** | **Act in Schwab** (6.a EXECUTION READY → limit buy; 6.b RUNNER WANING / PREPARE TO SELL → limit sell; 6.c SELL - BREAKDOWN / GET OUT → sell now) | Schwab | during session | Manual by design |
| **7** | **Records and diagnostics** (7.1 StrategyReport upload, 7.2 Provider comparison, 7.3 Dashboard notes) | bottom of page | not daily | Built |

## BORING OVERNIGHT (layout reordered 2026-11-16)

| Step | What | Where on the Dashboard | When (AZ) | Status |
|---|---|---|---|---|
| **0** | **Setup** (header; the clock shows the buy window and the next GTC_EXT sell window) | 0.1–0.3 | | Built |
| **1** | **Import the TOS Overnight scanner CSV** | **Import a Thinkorswim CSV** bar (same bar as Intraday 2.a) | before 13:00 | Built |
| **2** | **Shortlist and qualify** | | | |
| 2.1 | Historical shortlist | Red Zone (labelled **BORING OVERNIGHT · HISTORICAL SHORTLIST**) | after import | Built |
| 2.2 | Exclusion watchlist | **EXCLUSIONS** | as needed | Built |
| 2.3 | Candidate qualification table (Boring Top 4 / My Top 4) | **BORING OVERNIGHT · QUALIFICATION** | after import | Built |
| 2.4 | Daily data audit | **Daily Data Audit** | after import | Built |
| 2.5 | Daily + catalyst evidence for the Top 4 | **Top 4 live evidence** | after import | Built |
| 2.6 | Human catalyst review | **Human catalyst review queue** | after 2.5 | Built |
| **3** | **Per-symbol evidence** (3.1 chart, 3.2 Fetch & analyze bars, 3.3 20-pair history, 3.4 boundary audit, 3.5 40-window sell history, 3.6 sell-limit gates, 3.7 saved snapshots) | **FOCUSED SYMBOL**, then **Overnight Candidate Workspace** | any time | Built |
| **4** | **READY check** | end of **Overnight Candidate Workspace** | **13:00–14:00** | Built, shadow mode |
| **5** | **Act in Schwab**: buy in the final hour; GTC_EXT limit sell at MCC's limit | Schwab | 13:00–14:00 | Manual by design |
| **6** | **Optional research** (6.1 SPY control test, 6.2 TOS Daily OHLC parity, 6.3 Provider comparison) | after READY | rarely | Built |

Hidden in Boring Overnight (intraday-only): morning snapshot, Discovery tools, decision explanation, timeframe handoff, intraday metric cards. The Overnight field-availability report is hidden in Intraday.
## Layout problems the numbering exposed (2026-11-16) and their status

1. **No import in Intraday:** fixed. The import is a shared bar, Intraday 2.a / Overnight 1.
2. **Intraday ran backwards (5 → 4 → … → 2 → 1):** fixed. The page now reads 1 → 2 → 3 → 4 → 5 → 7.
3. **Red Zone not first:** improved. Steps 1–3 sit above it as compact or collapsed bars, so the Red Zone is the first full panel.
4. **Overnight scattered:** fixed. It now reads 1 → 2 → 3 → 4 → 6.
5. **Intraday-only panels showing in Overnight:** fixed (hidden).
6. **Found while fixing:** the Intraday Red Zone filtered imported rows with the **Boring Overnight** rules, so a normal TOS watchlist produced 0 cards. Fixed: Intraday no longer applies that filter. Also fixed: after switching from Overnight back to Intraday, the Red Zone kept the Overnight heading.
## Your desired workflow vs MCC today

| You want | MCC today | Gap |
|---|---|---|
| MCC scans for you (scanners 1, 2A, 2B, Momentum Pivot, D) and builds the watchlist | Candidates come from your TOS watchlist CSV import (now in both modes), Alpaca's top-50 movers list, or symbols you type | Your scanner formulas (`docs/Scanners/`) are **not** implemented in MCC, and MCC has no market-wide universe to run them over |
| Red Zone = best 4 right now, auto-refreshing | Automatic mode exists; it ranks the imported scanner score or Alpaca discoveries, and cards refresh every 15 s | Good mechanics; the input quality is limited by the gap above |
| Clear colour-coded states: heating up / EXECUTION READY / winding down / sell now | MCC computes 9 states (`SignalState`: NoTrade, PrepareBuy, Buy, Runner, RunnerWaning, PrepareSell, ConfirmedWeakness, Breakdown, Exit) and shows them as text, with 3 generic colours | Your TOS studies colour each state. 1M: WAIT grey, PREPARE yellow, EXECUTION READY green, EXECUTION EXPIRED yellow, SWING RECLAIM orange, PREPARE TO SELL yellow, all SELL/GET OUT states red. 5M: RUNNER RISING/RUNNER green, RUNNER WANING yellow, SWING ZONE orange. MCC does not mirror those colours, and does not alert per state change |
| States match your installed TOS studies | MCC's V2 engine reproduces the studies' logic from Alpaca bars | Parity with the installed 1M/5M studies is **not evidenced** (To-Do §5) |
| Live data good enough to trade on | Live 1M/5M bars are **IEX** (partial exchange coverage); recent SIP is refused (HTTP 403) | IEX volume and RVOL differ from TOS. A full-market live scanner needs a live consolidated feed. Data not found in sources for a free option: the reviewed paid options are Alpaca Algo Trader Plus ($99/month) and Massive Stocks Advanced ($199/month), prices checked 2026-11-14 |

**What drifted:** most recent work (2026-11-13 → 11-14) went into Boring Overnight (SIP history, sell limit, READY). Your daily core is INTRADAY MOMENTUM at 07:30 AZ, and its biggest gaps (import, layout, state colours, automatic scanning) were not worked on in that period.

## Proposed order of work (the operator decides)

1. ~~Intraday import~~: done 2026-11-16.
2. ~~Reorder both layouts~~: done 2026-11-16.
3. **State colours and alerts on Red Zone cards:** mirror the TOS colours above, and add per-transition alerts (e.g. a sound when a symbol turns PREPARE TO BUY or EXECUTION READY, a different one for SELL).
4. **Automatic scanning:** implement scanners 1, 2A, 2B, Momentum Pivot and D as MCC rules. This first needs a decision on the live data source, because IEX cannot reproduce TOS volume-based scans.
5. **1M/5M parity evidence** against the installed studies.

Boring Overnight keeps running in shadow mode in the meantime.
