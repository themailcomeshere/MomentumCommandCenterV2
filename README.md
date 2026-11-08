# Momentum Command Center V3

## Current project status — 2026-11-07

- Working branch: `feature/opportunity-quality-classification`. The main Dashboard is `frontend/index.html`, served at `http://localhost:53360/`. There is no PrototypeIndex acceptance target.
- The operator's local Release build/test PASS was at source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`. The later ATR session-gap detector and regression tests passed GitHub Actions on code/test commit `151cb97976121600833c8927bddf4cf175567b7b` (Release build, 0 warnings/errors, 236/236 tests). Subsequent commits include documentation and PowerShell evidence-script updates; the newest branch-head workflow is queued. The new PowerShell scripts have not yet been executed on the operator's Windows machine.
- Alpaca's current usable feed is IEX. Recent SIP requests have repeatedly returned HTTP 403 for this account. Do not retry SIP, switch configuration, or ask the operator to repeat SIP requests. SIP-dependent consolidated qualification and GTC_EXT work remain parked unless the operator reports a genuine entitlement change.
- The visible **Daily data audit** panel was operator-verified for RTO, EVR, and CBOE: 3 symbols returned, 0 API errors, 0 qualification-usable, all diagnostic-only / `DATA? · IEX_PARTIAL`, with 20/20 sessions, zero missing, and FRESH. RTO/EVR independent OHLCV parity previously failed on IEX; CBOE independent Daily reference data is absent.
- Existing EVR/CBOE StrategyReport CSVs and persisted observations are already committed. Latest operator recheck: EVR 5m 327 observations / 20 compared / 2 gaps; CBOE 5m 338 / 20 / 1 gap. User reports chart bars beginning at 05:21 (EVR) and 06:15 (CBOE), but AM/PM/date/timezone are not yet anchored. See [Nov 7 ATR/catalyst evidence](docs/Validation/ATR-and-Catalyst-Operator-Evidence-20261107.md). Do not ask for or re-import the same exports. Latest 5m ATR values are within numeric tolerance and Wilder recurrence is rounding-consistent, but EVR/CBOE parity remains `GAPS_PRESENT` because timestamp continuity is unresolved. Only a demonstrably new corrected export after compiling the revised ThinkScript could add new evidence.
- A session-boundary gap-detector fix is committed: same-date 00:00–00:10 ET intervals wholly inside the 20:00–04:00 ET closure are excluded, while premarket-overlapping and after-hours gaps remain countable. Three regression tests passed; GitHub Actions build passed with 0 warnings/errors and 236/236 tests on `151cb97976121600833c8927bddf4cf175567b7b`. Local API parity re-read remains pending; no repeat export/import is needed.
- Catalyst human-review validation endpoint is implemented, but the operator's latest local response has zero saved reviews and therefore no holdout metrics. Real positive and negative human adjudications remain required. The 20-training/20-unseen-test walk-forward validator is implemented; runtime qualification against 40 complete eligible sessions is not captured. It is blocked by source-qualified GTC_EXT evidence and the known SIP restriction.
- New PowerShell evidence scripts: `scripts/validation/Capture-DashboardBrowserDiagnostics.ps1` (CDP console/network/screenshot) and `scripts/validation/Test-RegularSessionStreamCollector.ps1` (regular-session WebSocket/collector polling). See [Browser and Stream Collector Acceptance](docs/Validation/Browser-and-Stream-Collector-Acceptance-20261106.md).
- Canonical current checklist: [Master To-Do](docs/Master-To-Do.md). Current continuation: [New Chat Handoff](docs/NewChatHandoff-20261107.md). Detailed backlog: [Active Engineering Backlog](docs/Active-Engineering-Backlog-20261106.md).

**Safety boundary:** MCC is decision-support-only. Order submission is NOT IMPLEMENTED. Missing, stale, incomplete, or unverified inputs remain UNKNOWN / DATA? and never become READY by assumption.

---

## Historical branch-head verification — 2026-11-04 (superseded)

The latest feature-branch head for the standalone UI changes is `3c27c787f7ffcb434be9893ca73ba58b8cad00fe`; exact-head GitHub Actions run [41916672903](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41916672903) passed. The workflow runs `node --check frontend/app.js`, .NET 8 restore, Release build, and the full .NET test suite. This is a source/CI result only; local browser acceptance remains open. System Readiness is collapsed by default, symbol refresh state is explicit, missing custom-expression metrics fail closed to DATA?, and GTC_EXT capture is exposed behind an effective-SIP preflight.

The local exclusion API returned 56 entries in the user's test. The feature-branch persisted file contains 55 canonical exclusions plus TEST; OSW, EVR and MHK are not in the canonical list. The user's symbol-filter result is consistent with the seeded list.

# November 3, 2026 — current feature-branch acceptance update

- Latest feature-branch source commit at this update: `9777b774de414c4af39ad7ee82b18990b7924972`. Exact-head GitHub Actions run 41702560827 is running; do not call this commit CI-verified until it completes.
- Scanner CSV import acceptance is based on the current export, not a fixed result count: user reports 198 imported and 198 scanner rows rendered; historical gates 187 PASS / 11 FAIL / 0 UNKNOWN; 0 exclusion matches; 0 final READY.
- Red Zone reported 0/4 despite historical PASS rows. The import code had selected the Imported CSV option before inserting it into the select element; this is now corrected so the option is inserted before setting the selected value. Browser verification remains required.
- Qualification panel now exposes Unknown historical metrics as a separate summary metric. Sticky table headers are enabled in table wrappers. The API-backed exclusions list has a bounded 460px scroll viewport.
- Runtime test result: RKLX stored bars 1M=40 / 5M=31; idempotency True/True. Stream test: SPY advanced 5 bars; RKLX/ASTX/XRPN did not. See `docs/Runtime-Acceptance-Runbook.md` §1C; do not confuse subscription acknowledgement with delivery.
- The persisted exclusion list count 56 is expected from 55 canonical exclusions plus TEST. OSW/EVR/MHK are not in the canonical source list.
- The 2026-11-03 12:00 PM Arizona RTO screenshot is daytime 1M/5M evidence, not the 3:55 PM EOD cutoff test. It shows 5M Permission NO / neutral structure and 1M WAIT; repeated PREPARE TO SELL labels are not GET OUT.
- The prior 248-row count is withdrawn as an acceptance target. Validate that every row in the current export is imported and rendered.

See `docs/NewChatHandoff-20261106.md` for the exact operator sequence and `docs/Master-To-Do.md` for current open items.

---

## 2026-11-03 — Scanner CSV import retry

The synced export is now present at `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`. It contains two metadata preamble lines (`Watchlist Scanner`, `Results`) followed by the actual comma-delimited header on line 3. The importer previously assumed line 1 was the header, which caused the reported missing Symbol/Ticker error.

`frontend/app.js` now searches the first 25 nonblank rows for a normalized Symbol/Ticker header, then imports the data rows after it. The uploaded file was parsed against the same delimiter/header rules: comma delimiter, header row 3, **50 nonblank ticker rows**, first three MHK/OSW/EVR with Overnight Scores 73.42/70.97/68.25. Browser interaction has not yet been exercised, so final UI acceptance remains pending. Do not re-upload or edit the CSV to remove its preamble.

Current importer fix commit: `b05c63713452cbbbb5632a87c639f655ad027e73`. GitHub Actions run 41549757022 is still running at the time this note was added.

## November 2 follow-up — current operator and strategy notes

- Fixed the update script's PowerShell branch refspec interpolation; the branch variable is now braced before the colon.
- Hardened the prototype scanner CSV importer for UTF-8 BOM, comma/semicolon/tab delimiters, quoted headers, and Symbol/Ticker variants. The actual Overnight Scanner export was not present in the active branch tree during this review, so its exact header format remains unverified.
- Prototype clock now sits to the right of the title within the title's parent flex row; transport status text no longer inherits the yellow status-dot background.
- Current Library 1M baseline is V14.5 with normal 5M authority V4.1.7. A source audit found the 1M reconstructed 5M Permission omits the 5M Daily VWAP condition; see [1M V14.5 parity audit](docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md). No uncompiled study replacement is promoted.
- November 2 overnight chart review: MHK, OSW, and EVR are not BUY/HOLD candidates on the supplied chart evidence. The supplied charts are 1D/5m (not 1m): EVR and OSW show visible EOD GET OUT bubbles on their 5m charts, but the generating study/source has not been isolated; their top 5M labels remain WAIT/Permission NO/BEARISH; MHK shows 5M WAIT/Permission NO/neutral structure and no visible EOD GET OUT. No 1M chart was supplied; MHK's current ON move is 3.13%. No numeric Calculated Limit Sell is supported by the currently available GTC_EXT/ATR evidence.

- A second 1M/5M parity defect is documented: V14.5.1 uses 15:55 Eastern for a 3:55 PM Arizona cutoff; V4.1.7 uses 19:55 Eastern during standard time, which is also wrong. Correct mapping is 18:55 EDT / 17:55 EST. Both studies need matched-bar DST validation before trusting EOD labels.

- Library source clarification: the latest full 1M study is `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (V14.5.1). The later-modified V14.4.10 artifact is explicitly aggressive-lane audit-only, not the normal full study. V14.5.1 retains the VWAP and EOD clock parity defects documented above.
- GitHub Actions run [41546963701](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41546963701) passed the .NET build/test on exact commit `825276a844a5b3079aa3ea4bd1cb8ccbd90e283b`. Local execution of `Get-Latest-And-Test.ps1` and browser acceptance remain pending.

---

- November 2 after-hours stream check: API health OK and WebSocket SUBSCRIBED with SPY/RKLX/ASTX/XRPN acknowledged. No bars/counters advanced because the user ran it after 3 PM Arizona; regular-session delivery remains unverified. See `docs/Runtime-Acceptance-Runbook.md` §1A.
## November 2, 2026 — Technical workflow and Overnight Scanner

- Operator workflow: [Technical Workflow](docs/Technical-Workflow.md). This is the normal get-latest/build/restart/API smoke-test sequence.
- Detailed acceptance source of truth: [Runtime Acceptance Runbook](docs/Runtime-Acceptance-Runbook.md). Run specialized tests only when the relevant feature changes or needs certification; do not run the entire runbook after every commit.
- Update/test script: `scripts/Get-Latest-And-Test.ps1` (clean-tree guard, fetch/switch/pull, Release restore/build/test, exact SHA).
- Latest source/script head `237fcbfc71f9b03bda64b1e6047d61355c849447` passed Release build and 179/179 tests with 0 warnings/0 errors in [Actions run 41540235754](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41540235754).
- Overnight process: [Overnight Scanner Quick Start](docs/Overnight-Scanner-Quick-Start.md).
- November 2 shortlist and catalyst review: [Overnight Scanner Review](docs/Validation/Overnight-Scanner-Review-20261102.md).
- Red Zone now re-renders immediately after scanner CSV import. Live 1M/5M card assessments refresh every 15 seconds; Alpaca live discoveries refresh every 30 seconds only when that group is selected.
- Thinkorswim Overnight scan is not automatically connected to MCC; CSV export/import is the current handoff. Alpaca top-gainers/losers discovery is a separate SIP-based source and the user's current subscription denied recent SIP requests.
- Prototype header repaired: System Readiness is a sibling of topbar-brand and marketTransportLabel wraps at word boundaries.
- Active checklist: [Master To-Do](docs/Master-To-Do.md) · [Handoff](docs/NewChatHandoff-20261106.md) · [Active Work Checklist](docs/Active-Work-Checklist-20261102.md).

---
## November 2 follow-up — current feed, timestamp, and broker integration status

- Alpaca IEX override was `secrets.json` taking precedence over `appsettings.json`. Setting secrets.json to SIP selected SIP, but the recent INO request returned HTTP 403: the current subscription does not permit recent SIP data. Restoring IEX returned `VALIDATED_DATA_RETURNED` with 62 bars. Use IEX until SIP access is actually authorized.
- The prototype Main Chart layout was visually accepted by the user with no horizontal overflow.
- Corrected the 1M timestamp display: `sourceTimestampUtc` is the bar start and `completedAtUtc` is the completed candle close. The UI now shows the close time and interval; the in-progress minute remains excluded. This display fix does not establish actual provider latency.
- Latest INO comparison: TOS Close 2.06 / VWAP 1.89 / ATR14 0.02; MCC Close 2.05500 / VWAP 1.87031 / ATR14 0.01429. The TOS 24.6 ATR value was a live/latest label, not a historical bar value. Matched-bar parity remains open.
- User reports Schwab Trader API — Individual approval. App creation, Accounts and Trading Production attachment, callback URL, app-level Ready status, OAuth and read-only position sync remain to be done. No order-writing integration is in scope.
- Release build and 179/179 tests passed on head `83cd9780daa9f8e94628e8d24f4bc6ab4b03c415` in [Actions run 41514111035](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41514111035). Subsequent documentation-only commits are included in the next branch-head workflow.
- The RTO audit is exact 40-boundary coverage across 20 valid sessions. Prior 38/40 result was IEX, with missing 2026-10-14 13:30Z and 2026-10-21 13:30Z opens. SIP comparison is blocked by the explicit Alpaca 403.
- Current work tracking: [Active Work Checklist](docs/Active-Work-Checklist-20261102.md), [Master To-Do](docs/Master-To-Do.md), [New Chat Handoff](docs/NewChatHandoff-20261106.md).
- Timestamp diagnosis: [Live 1-Minute Bar Timestamp and Latency](docs/Validation/Live-Bar-Timestamp-Latency-20261102.md). Schwab plan: [Thinkorswim / Schwab Integration Plan](docs/Thinkorswim-Schwab-Integration-Plan.md).

---
## November 2 — live IEX diagnosis and Main Chart layout

- User's tracked API appsettings contains `Feed: sip`, but running API still reported IEX. Launch settings only sets Development. New `/api/providers/status` output identifies the effective configuration provider and all sources contributing the feed setting; inspect User Secrets and environment overrides before further guessing.
- REST, WebSocket, and live assessment now default to SIP if no feed setting exists. An explicit IEX override can still win by configuration precedence.
- Prototype Main Chart layout: ticker / title / Live Opportunity Classification header; chart ~2/3 width with 2x3 metric grid to the right. Exact bar timestamp shown in Arizona time and UTC; close, VWAP, and ATR14 display five decimals.
- TOS study and comparison steps: [Thinkorswim / MCC Extension Parity](docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).
- User's `RTO-SIP-20261101.json` still records `feed=iex`, 38/40 exact boundaries and two missing opens. It is not SIP validation.
- The updated code source head `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae` passed Release build and **179/179 tests** in Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777). The docs-updated head `4443506be982d0ff6726992c7b2294d97871bad2` also passed run [41504521572](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504521572), 179/179.
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

## Product overview

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
- Current continuation handoff: [docs/NewChatHandoff-20261106.md](docs/NewChatHandoff-20261106.md)
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

## Dashboard entry point — main index only

- `/` serves `frontend/index.html`. This is the only Dashboard page for implementation, testing, and browser acceptance.
- `frontend/prototypeIndex.html` was removed after the approved layout was migrated into the main index. Do not recreate, use, or ask the operator to test a PrototypeIndex page.
- Keep all future Dashboard changes on `frontend/index.html`, `frontend/app.js`, and `frontend/styles.css` as appropriate.


## 2026-11-04 — MCC-native workflow updates

The main Dashboard provides button-driven 20-session regular close/open retry and SIP-gated 20-session GTC_EXT sell-window capture. The latter checks the effective Alpaca feed and does not request qualification history when IEX is configured. Missing custom-expression metrics are classified DATA?, not FAIL. System Readiness is collapsible, and Decision Explanation shows the symbol being refreshed. See [Technical Workflow](docs/Technical-Workflow.md), [Overnight Scanner to Boring Overnight Workflow](docs/Overnight-Scanner-to-Boring-Overnight-Workflow.md), and [Master To-Do](docs/Master-To-Do.md) for operating instructions and remaining validation gates.


## 2026-11-04 — candidate-group and header workflow

The Overnight qualification panel supports separate **MCC Top 4** (four highest imported scanner scores) and **My Top 4** (exactly four user-selected imported symbols) groups. Selection persists in the browser; Daily + catalyst refresh queries only the active four. This is scanner-score selection, not a calibrated Opportunity Quality rank. Morning Command Brief is adjacent to the product title; WebSocket status is within System Readiness. The approved Quick Start and workspace navigation are part of the main Dashboard; no separate prototype page is used.


The qualification panel also exposes an append-only human catalyst review queue for articles returned by the active group. Human labels are stored separately from automated triage and cannot clear a catalyst; labelled-sample adjudication and measured classifier validation remain open.


 
## SIP entitlement and sell-limit status — November 4, 2026

Repeated recent Alpaca SIP requests returned HTTP 403 because the account subscription does not permit recent SIP data. This is a known external blocker. Do not repeat SIP requests or ask the operator to reconfigure/test SIP. IEX remains partial-exchange diagnostic coverage; SIP-dependent qualification, live movers, and GTC_EXT target research remain parked unless the operator independently reports a genuine entitlement change.

The GTC_EXT exact-window capture and the fail-closed sell-limit calculator exist, but a provenance-bound end-to-end calculation and validated walk-forward result are still outstanding. Wilder ATR code exists; Thinkorswim numeric parity is not established. Numeric sell limits remain withheld until exact SIP coverage, matched ATR evidence and walk-forward validation are complete.


## November 7 operator evidence update

The current feature-branch head `b212caf0c2d1df0393fdcc05b5f872c0966aeb67` passed GitHub Actions [run 42340443270](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42340443270). This is repository CI only, not local runtime or Thinkorswim acceptance.

Latest local ATR recheck remains `GAPS_PRESENT`: EVR 5m has 2 timestamp gaps; CBOE 5m has 1. The chart-visible bar-start observations reported by the operator still need exact date, AM/PM and timezone alignment before changing the detector or requesting a new export. Catalyst validation currently has zero human review records and no measurable holdout metrics. Do not invent labels, synthesize bars, relax tolerance, or promote unknown catalyst state to CLEAR.

Current operator steps: [New Chat Handoff 2026-11-07](docs/NewChatHandoff-20261107.md). Full recorded evidence: [ATR and Catalyst Operator Evidence 2026-11-07](docs/Validation/ATR-and-Catalyst-Operator-Evidence-20261107.md).


## Latest operator update — November 8 UTC / November 7 Arizona

The operator verified local source SHA `bb6a04aedc6f3cbfda1250a7cc9d8d676594419e` and API health OK. New CBOE/EVR inspect imports were idempotent, but the exports did not cover the requested chart intervals: EVR began at 09:30 ET (after the requested 04:00–09:25 windows) and CBOE ended at 15:55 ET (before the requested 16:05–18:35 window). Import gapCount is an interior-gap count, not proof of full-session coverage.

New code reports leading/trailing scheduled 04:00–20:00 ET session coverage per Eastern date, adds regression tests, and exposes catalyst human-review exclusion reasons in the API and Dashboard. Three catalyst reviews are now saved, but only one is eligible and two are excluded; the exact exclusion reasons are not known until the new build's validation response is queried. Latest implementation commits are on `feature/opportunity-quality-classification`; see [Master To-Do](docs/Master-To-Do.md), [current handoff](docs/NewChatHandoff-20261107.md), and [dated evidence](docs/Validation/ATR-and-Catalyst-Operator-Evidence-20261107.md).


## November 8, 2026 — ATR/catalyst validation status

The ATR import endpoint now returns the parser's leading/trailing scheduled 04:00–20:00 ET session coverage diagnostics as `sessionCoverage`; a regression test covers partial-session exports. This fixes an API response omission, not the underlying incomplete source. The committed EVR and CBOE Nov 4 Strategy Reports contain only 82 ATR payload rows each and are incomplete across the extended-hours window, with additional interior timestamp gaps. Embedded V2.1 epochs are the bar timestamps; the Strategy Report Date/Time field is an order-event timestamp.

Catalyst review validation currently has three saved records: two UNCERTAIN records (ENOV and ARDT) correctly excluded, and one EWTX MATERIAL_RISK review against an UNCLASSIFIED saved prediction (a false negative). One eligible observation cannot support a chronological holdout or classifier promotion. Keep unknown/uncertain states fail-closed; see the dated operator evidence and Master To-Do for details.
