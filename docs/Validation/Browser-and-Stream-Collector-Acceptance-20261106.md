# Browser, WebSocket, and Collector Acceptance — 2026-11-06

**Branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html`, served at `http://localhost:53360/`  
**No PrototypeIndex:** the main index is the only supported Dashboard entry point.

## What the operator has already verified

- The visible Dashboard section is named **Daily data audit**.
- The operator selected `RTO,EVR,CBOE` and clicked **Validate Daily data**.
- UI summary: 3 symbols checked; 0 qualification-usable; 3 returned but DATA? / diagnostic-only; 0 API errors.
- All three show 20/20 sessions, zero missing, and FRESH. Feed is Alpaca/IEX and qualification is `DATA? · IEX_PARTIAL`.
- This confirms the audit panel renders observed metrics and provenance and correctly does not promote IEX to qualification-usable. It does not prove independent-source parity or full Dashboard acceptance.

| Symbol | Observed close | Observed SMA20 | Observed 20D average volume | Daily panel |
|---|---:|---:|---:|---|
| RTO | 23.615 | 22.3118 | 134,701 | DATA? / IEX_PARTIAL |
| EVR | 284.600 | 272.1878 | 50,448 | DATA? / IEX_PARTIAL |
| CBOE | 353.280 | 325.0030 | 76,734 | DATA? / IEX_PARTIAL |

Independent comparison:
- RTO and EVR were already compared with the committed 20-session Stock Analysis / S&P Global reference files. Both comparisons failed on IEX; all 20 volume rows and multiple OHLC fields differ.
- CBOE has no independent Daily reference file or parity result in the repository. **Data not found in sources.**
- Do not rerun the same IEX comparison unchanged. Do not retry SIP. The user has repeatedly confirmed that the current Alpaca account cannot access recent SIP data (HTTP 403); treat that as a known external blocker, not an operator action.

## Overnight qualification: what fail-closed means

The **Daily data audit** is a source-quality diagnostic panel. It is not the Boring Overnight candidate decision.

The Boring Overnight qualification panel evaluates separate gates. Imported scanner history is only a shortlist input. A candidate cannot become READY merely because its imported historical metrics pass. Required current price, Daily liquidity, exclusions, catalyst evidence, overnight-session completeness, and current 5M/1M chart state must each be present, fresh, and pass their own rules.

Fail-closed means:
- A known hard-gate failure stays BLOCKED/REJECTED.
- A required value that is missing, stale, incomplete, or unverified remains DATA? / UNKNOWN.
- Unknown catalyst status is not CLEAR.
- Missing bars are not imputed; missing metrics are not replaced with zero.
- A high scanner score, 5M permission, 1M event, or Daily panel result cannot override another failed/unknown gate.
- Final READY remains unavailable until every required gate is affirmatively satisfied.

This behavior is already part of the documented evaluation contract. The remaining acceptance is to inspect the live qualification panel after a current scanner import and capture the actual per-candidate statuses/reasons; the Daily audit output alone does not prove that panel's behavior.

## Latest operator attempt — 2026-11-06 evening

### Browser capture failure

The operator invoked the script from the repository root using `-BaseUri "http://localhost:53359/" -CaptureSeconds 120`. PowerShell found and executed the script at the expected repository path, but `Start-Process` failed at the Edge launch line with “The system cannot find the file specified.” Therefore the failure is not evidence that the PowerShell script path is wrong; the resolved browser executable path was not launchable.

The script has been hardened on the feature branch:
- An explicit `-BrowserPath` must resolve to an existing file named `msedge.exe`.
- Automatic discovery checks standard 32-bit/64-bit/local Edge install paths, Windows App Paths registry entries, and all `Get-Command msedge.exe` matches.
- The selected executable is normalized to an absolute path, printed before launch, and checked again immediately before Start-Process.
- Launch failures now report the exact resolved executable and underlying Start-Process error.

**Not yet captured:** browser console/network events, screenshot, and manifest. The fix has not yet been executed on the operator's machine. If discovery still fails, obtain the actual installed path with `Get-Command msedge.exe -All | Select-Object Source,Definition` or locate `msedge.exe`, then pass its full path with `-BrowserPath`. Do not pass the script path as BrowserPath.

### Stream collector attempt

The operator ran the read-only stream/collector script twice at 2026-11-06 18:52 ET. Both attempts correctly stopped because the time was outside 09:30–16:00 ET. This is expected fail-closed guard behavior and is **not** a collector test result. The next weekday opportunity is Monday 2026-11-09 at 09:30 ET / 07:30 Arizona, provided the exchange is actually open. The operator must still account for holidays and early closes.

### ATR parity reread

The operator restarted/re-read parity using existing persisted observations. Latest responses:
- EVR 5m: `GAPS_PRESENT`, 327 source observations, 20 compared bars, 2 timestamp gaps, MAE 0.0000294330206094, maximum absolute error 0.0000495349247568.
- CBOE 5m: `GAPS_PRESENT`, 338 source observations, 20 compared bars, 1 timestamp gap, MAE 0.0000203865947752, maximum absolute error 0.0000426076307465.

Both maximum errors are within the 0.00005 numeric tolerance, but neither status is MATCH. The detector now excludes the previously identified EVR 00:00–00:10 ET interval wholly inside the scheduled overnight closure; the latest EVR gap count is 2 rather than the earlier 3. Remaining gaps still block continuity certification. Do not impute bars or re-import unchanged source CSVs.

## ATR parity exact-gap diagnostics added

The parity response has been extended with a `timestampGaps` array. Each qualifying gap reports the previous observed bar, current observed bar, expected next bar, first observed bar after the gap, missing interval count and elapsed gap duration in UTC. This does not change the status calculation or make a gap acceptable. It is intended to make chart-side investigation precise without another export/import cycle.

GitHub Actions passed the browser discovery/launch fix on `50945206dd3ac207ab96efbcb9a42911cc13d335` and the ATR gap-detail implementation/test on `5bc2ac57f5f83e00f77930e7ae13374d556828a4`: Release build, 0 warnings/errors, 236/236 tests. This does not execute PowerShell on the operator's machine or compile the ThinkScript in TOS. After CI passes and the operator pulls/builds/restarts, use the PowerShell query in [Master To-Do](../Master-To-Do.md) / [Active Engineering Backlog](../Active-Engineering-Backlog-20261106.md) to retrieve the exact intervals.

## 1. Browser console/network capture

PowerShell script: `scripts/validation/Capture-DashboardBrowserDiagnostics.ps1`

It uses the local Chromium DevTools Protocol (CDP), launches an isolated Microsoft Edge profile when a remote-debugging endpoint is not already available, navigates to the main Dashboard, and records:
- Browser console and uncaught JavaScript exceptions.
- Network request, response status, and failed-request events.
- A Dashboard screenshot.
- A manifest with counts and capture timestamps.

URLs are sanitized to origin/path/query-key names. Request headers, cookies, response bodies, credentials, and query values are not saved.

Run from the repository root:

```powershell
.\scripts\validation\Capture-DashboardBrowserDiagnostics.ps1 `
  -BaseUri "http://localhost:53360/" `
  -CaptureSeconds 120
```

During the 120-second capture, interact with the main Dashboard: switch between Intraday Momentum and Boring Overnight, inspect the Daily data audit, refresh a symbol, and open the relevant qualification/sell-limit panels. The script saves a timestamped folder under `artifacts/browser-diagnostics/`.

Acceptance review:
- No uncaught JavaScript exceptions.
- No unexplained console errors.
- No unexplained same-origin API 4xx/5xx responses or failed requests.
- Confirm the Daily audit API requests return the three selected symbols and the UI reports their actual source/feed/freshness/status.
- Confirm the Boring Overnight panel does not display READY when required gates remain DATA?/UNKNOWN.
- Review the screenshot and network events; a nonzero HTTP error count is evidence to inspect, not an automatic defect (some negative/preflight routes may intentionally reject).

## 2. Regular-session WebSocket and collector capture

PowerShell script: `scripts/validation/Test-RegularSessionStreamCollector.ps1`

This is a read-only polling/evidence script. It does not change feed configuration, create subscriptions, refresh historical bars, or mutate collector settings. It polls API health, provider status, WebSocket stream status, and historical collector status every 15 seconds for ten minutes. It saves all snapshots, per-symbol bar-count/timestamp deltas, collector cycle/watermark deltas, and post-capture live assessments.

Run only during an actual US-equity regular session. The script checks weekday and 09:30–16:00 ET wall-clock bounds, but it cannot independently know exchange holidays or early closes; the operator must confirm the market is open.

```powershell
.\scripts\validation\Test-RegularSessionStreamCollector.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbols SPY,RKLX,ASTX,XRPN `
  -PollIntervalSeconds 15 `
  -DurationMinutes 10 `
  -ConfirmRegularSession
```

Before running:
1. Start the API and main Dashboard; leave the API process running for the full test.
2. Confirm `/api/health` is OK and `/api/providers/status` identifies the actual feed. Do not attempt SIP.
3. Confirm the stream is enabled and the expected symbols are acknowledged in `subscribedSymbols`. If the Dashboard dynamically subscribes symbols, load the symbols in the Dashboard first.
4. Check `/api/market-data/collector/status` for whether the historical collector is enabled and which symbols it polls. The historical collector is separate from the WebSocket stream.
5. The script records `intervalSeconds`. Its default 10-minute window should cover multiple common polling intervals; if the configured collector interval exceeds 300 seconds, increase `-DurationMinutes` to at least twice that interval (rounded up to minutes) so a cycle can be observed.

Acceptance:
- WebSocket subscription acknowledgement is necessary but not sufficient.
- A symbol's WebSocket delivery passes only when it is acknowledged and its completed-bar count and `lastBarUtc` advance during the regular-session interval; global `receivedBars` and `persistedBars` should advance too.
- A symbol with no IEX-eligible print may not advance. Do not infer a global failure from one sparse symbol; compare SPY and the other symbols and preserve IEX provenance.
- WebSocket 1M authority additionally requires at least 50 completed persisted WebSocket bars, freshness, correct feed, and `sourceTransport=ALPACA_WEBSOCKET_LATEST_BAR`. 5M permission remains REST-backed.
- Collector acceptance is separate: it must be enabled/configured, its cycle count and relevant watermarks must advance, and errors must be explained. A disabled collector is NOT TESTED, not a pass.
- Do not infer that a collector poller is the WebSocket stream, or that either is a market-wide scanner.

Artifacts are saved under `artifacts/runtime-validation/stream-collector-<timestamp>/`. Preserve the full folder and report the exact local branch SHA and test interval.

## 3. SIP and repeated data requests

No further SIP requests are part of this acceptance plan. The prior HTTP 403 entitlement result is known and has been reconfirmed by the operator. Continue using IEX for diagnostic plumbing only; keep Daily qualification, consolidated OHLCV parity, SIP movers, and GTC_EXT sell-limit qualification blocked. Revisit only if the user independently reports an actual entitlement change.

## [Uncertainties]

- The supplied Daily panel text is user-reported browser evidence; a screenshot and CDP console/network capture have not yet been committed.
- The CBOE Daily independent reference and comparison are absent.
- Regular-session WebSocket/collector runtime deltas have not yet been captured with the new script.
- Whether the optional historical collector is enabled in the operator's current local runtime is not established by the repository evidence.
