# Opportunity Quality and Ranking

**Status:** First diagnostic domain slice implemented; market-data integration and calibration pending  
**Purpose:** Separate technical entry eligibility from opportunity quality and execution timing.

## Problem statement

A valid `EXECUTION READY` event establishes that the current entry rules were satisfied at a particular time. It does not establish that the stock has attractive follow-through potential, sufficient remaining range, or favorable reward relative to risk. The decision interface must not imply that all READY events are equally worth trading.

This workstream adds an explainable quality assessment alongside existing signals. It must not silently rewrite or weaken the current 5M authority, 1M execution gates, finite READY window, or position-management rules.

## Decision dimensions

Keep these dimensions separate in the domain model and UI:

1. **Eligibility** — Is the existing strategy's setup valid at this timestamp?
2. **Opportunity quality** — Does available evidence support prioritizing this candidate for potential movement?
3. **Entry timing** — Is the entry location acceptable now, or is it extended / poorly positioned?
4. **Data confidence** — Are the inputs complete, fresh, and comparable?

A high quality score must never override a failed eligibility gate. Good opportunity quality does not make an extended entry safe. Missing or stale required inputs must never silently produce a high-quality or READY result.

## Initial classification contract

Use these as explainable output classifications, not claims of profitability:

- `READY_HIGH_OPPORTUNITY`: eligible, sufficient fresh inputs, and quality evidence meets a calibrated high band.
- `READY_MODERATE`: eligible, sufficient inputs, and evidence is mixed/moderate.
- `READY_BORING`: eligible, but measured movement/participation evidence is below the intraday-priority threshold.
- `READY_EXTENDED`: eligible thesis, but entry location exceeds the validated extension guard.
- `WATCH_DEVELOPING`: setup may be developing, but the existing execution event is not currently valid.
- `BLOCKED`: a hard eligibility or risk gate failed.
- `DATA_UNKNOWN`: required data is missing, stale, or invalid.

The public labels must remain distinct from the Thinkorswim `EXECUTION READY` event. Current display labels are `QUALITY — HIGH`, `QUALITY — MODERATE`, `QUALITY — LOW`, and `TIMING — EXTENDED`; stable machine-readable classifications and reason codes remain unchanged.

## Proposed input snapshot

Every evaluation must be reproducible from an immutable timestamped snapshot. Candidate fields:

- Symbol, evaluation timestamp, exchange/session context, and timezone.
- Existing 5M authority/permission state and source timestamp.
- Existing 1M candidate, confirmation, READY state, READY start/expiry, and block reasons.
- Last price and timestamp; recent 1M and 5M OHLCV bars with source/freshness metadata.
- Relative volume, calculated with a documented session/time-of-day baseline.
- Short-horizon price change / acceleration, with explicit lookback windows.
- Recent range or ATR and extension relative to a documented reference (e.g. VWAP or moving average).
- Distance to nearby resistance / recent swing high where the data source supports it.
- Spread/liquidity proxy only when sourced reliably; do not infer spread from candles.
- Input-quality flags and per-field timestamps.

Do not add a field to the live contract until its source, session semantics, calculation, and stale-data behavior are defined. If the current source cannot support a field reliably, mark it unavailable rather than fabricating a proxy.

## Scoring design

Start with a transparent, component-based score and retain each component contribution and reason. Do not set production thresholds solely by intuition.

Candidate components for research:
- Participation: relative volume versus a time-of-day-appropriate baseline.
- Momentum: short-horizon directional change and acceleration.
- Movement capacity: recent realized range / ATR, with context for price and liquidity.
- Entry location: extension from VWAP / structure and proximity to recent highs or resistance.
- Data quality: a hard confidence gate, not a bonus that can compensate for missing market data.

The quality score is a ranking aid, not a probability of profit. Do not describe it as a win probability unless calibrated and validated as such. Avoid double-counting highly correlated inputs. Initially store component values and expose them in diagnostics; choose weights and bands only after historical comparison.

## Evaluation precedence

Apply precedence in this order:

1. Validate required inputs and freshness; otherwise `DATA_UNKNOWN`.
2. Evaluate existing hard eligibility and strategy gates; failure => `BLOCKED`.
3. Evaluate existing finite execution-event validity; no active valid event => `WATCH_DEVELOPING` or the existing non-ready state.
4. Evaluate entry timing / extension guard; unsafe timing => `READY_EXTENDED` (the underlying thesis remains separately represented).
5. Evaluate calibrated opportunity-quality band; distinguish high, moderate, and boring.
6. Persist the result, component values, reason codes, input snapshot reference, and strategy/config version.

Do not collapse `NO NEW SIGNAL`, `HOLD`, and `EXIT`. Opportunity ranking is an entry-prioritization layer and must not independently manage an open position.

## Required reason codes

Initial examples (stable identifiers; exact enum naming may follow repository conventions):

- `DATA_MISSING`, `DATA_STALE`, `DATA_INVALID`
- `PERMISSION_OFF`, `NO_VALID_EXECUTION_EVENT`, `READY_EXPIRED`
- `EXTENSION_DANGER`, `LOW_RELATIVE_VOLUME`, `WEAK_MOMENTUM`
- `LIMITED_RECENT_RANGE`, `RESISTANCE_TOO_CLOSE`
- `QUALITY_HIGH`, `QUALITY_MODERATE`, `QUALITY_LOW`

Each decision should explain both why it qualified and why it was downgraded or blocked. A decision may contain multiple reason codes.

## Historical validation plan

Capture candidates at the time they are observed, including candidates not traded. For each candidate, preserve signal time, intended entry, actual fill when present, exit, and subsequent bars over a consistent evaluation horizon.

Measure at minimum:
- Maximum favorable excursion (MFE) and maximum adverse excursion (MAE) from the candidate/entry reference.
- Realized return and return after a fixed horizon, when historical bars are available.
- Time to favorable/adverse movement and time in position.
- Opportunity capture rate and high-quality candidates missed.
- False-positive rate for high-quality classifications.
- Slippage/spread sensitivity where actual data exists.
- Time/capital tied up in low-movement candidates.

Compare classifications to the current READY-only baseline. Evaluate multiple market regimes and symbols; do not tune thresholds on a couple of recent trades. Use time-separated validation to reduce overfitting. Never manufacture MFE/MAE from trade-history rows alone; they require market bars.

## Test requirements

Unit tests should cover:
- Hard gates always dominate quality score.
- Missing/stale/invalid inputs produce `DATA_UNKNOWN`, never READY.
- Extension danger can downgrade timing without declaring the 5M thesis dead.
- READY expiry remains enforced by the existing execution engine.
- Stable, deterministic classifications for identical snapshots/configuration.
- Reason codes explain every blocked/downgraded decision.
- No opportunity-ranking result creates HOLD/EXIT or changes open-position lifecycle state.
- Session boundaries, timezone conversions, and time-of-day volume baselines are explicit.

Add integration/comparison tests against known TOS snapshots before using classifications in the live workflow.

## Implementation sequence

1. Inspect the repository's actual solution structure and existing market-snapshot / decision models.
2. Add pure domain types for the opportunity input snapshot, component assessment, classification, and reason codes in the repository's established layer/naming conventions.
3. Implement deterministic evaluation with no external I/O.
4. Add unit tests for precedence, missing data, timing, and deterministic reasons.
5. Add persistence and historical outcome linkage only after the domain contract is testable.
6. Surface concise labels and detailed explanations in UI/CLI after the underlying results are reproducible.
7. Calibrate bands using recorded historical candidates and outcomes; keep the feature diagnostic-only until evidence supports operational use.

## Explicit non-goals

- No automatic order placement or brokerage submission.
- No replacement of the existing 5M authority or 1M execution logic.
- No unverified news/catalyst-clear assumption.
- No claim that a high score guarantees a profitable trade.
- No production thresholds chosen from the MNSB experience alone.


## Current implementation status (2026-10-16)

Implemented on `feature/opportunity-quality-classification`:

- `OpportunityQualityInput`: explicit, timestamped input contract. Recent range and RVOL are supplied measurements; the assessor does not invent them from one bar.
- `OpportunityQualityAssessor`: deterministic, pure assessment with data freshness checks and precedence for data quality, existing eligibility, active execution-event validity, entry timing, and quality.
- `OpportunityQualityAssessment`: stable classification, human-facing display label, and reason codes.
- `OpportunityQualityConfig.DiagnosticDefaults`: provisional prototype bands only; they are not calibrated or approved for live decisions.
- Unit tests cover strong/moderate/boring classifications, stale/missing data, future timestamps, eligibility precedence, inactive events, extension, and nearby resistance.

This is deliberately not yet wired into `V2SignalEngine`, the CLI, market-data ingestion, or a front end. The current `BarSnapshot` contains a single bar and indicators but does not itself provide a trustworthy recent-range baseline, nearby resistance measurement, time-of-day-relative volume provenance, or an execution event's finite READY lifetime. Those values must come from the forthcoming adapter/replay contract. The caller must explicitly pass the existing eligibility and active execution-event state; the assessor does not infer them.

The classification is diagnostic-only. A successful build and passing unit tests are still required in the local environment; this repository integration did not execute a .NET build.

## Implementation and data audit — 2026-10-26

The domain assessor and unit tests exist, but the classification remains a standalone diagnostic slice. It is not wired to the market-data replay, existing V2 signal engine, CLI, API, or dashboard. The OHLCV reader does not calculate relative volume, momentum indicators, ATR extension, recent range, resistance distance, or the actual Thinkorswim READY event. Those must be supplied by a validated snapshot adapter before a classification can represent a reproducible market observation.

The October 19 XRPN 1M/5M export pair has been checked for internal OHLCV consistency on complete windows, but the files do not contain the internal ThinkScript gate states. It therefore does not calibrate the assessor or explain READY frequency. See `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`.

**Presentation rule (implemented 2026-10-26):** Display labels now use `QUALITY — HIGH`, `QUALITY — MODERATE`, `QUALITY — LOW`, and `TIMING — EXTENDED` instead of labels beginning with `READY`. When surfaced, show separate fields for `Execution: EXECUTION READY / NOT READY / UNKNOWN` and `Opportunity quality: HIGH / MODERATE / LOW / EXTENDED / UNKNOWN`. The quality field must never create or imply the actual execution event. Unit tests were updated for the label and invalid negative metrics; the current branch CI result must still be checked before calling these changes verified. The default bands remain provisional and must not be promoted based on unit tests or this small OHLCV sample.


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
