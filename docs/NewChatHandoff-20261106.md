# CURRENT HANDOFF — 2026-11-07, ATR gap session slots + feature continuation

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Branch:** `feature/opportunity-quality-classification`  
**Dashboard:** `frontend/index.html`  
**Order submission:** NOT IMPLEMENTED; decision-support-only.

# CURRENT ENGINEERING CONTINUATION — 2026-11-07

This section supersedes older ATR gap-count interpretations below. Keep all code changes on `feature/opportunity-quality-classification`; main Dashboard remains `frontend/index.html`. Do not use PrototypeIndex. Order submission remains NOT IMPLEMENTED.

## Exact ATR gaps supplied by operator

Latest parity response from the operator:
- EVR 5m: 2 gaps.
  1. 2026-11-05 05:10Z → 14:05Z = 00:10 → 09:05 EST. Raw elapsed arithmetic reports 106 five-minute intervals, but the scheduled 20:00–04:00 ET closure is excluded. There are 61 expected timestamp slots in the 04:00–20:00 ET chart session: 04:00 through 09:00 ET.
  2. 14:05Z → 14:30Z = 09:05 → 09:30 ET. Four expected slots: 09:10, 09:15, 09:20, 09:25 ET.
- CBOE 5m: 1 gap.
  1. 2026-11-04 21:00Z → 23:40Z = 16:00 → 18:40 ET. 31 expected after-hours slots: 16:05 through 18:35 ET.

**Code change:** `AtrTimestampGapDetector` now counts only expected timestamp slots inside 04:00–20:00 ET and returns first/last expected missing UTC timestamps. The previous EVR `missingIntervalCount=106` was misleading wall-clock arithmetic; the new expected chart-session count is 61. The count does not prove a trade or candle existed. Never impute bars.

CI passed for the detector + test commit `028796bb4915ab8b118f7f5550eb8acd425aa250`: Release build, zero warnings/errors, 239/239 tests. Later commits add catalyst validation, Overnight provenance, and Opportunity Quality pool ranking; check exact latest branch-head CI before pulling.

Full analysis: `docs/Validation/ATR-Gap-Session-Slot-Analysis-20261107.md`. Catalyst and ranking API details: `docs/Validation/Catalyst-and-Opportunity-Quality-Validation-20261107.md`.

## Additional feature implementation in this continuation

### Overnight opening reference
The history result now explicitly labels the next-session reference as `ONE_MINUTE_BAR_OPEN_PROXY` and `officialOpeningAuctionVerified=false`. The prior regular close → next 09:30 one-minute bar Open endpoint spans Day 1 after-hours and Day 2 premarket in its net return, but does not reconstruct the path or GTC_EXT extrema. Do not claim auction-print parity. Exact historical scanner buy/sell endpoints still require verification against the original scanner formula.

### Catalyst validation
- Human review records now persist the triage prediction severity/categories that existed when the article was reviewed.
- New endpoint: `GET /api/market-data/catalyst-reviews/validation`.
- Uses a chronological later-20% holdout and reports confusion matrix, precision, recall, specificity and accuracy.
- Positive human labels: MATERIAL_RISK / MATERIAL_EVENT. Negative labels: NON_MATERIAL / IRRELEVANT. DUPLICATE / UNCERTAIN, legacy records without saved prediction and conflicting duplicate-article labels are excluded.
- This evaluates fixed keyword triage; it does not train the model or create catalyst clearance.
- No human-label dataset or measured metrics has been evidenced yet.

### Opportunity Quality pool ordering
- New endpoint: `GET /api/market-data/opportunity-quality/rank?symbols=EVR,CBOE,...`.
- Accepts 1–50 unique explicit symbols; concurrency is capped at four; returns every candidate including DATA? / BLOCKED.
- Diagnostic ordering uses existing classification, then raw 5M RVOL, 30-bar 1M range/ATR and prior-high distance. It does not invent a weighted score/probability.
- Main Dashboard now has **Rank quality pool · top 50** in the Red Zone controls. It ranks the top 50 imported scanner-score candidates and renders diagnostic classification/metrics/reasons. This is a disclosed prefilter, not the full imported list when more than 50 rows exist. Operator click-through/runtime acceptance and exact latest-head CI remain open. Pool paging/bounds and historical calibration remain open.

## Source audit: 1M / 5M authority and EOD

Repository source review found:
- `docs/Charts/1M/MainStudy_V14.5.2.txt` includes reconstructed 5M Daily VWAP, 5M close > EMA9, EMA9 >= EMA20 and RSI checks. The independent 1M Daily VWAP execution gate remains separate.
- `docs/Charts/5M/MainStudy.txt` and 1M V14.5.2 both map 15:55 Arizona to 17:55 ET standard time / 18:55 ET daylight time.
- This is source inspection only. Neither study has been compiled or matched-bar validated in Thinkorswim in this continuation.

## Operator next steps

1. Pull latest branch and run Release tests:
```powershell
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
dotnet test .\MomentumCommandCenterV2.sln -c Release
git rev-parse HEAD
```
2. After API rebuild/restart, re-read ATR parity and inspect the corrected count/first-last fields:
```powershell
$base = "http://localhost:53360"
$evr = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&warmupBars=99&windowBars=119"
$cboe = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m&warmupBars=99&windowBars=119"
$evr | Select-Object status,timestampGapCount,timestampGaps | ConvertTo-Json -Depth 10
$cboe | Select-Object status,timestampGapCount,timestampGaps | ConvertTo-Json -Depth 10
```
3. In TOS inspect EVR 04:00–09:00 ET, EVR 09:10–09:25 ET, and CBOE 16:05–18:35 ET. Record whether candles actually appear. If present but absent from the export, correct export; if absent from chart, document no-bar semantics. Never impute.
4. Compile the revised V2.1 ATR study, 1M V14.5.2 and 5M MainStudy in TOS. Compare matched completed 5M permission and 1M execution; test Arizona 15:54/15:55/15:56 on standard/daylight dates.
5. Run catalyst validation:
```powershell
Invoke-RestMethod "$base/api/market-data/catalyst-reviews/validation" | ConvertTo-Json -Depth 12
```
Review articles and save both positive and negative human labels; only then can the chronological holdout have meaningful precision/recall.
6. Exercise the explicit Opportunity Quality pool API with a bounded set of candidate symbols:
```powershell
Invoke-RestMethod "$base/api/market-data/opportunity-quality/rank?symbols=EVR,CBOE,RTO" | ConvertTo-Json -Depth 20
```
The endpoint accepts up to 50 symbols. It is diagnostic-only and not yet shown in the Dashboard.
7. Re-run browser diagnostics after pulling the Edge executable discovery fix. The previous failure was the resolved Edge executable path, not the script path.
8. Run stream collector during an actual regular session. Friday 18:52 ET attempts were correctly rejected; next weekday opportunity was Monday 2026-11-09 09:30 ET / 07:30 Arizona, only if the exchange is actually open.
9. Keep SIP/40-session walk-forward parked unless the operator independently reports changed SIP entitlement. Do not retry SIP or ask for another entitlement check.

## Remaining items that cannot be truthfully marked complete yet

- TOS chart-side classification of the exact ATR gaps; ThinkScript compilation and matched-state replay.
- Official auction print / first eligible observed trade parity.
- Catalyst human-label sample and measured holdout metrics.
- Dashboard integration of Opportunity Quality pool ordering and historical calibration.
- V3 Boring Overnight watchlist column; exact historical Overnight endpoint and authoritative metric source must be frozen before implementing it.
- Full Dashboard CDP/browser acceptance and regular-session stream/collector evidence.
- SIP-dependent 40-session runtime walk-forward.

# LATEST OPERATOR UPDATE — 2026-11-06, Daily audit UI and PowerShell acceptance

# LATEST OPERATOR UPDATE — 2026-11-06 evening: browser launch, stream guard, ATR reread

## Browser diagnostics

The operator ran `Capture-DashboardBrowserDiagnostics.ps1` from the repository root with `-BaseUri "http://localhost:53359/" -CaptureSeconds 120`. PowerShell found and executed the script, but `Start-Process` failed with “The system cannot find the file specified.” This points to the Edge executable path resolved by the script, not the script's own path.

**Code fix committed:** `50945206dd3ac207ab96efbcb9a42911cc13d335`.
- Explicit `-BrowserPath` is validated as an existing `msedge.exe` file.
- Discovery now checks standard Program Files / LocalAppData install paths, Windows App Paths registry entries, and all `Get-Command msedge.exe` results.
- The path is resolved to an absolute path, checked immediately before launch, and printed.
- Start-Process failures now identify the exact executable and underlying error.

**Still open:** the operator must pull the change and rerun. No CDP console/network capture, screenshot or manifest was produced by the failed attempt. If auto-discovery still fails, run `Get-Command msedge.exe -All | Select-Object Source,Definition` and pass the actual full executable path using `-BrowserPath`. Do not pass the diagnostics script path as `-BrowserPath`.

## Regular-session stream/collector

Both operator attempts at 2026-11-06 18:52 ET were correctly rejected because the regular session window is 09:30–16:00 ET. This is expected guard behavior, not a test pass and not a script defect. No stream/collector runtime evidence was captured.

Next weekday opportunity: Monday 2026-11-09 at 09:30 ET / 07:30 Arizona, only if the exchange is actually open. Confirm the session calendar; holidays and early closes still require operator judgment.

## ATR parity reread — current results supersede the earlier gap counts

The operator re-read the local parity endpoints after the session-aware detector update, using existing persisted observations; no CSV re-import was needed.

| Symbol | Status | Source observations | Compared bars | Gaps | MAE | Maximum absolute error |
|---|---|---:|---:|---:|---:|---:|
| EVR 5m | GAPS_PRESENT | 327 | 20 | 2 | 0.0000294330206094 | 0.0000495349247568 |
| CBOE 5m | GAPS_PRESENT | 338 | 20 | 1 | 0.0000203865947752 | 0.0000426076307465 |

Both maximum errors are within the 0.00005 tolerance. The EVR gap count fell from 3 to 2 because the same-date 00:00–00:10 ET interval inside the scheduled 20:00–04:00 ET closure is no longer counted. The remaining 2 EVR and 1 CBOE gaps still prevent MATCH. Inspect the exact missing intervals against the actual TOS chart/session contract; do not impute bars or re-import the unchanged CSVs. Revised ThinkScript compilation in TOS remains unverified.

## Additional ATR diagnostics implementation

To avoid another vague “2 gaps / 1 gap” loop, the parity API now includes a `timestampGaps` array containing exact previous/current UTC bars, expected next timestamp, first observed timestamp after the gap, missing interval count and gap duration. This is a diagnostic addition only; it does not weaken the gap gate or impute bars. A regression test checks the exact interval fields. GitHub Actions passed the Edge discovery fix (`50945206dd3ac207ab96efbcb9a42911cc13d335`) and ATR gap-detail implementation/tests (`5bc2ac57f5f83e00f77930e7ae13374d556828a4`): Release build, 0 warnings/errors, 236/236 tests. The subsequent documentation-only head was still running CI at the time of this handoff update.

After CI passes and local pull/build/API restart, run:
```powershell
$base = "http://localhost:53360"
$evr = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m&warmupBars=99&windowBars=119"
$cboe = Invoke-RestMethod "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m&warmupBars=99&windowBars=119"
$evr | Select-Object status,timestampGapCount,timestampGaps | ConvertTo-Json -Depth 10
$cboe | Select-Object status,timestampGapCount,timestampGaps | ConvertTo-Json -Depth 10
```
Inspect each reported interval in TOS. The JSON identifies the exact source discontinuity but does not determine whether TOS displayed a bar there; that requires chart-side evidence.

## Current operator action list

- [ ] Pull feature branch and rerun browser diagnostics; inspect the resolved Edge executable if launch still fails.
- [ ] Commit browser evidence folder after a successful capture; then review console, network and screenshot.
- [ ] Run stream/collector capture during an actual open regular session (next weekday opportunity Monday 2026-11-09, 09:30 ET / 07:30 Arizona).
- [ ] Inspect remaining EVR/CBOE ATR gaps in Thinkorswim and compile the revised V2.1 study.
- [ ] Continue remaining work in the order requested: ATR continuity and TOS authority/EOD; Overnight boundary; Catalyst validation; Opportunity Quality full-pool ranking; V3 Boring Overnight column; full Dashboard browser acceptance; walk-forward only when qualified history exists.

## Corrections to the previous action plan

- **Do not ask for the same EVR/CBOE ScannerExports again.** The committed V2.1 CSVs and persisted observations are already present in GitHub. The 327 EVR and 338 CBOE observations have been imported and analyzed; repeat imports were idempotent. The latest Wilder recurrence audit covers 326 EVR and 337 CBOE adjacent pairs and is rounding-consistent. Re-uploading the same exports adds no evidence. Only a demonstrably new/corrected export after the revised ThinkScript is compiled could be relevant; do not request it preemptively.
- **Do not retry SIP.** The operator has confirmed multiple times that recent Alpaca SIP access is denied (HTTP 403). Treat this as an established external entitlement blocker. No further SIP smoke request, feed reconfiguration, or GTC_EXT capture should be requested or initiated unless the operator independently reports an actual entitlement change.
- The visible Dashboard section is named **Daily data audit**. The operator has verified it on the main index with symbols RTO,EVR,CBOE.
- ATR gap-detector code was corrected after inspecting the already committed rows: same-date 00:00–00:10 ET gaps wholly inside the scheduled 20:00–04:00 ET closure no longer count as missing chart bars. Three regression tests cover the closure, a gap overlapping premarket, and an after-hours gap. GitHub Actions passed Release build and 236/236 tests on `151cb97976121600833c8927bddf4cf175567b7b`. Expected: one false-positive EVR gap removed; premarket-overlapping EVR and after-hours CBOE gaps remain blocked. Re-read parity in the local API after pull/build/restart using existing persisted rows; no CSV re-import is needed.

## Daily audit browser evidence

User-reported result: 3 symbols checked; 0 qualification-usable; 3 returned diagnostic-only / DATA?; 0 API errors. All 3 show 20/20 sessions, zero missing, FRESH, Alpaca/IEX, and `DATA? · IEX_PARTIAL`.

| Symbol | Close | SMA20 | Average volume |
|---|---:|---:|---:|
| RTO | 23.615 | 22.3118 | 134,701 |
| EVR | 284.600 | 272.1878 | 50,448 |
| CBOE | 353.280 | 325.0030 | 76,734 |

RTO/EVR independent OHLCV comparison previously failed against the committed Stock Analysis / S&P Global references. Do not rerun unchanged. CBOE independent reference/parity is absent: Data not found in sources. The UI result is a successful panel/display/fail-closed check, not independent data qualification.

## Meaning of Overnight fail-closed

The Daily data audit is not the Boring Overnight decision. Imported scanner historical PASS is shortlist evidence only. The candidate still needs current price, Daily liquidity, exclusion status, complete overnight-session history, catalyst evidence, and current 5M/1M state. Missing/stale/unknown required inputs remain DATA? / UNKNOWN; unknown catalyst is not CLEAR; a known hard failure remains BLOCKED. A high scanner score or technical signal cannot override those gates. READY must remain unavailable until every required gate is affirmatively satisfied. The live per-candidate panel output has not yet been captured in this turn.

## New PowerShell evidence scripts

1. Browser diagnostics: `scripts/validation/Capture-DashboardBrowserDiagnostics.ps1`. Uses Edge CDP to capture console/uncaught exceptions, sanitized network request/response/failure events, and screenshot. Command:

```powershell
.\scripts\validation\Capture-DashboardBrowserDiagnostics.ps1 -BaseUri "http://localhost:53360/" -CaptureSeconds 120
```

During capture, interact with the main Dashboard and inspect Daily data audit, Boring Overnight qualification, Red Zone, Morning Brief, and sell-limit gate explanations. It writes to `artifacts/browser-diagnostics/<timestamp>/`.

2. Regular-session stream/collector: `scripts/validation/Test-RegularSessionStreamCollector.ps1`. Read-only polling every 15 seconds for ten minutes; it does not alter feed settings. Command:

```powershell
.\scripts\validation\Test-RegularSessionStreamCollector.ps1 -BaseUri "http://localhost:53360" -Symbols SPY,RKLX,ASTX,XRPN -PollIntervalSeconds 15 -DurationMinutes 10 -ConfirmRegularSession
```

Run during an actual RTH session (script checks weekday and 09:30–16:00 ET bounds; operator must still confirm holiday/early-close calendar). A WebSocket subscription acknowledgement alone is not a pass. Require per-symbol completed-bar count and lastBarUtc advancement, global received/persisted deltas, and fresh assessment provenance. Collector is a separate historical poller; if disabled, report NOT TESTED/disabled.

Detailed instructions and acceptance criteria: `docs/Validation/Browser-and-Stream-Collector-Acceptance-20261106.md`.

## Current work status

- [x] Daily data audit panel user-verified for RTO/EVR/CBOE, fail-closed on IEX.
- [x] Added PowerShell CDP browser diagnostics script.
- [x] Added PowerShell regular-session stream/collector capture script.
- [ ] Run both scripts and commit their evidence folders.
- [ ] Complete full main Dashboard browser acceptance.
- [ ] Complete regular-session WebSocket delivery and collector runtime acceptance.
- [ ] Catalyst human-label/holdout validation.
- [ ] Opportunity Quality full-pool ranking.
- [ ] V3 Boring Overnight column.
- [ ] Overnight boundary / official 09:30 auction-print validation.
- [ ] Thinkorswim authority/EOD matched-state acceptance.
- [ ] 40-session walk-forward runtime qualification is blocked by incomplete eligible GTC_EXT source data and the known SIP entitlement issue; validator implementation already exists. Do not request a SIP attempt.



---

> **Historical context below — not the current operator action list.** Older sections are retained for audit trail. If an older instruction conflicts with the top **LATEST OPERATOR UPDATE**, current [Master To-Do](Master-To-Do.md), or [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md), follow the current documents. In particular: do not re-upload/re-import the same EVR/CBOE ScannerExports and do not retry/reconfigure SIP after the repeated HTTP 403 entitlement denial.

# OPERATOR UPDATE — 2026-11-06, Daily run and ATR gap follow-up

## Daily qualification evidence now received

Evidence is committed at `artifacts/daily-data-qualification/20261106T215302Z/` on `feature/opportunity-quality-classification`.

- RTO and EVR each returned exactly 20/20 expected sessions, zero missing dates and fresh latest sessions.
- API internal calculations reconcile exactly to the returned session rows.
- Independent comparison FAILED for both: multiple OHLC values differ and all 20 daily volume values mismatch the Stock Analysis / S&P Global reference.
- Feed is Alpaca IEX; adjustment is `all (split/dividend adjusted)`; response coverage is `IEX_PARTIAL`.
- API correctly sets `qualificationUsable=false`, leaves qualification metrics null and remains DATA?. This is correct fail-closed behavior.
- IEX partial venue coverage is consistent with the much lower observed volume than consolidated reference volume. Do not treat the IEX ADV as qualified liquidity. Obtain/verify SIP entitlement before repeating consolidated parity. A previous HTTP 403 remains a hard stop until entitlement changes.
- Dashboard screenshot/browser acceptance was not included. Data not found in sources.

| Symbol | API close | API SMA20 | API ADV20 | Independent close | Independent SMA20 | Independent ADV20 |
|---|---:|---:|---:|---:|---:|---:|
| RTO | 23.615 | 22.31175 | 134,700.5 | 23.63 | 22.3130 | 1,616,207.4 |
| EVR | 284.600 | 272.18775 | 50,448.1 | 284.47 | 272.2235 | 643,488.4 |

## ATR status and precise next diagnostic

Latest committed parity remains EVR 5m `GAPS_PRESENT` (3 gap groups in its qualification window, MAE 0.0000294330, max 0.0000495349) and CBOE 5m `GAPS_PRESENT` (1 gap group, MAE 0.0000203866, max 0.0000426076). Numeric error is within tolerance; continuity is not certified.

The latest 119-observation windows in `artifacts/sell-limit-evidence/20261106T143918/` were inspected. EVR has same-date discontinuities including 2026-11-05 05:00Z→05:10Z, 05:10Z→14:05Z, and 14:05Z→14:30Z; CBOE has 2026-11-04 21:00Z→23:40Z. Some are outside the intended sell window or cross non-trading periods. A gap count alone does not establish missing eligible bars. Check the parity gap policy against the documented chart-session contract before changing it.

The revised V2.1 ThinkScript removes the old 07:00–19:59 ET export filter, but compile/fresh-export evidence is still absent. Compile it in TOS and produce broad EVR/CBOE exports with all available ETH/RTH bars. Preserve epoch timestamps and inspect the actual source bars around each discontinuity. Do not synthesize bars or change tolerance.


### New recurrence audit result

The latest committed EVR/CBOE observation JSON was independently recalculated across every adjacent pair using the Wilder formula. EVR: 326 pairs, maximum absolute residual 0.0000928571, mean 0.0000329316. CBOE: 337 pairs, maximum 0.0000928571, mean 0.0000326198. No residual exceeded 0.0002; these residuals are consistent with four-decimal ATR rounding. The previous large EVR recurrence discontinuity is not present in the latest persisted series. This is not an ATR parity pass: EVR still reports 3 timestamp-gap groups and CBOE 1 in the latest 119-observation window. Inspect TOS chart bars and validate the gap policy against actual extended-hours/no-trade behavior before changing the gate. Detailed audit: `docs/Validation/ATR-Export-Coverage-Fix-20261106.md`.

## Next operator sequence

1. Do not repeat the IEX Daily run unchanged and do not request another SIP attempt. The repeated HTTP 403 is an established external blocker. Leave Daily DATA? and continue unrelated engineering; resume SIP-dependent work only if the operator independently reports a real entitlement change.
2. For ATR, compile the committed V2.1 source in Thinkorswim and confirm aggregation=5m, ATR14 Wilder, Phoenix timezone, ETH ON and adjustment ON.
3. Set a date range with at least 99 completed warm-up bars before the final 119 comparison observations. Export all available chart bars; do not filter by time of day.
4. Before importing, inspect epoch timestamps and chart bars around the listed discontinuities. Distinguish genuine absent bars/no-trade periods from export omissions; retain evidence for every decision.
5. Import each corrected CSV once, record appended/identical/correction counts, and rerun parity. Require continuous expected coverage under the explicit session contract and max error <=0.00005. Keep unresolved gaps blocked.
6. After ATR MATCH, proceed separately to SIP provider/TOS equality, exact previous RTH close, 40 complete GTC_EXT sessions, and 20-train/20-unseen-test walk-forward.
7. Dashboard screenshot/browser acceptance remains open. Other P1 workstreams remain outstanding: catalyst validation, Opportunity Quality full-pool ranking, V3 Boring Overnight column, overnight boundary, TOS authority/EOD, WebSocket/collector acceptance and full Dashboard browser acceptance.

---

## Latest operator evidence — 2026-11-06, approximately 14:39 Arizona

### ATR imports

The user re-imported the same expanded exports:
- CBOE: 338 parsed observations; 0 appended, 338 identical skipped, 0 corrections; 18 full-import gaps.
- EVR: 327 parsed observations; 0 appended, 327 identical skipped, 0 corrections; 5 full-import gaps.
- Both exports identify V2.1, ATR14 Wilder, Phoenix timezone, extended hours ON and corporate-action adjustment ON.
- Actual parsed first timestamps remain November 2, not October 19, despite the filenames.

The imports are idempotent. Re-importing these unchanged files does not repair timestamp gaps.

### Latest ATR qualification script

API health: `OK`; Alpaca effective feed: `iex`; provider validated: true.

The revised qualification runner reports:
- EVR parity: `GAPS_PRESENT`; MAE 0.0000294330206; max error 0.0000495349248; 3 qualification-window gaps.
- CBOE parity: `GAPS_PRESENT`; MAE 0.0000203865948; max error 0.0000426076307; 1 qualification-window gap.
- Provider comparison: `DIAGNOSTIC_MISMATCH_NON_SIP` for both symbols.
- `feedIdentityMatch=true` for both symbols.
- `exactProviderTosOhlcvMatch=false` for both symbols.
- SIP smoke, 40-session capture and sell-limit qualification were skipped because the effective feed was IEX.

No numeric sell limit is authorized. Do not interpret IEX mismatch as proof which data source is wrong or as a prediction of SIP results.

## Daily data — code and independent reference work completed

### Existing implementation review

The Daily API is `GET /api/market-data/daily?symbol=RTO` or `EVR`. It requests Alpaca `1Day` bars with `adjustment=all`, calculates exact latest 20 expected completed US-equity sessions and withholds qualification metrics unless the feed is SIP and the history is complete/fresh.

The previous implementation had an empty-response defect: a valid response with zero bars could call `Max()` on an empty collection while calculating freshness. This is fixed.

### Engineering changes committed

- Daily API now returns `latestExpectedSessionDate`, ordered `expectedSessionDates`, `missingSessionDates` and all returned `sessions` with date, timestamp, OHLC and volume.
- Empty history now returns incomplete/fail-closed metrics rather than throwing.
- Added unit tests for complete 20-session calculations, session-level provenance, incomplete IEX data and empty responses.
- Added a dedicated **Daily data audit** panel to the main Dashboard. It defaults to RTO and EVR and displays provider/feed, adjustment, latest expected session alongside the actual completed-bar timestamp, retrieval timestamp, observed close, SMA20, average volume, freshness, qualification status and missing dates.
- Added `scripts/validation/Test-DailyMarketDataQualification.ps1` to fetch RTO/EVR API responses, save raw JSON, recalculate close/SMA20/average volume from returned session rows, enforce configured/response feed identity and SIP fail-closed behavior, and compare against independent CSVs. The split-only/all-adjusted equivalence switch is hard-restricted to the verified 2026-10-12 through 2026-11-06 sample.
- Added independent reference CSVs:
  - `data/validation/daily-reference-20261106/RTO.csv`
  - `data/validation/daily-reference-20261106/EVR.csv`
- Added [Daily-Data-Qualification-20261106.md](Validation/Daily-Data-Qualification-20261106.md) with the source analysis, calculation baselines, command and acceptance requirements.

### Independent source baseline

Source: Stock Analysis historical data, which identifies S&P Global Market Intelligence as its underlying data source. Twenty sessions from 2026-10-12 through 2026-11-06.

| Symbol | 2026-11-06 close | Independent SMA20 | Independent ADV20 |
|---|---:|---:|---:|
| RTO | 23.63 | 22.3130 | 1,616,207.4 |
| EVR | 284.47 | 272.2235 | 643,488.4 |

These are independent reference calculations, not live MCC API values.

Stock Analysis discloses split-adjusted history, whereas Alpaca requests split/dividend adjustment. The known EVR ex-dividend dates (2026-08-28 and next 2026-11-27) and RTO ex-dividend date (2026-08-13) fall outside this sample window. The qualification runner requires the explicit `-ConfirmWindowAdjustmentEquivalence` switch before it compares these split-adjusted CSVs with Alpaca's all-adjusted series. Revalidate corporate actions before reusing that switch for a different window.

### Daily runtime status

<badge color="warning">NOT YET LIVE-VERIFIED</badge>

The local API was not accessible from this session. No actual RTO/EVR API payload, local Dashboard screenshot, or exact API-to-reference comparison is available yet. Data not found in sources for the current local Daily responses.

## Next operator steps — execute in order

### 1. Pull, build and restart

```powershell
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
dotnet test .\MomentumCommandCenterV2.sln -c Release
```

Use the repository's actual solution path if it differs. Record `git rev-parse HEAD`, then restart the API using the built source.

### 2. Run the RTO/EVR Daily qualification script

```powershell
.\scripts\validation\Test-DailyMarketDataQualification.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbols RTO,EVR `
  -ReferenceDirectory ".\data\validation\daily-reference-20261106" `
  -ReferenceProvider "Stock Analysis / S&P Global Market Intelligence" `
  -ReferenceAdjustment "split-adjusted" `
  -ConfirmWindowAdjustmentEquivalence
```

Inspect `artifacts/daily-data-qualification/<timestamp>/`:
- `RTO-daily-api.json`, `EVR-daily-api.json`
- `RTO-summary.json`, `EVR-summary.json`
- `daily-qualification-summary.json`
- copied independent reference CSVs.

Acceptance requires:
- Exactly 20 distinct expected sessions in ascending order.
- No missing expected sessions.
- Latest expected session present and fresh.
- API observed close, SMA20 and ADV20 exactly reconcile to recalculation from the 20 returned rows.
- Independent date coverage matches exactly.
- OHLC prices within $0.0001 and volume exact against the reference CSV.
- Feed identity and adjustment basis recorded.
- IEX may pass diagnostic arithmetic/reference comparison but must have `qualificationUsable=false` and null qualification price/SMA/ADV fields.
- SIP is not presumed available. Preserve 401/403/provider errors; do not switch feeds silently.

### 3. Verify the main Dashboard

Open `http://localhost:53360/`, switch to Boring Overnight, and click **Validate Daily data** in the Daily data audit panel.

Confirm the Dashboard shows the same RTO/EVR values and provenance as the saved JSON:
- Alpaca / effective feed.
- `all (split/dividend adjusted)`.
- Latest expected session and retrieval UTC timestamp.
- 20/20 sessions and no missing dates.
- Observed close, SMA20 and ADV20.
- Freshness.
- DATA? / IEX diagnostic if the feed is not SIP.

Capture a screenshot. The existing Overnight Qualification decision must remain DATA? when Daily feed/history is not qualifying; Final READY must remain zero while other gates are unvalidated.

### 4. Resume ATR qualification

The latest re-imports appended nothing. Do not keep re-importing the same CSVs. Compile/run the corrected V2.1 ThinkScript, investigate the 3 EVR and 1 CBOE qualification-window gaps, obtain continuous 119-bar windows and rerun parity. Only after TOS parity is continuous should provider/TOS comparison be treated as meaningful. SIP entitlement remains an external blocker.

### 5. Remaining P1 workstreams

Catalyst human-label/holdout validation; Opportunity Quality full-pool field-availability and ranking validation; separate V3 Boring Overnight column; exact 09:30 opening-print policy; Thinkorswim 5m/1m authority and EOD acceptance; regular-session WebSocket/collector recovery; and full main Dashboard browser acceptance remain open. See [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md).

## [Uncertainties]

- Actual local Daily API results and Dashboard rendering have not been captured in this session.
- Public reference source is split-adjusted; the all-adjusted equivalence is window-specific and depends on complete corporate-action history.
- Exact independent Daily OHLCV parity and SIP qualification are unverified.
- EVR/CBOE ATR timestamp gaps remain unresolved; IEX mismatch is diagnostic only.
- The latest PowerShell script and C# changes still require local execution; CI status for the newest commits is not yet established. Latest code commit: `35079a6eb1b4b7c61e882892f8824ace790e734c`.

---

# Historical evidence snapshot — 2026-11-06 09:27 Arizona

**Latest operator action:** ATR evidence exporter ran successfully. The ZIP was inspected and row-level analysis completed. This update supersedes the older "Next operator action" requesting the ZIP/export.

- Exporter source SHA: `9dd2c224d8f1373c7935eb3f94ed5fddea837e5f`.
- API health: `OK`; decision-support-only; order submission not implemented.
- Alpaca: IEX, configured and validated; last validation returned 8 bars. Massive: not configured.
- Results: EVR 1m `GAPS_PRESENT` (346 rows, 13 qualification-window gaps); EVR 5m `NOT_MATCH` (163 rows, 1 gap); CBOE 1m `NO_DATA`; CBOE 5m `NOT_MATCH` (165 rows, 3 qualification-window gaps).
- Exact error metrics, full-export gap counts, recurrence check, independent Wilder recomputation and next steps: [ATR-Evidence-Export-Analysis-20261106.md](Validation/ATR-Evidence-Export-Analysis-20261106.md).
- The exported ATR recurrence is broadly consistent with Wilder across consecutive rows after accounting for four-decimal ATR rounding. Independent recomputation closely reproduces the 5m parity error. A different initial seed/prefetch state or gap/session handling remains a hypothesis, not a proven root cause.
- The ZIP contains no provider OHLCV. Exact provider/TOS equality is still untested. It also contains no build/test transcript for the recorded SHA.

**Do not** re-run the exporter or re-import identical CSVs. Do not relax tolerance or impute gaps. Keep numeric sell-limit output blocked.

## Next operator action

The ATR evidence archive has been received and analyzed. The previous request to run `Export-AtrParityEvidence.ps1` and provide its ZIP is complete and superseded.

Follow the ordered instructions in [ATR-Evidence-Export-Analysis-20261106.md](Validation/ATR-Evidence-Export-Analysis-20261106.md): obtain materially longer EVR/CBOE 5m TOS history, resolve gaps from source evidence, test the seed/prefetch hypothesis, then separately capture provider OHLCV when entitled.

See `docs/Master-To-Do.md`, `docs/Active-Engineering-Backlog-20261106.md` and `docs/Runtime-Acceptance-Runbook.md`.

[Uncertainties]
- Numeric mismatch root causes and gap causes remain unknown.
- SIP entitlement, 40/40 history, walk-forward runtime pass, browser acceptance, provider/TOS equality and broker execution reconciliation remain unverified.

---

# OPERATOR VALIDATION UPDATE — 2026-11-06

**Branch:** `feature/opportunity-quality-classification`  
**Verified local source SHA:** `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`  
**Release build + tests:** PASS  
**API:** `OK`, mode `decision-support-only`; order submission `NOT IMPLEMENTED`.  
**Effective market-data feed:** Alpaca `iex`, validated. Massive is not configured.

## Latest ATR import evidence (imports were idempotent)

| Imported file | Symbol / timeframe | Parsed observations | Appended | Identical skipped | Corrections | Source timestamp gaps |
|---|---|---:|---:|---:|---:|---:|
| `StrategyReports_EVR_11032026_1M_ATR_v2_1.csv` | EVR / 1m | 346 | 0 | 346 | 0 | 48 |
| `StrategyReports_EVR_11042026_5M_ATR_v2_1.csv` | EVR / 5m | 82 | 0 | 82 | 0 | 1 |
| `StrategyReports_CBOE_11032026_5M_ATR.csv` | CBOE / 5m | 85 | 0 | 85 | 0 | 5 |
| `StrategyReports_CBOE_11032026_to_11042026_5M_ATR.csv` | CBOE / 5m | 165 | 0 | 165 | 0 | 6 |

All four imports identify `MCC_OHLCV_ATR_V2.1`, ATR(14) Wilder, `America/Phoenix`, extended hours ON and corporate-action adjustment ON. All returned `appendedCount=0`, `correctionCount=0`; therefore these uploads did not change the persisted observations.

### Interpretation and current blockers

- **EVR 1m:** 346 observations cover 2026-11-03 14:04–20:58 UTC, but the full export reports 48 gaps. Prior parity evidence showed 13 gaps in the 119-observation comparison window and numeric error within tolerance; that result was `GAPS_PRESENT`, not `MATCH`. The latest import is idempotent, but parity must be queried again against the persisted store before treating that as current runtime evidence.
- **EVR 5m:** The new 82-observation export spans 2026-11-04 14:05–21:00 UTC and has one 10-minute gap (14:20 expected, 14:30 actual UTC). It does not supply 119 consecutive bars. Its import did not add observations. The prior persisted parity result was `NOT_MATCH`; re-query it.
- **CBOE 5m:** The 85-observation file has five gaps; the 165-observation two-day file has six gaps. The two-day file includes a 3h20m discontinuity from 21:25 UTC Nov 3 to 00:45 UTC Nov 4, plus multiple 15–25 minute gaps. Neither total observation count nor a two-day span establishes a continuous 119-bar qualification window. The imports did not add observations.
- **Feed:** Alpaca is currently IEX, not SIP. The API reports one successful validation bar, which establishes connectivity only—not extended-hours completeness, SIP entitlement, or sell-window qualification. Massive remains unconfigured.
- **Build:** Release build/tests passed against the verified source SHA. The API health response does not expose the running process's Git SHA, so it does not independently prove which binary is currently running.
- Do not infer that a timestamp gap is a no-trade interval. No gaps may be imputed, tolerance must not be relaxed, and no numeric sell limit is authorized.

## Next operator verification

After restarting the API from the just-built source, run the following parity queries (no re-import is needed):

```powershell
$base = "http://localhost:53360"
$parity = @(
    "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=1m",
    "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m",
    "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m"
)
foreach ($url in $parity) {
    "`n===== $url ====="
    Invoke-RestMethod $url | ConvertTo-Json -Depth 20
}
```

Capture each full response, including status, parity version, sourceObservationCount, comparedBarCount, timestampGapCount, mean/maximum/p90 error, first/last compared timestamps, and provenance. The endpoint defaults to 99 warm-up bars plus 20 compared bars (119 total), seeds Wilder ATR from full imported history, and does not impute gaps.

Then investigate EVR 5m/CBOE 5m numeric mismatches using exact timestamp-matched imported OHLCV and ATR values: verify true range, prior close, full-history Wilder seed/recurrence, chronological order, symbol/timeframe isolation, session boundary treatment and export rounding. Establish whether missing timestamps are no-trade intervals or missing chart/export observations from independent source evidence.

Do not change the IEX configuration to SIP or attempt another SIP capture until SIP entitlement/access is confirmed. After access is confirmed, the sell-limit qualification still requires complete 20/20 SIP GTC_EXT sessions, exact prior RTH closes, all ATR/provider parity gates, fresh completed 5m ATR and a passing 20-session train + 20-session unseen-test walk-forward result.

[Uncertainties]
- Fresh parity responses after these four idempotent imports have not yet been supplied.
- The cause of the gaps and numeric mismatches is not established.
- SIP entitlement remains unconfirmed; prior SIP denial was HTTP 403.
- The API health response does not identify the running process SHA.
- Runtime walk-forward pass, browser acceptance and actual broker-fill reconciliation remain unverified.

---
# New Chat Handoff — latest operator evidence

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Working branch:** `feature/opportunity-quality-classification`  
**Dashboard entry point:** `frontend/index.html` served at `http://localhost:53360/`  
**Status:** ATR parity and sell-limit qualification remain blocked. This handoff supersedes the November 5 handoff for current status. Historical notes remain in `docs/NewChatHandoff-20261105.md` and `docs/Master-To-Do.md`.

## Current ATR evidence (operator-supplied API responses)

| Symbol | Timeframe | Status | Source observations | Compared bars | Timestamp gaps | Mean abs. error | Max abs. error |
|---|---|---|---:|---:|---:|---:|---:|
| EVR | 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.000026034 | 0.000046686 |
| EVR | 5m | `NOT_MATCH` | 163 | 20 | 1 | 0.001046717 | 0.001935736 |
| CBOE | 5m | `NOT_MATCH` | 165 | 20 | 6 | 0.000527 | 0.000965 |

Common provenance: `MCC-WILDER-ATR-PARITY-1.1`; ATR period 14; Wilder average; `America/Phoenix`; extended hours true; corporate-action adjustment true; study `MCC_OHLCV_ATR_V2.1`.

### Interpretation

- **EVR 1m:** Numeric errors are within the 0.00005 tolerance, but 13 timestamp gaps in the latest qualification window force `GAPS_PRESENT`. It is not a parity MATCH.
- **EVR 5m:** Numeric errors exceed tolerance substantially. One timestamp gap is also present. Investigate the numerical discrepancy; do not relax tolerance.
- **CBOE 5m:** Numeric errors exceed tolerance, with six timestamp gaps. Import persistence reported 165 observations parsed, 85 appended, 80 identical skipped, 0 corrections.
- All three imports are already persisted. Re-running the parity endpoint does not require re-importing.
- No missing bars may be synthesized or interpolated. No numeric sell limit may be published.

## Implemented code vs. evidence still required

1. Full-history Wilder seeding is implemented in `ThinkorswimAtrParityService`; the latest 119 bars are the qualification window, not a new seed window. API parity version is 1.1. Local running-SHA verification is still required.
2. The walk-forward validator is implemented. It uses 20 earlier complete training sessions and 20 later unseen test sessions. The history service retains 40 sessions and highs/lows; orchestration has a hard `WALK_FORWARD_VALIDATION` gate. A passing runtime result has not been supplied.
3. Main Dashboard is `frontend/index.html`. Do not use or recreate `prototypeIndex.html`.
4. SIP GTC_EXT remains blocked by the previously reported Alpaca SIP HTTP 403 entitlement response. Do not repeat the same request unless entitlement/access has changed.
5. Browser acceptance, exact provider/TOS OHLCV parity, exact reference close, full SIP coverage, and actual broker-fill reconciliation remain separate work.

## Immediate operator instructions

### A. Update, build and verify the exact branch

At the repository root in PowerShell:

```powershell
git status --short --branch
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
.\scripts\Get-Latest-And-Test.ps1
```

Record the exact SHA printed by the script. If the script reports a dirty tree, stop and inspect/commit/stash the changes; do not force-reset. Confirm the GitHub Actions run for that exact branch head is green. Local tests and CI are separate evidence.

### B. Restart the API cleanly and verify health/configuration

Stop the existing MCC API process before starting the rebuilt one. Do not launch a second instance on the same port. Start with the normal Visual Studio profile or:

```powershell
dotnet run --project .\src\MomentumCommandCenterV2.API\MomentumCommandCenterV2.API.csproj --launch-profile MomentumCommandCenterV2.API
```

Then run:

```powershell
$base = 'http://localhost:53360'
Invoke-RestMethod "$base/api/health" | ConvertTo-Json -Depth 10
Invoke-RestMethod "$base/api/providers/status" | ConvertTo-Json -Depth 10
```

Confirm decision-support-only mode, order submission NOT IMPLEMENTED, and record the effective Alpaca feed plus its configuration source. Do not paste secrets.

### C. Recheck parity WITHOUT re-importing

The three reports are already persisted. Query the existing parity endpoint for EVR 1m, EVR 5m and CBOE 5m using the same endpoint/query pattern already used for the previous responses. Capture the complete JSON for each. Verify:
- `comparisonVersion` is `MCC-WILDER-ATR-PARITY-1.1`;
- sourceObservationCount, comparedBarCount, timestampGapCount;
- meanAbsoluteError, maximumAbsoluteError, p90AbsoluteError;
- first/last compared timestamps;
- sourceFileName, studyVersion, importBatchId and importedAtUtc.

Do not import the same files again. Do not change tolerance, impute bars, or treat a numeric-only pass as MATCH.

### D. Diagnose the 5-minute mismatches

For EVR and CBOE separately:
1. Inspect the exact V2.1 payload observations and embedded epochs.
2. Reproduce true range using current high/low and the immediately preceding source-bar close, then Wilder's ATR with the full-history seed.
3. Compare the server and exported ATR at the same 20 timestamps after the 99-observation warm-up.
4. Check ordering, timeframe, timestamp alignment, session boundaries, symbol isolation, and whether persisted OHLC observations match the source payload.
5. Preserve gap samples and report the cause only when evidence establishes it.

EVR's max error is 0.001935735; CBOE's is 0.000965. These are not rounding-bound discrepancies.

### E. Obtain valid source history

- **EVR 5m:** export at least 119 consecutive completed V2.1 observations with no intraday gaps in the latest qualification window. Confirm extended-hours observations using embedded payload epochs. The old 82-observation export did not prove after-hours coverage.
- **EVR 1m:** resolve the 13 missing timestamps in the latest 119-bar window. If they cannot be established from source data, retain the blocked result.
- **CBOE 5m:** inspect all six gap samples from the import response and determine whether they are no-trade intervals or missing export/study data. Do not fill them.

If a new export is necessary, inspect its payload before importing. Import only genuinely new/corrected evidence and report the append/identical/correction counts.

### F. Validate remaining sell-limit gates in order

1. 119-bar continuous 5m TOS history: 99 warm-up + 20 comparisons.
2. ATR parity status `MATCH`, with no gaps and max absolute error within 0.00005.
3. Exact provider/TOS 5m OHLCV equality at every timestamp in that same window.
4. Fresh completed 5m ATR (no more than 10 minutes old).
5. Exact latest completed regular-session reference close from the same provider/feed.
6. SIP feed entitlement and 20/20 complete GTC_EXT windows, each with every 07:00–19:59 ET minute plus exact prior regular-session close. Previous SIP HTTP 403 means **do not retry** until entitlement/access changes and a small SIP request succeeds.
7. Walk-forward runtime validation: earliest 20 complete sessions train; next 20 unseen complete sessions test. Require 100% coverage, positive training target, test target-touch rate at least training empirical touch rate, and test maximum adverse excursion no greater than training AE P95.
8. All other orchestration gates pass.

The walk-forward implementation exists; do not describe it as not implemented. But implementation alone is not a PASS. A theoretical OHLCV touch is not a broker fill.

### G. Browser acceptance

After build and API restart:
1. Open `http://localhost:53360/` (not `/prototypeIndex.html`).
2. Hard-refresh with Ctrl+F5.
3. Confirm both workspaces and the main Dashboard layout render.
4. Confirm all five ATR provenance controls appear and retain the expected settings: ATR 14, Wilder, America/Phoenix, extended hours ON, corporate-action adjustment ON.
5. Confirm existing imports remain persisted and parity responses match the API results.
6. Capture browser console/network errors and report any missing controls or stale assets.

## Documentation authority

- Current status and full checklist: `docs/Master-To-Do.md`.
- This current handoff: `docs/NewChatHandoff-20261106.md`.
- Prior historical handoff: `docs/NewChatHandoff-20261105.md`.
- Normal update/build/restart sequence: `docs/Technical-Workflow.md`.
- Detailed runtime acceptance: `docs/Runtime-Acceptance-Runbook.md`.
- Walk-forward policy: `docs/Validation/Walk-Forward-Validation-Design-20261105.md`.
- ATR root-cause and seed investigation: `docs/Validation/ATR-V2-Parity-Investigation-20261105.md`.

## [Uncertainties]

- The cause of EVR 5m and CBOE 5m numeric mismatches is not established.
- The supplied gaps may be no-trade intervals or missing chart/export observations; source evidence is required.
- The currently running local API SHA is not present in the supplied responses.
- No passing walk-forward runtime result, 20/20 SIP qualification result, or browser acceptance evidence has been supplied.
- No actual broker execution ledger has been supplied; fills cannot be inferred from OHLCV.