# Scanner and Watchlist Workbench

**Status:** Local front-end workbench implemented; live scanner/provider integration remains unimplemented.

## What exists

The static front end now provides:

- Four documented scanner groups: Momentum Hunter, Breakout Trigger, Reclaim/Continuation, and Market Movers/Discovery.
- Candidate rows with symbol, score, status, and reason.
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
