# API developer workflow — Swagger UI

## Local launch

From the repository root:

```powershell
dotnet run --project src/MomentumCommandCenterV2.API --configuration Release
```

Use the HTTP/HTTPS address printed by ASP.NET Core. Open `/swagger` on that same origin for interactive OpenAPI documentation. `/api/health` remains the fast liveness check. Swagger is a local developer/test surface; it does not mean provider credentials are valid or that endpoints are safe to rely on for trading.

## Useful endpoints

- `GET /api/health` — API process health and decision-support-only posture.
- `GET /api/providers/status` — provider configuration and whether non-empty data has been returned in this process.
- `GET /api/market-data/compare` — fetch and compare a bounded historical window across configured providers.
- `GET /api/morning-brief` — current benchmark observations; several sections intentionally return DATA? / NOT CALCULATED until live sources and scoring are connected.
- `POST /api/boring-overnight/evaluate` — deterministic hard-gate evaluation from a supplied candidate snapshot. This is not yet connected to automatic scanner discovery and does not calculate a validated sell target.

## Boring Overnight request example

```json
{
  "symbol": "EXAMPLE",
  "dataFresh": true,
  "price": 25.0,
  "sma20": 24.5,
  "averageDailyVolume20": 1200000,
  "overnightWinRatePct": 75.0,
  "averageOvernightPct20": 0.35,
  "averageOvernightPct5": 0.55,
  "medianOvernightPct20": 0.30,
  "currentOvernightPct": 0.25,
  "excluded": false,
  "catalystStatus": "CLEAR",
  "fiveMinutePermission": true,
  "fiveMinuteStructure": "BULLISH",
  "oneMinuteState": "EXECUTION READY",
  "extensionAtr": 0.8,
  "currentVolumeVsAverage": 1.2,
  "dailyRangePct": 1.8,
  "atrPct": 1.5
}
```

Percent values are percentage points: `0.5` means 0.5%. Use `catalystStatus` = `CLEAR`, `RISK`, or `UNKNOWN`. Unknown or missing critical inputs return DATA?; failed hard gates return BLOCKED; soft warnings return WATCH. READY means only the supplied snapshot passed these gates. Do not submit orders from this result.

## Security and evidence

Never paste credentials into Swagger fields, browser storage, issue comments, or chat. Provider secrets remain server-side User Secrets/environment configuration. Report sanitized errors and timestamps only. Successful historical bars validate that specific request path, not streaming, complete session coverage, entitlement breadth, or live trading readiness.


## StrategyReport CSV upload

Open `POST /api/strategy-report/upload`, choose the multipart `file` field, and upload a Thinkorswim StrategyReport `.csv`. Example limits: 10 MB and 10,000 valid data rows. Response includes source headers, full parsed rows, first 100 preview rows, row warnings, and likely timestamp/symbol/event column names. The parser preserves report content; a StrategyReport is not assumed to be a continuous OHLCV export.

## Exclusion watchlist

- `GET /api/watchlists/exclusions` lists the local MCC exclusions.
- `POST /api/watchlists/exclusions` example body: `{ "symbol": "TTE", "reason": "Manual exclusion" }`.
- `DELETE /api/watchlists/exclusions/TTE` removes TTE from MCC's local list.

The evaluator automatically checks this local list. It is not synchronized to the personal Thinkorswim watchlist. Do not assume a TOS Add to Watchlist action updates MCC until a supported import/sync path is implemented.

## Current verification

User previously reported 91 passing Release tests before this change set. Current-head GitHub Actions run must finish successfully before the new endpoints are treated as verified. Never expose unauthenticated Swagger/API beyond a trusted local environment.


## Dashboard tools

When launched through the API, the dashboard now includes:

- **MCC exclusion watchlist**: add/remove/refresh symbols against the persisted API list. This is the list used by the Boring Overnight evaluator. It is separate from the browser-local Working Candidates list and separate from Thinkorswim's personal watchlist.
- **StrategyReport upload**: select a Thinkorswim CSV, upload it, review parsed row/column counts and warnings, inspect a preview, and download the complete parsed JSON.

The exclusion list is local to the API host and stored at `App_Data/exclusions-watchlist.json`. Back it up if needed. There is no verified direct Thinkorswim watchlist sync.
