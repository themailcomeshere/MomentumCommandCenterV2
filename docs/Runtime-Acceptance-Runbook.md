# Runtime Acceptance and Remaining Work Runbook

**Updated:** 2026-10-31  
**Branch:** `feature/opportunity-quality-classification`  
**Purpose:** Turn the outstanding runtime and external-dependency checklist into repeatable evidence collection. This is an acceptance runbook, not a claim that live runtime behavior has passed.

## 1. Market-data runtime capture

### Automated capture script

Use `scripts/validation/Test-MarketDataRuntime.ps1` against the running API. Run it during regular market hours, not after the close.

Example (PowerShell):

```powershell
./scripts/validation/Test-MarketDataRuntime.ps1 `
  -BaseUri "http://localhost:5080" `
  -Symbol "RKLX" `
  -Provider "Alpaca" `
  -Feed "iex" `
  -FromUtc "2026-10-30T19:00:00Z" `
  -ToUtc "2026-10-30T19:35:00Z" `
  -WaitSeconds 300
```

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

### RKLX persistence check

The 2026-10-30 RKLX REST query returned four bars in each timeframe and `newlyPersistedObservationCount=0`. The status is unresolved until the script's `stored-bars.json` confirms whether those exact provider/feed/timeframe/source timestamps already exist. An idempotent zero-new-write result is plausible but is not proof of persisted membership.

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
- Each record contains a unique snapshot ID, generated/recorded UTC timestamps, report version, SHA-256 content hash, and the complete report JSON.
- The live response includes `snapshotPersistenceStatus`, `snapshotId`, and `snapshotVersion`.
- `GET /api/morning-brief/history?limit=20` returns newest-first immutable snapshots.
- Persistence failure is reported as `snapshotPersistenceStatus=FAILED`; the generated report remains available and the failure is logged.

Version `morning-brief-v1` identifies the report schema generation. This is not yet a full input-watermark archive: per-provider/per-symbol source watermarks, raw response retention policy, and outcome linkage remain future work. A snapshot preserves unknown/partial states and does not imply that the underlying provider data is complete or validated.

## 4. Collector outage, restart, and sleep/wake recovery

The historical REST collector already has durable per-provider/symbol/timeframe watermarks, atomic checkpoint replacement, overlap polling, bounded 30-day catch-up chunks, and repeated polling after transient errors.

Controlled runtime acceptance remains open:
1. Capture `/api/market-data/collector/status`, the configured checkpoint path, and a copy/hash of the checkpoint file.
2. Interrupt provider connectivity or make the provider unavailable without stopping the API. Confirm cycle errors are surfaced and no watermark advances on failed retrieval.
3. Restore connectivity. Confirm polling resumes, overlap/backfill retrieves the missed interval, observations append idempotently, and the watermark advances only after successful persistence.
4. Stop the API cleanly, restart it, and verify the checkpoint is reloaded and collection resumes from the watermark with overlap.
5. Put the host to sleep and wake it. Confirm delayed cycles recover without duplicate source observations or silent skipped intervals.
6. Preserve before/after checkpoint files, status JSON, provider/feed, UTC outage interval, and stored-bar query results.

A configured worker or a passing unit test is not proof of restart/sleep-wake recovery on the target host.

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
| Morning Brief immutable snapshots | Implemented; CI pending | Store, history endpoint, tests added in this branch pass candidate |
| RKLX stored-bar membership | Awaiting runtime evidence | Run the PowerShell capture against the user's API |
| Regular-session WebSocket delivery | Awaiting runtime evidence | Run the capture during RTH with stream enabled |
| WebSocket-backed 1M assessment | Awaiting runtime evidence | Must meet subscription, 50-bar, freshness, and provenance gates |
| Dashboard browser acceptance | Awaiting user runtime | Checklist in section 2 |
| Collector outage/restart/sleep-wake | Awaiting controlled host test | Procedure in section 4 |
| Thinkorswim state replay | Blocked by missing diagnostics | Export same-bar study-state tape |
| Automated Overnight boundaries | Blocked by session-policy decisions | Explicit policy required before calendar automation |
| Production scanner validation | Blocked by provider/account entitlement | Confirm provider coverage and licensing |
| Schwab read-only integration | Blocked by user-side approval/configuration | See `docs/Thinkorswim-Schwab-Integration-Plan.md` |
| Desktop packaging/startup | Blocked by product requirements | Decide target platform and lifecycle |

## [Uncertainties]

- The user's currently running API deployment, browser state, Alpaca entitlement, and local collector checkpoint are not accessible from this repository session.
- No installed Thinkorswim diagnostic state export is present in the supplied RTO OHLCV files.
- No approved overnight session-boundary policy or desktop distribution target is documented in the current source.
