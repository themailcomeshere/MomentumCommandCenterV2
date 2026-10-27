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
5. The prior placeholder rows in `eligible-window-high-excursions.csv` have been removed; it is now header-only with zero observations. No Boring Overnight target reach rate or alternative limit is justified from it.
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


## Follow-up evidence update — 2026-10-26

- [DONE — XRPN OHLCV PAIR] The intended data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv is present on the feature branch. It has 391 consecutive 1M payload rows from 06:30–13:00 Arizona. Against the October 2 XRPN 5M export, 78 complete 5M windows have full 1M coverage and exact OHLCV aggregation match; the last 5M timestamp has no complete forward 1M group. This closes the same-symbol OHLCV-pair gap, not the ThinkScript diagnostic-state gap.
- [BLOCKED — THINKSCRIPT DIAGNOSTICS] The two reports contain OHLCV transport payloads only. Per-bar Permission/block-code/candidate/extension-danger/READY/re-arm/expiration states: Data not found in sources. Keep study parity gate blocked until diagnostics are exported or their exact uploaded path is identified.
- [PARTIAL — ALPACA] User reports credentials were previously saved and 87/87 tests passed. This is not yet live-provider verification. Follow the exact API smoke-test steps in docs/Next-Steps-and-User-Inputs.md and return sanitized HTTP status, bar count, source timestamps and feed. No secrets in chat.
- [PARTIAL — OVERNIGHT RESEARCH] Reviewed the 2026-10-26 scanner screenshot and identified TTE, RTO, NNN, OKE, AHR, FMS as initial chart-review candidates, with CCI secondary. Chart screenshots alone do not establish target-touch rates, catalyst clearance, or actual fills. Source-backed close and full 07:00–20:00 Eastern window bars with gaps preserved as DATA_GAP are still required.

## [Uncertainties]

- Exact installed ThinkScript diagnostic fields remain absent from the OHLCV files.
- The user's 87/87 test result is reported but not independently observed in this session; no live Alpaca response has been returned yet.
- Overnight candidate ranking is preliminary and limited to visible scanner fields; source-backed catalyst/news and fill evidence remain unavailable.


## User-provided verification and correction — 2026-10-26

- [PASS — API PROCESS] /api/health returns OK in decision-support-only mode. Order submission is not implemented.
- [PASS — TEST SUITE, USER-REPORTED] dotnet test MomentumCommandCenterV2.sln --configuration Release: build succeeded, 87 passed, 0 failed, 0 skipped.
- [BLOCKED — LIVE PROVIDER] /api/providers/status reports Alpaca and Massive NOT_CONFIGURED. /api/market-data/compare returned zero bars/observations and explicit missing Alpaca key/secret configuration. Do not claim live verification passed. API process and tests pass, but provider authentication/data retrieval has not succeeded.
- [NEXT — ALPACA CONFIG] API project's UserSecretsId is MomentumCommandCenterV2-API-Local. Set the existing Alpaca key and secret with dotnet user-secrets set "MarketData:Alpaca:ApiKey" "<YOUR_EXISTING_ALPACA_KEY>" --project src/MomentumCommandCenterV2.API and corresponding MarketData:Alpaca:ApiSecret command. Replace placeholders only locally; never send credentials to chat. Restart API and repeat provider status and comparison request.
- [CORRECTED — OVERNIGHT CHART WINDOW] Prior instruction to capture 10/26–10/28 was invalid. User will provide AHR, FMS, and NNN charts for 10/21–10/23. Accept for visual triage; quantitative target research still requires historical bars spanning regular-session close and complete next 07:00–20:00 Eastern window, with DATA_GAP intervals preserved.
- [OPEN — XRPN READY] OHLCV alignment is complete for 78 windows; internal study diagnostic state remains unavailable in reviewed files.

## [Uncertainties]

- Why existing saved credentials are not present under this API project's UserSecretsId is unknown.
- Screenshot review cannot establish source-backed target reach rates or actual order fills.


## Update — 2026-10-26 (SPY smoke test and provider-status semantics)

- [PASS — ALPACA HISTORICAL REQUEST] Visual Studio API returned 5 SPY 5M IEX bars for 2026-10-23T13:30Z–13:50Z, persisted 5 observations, zero duplicate timestamps, zero reported gaps, no provider error. This is a successful historical data smoke test for that window.
- [STATUS ENDPOINT DEFECT FOUND] `/api/providers/status` was hardcoded to report `CONFIGURED_NOT_YET_VALIDATED` whenever credentials existed, irrespective of successful data retrieval. This made the status misleading after the successful compare.
- [CODE UPDATED — NEEDS TEST/CI] Provider adapters now track the last successful non-empty response in process memory. Status reports `VALIDATED_DATA_RETURNED` with validation timestamp and bar count after data is returned; empty results do not mark validation. Added an empty-response unit test. Exact updated-head test/CI result remains unverified.
- [LIMITS] This does not validate all symbols/time windows, SIP entitlement, streaming, overnight GTC_EXT coverage, or production readiness. Massive remains unconfigured and is not required for the initial Alpaca path.

## [Uncertainties]

- The current feature-branch head's CI and Release test status have not yet been observed after the provider-status code changes.
- Provider validation is process-local and resets on API restart; durable validation history is not implemented.


## Follow-up — CCI/NICE and TTE bubble fix (2026-10-26)

- [DONE — VISUAL TRIAGE] AHR/FMS/NNN screenshots reviewed using the user's confirmation that they represent the 2026-10-21–2026-10-23 sample. See docs/Validation/Overnight-Candidate-Review-20261026.md. Quantitative full-window research remains blocked.
- [WATCH — CCI] Score 68.11 (AGGRESSIVE), ON win 60%, mean 1.24% vs median 0.08%, 5D 2.73%, current ON 0.03%. Chart: 5M Permission NO, neutral structure, bullish regime not established. Not READY.
- [WATCH — NICE] Score 60.47 (AGGRESSIVE), ON win 75%, mean/median 0.70%/0.60%, 5D 0.70%, current ON 0.55%. Chart: 5M Permission YES but neutral structure, RVOL 9.48, execution environment NO and price below displayed VWAP/EMAs/SMA50. Unrounded 5D > 20D gate, daily average volume and catalyst state need verification. Not READY.
- [SOURCE CHANGE — EOD BUBBLE] docs/Charts/1M/MainStudy.txt now includes a one-time red EOD GET OUT bubble on entry to EOD_GET_OUT; no signal/permission calculations changed. User must install and validate this ThinkScript in TOS. .NET CI does not validate ThinkScript compilation.
- [PASS — PRIOR-HEAD CI] GitHub Actions passed on commit 4f5ed265e54e9a924935a58b8b3233b31b264279 (run 40414827751); user reports 88 tests passed. Newer source/docs commits need a current-head CI check.
- [OPEN — FULL OVERNIGHT RESEARCH] Source-backed prior regular close plus complete next 07:00–20:00 Eastern GTC_EXT bars are still required. Preserve gaps as DATA_GAP; screenshots cannot establish target-touch rates or fills.
- [OPEN — REMAINING ENGINEERING] Pagination/retries, collector lifecycle/recovery, durable checkpoints, session-aware gap states, storage hardening, Morning Brief/scanner integrations, and fixture-based Boring Overnight calculations remain important before live reliance.


## Follow-up — 2026-10-27

- [IMPLEMENTED — CI PENDING] Swagger/OpenAPI UI added at `/swagger` with developer workflow documentation.
- [IMPLEMENTED — CI PENDING] Deterministic Boring Overnight hard-gate API added at `POST /api/boring-overnight/evaluate`, with tests for missing critical data, a hard-gate failure, and a WATCH warning. Inputs are supplied by the caller; automatic provider/scanner/catalyst wiring is still absent.
- [PARTIAL — ALPACA] User smoke test confirms successful historical Alpaca IEX bars. Latest compare response reports `persistedObservationCount=0`; investigate whether observations were idempotent duplicates already in the JSONL store. Do not claim persistence of new observations from this response.
- [BLOCKED — THINKSCRIPT] The user's installed chart still has no bubbles after the previous attempted fix. Source identity mismatch exists between the repository V14.4.10 MainStudy and uploaded V14.5 reference. Installed-study bubble fix must be corrected and verified in Thinkorswim.
- [OPEN — TARGET RESEARCH] See `docs/Validation/Boring-Overnight-Target-Policy-20261027.md`. No optimized target can be selected from the current data; full eligible-window bars, prior-only rolling metrics and walk-forward evaluation are required.
- [OPEN — CURRENT-HEAD CI] Verify the exact current branch head before any release recommendation.


- [PATCH ARTIFACT CREATED — NOT VERIFIED] Generated a V14.5-specific source copy with a one-time EOD GET OUT bubble on transition into `EOD_GET_OUT`. It is not the same as the repository's V14.4.10 MainStudy and must be installed and visually verified in Thinkorswim before the bubble issue can close.


## Follow-up — 2026-10-27: API usability

- [IMPLEMENTED — CI PENDING] StrategyReport CSV upload endpoint with 10 MB / 10,000-row bounds, quoted-field parser, complete parsed row return and preview/warnings. Validate with actual TOS exports.
- [IMPLEMENTED — CI PENDING] MCC local persisted exclusion watchlist CRUD. Evaluator checks it automatically. Not a direct Thinkorswim watchlist sync.
- [IMPLEMENTED — CI PENDING] Swagger examples added across current API routes; upload has multipart file schema. Swagger enabled for Release local runs. Do not expose unauthenticated API/Swagger publicly.
- [BLOCKED — USER VERIFICATION] Cleaned V14.5 source artifact and EOD bubble change not compiled/verified in Thinkorswim.
- [OPEN — CURRENT-HEAD CI] Workflow run 40538766766 was queued at last check. User-reported 91 passes applies to earlier code only.


- [IMPLEMENTED — CURRENT-HEAD CI PENDING] Dashboard now exposes API-backed exclusion management and StrategyReport upload/parse/JSON export. Verify browser workflows against the running Release API.
- [CI FIX APPLIED — RECHECK REQUIRED] Build failed because OpenAPI extension package was missing; added Microsoft.AspNetCore.OpenApi 8.0.0. Latest workflow run 40539484300 was in progress at last check.


## 2026-10-27 reliability/parity follow-up

- [IMPLEMENTED — CI PENDING] StrategyReport rows with explicit MCC OHLCV v1 payloads convert into the exact Chart Parity Template schema; dashboard downloads the parity CSV.
- [IMPLEMENTED — CI PENDING] Alpaca pagination, transient retry, range filtering, duplicate timestamp handling; added request-driven `/api/market-data/bars` persistence endpoint.
- [CI WARNING] The first parity UI build failed because `StrategyReportParseResult` lacked the new `ParityRows` property. Corrected, and a regression test was added. Workflow run 40545469124 queued at last check.
- [NOT COMPLETE] No continuous background collector/recovery, no scanner candidate provider, no full live Boring Overnight computation, no verified TOS watchlist sync, no current V14.5 exact-source bubble consolidation. The API remains decision-support-only and order submission is not implemented.
