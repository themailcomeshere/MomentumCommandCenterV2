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
