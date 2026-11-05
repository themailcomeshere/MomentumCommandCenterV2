# Overnight Sell-Limit Walk-Forward Validation — 2026-11-05

**Branch:** `feature/opportunity-quality-classification`  
**Validator:** `BoringOvernightWalkForwardValidator`  
**Version:** `MCC-OVERNIGHT-WALK-FORWARD-1.0`

## Purpose and separation

The validator estimates a target from earlier sessions and tests it only on later, unseen sessions. It consumes persisted 1-minute OHLCV session summaries; it does not use broker order or execution records. A high reaching a theoretical sell price is a **bar touch**, not a fill, execution probability, or proof that a limit order could have executed.

Actual broker fills must remain in a separate future execution ledger keyed by broker order/execution identifiers, side, quantity, price, timestamp, fees, and status. No fill is synthesized from OHLCV.

## Chronological sample contract

- Minimum training set: 20 complete sell sessions.
- Minimum unseen test set: 20 later complete sell sessions.
- Total required: 40 complete sell sessions, ordered by sell-session date.
- The earliest 20 sessions form training; the subsequent 20 form test.
- Duplicate dates, missing session OHLCV, missing reference closes, missing interval coverage, invalid prices, or incomplete low/high observations block validation.
- Training data alone determines the candidate target. Test observations are never used to select or tune that target.

The history service now retains both the highest and lowest eligible-window prices for each session and requests/audits 40 sessions. The eligible sell window remains 07:00–19:59 America/New_York with the exact prior regular-session close.

## Metrics and calculations

For each session (i):

- Favorable excursion: (MFE_i = (High_i / ReferenceClose_i - 1) \times 100\).
- Adverse excursion: (AE_i = max(0, (ReferenceClose_i - Low_i) / ReferenceClose_i \times 100)\).

Training-only target:

1. Sort the 20 training MFE values ascending.
2. Select the nearest-rank quantile: (rank = ceil(q \times n)), where (q) is the selected quantile fraction.
3. The selected training MFE at that rank is the target return percentage.
4. A non-positive training target fails; no positive numeric sell limit is issued.

Touch measurement:

- Training expected touch rate is the fraction of training sessions whose MFE is greater than or equal to the training target.
- Test target-touch rate is the fraction of unseen test sessions whose MFE is greater than or equal to that same fixed training target.
- The test target is not recomputed or adjusted.

Adverse-excursion measurement:

- Compute the nearest-rank 95th percentile of training AE.
- Report mean and maximum AE across the unseen test sessions.
- The unseen test maximum AE must not exceed the training 95th-percentile bound.

Coverage:

- Training and test coverage must each be 100% of the required sessions.
- No missing bars, reference closes, or price fields are filled or interpolated.

## Pass/fail policy v1.0

Validation passes only when all of the following are true:

1. At least 20 complete training sessions and 20 later complete test sessions exist.
2. The selected training quantile produces a positive target return.
3. Test target-touch rate is at least the empirical training touch rate.
4. Maximum test adverse excursion does not exceed the training adverse-excursion P95 bound.

Otherwise the status is `INSUFFICIENT_SAMPLE`, `INSUFFICIENT_COVERAGE`, `INVALID_SAMPLE`, or `FAILED`; the orchestrator's `WALK_FORWARD_VALIDATION` gate remains blocked and no numeric limit is published.

This is a conservative, transparent initial policy, not a claim that the target is profitable or fillable. Thresholds are versioned so later changes can be compared rather than silently altered. A later validation revision should evaluate confidence intervals, costs/slippage, regime stratification, and multiple rolling out-of-sample folds before any production claim.

## Code and tests

- `BoringOvernightWalkForwardValidator.cs`: pure chronological validator.
- `BoringOvernightSellWindowHistoryService.cs`: expands retained session summaries to 40 and captures each session's lowest as well as highest eligible price.
- `BoringOvernightSellLimitOrchestrator.cs`: adds a hard `WALK_FORWARD_VALIDATION` gate and passes only the first 20 training sessions to the candidate quantile calculator.
- `BoringOvernightWalkForwardValidatorTests.cs`: pass, target-touch failure, adverse-excursion failure, insufficient sample, and incomplete coverage tests.

## Remaining limitations

- SIP entitlement and complete GTC_EXT observations remain external dependencies. IEX cannot satisfy the SIP gate.
- Current ATR parity, 99-bar 5-minute warm-up, provider/TOS 5m OHLCV match, and exact current reference close remain independent upstream gates.
- This validator uses theoretical OHLCV highs/lows only. Broker fills are not implemented and cannot be inferred.
- A single 20/20 split is an initial walk-forward gate. Multiple rolling folds and transaction-cost analysis remain future validation work.
