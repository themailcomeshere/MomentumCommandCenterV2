# MCC Next Steps and External Dependencies

**Updated:** 2026-10-23 (Arizona time)
**Canonical companion to:** `docs/Master-To-Do.md`
**Purpose:** Separate development work that can proceed locally from user decisions, account access, and validation that cannot be fabricated.

## Direct answer: do you need both provider accounts?

**No. Create neither paid subscription yet.** MCC can continue to be developed and tested without vendor credentials. Accounts are needed to verify real responses and entitlements, not to write most of the infrastructure.

The current code has historical REST adapters for Alpaca and Massive. It does not yet have a live streaming collector, scanner integration, broad movers, quote/spread feed, news/catalyst source, or production recovery/backfill pipeline.

Suggested sequence:
1. Create one free Alpaca account first. Use it for initial API plumbing and schema/provenance tests; never treat its free-tier results as production-grade market authority. Defer Massive until a second provider materially improves comparison.
2. Configure one provider at a time with server-side environment variables and test the adapter against real responses. Do not put keys in source control, browser JavaScript, screenshots, or chat.
3. Keep paid subscriptions on hold until we complete the documented proof-of-fit comparison and verify personal-use/display rights, historical retention rights, extended-hours behavior, rate limits, scanner/mover coverage, and exact plan entitlements.
4. If you prefer to minimize setup, start with one free provider. We can implement the provider-neutral contracts and test harness first, then add the second account when it materially improves the comparison.

The current research shortlist is Alpaca Algo Trader Plus and Massive Stocks Advanced, but neither is selected or verified. See `docs/Provider-Comparison.md`.

## User actions that genuinely require your input

### A. Provider access (only for live-data verification)
- [ ] Create a free Alpaca account and generate an API key/secret. No paid plan is needed for this first smoke test.
- [ ] Configure credentials locally using .NET User Secrets; exact commands are in `src/MomentumCommandCenterV2.API/README.md`. `appsettings.json` is for non-secret defaults only. Do not send secrets in chat or commit them.
- [ ] Confirm which feed the account actually allows. Keep `iex` unless the account explicitly entitles you to another feed; do not configure SIP unless entitled.
- [ ] Run the API locally, inspect `/api/providers/status`, then run a small `/api/market-data/compare` request. Report sanitized status/error messages and observed timestamps only.
- [ ] We will not call the provider "verified" until real authentication, feed entitlement, timestamps and coverage are checked. Defer Massive account setup until a second provider is needed.

### B. Thinkorswim parity evidence (required before trusting trading gates)
- [x] Received the XRPN 2026-10-02 screenshot with normal 5M study, Authority Diagnostic V2 and readable time axis covering the target window (user clarified the top-left chart date can be ignored). Do not request this screenshot again.
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

## 2026-10-26 export audit reconciliation

The files already committed to `feature/opportunity-quality-classification` have been reviewed. Do not ask the user to re-upload the same files.

- The October 19 XRPN 1M/5M pair is available and useful for OHLCV coverage checks. Its 1M export has 30 absent minute timestamps from 09:00–12:59 Arizona; 27 complete post-09:00 5M windows exactly match aggregated 1M OHLCV.
- The file named `data/StrategyReports_XRPN_10022026_1M_EntireDay.csv` actually contains October 22 data, while its 5M companion is October 2. This is a filename/content date mismatch, not an aligned pair.
- These reports contain raw OHLCV payloads, not internal ThinkScript READY/Permission diagnostics. To diagnose why READY is rare, the missing evidence is the gate state on the same bars (or an equivalent diagnostic export), not another screenshot of the already supplied October 2 chart.
- The current files do not cover the full GTC_EXT-eligible window for Boring Overnight target research. `eligible-window-high-excursions.csv` is placeholder data and must not be used as market evidence.

See `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md` for row counts, date/range checks, complete-window comparison, limitations, and next steps.
