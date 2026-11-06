# Technical Workflow — Update, Build, Restart, and Verify MCC

**Purpose:** The repeatable operator workflow to follow after a new commit or release. This is the short operational entry point; the detailed acceptance criteria and specialized procedures remain in `Runtime-Acceptance-Runbook.md`.

## Source-of-truth hierarchy

1. **This document** — the normal sequence for updating, building, restarting, and smoke-testing a new MCC version.
2. **`Runtime-Acceptance-Runbook.md`** — authoritative detailed runtime acceptance criteria, scripts, evidence files, and failure handling.
3. **Feature-specific runbooks** — use only when that feature changed or is being certified (for example WebSocket delivery, Boring Overnight history, provider/feed comparison, or Schwab integration).
4. **`Master-To-Do.md` and `NewChatHandoff-20261106.md`** — current status and open work, not substitutes for test procedures.

**You do not need to execute every section of the Runtime Acceptance Runbook after every commit.** Run the standard update/build/API smoke test below every time. Run deeper acceptance only for changed functionality, a scheduled certification, or when a smoke test exposes a problem.

## A. Get latest source and run Release tests

1. Close any editor/build process that may hold files open. The API can remain running for this source-update step, but it must be stopped before restarting after the build.
2. Open PowerShell at the repository root.
3. Confirm your local changes are committed or safely stashed. The script deliberately refuses to overwrite a dirty working tree.
4. Run:

```powershell
.\scripts\Get-Latest-And-Test.ps1
```

The script:
- fetches `origin/feature/opportunity-quality-classification`;
- switches to that branch if necessary;
- fast-forward pulls only (it will not create a merge commit);
- restores the solution;
- builds `MomentumCommandCenterV2.sln` in Release;
- runs the full test suite;
- prints the exact source SHA.

**Pass condition:** script exits successfully, Release build succeeds, and tests report zero failures. Record the SHA. If it stops because the working tree is dirty, inspect/commit/stash your work first; do not force-reset.

GitHub Actions must also pass for the exact branch head before calling the release verified. Local tests and CI are separate checks.

## B. Stop the old API process completely

Do not launch a second API instance over the old one.

- In Visual Studio, use **Debug → Stop Debugging** (Shift+F5) and wait for the process to exit.
- If the API was launched outside Visual Studio, inspect the listener before stopping it:

```powershell
$listener = Get-NetTCPConnection -LocalPort 53360 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    $apiPid = $listener.OwningProcess
    Get-CimInstance Win32_Process -Filter "ProcessId = $apiPid" | Select-Object ProcessId, ExecutablePath, CommandLine
    # Only after confirming this is the MCC API process:
    Stop-Process -Id $apiPid
}
```

Wait until port 53360 is no longer listening. The local launch profile maps HTTP to `http://localhost:53360` and HTTPS to `https://localhost:53359`. Do not mix scheme and port.

## C. Start the newly built API

From the repository root, either press F5 in Visual Studio using the `MomentumCommandCenterV2.API` profile, or run:

```powershell
dotnet run --project .\src\MomentumCommandCenterV2.API\MomentumCommandCenterV2.API.csproj --launch-profile MomentumCommandCenterV2.API
```

Keep the process running while checking the endpoints.

## D. API smoke-test sequence

Use HTTP consistently on port 53360:

```powershell
$base = 'http://localhost:53360'
Invoke-RestMethod "$base/api/health" | ConvertTo-Json -Depth 8
Invoke-RestMethod "$base/api/providers/status" | ConvertTo-Json -Depth 10
Invoke-RestMethod "$base/api/market-data/stream/status" | ConvertTo-Json -Depth 10
Invoke-RestMethod "$base/api/market-data/collector/status" | ConvertTo-Json -Depth 10
```

### 1. `/api/health`

Expect `status: OK`, `mode: decision-support-only`, and `orderSubmission: NOT IMPLEMENTED`.

### 2. `/api/providers/status`

Inspect the complete Alpaca object:
- `configuredFeed` — currently use IEX for usable data;
- `configuredFeedSource` and `feedConfigurationSources` — identify which local config source wins;
- `configured` and `validated`;
- `status`, `lastSuccessfulDataValidationUtc`, and bar count.

The user's local `secrets.json` overrides tracked `appsettings.json`. It currently selects IEX. SIP selection was tested, but Alpaca denied recent SIP data with HTTP 403 (`subscription does not permit querying recent SIP data`). Do not change to SIP until a recent SIP request succeeds. Never paste credentials into chat or commit them.

### 3. `/api/market-data/stream/status`

Check `enabled`, `connected`, `state`, `feed`, `subscribedSymbols`, and per-symbol `barsBySymbol`.

`SUBSCRIBED` confirms connection/subscription only. It does **not** prove bar delivery. Outside regular market hours, zero new bars is not a failure by itself.

### 4. `/api/market-data/collector/status`

This is the historical polling collector, separate from the WebSocket minute-bar stream. Check enabled/configured state, last attempt/success, counts, and errors. It is not a market-wide scanner.

### 5. Focused live assessment

Run for the focused symbol and, when useful, the Red Zone candidates:

```powershell
Invoke-RestMethod "$base/api/market-data/live-assessment?symbol=SPY&providerName=Alpaca" | ConvertTo-Json -Depth 20
```

Inspect 1M and 5M feed/provenance, source timestamps, completed-bar timestamps, freshness/age, ATR/VWAP, 5M Permission, and 1M execution. An HTTP 502 wrapping provider HTTP 403 is a provider entitlement failure, not a frontend rendering defect.

### 6. Scanner discovery health (when validating Red Zone/live discovery)

```powershell
Invoke-RestMethod "$base/api/scanner/discoveries?limit=200" | ConvertTo-Json -Depth 20
```

This is Alpaca's SIP-based top-50 gainers/losers discovery source. It is not the Thinkorswim Overnight scan and not a full-market universe. Inspect `health`, `lastSuccessAt`, `consecutiveFailures`, `count`, `receivedAt`, and `sourceTimestamp`. The current Alpaca SIP entitlement denial can prevent new discovery events; stale persisted events must not be described as current.

## E. Browser acceptance

1. Open `http://localhost:53360/` (the root page served from `frontend/index.html`).
2. Hard-refresh (`Ctrl+F5`) after frontend changes.
3. Confirm the header, System Readiness, market-session clock, workspace dock, Main Chart, and Red Zone render without horizontal overflow.
4. Confirm the completed 1M timestamp says when the candle closed; the bar-start time is separately identified.
5. Confirm Red Zone refreshes after scanner group changes/imports and that card market assessments refresh automatically. The browser refreshes card assessments every 15 seconds. Alpaca live discoveries refresh every 30 seconds when the Alpaca live-discovery group is selected.
6. The root page is the only supported Dashboard entry point. Do not use or recreate `prototypeIndex.html`. A Thinkorswim scan is not automatically imported into MCC.

## F. Market-hours delivery test — only when needed

Run this during regular market hours, not after-hours:

```powershell
.\scripts\validation\Test-MarketDataRuntime.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbol "RKLX" `
  -Provider "Alpaca" `
  -Feed "iex" `
  -FromUtc "<START-UTC>" `
  -ToUtc "<END-UTC>" `
  -WaitSeconds 300
```

Replace the UTC interval with the actual test window. Preserve the complete timestamped output folder. A delivery pass requires acknowledged subscription **and** advancing completed-bar counts/timestamps for the symbols being tested, with received/persisted counts advancing. The exact four-symbol acceptance is SPY, RKLX, ASTX, and XRPN. A zero-bar result for an individual IEX symbol may reflect no eligible prints; compare against the other symbols and preserve feed provenance.

Use the detailed procedure in `Runtime-Acceptance-Runbook.md` §1A. You do not need to run the full 5-minute test after every documentation-only or unrelated UI commit.

## G. Feature-specific checks — do not run indiscriminately

| Change / investigation | Required deeper check |
|---|---|
| WebSocket stream/collector changes | Run §1A and inspect per-symbol deltas and persisted bars |
| Feed/provider adapter changes | Check `/api/providers/status`, then matched provider bars and feed provenance |
| Live assessment/indicator changes | Compare exact same completed candle with TOS; inspect `/api/market-data/live-assessment` |
| Boring Overnight calculation/history changes | Run the 20-pair capture and exact 40-boundary audit; see `docs/BoringOvernight.md` and §1B |
| Red Zone/scanner changes | Check `/api/scanner/discoveries`, collector health, CSV import, automatic refresh and stale-data handling |
| Morning Brief changes | Check `/api/morning-brief` and history only when that feature changes |
| Schwab integration changes | Follow `docs/Thinkorswim-Schwab-Integration-Plan.md`; read-only OAuth acceptance only |

RTO audit is not a routine every-release test. It is a specific historical coverage comparison of 40 expected RTO close/open 1-minute boundaries across 20 sessions. The prior 38/40 result was IEX; SIP comparison remains blocked by Alpaca's recent-SIP HTTP 403. See `Runtime-Acceptance-Runbook.md` §1B.

## H. Release evidence to retain

For each meaningful release, record:
- branch and exact Git SHA;
- local Release build/test result;
- exact-head GitHub Actions run and conclusion;
- API health/provider/stream/collector status;
- tested feed and entitlement;
- market-hours runtime artifacts, if that test was required;
- browser acceptance result;
- known limitations or failed acceptance criteria.

Do not label a release fully runtime-accepted solely because it compiles or because `/api/health` is OK.

## Current known operational constraints (2026-11-02)

- Alpaca IEX is the current usable feed. Recent SIP access was explicitly denied by the current subscription.
- Stream subscription is not proof of stream delivery; test during regular market hours.
- The Alpaca movers discovery source is SIP-based and may be degraded by the current entitlement. It is not the TOS Overnight scan.
- Red Zone candidate assessment refresh and source candidate refresh are separate processes.
- MCC remains decision-support only; order submission is not implemented.

## [Uncertainties]

- Exact user-local `secrets.json` path is environment-dependent; the API's `configuredFeedSource` is the authoritative diagnostic.
- SIP access may change if Alpaca entitlement changes.
- A market-hours delivery test cannot be completed after-hours.

## MCC-native data recovery and daily controls (2026-11-04)

The dashboard now owns the routine data actions. Use PowerShell only for developer maintenance (get latest/build/test/API process diagnostics), not for the ordinary candidate review.

1. **Import the Thinkorswim Overnight Scanner CSV.** Compare imported/rendered rows with the file's data-row count. The export's custom-expression-limit rows are unavailable evidence, not failed candidates. The qualification panel labels rows with any missing required historical metric as `DATA?`; only fully populated rows can be PASS or FAIL.
2. **Fetch Daily + catalyst for top 4.** Click after the CSV is imported and you have reviewed the score-ranked shortlist. It fetches adjusted Daily metrics and the preceding 24 hours of ticker news for only the four highest-scored imported rows. Daily qualification fields require complete, fresh SIP coverage. IEX or incomplete Daily data remains DATA?. Catalyst triage is provisional; no-news is UNKNOWN and material/unclassified results require manual review. This action does not run before every entry, does not monitor an open position, and cannot produce READY.
3. **Fetch / retry 20-session close/open history.** Run for a serious Overnight candidate when its regular-session history is missing or needs refreshing. MCC re-fetches provider 1-minute bars for the exact 20 close/open pairs, persists returned observations, and displays the provider/store exact-boundary audit. For EVR, this is the retry action for the eight missing exact 09:30 ET opens. It succeeds only if the provider returns those exact bars; a bar one to seven minutes later is not a substitute.
4. **Fetch 20-session GTC_EXT sell-window history.** Use from the Calculated Limit Sell panel only after `/api/providers/status` reports effective Alpaca feed SIP. The button preflights the effective feed and does not issue a fetch when it is IEX or unknown. With SIP, it fetches/persists 20 sell windows in bounded 7-day chunks and audits the exact 780 1-minute intervals from 07:00 through 19:59 ET plus each exact prior regular-session close. IEX is partial-exchange diagnostic only. Missing intervals remain missing.
5. **Calculated Limit Sell.** The capture button is evidence acquisition, not the calculation itself. A numeric target remains withheld until all 20 windows are exact and complete on SIP, ATR parity is MATCH, and walk-forward target-touch/adverse-excursion validation is complete. A bar high is not an executable quote or a fill.
6. **Entry vs. exit.** The 5M → 1M handoff is automatically refreshed; do not click it before every entry. Before entry, check 5M permission and then 1M execution confirmation on completed bars. After entry, exit management is a separate position-lifecycle evaluation. MCC currently lacks broker-synchronized open-position state, so the live handoff cannot yet issue a position-specific EXIT decision. Do not reuse a fresh-entry decision as an exit rule.
7. **System Readiness.** Expand the collapsed disclosure in the dashboard header only when you need the provider/integration readiness details.

[Uncertainties]

The local runtime's effective feed may differ from tracked configuration because local secrets/environment settings take precedence. Always trust the current `/api/providers/status` response, not the tracked appsettings value or a requested feed label.


## November 4 UI workflow updates

The Overnight qualification panel supports **MCC Top 4** and **My Top 4**. MCC Top 4 is the four highest imported scanner scores. My Top 4 is exactly four user-selected imported symbols, saved in browser local storage. The Daily + catalyst action queries only the active group. Do not conflate scanner-score ordering with a calibrated Opportunity Quality ranking.

The Morning Command Brief sits beside the product title. System Readiness contains the WebSocket status (and prototype badge on the prototype page). Quick Start is in the prototype's left dock; production currently has no aside/dock, so production Quick Start access is an outstanding layout item rather than a claim of completion.


 
## SIP entitlement recovery and GTC_EXT calculation gate

Alpaca HTTP 403 with `subscription does not permit querying recent SIP data` is an account entitlement rejection. Do not retry the same SIP query or downgrade the qualification workflow to IEX. Activate SIP market-data entitlement with Alpaca, configure `MarketData:Alpaca:Feed=sip` locally (including checking environment overrides), restart the API, and confirm `/api/providers/status` reports SIP. Test one recent SPY bar before bulk recovery. Keep credentials local.

Only then run exact EVR close/open recovery and the GTC_EXT 20-session capture. A complete GTC_EXT sample requires 20/20 sessions, each with the exact prior close and all 780 one-minute bars from 07:00 through 19:59 ET. Missing timestamps remain missing.

The sell-limit calculator is fail-closed: no numeric limit until complete SIP windows and independently matched ATR parity are available. MCC's Wilder ATR implementation is not yet proven equivalent to the installed Thinkorswim study. A matched fixture must use identical symbol, completed timestamp, timeframe, session scope, adjustment and study settings. The integrated calculation API and out-of-sample walk-forward validation remain open. A historical bar high touch is not an actual fill.


## ATR parity evidence export (2026-11-06)

After the API is running from the intended local build, run:

```powershell
.\scripts\validation\Export-AtrParityEvidence.ps1
```

The script calls `/api/health`, `/api/providers/status`, `/api/strategy-report/atr-parity` and `/api/strategy-report/atr-observations` for EVR and CBOE at 1m and 5m. It saves the aggregate parity responses and full persisted Thinkorswim observation rows as separate JSON files, plus a manifest and SHA-256 hashes, under `artifacts/atr-parity-evidence/<timestamp>/`.

Zip the generated timestamp folder when sharing evidence. Do not include credentials. The observation files contain TOS-imported OHLCV/ATR and provenance; they do not contain provider bars and cannot by themselves prove provider/TOS OHLCV equality. The script does not infer why a timestamp is missing or calculate broker fills.


## G. PowerShell browser and market-session diagnostics

Use the dedicated PowerShell scripts for these technical acceptance checks; do not substitute manual DevTools copy/paste unless the script cannot run.

### Browser console/network and screenshot

```powershell
.\scripts\validation\Capture-DashboardBrowserDiagnostics.ps1 -BaseUri "http://localhost:53360/" -CaptureSeconds 120
```

The script attaches to or launches an isolated Edge CDP session, captures browser console/uncaught exceptions, sanitized network request/response/failure metadata, and a screenshot. During capture, interact with the main `frontend/index.html` Dashboard. It does not collect cookies, request headers, response bodies, or query values.

### Regular-session WebSocket and collector

```powershell
.\scripts\validation\Test-RegularSessionStreamCollector.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbols SPY,RKLX,ASTX,XRPN `
  -PollIntervalSeconds 15 `
  -DurationMinutes 5 `
  -ConfirmRegularSession
```

Run during a confirmed regular session. The script checks weekday and 09:30–16:00 ET wall-clock bounds but cannot independently identify exchange holidays or early closes. It polls health, provider, stream and collector status without changing configuration. Review per-symbol bar-count and last-bar timestamp advancement, global received/persisted counters, collector cycles/watermarks/errors, and post-capture assessment provenance. A `SUBSCRIBED` state alone is not a delivery pass.

Detailed criteria: [Browser and Stream Collector Acceptance](Validation/Browser-and-Stream-Collector-Acceptance-20261106.md).

**Feed restriction:** use the current IEX configuration for diagnostic plumbing. Do not retry SIP or request the operator to reconfigure it; repeated HTTP 403 entitlement denial is already established. SIP-dependent qualification remains parked unless entitlement materially changes.
