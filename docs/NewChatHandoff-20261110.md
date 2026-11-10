# November 10, 2026 — Operator feedback and engineering handoff

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Working branch:** `feature/opportunity-quality-classification`  
**Authoritative Dashboard:** `frontend/index.html` served from the API root  
**Scope:** Boring Overnight unless explicitly noted

## Summary

The November 10 operator run exposed a stale-price presentation issue, missing visibility into sell-limit evidence gates, zero-bar scanner results on IEX, and a BAYRY Daily response-envelope failure. UI and API changes have been committed to the feature branch. Do not merge to `master` as part of this work.

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
- The existing 1M/5M ATR parity StrategyReport is not the intended input. A StrategyReport is acceptable only if it contains those exact daily OHLC rows and no order-event rows.
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

The Yahoo endpoint initially had a compile failure caused by a named-argument typo in its error response. That typo was corrected in the next commit. Subsequent source/test commits triggered new workflows. The latest exact-head workflow must be checked before claiming a full pass. Local Windows build, browser acceptance, provider entitlement and market-hours stream tests remain pending.

## [Uncertainties]

- Yahoo Finance post-market metadata may be delayed, stale, unavailable, or rate-limited. The UI labels it as a secondary reference and displays its source timestamp.
- Alpaca IEX may not return trades for symbols whose activity is on other exchanges. Missing IEX observations are not market-wide absence.
- The operator's RKLX values ($9.14 then $9.07) are user-observed values; independent online snapshots reviewed during this pass differed. Local timestamped quote comparison is required.
- No Boring Winner source CSV has yet been imported in this turn; no actual PASS/FAIL result is claimed.
- No local runtime/browser test has yet been performed against these latest commits.
