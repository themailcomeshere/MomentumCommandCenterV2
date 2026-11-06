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

## API timestamp convention (UTC and Arizona)

All API date/time query parameters and JSON-body date/time fields use ISO-8601 **UTC timestamps ending in `Z`**. Do not enter Arizona local time directly into these fields. Swagger descriptions include UTC examples; where relevant they also show the Arizona equivalent.

For October 26, 2026 (Arizona is UTC−07:00):
- `2026-10-26T13:30:00Z` = 06:30 Arizona.
- `2026-10-26T19:59:00Z` = 12:59 Arizona.
- `2026-10-26T20:00:00Z` = 13:00 Arizona.

Overnight boundary timestamps must be copied from the stored-bars response, not inferred from these examples. Bar timestamps commonly identify the start of a bar, and exact conventions depend on the provider/feed.

Request templates:
- [20-pair evaluate-with-history JSON template](../../../docs/templates/boring-overnight-evaluate-with-history.template.json)
- [Template instructions, UTC/AZ conversion, and negative test](../../../docs/templates/README-boring-overnight-api-templates.md)

## Endpoints

- GET /api/health
- GET /api/providers/status
- GET /api/market-data/compare?symbol=UPC&timeframe=1m&from=2026-10-22T16:00:00Z&to=2026-10-22T17:00:00Z
- GET /api/morning-brief

The comparison endpoint requests the same window from both adapters, returns first/latest observations, duplicate timestamps, and observed timestamp gaps. It does not assume an empty interval means zero volume or provider failure. The Massive adapter filters aggregate results back to the exact requested instants because the REST endpoint's date range is date-based.

## Explicit current limits

This is a research/decision-support market-data stack, not a consolidated, fully licensed production execution feed. An append-only local JSONL raw-bar store is implemented. Alpaca REST history/Daily retrieval and provider comparison are implemented. An optional Alpaca minute-bar WebSocket worker supports acknowledged subscriptions, reconnects, completed 1m bars and late corrections; the live assessment selects WebSocket 1m only when subscription, minimum 50 completed persisted bars, freshness, feed identity and source-transport provenance all pass. Otherwise it falls back to REST; 5m Permission remains REST-backed. This is completed-minute-bar streaming, not raw trades or NBBO quotes.

An optional historical polling collector is implemented for explicitly configured symbols and is disabled when its symbol configuration is empty. It polls 1m/5m history with overlap and persists idempotently. It is not the WebSocket worker, an Overnight event collector, or a market-wide scanner. Inspect `/api/market-data/collector/status` for enablement, cycles, watermarks and errors.

The Alpaca SIP-based top-gainers/losers discovery poller, append-only event store and API are implemented, but the current account has repeatedly received HTTP 403 for recent SIP requests. IEX remains the usable diagnostic OHLCV feed; it is partial-exchange coverage and does not qualify consolidated liquidity, full-universe discovery or GTC_EXT target research. Do not retry SIP unless the operator reports a genuine entitlement change.

Ticker-scoped Alpaca news retrieval, provisional keyword triage and an append-only human review store exist. They are not a validated catalyst classifier: no-news or incomplete coverage remains UNKNOWN, and the automated triage must not assert CLEAR. Quote/NBBO spread, order book, halt-state, complete market-wide universe, validated breadth/sector flow, provider licensing suitability and broker-position synchronization are not established by these components. A successful HTTP request or WebSocket subscription does not prove complete market coverage or decision readiness.

The Morning Brief remains partial and may report DATA NOT FOUND IN SOURCES / NOT CALCULATED for validated sectors, broad-market breadth, catalyst clearance, market regime, Red Zone qualification, and Boring Overnight. No auto-trading or order submission exists.


## Raw bar observation store

Provider-comparison requests append valid returned bars to an append-only JSONL file outside the repository by default: LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl. Set MCC_MARKET_BAR_STORE_PATH to choose another path. Identical observations are skipped by source/symbol/timestamp/content hash; changed values are appended as corrections linked to the prior observation ID. Each record includes source time and receive time. This is a local persistence foundation, not a managed database or full late-event/reconciliation pipeline.


## What requires accounts versus what does not

Accounts are not required to continue implementing provider-neutral infrastructure or fixture-based tests. Free credentials are useful for validating real authentication, response shapes and timestamps. Do not buy a paid plan until proof-of-fit and licensing checks are complete. One configured provider can be smoke-tested; both are needed only for direct side-by-side provider comparison. See [Next Steps and User Inputs](../../../docs/Next-Steps-and-User-Inputs.md).


## Alpaca smoke test — step by step

Use the Alpaca key and secret already saved in this machine's .NET User Secrets store. Do not paste them into chat or commit them. If they were saved for a different project/UserSecretsId, the API may not see them; verify with the provider-status endpoint before resetting anything.

1. From the repository root, run: dotnet run --project src/MomentumCommandCenterV2.API
2. Use the local URL/port printed by ASP.NET Core. Open /api/health and confirm the API responds.
3. Open /api/providers/status. Confirm Alpaca reports configured. This checks configuration only; it does not authenticate to Alpaca.
4. Request /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z. This asks for the 09:30–09:50 Eastern window on 2026-10-23. Use the same local host/port as step 2. Inspect Alpaca status/error, bar count, first/latest source timestamps, feed, duplicates and observed gaps. If Massive is not configured, its provider error is expected; report it separately.
5. Separately run dotnet test MomentumCommandCenterV2.sln --configuration Release from the repository root. Passing tests validate local code/fixtures; they do not prove live authentication or data entitlement.
6. Report only sanitized output: HTTP status, provider status/error, feed name, returned bar count, source timestamps, duplicate count and gap count. Remove keys, secrets, authorization headers and tokens.

Keep the feed set to iex unless the Alpaca account explicitly entitles a different feed. No paid subscription and no Massive account are required for this first Alpaca-only smoke test. A successful request still does not establish that the account/feed is suitable for live trading or that extended-hours coverage matches Thinkorswim.


## Optional historical collector

The hosted collector is disabled by default. To enable a narrow test universe using .NET User Secrets from the API project directory:

```powershell
dotnet user-secrets set "MarketData:Collector:Symbols" "SPY,QQQ,TTE,FMS"
dotnet user-secrets set "MarketData:Collector:IntervalSeconds" "60"
dotnet user-secrets set "MarketData:Collector:LookbackMinutes" "20"
```

Restart the API and open `/api/market-data/collector/status`. It should report enabled=true and configured symbols. It polls overlapping 1m/5m history and persists idempotently. Clear the Symbols secret to disable. This is not a market-wide scanner or a websocket feed. Do not store provider credentials in tracked files.


## Bounded scanner and CSV parity import (2026-10-27)

`GET /api/scanner/universe` scans only the symbols explicitly supplied in the query string (maximum 50 symbols, 7-day window). It ranks by absolute observed price change and reports bar count, timestamp range, age of latest bar, observed high-low range and summed volume. It is not market-wide movers discovery and does not provide a recommendation or probability. Do not use it as a replacement for validated 5M permission/1M execution gates.

`POST /api/market-data/import-csv` accepts a standard Alpaca/IEX OHLCV CSV with timestamp/time, open, high, low, close and volume columns. Include form fields `symbol` and `timeframe` when those columns are absent. It outputs the 17-column parity schema, placing imported data in Payload* only; Chart* stays blank until a separate chart source is supplied.
