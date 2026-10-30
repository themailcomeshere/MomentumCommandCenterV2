# MCC Collector, Morning Brief, and Browser Validation Runbook — 2026-10-29

This runbook is for the remaining local-runtime tests. Use temporary validation files first. Do not overwrite the normal market-bar store or collector checkpoint during fault tests.

## 1. Pull and configure the local API

From the repository root in PowerShell:

```powershell
git checkout feature/opportunity-quality-classification
git pull
dotnet test .\MomentumCommandCenterV2.sln
```

Set Alpaca credentials in .NET User Secrets for the API project. Run these locally and never paste the values into chat or commit them:

```powershell
dotnet user-secrets set "MarketData:Alpaca:ApiKey" "<YOUR_ALPACA_KEY>" --project .\src\MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Alpaca:ApiSecret" "<YOUR_ALPACA_SECRET>" --project .\src\MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Collector:Symbols" "SPY,QQQ,IWM,RTO,AMT" --project .\src\MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Collector:IntervalSeconds" "60" --project .\src\MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Collector:LookbackMinutes" "20" --project .\src\MomentumCommandCenterV2.API
dotnet user-secrets set "MarketData:Collector:OverlapMinutes" "5" --project .\src\MomentumCommandCenterV2.API
```

For an isolated recovery test, configure temporary store/checkpoint paths in the same PowerShell window:

```powershell
New-Item -ItemType Directory -Force .\artifacts\collector-validation | Out-Null
$env:MCC_MARKET_BAR_STORE_PATH = "$PWD\artifacts\collector-validation\market-bars.jsonl"
$env:MCC_MARKET_COLLECTOR_CHECKPOINT_PATH = "$PWD\artifacts\collector-validation\collector-watermarks.json"
$env:MCC_SCANNER_DISCOVERY_PATH = "$PWD\artifacts\collector-validation\scanner-discoveries.jsonl"
```

Start the API:

```powershell
dotnet run --project .\src\MomentumCommandCenterV2.API
```

Use the HTTPS base URL printed by ASP.NET Core. Earlier local tests used `https://localhost:53359`; use the URL printed by your current run if the port differs.

## 2. Collector baseline and timeframe identity

1. Open `/api/market-data/collector/status`.
2. Confirm `enabled=true`, Alpaca appears under configured providers, and the configured symbols are exactly the temporary test universe.
3. Wait for at least one cycle. Confirm `cycles` increments and `lastCycleCompletedUtc` is populated.
4. Inspect the temporary JSONL file. New observations must include `bar.timeframe` equal to `1m` or `5m`; the checkpoint file must have separate keys ending in `/1m` and `/5m`.
5. Query a timeframe explicitly:

```powershell
$base = "https://localhost:53359"
Invoke-RestMethod "$base/api/market-data/stored-bars?symbol=SPY&fromUtc=2026-10-01T13:30:00Z&toUtc=2026-10-29T20:00:00Z&provider=Alpaca&timeframe=1m&limit=10000"
Invoke-RestMethod "$base/api/market-data/stored-bars?symbol=SPY&fromUtc=2026-10-01T13:30:00Z&toUtc=2026-10-29T20:00:00Z&provider=Alpaca&timeframe=5m&limit=10000"
```

Use a window no longer than 31 days. The two result sets must not contain the other timeframe.

**Legacy migration behavior:** rows written before timeframe identity was added are preserved as `UNKNOWN`. They are intentionally excluded from `timeframe=1m` and `timeframe=5m` queries. Re-fetch the required history through `/api/market-data/bars` so correctly tagged rows are appended. Do not edit old JSONL records to guess their timeframe.

## 3. Collector restart and outage/backfill test

1. Record the current checkpoint JSON, JSONL line count, latest 1m timestamp, latest 5m timestamp, and API status.
2. Stop the API normally. Do not delete the store or checkpoint.
3. Restart the API using the exact same environment variables and paths.
4. Wait for one or two cycles. Confirm the worker restores the checkpoint and resumes with the configured overlap. Identical source observations should not create duplicate JSONL records.
5. For a controlled outage test, stop the API for at least 30 minutes while the market is open. Restart it and verify new source timestamps fill the missed interval up to the provider's available history. Compare the restored timestamps with a direct `/api/market-data/bars` request for the same symbol/timeframe/window.
6. Verify 1m and 5m watermarks advance independently. Do not compare their latest timestamps as if they should be equal.
7. Test an empty response with a quiet/post-market symbol/window. An empty response must not create synthetic bars or be interpreted as no trading. It should not advance the watermark to an invented timestamp.
8. Repeat the identical historical `/api/market-data/bars` request. `newlyPersistedObservationCount` should be zero for identical source data. If the provider returns a genuine correction, the JSONL store should append a new observation with `supersedesObservationId` pointing to the prior version.

The existing automated tests cover store restart idempotency, correction retention, timeframe separation, and legacy UNKNOWN rows. A live provider correction cannot be forced on demand; use the test fixture for deterministic correction validation.

## 4. Broad-market scanner discovery

1. Confirm the API process has the same Alpaca credentials configured.
2. Call `/api/scanner/discoveries?limit=200` or use **Refresh Alpaca market movers** in the Candidate discovery panel.
3. Confirm events identify `ALPACA_TOP_GAINERS` or `ALPACA_TOP_LOSERS`, include the source timestamp when supplied, and remain `DataUnknown` / `NOT_A_TRADE_SIGNAL`.
4. Check collector health and the response's configured flag. A 401/403 or subscription error must appear as degraded source health, not an empty successful scan.
5. The current integration is Alpaca's top-50 stock movers (SIP-based), not a complete exchange-wide symbol universe. It does not provide full-market breadth or 5M Permission/1M READY. Verify the account's screener entitlement before treating the feed as operational.

## 5. Morning Brief source checks

Call `/api/morning-brief` after the API is running with credentials.

Expected source-backed sections:
- Benchmark observations: SPY, QQQ, IWM.
- Sector ETF proxy observations: XLB, XLC, XLE, XLF, XLI, XLK, XLP, XLRE, XLU, XLV, XLY.
- Movers: persisted Alpaca top-mover discoveries and collector health.
- News headlines: up to 50 Alpaca news items with headline, symbols, author, publication/update timestamps, and URL.

Expected explicit limitations:
- Sector ETFs are proxies, not constituent-level industry breadth or money flow.
- Top gainers/losers are a mover sample, not full-market advancing/declining breadth. Full breadth remains DATA NOT FOUND IN SOURCES.
- News headlines are unclassified source material. Catalyst classification/confidence is not implemented; the catalyst decision field remains DATA NOT FOUND IN SOURCES / UNKNOWN.
- No probabilities, regime strength, or Overnight READY decision are inferred from these feeds.

If Alpaca's movers endpoint is forbidden by the account's SIP entitlement, record the exact HTTP status and provider message. Do not switch feeds or infer SIP entitlement from IEX bar success.

## 6. Browser-level regression

1. Pull the branch, restart the API, and hard-refresh the browser (Ctrl+Shift+R).
2. Confirm the readiness panel now identifies the Alpaca movers adapter and says SIP entitlement is pending.
3. In Candidate discovery, click **Refresh Alpaca market movers**. Verify the live option appears and rows show symbol, percentage move, discovery group, source timestamp, and receipt age. Confirm the status remains DISCOVERY — NOT QUALIFIED.
4. Confirm the user-supplied historical scan still works and is clearly distinct from the market-wide mover discovery endpoint.
5. Confirm Morning Command Brief displays benchmark rows, sector ETF proxy symbols, mover collector status, and timestamped news headlines. Verify missing full-market breadth and catalyst classification remain visibly unknown.
6. In Boring Overnight, run the existing Post-market control test and separately run a requested bar-coverage request. Confirm the first does not falsely populate the second.
7. Query `stored-bars` with `timeframe=1m` and `timeframe=5m`; confirm no cross-timeframe rows.
8. Exercise empty/error responses, narrow and wide browser widths, keyboard-only tab/enter navigation, and table overflow. Check browser console and Network tab for failed requests, unhandled exceptions, stale rows, or old values that remain after a failed refresh.
9. Capture screenshots of the scanner, Morning Brief, Overnight post-market test, and timeframe-filtered stored-bars response. Record API base URL, branch commit SHA, and test date.

Run the full suite again against the exact commit tested in the browser:

```powershell
dotnet test .\MomentumCommandCenterV2.sln
git rev-parse HEAD
```

## 7. Release acceptance

- [ ] Collector restart resumes from the same durable checkpoint/store paths.
- [ ] Controlled outage backfill is complete for the requested symbols/timeframes.
- [ ] Timeframe-specific stored-bar queries return only the requested timeframe.
- [ ] Alpaca scanner entitlement and freshness are confirmed; failures remain visible.
- [ ] Morning Brief sources show timestamps, provenance, and coverage; breadth/catalyst classification remain UNKNOWN where unavailable.
- [ ] Browser matrix passes without console/network errors.
- [ ] Full .NET suite passes on the exact browser-tested commit SHA.
- [ ] No trading decision or READY state is produced by scanner discovery or unclassified news.

## [Uncertainties]

- Alpaca movers is documented as SIP-based; the user's account entitlement is not visible from repository code.
- A genuine Alpaca historical correction may not occur during a short manual test; deterministic correction behavior is covered by automated store tests.
- Provider history retention and extended-hours coverage may differ by account plan/feed.
- The API port can differ from the earlier `https://localhost:53359` example.
