# New Chat Handoff — 2026-11-13

**Branch:** `feature/opportunity-quality-classification` (never `master`)
**Checklist:** [Master-To-Do.md](Master-To-Do.md) — the single authoritative list
**Rules:** [CLAUDE.md](../CLAUDE.md) (commit and push every change; Markdown and data guardrails)
**Previous handoff:** [NewChatHandoff-20261110.md](NewChatHandoff-20261110.md) (history through Nov 13 morning)

## State at hand-off

| Item | Value |
|---|---|
| Latest code head | `e7c0810` — CI run `43504399098` SUCCESS |
| Local Release tests (Claude, this machine) | 344 / 344 passed |
| Last operator-verified head | `4c743a1` — 302 / 302 |
| Dashboard cache keys | `app.js?v=20261113-field-availability-pool-provenance-v2`, `styles.css?v=20261113-field-availability-v1` |

## What changed in this pass

| Commit | Change |
|---|---|
| `4ac53f7` | `BoringOvernightCaptureInputMapper`: capture-history technical mapping, SIP Daily range/ATR proxy and current-interval classification moved out of `Program.cs`; 26 tests. A JSON `null` feed is now "missing", not `""`. |
| `8445c4e` | JSONL cold rebuild: read/open/gate-wait timings in `performanceDiagnostics`; synchronous 1 MiB buffered read replaces per-buffer async hops. |
| `b5a3283` | Opportunity Quality: the pool mapper hard-blocks exclusion-watchlist symbols and carries per-symbol provenance; 8 full-pool fixture tests; Dashboard field-availability report; provenance on ranking cards; **startup `ReferenceError` fix** (`liveAssessmentInFlight` was declared after the first `render()`). |
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

**2. Start MCC with the launcher** (if an older API is running from Visual Studio or `dotnet run`, stop that first)
```powershell
.\scripts\MCC.ps1 start
.\scripts\MCC.ps1 start     # must say "already running", not start a second copy
.\scripts\MCC.ps1 status    # copy the PID, Source SHA and log paths
```
If the start fails, the launcher prints the last log lines. Paste them.

**3. Cold-load measurement** — immediately after step 2, in the browser or PowerShell:
```powershell
Invoke-RestMethod "http://localhost:53360/api/market-data/live-assessment?symbol=EVR&providerName=Alpaca" | Select-Object -ExpandProperty performanceDiagnostics
```
Run it twice. From the first response, record `jsonlIndexLastLoadElapsedMs`, `jsonlIndexReadElapsedMs`, `jsonlIndexDeserializeElapsedMs`, `jsonlIndexIndexElapsedMs`, `jsonlIndexGateWaitBeforeLoadMs`, `jsonlIndexLoadCount` and `totalElapsedMs`. From the second, record `totalElapsedMs` and `jsonlIndexLoadCount` (it must still be 1).

**4. Dashboard checks** — open `http://localhost:53360/`, hard refresh (Ctrl+F5), DevTools → Network:
- Confirm `app.js?v=20261113-field-availability-pool-provenance-v2` loaded.
- **Intraday Momentum:** the Main Chart cards should fill on the first load, without waiting ~15 s. Note the time in the Console; there should be no `before initialization` error.
- **Boring Overnight:** import the Watchlist Scanner CSV. Under the import strip, open **Field availability**, then click **Export availability JSON** and keep the file.
- Run the Opportunity Quality ranking (top-50 control). Each card should end with `Source Alpaca / iex · 1M bar … · 5M bar … · evaluated …`. Add a test symbol to EXCLUSIONS, rerun, and confirm it shows `EXCLUSION WATCHLIST — BLOCKED`. Remove it afterwards.
- Control contract: click **Fetch & analyze bars**. Network should show `GET /api/market-data/bars` then `GET /api/boring-overnight/snapshots`. Then click **Refresh saved evaluations**. Network should show only `GET /api/boring-overnight/snapshots`.

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

- The in-API cold load after the read-path change is not yet measured on the operator machine.
- The launcher and backup were verified in isolation (Production environment, temp data), not with Development secrets or the real 315 MB data set.
- `calc is not defined` resolution is inferred, not confirmed.
