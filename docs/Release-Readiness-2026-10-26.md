# MCC Release Readiness — 2026-10-26

**Working branch:** `feature/opportunity-quality-classification`  
**Release posture:** `LOCAL PROTOTYPE / DIAGNOSTIC ONLY`  
**Trading reliance:** `NOT APPROVED`  
**Automated order placement:** Out of scope; no orders submitted by this work.

## What is usable now

- The local .NET application, dashboard/workbench, API scaffolding, and raw market-bar replay foundation are present in the branch.
- The branch includes REST market-data adapters and read-only Alpaca news lookup; live credentials, plan entitlements, live response parity, and production freshness have not been verified.
- Source-controlled XRPN 1M/5M OHLCV can be used for selected offline coverage and aggregation tests.
- Twenty chart-entered bars per timeframe in the October 19 parity worksheets have been compared: 1M = 10 MATCH / 10 ROUNDING ONLY; 5M = 9 MATCH / 11 ROUNDING ONLY.
- Opportunity-quality labels were changed to `QUALITY — HIGH/MODERATE/LOW` and `TIMING — EXTENDED`, separating the diagnostic classification from Thinkorswim `EXECUTION READY`. The source/test changes passed a .NET workflow run on commit `e7e59d4`. The latest branch-head documentation/test workflow also passed on commit `c1895641` (run 40380631692). Any subsequent commit requires a fresh current-head check.

## Release gates before relying on MCC for live decisions

| Gate | State | Evidence required to close |
|---|---|---|
| Current-head build/test | PENDING | Green GitHub Actions run tied to the exact current branch head |
| Provider/live-data proof | BLOCKED | User-configured credentials, real API smoke tests, entitlements, and comparison against Thinkorswim |
| Market-data timestamp/session semantics | PARTIAL | Chart/study/session settings documented; representative bar-open/close/availability convention verified; DST and ETH tests |
| Data completeness and recovery | OPEN | Durable persistence/restart recovery, reconnect/backfill, explicit gap states, failure tests |
| ThinkScript study parity | BLOCKED | Same-symbol/date bars plus installed-study identity and per-bar Permission/READY/extension/re-arm diagnostics |
| Opportunity-quality integration | PARTIAL | Reproducible indicator/volume/range/resistance inputs and replay wiring; quality must not grant execution eligibility |
| Boring Overnight target research | BLOCKED | Source-backed regular close and complete 07:00–20:00 Eastern GTC_EXT eligible-window bars; no unresolved gaps or `DATA_GAP` per row |
| Realized outcome validation | DATA NOT FOUND IN SOURCES | Actual order/fill exports, timestamps, fees and slippage; keep separate from historical touch studies |
| News-assisted unfilled-sell review | PARTIAL | Existing read-only CLI is not a full position monitor; source coverage, official halt status, price/volume/spread and manual decision logging remain |
| Branch integration | CAUTION | Reconcile the diverged feature branch with master via scoped review; do not merge wholesale |

## Data findings that change the release decision

1. The October 2 file uploaded under an XRPN-looking name was actually **HPK 1M**. It has 214 rows across 390 expected minute timestamps (176 absent) and exactly duplicates the October 2 HPK subset in the existing HPK multi-day export. It is now named `data/StrategyReports_HPK_10022026_1M_EntireDay.csv`.
2. The October 2 XRPN 5M file still has no verified same-symbol XRPN 1M partner.
3. The October 19 XRPN 1M/5M pair supports selected OHLCV consistency checks, but its one-minute export has known gaps and its payload does not contain ThinkScript state.
4. The current chart-parity worksheets have 48 unfilled 1M rows and 13 unfilled 5M rows. Missing intervals are not classified as no-trade intervals or chart gaps.
5. `eligible-window-high-excursions.csv` contains placeholders, not sourced market observations. No Boring Overnight target reach rate or alternative limit is justified from it.
6. Historical target touch is not a fill; actual trade outcomes were not found in the audited sources.

## Work that can proceed without user action

- Add automated validation for symbol/date mismatches between report header, embedded payload, and filename.
- Add parser/importer regression tests for missing timestamps, duplicate rows, malformed payloads, and timestamp provenance.
- Keep the October 19 source/parity fixtures and make the gap states explicit.
- Finish opportunity-quality replay wiring only after validated inputs can be produced without look-ahead.
- Continue documentation reconciliation and CI checks; preserve all unknowns rather than synthesizing data.

## Specific evidence still needed from the user / Thinkorswim

1. If October 2 is the desired READY diagnosis: the **XRPN 1M** export for October 2, matching the existing XRPN 5M export, plus per-bar diagnostic states from the installed 1M/5M studies around the observed failure. The HPK upload is not the needed XRPN file.
2. For READY-frequency root cause generally: a representative same-symbol/date 1M and 5M diagnostic export including raw Permission/block code, candidate/trigger, extension danger, READY activation/expiration and re-arm state. The existing screenshot does not need to be re-uploaded.
3. For Boring Overnight research: source-backed regular-session close plus all available bars across the following 07:00–20:00 Eastern GTC_EXT window. If any interval is missing, keep that observation marked `DATA_GAP`.
4. For live-provider verification: configure credentials locally and run the documented smoke test. Do not send API keys in chat or commit them to the repository.

## [Uncertainties]

- Payload timestamp semantics are consistent with the sampled chart clock labels, but bar-open versus bar-close/decision-availability semantics are not fully documented across chart settings.
- The cause of missing 1M timestamps is unresolved.
- READY gate frequency and production study parity are not established by raw OHLCV.
- Full eligible-window overnight reach, actual fill rates, and realized outcomes are not established.
- Current-head CI and branch reconciliation must be verified before promoting the branch.
