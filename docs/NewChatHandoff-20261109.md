# November 9, 2026 — Boring Overnight UI implementation handoff

**Branch:** `feature/opportunity-quality-classification`  
**Base:** `4ea0c46db0d80676b192e810a84979f2b675d018`

## Implemented

- Focused-symbol metrics: Previous Regular-Session Close, Latest Price (labelled AFTER-HOURS / EXTENDED when applicable), and Calculated Limit Sell.
- Prices use returned Alpaca 1-minute bars over five calendar days and show Phoenix timestamps. Latest bar close is not a live quote. Exact 16:00 ET close is claimed only when the 15:59 ET bar is present.
- Quick limit-sell action delegates to the existing quantile/evidence-gate handler and `POST /api/boring-overnight/sell-limit`; existing evidence gates remain and no order is submitted.
- Boring Overnight Red Zone cards use historical 5-minute bars and mark intraday 5M/1M gates NOT USED.
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

## Boring Winner

Boring Winner is an active strategy-development objective, not a production-ready gate. Its formula is documented; source-qualified chronological validation and outcome calibration remain open. Do not mark candidates READY from this formula alone.

## [Uncertainties]

- Provider may return no after-hours bars or partial IEX observations; values remain DATA? or latest timestamp.
- A 1-minute bar close is not an NBBO quote or guaranteed executable price.
- Sell-limit API remains fail-closed.
- Browser/runtime acceptance and latest branch-head CI are pending.
- Background monitoring uses the visible-page refresh cycle.

---

