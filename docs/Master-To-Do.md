## Latest verified checkpoint — 2026-10-30

- Branch: `feature/opportunity-quality-classification`
- Verified branch head at checkpoint: `0d669ad888400682575b7d8bd895f7ff55b2436d`
- GitHub Actions run **707** passed Release build and **152/152 tests** after correcting the transport-provenance test fixture.
- Runs **708–714** completed successfully, including hybrid transport UI/header, documentation, and latest cache-bust updates.
- Frontend JavaScript syntax parsed successfully after Morning Brief, replay, and hybrid-source UI changes.
- Browser acceptance remains with the user: modal/accordion behavior, actual symbol freshness JSON, replay against populated stored history, and live WebSocket subscription/data flow.
- Historical per-symbol stale causes for RKLX, ASTX, and XRPN are not yet known. The generic stale response supplied earlier does not contain the timeframe ages or timestamps. **Data not found in sources.**
- Schwab OAuth/account/position sync remains unimplemented pending the user's Developer Portal approval, callback URL, and secure server-side app credentials. Order execution remains disabled.

## Validation and hybrid-source checkpoint — 2026-10-30

- [DONE — SOURCE] Raw `MarketBar` observations now retain `SourceTransport` (`REST` or `WEBSOCKET`). The append-only store includes transport in its content hash, so identical OHLCV arriving by a different transport is recorded as a provenance change rather than silently deduplicated.
- [DONE — SOURCE] Live assessment uses the persisted 1m series only when the Alpaca WebSocket is confirmed subscribed, at least 50 completed 1m bars are available, the latest bar is fresh, and the latest observation is explicitly tagged `WEBSOCKET`. Otherwise it falls back to REST. 5m permission remains REST.
- [DONE — UI] Decision Explanation, Main Chart, and the header now identify the actual 1m transport and 5m REST source. Header transport label follows the selected symbol's assessment.
- [PASS — TESTS] GitHub Actions run **707** for commit `8b015d73e88cda62ffef79e9a103836218b5a8e9`: Release build succeeded; **152/152 tests passed**. The new transport-provenance test initially failed because its fixture omitted `Timeframe="1m"`; that fixture was corrected. Runs 701–706 are historical failures from that one test-fixture issue and are superseded by the passing run 707.
- [PASS — CI] Runs 708, 709, and 710 passed for frontend cache-bust/header updates. Run 711 for commit `980dea5d3cf4e217fab072689db04f7b6c0971e6` was in progress at this checkpoint.
- [OPEN — CURRENT HEAD] Verify CI for the latest documentation and cache-bust commits before treating the newest branch head as validated.
- [OPEN — RUNTIME] WebSocket is optional and disabled by default. User must configure it server-side and verify subscription, bar arrival, reconnect, and 1m/REST parity. Browser runtime verification remains outstanding.

## Active implementation checkpoint — Morning Brief, replay, stream, and stale diagnostics — 2026-10-30

## Active implementation checkpoint — Morning Brief, replay, stream, and stale diagnostics — 2026-10-30

### Completed in source (CI and browser acceptance pending)
- [DONE — UI] Morning Command Brief now has two entry points: a full-width collapsed-by-default section near the bottom of MCC and a header button that opens a modal. Both show the same API-backed report snapshot.
- [DONE — API] Live assessment stale-data failures now identify which timeframe(s) are stale and include observed age, threshold, source timestamp, expected close, and feed. The response also includes structured `freshnessDiagnostics`.
- [DONE — API/UI] Added `GET /api/market-data/replay` and a Historical Decision Replay panel. It requires an explicit UTC as-of timestamp and one explicit feed; only completed stored bars at or before as-of are considered. Missing history returns an explicit status.
- [DONE — API] Replay calls the same current `V2SignalEngine` with the default `V2Config`. The UI checkbox allows a simulated `hasPosition=true` run to exercise the existing position-management branch. This is a simulation, not a Schwab position read.
- [DONE — API] Added an optional Alpaca WebSocket minute-bar worker. It is disabled by default, authenticates, confirms the subscription acknowledgement, reconnects with backoff, persists `bars` and late `updatedBars` as append-only observations, and exposes `/api/market-data/stream/status`.
- [DONE — UI] Added a header status indicator for the optional WebSocket minute-bar collector. It distinguishes disabled/connected/error states. The live assessment now uses the persisted 1m WebSocket series when the stream is confirmed, at least 50 completed bars exist, the latest bar is fresh, and the latest stored observation is explicitly tagged WEBSOCKET. Otherwise it falls back to 1m REST. 5m permission bars remain REST.
- [DONE — SAFETY] Order submission remains unimplemented and disabled.

### Critical open work
- [PRIORITY 1 — USER DATA REQUIRED] After pulling this branch and refreshing MCC, request the live-assessment JSON for RKLX, ASTX, and XRPN. The new stale reason now tells us whether 1M, 5M, or both exceeded their freshness limits, with exact ages/timestamps/feed. The generic message previously supplied does not contain those per-symbol values. **Data not found in sources.** Do not assume the three tickers have the same stale timeframe or cause.
- [OPEN — REPLAY VALIDATION] Run the new replay panel against a timestamp with known stored 1M/5M bars. Confirm it returns REPLAYED, uses the selected feed, excludes all bars after as-of, and returns INSUFFICIENT_STORED_HISTORY when coverage is missing. Historical V2 config versioning is not yet archived; replay uses the current default config.
- [OPEN — STREAM VALIDATION] Configure the worker, verify `/api/market-data/stream/status` reports SUBSCRIBED/CONNECTED, and verify 1m bars plus late corrections are appended. The live assessment now uses WebSocket-sourced 1m bars when the connection/history/freshness/provenance guards pass; it falls back to REST otherwise. 5m permission remains REST-backed.
- [OPEN — STREAM CONFIG] Set `MarketData:Alpaca:StreamEnabled=true` and `MarketData:Alpaca:StreamSymbols=SPY,RKLX,ASTX,XRPN` in server-side configuration. Keep the existing feed selection aligned with entitlement (`iex` for Basic; `sip` only when authorized). Do not place credentials in frontend files or chat.
- [OPEN — SCHWAB] Read-only Schwab OAuth/account/position synchronization is not yet implemented. The API product has no clearly published monthly price on its official product page; confirm any fees/approval with Schwab. User must register an Individual Developer role, request Trader API Individual access, create an approved app, and provide the app key/secret plus callback URL through secure server-side configuration. Never share credentials in chat.
- [OPEN — POSITION-AWARE] The replay checkbox tests the engine's existing boolean `hasPosition` branch only. Actual broker positions, quantity, side, average entry, sync freshness, and unknown/stale-position fail-closed behavior are not connected. Add a normalized read-only position snapshot before claiming real position-aware operation.
- [OPEN — BROWSER] User has confirmed the red X/low score presentation, but must verify the new Morning Brief locations/modal, replay controls, and WebSocket status chip after pulling latest branch.
- [OPEN — BRIEF DATA] `/api/morning-brief` is still a partial market snapshot. It does not yet establish complete-market breadth, verified sector money flow, classified catalysts, historical outcome probabilities, a validated Red Zone rank, or Boring Overnight qualification. Missing inputs must remain DATA NOT FOUND IN SOURCES.

### Acceptance checklist
1. Run Release build/tests and inspect CI for the exact new branch head.
2. Pull branch and hard-refresh MCC.
3. Open Morning Command Brief from the header; close it; expand/collapse the full-width footer section. Verify both show the same current API report timestamp.
4. Inspect RKLX, ASTX, XRPN and copy each full live-assessment JSON response; compare `freshnessDiagnostics.oneMinute` and `fiveMinute`.
5. Replay one known timestamp with adequate stored history; repeat with `hasPosition=true`; inspect the full audit JSON and confirm future-bar exclusion.
6. Enable WebSocket only after server configuration; verify subscription acknowledgement, bar count, reconnect state, and late-bar corrections. Compare streamed 1M bars with REST for the same feed.
7. Complete Schwab Developer Portal setup and supply only non-secret callback/approval facts here; enter secrets directly into server-side secret storage.
8. Keep order submission disabled.


## Validation status correction — 2026-10-30

- GitHub Actions run 663 **PASSED** for live Decision Explanation / score UI commit `d2dbd628fa40de11e49b0693b25280e2dc22453e`.
- GitHub Actions run 664 **PASSED** for the Master To-Do documentation update.
- GitHub Actions run 665 is still running for the latest Handoff documentation update; the code itself has already passed run 663.
- Frontend JavaScript syntax check passed. User confirmed the live assessment endpoint returned Success.
- Local browser verification of this newest Decision Explanation/score presentation remains open.

## Latest validation checkpoint — 2026-10-30

- Current implementation adds live Decision Explanation, six-factor V2 score breakdown, colored Red Zone score bands, exact UNKNOWN reasons, 15-second REST refresh, and a 10-second assessment cache.
- [USER CONFIRMED] Direct `GET /api/market-data/live-assessment` endpoint test returned Success.
- [PASS] Frontend `app.js` JavaScript syntax check.
- [PASS] GitHub Actions run 661 for commit `8ff0ea4a01af356c55c901321585fb92b5d90241`.
- [PASS] GitHub Actions run 662 for commit `c5e14a6f30a463062e3d7ef221b73a1ca505ba60`.
- [IN PROGRESS] GitHub Actions run 663 for commit `d2dbd628fa40de11e49b0693b25280e2dc22453e` (state-label formatting). Do not mark the current branch head validated until this completes.
- [OPEN — USER BROWSER TEST] The new live Decision Explanation, score colors, UNKNOWN explanations, and Product Feedback bottom placement need a local browser check.
- [OPEN — SYMBOL DATA] Exact RKLX/ASTX/XRPN evaluation reasons were not supplied in this conversation. The updated cards now show each symbol's server-returned status/reason; inspect those instead of guessing.
- [OPEN — STREAM] Current hybrid path: 1m uses confirmed WebSocket-persisted bars when eligible, otherwise REST; 5m permission remains REST. The V2 rules operate on completed 1m/5m bars; raw tick streaming is not required.
- [OPEN — POSITIONS] Schwab/Thinkorswim read-only position integration is design-only; see `docs/Thinkorswim-Schwab-Integration-Plan.md`.

## 2026-10-30 — Live Decision Explanation, score semantics, replay clarification

- [DONE — CODE] Added `fiveMinute.scoreBreakdown` to the live-assessment API. It returns the six V2 5M conditions individually (true/false/null), score maximum 6, and permission threshold 5.
- [DONE — CODE] Replaced the top-level demo snapshot Decision Explanation with symbol-specific live API data. It now shows the engine state/reason, 5M score conditions, 1M/5M source timestamps, freshness, feed, RVOL baseline sample count, and 1M entry context.
- [DONE — CODE] Red Zone score display is color-coded: 0–2 red with X; 3–4 amber; 5–6 green. The green **EXECUTION READY** label is reserved for `decision.entryAllowed=true`; a strong 5M score without 1M confirmation says to wait.
- [DONE — CODE] UNKNOWN cards now display the exact API evaluation status/reason rather than an unexplained UNKNOWN.
- [DONE — CODE] Removed the misleading Replay point slider. It previously selected five hard-coded examples, not historical bars or historical decisions. The UI now explicitly says historical replay is not implemented.
- [DONE — CODE] Moved Product Feedback to the bottom, after Provider Comparison.
- [DONE — CODE] Changed the dashboard refresh cadence to 15 seconds and the per-symbol API cache to 10 seconds. This remains REST polling, not streaming.
- [DONE — DOCS] Updated this To-Do, New Chat Handoff, User Manual, and Live Symbol Assessment guide; added a Schwab/Thinkorswim integration plan.
- [VALIDATION] GitHub Actions run 648 passed for commit `c808456b1cb36382fe9491735e79c6de336f47e8` (nullable RVOL compile fix). The current Decision Explanation/score/UI commits are newer and require their own CI run before acceptance.
- [OPEN — USER TEST] Confirm live Decision Explanation follows selected symbol; verify 6-factor criteria and timestamp/feed; confirm 0–2 red, 3–4 amber, 5–6 green, and entry-ready only when 1M entryAllowed is true.
- [OPEN — SYMBOL DIAGNOSIS] RKLX, ASTX, and XRPN previously displayed UNKNOWN. The new UI shows each symbol's actual `evaluationStatus` and `evaluationReason`; inspect those responses to establish the individual cause. Do not assume the same cause for all three.
- [OPEN — REPLAY] Implement true historical decision replay using stored 1m/5m bars, explicit as-of time, and the same versioned V2 engine/config. Preserve historical data quality and never use today's future bars.
- [OPEN — STREAMING DECISION] Optional Alpaca WebSocket minute bars and late corrections are implemented. The live assessment consumes a fresh WebSocket-sourced 1m latest bar when eligible; 5m remains REST. Validate source parity and reconnect behavior before treating this as production-ready.
- [OPEN — THINKORSWIM/SCHWAB] Read-only Schwab Trader API position integration plan is documented in `docs/Thinkorswim-Schwab-Integration-Plan.md`. Requires user-owned Schwab developer app, approved API access, OAuth token lifecycle, secure token storage, account/position mapping, and reconciliation. Do not enable order submission as part of the position-read phase.

## User browser verification update — 2026-10-30

- [USER VERIFIED] Morning Brief News Link hover.
- [USER VERIFIED] Superscript legend lines.
- [USER VERIFIED] Main Chart placement.
- [USER VERIFIED] Red Zone focus updates.
- [USER VERIFIED] Red Zone refresh.
- [USER VERIFIED] Manual Red Zone management.
- [USER VERIFIED] System Readiness topbar.
- [USER VERIFIED] Provider Comparison placement at bottom.
- [USER VERIFIED] Quick Start modal.
- [USER VERIFIED] Favicon 404 no longer present.
- [IMPLEMENTED; USER RETEST PENDING] Section IDs.
- [STILL ONGOING] BrowserLink `$.noConflict` error. Do not mark fixed until the injecting script and stack are identified.

## Final implementation checkpoint — live data and layout (2026-10-30)

- Current implementation includes `GET /api/market-data/live-assessment`, `LiveSymbolAssessmentService`, the live frontend consumers, 600px scanner table, compact topbar, tests, and documentation.
- The latest technical-calculation refinements use Wilder RSI14 and ATR14, regular-session-only VWAP, same-clock-slot 5m RVOL, and explicit future timestamp status.
- Live UI is REST-polled every 60 seconds; API results are cached for 30 seconds. This is not streaming/tick data.
- Latest code SHA before this documentation update: `bc1d7f9bf3c862d8780c665b1b68585991d64644`.
- GitHub Actions for this work have been created but are still queued; no successful build/test result exists yet for the live-assessment changes. Previous successful run 620 predates this feature and must not be used as evidence that the new service compiles.
- Browser runtime acceptance is pending for the new live endpoint, auto-updating Red Zone/Main Chart, 600px table, sticky Watch action and compact header.

## 2026-10-30 — Live symbol assessment / Main Chart / Red Zone

- [DONE — CODE] Added `GET /api/market-data/live-assessment?symbol=...` and `LiveSymbolAssessmentService`. It retrieves Alpaca 1m bars over a 3-calendar-day lookback and 5m bars over a 21-calendar-day lookback, persists both source sets, and caches each symbol/provider assessment for 30 seconds.
- [DONE — CODE] Added deterministic OHLCV calculations for EMA9, EMA20, SMA50, RSI14, MACD(12,26,9), ATR14 and session VWAP.
- [DONE — CODE] Added same-time-of-day 5m relative volume: current 5m bar volume divided by the mean volume for the same regular-session 5m slot across up to ten previous sessions. Requires at least five prior session samples; this is a calculated same-slot baseline, not a vendor-provided consolidated RVOL value.
- [DONE — CODE] Connected the existing `V2SignalEngine` to current completed 1m/5m snapshots. Response includes 5M score/permission, 5M structure, 1M structure/momentum, entry permission, state/action, extension and reason.
- [DONE — CODE] Added explicit fail-closed statuses for insufficient history, stale source bars, insufficient same-slot RVOL baseline and outside-regular-session requests. Permission/execution are not calculated in those states.
- [DONE — CODE] Red Zone now requests a live assessment for every visible pinned/discovered ticker and updates extension, 1m/5m freshness, V2 score, 5M permission, 1M execution and mini-chart. A manually pinned ticker is no longer permanently stuck at DATA?/UNKNOWN; those values resolve when the provider returns sufficient source evidence.
- [DONE — CODE] Main Chart now consumes the same live-assessment endpoint and uses returned 1m OHLCV bars. Symbol changes refresh the chart; browser refreshes the focused chart and Red Zone cards every 60 seconds.
- [DONE — CODE] Moved `workspace-toolbar` inside the header. System Readiness now sits to the right of the brand in a 2×2 grid.
- [DONE — CODE] Scoped Scanner Workbench `.table-wrap` to 600px height with vertical and horizontal scrolling, sticky table headers, and a sticky right-side Watch action column to keep Watch visible while horizontally scrolling.
- [DONE — TESTS ADDED] Added `LiveSymbolTechnicalCalculatorTests.cs` covering indicator snapshots, same-time-of-day RVOL sample sufficiency, and Eastern regular-session boundaries.
- [LIMITATION — IMPORTANT] This is near-live historical REST polling, not a websocket/tick stream. The browser refresh cadence is 60 seconds and the server assessment cache is 30 seconds. It does not guarantee every trade/quote update.
- [LIMITATION — IMPORTANT] V2 is evaluated with `hasPosition=false` because MCC does not yet have a broker-position source. The resulting 1M execution label is an entry-context assessment, not open-position management.
- [LIMITATION — IMPORTANT] No catalyst/news classification, halt status, NBBO spread, order-book depth, or Thinkorswim study values are inferred. Provider/feed entitlement still determines bar coverage.
- [OPEN — VALIDATION] Run GitHub Actions for the latest head and inspect test results. Then test live API response with a known liquid ticker during regular session; verify returned source timestamps, feed, bar counts, same-slot baseline sample count, V2 output, and failure behavior with an unconfigured/invalid symbol.
- [OPEN — USER BROWSER RETEST] Pull latest branch and verify: manual pin resolves from actual provider data; extension and freshness update; 5M permission and 1M execution show real engine output; Main Chart displays latest 1m bars; data refreshes on 60s cadence; stale/out-of-session/insufficient baseline states fail closed; table-wrap is 600px and Watch remains visible; header compactness and 2×2 readiness; toolbar inside header.
- [OPEN — PRODUCT] True websocket streaming / trade-level live charting remains future work if the 60-second REST refresh is not sufficiently current.

## 2026-10-30 — MCC browser feedback / layout pass
- [DONE — USER CONFIRMED] Morning Brief News Link hover behavior works in the user's local browser.
- [DONE — CODE] Split the Alpaca market-mover footnote into separate lines for ATG, ATL, Change, and Source.
- [DONE — CODE] Moved the focused-symbol 1-minute close-price chart to the top of the main workspace and labeled it **Main Chart**.
- [DONE — CODE] Red Zone tile focus now invokes the dashboard renderer as well as refreshing the focused chart; this addresses the prior behavior where the focused chart changed but `status-grid` / `layout` did not consistently rerender.
- [DONE — CODE] Added a dedicated **Refresh Red Zone** control. It re-renders the currently selected scanner group's candidates and reloads chart requests. To retrieve newly persisted Alpaca discovery events, use **Refresh Alpaca market movers**; the Red Zone button does not itself query the provider.
- [DONE — CODE] Added browser-local Red Zone management: manually pin a ticker, dismiss a scanner candidate, and restore dismissed candidates. Pinned symbols persist in localStorage under `mcc.redZoneState.v1`; they are not server-synced. Manually pinned symbols without source rows display `MANUAL — REVIEW` and DATA?/UNKNOWN fields rather than fabricated metrics.
- [DONE — CODE] Moved System Readiness into the header topbar and added stable IDs to the major workspace sections, including `main-chart-section`, `morning-command`, `morning-brief`, `red-zone`, `opportunity-status`, `decision-layout`, `scanner-workbench`, and `provider-comparison`.
- [DONE — CODE] Moved Provider Comparison out of the primary morning workspace and relabeled it as a diagnostic step, not a daily workflow step. It compares historical REST responses over the same symbol/date/interval; it does not prove full-session coverage, live streaming, reconnect/backfill, or entitlements.
- [DONE — CODE] Added a Quick Start modal with the broad morning/daily workflow and an opener in the header plus a footer opener.
- [DONE — CODE] Added an explicit SVG favicon link and `frontend/favicon.svg` to address the missing favicon asset. Browser verification of the disappearance of the 404 is still required.
- [UNRESOLVED — BROWSER ENVIRONMENT] The reported BrowserLink `$.noConflict` error remains unverified/unfixed. Neither `frontend/app.js`, `frontend/index.html`, nor API `Program.cs` contains a BrowserLink/noConflict reference; this suggests a development-host/browser-injected script may be involved, but the exact source is not established. Do not mark fixed without reproducing in the local browser and identifying the script URL/stack.
- [CI] GitHub Actions .NET build/test run 41095658724 passed for commit `c4bc11c3b894fb027bb1608c3e9976ae2e17da35`. This is a .NET workflow result, not browser JavaScript or UI validation. Later favicon/docs commits require their own latest-head CI check.
- [OPEN — USER BROWSER RETEST] Pull the latest branch and verify: hover News Link; each legend line; Main Chart placement and focus; Red Zone refresh/pin/dismiss/restore; topbar readiness; Provider Comparison placement; Quick Start open/close; stable section IDs; favicon 404; BrowserLink console error. Record the exact tested SHA and console/network output.
- [OPEN — DATA/PRODUCT] Main Chart is currently an API-backed 1-minute close-price line, not a full OHLC candlestick chart or a 1M/5M study chart. Decision summary cards still show the existing illustrative replay snapshot; clicking a Red Zone symbol rerenders them but does not yet calculate per-symbol 5M permission/1M execution state. Do not present demo values as live symbol-specific state.

## 2026-10-29 — SPY post-market control and dashboard coverage audit

- [VERIFIED LIVE BY USER] SPY 1m Alpaca/IEX for 2026-10-28 20:00–23:55 UTC returned 2 bars at 20:00 and 20:54 UTC, gapCount 1. 20:00 UTC = 16:00 EDT (regular close boundary); 20:54 UTC = 16:54 EDT (post-market). The 20:54 observation establishes that the configured IEX route returned at least one SPY post-market bar in this window. Two bars are extremely sparse and do not establish complete after-hours coverage.
- [UPDATED INTERPRETATION] RTO's zero bars for the same post-market window is therefore symbol-specific in the observed result, not evidence that Alpaca/IEX categorically cannot return extended-hours observations. Still unresolved: whether RTO had eligible IEX prints, IEX venue coverage, provider behavior, and the account's feed entitlement. Do not claim SIP is required or switch feed without entitlement evidence.
- [DASHBOARD IMPLEMENTED] Added a Post-market control test to the Boring Overnight panel. It queries the selected symbol and SPY over the same user-entered UTC window, displays provider/feed, count, first/latest source timestamps, and explicitly labels empty/sparse responses as inconclusive. It does not infer no-trade or qualification.
- [DASHBOARD OPEN P0] Saved snapshot table still correctly labels records as input snapshots, not final decisions. Evaluator request requires explicit 20+ session boundaries and caller-supplied non-history evidence; do not auto-submit or display READY until session boundaries, independent source evidence, and decision persistence are implemented.
- [OPEN] Timeframe identity in persisted bar storage, exchange calendar, automatic boundary-pair generation, final decision persistence, independent catalyst/5M/1M evidence, and market-wide discovery remain open. These are not safely solved by frontend-only changes.
## 2026-10-29 — RTO live retest: regular-session bars present, after-hours not demonstrated

- [VERIFIED LIVE BY USER] Exact RTO request for 2026-10-28 20:00–23:55 UTC returned a normalized empty result: feed null, barCount 0, no timestamps, no gaps. The null-bars adapter behavior is now confirmed in the user's running API. An empty response is not proof of no trading and does not validate an extended-hours feed.
- [VERIFIED LIVE BY USER] RTO request for 2026-10-28 19:00–23:55 UTC returned 47 IEX bars, first 19:01 UTC and last 19:59 UTC, with six reported missing-minute gaps. All returned bars are within the first hour only.
- [TIME CONVERSION — IMPORTANT] On 2026-10-28, US Eastern daylight time was UTC−4. Therefore 19:00–19:59 UTC is 15:00–15:59 ET (the final regular-session hour); 20:00 UTC is 16:00 ET (regular close); 20:00–23:55 UTC is 16:00–19:55 ET (post-market). The second request proves RTO regular-session data near the close, not extended-hours coverage.
- [INTERPRETATION] These two responses alone do not establish that Alpaca globally fails to return extended-hours bars. Need a same-window liquid-symbol control (SPY) using the same provider/feed, plus ideally a symbol known to have post-market trades. Compare response timestamps and feed. Do not switch from IEX to SIP or claim an entitlement defect without account/provider evidence.
- [NEXT LIVE TEST] Query SPY with timeframe=1m, from=2026-10-28T20:00:00Z, to=2026-10-28T23:55:00Z, providerName=Alpaca. Then, if practical, query RTO for 2026-10-28T19:50:00Z–2026-10-28T20:10:00Z to verify the close boundary. Record feed, barCount, first/latest timestamps and sanitized provider errors. SPY returning bars after 20:00Z demonstrates some IEX post-market coverage for that symbol/window; SPY empty means the cause remains ambiguous (IEX venue coverage, no eligible trades, provider request semantics, or entitlement).
- [OPEN — OVERNIGHT P0] The current evidence is sufficient to label RTO extended-hours coverage unverified/absent in this request, not to diagnose its cause. Keep missing bars as missing; never synthesize or carry forward regular-session prices into post-market.

## 2026-10-29 — RTO bars:null follow-up

- [VERIFIED LIVE BY USER] Retesting RTO for UTC `2026-10-28T20:00:00Z`–`2026-10-28T23:55:00Z` returned HTTP 502 because the successful Alpaca JSON payload had top-level `bars: null`, `next_page_token`, and `symbol`. This is different from the previously anticipated symbol-keyed array shape.
- [CODE CHANGE — TEST PENDING] The adapter now treats `bars: null` on the first page with no continuation token as an empty result, not as a successful data validation. It still rejects missing `bars`, and rejects null bars on a continuation page to avoid silently returning partial paginated data.
- [INTERPRETATION] An empty result is not proof of zero trading, nor does it establish whether the cause is feed entitlement, historical availability, symbol coverage, or a provider behavior. The actual cause remains unresolved without a provider response that includes an explanatory code/message or a non-empty RTO result.
- [NEXT LIVE TEST] Pull/restart with this change, repeat the exact RTO request, and inspect whether the endpoint returns HTTP 200 with `barCount: 0` / empty bars. If empty, compare the same time window against a known liquid symbol and a nearby regular-session RTO window. Do not mark the provider validated from an empty result.
- [OPEN — OVERNIGHT P0] Timeframe-aware observation identity, authoritative exchange sessions/calendar, automated 20-session boundary pairs, independently sourced catalyst/market evidence, and persistence/display of the final evaluator decision remain incomplete. These remain release blockers for Overnight readiness.

## 2026-10-29 — Live Alpaca REST verification and RTO payload-shape follow-up

- [VERIFIED LIVE BY USER] `/api/providers/status` reports Alpaca configured and `VALIDATED_DATA_RETURNED`, last successful validation at `2026-10-29T17:44:51.874Z`, 54 bars. This validates a non-empty response in that running API process, not every symbol/session/feed entitlement.
- [VERIFIED LIVE BY USER] `/api/market-data/bars` for SPY, 1m, UTC `2026-10-29T17:20:00Z`–`17:40:00Z`, Alpaca/IEX returned 21 bars, first/last timestamps exactly matched the requested bounds, gapCount 0, and no provider error. This confirms the live REST path returns usable data for that specific regular-session window.
- [VERIFIED LIVE BY USER] `/api/market-data/compare` returned the same 21 Alpaca bars with no error. Its persistedObservationCount was 0 because the preceding bars request had already persisted the observations; zero newly persisted records is not a fetch failure. Massive is unconfigured and should be treated separately.
- [ROOT CAUSE HYPOTHESIS — FIX IMPLEMENTED, NEEDS LIVE RETEST] The failed RTO after-hours response has top-level keys `bars`, `next_page_token`, and `symbol`. The adapter assumed `bars` must be a JSON array. It now also accepts an object under `bars` keyed by the requested symbol, while refusing to consume another symbol's array. Added a regression test for `bars: { "RTO": [...] }`. The user's provided error did not expose the nested `bars` value type or keys, so this is a strongly indicated response-shape mismatch, not yet confirmed as the exact runtime shape.
- [DONE — DIAGNOSTICS] If the response still does not match either supported shape, the exception now includes the top-level keys, `bars` value kind, nested `bars` keys, and requested symbol, with bounded provider code/message when present.
- [NEXT P0 — USER RETEST] Restart the API and retry RTO for UTC `2026-10-28T20:00:00Z`–`2026-10-28T23:55:00Z`. Verify that bars and source timestamps are from Oct 28 and inside the requested interval. If it still fails, capture the new sanitized shape diagnostic; do not infer after-hours coverage from the SPY regular-session test.
- [OPEN — OVERNIGHT P0] Timeframe identity in persisted observations, authoritative exchange-calendar/session-boundary generation, full 20-pair history evaluation, and persisted/displayed final decisions remain unfinished. Catalyst status remains unknown without a validated catalyst source; no READY decision may be inferred from missing evidence.

## 2026-10-29 — Alpaca live-response diagnostics and Overnight stale-result fix

- [DONE — CODE] The Overnight dashboard now clears all previously displayed bar counts/timestamps/gap values before each request and when a request fails. A failed request can no longer leave an older successful response looking like the result for the newly selected date window.
- [DONE — CODE] When Alpaca returns HTTP 2xx JSON without the expected `bars` array, the API now reports top-level response keys and sanitized provider `code`/`message` fields (bounded length) instead of only saying the bars array is missing. No credential/header values are included.
- [DONE — TEST ADDED] Added a fixture test for an unexpected 2xx payload, checking diagnostic content and that the adapter does not mark the provider validated. Fixture tests do not prove live Alpaca connectivity.
- [NEXT — USER LIVE TEST] Restart the API and request a small recent completed 1m window during regular market hours using `/api/market-data/bars`; compare it with `/api/market-data/compare`. Record HTTP status, provider/feed, requested UTC window, bar count, first/latest source timestamps and sanitized error fields. Do not share credentials or headers.
- [OPEN — DATA/ENTITLEMENT] The actual Alpaca response for the user's failed Oct 28 RTO after-hours window is not available to this repository session. We cannot conclude whether it was a feed entitlement issue, provider payload variation, a historical window limitation, or another cause until the new diagnostic response is captured.
- [OPEN — OVERNIGHT P0] Timeframe is still not part of persisted bar identity. Do not use stored observations for timeframe-specific Overnight metrics until that identity/query filter is implemented.
- [OPEN — OVERNIGHT P0] Authoritative exchange calendar/session boundaries and full 20-pair automated evaluation remain incomplete. Unknown catalyst evidence continues to block READY.

## Current implementation status — 2026-10-29 (Arizona time)

- [DONE — CI VERIFIED BASELINE] Prior workflow `40820328662` passed for commit `ff1e00450904fe0939cbf62c5bfabbefced0a2ea`. History-backed service/test/API commits `65603d9`, `76d90c2`, `cc1d852` each have green CI runs. Catalyst fail-closed fix `9ae6b79f43c07293b545b6447fdc82104b7f2790` and newest docs are pending their own workflow results.
- [IMPLEMENTED — CI PENDING] Added `BoringOvernightHistoryEvaluationService`. It replaces caller-entered overnight win rate, 20-session average, 5-session average, and 20-session median with values derived from the history calculator. If history is incomplete or too short, all four fields are set to null; no manual-value fallback is permitted.
- [IMPLEMENTED — CI PENDING] Added `POST /api/boring-overnight/evaluate-with-history`: reads stored bars, calculates history using explicit UTC close/open boundaries, resolves MCC's local exclusion list, evaluates the hard gates, and appends the resulting candidate snapshot. Because no validated catalyst provider is connected, the endpoint forcibly sets `CatalystStatus=UNKNOWN` regardless of caller input, preventing READY until catalyst evidence is wired. Other non-history inputs are still caller supplied.
- [IMPLEMENTED — TESTS ADDED, CI PENDING] Added tests that prove history values override caller-supplied metrics, incomplete history clears those metrics, and symbol mismatches are rejected. Also reject ambiguous history calculations that span multiple feeds when `feed` is omitted; this prevents combining unlike IEX/SIP observations into one overnight statistic.
- [NEXT P0] Add authoritative exchange-calendar/session boundary generation and validation (holidays, half-days, DST, provider bar timestamp convention). Until then the API requires explicit boundary timestamps.
- [NEXT P0] Replace externally supplied catalyst status, 5M Permission/structure, 1M state, price/SMA/volume and freshness with independently sourced, timestamped evidence. Unknown catalyst status must continue to block READY.
- [NEXT P0] Validate corporate-action adjustment and full eligible GTC_EXT window coverage; compare MCC ATR/extension and boundary pairing against matching Thinkorswim/provider data.
- [NEXT P1] Integrate the persisted evaluated snapshot into scanner, Morning Brief and MarketWatch surfaces after evidence contracts and tests are stable.
- [USER ACTION — LOCAL VALIDATION] After CI passes, pull the branch and run the API tests described in `docs/Next-Steps-and-User-Inputs.md`. Do not send credentials.

## Local Alpaca setup and XRPN evidence — 2026-10-23

- [DONE — SOURCE CHANGE, CI PENDING] Added API `UserSecretsId`, removed empty credential placeholders from tracked `appsettings.json`, documented local User Secrets commands, and standardized Alpaca credential resolution on ASP.NET Core configuration.
- [DONE — TESTS ADDED, CI PENDING] Added `AlpacaMarketDataAdapterTests` for configured credentials/request headers/bar parsing and missing-credentials behavior. These are fixture-based tests and do not contact Alpaca.
- [DONE — EVIDENCE RECEIVED] User supplied the XRPN 2026-10-02 screenshot showing the normal 5M study, Authority Diagnostic V2 and readable target time axis; top-left chart date is explicitly excluded per user clarification. Do not request it again.
- [USER ACTION — NEXT] Create a free Alpaca account, configure credentials locally using `src/MomentumCommandCenterV2.API/README.md`, run a small real API smoke test, and share only sanitized status/errors/timestamps. Do not share credentials. Massive and paid plans remain deferred.
- [OPEN] Check CI/build/test results at the latest branch head after these commits. A source edit or successful prior build does not establish current-head verification.

# Momentum Command Center V3 — Master To-Do

**Canonical location:** `docs/Master-To-Do.md`  
**Reviewed:** 2026-10-23 (Arizona time)  
**Branch:** `feature/opportunity-quality-classification`


## UPC extension-danger example — 2026-10-23

- [PARTIAL — EVIDENCE REVIEWED] Added `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md` for the user's UPC 5M/1M screenshots and event facts. The reported state is 5M `Perm=1`, `PBLOCK=0`, while 1M says `CANDIDATE BLOCKED: EXTENSION DANGER`, `IN-TREND CANDIDATE`, `EXT=2.35`. This is consistent with a valid higher-timeframe permission and a blocked 1M entry; do not weaken the gate.
- [DONE — USER CONFIRMED] Screenshots/event are from 2026-10-22 and Thinkorswim chart timezone is Arizona. This resolves the date/timezone question only; it does not classify the missing intervals or prove study-state parity.
- [OPEN] [PARTIAL] Reviewed `data/StrategyReports_UPC_102226.csv`: this is a strategy report covering 09:37–09:47, not a continuous OHLCV export; it cannot establish the reported 09:16–09:36 missing-bar intervals. Obtain a full bar export or exact timestamp inspection. Missing chart bars do not prove zero volume or no movement; never synthesize them.
- [OPEN] Confirm installed 1M source and `BLK=6` mapping. Study names alone do not prove installed source parity.

## Provider comparison refresh — 2026-10-23

- [DONE — RESEARCH DOCUMENTATION] Expanded `docs/Provider-Comparison.md` to compare free and paid tiers against ultimate MCC requirements. Free tiers are suitable for plumbing/prototyping only; they are not approved as live authority feeds.
- [RECOMMENDED SHORTLIST] Alpaca Algo Trader Plus ($99/mo public list price) versus Massive Stocks Advanced ($199/mo public list price), pending proof of fit, account entitlements, licensing, small-cap/premarket coverage and TOS comparison.
- [OPEN] Run proof-of-fit tests before subscription/provider selection. No provider is selected or connected.

## Status and migration note

This file preserves the prior consolidated To-Do text below, after reviewing all 180 lines. Completed repository tasks are marked in the current status addendum at the end; unresolved ThinkOrSwim/chart validation tasks remain open because source-controlled files do not prove which study is installed in the user's TOS workspace. The prior reference to a Boring Overnight TODO under `/Trading System/` is not present in this repository; the criteria are now documented in `docs/BoringOvernight.md`. The current repository's `docs/Charts/1M/` and `docs/Charts/5M/` studies are the source-controlled references; do not infer that they match the installed study without comparison.

# THIS IS A .MD FILE

# Momentum Command Center V3 — Consolidated Master To-Do
**Updated:** 2026-10-15 (Arizona time)  
**Purpose:** Canonical master list. Consolidates the 2026-10-06 master TODO, the 2026-10-13 Boring Overnight Decision Column TODO, and subsequent documented project status. The dedicated Boring Overnight TODO remains a detailed companion; this master list must be updated whenever that companion changes.

## Status key
- [NOW] Current priority
- [NEXT] Next in sequence
- [HOLD] Deliberately deferred
- [VALIDATED] Substantially validated, not a claim of profitability
- [DONE] Completed and verified
- [UNVERIFIED] Must be checked against repository/current installed artifact

## 1. 5M authority / Permission — [NOW]
- [NOW] Reconstruct the known XRPN 10/2 Permission `1 -> 0` transition at 12:45 Arizona chart time. Known facts already supplied: Permission 0 premarket; Permission 1 plus BUY at 8:25; Permission 0 by 12:45. Do not ask user to rediscover these transitions.
- [EVIDENCE REVIEWED, 2026-10-22] First screenshot (`Screenshot 2026-10-22 161411.png`) showed the normal 5M study and Authority Diagnostic V2 working on morning bars, but not the target transition.
- [TARGET WINDOW CAPTURED — BRANCH INDICATED, 2026-10-22] The follow-up Project Source screenshot (`Screenshot 2026-10-22 161705.png`) shows XRPN 10/2/26 5M around 12:10–12:55 Arizona chart time, with both studies visible. Around the Permission revocation, red diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, and `EOD=0`; the displayed conditions include `VWAP=0`, `EMA9=0`, and `EMA9>20=0`. In Authority Diagnostic V2, `PBLOCK=2` is the persistent-major-failure branch, so the screenshot strongly indicates that branch caused the revocation rather than EOD. Verify the exact ordering/priority in source before changing logic; do not infer a defect from the revocation alone.
- [AUDIT RESULT — 2026-10-22] The follow-up screenshot indicates `PBLOCK=2` (persistent major failure) with `EOD=0` on the revoked bars. `docs/Charts/5M/MainStudy.txt` confirms `majorFailureRaw = price < dailyVWAP AND price < ema9 AND ema9 < ema20 AND ema20 < sma50`; `majorFailurePersistent` requires this condition on `majorFailureBars` bars (default 2). The Permission latch checks EOD first, then persistent major failure, then persistent structural damage, and recovers only when `bullishPermissionRaw` is true. This matches the observed PBLOCK mapping and does not by itself demonstrate a logic defect. Preserve Permission logic; proceed to the 5M→1M handoff audit.
- [DONE — USER EVIDENCE RECEIVED, 2026-10-23] The uploaded XRPN screenshot shows the 5M chart on 2026-10-02 with both the normal 5M study and Authority Diagnostic V2 visible and a readable time axis spanning approximately 12:00–13:35 Arizona; the 1M chart is also dated 2026-10-02 and spans the requested area. Per user clarification, ignore the date displayed on the separate top-left chart. The screenshot covers the requested 12:30–13:00 window. Use this as the evidence artifact for the Permission-revocation audit; do not request the same screenshot again.
- [NEXT] Decide whether the persistent Permission latch semantics match intended trading behavior. Preserve separation of PRAW/current conditions, Permission latch, bullish regime, established BUY thesis, runner state, and execution environment.
- [VALIDATED] `buyEstablished` / `buyEstablishedPrior` protects the first BUY bar from being swallowed by runner states.
- [VALIDATED] Authority Diagnostic V2 exists and mirrors the normal V4.1.7 5M logic without 1M references. Use beside normal 5M study for targeted testing; keep normal historical debug off unless needed.
- [HOLD] No change to Permission logic until a specific defect is demonstrated.

## 2. 5M -> 1M handoff — [NEXT after Permission audit]
- [DONE — SOURCE AUDIT ONLY, 2026-10-22] Compare 5M authority predicates with the 1M reconstructed 5M layer and document concrete raw-Permission, structural-damage, and major-failure differences in `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`. No ThinkScript edits made; installed-study parity and behavioral contract remain open.
- [NEXT] Validate fresh candidate, in-trend candidate, continuation, confirmation, event-armed READY, expiration, and re-entry paths against same-bar installed-study evidence and the intended 5M gate.
- [NEXT] Test whether RUNNER / WANING / SWING suppress legitimate 1M opportunities; source audit alone does not establish behavior.
- Verify extension danger blocks chasing without falsely declaring the 5M thesis dead.
- Preserve architecture: 5M never references 1M; 1M may reference 5M.

## 3. 1M execution pipeline — [HOLD broad edits; audit targeted behavior]
- [UNVERIFIED] Reconcile current user-installed study with Library copies: `V14_4_9_AggressiveSetupHandoff_CompositeBubble` (known compile-clean diagnostic baseline, duplicate files) and `V14_4_10_AggressiveAuditOnly` (later audit-only files, duplicate copies). Filename/modified date alone does not establish production baseline.
- Trace actual transitions and bars before editing; avoid version churn that changes only labels or does not address a demonstrated defect.
- Validate fresh execution candidate, in-trend candidate, continuation candidate, confirmation event, event-armed opportunity, finite READY window, READY event, candidate-block reasons, execution expiration, extension warning/danger, swing reclaim/re-entry.
- Preserve negative test: opportunity can exist while READY is blocked by extension danger. This is not automatically a broken bullish thesis.
- Validate historical debug on scrolled bars; use per-bar bubbles for history rather than relying on right-edge AddLabel values.
- Distinguish `NO NEW SIGNAL` from HOLD or EXIT. Missing signal bubbles are not themselves a SELL signal.

## 4. Runner management — [NEXT after authority/handoff validation]
- Validate RUNNER RISING -> RUNNER; RUNNER -> RUNNER RISING; RUNNER -> RUNNER WANING; WANING -> SWING ZONE; SWING recovery; and WANING/SWING -> actual thesis failure.
- Runner waning is caution, not automatic SELL. Preserve goal of holding valid runners rather than repeatedly taking tiny profits.
- Define and test explicit position-management/exit behavior separately from entry readiness.

## 5. Boring Overnight decision column / chart study — [NOW: specification first]
Detailed companion: `docs/BoringOvernight.md`. Preserve its complete criteria and update it alongside this master.

### Required pipeline
1. Overnight Edge Score (historical overnight profitability/edge).
2. Boringness Score (consistency and controlled risk/return distribution).
3. Event/Catalyst Risk (earnings, M&A, regulatory/government decisions, corporate actions, material news, sector/regime shocks).
4. Current-Day Qualification (abnormal intraday extension, unusual volume, fresh catalyst spike, excessive ON move, deterioration from normal behavior).
5. 5M structure/permission where appropriate.
6. 1M entry timing where appropriate.
7. Decision status: `READY`, `WATCH`, `BLOCKED`, `DATA?`, plus reason codes and data freshness.
8. Zero position is valid; never silently treat missing/stale data as READY.

### Initial gates to preserve pending validation
- Price >= $5.
- 20-day average volume >= 500,000 shares/day.
- Overnight win rate >= 60%.
- 20-day average ON return >= +0.10%.
- 5-day average ON return > 20-day average ON return under current cheat-sheet rule.
- Price above 20-day SMA.
- Not on the maintained Schwab exclusion list.
- No material event/catalyst risk incompatible with boring overnight holding.

### Initial Boringness score (100 points)
- ON win % 25; median ON % 20; 20D average ON % 15; 5D average ON % 10; consistency 10; overnight volatility 10; average volume 5; price 5.
- Bands: 90–100 PERFECT BORING; 80–89 VERY GOOD; 70–79 POSSIBLE; 60–69 AGGRESSIVE; below 60 REJECT / NOT BORING ENOUGH. Score never overrides hard gates.
- Preferred ranges: current ON move <3% (>=3% increases penalty/review); 20D average +0.10% to <2.0%; 5D average positive to <5.0%; consistency >=70% preferred and >=80% excellent; median should be positive and reasonably close to mean.
- Penalize abnormal intraday moves/volume, sharp extension, fresh news/earnings/catalyst, M&A/regulatory/government decisions, corporate actions, sector/regime shocks, deteriorating 5M structure.
- Existing exclusion list includes `SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE`; other maintained lists contain additional exclusions. Reconcile all current sources; do not assume this subset is exhaustive.

### Implementation sequence
- [NOW] Write column specification/data contract: field definitions, windows, session boundaries, source, freshness, missing-data handling, reason codes, extended-hours assumptions.
- Implement only calculations ThinkScript can reliably support; label proxies honestly. Split into coherent columns if one column truncates critical information.
- ThinkScript cannot establish catalyst-clear status reliably or retain full authoritative rolling history. Missing event data => DATA? / manual review, never READY.
- Validate known winners, losers, catalyst spikes, overextended candidates, and all hard-gate failures. Compare same-symbol/same-date ThinkScript results against independently calculated C# results.
- Add chart study only if it explains information not usefully shown by the column.
- Track false positives, missed opportunities, ON win rate, average/median return, adverse excursion, spread/slippage. Do not infer profitability from compilation or a few chart examples.
- No automatic order placement until a separate risk/order-control workflow is specified and tested.

## Extended momentum / continuation lane (user requirement, 2026-10-22)

- [RESEARCH REQUIREMENT] Do not treat a large current-day gain or an EXT warning as an automatic permanent rejection. Preserve the existing risk warning, but evaluate a separate **Extended Momentum / Continuation Watch** path for stocks already up roughly 10–15% when the user begins reviewing them.
- [DESIGN ONLY — NOT A BUY SIGNAL] This path should combine fresh price/volume continuation evidence, catalyst/news context with source and timestamp, tradability/liquidity/spread checks, current 5M Permission, and current 1M execution state. News sentiment or a strong narrative alone must never grant eligibility.
- [SEPARATE STATES] Distinguish (1) extended and not currently executable, (2) continuation remains credible and should be watched, and (3) a fresh, valid entry event exists under the unchanged 5M/1M gates. A continuation candidate can stay on a pullback/reclaim watchlist without being marked EXECUTION READY.
- [SAFETY] Never bypass the exact 1M extension formula or turn red EXT into a green/permission state. Any alternative entry policy needs explicit risk limits, gap/halts/spread safeguards, an auditable reason code, and historical/out-of-sample validation before it can affect actionable ranking.
- [OPEN] Determine whether the existing red In-trend candidate bubble is a diagnostic, setup classification, or actual eligibility state by checking the exact installed ThinkScript and chart examples. Do not claim it means technically safe to buy until its formula and semantics are verified.
- [OPEN] Test whether continuation logic should allow a separate re-entry trigger (for example a defined pullback/reclaim or consolidation breakout) rather than weakening the original EXECUTION READY gate.
- [TIME CONTEXT] User's 08:30 reference is Arizona local time. Arizona generally does not observe US daylight saving time, while US Eastern market hours do; therefore the US market open shifts between 07:30 and 08:30 Arizona time depending on the season. Label dashboard/session times with timezone and market-session context.

## Broker integration boundary (user preference, 2026-10-22)

- [DECISION] Schwab website/manual order entry is the preferred baseline. Do not implement Schwab API access, credential handling, or automated order submission merely for convenience.
- [POSSIBLE FUTURE UX] Consider a clearly labeled Open Schwab / open trade-ticket handoff only if a legitimate, supported URL workflow is verified. Do not imply that it can prefill symbol, quantity, account, or order type until confirmed.
- [NOT APPROVED] Do not implement a one-click Buy Maximum Amount control. It is too easy to submit a mistaken oversized order, and broker-side buying power/margin/order constraints cannot be safely inferred from MCC's local data. If a sizing helper is later requested, make it a non-submitting calculator with explicit custom/max budget choice, visible price and share calculation, reserve/cushion, and a final manual review in Schwab.

## Dashboard feedback and scanner feed decision (2026-10-22)

- [PARTIAL — UI FEEDBACK SLICE] The static dashboard now has an explicit system-readiness panel and a local feedback notebook so the user can review layout and behavior before provider integration. Notes persist only in browser local storage and can be exported as JSON; they are not uploaded automatically.
- [NOW] Use dashboard feedback to prioritize UI changes. Keep demo/unknown labels visible; never make mock data look live.
- [BLOCKED — PROVIDER NOT SELECTED] Thinkorswim is not connected as a live scanner/API feed. It is the source of chart-study behavior and validation evidence. No live scanner provider/feed is selected or verified in repository sources.
- [NEXT] Compare eligible feed/provider options against real-time 1M/5M OHLCV, scanner API coverage, timestamps/session/ETH behavior, historical data, entitlements, licensing, rate limits, and outage/reconnect behavior before implementing an adapter.
- [HOLD] Do not bind the dashboard to an assumed Thinkorswim endpoint or a guessed third-party API.

## Scanner integration readiness and EXT prequalification (2026-10-22)

- [NOW] Define a provider-neutral scanner-event contract before wiring any provider: normalized symbol, scanner group, source/event ID when available, first-seen and observed timestamps, data-as-of timestamps, raw-field provenance, schema/version, freshness, current status/reason codes, and evaluator/study version. Discovery is not an entry signal.
- [NOW] Add extension qualification to the scanner design. The user's reported problem is that red-state 1M EXT / extension-danger often coincides with no EXECUTION READY bubble, wasting chart-review time. Keep this as a measured workflow hypothesis until timestamped examples quantify it.
- [NOW] Default actionable scanner queue must deprioritize/filter candidates whose current extension exceeds the exact versioned 1M study envelope. Preserve them in a recoverable WATCH_PULLBACK list; do not permanently discard a symbol merely because it is extended now. Pullback requalification requires fresh data, a new valid event, and unchanged 5M authority / 1M execution gates.
- [NOW] Distinguish extension warning from extension danger. Never substitute a guessed percentage threshold or the provisional OpportunityQualityAssessor threshold for the exact ThinkScript ATR-extension definition. Missing/stale extension inputs => DATA?, never “not extended” or ACTIONABLE.
- [BLOCKER] Before live scanner gating, complete exact-study indicator parity for ATR/extension and related gates; classify the 31 irregular 1M intervals; finish full-session 1M-to-5M reconciliation; repeat RTH/extended-hours and Arizona/Eastern DST tests. See docs/Validation/MCC-OHLCV-20261020.md.
- [PARTIAL — CONTRACT SCAFFOLD COMMITTED 2026-10-22] Added `ScannerDiscoveryEvent` and `IScannerDiscoverySource` in Application. This defines a provider-neutral DTO/source boundary only; append-only storage, freshness/deduplication validation, qualification workflow, tests, and provider adapter remain open. No build/test result has been verified for this commit.
- [NEXT] Implement extension prequalification only after its inputs and thresholds have parity tests. Verify warning/danger behavior, pullback requalification, stale-data handling, and that scanner filtering never changes the 5M thesis.
- [BLOCKED — PROVIDER DECISION] Choose a provider only after API coverage, licensing, rate limits, latency, credentials, and failure behavior are specified. Data not found in sources: a selected live scanner provider and verified access contract.
- [NEXT] Run shadow mode against timestamped scanner discovery, first 5M Permission, first 1M READY, extension blocks, pullback/re-entry, and outcomes. Measure chart reviews avoided and valid opportunities hidden before enabling hard exclusion by default.
- [HOLD] Do not connect live scanner results to orders or let scanner/quality ranking override 5M authority or 1M READY.

## 6. Scanners / daily workflow — [PARTIAL: local workbench implemented]
- [DONE] Add a local scanner/watchlist workbench with four scanner groups, candidate statuses/reasons, local CSV import, and browser-local watchlist persistence. See `docs/Scanner-and-Watchlist-Workbench.md`.
- [HOLD] Reconnect the Command Center to a live scanner workflow until 5M/1M behavior is sufficiently validated and a provider-neutral scanner contract exists.
- Scanner groups: A Momentum Hunter; B Breakout Trigger; C Reclaim/Continuation; D Market Movers/Discovery (daytime, not Overnight).
- Maintain regular-session default and separate overnight workflow explicitly.
- Daily Gainer Review after 6 PM Arizona time: regular-session biggest intraday gainers only, exclude overnight holds; record scanner appearances/timing, chart states, news/catalysts, Schwab tradability/exclusions, and miss classification.
- Preserve timestamped scanner screenshots and actual trades for V2/V3 validation.
- Keep Schwab-unavailable/undesired securities excluded across relevant scans; reconcile maintained lists rather than replacing them with a partial list.

[NOW] Scanner discovery timestamp and missed-move audit
	• Record the earliest known timestamp each symbol appears in each scanner, including scanner/group name and the scan result at that time.
	• Record when the symbol is first added to the working watchlist, if different from its scanner appearance.
	• Preserve timestamped scanner snapshots or append-only candidate logs so discovery history is not lost when scanner results change.
	• For each candidate, correlate discovery time with:
		○ First 5M Permission=1 and its EXT value.
		○ First 5M BUY / PREPARE TO BUY / runner state.
		○ First 1M EXECUTION READY.
		○ READY expiration and candidate-block reasons, including extension danger.
		○ Subsequent runner waning, pullback, swing recovery, or thesis failure.
	• Calculate the price move already realized between the earliest recorded discovery and first actionable READY, with a clearly defined reference price and session boundary.
	• Classify missed opportunities as discovery too late, qualification too late, execution blocked/expired, no valid setup, data unavailable, or unknown.
	• Track scanner misses and false positives across historical sessions before changing scan thresholds or signal logic.
	• Keep regular-session DGR separate from Boring Overnight tracking.

From <https://chatgpt.com/g/g-p-6ad0f6bcb72c81918e3cdd194cda6630-momentum-command-center-v3/c/6ad0f88f-d89c-83e8-9eeb-9923a03fc1d2> 





## 7. C# V3 implementation — [NEXT / data and replay foundation]
Repository supplied by user: https://github.com/themailcomeshere/MomentumCommandCenterV2.git
- [DONE] Verify the actual repository branch, latest commit, solution/project tree, framework, namespaces, and build/test commands directly against the repository. The working branch is `feature/opportunity-quality-classification`; it targets .NET 8.
- [DONE] `ITradeImporter`, `SchwabTradeImporter.ImportFile()`, and `TradeImportResult` exist in the current solution.
- [PARTIAL] Importer validation and xUnit tests exist. Expand coverage for any uncovered duplicate/idempotency, partial-failure, and diagnostic cases when importer work resumes.
- [NEXT] Persistence, provider-neutral indicator snapshots, decision logging, market-data parity, and later production UI remain future work. This milestone adds raw-bar replay primitives only.
- Keep boundaries clean: raw import -> normalized application/domain models -> persistence -> bar/indicator calculations -> deterministic 5M authority -> 1M execution -> separate overnight decision -> logged outcome/backtest.
- Use pure, testable calculations/state transitions; make data source, time zone, session boundary, missing-data and freshness behavior explicit.
- C# should own/consume authoritative rolling ON stats, median/mean and 5D/20D metrics, consistency/volatility, scores, catalyst metadata, freshness, reason-coded decisions and outcome logs.
- Record candidate snapshots, status/reasons, intended and actual entry/exit, and outcomes for audit and threshold evaluation.
- Preserve `///` XML documentation with flowerboxing. Deliver complete coherent code and next-step instructions together.
- No claims that live brokerage integration, external event feeds, backtesting, automated orders, or production UI exist until verified in source/tests. No automated orders until separately specified and tested.
- Ensure any intentional divergence between C# and TOS behavior is documented and covered by comparison tests.

## 8. Repository README / docs — [PARTIAL]
- [DONE] README and architecture/data contracts have been refreshed from verified repository contents. The original README is preserved at `docs/archive/README_ORIGINAL.md`.
- [PARTIAL] README now documents .NET 8 architecture, replay command, demo UI, safety boundaries, and core roadmap. Provider configuration, scanner interfaces, and deployment workflow remain unimplemented and are not claimed.
- Keep source-of-truth boundaries explicit: current TOS studies are live behavioral reference under validation; C# is intended to make logic/data/decisions reproducible and testable.

## 9. Historical validation / diagnostics — [NEXT]
- Resolve known XRPN 10/2 Permission revocation before collecting new-day evidence for that question.
- Then use controlled sessions to gather transition evidence, not assume production readiness.
- Historical diagnostics must show per-bar state accurately when scrolled; distinguish current/right-edge state from historical bar state.
- [DOC ADDED] Use `docs/Charts/1M/DiagnosticStudy.txt` as the audit procedure for historical 1M READY bubbles. It documents the existing per-bar diagnostics in `MainStudy.txt`; it does not alter signal logic or claim the installed Thinkorswim study matches the repository source.
- Compare scanner candidates, signals, fills, exits, and outcomes; classify false positives, missed trades, invalid entries, exits too early, and missed runner continuation.
- Validate Arizona/Eastern session conversions and EOD logic across DST/standard-time boundaries.

## 10. User interaction / operating rules
- Correctness and thoroughness over expediency. Review all relevant Library items, not just the newest one.
- On screenshot analysis, identify date/session boundary and timeframe; explicitly separate observed facts from inference and unknowns.
- For study changes, use handoff heading “1. What I changed”; explain screenshot purpose, exact Study/Library artifact, user actions, and next steps.
- Do not request screenshots or transitions already provided. Do not misstate current Arizona time or tell user to wait for a future session when already in it.
- Do not create new study versions without a demonstrated defect and explicit test purpose; confirm compilation and chart effect before calling it successful.
- Trading signals are evidence, not guarantees. `EXECUTION READY` does not replace position sizing, risk limits, or exit rules. Distinguish HOLD, EXIT, and NO NEW SIGNAL.

## 11. Immediate order of operations
1. [NOW] Build the current branch and run the full xUnit suite; GitHub file edits do not execute .NET.
2. [NOW] Reconcile the repository's 1M/5M studies with the actually installed ThinkOrSwim versions; do not infer identity from filenames.
3. [NEXT] Define timestamp open/close semantics, session/extended-hours policy, and DST behavior for historical bars.
4. [NEXT] Add sanitized real historical fixtures and golden indicator-parity tests against the source studies.
5. [NEXT] Connect replay frames to V2 signal evaluation without changing existing signal rules; log transitions and opportunity assessments.
6. [NEXT] Investigate the known XRPN 10/2 Permission revocation from existing chart evidence; keep this separate from speculative edits and replay-foundation work.
7. [NEXT] Continue targeted 5M/1M handoff, READY expiration/extension, runner, and exit-behavior validation.
8. [NEXT] Resume scanner-discovery correlation, trade-quality analytics, and V1/V2 historical validation after data parity is trustworthy.
9. [HOLD] Production UI/provider/news integration and any order automation until their contracts and safety requirements are separately specified.

---

## 12. Repository and documentation status update (2026-10-16)

### Done in this branch
- [DONE] Inspect the current branch and latest commits, including separated Models, development rules, chart-study source files, and documentation additions.
- [DONE] Add XML flowerboxing with purpose/when-called context to current classes and methods; a follow-up source audit found no unflowerboxed method signatures in the inspected C# files.
- [DONE] Add raw timestamped OHLCV bar contract and deterministic replay foundation; replay emits frames at 1M timestamps and attaches only 5M bars with timestamp <= the frame timestamp.
- [DONE] Add CSV ingestion with explicit timezone-offset requirement, invariant numeric parsing, and duplicate/malformed OHLCV validation.
- [DONE] Add CLI replay command and synthetic CSV fixture.
- [DONE] Add unit tests for no-look-ahead, duplicate bars, and invalid OHLC.
- [DONE] Add `docs/Market-Data-and-Replay-Contract.md`.
- [DONE] Expand `docs/BoringOvernight.md` with Design vs Use, gates, score components, ranges, catalyst risk, and limitations.
- [DONE] Populate `docs/NewChatHandoff.md`.
- [DONE] Scaffold static front end under `frontend/`; it is demo-only and not connected to market data or a broker.
- [DONE] Put development rules in `docs/rules/abpDevRules.md` (already moved by user).

### Still open / not verified by this branch
- [NOW] Run the local build and all tests after the new replay/adapter/CLI changes; GitHub file operations do not run .NET.
- [DONE] Add XML flowerboxing to the current C# class/type and method declarations identified during the repository audit; continue enforcing this on all new and changed code.
- [NOW] Reconcile the source-controlled chart studies against the actual installed 1M/5M ThinkOrSwim studies; filenames and repository state do not establish runtime version.
- [NEXT] Add sanitized historical 1M/5M fixtures and provenance.
- [NEXT] Define bar timestamp open/close convention, session calendar, extended-hours policy, regular-session VWAP reset, and DST handling.
- [NEXT] Port TOS indicator formulas with golden parity tests before connecting calculated values to the signal engine.
- [NEXT] Connect replay frames to existing V2 signal evaluation without changing signal behavior; persist transitions and opportunity assessments.
- [NEXT] Add daily trading-session grouping, MFE/MAE, hold-time/entry-to-peak/exit-giveback, runner/early-exit/missed-runner analysis, limit-price analysis, scanner events, and V1/V2 historical validation.
- [NEXT] Reconcile all Schwab exclusion lists; the documented list is not guaranteed exhaustive.
- [HOLD] Live market-data provider, news/catalyst feed, production front-end/API, and broker connectivity until the contracts and provider are chosen.
- [OUT OF SCOPE] Automated order placement and autonomous Schwab submission.

### Line-by-line disposition of the prior checklist

- Sections 1–4 (5M permission, 5M→1M handoff, 1M execution, runner management): retained as unresolved TOS validation work. No speculative study edits were made.
- Section 5 (Boring Overnight): strategy criteria migrated into `docs/BoringOvernight.md`; implementation and historical validation remain open. Its former companion path under `/Trading System/` did not exist in this repository.
- Section 6 (scanners/Daily Gainer Review): retained as future scanner-event and discovery-timestamp work; no scanner data source is connected.
- Section 7 (C# V3 implementation): repository was inspected; importer/trade-analysis work already exists in the solution. Market-data/replay is now a first slice, not a complete backtest. Remaining persistence, historical market correlation, event sourcing, and provider work stay open.
- Section 8 (README/docs): README and the replay contract were updated from verified repository content; this master list and handoff now exist under `docs/`.
- Section 9 (historical validation): retained. Replay primitives exist but no source-study parity, fills, or P&L simulation is claimed.
- Section 10 (user interaction/operating rules): retained as operating constraints; development-specific rules are in `docs/rules/abpDevRules.md`.
- Section 11 (immediate order): reprioritized to build/test this milestone first, then TOS version/behavior audit, then data/session/indicator parity, then signal/replay integration, and later trade/scanner analytics. The historical XRPN 10/2 revocation remains a separate unresolved investigation requiring chart evidence; it is not solved by this C# replay slice.
- The old README-derived trade importer and statistics bullets remain relevant background; implemented functionality is described in `README.md`, while market-dependent performance judgments remain unverified until historical bars are available.


- [DOC ADDED] `docs/Research/ClearStreet-Comparison.md` records relevant product patterns (unified decision surface, multi-factor discovery, transition alerts, risk context, API contracts) and explicitly excludes broker infrastructure/automated execution from MCC scope.
- [RULES UPDATED] `docs/rules/thinkscript.md` now reflects MCC's 5M-authority/1M-execution hierarchy, historical-bar semantics, formula-fidelity requirements, and validation gates; generic `AddOrder()` advice no longer implies orders belong in a chart study.

## 13. Boring Overnight source reconciliation

The original master list and `docs/BoringOvernight.md` agree on the initial 100-point weights, hard gates, score bands, preferred ranges, “too hot” penalties, event/catalyst caution, 5M/1M review, and zero-position principle. The exclusions list is explicitly partial pending reconciliation. Overnight Edge and Boringness must remain separate; no final score should override a failed/unknown hard gate.

## 14. Current execution checkpoint (2026-10-18)

- [BLOCKED HERE / STILL OPEN] Attempted to clone the current branch and run `dotnet test MomentumCommandCenterV2.sln`; this environment could not resolve `github.com`, so the clone failed before restore/build/test. This is an environment/network limitation, not a code failure and not a passing test result.
- [NOW] Use the repository's GitHub Actions `.NET build and test` workflow to validate the current branch, or run the documented Release restore/build/test commands locally. Confirm the run corresponds to the current branch head before marking this item DONE.
- [NEXT] Once build/test is green, reconcile the installed Thinkorswim 1M/5M studies against `docs/Charts/1M/MainStudy.txt` and `docs/Charts/5M/MainStudy.txt`; then capture sanitized historical bars with provenance before indicator parity work.
- No signal logic, ThinkScript thresholds, or replay semantics were changed during this checkpoint.

## 15. Opportunity classification: practical verification and news boundary (2026-10-18)

- [DOC ADDED] `docs/Opportunity-Classification-Verification.md` defines a shadow-mode workflow, required input provenance, outcome comparisons, acceptance gates, and the boundary between a news provider, web search, and an LLM.
- [USER-REPORTED PASS] User reports all unit tests pass. This is recorded as user-reported; no local build/test execution was possible in the prior environment attempt. Do not confuse unit-test success with live market-data parity or profitability.
- [NOW] User is handling sanitized historical 1M/5M fixtures and provenance, plus session/timezone definitions.
- [NOW] Implement MCC-owned OHLCV export contract/parser and preserve the legacy `kg_EveryTickSOHLCP` report as a rejected negative fixture. Complete Thinkorswim compilation, timestamp/order-name verification, native-export comparison, and consecutive 1M/5M chart parity before treating any export as production-ready.
- [NEXT] Implement golden indicator-parity tests against exact versioned ThinkScript studies, then connect replay to the existing V2 signal engine without changing rules.
- [NEXT] Build shadow-mode opportunity snapshots and outcome linkage only after timestamp/session/indicator parity. Keep classification diagnostic-only until out-of-sample evaluation demonstrates value over the READY-only baseline.
- [HOLD] News/catalyst provider selection and AI-assisted headline classification until coverage, licensing, latency, costs, provenance, and unknown/stale behavior are specified. Current repository has no verified live-news or per-symbol ChatGPT/Google query integration.

## 16. MCC-owned OHLCV export checkpoint (2026-10-20)

- [DONE] Add the uniquely named `MCC_OHLCV_V1` strategy artifact with explicit date, Eastern-time session, aggregation, actual-volume payload, visible diagnostics, and simulated close/reopen report transport.
- [DONE] Add the strategy-report parser and reject legacy `kg_EveryTickSOHLCP` rows, transport-only reports, malformed OHLC, invalid timestamps, negative volume, duplicates, and symbol/timeframe mismatches.
- [DONE] Validate the repository against successful 1M and 5M report shapes. `StrategyReports_XRPN_102026_933.csv` contains 355 1M payload rows; `StrategyReports_XRPN_102026_5m_offset_1027.csv` contains 81 5M payload rows.
- [DONE] Enforce that `aggregationLabel` matches the actual Thinkorswim chart aggregation; a mismatch must emit no payload rows and show a diagnostic mismatch state.
- [DONE] Add dependency-free PowerShell validation scripts under `tools/` for single-report structure, 1M/5M export comparison, and report-versus-payload timestamp offsets.
- [DONE] Add `tools/New-MccChartParityTemplate.ps1` and generate first-20-row 1M/5M comparison templates under `data/validation/`.
- [DONE] Add a local scanner/watchlist front-end workbench without claiming live provider, news, broker, or order connectivity.
- [DONE] Record automated 1M/5M validation results in `docs/Validation/MCC-OHLCV-20261020.md`: 52 complete 5M windows reconstructed from 1M rows match OHLCV; 29 windows are incomplete; report timestamps are transport metadata.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 1M sample contains 20 consecutive bars (13:28–13:47 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; rows with precision differences are labeled `ROUNDING ONLY`. See `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv`.
- [DONE — USER-SUPPLIED PARITY FILE] The `_UPDATED_3.csv` 5M sample contains 20 consecutive bars (13:20–14:55 UTC), with chart values and `Finding` populated on every row. All rows are classified `MATCH`; precision differences are labeled `ROUNDING ONLY` where applicable. See `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Constituent 1M-to-5M reconstruction is independently possible only for the overlapping complete 1M windows in the supplied 1M sample; the full-session reconciliation remains open.
- [VALIDATED] The `_UPDATED_3.csv` 1M and 5M files provide 20 consecutive payload rows each and now include independent chart OHLCV values and row-level findings. This completes the requested manual chart sample for those two windows; it does not by itself classify the separate 31 gaps in the broader 1M export.
- [DONE — SAMPLE PARITY] Independent chart OHLCV values are supplied for 20 consecutive 1M bars and 20 consecutive 5M bars in the `_UPDATED_3.csv` files. The broader 1M export's 31 gaps remain unclassified and require separate evidence.
- [VALIDATED — OVERLAP CHECK] The three complete shared 5M windows beginning 13:30, 13:35, and 13:40 UTC reproduce the 5M payload OHLCV exactly when their five constituent 1M payload bars are aggregated. The 13:45 window is not fully testable from this 1M sample because 13:48 and 13:49 are outside its range. Details: `docs/Validation/MCC-OHLCV-20261020.md`.
- [DONE — CLARIFIED] The raw Strategy Reports remain payload-only and are not completed parity evidence. The completed manual comparison evidence is in `data/validation/XRPN_1m_parity_template - XRPN_1m_parity_template_UPDATED_3.csv` and `data/validation/XRPN_5m_parity_template - XRPN_5m_parity_template_UPDATED_3.csv`. Do not revert to the original templates or treat `_UPDATED_2.csv` as the latest completed sample.
- [USER-CONFIRMED DONE LOCALLY — 2026-10-22; REPOSITORY EVIDENCE SYNC PENDING] User reports that all 31 irregular 1M intervals were reviewed and classified locally, full-session 1M-to-5M reconciliation was completed, and RTH/extended-hours plus Arizona/Eastern DST checks were completed; observed outcomes were MATCH or ROUNDING ONLY. Do not ask the user to repeat these tests. Detailed local row-level results/settings/provenance are not in the current repo; see `docs/Validation/MCC-OHLCV-20261020.md`. If auditability is needed, sync the completed artifacts/manifest rather than redoing the work.
- [NEXT] Reconcile the user-confirmed local results into repository provenance: exact files and hashes where available, chart/study version, date/timezone, RTH/ETH settings, DST boundary dates, row/window counts, and result summary. Do not fabricate missing metadata.
- [NEXT] Investigate native Thinkorswim chart-data export as the preferred transport if it exposes the same timestamp/OHLCV fields without strategy-order timing semantics.
- [HOLD] Do not mark the export production-ready, connect it to replay as authoritative market data, or calibrate signal/opportunity logic until the external chart-parity items are complete.


## Morning Report / Morning Routine / Dashboard workflow (2026-10-22)

- [NOW — PRODUCT WORKFLOW] User wants to sit down around 08:30 Arizona time and see a dated Morning Report with an automatically populated/re-ranked watchlist, top 1–4 ranked candidate “Red Zone” multi-pane, and a Boring Overnight decision.
- [DOC ADDED] `docs/User-Manual.md` is the user manual and workflow specification. It distinguishes existing prototype behavior from planned functionality and documents launch, data readiness, watchlist, Red Zone, Boring Overnight, continuation, manual Schwab, and feedback flows.
- [IMPORTANT — NOT IMPLEMENTED] Current Dashboard is static HTML/CSS/JS opened via `frontend/index.html` in a browser, not an .exe. It uses demo fixtures/local CSV and browser-local watchlist/feedback. No live feed/scanner, automatic ranking, Morning Report generator, live Red Zone, live Boring Overnight calculator, C# engine wiring, or broker integration is present.
- [SOURCE AUDIT — REPOSITORY ONLY] `docs/Charts/1M/MainStudy.txt` defines `extensionATR = AbsValue(price - dailyVWAP) / ATR(14)`, warning at 1.50 ATR and danger at 2.00 ATR by default; bullish extension danger is price above Daily VWAP and at/above danger threshold. `inTrendExecutionCandidate = executionCandidate AND NOT freshTrigger`; its event is transition/re-arm gated. Final READY separately requires an active execution window, 5M execution permission, and no bullish extension danger. This establishes repository source semantics, not that the installed Thinkorswim study matches.
- [NOW] Continue audit of the exact 1M/5M installed-study parity and the bubble's real chart examples before any rule changes. Do not reinterpret IN-TREND CANDIDATE as READY or weaken extension thresholds without validation.
- [DONE — SPECIFICATION] Define provider-neutral ingestion, persistence, freshness, recovery, report snapshot, and validation requirements in `docs/Overnight-Collection-Contract.md`.
- [BLOCKED — PROVIDER / ENTITLEMENT] Choose and verify a supported market-data/scanner provider and licensing before implementing its adapter, exact freshness thresholds, and backfill/reconnect behavior.
- [NEXT] Implement append-only event/observation persistence, idempotent ingestion, health monitoring, recovery tests, deterministic report generation, ranking, Red Zone, and Boring Overnight wiring after provider selection.
- [NEXT] Decide desktop delivery only after app integration architecture is known. Browser launch is current reality; an .exe is not built or required yet.
- [USER HISTORY GAP] Repository search and available project handoff do not contain the full prior Morning Routine conversation. The manual documents a proposed workflow from known requirements, but the user should supply or point to the prior routine if exact historical steps/metrics must be restored.


## Overnight scanner / persistent Morning Report requirement (2026-10-22)

User wants to leave the released/deployed MCC program open overnight so scanner discoveries/data are collected before the 08:30 Arizona Morning Report. Do not mark this satisfied by an open browser tab or a static preview.

- [x] Add preview-only Morning Command Brief and responsive ranked 1–4 Red Zone layout to the static UI; include explicit “overnight collection NOT RUNNING” status and JSON report-preview export.
- [ ] Select a supported/licensed scanner and market-data provider; document actual APIs, entitlements, timestamp/session conventions, rate limits, and terms. Provider is currently unselected.
- [x] Write the provider-neutral collection contract in `docs/Overnight-Collection-Contract.md`; it deliberately leaves provider-dependent thresholds unspecified.
- [ ] Implement a long-running collection service that works independently of the browser tab; handle startup, reconnect, rate limits, outages, and graceful shutdown.
- [ ] Persist raw discovery events and timestamped market observations with source/event IDs, source and receive times, session/timezone, schema/version, raw fields, and ingestion disposition.
- [ ] Implement collector health states RUNNING / DEGRADED / STOPPED / UNKNOWN, last-success timestamp, freshness checks, gap/duplicate detection, and no synthetic bars.
- [ ] Implement recovery tests for machine sleep/wake, network/provider outage, authentication expiry, replayed/duplicate events, session boundaries, and process restart.
- [ ] Implement deterministic, versioned Morning Report generation from persisted data; include provenance/freshness per candidate and exportable report snapshots.
- [ ] Connect the live service to scanner discovery, deduplication, automatic re-ranking, continuation watch, and the Red Zone; show only up to four ranked candidates and never treat rank as permission.
- [ ] Wire Boring Overnight calculations and hard gates to persisted, validated inputs; unknown catalyst/data remains DATA? / manual review.
- [ ] Define packaging and startup: browser/local service vs desktop app/installer. Do not claim .exe/background collection exists until built, deployed, and tested.
- [ ] Keep provider secrets out of frontend, localStorage, repository, and logs.
- [x] Verify the latest branch-head .NET workflow after the UI changes; head `bcb1ef5027859cd4448daa08191df83e0e6eb871`, workflow run 91, conclusion `success` (checked 2026-10-23 UTC).
- [ ] Run a browser-based end-to-end smoke test of the Morning Command Brief and JSON export; structural checks are not a browser test.


## Morning Routine recovery, provider comparison, and collection scaffold (2026-10-23)

- [x] Read the full Project source file Morning.txt (992 lines) and consolidate its non-redundant requirements in docs/Morning-Report-and-Routine.md. This is a curated requirements spec, not a claim the linked original chat was fully recovered.
- [x] Document provider candidates and public plan tradeoffs in docs/Provider-Comparison.md. Preliminary evaluation pair: Alpaca Algo Trader Plus vs Massive Stocks Advanced; no provider selected and no account entitlements verified.
- [x] Implement an initial provider-neutral persistence scaffold: IScannerDiscoveryEventStore, JSONL append-only adapter, batch ingestion validation, idempotency key, receipt timestamp, and tests for restart deduplication/corrupt-log failure.
- [x] Add a first append-only raw market-bar observation store with source/receive timestamps, OHLCV validation, content-hash idempotency, and append-only correction links. Implemented in API; tests cover restart deduplication and corrections. Session-aware gap classification and corruption/retention policy remain open.
- [ ] Add API-hosted collector health state and a cancellable long-running streaming/polling worker. A polling-worker scaffold exists but is not wired to a real provider source; Alpaca/Massive historical REST adapters now exist. Live stream binding, recovery/backfill, and operational health remain open.
- [x] Verify .NET build/tests for the API host, provider adapters, raw-bar store, and store tests. Latest verified run 151: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012211560. Browser and live-provider tests remain open.
- [ ] Browser smoke test the Morning Brief and top-right Red Zone layout; user has manually confirmed the current visual look, but latest layout change still needs browser confirmation.
- [ ] Validate source differences against installed Thinkorswim studies using the exact user procedure in docs/Validation/5M-to-1M-User-Validation-Steps.md (to be created); do not ask user to repeat prior OHLCV tests already confirmed done.
- [ ] Resolve provider decision only after entitlement, scanner coverage, session behavior, historical backfill, retention/licensing, and representative-symbol comparison.


### Status correction — follow-up work after the prior checkpoint (2026-10-23)

The earlier workflow success recorded above was for its then-current branch head only. It does **not** establish CI status for the newer persistence scaffold and upper-right Red Zone commits.

- [x] Consolidate the supplied Morning.txt excerpts into docs/Morning-Report-and-Routine.md.
- [x] Add provider evidence and proof-of-fit checklist in docs/Provider-Comparison.md.
- [x] Add the provider-neutral JSONL discovery-event store and batch-ingestion validation scaffold; tests are added but their latest CI result must be checked.
- [x] Place Morning Brief left and Red Zone upper-right in the responsive wide-screen command workspace. User browser confirmation is still needed for this specific layout change.
- [x] Create docs/Validation/5M-to-1M-User-Validation-Steps.md with the exact installed-study evidence needed from the user.
- [x] Update the User Manual, Scanner Workbench, and New Chat Handoff.
- [ ] Verify .NET build/test workflow against the latest branch head after all changes.
- [ ] Browser-test the new top-right layout and export interaction.
- [ ] After provider selection, extend persistence to market bars and implement cancellable collector, health reporting, reconnection/backfill, and restart/sleep recovery.


### Collector lifecycle scaffold extension (2026-10-23)

- [x] Add ScannerDiscoveryPollingWorker with cancellation, retry-on-next-poll behavior, and RUNNING / DEGRADED / STOPPED / UNKNOWN health snapshots.
- [x] Add an offline test proving a successful empty poll is recorded as zero observations and cancellation stops the worker.
- [ ] Add explicit degraded-source/recovery test coverage and sleep/wake/backfill reconciliation after provider semantics are known.
- [ ] Wire the worker into a real host and implement a provider adapter; no configured source exists, so the worker is not currently running.


## Provider comparison UI / UPC CSV follow-up — 2026-10-23

- [DONE — STATIC UI ONLY] Added parallel Alpaca and Massive Stocks comparison cards to `frontend/index.html`, styled in `frontend/styles.css`, with a local comparison checklist action in `frontend/app.js`. User can set symbol, aggregation, and date; it marks both feeds NOT CONNECTED and shows required comparison fields. The button does not contact vendors and is not a live integration.
- [DONE — CSV REVIEW] Reviewed `data/StrategyReports_UPC_102226.csv`. It records four strategy/order events between 09:37 and 09:47 Arizona time and embedded isolated 1M OHLCV payloads. It is not a full OHLCV time series, so it cannot determine whether the reported earlier candles were absent.
- [RESOLVED] User confirms UPC screenshots/event date 2026-10-22 and Thinkorswim chart timezone Arizona.
- [OPEN] Obtain continuous 1M/5M UPC OHLCV or verify exact bar timestamps in TOS for 09:16–09:36. Do not fill missing bars.
- [OPEN] Implement real backend adapters for both vendors only after credentials and entitlement/licensing decisions; keep secrets out of frontend JS. Compare source timestamps, bars, quotes, missing intervals, reconnect and backfill.


## API host, REST adapters, and Morning Brief integration (2026-10-23)

- [DONE — IMPLEMENTED, NOT YET BUILD-VERIFIED] Added `src/MomentumCommandCenterV2.API` as an ASP.NET Core .NET 8 project and included it in `MomentumCommandCenterV2.sln`. It serves the existing `frontend/` workbench when run from the repository layout and exposes health/provider-status endpoints.
- [DONE — REST HISTORY ADAPTERS, NOT LIVE-VALIDATED] Added server-side Alpaca historical 1M/5M bars adapter and Massive historical aggregate adapter. Credentials are read from configuration/environment variables, never from frontend JavaScript. No API credentials were provided; no live provider request was performed.
- [DONE — FIRST COMPARISON ENDPOINT] Added `GET /api/market-data/compare` to request the same symbol/timeframe/window from both providers and report bar counts, first/latest bars, duplicate timestamps, and observed timestamp gaps. Gap count does not imply zero trading or a provider defect. Websocket, scanner/movers, quotes, pagination, durable bar storage, reconnect/backfill, and market-session-aware gap classification remain open.
- [PARTIAL — MORNING BRIEF] Added `GET /api/morning-brief` for SPY/QQQ/IWM 5M observation snapshots from configured providers, and connected the dashboard to provider status, the comparison endpoint, and Morning Brief. Sectors, breadth, movers, catalysts/news, validated market regime, Red Zone scoring, historical expectations, and Boring Overnight remain explicitly DATA? / NOT CALCULATED.
- [OPEN — REQUIRED BEFORE TRADING RELIANCE] Build/test the latest branch head, run live API requests with user's licensed credentials, verify provider entitlements and extended-hours coverage, compare both feeds against TOS, and test session/gap semantics. These commits are not a claim of build success, CI success, live-data parity, or production readiness.
- [OPEN] Add durable raw OHLCV persistence with source/receive timestamps, late/corrected bar handling, and restart recovery.
- [OPEN] Add websocket streaming, health state, reconnection and historical backfill before considering real-time operation.
- [OPEN] Continuous UPC OHLCV remains needed only to resolve whether the specific 09:16–09:36 chart intervals were truly missing. It is not a blocker to implementing provider adapters.


### CI verification — API adapters and dashboard wiring (2026-10-23)

- [VERIFIED] GitHub Actions run 147 at commit `ae8180866def759612fa7c1a515dc93586143056` completed successfully: solution restore, Release build, and test suite all passed. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011426478
- This validates compilation and existing automated tests only. It does not validate live provider credentials, API entitlements, response behavior against real accounts, browser rendering, data parity, or readiness for live trading.


### Raw market-bar persistence slice — 2026-10-23 [BUILD/TEST VERIFIED]

- [IMPLEMENTED — CI PENDING FOR LATEST COMMIT] Added `IMarketBarObservationStore` and `JsonlMarketBarObservationStore` to the API host. Provider-comparison requests append valid raw OHLCV observations to JSONL outside the repository by default (LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl); override with `MCC_MARKET_BAR_STORE_PATH`.
- Identical observations are idempotent by provider/symbol/source timestamp/content hash. Changed values append a new observation with a `SupersedesObservationId`; prior records are not overwritten. Records preserve source timestamp, receive timestamp, provider/feed and raw OHLCV.
- [VERIFIED — TESTS PASS, 2026-10-23] Added `tests/MomentumCommandCenterV2.Tests/JsonlMarketBarObservationStoreTests.cs` for idempotency across store restart and append-only correction history. GitHub Actions run 148 at commit `c702afa21fac9dfa5a31b18463cc4429aa4adc71` passed restore, Release build, and tests: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011865804.
- [OPEN] Add corruption recovery policy, disk/retention monitoring, session-aware gap semantics, and ensure write failures surface as a clear API health condition.


### Morning Brief and raw-bar persistence verification status

- Morning Brief now records observed SPY/QQQ/IWM 5M bars into the same append-only store and displays the observed first-to-last window change when data is available. These are observations, not a validated market-regime classification.
- The provider comparison UI shows newly persisted observation counts. REST history only; no WebSocket/live collector.


- [LATEST HEAD VERIFIED] GitHub Actions run 151 at `ae6ee7acc30e85a6268b5ea8fc40d86429d3461b` completed successfully for restore, Release build, and tests. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012059701. No live provider, browser, or entitlement validation is implied.


## Canonical dependency/status correction — 2026-10-23

Use `docs/Next-Steps-and-User-Inputs.md` as the single checklist for user actions versus work that can proceed without accounts.

- **Accounts:** no paid subscriptions are needed for the next development milestone. Free accounts are optional for live API smoke tests. One provider is enough to begin; both are useful for a same-window comparison. Do not select or pay for a production plan until proof-of-fit, account entitlements, license rights, premarket/small-cap coverage and TOS parity have been checked.
- **API:** the ASP.NET Core host, Alpaca/Massive historical REST adapters, comparison endpoint, partial Morning Brief and append-only JSONL raw-bar store are implemented. Latest CI run 151 passed restore, Release build and tests. No live provider calls, browser end-to-end test or license verification has been performed.
- **Offline work that remains actionable now:** adapter pagination/error/retry tests; hosted collector lifecycle and health; stream abstraction; recovery/backfill design and tests; store corruption/retention/error handling; fixture-driven Morning Brief/scanner/Boring Overnight calculations; session-aware gap semantics; ThinkScript source parity tests.
- **User evidence still required:** XRPN 5M screenshot listed in `docs/Validation/5M-to-1M-User-Validation-Steps.md`; UPC continuous bars or exact timestamp inspection only if the historical 09:16–09:36 gap question still needs resolution; review the API-hosted browser layout. These are not prerequisites for offline infrastructure work.
- **Morning Brief scope:** currently benchmark observation/freshness only. Broad movers, sector/breadth, sourced news/catalysts, validated regime, Red Zone qualification and live Boring Overnight remain unimplemented and must not be displayed as known.

## Export and opportunity-quality review — 2026-10-26

**Audit:** `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`

- [DONE — REPOSITORY DATA AUDIT] Found an aligned XRPN 1M/5M OHLCV pair for 2026-10-19: `data/StrategyReports_XRPN_102026_933.csv` (355 1M payload rows) and `data/StrategyReports_XRPN_102026_5m_offset_1027.csv` (81 5M payload rows).
- [DONE — DATA PROVENANCE] Corrected the October 22 1M report filename to `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`; it is not the October 2 1M pair. Do not pair it with the actual 2026-10-02 5M file.
- [PARTIAL — INTERNAL OHLCV CHECK] In the October 19 post-09:00 Arizona overlap, 27 complete 5M windows have all five 1M payload rows and their aggregated OHLCV matches the corresponding 5M payload. Thirty minute timestamps from 09:00–12:59 are absent from the 1M file; missing rows remain gaps, not synthesized bars.
- [BLOCKED — DIAGNOSTIC STATES] OHLCV payloads do not contain internal ThinkScript Permission, candidate, extension, READY, re-arm, or expiration states. READY-frequency root cause cannot be attributed from OHLCV alone. Existing October 2 screenshot remains available; do not ask user to re-upload it.
- [BLOCKED — OVERNIGHT WINDOW] Audited bars do not cover the full 07:00–20:00 Eastern GTC_EXT eligible window. `eligible-window-high-excursions.csv` previously contained placeholder values 1–20; those rows were removed, leaving a header-only input with zero observations. No target-touch rate or alternative limit recommendation may be derived from it.
- [AUDIT — OPPORTUNITY QUALITY] `OpportunityQualityAssessor` and unit tests exist, but the assessor is not wired to replay, V2SignalEngine, CLI, API, or UI. Required metrics are not derived from raw OHLCV. Keep provisional thresholds diagnostic-only; presentation must distinguish opportunity quality from the actual ThinkScript EXECUTION READY event.
- [NEXT — NO USER INPUT REQUIRED] Keep the October 19 pair as a coverage fixture; add regression checks for filename/header/payload date consistency where the ingestion workflow has file-level provenance; keep incomplete intervals explicit.
- [NEXT — USER/THINKORSWIM EVIDENCE] To diagnose a particular READY gate, obtain diagnostic states for the same symbol/date/bars as the OHLCV. For Boring Overnight, use a full eligible-window export with source-backed regular close; otherwise mark DATA_GAP.
- [NO PROMOTION] These exports are not proof of production timestamp semantics, indicator parity, fill probability, or strategy profitability.

## Opportunity-quality label safety and branch validation — 2026-10-26

- [DONE — SOURCE CHANGE, CI NOT YET CONFIRMED] Changed display labels from `READY — HIGH/MODERATE/BORING/EXTENDED` to `QUALITY — HIGH/MODERATE/LOW` and `TIMING — EXTENDED`. The labels now avoid conflating this diagnostic classification with the existing Thinkorswim `EXECUTION READY` event. Machine-readable enum values and eligibility precedence are unchanged.
- [DONE — TEST SOURCE UPDATED, CI NOT YET CONFIRMED] Updated the high-quality label assertion and added four negative-metric cases covering negative RVOL, ATR extension, recent range, and resistance distance; invalid metrics should fail closed to `DATA_UNKNOWN`.
- [OPEN — CI VERIFICATION] GitHub combined commit status returned no status records for the latest branch commit at audit time. Do not call the current branch build/test green until a workflow run tied to the latest commit is confirmed.
- [INTEGRATION CAUTION] Repository compare reports `feature/opportunity-quality-classification` as diverged from `master` (268 commits ahead and 152 behind in the compare result). Do not merge this branch wholesale into master without a deliberate file/history reconciliation. The changes in this work session remain on the feature branch.

## October upload uncertainty assessment — 2026-10-26 (supersedes prior preliminary note)

- [DONE — DATA PROVENANCE] Inspected the newly uploaded `StrategyReports_XRPN_10022026_1M_EntireDay_2.csv` by report header and embedded payload. It is actually **HPK 1M**, October 2, with 214 rows from 06:30–12:59 Arizona. Renamed it to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv` and removed the misleading XRPN-named path.
- [DATA QUALITY] Those 214 rows match all corresponding October 2 rows in the existing HPK multi-day file exactly. The upload is a duplicate/subset, not new XRPN data. The October 2 XRPN 5M file still has no verified same-symbol XRPN 1M partner.
- [PARTIAL — CHART PARITY] Existing October 19 parity worksheets have chart-entered values for 20 bars per timeframe: 1M has 10 MATCH and 10 ROUNDING ONLY; 5M has 9 MATCH and 11 ROUNDING ONLY. 48 1M and 13 5M rows remain unfilled. This supports sampled OHLCV transport mapping but not full chart/session parity.
- [PARTIAL — TIMESTAMP SEMANTICS] All 40 manually entered chart labels match the local clock digits represented by the payload timestamps. The worksheet records `ChartTimestampMeaning=MATCH`, not BAR_OPEN/BAR_CLOSE/decision availability; the timestamp convention is not fully documented.
- [BLOCKED — GAP CLASSIFICATION] October 2 HPK 1M has 214 of 390 expected minute timestamps, leaving 176 absent. October 19 XRPN has 30 absent timestamps in the 09:00–12:59 interval. Do not infer that missing rows are no-trade periods or chart gaps.
- [BLOCKED — READY ROOT CAUSE] The parity worksheets contain OHLCV chart values, not per-bar ThinkScript READY/Permission/candidate/extension/re-arm states. The October 2 XRPN diagnostic question still needs XRPN 1M and same-bar diagnostic state.
- [BLOCKED — BORING OVERNIGHT] No complete 07:00–20:00 Eastern GTC_EXT window or actual-fill dataset has been found. `eligible-window-high-excursions.csv` now contains only the header; its fabricated values were removed. It has zero verified observations.
- [CI] GitHub Actions run `40379797275` for commit `e7e59d4` completed successfully (restore, build, test). Documentation commits made after that run trigger newer runs; do not treat the current branch head as green until its own run completes.
- [INTEGRATION CAUTION] Keep branch-divergence caution in force; do not merge this feature branch wholesale into master.
- Detailed uncertainty table: `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md`.

## Launch-week release gates — 2026-10-26

Use `docs/Release-Readiness-2026-10-26.md` as the concise launch-gate summary. This is a prioritization aid, not a declaration of release approval.

- [DONE — DOCS] Added explicit release-readiness assessment separating local prototype use from live-trading reliance.
- [DONE — CONTRACT CLARITY] Clarified in `docs/MCC-OHLCV-Export-Workflow.md` that the normalized CSV contract expects completed-bar availability timestamps, but raw `GetTime()[1]` semantics remain unverified until chart parity proves the mapping.
- [NEXT — NO USER INPUT] Add automated report provenance checks for filename/header/payload symbol and date consistency; add importer regression coverage for duplicate, missing, malformed, and partial-window rows.
- [NEXT — NO USER INPUT] Reconcile duplicated and legacy To-Do material; preserve one canonical Master To-Do and link domain-specific specifications rather than creating competing checklists.
- [BLOCKED — USER/THINKORSWIM] Obtain actual XRPN 1M for October 2 if that exact event remains the target; the new file was HPK and duplicates existing HPK rows.
- [BLOCKED — USER/THINKORSWIM] Obtain same-bar 1M/5M gate-state diagnostics for READY-frequency attribution.
- [BLOCKED — DATA SOURCE] Obtain full GTC_EXT eligible-window bars plus source-backed regular close for target reach research; actual fills are a separate data source.
- [CI STATUS] Latest confirmed feature-branch head `c1895641` passed workflow `40380631692`; the release-readiness note was then updated, so recheck the workflow for the resulting newest head before promotion.


## Follow-up update — 2026-10-26 (XRPN pair, Overnight Scanner, Alpaca)

- [DONE — OHLCV PAIR] data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv contains XRPN 1M data for 2026-10-02: 391 consecutive timestamps from 06:30–13:00 Arizona. Against data/StrategyReports_XRPN_10022026_5M_EntireDay.csv, 78 complete 5M windows aggregate exactly from the 1M rows. The same-symbol OHLCV pair is now available.
- [BLOCKED — READY ROOT CAUSE] The pair's strategy report payloads contain only OHLCV transport rows, not per-bar Permission, block code, candidate/trigger, extension danger, READY activation/expiration, re-arm, or expiration diagnostics. Data not found in sources. Need the diagnostic exports or exact repository path if they were uploaded separately. Do not attribute READY rarity from OHLCV alone.
- [NEXT — USER LOCAL ACTION] Run the documented Alpaca smoke test in src/MomentumCommandCenterV2.API/README.md and docs/Next-Steps-and-User-Inputs.md. Saved Alpaca credentials are intended for this test. Report sanitized API response/errors and timestamps only. User reports 87/87 tests pass; this is not live-provider verification.
- [NEXT — BORING OVERNIGHT] Initial chart-review shortlist from the 2026-10-26 scanner screenshot: TTE, RTO, NNN, OKE, AHR, FMS; CCI secondary. Request same 5M chart window across symbols with ETH enabled: 2026-10-26 regular session through 2026-10-27 20:00 Eastern. Full quantitative research still requires source-backed close and full next GTC_EXT window OHLCV with DATA_GAP intervals preserved.
- [RELEASE POSTURE] Local prototype / diagnostic only; no live-decision reliance until provider authentication/entitlements, timestamp semantics, persistence/recovery, installed-study parity and overnight research gates are closed. Do not merge the diverged feature branch wholesale.

## [Uncertainties]

- Study diagnostic exports are not present in the reviewed October 2 OHLCV files.
- Alpaca credentials are user-reported as saved; live auth is not yet verified.
- Screenshot-based overnight shortlist is provisional; catalyst/news clearance and full data coverage are unavailable.


## Correction and verification update — 2026-10-26

- [CORRECTED] Prior chart screenshot request for 2026-10-26–10-28 was invalid. User is providing AHR, FMS, NNN charts for completed historical dates 2026-10-21–10-23. Accept those for visual triage; do not ask them to retake based on the invalid request.
- [PASS — API HEALTH] /api/health reports OK, decision-support-only; order submission is not implemented.
- [PASS — TESTS, USER-REPORTED] Release test command succeeded: 87 passed, 0 failed, 0 skipped.
- [BLOCKED — LIVE PROVIDER] Both Alpaca and Massive report NOT_CONFIGURED. Compare endpoint returned zero bars and zero persisted observations. Alpaca error identifies missing MarketData:Alpaca:ApiKey and ApiSecret or ALPACA_API_KEY / ALPACA_API_SECRET. User's saved credentials are not available to this API process under its current configuration.
- [NEXT — LOCAL ACTION] API UserSecretsId is MomentumCommandCenterV2-API-Local. Run the two dotnet user-secrets set commands documented in NewChatHandoff.md and Next-Steps-and-User-Inputs.md, restart API, verify provider status, and rerun compare. Never send credentials in chat. Massive can remain unconfigured.
- [OPEN — XRPN] 1M/5M OHLCV alignment verified for 78 complete windows; per-bar Permission/block/candidate/extension-danger/READY/re-arm/expiration diagnostics remain missing from reviewed reports.

## [Uncertainties]

- Existing credential storage location is not known; project-scoped User Secrets should resolve this if credentials are already owned and valid.
- Screenshots are visual evidence only; full-window OHLCV and actual fill outcomes remain unverified.


## Final Overnight review and Visual Studio launch update — 2026-10-26

- [PARTIAL — VISUAL REVIEW ONLY] Inspected Project Sources screenshots for AHR, FMS, NNN. Their visible chart headers show 2026-10-19–2026-10-21, not the intended 2026-10-21–2026-10-23 sample. Do not label these as the requested date range. Visual-only observations and limits are recorded in docs/NewChatHandoff.md.
- [BLOCKED — DATE-CORRECT OVERNIGHT ANALYSIS] The intended Oct 21–23 screenshots were not identified in the inspected files. Data not found in sources for that exact window in this review. If they exist under different Project Sources names, identify the exact files; otherwise the correct-date visual review remains open. Do not ask the user to repeat already completed work without checking for the correct files first.
- [BLOCKED — TARGET RESEARCH] Screenshots do not establish full subsequent 07:00–20:00 Eastern GTC_EXT bars, target touch/reach percentages, or actual fills. Need source-backed prior regular close and full eligible window for each completed session, with missing bars preserved as DATA_GAP.
- [NEXT — USER / VISUAL STUDIO] User reports Alpaca secrets were saved successfully and the API is launched from Visual Studio. Stop/restart the API in Visual Studio so it reloads configuration; verify launch environment is Development and the API project's UserSecretsId is MomentumCommandCenterV2-API-Local. Recheck /api/providers/status and /api/market-data/compare on the same localhost process. A configured flag alone is insufficient; actual bars and source timestamps are required.
- [KEEP BLOCKED] Do not call today's Overnight qualified or optimize the +0.5% target from the inspected screenshots. The current target is a heuristic; validated touch-rate research and actual fills remain unavailable.


## 2026-10-26 — Overnight screenshot correction, TTE bubbles, Alpaca status fix

- [ACCEPTED — USER DATE CLARIFICATION] User confirms the three existing AHR/FMS/NNN screenshots are for 2026-10-21–2026-10-23. Previous assistant date challenge was incorrect; do not request the images again. Complete the visual analysis using the user's stated window while preserving screenshot-only limitations.
- [NEXT — 1M DISPLAY DIAGNOSTIC] TTE 1M chart has no visible bubbles, while FMS has many. The Library V14.5 source defaults `showStateBubbles=yes` but only draws bubbles for explicit transitions/events; no generic per-bar state bubble is present and the inspected bubble section does not plot a dedicated GET OUT/EOD bubble. Verify TTE's lower-pane 1M study input `showStateBubbles` and exact study/version against FMS before changing code. Missing bubbles alone do not mean HOLD or SELL.
- [PASS — ALPACA HISTORICAL SMOKE TEST] User's Visual Studio API returned 5 SPY 5M IEX bars and persisted 5 observations for 2026-10-23 13:30Z–13:50Z, zero duplicate timestamps, zero reported gaps, no error. This validates a successful historical request for that window only. Massive remains optional/unconfigured.
- [CODE CHANGE — PROVIDER STATUS] `/api/providers/status` was hardcoded to return `CONFIGURED_NOT_YET_VALIDATED` whenever credentials existed, even after a successful compare. Updated the interface/adapters and endpoint to expose last successful non-empty data validation and return `VALIDATED_DATA_RETURNED` when appropriate. Added a unit test that an empty response does not validate. Changes are on `feature/opportunity-quality-classification`; current-head CI and tests for these edits still need verification.
- [USER ACTION — AFTER UPDATED BUILD] Restart API in Visual Studio and repeat the SPY compare request; verify `/api/providers/status` changes to `VALIDATED_DATA_RETURNED`. If not, verify Visual Studio is running the updated feature-branch code and restarted process.


## 2026-10-26 — completed review work and next release priorities

- [DONE — VISUAL TRIAGE] Reviewed the user's three AHR/FMS/NNN chart screenshots for the user-confirmed 2026-10-21–2026-10-23 sample; no re-upload required. Detailed observations and limitations: docs/Validation/Overnight-Candidate-Review-20261026.md.
- [DONE — NEW CANDIDATE TRIAGE] Evaluated CCI and NICE scanner fields and chart screenshots. Both are WATCH / NOT READY; neither score overrides chart conditions, missing catalyst clearance, or unverified daily average volume. NICE's displayed 5D and AVG ON values both round to 0.70%, so the strict 5D > 20D gate requires unrounded values.
- [DONE — SOURCE FIX] Added a transition-only red EOD GET OUT bubble to docs/Charts/1M/MainStudy.txt. The state was already labeled GET OUT but EOD_GET_OUT was not included in the bubble event list. This is display-only; no entry/permission logic changed. [USER ACTION] Install the updated 1M study in Thinkorswim and verify the EOD bubble; this cannot be done from GitHub.
- [PASS — CI AT PRIOR HEAD] GitHub Actions .NET build/test succeeded on 4f5ed265e54e9a924935a58b8b3233b31b264279 (run 40414827751); user reports 88 tests passed. Check CI again after subsequent source/doc commits. ThinkScript is not compiled by the .NET workflow.
- [PASS — ALPACA HISTORICAL SMOKE TEST] SPY 5M IEX returned 5 bars/5 persisted observations, zero duplicate timestamps/gaps, no error; provider status now reports VALIDATED_DATA_RETURNED after successful non-empty fetch. This is limited to that request and process lifetime.
- [OPEN — CCI/NICE HARD GATES] Verify exact unrounded 5D > 20D average, 20D average daily volume >=500K, freshness, exclusion list and source-backed catalyst review. Until then keep both WATCH.
- [OPEN — QUANTITATIVE OVERNIGHT RESEARCH] Need source-backed prior regular close plus complete next 07:00–20:00 Eastern GTC_EXT bars; preserve missing intervals as DATA_GAP. Historical touch is not fill. No target optimization or READY claim from screenshots alone.
- [NOW — RELEASE READINESS] Prioritize current-head CI; install/verify 1M EOD bubble; finish market-data pagination/retry/error handling and session-aware gaps; collector lifecycle/restart/backfill/durable checkpoints; storage failure/rotation tests; fixture-driven Morning Brief/scanner/Boring Overnight; current-head parity/diagnostic review. Do not merge the diverged feature branch wholesale into master.


- [DONE — INITIAL PUBLIC CATALYST CHECK] CCI Q3 earnings/outlook release confirmed for Oct 21 after close; include recent post-earnings risk in manual review. NICE official IR event page lists no upcoming events, but full current-news/catalyst clearance remains OPEN. Sources and details: docs/Validation/Overnight-Candidate-Review-20261026.md.


## MCC readiness sprint — 2026-10-27

- [IMPLEMENTED — CI PENDING] Added Swagger/OpenAPI (`/swagger`) to the ASP.NET Core API and documented local launch / smoke-test flow in `docs/API-Swagger-Workflow.md`. Use the actual host/port printed by ASP.NET Core. Swagger is a test interface, not evidence of provider entitlement or production readiness.
- [IMPLEMENTED — CI PENDING] Added `POST /api/boring-overnight/evaluate` and `BoringOvernightEvaluator`. The evaluator applies deterministic hard gates: data freshness, excluded-list state, price > $5 and > SMA20, 20D average daily volume >= 500,000, ON win rate >= 60%, 20D average ON >= +0.10%, 5D average ON strictly greater than 20D average, catalyst clearance, 5M permission + bullish structure, and no 1M SELL/GET OUT/BLOCK state. Missing/unknown critical inputs return DATA?; hard-gate failures return BLOCKED; heat/extension warnings return WATCH. It does not discover candidates, source inputs, calculate a validated limit target, or submit orders.
- [ADDED TESTS — CI PENDING] Added tests for unknown catalyst/permission, volume-gate failure and extension warning.
- [DONE — RESEARCH POLICY] Added `docs/Validation/Boring-Overnight-Target-Policy-20261027.md`. Keep +0.5% as a comparison heuristic; do not call median ON % a 50% limit-touch/fill probability. Complete GTC_EXT windows and walk-forward target-touch research remain required.
- [OPEN — THINKSCRIPT ARTIFACT MISMATCH] User reports no bubbles after applying the prior fix, with no compile error. The repository `docs/Charts/1M/MainStudy.txt` is V14.4.10, while the uploaded local reference is `MomentumCommandCenterV3_2_1M_V14_5.txt`; these are not proven to be the same source. Prior bubble change did not resolve the installed chart. Do not claim fixed; identify and patch the exact installed V14.5 source, then verify a historical EOD transition bubble in TOS.
- [OPEN — CURRENT-HEAD CI] Run/inspect Release build and tests for the exact latest branch head after these changes; do not reuse prior green workflow status.
- [OPEN — END-TO-END INTEGRATION] Wire evaluator inputs to provider-backed and persisted scanner/catalyst/indicator data with timestamps/provenance. Until then, endpoint is a deterministic policy test harness, not an autonomous nightly workflow.


- [PATCH ARTIFACT CREATED — TOS USER VERIFICATION REQUIRED] Generated `/mnt/data/MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt` from the uploaded V14.5 source (not the repository's older V14.4.10 MainStudy). It adds a single bubble on transition into `EOD_GET_OUT`. Install this exact source in the lower 1M study and visually verify on a chart spanning the transition. Do not mark complete until verified; local artifact has not been committed as a canonical repository source yet.


## API usability / StrategyReport / exclusion watchlist — 2026-10-27

- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added `POST /api/strategy-report/upload` for multipart CSV upload, 10 MB limit, 10,000 data-row limit, quoted-field parsing, row-width warnings, full parsed rows plus 100-row preview, and likely timestamp/symbol/event column detection. It preserves what the report contains; it does not reconstruct continuous OHLCV or infer fills absent from the file. Parser unit tests added.
- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added local persisted MCC exclusion list at `GET/POST/DELETE /api/watchlists/exclusions`. Boring Overnight evaluation now derives the excluded flag from this list instead of trusting the request field. Store lives under API `App_Data/exclusions-watchlist.json`; ensure this directory is writable and protected by local deployment policy.
- [LIMITATION] This is MCC's local list, not a direct Thinkorswim watchlist integration. Data not found in sources for an official TOS API that lets MCC read or mutate the user's personal watchlists. If TOS watchlist membership must be authoritative, add a documented export/import workflow or supported connector after verifying the actual TOS export format.
- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added example-oriented summaries/descriptions for health, provider status, market data compare, Morning Brief, Boring Overnight evaluation, CSV upload and exclusion watchlist endpoints. Swagger enabled regardless of Development environment so local Release runs can use `/swagger`; do not expose unauthenticated Swagger/API publicly.
- [OPEN] Verify current-head workflow run 40538766766, then test CSV upload with a real StrategyReport export, add/remove exclusion symbol, and confirm the evaluator blocks it.
- [OPEN] Add an MCC frontend upload control and exclusion-watchlist management panel after API contracts are verified.
- [OPEN] Test JSONL persistence count semantics and continue Alpaca pagination/retry, collector recovery, and source-backed scanner wiring.
- [OPEN — THINKSCRIPT] Rebuilt the V14.5 file as a cleaned single-statement-per-line source, commented the wrapped prose fragments that caused invalid statements, and added the EOD transition bubble. This is a locally generated artifact, not yet compile-verified by Thinkorswim; user must test the exact file.


- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Added API-backed frontend panels for MCC exclusions and StrategyReport CSV upload. Exclusions can be added/removed and refreshed from the persisted API list. StrategyReport upload renders row/column summary, warnings, first 100-row preview and offers full parsed JSON download. Existing "Working candidates" browser-local watchlist remains separate and must not be confused with exclusions.
- [CI INVESTIGATION] Workflow run 40538986271 failed because `WithOpenApi` was missing from the .NET 8 API project's references. Added `Microsoft.AspNetCore.OpenApi` 8.0.0 and re-triggered build. Newer workflow 40539484300 is in progress at latest UI/style commit; verify its result before calling the current head green.


## 2026-10-27 follow-up — parity conversion and Alpaca ingestion

- [IMPLEMENTED — CI PENDING] StrategyReport parser now detects embedded `MCC_OHLCV|v1|...` payloads and transforms them into the exact 17-column Chart Parity Template shape: SourceRow, Symbol, Timeframe, PayloadTimestampUtc, PayloadOpen/High/Low/Close/Volume, ChartTimestamp, ChartOpen/High/Low/Close/Volume, ChartTimestampMeaning, Finding. The dashboard can download the converted parity CSV. It only creates rows from explicit MCC payloads; ordinary StrategyReport rows are preserved but are not guessed into OHLCV.
- [IMPLEMENTED — CI PENDING] Alpaca adapter supports next_page_token pagination, up to 100 pages (safety stop), deduplication by timestamp, and up to 3 attempts for transient network/429/5xx/timeout responses. Bounds timestamps to requested UTC range. This is not streaming; it is bounded historical REST retrieval.
- [IMPLEMENTED — CI PENDING] Added `GET /api/market-data/bars` for one provider with explicit timeframe/window, normalized bars, feed, source timestamps, gap diagnostics, and newly-persisted observation count. Maximum 31-day request. Extended-hours availability still depends on feed, entitlement, symbol, and returned data; gaps must not be interpreted as no trades without session-aware logic.
- [IMPORTANT] Current-head CI is queued/in progress; earlier runs failed at compile because the StrategyReport result contract did not yet include ParityRows. The contract has been corrected and a parity conversion regression test added. Do not call the latest code green until the newest run passes.
- [OPEN] Full scanner/Boring Overnight cannot be declared live yet: Alpaca historical bars do not provide the Thinkorswim study's 5M permission / 1M execution state, current news/catalysts, official exclusion-list sync, or source-defined overnight metric definitions. Build an explicit data contract and source each missing input; return DATA? until complete. Do not substitute a lookalike indicator for authoritative TOS state.
- [OPEN] Background collector lifecycle/recovery, persisted cursor/watermark, backfill scheduling, retention, session-aware gap detection, and health/status telemetry remain unimplemented. Current /api/market-data/bars is request-driven collection, not a background collector.


- [IMPLEMENTED — CI PENDING] Added opt-in `MarketDataCollectionWorker`. Set `MarketData:Collector:Symbols` to a comma/semicolon-separated list (for example `SPY,QQQ,TTE,FMS`) to poll overlapping 1m/5m historical windows. Default interval 60 seconds, default lookback 20 minutes; bounded config ranges. Append-only storage deduplicates repeats and the overlapping lookback recovers recent bars after restarts. `GET /api/market-data/collector/status` reports enabled state, configured symbols, cycles, new observations, errors, and timestamps. Disabled when no symbols are configured. This is not a market-wide scanner or websocket feed.
- [OPEN] Validate worker under a real configured Alpaca key, restart API and confirm recent bars are backfilled without duplicate persistence; test 429/5xx retry and API cancellation.


- [IMPLEMENTED ARTIFACT — TOS VERIFICATION PENDING] Created `V14_5_Bubble_Stack_Replacement.txt`: one composite bubble per 1M bar with all simultaneous event names, including EOD transition, to eliminate overlapping separate bubbles. Apply to the current cleaned V14.5 study only; the Library source is stale. Thinkorswim compile and visual validation remain open.
- [CI FIX APPLIED — RECHECK REQUIRED] Removed the unused collector status constructor parameter that failed current-head build. Verify latest workflow before pulling.

## 2026-10-27 — current-head implementation audit and recovery/parity fixes

### Implemented in this checkpoint (CI still must be verified)
- StrategyReport parser corrected for actual Thinkorswim export structure: report preamble (title, Symbol, Work Time), semicolon-delimited order table, embedded MCC_OHLCV v1 payload, comma-grouped Unix-millisecond timestamps, and Windows CRLF line endings. Explicit payload rows are projected to the exact 17 canonical parity-template columns. Chart-side fields remain blank unless present in source; order price is never substituted for OHLCV.
- Added regression coverage for real-format semicolon StrategyReport conversion and for reports with no explicit payload, where parity rows must not be fabricated.
- Collector now persists per-provider/symbol/timeframe source-timestamp watermarks to collector-watermarks.json (override with MCC_MARKET_COLLECTOR_CHECKPOINT_PATH). It resumes from the watermark minus an overlap, writes checkpoints atomically after successful non-empty retrieval/persistence, and catches up long outages in bounded 30-day chunks.
- Collector's newlyPersistedObservations counter increments only by the store's returned append count. An identical already-seen bar returning zero is an idempotent no-op, not a persistence failure.
- Inspected user-fixed commit 0caf00004a837e269c175f7d4c34827120ac8fe6: it adds Swashbuckle.AspNetCore.SwaggerGen and Xunit imports. Prior handoff records workflow 40541586544 green for that SHA; this does not validate newer edits.

### Still open — do not overstate live readiness
- Current-head build/test status must be checked after commits 1c0b54e, bf62d3b, c7acc54, d1af047, and a69771c. Green CI on an earlier SHA does not validate this head.
- Collector recovery is bounded by provider REST limits (30-day chunks); very long outages catch up over repeated cycles. Test restart, checkpoint corruption, filesystem permissions, transient failures, duplicate bars, corrections and cancellation against a configured provider.
- Alpaca pagination/retries are already present. No websocket/streaming ingestion is implemented. Extended-hours coverage depends on feed entitlement and actual returned bars; validate with known premarket, RTH, postmarket and next-session samples.
- /api/market-data/bars reports timestamp gaps, not session-aware missing-bar truth. Closed sessions, halts and no-trade intervals must not be classified as data failures without session-aware rules.
- Provider-backed scanner and Boring Overnight remain incomplete. The evaluator still needs sourced daily/overnight metrics, timestamped catalyst clearance, authoritative 5M Permission/structure, 1M execution state, and freshness/exclusion inputs. Historical OHLCV alone cannot reconstruct TOS states or a news verdict. Missing inputs must remain DATA?.

### PR state
- Current open-PR search returned no open pull requests. PR #7 is closed and must remain unmerged. User reports cleaning up other branches/PRs; no merge action is required unless a fresh query finds an open PR.

### User actions after this update
1. Verify latest workflow for the exact feature branch head, then run dotnet test MomentumCommandCenterV2.sln --configuration Release. Record exact tested SHA and pass/fail/skip counts.
2. Upload a committed real-format StrategyReport through Swagger POST /api/strategy-report/upload. Verify parityRows is non-empty, exactly 17 columns match the canonical template, and timestamp/OHLCV values are correct. Do not substitute order execution price for OHLCV.
3. Configure MarketData:Collector:Symbols locally (e.g. SPY,QQQ,TTE,FMS). Inspect GET /api/market-data/collector/status before and after stopping/restarting the API; verify watermarks persist, advance, and identical bars do not inflate new-write counts.
4. Validate one completed symbol/date across premarket, regular session and postmarket using the configured Alpaca feed; compare returned timestamps to a source export. Do not mark coverage complete from one SPY window.
5. Keep missing catalyst/TOS-state/daily metrics as DATA? until a real source is wired and validated.

- [CORRECTION — PARITY TIMESTAMP SEMANTICS] The StrategyReport `Date/Time` column is an order-event timestamp and must not be copied into `ChartTimestamp`. The parser now leaves `ChartTimestamp` blank unless the source explicitly labels a chart timestamp, and records why in `ChartTimestampMeaning` / `Finding`. The regression test asserts this separation.

## 2026-10-27 — user validation findings and immediate fixes

- User's Release test run failed 2 StrategyReport parser tests because the new fixtures encoded literal backslash-n characters instead of newline characters. Fixed both fixtures to use actual C# newline escapes and fixed parser line-ending normalization before header-offset slicing. Relevant commits: e494810, 3e8f9c4, f617743. This is a code fix only; tests have NOT yet been rerun in this environment.
- User's collector status proves the configured Alpaca historical REST adapter did return data in the running process: configured provider Alpaca, 1 completed cycle, 69 newly persisted observations, zero cycle errors, watermarks across SPY/QQQ/TTE/FMS 1m/5m. This is evidence of successful REST retrieval/persistence for that cycle, not full session coverage or streaming.
- Dashboard provider card was misleading/stale: static HTML defaults to NOT CONNECTED and the prior JS only updated connection text, leaving latest observation/coverage/recovery at DATA NOT LOADED/Not measured/Not run. Updated frontend/app.js to populate current process-local provider validation, last validation time/count, configured Alpaca feed, and collector cycle/new-write/error summary from API endpoints. If API fetch fails, card explicitly says API NOT CONNECTED (static preview or API unavailable).
- Extended-hours validation procedure: use a completed market day and compare raw returned timestamps to an independent TOS/export source. For 2026-10-26, the Eastern 07:00–20:00 ET window maps to 2026-10-26T11:00:00Z through 2026-10-27T00:00:00Z. Example endpoint: /api/market-data/bars?symbol=SPY&timeframe=1m&from=2026-10-26T11%3A00%3A00Z&to=2026-10-27T00%3A00%3A00Z&providerName=Alpaca. Repeat for 5m and at least one individual equity such as TTE; inspect feed, barCount, first/latest source timestamps, gaps, and bar payload. Confirm the configured feed/entitlement and compare with TOS data before claiming coverage.
- The exact current-head workflow remains unverified by the GitHub connector's workflow/status wrappers; empty status arrays are not evidence of a green build. User must rerun dotnet test MomentumCommandCenterV2.sln --configuration Release after pulling latest feature branch, and verify Actions for that exact SHA.
- [PARSER FOLLOW-UP] The embedded payload test's Unix-millisecond epoch `1792430880000` is `2026-10-19T17:28:00Z` (verified), not 13:28Z. Corrected the expected timestamp. Grouped payload volume such as `8,210` is now normalized to `8210`. Commits `dab2bc8` and `ba5c92f`. These are source/test corrections; current-head Release test result is still pending.

## October 27, 2026 — latest evidence and current blockers

- [USER EVIDENCE RECEIVED] At 2026-10-27 17:53–18:26 UTC, `/api/providers/status` reported Alpaca `VALIDATED_DATA_RETURNED`, configured feed `iex`, and successful non-empty responses (3 bars, later 2 bars). This proves recent REST requests returned data, not complete session coverage or SIP entitlement.
- [USER EVIDENCE RECEIVED] Collector snapshot reports 2 cycles, 36 newly persisted observations, 0 errors in the reported last cycle, and durable per-symbol/timeframe watermarks. Restart/recovery still needs a controlled test.
- [PARTIAL — DATA REVIEW] Saved October 26 IEX samples report SPY 1m: 394 bars, 8 observed gaps; TTE 1m: 168 bars, 91 observed gaps; TTE 5m: 75 bars, 3 observed gaps. These are not enough to call the provider inaccurate: compare exact timestamps and OHLCV against the matching Thinkorswim export and distinguish exchange/session inactivity from missing observations.
- [SOURCE CHANGE] Added `POST /api/market-data/import-csv` to convert a conventional Alpaca/IEX OHLCV CSV into the canonical 17-column parity schema. It fills Payload* only; Chart* and ChartTimestamp remain blank until an independent chart export is supplied. Added parser tests; latest CI must verify the new endpoint and tests.
- [SOURCE CHANGE] Provider status fetches use `cache: no-store`; provider labels are clearer and the dashboard script URL is versioned to reduce stale-JavaScript issues. If the dashboard remains static, verify the browser's address is the same API host/origin and hard reload after pulling the branch.
- [OPEN — CRITICAL FOR SCANNER] The hosted collector only polls explicitly configured symbols (SPY, QQQ, TTE, FMS). There is no market-wide movers/discovery source, news/catalyst feed, quote/spread data, or validated candidate scoring. Do not present these four configured symbols as a market-wide scan. Scanner work needs a licensed discovery feed or a user-supplied candidate universe plus explicit stale-data and liquidity checks.
- [OPEN — CANDIDATE REVIEW] Overall overnight scan screenshot is available in this conversation. Six candidate chart images exist in the repository folder `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026`, but this session could not retrieve those PNG bytes through the connected repository reader. No candidate shortlist or limit-sell prices are claimed without chart evidence, actual entry assumptions, and risk levels.
- [OPEN — SOURCE FORMAT] The repository currently contains the saved API response text for SPY/TTE coverage and an XRPN Thinkorswim StrategyReport CSV; Data not found in sources for the claimed matching TTE Thinkorswim StrategyExport file. Upload the exact TTE StrategyExport and the six charts directly into the conversation to complete the comparison.
- [USER ACTION] Pull the latest `feature/opportunity-quality-classification` branch, run Release tests, test the dashboard on the API-served same origin, use Swagger to test both StrategyReport upload and Alpaca CSV import, and upload the TTE chart-export pair plus six candidate chart screenshots here.

- [SOURCE CHANGE — CI PENDING] Added `GET /api/scanner/universe`: bounded 1–50-symbol historical scan returning bar count, first/latest timestamps, latest-bar age, observed window change, observed range, and summed volume. It is explicitly user-universe-only, not market-wide discovery, and does not generate READY/BUY labels. This provides a transparent scan primitive while a licensed market-wide movers source remains unresolved.
- [DATA NOT FOUND IN SOURCES] `data/HowToTestExtendedHoursCoverage_5m.txt` exists but is empty. The SPY 5-minute response is not available in the repository and must be recaptured; do not infer SPY 5m coverage from SPY 1m.

- [DATA REVIEWED — INTERNAL CONSISTENCY PASS] Compared the 75 TTE 5m bars to aggregation of the saved TTE 1m bars in the same UTC five-minute buckets. All 75 available 5m bars matched the aggregate OHLCV values from the corresponding 1m rows; 0 mismatches observed. This is a consistency check between the saved IEX responses, not independent parity against Thinkorswim.

- [SOURCE CHANGE — CURRENT-HEAD CI PENDING] Connected the Scanner Workbench to `GET /api/scanner/universe` with symbols, timeframe, and local-time window controls. Results are descriptive historical data only. Latest verified .NET CI run passed at commit `7eb26118f360ba0d9cc0b2dbc274ca346bc99240`; later frontend/docs commits are running their own workflows and current-head status must be checked before release.


## 2026-10-27 follow-up — dashboard, import, parity, solution visibility

- Fixed a JavaScript syntax error in the StrategyReport upload handler: a literal `\\n` had been written between statements. This parse error prevented the browser from executing the entire `app.js`, which explains why provider status stayed at “NOT CONNECTED / DATA NOT LOADED” and the scanner submit appeared to do nothing. Updated the script URL to force a fresh download: `app.js?v=20261027-upload-js-fix`.
- Added an explicit Swagger OpenAPI multipart schema to `POST /api/market-data/import-csv`: required binary `file`, optional `symbol`, and optional `timeframe` (1m/5m). Without this, Swagger generated a request body with no upload fields even though the endpoint code accepted multipart form data.
- Added solution folders `Documentation` and `Data and Validation Fixtures` to `MomentumCommandCenterV2.sln`, with the files under `docs/` and `data/` listed as Solution Items. They are reference/test assets, not separate executable .NET projects; keeping them as solution items is the appropriate fit.
- Inspected the saved TTE StrategyReport parse result (77 explicit payload bars) against the saved IEX 5m response (75 bars). 74 timestamps overlap; 3 payload timestamps are absent from IEX; all 74 overlapping bars have at least one OHLCV mismatch. Median absolute close difference is about $0.02, maximum $0.23. Total volume over overlapping timestamps is 705,686.6302 in payload vs 45,441 in IEX (about 15.53x). This is a material parity failure; do not treat IEX-only as interchangeable with the Thinkorswim chart feed. Full method/results: `docs/Validation/Alpaca-IEX-vs-Thinkorswim-TTE-20261026.md`.
- Reviewed six chart screenshots in `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026/`. None shows a clean overnight entry at the captured end-of-day state: BNC has 2.68 ATR extension and no 1m execution environment; RTO has 5m permission but the 1m state is EOD GET OUT / no swing-buy environment; MFM, SXC, SAP, and PANW show 1m/5m GET OUT or permission NO and/or substantial extension. No buy shortlist is justified from these end-of-day snapshots. RTO is the strongest *watch-only* chart of this set, not an entry recommendation; price is around $22.89, with nearby chart reference around $22.96 and session high $23.23, but a limit-sell cannot be set responsibly without a valid entry, next-session setup, and risk/invalidation plan.
- The user's listed red-X Actions runs were older commits that failed at build with CS0173 nullable conditional errors in the scanner endpoint. Those errors were fixed; subsequent workflows were green through the branch head observed before these latest dashboard/Swagger/solution changes. The new commits must complete CI before release.
- Collector recovery UI: Provider comparison > Alpaca card > “Gap / recovery test” displays collector cycles, new observations, and last-cycle errors. It explicitly says restart recovery still needs a controlled test. To verify persistence, capture `/api/market-data/collector/status` before restart, restart the API, then check the same endpoint again: watermarks should remain at/near the previous source timestamps and advance after the next successful cycle. Counters may reset on process restart; the checkpoint file and watermark values are the persistence evidence.


## Update — 2026-10-27: parity import, scanner freshness, and overnight chart re-review

### CSV import failure: root cause found and code changed
- User's 400 response was reproducible from the checked-in fixture shape: `data/TTE_ExportAttempt.csv` is **JSON content**, not delimited CSV. It is an Alpaca historical-bars response with root metadata (`symbol`, `timeframe`, `provider`, `feed`) and a `bars` array. The existing parser searched only for a delimited OHLCV header, so the 400 was expected for this mislabeled JSON export.
- Updated `AlpacaCsvParityParser` to accept both ordinary comma-delimited OHLCV CSV and Alpaca historical-bars JSON responses; added a fixture-based JSON parsing test; clarified the Swagger description. JSON still populates only Payload* values; chart values remain blank until a separate chart export is supplied.
- Relevant commits: parser `3e9f831ba42759657342d78f010b871fda9fa25a`, test `2e95de4b4a8decbd4e02aabab28a684042ddd722`, Swagger description `952215474ad7d8c5144402df1c08b40f67cfc9b6`. These are source commits, not proof of passing tests; CI for the latest head must be checked.
- Endpoint separation: use `POST /api/market-data/import-csv` for Alpaca OHLCV CSV or Alpaca bars JSON. Use `POST /api/strategy-report/upload` for Thinkorswim StrategyReport CSV (the report format begins with `Strategy report` and has semicolon-delimited trade columns). Do not send a StrategyReport to the Alpaca OHLCV parser.
- Follow-up: rename the JSON fixture to `.json` in a later cleanup and update the solution item path, or store an actual CSV fixture with a truthful extension. Current endpoint accepts only a `.csv` filename even when its content is JSON; this compatibility choice is temporary and confusing.

### Scanner output and collector
- User retested scanner: FMS/TTE/QQQ/SPY status is `OBSERVED`, with reported freshness 138.39/138.39/83.39/133.39 minutes. Treat all as stale historical observations, not current candidates. Score alone must never turn these rows into trade-ready decisions. Need an explicit max-freshness gate and a visible `STALE` / `NOT ACTIONABLE` result when exceeded.
- The response itself explains that relative-volume baseline, quote/spread, catalyst, halt, sector, and Thinkorswim eligibility are absent. These are required gaps for market-wide discovery/overnight qualification, not just UI polish.
- Collector status before/after restart shows watermark values unchanged, counters resetting from cycles=6 to cycles=1, newly persisted observations=6, lastCycleErrors=0, recentErrors empty. This is consistent with process restart and continued successful polling. It is not a fully isolated proof that the checkpoint file was read from disk. Verify the exact checkpoint file exists, record its last-write time/content before restart, restart API, compare persisted JSON and endpoint watermarks, then wait for a new eligible bar and confirm watermark advancement. Do not expect source timestamps to advance while the provider returns no newer bars.

### Overnight screenshots: corrected interpretation
- Added `docs/Validation/Overnight-Chart-ReReview-20261027.md` and linked it under the solution's Documentation items.
- RTO 5M is constructive (permission YES, bullish structure/regime, ~0.43 ATR extension, RVOL ~5.16), but its newer 1M panel explicitly ends with GET OUT / 5M permission NO / ~5.7 ATR extension and a large last impulse bar with blocked/expired execution labels. Treat it as a conditional watchlist candidate, not an entry-ready buy. The mismatch is timeframe-specific; neither blindly discard 5M nor override 1M.
- ATR extension is not an automatic veto because the strategy prefers “boring.” It is a risk/chase penalty. Here RTO's 1M extension is extreme and the latest 1M state does not confirm entry.
- MFM: top 5M runner/permission positive but low displayed RVOL (~0.13); lower 1M permission NO, ~2.67 ATR extension and GET OUT. BNC: 5M extension ~2.68 ATR and lower 1M ~10.24 ATR/GET OUT. SXC: 5M permission NO after a high-RVOL spike/fade. SAP: bearish/permission NO/~3.87 ATR. PANW: 5M transitioning/~3.35 ATR, lower 1M permission NO/~5.37 ATR. No screenshot alone qualifies a Boring Overnight BUY.
- Screenshots are historical captures, not current market state. Boring Overnight still requires independent historical overnight stats, daily liquidity, exclusions, fresh prices, and catalyst/news clearance. Data not found in sources for a complete current gate check.

### Solution/project loadability
- Reviewed `MomentumCommandCenterV2.sln`: Documentation and Data and Validation Fixtures are standard Visual Studio Solution Folder entries, not executable projects. Their paths are relative to the solution root and the project-type GUID is the standard Solution Folder GUID. The latest checked-in solution references existing documentation/data files from the repository listing.
- GitHub CI is the authoritative repository build check available here; it does not reproduce a user's Visual Studio local SDK/IDE state. If Visual Studio still shows actual `.csproj` entries as “incompatible” or “cannot load,” capture the exact project names and the first Output > Solution/Project load error. Do not remove valid project references or retarget frameworks without that error. Verify installed .NET 8 SDK and Visual Studio version locally.

### Next actions
1. Pull `feature/opportunity-quality-classification`, stop old API processes, restart the API, and upload `data/TTE_ExportAttempt.csv` to `/api/market-data/import-csv`. Expected: 75 parity rows, Symbol TTE, Timeframe 5m, payload values populated, chart values blank. This expectation follows from the checked-in JSON fixture's reported barCount; the endpoint response must be confirmed by user.
2. Upload an actual Thinkorswim report to `/api/strategy-report/upload`, not the Alpaca import endpoint.
3. Confirm CI status on the latest commit; run `dotnet test MomentumCommandCenterV2.sln --configuration Release` locally.
4. Make stale-data a hard scanner gate before market-wide discovery work; then add universe discovery, quote/spread/liquidity checks, catalyst/news with timestamps, halt/sector context, exclusion integration, and independently validated overnight scoring.
5. For RTO, do not place an overnight order based on these snapshots. Re-evaluate only with fresh 5M/1M data and all Boring Overnight hard gates.



## 2026-10-27 — Scan & Analyze and Arizona-local time entry

### Implemented on `feature/opportunity-quality-classification`
- Renamed the scanner action to **Scan & Analyze (1M + 5M)**. The form now retrieves `GET /api/market-data/bars` twice for each supplied symbol, once with `timeframe=1m` and once with `timeframe=5m`, using the existing Alpaca provider selection. Calls are bounded to 1–25 unique symbols per run and run in batches of three symbols to avoid launching an unbounded burst of requests.
- Each symbol's row reports both timeframe bar counts, first-to-last close change, observed high-low range relative to the first close, summed volume, latest source timestamp and latest-bar age. Per-timeframe errors and empty results remain visible. The results explicitly say `OBSERVED · NOT QUALIFIED` when both timeframes return bars, or `INSUFFICIENT DATA` when either timeframe has no bars.
- The UI accepts and labels start/end as **Arizona local time** and converts those wall-clock values to UTC ISO timestamps before calling the API. Conversion uses `Intl.DateTimeFormat` with `America/Phoenix`, not the workstation's local timezone and not a hard-coded UTC offset. The summary reports both the entered Arizona values and converted UTC boundaries.
- Updated the browser script cache-buster so the API-served dashboard loads the new implementation.
- Commits: `cb6da905c5c9a385c24a7cf4a52e6e662f3a865a` (form labels/identity) and `f2baaa28355eb68e19b300dc501049474f842cbf` (dual-timeframe implementation). These commits are source changes, not proof of a passing CI run or local browser test.

### Contract clarification
- `GET /api/market-data/bars` is the correct existing source for normalized Alpaca OHLCV bars; it returns bars and metadata including provider/feed, requested UTC window, source timestamps, gaps, and persistence count. It does **not** return the internal Thinkorswim 5M Permission or 1M EXECUTION READY state.
- Scan & Analyze is a first integrated data-gathering and descriptive-analysis step. It is not yet the full Boring Overnight qualification pipeline. It does not yet calculate validated relative volume, ATR extension parity, overnight historical win rate/median/5D/20D metrics, quote spread, market-wide discovery, timestamped catalyst clearance, halt/sector context, or verified TOS states. Missing inputs must not be interpreted as passes.
- Freshness is displayed as age; this change deliberately does not invent a universal stale threshold. Qualification needs a documented policy that distinguishes intraday entry freshness from completed-session overnight research.
- The date/time controls are wall-clock values in `America/Phoenix`; API parameters and persisted provider timestamps remain UTC. Verify this on the user's Windows browser because source edits alone do not execute JavaScript.

### User verification sequence
1. Pull `feature/opportunity-quality-classification`; confirm the branch and commit.
2. Run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
3. Start the API and open its hosted dashboard (not `file://` preview).
4. Enter a small universe (e.g. `SPY,QQQ,TTE,FMS`), select a window with known bars, and confirm one row per symbol includes both 1M and 5M summaries.
5. Check a known Arizona time manually: on October 27, 2026, Phoenix is UTC−07:00, so Arizona 09:30 should be sent as 16:30Z. Confirm the status text shows the entered local time and the exact UTC values.
6. Try a symbol with no data or invalid provider conditions and confirm the result is `INSUFFICIENT DATA` or displays a request error rather than claiming qualification.
7. Verify CI for the exact resulting branch head before calling the change released.

### Remaining user input / decisions
- Define the intended historical session window for Boring Overnight (including the chosen after-hours boundary and treatment of incomplete current sessions). The current form defaults to the last six hours in Arizona local time; change this default only after the desired workflow is agreed.
- Confirm the symbol source for each run: manual comma-separated list / local watchlist first; market-wide discovery requires a selected and validated discovery feed.
- Agree on explicit freshness policies by workflow. Do not use the same arbitrary age threshold for an intraday entry and a completed historical overnight analysis.
- Supply/confirm the exact installed Thinkorswim 5M and 1M study versions and matching parity evidence before representing API-derived indicators as equivalent to their state labels.

### Release status
- Source implemented; CI/browser execution for commits above is **not yet verified in this work session**. Local test suite was reported by the user as 100 succeeded before these new changes; that prior run does not cover this implementation.


## 2026-10-27 — To-Do execution pass and dashboard Arizona timestamps

### Completed in this pass
- [DONE — SOURCE CHANGE] Scan & Analyze's REASON summary now formats each latest 1M/5M source timestamp in `America/Phoenix` and labels it “Arizona”; it no longer emits the latest bar timestamp as an ISO UTC string inside the reason text. UTC request bounds, API fields, and provider/persistence timestamps remain UTC by design. Commit: `fc17f9febce848abee86f35bdf99232a4f5172dc`.
- [DONE — IMPLEMENTATION SCAFFOLD] Added `docs/Implementation-Backlog-Scaffold-20261027.md`: workstream contracts, acceptance criteria, deterministic test cases, external blockers, and release gates covering dashboard/time, scanner discovery/qualification, providers/collector, Boring Overnight, TOS parity, extended continuation, Morning Report/Red Zone, import/parity, tests, and operating docs. This is explicit scaffolding, not a claim those blocked integrations are complete.
- [DONE — SOURCE AUDIT] Re-read the canonical Master To-Do, current handoff, API routes, and Scan & Analyze code. The dashboard's current Scan & Analyze still retrieves descriptive Alpaca OHLCV; it does not yet calculate validated qualification or TOS state parity.
- [DONE — DOCUMENTATION] Updated handoff to identify the timestamp fix, backlog scaffold, exact current branch state, and next validation actions.

### Per-workstream disposition (all canonical To-Do areas)
- [PARTIAL — 5M AUTHORITY] Source/screenshot evidence supports the observed PBLOCK=2 / EOD=0 revocation; no defect proven. Preserve Permission logic. Remaining: installed-study parity and explicit decision on persistent-latch semantics.
- [PARTIAL — 5M→1M HANDOFF] Source audit exists. Remaining same-bar installed-state tests for fresh/in-trend/continuation/confirmation/armed READY/expiry/re-entry; do not weaken the gate without evidence.
- [PARTIAL — 1M EXECUTION / RUNNER] Diagnostics and source references exist; complete installed-version reconciliation and transition tests remain evidence-dependent. No unsupported study logic changes made.
- [PARTIAL — BORING OVERNIGHT] Criteria and API evaluator scaffold exist; full independent historical edge/boringness, complete eligible window, event clearance and validated ranking are not connected. Unknown inputs must fail closed.
- [PARTIAL — EXTENDED CONTINUATION] Separate-state design and contracts scaffolded. Exact ATR-extension parity, live context and historical shadow results remain required before actionable ranking.
- [PARTIAL — SCANNER] Scan & Analyze collects both 1M and 5M OHLCV for a supplied list and summarizes it. Market-wide discovery, append-only discovery event store, explicit freshness policy, quote/spread/liquidity, catalyst/halt/sector context, exact extension, and TOS gates remain open. Provider choice/entitlement is a blocker for live discovery; no guessed provider endpoint added.
- [PARTIAL — C# / API / COLLECTOR] Historical REST adapters, bars endpoint, persistence/collector and status routes exist. Durable restart proof, correction/retention policy, session-aware gap classification, websocket/reconnect/backfill and production monitoring remain open.
- [PARTIAL — MORNING REPORT / RED ZONE] UI, report/export and API snapshot scaffolds exist. Persisted report snapshots and qualified-candidate ranking remain unconnected; demo/import data must remain visibly non-live.
- [PARTIAL — IMPORT / PARITY] Alpaca CSV/JSON import and StrategyReport separation exist. Rename the JSON fixture truthfully, add filename/header/payload provenance checks, and preserve gaps/partial windows. TOS state parity is still blocked by missing diagnostics.
- [PARTIAL — VALIDATION] Existing sampled OHLCV parity evidence is useful but not full-session parity. Add time conversion, freshness, evaluator, persistence/restart and deterministic report tests; run CI/tests at the exact new head.
- [PARTIAL — README / USER WORKFLOW / VS LOADABILITY] Handoff and canonical To-Do updated. If an actual C# project still fails to load, exact Visual Studio Output error remains necessary; solution folders are not projects.
- [HOLD — BROKER] Schwab API, credentials and order placement remain intentionally unimplemented. Manual Schwab workflow stays the boundary.

### Blockers retained rather than guessed
- Live scanner/provider: provider choice, credentials, entitlements and licensing.
- TOS parity: installed study version and same-bar internal diagnostics.
- Boring Overnight: complete source-backed regular close and eligible overnight-window OHLCV; session boundary decision; actual fills are a separate dataset.
- Catalyst clearance: source with known coverage, timestamps and latency.
- Freshness: separate approved policy for intraday execution and overnight research.
- Visual Studio: exact project name and first load error if an actual project remains incompatible.

### Immediate validation after pull
1. Confirm branch and exact head; run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
2. Open the dashboard served by the API and scan a known window. Confirm the REASON field's latest 1M and 5M timestamps display Arizona time, while the completion status still exposes UTC request boundaries for audit.
3. Verify a known timestamp on the Windows browser configured to a non-Arizona timezone; test both a winter date and a summer date.
4. Verify CI for the exact resulting head. Do not treat source changes or previous-head test counts as current-head verification.

### Release status
- Source/docs updated; local browser behavior and exact-head CI/test results are not verified by this source-editing pass. MCC remains a historical-data research workbench, not a live trading-ready scanner.


## 2026-10-28 — User validation feedback, sparse scanner results, provider date default

### User-confirmed verification of prior commit
- [USER VERIFIED] Pulled `feature/opportunity-quality-classification`, API-hosted dashboard starts, Scan & Analyze runs, 1M/5M times align, Arizona timestamp display looks correct, `dotnet test MomentumCommandCenterV2.sln --configuration Release` reports 100 passed, and CI shows all green for the prior head.
- [OPEN VALIDATION] User's “Summer” timestamp example was 2026-10-26, which is autumn; a true summer-date check (e.g. July) is still needed. Arizona does not observe daylight-saving time and remains UTC−07:00, but verify the timezone formatter with a real summer date.

### Implemented in this pass
- [DONE — SOURCE] Scan & Analyze filters invalid timestamps and sorts returned bars chronologically before summarizing.
- [DONE — SOURCE] Change and high-low range now require at least two valid bars. One bar no longer produces false `0.00% change` / `0.00% range`.
- [DONE — SOURCE] Status is `INSUFFICIENT DATA` unless each timeframe returns at least two valid bars. REASON explains missing/one-bar observations and shows a coverage warning. A bar count is an observation count, not proof of full-window coverage.
- [DONE — SOURCE] Provider Comparison Test Date defaults to the current Arizona calendar date when the field is empty; removed the stale static 2026-10-22 value. Date end includes the last millisecond of Arizona day.
- [DONE — DOCS] Implementation scaffold extended with PMI diagnosis boundaries and the next verification sequence.

### PMI case interpretation
- [USER REPORTED] PMI on 2026-10-26, Arizona 06:02–16:02, returned one 1M bar and one 5M bar, both timestamped 06:55 Arizona, volume 120. Latest bar age was about 3015.6 minutes.
- [CONCLUSION] Previous `+0.00%` / `0.00% range` was mathematically produced from the same lone observation as both first and last. It did not mean PMI was flat or that a decline had not occurred.
- [NOW CORRECTED] One bar is explicitly insufficient to calculate change/range; status and reason report the shortage instead of implying no movement.
- [STILL UNKNOWN] Why the provider returned only one bar. Data not found in sources to attribute this to a ticker issue, provider coverage, feed entitlement, request behavior, or API normalization. Inspect raw `GET /api/market-data/bars` payload and provider/feed/gap metadata before assigning a cause.

### Next checklist — do not skip
- [ ] Run PMI again on the same window; confirm both timeframes are `INSUFFICIENT DATA`, change/range unavailable, and the REASON names the one-bar coverage warning.
- [ ] Compare PMI response metadata (`provider`, `feed`, `barCount`, first/latest source timestamps, `gapCount`) with Provider Comparison results. Diagnose from returned evidence, not assumptions.
- [ ] Run a known liquid symbol for the same interval to check the multi-bar path.
- [ ] Verify Provider Comparison Test Date initially defaults to today's Arizona date, but preserves the user's changed selection.
- [ ] Verify an actual July date (not 2026-10-26) from a browser whose OS timezone is not Arizona.
- [ ] Run Release tests and CI at the exact source head containing the sparse-bar and provider-date changes.

### Status of all remaining unchecked items
Continue work without waiting for another confirmation when a task can be safely implemented from existing contracts. For items requiring a user choice, credentials/entitlements, licensed source, installed Thinkorswim study diagnostics, full source-backed overnight data, or proof from the user's environment: scaffold the interfaces, fail-closed behavior, tests and acceptance criteria; leave the specific external evidence item open. See `docs/Implementation-Backlog-Scaffold-20261027.md`. Do not relabel scaffolding as completed functionality.


- [DONE — SOURCE, 2026-10-28 follow-up] Scanner summary also excludes missing high/low fields from range inputs and requires finite first/last close values before calculating change. This prevents malformed/missing price fields from being displayed as a numeric movement result.
- [VALIDATION] This follow-up is source-only verified by inspection. The available GitHub combined-status lookup returned no status records for the latest source commit; do not treat the prior green CI as proof for this exact head.


## Verification checkpoint — Provider Comparison date and Swagger CI — 2026-10-28

- [DONE — SOURCE COMMITTED] Fixed the Swagger tag mapping to use ordinary `if/else` conditions instead of the invalid switch-pattern arm that failed CI with CS8780. Workflow run [40732946327](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40732946327) had failed in Build; Test was skipped. The user's 100/100 local test result does not supersede that CI failure.
- [DONE — SOURCE COMMITTED] Fixed Provider Comparison date initialization. `providerDate` is an HTML `type=date` input and must receive `YYYY-MM-DD`; the previous code supplied a datetime-local value (`YYYY-MM-DDTHH:mm`) that the browser rejects as invalid, leaving the field blank. Initialization is placed before later dashboard rendering, and the script cache query is bumped to `20261028-provider-date-and-swagger-fix`.
- [PENDING — CI] Verify build and all tests on the new branch head. Do not mark CI green until a new workflow run completes successfully.
- [PENDING — USER RUNTIME] Refresh dashboard and confirm Provider Comparison displays the current Arizona date on initial load and after refresh; verify a manually chosen date is preserved.
- [PENDING — DIAGNOSTIC] BrowserLink warning `$.noConflict is not a function` remains unconfirmed. Compare a run with Visual Studio Browser Link disabled or outside debugging before changing application code.
- [DONE — USER VERIFIED BEFORE THIS CHECKPOINT] Swagger workflow groups appeared in UI; Scan & Analyze Reason text wrapped without widening the page.


## Implementation checkpoint — fail-closed validation hardening — 2026-10-28

- [DONE — SOURCE IN THIS COMMIT] Boring Overnight evaluator now rejects blank/invalid symbols and non-finite numeric values (NaN, positive infinity, negative infinity) as DATA? before gate evaluation. This closes a correctness hole because NaN comparisons can all evaluate false and accidentally let a malformed snapshot appear eligible.
- [DONE — TESTS ADDED IN THIS COMMIT] Added evaluator regression cases for NaN/infinities and invalid symbols, plus raw market-bar store tests for non-finite prices, impossible OHLC relationships, and negative volume.
- [PENDING — CI] Run the Release build/test workflow for this exact commit before treating the new cases as passing.
- [NOT IMPLEMENTED / BLOCKED BY EVIDENCE] Do not claim autonomous Boring Overnight qualification: daily/overnight metric computation, complete GTC_EXT session coverage, session-aware gap classification, catalyst source, and authoritative TOS states still require implementation and/or source evidence. The evaluator continues to accept caller-supplied snapshots only.
- [NOT IMPLEMENTED / BLOCKED BY EXTERNAL SETUP] Live scanner discovery and provider proof remain blocked by provider selection, credentials/entitlements, coverage/licensing, and real-data smoke tests. The current collector is explicit-symbol historical REST polling, not a market-wide scanner or websocket stream.
- [USER/LOCAL VALIDATION OPEN] Confirm the Provider Comparison date after browser refresh; source initialization and cache-bust are committed, but browser behavior has not been independently observed here.
- [OPEN] BrowserLink $.noConflict is not a function remains unisolated. Compare with Browser Link disabled before adding application workarounds.


## Follow-up correction — validation hardening compile fix — 2026-10-28

- [FIX COMMITTED] The first validation-hardening CI run failed during build with CS0136 because a local variable named reasons conflicted with a later declaration in the evaluator. Renamed the early-return variable to invalidReasons in commit 65c83f7918200e413a5b20fb7942392357c7d193.
- [PENDING — CURRENT HEAD] Re-run build and tests on the exact latest head after this fix. The previous run 40737130070 is a known failure and must not be reported as passing.
- [NEXT] Verify the new evaluator tests for NaN/infinite values and invalid symbols, and the store tests for invalid OHLCV, once the workflow completes.


## Follow-up — batch validation is atomic — 2026-10-28

- [DONE — SOURCE COMMITTED] The raw-bar store now validates every bar in a batch before opening the append stream. A malformed later row can no longer leave earlier rows from that same batch partially persisted.
- [TEST ADDED] Invalid-OHLCV tests now submit a valid bar followed by the invalid bar and assert the store creates no file.
- [PENDING — CI] Confirm the latest branch-head workflow passes build and tests; earlier validation commit 4208441 failed build, and commit 65c83f7 fixes that compiler issue. Neither failure is considered green.


## Verification resolution — validation hardening CI green — 2026-10-28

- [DONE — CURRENT SOURCE HEAD VERIFIED] Commit 9c1b677682f7dd659d355669ddceb99194a7d5fb passed GitHub Actions run [40737545891](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40737545891).
- [PASS] Release build succeeded.
- [PASS] 108/108 tests passed, including the new Boring Overnight non-finite/symbol fail-closed tests and malformed OHLCV batch-validation tests.
- [DONE — SOURCE] The raw market-bar store validates the complete batch before opening its append stream; malformed later bars cannot leave earlier bars from that batch partially persisted.
- [STILL OPEN] Provider Comparison date must be confirmed in the user's browser after refresh; BrowserLink warning remains unisolated; live provider credentials/entitlements and source-backed overnight coverage remain external/data blockers.
- This verification applies to source commit 9c1b677682f7dd659d355669ddceb99194a7d5fb. Any later source or documentation commit requires checking its own CI run before calling that new head verified.




## Overnight Scanner review — 2026-10-28 (evidence correction)

- [CORRECTED] The earlier wording claiming all 11 individual ticker charts had been reviewed is not supported by the evidence accessible in this session. The GitHub image route did not return those image contents. Do not treat the earlier EVR-first / SXC-recovery chart-pattern claims as verified from today's source evidence.
- [DONE — REVIEW RECORDED] Added `docs/Validation/Overnight-Scanner-Review-20261028.md`, based on the readable scanner screenshot and the MCC table supplied by the user.
- [PRELIMINARY BORING OVERNIGHT ORDER] RTO, AMT, SXC for further chart review only. RTO has a reported 1.29% MCC range and 70% ON win rate; SXC has a 6.20% range and should not automatically be ranked first for a low-noise strategy.
- [DONE — USER CLARIFICATION APPLIED] The red 1M `EOD: GET OUT` state at 12:55 Arizona is intended live-dashboard behavior, not a surprise or automatic candidate disqualification. Keep 5M setup/permission distinct from 1M execution/exit state.
- [IMPORTANT] All supplied MCC rows say `OBSERVED · NOT QUALIFIED`; the snapshots are around 12:55–12:59 Arizona and are not current actionable entries.
- [BLOCKED — FRESH DATA] Current quote refresh, catalyst/news clearance, and next-session event risk were not verified. Catalyst status: Data not found in sources.
- [NEXT IMPLEMENTATION] MCC should expose a unified candidate-review record combining overnight statistics, 5M setup/permission, 1M execution state, sample completeness/freshness, extension/range, catalyst status, and a human-readable action. Do not fabricate weights; preserve raw fields and missing-input reasons.
- [NEXT IMPLEMENTATION] Add a freshness gate that marks stale bar windows and prevents old `OBSERVED` rows from being mistaken for current entries. Define age thresholds from the application's session/data contract before enforcing them.


## Overnight Scanner review and action checkpoint (2026-10-28)

- [DONE — EVIDENCE REVIEWED] Added docs/Validation/Overnight-Scanner-Review-20261028.md using the readable scanner screenshot and supplied MCC 1M/5M table. Linked individual chart images could not be inspected; no candle-pattern claims were fabricated.
- [PRELIMINARY RANKING] RTO first, AMT second, SXC third for further Boring Overnight chart review. None is READY. SXC's 6.20% intraday range conflicts with the low-noise objective; RTO has a 1.29% range and 70% ON win rate.
- [WATCH / TOO HOT] TRMD, EVR, MANH, OSW and STNG have current ON moves above the preferred below-3% boringness range; deprioritize for today's boring-first shortlist.
- [DATA STALE] MFM has only 8 bars and last observations around 12:25–12:28 Arizona; mark DATA? / not actionable.
- [IDENTITY CHECK] Confirm BNC instrument identity; scanner description reads “BNB Standard Corp”.
- [OPERATING RULE CLARIFIED] Red EOD: GET OUT at 12:55 Arizona is intentional for the live dashboard.
- [OPEN] All supplied MCC rows are OBSERVED · NOT QUALIFIED. Do not turn scanner rank into execution authorization.
- [NEXT] Verify current data, catalyst/event status, 20D volume, price/20D-SMA, maintained exclusion list, identity, and chart state before any Boring Overnight READY.
- [NEXT ENGINEERING] Boring Overnight remains snapshot-fed; daily/overnight metric calculation, GTC_EXT session collection, catalyst inputs and live scanner discovery are not fully wired end-to-end. Keep unknown/stale inputs fail-closed.
- [BLOCKED BY IMAGE RETRIEVAL] The linked individual symbol charts were not readable through the current GitHub image route. The ranking is limited to the accessible scanner screenshot and supplied MCC table.


## Unified candidate review scaffold — 2026-10-28

- [DONE — CONTRACT SCAFFOLD] Added docs/Architecture/Unified-Candidate-Review-Contract.md defining the unified candidate review record, precedence, data provenance, freshness behavior, reason codes, implementation seams, acceptance criteria and blockers.
- [NOT YET IMPLEMENTED] The contract is not yet wired to domain types, API, persistence, provider data or dashboard. Continue implementation from this contract; do not mark the feature complete based on documentation alone.


## RTO / AMT chart review — 2026-10-28

- [DONE — PROJECT SOURCES REVIEWED] Visually reviewed the directly uploaded RTO and AMT Thinkorswim screenshots; detailed observations are in `docs/Validation/RTO-AMT-Chart-Review-20261028.md`.
- [RTO — HISTORICAL SHORTLIST ONLY] 5M pane shows Runner Rising, Permission YES, RSI 61.1 and extension 0.49 ATR; lower 1M pane shows GET OUT / EOD GET OUT and a session of weakness followed by a late rebound. Do not treat the 5M green state as late-day overnight-entry authorization.
- [AMT — DEPRIORITIZE FOR THIS SESSION] 5M pane shows extension 2.03 ATR and the chart shows a large selloff/recovery from about $175.78 toward $184.02; lower 1M pane ends in GET OUT / EOD GET OUT. Not a boring-first entry from this chart evidence.
- [NO CANDIDATE PROMOTED] RTO remains first for further research, AMT deferred, SXC remains third/caution. No `READY` status established; hard gates and catalyst/data freshness remain unverified.
- [NOW — ENGINEERING] Add source-backed ATR/true-range and ATR-normalized extension calculations with explicit timeframe/lookback and tests; compare against TOS outputs before qualification integration. Preserve distinct 5M authority, 1M execution/EOD, RVOL, historical Boringness and freshness fields.
- [OPEN — STATE RECONCILIATION] The top 5M and lower 1M panes display differing Permission labels. Align exact bar timestamps and study inputs before asserting a bug or changing logic.
- [OPEN — END-TO-END] Boring Overnight remains caller-snapshot-fed; market-calendar-aware close/open pairing, complete GTC_EXT bars, catalyst provider, persistent decision snapshots, and scanner/Morning Report/MarketWatch integration are not verified as end-to-end.


## Signal semantics and implementation — 2026-10-28

- [DONE — CODE] Corrected `BoringOvernightEvaluator` on `feature/opportunity-quality-classification` to model `EodSignal` separately from clock/session state. `GET OUT` / `SELL` is explicitly an active exit signal: it blocks a fresh overnight entry and tells the operator to manage an existing position, but does not mean the trading day has ended. Commit: `89ad3ad03cf80552a973419f0665d54245f753a7`.
- [DONE — TESTS ADDED] Added tests covering `EodSignal=GET OUT` and `SELL` to ensure status is BLOCKED for a new entry and the reason is not misrepresented as market/session closure. Test commit: `fa03c32bbd5692e55fed5225b7fd87106e9dbef0`. CI/build still requires verification on this exact head.
- [RTO — RE-RANKED WITH SIGNAL SEMANTICS] Best of RTO/AMT/SXC for further boring-first review, not READY: supportive 5M trend/permission and modest 0.49 ATR extension, but active 1M/EOD GET OUT is an exit signal and RVOL 4.32 needs review.
- [AMT — DEFER] Active 1M/EOD GET OUT is an exit signal; extension ~2.03 ATR and large intraday selloff/recovery conflict with boring-first entry.
- [SXC — DATA GAP] No SXC chart was available in this review; the 6.20% scanner range is high relative to the low-noise objective. Keep WATCH/DEFER, not READY.
- [OPEN — ATR ENGINEERING] Implement explicit source-backed True Range/ATR/ATR-normalized extension from OHLCV with configurable timeframe/lookback and unit tests; do not integrate as authoritative until parity against selected TOS study output.
- [OPEN — END-TO-END P0] Historical close/open pairing, complete GTC_EXT eligible-window coverage, catalyst provider, persisted candidate decisions, and live scanner/Morning Report/MarketWatch integration remain incomplete. Unknown/stale inputs must fail closed.


## ATR implementation progress — 2026-10-28

- [DONE — SOURCE + TESTS ADDED] Added `BoringOvernightAtrCalculator` using explicit OHLCV bars, timeframe, period, Wilder smoothing, latest True Range, ATR%, and signed ATR extension from an explicitly supplied reference price. Missing history, mixed symbol/timeframe, duplicate timestamps and invalid OHLCV fail closed.
- [DONE — TESTS ADDED] Added tests for Wilder ATR smoothing, ATR%, signed extension, insufficient history, and mixed timeframes. Source commit: `ea8a27700f6609d1e78a7a9d93a3d192426448dc`; tests committed in `0c9be200270327ba70b4ef93a36f535be2b3422b`, with corrected smoothing expectation in `6f000252ba9724dda40687748a6905b17072ae0a`.
- [NOT YET QUALIFICATION-AUTHORITATIVE] The calculator is not yet wired into candidate evaluation. Its Wilder ATR and extension convention must be compared with the exact selected TOS study/timeframe/lookback before it is used as a gate.
- [OPEN — END-TO-END P0] Close/open overnight pairing, complete GTC_EXT coverage, catalyst evidence, persisted decisions, and scanner/Morning Report/MarketWatch integration remain incomplete.
- [CI UNKNOWN] GitHub status checks and commit-associated workflow lookup returned no records for the latest documentation head. Local build/test execution could not run in this environment because direct GitHub network access is unavailable. Do not mark tests as passed until CI or a local build confirms them.


## Time-gated Boring Overnight decision bubble — 2026-10-28

- [DONE — DECISION HELPER + TESTS ADDED] Added BoringOvernightHoldDecision and tests. Inside the configurable 12:30–12:55 local-time window, READY => green HOLD OVERNIGHT; known non-READY => red EOD GET OUT; missing/invalid evidence => yellow DATA? — NO HOLD. Outside the window it returns NO DECISION and does not emit either trading bubble. Timezone is an explicit input, not guessed.
- [DONE — DESIGN DOCUMENT] Added docs/Architecture/Boring-Overnight-Hold-Decision-Bubble.md with decision semantics, color/status mapping, qualification gate contract, production checklist, and precise user instructions.
- [PARTIAL — API HELPER NOT YET INTEGRATED] Helper accepts a supplied evaluator result; it is not yet wired to persisted source-backed candidate data.
- [DONE — THINKSCRIPT SOURCE UPDATED] Updated docs/Charts/1M/MainStudy.txt: 12:30–12:55 Arizona decision window; red EOD GET OUT only on a known chart-technical failure at the final window bar; yellow DATA? — NO HOLD when chart technical checks pass but required external qualification evidence is unavailable. The ThinkScript study deliberately does not emit green HOLD OVERNIGHT because the historical metrics/catalyst/exclusion/freshness evidence is not connected.
- [DONE — 5M LABEL CLARIFIED] docs/Charts/5M/MainStudy.txt now labels the clock-based control as ENTRY CUTOFF, not EOD GET OUT. The late-session entry cutoff logic remains in place.
- [DONE — TIMEZONE CONFIRMED] User selected Arizona time (America/Phoenix). The 1M study converts the window to Eastern clock inputs (15:30–15:55 ET during DST; 14:30–14:55 ET during standard time), because thinkScript time functions use Eastern time.
- [USER ACTION REQUIRED] Copy the updated docs/Charts/1M/MainStudy.txt into the existing 1M Thinkorswim study, save/compile, and validate the decision output on the 12:55 Arizona bar. Do not treat yellow DATA? as a failure; it indicates external qualification data is not yet wired.
- [OPEN P0] Validate ThinkScript compilation and same-bar 1M/5M alignment; wire complete qualification data into the server/API and decide how that authoritative result will reach the chart without manual stale inputs.
- [OPEN P0] Complete historical overnight metrics, GTC_EXT coverage, catalyst/exclusion/freshness evidence, persisted decisions, scanner/Morning Report/MarketWatch integration, and build/test verification.


## Dynamic Overnight bubble — implementation update 2026-10-28

- Removed the independent EOD clock-state override from the 1M `finalState`. The 3:55 PM Arizona cutoff still blocks intraday entries, but it no longer forces the 1M state to GET OUT or independently asserts an overnight decision.
- Added the 12:30–12:55 PM Arizona window; the 1M study evaluates its chart-visible technical blockers at the 12:55 bar. A known technical failure emits red EOD GET OUT. If technical checks do not fail, it emits yellow DATA? — NO HOLD because overnight history, catalyst, exclusions and data freshness are not available in ThinkScript.
- The 5M study's time-based label now says ENTRY CUTOFF ACTIVE/INACTIVE instead of EOD GET OUT/ACTIVE. The late-session entry cutoff behavior is preserved.
- Removed EodSignal as an evaluator input to avoid circular qualification: EOD GET OUT is now an output of the decision, not an independent input that makes the decision fail itself. 1M SELL/GET OUT/BLOCK execution state remains a legitimate gate.
- Arizona convenience overload and timezone test added to the server decision helper.
- Commits: 89e05e0f99fc78a72328b3f8bc52fc2c1b2283bb (1M study), 1fa0e0a49625af0ce970ddd2279f169bcfb62413 (label placement), e8c28953eec60b0ede81847587f247b2aefb360d (Arizona API overload), a4138830e7d96b28755b3a80648c091444687d61 (timezone test), 685994a2545998cdf2601d309eca505a7deb4042 (5M label), 6e28eed55b4f365ff5e38aa937ea6da4b164f9e6 (remove circular EOD input), 70d8b4befd218ae53475e9485c6bf44bee6a4c2a (test update).


## 2026-10-28 — Arizona 12:55 Boring Overnight bubble validation

- [DONE — USER-REPORTED RUNTIME VALIDATION] User installed the updated 1M and 5M studies successfully; both compiled.
- [DONE — USER-REPORTED 1M OUTCOME] At the 12:55 PM Arizona decision bar, SXC displays red `EOD GET OUT` when a defined chart-technical blocker is present. Evidence: [SXC screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212549.png).
- [DONE — USER-REPORTED 1M OUTCOME] RTO displays yellow `DATA? — NO HOLD` when the chart checks do not fail but external qualification evidence is unavailable. Evidence: [RTO screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212531.png).
- [EXPECTED / NOT A DEFECT] Green `HOLD OVERNIGHT` is not expected until the full qualification inputs are connected. The observed RTO DATA? result is fail-closed behavior, not permission to hold.
- [NEXT — IMPLEMENTATION, NO NEW USER INPUT REQUIRED] Connect the server-side decision helper to timestamped persisted candidate snapshots and provider-backed overnight metrics, exclusions, catalyst state and freshness. Keep unknown or stale evidence in DATA? and known technical/hard-gate failures blocked. Add API/scanner/report integration only after the authoritative input contract is complete.
- [OPEN — VERIFICATION LIMIT] The screenshots and runtime outcomes are reported by the user; no automated screenshot parsing or same-bar 1M/5M parity run was performed here. Repository CI/build status for the current head remains unverified until checked explicitly.


## CI recovery and working-state implementation — 2026-10-28

- [DONE — CI VERIFIED] Fixed the stale Wilder ATR test expectation: for the documented 2-period sequence, the seeded ATR is 2.5 and the next Wilder-smoothed value is 2.25. Implementation unchanged. Commit `96283fb20c054aee326205ff6d19c438f3033862`; GitHub Actions run `40818276331` completed successfully. This verifies the pipeline at that commit only; rerun after subsequent source changes.
- [NOW — OVERNIGHT FIRST] Replace caller-assembled qualification payloads with a reproducible, timestamped candidate snapshot contract that records symbol, as-of time, source/provider/feed, source timestamps, calculation version, required fields, missing/stale fields, and reason codes. Persist snapshots append-only; never overwrite history or allow missing evidence to pass READY.
- [NOW] Add a queryable read path over stored OHLCV observations and calculate overnight statistics only from explicitly paired, session-valid close/open observations. Preserve source/provider/feed and correction lineage. Reject incomplete windows; do not treat gaps as zero activity.
- [NOW] Add focused tests for snapshot round-trip, idempotency/corrections, stale/missing inputs, session boundaries, and hard-gate fail-closed behavior. Verify CI after every source commit.
- [OPEN — EXTERNAL INPUT REQUIRED] Live catalyst clearance cannot be completed without a selected/connected news or event source. Keep UNKNOWN => DATA? and provide a precise missing-evidence reason; do not fabricate clearance.
- [OPEN — EXTERNAL INPUT REQUIRED] A real market-wide scanner requires a selected, entitled provider and validated symbol universe. Existing bounded user-supplied-universe scans are not market-wide discovery.
- [OPEN — USER WORKSPACE REQUIRED] Thinkorswim installed-study parity and chart exports cannot be established by repository tests alone. Keep 5M authority and 1M execution gates unchanged until exact source/output comparisons are available.
- [OPEN — VALIDATION REQUIRED] GTC_EXT fill/outcome research, overnight target-touch probability, spread/slippage, and out-of-sample performance require actual timestamped trade/bar outcomes. No profitability claim is supported yet.
- [DONE — STATUS LABEL CONFIRMED BY USER] User observed red `EOD GET OUT` on SXC and yellow `DATA? — NO HOLD` on RTO. The green hold state is intentionally unavailable while external qualification evidence is missing.

### Immediate execution order
1. Build append-only candidate snapshot persistence + read/query contract.
2. Build validated overnight-stat calculation from real session-paired bars.
3. Connect evaluator to persisted snapshot and explicit freshness/provenance; maintain fail-closed semantics.
4. Add tests and keep CI green.
5. Add catalyst/exclusion evidence adapters only when sources are real and validated.
6. Integrate with scanner/Morning Brief; clearly label bounded versus market-wide discovery.
7. Finish TOS parity, GTC_EXT outcomes and out-of-sample research before actionable READY is enabled.

- [IMPLEMENTED — CI PENDING] Added append-only Boring Overnight candidate snapshot storage in `BoringOvernightCandidateSnapshotStore.cs` and endpoints `POST /api/boring-overnight/snapshots` / `GET /api/boring-overnight/snapshots`. Records include as-of/recorded timestamps, source/provider/feed, calculation version, field source timestamps and the evaluator request; API overwrites caller-supplied exclusion flag with MCC's local exclusion list before evaluation. Provenance is recorded, not independently verified. Commit `5de6613ea951d4d38ba160c1108a22ed4ae64870`; run `40818694414` queued at time of this note.
- [OPEN] Add automated tests for snapshot JSONL persistence, query ordering, validation and evaluator integration. Do not treat endpoint availability as a completed qualification pipeline: overnight statistics are not yet derived from validated session-paired bars.

- [DONE — CI VERIFIED] Added `BoringOvernightCandidateSnapshotStoreTests` covering provenance round-trip, case-insensitive symbol reads, non-UTC as-of rejection, and symbol mismatch rejection. CI passed for snapshot store and API source commits: run `40818681818` and run `40818694414`. The newer test commit `8139ef7eb6934fad423c41a29a28cfac4e4bc23b` has its own run queued; wait for that result before declaring current HEAD green.


### Persisted raw market-bar query — 2026-10-28
- [IMPLEMENTED — CI PENDING] Extended `IMarketBarObservationStore` with a read/query contract and implemented latest-version-per-source-timestamp retrieval in `JsonlMarketBarObservationStore`. Historical corrections remain append-only; queries return the latest correction for each provider/symbol/timestamp.
- [IMPLEMENTED — CI PENDING] Added `GET /api/market-data/stored-bars?symbol=RTO&fromUtc=2026-10-27T00:00:00Z&toUtc=2026-10-29T00:00:00Z&provider=Alpaca&limit=1000`. It returns raw persisted observations only; it does not infer market sessions or calculate overnight returns.
- [IMPLEMENTED — CI PENDING] Added tests for correction-aware query results, provider/window filtering, and invalid query inputs. CI for the interface-only intermediate commit failed because the interface was temporarily ahead of its implementation; subsequent commits add the implementation and tests. Verify latest HEAD before calling this green.
- [NEXT] Use the new read path as the foundation for a separately tested session calendar/overnight pairing service. Do not derive Overnight win-rate or return metrics until the regular-session close and next-session open definitions, timezone, eligible bar windows, and missing-session policy are explicit and tested.

- [IMPLEMENTED — CI PENDING] Added `BoringOvernightHistoryCalculator` and `POST /api/boring-overnight/history`. It calculates close-to-next-open returns only from persisted observations matched to caller-supplied exact UTC boundary timestamps. It returns per-pair provenance IDs and missing-boundary reason codes; if any requested pair is missing or history is short, it suppresses rolling metrics. It does not infer holiday calendars or establish catalyst clearance.
- [IMPLEMENTED — CI PENDING] Added tests for 20 explicit close/open pairs, missing-boundary fail-closed behavior, and duplicate next-session boundaries.
- [CI FAILURE DIAGNOSED] The first market-bar query test run failed only because the test expected the corrected bar's open to be 10.5, but the corrected fixture changes High/Close and leaves Open at 10.0. Corrected the assertion; new CI run is pending. No production query failure was indicated by this assertion.

- [TEST FIX COMMITTED — CI PENDING] Corrected two test-fixture expectations discovered by CI: the corrected market bar retains open=10.0, and the second missing-pair fixture lacks the previous-close timestamp. Commit `edef25ded7db56af767a71d8be037e32ef566f56`; workflow `40820294911` is running. Earlier failures are test expectation issues; continue checking the latest run rather than marking green prematurely.


### Swagger timestamp examples and Overnight request templates — 2026-10-29
- [IMPLEMENTED — CI PENDING] Standardized Swagger descriptions for timestamp-bearing market-data endpoints to show ISO-8601 UTC values ending in `Z`, with Arizona conversions for relevant examples. Updated `/api/market-data/stored-bars`, `/api/market-data/compare`, `/api/market-data/bars`, `/api/scanner/universe`, Overnight history/evaluation, and snapshot endpoints. API README now states the UTC convention.
- [IMPLEMENTED — TEMPLATE CREATED, CI PENDING] Added `docs/templates/boring-overnight-evaluate-with-history.template.json` with all required fields and 20 explicit boundary entries. Timestamps and dates are illustrative and MUST be checked/replaced against actual stored source bars before use; the template is not evidence that the store contains those observations.
- [IMPLEMENTED — INSTRUCTIONS ADDED, CI PENDING] Added `docs/templates/README-boring-overnight-api-templates.md` explaining UTC-to-Arizona conversion, how to fill boundaries from stored-bars, what to inspect in the response, the snapshot count=0 diagnostic, and the negative test.
- [NEXT P0] Replace illustrative timestamps in the template with exact timestamps from the user's actual provider/feed store. Add authoritative exchange calendar/session boundary generation and validation before treating computed returns as production-grade.
- [CI CHECK] At time of documentation, CI for the new UTC-description, template, and README commits still needs checking on latest HEAD. Do not mark the current branch green until its latest workflow passes.

- [DOCS REFINED] Template instructions explicitly identify the example `asOfUtc` value and require replacing it with the actual evaluation timestamp; the JSON remains parseable rather than using a non-date placeholder.


## Overnight dashboard bar workflow — 2026-10-29

- [IMPLEMENTED — CI PENDING] Replaced the static Boring Overnight checklist with an API-backed bar retrieval panel. It supports user-selected 1m/5m bars, Arizona-local From/To inputs converted to UTC, requests that extend past 1:00 PM Arizona, reports first/latest source timestamps, feed, bar count, gaps, and newly persisted observations, and reads saved input snapshots.
- [IMPORTANT LIMITATION] This dashboard panel fetches and displays source bars and persisted evaluator inputs; it does not auto-generate the 20 Overnight session boundary pairs or declare a candidate READY. Snapshot GET does not persist or return the evaluator's final decision. Stored bar observations do not currently carry an explicit timeframe identity, so the 1m/5m choice is valid for the direct fetch request but must not be used to claim that persisted-bars history is timeframe-isolated. Fix storage identity before automating historical evaluation.
- [NEXT P0] Add timeframe to persisted observation identity and query filters so 1m and 5m observations at the same source timestamp cannot overwrite or masquerade as each other.
- [NEXT P0] Add an authoritative exchange calendar/session-boundary service, then wire the dashboard to the evaluate-with-history endpoint without manually authored 20-pair JSON. Until then, exact session boundary input remains an API limitation and missing history must fail closed.


## 2026-10-29 — Remaining MCC work audit (six requested workstreams)

- [AUDIT ADDED] Created `docs/Validation/MCC-Remaining-Work-Audit-20261029.md` with implementation truth, blockers, and exact validation plan for all six requested workstreams.
- [5M→1M PARITY — PARTIAL, NOT CERTIFIED] Repository source comparison is complete and documented. 1M reconstructs 5M state and predicates differ in Permission/structural damage/major failure. Installed study and matched-bar behavioral parity remain unverified. No gate changes made.
- [RUNNER / EXIT — SOURCE AUDIT ONLY] 5M is authority for runner/waning/swing/failure; 1M contains short-term exit/invalidation behavior. Matched state-transition validation and installed-study parity remain open; no thresholds changed.
- [BROAD-MARKET DISCOVERY — NOT IMPLEMENTED] Discovery contracts, ingestion, JSONL event store, and worker scaffold exist, but there is no configured production discovery provider or hosted worker. Manual universe is not broad-market discovery.
- [COLLECTOR — PARTIAL] Configured-symbol REST polling, overlap, per-timeframe checkpoints, append-oriented JSONL observations, duplicate suppression, and correction retention exist. Persisted observation identity still omits timeframe; controlled outage/backfill/crash recovery and durable runtime behavior remain unvalidated.
- [MORNING BRIEF — SPECIFICATION ONLY] Requirements are documented; live benchmark health, breadth, broad movers, sector aggregates, sourced catalysts, and immutable report snapshots are not demonstrated end-to-end.
- [BROWSER / RELEASE — CI PASS, BROWSER OPEN] Dashboard code/docs workflows including run `40966465421` passed. No browser-level regression, accessibility, responsive, or release test was executed. Not release-ready.
- [USER LIVE TEST RESULT] RTO 0 bars/feed unknown; SPY Alpaca/IEX 2 bars (20:00Z, 20:54Z) in 20:00–23:55Z window. Confirms one SPY post-market observation only; RTO cause and complete session coverage remain unknown. The Requested bar coverage panel remains NOT LOADED because no request was run there.
- [RELEASE GATE] All six requested workstreams remain partially implemented, unverified, or not implemented end-to-end. See the new audit document for exact test steps and release disposition. Do not mark READY or release-ready based on the control test.


## 2026-10-29 — Engineering follow-up

- [PARTIAL] 5M→1M source audit: installed study files are already in GitHub. Installed 1M is V14.4.10; repository 1M is V14.4.11. Exact installed source and matched-bar parity remain open.
- [PARTIAL] Runner/exit: October 28 RTO/AMT screenshot review was completed. It is not a timestamp-aligned state tape. See `docs/Validation/Runner-Exit-State-Validation-20261029.md`.
- [IMPLEMENTED FOUNDATION] Alpaca SIP top-50 gainers/losers source, polling host, JSONL persistence, API endpoint, and scanner UI refresh control added. Entitlement/full-universe coverage not verified.
- [IMPLEMENTED] Persisted market-bar identity now includes timeframe and feed. Overnight history queries 1m only. Legacy rows stay UNKNOWN; new provider fetches are required to backfill correctly tagged history. Added regression tests.
- [PARTIAL] Morning Brief now includes SPY/QQQ/IWM, 11 sector ETF proxies, Alpaca mover observations, and timestamped unclassified news. Full-market breadth and catalyst classification remain DATA NOT FOUND IN SOURCES.
- [PARTIAL] Collector local restart/outage/backfill validation and browser regression remain.
- Runbooks: `docs/Validation/Collector-MorningBrief-Browser-Runbook-20261029.md` and `docs/Validation/Runner-Exit-State-Validation-20261029.md`.
- CI green for current code/test changes. Release remains NOT RELEASE-READY until local provider, collector, matched-bar, and browser evidence passes.


## 2026-10-29 — RTO runner evidence, collector reconciliation, and dashboard chart UX

### Verified from user's latest RTO chart and local API output
- [EVIDENCE RECEIVED] New RTO 5M/1M screenshot shows 5M status `5M STATE: GET OUT`, while the 1M status strip shows `1M STATE: EXECUTION EXPIRED`. The 1M pane does **not** show `GET OUT` or `EOD GET OUT` in this capture. Do not reuse the earlier Oct 28 screenshot description for this separate chart.
- [PARTIAL] The 5M pane contains a visible progression of permission/diagnostic bubbles and a later runner event. Exact event-to-event parity still requires timestamp-aligned 1M/5M CSV rows. The exported files were said to be committed under `data/validation/RTO`, but their exact filenames/content were not exposed by the repository file reads available in this pass: Data not found in sources. Do not claim the exports have been parsed.
- [PARTIAL — COLLECTOR] User confirmed repeated ingestion returned `newlyPersistedObservations: 0` with zero cycle errors, consistent with idempotent replay for the observations already present.
- [OPEN — COLLECTOR SYMBOL/WATERMARK RECONCILIATION] Status reports configured symbols SPY, QQQ, IWM, RTO, AMT, but the checkpoint contains only SPY, QQQ, TTE, FMS keys. There are no shown IWM/RTO/AMT watermarks; TTE/FMS keys appear to be retained from a prior symbol configuration. Verify current worker's configured symbol set and actual returned bars before calling per-symbol collection complete. Do not create watermarks for empty responses.
- [VERIFIED USER OUTPUT] Alpaca provider status reports configured/validated and one returned bar. This is a successful non-empty data validation, not a full coverage or scanner-entitlement proof.
- [USER CONFIRMED] Morning Brief market/sector/news content displayed substantial data. User requests each headline's URL be presented as a `News Link` hyperlink with the raw URL as hover tooltip.

### Dashboard changes committed
- [DONE — CODE] Morning Brief news headline URLs render as `News Link` anchors with the URL in the title tooltip; published timestamps display in Arizona time.
- [DONE — CODE] Discovery group labels render as ATG¹ / ATL¹; change is displayed as a percentage and source timestamp as Arizona local time. Added the abbreviation/field footnote to Candidate Discovery.
- [DONE — CODE] Added API-backed 1-minute close-price line charts to the focused-symbol chart area and Red Zone cards. Empty/failed responses display as unavailable; they are not rendered as flat/zero lines. Selecting a candidate tile focuses the matching symbol when that symbol exists in the main selector.
- [OPEN — BROWSER VALIDATION] Verify API response shape, price line rendering, card click-to-focus, hover tooltip, responsive layout, and no console errors in the local browser.

### Remaining RTO validation
- [NEXT] Locate/confirm the exact 1M and 5M export filenames in `data/validation/RTO`; parse both and report row counts, first/last timestamps, timezone convention, OHLCV validity, duplicate/gap counts.
- [NEXT] Join bars by explicit Arizona-local candle time after proving whether export timestamps are bar starts or closes. Do not shift timestamps by assumption.
- [NEXT] Build the state tape from matched 5M/1M study event labels and assert only observed transitions. Keep 5M permission and 1M execution/exit state as separate state machines.
- [OPEN] Matched timestamp parity and runner/exit replay remain unvalidated. No change to ThinkScript trading gates is authorized by this screenshot alone.

## CI checkpoint — 2026-10-30

- [CI VERIFIED] GitHub Actions run **41095950730 / run 618** completed successfully for commit `7c7e20810b70f2a6620fb8f752844a15eed130c3`. This run includes the dashboard code, favicon, user manual, provider comparison note, browser runbook, and master To-Do updates committed through that SHA. The workflow is the repository's .NET build/test workflow; it does not execute browser UI tests.
- [BROWSER VALIDATION OPEN] Local browser acceptance remains outstanding for the new layout and favicon, and the BrowserLink `$.noConflict` error remains unresolved pending its injected script URL/stack trace.
