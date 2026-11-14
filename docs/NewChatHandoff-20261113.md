# New Chat Handoff — 2026-11-13

**Branch:** `feature/opportunity-quality-classification` (never `master`)
**Checklist:** [Master-To-Do.md](Master-To-Do.md) — the single authoritative list
**Rules:** [CLAUDE.md](../CLAUDE.md) (commit and push every change; Markdown and data guardrails)
**Previous handoff:** [NewChatHandoff-20261110.md](NewChatHandoff-20261110.md) (history through Nov 13 morning)

## State at hand-off

| Item | Value |
|---|---|
| Latest code head | `d6d52ab` (JSONL startup warm-up); previous code head `e7c0810` — CI run `43504399098` SUCCESS |
| Local Release tests (Claude, this machine) | 345 / 345 passed on `d6d52ab` |
| Operator cold-rebuild measurement (2026-11-13) | 7.3 s (read 0.9 s, deserialize 5.4 s, gate wait 0), down from 23.3 s |
| Last operator-verified head | `4c743a1` — 302 / 302 |
| Dashboard cache keys | `app.js?v=20261113-field-availability-pool-provenance-v2`, `styles.css?v=20261113-field-availability-v1` |

## What changed in this pass

| Commit | Change |
|---|---|
| `4ac53f7` | `BoringOvernightCaptureInputMapper`: capture-history technical mapping, SIP Daily range/ATR proxy and current-interval classification moved out of `Program.cs`; 26 tests. A JSON `null` feed is now "missing", not `""`. |
| `8445c4e` | JSONL cold rebuild: read/open/gate-wait timings in `performanceDiagnostics`; synchronous 1 MiB buffered read replaces per-buffer async hops. |
| `b5a3283` | Opportunity Quality: the pool mapper hard-blocks exclusion-watchlist symbols and carries per-symbol provenance; 8 full-pool fixture tests; Dashboard field-availability report; provenance on ranking cards; **startup `ReferenceError` fix** (`liveAssessmentInFlight` was declared after the first `render()`). |
| `d6d52ab` | `JsonlMarketBarIndexWarmupService` builds the index in the background at API start; `performanceDiagnostics` adds `jsonlIndexLoadTrigger` and `jsonlIndexLoadCompletedAtUtc`. A source-generated JSON context was benchmarked and rejected (no gain). |
| `e7c0810` | `scripts/MCC.ps1` launcher; `POST /api/admin/shutdown` (token + loopback only); `/api/health` reports PID and `sourceSha`; `Backup-MccData.ps1` / `Restore-MccData.ps1`; data inventory. |
| docs | [Control contract](Engineering/Boring-Overnight-Control-Contract-20261113.md), [Provider runtime inventory](Engineering/Market-Data-Provider-Runtime-Inventory-20261113.md), [Interactive Windows operation](Engineering/Interactive-Windows-Operation.md), `scripts/New-ReleaseEvidence.ps1`. |

### Key finding — JSONL cold load

A copy of the operator's `market-bars.jsonl` (138.6 MB, 276,441 lines) loads in **~1.5 s** in an isolated process: read 0.2 s, deserialize 1.1 s, index 0.2 s. The 23.3 s seen in the API on Nov 12 is therefore startup contention, not file size. The new timings show which part remains after the read-path change.

## Operator sequence (do in order)

**1. Update and verify the build**
```powershell
cd C:\Users\jampo\source\repos\MomentumCommandCenterV2
.\scripts\Get-Latest-And-Test.ps1
```
Report the printed SHA and the test totals (expect ≥ 344 passed, 0 failed).

**2. Start MCC with the launcher.** First stop any API you started another way. On 2026-11-13, `status` printed `RUNNING but not started by this launcher ... source SHA UNKNOWN - graceful stop False`, which means the API had been started from Visual Studio or `dotnet run`. Stop it in Visual Studio (Shift+F5), or close the `dotnet` console window. Then:
```powershell
.\scripts\MCC.ps1 status    # must now say NOT RUNNING
.\scripts\MCC.ps1 start
.\scripts\MCC.ps1 start     # must say "already running", not start a second copy
.\scripts\MCC.ps1 status    # must say RUNNING with a real 40-character SHA, PID and two log paths
```
If `start` says the port is in use, something else is still on 53360. The message names the process; stop it and retry. If the start fails, the launcher prints the last log lines. Paste them.

**3. Startup warm-up check.** Wait about 15 seconds after `start`, then run:
```powershell
Get-Content (Get-ChildItem "$env:LOCALAPPDATA\MomentumCommandCenterV2\run\api-*.out.log" | Sort-Object LastWriteTime | Select-Object -Last 1).FullName | Select-String "JSONL market-bar index"
$d = (Invoke-RestMethod "http://localhost:53360/api/market-data/live-assessment?symbol=EVR&providerName=Alpaca").performanceDiagnostics
$d | Select-Object totalElapsedMs, jsonlIndexLoadTrigger, jsonlIndexLastLoadElapsedMs, jsonlIndexReadElapsedMs, jsonlIndexDeserializeElapsedMs, jsonlIndexGateWaitBeforeLoadMs, jsonlIndexLoadCount
```
Expected: the log line reads `JSONL market-bar index ready: trigger STARTUP_WARMUP ...`, `jsonlIndexLoadTrigger` is `STARTUP_WARMUP`, `jsonlIndexLoadCount` is 1, and `totalElapsedMs` is well under 1000. Paste the output.

**4. Dashboard checks.** Open `http://localhost:53360/` and press **Ctrl+F5**. Open DevTools with **F12** and click the **Network** tab. Leave it open for everything below. Typing `app.js` in the Network filter box should show `app.js?v=20261113-field-availability-pool-provenance-v2`. Clear the filter box afterwards.

- **4a. Startup fix (Intraday).** Set the left-side **WORKSPACE** switch to **INTRADAY MOMENTUM** and press Ctrl+F5. The Main Chart cards (1M CLOSE, DAILY VWAP, ATR14, ...) should fill within a few seconds, not after about 15 s. In DevTools, open the **Console** tab: there must be no red `Cannot access ... before initialization` error.
- **4b. Field availability (Boring Overnight).** Switch the WORKSPACE to **BORING OVERNIGHT**. In **Overnight Candidate Workspace**, under "1 - Import the Watchlist Scanner export", click **Import Overnight CSV** and choose today's scanner CSV. A line starting **Field availability: N/M rows complete** appears under the import status. Click it to expand the table, then click **Export availability JSON** and keep the file.
- **4c. Opportunity Quality ranking.** Scroll to the **Red Zone** panel (eyebrow "TOP CANDIDATES - MULTI-PANE MONITOR"). In its button row, click **Rank quality pool - top 50**. The cards appear directly under the status line in that panel and can take a minute for 50 symbols. Each card's last line must start with `Source Alpaca / iex` and show the 1M bar, 5M bar and evaluated times in AZ. The status line shows `(N excluded, N DATA?)`.
- **4d. Exclusion test.** Scroll down to the collapsible **EXCLUSIONS - API-BACKED / MCC exclusion watchlist** panel and click its header to expand it. Type one symbol that appeared in the ranking (pick one that is *not* already excluded), enter the reason `test`, and click **Add exclusion**. Scroll back to Red Zone and click **Rank quality pool - top 50** again. That symbol's card must now read `QUALITY — EXCLUDED`, list `EXCLUDED_WATCHLIST` first in its reasons, and end with `EXCLUSION WATCHLIST — BLOCKED`. Go back to EXCLUSIONS and remove the symbol.
- **4e. Network order (control contract).** Stay in **BORING OVERNIGHT**. In DevTools Network, click the clear icon (the circle with a slash). In **Overnight Candidate Workspace**, the form row has Symbol / Bar interval / From / To. Click **Fetch & analyze bars**. Network must list `bars?symbol=...` and then `snapshots?limit=50...`. Clear again and click **Refresh saved evaluations**. Network must list only `snapshots?...`. Clear again and click **Fetch / retry 20-session close/open history**. It must list one `capture-history` (method POST; add the Method column by right-clicking the Network column headers if it is hidden), followed by `snapshots?...`. Report each list. Other background requests (for example `live-assessment`, `clock`, `stream/status`) may also appear; ignore them.

**5. Regular-session capture (06:30–13:00 Arizona)**
- Focus CRBG. Run `GET /api/market-data/live-assessment?symbol=CRBG&providerName=Alpaca` and report `evaluationStatus`, `dataFresh`, both freshness ages/limits and both feeds.
- Click **Fetch / retry 20-session close/open history**. Report `technicalAssessment.intradayBarsUsable`, `v2DecisionUsable`, `feedMatchesCapture`, the five technical fields, `currentInterval.state` and the final boundary-audit row (`auditStatus`, `endpointAvailableAtUtc`, `auditedAtUtc`).
- Confirm no `calc is not defined` message.

**6. Stop, back up, staged restore**
```powershell
.\scripts\MCC.ps1 stop                 # expect "stopped gracefully"
.\scripts\Backup-MccData.ps1           # note the folder and the copied MB
.\scripts\Restore-MccData.ps1 -BackupPath "<folder printed above>"   # staged; live data untouched
```

**7. Sign-out check:** sign out of Windows and back in, then run `.\scripts\MCC.ps1 status`. It should say NOT RUNNING. Run `start` and confirm the collector advances (`GET /api/market-data/collector/status`) without errors.

**8. Release evidence:** with MCC running, run:
```powershell
.\scripts\New-ReleaseEvidence.ps1 -BackupManifest "<backup folder>\manifest.json" -Blocker "SIP entitlement (HTTP 403)","TOS compile not evidenced"
```
Then commit and push the two files it writes under `docs\Validation\Release-Evidence\`.

## Open blockers (unchanged)

SIP entitlement (403); no independent consolidated Daily/OHLCV source; TOS compile and parity not evidenced; Boring Overnight READY semantics not frozen (buy time, endpoint, auction print); catalyst sample too small; Schwab and broker reconciliation parked.

## Uncertainties

- Operator measured 7.3 s in-API after the read-path change; the startup warm-up (`d6d52ab`) is not yet measured on the operator machine.
- The launcher and backup were verified in isolation (Production environment, temp data), not with Development secrets or the real 315 MB data set.
- `calc is not defined` resolution is inferred, not confirmed.
