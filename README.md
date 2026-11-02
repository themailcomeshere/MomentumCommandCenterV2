## November 2 — live IEX diagnosis and Main Chart layout

- User's tracked API appsettings contains `Feed: sip`, but running API still reported IEX. Launch settings only sets Development. New `/api/providers/status` output identifies the effective configuration provider and all sources contributing the feed setting; inspect User Secrets and environment overrides before further guessing.
- REST, WebSocket, and live assessment now default to SIP if no feed setting exists. An explicit IEX override can still win by configuration precedence.
- Prototype Main Chart layout: ticker / title / Live Opportunity Classification header; chart ~2/3 width with 2x3 metric grid to the right. Exact bar timestamp shown in Arizona time and UTC; close, VWAP, and ATR14 display five decimals.
- TOS study and comparison steps: [Thinkorswim / MCC Extension Parity](docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).
- User's `RTO-SIP-20261101.json` still records `feed=iex`, 38/40 exact boundaries and two missing opens. It is not SIP validation.
- The updated code source head `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae` passed Release build and **179/179 tests** in Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777). Later commits are documentation-only; final docs-head CI is being checked.
## November 2 follow-up — feed provenance and Main Chart layout

- Reviewed the user's two latest commits. `RTO-SIP-20261101.json` still reports `feed=iex`, 38/40 exact boundary bars, and missing October 14 / October 21 opening bars; the filename is not evidence of SIP.
- API provider status now exposes the Alpaca adapter's effective feed and the configuration providers supplying the feed setting. REST, WebSocket, and live assessment now default to SIP rather than silently choosing IEX when the setting is absent.
- Main Chart header now separates ticker, chart title, and Live Opportunity Classification. The chart is approximately two-thirds width; the right side is a 2x3 metric grid (five metrics plus a TBD placeholder). Responsive breakpoints prevent horizontal overflow.
- Close, Daily VWAP, and ATR14 diagnostics now display five decimal places for low-priced stocks.
- Thinkorswim parity instructions and study: [Thinkorswim / MCC Extension Parity](docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).
- Runtime feed source, actual SIP delivery, Thinkorswim parity, and browser acceptance remain pending.
## November 2 validation notes

- The July 3, 2028 early-close test remains enabled and uses the xUnit predicate overload for `Assert.Single`.
- The user's first Release test run reported 172/176. The supplied-extension and three TradeAnalyzer fixture failures were corrected; GitHub Actions run [41491616284](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41491616284) then passed the Release build and **178/178 tests** on source commit `c0412da1a5e4c40f8853bd07d5791277fc3c7f22`.
- The prototype dock now has **INTRADAY MOMENTUM** left of the toggle, **BORING OVERNIGHT** right of it, and **WORKSPACE** below it.
- Prototype Main Chart and Red Zone now expose the 1M close, Daily VWAP, ATR14, and normalized ATR Extension inputs. The `11.20 ATR` display is the normalized extension, not raw ATR14.
- API feed diagnostics now distinguish configured feed from observed 1M/5M feed. Committed API default is SIP; entitlement and runtime SIP delivery remain unverified.
- Thinkorswim numeric parity requires a matched symbol and completed 1M timestamp. The earlier `24.5 ATR` vs `11.20 ATR` comparison remains unresolved.
- RTO follow-up evidence remains IEX, not SIP; 38/40 exact boundaries, with October 14 and October 21 open bars missing. Repeat only after observed SIP feed is confirmed.
- Detailed record: [Live Extension and Feed Diagnostics](docs/Validation/Live-Extension-and-Feed-Diagnostics-20261102.md).

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

Five or more conditions are required for long permission. The live API exposes this six-condition 5M score independently of 1M execution availability, but only when the 5M history, freshness, and same-slot RVOL baseline are valid. A score of 5–6 means 5M Permission only; 1M entry confirmation has additional alignment, MACD, RSI, RVOL, and ATR-extension gates, and the combined decision must separately report `entryAllowed=true` before the UI labels EXECUTION READY. See the source engine and [Opportunity Quality and Ranking](docs/Opportunity-Quality-and-Ranking.md); do not infer Thinkorswim parity from the C# implementation alone.

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

Each newly generated `GET /api/morning-brief` report is persisted as an immutable, versioned JSONL snapshot. The live response includes persistence status and snapshot ID/version. Query `GET /api/morning-brief/history?limit=20` to retrieve prior reports. Configure the server-side path with `MCC_MORNING_BRIEF_SNAPSHOT_PATH`; otherwise the API uses the local application-data directory. Snapshot persistence preserves partial/unknown report fields and does not imply market-data validation. Explicit UTC bar watermarks from the market-landscape payload are included; raw input references, news/discovery watermarks, full calculation dependency manifests, and outcome linkage remain future work.

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

GitHub Actions run **792** passed for the Morning Brief snapshot and source-watermark implementation commit `61e8483c6cce4cea5e5131e773086db1ff91bd74`: .NET 8 Release build succeeded with 0 warnings/0 errors and **156/156 tests passed**. The runtime-validation script passed workflow **780**; source-watermark test commit passed workflow **793**. Later documentation updates still require checking the exact latest branch-head workflow. Browser acceptance and live in-session provider acceptance remain separate requirements.

## Project control

- Canonical checklist: [docs/Master-To-Do.md](docs/Master-To-Do.md)
- Current continuation handoff: [docs/NewChatHandoff.md](docs/NewChatHandoff.md)
- Market-data runtime evidence: [docs/MarketData-ChatTests-20261030.md](docs/MarketData-ChatTests-20261030.md)
- Runtime acceptance runbook: [docs/Runtime-Acceptance-Runbook.md](docs/Runtime-Acceptance-Runbook.md)
- User guide: [docs/User-Manual.md](docs/User-Manual.md)

GitHub source is authoritative for code and committed documentation. A checked-off implementation task requires source evidence; CI, browser acceptance, provider runtime validation, and external approvals must be tracked separately.


## Scanner, Red Zone, and Watchlist workflow

1. **Refresh Alpaca market movers** loads persisted Alpaca SIP-based discovery events from the API. It does not itself run a full-market scan; discovery rows are candidates, not qualified opportunities.
2. With the live discovery group selected, returned rows populate the scanner and the Red Zone preview (up to four). Current live ordering uses absolute daily percentage change; other groups use their supplied score. This is a provisional ordering, not a calibrated excitement/quality rank.
3. **Watch** adds a ticker to the separate browser-local Watchlist. It does not pin the ticker to Red Zone.
4. To keep a symbol in Red Zone, use **Add a symbol → Pin to Red Zone**. Manual pins are placed before scanner-ranked rows. There is not yet a Manual/Automatic mode toggle.
5. Each Red Zone tile requests a separate live assessment and displays 5M score/permission, 1M execution state, extension, freshness, decision reason and a 1M sparkline. New arrivals among the four slots can trigger the existing user-toggleable sound alert.
6. Red Zone cards now show the available 5M same-time-of-day RVOL and flag values below the existing V2 threshold, including a Decision Explanation caution. This is a diagnostic display only. Current ranking still does not incorporate RVOL, average-volume/liquidity, acceleration or spread quality, so a low-participation candidate may still rank too highly.
7. Planned Red Zone work is tracked in the top of `docs/Master-To-Do.md`: Manual/Automatic toggle, versioned excitement rank, participation/volume weighting, Decision Explanation, semantic tile border/accent, stable re-ranking and tests.
8. Main Chart / Decision Explanation refresh for the focused symbol. 5M Permission, 1M EXECUTION READY and Red Zone excitement remain separate. Red Zone is attention prioritization only and never authorizes an entry or order.

## Prototype header layout

The prototype's `#system-readiness` section is nested inside the same `.topbar-brand` flex container as the title and topbar tools, aligned on the right. The market-session clock appears above the readiness heading. Production markup remains separate and is not changed by prototype experiments.

## Production and prototype dashboard pages

- Production: `/` serves `frontend/index.html`.
- Prototype: `/prototypeIndex.html` serves `frontend/prototypeIndex.html`.
- Apply UI experiments to `prototypeIndex.html` only. Promote a design to `index.html` only after explicit user approval.
