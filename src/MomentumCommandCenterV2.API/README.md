# MomentumCommandCenterV2.API

ASP.NET Core .NET 8 host for the existing frontend workbench and server-side market-data adapters.

## Run

From the repository root:

```bash
dotnet run --project src/MomentumCommandCenterV2.API
```

Open the URL printed by ASP.NET Core. The API serves the existing dashboard from the frontend directory when that directory exists. Health endpoint: /api/health.

## Configure credentials locally

The API project is configured for .NET User Secrets. Use that for local development; the tracked `appsettings.json` contains only the default Alpaca feed (`iex`) and no credential values. ASP.NET Core also reads environment variables using the standard configuration mapping.

From the repository root, after creating your Alpaca account and generating an API key/secret, run these commands in a terminal on the same machine that runs MCC:

```powershell
dotnet user-secrets set "MarketData:Alpaca:ApiKey" "YOUR_ALPACA_KEY_ID" --project src/MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Alpaca:ApiSecret" "YOUR_ALPACA_SECRET_KEY" --project src/MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Alpaca:Feed" "iex" --project src/MomentumCommandCenterV2.API
dotnet run --project src/MomentumCommandCenterV2.API
```

Replace the two placeholders locally; do not paste real keys into chat, source files, screenshots, or issue reports. Keep `iex` unless your Alpaca account explicitly entitles you to another feed. The credentials are stored in the local .NET User Secrets store outside the repository. Do not use real credentials in `appsettings.json`.

To verify configuration without exposing keys, open `/api/providers/status`. It reports only whether each provider is configured; that is not proof that authentication, feed entitlement, timestamps, or data coverage are valid. The first real-data smoke test is `/api/market-data/compare` with a small symbol/time window. Check returned errors and bar timestamps before considering the provider validated.

Environment-variable alternatives for the API process use standard ASP.NET Core double-underscore mapping:
- `MarketData__Alpaca__ApiKey`
- `MarketData__Alpaca__ApiSecret`
- `MarketData__Alpaca__Feed`

For Massive later, use `MarketData:Massive:ApiKey` with User Secrets or `MarketData__Massive__ApiKey` as an environment variable. Never expose credentials to the frontend.

## Endpoints

- GET /api/health
- GET /api/providers/status
- GET /api/market-data/compare?symbol=UPC&timeframe=1m&from=2026-10-22T16:00:00Z&to=2026-10-22T17:00:00Z
- GET /api/morning-brief

The comparison endpoint requests the same window from both adapters, returns first/latest observations, duplicate timestamps, and observed timestamp gaps. It does not assume an empty interval means zero volume or provider failure. The Massive adapter filters aggregate results back to the exact requested instants because the REST endpoint's date range is date-based.

## Explicit current limits

These are first REST/history adapters, not a production market-data stack. An append-only local JSONL raw-bar store is implemented and wired to comparison/Morning Brief requests. WebSocket streaming, reconnect/backfill orchestration, scanner/movers, quote/spread coverage, news/catalysts, rate-limit coordination, provider licensing review, full pagination, and market-session-aware gap classification are not implemented. A successful HTTP request does not prove that the selected plan/feed is appropriate for live decisions.

The Morning Brief intentionally returns DATA NOT FOUND IN SOURCES / NOT CALCULATED for sectors, broad movers, catalysts, validated market regime, Red Zone qualification, and Boring Overnight until those sources and pipelines exist. No auto-trading or order submission exists.


## Raw bar observation store

Provider-comparison requests append valid returned bars to an append-only JSONL file outside the repository by default: LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl. Set MCC_MARKET_BAR_STORE_PATH to choose another path. Identical observations are skipped by source/symbol/timestamp/content hash; changed values are appended as corrections linked to the prior observation ID. Each record includes source time and receive time. This is a local persistence foundation, not a managed database or full late-event/reconciliation pipeline.


## What requires accounts versus what does not

Accounts are not required to continue implementing provider-neutral infrastructure or fixture-based tests. Free credentials are useful for validating real authentication, response shapes and timestamps. Do not buy a paid plan until proof-of-fit and licensing checks are complete. One configured provider can be smoke-tested; both are needed only for direct side-by-side provider comparison. See [Next Steps and User Inputs](../../../docs/Next-Steps-and-User-Inputs.md).
