# Morning Report & Routine — Consolidated Product Specification

**Status:** Consolidated from the Project source Morning.txt; requirements baseline, not a claim of implementation. Primary review time: approximately 08:30 Arizona. The goal is a 30-second briefing with drill-down evidence.

## UI direction
Red Zone belongs at the top-right of the workspace, similar to Thinkorswim Flexible Grid. Support one to four flexible panes; four is a maximum, not a quota. Whether a persistent left Watchlist/News rail exists remains open pending user feedback. Keep panels modular.

## 1. Morning Landscape — what happened?
- Market: SPY, QQQ, IWM direction/change, breadth, VIX/volatility context, and overall STRONG / NORMAL / WEAK regime only when inputs and methodology are validated.
- Industry/sector money flow: strongest and weakest industries; percent change, momentum, relative strength, participation, acceleration/fading, and comparison with yesterday's leaders.
- Overnight/premarket movers: symbols, percentage move, liquidity/volume, when the move occurred, and how much preceded the user's trading window.
- Catalyst/news: source, publication and observation timestamps, catalyst type/confidence. Types include earnings, FDA/regulatory, contract, acquisition, financing, analyst action, partnership, product/news, SEC filing, rumor/social-driven, sector-wide, and unknown.
- Never invent values or infer catalyst from price alone. Missing/stale inputs display DATA? / UNKNOWN.

## 2. Today's Market Expectations — scenarios, not stock-price predictions
Describe the conditions under which strategies historically performed well or poorly: momentum environment, volatility, breakout/continuation environment, mean-reversion risk, and chase risk. Explain favored archetypes such as fresh catalyst + rising RVOL + strong industry + bullish 5M structure + early reclaim/breakout. Identify unfavorable archetypes such as extreme premarket extension with fading momentum.
Do not show probabilities until trained on enough timestamped outcomes, validated out-of-sample, and calibrated. Before then show qualitative evidence or INSUFFICIENT HISTORY, not fabricated percentages. Never say an individual stock will rise; report conditional scenarios.

## 3. Current Conditions — continuously refreshed
Track market regime, momentum/breadth, volatility, industry concentration/relative strength, fresh catalyst coverage, chase/extension risk, and source health/freshness. Save immutable snapshots so an 08:30 state and a later 10:15 state can be compared. Record expectation-vs-outcome divergences rather than overwriting the morning snapshot.

## 4. Opportunity hierarchy and explanations
Use Market → Industry → Discovery → Catalyst → Stock → 5M authority → 1M execution → Historical evidence → Position management → Outcome/learning.
Every candidate needs “Why am I seeing this?” with source/time and reason codes: catalyst, industry rank, RVOL change, 5M state, 1M setup/event, historical analog coverage, trading-window fit, and how much of the move has already occurred.
Keep separate: data confidence; discovery/rank; setup eligibility/5M Permission; 1M execution timing/READY; extension/exhaustion; position management; outcome/model confidence. A rank is not permission. A good setup and good execution are separate dimensions. Manual Schwab entry remains the baseline.

## 5. Red Zone requirements
- Top-right priority area, flexible-grid-like, with 1–4 independently sized panes.
- Each pane: symbol, rank and rationale, price/% move, timestamp/freshness, catalyst context, extension risk, 5M authority, 1M execution, blockers, and explicit unknown states.
- Show fewer than four when fewer qualify or data is incomplete.
- Retain already-extended (+10–15% premarket examples) symbols in a continuation-watch lane without permitting blind chasing. Extension alone must not permanently erase a symbol.
- Left Watchlist/News rail is undecided; let iterative UI feedback determine it.

## 6. Boring Overnight integration
Eventually evaluate catalyst and age, overnight behavior/history, liquidity/spread, industry/market regime, extension/exhaustion, continuation/deterioration, and expected overnight range. Show component inputs and hard gates. Unknown catalyst, stale data, missing bars, or unresolved risk remains UNKNOWN / DATA? / MANUAL REVIEW. “Good overnight hold” is not the same as “stock currently moving.” Live calculation is not implemented.

## 7. Prediction, Journey, and learning roadmap
**Phase 1 — prove current rules:** preserve the existing 2.5% benchmark as a measurement baseline; don't raise it just to force a larger target. Collect actual outcomes; this document changes no strategy thresholds.
**Phase 2 — measure:** win rate, average winner/loser, expectancy, drawdown, MFE/MAE, setup/time-of-day/catalyst/BB-MACD/5M-1M/runner/re-entry performance, time-to-target (+1/+2/+3/+5/+10%), adverse thresholds, session, and missed-fill/exit outcomes when manually recorded.
Every observation should preserve offset-aware source time, Arizona display time, Eastern market/session context, minutes since regular open, minutes relative to configured trading window, and session classification. Machine-local time cannot substitute for source time.
**Phase 3 — contextual scenarios:** define/validate market and industry regime inputs, coverage and missing-data behavior; compare prior-day leaders to current participation; combine context without letting stock-only scores hide weak conditions. Probability of target-before-adverse outcomes requires reproducible samples, out-of-sample validation, calibration, sample count, and model version. “DO NOT FORCE TRADES” is a valid output.
**Journey visualization:** eventually show price path milestones and historical analog paths, with MFE/MAE and time-to-target only when supported by data. The “Momentum Quest” game-like UI is exploratory, not an early dependency.
**Execution intelligence:** later measure manually entered signal/limit/fill/no-fill and subsequent path. Keep Trade Quality separate from Execution Quality. Never infer fills or submit orders.

## 8. Catalyst maturation / exhaustion
Treat “pump → dump” as a probabilistic condition, not fact. Track catalyst strength/age, momentum strengthening/fading, extension, buyer/volume participation, selling pressure, and data confidence separately. Wording such as “PUMP MATURING → ELEVATED DUMP RISK” is allowed only after a validated model/rule and sufficient evidence; otherwise show component observations and RISK MODEL NOT VALIDATED. GRI is a potential research example, not proof of predictive accuracy.

## 9. Evidence and storage
Persist immutable versioned Morning Report and condition snapshots, candidate ranks/reasons, source/receive timestamps, session/timezone, source ID, freshness/gaps, model/rule/config version, input watermark, and later outcomes. Keep raw evidence separate from derived assessments. Never synthesize missing bars or silently overwrite history. Retain provider payloads only where permitted by license.

## 10. Current implementation truth
The static browser dashboard is a UI preview, not a live scanner or persistent report service. Automatic ranking, market/industry regime, news/catalyst, calibrated predictions, live Red Zone, Boring Overnight, Journey analytics, and execution learning are not connected end-to-end. The 5M source is authority; 1M may reconstruct 5M state and needs installed-study parity validation. Provider, entitlements, freshness thresholds, and backfill remain open. No automatic orders.

## User feedback / unresolved items
- Continue evaluating top-right Red Zone; left Watchlist/News rail undecided.
- Exact available trading window is needed only before time-window features are calculated.
- Provider choice must follow fit testing and account/contract verification.
- The source file contains selected copied excerpts, not the complete linked chat; further historical details may be absent.

## [Uncertainties]
Examples of scores, regimes, industry rankings and probabilities in Morning.txt are brainstorming, not measured output. The spec consolidates the supplied excerpts; the original chat may contain details not copied into the file.
