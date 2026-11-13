# Interactive Windows Operation — Launcher, Data Inventory, Backup/Restore

**Created:** 2026-11-13
**Decision (2026-11-11):** MCC runs only while the operator is logged in. No Windows Service and no unattended-after-sign-out operation.

## 1. Durable-path inventory (source-verified 2026-11-13)

Every store resolves its environment-variable override first. Default root: `%LOCALAPPDATA%\MomentumCommandCenterV2\`. The same table is the data source for `scripts/lib/MccDataInventory.ps1`.

| Store | Default file | Override | Kind | Owner (source) |
|---|---|---|---|---|
| Market bars | `market-bars.jsonl` | `MCC_MARKET_BAR_STORE_PATH` | append-only JSONL | `JsonlMarketBarObservationStore` |
| Scanner discoveries | `scanner-discoveries.jsonl` | `MCC_SCANNER_DISCOVERY_PATH` | append-only JSONL | `JsonlScannerDiscoveryEventStore` (wired in `Program.cs`) |
| Boring Overnight snapshots | `boring-overnight-snapshots.jsonl` | `MCC_BORING_OVERNIGHT_SNAPSHOT_PATH` | append-only JSONL | `BoringOvernightCandidateSnapshotStore` |
| Morning Brief snapshots | `morning-brief-snapshots.jsonl` | `MCC_MORNING_BRIEF_SNAPSHOT_PATH` | append-only JSONL | `MorningBriefSnapshotStore` |
| Catalyst human reviews | `catalyst-human-reviews.jsonl` | `MCC_CATALYST_REVIEW_PATH` | append-only JSONL | `CatalystHumanReviewStore` |
| TOS ATR observations | `thinkorswim-atr-observations.jsonl` | `MCC_TOS_ATR_STORE_PATH` | append-only JSONL | `JsonlThinkorswimAtrObservationStore` |
| TOS Daily gap reference | `tos-daily-gap-reference.jsonl` | `MCC_TOS_DAILY_GAP_PATH` | append-only JSONL | `ThinkorswimDailyGapCalculator` store |
| Collector watermarks | `collector-watermarks.json` | `MCC_MARKET_COLLECTOR_CHECKPOINT_PATH` | rewritten JSON | `MarketDataCollectionWorker` |
| Exclusion watchlist | `src/MomentumCommandCenterV2.API/App_Data/exclusions-watchlist.json` | none (content root) | rewritten JSON, tracked in git | `ExclusionWatchlistStore` |

**Not durable server data (deliberately excluded from backup):**
- Secrets: user-secrets id `MomentumCommandCenterV2-API-Local` (`%APPDATA%\Microsoft\UserSecrets\…\secrets.json`) and `ALPACA_API_KEY`, `ALPACA_API_SECRET`, `MASSIVE_API_KEY`. User secrets load only when `ASPNETCORE_ENVIRONMENT=Development`.
- Browser `localStorage` (per browser profile): `mcc.scannerImport.v1`, `mcc.redZoneState.v1`, `mcc.workspaceMode.v1`, `mcc.watchlist`, `mcc.dashboardFeedback.v1`, `mcc.overnightShortlists.v1`.
- Logs: the API logs to the console only (`appsettings.json` has no file sink). When started by the launcher, stdout/stderr go to `%LOCALAPPDATA%\MomentumCommandCenterV2\run\api-<UTC>.out.log` / `.err.log`; the newest 10 pairs are kept.
- Imported source files (scanner CSV, StrategyReport CSV, TOS Daily CSV) are read by the browser or uploaded; the API persists the parsed observations above, not the original files. Keep the originals yourself if you need them.

Configuration: `appsettings.json` (`MarketData:Alpaca:StreamEnabled`, `StreamSymbols`, `Feed=iex`); any key can be overridden with `MarketData__Alpaca__…` environment variables.

## 2. Launcher — `scripts/MCC.ps1`

```powershell
.\scripts\Get-Latest-And-Test.ps1     # builds Release; the launcher runs that build
.\scripts\MCC.ps1 start               # single instance; waits for /api/health; opens the Dashboard
.\scripts\MCC.ps1 status              # PID, source SHA, health, log paths
.\scripts\MCC.ps1 stop                # graceful stop; force only after 30 s, and says so
.\scripts\MCC.ps1 open                # open the Dashboard
```

- **Single instance:** a PID file (`run\api.pid.json`) records PID + process start time (guards against PID reuse). A second `start` reports the running PID and does not launch. If another process owns the port, `start` refuses and names it.
- **Visible startup failure:** if `/api/health` does not answer within 90 s or the process exits, the launcher prints the exit code and the last 30 lines of both logs, and exits non-zero.
- **Graceful stop:** `start` generates a random 256-bit token, passes it to the API as `MCC_SHUTDOWN_TOKEN`, and stores it in `run\shutdown.token`. `stop` calls `POST /api/admin/shutdown` with header `X-MCC-Shutdown-Token`. The endpoint is disabled (404) without a token, accepts only loopback callers, compares in constant time (`LocalShutdownPolicy`, unit-tested), and calls `IHostApplicationLifetime.StopApplication()` so hosted workers get their normal shutdown.
- **Runtime SHA:** `/api/health` now reports `processId`, `processStartedAtUtc`, `sourceSha` (from `git rev-parse HEAD`, suffixed `+DIRTY` when the working tree has changes) and `launchedBy`. This is the "API process SHA" release gate.
- **Sign-out:** the API is a child process of the user session; Windows ends it at sign-out. Next login: run `start` again; the collector resumes from `collector-watermarks.json`.

**Verified 2026-11-13 (isolated: port 53991, Production environment, temp data paths, stream disabled):** start → health OK with SHA; second start refused (same PID); status; unauthenticated shutdown → 403; stop → "stopped gracefully" with `Application is shutting down...` in the log; status → NOT RUNNING; caller environment variables restored. **Not yet verified:** operator machine with Development secrets, sign-out behavior, watermark resume without duplicates.

## 3. Backup and restore

```powershell
.\scripts\MCC.ps1 stop
.\scripts\Backup-MccData.ps1                              # -> Documents\MCC-Backups\mcc-backup-<UTC>\
.\scripts\Restore-MccData.ps1 -BackupPath <folder>        # verify + staged copy; live data untouched
.\scripts\Restore-MccData.ps1 -BackupPath <folder> -ToLive  # API must be stopped
```

- `manifest.json` records each file's source path, override flag, bytes, last write time, SHA-256 and whether the last JSONL line is incomplete.
- Backup refuses while the API is listening unless `-AllowRunning`.
- Restore verifies every hash before writing anything. A mismatch aborts with "nothing was restored".
- `-ToLive` moves each live file to `%LOCALAPPDATA%\MomentumCommandCenterV2\pre-restore-<UTC>\` before writing, re-hashes every restored file, and deletes nothing.

**Verified 2026-11-13 in a temp data root:** backup (4 present, 5 absent, incomplete last line flagged); staged restore; a tampered backup was rejected with "SHA-256 mismatch"; a live restore replaced a changed file and preserved the old copy in `pre-restore-…`; the git-tracked exclusions file was restored byte-identical. **Not yet verified:** a backup of the operator's real ~315 MB data set, restore on a different machine, backup while the API is writing.

## 4. Remaining acceptance (operator)

1. `.\scripts\MCC.ps1 start` on the real machine (Development) → Dashboard opens; `status` shows the SHA.
2. Run `start` again → refused. `stop` → "stopped gracefully".
3. Sign out and back in → `status` says NOT RUNNING; `start`; confirm collector watermarks advance with no duplicate observations (`newlyPersistedObservationCount` 0 for already-stored bars).
4. `stop`, `Backup-MccData.ps1`, then a staged `Restore-MccData.ps1`; check that the reported sizes match.
5. Recovery drills still open: crash (kill the PID), sleep/wake, network loss, provider errors, disk full, duplicate stream replay.
