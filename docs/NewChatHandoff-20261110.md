# November 10, 2026 — Operator feedback and engineering handoff

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Working branch:** `feature/opportunity-quality-classification`  
**Authoritative Dashboard:** `frontend/index.html` served from the API root  
**Scope:** Boring Overnight unless explicitly noted

## Summary

The November 10 operator run exposed a stale-price presentation issue, missing visibility into sell-limit evidence gates, zero-bar scanner results on IEX, a BAYRY Daily response-envelope failure, and a JavaScript startup temporal-dead-zone error. Fixes are committed to the feature branch. Do not merge to `master` as part of this work.

## Implemented

### Focused symbol dock

- A color-coded classification now appears immediately below `#symbol`.
- Intraday mode shows the existing Opportunity Quality diagnostic classification and its color.
- Boring Overnight mode reads the latest imported TOS Daily OHLC reference for the selected symbol and displays Boring Winner PASS / FAIL as **RESEARCH ONLY**. If no import exists, it displays NOT CALCULATED / DATA?.
- Boring Winner does not grant READY, execution permission, or an order action.

### Main Chart instrument name

- Main Chart heading and chart caption now include the instrument display name when metadata is returned.
- Name lookup uses the Alpaca Trading API asset metadata endpoint; Yahoo Finance metadata is the fallback.
- If neither source supplies a name, the ticker remains visible and the heading says the name is unavailable.

### Overnight price provenance

- Added `GET /api/market-data/latest-trade` using Alpaca's configured feed.
- Added `GET /api/market-data/yahoo-extended-hours` as an optional secondary reference.
- The price card compares the source timestamps of the last returned 1M bar, Alpaca latest trade and Yahoo post-market reference, then selects the newest available observation.
- The card identifies the source, Arizona timestamp and age. Old observations are explicitly labelled stale.
- Alpaca IEX is partial coverage. Yahoo post-market metadata is indicative secondary data. Neither source is an NBBO quote or a guaranteed executable price.
- The old $9.7800 RKLX value came from the last returned IEX 1M bar at 2:46 PM Arizona on November 9. It was not a current after-hours quote.

### Calculated Limit Sell

- The focused-symbol quick action still delegates to `POST /api/boring-overnight/sell-limit`.
- Added a disclosure directly below the quick action showing each evidence gate and its PASS/BLOCKED detail.
- No new calculator, order submission, or gate relaxation was introduced.
- The orchestration reads persisted observations only. Five calendar days of 1M bars do not establish the 40 complete GTC_EXT sessions, SIP feed, exact previous-session close, ATR parity, same-source OHLCV, or walk-forward validation.

### Overnight execution-window calculator

- Candidate research and buying take place only during the final 60 minutes of the regular session. The buy deadline is the actual regular-session close, derived from the exchange calendar; in November standard time this is 2:00 PM Arizona, with a 1:00 PM Arizona start on a normal session. During daylight time the normal-session window is noon–1:00 PM Arizona. Early-close days use the final hour before the shortened close. After-hours is part of the holding/measurement interval, not a time to initiate the purchase.
- Buy sessions are Monday–Thursday only. The next sell session is the next exchange trading day, with the GTC_EXT eligible window 7:00 AM–8:00 PM Eastern.
- The header clock now displays the date-specific buy and next-session sell windows in Arizona local time. It uses named time zones and the maintained calendar for weekends, holidays, DST and early closes.
- This is a schedule calculator, not proof of per-symbol GTC_EXT eligibility or a fill. Schwab order-ticket confirmation remains required.

### Startup error correction

- Fixed `ReferenceError: Cannot access 'decisionRefreshSequence' before initialization`. The sequence counter is now initialized at the top of `frontend/app.js`, before the initial `render()` can invoke `refreshDecisionExplanation()`.
- Bumped the Dashboard script cache key in `frontend/index.html` to `20261110-decision-refresh-init-final-hour-buy-v1`; restart the API and hard-refresh so the browser does not keep the old cached script.
- JavaScript syntax validation passed. The .NET build/test workflow passed for code-bearing commit `aa60685580c266410df1149779d70d4bc15268ef` (run `42836168264`). The latest cache-key-only commit has a separate workflow still running at handoff time.

### Scanner Workbench

- Boring Overnight Scan & Analyze now defaults to a five-calendar-day window. Intraday keeps the six-hour default.
- The result summary includes actual provider/feed provenance.
- A zero-bar result now explains that no observations were returned on the configured provider/feed/window; it does not claim that the security had no market activity.
- IEX is partial exchange coverage. Do not silently switch the configured feed or interpret missing IEX observations as a market-wide no-trade result.

### BAYRY Daily response diagnosis

- Alpaca Daily parser accepts a normal `bars: []` array and a symbol-keyed `bars.{SYMBOL}` array.
- Explicit `bars: null` or a missing requested symbol in a keyed envelope is treated as an empty/incomplete observation set.
- Unexpected JSON kinds still produce a shape-specific error rather than being coerced.
- Added regression coverage. The actual BAYRY runtime payload shape has not yet been independently captured; operator re-test is required.

### Stream status

- A successful Alpaca authentication/subscription clears a previous connection error so a stale `connection limit exceeded` message is not shown after a successful reconnect.
- Recheck during regular market hours. An after-hours zero-bar count is not by itself proof of a broken stream.

### Boring Winner Daily OHLC import

- UI now explains the exact CSV contract: one row per completed Daily bar with `Date, Open, High, Low, Close`.
- The existing 1M/5M ATR parity StrategyReport is not the intended input. The existing `MCC_OHLCV_ATR_V2.1` StrategyReport is NOT the required Daily OHLC input and will not automatically calculate Boring Winner. No dedicated Boring Winner Daily OHLC export study has been delivered yet. Use a separately prepared CSV with one completed daily bar per row until that study/export path is implemented.
- Import defaults: Daily aggregation, Extended Hours OFF, corporate-action adjustment ON, chart timezone America/Phoenix.
- At least 21 completed daily bars are required; 40–60 is preferred for review.
- These settings are recorded as provenance, not independently proven by the calculator. Boring Winner remains RESEARCH ONLY.

## Operator API evidence supplied November 10

- `/api/health`: OK; decision-support-only; order submission NOT IMPLEMENTED.
- Alpaca provider: IEX configured and validated; 1,204 bars in the most recent successful validation.
- Massive: not configured.
- Minute-bar stream snapshot: SUBSCRIBED, but zero received/persisted bars and a previous `connection limit exceeded` error were present. The last error may have been stale; latest code clears it on a successful reconnect.
- Collector: six cycles completed without errors, but zero new observations were persisted during that after-hours window.
- Scanner: 20 supplied symbols returned no 1M/5M bars on the tested IEX request window. This does not prove no trading occurred on other exchanges or feeds.

## Why the calculated limit remains DATA? / NOT CALCULATED

The 1M bars endpoint returns and persists recent OHLCV. The sell-limit orchestrator has a different, stricter evidence contract:

1. Consolidated SIP feed.
2. Forty complete GTC_EXT sell windows (20 chronological training + 20 later unseen test), each with every required minute and exact prior regular-session close.
3. Exact current reference close from the same provider/feed.
4. Correct TOS ATR V2.1 provenance: ATR(14), Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON.
5. Server Wilder ATR parity MATCH over the 119-bar 99-warm-up + 20-comparison window.
6. 99 consecutive completed 5M TOS warm-up observations.
7. Exact 119-bar provider/TOS OHLCV match.
8. Fresh completed TOS 5M ATR observation.
9. Walk-forward test pass on unseen sessions.
10. Training-only target quantile calculation.

IEX cannot pass the SIP gate. A recent 5-day 1M response cannot substitute for the 40-window history or the 5M ATR import. The new disclosure should show the exact local failed gate list after the operator runs the action.

## Remaining P0/P1 roadmap — still open

| Priority | Item | Why it remains open |
|---|---|---|
| P0 | Revised ThinkScript compile | Requires compile/reload in Thinkorswim and a new source export |
| P0 | EVR 1M continuity | Requires exact source timestamps and verified missing-bar intervals |
| P0 | 119 exact provider/TOS OHLCV matches | Requires matched 119-bar source observations; no interpolation |
| P0 | Exact previous RTH close | IEX partial feed cannot certify consolidated/official close |
| P1 | Overnight execution-window calculator | Exact Arizona-local buy and sell/measurement endpoints must be frozen first |
| P1 | Separate V3 Boring Overnight study | Depends on frozen endpoints and source-backed metrics; do not invent a TOS study |
| P1 | Official auction / eligible-trade verification | Requires authoritative entitled auction/trade data |
| P1 | BAYRY Daily response diagnosis | Parser supports additional envelopes; local retest and actual sanitized payload shape remain open |
| P1 | Independent consolidated Daily source | IEX is partial; SIP entitlement currently unavailable |
| P1 | Catalyst representative dataset | Requires source-backed, independently reviewed labels |
| P1 | Reviewer agreement / classifier qualification | Requires double-reviewed sample and measured agreement |
| P1 | Opportunity Quality historical calibration | Requires chronological candidate snapshots and outcome labels |
| P1 | Boring Winner chronological validation | Requires source-qualified TOS Daily OHLC samples and session parity |
| P1 | Boring Overnight READY + alert | No validated Overnight READY definition exists; keep READY fail-closed |

## Operator next steps

1. Confirm clean working tree and branch:
   ```powershell
   git status --short
   git branch --show-current
   ```
   Expected branch: `feature/opportunity-quality-classification`.

2. Pull/build/test:
   ```powershell
   .\scripts\Get-Latest-And-Test.ps1
   ```
   Do not discard uncommitted local changes. Record the exact tested SHA and test summary.

3. Stop duplicate API processes, restart the API, and hard-refresh the Dashboard.

4. In Boring Overnight, select RKLX:
   - Confirm Main Chart shows the ETF name.
   - Confirm the dock shows Boring Winner research status or NOT CALCULATED.
   - Confirm AFTER-HOURS PRICE only appears when a post-market observation is actually returned.
   - Confirm source and timestamp identify Yahoo or Alpaca.
   - Confirm stale observations are labelled stale and are not presented as live quotes.
   - Compare the displayed value/time with the independent quote source you are using.

5. Click **Assess calculated limit sell** and expand **Why is the limit unavailable?**. Copy the complete blocked gate codes and details. Do not treat the presence of many recent 1M bars as evidence that the sell-limit gates pass.

6. Run Scan & Analyze on the Overnight top 20. Confirm the request window is five days and the result summary says Alpaca/IEX. If symbols still return zero bars, preserve the no-bars result; do not switch to SIP unless the provider entitlement genuinely changes.

7. Re-run the BAYRY Daily audit. If it still errors, capture the sanitized JSON response shape and exact HTTP status only. Do not share API keys, secrets, or authorization headers.

8. Recheck `/api/market-data/stream/status` during regular market hours. Confirm lastMessageUtc, lastBarUtc, receivedBars and persistedBars advance. If `connection limit exceeded` returns, stop any other MCC API instance or process using the same Alpaca stream account before retrying.

9. For Boring Winner, export a TOS Daily OHLC CSV with Date/Open/High/Low/Close, Extended Hours OFF, corporate-action adjustment ON, chart timezone America/Phoenix, and at least 21 completed bars (preferably 40–60). Import one symbol first and report the eight-column parity plus Boring Winner result.

10. Send the exact SHA/test output, RKLX price/source/timestamp, sell-limit blocked gate list, scanner feed/window results, BAYRY response, and Boring Winner import result. These are the inputs needed to close the remaining runtime and source-evidence tasks.

## CI and acceptance status

The Yahoo endpoint initially had a compile failure caused by a named-argument typo in its error response. That typo was corrected. Code-bearing head `436d0a53d10501f2053cce7b203f5f5ed1b7a689` passed GitHub Actions JavaScript syntax validation, .NET Release build (0 warnings / 0 errors), and all 266 tests (run `42751327218`). Later commits through the handoff are documentation-only; their exact-head workflow is queued. Local Windows build, browser acceptance, provider entitlement and market-hours stream tests remain pending.

## [Uncertainties]

- Yahoo Finance post-market metadata may be delayed, stale, unavailable, or rate-limited. The UI labels it as a secondary reference and displays its source timestamp.
- Alpaca IEX may not return trades for symbols whose activity is on other exchanges. Missing IEX observations are not market-wide absence.
- The operator's RKLX values ($9.14 then $9.07) are user-observed values; independent online snapshots reviewed during this pass differed. Local timestamped quote comparison is required.
- No Boring Winner Daily OHLC CSV has yet been imported; no actual PASS/FAIL result is claimed. The dedicated ThinkScript Daily OHLC export study is still outstanding.
- No local runtime/browser test has yet been performed against these latest commits.


## November 10 follow-up — operator runtime evidence and UI corrections

### Clarifications: StrategyReport and Boring Winner

- The only supplied Thinkorswim study is `MCC_OHLCV_ATR_V2.1 — OHLCV + Thinkorswim ATR parity export`. No new Boring Winner export study has been delivered.
- Importing that 1M/5M ATR StrategyReport does **not** calculate Boring Winner. Its interval and payload contract are not the Daily OHLC input contract.
- The existing Boring Winner calculator runs only after a separate Daily OHLC reference import containing completed daily rows with Date/Open/High/Low/Close. The API's TOS Daily reference import calculates the eight scanner metrics, including Boring Winner, from those daily rows. It does not derive daily bars from the ATR StrategyReport.
- Therefore no Boring Winner PASS/FAIL is available until a separate Daily OHLC dataset is imported and the calculation's session semantics are validated. A dedicated Thinkorswim export study is still outstanding.

### Strategy clock — preserve the intended purchase interval

- The final hour of regular trading remains the candidate-evaluation and buy window. The actual regular-session close is the buy deadline.
- The overnight measurement begins at that close and includes Buy Day after-hours and Sell Day premarket.
- During the regular session, `#overnightBuyCountdown` must display time remaining to the regular close, not DATA? and not time remaining only until the final-hour window starts. The countdown uses escalating urgency colors as the close approaches.
- The API response supplied by the operator confirms `phase=REGULAR`, `targetEvent=REGULAR_SESSION_CLOSE`, evaluation start 13:00 Arizona, and buy deadline at the regular close. Do not change the strategy window or its endpoints.

### UI fixes committed after operator retest

- [x] Assign the API's buy-window start, sell-window start/end and buy/sell session dates to the frontend state. Previously they were parsed but never assigned, which left the buy countdown DATA? and schedule details blank.
- [x] During API phase REGULAR, show the countdown to `overnightBuyDeadlineUtc` (the actual session close) and apply green / yellow / orange / red urgency styling by remaining time.
- [x] Call `refreshInstrumentTitle(symbol)` on each focused-chart refresh. The function previously existed but was never called, explaining why RKLX's returned name did not appear anywhere in the Dashboard.
- [x] Open the focused Calculated Limit Sell evidence disclosure when the quick action is clicked, and mirror every returned gate's PASS/BLOCKED status and explanation directly inside that disclosure.
- [x] Add `performanceDiagnostics` to uncached live-assessment responses: provider fetch wall time plus separate 1m and 5m fetch durations, source used for each timeframe, WebSocket store read time, persistence time, calculation time and total server time.
- [x] Bump the Dashboard app.js query string to force browsers to load the new code.
- [ ] Exact-head CI and local Windows/browser acceptance of these follow-up changes remain pending.

### Operator runtime evidence supplied November 10

- [x] API health returned OK, decision-support-only; order submission remains NOT IMPLEMENTED.
- [x] During the 10-minute regular-session stream test, SPY advanced 10 completed bars (49 to 59), RKLX advanced 3 (21 to 24), ASTX advanced 3 (34 to 37), and XRPN advanced 2 (5 to 7). All four were acknowledged at start and end. Latest timestamps advanced for each symbol. This is evidence of actual delivery for these four symbols during this interval, not proof of complete IEX coverage or TOS parity.
- [x] Collector cycles advanced 39 to 48, with zero reported cycle errors.
- [x] Scanner CSV import rendered all 50 local rows. The separate Scan & Analyze (1M + 5M) action was then run on 20 candidates. All returned rows were OBSERVED · NOT QUALIFIED on Alpaca/IEX. Counts varied widely (5M 5–27 bars; 1M 6–75 bars); these observations are not Overnight qualification, Boring Winner calculations, or trade signals.
- [x] BAYRY Daily audit returned 0 valid sessions, 0 available completed sessions and all 20 expected session dates missing (October 13–November 9, 2026). The parser change did not create data. Keep BAYRY Daily qualification blocked; do not backfill older sessions or weaken completeness.
- [x] RKLX instrument endpoint returned the full Alpaca asset name, but the operator confirmed it was absent from the Dashboard before the latest frontend fix.
- [x] Live-assessment calls for EVR, RKLX, ASTX and SPY took approximately 51.8 seconds each in the operator's local runtime. This is unacceptable for interactive UX. The current service requests 1m bars over 3 calendar days and 5m bars over 21 calendar days in parallel; it uses WebSocket 1m history only when at least 50 fresh completed WebSocket bars exist. In the supplied stream test RKLX (24), ASTX (37) and XRPN (7) did not meet that 50-bar threshold, and 5m remains REST-backed. The new response timing breakdown includes separate 1m and 5m durations and their source paths, so it can identify which request dominates; the exact cause is not established yet.

### Correct operating instructions for the next retest

1. Pull/build/test the latest feature branch and restart the API. Hard-refresh the Dashboard; the new script query key should prevent reuse of the older app.js.
2. Select **BORING OVERNIGHT** before testing the buy countdown or Calculated Limit Sell quick action.
3. During regular market hours, confirm the buy countdown says **Time remaining in regular market**, counts down to the actual regular close, and changes urgency color as the deadline approaches. Also verify the evaluation countdown separately opens at the final-hour start.
4. Select RKLX and verify its full ETF name appears in the Main Chart heading.
5. In the focused Calculated Limit Sell card, click **Assess calculated limit sell**. Its **Why is the limit unavailable?** disclosure should open and show every API-returned gate with PASS/BLOCKED and its explanation. The feed and qualification rules remain unchanged.
6. Measure one live assessment first, not four sequential requests:
   ```powershell
   $base = 'http://localhost:53360'
   $sw = [System.Diagnostics.Stopwatch]::StartNew()
   $assessment = Invoke-RestMethod "$base/api/market-data/live-assessment?symbol=EVR&providerName=Alpaca"
   $sw.Stop()
   "Client elapsed: $($sw.Elapsed.TotalSeconds) seconds"
   $assessment.performanceDiagnostics | ConvertTo-Json -Depth 5
   ```
   Record client elapsed and the server timing breakdown. If a cached response lacks `performanceDiagnostics`, restart the API to clear process memory and repeat once.
7. Do not repeat the 10-minute stream test; it has been completed for the supplied symbols. Do not repeat the BAYRY audit unchanged; it already establishes 0/20 sessions. Do not re-run the scanner import or 1M/5M analysis unless the code/data source changes.
8. Boring Winner remains untested until a separate TOS Daily OHLC CSV (Date/Open/High/Low/Close) is imported. Do not use the existing ATR V2.1 1M/5M StrategyReport for that step.

### Interpretation of Scanner Workbench steps

- Export the Overnight Scanner candidate list (the operator's CSV export), then import that CSV into MCC. The import populates historical Overnight Scanner metrics and rows.
- **Scan & Analyze (1M + 5M)** is a separate optional live intraday diagnostic over the imported candidate symbols. It does not calculate Boring Winner or change the imported Overnight Scanner metrics.
- The supplied 20-symbol analysis completed and returned OBSERVED · NOT QUALIFIED. No additional click is required for that completed scan. Keep the results diagnostic-only, especially with partial IEX coverage.


## November 10 follow-up — store indexing and Calculated Limit Sell failure


### Latest operator retest

- **RKLX Main Chart full ETF name:** PASS.
- **Boring Overnight regular-session countdown:** PASS; it displays remaining regular-session time rather than DATA?.
- **Countdown urgency colors:** PASS.
- **Separate final-hour evaluation clock transition to OPEN:** NOT YET VERIFIED (operator marked TBD).
- **Calculated Limit Sell disclosure/gates:** FAIL. Operator clicked the action in BORING OVERNIGHT mode; the quick card remained at `Assessment running…` and `DATA? — NOT CALCULATED`. No complete gate list was displayed. Do not tell the operator this fix passed.

### Live-assessment timing evidence

One EVR measurement after API update/restart:

| Measurement | Time |
|---|---:|
| PowerShell client elapsed | 12.046 s |
| Server total | 51.622 s |
| Alpaca 1m REST fetch | 233.8 ms |
| Alpaca 5m REST fetch | 491.2 ms |
| Combined provider fetch wall time | 487.3 ms |
| WebSocket store read | 11.832 s |
| Persistence | 39.284 s |
| Calculation | 7.2 ms |

Interpretation: REST retrieval and indicator calculation are not the bottleneck in this sample. The existing JSONL store was performing full-file scans/deserialization for reads and, separately, rebuilding its complete idempotency index before append. This is the source-code explanation consistent with the two long store stages. The discrepancy between the 12.046s client duration and 51.622s server duration is not yet explained; preserve both values and investigate if it persists.

### Code changes since prior handoff

- `JsonlMarketBarObservationStore` now loads the append-only JSONL history into a latest-observation in-memory index once per API process. A symbol index supports reads for one symbol without scanning the complete file on every call. Appends update the in-memory latest index while preserving the original JSONL append-only history, duplicate idempotency, and correction supersession IDs.
- Added a regression test that creates persisted history, constructs a new store instance, reads it to rebuild the index, appends a correction, then confirms the corrected latest observation and lineage.
- Calculated Limit Sell orchestration now returns `performanceDiagnostics` for history-window reads, exact reference-close read, ATR-store read, provider parity-bar read, walk-forward validation, and total duration.
- Dashboard displays those stage timings in the status line after a completed assessment. Cache key: `20261110-jsonl-index-sell-limit-timings-v1`.
- These changes are committed only to `feature/opportunity-quality-classification`; do not cherry-pick to master without explicit instruction.

### Important cold-start behavior

The first store read after each API process restart still has to scan the existing JSONL file once to rebuild the in-memory index. The improvement is that this full scan is no longer repeated for every query and append. Measure the first post-restart request separately from subsequent warm requests. If the one-time rebuild itself remains too slow, next engineering step is a durable indexed store/sidecar migration with a safe rebuild/consistency strategy; do not delete or compact the append-only source history as a shortcut.

### Next operator actions

1. From the repository root, verify branch and status:
   ```powershell
   git status --short
   git branch --show-current
   .\\scripts\\Get-Latest-And-Test.ps1
   ```
   Expected branch: `feature/opportunity-quality-classification`. Do not discard local changes. Record the exact tested SHA and Release build/test summary.
2. Restart the API from that tested build and hard-refresh the Dashboard.
3. Run one EVR live-assessment request first to trigger the one-time JSONL index rebuild after restart. Record it as the cold/warm-up request; it may be slower than a warm request. Do not run several symbols yet.
4. In **BORING OVERNIGHT** mode, click **Assess calculated limit sell** once. Wait for completion. Confirm the disclosure opens and contains the entire returned gate list, with PASS/BLOCKED and details. Copy the result status and `performanceDiagnostics`. If the request remains running, capture the POST request's HTTP status, elapsed time, response body (if any), and API log exception; do not click repeatedly or assume DATA? means a gate failed.
5. Run one further EVR live-assessment request and record PowerShell client elapsed plus `performanceDiagnostics`; this second request is the meaningful warm-cache comparison.
6. At the scheduled final-hour start, verify the evaluation clock separately changes to OPEN. This remains TBD.
7. Send the exact Release build/test output, calculated-sell result/gates/timings, and cold-vs-warm EVR timings.

### Do not repeat unchanged work

- Do not repeat the four-symbol 10-minute stream test, the 50-row import, the 20-symbol Scan & Analyze, or the BAYRY 0/20 Daily audit unless the source/code changes.
- Do not change IEX feed or relax the 40-window SIP, reference-close, ATR parity, 99-bar warmup, OHLCV match, freshness, or walk-forward gates.
- Do not submit or modify broker orders. The endpoint is read-only research calculation only.
- Boring Winner still requires its separate Daily OHLC CSV; the existing 1M/5M ATR StrategyReport is not that input.

### Current uncertainty

- The operator has not yet retested Calculated Limit Sell after the new store index and stage diagnostics.
- No exact-head CI or local Windows build/test result is available for the latest code commits at handoff time.
- The client/server elapsed mismatch (12.046s versus 51.622s) is unexplained.
- The one-time JSONL index rebuild duration and warm-read improvement have not yet been measured on the operator's data file.


## November 10 follow-up — measured store performance and quick-action root cause

### Latest operator measurements

| Metric | Cold/warm-up request | Warm request |
|---|---:|---:|
| Client elapsed | 0.649 s | 0.399 s |
| Server total | 475.2 ms | 391.5 ms |
| WebSocket-store read | 7.1 ms | 4.1 ms |
| Persistence | 3.5 ms | 6.7 ms |
| Calculation | 30.9 ms | 3.5 ms |
| Alpaca 1m REST | 210.8 ms | 220.0 ms |
| Alpaca 5m REST | 427.5 ms | 373.8 ms |

The in-memory index change materially improved the measured live-assessment path. Compared with the earlier 12.046s client / 51.622s server sample, the new warm request is 0.399s client / 0.392s server. The first new request was 0.649s client / 0.475s server. Client/server timing differences are now about 174ms and 8ms respectively. Do not claim a general multi-symbol benchmark from these two EVR samples.

**Cold-index caveat:** the reported 7.1ms WebSocket-store read is not a direct measurement of the JSONL index rebuild. Another startup/background request may have loaded the singleton store before the measured EVR request. New diagnostics now expose jsonlIndexLoadCount, jsonlIndexLastLoadElapsedMs, jsonlIndexUniqueObservationKeys, and jsonlIndexLoaded. Use these fields after a fresh API restart to identify when and how long the rebuild took.

### Calculated Limit Sell no-Network diagnosis

The quick card's previous event handler showed “Assessment running…” then programmatically clicked the detailed assessment button. The detailed handler validated symbol and quantile before making its first fetch. The quantile field had no default and was placed in the separate detailed panel. If blank, the handler returned immediately, so DevTools showed no Network request and the quick card remained stuck at its optimistic running message. The exact field value at the operator's failed click was not captured, so this is the source-level cause consistent with no Network activity, not a recorded DOM snapshot.

The feature branch now:

- Moves the quantile control into the quick card and defaults it visibly to 50th percentile (median); operator can change it.
- Replaces the synthetic click chain with one shared async assessment function called directly by both buttons.
- Reports invalid symbol/quantile values in the quick disclosure, rather than leaving a false running state.
- Disables/re-enables both action buttons around the request and mirrors every returned gate row into the quick disclosure.
- Keeps the same API endpoint, feed selection, evidence gates, and no-order behavior.

After hard refresh, a successful click should generate GET /api/providers/status, followed by POST /api/boring-overnight/sell-limit.

### Cold index diagnostics and clock boundary tests

- Store now reports its loaded state, index-build count, last index-build elapsed milliseconds, and unique indexed source-key count.
- Live-assessment response includes those diagnostics under performanceDiagnostics.
- Regression test asserts that a restarted store builds its index once, reads the persisted key, then appends a correction without a second rebuild.
- Added API clock test at the exact final-hour start and exact buy deadline. Existing tests cover before/inside final hour, DST, after-hours, holidays/weekends, and early close.
- Frontend clock logic evaluates Date.now() >= overnightEvaluationTargetUtc && Date.now() < overnightBuyDeadlineUtc on its one-second interval. The code path is deterministic; the operator's actual browser transition at the scheduled Arizona time remains to be observed.

### Next operator actions

1. Pull latest feature/opportunity-quality-classification, verify branch/status, and run .\scripts\Get-Latest-And-Test.ps1. Restart API from the tested build.
2. Hard-refresh Dashboard. Current JS cache key: 20261110-sell-limit-quick-action-index-diagnostics-v3.
3. In BORING OVERNIGHT mode, select RKLX. Confirm the quick card shows research target quantile 50. This is the visible median default; change only if another percentile is intended.
4. Click Assess calculated limit sell once. In DevTools Network confirm GET /api/providers/status then POST /api/boring-overnight/sell-limit. Wait for response; inspect all gates in the disclosure. Save the HTTP status, response body, gate list, and performanceDiagnostics. A BLOCKED response with gate rows is a completed assessment, not a UI failure.
5. After the API restart, run one EVR live-assessment request and include the new fields jsonlIndexLoadCount, jsonlIndexLastLoadElapsedMs, jsonlIndexUniqueObservationKeys, and jsonlIndexLoaded. This reveals whether the measured request triggered the index rebuild or the store was initialized earlier.
6. Run one more EVR request; verify the load count remains 1 and the warm store timings remain low.
7. At the scheduled final-hour start, verify the evaluation countdown becomes OPEN; at the buy deadline it must close. API boundary tests now cover exact start/deadline, but browser runtime acceptance is still required.

### Current limits / uncertainty

- The exact browser DOM quantile value during the previous failed click was not captured; the blank-field early return is the source-code explanation consistent with no Network activity.
- The index rebuild duration is not yet known; previous 7.1ms store read does not prove the rebuild occurred during that request.
- Calculated Limit Sell still needs operator browser acceptance after this fix.
- The final-hour clock has boundary tests but no operator browser observation at the scheduled time.
- Latest code-bearing commit 1f5275c9448eb3b1608fadee847d37ac855044e8 passed GitHub Actions: frontend JavaScript syntax validation, .NET Release build, and .NET tests. Documentation-only commits now advance the branch beyond that tested code SHA; their latest workflow is pending. Local Windows/API/browser acceptance remains pending.
