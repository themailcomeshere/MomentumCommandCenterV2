# 2026-11-04 runtime follow-up and remaining-work execution notes

Branch: `feature/opportunity-quality-classification`

## Runtime evidence received

### GTC_EXT capture

The user's POST requests for RTO and EVR to `/api/boring-overnight/capture-sell-window-history` both returned HTTP 422. The response bodies were not included, so the exact runtime exception text is not yet available.

The separate EVR 20-session capture returned:

- Provider/feed: Alpaca / IEX.
- Exact previous-close / next-open pairs: 12 valid, 8 missing.
- Daily expected-session coverage: 20 sessions, but `IEX_PARTIAL`; qualification metrics are null and `qualificationUsable=false`.
- Catalyst: 0 articles, `UNKNOWN`; no-news is not clearance.
- Snapshot evaluation: `DATA?`, ineligible.
- No order was submitted.

The repository's committed `appsettings.json` has `MarketData:Alpaca:Feed=sip`, while the running API reports IEX. The effective runtime setting is therefore being overridden or the running process is not using the expected configuration/build. The exact source must be read from `GET /api/providers/status`; do not assume the committed JSON file controls the running process.

## Code change made in response

The GTC_EXT capture service now checks an Alpaca adapter's effective configured feed against the requested feed before making any provider request. A mismatch throws a descriptive validation error identifying the configured feed, the requested feed, the `/api/providers/status` diagnostic, and the need to restart after correcting the effective `MarketData:Alpaca:Feed` setting. This prevents a multi-week SIP request from unnecessarily fetching IEX data and then failing at the post-fetch source validation.

A regression test was added using an Alpaca adapter configured as IEX and a requested SIP capture. It asserts the fail-fast error and that no observations are persisted.

Validation is pending GitHub Actions for:
- Code commit `a283dd2517fd68627beabdbd9e58d8891f619c88`.
- Test commit `c901f8e8afa22d24c7a6dab625ab7918e3e8e678`.

Do not treat this change as validated until the exact-head workflow run passes.

## Remaining work — dependency-ordered

### 1. Opportunity Quality current-data qualification and ranking

Existing state: the pure diagnostic assessor and unit tests exist. It is not wired to the live market-data snapshot, dashboard, or replay. The live assessment currently exposes completed 1m/5m bars, source timestamps/freshness, 5m same-time-of-day relative volume, 1m ATR extension, and the V2 decision. It does not yet supply a complete validated Opportunity Quality input snapshot with a documented recent-range baseline, resistance distance, and explicit finite execution-event lifetime.

Next implementation must preserve strict separation between V2 eligibility and quality ranking. Missing or stale measurements remain DATA_UNKNOWN. Provisional thresholds stay diagnostic-only. Do not equate V2 `entryAllowed` with a Thinkorswim execution event until parity is established.

### 2. Official 09:30 ET opening-auction print validation

Existing 09:30 ET one-minute bar Open is explicitly a proxy. The required strategy definition remains the official opening-auction print. Need a consolidated SIP trade-level source with auction condition evidence and exact timestamp/price parity, plus comparison to Thinkorswim. Do not substitute the first later bar or a nearby trade.

### 3. Human-labelled catalyst validation

Current Alpaca News phrase triage is provisional. No-news is UNKNOWN; no status can emit CLEAR. Human-labelled examples, authoritative-source coverage, adjudication rules, and held-out false-positive/false-negative measurements are absent. The user must review/label actual ticker-linked articles; classifier performance cannot be claimed before that work.

### 4. Connect complete GTC_EXT observations to sell-target calculations

The service and calculator exist, but no complete 20-session SIP GTC_EXT runtime dataset has been received. The user's recent capture failed because the runtime feed is IEX. Only connect calculator observations after all 20 sessions contain the exact prior regular close and all 780 expected minute bars, and ATR parity is MATCH.

### 5. Walk-forward target validation

Blocked by item 4 and the absence of a verified out-of-sample outcome dataset. In-sample historical high-touch is not fill probability. Validation must use time-separated windows and report target touch, MAE/MFE, and execution sensitivity separately.

### 6. Actual Schwab fills, exits, fees and realized P/L

The Schwab trade CSV importer exists, but the current export format does not provide quantity and the existing importer treats filled orders as one unit. Actual account sync/OAuth is not implemented; no verified fills, fees, or execution data is connected. The integration plan requires the OAuth callback/hosting origin, app registration, server-side credentials, token persistence, and read-only account/order/fill endpoints. Do not infer quantities or fees.

## Operator evidence required

1. Call `GET /api/providers/status` and provide the Alpaca object, especially `configuredFeed`, `configuredFeedSource`, and `feedConfigurationSources`. Do not provide credentials.
2. If the effective feed is IEX, remove/adjust the overriding setting so it is SIP, restart the API process, then call `/api/providers/status` again. Do not rerun captures until the status response says SIP.
3. Repeat the GTC_EXT capture for RTO and EVR. If it still fails, provide the full HTTP response body from the 422/409 response.
4. Repeat EVR `capture-history` after SIP is active. Provide the exact eight `history.missing` entries and `boundaryCoverageAudit`; nearest bars are diagnostic only.
5. Human catalyst labels and official auction-print parity require source records/Thinkorswim comparison; those cannot be inferred from the current API response.



## Schwab CSV quantity integrity update

The Schwab CSV importer previously substituted quantity=1 because the reviewed order-history export did not include a quantity field. That behavior could produce fabricated position sizes and realized P/L. The importer now requires an explicit positive integer `Quantity` column and rejects missing/invalid quantities; it no longer silently assumes one share. Tests cover multi-share FIFO matching and missing-quantity rejection.

This is a safety improvement, not completion of Schwab integration. The operator must export a Schwab order-history format that includes filled quantity. Fees/commissions, order IDs, partial-fill aggregation, account sync/OAuth, actual order/position state and realized P/L reconciliation remain outstanding. A CSV without the Quantity column is intentionally rejected.


## Catalyst human-label collection layer

Added `CatalystHumanReviewStore` and `POST/GET /api/market-data/catalyst-reviews`. The append-only JSONL store validates symbol, source URL, headline and a fixed human label taxonomy (`MATERIAL_RISK`, `MATERIAL_EVENT`, `NON_MATERIAL`, `IRRELEVANT`, `DUPLICATE`, `UNCERTAIN`). It does not write to the automated assessment, does not infer CLEAR, and does not train a classifier. The local API currently has no reviewer identity/authentication or review UI. The next operator step is to inspect actual returned article bodies/filings and submit human labels; classifier evaluation still needs adjudicated, time-separated examples.


## 2026-11-04 UI handoff update — PowerShell replaced for routine captures

The production and prototype dashboards now include a user-facing GTC_EXT capture button. It preflights `GET /api/providers/status` and invokes `POST /api/boring-overnight/capture-sell-window-history` only when the effective Alpaca feed is SIP. The user no longer needs to run that capture endpoint manually in PowerShell for normal operation. Current runtime feed is IEX, so the button should report that capture was not started; this is intentional fail-fast behavior.

The existing 20-session close/open capture button is now explicitly described as the retry action for missing exact timestamps, including EVR's eight opens. It can only persist exact bars actually returned by the provider.

The front-end qualification logic now returns DATA? whenever one or more required historical custom-expression values are missing. It does not count missing values as historical failures. Confirm the November 4 443-row import in the browser: 200 complete rows and 243 unavailable rows should be visible as complete/unknown respectively; 32 complete rows are historical fails according to the November 4 scanner review.

The calculated-limit engine remains a library component, not a completed live API/UI workflow. GTC_EXT capture is now exposed in the dashboard, but the engine must not be connected to a numeric target until all 20 SIP windows are complete, ATR parity is MATCH, and walk-forward target validation is complete.
