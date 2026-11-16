# MCC — layout recommendations and live-readiness assessment (2026-11-16)

This is advice for the operator, who will edit `frontend/index.html` and `styles.css` personally. Nothing in this document is built.

## 1. Layout: what similar tools do, and what MCC should copy

**Common pattern:** day-trading desks such as Thinkorswim, TradingView, Trade Ideas, DAS Trader and Benzinga Pro share a pattern, not a single design:
- a **fixed multi-pane grid on one screen**, not a long scrolling page;
- a **narrow list on the left** (scanner or watchlist), the **chart in the middle**, and **alerts/news on the right or bottom**;
- **status shown by colour and position**, with text only on demand;
- **no scrolling during the session**.

**Where MCC is today:** one long page, reordered per workspace with CSS `order`, with long explanatory paragraphs on each card. That suits learning and auditing. During the session it's slow: the Red Zone, the chart and the scan table can't all be seen at once on one monitor.

### Recommended: an Intraday cockpit (one 1920×1080 screen, no scrolling)

```
┌─────────────────────────────── sticky top bar ────────────────────────────────┐
│ WORKSPACE · clock (session/countdown) · system readiness · sound on/off · alerts │
├──────────────┬──────────────────────────────────────┬──────────────────────────┤
│ 2.a SCAN     │ 4 RED ZONE — 2×2 cards               │ 5 FOCUSED SYMBOL         │
│ compact list │ banner colour = state, price, %chg,  │ chart (1M with VWAP)     │
│ sym %chg     │ RVOL, extension, time-in-state,      │ state + 5M/1M handoff    │
│ 1 2A 2B PIV D│ Mark holding                         │ one-line "why"           │
│ (click → pin)│                                      │                          │
├──────────────┴──────────────────────────────────────┴──────────────────────────┤
│ event log strip: 10:42:13 AZ PRU → EXECUTION READY · … (click → focus)           │
└───────────────────────────────────────────────────────────────────────────────┘
  Collapsed drawers: Morning brief · Import · Exclusions · Ranking · Diagnostics · Records
```

1. **Show one workspace at a time** (tabs, or `body.boring-overnight-mode` hiding everything else). Replace one long page with two screens.
2. **Red Zone cards: 6 facts, not paragraphs.** Banner state, price, % change, RVOL, extension in ATR, and time in state. Move "Freshness: …" and the reason paragraph into a hover or expand.
3. **Use state colour only for state.** Everything else stays neutral, so green and red always mean "act".
4. **Put an event log strip at the bottom.** State changes with time, clickable. This is how scanners like Trade Ideas present alerts, and it replaces hunting for what changed.
5. **Diagnostics in drawers:** provider comparison, StrategyReport upload, field availability, notes.
6. **Numbers right-aligned, tabular figures** (the 2.a table already does this). Short labels: `RVOL 2.4×` rather than "5M participation: 2.40x same-slot RVOL…".

### Recommended: an Overnight desk (one table instead of 2.1–2.6 + 4)

One row per candidate, columns left to right in the order you decide:

`Symbol | Edge | Boringness | Win% | Avg/Med | 5D | Today ON | Daily audit ✓ | News (n to label) | Sign-off | READY state | Limit | [Check]`

Clicking a row opens a drawer with the gates, the articles to label and the sign-off. The current panels stay as the "detail" view. This removes the scroll between 2.1 and 4, and puts the candidates side by side when you choose 1–2 names.

### Where to edit

- Section order and visibility per workspace: the block at the **end of `frontend/styles.css`** (`#mcc-static-0049-main` flex order, `body.boring-overnight-mode … {display:none}`).
- Card content: `renderMorningReport` (Red Zone) and `renderReadyCard` (READY) in `frontend/app.js`.
- After editing, bump the `?v=` cache keys in `index.html`, then press Ctrl+F5.

## 2. How close is MCC to live use?

| Area | Ready now | Not yet | What closes the gap |
|---|---|---|---|
| **Boring Overnight** | Import, shortlist, Daily SIP audit, V3 scores, READY with catalyst labelling and sign-off, limit (tested or LIMIT UNTESTED), late window with typed price, shadow log | 0 of 10 shadow sessions reviewed; sell limit untested for most symbols; V3 TOS columns not compiled; catalyst sample small | 10 logged sessions (about 2 weeks), then you decide whether to switch READY to live |
| **Intraday Momentum** | Auto-scan (free data), Red Zone with state colours and alerts, TOS import, ranking, Mark holding | Auto-scan not yet run live; 1M/5M states not proven to match your TOS studies; live data is IEX (partial volume) | 5-session scan comparison; 1M/5M parity export (To-Do §5); data-source decision ($99 Alpaca Algo Trader Plus if coverage is short) |
| **Orders** | Never, by design | — | Schwab integration is parked; you place every order |

**Overall:**
- **Boring Overnight** is usable now as decision support in shadow mode. You can trade from it at your own discretion while it logs.
- **Intraday** is usable as a **second opinion** next to TOS, not as the primary signal, until parity and the data source are settled.

## 3. Two monitors? Is TOS still needed?

**Yes, two monitors. TOS stays necessary.**
- **TOS/Schwab is where you place orders**, and MCC never will, so TOS is permanent for execution.
- **Until parity is proven, TOS charts are the authority** for 1M/5M states. MCC's states come from IEX bars, and its engine isn't yet shown to match your installed studies.
- **TOS scanners stay as the benchmark** until the 5-session comparison shows MCC's free scan finds what you need. After that, TOS scanning becomes optional.

Suggested setup:
- **Monitor 1:** MCC (Red Zone cockpit, or the Overnight desk).
- **Monitor 2:** TOS with the chart for the focused symbol and the order ticket.

**Long term:** TOS for orders (plus a confirming chart); MCC for scanning, ranking and signals. That needs the parity evidence and, for intraday, a consolidated live feed.
