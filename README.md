# Momentum Command Center V3

**Repository:** `MomentumCommandCenterV2`  
**Runtime:** .NET 8 / ASP.NET Core  
**Purpose:** Market research and trading decision support. **Not an auto-trader.**  
**Current development branch:** `feature/opportunity-quality-classification`

The repository and C# namespaces retain the V2 name for continuity; the broader project is Momentum Command Center V3.

## Timezone

Use `America/Phoenix` for Arizona-local display and input. Arizona does not observe daylight saving time; Phoenix remains UTC−07:00 year-round. Persist and exchange timestamps as UTC/ISO-8601.

## Decision architecture

`5M Direction / Permission → 1M Execution Confirmation → Position Lifecycle → Runner / Weakening / Exit`

The 5M engine is the directional authority. The 1M engine handles execution timing and short-term confirmation. A valid `EXECUTION READY` event is not a ranking of expected opportunity quality and is not a profitability claim.

The current V2 5M permission score has six conditions:

1. 5M close above VWAP.
2. 5M close above EMA 9.
3. EMA 9 above EMA 20.
4. EMA 20 above SMA 50.
5. RSI 14 at or above 50.
6. Same-regular-session-clock-slot 5M relative volume at or above 1.50.

Five or more conditions are required for long permission. 1M entry confirmation has additional alignment, MACD, RSI, RVOL, and ATR-extension gates. See the source engine and [Opportunity Quality and Ranking](docs/Opportunity-Quality-and-Ranking.md); do not infer Thinkorswim parity from the C# implementation alone.

## Solution structure

- `src/MomentumCommandCenterV2.Core` — deterministic domain and signal/replay logic.
- `src/MomentumCommandCenterV2.Application` — application contracts and use cases.
- `src/MomentumCommandCenterV2.Infrastructure` — provider and persistence adapters.
- `src/MomentumCommandCenterV2.API` — ASP.NET Core Minimal API, hosted workers, and dashboard host.
- `src/MomentumCommandCenterV2.Cli` — local command-line workflows.
- `tests/MomentumCommandCenterV2.Tests` — xUnit tests.
- `frontend/` — editable HTML, CSS, JavaScript, and favicon. These files are exposed in Visual Studio as a `frontend` Solution Items folder.

## Market data and live assessment

The API currently includes:

- Alpaca and Massive historical OHLCV REST adapters.
- Append-only JSONL raw market-bar observations with provider, feed, timeframe, timestamp, and transport provenance.
- A configured-symbol REST collection worker with persisted watermarks and overlapping backfill.
- An optional Alpaca WebSocket minute-bar worker that handles subscription acknowledgements, reconnects, minute bars, and late `updatedBars` corrections.
- Alpaca SIP-based top-gainers/top-losers discovery ingestion. This is a bounded movers feed, not a complete market-wide scanner.
- A live symbol assessment that calculates technical indicators and calls the existing V2 signal engine.

The live-assessment source policy is hybrid:

- 1M uses persisted WebSocket observations only when subscription is confirmed, sufficient completed-bar history exists (at least 50 bars), the latest bar is fresh, and the latest observation is explicitly tagged WebSocket.
- Otherwise 1M falls back to historical REST.
- 5M Permission remains REST-backed.

A connected WebSocket is not proof of fresh market data. Inspect `/api/market-data/stream/status` for acknowledged symbols, per-symbol bar counts, latest bar timestamps, received/persisted totals, and errors. A stream message timestamp is not a bar timestamp. Provider feed entitlement controls coverage; missing bars are not zero-volume bars.

Useful endpoints include:

- `GET /api/providers/status`
- `GET /api/market-data/bars`
- `GET /api/market-data/stored-bars`
- `GET /api/market-data/live-assessment`
- `GET /api/market-data/stream/status`
- `POST /api/market-data/stream/subscribe`
- `GET /api/market-data/replay`
- `GET /api/scanner/discoveries`
- `GET /api/morning-brief`
- `GET /api/morning-brief/history`

## Morning Brief snapshot history

Each newly generated `GET /api/morning-brief` report is persisted as an immutable, versioned JSONL snapshot. The live response includes persistence status and snapshot ID/version. Query `GET /api/morning-brief/history?limit=20` to retrieve prior reports. Configure the server-side path with `MCC_MORNING_BRIEF_SNAPSHOT_PATH`; otherwise the API uses the local application-data directory. Snapshot persistence preserves partial/unknown report fields and does not imply market-data validation. Per-source input watermarks and outcome linkage remain future work.

## Historical replay and its limits

The API replay endpoint uses persisted completed 1M/5M bars at or before an explicit as-of timestamp and invokes the current V2 engine/configuration. It prevents future-bar lookahead. Historical V2 configuration versions are not yet archived, and this is not a complete backtest: it does not reproduce installed Thinkorswim study outputs, simulate realistic fills/slippage, or establish historical profitability.

The CLI also supports local CSV replay:

```bash
dotnet run --project src/MomentumCommandCenterV2.Cli -- replay samples/market-data/sample-bars.csv
```

## Dashboard and frontend architecture

The dashboard is a static HTML/CSS/JavaScript frontend hosted by the ASP.NET Core Minimal API through its physical-file provider. It is **not an MVC/Razor application**.

Edit these files directly:

- `frontend/index.html` — page structure and element IDs.
- `frontend/styles.css` — layout and responsive styling.
- `frontend/app.js` — browser behavior and API requests.
- `frontend/favicon.svg` — browser icon.

Open `MomentumCommandCenterV2.sln` in Visual Studio and expand the `frontend` Solution Items folder. Run the API project and use its hosted URL; opening the page with `file://` is offline/demo-only and cannot call same-origin API routes.

## Current product boundaries

The current dashboard, API, scanner, and Morning Brief are an evolving research workbench, not a production trading platform.

Not yet established end-to-end:

- Installed Thinkorswim 5M/1M study parity and matched-bar state-transition validation.
- Full-universe discovery and entitlement/licensing suitability.
- Complete-market breadth, verified sector flows, classified catalysts, halt/spread/order-book evidence, or outcome-calibrated opportunity probabilities.
- Fully automated Boring Overnight calculations and qualification from validated, session-paired persisted history.
- Versioned historical decision configuration and realistic fill/outcome backtesting.
- Schwab OAuth and read-only broker-position synchronization.
- Order placement or autonomous trading. Order submission remains intentionally unimplemented.

Unknown, missing, or stale evidence must remain UNKNOWN / DATA? and must never become READY by assumption.

## Build and test

Use Visual Studio 2022 and the .NET 8 SDK:

```bash
dotnet build MomentumCommandCenterV2.sln --configuration Release
dotnet test MomentumCommandCenterV2.sln --configuration Release
```

GitHub Actions run **779** passed for the Morning Brief snapshot implementation commit `844581de9c134c82bb898bdf4a9d3d3db1ce476e`: .NET 8 Release build succeeded with 0 warnings/0 errors and **156/156 tests passed**. Runtime-validation script run **780** and documentation runs **781–785** also passed. Later documentation/user-manual updates still require checking the exact latest branch-head workflow. Browser acceptance and live in-session provider acceptance remain separate requirements.

## Project control

- Canonical checklist: [docs/Master-To-Do.md](docs/Master-To-Do.md)
- Current continuation handoff: [docs/NewChatHandoff.md](docs/NewChatHandoff.md)
- Market-data runtime evidence: [docs/MarketData-ChatTests-20261030.md](docs/MarketData-ChatTests-20261030.md)
- Runtime acceptance runbook: [docs/Runtime-Acceptance-Runbook.md](docs/Runtime-Acceptance-Runbook.md)
- User guide: [docs/User-Manual.md](docs/User-Manual.md)

GitHub source is authoritative for code and committed documentation. A checked-off implementation task requires source evidence; CI, browser acceptance, provider runtime validation, and external approvals must be tracked separately.
