# CURRENT HANDOFF — 2026-11-06, expanded ATR + provider qualification evidence

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Branch:** `feature/opportunity-quality-classification`  
**Dashboard:** `frontend/index.html`  
**Order submission:** NOT IMPLEMENTED; decision-support-only.

## Latest operator evidence — 2026-11-06 10:53 Arizona

Evidence ZIP: `20261106T105332.zip`. API health was `OK`. Alpaca effective feed was `iex`; no SIP request was made. The qualification runner correctly skipped SIP smoke, 40-session capture and sell-limit calculation.

### Imports and parity

| Series | Parsed/persisted observations | Appended | Identical skipped | Import gaps | Parity | Compared bars | 119-window gaps | Mean absolute error | Max absolute error |
|---|---:|---:|---:|---:|---|---:|---:|---:|---:|
| EVR 5m | 327 | 2 | 325 | 5 | `GAPS_PRESENT` | 20 | 3 | 0.0000294330 | 0.0000495349 |
| CBOE 5m | 338 | 9 | 329 | 18 | `GAPS_PRESENT` | 20 | 1 | 0.0000203866 | 0.0000426076 |

Both numeric ATR comparisons are within 0.00005 tolerance. Neither is a parity MATCH because gaps remain in the latest 119-observation window. No gaps were imputed. The CSV filenames say October 19–November 5, but actual parsed observations start November 2; the filenames do not prove coverage back to October 19.

### Provider/TOS comparison

| Measure | EVR | CBOE |
|---|---:|---:|
| TOS timestamps requested | 119 | 119 |
| IEX provider bars returned | 104 | 109 |
| Missing provider bars | 15 | 10 |
| Exact OHLCV rows | 0 | 0 |
| Response feed | `iex` | `iex` |

Every returned provider bar also identified IEX. This is diagnostic only. It establishes that IEX did not exactly match the imported TOS series; it does not establish which source is wrong or predict SIP results. Exact field counts and row-level mismatches are in [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md).

## Engineering changes since previous handoff

- Updated V2.1 ThinkScript source `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt` to export all available chart bars within the selected date range; removed the 07:00–19:59 ET export filter. It has not been compiled/run in Thinkorswim.
- Added `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1` in the prior update. It preflights health/provider status, compares exact latest 119 timestamps and only attempts SIP qualification when effective feed is SIP.
- Hardened that runner in commit `fc13d6b3199cf9fbbc8a9a4059b1fe6567fb1327`: explicit diagnostic/qualification mode, separate configured/response/row feed identity, per-field mismatch counts, and distinct non-SIP diagnostic status. This edit has not been executed in local PowerShell.
- Added [Sell-Limit-Evidence-Analysis-20261106.md](Validation/Sell-Limit-Evidence-Analysis-20261106.md), updated [Master-To-Do.md](Master-To-Do.md) and [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md).
- GitHub Actions `build-and-test` passed at `d00dbd61ede861f6a9924165377a17e9d8001022` for the earlier runner/study source. It does not validate the later PowerShell edit or ThinkScript compile.

## Current qualification gates

| Gate | Status |
|---|---|
| EVR 5m ATR parity MATCH | BLOCKED — 3 gaps in 119-window |
| CBOE 5m ATR parity MATCH | BLOCKED — 1 gap in 119-window |
| Continuous 119-bar TOS history | BLOCKED |
| Exact provider/TOS 5m OHLCV | NOT ESTABLISHED — IEX diagnostic 0/119 for both |
| Exact prior regular-session close from same qualified feed | NOT CAPTURED |
| SIP entitlement | NOT CONFIRMED; effective feed IEX |
| 40/40 SIP GTC_EXT sessions | NOT CAPTURED |
| Runtime 20-train / 20-unseen-test walk-forward PASS | NOT PROVEN |

No numeric sell limit is authorized. No broker order or fill integration exists. OHLCV touches are not fills.

## Next operator steps — in order

1. Pull `feature/opportunity-quality-classification`; inspect the latest PowerShell runner diff. Run Release build/tests against the exact pulled source before restarting the local API.
2. In Thinkorswim, replace/compile the study using `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`. Confirm 5m chart and study aggregation, ATR14 Wilder, Phoenix timezone, extended hours ON, corporate-action adjustment ON, `chart=MATCH`, and full-history export enabled.
3. Choose a date range that actually produces at least 99 earlier completed 5m bars before the latest 119-bar qualification window. Export EVR and CBOE with all available premarket, RTH and after-hours bars. Do not filter by time of day. Preserve embedded epoch timestamps.
4. Confirm actual first/last observation timestamps and all gaps; do not rely on filename dates. Import each CSV once and record parsed/appended/identical/correction counts and per-observation provenance.
5. Re-run EVR/CBOE 5m parity with unchanged 0.00005 tolerance. Inspect each remaining gap in TOS/source data. Do not synthesize zero-volume bars or interpolate.
6. Run `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1`. Verify `comparisonMode`, `configuredFeed`, `providerResponseFeed`, `feedIdentityMatch`, `providerBarFeedMismatchCount` and `mismatchBreakdown`. While effective feed is IEX, all comparisons are diagnostic-only and SIP work must be skipped.
7. Obtain SIP entitlement/access through the provider account. Do not retry SIP until entitlement has changed. Then configure effective feed SIP, restart API, verify `/api/providers/status`, and run one recent SPY 1m SIP smoke request.
8. After SIP smoke passes, capture and independently audit 40 complete GTC_EXT sessions: 20 chronological training plus 20 later unseen test; each requires 780 exact 1m bar starts (07:00–19:59 ET) and the exact previous regular-session close.
9. Run the sell-limit endpoint only after the source-qualified 40 sessions exist. Preserve training/test metrics, exclusions, exact provider/feed provenance and all gate results.

## Other unfinished workstreams

- Daily data: RTO/EVR 20-completed-session close/SMA20/average-volume parity and runtime/UI provenance are not validated.
- Catalyst: representative article collection, human adjudication, double-review agreement, chronological holdout precision/recall and source coverage remain outstanding. Automated keyword triage must not claim CLEAR.
- Opportunity Quality: refresh scanner field-availability denominators (296 unavailable custom-expression rows is stale historical audit), define candidate-pool completeness, then validate full-pool ranking/calibration and rank stability. Provisional score bands remain diagnostic.
- V3 Boring Overnight column: separate planned deliverable; implement from its data contract, preserve READY/WATCH/BLOCKED/DATA? and hard gates; do not conflate with MCC Opportunity Quality.
- Overnight boundary: resolve missing exact 09:30 opens and decide official auction print versus first eligible observed trade. A 1m bar Open is only a proxy.
- Thinkorswim authority/EOD: compile current 5m/1m studies, validate 5m authority and 1m timing-only role, replay state labels, and test exact Arizona EOD cutoff.
- WebSocket/collector: capture regular-session per-symbol deltas and test restart, sleep/wake, outage, auth failure, duplicate/replay and session-boundary recovery.
- Dashboard: perform full main-index browser acceptance, console/network review, responsive checks, and measured load/Fetch & Analyze timing against a captured baseline.
- P2: define actual broker execution reconciliation from broker order/fill records; Schwab read-only remains blocked on approval/OAuth; provider licensing/coverage and packaging/startup/backup remain unresolved.

See [Active Engineering Backlog](Active-Engineering-Backlog-20261106.md) for acceptance criteria and [Runtime Acceptance Runbook](Runtime-Acceptance-Runbook.md) for evidence capture procedures.

## [Uncertainties]

- Remaining TOS gaps may be legitimate no-trade intervals or export omissions; the supplied CSV cannot classify them.
- IEX/TOS differences do not establish which source is wrong or predict SIP results.
- Latest ThinkScript and PowerShell edits are not validated in their target runtimes.
- SIP entitlement, 40-session completeness, exact prior close, runtime walk-forward PASS, browser acceptance, regular-session stream recovery and actual broker fills remain unverified.
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
