# Boring Overnight Strategy

**Purpose:** Prefer repeatable, controlled overnight returns over the largest possible gap. A zero position is a valid outcome. This is a research framework and checklist, not a guarantee of return or an automatic trade signal.

## 1. Strategy design — what the system should evaluate

Keep the concepts separate until independently tested.

1. **Overnight Edge Score:** historical ability to produce positive overnight returns.
2. **Boringness Score:** how controlled, repeatable, liquid, and non-extreme that edge is.
3. **Event/Catalyst Risk:** earnings, M&A, regulatory/government decisions, corporate actions, material news, or sector/regime shocks that may make the hold non-boring.
4. **Current-Day Qualification:** unusual intraday move or volume, sharp extension, large current overnight move, or deterioration from normal behavior.
5. **5M Structure/Permission:** current structure and authority gate.
6. **1M Confirmation:** entry timing, if the chosen workflow uses a 1M entry.
7. **Final decision:** READY, WATCH, REJECT/BLOCKED, DATA?, or ZERO POSITION, with reasons and data freshness.

The final decision must not be a single opaque score. Preserve component scores and hard-gate outcomes so a user can see why a candidate passed or failed.

## 2. Initial Boringness score — 100 points

These weights are the current cheat-sheet specification and remain provisional until tested against historical data.

| Component | Weight | Preference |
|---|---:|---|
| Overnight win % | 25 | Higher is better |
| Median ON % | 20 | Positive and reasonably close to the mean |
| 20D average ON % | 15 | Positive; do not reward extreme gaps |
| 5D average ON % | 10 | Positive and improving, but controlled |
| Overnight consistency | 10 | Higher is better |
| Overnight volatility | 10 | Lower is better relative to the return |
| Average volume | 5 | Higher liquidity |
| Price | 5 | Prefer established stocks above $5 |
| **Total** | **100** | |

### Hard rejection gates

Reject or require explicit investigation if any condition is true:

- Overnight win rate below 60%.
- 20-day average overnight return below +0.10%.
- 5-day average overnight return less than or equal to the 20-day average (the current cheat-sheet rule).
- 20-day average volume below 500,000 shares/day.
- Price below $5.
- Price below the 20-day SMA.
- Symbol appears on the maintained Schwab-excluded list.
- Material event/catalyst risk is incompatible with an overnight hold.
- Critical data is missing or stale.

Current exclusions documented in the source list: `SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE`. Treat this as a known subset pending reconciliation against all maintained lists; do not assume it is exhaustive.

### Ranking bands

| Score | Classification |
|---:|---|
| 90–100 | PERFECT BORING |
| 80–89 | VERY GOOD |
| 70–79 | POSSIBLE |
| 60–69 | AGGRESSIVE |
| Below 60 | REJECT / NOT BORING ENOUGH |

A high score never overrides a failed hard gate or unknown catalyst status.

### Preferred ranges (ranking guidance, not hard gates)

- Current ON %: small positive to below 3%; at or above roughly 3% increasingly warrants penalty/review.
- 20D average ON %: +0.10% to below 2.0%.
- 5D average ON %: positive to below 5.0%, ideally above the 20D average without becoming extreme.
- Median ON %: positive and reasonably close to average. A high average with a tiny median may indicate outlier gaps.
- ON win rate: 70%+ attractive; 80%+ excellent only if other measures remain controlled.
- Consistency: 70%+ preferred; 80%+ excellent.
- Volatility: judge relative to the return earned, not in isolation.
- Volume: comfortably above the 500K/day floor is preferable.

### Current-day “too hot” penalties

Reduce the rank or move to WATCH/REJECT when current behavior is no longer boring: abnormal intraday move/volume, current ON move >=3%, fresh news or earnings response, M&A/regulatory/government decision risk, corporate action, oil/geopolitical/sector shock, sharp extension, a gap that distorts historical averages, or materially deteriorating 5M structure. Examples previously noted during testing include ECVT/FPS as technically strong but potentially too hot, and FRO/DHT as potentially exposed to oil/tanker or catalyst sensitivity. These are examples to re-check, not permanent symbol classifications.

## 3. Strategy use — the human workflow

### Design vs. Use

**Design (research/build phase):**
- Define each metric, calculation window, overnight session boundary, split/dividend handling, extended-hours assumptions, and data source.
- Validate formulas against independently calculated same-symbol/same-date examples.
- Test winners, losers, outliers, catalyst spikes, overextended candidates, and each hard-gate failure.
- Keep Overnight Edge and Boringness distinct; measure false positives and missed candidates before tuning weights.
- Treat ThinkScript limitations honestly. If reliable event status or full rolling history is unavailable, return DATA?/MANUAL REVIEW rather than READY.
- Version the spec and chart studies; filename or modified date alone does not prove which study is installed in ThinkOrSwim.

**Use (each candidate/night):**
1. Check data freshness and confirm the overnight calculation uses the intended session boundary.
2. Apply all hard rejection gates first. A high score does not rescue a failed gate.
3. Review Overnight Edge and Boringness separately; inspect median vs. mean and 5D vs. 20D behavior.
4. Check earnings, M&A, regulatory/government events, corporate actions, news, and sector/regime exposure from a trustworthy source. Unknown event status is not clear status.
5. Apply current-day “too hot” and extension review.
6. Confirm 5M structure/permission and 1M timing according to the selected setup.
7. Read the decision and reason codes; manually verify the underlying chart and any suspicious scanner output.
8. Choose READY, WATCH, REJECT/BLOCKED, DATA?, or ZERO POSITION. Do not force a trade because the list is empty or because a candidate has a high rank.
9. Record the decision and later compare it with actual overnight outcome, adverse excursion, and source data. Do not change thresholds because of one result.

## 4. What the system should show

- Symbol, last update time, and data freshness.
- Overnight Edge Score and component inputs.
- Boringness Score and component contributions.
- Each hard gate: PASS / FAIL / UNKNOWN.
- Event/Catalyst Risk: CLEAR only when supported by a reliable source; otherwise UNKNOWN/REVIEW.
- Current-day heat/extension flags.
- 5M permission/structure and optional 1M timing.
- Final status and reason codes.
- A clear zero-position outcome.

## 5. Implementation notes / limitations

- ThinkScript may support some price/volume calculations but cannot establish catalyst-clear status reliably without a suitable event feed.
- C# replay can support longer historical windows only when supplied with trustworthy, timestamped data. It must not fabricate MFE/MAE or catalyst state from trade history alone.
- Initial score weights and thresholds are provisional design choices, not empirically calibrated trading rules.
- Keep overnight strategy separate from intraday momentum ranking. A stock can be a strong intraday opportunity and still be a poor boring overnight candidate.

## Source notes

This document consolidates the strategy and cheat-sheet criteria present in the prior Boring Overnight notes and master To-Do. Where source material conflicts or is incomplete, the current explicit rule is preserved and remaining reconciliation/calibration is pending.

## October 26, 2026 — research status and evidence boundary

- The user's current GTC_EXT window is 07:00–20:00 Eastern. During Eastern daylight time this maps to 04:00–17:00 Arizona; research must cover the complete eligible interval, not just regular-session hours.
- The +0.5% sell target is the user's current heuristic benchmark, not a fixed strategy rule. Keep it as one candidate and compare alternative target limits only after a source-backed historical reach study with out-of-sample evaluation.
- The current October 19 XRPN OHLCV pair is daytime-only and has known one-minute timestamp gaps. It cannot establish full-window overnight reach or actual fill probability.
- The newly uploaded October 2 file was actually HPK 1M and duplicates the October 2 portion of the existing HPK multi-day file. It does not create an XRPN October 2 pair.
- The prior placeholder rows in `eligible-window-high-excursions.csv` have been removed; it is now header-only with zero observations. Do not use it to estimate target reach.
- Until full eligible-window OHLCV and source-backed regular-session closes are available, mark affected rows `DATA_GAP`; do not recommend a new limit from incomplete data. Keep actual fills/fees separate from reference-close touch research.
- Detailed uncertainty dispositions and next evidence: `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`.
