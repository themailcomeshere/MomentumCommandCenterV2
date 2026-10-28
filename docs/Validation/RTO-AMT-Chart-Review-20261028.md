# RTO and AMT Chart Review — 2026-10-28

**Evidence reviewed:** Project Source screenshots `Screenshot 2026-10-28 160419.png` (RTO) and `Screenshot 2026-10-28 152731.png` (AMT). Thinkorswim chart timezone appears Arizona; chart headers show 2026-10-28. This is a visual study review, not a current live quote check.

## RTO

### Observed on the 5-minute / multi-day pane
- Header is RTO, 5m, with displayed price about **$22.79** at the right edge.
- Status strip shows `5M STATE: RUNNER RISING`, `5M PERMISSION: YES`, `5M STRUCTURE: RUNNER`, `BULLISH REGIME: ESTABLISHED`, `RVOL: 4.32`, `RSI: 61.1`, `EXTENSION: 0.49 ATR`, `RUNNER MOMENTUM: RISING`, and `1M EXECUTION ENVIRONMENT: YES`.
- The visible recent session is mostly range-bound around the upper-$22s after an earlier decline; a short-lived spike and reversal appear on the prior visible session. There is no visual basis here to call the entire symbol low-risk solely from its daily chart.
- The lower 1-minute pane shows a prolonged soft/downward session structure, a late recovery, and a red `1M STATE: GET OUT` / `EOD: GET OUT` state. Its status strip also shows `5M PERMISSION: NO`. The differing top/lower status displays are pane/time-context specific and must not be merged into one inferred state without timestamp alignment.

### Interpretation
RTO remains the best *historical boring-profile* candidate among the original shortlist, but **this screenshot does not establish a fresh overnight entry**. The 1M EOD/GET OUT state is an exit/late-session state, not an overnight-buy authorization. The late rebound follows intraday weakness; inspect the actual intended entry-time snapshot and catalyst/freshness gates before any READY decision. The 5M extension displayed as 0.49 ATR is comparatively modest at that pane's current bar, but it does not erase the lower-pane deterioration or EOD state.

## AMT

### Observed on the 5-minute / multi-day pane
- Header is AMT, 5m, with displayed price about **$184.02** at the right edge.
- Status strip shows `5M STATE: RUNNER RISING`, `5M PERMISSION: YES`, `5M STRUCTURE: BULLISH`, `BULLISH REGIME: ESTABLISHED`, `RVOL: 0.01`, `RSI: 56.9`, `EXTENSION: 2.03 ATR`, `RUNNER MOMENTUM: RISING`, and `1M EXECUTION ENVIRONMENT: YES`.
- The visible price path has a sharp selloff to a marked low around **$175.78**, followed by a strong recovery toward $184. This is a large intraday excursion rather than a quiet, narrow range.
- The lower 1-minute pane shows the same broad decline/recovery shape and ends with red `1M STATE: GET OUT` / `EOD: GET OUT` labels. Its strip shows `5M PERMISSION: NO`; this pane's state must be interpreted at its own displayed time, not replaced with the top pane's label.

### Interpretation
AMT is **not the preferred Boring Overnight candidate for this session**. The visible rebound may be technically meaningful, but the large range and 2.03-ATR extension indicate substantial movement relative to the study's ATR measure. The EOD/GET OUT state is not a buy signal. AMT remains second in historical-screening priority only; today's chart increases the reason to defer it rather than promote it.

## Updated shortlist

1. **RTO — first for further review, not READY.** Historical metrics and relatively modest 5M extension look better, but lower-pane deterioration/EOD means no entry authorization from these screenshots.
2. **AMT — defer for this session.** Strong rebound, but large intraday excursion, 2.03-ATR extension and EOD/GET OUT state conflict with a boring-first entry.
3. **SXC — remains third / caution.** No new SXC chart was reviewed in this pass; its previously reported 6.20% MCC range remains a material low-noise concern.

No candidate is promoted to `READY`. The screenshots do not establish price/20D-SMA gate, 20-session average volume, complete historical sample quality, current catalyst clearance, exclusion status, spread, or source freshness. The displayed RVOL and ATR extension are TOS study values at the chart's selected bar; they are not a replacement for independent, source-backed MCC calculations.

## MCC implementation implications

- Preserve separate fields for historical Boringness, current 5M permission/structure, 1M execution/EOD state, ATR extension, RVOL, and data freshness.
- Interpret red `EOD: GET OUT` as an active study signal, not a clock or session-end state. It is an exit/position-management instruction; it does not mean the market day has ended. For a *new* overnight entry, treat an active GET OUT/SELL signal as a no-entry condition, while separately respecting higher-timeframe 5M authority.
- Calculate ATR and ATR-normalized extension from source-backed bars with an explicitly versioned timeframe/lookback. Keep the exact TOS study output as a comparison oracle until parity is tested.
- Treat the screenshot as historical chart evidence, not live/current provider data.

## [Uncertainties]
- The screenshots show chart-selected-bar labels; exact timestamps for every displayed label cannot be independently read from the image at pixel precision. The user confirms red `EOD: GET OUT` is a signal, not a session-end label.
- The cause of the differing 5M permission labels between panes is not established; do not infer a logic defect without aligning the pane timestamps and study inputs.
- No catalyst/news feed, source-backed daily-volume history, complete overnight return history, or live quote freshness was available in these screenshots.


## Signal-semantics correction — 2026-10-28

- `EOD: GET OUT` is an active study signal, not an end-of-day/session-state label. Do not infer that the market is closed or that the label is merely a clock boundary.
- For a held position, treat GET OUT as an exit/position-management signal. For a fresh Boring Overnight entry, it remains a no-entry condition; this distinction prevents accidentally interpreting an exit instruction as entry permission.
- RTO: 5M is supportive (Runner Rising, Permission YES, bullish regime established, extension 0.49 ATR), but 1M GET OUT / EOD GET OUT is an active exit signal and RVOL 4.32 warrants investigation. Best of the three for further review, but not READY for a new overnight entry.
- AMT: 5M Permission YES does not cancel the active 1M GET OUT signal. Extension around 2.03 ATR and the large selloff/rebound make it a poor boring-first entry for this snapshot. If already held, follow the exit signal.
- SXC: no individual SXC chart was available in the reviewed source set. The scanner's 6.20% range is inconsistent with the low-noise preference; keep WATCH/DEFER pending chart and hard-gate verification.
