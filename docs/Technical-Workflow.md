# Technical Workflow — Update, Build, Restart, and Verify MCC

**Purpose:** The repeatable operator workflow to follow after a new commit or release. This is the short operational entry point; the detailed acceptance criteria and specialized procedures remain in `Runtime-Acceptance-Runbook.md`.

## Source-of-truth hierarchy

1. **This document** — the normal sequence for updating, building, restarting, and smoke-testing a new MCC version.
2. **`Runtime-Acceptance-Runbook.md`** — authoritative detailed runtime acceptance criteria, scripts, evidence files, and failure handling.
3. **Feature-specific runbooks** — use only when that feature changed or is being certified (for example WebSocket delivery, Boring Overnight history, provider/feed comparison, or Schwab integration).
4. **`Master-To-Do.md` and `NewChatHandoff.md`** — status and open work, not substitutes for test procedures.

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

1. Open `http://localhost:53360/prototypeIndex.html`.
2. Hard-refresh (`Ctrl+F5`) after frontend changes.
3. Confirm the header, System Readiness, market-session clock, workspace dock, Main Chart, and Red Zone render without horizontal overflow.
4. Confirm the completed 1M timestamp says when the candle closed; the bar-start time is separately identified.
5. Confirm Red Zone refreshes after scanner group changes/imports and that card market assessments refresh automatically. The browser refreshes card assessments every 15 seconds. Alpaca live discoveries refresh every 30 seconds when the Alpaca live-discovery group is selected.
6. Remember: the prototype and production pages share `app.js`; they have separate HTML/CSS layout. A Thinkorswim scan is not automatically imported into MCC.

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