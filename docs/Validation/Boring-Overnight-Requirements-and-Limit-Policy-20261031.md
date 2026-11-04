# Boring Overnight — requirements and calculated limit policy
**Date:** 2026-10-31  
**Branch:** feature/opportunity-quality-classification

## Operating model

Boring Overnight seeks controlled repeatability, not the largest possible gap. It is a decision-support workflow. A zero position is valid. No order submission is implemented.

## Clarifications

- The rolling sample is 20 valid trading-session close/open pairs, not 20 calendar days. Five-session statistics are compared with the 20-session baseline.
- Overnight edge is prior regular-session close to next regular-session open. It is not the user's GTC_EXT selling window.
- The user's eligible sell window is 07:00–20:00 Eastern. Target research must use complete timestamped coverage throughout that window, including gaps.
- Catalyst clearance requires a healthy, validated source and documented review coverage. No connected validated source means UNKNOWN, not CLEAR.
- The 5M/1M authority contract is settled. Installed-study/version and matched-bar parity are a separate certification task; it should be certified once per exact study hash/input configuration, not re-debated for every candidate.

## Calculated Limit Sell contract

The candidate limit is:

    reference close × (1 + historical target-return quantile / 100)

The target-return quantile must be calculated from pre-decision, complete GTC_EXT historical observations and evaluated walk-forward. ATR does not manufacture a target return; it supplies volatility context. Display ATR, ATR%, and the candidate target distance in ATR units. No arbitrary ATR multiplier is permitted absent empirical calibration.

Required evidence before displaying a numeric candidate:
1. Fresh, source-backed regular-session reference close and timestamp.
2. At least 20 valid prior session observations for the chosen historical quantile.
3. Complete 07:00–20:00 Eastern eligible-window bars for each included session, with gaps explicitly accounted for.
4. ATR from a specified timeframe/period and source, with exact Thinkorswim parity status.
5. Target-touch rate, time-to-touch and adverse excursion measured walk-forward; actual fill claims require broker fill records.

The existing +0.5% target remains a user heuristic comparison only. Median close-to-open return is not a substitute for eligible-window target-touch probability.

## Current implementation truth

- The evaluator applies hard gates to explicit inputs.
- The history calculator calculates close-to-open returns from persisted 1-minute observations and caller-supplied exact UTC timestamps.
- The history calculator does not generate exchange calendars, identify early closes, or build GTC_EXT target-touch windows.
- The ATR calculator implements Wilder ATR from supplied OHLC bars but has no certified parity with the exact installed TOS study.
- `GET /api/boring-overnight/sell-window-history?symbol=RTO&provider=Alpaca&feed=iex&asOfUtc=...` now audits the latest 20 completed 07:00–20:00 ET sell windows from persisted 1-minute bars. Each window requires the exact prior regular-session close bar and all 780 minute-bar starts from 07:00 through 19:59 ET. Missing minutes, missing closes and feed mismatches remain explicit. IEX is diagnostic partial-exchange coverage; even 20/20 complete IEX windows are not qualification-usable. SIP is required for consolidated market-wide target research. The endpoint does not fetch, impute, infer quotes, or claim fills.
- The history-backed evaluator forces catalyst status to UNKNOWN because no validated catalyst provider is wired to that route.
- The dashboard's new Calculated Limit Sell panel displays DATA? until required target and ATR evidence is available.
- The sell-limit calculator distinguishes complete zero/negative maximum-favorable-return windows from missing/incomplete data. A non-positive selected quantile returns `NO_POSITIVE_TARGET` with no numeric limit or target-touch rate; it is not mislabeled `DATA?`.

## Current disposition

**NOT READY for automated or actionable READY qualification.** Use WATCH/manual research only. The repository does not currently contain a validated per-symbol 20-session target dataset or complete GTC_EXT outcome data. Data not found in sources.

## Next engineering gates

1. Generate regular-session boundaries from an authoritative exchange calendar, including holidays and early closes.
2. Build source-backed 20-pair statistics from timeframe/feed-isolated stored bars and expose counts/gaps/provenance.
3. Build complete GTC_EXT window coverage and target-touch research separately from close/open edge.
4. Connect a validated catalyst/event source with coverage/health semantics.
5. Certify installed 5M study parity once per exact source hash and input configuration; preserve 1M as execution/exit.
6. Integrate the calculated limit into the dashboard only after required data and ATR parity are available.
7. Add walk-forward tests and exact-head CI before promoting any target policy.

## [Uncertainties]

- The final target quantile is not selected or empirically validated.
- Complete GTC_EXT history, actual fill records, and slippage data are not present in reviewed sources.
- Exact installed TOS study hash/input parity is not certified in this review.
- A production catalyst source and its coverage/entitlement are not verified.


### Sell-limit calculation engine — 2026-10-31

- Added BoringOvernightSellLimitCalculator. It takes the selected pre-decision historical quantile of per-session maximum favorable price movement during complete eligible sell windows, calculates a candidate limit from the current reference close, and reports historical touch rate plus target distance in ATR units.
- ATR is context only; no arbitrary ATR multiplier is used. The calculator requires at least 20 complete windows and exact ATR parity status MATCH before returning a numeric candidate. Incomplete windows return DATA?; unverified ATR parity returns NOT VERIFIED.
- Added unit tests for the nearest-rank quantile, in-sample touch rate, incomplete windows, short history, and ATR parity gate. CI/build has not yet been verified for this commit.
- This is the calculation engine only. The API does not yet assemble full eligible-window observations from persisted bars, and the dashboard does not yet call this calculator. The dashboard correctly remains DATA? until that integration and its source evidence exist.
