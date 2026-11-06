# Dashboard workflow — main index only

**Authoritative Dashboard entry point:** `frontend/index.html` (local API root URL `/`).

The approved layout previously developed in `frontend/prototypeIndex.html` has been migrated into `frontend/index.html`. The separate prototype page has been removed to prevent testing the wrong page or maintaining divergent markup.

## Rules for all future Dashboard work

1. Implement and test UI changes directly in `frontend/index.html` and shared assets.
2. Do not recreate a separate prototype HTML entry point unless the user explicitly requests one.
3. Preserve all main-index functionality when changing layout: StrategyReport ATR import controls, calculated sell-limit evidence controls, Morning Brief, Quick Start, scanner/Red Zone, chart, and decision panels.
4. Browser acceptance must use the local API root URL `/`, which serves `frontend/index.html`. Do not use a stale tab or a copied HTML file.
5. After frontend changes, run the repository test script, restart the API when static assets change, hard-refresh the root page, and verify the actual DOM and browser console/network state.
6. Keep the `app.js` cache-busting query string current when the script changes. The API also sends no-cache headers for `index.html` and `app.js`.

## Migrated UI elements

- Fixed left dock with focused-symbol text input, vertical Intraday Momentum / Boring Overnight switch, Quick Start and Reset controls.
- Responsive top bar with market-session/Overnight countdowns and System Readiness.
- Integrated opportunity-classification card and six metric slots: completed 1M close/timestamp, Daily VWAP, ATR14, RVOL, ATR extension, and an explicitly unassigned placeholder.
- Workspace toggle is bridged to the existing application `#mode` control; focused symbol continues through the existing `#symbol` control.

The page is still decision-support software. No broker order placement is implied by the layout migration.
