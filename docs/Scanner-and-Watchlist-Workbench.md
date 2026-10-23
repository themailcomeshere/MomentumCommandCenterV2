# Scanner and Watchlist Workbench

**Status:** Local front-end workbench implemented; live scanner/provider integration remains unimplemented. The dashboard includes a local-only feedback notebook and explicit connection/readiness status.

## Scanner integration status update (2026-10-22)

The Application layer now has a provider-neutral discovery DTO at `src/MomentumCommandCenterV2.Application/Models/ScannerDiscoveryEvent.cs` and source boundary at `src/MomentumCommandCenterV2.Application/Interfaces/IScannerDiscoverySource.cs`. These are contracts only: there is no concrete provider adapter, persistence, polling workflow, or live scanner connection yet. A provider must not be presented as integrated until those parts exist and are tested.

The DTO preserves first-seen/observed/source timestamps, source and schema identifiers, raw fields, evaluator version, reason codes, and a presentation disposition. The disposition values (`ActionableQueue`, `WatchPullback`, `FilteredExtended`, `Blocked`, `DataUnknown`) are scanner-list presentation states only; they do not create or replace 5M Permission or 1M EXECUTION READY.

### Extension qualification requirement

Candidates currently outside the exact 1M study's extension envelope should be removed from the default actionable queue or deprioritized, with a visible reason and timestamp, but retained in a recoverable pullback-watch list. Do not permanently exclude a symbol simply because it is extended now. Missing or stale extension inputs must result in `DATA?`. Exact ATR-extension formula/threshold parity and historical data validation are prerequisites to implementing this gate. See `docs/Master-To-Do.md` and `docs/Validation/MCC-OHLCV-20261020.md`.

## What exists

The static front end now provides:

- Four documented scanner groups: Momentum Hunter, Breakout Trigger, Reclaim/Continuation, and Market Movers/Discovery.
- Candidate rows with symbol, score, status, extension input, freshness, and reason; absent extension/freshness fields display as unknown rather than implying a safe state.
- A system-readiness panel that explicitly shows market data as demo-only, Thinkorswim as not connected, live scanner as not selected, and order execution as not implemented.
- A dashboard feedback notebook with category/priority, browser-local persistence, individual deletion, clear, and JSON export. Feedback is not uploaded automatically.
- CSV import from a local file with a required `Symbol` or `Ticker` column and optional `Score`, `Status`, and `Reason` columns.
- One-click add-to-watchlist controls.
- Manual ticker entry and removal.
- Browser-local persistence using `localStorage` under `mcc.watchlist`.
- Explicit local/demo labels; no broker, scanner, news, or order API is called.

## CSV import shape

```csv
Symbol,Score,Status,Reason
XRPN,86,WATCH,Confirm 5M permission and 1M timing
MNSB,78,DATA?,Need fresh market snapshot
```

The import is evidence display only. It does not calculate scanner scores, verify tradability, infer catalysts, or create a trading decision.

## Required production work

1. Define a provider-neutral scanner-event contract with source timestamp, scanner group, raw fields, and freshness.
2. Add an adapter for the selected scanner/provider; do not hard-code a provider into the front end.
3. Persist append-only discovery events so first-seen timestamps are not lost.
4. Correlate discovery events with 5M permission, 1M READY, extension blocks, and outcomes.
5. Reconcile the complete Schwab exclusion/unavailable list.
6. Add explicit stale/missing-data handling; missing scanner data is `DATA?`, never `READY`.
7. Move watchlist persistence behind a validated application/API contract before multi-user or production use.
8. Keep regular-session discovery separate from Boring Overnight candidates.

## Use

Open `frontend/index.html` directly in a browser. Use the scanner group selector for the illustrative local candidates, use **Import CSV** for a local candidate file, and add candidates to the browser-local watchlist. This workbench is not a live scanner and does not place orders.


## Provider decision — current state

**No live scanner provider is selected or connected. Thinkorswim is not currently an API/feed integration.** Thinkorswim remains the source of the versioned chart-study behavior and manually inspected chart evidence; the repository contains no verified Thinkorswim live API/session adapter. The current front end only reads local fixtures and user-imported CSV files.

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

The user's desired operating model is to leave the released/deployed MCC program open overnight so the Morning Report is populated with recent scanner discoveries by 08:30 Arizona time. This is **not supported by the current static browser workbench**. Leaving `frontend/index.html` open does not create scanner traffic, ingest market data, or persist provider observations.

The UI now has a preview-only Morning Command Brief, a responsive Red Zone showing up to four locally ranked demo/import candidates, and a JSON export for that preview. The report clearly identifies itself as PREVIEW_ONLY and says the overnight collector is not running. Supplied scores are sorted for UI demonstration only; they are not the validated MCC opportunity ranking. 5M Permission and 1M execution remain UNKNOWN in these tiles.

Required architecture before overnight collection can be relied on:
1. Select and verify a supported provider and its licensing/entitlements.
2. Implement a long-running provider adapter/collector independent of the browser tab.
3. Persist raw discovery events and timestamped market observations with provenance and schema/version identifiers.
4. Implement health/freshness, gap/duplicate detection, reconnect and restart recovery, and machine sleep/wake handling.
5. Build deterministic Morning Report generation from persisted data and versioned scoring.
6. Connect ranked discovery, continuation watch, Boring Overnight, and Red Zone to that validated data path.

Until then, status must remain NOT RUNNING / NOT CONNECTED. Never represent a demo/import CSV as live data; do not synthesize missing bars; no provider secrets in browser code or localStorage.
