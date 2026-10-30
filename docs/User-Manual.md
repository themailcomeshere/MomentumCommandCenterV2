# Momentum Command Center V3 — User Manual and Morning Workflow

**Status:** Workflow specification for the current prototype; not a claim that the production features are implemented.
**Branch:** `feature/opportunity-quality-classification`
**Last reviewed:** 2026-10-30

## 1. What MCC is today

MCC is a C#/.NET 8 research and trading decision-support prototype, not an auto-trader. The current Dashboard is a static HTML/CSS/JavaScript workbench. It is opened in a browser; it is not currently a Windows `.exe` application.

Open the MCC frontend through the running .NET API host (or its configured static-file host). The page imports a local CSV, maintains browser-local watchlist/Red Zone pins, and requests live Alpaca historical 1m/5m bars for focused and pinned symbols. Main Chart, Decision Explanation, and Red Zone now consume the live assessment endpoint, which calculates technical indicators and calls the existing V2 signal engine. Watchlist and manual Red Zone pin/dismiss state remain in browser local storage; they do not sync to GitHub or another computer automatically.

**Current limits:** market data is near-live historical REST polling (15-second browser refresh with a 10-second API cache), not websocket/tick streaming; provider feed/entitlement controls coverage; market-wide scanner discovery is a separate SIP-dependent top-movers feed; Thinkorswim is not connected; broker positions/order execution are not connected; no catalyst/halt/NBBO/order-book classification is inferred; Boring Overnight is a separate historical evaluation workflow. Decision values are withheld when bar history, freshness, or the same-time-of-day 5m volume baseline is insufficient.

## 2. Intended 08:30 Arizona Morning Report

The product goal is that the user can open MCC around 08:30 Arizona time and immediately see a dated, timestamped Morning Report instead of manually assembling the day's candidates. The exact historical Morning Routine previously discussed is not present in the repository or the accessible project handoff notes. This section is therefore a proposed product contract based on documented project requirements, not a claim that every step matches the prior conversation. See the Master To-Do's Morning Routine item and the user's action-needed note.

### Report header
- Report date and generated-at timestamp.
- Arizona local time and market-session context; show the corresponding Eastern time and whether US markets are premarket or regular session.
- Data provider, source timestamps, last update, and freshness/staleness state for each feed.
- Explicit system status: live, delayed, imported, demo, unavailable, or DATA?.
- A concise overall readiness summary; never hide a missing feed behind a green status.

### Morning Report sections (target)
1. **System/data health:** market-data/scanner provider, connection state, last successful update, missing/stale symbols, timestamp/session consistency, and any known data-quality warnings.
2. **Market context:** only sourced and timestamped market/sector context that MCC can actually retrieve. If no source is connected, show DATA NOT AVAILABLE rather than narrative filler.
3. **Boring Overnight candidates:** ranked candidates using the documented Overnight Edge, Boringness, catalyst/event risk, current-day qualification, 5M structure where appropriate, and 1M timing where appropriate. Display hard-gate failures, reason codes, freshness, and READY/WATCH/BLOCKED/DATA?; zero position remains valid.
4. **Intraday scanner/watchlist:** provider-discovered candidates grouped by Momentum Hunter, Breakout Trigger, Reclaim/Continuation, and Market Movers/Discovery. Deduplicate symbols while preserving source events and first-seen timestamps. Rank with transparent factors; distinguish quality ranking from entry eligibility.
5. **Extended Momentum / Continuation Watch:** retain candidates already up substantially (including the user's observed 10–15% cases) without declaring them buyable. Show EXT ATR, extension warning/danger, source/time-stamped catalyst, volume/liquidity/spread, 5M permission, and the exact reason no fresh entry is available. Requalification must require fresh evidence and existing 5M/1M gates; do not weaken EXT thresholds by default.
6. **Top Candidates / “Red Zone” panel:** show the top 1–4 candidates at once in a ranked multi-pane layout, inspired by live sports multi-game coverage. Each pane should include symbol/rank, current price and timestamp, % change, volume/RVOL, extension, catalyst/source, 5M Permission, 1M event/READY state, data freshness, key reasons, and next invalidation/blocker. It is a monitoring display, not an order signal. If fewer than four candidates meet the display criteria, show fewer; never pad with weak or fake candidates. Initially use static/demo cards only; live updating and ranking await validated data and engine integration.
7. **Action queue:** clearly separate (a) actionable under validated gates, (b) watch/pullback, (c) extended/no entry now, (d) blocked, and (e) DATA?. No candidate may be called executable solely due to news, score, or scanner inclusion.
8. **Manual decision log:** user can record watched/ignored/traded decisions and reasons, with symbol, timestamp, source snapshot, and export. Avoid storing sensitive broker credentials or account data.
9. **End-of-session feedback:** allow the user to save dashboard observations in the local feedback notebook and export JSON for review.

## 3. How the intended daily workflow should work

### Step 1 — Load MCC
**Prototype today:** open `frontend/index.html` in a browser. No installer or executable exists in the documented implementation.
**Target:** provide a reliable desktop shortcut/packaged app or locally hosted web app only after the UI is connected to the application services. An `.exe` is a packaging choice, not a prerequisite for a useful Dashboard. Do not build a fake executable that merely wraps a disconnected demo page.

### Step 2 — Initialize and verify data
- Determine the report date and Arizona/Eastern session context.
- Connect only a selected, supported market-data/scanner provider.
- Load current and required historical OHLCV with explicit timestamps, session definitions, timezone offsets, and provenance.
- Check freshness, gaps, duplicates, stale quotes, symbol identity, and feed health.
- If any required input is missing or stale, mark the affected symbol/metric DATA? and block dependent calculations. Never synthesize missing bars.
- Do not proceed as though the report were live if the source is demo/imported.

### Step 3 — Populate and rank the watchlist
- Discover candidates from the configured scanner groups.
- Preserve raw discovery events, provider, first-seen time, source timestamp, group, and raw fields.
- Deduplicate symbols for display without discarding event history.
- Compute only validated, versioned features; display score components and reason codes.
- Re-rank on new valid events/data, with stable tie-breaks and visible “last ranked” time. Avoid visual jitter from inconsequential changes; record ranking changes.
- Retain extended candidates in the continuation/pullback lane instead of silently losing them.
- User-added symbols remain distinguishable from provider-discovered candidates.
- The current page does **not** do this automatically. Its demo rows and imported CSV rows are not calculated by the C# engine.

### Step 4 — Populate the Top 1–4 Candidate “Red Zone”
- Select up to four candidates from the ranked universe based on explicit criteria.
- Show them simultaneously in a 1–4 split layout, ordered by rank. A larger candidate count belongs in the watchlist, not forced into this panel.
- Update only when new timestamped source data arrives; display each pane's quote/data age and the last refresh.
- Preserve clear distinctions: opportunity quality, 5M authority/Permission, 1M execution event, extension risk, and position management.
- A strong candidate can remain visible while blocked or extended, but the panel must label why it is not actionable.
- The current UI has no Red Zone panel, live chart tiles, or automatic re-ranking. These are planned features.

### Step 5 — Determine Boring Overnight
For each candidate, apply the source-controlled `docs/BoringOvernight.md` specification:
1. Overnight Edge Score.
2. Boringness Score.
3. Event/Catalyst Risk.
4. Current-Day Qualification, including abnormal extension/volume and deterioration.
5. 5M structure/permission where appropriate.
6. 1M entry timing where appropriate.
7. Decision state with reason codes and freshness.

Initial gates documented there include price at least $5, 20-day average volume at least 500,000 shares/day, overnight win rate at least 60%, 20-day average ON return at least +0.10%, 5-day average ON return greater than the 20-day average, price above 20-day SMA, no maintained Schwab exclusion, and no incompatible material event/catalyst risk. Treat these as specification inputs pending full implementation/validation, not as a currently computed dashboard result. Score bands never override hard gates. Unknown catalyst data is DATA? / manual review, not READY.

The user makes the final decision. A zero position is valid. The Dashboard must not manufacture a buy recommendation when the data or gates are incomplete.

### Step 6 — Review intraday candidates and continuation
- Check 5M Permission first for the higher-timeframe authority; 1M only times entry and may consume 5M context. 5M must never depend on 1M.
- A candidate bubble such as IN-TREND CANDIDATE indicates a candidate event, not automatically a READY entry. The checked source defines `inTrendExecutionCandidate = executionCandidate AND NOT freshTrigger`; the event occurs on a transition with re-arm logic. Final READY remains separately gated by an active execution window, 5M permission, and no bullish extension danger.
- In the repository's 1M MainStudy, ATR extension is `AbsValue(price - dailyVWAP) / ATR(14)`; warning defaults to 1.50 ATR and danger to 2.00 ATR. Bullish extension danger is price above Daily VWAP and extension at/above danger. These are repository-source findings, not proof the installed Thinkorswim study is identical.
- If EXT is dangerous, keep the candidate visible for monitoring but do not call it entry-ready. A pullback/reclaim or consolidation-breakout alternative must be separately specified, tested, and validated before changing trading rules.
- Catalyst/news context needs a trustworthy source, publication time, observation time, and freshness. News alone never grants eligibility.

### Step 7 — Manual Schwab entry
MCC does not submit orders. Manual Schwab website order entry is the current preference. No broker credentials/API, order ticket prefill, or “Buy Maximum Amount” button is implemented or assumed supported. A future sizing helper should be non-submitting, show price timestamp, selected custom/max budget, reserve/cushion, whole-share calculation and assumptions, and leave final review/submission to the user in Schwab.

### Step 8 — Record feedback and close the loop
- Save dashboard issues with category, priority, symbol, timeframe, timestamp, and expected-versus-observed behavior.
- Export feedback JSON before switching browsers/devices or clearing browser data.
- Feed validated observations into the To-Do list and user manual. Do not claim notes are uploaded automatically.

## 4. Current controls in the prototype
- Workspace selector: Intraday Momentum / Boring Overnight demo views.
- Symbol selector and replay slider: illustrative fixtures, not live symbols/data.
- Scanner group selector: demo candidates; local CSV import accepts Symbol/Ticker and optional Score, Status, Reason, Extension/Ext, Freshness/Data_As_Of.
- Watchlist: manual add/remove/clear; stored in browser local storage.
- Feedback notebook: category/priority/note; stored locally; JSON export.
- System readiness panel: states demo-only market data, no Thinkorswim connection, no selected live scanner, no order execution.
- Reset demo: restores illustrative view.

## 5. Planned acceptance criteria
- [ ] Launch instructions and desktop packaging decision documented; do not promise an .exe until built and distributed.
- [ ] Live provider selected, licensed/authorized, and integrated with connection/freshness/error states.
- [ ] Source-attributed Morning Report is generated for the report date and 08:30 Arizona workflow.
- [ ] Automated watchlist discovery, deduplication, ranking, stable ordering, and append-only event history work on real timestamped data.
- [ ] Top 1–4 Red Zone tiles show rank, reasons, price/time, source freshness, 5M/1M states, extension and catalyst context.
- [ ] Boring Overnight calculations match documented specification and independent test fixtures.
- [ ] EXT/continuation behavior is validated without bypassing the current entry gate.
- [ ] Missing/stale data produces DATA? and cannot become READY.
- [ ] Historical replay and out-of-sample tests are completed before any new rule affects actionable ranking.
- [ ] UI feedback/export survives routine use; data persistence and backup behavior are documented.
- [ ] Manual Schwab remains separate; no automatic order submission.

## 6. Canonical references
- `docs/Master-To-Do.md` — canonical task list.
- `docs/NewChatHandoff.md` — recovery/context handoff.
- `docs/Scanner-and-Watchlist-Workbench.md` — scanner and local dashboard status.
- `docs/BoringOvernight.md` — Boring Overnight criteria.
- `docs/Charts/1M/MainStudy.txt` and `docs/Charts/5M/MainStudy.txt` — repository study references.
- `README.md` — project overview and current prototype launch instructions.

## [Uncertainties]
- The detailed prior Morning Routine conversation is not available in the accessible project source/history or repository search. The proposed report layout and workflow above are grounded in documented project requirements, but may not capture every step or metric from that earlier conversation.
- The repository ThinkScript source may differ from the user's currently installed Thinkorswim studies; confirm installed version before treating the source audit as runtime proof.
- Live provider/API, packaging choice (.exe versus local web app), and actual live ranking formulas are not selected or implemented.


## Overnight collection and Morning Report — implementation requirement (2026-10-22)

The user intends to leave the released/deployed MCC program open overnight so scanner discovery and data collection are already available in the Morning Report around 08:30 Arizona time. This is a product requirement, **not a capability that exists today**.

### Required behavior before this can be relied upon
- A real, authorized market-data/scanner provider must be selected and integrated. No provider is selected today.
- A long-running collection service must poll/subscribe to supported discovery and market-data endpoints, handle reconnects/rate limits/outages, and persist events and timestamped observations. The browser page by itself cannot do this in the current implementation.
- Collection should be independent from the presentation tab where architecture permits. If the UI is closed/reloaded, the collector should continue as a local background service or a hosted service, rather than depending on browser-tab timers.
- If a desktop app is chosen, startup/shutdown behavior and background-service lifetime must be explicit. If using a local service, the user must launch it and leave the machine awake/connected; OS sleep, network loss, provider outages, authentication expiry, or market closures can interrupt collection. Do not imply the app can gather valid new market bars while the relevant market is closed.
- Persist raw source events and observations before derived scoring; include provider/source ID, event ID, symbol, source timestamp, receive/observed timestamp, timezone/session metadata, schema/version, and ingestion disposition. Preserve history; do not silently overwrite a symbol's prior event.
- On startup and at report generation, reconcile last successful poll, latest source timestamp, gaps, duplicate events, provider status, and stale symbols. Show collector status as RUNNING, DEGRADED, STOPPED, or UNKNOWN with last-success time. Missing/stale data must remain DATA?.
- Generate a report for the current report date only after the data-quality checks run. Clearly separate overnight-acquired observations from premarket/RTH updates. Include source attribution and freshness on every candidate.
- The Morning Report must be reproducible from persisted observations and versioned scoring logic; export report snapshot/JSON and record scoring version.
- Test overnight process recovery, machine sleep/wake, network outage, provider outage, duplicate/replayed events, timestamp/session boundaries, and startup after missed collection. No missing bars may be synthesized.
- Security: never put provider credentials in frontend code, browser storage, committed files, or logs. Use approved local secret storage/configuration.
- The current UI now includes a **preview-only** Morning Command Brief, Red Zone layout, and JSON export. It intentionally states that overnight collection is not running and uses only local demo/import rows. It is not evidence that the background scanner exists.


## Recovered Morning Report requirements and current provider research (2026-10-23)

- Consolidated the provided Project source file Morning.txt into [Morning Report and Routine](Morning-Report-and-Routine.md). It captures Morning Landscape, scenario-based market expectations, continuously refreshed conditions, contextual opportunity hierarchy, Red Zone, Boring Overnight integration, probabilistic catalyst-maturation risk, Journey/outcome tracking, personalized timing, and eventual execution-quality learning.
- The Red Zone is intended to occupy the **upper-right** of the main workspace. A persistent left Watchlist/News rail is not yet decided; layout remains open for user feedback.
- [Provider comparison](Provider-Comparison.md) evaluates Alpaca, Massive, Twelve Data, and Schwab evidence. No provider has been selected or account entitlements verified. Do not use delayed/single-venue feeds as though they were complete real-time consolidated data.
- A first provider-neutral event persistence scaffold now exists: validated batch ingestion, JSONL append-only persistence, idempotency, and receipt timestamps. It is not a live collector and currently persists scanner discovery events, not a complete market-bar store.
- To validate the actual installed Thinkorswim studies, follow [the exact 5M-to-1M validation steps](Validation/5M-to-1M-User-Validation-Steps.md). Previously completed OHLCV/DST tests should not be repeated; this request is specifically about installed-study source and same-bar signal-state evidence.


## API testing with Swagger — added 2026-10-27

Run the API locally with `dotnet run --project src/MomentumCommandCenterV2.API --configuration Release` and open `/swagger` on the host/port printed by ASP.NET Core. Use Swagger to run the health, provider-status, market-data comparison and Boring Overnight evaluator endpoints without manually building URLs. Swagger does not guarantee authentication, data entitlement, completeness, or trading readiness. Never enter API keys into Swagger requests.

`POST /api/boring-overnight/evaluate` is a deterministic policy-test endpoint. It expects caller-supplied snapshot fields; it is not yet connected to automatic scanner/catalyst collection. Missing or unknown critical values should remain DATA? rather than being guessed. It does not calculate a validated overnight limit target and never submits orders.


## Dashboard interaction guide — 2026-10-30

### Main Chart
The Main Chart is at the top of the main workspace. It displays an API-backed 1-minute close-price line for the selected symbol. Choose a symbol from the workspace toolbar or activate a Red Zone card to focus it. This is not yet an OHLC candlestick chart and does not plot the Thinkorswim 5M/1M study states.

### Morning Command Brief and discovery
1. Review the Morning Command Brief and source/freshness notices.
2. Select a scanner group and use **Refresh Alpaca market movers** to load persisted Alpaca discovery events.
3. Discovery rows are not qualified trading signals. Review them in Red Zone and use Scan & Analyze for explicit 1M/5M bar windows.

### Red Zone controls
- **Refresh Red Zone** redraws the currently selected candidate source and retries the Red Zone price-chart requests.
- **Refresh Alpaca market movers** fetches persisted provider discoveries and changes the selected scanner group to the live-discovery group.
- **Pin to Red Zone** manually adds a ticker to the top of the four-card display.
- **Dismiss** hides an automatically sourced candidate from Red Zone.
- **Restore dismissed** clears dismissals and allows those source candidates to appear again.
- Pins and dismissals are stored in this browser's localStorage. They do not sync to another browser/device or to the server. Clearing site data can erase them.
- A manually pinned symbol without an underlying scanner row displays `MANUAL — REVIEW`; it has no invented score, freshness, permission, or execution state.

### Provider Comparison
Provider Comparison is in the diagnostics area and is not part of the normal morning routine. Use it when evaluating or re-testing Alpaca versus Massive: choose a ticker, 1m/5m interval and date, then prepare a same-window historical REST comparison. Inspect bars, first/latest timestamps, timestamp gaps and duplicates. This is not a streaming test, complete-session guarantee, reconnect/backfill test, entitlement check, or provider approval.

### Quick Start
Open Quick Start from the header or the footer. It summarizes the normal order: review the brief, refresh discovery, inspect Red Zone, focus the Main Chart, run Scan & Analyze, review 5M/1M context, and use Overnight separately.

### Current Decision Explanation
The Decision Explanation panel follows the selected symbol and requests its live 1m/5m assessment. It shows the actual V2 engine decision, six individual 5M score conditions, source/feed, timestamps, freshness, and the server's evaluation reason when a score is UNKNOWN. A 5M score of 5/6 or 6/6 establishes 5M permission; **EXECUTION READY** appears only when the separate 1M entry gate also passes. No broker connection or order submission exists.


## Live Main Chart and Red Zone — 2026-10-30

### What happens when you pin a ticker
Pinning a symbol adds it to the browser-local Red Zone list. MCC now automatically requests a live symbol assessment for each visible Red Zone symbol, including manually pinned symbols that do not appear in the scanner discovery rows. The assessment uses Alpaca historical REST bars and does not depend on the ticker having a scanner score.

### Automatic data refresh
- Each visible Red Zone card requests a fresh assessment when rendered.
- The browser refreshes Red Zone assessments, the focused Main Chart, and Decision Explanation every 15 seconds while the page is visible.
- The API caches each symbol/provider result for 10 seconds to avoid duplicate provider requests.
- The Main Chart displays up to the latest 180 completed 1-minute bars.
- The service fetches 1m bars from the prior 3 calendar days and 5m bars from the prior 21 calendar days, then persists the returned observations.

This is near-live REST polling, not a websocket or tick stream. It does not promise quote-level or trade-level updates between refreshes.

### What Red Zone values mean
- **Extension:** Non-negative distance from the latest 1m close to its 1m EMA9, measured in 1m ATR14 units.
- **Freshness:** Age since the expected close of the latest completed 1m and 5m provider bars. The current limits are 120 seconds for 1m and 360 seconds for 5m.
- **5M score / permission:** Calculated by the existing V2SignalEngine from 5m VWAP, EMA9, EMA20, SMA50, RSI14 and same-time-of-day relative volume.
- **1M execution:** Entry-context state calculated by V2SignalEngine using 1m VWAP, EMA9/EMA20 structure, MACD, RSI and the 5m permission result. CONFIRMED means the engine's entryAllowed flag is true; WAITING means 5m permission exists but 1m entry conditions are incomplete; BLOCKED means 5m permission is absent.
- **Relative volume:** Current 5m volume divided by average volume for the same 5-minute regular-session clock slot across up to ten prior sessions. At least five prior samples are required. This is a calculated same-time-of-day baseline, not an Alpaca-provided consolidated RVOL field.

### Why a pinned ticker can still show UNKNOWN
The ticker can be pinned even if it has no scanner source row. Market-data qualification still requires valid provider bars:
- at least 50 completed 1m bars and 51 completed 5m bars (for current and previous 5m context);
- fresh latest completed bars;
- at least five prior regular-session volume samples for the current 5m clock slot;
- evaluation during the US regular session.

If any required evidence is missing, stale or unavailable, MCC withholds permission/execution and displays an explicit status/reason. Do not treat UNKNOWN as a pass or a failure of the stock itself.

### Decision-engine boundary
The API calls the existing V2SignalEngine with `hasPosition=false`. This is an entry-candidate assessment. It does not perform existing-position management, send orders, or connect to Thinkorswim. No catalyst, halt, NBBO spread, order-book depth, or news classification is inferred.

### Layout
- System Readiness is now a 2×2 grid to the right of the brand.
- Workspace toolbar is inside the header.
- Scanner Workbench table is 600px high with vertical/horizontal scrolling; its Watch action column remains sticky on the right while horizontally scrolling.


### V2 score bands and UNKNOWN
- 0–2/6: low alignment (red X).
- 3–4/6: partial alignment (amber).
- 5–6/6: strong alignment (green check). Five of six is the engine's minimum 5M permission threshold.
- A high 5M score is not an entry signal. The 1M execution gate must independently pass before the card says **EXECUTION READY**.
- UNKNOWN is not zero. The live card now reports the exact API status/reason, such as insufficient history, stale bars, insufficient same-slot RVOL samples, or outside regular session.

### Replay point
The former slider only selected one of five fixed demo examples. It did not replay chart history or recalculate a past V2 decision. It has been removed from the active workspace. Historical decision replay is not implemented yet.


## Morning Command Brief, replay, and stream status — 2026-10-30

- Open **Morning Command Brief** from the header for a modal, or expand the collapsed full-width Morning Command Brief near the bottom of the page. Both show the same partial API-generated report.
- The report is a source snapshot, not a trade recommendation. Missing breadth, catalysts, probabilities, Red Zone qualification, and Boring Overnight calculations remain unavailable.
- Historical Decision Replay requires a symbol, exact feed (IEX/SIP), and explicit UTC as-of timestamp ending in Z. It uses persisted completed bars only; the API does not fetch missing replay history.
- The replay checkbox simulates the engine's existing boolean position state. It does not connect to Schwab or represent actual shares/entry price.
- The header WebSocket chip reports the optional Alpaca minute-bar archive. It is disabled by default and does not mean the live V2 assessment is stream-driven; the live assessment remains REST-backed until separate source integration/parity validation.
- If a live assessment says stale, inspect its `freshnessDiagnostics` for 1m/5m age, threshold, source timestamp, and expected close. A generic stale message cannot diagnose a specific ticker.
