## 2026-11-01 — RTO boundary audit and strategy policy

The user supplied an RTO capture for the 20 regular close/open pairs:
- 4,048 provider bars fetched; 0 newly persisted.
- 18 of 20 pairs valid; missing next-session opens at 2026-10-14T13:30:00Z and 2026-10-21T13:30:00Z.
- Rolling win rate and return metrics were correctly withheld because the required sample is incomplete.

This output does **not** establish why those exact observations are missing. A large fetched-bar count does not prove the provider returned the 09:30 Eastern bar on every session. Zero new writes can be consistent with idempotent persistence for returned bars while the expected boundary minute is absent from the provider response. The capture endpoint now returns `boundaryCoverageAudit` with, for each expected close/open timestamp:
- whether the configured provider response contained the exact bar;
- whether the persisted store contains the exact bar for the same feed;
- the nearest provider timestamp and its offset;
- a status distinguishing provider absence from a provider/store mismatch.

After pulling the updated branch, rerun the capture for RTO. Do not substitute a neighboring minute, use another feed's bar in the same sample, or calculate partial-window metrics under the 20-pair label. If the IEX response itself lacks the exact opening bars, perform a separate entitled SIP/alternate-provider comparison and preserve feed identity. The new exact-boundary audit is distinct from the older ticker-vs-SPY post-market control test, which only proves whether any bars were returned in one shared interval.

### Strategy thresholds: preserve pending evidence

Keep the current provisional gates unchanged:
- price > $5;
- 20-session average daily volume >= 500,000;
- price > 20D SMA;
- 20-session overnight win rate >= 60%;
- 20-session mean overnight return >= +0.10%;
- unrounded 5-session mean strictly > 20-session mean;
- exclusions and catalyst clearance must be affirmatively verified;
- current 5M/1M state and freshness gates remain separate.

Reason: MCC currently has a capture/calculation path but does not yet have complete, certified provider boundary coverage across the universe, a complete GTC_EXT outcome dataset, verified catalyst coverage, corporate-action-normalized outcome lineage, or walk-forward calibration. Changing thresholds now would tune against incomplete and potentially biased samples. The 20-pair window is an operational minimum, not proof of a durable edge. Preserve the existing rules until a larger point-in-time dataset can compare candidate alternatives out of sample after realistic costs. Any later change should be versioned and tested, not silently edited.

### Thinkorswim parity status

The 5M authority / 1M execution architecture and study source/version work have been done. The separate one-time certification of the exact study installed in the user's Thinkorswim instance—study identity/hash, input overrides, matching bar feed, and matched-bar state output—has not been evidenced in the current repository records. Mark it NOT VERIFIED until that evidence is captured. Once certified, do not repeat it unless the installed study, inputs, or relevant signal implementation changes.

---

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


## Current candidate review — 2026-10-26

- The current CCI and NICE scanner/chart review is recorded in docs/Validation/Overnight-Candidate-Review-20261026.md. Both remain WATCH / NOT READY on the supplied evidence.
- CCI's Oct 21, 2026 Q3 results/outlook update is a recent earnings catalyst; NICE's official IR event page lists no upcoming events but this is not a complete catalyst clearance.
- Do not infer READY from the scanner score. Verify exact unrounded 5D > 20D mean, 20-day average daily volume >=500K, source freshness, exclusions, current catalyst/news and 5M/1M chart state.
- Full eligible-window reach research remains blocked on source-backed regular close and complete next 07:00–20:00 Eastern GTC_EXT OHLCV with missing intervals preserved as DATA_GAP. A historical touch is not an actual fill.


## Implementation update — 2026-10-27

- A new API evaluator exists at `POST /api/boring-overnight/evaluate`. It applies the documented hard gates for price, SMA20, average daily volume, overnight win rate, 20D average, strict 5D > 20D, excluded-list status, catalyst status, 5M permission/structure, 1M state, freshness and extension/current-day heat warnings.
- This is the first deterministic evaluator slice, not the full scanner-to-decision pipeline. Inputs are caller-supplied and must be sourced/timestamped by the caller. Automatic candidate discovery, catalyst feed integration, persisted candidate metrics, and validated sell-target selection are still open.
- `docs/Validation/Boring-Overnight-Target-Policy-20261027.md` defines why the +0.5% heuristic and median ON % must be evaluated against complete eligible-window bars and walk-forward outcomes before selecting a target.


## 2026-10-27 — implementation checkpoint

- StrategyReport uploads now parse the actual Thinkorswim semicolon-delimited report format and convert explicit MCC_OHLCV v1 payloads into the repository's exact 17-column parity template. Chart-side comparison fields stay blank unless present in source. No order price is treated as OHLCV, and no parity rows are fabricated if the export has no explicit payload.
- Collector recovery now uses persisted source-timestamp watermarks per provider/symbol/timeframe, a configurable overlap, atomic checkpoint writes and 30-day backfill chunks. The worker's new-observation counter reflects actual newly appended observations, not total bars returned.
- These source changes are on feature/opportunity-quality-classification. Current-head CI has not been verified; do not treat the prior successful workflow for commit 0caf00004a837e269c175f7d4c34827120ac8fe6 as validation of the latest code.
- Still blocked: source-backed daily/overnight metric calculation, full GTC_EXT session coverage, session-aware gap classification, catalyst/news source, authoritative 5M Permission/structure and 1M state. The evaluator remains a deterministic hard-gate function over caller-supplied inputs; it is not yet an autonomous provider-backed Boring Overnight scanner.
- Do not optimize or promote the +0.5% target from OHLCV alone. Need complete source-backed eligible-window bars, missing interval accounting, walk-forward touch-rate evaluation and separate actual-fill evidence.

## Implementation checkpoint — fail-closed validation hardening — 2026-10-28

- [DONE — SOURCE IN THIS COMMIT] Boring Overnight evaluator now rejects blank/invalid symbols and non-finite numeric values (NaN, positive infinity, negative infinity) as DATA? before gate evaluation. This closes a correctness hole because NaN comparisons can all evaluate false and accidentally let a malformed snapshot appear eligible.
- [DONE — TESTS ADDED IN THIS COMMIT] Added evaluator regression cases for NaN/infinities and invalid symbols, plus raw market-bar store tests for non-finite prices, impossible OHLC relationships, and negative volume.
- [PENDING — CI] Run the Release build/test workflow for this exact commit before treating the new cases as passing.
- [NOT IMPLEMENTED / BLOCKED BY EVIDENCE] Do not claim autonomous Boring Overnight qualification: daily/overnight metric computation, complete GTC_EXT session coverage, session-aware gap classification, catalyst source, and authoritative TOS states still require implementation and/or source evidence. The evaluator continues to accept caller-supplied snapshots only.
- [NOT IMPLEMENTED / BLOCKED BY EXTERNAL SETUP] Live scanner discovery and provider proof remain blocked by provider selection, credentials/entitlements, coverage/licensing, and real-data smoke tests. The current collector is explicit-symbol historical REST polling, not a market-wide scanner or websocket stream.
- [USER/LOCAL VALIDATION OPEN] Confirm the Provider Comparison date after browser refresh; source initialization and cache-bust are committed, but browser behavior has not been independently observed here.
- [OPEN] BrowserLink $.noConflict is not a function remains unisolated. Compare with Browser Link disabled before adding application workarounds.


## Follow-up correction — validation hardening compile fix — 2026-10-28

- [FIX COMMITTED] The first validation-hardening CI run failed during build with CS0136 because a local variable named reasons conflicted with a later declaration in the evaluator. Renamed the early-return variable to invalidReasons in commit 65c83f7918200e413a5b20fb7942392357c7d193.
- [PENDING — CURRENT HEAD] Re-run build and tests on the exact latest head after this fix. The previous run 40737130070 is a known failure and must not be reported as passing.
- [NEXT] Verify the new evaluator tests for NaN/infinite values and invalid symbols, and the store tests for invalid OHLCV, once the workflow completes.


## Signal semantics and entry qualification — 2026-10-28

`EOD: GET OUT` is a study signal, not an end-of-day/session-state label. It does not mean the market is closed. Treat it as an exit/position-management instruction for a held position; do not open a new Boring Overnight position while the active exit signal is present. The evaluator now models optional `EodSignal` separately, with tests for GET OUT and SELL. This is a signal interpretation fix only; it does not provide live data, a catalyst check, or a READY decision.


## ATR calculation implementation — 2026-10-28

The new `BoringOvernightAtrCalculator` computes True Range and Wilder ATR from source OHLCV bars, with explicit timeframe and period, plus ATR% and signed ATR extension from a supplied reference price. It rejects insufficient history, mixed symbols/timeframes, duplicate timestamps and invalid OHLCV. Tests are committed but have not been verified by a successful build/CI run. Do not use the values as a qualification gate until exact Thinkorswim study parity is established.


## Dynamic Hold Overnight decision bubble — 2026-10-28

The independent EOD GET OUT bubble should be replaced by a time-gated outcome of the Boring Overnight hold evaluation, not exist in a vacuum. See docs/Architecture/Boring-Overnight-Hold-Decision-Bubble.md.

During 12:30–12:55 in an explicitly configured local timezone:
- READY -> green HOLD OVERNIGHT.
- Known failed/caution outcome -> red EOD GET OUT.
- Missing or invalid required evidence -> yellow DATA? — NO HOLD.
- Outside the window -> NO DECISION; neither HOLD nor GET OUT is asserted by this windowed decision.

The server-side decision helper and tests are committed. The actual ThinkScript source is absent from this branch, and the helper is not yet wired to persisted source-backed inputs; do not treat the TOS bubble as implemented. User must confirm Arizona versus chart timezone and provide the current ThinkScript source.


## Arizona-time decision bubble implementation — 2026-10-28

The user confirmed America/Phoenix. The canonical 1M study now evaluates the chart-visible technical state at the final bar of the 12:30–12:55 Arizona window. It emits red EOD GET OUT for known chart-technical failures; otherwise it emits yellow DATA? — NO HOLD because the ThinkScript source does not contain the external overnight history, catalyst, exclusion and freshness evidence. It intentionally does not emit green HOLD OVERNIGHT until those inputs are authoritatively connected.

The 5M label is now ENTRY CUTOFF ACTIVE/INACTIVE. The existing 3:55 PM Arizona cutoff continues to block intraday entries but is not itself an overnight hold decision. See docs/Architecture/Boring-Overnight-Hold-Decision-Bubble.md for the full contract and docs/Next-Steps-and-User-Inputs.md for installation instructions.


## Monday release gate — 2026-11-02

**Disposition: NOT READY for automated or actionable READY qualification.** The evaluator and snapshot/history endpoints are implemented, but a live validated candidate pipeline is not. Monday use is limited to WATCH / manual research.

Reasons:
- The historical calculator uses explicit caller-supplied regular-session close/open bar timestamps. It does not infer exchange calendars or guarantee a complete 20-session window.
- The user's GTC_EXT sell window (07:00–20:00 Eastern) is a separate interval from the regular close-to-next-open return. Full eligible-window coverage and target-touch evidence are not established. A touch is not a fill.
- The history-backed evaluation route deliberately sets catalyst status to UNKNOWN because no validated catalyst provider is connected. It cannot safely return READY on that path.
- Other fields such as daily average volume, price/SMA, current-day heat, and installed Thinkorswim 5M/1M states require independently validated source data and timestamps.
- Scanner/Morning Brief preview rows are not a validated overnight universe.

### Monday manual-only procedure

1. Use the candidate list only as a research/watchlist. Do not promote a preview rank or intraday score into Boring Overnight READY.
2. Verify the hard gates using unrounded, source-backed data: price >$5; 20D average daily volume >=500,000; price >20D SMA; overnight win rate >=60%; 20D mean overnight return >=+0.10%; and strictly 5D mean >20D mean.
3. Verify the full maintained Schwab exclusion list.
4. Review current earnings, M&A, corporate actions, regulatory/government decisions, material news, and sector/regime risks. Unknown coverage stays UNKNOWN.
5. Confirm current 5M Permission/structure and 1M state. GET OUT / SELL / BLOCK prevents a fresh overnight entry.
6. Preserve source/feed, UTC source timestamps, session boundary timestamps, and gaps. Any missing critical evidence => DATA?; hard-gate failure => BLOCKED; caution/heat => WATCH.
7. Zero position remains a valid decision. MCC does not submit, cancel, replace, or manage orders.

### Release checklist

- [ ] Approve exact regular close/open and GTC_EXT session semantics, including ETH/RTH, holidays, early closes, and timestamp conventions.
- [ ] Validate daily history for 20D SMA and average volume.
- [ ] Validate at least 20 complete regular-close/next-open pairs per symbol.
- [ ] Validate complete GTC_EXT coverage separately for target-touch research; do not call a touch a fill.
- [ ] Connect and validate catalyst/news coverage and freshness.
- [ ] Reconcile all Schwab exclusion sources.
- [ ] Match C# 5M states to installed Thinkorswim diagnostics.
- [ ] Integrate a reproducible scanner universe and immutable candidate decisions.
- [ ] Shadow-test across winners, losers, outliers, catalysts, and no-trade cases before calibration or promotion.

## Daylight-saving conversion — November 2, 2026

From Monday November 2, Phoenix is two hours behind Eastern because Arizona remains UTC-07:00 and Eastern returns to UTC-05:00. The documented 07:00–20:00 ET GTC_EXT window maps to 05:00–18:00 Arizona. Regular-session open maps to 07:30 Arizona and close to 14:00 Arizona. Use timezone identifiers rather than fixed offsets in any future automatic session-boundary implementation.

Evidence and Monday operating instructions: `docs/Monday-Readiness-2026-11-02.md`.


## 2026-10-31 — Qualification semantics and sell-limit policy clarification

### 20-session sample

The rolling window means **20 valid trading-session close/open pairs**, not 20 calendar days. It is an initial research sample size, not a guarantee of statistical significance. Keep both 5-session and 20-session measurements so recent deterioration/improvement is visible beside the broader baseline. Show valid pair count, missing pair count, date range, and feed. Do not fill missing pairs with zero or silently skip them. The current all-pairs-complete rule is intentionally conservative; if we later allow partial windows, that must be a separately versioned and tested policy with an explicit minimum sample.

### Session boundary definitions

Keep these measurements separate:

1. **Overnight edge:** prior regular-session final 1-minute bar close to next valid regular-session first 1-minute bar open. Timestamps are provider bar-start timestamps in UTC. A normal session's final 1-minute bar starts at 15:59 America/New_York and its first bar starts at 09:30 America/New_York; early closes and holidays require an authoritative exchange calendar. Do not use after-hours last trade or premarket first trade as substitutes.
2. **Sell opportunity:** the complete user-eligible GTC_EXT window, 07:00–20:00 America/New_York. Measure target touch, time-to-touch, adverse excursion, and coverage gaps over that full window. A historical bar high touching the limit is not evidence of an actual fill.

The current history calculator implements only item 1 and requires caller-supplied exact UTC timestamps. It does not generate an exchange calendar. The dashboard's ordinary bar fetch does not construct either measurement automatically.

### Catalyst clearance

CLEAR must mean a defined, timestamped review from a connected and validated event/news source over a documented horizon, covering at minimum earnings, guidance, M&A, corporate actions, regulatory/court/government events, material company news, and relevant sector/regime events. Store source, query coverage, publication timestamps, checked-at time, and matched events. An empty response is not automatically CLEAR unless source health and coverage are verified. Until this exists, UNKNOWN is correct. Do not treat a manual assertion as provider-verified clearance.

### 5M Thinkorswim parity

The **architecture and signal contract are established**: 5M owns structure/permission; 1M owns execution/confirmation; a 1M exit/block prevents a fresh overnight entry. Do not ask the user to re-approve those rules each candidate cycle.

The separate engineering question is whether the exact installed Thinkorswim study/version and its input overrides match the repository's authoritative 5M study and whether the observed chart output matches MCC on identical bars. Record one parity certification against exact study version/hash and matched-bar fixtures; reuse it while that exact version/hash and inputs remain unchanged. A changed study, input override, or signal implementation invalidates certification. Until the one-time certification evidence exists, show NOT VERIFIED rather than implying that the strategy contract itself is unsettled.

### Calculated Limit Sell

The target must be a **candidate limit**, not an execution promise. The calculation should use a source-backed reference close and a pre-decision historical target-return quantile derived from complete eligible GTC_EXT windows. ATR is a volatility/context measure: show ATR, ATR%, and the candidate target's distance in ATR units. Do not invent an ATR multiplier or combine ATR and mean/median returns using arbitrary weights. The historical target distribution chooses the candidate return; ATR contextualizes how extended that target is.

Display:
- Calculated Limit Sell in dollars;
- reference regular-session close and timestamp;
- target return and quantile;
- ATR value, period/timeframe, source, and parity state;
- 20-session sample count and full GTC_EXT coverage;
- historical target-touch rate, time-to-touch, and data-gap count;
- status PROVISIONAL, DATA?, or NOT VERIFIED.

Do not issue a numeric limit if the reference close, selected target quantile, complete eligible-window sample, or required ATR context is absent. Do not claim a fill probability until target-touch evidence is walk-forward tested; do not claim actual fill probability without broker execution data. The +0.5% value remains a user heuristic comparison only, not the calculated target.

### Dashboard update — 2026-10-31

The Overnight panel now has an explicit Calculated Limit Sell display. It deliberately shows DATA? — NOT CALCULATED until the historical target distribution and ATR context are source-backed and validated. This replaces a silent omission with a visible fail-closed state; it does not claim the calculation pipeline is complete.


### Sell-limit calculation engine — 2026-10-31

- Added BoringOvernightSellLimitCalculator. It takes the selected pre-decision historical quantile of per-session maximum favorable price movement during complete eligible sell windows, calculates a candidate limit from the current reference close, and reports historical touch rate plus target distance in ATR units.
- ATR is context only; no arbitrary ATR multiplier is used. The calculator requires at least 20 complete windows and exact ATR parity status MATCH before returning a numeric candidate. Incomplete windows return DATA?; unverified ATR parity returns NOT VERIFIED.
- Added unit tests for the nearest-rank quantile, in-sample touch rate, incomplete windows, short history, and ATR parity gate. CI/build has not yet been verified for this commit.
- This is the calculation engine only. The API does not yet assemble full eligible-window observations from persisted bars, and the dashboard does not yet call this calculator. The dashboard correctly remains DATA? until that integration and its source evidence exist.


## 2026-10-31 — One-click 20-session close/open capture

- Added `POST /api/boring-overnight/capture-history`. The endpoint selects the configured provider, fetches 1-minute bars for the most recent 20 session pairs, appends them to the raw observation store, reads back the latest persisted 1m observations, and calculates the 20 regular-session close/open returns.
- Added `UsEquitiesSessionCalendar` (`MCC_US_EQUITIES_CALENDAR_V1`) to generate exact UTC timestamps from America/New_York local session dates, including the documented NYSE holiday rules and the implemented common early-close rules (day after Thanksgiving, Christmas Eve, July 3 when it is a trading day). DST is timezone-derived, not hard-coded.
- Added the Overnight dashboard button **Refresh/Capture 20-session close/open calculation**. It displays valid/required pairs, win rate, 20-pair mean/median, 5-pair mean, missing pairs, feed, fetched/newly persisted bar counts, calendar version, timestamps, and pair-level source evidence.
- Retrieval is chunked into at-most-30-day provider requests and deduplicated by source timestamp before persistence.
- Added calendar tests for 20 consecutive pairs, weekend handling, post-DST UTC conversion, Thanksgiving, Good Friday, and Christmas Eve.
- **Limitations:** this is an MCC-maintained ruleset, not a live exchange-calendar service. Review it against the official exchange calendar and add fixtures for all exceptional holiday/early-close cases before production qualification. Missing exact boundary bars suppress rolling metrics. It is regular close-to-next-open history only—not the 07:00–20:00 ET GTC_EXT sell-target study, catalyst clearance, or a READY decision.
- Exact-head Release build, tests, API runtime capture, and browser acceptance are still pending.
