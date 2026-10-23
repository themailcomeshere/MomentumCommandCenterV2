# MomentumCommandCenterV2.API

ASP.NET Core .NET 8 host for the existing frontend workbench and server-side market-data adapters.

## Run

From the repository root:

```bash
dotnet run --project src/MomentumCommandCenterV2.API
```

Open the URL printed by ASP.NET Core. The API serves the existing dashboard from the frontend directory when that directory exists. Health endpoint: /api/health.

## Configure credentials securely

Do not place real API keys in source control, appsettings.json, browser JavaScript, screenshots, or issue reports. Use environment variables or .NET User Secrets.

Alpaca:
- ALPACA_API_KEY
- ALPACA_API_SECRET
- Optional ALPACA_DATA_FEED (defaults to iex; configure sip only if entitled)

Massive:
- MASSIVE_API_KEY

Equivalent configuration keys are MarketData:Alpaca:ApiKey, MarketData:Alpaca:ApiSecret, MarketData:Alpaca:Feed, and MarketData:Massive:ApiKey.

## Endpoints

- GET /api/health
- GET /api/providers/status
- GET /api/market-data/compare?symbol=UPC&timeframe=1m&from=2026-10-22T16:00:00Z&to=2026-10-22T17:00:00Z
- GET /api/morning-brief

The comparison endpoint requests the same window from both adapters, returns first/latest observations, duplicate timestamps, and observed timestamp gaps. It does not assume an empty interval means zero volume or provider failure. The Massive adapter filters aggregate results back to the exact requested instants because the REST endpoint's date range is date-based.

## Explicit current limits

These are first REST/history adapters, not a production market-data stack. Websocket streaming, reconnect/backfill orchestration, scanner/movers, quote/spread coverage, news/catalysts, durable raw-bar storage, rate-limit coordination, provider licensing review, full pagination, and market-session-aware gap classification are not implemented. A successful HTTP request does not prove that the selected plan/feed is appropriate for live decisions.

The Morning Brief intentionally returns DATA NOT FOUND IN SOURCES / NOT CALCULATED for sectors, broad movers, catalysts, validated market regime, Red Zone qualification, and Boring Overnight until those sources and pipelines exist. No auto-trading or order submission exists.
