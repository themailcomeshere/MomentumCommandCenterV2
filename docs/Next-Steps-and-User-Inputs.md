# MCC Next Steps and External Dependencies

**Updated:** 2026-10-23 (Arizona time)
**Canonical companion to:** `docs/Master-To-Do.md`
**Purpose:** Separate development work that can proceed locally from user decisions, account access, and validation that cannot be fabricated.

## Direct answer: do you need both provider accounts?

**No. Create neither paid subscription yet.** MCC can continue to be developed and tested without vendor credentials. Accounts are needed to verify real responses and entitlements, not to write most of the infrastructure.

The current code has historical REST adapters for Alpaca and Massive. It does not yet have a live streaming collector, scanner integration, broad movers, quote/spread feed, news/catalyst source, or production recovery/backfill pipeline.

Suggested sequence:
1. If you are comfortable creating free developer accounts, create both free accounts and use them only for initial API plumbing and schema/provenance tests. Never treat their results as production-grade market authority.
2. Configure one provider at a time with server-side environment variables and test the adapter against real responses. Do not put keys in source control, browser JavaScript, screenshots, or chat.
3. Keep paid subscriptions on hold until we complete the documented proof-of-fit comparison and verify personal-use/display rights, historical retention rights, extended-hours behavior, rate limits, scanner/mover coverage, and exact plan entitlements.
4. If you prefer to minimize setup, start with one free provider. We can implement the provider-neutral contracts and test harness first, then add the second account when it materially improves the comparison.

The current research shortlist is Alpaca Algo Trader Plus and Massive Stocks Advanced, but neither is selected or verified. See `docs/Provider-Comparison.md`.

## User actions that genuinely require your input

### A. Provider access (only for live-data verification)
- [ ] Decide whether to create free Alpaca and/or Massive accounts for the first proof of concept. No paid plan is needed for the next coding milestone.
- [ ] If using Alpaca, create API credentials and confirm which feed your account actually allows. Basic normally means IEX; do not configure SIP unless entitled.
- [ ] If using Massive, create an API key and confirm the endpoints/data plan enabled for the account.
- [ ] Set credentials as local environment variables on the machine running the API. Do not send secrets in chat or commit them.
- [ ] Run the API locally and report sanitized status/error messages only. We will not call a provider "verified" until the actual response, timestamps, entitlement and coverage are checked.

### B. Thinkorswim parity evidence (required before trusting trading gates)
- [ ] Provide the exact 5M chart screenshot requested in `docs/Validation/5M-to-1M-User-Validation-Steps.md` for XRPN 2026-10-02 around 12:30–13:00 Arizona time, with the normal 5M study, Authority Diagnostic V2, and readable time axis visible.
- [ ] For the UPC chart-gap question, either provide continuous 1M/5M OHLCV covering 2026-10-22 09:16–09:36 Arizona time, or inspect exact timestamps in TOS. This is only needed to settle that historical incident; it does not block adapter development.
- [ ] When asked for installed-study evidence, capture the study name/version and the relevant bars. A repository file does not prove that the same code is installed in TOS.

### C. UX feedback
- [ ] Review the hosted dashboard after running the API, not just the static HTML. Report any layout or wording issues. Browser rendering has not been independently automated/verified.
- [ ] Confirm whether the proposed left Watchlist/News rail is wanted only after seeing the current responsive layout; the spec intentionally leaves it undecided.
- [ ] Supply your actual trading window only when we implement time-window-specific ranking; do not block the data foundation on it.

## Work I can continue without your accounts or feedback

These tasks should not wait for provider credentials:
- [ ] Add provider pagination, bounded retries, timeouts, rate-limit/error normalization, and tests with sanitized fixture payloads.
- [ ] Add explicit provider-neutral stream/collector interfaces and a background hosted-service lifecycle with RUNNING / DEGRADED / STOPPED / UNKNOWN health. Keep real vendor stream bindings marked NOT CONNECTED until tested.
- [ ] Add collector health and durable ingestion checkpoints, process-restart behavior, duplicate/late/correction handling, and deterministic shutdown tests.
- [ ] Improve raw-bar storage validation, corrupt-log behavior, storage error health reporting, retention/rotation design, and store tests.
- [ ] Define market-session-aware gap classification without labeling an interval as a data defect until the calendar, eligible session and provider semantics are known.
- [ ] Build the Morning Brief pipeline incrementally: benchmark observations and freshness first; then broad market movers, sector/breadth data, and news/catalyst provider contracts. Missing inputs must stay DATA? / NOT CALCULATED.
- [ ] Build a source-neutral scanner-event contract, candidate provenance, deduplication, re-ranking and continuation-watch workflow. Do not let ranking bypass 5M Permission or 1M READY.
- [ ] Implement Boring Overnight calculation contracts and deterministic offline calculations against fixtures; keep catalyst status UNKNOWN without an event source.
- [ ] Continue 5M/1M indicator parity audits and tests from versioned source-controlled ThinkScript. Do not change live trading gates without evidence from the installed studies.
- [ ] Update the canonical To-Do and handoff whenever implementation or verification status changes.

## Decisions that should not be guessed

- [ ] Final paid provider / plan and license rights.
- [ ] Whether the provider's session definitions and bar timestamp semantics match the exact Thinkorswim study behavior.
- [ ] Whether scanner endpoints cover the user's required small-cap and premarket universe.
- [ ] The exact production freshness limits, gap policy, and handling of halts / no-trade intervals.
- [ ] Whether the left Watchlist/News rail belongs in the final layout.
- [ ] Any strategy threshold changes or new continuation-entry rule.
- [ ] Whether MCC has achieved enough measured parity and operational recovery to replace TOS for decision support.

## Proposed milestone order

1. **Offline reliability:** pagination, retries/timeouts, fixture tests, store hardening, health lifecycle.
2. **One-provider smoke test:** configure one free account; verify auth, response schema, source timestamps and historical window. Add the second provider for direct comparison.
3. **Live observation collector:** stream + REST reconciliation, reconnect/backfill, raw data persistence, health/freshness.
4. **Market Brief:** benchmark health, broad discovery/movers, sectors/breadth and sourced catalyst/news data.
5. **Signal parity:** replay same symbol/date against source-controlled and installed Thinkorswim studies; preserve 5M authority / 1M execution separation.
6. **Decision layers:** Red Zone and Boring Overnight from verified inputs, reason codes and DATA? states.
7. **Shadow validation:** run MCC alongside TOS across representative sessions; log differences and missed/false candidates before relying on it.

**Release gate:** MCC is not a TOS replacement for live decisions until data entitlement, live freshness, recovery/backfill, timestamp/session parity, study parity, and failure-state behavior have been tested. No automated order placement is in scope.

## [Uncertainties]

- Provider plan prices and entitlements can change and have not been confirmed in the user's accounts.
- No account credentials, license terms, or live API response payloads were supplied; live compatibility is unverified.
- The user's final layout preference and exact trading window are not yet confirmed.
