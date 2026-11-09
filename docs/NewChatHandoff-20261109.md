# November 9, 2026 — Boring Overnight UI implementation handoff

**Branch:** `feature/opportunity-quality-classification`  
**Base:** `4ea0c46db0d80676b192e810a84979f2b675d018`

## Implemented

- Focused-symbol metrics: Previous Regular-Session Close, Latest Price (labelled AFTER-HOURS / EXTENDED when applicable), and Calculated Limit Sell.
- Prices use returned Alpaca 1-minute bars over five calendar days and show Phoenix timestamps. Latest bar close is not a live quote. Previous Regular-Session Close excludes an in-progress RTH session; exact 16:00 ET close is claimed only when the 15:59 ET bar is present.
- The collapsible Decision Explanation now carries the 1M close, RTH Daily VWAP, simple 1M ATR14, and VWAP extension diagnostics from the same returned bars. Missing volume/high/low/history remains DATA?. Provider/feed/time provenance is shown. These values are descriptive only and do not establish qualification or execution permission.
- Quick limit-sell action delegates to the existing quantile/evidence-gate handler and `POST /api/boring-overnight/sell-limit`; existing evidence gates remain and no order is submitted.
- Boring Overnight Red Zone cards use historical 5-minute bars, hide redundant 1M/VWAP/ATR/freshness paragraphs, and mark intraday 5M/1M gates NOT USED.
- Manual pins are polled in background during Automatic mode. Sound requires a prior known false followed by true; UNKNOWN→true does not alert.
- Scanner Workbench Symbols auto-populates with up to 20 unique imported Overnight symbols sorted by Overnight Score. Manual **Use Overnight Top 20** button is available and does not submit the scan.
- Removed Red Zone static chip/paragraph and redundant label ID. Preserved `.gate-warning` and Ticker placeholder.
- Decision Explanation and Timeframe Handoff are collapsible with pointer cursor. Timeframe Handoff is hidden in Boring Overnight mode.

## Operator acceptance

1. Run `.\scripts\Get-Latest-And-Test.ps1`, restart API, hard-refresh Dashboard.
2. Select BORING OVERNIGHT and verify all three focused-symbol metrics and timestamps.
3. Confirm latest price is a bar close, not a live quote; exact 16:00 ET close only when 15:59 ET bar exists.
4. Confirm Red Zone cards show Overnight research / intraday gates NOT USED.
5. Set research quantile in existing Calculated Limit Sell panel; click focused-symbol **Assess calculated limit sell** and verify evidence gates. IEX/missing evidence should remain DATA? / NOT CALCULATED.
6. Enable sound and test a manually pinned symbol outside Automatic top four through a known false→true execution-ready transition.
7. Import scanner CSV; verify up to 20 unique symbols sorted by Overnight Score. Confirm Scan & Analyze is not automatically submitted.
8. Verify disclosures open/close. Timeframe Handoff hidden in Boring Overnight and available in Intraday.

## Boring Winner — implemented as a separate research calculation

The TOS Daily OHLC calculator now returns the Boring Winner result separately from the eight active Overnight Scanner metrics. It evaluates:
- 20-session win rate ≥ 60%
- 20-session average return between +0.10% and +3.00%, inclusive
- At least 10 of the last 20 returns within +0.10%…+3.00%
- 20-session sample standard deviation ≤ 3.00%
- Current daily close above the 20-day SMA

The API result exposes pass/fail, in-range count, and sample standard deviation. The Dashboard displays this as **RESEARCH ONLY** after importing at least 21 TOS daily OHLC bars. It does not change the existing Overnight Score, historical qualification, or READY state. A unit test covers a qualifying 20-return sample.

Remaining: validate against chronological, source-qualified data and confirm the operator's actual TOS daily aggregation/session settings. Do not promote this to a production gate or TOS V3 study until those checks pass.

## [Uncertainties]

- Provider may return no after-hours bars or partial IEX observations; values remain DATA? or latest timestamp.
- A 1-minute bar close is not an NBBO quote or guaranteed executable price.
- Sell-limit API remains fail-closed.
- Browser/runtime acceptance and latest branch-head CI are pending.
- Background monitoring uses the visible-page refresh cycle.

---

