# Runtime Acceptance and Remaining Work Runbook

**Updated:** 2026-11-02 (SIP entitlement result, completed-bar timestamp diagnostic, Schwab approval)  
**Branch:** `feature/opportunity-quality-classification`  
**Purpose:** Turn the outstanding runtime and external-dependency checklist into repeatable evidence collection. This is an acceptance runbook, not a claim that live runtime behavior has passed.

## How to use this runbook

For the normal per-commit/per-release procedure, start with [Technical Workflow](Technical-Workflow.md). It covers the safe get-latest script, Release build/tests, stopping and restarting the API, standard API smoke checks, and browser acceptance.

This runbook remains the detailed acceptance source of truth for specialized runtime tests, expected evidence, failure handling, and market-hours procedures. Do not execute every section after every commit; run the sections relevant to the changed feature or a scheduled certification.

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
- `provider-refresh.json`: fresh provider responses for 1M and 5M, including `barCount` and `newlyPersistedObservationCount`.
- `stored-bars.json`: latest stored RKLX observations for 1M and 5M, filtered locally by provider/feed/timeframe, plus exact timestamp/OHLCV/provenance comparison against the fresh provider response. `zeroNewWritesConsistentWithIdempotentDuplicates=true` is set only when the provider returned bars, the API reported zero new writes, and every fresh bar exactly matches a stored row.
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

The user does not need to manually determine idempotency. The updated script now re-requests the provider bars for both timeframes, then compares each returned bar with the stored latest row by normalized UTC timestamp, symbol, OHLCV, provider, feed, timeframe, and transport. It marks a zero-new-write result as consistent with idempotent duplicates only when every returned bar has an exact stored match. Empty provider responses or mismatches remain inconclusive/false. The current uploaded files alone do not prove that the separate 19:00Z–19:35Z request returned identical payloads.

## 1A. Monday, November 2, 2026 — regular-session WebSocket acceptance

**Target window:** 07:30–07:40 Arizona = 09:30–09:40 America/New_York = 14:30–14:40 UTC. Phoenix remains UTC-07:00; Eastern is UTC-05:00 after the November 1 DST transition.

### Before the open
1. Pull the latest `feature/opportunity-quality-classification` branch and record `git rev-parse HEAD`.
2. Start the MCC API in Visual Studio (F5) or the existing local launch profile. Keep it running throughout the capture.
3. Confirm `GET http://localhost:53360/api/health` returns `status: OK`.
4. Confirm `GET http://localhost:53360/api/market-data/stream/status` shows streaming enabled and SPY, RKLX, ASTX, XRPN in the acknowledged/subscribed symbols. If the app uses HTTPS instead, use `https://localhost:53359`; do not mix the scheme and port.
5. Confirm server-side configuration has `MarketData:Alpaca:StreamEnabled=true`, Alpaca credentials, and the expected initial StreamSymbols. Do not paste credentials into chat or the repository.

### Run at 07:30 Arizona

Open PowerShell in the repository root and run this at or just after 07:30 Arizona on Monday November 2:

```powershell
./scripts/validation/Test-MarketDataRuntime.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbol "RKLX" `
  -Provider "Alpaca" `
  -Feed "iex" `
  -FromUtc "2026-11-02T14:30:00Z" `
  -ToUtc "2026-11-02T14:40:00Z" `
  -WaitSeconds 300
```

The script refreshes the selected symbol's 1M/5M REST history, captures stream/collector/provider status, waits five minutes, captures status again, and saves live assessments for SPY/RKLX/ASTX/XRPN. It writes a timestamped folder under `artifacts/runtime-validation/`.

### What to inspect
- `runtime-before.json` and `runtime-after.json`: stream enabled, acknowledged subscriptions, receivedBars and persistedBars.
- `stream-delta.json`: for each symbol, `acknowledgedBefore/After`, `barCountBefore/After`, `newBarsObserved`, `lastBarBeforeUtc`, `lastBarAfterUtc`, and `advanced`.
- `live-assessments.json`: `oneMinute.sourceTransport`, 1M source timestamp/freshness and 5M REST-backed score/status.
- `provider-refresh.json` and `stored-bars.json`: selected RKLX/Alpaca/IEX provider-to-store history comparison.

### Acceptance rules
- A symbol is a delivery pass only when its subscription is acknowledged **and** its completed-bar count and lastBarUtc advance in the regular-session window. receivedBars/persistedBars should also advance.
- Zero bars for one symbol can reflect no eligible IEX prints; compare with SPY and other symbols and retain feed provenance. Do not call it a global stream failure without evidence.
- A subscription acknowledgement with zero bar advancement is NOT a delivery pass.
- 1M assessment may select WebSocket only after acknowledgement, at least 50 completed persisted WebSocket 1M bars, freshness, correct feed and WebSocket provenance. 5M Permission remains REST-backed.
- Do not lower freshness/history thresholds. If no bar delivery occurs, save the artifacts and report DATA? / NOT VERIFIED; do not manufacture bars or switch feed silently.

## 1B. RTO IEX versus SIP exact-boundary comparison

The capture route uses the configured provider/feed; the request body does not accept a feed override. Tracked `appsettings.json` sets `MarketData:Alpaca:Feed` to `sip`. The user temporarily changed local `secrets.json` to `sip` for diagnosis, then restored it to `iex`; `secrets.json` is the effective provider and currently selects IEX. However, Alpaca returned HTTP 403 with `subscription does not permit querying recent SIP data`. Keep the local runtime on IEX for usable data. Do not repeat the SIP RTO capture until Alpaca enables recent SIP access; a configured feed is not an entitled/validated feed.

### Procedure

1. Preserve the original IEX response and its exact-boundary audit (18/20 pairs; missing next-session opens 2026-10-14 13:30Z and 2026-10-21 13:30Z).
2. First confirm Alpaca's account subscription permits recent SIP data. The user's 2026-11-02 attempt returned HTTP 403 (`subscription does not permit querying recent SIP data`), so the audit is currently blocked. Do not retry while that entitlement response remains unchanged.
3. Once entitlement is confirmed by a successful recent SIP data request, set `MarketData:Alpaca:Feed=sip` in local server-side configuration, restart the API, and confirm provider status and recent bar responses both succeed with observed `feed=sip`. Keep credentials local; do not put them in Git or chat.
4. Run the same 20-session request:

```powershell
$body = @{ symbol = 'RTO'; providerName = 'Alpaca'; requiredWindow = 20 } | ConvertTo-Json
$result = Invoke-RestMethod -Method Post -Uri 'http://localhost:53360/api/boring-overnight/capture-history' -ContentType 'application/json' -Body $body
New-Item -ItemType Directory -Force .\artifacts\runtime-validation | Out-Null
$result | ConvertTo-Json -Depth 20 | Set-Content .\artifacts\runtime-validation\RTO-SIP-20261101.json -Encoding utf8
$result.boundaryCoverageAudit | Format-Table boundaryType, expectedTimestampUtc, providerReturnedExactBar, storeHasExactBar, nearestProviderBarTimestampUtc, nearestProviderBarOffsetMinutes, status
```

5. Verify the returned `feed` is exactly `sip`. If the response reports `iex`, do not treat it as an SIP comparison: the endpoint can reuse a single unambiguous stored feed when the provider returns no bars.
6. Inspect all 40 expected close/open boundaries and specifically the two missing opens. Record the provider exact-bar flag, store exact-bar flag, nearest provider timestamp and status. Preserve the raw JSON and provider error/status if the request fails.
7. Compare the IEX and SIP responses by exact UTC timestamp and same-symbol OHLCV, but keep the two feed series separate. Do not fill the IEX history with SIP rows or combine the two feeds into one rolling 20-pair return series.
8. Restore the local configuration to IEX and restart the API after the comparison.

### Interpretation

- SIP returns the exact bars at both missing timestamps: SIP access is demonstrated for RTO and this historical interval; IEX still has a feed-specific coverage gap. Keep a complete SIP series separate from the IEX series.
- SIP returns other RTO bars but omits one or both exact timestamps: SIP coverage is also incomplete for those boundaries; retain DATA_GAP.
- SIP request returns an explicit authorization/plan error: preserve the exact provider status/message as evidence of an entitlement/access problem. Do not infer more than the provider response establishes.
- SIP returns zero bars or the response feed is not `sip`: comparison is inconclusive. Do not accept the endpoint's possible fallback to stored IEX observations as proof of SIP access.
- Even a successful SIP historical-bar response does not establish live SIP entitlement for every symbol, complete universe coverage, quote coverage, or Schwab-executable fills. Verify the account plan separately.

**Acceptance evidence:** both raw responses, exact branch/API SHA, local feed configuration, returned feed field, request time, exact boundary audit, and any provider HTTP/status error. No threshold tuning until all 20 pairs are exact and the broader validation requirements are met.

---
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


## 1C. SIP entitlement result — 2026-11-02

The user stopped the prior API process, restarted the latest build, and queried `/api/providers/status`.

First response (before correcting local `secrets.json`):
- `configuredFeed=iex`
- `configuredFeedSource=JsonConfigurationProvider for 'secrets.json' (Optional)`
- `appsettings.json=sip`, `secrets.json=iex`

This proved the IEX selection came from the higher-precedence local `secrets.json`, not from the tracked appsettings file.

After changing local `secrets.json` to `sip`:
- `configuredFeed=sip`
- `configuredFeedSource=JsonConfigurationProvider for 'secrets.json' (Optional)`
- Both appsettings and secrets providers report `sip`
- Provider state was `CONFIGURED_NOT_YET_VALIDATED`

The subsequent INO live-assessment request failed with HTTP 502 wrapping Alpaca HTTP 403:
`subscription does not permit querying recent SIP data`.

When the user restored `secrets.json` to `iex`:
- `configuredFeed=iex`
- Provider validation returned to `VALIDATED_DATA_RETURNED`
- The latest validation returned 62 bars.

### Disposition

- [x] Feed configuration precedence root cause identified: local `secrets.json` overrides tracked `appsettings.json`.
- [x] Confirmed that both configuration sources can be set to SIP and the adapter selects SIP.
- [x] Captured explicit provider entitlement denial for recent SIP data.
- [x] Restored IEX and confirmed successful provider validation/data return.
- [ ] Obtain an Alpaca subscription that permits recent SIP data, or choose another independently entitled SIP source.
- [ ] Only after a recent SIP request succeeds, rerun the exact 40-boundary RTO audit and preserve the response's actual `feed` value.
- [ ] Keep IEX and SIP datasets separate; do not mix or impute missing bars.

**Operational recommendation:** leave the local feed on IEX for now. Do not label SIP as available, do not rename the IEX RTO artifact as SIP, and do not run the SIP audit against a 403 response.

## 5. Schwab Trader API approval

The user reports Schwab Trader API — Individual approval on 2026-11-02. This clears the Individual API approval prerequisite only. App creation, Accounts and Trading Production attachment, app-level Active/Ready status, exact callback URL registration, local secret configuration, OAuth, and read-only account/position retrieval remain open. See `docs/Thinkorswim-Schwab-Integration-Plan.md`. Never request or store App Key, App Secret, authorization code, or tokens in chat/Git.
