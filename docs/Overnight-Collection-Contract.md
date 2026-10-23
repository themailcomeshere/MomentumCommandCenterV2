# Overnight Scanner Collection Contract (Provider-Neutral)

**Status:** Specification only; no live provider, collector, persistence adapter, or scheduled service is implemented by this document.  
**Target workflow:** A reliable, timestamped Morning Report available around 08:30 Arizona time.

## 1. Non-goals and authority

- Scanner discovery is candidate-universe input, not a signal.
- It cannot create or override 5M Permission, 1M EXECUTION READY, position management, or a broker order.
- No automatic order submission or Schwab API assumption.
- No fabricated bars, synthetic observations, silent gap filling, or stale-as-current substitutions.
- The collector must operate independently of the browser tab.

## 2. Observation envelope

Every received scanner discovery or market observation must preserve:

- `schemaVersion` and adapter/collector version.
- `sourceId`, source event ID when available, scanner/query identity, symbol, and raw source fields/payload (subject to provider licensing and retention rules).
- `sourceTimestamp` when supplied and distinct `receivedAt` / `observedAt` timestamps as `DateTimeOffset`.
- Explicit source timezone/session semantics; do not reinterpret an offset-free timestamp as local time.
- Ingestion disposition: accepted, duplicate, invalid, stale, or rejected, with stable reason codes.
- Instrument/timeframe and interval boundaries for market bars; OHLCV invariants must be checked before downstream use.
- Provider payload/version identity sufficient to reproduce the normalized record where permitted.

A source timestamp may be absent; preserve that absence. Never replace it with receive time.

## 3. Persistence contract

- Persist raw/normalized observations append-only; corrections are new records linked to the original, not silent overwrites.
- Enforce idempotency where a stable source event ID exists. Otherwise use a documented composite key and retain collision diagnostics.
- Keep source payload, normalized fields, ingestion decision, and evaluation result separable.
- Persist report snapshots with generation time, report date/session, input watermark, scoring/config version, and row-level provenance.
- Store secrets only in an appropriate local secret store or environment configuration; never in frontend assets, localStorage, committed files, or logs.

## 4. Health and freshness

Collector health values: `RUNNING`, `DEGRADED`, `STOPPED`, `UNKNOWN`.

Expose at least: last successful poll/receive, last source timestamp, latest persisted event, consecutive failures, last error category, active provider/adapter version, and whether the host has resumed from sleep/restart.

Freshness thresholds are provider- and field-specific and must be configured from the selected provider's documented semantics. Until a provider is selected, exact thresholds are **not specified**. Stale or unknown critical inputs must lead to `DATA?` / manual review, never READY.

Detect and surface duplicate events, timestamp regressions, missing intervals, unexpected session gaps, authentication expiry, rate limiting, provider outages, process restarts, and host sleep/wake. Never manufacture data to make a series look continuous.

## 5. Collection lifecycle

1. Validate configuration and provider entitlement before starting.
2. Load the last durable checkpoint and establish a source-supported resume point.
3. Start polling/streaming with cancellation and bounded retry/backoff; honor rate limits and provider terms.
4. Validate and persist each observation before publishing it downstream.
5. On disconnect, publish DEGRADED and retain diagnostics; reconnect without duplicating persisted observations.
6. On shutdown, flush durable writes and checkpoint only successfully persisted progress.
7. On restart/wake, reconcile the checkpoint with the provider's supported backfill/replay behavior; if completeness cannot be established, flag the affected interval unknown.

## 6. Morning Report snapshot

Each report must include:

- Arizona generation timestamp and explicit market-session/date context.
- Collector health and per-source freshness, last-success timestamp, and unresolved gaps.
- Input watermark and schema/config/scoring versions.
- Ranked discovery candidates with source/time/provenance and reason codes.
- Separate candidate disposition, 5M authority state, 1M execution state, extension risk, and data confidence.
- Up to four Red Zone tiles only when supported by fresh validated inputs; fewer than four is valid.
- Boring Overnight component inputs and every hard-gate PASS/FAIL/UNKNOWN; unknown catalyst status cannot be shown as clear.
- Exportable immutable report snapshot and a visible “not running / incomplete / stale” state where appropriate.

Ranking is not trade permission. Empty queue and zero position are valid outputs.

## 7. Failure/recovery tests required before operational use

- Provider timeout, rate limit, service outage, and authentication expiry.
- Duplicate/replayed event, source timestamp regression, malformed row, and symbol mismatch.
- Market session boundary, extended-hours convention, daylight-saving boundary, and timezone offset preservation.
- Machine sleep/wake, process crash/restart, partial persistence failure, and backfill gap.
- Stale/missing catalyst data, stale market data, and incomplete report input.
- Deterministic regeneration of the same report from the same persisted input watermark and versioned configuration.

## 8. Decisions blocked on user/provider input

- Provider and product tier/entitlement; licensing/retention permissions.
- Required scanner universes, fields, and acceptable discovery latency.
- Provider-supported streaming/polling/backfill behavior and exact freshness thresholds.
- Local service vs desktop packaging and preferred startup/recovery behavior.
- Historical Morning Routine steps/metrics: **Data not found in sources.** Current manual is a proposal based on documented requirements, not a verified transcript of the earlier routine.

Until these decisions are resolved and the collector is implemented/tested, production status remains **NOT CONNECTED / NOT RUNNING**.
