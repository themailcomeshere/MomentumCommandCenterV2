# Unified Candidate Review Contract (Scaffold)

**Status:** Design scaffold; not wired to application services or UI.  
**Created:** 2026-10-28  
**Purpose:** Bring Overnight Scanner evidence, 5M authority, 1M execution, freshness, extension, catalyst status, and data confidence into one explainable review record without conflating their authority.

## Non-negotiable precedence

1. Invalid, missing, stale, or incomparable required inputs => DATA_UNKNOWN / DATA?; never READY.
2. Existing 5M authority and 1M execution gates remain authoritative. A ranking score cannot create permission or execution readiness.
3. A valid execution event is time-limited; an old OBSERVED row is not a current signal.
4. Catalyst unknown is not catalyst-clear.
5. Position management (HOLD / EXIT) is separate from entry ranking.
6. Zero position is a valid decision.
7. Do not infer spread, catalyst status, volume baseline, or ATR extension from fields that do not support those measurements.

## Candidate review record

Required identity and provenance:
- Symbol and verified instrument identifier where available.
- Evaluation timestamp, timezone, exchange/session context.
- Source/provider name, source event ID, observation timestamp, data-as-of timestamp.
- Schema version, evaluator/study version, configuration version.
- Overall data-quality status and per-input freshness/status.

Overnight evidence:
- Overnight win rate and sample count.
- 5D and 20D average overnight return, median overnight return, consistency/dispersion metric.
- Current ON return and ON session definition.
- Average daily volume, price, 20D SMA, maintained exclusion-list result.
- Source window, calculation version, and missing-data reasons.

Technical state:
- 5M permission and authority state, timestamp, source/study version.
- 1M candidate/confirmation/READY state, READY event time and expiry, block reasons.
- Recent 1M/5M range and ATR-extension values only when calculated using the exact versioned study definition.
- Intraday movement/volume metrics with explicit windows and baselines.
- Position lifecycle state, kept separate from entry eligibility.

Risk/context:
- Catalyst status enum: CLEAR_FROM_CHECKED_SOURCES, RISK_FOUND, UNKNOWN, STALE.
- Each catalyst finding includes source, observed/published timestamp, checked-through timestamp, and concise reason. UNKNOWN/STALE blocks READY.
- Spread/liquidity state only when sourced; otherwise UNKNOWN.
- Event/exclusion checks record which lists/sources and versions were used.

Decision:
- Classification: READY_HIGH_OPPORTUNITY, READY_MODERATE, READY_BORING, READY_EXTENDED, WATCH_DEVELOPING, BLOCKED, DATA_UNKNOWN. These are review classifications and do not replace the existing TOS EXECUTION READY event.
- Execution state: READY, NOT_READY, UNKNOWN.
- Suggested workflow action: REVIEW_CHART, WATCH_PULLBACK, WATCH_CATALYST, BLOCKED, DATA_REPAIR, or NO_ACTION.
- Multiple stable reason codes and plain-language explanation.
- Component values and gate results preserved; no opaque composite score required for the first version.

## Freshness behavior

Do not set arbitrary universal age thresholds in this contract. Configure thresholds by data type, session, provider, and workflow, and make them visible. At minimum:
- Missing timestamp => DATA_UNKNOWN.
- Future timestamp beyond permitted clock-skew tolerance => DATA_UNKNOWN.
- Age above configured threshold => DATA_STALE.
- Outside-session timestamp must be interpreted against the configured session calendar, not automatically considered stale or fresh.
- Incomplete bar windows, gaps, or unresolved timestamp semantics => DATA_UNKNOWN for any calculation that depends on those bars.
- Last observation can be displayed for context but cannot be described as current.

## Suggested implementation seams

- Application: immutable CandidateReviewInput, CandidateReviewAssessment, DataFreshnessAssessment, CatalystAssessment, and stable reason-code enums.
- Domain/service: deterministic precedence evaluator with injected clock/config; no network I/O.
- Infrastructure: source adapters and append-only timestamped observation storage.
- API: explicit endpoint for evaluating a supplied snapshot and separate endpoint for latest persisted review; never let caller-supplied snapshots masquerade as live provider data.
- UI: show source/as-of/freshness, execution state, overnight profile, catalyst status, reasons, and action separately.
- Tests: missing/stale/future timestamps, unknown catalyst, expired READY event, permission off, extension danger, excluded symbol, malformed numeric inputs, mismatched symbol/timeframe, and repeated deterministic evaluations.

## Acceptance criteria before live use

- Same immutable input/config yields the same assessment.
- Every non-READY outcome contains stable reason codes.
- Missing/stale values cannot default to zero or clear.
- Ranking cannot override 5M permission or 1M READY expiration.
- Every displayed value exposes source and as-of time.
- API/UI clearly distinguish caller-supplied test snapshots from provider-backed observations.
- Historical comparison and TOS parity are completed before using ranking as actionable guidance.

## Current blockers

- Provider selection/entitlement and production-quality collection/recovery.
- Fully calculated overnight metrics and complete GTC_EXT session coverage.
- Catalyst/news source with coverage, timestamp and licensing checks.
- Exact installed Thinkorswim study parity and validated freshness thresholds.
- Persistence, evaluator wiring, API integration, dashboard integration and tests are not implemented by this document.
