# Boring Overnight target-price policy — 2026-10-27

## Decision

Do not hard-code a universal +0.5% limit, and do not blindly use the historical median overnight return as a guaranteed target. Implement the qualification gates as deterministic code now; keep target selection explicitly provisional until the eligible-window dataset is complete.

## Why median ON % alone is insufficient

The median is a useful baseline for a typical historical outcome, but it is not the same as the probability of touching a limit during the sell window. Overnight return calculations can differ from limit-price execution because of gaps, quote spreads, extended-hours liquidity, bar sampling, and the exact limit order's eligible time. A target set at the historical median does not imply a 50% fill probability.

Current-day volume, daily range and ATR help identify abnormal conditions and constrain chase risk; they do not by themselves estimate the conditional distribution of the following overnight return. Do not add them together as arbitrary multipliers.

## Recommended target research

For each symbol and historical decision date, preserve:

- Source-backed regular-session close and its timestamp.
- Prior rolling median, mean, win rate, quantiles and sample count, computed only from observations available before that decision.
- Complete eligible GTC_EXT sell-window bars from 07:00 to 20:00 Eastern, including source timestamps/feed and explicit DATA_GAP intervals.
- Whether the proposed limit price was actually touched during the eligible window; separately store actual order/fill records when available.
- Current-day realized range, ATR, volume vs. historical baseline, event/catalyst status, 5M permission/structure, and 1M state at decision time.

Compare fixed +0.5% with candidate targets derived from pre-decision quantiles (e.g. 25th, 35th, 50th percentiles), and evaluate target-touch rate, time-to-touch, adverse excursion, net outcome after spread/fees/slippage, and sample size. Use walk-forward / out-of-sample periods and avoid look-ahead leakage. A touch is not a fill; realized results require actual order/fill data.

## Interim policy

- Keep +0.5% as a labeled user heuristic for comparison only, not as an optimized rule.
- Display median ON % and the candidate target side-by-side, with sample count, data coverage, target-touch estimate, and a clear provisional status.
- Do not recommend a numeric target if the source-backed close, complete eligible window, or required historical sample is missing. Return DATA?.
- Do not permit current-day volume/range/ATR to override catalyst, 5M authority, 1M exit, or data-quality gates.
- The new API qualification endpoint evaluates hard gates from a supplied snapshot. It does not yet discover candidates, retrieve all required metrics automatically, calculate a validated target, or place orders.

## Uncertainties

- Complete historical GTC_EXT bars and per-candidate outcomes: Data not found in sources.
- Actual fills, slippage and eligible limit-order execution semantics: Data not found in sources.
- The final target quantile and minimum sample size must be selected from walk-forward evidence, not intuition.
