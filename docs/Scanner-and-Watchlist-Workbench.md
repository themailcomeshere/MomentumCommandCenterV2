# Scanner and Watchlist Workbench

**Status (2026-11-02):** Dashboard workbench and CSV import are implemented. Alpaca SIP-based top-gainers/losers discovery has a hosted poller, append-only persistence, and an API endpoint; account entitlement is currently denied for recent SIP requests. Red Zone card assessments refresh automatically, but the TOS Overnight scan is not connected and the Overnight qualification engine is not complete.

## Current scanner integration status — 2026-11-02

The repository now contains a concrete `AlpacaScannerDiscoverySource`, hosted polling service, append-only JSONL event store, and `GET /api/scanner/discoveries`. The source queries Alpaca's SIP-based top-50 gainers/losers endpoint; the collector health is exposed with persisted discovery events.

The user's current Alpaca subscription denied recent SIP data with HTTP 403 (`subscription does not permit querying recent SIP data`). Therefore the collector may be degraded and must not be represented as a fresh/live discovery feed until a successful source poll occurs. The OHLCV IEX feed does not grant SIP access to the separate movers endpoint.

The front end consumes persisted discoveries only when the `Alpaca live discovery` scanner group is selected. It polls that group every 30 seconds. Red Zone card assessments refresh every 15 seconds while the page is visible. The scanner CSV import now immediately re-renders both the scanner table and Red Zone.

The Thinkorswim Overnight scan remains an external candidate source: there is no verified TOS scanner API integration. Export/import of a TOS CSV is still a manual handoff. Red Zone's technical assessment is not the Boring Overnight qualification engine.

### Extension qualification requirement

Candidates currently outside the exact 1M study's extension envelope should be removed from the default actionable queue or deprioritized, with a visible reason and timestamp, but retained in a recoverable pullback-watch list. Do not permanently exclude a symbol simply because it is extended now. Missing or stale extension inputs must result in `DATA?`. Exact ATR-extension formula/threshold parity and historical data validation are prerequisites to implementing this gate. See `docs/Master-To-Do.md` and `docs/Validation/MCC-OHLCV-20261020.md`.

## What exists

The static front end now provides:

- Four documented scanner groups: Momentum Hunter, Breakout Trigger, Reclaim/Continuation, and Market Movers/Discovery.
- Candidate rows with symbol, score, status, extension input, freshness, and reason; absent extension/freshness fields display as unknown rather than implying a safe state.
- A system-readiness panel that distinguishes market data, Thinkorswim (not connected), live scanner discovery, and order execution (not implemented).
- A dashboard feedback notebook with category/priority, browser-local persistence, individual deletion, clear, and JSON export. Feedback is not uploaded automatically.
- CSV import from a local file with a required `Symbol` or `Ticker` column and optional `Score`, `Status`, `Reason`, `Extension`/`Ext`, and `Freshness`/`Data_As_Of` columns; importing immediately refreshes Red Zone.
- One-click add-to-watchlist controls.
- Manual ticker entry and removal.
- Browser-local persistence using `localStorage` under `mcc.watchlist`.
- Explicit preview/discovery labels; no broker order API is called. Alpaca persisted discovery and market-assessment API calls are used by the dashboard.

## CSV import shape

```csv
Symbol,Score,Status,Reason
XRPN,86,WATCH,Confirm 5M permission and 1M timing
MNSB,78,DATA?,Need fresh market snapshot
```

The import is evidence display only. It does not calculate scanner scores, verify tradability, infer catalysts, or create a trading decision. It immediately updates the scanner table and Red Zone candidate rows; each Red Zone card then requests current 1M/5M technical assessment.

## Remaining production work

1. Validate recent-SIP entitlement or select an independently entitled scanner source; record collector health and freshness.
2. Expand discovery beyond top-50 gainers/losers only if a licensed provider and required universe/filter coverage are established.
3. Correlate discovery events with 5M permission, 1M execution, extension blocks, and outcomes.
4. Reconcile the complete Schwab exclusion/unavailable list.
5. Add explicit stale/missing-data handling; missing scanner data is `DATA?`, never `READY`.
6. Move watchlist persistence behind a validated application/API contract before multi-user or production use.
7. Build the separate Boring Overnight history, catalyst, liquidity, and decision pipeline; do not treat top-mover discovery as an Overnight scan.

## Use

Serve the dashboard through the MCC API (`http://localhost:53360/`) so same-origin API calls work. Use the scanner group selector for local illustrative candidates or the Alpaca live discovery group, use **Import CSV** for a Thinkorswim/local candidate export, and add candidates to the browser-local watchlist. The Alpaca source is a limited SIP-based movers feed, not a full-market scanner. This workbench does not place orders.


## Provider decision — current state

**Alpaca is the implemented first discovery adapter.** The API polls its SIP-based top-50 stock gainers/losers endpoint and persists discovery events. The user's current subscription returned HTTP 403 for recent SIP data, so fresh collector operation is not validated. Complete-universe coverage is not provided. Thinkorswim remains the chart-study evidence source and external Overnight scan; no verified TOS live scanner/API adapter exists. The front end reads local fixtures, user-imported CSV files, and persisted Alpaca discoveries via the API.

Before choosing a feed, compare the actual capabilities needed: real-time 1M/5M OHLCV, scanner/query support, symbol universe and filters, timestamps/session conventions, extended-hours coverage, entitlement/licensing, rate limits, reconnect behavior, and historical access. The exact provider/API choice is **Data not found in sources.** Do not wire the UI to an unverified endpoint or represent a provider as selected.


## Extended Momentum / Continuation Watch

The desired behavior is to preserve visibility for strong stocks that have already gained roughly 10–15% before the user begins reviewing them. EXT remains a real risk warning, but extension alone should not permanently discard the candidate. A separate watch disposition can retain the symbol for monitoring without labeling it executable.

Required state distinctions:
- **EXTENDED — NO ENTRY NOW:** outside the validated extension envelope; not actionable.
- **CONTINUATION WATCH:** current trend/catalyst and price-volume evidence justify monitoring, but no fresh execution event is confirmed.
- **FRESH ENTRY EVENT:** only after the unchanged 5M Permission and 1M execution conditions pass, plus the validated entry policy and risk checks.

Any catalyst/news inputs must include source, publication time, observation time, and freshness. Missing/stale news or price data is unknown, not positive. News strength alone cannot produce eligibility. Candidate retention must not suppress EXT warnings or override the exact source-study formula. Test a distinct pullback/reclaim or consolidation-breakout path against historical/out-of-sample data before it affects actionable ranking.

The existing red "In-trend candidate" bubble has not been semantically verified from the exact installed ThinkScript in the repository. Its meaning is **Data not found in sources** until its formula and relevant chart states are checked.

## Schwab / order-entry boundary

MCC remains non-executing. Manual Schwab website entry is the preferred baseline. Do not integrate broker credentials or implement one-click "Buy Maximum Amount." A future non-submitting sizing calculator may support a custom budget or an explicitly selected maximum budget, but it must show its assumptions, current price timestamp, calculated whole-share quantity, reserve/cushion, and final manual review warning. Any "Open Schwab" navigation should wait until a legitimate supported link/prefill workflow is verified.


## Persistent overnight collection and Morning Report (2026-10-22)

The API process hosts an Alpaca SIP-based scanner discovery poller and persists its observations independently of the browser tab. However, the current account's recent-SIP entitlement denial can prevent fresh events. Leaving the browser open is not required for server-side polling while the API process is running. The current implementation is limited to Alpaca top-50 gainers/losers; it is not the Thinkorswim Overnight scan or a full-market Overnight qualification system.

The UI now has a preview-only Morning Command Brief, a responsive Red Zone showing up to four locally ranked demo/import candidates, and a JSON export for that preview. The report clearly identifies itself as PREVIEW_ONLY and says the overnight collector is not running. Supplied scores are sorted for UI demonstration only; they are not the validated MCC opportunity ranking. 5M Permission and 1M execution remain UNKNOWN in these tiles.

Required architecture before overnight collection can be relied on:
1. Select and verify a supported provider and its licensing/entitlements.
2. Implement a long-running provider adapter/collector independent of the browser tab.
3. Persist raw discovery events and timestamped market observations with provenance and schema/version identifiers.
4. Implement health/freshness, gap/duplicate detection, reconnect and restart recovery, and machine sleep/wake handling.
5. Build deterministic Morning Report generation from persisted data and versioned scoring.
6. Connect ranked discovery, continuation watch, Boring Overnight, and Red Zone to that validated data path.

Until then, status must remain NOT RUNNING / NOT CONNECTED. Never represent a demo/import CSV as live data; do not synthesize missing bars; no provider secrets in browser code or localStorage.


## Morning routine recovery and provider-neutral persistence scaffold (2026-10-23)

- Requirements from the Project source file Morning.txt are consolidated in docs/Morning-Report-and-Routine.md. The intended Red Zone location is upper-right; a left Watchlist/News rail remains a user-feedback decision.
- Provider candidates and fit-testing requirements are documented in docs/Provider-Comparison.md. No provider is selected or authenticated.
- A JSONL append-only discovery event store and application ingestion validator are now scaffolded. This persists discovery events and receipt time, deduplicates by provider event ID or a deterministic fallback key, and rejects malformed log lines. It does not poll/stream a provider, persist full OHLCV bar history, or generate the live Morning Report.
- The precise evidence needed for installed-study validation is documented in docs/Validation/5M-to-1M-User-Validation-Steps.md.
- The Red Zone is being positioned in the upper-right command workspace. Current content remains preview/demo until real source data, validation, and service integration exist.


## Polling worker scaffold update (2026-10-23)

A generic cancellable polling worker and health snapshot now exist in the Application layer. It retries on the next configured poll after a source error and records RUNNING / DEGRADED / STOPPED / UNKNOWN. An offline test covers an empty successful batch and graceful cancellation. No concrete source, host startup wiring, or live collection exists until a provider is selected.
