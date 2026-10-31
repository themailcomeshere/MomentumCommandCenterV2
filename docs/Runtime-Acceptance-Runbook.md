# Runtime Acceptance and Remaining Work Runbook

**Updated:** 2026-10-31 (runtime failure diagnosis and collector recovery procedure)  
**Branch:** `feature/opportunity-quality-classification`  
**Purpose:** Turn the outstanding runtime and external-dependency checklist into repeatable evidence collection. This is an acceptance runbook, not a claim that live runtime behavior has passed.

## 1. Market-data runtime capture

### Automated capture script

Use `scripts/validation/Test-MarketDataRuntime.ps1` against the running API. Run the stored-bar query at any time; run the 300-second stream portion during regular market hours.

**Local Visual Studio Development ports:** `src/MomentumCommandCenterV2.API/Properties/launchSettings.json` maps `https://localhost:53359` to HTTPS and `http://localhost:53360` to HTTP. Do not combine `http` with port `53359`. The user's attempted `http://localhost:53359` was a scheme/port mismatch and explains the connection reset. Use:

```powershell
./scripts/validation/Test-MarketDataRuntime.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbol "RKLX" `
  -Provider "Alpaca" `
  -Feed "iex" `
  -FromUtc "2026-10-30T13:00:00Z" `
  -ToUtc "2026-10-30T19:35:00Z" `
  -WaitSeconds 300
```

The script now checks `/api/health` before collecting data and rejects the known local scheme/port mismatch with a direct message. If using HTTPS instead, use `https://localhost:53359` and ensure the ASP.NET development certificate is trusted by Windows/PowerShell.

Replace the example time window with the exact UTC interval being investigated. The script writes:
- `stored-bars.json`: latest stored RKLX observations for 1M and 5M, filtered locally by provider/feed/timeframe.
- `runtime-before.json` and `runtime-after.json`: stream, collector, and provider status snapshots.
- `stream-delta.json`: per-symbol acknowledged state, bar-count delta, and last-bar timestamps.
- `live-assessments.json`: full live assessment response for each requested symbol.

Keep the entire output directory. Record the tested branch SHA, API deployment SHA, feed entitlement, and whether the interval was RTH or extended hours alongside the artifacts.

### Acceptance criteria

- The API is running the intended branch commit and the stream is explicitly enabled in server-side configuration.
- The status is `SUBSCRIBED`, and the target symbol appears in `subscribedSymbols`.
- During a regular-session window, `barsBySymbol[symbol].barCount`, `lastBarUtc`, `receivedBars`, and `persistedBars` advance as eligible minute bars arrive.
- A WebSocket-backed 1M assessment is accepted only when subscription is acknowledged, at least 50 completed 1M WebSocket observations exist, the latest bar is fresh, and the selected observation has WebSocket provenance. The returned source label must explicitly identify the WebSocket latest bar.
- 5M Permission remains REST-backed.
- A no-bar result outside regular hours, or for a symbol with no eligible prints on the selected feed, is not by itself a stream failure.
- Do not lower freshness thresholds to conceal sparse source coverage. Missing bars are not zero-volume bars.

### RKLX persistence check — uploaded evidence reviewed

The two uploaded repository artifacts are `data/validation/RKLX/MarketData_StoredBars.txt` and `data/validation/RKLX/MarketData_StoredBars_5m.txt`. They contain **13 current stored 1M observations** and **12 current stored 5M observations**, respectively, for RKLX / Alpaca / IEX over **2026-10-30 13:30Z–17:30Z**. Each row carries an observation ID and a `supersedesObservationId`, and the endpoint's note says it returns the latest stored version per provider/symbol/timeframe/feed/source timestamp.

This proves stored membership for those 13 and 12 rows in that artifact window. It does **not** verify the separate four 1M / four 5M observations previously described at 19:00Z–19:35Z: that later interval is outside the uploaded files' 13:30Z–17:30Z range. The script invocation that failed has not yet queried the live API, so the exact four later observations remain unverified.

The user does not need to manually determine idempotency. Once the exact source bars and matching stored rows are compared on provider, feed, timeframe, source timestamp, and OHLCV/content hash, a zero-new-write result for an already-present identical observation is expected idempotent behavior. The current uploaded files alone do not prove that the separate 19:00Z–19:35Z request returned identical payloads.

## 2. Browser acceptance

Run against the user's API-hosted dashboard after pulling the target commit and hard-refreshing. Capture screenshots and browser console output.

- Full-width header and wrapping of demo/stream status indicators.
- Main Chart symbol prominence and chart-side 2x2 Opportunity Status layout.
- Morning Brief header modal and lower collapsed section; both show the same generated report timestamp.
- Morning Brief history endpoint returns persisted snapshots after a refresh/restart.
- Arizona-local replay input converts to UTC correctly; missing required parameters return HTTP 400 with a clear response.
- Red Zone four-column desktop, two-column tablet, and one-column mobile layouts.
- Sound test and new-candidate alert only after the user explicitly enables browser audio.
- No duplicate element IDs, JavaScript errors, or stale cache assets.

Browser acceptance is a user-runtime check. CI build/tests and JavaScript parsing do not substitute for it.

## 3. Morning Brief snapshot persistence

The API now writes each newly generated report to an append-only JSONL store before caching it for the 60-second response cache.

- Default path: local application data, `MomentumCommandCenterV2/morning-brief-snapshots.jsonl`.
- Override: `MCC_MORNING_BRIEF_SNAPSHOT_PATH`.
- Each record contains a unique snapshot ID, generated/recorded UTC timestamps, report version, SHA-256 content hash, explicit source-bar watermarks extracted from the report's marketLandscape (provider/symbol/timeframe/feed/transport plus latest UTC source timestamp), and the complete report JSON.
- The live response includes `snapshotPersistenceStatus`, `snapshotId`, and `snapshotVersion`.
- `GET /api/morning-brief/history?limit=20` returns newest-first immutable snapshots.
- Persistence failure is reported as `snapshotPersistenceStatus=FAILED`; the generated report remains available and the failure is logged.

Version `morning-brief-v1` identifies the report schema generation. This is not yet a full input-manifest archive: raw provider response references/retention policy, news/discovery input watermarks, calculation dependency manifests, and later outcome linkage remain future work. A snapshot preserves unknown/partial states and does not imply that the underlying provider data is complete or validated. Source watermarks are recorded only when the report includes an explicit UTC latest bar with provider, symbol, timeframe, feed, and transport.

## 4. Collector outage, restart, and sleep/wake recovery

### What the status fields mean

- `cycles`: total completed collector cycles since this API process started.
- `lastCycleStartedUtc` / `lastCycleCompletedUtc`: start and completion of the most recently completed cycle.
- `lastCycleErrors`: number of per-provider/symbol/timeframe retrieval or persistence failures in that most recent cycle only. It is not a lifetime total.
- `recentErrors`: rolling queue of up to 20 error messages recorded in this process. It can retain an earlier failure after a later cycle succeeds.
- `newlyPersistedObservations`: cumulative number of newly appended observations since this API process started. It is not the latest cycle's count.
- `watermarks`: latest successfully retrieved source-bar timestamps by provider/symbol/timeframe. They are market-data timestamps, not the time the collector last polled.
- `checkpointPath`: the actual JSON file path used by the worker. It can be overridden with `MCC_MARKET_COLLECTOR_CHECKPOINT_PATH`.
- `checkpointLoadCompleted`, `checkpointFileFound`, `checkpointLoadedEntryCount`, `checkpointLoadCompletedUtc`, and `checkpointLoadError`: explicit startup evidence that the checkpoint load attempt completed, whether a file existed, how many keys were loaded, and whether loading failed. These diagnostics were added to remove ambiguity from restart testing.

### Step 1 — capture the baseline checkpoint

Run this from the repository root while MCC is running:

```powershell
./scripts/validation/Capture-CollectorCheckpoint.ps1 -BaseUri "http://localhost:53360"
```

It writes `collector-status.json`, a copy of `collector-watermarks.json` when present, and `checkpoint-manifest.json` containing the exact source path, file size, last-write time, SHA-256, status watermarks, and checkpoint-load diagnostics. Keep the whole timestamped output folder. Do not edit or delete the live checkpoint.

For the supplied 2026-10-31 status sample:
- `cycles=6`, `lastCycleErrors=0`, and `recentErrors=[]`: the sample shows no recorded cycle error in the latest cycle or retained error queue. It does not prove that error handling works, because no controlled failure was observed.
- `newlyPersistedObservations=0`: no new observations had been appended by this worker process up to that capture. This is cumulative, not a per-cycle counter.
- Configured symbols are SPY, QQQ, IWM, RTO, and AMT. TTE and FMS watermark entries are leftovers from an earlier configuration; the worker does not prune old keys from the checkpoint when symbols are removed. They are not in the current configured-symbol list and should not be interpreted as active subscriptions.
- The timestamps are from Friday 2026-10-30, while the reported cycles completed Saturday 2026-10-31. The collector continues polling on weekends; a successful empty provider response does not advance a source watermark. The sample alone does not prove a missed-bar gap or a recovery event.

### Step 2 — what “confirm cycle errors are surfaced” means

This requires a deliberate provider request failure in a **local test instance**, not a production account. During the failure window, wait for at least one full cycle (the configured interval is 60 seconds in the supplied status). Then query `/api/market-data/collector/status` and verify all of the following:

1. `cycles` increased and `lastCycleCompletedUtc` advanced, showing that a cycle finished.
2. `lastCycleErrors` is greater than zero for the cycle containing the failed request.
3. `recentErrors` contains the failed key, such as `Alpaca/SPY/1m`, and an exception/provider error description.
4. The affected key's `watermarks` source timestamp did not advance during the failed retrieval.
5. No false success is inferred from `newlyPersistedObservations`; it is a cumulative process counter and may include successful writes for other keys.

Restore the provider in the local test instance, wait for the next cycle, and verify that the errors stop incrementing, the affected watermark advances only after bars are successfully persisted, and stored observations cover the missed interval. `recentErrors` may still show the earlier failure because it is a rolling history; judge recovery using the new cycle's `lastCycleErrors`, timestamps, watermark, and stored bars together.

Do not disable the workstation's network adapter or modify a production credential to manufacture a failure. If a safe isolated provider-failure setup is unavailable, leave this test unchecked rather than risking the live environment.

### Step 3 — verify that the checkpoint is reloaded after restart

1. Run the baseline capture script and retain its `collector-watermarks.json` copy and SHA-256.
2. Stop the API cleanly (Ctrl+C in the terminal running the API). Do not delete, rename, or edit the checkpoint.
3. Start the same API project again with the same environment, configured symbols, and checkpoint path.
4. Run the capture script again after the API has started. Wait until `checkpointLoadCompleted=true`.
5. Confirm `checkpointFileFound=true`, `checkpointLoadError=null`, and `checkpointLoadedEntryCount` equals the number of keys in the saved checkpoint JSON.
6. Compare the returned `watermarks` with the saved JSON. During a closed market, values should match exactly. During an open market, a value may be newer because the first collector cycle can already have fetched and persisted new bars; it must not be older than the saved source timestamp.
7. Confirm `lastCycleCompletedUtc` advances and subsequent stored-bar queries show the collector continuing from the restored watermark with the configured overlap.

The new `checkpointLoad*` status fields were added specifically to make this observable; before that change, comparing only a later watermark snapshot could not conclusively distinguish a loaded checkpoint from a fresh initial-lookback run.

### Step 4 — verify outage backfill and sleep/wake recovery

Use the same baseline/status/checkpoint capture around a controlled local test. Record exact UTC start/end of the interruption. After service returns or the machine wakes, verify:
- the collector completes new cycles;
- the relevant watermark moves forward only after successful persistence;
- the stored-bars endpoint contains the returned bars across the interruption window, including the configured overlap;
- repeated polling does not create duplicate current observations for the same provider/symbol/timeframe/feed/source timestamp;
- the checkpoint JSON is atomically replaced and its hash changes only when watermarks change.

The worker requests at most 30 days per catch-up chunk, polls with the configured overlap, and persists bars before advancing each watermark. Sleep/wake testing is separate from a process restart: a restart test does not prove Windows power-resume behavior.

Preserve before/after checkpoint copies, hashes, status JSON, provider/feed, exact UTC interruption interval, and stored-bar query outputs. A passing source inspection or unit test is not a substitute for target-host execution.

## 5. Thinkorswim state parity

The RTO 1M/5M exports prove OHLCV aggregation parity for six windows only. They do not contain the installed study's per-bar state outputs.

Required input: same-symbol, same-date, same-study-version 1M and 5M diagnostics with timestamps and explicit values/reason codes for Permission, READY, RUNNER, WANING, SWING, GET OUT/EOD GET OUT, candidate/trigger, extension danger, READY expiry, re-arm, and re-entry. Include the study inputs/settings and timezone/session context.

Do not reconstruct or label these states from OHLCV alone. Once the diagnostic tape exists, match by candle timestamp and replay the 5M authority first, then evaluate 1M execution against the authoritative 5M state.

## 6. Overnight session-boundary calculation

The current history calculator uses explicit caller-supplied close/open timestamps and persisted 1M observations. It fails closed when pairs are missing. It does not infer an exchange calendar or define the user's eligible overnight window.

Automation is blocked until the session policy is specified and validated:
- Which exchanges/venues and instruments are in scope.
- Exact regular-session close and next eligible session open.
- Whether premarket/after-hours bars participate in the Overnight window.
- Holiday/early-close handling and daylight-saving behavior.
- Whether the boundary price uses the bar open, close, or another source field.
- Required coverage and missing-session behavior.

After those choices, use a maintained exchange calendar and test holidays, early closes, DST transitions, halted symbols, missing first/last bars, and extended-hours gaps. Until then, do not silently invent boundaries or calculate a READY qualification from guessed timestamps.

## 7. External dependencies and product decisions

- **Scanner/provider entitlement:** verify the selected provider's actual product, account entitlement, feed, symbol coverage, rate limits, and licensing with the account owner. A top-movers endpoint is not a full-market scanner or breadth source.
- **Schwab read-only positions:** blocked on Individual Developer approval, app registration, callback URL, OAuth configuration, and secure server-side secret storage. No credentials belong in Git or chat. Order submission remains disabled.
- **Desktop packaging/startup:** requires a target decision (Windows-only vs cross-platform, installer/update mechanism, local API lifecycle, data directory/backup behavior, and whether browser-based UI remains). Do not choose a packaging stack before these requirements are known.

## 8. Current status summary

| Work item | Status | Evidence / blocker |
|---|---|---|
| Morning Brief immutable snapshots and market-landscape source watermarks | Implemented; CI verified | Run 792 passed Release build and 156/156 tests for source commit `61e8483c6cce4cea5e5131e773086db1ff91bd74` |
| RKLX stored-bar membership | Awaiting runtime evidence | Run the PowerShell capture against the user's API |
| Regular-session WebSocket delivery | Awaiting runtime evidence | Run the capture during RTH with stream enabled |
| WebSocket-backed 1M assessment | Awaiting runtime evidence | Must meet subscription, 50-bar, freshness, and provenance gates |
| Dashboard browser acceptance | Awaiting user runtime | Checklist in section 2 |
| Collector outage/restart/sleep-wake | Awaiting controlled host test | Capture script and detailed steps in section 4; status now exposes checkpoint-load diagnostics |
| Thinkorswim state replay | Blocked by missing diagnostics | Export same-bar study-state tape |
| Automated Overnight boundaries | Blocked by session-policy decisions | Explicit policy required before calendar automation |
| Production scanner validation | Blocked by provider/account entitlement | Confirm provider coverage and licensing |
| Schwab read-only integration | Blocked by user-side approval/configuration | See `docs/Thinkorswim-Schwab-Integration-Plan.md` |
| Desktop packaging/startup | Blocked by product requirements | Decide target platform and lifecycle |

## [Uncertainties]

- The user's currently running API deployment, browser state, Alpaca entitlement, and local collector checkpoint are not accessible from this repository session. The supplied status snapshot is static evidence only; no controlled outage, restart, or sleep/wake test has been run here.
- No installed Thinkorswim diagnostic state export is present in the supplied RTO OHLCV files.
- No approved overnight session-boundary policy or desktop distribution target is documented in the current source.
