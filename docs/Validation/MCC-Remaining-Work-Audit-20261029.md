# MCC Remaining Work — Engineering Audit and Release Gate
**Reviewed:** 2026-10-29  
**Branch:** `feature/opportunity-quality-classification`  
**Scope:** Repository implementation/source audit plus the user-supplied live post-market control result. No local browser, live Thinkorswim session, provider account, or deployed API runtime was available to this review.

## Executive result

The user-run Post-market control test completed as designed. SPY returned two Alpaca/IEX observations (20:00Z and 20:54Z); RTO returned none. The later SPY timestamp establishes that the route returned at least one post-market observation for SPY in that window. It does not establish complete session coverage, RTO entitlement, or why RTO was empty. The dashboard correctly preserves those distinctions.

The six requested workstreams were audited. **None can honestly be marked fully complete from repository evidence alone.** Several have source scaffolding or partial logic; the remaining blockers are listed below. No trading logic was changed to force apparent parity, no provider was invented, and no missing data was synthesized.

## Checklist and findings

### 1. 5M Permission → 1M execution parity — PARTIAL / NOT CERTIFIED

- [x] Compared repository 5M authority and 1M consumer sources; detailed comparison remains in `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`.
- [x] Confirmed 1M reconstructs a 5M layer from secondary 5-minute OHLCV; it does not consume the actual 5M study's internal Permission latch.
- [x] Recorded concrete predicate differences: raw Permission, structural damage, and major-failure conditions are not textually identical. In particular, the 1M reconstructed major-failure predicate omits the explicit Daily VWAP condition used by the 5M source.
- [x] Confirmed canonical 1M READY is a finite execution window gated by reconstructed 5M execution permission and extension/invalidation checks. Candidate, reconstructed 5M BUY, and READY are distinct states.
- [ ] Compare exact installed study sources, versions, and input overrides against repository sources.
- [ ] Validate matched 5M/1M events on identical symbol/date/time bars, including permission revoke/recovery, candidate, READY expiry, extension danger, continuation/re-entry, and EOD.
- **Gate:** Source comparison is complete; behavioral and installed-study parity is not. Preserve existing gates until matched evidence supports a change.

### 2. Runner management and exit-state validation — SOURCE AUDIT ONLY / NOT CERTIFIED

- [x] Confirmed 5M is designated authority for runner, waning, swing, and failure states; 1M consumes/reconstructs 5M context and also has short-term exit protections.
- [x] Confirmed the 5M source header describes an explicit BUY-established latch before runner states are displayed.
- [ ] No reproducible matched-bar state-transition audit currently proves RUNNER → WANING → SWING / failure / exit behavior across the actual installed studies.
- [ ] Validate 1M short-term invalidation and 5M structural exit separately; record which timeframe owns each transition and whether a 1M exit can occur while 5M runner remains intact.
- [ ] Add a state-transition fixture/replay test only after authoritative expected transitions are captured from installed-study evidence.
- **Gate:** Do not infer position-management behavior from a state label or header comment alone. No exit threshold was changed.

### 3. Broad-market scanner discovery — NOT IMPLEMENTED END-TO-END

- [x] Confirmed provider-neutral discovery contracts, event ingestion, JSONL event persistence, and a polling-worker scaffold exist.
- [x] Confirmed the polling worker's own documentation says no provider implementation or background host is wired.
- [ ] The current scanner universe path is not evidence of market-wide discovery. A manually supplied/configured symbol universe is not a broad market scanner.
- [ ] Select and configure a licensed market-wide movers/scanner source; verify universe breadth, premarket/regular/after-hours support, pagination, rate limits, symbol normalization, and timestamps.
- [ ] Implement provider adapter + hosted worker + health/status endpoint + persisted discovery events + UI integration.
- [ ] Test duplicates, late/out-of-order discoveries, provider pagination, empty batches, source failures, and source freshness.
- **Gate:** No broad-market discovery source was configured or validated in this repository session. Data not found in sources for a selected production scanner provider/entitlement.

### 4. Collector recovery, backfill, and durable ingestion — PARTIAL IMPLEMENTATION / NOT VALIDATED

- [x] Confirmed an API-hosted collector polls configured symbols for 1m and 5m bars, uses overlap windows, stores source observations, and saves per-provider/symbol/timeframe watermarks to an atomic checkpoint file.
- [x] Confirmed the raw JSONL store is append-oriented, idempotent for identical observations, and retains changed observations as corrections.
- [x] Confirmed collector polling is explicitly configured-symbol historical REST polling, not market-wide discovery or websocket streaming.
- [ ] Observation-store identity currently keys provider/symbol/source timestamp, not timeframe. The collector watermark distinguishes 1m/5m, but persisted observations do not. Do not claim timeframe-safe retrieval or use stored bars for timeframe-specific calculations until the schema/query identity is migrated with backward-compatible handling.
- [ ] The worker advances a watermark only when a non-empty batch includes a newer bar. It has no explicit completed-through watermark for successfully queried empty windows. Recovery behavior for long outages, holidays, provider truncation, and persist-then-checkpoint crash windows needs controlled fault-injection tests.
- [ ] Validate checkpoint corruption/recovery, partial write/restart, long outage catch-up, chunk boundaries, duplicate/late/corrected bars, multiple provider feeds, concurrent reads, disk-full/permission errors, and process restart against a durable path.
- [ ] Add explicit collector health/lag/backfill state and operational alerts; current status is useful diagnostics but is not a complete recovery proof.
- **Gate:** Useful foundations exist; end-to-end recovery and durability are not validated. Data not found in sources for production recovery test evidence.

### 5. Morning Brief enrichment — SPECIFICATION ONLY / NOT IMPLEMENTED END-TO-END

- [x] Confirmed `docs/Morning-Report-and-Routine.md` specifies benchmark direction, breadth, volatility, sectors, movers, sourced catalysts, freshness, and immutable snapshots.
- [ ] Current endpoint/UI does not demonstrate live benchmark health, broad movers, breadth methodology, sector ranking, or a connected sourced-catalyst feed.
- [ ] Integrate timestamped benchmark observations (SPY/QQQ/IWM and volatility context), a defined breadth denominator/method, broad-market movers, sector/industry aggregates, and licensed news/catalyst source.
- [ ] Show source, publication time, observation time, age, coverage, and explicit DATA? / UNKNOWN for missing or stale components.
- [ ] Persist immutable, versioned Morning Brief snapshots and add deterministic tests for stale, partial, unavailable, and conflicting inputs.
- **Gate:** Do not display STRONG/NORMAL/WEAK regime or catalyst claims until the underlying sources and methodology are validated. Data not found in sources for a configured production catalyst source or validated breadth feed.

### 6. Browser-level dashboard regression and release validation — AUTOMATED CI PASS / BROWSER NOT VALIDATED

- [x] The post-market comparison HTML/JS/CSS and documentation changes are committed.
- [x] GitHub Actions run `40966465421` completed successfully for the dashboard documentation commit. The preceding UI, JS, and CSS commits also have successful workflow runs.
- [ ] No browser-level run, screenshot comparison, accessibility pass, or end-to-end API/UI test was executed in this review.
- [ ] Validate all workspaces and key flows: scanner/universe, MarketWatch, Morning Brief, Boring Overnight, symbol/timeframe changes, empty/error/stale states, snapshot retrieval, provider failures, and post-market control comparison.
- [ ] Verify responsive layouts, console/network errors, keyboard operation, table overflow, stale-value clearing, and API restart behavior.
- [ ] Run the full .NET test suite and browser suite against the exact final branch head; capture commit SHA and run URLs.
- **Gate:** CI success for recent commits is not browser/release validation. Release remains blocked pending the local/browser test matrix.

## Post-market control test — result supplied by user

- RTO: Alpaca, 0 bars, feed unknown, no source timestamps.
- SPY: Alpaca/IEX, 2 bars, first 2026-10-28T20:00:00Z, latest 2026-10-28T20:54:00Z.
- 2026-10-28 was EDT (UTC−4): 20:00Z was 16:00 EDT, the regular-session close boundary; 20:54Z was 16:54 EDT, post-market.
- Interpretation: IEX returned at least one SPY post-market observation in this requested window, but coverage is sparse. RTO's empty response remains cause-unknown. This does not establish full post-market coverage, SIP entitlement, or absence of RTO trading.
- The dashboard's separate “Requested bar coverage” panel still shows NOT LOADED / DATA? because no request was run for that panel. The comparison test is not a qualification request and does not populate the 20-session Overnight evaluator.

## Exact validation plan

1. **Thinkorswim parity:** Use the existing instructions in `docs/Validation/5M-to-1M-User-Validation-Steps.md`. Supply the complete installed 5M and 1M sources, input overrides, and matched chart captures for one revoke/recovery or READY event. Compare same-symbol/same-date bars and 5M candle boundaries.
2. **Runner/exit:** On the same historical interval, capture 5M Permission, runner/waning/swing/failure labels and 1M READY/invalidated/exit labels before, during, and after each transition. Record the expected transition sequence; then encode it as a replay fixture.
3. **Scanner:** Choose an entitled broad-market discovery provider. Configure credentials locally (never commit them). Run one full premarket and one regular-session poll; record page counts, symbols, source timestamps, freshness, errors, and duplicate behavior.
4. **Collector:** Set explicit temporary store/checkpoint paths. Run with a small test universe; stop/restart the API; simulate a multi-hour outage and verify overlap/backfill. Inspect stored observations for corrections and duplicate suppression. Repeat with an empty response and a provider error. Confirm timeframe identity is resolved before consuming persisted data for timeframe-specific metrics.
5. **Morning Brief:** Once feeds are configured, run with fresh, stale, missing, and partially available inputs. Verify every field carries source/age/coverage and the regime stays UNKNOWN when required inputs are missing.
6. **Browser/release:** Pull the final branch, restart API, hard-refresh, execute each workspace test, inspect browser console/network, and capture screenshots. Run `dotnet test MomentumCommandCenterV2.sln` (or the repository's actual solution path) and record the exact commit SHA. The browser suite must run against that same SHA.

## Release disposition

**NOT RELEASE-READY.** The diagnostic dashboard control test works in the user-supplied run and recent GitHub Actions workflows passed. The six workstreams above still contain explicit validation/integration blockers. No final decision engine, broad-market scanner, fully enriched Morning Brief, installed Thinkorswim parity certification, or browser-tested release should be represented as complete.

## [Uncertainties]

- Installed Thinkorswim study sources and input settings may differ from repository copies; actual chart state cannot be read from this repository.
- The selected production scanner/news provider, account entitlement, and licensing scope are not specified in source.
- Collector recovery depends on the local API configuration, durable file paths, provider history limits, and runtime failure modes; no controlled local runtime test was available.
- Browser behavior and API/UI integration may differ from static source inspection; no browser automation/runtime was available.


<!-- Follow-up: CI workflows for the prior documentation commits were confirmed green on 2026-10-29. -->
