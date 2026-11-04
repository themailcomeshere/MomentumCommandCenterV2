# New Chat Handoff — November 4, 2026

**Working branch:** `feature/opportunity-quality-classification`  
**Latest validated code head:** `15d642ae986686a0c0a44fe48f27c04718fc6a19`  
**Current branch head:** `ec500a161abcf94d92d0ce36646bf43916d5e8af` (exact-head CI pending)  
**Last validated code head:** `f1804cc2171b96e70309fccd9053c704d0b7711c` (Release build, JS syntax check, 198/198 tests, 0 warnings/0 errors; Actions run [41882407536](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41882407536))  
**Unvalidated changes after that head:** store-level feed filtering, source-isolated 7-day GTC_EXT audit reads, and one store regression test. Exact-head CI is required.  
**Canonical execution checklist:** `docs/Master-To-Do.md`

## Latest implementation
- Daily API: `GET /api/market-data/daily?symbol=RTO`. With IEX, diagnostic observed values are returned separately while qualification price/SMA20/average-volume remain null; IEX is partial-exchange coverage and cannot establish market-wide liquidity. SIP is required for those qualification fields. Alpaca `1Day`, `adjustment=all`, exact 20 expected sessions from MCC's versioned exchange calendar. Current session excluded until scheduled close; early closes honored. Missing any expected session suppresses SMA20 and 20-session average volume. Price is suppressed if latest expected session is absent.
- Catalyst API: `GET /api/market-data/catalyst-assessment?symbol=RTO`. Ticker-scoped Alpaca News for the preceding 24 hours, pagination up to 10 pages, versioned provisional keyword triage. It cannot return CLEAR. No news = UNKNOWN; material/unclassified news requires manual review; severe risk phrase = RISK_PRESENT.
- Both the actual UI button workflow (`POST /api/boring-overnight/capture-history`) and `evaluate-with-history` now fetch Daily and catalyst inputs, overwrite caller-supplied Daily/catalyst values, and keep catalyst UNKNOWN except explicit RISK_PRESENT. IEX Daily bars are diagnostic only: qualification price/SMA20/average-volume are null unless the configured feed is SIP, because IEX is partial-exchange coverage.
- Snapshots now persist requested/valid/missing Overnight pair counts, Daily source/feed/adjustment/completeness/observed metrics, and catalyst article IDs/headlines/publication timestamps/URLs/categories/rule version/coverage. Production and prototype saved-snapshot tables show history coverage, Daily fields/feed quality/source timestamp, and catalyst count/coverage/rules plus up to two headlines.
- Daily and catalyst capture/snapshot evidence integration, exact-session completeness audit, SIP-only qualification gate, snapshot persistence tests, and production/prototype table changes passed Release build and **188/188 tests**, 0 warnings/0 errors on code commit `9fb66f14e907ef029ac7955ef46802aa789e74c6` (Actions run [41876561554](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41876561554)). The subsequent top-four qualification UI integration is not yet included in that validated head.
- This is not yet live-provider validation, browser acceptance, or final catalyst-classifier validation. SIP entitlement and Daily adjustment parity are not established. If Alpaca is currently configured for IEX, the qualification Daily fields will intentionally be null. 5M/1M evidence remains separately sourced.

## What I need from the operator
1. Pull the branch and run `scripts/Get-Latest-And-Test.ps1`; restart the API.
2. Request:
   - `GET /api/market-data/daily?symbol=RTO`
   - `GET /api/market-data/daily?symbol=EVR`
   - `GET /api/market-data/catalyst-assessment?symbol=RTO`
   - `GET /api/market-data/catalyst-assessment?symbol=EVR`
   - `GET /api/providers/status`
3. Paste the full JSON responses (do not include API keys/secrets). For Daily, we need provider/feed, latest completed session, close, SMA20, average volume and completeness. For catalysts, we need article headlines, publication times, URLs, coverage completeness and provisional categories.
4. Run the existing 20-session capture/evaluate-with-history flow for RTO and EVR. Confirm the new snapshot contains the Daily inputs only when all 20 expected sessions exist, and that catalyst is UNKNOWN unless the provisional rules detect an explicit risk phrase.
5. Compare Daily values against an independent chart/provider on the same adjustment basis. Report any provider 401/403 errors verbatim with credentials removed.
6. In the Boring Overnight qualification panel, click **Fetch Daily + catalyst for top 4**. Confirm it queried the four highest-scored imported symbols, only shows qualification Daily values for complete/fresh SIP data, and never displays READY. Other imported symbols should remain DATA?.
7. Ensure the configured Alpaca feed is SIP with entitlement, then POST `{ "symbol": "RTO", "providerName": "Alpaca", "feed": "sip" }` to `/api/boring-overnight/capture-sell-window-history`; repeat for EVR. Save the JSON. Inspect `fetchedBarCount`, `newlyPersistedBarCount`, `completeWindowCount`, `allWindowsComplete`, `qualificationUsable`, each window's `missingIntervalCount`, and reference-close provenance. IEX is diagnostic only and cannot qualify consolidated target research.

- `BoringOvernightSellLimitCalculator` now treats complete flat/negative GTC_EXT outcomes as valid evidence; a non-positive target quantile returns `NO_POSITIVE_TARGET` with no numeric limit or misleading touch rate. Regression tests cover both adverse and flat samples. Exact-head CI is pending.

## Remaining engineering
- Browser-verify the new top-four Daily/catalyst refresh in both qualification panels. Exact-head CI for the new UI integration must pass first.
- Browser-verify the expanded saved-snapshot table in production and prototype.
- Resolve EVR's eight missing exact next-session opens without nearest-bar substitution.
- Establish official 09:30 ET auction-print parity; 09:30 one-minute bar Open is still only a proxy.
- Complete runtime GTC_EXT validation for RTO/EVR using the new capture endpoint; the source implementation exists but no 20/20 SIP extended-hours result has been observed. Then connect complete observations to the sell-limit calculator, run walk-forward target validation, and link actual fills/exits/fees/realized P&L.
- Integrate current-data Opportunity Quality and Red Zone ranking; calibrate only from historical outcomes.
- Validate catalyst rules with human-labelled articles and authoritative-source coverage before any CLEAR_WITH_SOURCE status is introduced.

[Uncertainties]
- Alpaca Daily feed entitlement and corporate-action adjustment parity have not been runtime-verified.
- The 20-session exchange calendar is MCC-maintained; unexpected exchange closures are not automatically covered.
- Phrase triage is provisional and has not been measured against a labelled catalyst dataset.
- The validated code head passed CI; documentation-only commits followed.

---

## November 3, 2026 — latest operator acceptance and follow-up

**Branch:** `feature/opportunity-quality-classification`  
**Active checklist:** `docs/Master-To-Do.md`

### User-confirmed results
- Red Zone clear-all, restore/repopulate and re-import flow all passed browser testing.
- 20-session capture now takes approximately 7–24 seconds, versus approximately 2.4 minutes before the one-range-scan store optimization.
- RTO 1m Fetch & Analyze (13:00–19:12 Arizona) took approximately 28 seconds. Completion feedback now includes returned bar count and elapsed seconds for repeatable measurements.
- Full page load is approximately 45 seconds; latest change defers Morning Brief loading until its modal is opened and deduplicates concurrent live-assessment requests by symbol.
- User selected the official 09:30 ET opening print as the intended Overnight return definition.

### Snapshot behavior
- Previously, `GET /api/boring-overnight/snapshots` only read records; the UI had no routine action that created one. `POST /api/boring-overnight/snapshots` and `POST /api/boring-overnight/evaluate-with-history` were available, but the 20-session capture did not persist a snapshot.
- Latest change: every successful 20-session capture now appends a snapshot containing history-derived evaluator inputs. It also records UNKNOWN current price, Daily liquidity, catalyst and chart inputs; incomplete 20-session history leaves historical fields null. The UI refreshes the saved-snapshot list after capture.
- Snapshot is an append-only evidence record, not a final trading decision. It does not make a candidate READY.
- The next browser test must confirm one new snapshot appears after capture and that incomplete history/current gates remain UNKNOWN.

### Strategy-definition caveat
The user chose the official 09:30 ET opening print. Existing history calculation currently uses the Open of the exact 09:30 ET one-minute bar. That is not independently verified as the official opening-auction print. UI/API provenance now labels it a one-minute bar-open proxy. Need validated trade/auction-print evidence or an explicit provider guarantee before describing the computed return as official-opening-print performance. Never shift to 09:31 or nearest bar without approval.

### UI and performance changes
- Morning Brief no longer fetches 14 benchmark/sector histories and news during initial dashboard startup; it loads when its modal is opened.
- Concurrent chart/decision calls for the same symbol share one in-flight live-assessment request.
- 40-row Provider response vs persisted store audit is collapsed by default and height-bounded when expanded.
- Ticker-vs-SPY is an optional collapsed troubleshooting control, not a routine qualification gate.

### EVR evidence
User reported EVR history 12/20, provider exact 32/40, store exact 32/40. Eight boundaries are missing. Diagnose EVR from its own audit rows and nearest timestamp offsets; do not infer same causes as RTO.

### Operator next steps
1. Pull latest branch, run `.scripts\Get-Latest-And-Test.ps1`, then hard-refresh.
2. Confirm page startup and Morning Brief behavior; capture a Network HAR with timing if page still takes ~45s.
3. Fetch RTO 1m bars again; note the new elapsed seconds and bar count. If still slow, capture HAR plus API console logs.
4. Run 20-session capture once and verify saved snapshot list increments by one; confirm snapshot says UNKNOWN for missing current gates.
5. Open the collapsed exact-boundary audit only when needed; inspect EVR's eight missing timestamps and nearest offsets.
6. Do not call 09:30 one-minute bar-open return an official auction-print return until feed/source parity is proven.

## Latest handoff — November 3, 2026 (Red Zone recovery and Overnight capture)

**Active branch:** `feature/opportunity-quality-classification`  
**Active execution checklist:** `docs/Master-To-Do.md`  
**Full dependency audit:** `docs/Validation/Full-Master-To-Do-Audit-20261103.md`

### Latest changes
- Red Zone dismissals were global in browser storage. A fresh CSV import did not reset them, so the imported scanner could render all 198 rows while Red Zone rendered 0/4.
- Added **Dismiss/Clear all Red Zone Candidates** to dismiss every symbol in the selected scanner group and remove that group's manual pins.
- **Restore dismissed / repopulate** clears the dismissal list and renders eligible candidates again.
- A fresh CSV import now clears stale dismissals for symbols present in that imported file. The import feedback reports the number reset.
- Saved evaluator-input snapshot status is now separate from market-bar fetch status. The former can report 0 snapshots without overwriting the latter's OHLCV result.
- 20-session capture now displays elapsed progress.
- History retrieval now attempts one bounded-range JSONL scan; if the result reaches the 10,000-row cap it falls back to exact per-pair reads, preserving older boundary coverage.

### User's current 20-session result
- 17 / 20 valid close/open pairs.
- 37 / 40 exact provider boundaries and 37 / 40 exact stored boundaries.
- Alpaca / IEX.
- 3,965 bars fetched / 0 newly stored.
- Calendar: `MCC_US_EQUITIES_CALENDAR_V2`.
- Rolling metrics remain DATA? because the three exact missing bars invalidate the complete 20-pair window. Zero newly stored is expected when the returned observations already exist; do not interpret it as no data fetched.

### Still required
1. Pull latest feature branch and run `.scripts\Get-Latest-And-Test.ps1`.
2. Browser-test clear-all, restore/repopulate, re-import dismissal reset, 198 scanner rows and up to four Red Zone cards.
3. Verify the OHLCV fetch result remains visible after the saved-input snapshot request.
4. Time the optimized 20-session capture; compare response and store size. No performance assertion until measured.
5. Inspect the exact three missing next-open timestamps and nearest-bar offsets. Do not use nearest-bar imputation.
6. Decide whether the strategy measures official 09:30 ET open or first observed eligible bar after 09:30; do not silently change the metric.
7. Continue the remaining qualification gates: Daily price/SMA20/20D volume, validated catalyst clearance, current 5M/1M evidence, GTC_EXT outcomes and target validation.

### Important distinction
`GET /api/boring-overnight/snapshots` reads persisted evaluator input snapshots. It does not read market bars and does not return the evaluator decision. `POST /api/boring-overnight/capture-history` calculates the 20 regular-session close/open history; `GET /api/market-data/bars` fetches the selected symbol/timeframe OHLCV bars.

---

## Latest handoff — November 3, 2026 (runtime fix and Overnight process)

**Branch:** `feature/opportunity-quality-classification`

### Completed and verified
- Startup error `Cannot access 'redZoneAssessmentRefreshers' before initialization` fixed by moving Red Zone state declarations before restored-import/startup rendering. Commit: `4906d4c1119bedec6ec83f67cd21ff19d2c409fa`.
- Exact-head CI for that code commit passed: [Actions 41730308073](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41730308073).
- Documentation exact-head CI passed: [Actions 41730346398](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41730346398).
- Runtime archive `20261103T190356Z.zip` is now uploaded and contains `live-assessments.json`, `stream-delta.json`, `runtime-before.json`, `runtime-after.json`, `provider-refresh.json`, and `stored-bars.json`. File inventory is confirmed, but JSON content analysis has not yet been performed.
- Added `docs/Validation/Overnight-Process-Operational-Checklist-20261103.md`.

### Overnight process — controlling schedule
- Strategy interval: Day 1 after-hours plus Day 2 premarket.
- Start actionable review in the final 60 minutes before Day 1 regular-session close. Do not wait until 7:55 PM ET or until after-hours ends.
- Calculate evaluation start from actual exchange close minus 60 minutes; render in America/Phoenix. Normal Arizona display: noon during EDT, 1:00 PM during EST; adjust for early closes.
- Buy deadline is actual Day 1 regular-session close.
- Precompute historical metrics earlier; use final hour to refresh current-day qualification and produce a manual-review shortlist.
- No automated order submission.

### Latest CSV / Red Zone acceptance
- Current imported file: 198 rows; 187 historical PASS, 11 FAIL, 0 UNKNOWN, 0 exclusion matches, 0 final READY.
- Expected top-four Red Zone preview by Overnight Score among historical-gate passes: LXU 68.60, OSW 67.48, MHK 66.79, PUK 65.13 (manual pins/dismissals may alter the display).
- Browser acceptance after the TDZ fix is still pending. Confirm no console exception, restored Imported CSV, and actual four cards; do not treat CI as browser proof.
- 0 READY is correct until price, 20D volume, catalyst and current chart gates are source-backed.

### Runtime archive — next analysis
Use the uploaded archive already attached to this conversation; do not ask for another copy. Inspect:
1. `live-assessments.json`: per-symbol `oneMinute.sourceTransport`, `sourceTimestampUtc`, `completedAtUtc`, age/freshness and completed WS bar count.
2. `stream-delta.json`: received/persisted deltas per symbol.
3. `runtime-before.json` / `runtime-after.json`: connection/subscription state.
4. `provider-refresh.json` and `stored-bars.json`: REST response vs persisted OHLCV corroboration.
A subscription acknowledgement is not delivery. Do not lower the 50-completed-bar minimum or freshness threshold. 5M Permission remains REST-backed. JSON content has not yet been analyzed; no transport conclusion is currently justified.

### Remaining blockers
- Browser acceptance for the imported Red Zone cards and startup fix.
- Daily price/SMA20/20D average-volume source integration.
- Validated catalyst evidence; UNKNOWN is not CLEAR.
- Matched 5M authority / 1M execution evidence and Thinkorswim compile/parity.
- Complete source-backed GTC_EXT-eligible sell windows, actual Schwab order eligibility and actual fill/outcome history. Historical high touch is not fill probability.
- RTO 3:55 PM Arizona EOD test and 20-session RTO exact-boundary audit remain separate.

### Operator's next actions
1. Pull latest feature branch and run `.`\`scripts\Get-Latest-And-Test.ps1`.
2. Restart API, hard-refresh `http://localhost:53360/prototypeIndex.html`, and verify the console, restored import, 198 scanner rows and four Red Zone cards.
3. Do not re-upload the runtime ZIP; its contents are awaiting analysis.
4. Continue the next regular-session runtime test only if the archive analysis identifies a specific gap.
5. Keep Boring Overnight at WATCH/manual research until all hard gates have source-backed evidence.

## Latest operator report — November 3, 2026, 3:20 PM Arizona

- API health passed: status OK, decision-support-only, order submission not implemented.
- Exclusions passed: 56 entries; among OSW/EVR/MHK/TEST only TEST returned. This matches canonical 55 plus TEST.
- Browser console error on startup: `Cannot access 'redZoneAssessmentRefreshers' before initialization`. Root cause was a temporal-dead-zone ordering bug: restored/imported scanner rendering called `renderMorningReport()` before the later `let redZoneAssessmentRefreshers=[]` declaration executed.
- Fixed in `frontend/app.js` by moving Red Zone state declarations immediately after `scannerData` initialization, before restored-import and startup refresh paths. Current declaration line 111; first startup render call line 148. Commit: `4906d4c1119bedec6ec83f67cd21ff19d2c409fa`.
- Next: pull latest, hard-refresh, confirm no console exception, confirm Red Zone top-four cards. CI/browser acceptance for this newest fix still needs checking.
- User uploaded `20261103T190356Z.zip`; its six expected JSON files are present. File inventory is confirmed, but JSON contents remain unanalyzed. Inspect `live-assessments.json`, `stream-delta.json`, `runtime-before.json`, `runtime-after.json` first; corroborate with provider-refresh and stored-bars. No source provenance conclusions until those values are inspected.

## Latest repository checkpoint — 2026-11-03

- Feature branch head: `d94d27a6a63876206b8bbc66dffa8214d13cd5db`.
- Exact-head GitHub Actions run [41702900843](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41702900843) passed.
- The UI source changes and Imported CSV selection-order fix are included. Browser acceptance is still open.
- User's local API query returned 56 exclusions. The committed feature-branch `src/MomentumCommandCenterV2.API/App_Data/exclusions-watchlist.json` also contains 56 entries (55 canonical + TEST). Among OSW/EVR/MHK/TEST, only TEST is present, which matches the canonical source. This is consistent with the API reading the seeded file; no evidence currently supports the stale-file theory.
- The 198-row current import is not expected to match the prior 248-result screenshot. The requirement is complete import/render of the current export, not a fixed row count.

# 2026-11-03 — Current operator handoff (supersedes older next-step lists)

## Current branch and repository state
- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Working branch: `feature/opportunity-quality-classification`
- Do not merge `master` wholesale. The branches are materially divergent; selectively port specific docs/source only after compare and tests.
- Feature branch now includes UI changes for sticky table headers, a bounded exclusions list viewport, and an Unknown historical metrics summary. These source commits have not yet received exact-head CI/browser acceptance in this handoff.

## Latest scanner / qualification result
- User reports 198 imported rows and 198 scanner rows rendered.
- Historical metrics: 187 PASS, 11 FAIL, 0 UNKNOWN.
- Exclusion matches: 0; final READY: 0.
- The fixed 248-row target is withdrawn. Row counts vary by scan snapshot; validate complete import of the current file, not a stale count.
- 0 READY is expected because current price, 20D average volume, catalyst clearance, and current chart gates are not evidenced by the CSV.
- The qualification panel now has a visible Unknown historical metrics summary. Browser confirmation is still required.
- Red Zone previously reported 0/4 despite 187 historical PASS. The import-selection ordering defect is fixed in `9777b774de414c4af39ad7ee82b18990b7924972`. Re-running the current committed CSV through the importer-equivalent parser yields 187 eligible rows and expected top four by Overnight Score: **LXU 68.60, OSW 67.48, MHK 66.79, PUK 65.13**. Browser confirmation remains open; manual pins/dismissals can change the visible four.

## Exclusion-list interpretation
- Feature runtime list seed is 55 canonical exclusions plus preserved TEST = 56.
- OSW, EVR, and MHK are not in the canonical 55-symbol source. Their absence from the API result is expected; it does not prove the API is reading an old file.
- After API restart, query `GET /api/watchlists/exclusions`, verify count 56, and inspect several canonical symbols plus TEST. Do not add OSW/EVR/MHK without an explicit operator decision.

## November 3 runtime result
User ran the runtime test against localhost:53360:
- RKLX Alpaca/IEX stored bars: 1M=40, 5M=31.
- Provider refresh/store idempotency: 1M=True, 5M=True. This confirms exact match of the returned provider bars to stored observations for that request.
- Stream snapshot over five minutes: SPY advanced 5 bars; RKLX, ASTX and XRPN did not advance.
- All four were acknowledged before and after. Acknowledgement alone is not a delivery pass.
- Do not call the three no-advance symbols failures without checking whether IEX had eligible prints and examining each symbol's assessment provenance/freshness.

The 50-completed-bar threshold is a source-selection requirement for choosing WebSocket as the latest 1M assessment source. It is not a demand for 50 new bars during a five-minute capture. Require all of:
1. At least 50 completed persisted 1M WebSocket observations.
2. A fresh latest completed bar.
3. `oneMinute.sourceTransport` identifies WebSocket.
4. `oneMinute.sourceTimestampUtc` / `completedAtUtc` match the expected current bar and age.
5. 5M Permission remains REST-backed.

Inspect JSON files in `artifacts/runtime-validation/20261103T190356Z`. If the folder is not present locally, the artifact evidence is not available to this chat; do not fabricate its JSON fields.

## RTO screenshot review — November 3, 12:00 PM Arizona
- Uploaded chart shows RTO 5M and 1M views, not a 3:55 PM cutoff capture.
- 5M labels: PREPARE TO SELL; Permission NO; structure NEUTRAL; bullish regime established; RVOL 0.28; RSI 50.1; extension 0.58 ATR; runner momentum neutral.
- 1M labels: WAIT; reconstructed 5M Permission NO; 5M structure TRANSITIONING; RVOL 1.49; RSI 51.8; NX 1.31 ATR.
- Repeated PREPARE TO SELL bubbles are not equivalent to SELL or EOD GET OUT. No visible EOD GET OUT event is established in this screenshot.
- This chart does not prove the 3:55 PM Arizona EOD behavior. Capture the exact bars immediately before, at, and after the cutoff from the correct installed study before closing that validation.

## Clarification of prior Steps 5–7
These are three separate checks; none asks you to re-export a fixed number of scanner rows.

5. **Live WebSocket provenance:** During regular hours, capture before/after stream status and full live-assessment JSON. Compare per-symbol bar-count deltas, latest completed timestamp, transport source, and freshness. SPY advanced; the other three did not in the supplied test. Save the result; do not lower freshness or history thresholds.
6. **RTO 20-session boundary audit:** This is a historical close/open data-coverage test, separate from WebSocket delivery. The latest reviewed IEX artifact had 37/40 exact boundaries, with three next-session opens missing. Keep missing bars explicit; do not substitute nearest bars.
7. **Thinkorswim RTO study validation:** Use the uploaded same-day 1M/5M screenshot for context, but it is a noon screenshot. It cannot validate the EOD cutoff. Capture matched completed bars around 3:55 PM Arizona and identify the exact study generating any EOD bubble.

## Immediate operator sequence
1. Pull latest branch and run `.scripts\Get-Latest-And-Test.ps1`.
2. Stop the old API process, restart the newly built API, and hard-refresh `http://localhost:53360/prototypeIndex.html`.
3. Import the current scanner CSV once. Confirm 198/198 scanner rows, 187/11/0 historical counts, 0 exclusions, and 0 READY. Confirm the Unknown summary is visible.
4. Confirm Red Zone now shows the top four historical-pass candidates. If it still shows 0/4, capture the selected scanner-group value, import-status text, and browser console; this is an open code/runtime defect.
5. Refresh exclusions. Confirm 56 entries and TEST plus canonical symbols. OSW/EVR/MHK are not expected to match the canonical list.
6. On the next regular-session test, save the complete runtime-validation artifact folder and inspect the four live-assessment JSON objects for transport/timestamp/freshness.
7. Treat the RTO EOD study test and the 20-session RTO history audit as separate work. Do not rerun the 20-session capture merely to test WebSocket delivery.

## 2026-11-03 follow-up — corrected Overnight process, 10:52 scanner export, exclusions and runtime evidence

- [x] Reconciled the new master-branch procedure commit `c14380c867887f98331e382b7dc7049ac1b86894`: Overnight spans Day 1 after-hours plus Day 2 premarket; candidate review starts in the final 60 minutes BEFORE the Day 1 regular-session close. The mistaken 20:00 ET process start is withdrawn.
- [x] Copied the current master source-of-truth procedure, decision data contract, provisional trade-readiness runbook, and Schwab exclusions documentation into `docs/project-control/` on this feature branch. Copied the canonical machine-readable exclusion list into `src/MomentumCommandCenterV2.Infrastructure/Resources/schwab-exclusions.txt`. This is a selective documentation/source sync, not a branch merge.
- [x] Verified `MarketSessionClock` already derives the final-hour evaluation start from the exchange's actual regular close and the versioned calendar. It moves with early closes. Unit tests explicitly cover Nov 27, 2026 early close; visual browser acceptance remains pending.
- [x] Parsed `2026-11-03-WatchListScanner_1052am.csv`: 198 data rows, 198 unique symbols, zero duplicates. The reported 248-row scanner output is still 50 rows larger than the file.
- [x] Applied five historical gates to all 198 rows: 187 pass; 11 fail; 0 unknown. Failures: current ON >=3% (HLF, EVER, XHR, PHG, JAN, AHRT, ALSN); 5D ON <= 20D mean (CTRE, KVUE, POR); Above 20 SMA = 0 (DG).
- [x] Cross-checked all 198 symbols against the canonical 55-symbol Schwab exclusion list: zero overlaps, including OSW/EVR/MHK.
- [x] Fixed the feature branch's persisted exclusion JSON: seeded 55 canonical exclusions and preserved TEST, for 56 entries. Seed commit `2368ab989fe79fd991bf739835167eb956ed17f9` passed CI run [41694304187](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41694304187). Local API must be restarted and queried after pull; runtime count 56 is not yet user-confirmed.
- [x] Inspected uploaded runtime artifacts. RKLX 1M/5M refresh returned one exact bar each and the store contains matching observations with zero missing/mismatched bars; the idempotency flag is false because this capture inserted a new observation, not because a previously persisted duplicate was mishandled.
- [x] Runtime live assessments show SPY 1M uses ALPACA_WEBSOCKET_LATEST_BAR with 545 bars and a fresh 16:31Z source timestamp. RKLX (68 bars, 16:17Z), ASTX (117 bars, 16:24Z) and XRPN (53 bars, 16:28Z) use REST because their latest timestamps were stale at the 16:32Z assessment; bar count alone does not qualify for WebSocket latest-bar authority.
- [x] RTO 20-session artifact confirms Alpaca IEX, 3,965 fetched bars, 37/40 exact boundaries. Missing exact next-session opens: Oct 14 (+4m), Oct 21 (+1m), Nov 3 (+1m). Keep the 20-session result incomplete; do not fill from nearest bars or label it SIP.
- [x] Updated importer success feedback to report actual rendered scanner row count and actual Red Zone card count/symbols. Red Zone subtitle now clarifies imported preview is top four by Overnight Score among the five historical-gate passes; not a final Boring Overnight rank. Code CI passed in run [41693652415](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41693652415).
- [x] Added an explicit Boring Overnight Qualification panel to production and prototype dashboards. It applies the five historical CSV gates to all imported rows, queries MCC's local exclusion API, and leaves current price, 20D average volume, catalyst and chart gates as DATA?; READY remains zero until those gates are implemented and evidenced. JS commit `daf8564d751f27bca40b1c42e745ec62593f70be` passed CI [41695106655](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695106655); styles commit `23d9dafeb6f61f3c2895a0292e1084ccbc9aba5b` passed [41695141848](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695141848).
- [x] Imported scanner CSV is now persisted in browser localStorage (`mcc.scannerImport.v1`) with filename and import timestamp, restored on page reload, and re-renders scanner rows, Red Zone preview and qualification panel. Commit `ae89734ab62c702e4d16a6ba34761de99f56a0f8`; CI [41695693825](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695693825) passed.
- [ ] Browser-verify the qualification panel shows 198 imported / 187 historical pass / 11 fail / 0 unknown / 0 excluded after the latest exclusion seed is active; current price and 20D volume must remain DATA?.

- [ ] Browser-verify import feedback, `tbody#scannerRows`, and actual Red Zone `#redZoneGrid`. `#mcc-static-0278-div` is only the static scanner-table wrapper; it is not the Red Zone element and is not expected to be replaced.
- [ ] Re-export all 248 scanner rows; current 10:52 export still has 198 (50 missing).
- [ ] Current price >$5, 20D average daily volume >=500K, catalysts, Daily charts, 5M Permission/structure, and 1M execution evidence are not in this CSV. No Boring Overnight READY/BUY decision is supported yet.
- [ ] Thinkorswim cutoff test remains inconclusive: no bars were available at Arizona 15:54/15:55/15:56 for the supplied RTO/SPY chart views. Bubbles/arrows on other after-hours bars do not establish the exact cutoff bar or correct source study.
- [ ] After pulling the exclusion seed, restart API and verify `GET /api/watchlists/exclusions` returns 56 entries (55 canonical + TEST).


## 2026-11-03 follow-up — CSV metrics and exclusion comparison

- Canonical 55-symbol exclusion-list comparison across all 195 CSV tickers: **zero overlaps**. This is not proof of live Schwab tradability.
- Importer commit `649361834984292eab66fa577e960237d6ff1e7e` now preserves all seven Overnight metrics on each imported candidate and shows a compact metric summary in the reason cell.
- Importer-equivalent parsing against the uploaded CSV returned header row 3, 195 rows, 195 unique symbols and zero duplicates. Browser acceptance remains pending.

---

# 2026-11-03 — latest project handoff (authoritative follow-up)

**Active branch:** `feature/opportunity-quality-classification`  
**Latest feature branch head:** `bf47ad61adc537325fc96f60f39947351497c016`  
**Latest validated code commit:** `9302295611277ce417ca6056308b5300c1667930`  
**Exact-head CI:** [41695981736](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41695981736) — Release build and 182/182 tests passed. This includes the qualification panel, local import persistence, and historical-gate filtering of imported Red Zone candidates. Browser visual/interaction acceptance and Thinkorswim compilation remain separate and are not covered by .NET CI.

## Overnight process — corrected

The newer November 3 project-control documents on `master` supersede the erroneous 20:00 ET start note:
- Overnight includes Day 1 after-hours and Day 2 premarket.
- Review/qualify candidates in the final 60 minutes before the Day 1 regular-session close.
- Evaluation start = actual regular close minus 60 minutes.
- Buy deadline = actual regular-session close.
- Normally noon Arizona during EDT and 1:00 PM Arizona during EST; adapt to early closes using the exchange calendar.
- Do not wait until after-hours ends. Historical after-hours and next-day premarket are both part of the Overnight interval.
- Exact historical sell-side endpoint and broker buy qualifier/eligibility still need source-backed confirmation.

## Market-session clock implementation

- `MarketSessionClockSnapshot` now returns `overnightEvaluationStartUtc`, `overnightBuyDeadlineUtc`, and `overnightEvaluationOpen`.
- Both the production dashboard and prototype header display a countdown until evaluation starts (or OPEN during the window) and a countdown to the buy deadline.
- Uses `UsEquitiesSessionCalendar` and America/New_York; early-close days use the actual scheduled close.
- Production `frontend/index.html` layout was not changed.
- 182 tests passed in exact-head CI run 41679267305. Browser acceptance remains open.

Quick Start and parity references were also updated: `docs/Overnight-Scanner-Quick-Start.md` and `docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md`.

## 1M ThinkScript source

- Project Library search found the full `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` source (1,260 lines, V14.5.1). The uploaded `CurrentStudy_CouldBeWrong.txt` is V14.5 (1,424 lines), not the same source; substantial differences exist in execution-window, reconstructed 5M, state and bubble logic. V14.5.2 is now derived from the actual Library V14.5.1 baseline.
- Rebased `docs/Charts/1M/MainStudy_V14.5.2.txt` on the actual V14.5.1 Library source (1,298 lines); preserved the upload unchanged.
- V14.5.2 matches 5M V4.1.7 Permission: 5M close > Daily VWAP, 5M close > EMA9, EMA9 >= EMA20, RSI >= minimum. The separate 1M VWAP execution gate remains intact.
- V14.5.2 maps Arizona 3:55 PM to 18:55 EDT / 17:55 EST using the same US DST calendar logic as the 5M study.
- Do not replace the installed study until TOS compile succeeds and matched-bar outputs are checked in both DST regimes. GitHub CI cannot compile ThinkScript.

## November 3 scanner export

Detailed review: [Overnight Scanner Full-Universe Review](Validation/Overnight-Scanner-Full-Universe-20261103.md).

- CSV has two metadata lines, header on line 3, 195 unique ticker rows, zero duplicates.
- The scanner reportedly displayed 248; 53 rows are not present in this export.
- Five available historical-metric gates: 107 pass, 81 incomplete, 7 fail.
- Top historical-score triage: MHK, PUK, AON, DANOY, GEHC, WWW, VSXY, TRN, NAC, LAMR, VRTX, XPOF, FCPT, SWX, OPCH.
- These are not approved Boring Overnight candidates. Current price, 20D average volume, current exclusions, catalyst clearance and current chart states are still required.
- OSW, EVR and MHK are not on the canonical 55-symbol Schwab exclusion source found on master. Live broker eligibility and the feature branch's local App_Data exclusion file remain unknown.

## Runtime evidence supplied by user

### Market-data delivery
- API health check passed.
- SPY advanced 15 to 20 stored 1M bars (+5); XRPN advanced 1 to 2 (+1); RKLX stayed at 2; ASTX stayed at 2.
- RKLX idempotency comparisons were false for both 1M and 5M. Do not assume failure solely from a symbol not printing; inspect source transport, source timestamp, expected eligible prints and raw artifacts.
- Require WebSocket provenance, >=50 completed bars and freshness before expecting `ALPACA_WEBSOCKET_LATEST_BAR`; 5M permission remains REST-backed.
- Raw JSON artifacts were saved locally under `artifacts/runtime-validation/20261103T162715Z`; they are not committed/available in this review. Preserve them with branch SHA and exact test window.

### RTO 20-session history
- The supplied table contains 40 expected boundaries.
- Missing exact next-session opens: 2026-10-14 13:30Z (nearest +4m), 2026-10-21 13:30Z (nearest +1m), 2026-11-03 14:30Z (nearest +1m).
- Other displayed boundaries are exact. Do not impute adjacent bars.
- The pasted table does not expose actual feed metadata; inspect saved JSON before calling it SIP.

## Branch / merge

- GitHub compare: `master...feature/opportunity-quality-classification` reports status `diverged`, 1,194 commits ahead, 160 behind, 300 changed files.
- PR #7 was closed as incorrectly based on master and explicitly warns against wholesale merging.
- Master contains newer November 3 Overnight docs and the canonical 55-symbol exclusion provider; feature contains current project work and new commits.
- **Do not merge yet.** Reconcile master-only commits and branch-only code deliberately, identify the merge base/conflicts, then create a reviewed PR. No merge was performed.

## Source identity correction

The actual V14.5.1 Library file was found and read in full. V14.5.2 is now rebased on that file, not the user's V14.5 upload. See `docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md` for the exact source distinction and corrections.

## Immediate operator actions

1. Pull feature branch after exact-head CI; run `.\scripts\Get-Latest-And-Test.ps1` and hard-refresh `/prototypeIndex.html`. Verify both countdowns and the regular-session clock.
2. In TOS, add V14.5.2 as a separate study; do not overwrite V14.5.1 or V14.5. Compile first. Check 1M and 5M outputs at 15:54 / 15:55 / 15:56 Arizona on standard- and daylight-time dates; include the exact 5M completed-bar intervals and Permission components.
3. Export the full 248-row scanner result and verify 248 data rows after preamble/header. Then browser-import the 195-row file and the corrected full export.
4. Provide current Daily/5M/1M charts for the top filtered candidates and a current runtime exclusion-list response/file.
5. Inspect the local runtime JSON artifacts for sourceTransport/sourceTimestampUtc, per-symbol stream freshness and RKLX idempotency. Preserve branch SHA and exact window.
6. Inspect RTO JSON feed metadata and classify the three missing opens. Never replace them with nearest-minute bars.
7. Continue selective master/feature reconciliation; do not merge wholesale.

## [Uncertainties]

- Installed TOS study identity is not proven byte-for-byte; V14.5.2 is a proposed source revision pending TOS compile/parity.
- The CSV export count mismatch (195 versus 248) is unexplained.
- Live broker tradability, local runtime exclusions and catalyst clearance remain unverified.
- Runtime JSON artifacts are local and were not available in the repository review.
- The exact historical Overnight sell-side endpoint and Schwab buy qualifier remain unresolved.

---

## 2026-11-03 — full Master To-Do audit, EOD source correction, shortlist and prototype overflow

**Branch:** `feature/opportunity-quality-classification`  
**Latest code head before documentation updates:** `89b9fe73408cee06f0ddfedc1486b7038d335a2a`  
**Exact-head CI:** [Actions run 41575143773](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41575143773) — restore, Release build and tests all succeeded.  
**Full audit:** [Full Master To-Do Audit — 2026-11-03](Validation/Full-Master-To-Do-Audit-20261103.md).

### Confirmed user runtime results
- User ran `Get-Latest-And-Test.ps1` at `0917a90...`: Release build passed, 179 tests passed, 0 failed, 0 skipped.
- User imported the committed `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`: 50 rows imported; Red Zone updated; MHK, OSW, EVR and EHC populated.
- User accepted title/clock/transport-label positioning and visual polish.
- User has not yet pulled the latest prototype overflow fix.

### Prototype horizontal overflow fix
- Actual source: `frontend/prototypeIndex.html`, not production `frontend/index.html`.
- Root cause: prototype topbar had `margin-left:104px` without reducing its width.
- Fixed desktop width to `calc(100% - 104px)`; mobile width to `calc(100% - 78px)`.
- Commit: `3aac28cf638228fd0901251e59969d08af248e17`.
- User must pull and hard-refresh; confirm scrollbar gone at desktop and mobile.

### 1M/5M EOD correction — source identity caveat
- Repository 5M `docs/Charts/5M/MainStudy.txt` is normal V4.1.7. It uses the shared DST calendar logic and now has `eodTimeEasternStandard=1755`, `eodTimeEasternDaylight=1855`.
- Repository 1M `docs/Charts/1M/MainStudy.txt` identifies itself as V14.4.11 (time-gated Boring Overnight decision), not the full 1M V14.5.1 Library baseline. Its standard-time constant was also changed to 1755, but **do not install V14.4.11 as the full V14.5.1 replacement**.
- The full `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` source was not found in repo tree or Project file search. User must export/upload the full V14.5.1 source before it can be patched.
- Correct 3:55 PM Arizona mapping: 18:55 EDT in US daylight time; 17:55 EST in US standard time.
- CI does not compile ThinkScript. TOS compile and matched-bar diagnostics remain mandatory.
- Test 1M at Arizona 15:54/15:55/15:56; 5M at bar-open stamps 15:50/15:55/16:00, recording interval boundaries, DST flag, selected ET cutoff, and EOD state. Repeat in both DST and standard-time dates. Disable studies one at a time to identify screenshot bubble sources.

### Overnight shortlist disposition
- OSW: Nov 2 close $30.22; external 20D avg volume ~806,703; 5M WAIT / Permission NO / BEARISH; screenshot close $30.02, VWAP $30.37, RSI 29.5, RVOL 16.52, extension 3.35 ATR; Oct 28 earnings catalyst; WATCH/REJECT for boring overnight.
- EVR: Nov 2 close $303.26; external 20D avg volume ~641,313; 5M WAIT / Permission NO / BEARISH; screenshot close $303.26, VWAP $308.69, RSI 32.8, RVOL 21.95, extension 2.98 ATR; Oct 28 earnings catalyst; WATCH/REJECT.
- MHK: Nov 3 reported price $150.01; external 20D avg volume ~1,336,567; 5M WAIT / Permission NO / NEUTRAL; screenshot close $148.60, VWAP $148.59, RSI 51, extension 0.25 ATR; Oct 29 earnings/weak demand outlook and new buyback; current ON 3.13%, 5D mean 2.49% vs 20D 0.31%; WATCH only.
- Price and 20D average volume gates cross-check as PASS for all three; external statistics are not MCC provider data.
- Exclusion status: Data not found in sources. No current Daily/5M/1M chart set was supplied for Nov 3. No candidate qualifies for BUY/HOLD; zero position is valid.
- Full CSV remains missing: committed export has 50 rows while screenshot reported 248. User should export the full 248-row list and commit/upload it to test full parser/import.

### Red Zone / Boring Overnight implementation truth
- Existing code includes Boring Overnight hard-gate evaluator, explicit close/open history calculator, history evaluation that nulls caller-entered history when incomplete, candidate snapshots, local exclusion CRUD, and quantile sell-limit calculator requiring >=20 complete windows and ATR parity MATCH.
- Existing `OpportunityQualityAssessor` is a diagnostic classifier, not a versioned automatic top-four ranker.
- Still missing for actionable Boring Overnight READY: daily price/SMA20/20D volume provider, complete exclusion reconciliation, validated catalyst classification/coverage, current authoritative 5M + 1M source evidence, immutable full-universe candidate snapshots and full GTC_EXT outcomes.
- Calculated Limit Sell remains DATA?/not calculated; do not use +0.5% heuristic as target.
- Alpaca news client returns unclassified headlines; no catalyst CLEAR should be asserted from caller input.

### TOS scan automation impediment
Public Schwab Trader API documentation describes a separate developer product for market data/account/trading, but no public endpoint for retrieving custom Thinkorswim saved-scan results/custom columns or a TOS scanner webhook was found. Do not scrape the TOS desktop. Recommended route: local watched-folder CSV adapter (hash/idempotency, archive/reject folders, provenance, parse diagnostics, audit status) with manual upload fallback. This adapter is not yet implemented. Official references: https://developer.schwab.com/products/trader-api--individual and https://developer.schwab.com/user-guides.

### Alpaca collector status
- `AlpacaScannerDiscoverySource` calls movers endpoint `v1beta1/screener/stocks/movers?top=50`, so it is bounded top gainers/losers, not full-market breadth or TOS scan.
- Polling worker interval is 30–3600 seconds; events persist provider-neutrally.
- User identified local `secrets.json` as feed override. SIP was selected but recent SIP request returned HTTP 403 entitlement denial; IEX was restored and returned validated data (62 bars).
- SIP cannot be validated until Alpaca grants entitlement. Regular-session discovery freshness and WebSocket delivery/recovery still require runtime captures.

### What user should do next
1. Pull latest branch and hard-refresh prototype; confirm horizontal scrollbar is gone.
2. Export/upload the full 1M V14.5.1 ThinkScript source (no credentials).
3. Export/commit the complete 248-row TOS Overnight CSV.
4. Compile 5M V4.1.7 and full 1M V14.5.1 in TOS; run the DST/standard before-at-after diagnostic matrix and provide output.
5. During regular market hours, capture discovery/provider and WebSocket status before/after 5–10 minutes for SPY/RKLX/ASTX/XRPN.
6. Provide current Daily/5M/1M charts for OSW/EVR/MHK if they remain under consideration.
7. Keep Boring Overnight manual-only until source-backed catalyst, exclusion, daily liquidity and 20-session history gates pass.

## 2026-11-03 — Scanner CSV import retry

The synced export is now present at `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`. It contains two metadata preamble lines (`Watchlist Scanner`, `Results`) followed by the actual comma-delimited header on line 3. The importer previously assumed line 1 was the header, which caused the reported missing Symbol/Ticker error.

`frontend/app.js` now searches the first 25 nonblank rows for a normalized Symbol/Ticker header, then imports the data rows after it. The uploaded file was parsed against the same delimiter/header rules: comma delimiter, header row 3, **50 nonblank ticker rows**, first three MHK/OSW/EVR with Overnight Scores 73.42/70.97/68.25. Browser interaction has not yet been exercised, so final UI acceptance remains pending. Do not re-upload or edit the CSV to remove its preamble.

Current importer fix commit: `b05c63713452cbbbb5632a87c639f655ad027e73`. GitHub Actions run 41549757022 is still running at the time this note was added.

## 2026-11-02 — latest follow-up: script, CSV, prototype, ThinkScript parity, and overnight decision

- Branch: `feature/opportunity-quality-classification`. The November 2 follow-up source changes are committed. GitHub Actions run 41546963701 passed .NET build/test on exact head `825276a844a5b3079aa3ea4bd1cb8ccbd90e283b`; local PowerShell execution and browser acceptance remain open.
- Fixed `scripts/Get-Latest-And-Test.ps1` PowerShell refspec interpolation by using `${Branch}` before the colon.
- Hardened the scanner CSV importer for UTF-8 BOM, comma/semicolon/tab delimiters, quoted fields, `Symbol`/`Ticker`/`Symbol/Ticker`, and `Overnight Score`. The specific Overnight Scanner export was not found in the active branch tree; its exact import remains unverified.
- Prototype clock now sits directly to the right of the title inside the title parent; transport-label text no longer inherits the yellow dot background.
- Current full 1M Library source is `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (V14.5.1). The later V14.4.10 artifact is an isolated aggressive-lane audit-only mode, not the normal full study. V14.5.1 retains a reconstructed 5M Permission gap (missing 5M Daily VWAP) and a fixed 15:55 Eastern EOD cutoff that does not equal 3:55 PM Arizona.
- The 5M V4.1.7 source uses 18:55 Eastern in daylight time (correct for 3:55 PM Arizona) but 19:55 Eastern in standard time; correct standard-time mapping is 17:55 Eastern. Both studies need a targeted shared DST-aware EOD correction and matched-bar validation. Do not trust 1M READY/EOD labels as standalone authority until compiled and parity-tested.
- The four supplied screenshots include the Overnight scan and three **1D/5m** charts, not 1M charts. EVR and OSW show visible EOD GET OUT bubbles on their 5m charts, but the generating study/source has not been isolated; their top 5M labels remain WAIT/Permission NO/BEARISH; MHK shows 5M WAIT/Permission NO/neutral structure, with no visible EOD GET OUT. No 1M state is established.
- Overnight disposition: no new BUY/HOLD for MHK, OSW, or EVR. MHK current ON is 3.13% and its 5D mean is elevated relative to its 20D mean. EVR/OSW have visible EOD GET OUT bubbles on their 5m charts, but the generating study/source has not been isolated; their top 5M labels remain WAIT/Permission NO/BEARISH.
- Calculated Limit Sell remains DATA?/not calculated: no 20 complete GTC_EXT windows, walk-forward target-touch evidence, and exact ATR parity. Do not use +0.5% as a calculated target.
- See current source of truth: `docs/Master-To-Do.md`, `docs/Active-Work-Checklist-20261102.md`, `docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md`, and `docs/Validation/Overnight-Scanner-Review-20261102.md`.

---

## 2026-11-02 — after-hours WebSocket status snapshot

- API health returned OK.
- WebSocket returned enabled=true, connected=true, state=SUBSCRIBED, feed=iex, and acknowledged SPY/RKLX/ASTX/XRPN.
- The user ran this after 3 PM Arizona. lastBarUtc=null, receivedBars=0, persistedBars=0, barsBySymbol empty.
- Interpretation: API liveness and subscription acknowledgement passed. Regular-session delivery is NOT VERIFIED; after-hours zero bars do not indicate a failure. Run only Runtime Acceptance Runbook §1A during the next regular session, not the entire runbook.

## 2026-11-02 — Technical Workflow, Overnight Scanner and Red Zone

**Branch:** `feature/opportunity-quality-classification`
**Purpose of this handoff:** latest user requests for a repeatable technical update workflow, prototype header repair, Red Zone auto-update, and Overnight Scanner candidate review.

### New canonical workflow
- Operator entry point: `docs/Technical-Workflow.md`.
- Detailed runtime acceptance source of truth: `docs/Runtime-Acceptance-Runbook.md`.
- User should not run every specialized runbook test after every commit. Standard sequence is clean-tree get-latest script → Release restore/build/test → fully stop old API → restart → API health/provider/stream/collector/live assessment smoke checks → browser acceptance. Run specialized market-hours or feature acceptance only when required.
- New script: `scripts/Get-Latest-And-Test.ps1`. It refuses dirty working trees, fetches and switches/pulls the project branch, then runs Release restore/build/test and prints the SHA.

### Prototype and Red Zone changes
- Prototype `system-readiness` was incorrectly nested inside `topbar-brand`. It is now a sibling inside `topbar-layout`, matching production's two-column structure.
- `marketTransportLabel` now overrides inherited anywhere-wrapping and wraps at spaces.
- CSV import previously updated only the scanner table. It now also calls `renderMorningReport()` immediately, so Red Zone updates as soon as a new CSV is imported.
- Existing automatic cadence: Red Zone 1M/5M assessment cards refresh every 15 seconds while the page is visible; Alpaca persisted top-gainers/losers discovery refreshes every 30 seconds only when the Alpaca live-discovery group is selected.
- Important boundary: Thinkorswim Overnight scan is not automatically connected to MCC. Export/import CSV remains manual. Alpaca discovery is a separate SIP-based top-50 gainers/losers feed and current subscription denied recent SIP requests (HTTP 403).

### Overnight Scanner screenshot — 2026-11-02
- Screenshot reports 248 results, 50 displayed, sorted by Overnight Score.
- Top visible raw-score rows: MHK 73.42, OSW 70.97, EVR 68.25.
- Recommended chart-review order: OSW, EVR, MHK. MHK is raw rank #1 but current ON=3.13% and the recent surge makes it the hottest/least boring candidate.
- Full review: `docs/Validation/Overnight-Scanner-Review-20261102.md`.
- Operator instructions: `docs/Overnight-Scanner-Quick-Start.md`.
- Need user Daily and 5M charts for all three; price, 20D average volume, exclusion status and catalyst clearance are not visible in the screenshot.

### Next actions
1. Pull latest branch and run `.\scripts\Get-Latest-And-Test.ps1`.
2. Stop the previous API process completely, restart from the latest build, then hard-refresh `http://localhost:53360/prototypeIndex.html`.
3. Visually confirm header layout, transport label wrapping and no horizontal overflow.
4. Import a fresh TOS Overnight CSV and verify Red Zone changes immediately. Check that live card assessments refresh after 15 seconds. Do not expect TOS data to update without a new CSV import.
5. Upload Daily and 5M charts for OSW, EVR, and MHK. Review 1M only if a specific entry/exit timing decision is needed.
6. On the next regular session, run the dedicated WebSocket delivery acceptance; the November 2 after-hours SUBSCRIBED snapshot is only a connection check.
7. Continue Red Zone live discovery only after SIP entitlement or a different supported source is available.

### Schwab app form — lower priority, no blocker

- Choose **Accounts and Trading Production** only for Phase 1 read-only account/position access; do not select Market Data Production yet.
- App name: **Momentum Command Center**.
- Description: `Read-only account and position synchronization for Momentum Command Center. No order placement or trading actions are performed.`
- Callback URL: do not submit a guessed value. MCC's OAuth callback route and permanent HTTPS origin are not implemented/selected yet. The current localhost HTTPS launch URL is not a verified callback. See `docs/Thinkorswim-Schwab-Integration-Plan.md`.
- Schwab app creation is lower priority and not a blocker.

### Current files
- `docs/Technical-Workflow.md`
- `scripts/Get-Latest-And-Test.ps1`
- `docs/Overnight-Scanner-Quick-Start.md`
- `docs/Validation/Overnight-Scanner-Review-20261102.md`
- `docs/Active-Work-Checklist-20261102.md`
- `docs/Master-To-Do.md`
- `docs/Scanner-and-Watchlist-Workbench.md`

### [Uncertainties]
- Full scan export was not supplied; only the first 50 of 248 results are visible.
- Price and 20D average volume for the top three are not shown; those gates are unverified.
- Exact TOS Overnight Score formula and its relationship to the documented Boringness Score have not been certified.
- Live Alpaca discovery freshness is unverified while recent SIP requests are denied.

---
## 2026-11-02 — latest handoff: SIP entitlement, 1M timestamp, TOS parity, Schwab

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Branch:** `feature/opportunity-quality-classification`  
**Latest verified source/script head:** `237fcbfc71f9b03bda64b1e6047d61355c849447` (Release build, 0 warnings/0 errors, 179/179 tests; [Actions run 41540235754](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41540235754)). Subsequent control-document updates are documentation-only and are included in the current branch-head CI run.

### Current facts
- Alpaca IEX override source is known: tracked appsettings.json had sip; local secrets.json had iex and won by configuration precedence.
- User set secrets.json to sip; provider status reported configuredFeed=sip, but the INO live assessment returned Alpaca HTTP 403: `subscription does not permit querying recent SIP data`.
- User restored IEX; provider status returned VALIDATED_DATA_RETURNED and 62 bars. Keep IEX for usable data. SIP is configured only when selected, not entitled/validated.
- User confirmed the prototype Main Chart layout looks good and fits without horizontal scrolling.
- Main Chart timestamp display was corrected: API sourceTimestampUtc is the bar start; completedAtUtc is the close. UI now shows close time plus the bar interval. The current in-progress minute remains intentionally excluded. This does not prove provider latency is solved.
- Current INO comparison: TOS Close 2.06 / VWAP 1.89 / ATR14 0.02; MCC Close 2.05500 / VWAP 1.87031 / ATR14 0.01429. TOS EXT 24.6 was a live/latest label, not a historical per-bar reading. Exact parity remains open.
- Schwab Trader API — Individual approval is reported complete. App registration, Accounts and Trading Production attachment, app-level Active/Ready status, callback URL, secrets, OAuth and position sync remain open.

### Key docs
- Active work checklist: `docs/Active-Work-Checklist-20261102.md`.
- Canonical Master To-Do: `docs/Master-To-Do.md`.
- SIP and extension diagnostics: `docs/Validation/Live-Extension-and-Feed-Diagnostics-20261102.md`.
- Main Chart completed-bar timestamp/latency: `docs/Validation/Live-Bar-Timestamp-Latency-20261102.md`.
- Thinkorswim parity worksheet: `docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md`.
- Schwab integration setup/implementation plan: `docs/Thinkorswim-Schwab-Integration-Plan.md`.
- Runtime procedures and RTO boundary audit: `docs/Runtime-Acceptance-Runbook.md`.

### What to do next
1. Run CI for the latest branch head after the frontend timestamp correction and documentation updates.
2. Pull/rebuild and hard-refresh `prototypeIndex.html`; verify the revised completed-at timestamp on desktop/mobile.
3. During regular market hours, capture the raw INO live-assessment response. Compute `evaluatedAtUtc - oneMinute.completedAtUtc`; if >=60 seconds on a liquid symbol, investigate provider/source latency. Do not lower freshness thresholds.
4. Complete exact-date/same-completed-bar TOS comparison with five-decimal Close, VWAP, ATR14 and historical ExtensionATR from the Data Box. Do not use the live AddLabel value.
5. Keep the RTO SIP audit blocked until recent SIP data is actually authorized and returned. Prior 38/40 evidence is IEX; missing opens are 2026-10-14 13:30Z and 2026-10-21 13:30Z.
6. Continue Schwab read-only integration after app registration and callback URL are confirmed. Never ask for App Key, App Secret, authorization code or tokens in chat. Do not add order-writing endpoints.
7. Continue Red Zone opportunity ranking and Boring Overnight source/target validation as listed in the active checklist. Boring Overnight remains WATCH/manual, not actionable READY.

### RTO audit explanation
The RTO audit is not a strategy-performance score. It checks exact provider/store presence for 40 expected prior-close and next-open 1-minute bars across 20 sessions. It distinguishes a missing provider bar from a persistence failure and reports the nearest returned timestamp. The prior IEX response had 38/40 exact bars; the two missing next-session opens are listed above. The filename `RTO-SIP-20261101.json` does not establish SIP: its payload said `feed=iex`.

### [Uncertainties]
- Actual one-minute provider latency is not measured; the user has not supplied the timestamped live-assessment JSON for the affected interval.
- Exact TOS VWAP/ATR14 and historical ExtensionATR values for the same completed candle remain unavailable.
- Schwab app-level status, attached product and callback URL remain unknown.
- SIP entitlement may change later; current observed request was denied with HTTP 403.

---
## 2026-11-02 — user's live IEX response and final Main Chart layout pass

- User confirmed `src/MomentumCommandCenterV2.API/appsettings.json` contains `Feed: sip`; `appsettings.Development.json` is absent; launch profile sets only `ASPNETCORE_ENVIRONMENT=Development`.
- Local `/api/providers/status` still reported `configuredFeed: iex`, and live assessment also reported IEX. The launch profile shown does not explain that override. Development User Secrets and process/user/machine environment variables have higher precedence than JSON. New provider status now reports `configuredFeedSource` and `feedConfigurationSources`, and `configuredFeed` comes from the actual adapter instance.
- REST adapter, WebSocket worker, and live assessment now default to SIP when the setting is absent; adapter feed is normalized and exposed. Added a test for missing-feed configuration defaulting to SIP. Explicit higher-precedence IEX configuration can still override.
- Main Chart prototype markup has been rearranged per user: ticker at left, Main Chart title/description in the middle, Live Opportunity Classification at right. Chart is ~2/3 desktop width; right side is 2x3 metrics (1M Close / Daily VWAP; ATR14 / Relative Volume; ATR Extension / TBD). It stacks at narrower widths. This layout is scoped to `prototypeIndex.html`, not the production page.
- Close, Daily VWAP, ATR14 now display five decimals; the exact source timestamp is shown in Arizona time and UTC below 1M Close.
- Reviewed user commit `e7299ddd`: `RTO-SIP-20261101.json` content explicitly says `feed: iex`, 38/40 exact boundary bars, missing Oct 14 and Oct 21 opens, 18/20 overnight pairs. The filename does not establish SIP.
- Thinkorswim how-to / study: [Thinkorswim-MCC-Extension-Parity-20261102.md](Validation/Thinkorswim-MCC-Extension-Parity-20261102.md).
- [x] Release build and **179/179 tests** passed on code source head `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae` in Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777). The docs-updated head `4443506be982d0ff6726992c7b2294d97871bad2` also passed run [41504521572](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504521572), 179/179.
- [ ] After local rebuild/restart, inspect `configuredFeedSource` and `feedConfigurationSources`. If User Secrets is the effective source, inspect `dotnet user-secrets list --project src/MomentumCommandCenterV2.API`; if EnvironmentVariables, inspect `MarketData__Alpaca__Feed`. Do not expose credentials.
- [ ] Confirm observed 1M and 5M feeds are SIP, then repeat RTO audit.
- [ ] Compare TOS and MCC using same exact date/time, with five-decimal close/VWAP/ATR14 and same session scope.
- [ ] Browser-check the new Main Chart layout and no horizontal overflow at desktop/mobile sizes.

## 2026-11-02 — latest user commits, feed source diagnosis, and Main Chart re-layout

### User commits reviewed
- [x] Reviewed two commits ahead of the previously verified head: RTO JSON update and a substantial `frontend/prototypeIndex.html` update.
- [x] Preserved the user's prototype edits and applied the requested Main Chart layout on top.
- [x] Important evidence correction: file `RTO-SIP-20261101.json` is still `feed: "iex"`, not SIP. It records 3,949 fetched bars, 38/40 exact boundaries, two missing opens (Oct 14 and Oct 21), and 18/20 valid overnight pairs. Its filename does not establish SIP provenance.

### Latest fixes in progress / committed source
- [x] API provider status now reports the effective feed from the Alpaca adapter and the configuration providers that supplied `MarketData:Alpaca:Feed`. This should reveal whether IEX came from an environment variable, user secrets, JSON provider, or another override.
- [x] Changed REST adapter, WebSocket worker, and live assessment fallback default from IEX to SIP. Missing configuration no longer silently selects IEX.
- [x] Added an adapter test proving missing feed configuration defaults to SIP.
- [x] Main Chart now has three header columns: ticker chip, Main Chart title/description, and Live Opportunity Classification.
- [x] Chart occupies about two-thirds of the desktop content width. Five existing metrics plus a clearly marked TBD slot are arranged in a 2-column × 3-row grid to the right. Layout collapses responsively rather than forcing horizontal scrolling.
- [x] Close, Daily VWAP, and ATR14 are now shown at five decimal places; diagnostic text uses the same precision.
- [x] Added [Thinkorswim / MCC Extension Parity](Validation/Thinkorswim-MCC-Extension-Parity-20261102.md), including a ThinkScript study and exact-bar worksheet.

### Feed acceptance — still requires user's local runtime
The user supplied `appsettings.json` with `Feed: sip`, but both live endpoints still report `iex`. `launchSettings.json` contains only `ASPNETCORE_ENVIRONMENT=Development`; no explicit feed override is shown there. The cause is not yet proven. The new `/api/providers/status` diagnostics report:
- `configuredFeed`: feed cached by the actual Alpaca adapter instance
- `configuredFeedSource`: highest-precedence configuration provider with that key
- `feedConfigurationSources`: every provider contributing a value for that key, in provider order

After rebuild/restart, inspect these fields. Environment variables and user secrets can override appsettings. If the new source says `appsettings.json` but feed remains IEX, confirm the actual process content root/output directory and that the latest API assembly is running. If it identifies EnvironmentVariables, inspect only the named feed variable `MarketData__Alpaca__Feed` (do not expose credentials).

### Thinkorswim parity status
At the user's reported 11:26 bar: close approximately $2.00 in both; MCC previously showed VWAP $1.838, ATR14 $0.013, extension 11.89; TOS showed EXT 22.92. The prior MCC UI rounded inputs too aggressively for a conclusive ratio check. The UI now shows five decimals. MCC VWAP is HLC3 volume-weighted using RTH 1M bars; ATR14 is SMA(TrueRange,14). The new parity note tells the user how to expose ATR14 and extension as ThinkScript plots and inspect the historical candle with the Data Box. No numerical parity conclusion is made yet.

### Remaining acceptance
- [ ] Run Release build and all tests on the latest source head.
- [ ] Re-query `/api/providers/status`; capture `configuredFeed`, `configuredFeedSource`, and `feedConfigurationSources`.
- [ ] Verify live assessment `configuredFeed`, top-level `feed`, `oneMinute.feed`, and `fiveMinute.feed` are SIP.
- [ ] Confirm SIP entitlement / provider delivery; do not infer from config alone.
- [ ] Compare TOS/MCC close, VWAP, ATR14, and extension on the same date and exact completed 1M timestamp with five-decimal inputs.
- [ ] Browser-check desktop and mobile Main Chart layout and verify no horizontal overflow.
- [ ] Re-run RTO boundary audit only after observed SIP is confirmed. The latest committed RTO JSON is IEX, 38/40 exact, with Oct 14 and Oct 21 opening gaps.
- [ ] Confirm whether the new TBD metric should remain a placeholder or be assigned a defined metric. Do not invent one.

### [Uncertainties]
- Local API still reports IEX despite the supplied JSON showing SIP; source provenance is pending the new endpoint diagnostics.
- SIP entitlement and actual SIP delivery are not established.
- The exact date/timezone of the reported 11:26 comparison and the unrounded inputs are not yet available.
- Browser rendering has not been observed from the user's machine.

---

## 2026-11-02 — follow-up: test failures, visible extension inputs, dock labels, and SIP configuration

### User's latest local verification
- Release build succeeded for Core, Application, Infrastructure, API, and Tests.
- The first full run reported 176 total / 172 passed / 4 failed.
- Failure 1: `V2SignalEngineTests.SuppliedAtrExtensionIsPreservedAsThinkorswimDailyVwapMetric` asserted hard-exit flags while calling `Evaluate(..., hasPosition: false)`. Corrected the test to evaluate an existing position.
- Failures 2–4: three TradeAnalyzer tests returned `ENTRY_FILTER_FAILURE`. Root cause: shared `StrongSnapshot` fixture set VWAP = close − 10 while ATR = 1. Under the new absolute close-to-VWAP metric this represented a 10-ATR extension and the engine correctly blocked fresh entry. Corrected the test fixture to VWAP = close − 0.25. Production thresholds and entry logic were not relaxed.
- Added tests for absolute positive/negative VWAP distance and zero-ATR handling. The first CI attempt caught decimal `[InlineData]` compile errors; replaced them with compile-safe fact assertions. GitHub Actions run [41491616284](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41491616284) passed the Release build and **178/178 tests** on source commit `c0412da1a5e4c40f8853bd07d5791277fc3c7f22`.

### Current code changes
- Centralized the formula in `V2SignalEngine.CalculateAtrExtension(close, dailyVwap, atr)`; both the live calculator and engine fallback call it.
- Live assessment already calculated and returned `oneMinute.close`, `oneMinute.vwap`, and `oneMinute.atr`; the prototype now displays these explicitly as **1M Close**, **Daily VWAP**, and **ATR14**, alongside 5M RVOL and normalized **ATR Extension**.
- Red Zone's extension line now displays close, Daily VWAP, ATR14, and extension inputs. The previously reported MCC `11.20 ATR` is the normalized extension value, not raw ATR14.
- The prototype dock now has **INTRADAY MOMENTUM** to the left of the switch, **BORING OVERNIGHT** to the right, and **WORKSPACE** below the switch.
- API provider status and live assessment now expose the effective `configuredFeed`; live assessment also exposes observed `oneMinute.feed` and `fiveMinute.feed`.
- Committed API `appsettings.json` default is now `sip`; adapter test asserts configured SIP is sent and tagged on returned bars, with no IEX fallback.
- New detail: `docs/Validation/Live-Extension-and-Feed-Diagnostics-20261102.md`.

### Feed diagnosis / user action
The Alpaca adapter reads `MarketData:Alpaca:Feed` from the effective .NET configuration and sends that exact value as the REST `feed=` query. If the user still sees IEX, stop the old API process completely, pull latest branch, rebuild, restart, and inspect:
1. `GET /api/providers/status` → Alpaca `configuredFeed` must be `sip`.
2. `GET /api/market-data/live-assessment?symbol=INO&providerName=Alpaca` → `configuredFeed`, top-level `feed`, `oneMinute.feed`, and `fiveMinute.feed`.
3. If configuredFeed remains IEX, inspect `appsettings.Development.json`, environment variable `MarketData__Alpaca__Feed`, Visual Studio launch environment, and the actual process/output directory. Environment variables override appsettings. If configuredFeed is SIP but observed bars say IEX, capture sanitized JSON and investigate provenance; do not mix feeds.
4. If SIP returns an entitlement/provider error, preserve it. Do not fall back to IEX and claim SIP validation.

### Thinkorswim parity — still unresolved
Compare one identical completed 1M symbol/timestamp and record:
- close (price units)
- Daily VWAP (price units)
- ATR14 (price units; simple average of 14 TrueRange values)
- `Abs(close - Daily VWAP) / ATR14` (dimensionless ATR extension)
The earlier `EXT: 24.5 ATR` vs MCC `11.20 ATR` is not explained yet. Feed, bar timestamp, session VWAP, and ATR inputs must be matched before diagnosing the residual. Do not tune warning/hard-exit thresholds to force parity.

### Remaining acceptance
- [VERIFIED] Release build and 178/178 tests passed on source commit `c0412da1a5e4c40f8853bd07d5791277fc3c7f22`, Actions run 41491616284. Recheck CI after any further source change.
- Verify configured and observed SIP feed after full process restart; verify entitlement and 1m/5m feed separately.
- Match Thinkorswim and MCC inputs on the same completed bar.
- Browser-check prototype dock and metric cards at desktop/mobile sizes.
- RTO remains 38/40 exact on IEX; October 14 13:30Z and October 21 13:30Z open bars remain missing. Repeat with actual SIP bars before calling it an SIP audit.
- All previous RTO, Boring Overnight, scanner, Red Zone ranking, browser, WebSocket, and Schwab acceptance items remain as recorded below.

### [Uncertainties]
- Post-fix build/test result is not available.
- Effective feed source in the user's running process is unknown until `configuredFeed` is read from the new endpoint.
- SIP entitlement and actual returned SIP bars are unverified.
- Thinkorswim numeric parity and browser visual acceptance are unverified.

---

## 2026-11-02 — xUnit, prototype dock, extension parity, and RTO follow-up

### Source changes
- Fixed xUnit2031 in `UsEquitiesSessionCalendarTests.UsesJulyThird2028EarlyClose` with `Assert.Single(boundaries, predicate)`; the test remains enabled.
- Prototype dock places vertical `BORING OVERNIGHT` to the right of the toggle and `WORKSPACE` beneath it. The accessible toggle label updates to the destination mode.
- Corrected the Red Zone extension label to describe the installed Thinkorswim metric.
- Corrected the prototype Main Chart and Red Zone extension labels to `1M |close − Daily VWAP| / ATR14`; the API methodology now discloses the formula and its remaining session-VWAP parity limitation.
- Latest source commits include the technical-calculator change (`31b0bf3`) and final prototype label sizing (`92b1412`).

### Extension parity diagnosis and correction
- Installed Thinkorswim 1M source defines `price = close`, `dailyVWAP = vwap(period = AggregationPeriod.DAY)`, and `extensionATR = AbsValue(price - dailyVWAP) / atr`, where `atr = Average(TrueRange(high, close, low), 14)`.
- MCC previously calculated `max(0, (close - EMA9) / ATR)` and used Wilder-style recursive ATR. This was not the installed study's formula.
- Updated `LiveSymbolTechnicalCalculator` to use absolute close-to-session-VWAP distance and a simple 14-bar TrueRange average; updated the V2 fallback to the same VWAP-based definition. Added a regression test for preserving a supplied extension value.
- Remaining acceptance: compare MCC and TOS on the same symbol, completed 1M timestamp, Daily VWAP and 14-bar ATR. The current MCC session-VWAP implementation still needs matched-value parity verification. Do not claim exact parity before that evidence.

### RTO feed evidence — November 2
- User's posted outputs report `feed=iex` in both captures. The second output includes per-boundary status labels, but it is still IEX and is not an SIP comparison.
- 38/40 boundaries are exact. Missing next-session opens remain 2026-10-14 13:30Z (nearest 13:34Z) and 2026-10-21 13:30Z (nearest 13:31Z).
- 2026-11-02 open at 14:30Z is correct after DST ended.
- Endpoint uses configured feed and may fall back to stored observations; require response `feed=sip` before interpreting a capture as SIP. Keep histories separate.
- User reports 171 tests pass with the early-close test commented out. This is not a passing full suite; rerun with the xUnit fix and retain the test.

---

# Handoff update — 2026-11-01 prototype header alignment

- Moved `#system-readiness` inside the same `.topbar-brand` container as the title and topbar tools in `frontend/prototypeIndex.html`; it now occupies the right-side flex area. `#marketSessionClock` remains above the System Readiness heading.
- Removed three literal escaped-newline sequences from the readiness markup. Static IDs were preserved.
- Production `frontend/index.html` was not changed. Browser acceptance at desktop/mobile widths is still required; do not promote until explicitly approved.

---

# Handoff update — 2026-11-01 Red Zone design and RTO provider blockers

## Repository checkpoint
- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Branch: `feature/opportunity-quality-classification`
- Production dashboard: `frontend/index.html`; experimental UI: `frontend/prototypeIndex.html`. Keep UI experiments in the prototype until explicit promotion approval.
- README, Master To-Do, and this handoff were reviewed against the feature branch. The Master To-Do is the canonical active checklist; dated sections below it are historical evidence, not separate duplicate tasks.

## Red Zone — verified current behavior vs. requested target
- Current code in `frontend/app.js` sorts live-discovery rows by absolute daily percentage change; non-live groups sort by their supplied score. Manually pinned symbols are placed ahead of scanner-ranked rows.
- There is currently no Manual/Automatic toggle. Sound alerts are toggleable and fire on a newly appearing symbol among the four visible slots.
- Each tile separately fetches the live assessment and displays the 5M score/permission, 1M execution state, extension, freshness, decision reason and a 1M sparkline.
- The current rank does not include RVOL/volume participation, average-volume/liquidity, acceleration or spread quality. A separate per-card 5M same-slot RVOL diagnostic has now been added, but ranking is unchanged. It is therefore not the requested excitement rank. The 5M same-slot RVOL line exposes relative participation, but validated average daily volume, spread/liquidity, and broader participation quality remain unavailable to the rank.
- Required next work is captured in the new Red Zone section at the top of `docs/Master-To-Do.md`: manual/automatic mode, versioned excitement rank, volume/liquidity participation, 5M+1M evidence, per-tile Decision Explanation, restrained semantic border/accent, stable re-ranking, and acceptance tests.
- Design rule: rank attention, not authorization. Keep 5M Permission, 1M EXECUTION READY, and Red Zone excitement as three distinct concepts. A low-volume EXECUTION READY stock must be explicitly labelled and penalized, not silently promoted as a strong opportunity.
- Visual recommendation from UI/accessibility review: use a restrained 3–4px left-edge accent (not a full-card color fill), paired with a concise status tag and reason text. Use consistent semantic colors; amber for low participation/extension caution, red only for hard block/confirmed invalidation, neutral for DATA?/unknown. WCAG 2.2 SC 1.4.1 prohibits color as the only status cue.

## RTO IEX boundary investigation — current disposition
- User-supplied RTO capture: 4,048 1m bars fetched, 0 new writes, 18/20 valid close/open pairs.
- The exact-boundary audit shows 38/40 boundaries present. The two missing next-session opening bars are 2026-10-14 13:30Z and 2026-10-21 13:30Z (09:30 Eastern); nearest returned provider timestamps are 13:34Z and 13:31Z respectively. Store and provider agree, so the observed issue is absence from the returned IEX response, not a persistence loss.
- I reviewed the current repository README, capture checklist, and runbook. They document IEX as single-exchange coverage and SIP as consolidated coverage subject to plan access. No SIP capture payload, independent alternative-provider response, entitlement confirmation, Alpaca account-plan response, or credentials are present in the reviewed repository evidence.
- Therefore the independent SIP/alternative-provider comparison and account entitlement check are **NOT COMPLETED**. Data not found in sources. Do not mark either item done, do not infer that the account lacks SIP access, and do not fill either missing minute with a neighboring bar or another feed.
- The environment available for this review cannot execute the user's localhost API with their private Alpaca credentials. The README explicitly describes the real-provider smoke test as a manual local operation; credentials must remain on the user's machine and must never be copied into Git/chat.
- The existing boundary audit is useful: it distinguishes provider-response absence from store mismatch and correctly withholds rolling metrics when fewer than 20 exact pairs exist.
- The capture endpoint uses the configured feed, not a request-body feed override. Current tracked config is IEX. The exact local SIP comparison procedure is now documented in `docs/Runtime-Acceptance-Runbook.md` section 1B: temporarily set `MarketData:Alpaca:Feed=sip`, restart, capture the same 20 pairs, verify the response's `feed` is actually `sip`, and compare all exact boundaries. If the provider returns no SIP bars, the endpoint may fall back to a single stored feed; a response reporting `feed=iex` is not SIP evidence.

## Boring Overnight — three remaining launch gates
1. Compare the two exact RTO IEX gaps against SIP or a validated independent provider, using the same symbol, exact UTC boundaries, and separate feed-labelled results.
2. Confirm the actual Alpaca plan/feed entitlement and returned 1-minute boundary coverage. A configured feed name alone is not proof of account entitlement or complete coverage.
3. Connect source-validated catalyst status, daily SMA20/average volume, the canonical exclusions source, and current authoritative 5M/1M evidence before any READY evaluation. Missing/unknown evidence remains DATA?/WATCH; no forced READY.

The current Boring Overnight implementation is a 20-session regular close/open capture and deterministic evaluation slice, not a complete autonomous qualification pipeline. It does not establish the full GTC_EXT 07:00–20:00 ET window, catalyst clearance, validated daily metrics, full exclusion reconciliation, or Thinkorswim state parity. Continue manual WATCH/research only.

## Exact-head verification
- The branch README reports prior workflow 864 passing for the capture implementation commit, but later commits exist. The exact current branch-head CI must be checked before relying on the branch as release-ready.
- Browser acceptance, local API/feed entitlement, and live market-hours WebSocket delivery are separate from unit-test success.
- For Monday 2026-11-02, complete the documented 07:30 Arizona / 09:30 Eastern runtime acceptance for SPY, RKLX, ASTX and XRPN. Verify per-symbol completed-bar count and lastBarUtc advance; subscription acknowledgement alone is insufficient.

## Active checklist count
The Master To-Do currently tracks 35 unchecked operational items: 3 Boring Overnight, 7 P0 (including prototype header browser acceptance), 17 P1 (including the 8 Red Zone requirements), 4 P2, and 4 P3. Use its top consolidated checklist; do not count repeated historical entries as additional tasks.

---

# Handoff update — 2026-11-01 prototype UI, RTO diagnosis, and calendar correction

## Source-control state
- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Branch: `feature/opportunity-quality-classification`
- Current handoff includes a separate `frontend/prototypeIndex.html`; production `frontend/index.html` remains unchanged.
- UI work in this pass: removed the prototype's RESEARCH WORKBENCH eyebrow; moved the market-session clock above System Readiness at the top-right; moved the transport/demo indicator into System Readiness; added a fixed left dock with horizontal symbol input and vertical Intraday/Boring Overnight toggle; synchronized the toggle with Reset.
- Shared `frontend/app.js` now supports focusing the symbol from a Red Zone tile whether the production control is a SELECT or the prototype control is an INPUT.
- Prototype markup audit: 632 opening elements, all with IDs; no duplicate IDs. Production markup and shared stylesheet were not modified.
- Compare `/` (production) with `/prototypeIndex.html` (prototype). Future UI changes go to prototype only; promotion requires explicit user approval.
- The calendar ruleset is now `MCC_US_EQUITIES_CALENDAR_V2`. It corrects the false 2027-12-31 New Year's closure and includes published NYSE 2026–2028 holiday/early-close regression fixtures.
- Exact latest-head CI status is unavailable: GitHub reports no status checks or workflow runs for the latest documentation commit. The .NET SDK is not available in this execution environment, so do not claim this exact head has passed build/tests.
- The canonical Master To-Do has 35 unchecked operational items: 3 Boring Overnight, 7 P0, 17 P1, 4 P2, and 4 P3.

## Production/prototype promotion contract
- Production remains `frontend/index.html`; prototype is `frontend/prototypeIndex.html`.
- Prototype-only inline CSS controls the experimental layout. The prototype uses the same API/backend as production.
- User approval phrase such as "Make it official" is required before moving approved UI changes into production.
- See `docs/Prototype-Workflow.md`.

## Scanner workflow clarification
- Refresh Alpaca market movers loads persisted Alpaca SIP-based discovery events. It is not a complete-market scan.
- Selecting the live discovery group renders candidates into the scanner and Red Zone preview (up to four).
- `Watch` adds to browser-local `mcc.watchlist`; it does not pin to Red Zone.
- Pinning to Red Zone is a separate action using Add a symbol / Pin to Red Zone.
- A Watchlist entry or Red Zone rank does not create 5M Permission, 1M execution confirmation, or entry authorization.

## Symbol control
- Production `#symbol` starts at SPY from the selected HTML option. Red Zone card focus changes it to the selected ticker. Reset changes workspace mode only; it does not clear the symbol.
- Prototype `#symbol` is a text input initialized to SPY and supports manual editing.

## Installed Thinkorswim source inventory
- 5M Command Center V4.1.7: `docs/Validation/Installed-5M-Study.txt`, blob SHA `29e2ccf9c2896f9d4999bae9bcaff72503b19a00`.
- 5M Authority Diagnostic V2: `docs/Validation/Installed-5M-Authority-Diagnostic.txt`, blob SHA `f2c57603b332b94283a297d647cf3fc2021fcb00`.
- 1M Command Center V14.4.10: `docs/Validation/Installed-1M-Study.txt`, blob SHA `2553bcb4c4d5352316cbec3a884f7b8c2eafb65f`.
- Study identity/source inventory is complete. Only a matched-bar output parity replay remains if not already documented elsewhere.

## RTO capture diagnosis
User supplied RTO/Alpaca/IEX response:
- 4,048 bars fetched; 0 newly persisted.
- 18/20 valid close/open pairs.
- Missing next-session opening bars: 2026-10-14T13:30:00Z and 2026-10-21T13:30:00Z.
- Metrics withheld correctly; no win rate or return means should be inferred.

The supplied capture predates the new `boundaryCoverageAudit` field. Re-run after pulling. For each expected boundary, the response will report exact provider-response presence, exact store presence, nearest provider timestamp and offset, and a diagnosis status. This isolates source-feed absence from persistence/query mismatch. Do not substitute neighboring timestamps or mix IEX and SIP within one return series.

## Provider coverage audit terminology
The existing Overnight panel previously titled PROVIDER COVERAGE AUDIT was a selected-ticker-vs-SPY post-market control test over one shared time window. It is not a comprehensive provider coverage audit. It is now labelled accurately. Exact close/open boundary coverage is audited from the 20-session capture result; feed entitlement and IEX-vs-SIP completeness remain external/provider qualification tasks.

## Dashboard header
- Header now displays a server-backed countdown to regular close, 8:00 PM Eastern after-hours end, or next regular open.
- Timezone: America/New_York; calendar: MCC_US_EQUITIES_CALENDAR_V1.
- Workspace toolbar is inside `.topbar-brand`, below title/tools.
- Static `frontend/index.html` elements lacking IDs were assigned stable unique IDs. Existing IDs were preserved.

## Strategy rules
Do not change the current provisional Boring Overnight thresholds yet. An incomplete 18/20 RTO sample cannot support tuning. Wait for complete source-backed history, corporate-action handling, broader samples, full GTC_EXT outcome data, catalyst coverage, and walk-forward validation net of costs. The current thresholds remain the baseline pending evidence.

## Thinkorswim study parity
The 5M/1M authority architecture and source/version development are established. Exact installed 5M study/hash/input parity has not been certified in the current repository evidence. Obtain one matched-bar certification; reuse it until study/hash/inputs or relevant signal logic changes.

## Tomorrow's WebSocket acceptance — Monday 2026-11-02
- Run the local API with streaming enabled and symbols SPY, RKLX, ASTX, XRPN.
- Start the capture at 07:30 Arizona (09:30 Eastern); observe at least five minutes.
- Run the PowerShell command in `docs/Runtime-Acceptance-Runbook.md` using `http://localhost:53360` (or matching HTTPS URL/port).
- Accept only if each eligible symbol's barCount and lastBarUtc advance during regular hours, and receivedBars/persistedBars increase. Subscription acknowledgement alone is insufficient.
- Save the full output directory and record branch SHA, API SHA, feed, and exact test window.
- Verify eligible 1M assessment sourceTransport separately; 5M Permission remains REST.

## Current operating model
For now, run MCC locally (normally start the API from Visual Studio with F5) and use the localhost browser page served by that API. Desktop packaging/installer, automatic startup, updater, and production hosting remain unimplemented decisions. No standalone .exe is currently delivered.


# Handoff update — 20-session capture build validation — 2026-10-31

- Source implementation commit: `a0bf119c68f688a017e179e28a0bf145727f734f`.
- GitHub Actions run **864** passed: .NET Release build succeeded with 0 warnings/0 errors; **165/165 tests passed**.
- This validates compilation and unit tests for the new capture endpoint, calendar rules, and UI code. It does not validate live Alpaca entitlement/coverage, official exchange-calendar parity, or browser operation in the user's local MCC.
- Dashboard action: **Refresh/Capture 20-session close/open calculation**. API: `POST /api/boring-overnight/capture-history`.
- Current source limitation: calendar is MCC-maintained `MCC_US_EQUITIES_CALENDAR_V1`, not a live exchange calendar. Check generated boundaries against official NYSE calendar for the actual capture window.
- Exact session boundary bars are queried pair-by-pair from the persisted store to avoid the store's 10,000-row result cap truncating dense extended-hours history. Feed provenance is inferred from refreshed provider bars or a single unambiguous stored feed; mixed-feed data is rejected.
- Next user acceptance: pull branch, hard-refresh, enter candidate symbol, click capture, then review the 20 pair rows, exact UTC timestamps, provider/feed and missing pairs. Missing boundary bars mean rolling metrics stay DATA?.
- This does not collect the GTC_EXT 07:00–20:00 ET sell window, compute Calculated Limit Sell, clear catalysts, or qualify READY.




# Current handoff update — one-click 20-session Overnight history — 2026-10-31

- Added `UsEquitiesSessionCalendar` with `MCC_US_EQUITIES_CALENDAR_V1`, using America/New_York for DST and explicit US-equities holiday/common early-close rules.
- Added `POST /api/boring-overnight/capture-history`: fetches 1m bars through configured provider, chunks provider requests, persists raw bars, reads back stored observations, and calculates 20 exact prior-RTH-close to next-RTH-open pairs.
- Dashboard now has **Refresh/Capture 20-session close/open calculation** for the current Overnight Stock Candidate. It displays 20-pair/valid count, win rate, mean/median, 5-pair mean, missing pairs, feed, fetched/stored counts, calendar version and pair timestamps.
- Calendar tests added: 20 pairs, weekends, DST transition, Thanksgiving, Good Friday and Christmas Eve. Tests/Release build not yet run on exact branch head.
- Calendar rules are MCC-maintained, not live exchange calendar. Verify captured dates against official NYSE calendar before treating automatic boundary generation as production-certified.
- This is only regular close/open history. It does not collect the separate 07:00–20:00 ET GTC_EXT sell window, calculate a sell target, clear catalysts, or qualify READY.
- Canonical current task list is at the top of `docs/Master-To-Do.md`; historical dated sections remain below for evidence.


# New Chat Handoff — Monday readiness / independent 5M score — 2026-10-31

## Current operating disposition

- **Red Zone / V2 5M score:** source implementation now computes the six-factor 5M score independently of 1M execution availability. Ready for Monday live observation only after exact-head CI and runtime checks.
- **1M execution:** separate gate; must not be inferred from 5M score. WebSocket is used only when its subscription/history/freshness/provenance gates pass; REST fallback remains valid.
- **Boring Overnight:** NOT READY for actionable/automated READY. Manual WATCH/research only until historical session pairs, daily metrics, catalyst coverage, exclusions, session policy, and full GTC_EXT coverage are validated.

## Saturday user-supplied evidence

- Market-data validation command ran against `http://localhost:53360` for RKLX / Alpaca / IEX / 2026-10-30 13:00Z–19:35Z.
- Output: 25 stored 1M observations, 23 stored 5M; automated idempotency result 1M=True, 5M=True.
- User artifact folder: `artifacts/runtime-validation/20261031T202332Z`.
- Checkpoint baseline: `artifacts/collector-checkpoint/20261031T202543Z`.
- Live checkpoint path: `C:\Users\jampo\AppData\Local\MomentumCommandCenterV2\collector-watermarks.json`.
- Checkpoint SHA-256: `8E3AFBD5DAD5C3502DB625F0133DE697F9C8615BB35BDA09609A4CF7FAE44500`.
- Capture reported 3 cycles, 0 last-cycle errors. This does not prove failure handling, restart reload, or sleep/wake.
- WebSocket: SPY/RKLX/ASTX/XRPN acknowledged before/after, but barCount and newBarsObserved were zero. The run occurred Saturday, so regular-session delivery remains untested.
- User confirms all MCC application JavaScript errors are gone except Visual Studio Browser Link `$.noConflict is not a function`; disable Browser Link for the debugging session if it persists.

## Source changes made after the previous handoff

### Independent Red Zone 5M score

The old API returned `fiveMinute.scoreBreakdown.score=decision?.Score`, coupling the 5M score to the combined 1M/5M decision. A missing 1M decision therefore hid the 5M authority score.

Changes:
- `src/MomentumCommandCenterV2.Core/V2SignalEngine.cs`: added canonical `CalculateFiveMinuteScore(BarSnapshot, V2Config?)`; existing engine uses the same function.
- `src/MomentumCommandCenterV2.API/MarketData/LiveSymbolAssessmentService.cs`: independently calculates current 5M score only when 5M technical history, freshness (<=360s), and same-slot RVOL are available. Emits `score`, `permission`, and `status=CURRENT|UNKNOWN`.
- `frontend/app.js`: main Decision Explanation and Red Zone cards read `fiveMinute.scoreBreakdown`. 5+ is 5M Permission only. EXECUTION READY still requires `decision.entryAllowed=true`.
- `tests/MomentumCommandCenterV2.Tests/V2SignalEngineTests.cs`: added 6/6, 5/6, and 4/6 score assertions.

**Important:** exact-head CI and live runtime acceptance are not yet confirmed for these latest changes. Check branch HEAD and GitHub Actions before telling the user this build is verified.

## Monday clock conversion — 2026-11-02

- Phoenix remains UTC−07:00; Eastern returns to UTC−05:00 after the November 1 DST end.
- Phoenix is 2 hours behind Eastern.
- NYSE/Nasdaq regular open/close map to 07:30 / 14:00 Arizona.
- User's 08:30 Arizona start maps to 10:30 Eastern.
- Project GTC_EXT window 07:00–20:00 Eastern maps to 05:00–18:00 Arizona.
- Use `America/Phoenix` and `America/New_York`, not hard-coded UTC offsets for date-sensitive calculations.

## Boring Overnight blocker audit

Read:
- `docs/BoringOvernight.md`
- `src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightEvaluator.cs`
- `src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightHistoryCalculator.cs`
- `src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightCandidateSnapshotStore.cs`
- Boring Overnight routes in `src/MomentumCommandCenterV2.API/Program.cs`.

Findings:
- History calculator requires explicit close/open bar timestamps; no automatic exchange-calendar policy.
- It emits rolling metrics only when every requested pair exists and the required count is met.
- `/api/boring-overnight/evaluate-with-history` deliberately forces `CatalystStatus=UNKNOWN` until a validated catalyst provider exists.
- Caller-supplied non-history fields are not independently verified.
- GTC_EXT full-window target touch/fill research is distinct from regular close-to-next-open return.
- No current 20-session accepted candidate dataset or catalyst-clear evidence exists in the reviewed sources.

A Monday readiness runbook was created at `docs/Monday-Readiness-2026-11-02.md`; `docs/BoringOvernight.md` and Master To-Do were updated. Do not promote Boring Overnight to READY.

## Immediate next actions

1. Check exact current branch HEAD and CI.
2. Pull branch and hard-refresh MCC.
3. After 07:30 Arizona Monday, capture live assessments for SPY, RKLX, ASTX, XRPN. Confirm 5M score current and criteria populated; independently inspect 1M status/entryAllowed.
4. Run a 5–10 minute WebSocket before/after capture during regular market hours. Acknowledgement alone is not delivery.
5. Run collector restart/reload and safe isolated failure/recovery tests; sleep/wake remains separate.
6. Continue Boring Overnight as WATCH/manual research only until every launch gate in the Monday runbook passes.
7. Update Master To-Do and handoff with exact commit SHA, CI run, runtime timestamps, and artifact paths.

# New Chat Handoff — runtime fixes and collector acceptance — 2026-10-31

## Current source-control checkpoint

- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Branch: `feature/opportunity-quality-classification`
- This handoff was updated after the JavaScript subscription initialization fix, market-data script port/preflight fix, collector checkpoint-load diagnostics, and collector evidence capture/runbook changes.
- Verify the current branch HEAD and its GitHub Actions workflow before treating this checkpoint as build-verified. Do not rely on an older green workflow for these newest source changes.

## User-reported failures and source diagnosis

### PowerShell connection reset

The local Development profile in `src/MomentumCommandCenterV2.API/Properties/launchSettings.json` declares:
- HTTPS: `https://localhost:53359`
- HTTP: `http://localhost:53360`

The user supplied `http://localhost:53359`, which is the wrong protocol for that port. Use `http://localhost:53360` or `https://localhost:53359`. The validation script now preflights `/api/health` and explicitly rejects this known mismatch.

### JavaScript initialization error

`frontend/app.js` called `refreshFocusedPriceChart()` during startup. That function calls `ensureMinuteBarSubscription()`, which accessed `requestedMinuteBarSymbols` before its later `const` declaration. The declaration has been moved next to the initial module constants, before any startup invocation. The source fix is committed; a real browser reload is still required to verify.

### Visual Studio Browser Link error

`browserLink:33: $.noConflict is not a function` comes from the Visual Studio development-time Browser Link injection, not from the MCC frontend source. If it persists after the MCC JavaScript fix, disable Browser Link for the debugging session or run without Browser Link. Do not alter MCC market-data code to suppress this unrelated tooling error.

## RKLX uploaded evidence

- `data/validation/RKLX/MarketData_StoredBars.txt`: 13 current 1M RKLX/Alpaca/IEX observations, 2026-10-30 13:30Z–17:30Z.
- `data/validation/RKLX/MarketData_StoredBars_5m.txt`: 12 current 5M RKLX/Alpaca/IEX observations, same window.
- These rows prove stored membership for that window. They do not cover the distinct 19:00Z–19:35Z interval where four 1M/four 5M observations were previously reported. Re-query that exact interval through the running API before closing the remaining verification.
- The `supersedesObservationId` fields and latest-version store semantics support version replacement history, but do not alone prove that the later four-bar API response was identical to the stored payload. Compare timestamp, provider, feed, timeframe, OHLCV/content hash before classifying its zero-new-write count as idempotent.

## Collector status interpretation and new diagnostics

The user's sample showed:
- configured symbols: SPY, QQQ, IWM, RTO, AMT;
- watermarks also included TTE and FMS, which are retained old keys from prior configuration and are not active current configured symbols;
- `cycles=6`, `lastCycleErrors=0`, `recentErrors=[]`;
- `newlyPersistedObservations=0`, a cumulative per-process count;
- Saturday poll cycles and Friday source watermarks, which are not automatically a failure because the worker polls on weekends and empty successful responses do not advance source timestamps.

The worker now exposes:
- `checkpointLoadCompleted`
- `checkpointFileFound`
- `checkpointLoadedEntryCount`
- `checkpointLoadCompletedUtc`
- `checkpointLoadError`

These let the restart test directly establish whether the configured checkpoint was found and how many entries were loaded.

## New scripts and runbook

- `scripts/validation/Test-MarketDataRuntime.ps1`: checks API health and validates the known local port/scheme mismatch. It also re-requests provider 1M/5M bars and compares timestamps, OHLCV, provider, feed, timeframe, and transport against the latest stored rows. The script determines whether `newlyPersistedObservationCount=0` is consistent with idempotent duplicates; the user does not need to calculate this manually.
- `scripts/validation/Capture-CollectorCheckpoint.ps1`: captures `/api/market-data/collector/status`, copies the configured checkpoint, and records SHA-256, file size, last-write time, watermarks, and checkpoint-load diagnostics.
- `docs/Runtime-Acceptance-Runbook.md`: section 4 now defines each collector status field and provides precise cycle-failure, restart/reload, and sleep/wake acceptance procedures.

## Immediate continuation checklist

1. Check the exact latest branch HEAD and GitHub Actions.
2. Pull the branch in Visual Studio, run MCC, and hard-refresh the browser. Verify the `requestedMinuteBarSymbols` ReferenceError is gone.
3. Run the revised market-data script. Its `stored-bars.json` includes the automated idempotency result, and `provider-refresh.json` preserves the fresh provider responses:
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
4. Run `./scripts/validation/Capture-CollectorCheckpoint.ps1 -BaseUri "http://localhost:53360"`.
5. Complete the target-host failure/recovery, restart/checkpoint reload, and sleep/wake tests exactly as documented. Do not induce provider failure in a production account.
6. During regular market hours, perform the separate 5-minute WebSocket test for SPY, RKLX, ASTX, XRPN.
7. Update Master To-Do and this handoff with exact runtime artifact paths/timestamps and the exact commit/workflow SHA. Keep all items unchecked until evidence exists.

# Current continuation handoff — ChatDisconnect execution checkpoint — 2026-10-31

## Repository state

- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Branch: `feature/opportunity-quality-classification`
- Parent source checkpoint before this pass: `46eec0457bd68324a1421b8f5479a43a7d2052b2`
- Parent GitHub Actions run **776** passed for `46eec0457bd68324a1421b8f5479a43a7d2052b2`.
- Morning Brief snapshot and source-watermark implementation commit `61e8483c6cce4cea5e5131e773086db1ff91bd74` passed workflow **792**: Release build 0 warnings/0 errors; **156/156 tests passed**.
- Runtime validation script commit `fd6fe0b66b9ad9be5fd1ec31fdf26780d2eb4534` passed workflow **780**. Documentation workflows **781–785** passed.
- The exact branch head `ecd31a28248a3599a18c7a9a4080113c3b1f3e92` passed GitHub Actions run **803**: .NET 8 Release build and **156/156 tests**. This includes the README, User Manual, runbook, Master To-Do, and source-watermark changes. This handoff update itself creates a newer commit; verify its workflow before treating the post-handoff head as green.
- Read `docs/ChatDisconnect.txt` and reconciled its remaining-work checklist into the top of `docs/Master-To-Do.md`. The Master To-Do remains canonical.

## Work completed in this pass

### Morning Brief persistence/versioning

- Added `src/MomentumCommandCenterV2.API/MorningBrief/MorningBriefSnapshotStore.cs`.
- Newly generated `GET /api/morning-brief` reports are appended to an immutable JSONL store before entering the existing 60-second response cache.
- Each record includes snapshot ID, generated/recorded UTC timestamps, report version (`morning-brief-v1`), SHA-256 payload hash, and the complete report JSON.
- Snapshot metadata also captures explicit UTC source-bar watermarks from `marketLandscape`, keyed by provider/symbol/timeframe/feed/transport. Raw input references, news/discovery watermarks, full dependency manifest, and outcome linkage remain open.
- Live response adds `snapshotPersistenceStatus`, `snapshotId`, and `snapshotVersion`. Storage failures are logged and surfaced as `FAILED` without silently claiming persistence.
- Added `GET /api/morning-brief/history?limit=20` (limit 1–200), returning newest-first report snapshots.
- Default path is local application data under `MomentumCommandCenterV2/morning-brief-snapshots.jsonl`; override with server-side `MCC_MORNING_BRIEF_SNAPSHOT_PATH`.
- Added `MorningBriefSnapshotStoreTests.cs`: round-trip/provenance/hash, append-only preservation, UTC/missing timestamp validation, and limit validation.
- Remaining: exact-head CI; per-source input watermarks and input manifest; raw provider payload retention policy; outcome linkage. This is report snapshot history, not a complete research evidence archive.

### Runtime acceptance aid

- Added `scripts/validation/Test-MarketDataRuntime.ps1`.
- It queries stored RKLX 1M/5M bars, filters the returned observations locally by provider/feed, captures stream/collector/provider status before and after a configurable wait, records per-symbol bar-count deltas, and saves full live assessments for SPY/RKLX/ASTX/XRPN.
- Output files: `stored-bars.json`, `runtime-before.json`, `runtime-after.json`, `stream-delta.json`, and `live-assessments.json`.
- Added `docs/Runtime-Acceptance-Runbook.md` and updated the Morning Brief technical guide and backlog.

## Exact outstanding checklist

### P0 — user-runtime acceptance

1. Run the PowerShell script against the running API with a same-window RKLX UTC range; confirm whether the four 1M and four 5M observations are already persisted for Alpaca/IEX.
2. Run it during regular market hours for 300 seconds with stream enabled. Verify SPY/RKLX/ASTX/XRPN subscription acknowledgement and advancing per-symbol bar counts/timestamps.
3. Confirm live assessment reports the WebSocket source only when the 50-completed-bar, freshness, and provenance gates all pass. 5M remains REST.
4. Browser-test the revised header, 2x2 chart metrics, Morning Brief modal/section and history, Arizona-local replay, Red Zone responsive layout, and opt-in sound.
5. Verify Release build/tests on the exact current branch head.

### P1 — blocked by missing evidence or policy

- Thinkorswim state replay requires same-bar 1M/5M diagnostic exports with installed study version/settings and explicit state/reason outputs. RTO OHLCV alone is insufficient.
- Collector outage/restart/sleep-wake tests require controlled execution on the target host. Existing source has durable watermarks, overlap/backfill, retry, and atomic checkpoint replacement.
- Automated Overnight boundary generation requires the user's explicit eligible-session policy (RTH/ETH, close/open definition, holiday/early-close treatment). Current history calculator uses explicit timestamps and returns incomplete history when pairs are absent.
- Complete Morning Brief input-watermark/version lineage remains open after report snapshots.

### P2 — external dependencies / product decisions

- Scanner provider entitlement, coverage, and licensing require account-side verification.
- Schwab read-only sync requires approved Individual API access, app registration, callback URL, and secure server-side OAuth configuration. No secrets in Git/chat; no order submission.
- Desktop packaging needs a target platform and install/update/API lifecycle decision before implementation.

## Key architecture and safety constraints

- 5M owns Permission; 1M owns Execution. Do not invert this dependency.
- Boring Overnight remains a separate pipeline.
- Missing/stale/conflicting evidence remains UNKNOWN / DATA? / BLOCKED; never synthesize bars or assume catalyst CLEAR.
- The existing frontend is static HTML/CSS/JS hosted by ASP.NET Core Minimal API and visible under Visual Studio Solution Items; do not migrate to MVC without a concrete requirement.
- No order submission or autonomous trading.

## Continue here

1. Check GitHub Actions for the exact latest head; run failures must be fixed before claiming verification.
2. If CI passes, deploy/pull that exact head and run `scripts/validation/Test-MarketDataRuntime.ps1` during the next regular market session.
3. Capture browser acceptance and Thinkorswim state diagnostics as separate evidence artifacts.
4. Update this handoff and the canonical Master To-Do with exact SHA, workflow run, runtime timestamps, and artifacts—not estimates.

Read `docs/Runtime-Acceptance-Runbook.md` before running the acceptance steps.

---

## Current continuation checkpoint — RKLX post-close assessment — 2026-10-30

### Source-control checkpoint
- Repository: `themailcomeshere/MomentumCommandCenterV2`
- Branch: `feature/opportunity-quality-classification`
- Latest documentation checkpoint before this handoff refresh: `1da15c5b45531deec1bc53efaad6729f3647a30a`; the current code/solution checkpoint remains `3192c8f34d11273360bed82367563466a30b08ad`.
- The latest code/solution checkpoint is `3192c8f34d11273360bed82367563466a30b08ad`; GitHub Actions run 767 passed the .NET 8 build and 152/152 tests. Commits after that code checkpoint were documentation updates.
- The full-width header, corrected status-chip overlap, enlarged focused symbol, chart-side 2×2 Opportunity Status grid, and Visual Studio frontend Solution Items are already implemented. Do not reimplement them without new evidence.

### Latest RKLX evidence
- Direct Alpaca/IEX 1M and 5M queries for 19:00–19:35 UTC returned four bars each. The 1M timestamps were 19:00, 19:08, 19:25, 19:35; 5M timestamps were 19:00, 19:05, 19:25, 19:35.
- The 22:55 UTC assessment was post-close. Its last completed 1M/5M bars ended at 20:00 UTC; both were correctly stale. Permission and execution were not evaluated.
- The 22:50 UTC stream snapshot was connected/subscribed but had no received or persisted bars. Because this was after the regular-session close, it is not a valid stream-delivery failure test.
- `lastMessageUtc` includes control/auth/subscription messages; use `lastBarUtc`, `barsBySymbol`, `receivedBars`, and `persistedBars` to evaluate actual bar delivery.
- `newlyPersistedObservationCount=0` is consistent with deduplication but does not prove persisted-store contents. Verify via `GET /api/market-data/stored-bars`.

### Immediate next actions
1. Verify the four RKLX 1M and four 5M observations in the stored-bars endpoint using the exact provider/feed/timeframe/window.
2. During the next regular session, run the 5–10 minute WebSocket before/after test for SPY, RKLX, ASTX, and XRPN.
3. Confirm the live assessment selects WebSocket 1M only after acknowledged subscription, at least 50 completed bars, freshness, and WebSocket provenance. Keep 5M Permission REST-backed.
4. Browser acceptance remains separate: revised header, 2×2 chart-side metrics, Morning Brief modal, replay, Red Zone responsive layout, and sound permission.
5. Schwab Trader API approval/OAuth remains external and pending. No order submission.

### Frontend architecture
The UI is static HTML/CSS/JavaScript hosted by the ASP.NET Core Minimal API. It is not MVC/Razor. The editable files are visible in Visual Studio under the `frontend` Solution Items folder. MVC migration is not required to edit or maintain `index.html`; consider it only if a concrete server-rendered/controller requirement emerges.

Read the top of `docs/Master-To-Do.md` and the latest section of `docs/MarketData-ChatTests-20261030.md` before continuing.

---

## Final implementation validation — 2026-10-30

- Branch: `feature/opportunity-quality-classification`
- Code validation: GitHub Actions run **742** passed the .NET 8 build and **152/152 tests**, including the dynamic WebSocket subscription cap and stream persistence changes.
- Related runs **739–741** passed, covering the full-width Red Zone layout, replay/local-time UI, and Morning Brief API caching.
- Documentation/cache follow-up runs **743–746** passed.
- Frontend `app.js` syntax check: PASS. Duplicate HTML IDs: none.
- Browser acceptance is still outstanding: actual audio permission, modal/accordion interaction, local replay form, live symbol subscription acknowledgement, and per-symbol bar advancement must be checked in the user's running MCC instance.
- Live provider acceptance is still outstanding: all uploaded ChatTests were captured after market close; separate in-session RKLX 5M stale condition needs a fresh same-session response. Do not change freshness thresholds to conceal it.
- Schwab Trader API Individual approval remains pending. No Schwab credentials or tokens were requested or stored. Order execution remains disabled.

## MCC update — 2026-10-30 (user review follow-up)

### Completed in this iteration

- [x] Investigated all four `data/ChatTests/MarketData_*.json` files. SPY, RKLX, ASTX, and XRPN all returned `STALE_MARKET_DATA`; all four reported REST as the 1M source. Their evaluation timestamps were 20:08–20:12 UTC, after the 20:00 UTC / 1:00 PM Arizona regular-session close.
- [x] Recorded the separate RKLX in-session observation: 5M bar age 982s (360s limit), source 19:05 UTC / expected close 19:10 UTC, observed around 19:26 UTC. Exact provider/feed cause remains unresolved.
- [x] Added dynamic Alpaca subscription endpoint `POST /api/market-data/stream/subscribe?symbol=XYZ`. MCC requests subscriptions for the selected symbol and Red Zone cards; `StreamSymbols` is now only the initial server-side seed.
- [x] Dynamic symbols remain in API memory and are automatically re-subscribed after WebSocket reconnect. Default cap is 30 symbols (`MarketData:Alpaca:StreamMaxSymbols` is configurable); symbols are not persisted across API restarts, and the front end re-requests the currently selected and visible Red Zone symbols.
- [x] Stream status now reports acknowledged symbols, per-symbol bar counts/last timestamps, and separate received vs persisted totals.
- [x] Morning Brief modal now opens immediately using the existing rendered report; it no longer waits for a second full API call on every open. The API and browser each cache the report for 60 seconds; the modal opens immediately and refreshes stale data in the background.
- [x] Removed the duplicate Morning Command Brief heading from the modal body.
- [x] Moved Opportunity Classification, Last Price, RVOL, and ATR Extension into the Main Chart heading on the right; constrained the chart width.
- [x] Red Zone occupies the full available row and uses a four-column desktop grid with responsive 2-column/tablet and 1-column/mobile layouts.
- [x] Timeframe Handoff now explains how to use 5M permission vs 1M execution and how often to review each.
- [x] Historical replay now accepts Arizona-local date/time and converts it to UTC in the browser. Missing replay query parameters now return HTTP 400 with a clear explanation instead of an unhandled 500.
- [x] Added opt-in two-tone sound alert when a new ticker enters the four visible Red Zone slots. Sound must be enabled by clicking the control; live Alpaca discovery refreshes every 30 seconds while the Live Discovery group is selected.
- [x] Live discovery candidates now sort by absolute provider-reported percentage move, not alphabetically because a formatted percentage string was not numeric.

### Still open / acceptance

- [ ] Deploy and verify dynamic subscriptions in a running MCC API during regular market hours.
- [ ] Verify `barsBySymbol` advances for SPY, RKLX, ASTX, and XRPN and that eligible 1M assessments switch to `ALPACA_WEBSOCKET_LATEST_BAR`.
- [ ] Diagnose the separate in-session RKLX 5M stale condition with same-time provider bar evidence. Do not lower freshness limits to mask it.
- [ ] Verify Morning Brief initial-load latency and modal behavior in browser.
- [ ] Verify Arizona-local replay form and API HTTP 400 behavior.
- [ ] Verify Red Zone 1x4 layout and browser sound permissions.
- [ ] Schwab Trader API Individual remains pending approval; OAuth and read-only position sync are blocked until the user completes app registration and securely configures server-side credentials.
- [ ] Order submission remains intentionally disabled.

### Evidence document

See [MarketData-ChatTests-20261030.md](MarketData-ChatTests-20261030.md) for per-symbol timestamps, freshness ages, stream status, and runtime acceptance steps.

## Latest verified checkpoint — 2026-10-30

- Branch: `feature/opportunity-quality-classification`
- Verified branch head at checkpoint: `0d669ad888400682575b7d8bd895f7ff55b2436d`
- GitHub Actions run **707** passed Release build and **152/152 tests** after correcting the transport-provenance test fixture.
- Runs **708–714** completed successfully, including hybrid transport UI/header, documentation, and latest cache-bust updates.
- Frontend JavaScript syntax parsed successfully after Morning Brief, replay, and hybrid-source UI changes.
- Browser acceptance remains with the user: modal/accordion behavior, actual symbol freshness JSON, replay against populated stored history, and live WebSocket subscription/data flow.
- Historical per-symbol stale causes for RKLX, ASTX, and XRPN are not yet known. The generic stale response supplied earlier does not contain the timeframe ages or timestamps. **Data not found in sources.**
- Schwab OAuth/account/position sync remains unimplemented pending the user's Developer Portal approval, callback URL, and secure server-side app credentials. Order execution remains disabled.

## Validation checkpoint — hybrid WebSocket 1m / REST 5m — 2026-10-30

- `MarketBar.SourceTransport` records REST vs WEBSOCKET provenance. The raw store includes transport in its content hash, so an identical OHLCV bar received over WebSocket is retained as a new observation rather than lost to deduplication.
- Live assessment selects stored 1m bars only when stream subscription is confirmed, at least 50 completed 1m bars are present, the latest bar is within the 120-second freshness limit, and the latest observation is tagged WEBSOCKET. It falls back to REST otherwise. 5m permission remains REST.
- Decision Explanation, Main Chart, and the header show the actual transport.
- GitHub Actions run 707 passed Release build and **152/152 tests** for the transport-provenance test fixture correction. The initial new test failed because its fixture had `Timeframe=UNKNOWN`; fixed by setting `1m`. Runs 708–710 passed for the subsequent frontend changes. Run 711 was still in progress at the time of this checkpoint.
- Browser runtime and actual provider/WebSocket acceptance remain unverified. Keep WebSocket disabled until server-side configuration is intentionally enabled.

## Current handoff — Morning Brief, replay, streaming, and Schwab position sync — 2026-10-30

## Current handoff — Morning Brief, replay, streaming, and Schwab position sync — 2026-10-30

Branch: `feature/opportunity-quality-classification`

### Current implementation pass
- Morning Command Brief moved to a full-width, collapsed-by-default section near the bottom of MCC; a header **Morning Command Brief** button opens a modal snapshot of the same report.
- Live assessment stale-data response now reports per-timeframe stale age, threshold, source timestamp, expected close, and feed, plus structured `freshnessDiagnostics`.
- Added historical replay API `GET /api/market-data/replay` and UI panel. Required inputs: symbol, explicit feed, UTC as-of timestamp ending in Z; optional `hasPosition=true` simulates the engine's existing position-management branch. The service loads only persisted 1m/5m bars completed by as-of, and uses the same current V2SignalEngine/default V2Config. No lookahead and no order placement.
- Added optional Alpaca WebSocket minute-bar collector (disabled by default). It authenticates, checks subscription acknowledgement, receives `b` and `u` minute bars, persists corrections, reconnects with backoff, and exposes stream status. The live assessment uses the persisted 1m series when subscription is confirmed, at least 50 completed bars are available, the latest bar is fresh, and the latest observation is tagged WEBSOCKET. It falls back to REST otherwise; 5m permission remains REST-backed.
- Added header WebSocket collector health chip. The 1m assessment uses WebSocket-sourced data only when the explicit source/history/freshness checks pass; the 5m permission series remains REST-backed.
- Schwab integration is still blocked on the user's developer app approval/callback and secure server-side credentials. No read-only Schwab position connector is implemented yet.
- Order execution remains disabled.

### Immediate user validation / evidence needed
1. Pull latest branch and hard-refresh.
2. Open the header Morning Command Brief modal; then expand the collapsed full-width brief at the bottom. Verify both render the same API-generated report.
3. Select RKLX, ASTX, XRPN and capture each full `GET /api/market-data/live-assessment?symbol=...` JSON response. New `freshnessDiagnostics` identifies the exact stale timeframe(s). The generic stale message alone is insufficient to diagnose each symbol.
4. Replay a known stored timestamp with the matching feed (IEX or SIP). Then toggle simulated existing-position mode and compare the V2 result.
5. For WebSocket testing, add server-side `MarketData:Alpaca:StreamEnabled=true` and `MarketData:Alpaca:StreamSymbols=SPY,RKLX,ASTX,XRPN`. Confirm `/api/market-data/stream/status` reports a confirmed subscription. Keep feed selection within entitlement.
6. Do not share brokerage credentials or OAuth tokens. Complete Schwab portal registration/app approval and use secure server-side secrets.

### Important limitations
- Replay uses the current default V2 configuration; historical config versions are not yet archived.
- Replay `hasPosition` is only a boolean simulation. No broker-sourced quantity, side, average entry, account mapping, or sync-health guard is present.
- WebSocket-sourced 1m bars now feed the live assessment when connection, 50-bar history, freshness, and provenance guards pass. The 1m path falls back to REST when they do not. 5m permission remains REST-backed.
- Morning Brief remains a partial market snapshot; breadth, verified sector money flow, classified catalysts, probability/outcome history, validated Red Zone ranking, and Boring Overnight qualification remain incomplete.
- CI and browser acceptance must be checked on the exact latest head before calling this pass validated.

Read `docs/Master-To-Do.md` for the complete acceptance checklist and `docs/Live-Symbol-Assessment.md` for data/freshness semantics.


## Validation status correction — 2026-10-30

- GitHub Actions run 663 **PASSED** for live Decision Explanation / score UI commit `d2dbd628fa40de11e49b0693b25280e2dc22453e`.
- GitHub Actions run 664 **PASSED** for the Master To-Do documentation update.
- GitHub Actions run 665 is still running for the latest Handoff documentation update; the code itself has already passed run 663.
- Frontend JavaScript syntax check passed. User confirmed the live assessment endpoint returned Success.
- Local browser verification of this newest Decision Explanation/score presentation remains open.

## Latest validation checkpoint — 2026-10-30

- Current implementation adds live Decision Explanation, six-factor V2 score breakdown, colored Red Zone score bands, exact UNKNOWN reasons, 15-second REST refresh, and a 10-second assessment cache.
- [USER CONFIRMED] Direct `GET /api/market-data/live-assessment` endpoint test returned Success.
- [PASS] Frontend `app.js` JavaScript syntax check.
- [PASS] GitHub Actions run 661 for commit `8ff0ea4a01af356c55c901321585fb92b5d90241`.
- [PASS] GitHub Actions run 662 for commit `c5e14a6f30a463062e3d7ef221b73a1ca505ba60`.
- [IN PROGRESS] GitHub Actions run 663 for commit `d2dbd628fa40de11e49b0693b25280e2dc22453e` (state-label formatting). Do not mark the current branch head validated until this completes.
- [OPEN — USER BROWSER TEST] The new live Decision Explanation, score colors, UNKNOWN explanations, and Product Feedback bottom placement need a local browser check.
- [OPEN — SYMBOL DATA] Exact RKLX/ASTX/XRPN evaluation reasons were not supplied in this conversation. The updated cards now show each symbol's server-returned status/reason; inspect those instead of guessing.
- [OPEN — STREAM] Current path is hybrid: 1m WebSocket when confirmed/fresh with enough stored history, otherwise 1m REST; 5m permission remains REST. Raw tick streaming is not required.
- [OPEN — POSITIONS] Schwab/Thinkorswim read-only position integration is design-only; see `docs/Thinkorswim-Schwab-Integration-Plan.md`.

## 2026-10-30 — Live Decision Explanation, score semantics, replay clarification

- [DONE — CODE] Added `fiveMinute.scoreBreakdown` to the live-assessment API. It returns the six V2 5M conditions individually (true/false/null), score maximum 6, and permission threshold 5.
- [DONE — CODE] Replaced the top-level demo snapshot Decision Explanation with symbol-specific live API data. It now shows the engine state/reason, 5M score conditions, 1M/5M source timestamps, freshness, feed, RVOL baseline sample count, and 1M entry context.
- [DONE — CODE] Red Zone score display is color-coded: 0–2 red with X; 3–4 amber; 5–6 green. The green **EXECUTION READY** label is reserved for `decision.entryAllowed=true`; a strong 5M score without 1M confirmation says to wait.
- [DONE — CODE] UNKNOWN cards now display the exact API evaluation status/reason rather than an unexplained UNKNOWN.
- [DONE — CODE] Removed the misleading Replay point slider. It previously selected five hard-coded examples, not historical bars or historical decisions. The UI now explicitly says historical replay is not implemented.
- [DONE — CODE] Moved Product Feedback to the bottom, after Provider Comparison.
- [DONE — CODE] Changed the dashboard refresh cadence to 15 seconds and the per-symbol API cache to 10 seconds. This remains REST polling, not streaming.
- [DONE — DOCS] Updated this To-Do, New Chat Handoff, User Manual, and Live Symbol Assessment guide; added a Schwab/Thinkorswim integration plan.
- [VALIDATION] GitHub Actions run 648 passed for commit `c808456b1cb36382fe9491735e79c6de336f47e8` (nullable RVOL compile fix). The current Decision Explanation/score/UI commits are newer and require their own CI run before acceptance.
- [OPEN — USER TEST] Confirm live Decision Explanation follows selected symbol; verify 6-factor criteria and timestamp/feed; confirm 0–2 red, 3–4 amber, 5–6 green, and entry-ready only when 1M entryAllowed is true.
- [OPEN — SYMBOL DIAGNOSIS] RKLX, ASTX, and XRPN previously displayed UNKNOWN. The new UI shows each symbol's actual `evaluationStatus` and `evaluationReason`; inspect those responses to establish the individual cause. Do not assume the same cause for all three.
- [OPEN — REPLAY] Implement true historical decision replay using stored 1m/5m bars, explicit as-of time, and the same versioned V2 engine/config. Preserve historical data quality and never use today's future bars.
- [OPEN — STREAMING DECISION] 15-second REST polling is adequate for first testing completed 1m/5m candle decisions but is not push streaming. If lower latency is needed, add Alpaca websocket minute bars and updated-bar corrections; do not start with raw tick processing because current V2 rules evaluate completed bars.
- [OPEN — THINKORSWIM/SCHWAB] Read-only Schwab Trader API position integration plan is documented in `docs/Thinkorswim-Schwab-Integration-Plan.md`. Requires user-owned Schwab developer app, approved API access, OAuth token lifecycle, secure token storage, account/position mapping, and reconciliation. Do not enable order submission as part of the position-read phase.

## User browser verification update — 2026-10-30

- [USER VERIFIED] Morning Brief News Link hover.
- [USER VERIFIED] Superscript legend lines.
- [USER VERIFIED] Main Chart placement.
- [USER VERIFIED] Red Zone focus updates.
- [USER VERIFIED] Red Zone refresh.
- [USER VERIFIED] Manual Red Zone management.
- [USER VERIFIED] System Readiness topbar.
- [USER VERIFIED] Provider Comparison placement at bottom.
- [USER VERIFIED] Quick Start modal.
- [USER VERIFIED] Favicon 404 no longer present.
- [IMPLEMENTED; USER RETEST PENDING] Section IDs.
- [STILL ONGOING] BrowserLink `$.noConflict` error. Do not mark fixed until the injecting script and stack are identified.

## Final implementation checkpoint — live data and layout (2026-10-30)

- Current implementation includes `GET /api/market-data/live-assessment`, `LiveSymbolAssessmentService`, the live frontend consumers, 600px scanner table, compact topbar, tests, and documentation.
- The latest technical-calculation refinements use Wilder RSI14 and ATR14, regular-session-only VWAP, same-clock-slot 5m RVOL, and explicit future timestamp status.
- Live UI is REST-polled every 60 seconds; API results are cached for 30 seconds. This is not streaming/tick data.
- Latest code SHA before this documentation update: `bc1d7f9bf3c862d8780c665b1b68585991d64644`.
- GitHub Actions for this work have been created but are still queued; no successful build/test result exists yet for the live-assessment changes. Previous successful run 620 predates this feature and must not be used as evidence that the new service compiles.
- Browser runtime acceptance is pending for the new live endpoint, auto-updating Red Zone/Main Chart, 600px table, sticky Watch action and compact header.

## Handoff checkpoint — live symbol assessment implementation (2026-10-30)

### Current branch and implementation
- Branch: `feature/opportunity-quality-classification`. Inspect branch HEAD before modifying files.
- Added `src/MomentumCommandCenterV2.API/MarketData/LiveSymbolAssessmentService.cs`.
- Added `GET /api/market-data/live-assessment?symbol=XYZ&providerName=Alpaca` in API Program.cs and registered the service.
- Added `tests/MomentumCommandCenterV2.Tests/LiveSymbolTechnicalCalculatorTests.cs`.
- Frontend now uses the assessment endpoint for Main Chart and every visible Red Zone card.
- Main Chart and Red Zone auto-refresh every 60 seconds; server caches per symbol/provider for 30 seconds.
- Header now contains the workspace toolbar. System Readiness is a compact 2×2 grid to the right of the brand.
- Scanner Workbench table wrapper is 600px high, scrolls vertically/horizontally, keeps header sticky and keeps the Watch action column sticky at the right edge.

### Assessment calculations and decision semantics
- Provider requests: Alpaca 1m lookback 3 calendar days; Alpaca 5m lookback 21 calendar days. Returned completed bars are persisted to the append-only observation store.
- Indicators: EMA9, EMA20, SMA50, RSI14, MACD(12,26,9), ATR14 and session VWAP.
- Extension: non-negative 1m close-to-EMA9 distance in 1m ATR units.
- 5m relative volume: current bar volume divided by average volume from the same regular-session 5m clock slot across up to ten previous sessions. Requires at least five prior session samples. It is calculated from OHLCV; it is not a provider-supplied consolidated RVOL value.
- Uses the existing Core `V2SignalEngine` to calculate 5M permission and 1M entry-context execution. Response exposes score, permission, structure, momentum, entryAllowed, state/action, extension and reason.
- 1m freshness maximum: 120 seconds after expected bar close. 5m freshness maximum: 360 seconds after expected bar close.
- Minimum indicator history: 50 completed bars for each timeframe; a previous 5m snapshot is required for deterioration context.
- Fails closed: stale bars, insufficient history, insufficient same-slot RVOL samples or outside regular session => no 5M permission / 1M execution decision; response includes explicit status and reason.
- Assessment is called with `hasPosition=false`. It is for candidate entry evaluation, not existing-position management.
- Main Chart displays the latest 180 completed 1m bars. Provider/feed and source timestamps are included in assessment metadata.

### How to interpret manual Red Zone pins
A pin is now a request to assess a ticker, not a source-data record. Once the provider returns bars and the minimum calculations are available, the card updates automatically. If the symbol is unsupported, has insufficient history, has stale bars, or lacks five same-slot RVOL samples, the card must show the corresponding UNKNOWN/insufficient/stale state rather than invent a value.

### Known limitations — do not overstate “live”
- Historical REST polling every 60 seconds with a 30-second server cache; not websocket/tick streaming.
- No guaranteed quote/tick-level freshness.
- The configured Alpaca feed and account entitlement determine coverage; response reports feed when available.
- No NBBO spread, order-book depth, halt/catalyst classification, news interpretation, or Thinkorswim study inputs.
- The existing V2 engine is used as-is; thresholds remain the existing V2Config defaults and are not newly calibrated here.
- Market-wide discovery remains a separate top-movers/SIP-dependent feature. Pinning manually adds a symbol to the browser-local Red Zone, not to Alpaca's market-wide scanner.
- BrowserLink `$.noConflict` remains a separate unresolved development-host issue from previous handoff.

### Next required validation
1. Check latest branch SHA and GitHub Actions run. Do not rely on older passing .NET runs.
2. Run all .NET tests; specifically inspect the new LiveSymbolTechnicalCalculatorTests.
3. During US regular session, call `/api/market-data/live-assessment?symbol=SPY` and another known liquid symbol. Inspect provider/feed, 1m/5m bar counts, completed/source timestamps, freshness, same-slot RVOL sample count, 5M score/permission, 1M entry state and reason.
4. Test a manually pinned symbol absent from the scanner discovery data. It should still populate from market-data API if the provider supports it.
5. Test unconfigured provider, invalid symbol, stale/out-of-session, low-history and low-RVOL-baseline cases; confirm no fabricated green state.
6. Verify Main Chart and all visible Red Zone cards refresh on cadence; confirm browser console and Network panel.
7. Verify 600px scanner table scrolling, sticky Watch action column, compact header, 2×2 readiness and toolbar placement.
8. Decide whether to move from 60-second historical REST polling to websocket/streaming only after evaluating latency and provider entitlement requirements.

## Checkpoint — MCC dashboard layout and browser-feedback pass (2026-10-30)

### Repository state
- Working branch: `feature/opportunity-quality-classification`.
- Latest known commits from this pass: app logic `c4bc11c3b894fb027bb1608c3e9976ae2e17da35`, explicit favicon link `7f1f6cb8f22737b88021c84ac74d750daf1ed8a8`, favicon asset `da8480a2f2687080951ea6ad9fd8d5857852a278`. Documentation updates may advance the branch further; always inspect current branch HEAD before editing.
- GitHub Actions .NET build/test passed on `c4bc11c3b894fb027bb1608c3e9976ae2e17da35` (run 41095658724). This does not validate browser rendering or frontend JavaScript.
- User confirmed Morning Brief News Link hover works locally. BrowserLink `$.noConflict` error was reported as still present.

### Dashboard changes in this pass
- Main Chart is the focused symbol's API-backed 1-minute close-price line and now sits immediately below the workspace toolbar at the top of main.
- Red Zone focus calls `render()` and `refreshFocusedPriceChart()`; this makes the opportunity-status and decision-layout panels rerender on candidate focus. Current status metrics remain illustrative snapshots, not calculated symbol-specific signals.
- **Refresh Red Zone** re-renders the currently selected scanner group and reloads each tile's chart. **Refresh Alpaca market movers** is the action that queries persisted Alpaca discovery events and switches the scanner group to the live-discovery results.
- Red Zone manual pin/dismiss/restore uses browser localStorage key `mcc.redZoneState.v1`. Pins/dismissals are browser-local, not account- or server-synced. Manual ticker without a source row is explicitly `MANUAL — REVIEW` with DATA?/UNKNOWN fields.
- System Readiness moved into header topbar. Major sections have IDs. Provider Comparison moved to the diagnostics area and is not a daily step. Quick Start modal added, with header and footer openers. Market-mover legend lines are separated.
- Explicit `frontend/favicon.svg` link and asset added. This is a code-level remedy only until local browser confirms the 404 is gone.

### Known limits / unresolved browser errors
- BrowserLink `$.noConflict`: no reference found in frontend app/index or API Program.cs. Likely development-host injected, but this is not confirmed. Inspect console stack and script URL in the user's browser; do not suppress globally or change jQuery behavior speculatively.
- Favicon 404: asset/link added; still requires browser/network verification after pull/reload.
- Provider Comparison checks historical REST requests only. It does not test streaming, reconnect/backfill, complete-session coverage, or provider entitlements.
- Main Chart is a price line, not candlesticks or an MCC 1M/5M state chart.
- `status-grid` / `layout` refresh via renderer, but their decision snapshot content is still demo fixture content; it is not symbol-specific 5M permission or 1M execution.
- Red Zone pin/dismiss controls affect the visible four-card composition; they do not create a server-side watchlist or alter the provider scanner.

### Next browser pass
1. Pull latest branch and capture `git rev-parse HEAD`.
2. Confirm News Link hover; inspect all four legend lines.
3. Confirm Main Chart is first main content section after toolbar.
4. Focus a Red Zone card and confirm symbol badge, chart, opportunity-status, and decision-layout all respond.
5. Test Refresh Red Zone, Refresh Alpaca market movers, manual pin, dismiss, restore, page reload persistence.
6. Confirm System Readiness is in the topbar and major section IDs exist.
7. Open/close Quick Start from both entry points; check Escape and mobile layout.
8. Open Provider Comparison only as a diagnostic; confirm it is after the main workflow.
9. Check favicon network response and BrowserLink console stack/script URL. Do not mark BrowserLink fixed without identifying its origin.
10. Run `dotnet test .\\MomentumCommandCenterV2.sln` and record exact tested SHA; note that .NET tests do not substitute for browser checks.

## Checkpoint update — local Alpaca configuration and XRPN evidence (2026-10-23)

- Added the API project's `.NET User Secrets` identity and documented local Alpaca setup commands in `src/MomentumCommandCenterV2.API/README.md`. Tracked `appsettings.json` now contains only the non-secret default feed (`iex`); do not place API keys there.
- Alpaca adapter credentials now resolve through standard ASP.NET Core configuration, so User Secrets and environment variables follow the same configuration pipeline. Added deterministic xUnit coverage for configured credentials, request headers, bar parsing, and the unconfigured-provider path. GitHub source edits do not execute tests; latest-head CI must be checked before claiming these tests pass.
- User supplied the requested XRPN 2026-10-02 screenshot. It shows the normal 5M study, Authority Diagnostic V2, readable time axis and target 12:30–13:00 Arizona window; user confirmed the 5M and 1M chart dates are correct and the top-left chart date should be ignored. Canonical To-Do and external-dependency checklist now mark this evidence received.
- Remaining user action: create the free Alpaca account, configure credentials locally with the README commands, run the API, and report sanitized status/errors/timestamps. Do not ask for credentials in chat. Keep Massive deferred and paid subscriptions on hold.
- Still open without provider credentials: pagination/retries, collector lifecycle/recovery, storage hardening, fixture-driven Morning Brief/scanner/Boring Overnight, session-aware gap semantics, ThinkScript parity and installed-study validation.

# Momentum Command Center — New Chat Handoff

**Updated:** 2026-10-23
**Repository:** [MomentumCommandCenterV2](https://github.com/themailcomeshere/MomentumCommandCenterV2)
**Working branch:** `feature/opportunity-quality-classification`
**Canonical task list:** `docs/Master-To-Do.md`
**Purpose:** Recovery guide for a new ChatGPT conversation or interrupted project session. Treat the repository and this document as the working record; never invent missing facts.

## New user requirements — extended momentum and Schwab (2026-10-22)

- User prefers manual Schwab website order entry; no broker API, credentials, or automated order placement. A future "Open Schwab" link is only a possibility after a supported workflow is verified. Do not build one-click Buy Maximum Amount. If requested later, a non-submitting position-size calculator is the safer starting point.
- User wants MCC to account for stocks already up around 10–15% when they sit down to trade, especially when continuation/news context is strong. Do not automatically discard these candidates solely because they are extended; add an **Extended Momentum / Continuation Watch** research lane.
- This is not permission to override EXT, 5M Permission, or the 1M EXECUTION READY gate. Separate "extended/no entry now", "continuation candidate worth monitoring", and "fresh valid entry event". News must be timestamped and sourced, and cannot alone create eligibility.
- Audit the exact installed ThinkScript to determine what the red "In-trend candidate" bubble means. Do not describe it as technically safe to buy until its exact formula is verified.
- Evaluate a separate validated pullback/reclaim or consolidation-breakout re-entry condition, with spread/liquidity/gap/halts safeguards and out-of-sample tests, rather than weakening the original entry gate.
- Timezone: the user references 08:30 Arizona time. Arizona generally stays on MST while Eastern market hours shift with DST, so the US regular-session open is generally 07:30 Arizona time during Eastern daylight time and 08:30 during Eastern standard time. Dashboard labels should include timezone/session context.


## UPC extension-danger evidence review (2026-10-23)

- User supplied UPC screenshots and facts for a reported 2026-10-22 09:25 event. Reported 5M `Perm=1` and `PBLOCK=0`; 1M `EXT=2.35`, `CANDIDATE BLOCKED: EXTENSION DANGER`, and `IN-TREND CANDIDATE`. This supports keeping the 5M permission and 1M entry gate separate; no logic change is justified.
- Evidence review: `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md`.
- Screenshot headers appear to show 2026-10-23 around 11:26 AM, conflicting with the reported event date. Timezone is missing. Reported missing bars cannot be interpreted as zero movement/volume. Resolve exact bar timestamps and data before diagnosing gaps.
- The exact installed 1M source and `BLK=6` mapping remain unverified. Do not infer source parity from study names.

## Provider comparison refresh (2026-10-23)

- `docs/Provider-Comparison.md` now compares public free and paid tiers against MCC's target. Recommended paid finalists to proof-test: Alpaca Algo Trader Plus ($99/month) and Massive Stocks Advanced ($199/month); no purchase/provider decision made. Free tiers are for plumbing tests, not live authority.


## Provider comparison UI and UPC CSV update (2026-10-23)

- User confirmed UPC screenshot/event date is 2026-10-22 and TOS chart timezone is Arizona.
- Reviewed `data/StrategyReports_UPC_102226.csv`: strategy/order events are 09:37, 09:40, 09:41, 09:47 Arizona; it is not a continuous OHLCV export and cannot establish whether earlier bars were absent.
- Added offline comparison cards for Alpaca and Massive to `frontend/index.html`, CSS in `frontend/styles.css`, and a local checklist action in `frontend/app.js`. These are UI-only placeholders; no provider API calls/adapters/credentials are wired. The comparison button only prepares a test checklist and must not be described as a real provider comparison.
- Evidence review: `docs/Validation/UPC-Extension-Danger-Evidence-Review-20261023.md`; canonical To-Do updated. Still need full UPC OHLCV or exact TOS timestamp evidence, then build backend provider adapters and compare historical/stream data after entitlement/licensing review.

## 1. Mission and architectural boundaries

MCC is a C#/.NET 8 research and trading decision-support prototype, not an auto-trader. Do not implement automatic order placement or Schwab order submission.

- **5M is the authority/permission timeframe.** It establishes whether the broader setup has permission.
- **1M is the execution-timing timeframe.** It may consume 5M context; 5M must never depend on 1M.
- Keep data confidence, setup/entry eligibility, opportunity quality, entry timing, and position management as distinct dimensions.
- Scanner discovery is a candidate-universe input, not a trade signal. A scanner must not create or override 5M Permission or 1M `EXECUTION READY`.
- Opportunity-quality classifications remain diagnostic-only until exact input parity and out-of-sample validation support further use.
- Preserve raw data, source provenance, timezone offsets, and versioned study/config identities. Never synthesize missing bars or silently fill gaps.

## 2. User workflow and collaboration rules

- No fabrication. If data is missing, state: **Data not found in sources.**
- Flag assumptions and unknowns in a final `[Uncertainties]` section for complex responses.
- Be direct and thorough; execute the next task without unnecessary approval requests.
- For any multi-step work, report what changed, what is verified, what remains open, and exactly what the user needs to do (if anything).
- Do not claim build/tests/CI passed without evidence tied to the current branch head.
- Follow `docs/rules/abpDevRules.md`: flowerbox classes and methods with purpose/when-called comments; preserve clean layer dependencies.
- Update this handoff and `docs/Master-To-Do.md` when the project checkpoint materially changes.

## 3. Current project state

**New documentation:** [5M→1M handoff audit](Validation/5M-to-1M-Handoff-Audit-20261022.md) · [Overnight collection contract](Overnight-Collection-Contract.md) · [Canonical Master To-Do](Master-To-Do.md)

### Latest completed work (2026-10-23)

- Added `docs/Validation/5M-to-1M-Handoff-Audit-20261022.md`: source comparison confirms the 1M layer reconstructs rather than consumes the actual 5M Permission state, and records concrete raw-Permission, structural-damage, and major-failure predicate differences. No ThinkScript logic was changed; installed-study parity remains open.
- Added `docs/Overnight-Collection-Contract.md`: provider-neutral contract for event envelopes, append-only persistence, idempotency, freshness/health, sleep/restart/outage recovery, deterministic Morning Report snapshots, and required tests. Provider-specific thresholds remain undefined until provider selection.
- Latest branch-head .NET workflow was checked: head `bcb1ef5027859cd4448daa08191df83e0e6eb871`, run 91, conclusion `success`. This does not replace a browser-based end-to-end smoke test.

### Architecture/code already present

- Core market bar and replay models, CSV parsing, replay frame sequencing, V2 signal engine, and opportunity-quality diagnostic models/assessor.
- Strategy Report parser for the MCC-owned `MCC_OHLCV` payload, with validation/rejection paths for malformed/legacy rows.
- ThinkScript references in `docs/Charts/1M/` and `docs/Charts/5M/`, including Authority Diagnostic V2 for the 5M Permission audit.
- Static front-end under `frontend/` is a local/demo workbench; its sample rows are not live scanner output and are not calculated from the C# engine. The dashboard now includes a readiness panel and browser-local feedback notebook with JSON export.
- Application-layer scanner contract scaffold was committed on 2026-10-22: `ScannerDiscoveryEvent.cs` and `IScannerDiscoverySource.cs`. These are contracts only—not a concrete provider, polling job, storage layer, UI wiring, or live integration.
- Scanner candidate dispositions are presentation states (`ActionableQueue`, `WatchPullback`, `FilteredExtended`, `Blocked`, `DataUnknown`), not trade signals.

### Market-data validation status — important reconciliation

On 2026-10-22, the user clarified that they completed the following **locally** and saw only `MATCH` or `ROUNDING ONLY`: (1) all 31 irregular 1M intervals, (2) full-session 1M-to-5M reconciliation, (3) RTH/extended-hours session-setting checks, and (4) Arizona/Eastern DST checks. The user intentionally did not commit all detailed comparison work because they considered it wasteful.

Treat these checks as **USER-CONFIRMED DONE** for task planning. Do not ask the user to repeat them. However, detailed local comparison rows, run dates/settings, classifications, hashes, and provenance are not all present in the current repository; independent auditability is therefore incomplete. The efficient follow-up is to sync already-completed local artifacts or a concise manifest/summary if available—not rerun the analysis.

`docs/Validation/MCC-OHLCV-20261020.md` and `docs/Master-To-Do.md` were updated to reflect this distinction. The 20-row `_UPDATED_3.csv` 1M and 5M samples are already committed and documented as `MATCH`, with precision-only differences identified as `ROUNDING ONLY`. The `_UPDATED_4.csv` files contain later appended source payload rows whose chart comparison fields were observed blank in the repository; do not treat those appended rows as completed chart parity without reconciling the local copy.

### What remains a validation blocker

- Reconcile the user's completed local interval/full-session/session/DST results into repo evidence where practical (exact files, chart/study version, run dates, settings, row/window counts and hashes where available). Do not invent missing metadata.
- Verify build and tests at the current branch head. Older CI runs or user-reported local passes do not prove the current head passes.
- Complete golden indicator parity for the exact versioned ThinkScript definitions used by the C# application, especially ATR/extension and any other metrics that will gate scanner candidates.
- Keep replay authority and live scanner gating blocked until the required evidence and indicator parity are reconciled.

## 4. XRPN 2026-10-02 5M Permission audit — result

The user uploaded a screenshot showing the normal 5M study and Authority Diagnostic V2 around the Permission transition near 12:45 Arizona chart time. Red diagnostic bubbles show `Perm=0`, `PRAW=0`, `PBLOCK=2`, and `EOD=0`; visible conditions include price below VWAP and EMA9 and EMA9 below EMA20.

Source review in `docs/Charts/5M/MainStudy.txt` confirms the `PBLOCK=2` mapping to persistent major failure. The major-failure raw condition is price below Daily VWAP, price below EMA9, EMA9 below EMA20, and EMA20 below SMA50; persistence requires two bars by default. The Permission latch checks EOD, then persistent major failure, then persistent structural damage, and recovers when the bullish raw condition is true.

Conclusion: the screenshot and source agree that persistent major failure explains the observed Permission revocation. No Permission code change was justified by this evidence. The Master To-Do records the audit and the next handoff task.

## 5. Scanner integration decision and EXT problem

**Decision: start the provider-neutral scanner foundation; do not connect a live provider yet. Thinkorswim is not currently connected as a live scanner/API feed.**

The user reports that red-state `EXT`/extension-danger often coincides with no `EXECUTION READY`, causing wasted chart reviews. Treat the workflow issue as credible, but do not invent a measured frequency or alter the existing gate without data.

Required design:

- Filter/deprioritize currently overextended candidates from the default actionable queue using the exact versioned 1M study's ATR-extension formula and thresholds.
- Keep them in a recoverable `WATCH_PULLBACK` list; do not permanently discard a symbol solely because it is extended now.
- Requalification requires fresh data, a valid new event, and the existing 5M authority and 1M execution gates.
- Distinguish extension warning from danger. Never substitute guessed percentage thresholds or provisional opportunity-assessor thresholds for the exact study rules.
- Missing/stale extension metrics must produce `DATA?`, not an assumption that the candidate is not extended.
- Run shadow mode and measure chart reviews avoided, valid opportunities hidden, and later pullback/READY events before hard-exclusion becomes default.

Provider choice, verified API access, licensing, rate limits, and live credentials are not established in sources. Thinkorswim is currently the chart-study reference/validation environment, not an integrated feed. Do not claim a provider is integrated. No automatic orders.

## 6. Scanner commits and documents

Recent commits on `feature/opportunity-quality-classification`:

- `5c3f45d0a82202aa554550e7a991f0dc251629c6` — add `src/MomentumCommandCenterV2.Application/Models/ScannerDiscoveryEvent.cs`.
- `719db8631a1d3629cfccf98933817f8fb7afa9fd` — add `src/MomentumCommandCenterV2.Application/Interfaces/IScannerDiscoverySource.cs`.
- `59a516436994c9ac869c46ac48ee629d712731c1` — document scanner contract and EXT qualification in `docs/Scanner-and-Watchlist-Workbench.md`.
- `ec4029b2807a64727ad0de88a7d4dd5fa32dbb40` — add scanner integration and EXT prequalification items to Master To-Do.
- `7616548fb9779ee74538a6397fb833125cd93d02` — mark contract scaffold as partial; remaining acceptance work stays open.
- `eccc9c0cb1a6d0147778b6b1e15f547bfe400e03` — reconcile validation doc with user's locally completed MATCH/ROUNDING results.
- `1e057315783119059f599ef4026a09f654353b8b` — update Master To-Do to mark interval/session/DST checks user-confirmed done and evidence sync pending.

These commit hashes are the observed GitHub responses from the tool actions; they are not a claim that CI passed.

## 7. Recommended execution order from this checkpoint

1. [NOW] Inspect current branch head and workflow status; run or trigger build/tests for the exact head if supported. Record pass/fail and SHA accurately.
2. [NEXT] Audit exact 1M extension/ATR implementation and define the scanner qualification input contract from the versioned study. Confirm the C# app can calculate these inputs with golden parity tests; otherwise keep scanner extension gating blocked.
3. [NEXT] Implement deterministic provider-neutral scanner qualification and append-only discovery history. Add tests for first-seen preservation, deduplication, source timestamps, stale/missing values, reason codes, and transitions between actionable, pullback-watch, filtered, blocked, and data-unknown.
4. [NEXT] Add extension warning/danger tests, pullback requalification tests, and prove filtering never changes 5M Permission or 1M READY.
5. [NEXT] Reconcile completed local interval/session/DST artifacts into a compact manifest if available; do not repeat the tests solely to recreate already completed work.
6. [BLOCKED UNTIL CHOSEN] Implement one live provider adapter only after the user/provider choice and verified access requirements are known.
7. [NEXT] Run shadow mode on timestamped data and measure both efficiency gains and false-negative risk.
8. [NOW] Use the updated local dashboard for UI feedback; export feedback JSON before changing browsers/devices because notes are browser-local.
9. Keep the local UI clearly marked demo until it consumes the validated application contract; no order execution.

## 8. Canonical source map

- `README.md` — project overview and roadmap.
- `docs/Master-To-Do.md` — canonical task checklist and next-task authority.
- `docs/NewChatHandoff.md` — recovery guide (this file).
- `docs/Scanner-and-Watchlist-Workbench.md` — local workbench and scanner integration status.
- `docs/Validation/MCC-OHLCV-20261020.md` — market-data export/parity status and user-confirmed local validation note.
- `data/validation/XRPN/10192026/parity/` — 1M/5M parity templates, including `_UPDATED_3` and `_UPDATED_4` versions.
- `data/validation/XRPN/findings.md` and `data/validation/XRPN/manifest.md` — intended detailed evidence/provenance record; currently still contain incomplete template fields.
- `docs/Charts/1M/MainStudy.txt`, `docs/Charts/5M/MainStudy.txt` — versioned study logic.
- `docs/Charts/5M/DiagnosticStudy.txt` — Authority Diagnostic V2.
- `docs/Market-Data-and-Replay-Contract.md` — raw bar, timestamp, replay, and parity requirements.
- `docs/Opportunity-Classification-Verification.md` and `docs/Opportunity-Quality-and-Ranking.md` — diagnostic-only quality assessment and validation protocol.
- `docs/rules/abpDevRules.md` — code/comment/layering conventions.

## 9. Current user action

No immediate user action is required to continue code/document work. If already-completed local validation artifacts can be synced without repeating analysis, that would improve auditability; otherwise keep them recorded as user-confirmed completion with evidence-sync caveat. Provider selection will eventually require user input unless a provider is already specified in a source.

## [Uncertainties]

- Detailed local records supporting the 31-interval, full-session, RTH/ETH, and DST checks are not present in the current repository state. The user's completion report is authoritative for planning but not independently reproducible from the committed files.
- Build/test status at the current branch head is not verified here.
- Exact scanner provider and its API/entitlement/licensing details are not specified; Thinkorswim is not integrated as a live feed.
- Live scanner extension gating remains blocked until exact indicator parity is implemented and tested.
- Dashboard feedback is local to the browser until manually exported; there is no server-side feedback storage.

## Morning Report / Dashboard workflow checkpoint (2026-10-22)

- User's target is to open MCC around 08:30 Arizona time and see a Morning Report, automatically populated and re-ranked watchlist, top 1–4 ranked candidate “Red Zone” tiles, and Boring Overnight decision support.
- New user manual: `docs/User-Manual.md`. Canonical To-Do remains `docs/Master-To-Do.md`.
- Current UI reality: `frontend/index.html` is opened in a browser; it is not an .exe. The page is a static demo/import workbench with localStorage watchlist/feedback. No live data/scanner provider, automatic ranking, generated Morning Report, Red Zone tiles, live Boring Overnight scoring, application-engine wiring, or broker API is connected.
- Repository source audit: `docs/Charts/1M/MainStudy.txt` sets extension warning/danger defaults at 1.50/2.00 ATR using absolute price-to-Daily-VWAP distance divided by ATR(14); bullish danger is specifically price above VWAP and extension at/above danger. `inTrendExecutionCandidate` means `executionCandidate AND NOT freshTrigger`; event is transition/re-arm gated. Final READY separately requires an active execution window, 5M execution permission, and no bullish extension danger. It is a candidate classification, not itself READY. This audit does not prove the installed Thinkorswim study matches repository source.
- Morning workflow/manual records the target Morning Report sections, up-to-four split-screen “Red Zone”, ranked watchlist, Boring Overnight pipeline, extended continuation watch, manual Schwab boundary, and acceptance criteria. All unimplemented features are labeled as planned.
- Search of the repository and available project handoff notes did not recover the full earlier Morning Routine conversation. Do not pretend the proposed routine is an exact reproduction. If exact details matter, ask the user to paste or point to the earlier conversation.
- Next: continue installed-study/source parity audit; preserve current trading rules; define provider-neutral Morning Report and scanner data contracts; choose a verified data/scanner source; implement automated watchlist and ranking, Red Zone, and Boring Overnight only with provenance/freshness and validation. Decide .exe packaging after integration architecture is settled.


## Morning Report UI and overnight collector checkpoint (2026-10-22)

- User wants to leave released/deployed MCC open overnight and see collected scanner data in the 08:30 Arizona Morning Report. This is a requirement, not a current capability: the static browser workbench has no live provider, polling/streaming collector, persistent market-data event store, or background service. Keeping the tab open today does not collect data.
- UI updates:
  - `frontend/index.html`: Morning Command Brief section, explicit overnight collector NOT RUNNING status, scanner/watchlist/top-candidate/Boring Overnight status cards, responsive Red Zone placeholder.
  - `frontend/app.js`: locally rank demo/import scanner rows by supplied score for display only, render up to four Red Zone cards with unknown 5M/1M gates, update watchlist count, and export a JSON report preview.
  - `frontend/styles.css`: responsive Morning Report and 1–4 candidate tiles.
- The Morning Report is intentionally labeled preview-only. The displayed generated timestamp is the browser's local clock, not a market-data timestamp. The JSON export records liveScannerConnected=false, overnightCollectionRunning=false, provider=NOT_SELECTED, and Boring Overnight NOT_CALCULATED.
- Documentation updated: `docs/User-Manual.md`, `docs/Master-To-Do.md`, `docs/Scanner-and-Watchlist-Workbench.md`. New To-Do work requires a supported provider, long-running collector independent of browser tab, persisted raw timestamped events/observations, freshness/gap/duplicate checks, restart/outage/sleep recovery, versioned Morning Report, real ranked Red Zone, and Boring Overnight integration. The provider-neutral contract is now written in `docs/Overnight-Collection-Contract.md`; provider-specific adapter and operational thresholds remain blocked on provider selection.
- Do not claim live operation or .exe/background service until implemented and tested. Next engineering step is provider selection and data-ingestion/persistence architecture, not adding fake live-looking demo values.


## Latest checkpoint — Morning routine, provider research, collection scaffold (2026-10-23)

- Latest previously checked branch head `46c28fd4f56bc7db3b192a4b9a05b617bb42350a` had a successful .NET build-and-test workflow (run 39905346096). Subsequent commits in this checkpoint require a fresh CI check; do not assume they passed until verified.
- Read the complete Project source file Morning.txt (992 lines). Consolidated its selected excerpts into `docs/Morning-Report-and-Routine.md`. It covers the 08:30 Arizona briefing, market/industry context, scenario expectations, continuous conditions, contextual ranking, catalyst maturation risk, Boring Overnight, journey/outcome learning, personalized timing, and execution intelligence. Illustrative numbers in that file are not real outputs.
- Added `docs/Provider-Comparison.md` comparing Alpaca, Massive, Twelve Data, and Schwab evidence. Preliminary proof-of-fit candidates are Alpaca Algo Trader Plus and Massive Stocks Advanced. No provider is selected; account entitlements and contract rights are unverified.
- Added an initial provider-neutral persistence scaffold:
  - `src/MomentumCommandCenterV2.Application/Interfaces/IScannerDiscoveryEventStore.cs`
  - `src/MomentumCommandCenterV2.Application/Services/ScannerDiscoveryIngestionService.cs`
  - `src/MomentumCommandCenterV2.Infrastructure/Persistence/JsonlScannerDiscoveryEventStore.cs`
  - `tests/MomentumCommandCenterV2.Tests/JsonlScannerDiscoveryEventStoreTests.cs`
- Scaffold validates event fields/timestamps, stores discovery envelopes in append-only JSONL, retains receipt time, and deduplicates using source event ID or a deterministic fallback key. It is not a background worker/provider adapter and does not yet persist OHLCV bars.
- Updated Dashboard layout so Morning Brief sits left and Red Zone sits upper-right on wide viewports; the two-column workspace collapses responsively. User had approved the earlier look; the latest position change still needs browser confirmation.
- Created `docs/Validation/5M-to-1M-User-Validation-Steps.md` with exact steps and evidence needed from the installed Thinkorswim studies. User need not repeat the already completed OHLCV/DST work.
- Updated To-Do, User Manual, and Scanner Workbench with this status.


### Polling worker scaffold (2026-10-23)

- Added `src/MomentumCommandCenterV2.Application/Services/ScannerDiscoveryPollingWorker.cs`: cancellable generic poll loop, durable ingestion before success state, retry on next interval after errors, and health snapshot (RUNNING / DEGRADED / STOPPED / UNKNOWN). It is not hosted or running because no provider adapter is configured.
- Added `tests/MomentumCommandCenterV2.Tests/ScannerDiscoveryPollingWorkerTests.cs` for empty-batch handling and graceful cancellation.
- The event store contract import fix passed CI at commit `38b3f3ad248bd020ac48f377ce86421f7753f76d`; the latest worker/test commit needs its own CI result.


## API host, provider adapters, and Morning Brief — 2026-10-23

- Added project `src/MomentumCommandCenterV2.API` and included it in the solution. The API serves the existing dashboard under `frontend/` from the repository's normal directory layout.
- Added normalized provider contracts and first REST historical-bar adapters:
  - `src/MomentumCommandCenterV2.API/MarketData/AlpacaMarketDataAdapter.cs`
  - `src/MomentumCommandCenterV2.API/MarketData/MassiveMarketDataAdapter.cs`
  - `src/MomentumCommandCenterV2.API/MarketData/MarketDataContracts.cs`
- Credentials are read server-side from .NET configuration or `ALPACA_API_KEY`, `ALPACA_API_SECRET`, `ALPACA_DATA_FEED`, and `MASSIVE_API_KEY`. No credentials were supplied; no live requests or plan-entitlement checks have occurred.
- Endpoints: `/api/health`, `/api/providers/status`, `/api/market-data/compare`, and `/api/morning-brief`.
- Dashboard provider comparison now requests both adapters for the same UTC window. Morning Brief fetches only SPY/QQQ/IWM 5M observations from configured providers and explicitly labels sectors, movers, catalysts, historical expectations, Red Zone, and Boring Overnight as DATA? / NOT CALCULATED.
- Start with `dotnet run --project src/MomentumCommandCenterV2.API`. Keep keys out of Git, browser code, localStorage, and logs.
- **Not yet verified:** latest solution build/test/CI, endpoint smoke test, browser test, provider response parsing against entitled accounts, licensing, timestamp/session parity, pre/post-market coverage, pagination, WebSockets, reconnect/backfill, and durable OHLCV storage. Do not rely on this as a replacement for TOS yet.
- Continuous UPC OHLCV is not a prerequisite for the provider implementation. It is still the evidence needed to resolve the separate UPC chart-gap question for 09:16–09:36 on 2026-10-22.


### CI checkpoint — 2026-10-23

- GitHub Actions run 147 completed successfully at commit `ae8180866def759612fa7c1a515dc93586143056`. Restore, Release build, and tests passed: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40011426478
- This is not live provider validation or browser testing. The subsequent To-Do/handoff documentation-only updates should still be followed by a check of the newest workflow run before claiming latest-head validation.


### Raw OHLCV persistence extension — 2026-10-23

- Added `IMarketBarObservationStore` and `JsonlMarketBarObservationStore` under the API project. The comparison endpoint persists each provider's valid raw bars before returning comparison metrics.
- Default file: LocalApplicationData/MomentumCommandCenterV2/market-bars.jsonl. Override with environment variable `MCC_MARKET_BAR_STORE_PATH`.
- Append-only records include observation ID, received timestamp, source timestamp, provider, symbol, bar, content hash, and optional superseded observation ID. Identical observations are skipped; changed observations at the same provider/symbol/timestamp are appended as corrections.
- Persistence is local JSONL only; no database, retention controls, collector loop, websocket, or automatic restart/backfill process yet. Store tests cover identical-observation deduplication across store restart and append-only correction history; CI run 148 passed at commit `c702afa21fac9dfa5a31b18463cc4429aa4adc71`.


The Morning Brief endpoint now persists the observed SPY/QQQ/IWM bars it fetches and reports each benchmark's observed first-to-last window change when enough bars exist. It still does not calculate sector breadth, catalyst/news, scanner movers, validated regime, Red Zone eligibility, historical probabilities, or Boring Overnight qualification.


### Latest verification checkpoint

- GitHub Actions run 151 completed successfully at `ae6ee7acc30e85a6268b5ea8fc40d86429d3461b`: solution restore, Release build, and test suite passed. Run: https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40012059701.
- This is build/test verification only. Live API requests, browser rendering, account entitlements, market-data licensing, and parity with installed Thinkorswim studies remain unverified.


## Account dependencies and next work — 2026-10-23

- User confirmed the solution builds and runs locally. Do not ask them to repeat that setup.
- New canonical checklist: `docs/Next-Steps-and-User-Inputs.md`. It distinguishes free-account/live smoke-test needs, user-supplied TOS evidence, and offline implementation tasks.
- **Do not ask the user to buy both providers.** Neither account is required for provider-neutral coding, fixture tests, storage, collector lifecycle, or most Morning Brief contracts. One free account is enough to start a live smoke test; both are useful for direct side-by-side comparison. Paid plans remain on hold pending proof of fit and licensing/entitlement checks.
- Corrected stale To-Do/provider/API README entries: historical REST adapters and append-only JSONL raw-bar storage exist. Latest verified CI run 151 passed at code head `90ca6234a8bafc9230b6e4ef1996e569682c90a1`. Later documentation-only commits update dependency/status notes; check the latest Actions run before asserting current HEAD CI.
- Remaining work independent of accounts: pagination and resilient HTTP behavior; hosted collector/health lifecycle; streaming abstraction; reconnect/backfill/restart; raw-store corruption/retention/error health; fixture-based Morning Brief, scanner, and Boring Overnight pipelines; session-aware gap semantics; ThinkScript parity.
- User evidence that still matters: XRPN 5M transition screenshot per `docs/Validation/5M-to-1M-User-Validation-Steps.md`; UPC 09:16–09:36 continuous bars/exact timestamp inspection only if that historical gap question still needs resolution; review dashboard through API-hosted browser. None blocks offline engineering.
- Morning Brief remains only SPY/QQQ/IWM historical 5M observations plus explicit unknown placeholders. Do not claim broad movers, sector/breadth, news, validated regime, Red Zone or Boring Overnight are implemented.


## Checkpoint update — October 2 XRPN pair received; Alpaca smoke-test runbook — 2026-10-26

### XRPN October 2 OHLCV

- [DONE — OHLCV EXPORT RECEIVED] data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv is a genuine XRPN 1M export, not the previously misnamed HPK file. Report header: XRPN, 2026-10-02, 06:32–13:02 Arizona display range. Embedded payload has 391 consecutive minute timestamps from 06:30 through 13:00 Arizona (13:30–20:00 UTC); no missing one-minute payload timestamps were found.
- [DONE — TIMEFRAME CROSS-CHECK] Matched against data/StrategyReports_XRPN_10022026_5M_EntireDay.csv: 79 5M payload rows, same first/last payload timestamps. Of the 79 5M bars, 78 have a complete five-row 1M group; all 78 aggregated OHLCV groups match the corresponding 5M payload within numeric precision. The final 5M timestamp has no subsequent five-minute 1M group because both exports end at 13:00 Arizona. This validates exported OHLCV alignment for shared windows, not installed-study state parity.
- [BLOCKED — INTERNAL STUDY DIAGNOSTICS] The two repository CSVs expose the MCC_OHLCV transport payload only. No per-bar Permission, block-code, candidate/trigger, extension-danger, READY activation/expiration, re-arm, or expiration-state fields were found. Data not found in sources. If those diagnostics were uploaded separately, confirm their exact repository path; otherwise export the diagnostic study output for both timeframes.
- Keep the prior October 2 screenshot; do not ask the user to re-upload it. Pair its observed Permission revocation with the newly validated OHLCV only after same-bar diagnostic fields are available. Do not infer gate-state root cause from OHLCV alone.

### Overnight Scanner screenshot — 2026-10-26

- Screenshot reviewed: Screenshot 2026-10-26 143847.png. It is a ranked scanner view, not proof of Boring Overnight eligibility. Initial chart-review shortlist prioritizes lower displayed ON movement / steadier values over the highest raw Overnight Score: TTE, RTO, NNN, OKE, AHR, FMS, with CCI as a secondary comparison. Exclude BOLT and VISN from the first boringness batch because their displayed current ON moves are 6.22% and 33.8%, respectively; the screenshot alone cannot establish catalyst clearance, liquidity gate, source-backed close, or actual fill probability.
- Request same-window 5M chart screenshots with extended-hours bars enabled, covering Monday 2026-10-26 regular session and Tuesday 2026-10-27 from 07:00–20:00 Eastern. Capture the regular-session close and visible time axis for each symbol. Screenshots are visual triage only; quantitative target-touch research still requires timestamped OHLCV export and explicit DATA_GAP rows.

### Alpaca live API smoke test

- The user's saved Alpaca key/secret are the intended credentials for the first provider smoke test. Do not ask them to paste credentials. If /api/providers/status says Alpaca is not configured, re-enter them locally with the commands in src/MomentumCommandCenterV2.API/README.md.
- From the repository root, run: dotnet run --project src/MomentumCommandCenterV2.API. Open /api/health, then /api/providers/status. A configured status is only a configuration check.
- With the API still running, request /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z from the same local host/port printed by ASP.NET Core. This is 09:30–09:50 Eastern on Friday 2026-10-23. Check Alpaca request status, returned bar count, first/latest source timestamps, feed, duplicate timestamps and observed gaps. If Massive is unconfigured, its error is expected and does not invalidate an otherwise successful Alpaca response; record it separately.
- Run tests separately with dotnet test MomentumCommandCenterV2.sln --configuration Release. The user's reported 87/87 passing tests is good offline evidence, but it does not prove live authentication, feed entitlement, correct timestamps, or extended-hours coverage. Never include keys/secrets in the report.
- Share only sanitized results: health/status JSON, HTTP/error messages with tokens removed, provider/feed name, bar count, and source timestamps. Do not call Alpaca production-verified until authentication, entitlement, coverage and timestamp semantics are checked.

### Current branch / release posture

- Feature branch remains substantially diverged from master; do not merge it wholesale. Use scoped file/history reconciliation.
- MCC remains LOCAL PROTOTYPE / DIAGNOSTIC ONLY; live trading reliance is not approved. No orders were submitted by this work.

## [Uncertainties]

- OHLCV alignment is validated for 78 complete five-minute windows; the final endpoint bar has no complete subsequent 1M group by design of the export endpoint.
- Per-bar ThinkScript diagnostic fields were not present in the two XRPN OHLCV report files. Data not found in sources.
- The scanner screenshot has no source-backed news/catalyst or complete liquidity evidence; shortlist is for chart review, not a buy/hold recommendation.
- The local Alpaca credentials are user-reported as saved; actual API authentication has not yet been observed in this conversation.


### Handoff status after documentation edits — 2026-10-26

- Updated this handoff, docs/Next-Steps-and-User-Inputs.md, docs/Master-To-Do.md, docs/Release-Readiness-2026-10-26.md, and src/MomentumCommandCenterV2.API/README.md with the XRPN 1M/5M alignment result, the missing diagnostic-state evidence, the Overnight Scanner shortlist, and the exact Alpaca smoke-test procedure.
- The latest documentation edits have not been independently tested by a current-head GitHub Actions run in this session. Do not describe the latest branch head as CI-green until a run tied to the exact head completes.


## Correction and evidence update — 2026-10-26 (user response)

### Corrected chart-window request

- Retract the prior request to capture 2026-10-26 through 2026-10-28. It was an invalid historical-research instruction because those future sessions had not occurred when the screenshot request was made.
- User is capturing AHR, FMS, and NNN on 2026-10-21 through 2026-10-23. Accept this historical window for initial visual review; do not ask them to retake these screenshots merely to satisfy the previous erroneous range.
- This window provides recent historical shape only. It does not itself include the complete 07:00–20:00 Eastern GTC_EXT window for a subsequent session following each regular close. If initial chart review justifies further research, request a completed historical pair: prior regular-session close plus the entire next 07:00–20:00 Eastern window, with actual session dates explicitly named and all missing intervals preserved as DATA_GAP. Avoid future dates.

### Alpaca smoke test — actual result received

- /api/health returned OK, mode decision-support-only, orderSubmission NOT IMPLEMENTED, timestamp 2026-10-26T22:10:39Z.
- /api/providers/status reports Alpaca NOT_CONFIGURED and Massive NOT_CONFIGURED.
- /api/market-data/compare returned zero bars and zero persisted observations for both providers. Alpaca error: Set MarketData:Alpaca:ApiKey and ApiSecret (or ALPACA_API_KEY / ALPACA_API_SECRET). Massive error: Set MarketData:Massive:ApiKey or MASSIVE_API_KEY.
- Therefore, live-provider smoke test has NOT passed. Endpoint responds, but no provider returned market data.
- User reports dotnet test MomentumCommandCenterV2.sln --configuration Release completed successfully: build succeeded; 87 tests succeeded, 0 failed, 0 skipped. This is test evidence, not live provider evidence.

### Specific local credential fix

The API project UserSecretsId is MomentumCommandCenterV2-API-Local. Credentials saved under a different project/UserSecretsId or Windows account are not automatically visible to this API. In a terminal at repository root, set values locally without printing or sharing them:

1. dotnet user-secrets set "MarketData:Alpaca:ApiKey" "<YOUR_EXISTING_ALPACA_KEY>" --project src/MomentumCommandCenterV2.API
2. dotnet user-secrets set "MarketData:Alpaca:ApiSecret" "<YOUR_EXISTING_ALPACA_SECRET>" --project src/MomentumCommandCenterV2.API

Replace placeholders locally; never paste actual values into chat, source control, or screenshots. Restart API, recheck /api/providers/status, then rerun the same /api/market-data/compare request. If configured remains false, check same machine/user and target project. Do not create or buy Massive credentials for this test.

### Current user-reported status

- XRPN Oct 2 1M/5M OHLCV pair: aligned for 78 complete 5M windows; per-bar internal study diagnostics still missing from reviewed files.
- Overnight chart screenshots: user will provide AHR, FMS, NNN for Oct 21–23.
- Alpaca: credentials are not visible to this API process yet; actual authentication/data retrieval remains blocked.
- Tests: 87/87 passed according to user's terminal output.
- No live trading or order submission occurred.

## [Uncertainties]

- The reason saved Alpaca credentials are not visible is unknown until project-specific User Secrets are checked locally.
- The 10/21–10/23 chart screenshots may reveal visual behavior, but exact OHLCV coverage, GTC_EXT reach/touch rates, and actual fills cannot be established from screenshots alone.
- XRPN diagnostic fields may have been uploaded separately, but their exact path is not identified in reviewed branch files.


## Final Overnight review and Visual Studio credential update — 2026-10-26

### Screenshot date mismatch — do not mislabel evidence

- Three Project Sources screenshots were visually inspected: AHR, FMS, NNN.
- All three visible chart headers show 2026-10-19 through 2026-10-21, not the intended 2026-10-21 through 2026-10-23. Treat these screenshots only as a provisional visual read of Oct 19–21. Do not report them as the Final Analysis of the intended Oct 21–23 sample and do not infer the missing two dates.
- User had already been asked to capture Oct 21–23 after the assistant's earlier invalid future-date request. Do not ask the user to repeat work because of the assistant's mistake; ask only for the correct-date screenshots if they are not already present elsewhere in Project Sources.
- A chart screenshot can support qualitative visual observations, but not full GTC_EXT eligible-window coverage, exact target touch rates, actual fills, or a statistically validated target recommendation.

### Provisional chart observations (visual only; dates shown are Oct 19–21)

- **AHR:** The chart shows an early-session rise/spike toward the displayed high near 54.76, followed by a broad fade and lower prices into the right edge. The 5M status ribbon at the right edge reads 5M STATE: GET OUT, 5M PERMISSION: NO, bearish structure, extension about 1.71 ATR, and EOD GET OUT. This is consistent with deterioration by the displayed endpoint, not proof of overnight behavior or a sell-fill outcome.
- **FMS:** The chart shows a sharp downward gap/flush around the Tue boundary from roughly 22.0 to a low near 20.91, followed by a partial recovery and then relatively flat/lower consolidation near 21.5–21.7. The right-edge ribbon shows 5M STATE: GET OUT, 5M PERMISSION: NO, transitioning structure, RSI around 75.8 and extension around 6.46 ATR. The large gap and extreme displayed extension make this a poor example of a “boring” overnight without event/price-context investigation.
- **NNN:** The chart shows a rise from roughly 40.5 into a spike/high around 41.67, then a sustained fade toward about 40.6 by the right edge. The right-edge ribbon still shows permission YES / runner structure and rising runner momentum while extension is around 2.53 ATR and EOD is active. This demonstrates that a persistent 5M permission/runner state can coexist with intraday price deterioration and elevated extension; it does not prove overnight suitability.
- These observations are intentionally descriptive. The screenshots' exact dates/session boundaries and visual scale are not a substitute for exported bars and reference closes. No target-touch percentage is derived from them.

### Alpaca credentials and Visual Studio launch context

- User reports both MarketData:Alpaca:ApiKey and MarketData:Alpaca:ApiSecret were successfully saved to a .NET User Secrets store.
- User runs the application through Visual Studio and navigates to localhost endpoints; they are not launching the app with the previously provided dotnet run command.
- The next live-provider check must be made against the same Visual Studio-launched API process. User Secrets are loaded for the API project's UserSecretsId only when the app runs in Development configuration (the usual Visual Studio profile, but verify the launch profile/environment rather than assume).
- Stop/restart and re-run the API from Visual Studio after saving the secrets so the process reloads configuration. Then open /api/providers/status and /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z on the same localhost port.
- Success requires actual returned bars and source timestamps, not just configured: true. If still unconfigured, check the API project's exact UserSecretsId, Visual Studio launch profile's environment, and whether the secrets were set under the same Windows user. Do not share credentials or secret-store output containing values.
- Massive can remain unconfigured for the initial test.

### Immediate status / next steps

1. Assistant: keep the three chart screenshots marked as Oct 19–21; do not overstate them as Oct 21–23.
2. User: if Oct 21–23 screenshots exist in Project Sources, identify/attach the exact correct three files; otherwise provide the intended-date screenshots when convenient. No need to re-capture anything just to satisfy the earlier invalid date request.
3. User: restart the API in Visual Studio, verify /api/providers/status, then inspect /api/market-data/compare for real bars and source timestamps; send sanitized output only.
4. Assistant: after correct-date evidence and provider response are available, complete the date-correct visual review and then pursue full-window OHLCV / GTC_EXT target-touch research. Keep the launch recommendation blocked until evidence supports it.
5. Do not change the +0.5% reference target or claim it is optimized based on these screenshots. It remains a heuristic until a verified sample of full eligible windows is analyzed.

## [Uncertainties]

- Data not found in sources for the intended Oct 21–23 chart screenshots in the files inspected in this turn; the three inspected images visibly show Oct 19–21.
- It is not yet known whether the correct screenshots exist elsewhere in Project Sources but were not returned by the available file search.
- The visual chart endpoint status is not an independent provider/data export and does not establish order fills, catalyst clearance, or overnight returns.
- Whether Visual Studio is using Development and the same user-secrets profile remains to be confirmed from the actual running process.


## User correction, TTE/FMS chart review, and Alpaca validation — 2026-10-26

### Historical Overnight screenshots — accept user's date clarification

- User explicitly confirms the three AHR/FMS/NNN screenshots are for 2026-10-21 through 2026-10-23. Accept that user-provided date range as the intended study window; the previous response misread/misinterpreted the screenshots and incorrectly challenged the dates. Do not ask the user to re-provide those three images.
- Final analysis should use those screenshots as the 10/21–10/23 review set, with the limitation that screenshots are visual evidence and cannot establish exact target-touch rates or fills.

### Today's 1M chart screenshots — TTE and FMS

- TTE screenshot (2026-10-26): 5M ribbon shows `SELL - CONFIRMED WEAKNESS`, Permission NO, bearish structure, RSI about 53.1, extension about 3.58 ATR, EOD GET OUT. The lower 1M chart has no visible event bubbles in the displayed window, although state/metric labels are present.
- FMS screenshot (2026-10-26): 5M ribbon shows SELL/BREAKDOWN, Permission NO, bearish structure, RSI about 46.6, extension about 8.54 ATR, with very elevated displayed RVOL (about 15.47). The 1M chart has many historical event bubbles, including PREPARE TO SELL, SELL, EXECUTION EXPIRED, and extension-danger/candidate diagnostics. The right-edge 1M state is PREPARE TO SELL.
- No-bubble interpretation: absence of bubbles is not a HOLD or SELL signal by itself. In the Library's V14.5 1M source, `showStateBubbles` defaults to yes, but bubbles are plotted only for specific state/event transitions (e.g. PREPARE TO BUY, candidate events, READY, EXECUTION EXPIRED, PREPARE TO SELL, SELL); there is no generic per-bar bubble for every state and no dedicated GET OUT/EOD bubble in the inspected bubble section. Thus no qualifying bubble-producing transition in the displayed interval can produce a blank chart even while right-edge labels show a bearish/GET OUT state. However, because FMS displays many bubbles while TTE displays none, the screenshot alone cannot distinguish (a) TTE had no bubble-producing events, (b) `showStateBubbles` is off on TTE, or (c) the installed TTE study/version differs from FMS. Do not assert a single cause without checking the TTE study inputs/version.
- User walkthrough: on TTE's lower 1M pane, open Studies/Edit Studies, select the 1M Command Center study (not the 5M study), open its gear/settings, confirm `showStateBubbles = yes`, apply. Compare the exact 1M study name/version and inputs on TTE and FMS. If already yes and versions/settings match, do not change trading logic; report this and then use a temporary targeted diagnostic view to inspect state transitions. Do not change the 5M authority logic as a response to missing 1M bubbles.
- Caution: displayed extension is elevated on both; TTE ~3.58 ATR and FMS ~8.54 ATR. Their 5M permission is NO and structures are bearish. These are not fresh long-entry approvals. This is chart-state interpretation, not a personal position-management instruction.

### Alpaca smoke test — PASS for historical IEX bar retrieval

- User's Visual Studio-hosted API returned Alpaca configured true and status `CONFIGURED_NOT_YET_VALIDATED` from `/api/providers/status`.
- User's `/api/market-data/compare` request for SPY 5M from 2026-10-23 13:30Z to 13:50Z returned 5 Alpaca bars, 5 persisted observations, feed `iex`, first timestamp 13:30Z, latest timestamp 13:50Z, zero duplicate timestamps, zero reported gaps, and no error. This is actual successful historical market-data retrieval for that requested window and validates the configured credentials/feed path. It does not validate every symbol, every historical window, live streaming, SIP entitlements, or overnight-session coverage.
- Massive remains NOT_CONFIGURED; no need to configure it for this first successful Alpaca smoke test.
- Code review found `/api/providers/status` hardcoded `CONFIGURED_NOT_YET_VALIDATED` whenever credentials existed, independent of successful `/api/market-data/compare` requests. This made the status stale/misleading after successful data retrieval.
- Implemented a feature-branch fix: provider adapters now remember the timestamp and bar count of the last successful non-empty data response; `/api/providers/status` now reports `VALIDATED_DATA_RETURNED` when such a response has occurred in the current API process, and exposes `validated`, `lastSuccessfulDataValidationUtc`, and `lastSuccessfulDataValidationBarCount`. Empty successful responses do not mark validation. This is process-local state and resets when the API restarts; it is not a persistent health history.
- Added an Alpaca unit test for empty successful response not counting as data validation. The exact latest commit's CI status has not yet been verified; do not claim tests passed for these new code changes until GitHub Actions/local Release tests finish.

### Current next steps

1. User: check TTE lower 1M study's `showStateBubbles` setting and compare its exact study/version with FMS; return the setting/version result if bubbles remain absent.
2. User: after the status endpoint code update is available in the running build, restart the API in Visual Studio and repeat a small SPY compare request. `/api/providers/status` should then report `VALIDATED_DATA_RETURNED` after a successful non-empty response; if not, confirm the updated branch/build is actually running.
3. Assistant: check CI on the updated branch and address failures before promoting code.
4. Continue the date-correct 10/21–10/23 Overnight analysis from the three existing screenshots; do not request a duplicate upload.

## [Uncertainties]

- Exact TTE 1M study version and `showStateBubbles` input are not visible in the screenshot; these are the main unresolved causes of the blank bubble display.
- The provider validation state is intentionally in-memory and process-local; no durable provider-health history has been implemented.
- The successful SPY test proves the requested historical IEX bars were returned, not SIP entitlement, real-time stream, all symbols, or full GTC_EXT coverage.


## 2026-10-26 — CCI/NICE review, date-correct screenshot review, and 1M EOD bubble fix

### Historical screenshot set — COMPLETE for visual triage
- User confirms the three AHR/FMS/NNN screenshots represent the 2026-10-21–2026-10-23 sample. Accept the correction; do not ask for re-upload. Full visual notes are in docs/Validation/Overnight-Candidate-Review-20261026.md.
- AHR: visible spike then fade; right edge showed GET OUT / 5M Permission NO. Not boring at that snapshot.
- FMS: 5M Permission NO and EOD GET OUT; 1M extension about 6.45 ATR. Abnormally extended for a consistency-first overnight hold.
- NNN: best 5M permission of the three at the visible right edge (Permission YES / runner regime / execution environment YES), but 1M PREPARE and extension about 2.53 ATR. Scanner Boringness score 55.23 is below 60, so this is not a Boring Overnight READY.
- Screenshot-only triage is complete. Quantitative overnight target-touch research is still blocked on source-backed prior regular close and complete subsequent 07:00–20:00 Eastern GTC_EXT bars; missing intervals remain DATA_GAP and a touch is not a fill.

### New candidates CCI and NICE — WATCH, neither READY
- Scanner columns: CCI score 68.11 (AGGRESSIVE), ON Win 60%, AVG ON 1.24%, median 0.08%, 5D 2.73%, current ON 0.03%. Mean/median divergence warns that a few large gaps may dominate the average. CCI chart: 5M Permission NO, neutral structure, bullish regime not established, RVOL 0.04, extension 1.23 ATR.
- NICE score 60.47 (AGGRESSIVE), ON Win 75%, AVG ON 0.70%, median 0.60%, 5D 0.70%, current ON 0.55%. Rounded 5D and average values are equal; verify unrounded values against the strict 5D > 20D hard gate. NICE chart: 5M Permission YES but neutral structure, RVOL 9.48, extension 0.45 ATR, execution environment NO; price is below displayed VWAP/EMA9/EMA20/SMA50. Neither candidate qualifies from supplied evidence.
- Catalyst state and 20-day average daily share volume are not shown. Keep catalyst UNKNOWN; do not label READY. Full analysis and values are in docs/Validation/Overnight-Candidate-Review-20261026.md.

### TTE 1M bubble root cause and source fix
- User confirmed TTE and FMS use the same study/settings. Source inspection shows V14.5 only creates bubbles on transitions/events; the final EOD_GET_OUT state was label-only and was not included in stateBubbleEvent. Therefore an EOD/GET OUT label could appear without a bubble. This is a source-level omission, not evidence of a different 5M permission state.
- Updated docs/Charts/1M/MainStudy.txt to emit one red “EOD GET OUT” bubble on the transition into EOD_GET_OUT. This does not change entry gates, 5M authority, or state calculations; it only makes the EOD state transition visible. Source file is not automatically installed in Thinkorswim.
- User action: paste/update the 1M study from the repository source in Thinkorswim, save/apply it to TTE, then confirm one EOD GET OUT bubble appears when the chart enters the EOD state. Do not edit the 5M study. If other event bubbles remain absent outside EOD, use showHistoricalDebugBubbles=yes only for a tightly zoomed diagnostic window; turn it back off after debugging.

### Alpaca status and CI
- User's actual SPY 5M IEX compare returned 5 bars and 5 persisted observations for 2026-10-23 13:30Z–13:50Z, with zero duplicate timestamps, zero reported gaps and no error. The status endpoint then correctly returned VALIDATED_DATA_RETURNED after the provider-status code update; validation is process-local and resets on API restart.
- GitHub Actions .NET build/test workflow passed on commit 4f5ed265e54e9a924935a58b8b3233b31b264279 (run 40414827751). User also reports 88 tests passed. The subsequent ThinkScript text edit and documentation commits still require a current-head CI check; .NET CI cannot compile/validate ThinkScript semantics.
- Massive remains unconfigured and is not needed for the initial Alpaca path.

### Tonight / this week
- No supplied candidate is fully Boring Overnight READY. For tonight, only continue with manual source-backed catalyst checks and exact scanner hard gates; if any required field is unknown, choose WATCH / zero position.
- Near-term MCC work: current-head CI after the EOD bubble edit; install/verify the 1M EOD bubble; finish provider pagination/retries, collector lifecycle/recovery, durable data-quality states, Morning Brief and scanner contracts, and fixture-based Overnight calculations. Live stream, catalyst/news coverage, full GTC_EXT history, target-touch study, study parity, and realized outcomes remain release blockers.

## [Uncertainties]
- Exact unrounded scanner calculations, CCI/NICE daily average volume and catalyst status are not supplied.
- The user's date range for AHR/FMS/NNN is accepted, but visible chart labels/axes do not align perfectly; verify per-bar timestamps before quantitative research.
- No full GTC_EXT bars or actual fills for CCI/NICE/AHR/FMS/NNN were found.
- Thinkorswim installation of the updated 1M study must be done by the user and confirmed visually.


### CCI/NICE catalyst lookup — 2026-10-26
- CCI: company source confirms Q3 2026 results and full-year outlook update were released Oct 21 after close. Treat as a recent earnings/outlook event; review post-earnings volatility before any boring overnight classification. Source: https://crowncastle.gcs-web.com/news-releases/news-release-details/crown-castle-reports-third-quarter-2026-results-and-updates.
- NICE: official investor-relations event page currently lists no upcoming events, but that does not prove absence of company news or catalyst risk. Source: https://www.nice.com/investors.
- Current outcome remains WATCH / NOT READY for both. See docs/Validation/Overnight-Candidate-Review-20261026.md for values and caveats.


## Update — 2026-10-27 (API smoke test, Swagger, Boring Overnight, TOS bubble)

- User independently ran `GET /api/health`: status OK, decision-support-only, order submission NOT IMPLEMENTED.
- User independently ran `/api/providers/status`: Alpaca `VALIDATED_DATA_RETURNED`, last successful response 18 bars. This means a non-empty data response was returned during that API process; it is not proof of streaming or full coverage.
- User ran `/api/market-data/compare` for SPY 5m on 2026-10-23 13:30Z–13:50Z: Alpaca returned 5 IEX bars, timestamps 13:30Z and 13:50Z, no duplicates/gaps/error, but `persistedObservationCount=0` in this latest response. Massive remains unconfigured. Investigate why the API returns zero newly persisted observations (could be idempotent already-seen bars; verify store semantics before treating as a defect).
- User reports `dotnet test MomentumCommandCenterV2.sln --configuration Release`: 88 passed. This is user-reported test evidence; check CI against exact branch head after the latest commits.
- Added Swashbuckle and Swagger UI at `/swagger`; documented usage in `docs/API-Swagger-Workflow.md`. Local launch: `dotnet run --project src/MomentumCommandCenterV2.API --configuration Release`; open `/swagger` on the printed origin.
- Added `POST /api/boring-overnight/evaluate` plus deterministic hard-gate evaluator and unit tests. This is not yet connected to live scanner inputs. Missing critical data/catalyst/permission yields DATA?; hard failures BLOCKED; extension/current-day heat warnings WATCH. No orders are submitted.
- Limit target analysis is in `docs/Validation/Boring-Overnight-Target-Policy-20261027.md`. Do not declare +0.5% optimal or use median ON % as a fill probability without complete 07:00–20:00 Eastern GTC_EXT bars, walk-forward testing, and actual fill data.
- TOS bubble issue is NOT resolved. User applied the prior change without errors and sees no bubbles. Repository `docs/Charts/1M/MainStudy.txt` is V14.4.10, while the uploaded source reference is `MomentumCommandCenterV3_2_1M_V14_5.txt`. Exact installed source parity is not established; patch the exact V14.5 source and test on a historical EOD transition rather than re-editing the older reference blindly.
- Alpaca proof-of-fit remains historical REST only. No websocket, broad scanner/movers, or full GTC_EXT collector is implemented. Keep Massive deferred.
- Do not merge the divergent feature branch wholesale into master. Use a scoped PR after current-head CI and code review.


- Additional TOS follow-up: generated a corrected copy of the uploaded V14.5 source as `/mnt/data/MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It adds a one-time red EOD GET OUT bubble when `finalState` transitions into `EOD_GET_OUT`. This file is a local generated artifact, not yet committed to the repository. User must install this exact V14.5 file into the lower 1M study and inspect a chart containing the EOD transition; if the chart still shows no bubbles, collect a screenshot including the prior bar and confirm the installed study source/version. This remains unverified in TOS.


## Update — 2026-10-27: StrategyReport API, exclusions, Swagger examples, ThinkScript cleanup

- Latest branch head at handoff update: feature branch contains `POST /api/strategy-report/upload`; it accepts multipart/form-data field `file`, .csv only, maximum 10 MB, parses up to 10,000 valid rows, returns original headers, full parsed rows, first 100 preview rows, row warnings and likely timestamp/symbol/event columns. It does not infer absent bars/fills. Parser tests added; latest CI run must be verified.
- Added MCC local exclusion watchlist endpoints: `GET /api/watchlists/exclusions`, `POST /api/watchlists/exclusions` with `{ "symbol": "TTE", "reason": "Manual exclusion" }`, and `DELETE /api/watchlists/exclusions/TTE`. Persisted at API `App_Data/exclusions-watchlist.json`. Boring Overnight evaluation now reads this list and overrides caller-supplied `excluded` state.
- Important limitation: these endpoints do NOT sync to the Thinkorswim personal watchlist. No supported TOS API for personal watchlist CRUD has been verified in sources. Treat MCC's list as separate until export/import or another supported integration is established.
- Swagger is now enabled outside Development too, at `/swagger`, with example descriptions and a multipart file picker schema for StrategyReport. Only expose on a trusted local network; no authentication is implemented.
- The cleaned V14.5 ThinkScript file is available in the conversation attachment `MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It reflows statements, comments wrapped prose that was being parsed as code, and adds a red EOD transition bubble. Not TOS-compiled/visually verified; user reported earlier file errors and no bubbles. Test this exact file, don't assume success.
- User-reported prior local Release test: 91 passed before this new change set. GitHub Actions run 40538766766 for the current head was queued at the last check; no current-head pass claim.


## Final UI/CI checkpoint — 2026-10-27

- Added visible API-backed panels to the dashboard: MCC exclusion watchlist (add/remove/refresh) and StrategyReport CSV upload (parse, show count/columns/warnings/preview, download complete parsed JSON). Existing browser-local "Working candidates" list is separate and not the exclusion list.
- Swagger endpoint descriptions contain examples; Boring Overnight evaluator and exclusion add request bodies have OpenAPI examples, and StrategyReport upload exposes a multipart file picker schema. Swagger remains unauthenticated; keep API bound to localhost/trusted network only.
- First workflow run for the API changes failed at build because `WithOpenApi` extension was not referenced. Added `Microsoft.AspNetCore.OpenApi` package 8.0.0. Latest workflow run 40539484300 for commit `2c811c5da83cb5e807e9f95ef81eae05cb152de8` was in progress at last check. Verify this exact head after the next commit; do not reuse the user's earlier 91-test result as current-head proof.
- StrategyReport API returns all parsed rows up to 10,000 plus a 100-row preview. Parser unit tests were added. The front-end downloads the full response as JSON to avoid forcing a large report into the page.
- Exclusion watchlist writes `App_Data/exclusions-watchlist.json`; ensure local directory is writable and back it up if exclusions are important. This does not sync to TOS. User still needs to maintain TOS and MCC lists consistently or provide an export format for import.
- Cleaned ThinkScript file download: `MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt`. It removes blank-line bloat and fixes wrapped header prose that caused ThinkScript to parse fragments as code, plus adds the EOD transition bubble. Not TOS-compiled or visually verified.


## 2026-10-27 — StrategyReport parity conversion and Alpaca reliability follow-up

- User fixed the previously broken commit. Their fix commit was `0caf00004a837e269c175f7d4c34827120ac8fe6` (“Fixed broken commit”); GitHub Actions run 40541586544 succeeded for that exact SHA. This was before the latest changes described below.
- StrategyReport upload now attempts an exact MCC Chart Parity Template conversion for rows containing explicit `MCC_OHLCV|v1|symbol|timeframe|unixMillis|open|high|low|close|volume` payloads. It returns `parityRows` with the 17 template columns and the dashboard offers “Download Parity Template CSV”. Non-MCC StrategyReport rows remain available in the ordinary parsed data and are not invented as chart OHLCV. A regression test has been added.
- Alpaca historical adapter now has bounded next-page-token pagination, up to 100 pages, 3 attempts for transient network/timeout/429/5xx errors, requested-window timestamp filtering, and timestamp deduplication.
- Added `GET /api/market-data/bars?symbol=TTE&timeframe=1m&from=...Z&to=...Z&providerName=Alpaca` to fetch normalized source bars, persist new observations, and report feed, timestamps, gaps, and newly persisted count. 31-day maximum. This is request-driven historical retrieval, not a background collector or websocket.
- CI caught a compile error because the result record was not updated with `ParityRows`; this was corrected in commit `dfaa0fa50e1aa85e3953c251a328ec68c2b46655`, and the regression test commit is `25f2b7eec9fc5a6289c23f29cc91fcb4534f95de`. Workflow run 40545469124 was queued at the last check. Verify current head before pulling; do not rely on the earlier green run.
- Remaining implementation: background collector and restart recovery, persistence/cursor watermark, session-aware coverage/gaps, real scanner candidate source, live computed daily/overnight statistics with explicit formula, and 5M/1M TOS state input. Do not claim live Boring Overnight qualification until required inputs are available and validated. Alpaca OHLCV alone cannot truthfully reconstruct the authoritative Thinkorswim 5M permission and 1M state.
- TTE duplicate bubbles: do not patch a different older study as though it were the installed V14.5. Latest installed V14.5 source/screenshot has not been located in the repo's current tracked study path. Existing V14.4.10 source already uses a composite state bubble plus optional debug bubbles; stacking likely comes from multiple enabled bubble-producing paths or multiple event sources on the same bar. Need exact current V14.5 source to safely consolidate every active AddChartBubble call into a single event bubble per bar and assign a non-overlapping vertical offset. User's uploaded Project Sources screenshot was not directly available from repository search.


### Background collection addition — 2026-10-27

- Added opt-in `MarketDataCollectionWorker` registered as a hosted service. It is disabled unless `MarketData:Collector:Symbols` is configured with comma/semicolon-separated symbols. Default poll interval 60 seconds; default overlapping lookback 20 minutes. It fetches 1m and 5m bars from configured providers, persists through the idempotent JSONL store, and retries the overlap after process restart.
- New status endpoint: `GET /api/market-data/collector/status`. Configure `MarketData:Collector:Symbols=SPY,QQQ,TTE,FMS` in local User Secrets or environment variables. The collector does not discover symbols, run news/catalyst checks, or recreate the Thinkorswim study state. It is an explicit-symbol ingestion worker, not the complete overnight scanner.
- Latest CI run for registration commit `4412e08941382a2d931bdfe4416a11ecbe0982f8` was in progress at last check; verify the newest head before pull.


### TTE bubble-stack fix artifact — 2026-10-27

- Created local artifact `V14_5_Bubble_Stack_Replacement.txt` (conversation file) based on the V14.5 state/event names in the Library copy. It replaces multiple independent event bubbles with one composite bubble per bar and includes EOD transition text, so simultaneous candidate/ready/blocked events cannot print over each other as separate bubbles.
- This is a replacement section, not a full regenerated study. The Library V14.5 copy is older and has line-wrapping/comment corruption; do not overwrite the user's cleaned current V14.5 file with that stale full copy. Replace the entire state-bubble section in the current cleaned source, remove any separate EOD-only AddChartBubble, and verify in Thinkorswim. Not compiled/visually verified in TOS.
- CI for the collector first failed on an unused constructor parameter. Removed it in commit `72d4034468d0837c38a4baf726e43ca68ad9f7bf`; current workflow 40546245830 was in progress at last check.

## Current handoff — 2026-10-27 (after user-fixed commit)

### Latest user fix inspected
The user-fixed commit is 0caf00004a837e269c175f7d4c34827120ac8fe6 (“Fixed broken commit”). Its diff adds the missing Swashbuckle.AspNetCore.SwaggerGen import to API Program.cs and Xunit to parser tests. The prior handoff records workflow 40541586544 green for that SHA. That validates only the earlier commit, not newer work.

### Assistant changes on feature/opportunity-quality-classification
- Reworked StrategyReport parsing for the actual Thinkorswim format (preamble + semicolon table) and conversion of explicit MCC_OHLCV v1 payloads into the exact 17-column parity template. Handles comma-grouped epoch timestamps and Windows CRLF offsets. Added regression tests for real-format exports and no-payload non-fabrication.
- Added durable collector watermarks per provider/symbol/timeframe, overlap recovery, atomic checkpoint saves and bounded 30-day catch-up chunks after downtime. Collector status exposes checkpoint/watermark information.
- Corrected collector newly-persisted count to increment only by the store append count.
- New commits: 1c0b54e parser rewrite; bf62d3b parser tests; c7acc54 durable collector watermarks; d1af047 persistence counter; a69771c CRLF offset fix. Verify exact current head/CI before trusting or pulling; current-head workflow status has not been verified.

### PR status
No open PRs were returned by the current open-PR search. PR #7 is closed and must not be merged. Re-query before creating a new PR.

### What is not complete
- Collector is historical REST polling, not websocket streaming. Test checkpoint persistence/corruption, filesystem failures, restarts, cancellation and real provider recovery.
- Alpaca pagination/retries are present. Extended-hours completeness still needs a source-matched premarket/RTH/postmarket test with the configured feed.
- Gap counts are timestamp gaps, not session-aware missing-bar classifications.
- Provider-backed scanner/Boring Overnight is not complete: evaluator accepts a caller-supplied snapshot and cannot source verified daily/overnight metrics, catalyst status, authoritative 5M permission/structure or 1M state by itself. Do not infer those states from OHLCV; missing critical values remain DATA?.
- Latest source/test changes need a green current-head workflow and local Release tests before they are considered validated.

### User's specific next actions
1. Verify GitHub Actions for exact feature branch head; then pull and run dotnet test MomentumCommandCenterV2.sln --configuration Release. Report tested SHA and counts.
2. Upload a committed real-format StrategyReport to Swagger and verify non-empty parityRows with exactly 17 template columns and correct values.
3. Configure collector symbols locally, inspect /api/market-data/collector/status, restart the API, and verify watermarks resume and new persisted counts remain idempotent.
4. Test a known symbol/date across premarket, RTH and postmarket using the configured feed; compare source timestamps to TOS export.
5. Keep scanner DATA? for missing catalyst/TOS-state/daily metrics until those sources are wired and validated.

- [PARITY SEMANTICS CORRECTION] StrategyReport `Date/Time` represents an order event, not guaranteed chart-bar time. The parser deliberately leaves `ChartTimestamp` blank unless an explicit chart timestamp field is present and documents the reason in `ChartTimestampMeaning` / `Finding`; a regression test enforces this. Latest parser/test commits: `a8a2568`, `13f1c83`.


## User validation follow-up — 2026-10-27

- User's Release tests exposed 2 failing parser tests. Root cause: newly added test fixture strings used escaped backslash-n sequences (literal text) instead of actual newline escapes. Corrected the test fixture and normalized CSV line endings before computing header offsets and slicing the original content. Fix commits: e494810 (header parsing), 3e8f9c4 (fixture newline escapes), f617743 (normalization before slicing). Tests have not yet been rerun; do not mark them green.
- User supplied live collector status: Alpaca configured, 1 cycle, 69 newly persisted observations, 0 cycle errors, watermarks present for SPY/QQQ/TTE/FMS at 1m/5m. This confirms successful historical REST retrieval and persistence for that cycle. It does not establish websocket streaming, complete extended-hours coverage, restart recovery, or scanner integration.
- Dashboard provider card was misleading: initial static HTML said NOT CONNECTED and the JS only updated connection text. Updated frontend/app.js to show the provider's current process-local validation status, latest validation timestamp and bar count, configured Alpaca feed, and collector cycle/new-write/error summary. If API status fetch fails it now says API NOT CONNECTED (static preview or API unavailable). Latest UI commit: 8fc3e84. Need browser verification after current-head CI.
- Extended-hours test instructions: request a completed date for 07:00–20:00 Eastern and compare returned timestamps/bars with independent TOS/export data. For 2026-10-26, query UTC 2026-10-26T11:00:00Z to 2026-10-27T00:00:00Z. Example: /api/market-data/bars?symbol=SPY&timeframe=1m&from=2026-10-26T11%3A00%3A00Z&to=2026-10-27T00%3A00%3A00Z&providerName=Alpaca. Repeat 5m and an individual equity such as TTE. Verify feed/entitlement, bar count, first/latest source timestamps, gap timestamps and values. This test is not complete until compared with a source export.
- Latest workflow/combined status could not be verified with available GitHub wrapper results (the returned empty status/run arrays are inconclusive). Run dotnet test MomentumCommandCenterV2.sln --configuration Release after pulling latest feature branch and check GitHub Actions for exact HEAD SHA.
- Follow-up correctness checks: the parser fixture epoch `1792430880000` resolves to `2026-10-19T17:28:00Z`, so corrected the test expectation (commit `ba5c92f`). Grouped volume values like `8,210` are normalized to `8210` (commit `dab2bc8`). Latest parser/test fixes have not been executed by CI or locally after these commits; the next validation is a Release test run on the exact current feature-branch HEAD.


## October 27, 2026 — handoff update

Working branch remains `feature/opportunity-quality-classification`. Latest observed branch head before these changes was `1012b8c15a615021cc83880dc86556fceafc744a`; new changes add an Alpaca/IEX OHLCV CSV-to-parity parser and tests, plus provider dashboard cache/status corrections. Do not assume the new commits passed CI until the current branch-head workflow completes.

### Current evidence
- Alpaca provider status returned `VALIDATED_DATA_RETURNED` at 17:53–18:26 UTC on 2026-10-27, with configured feed `iex`. It proves some bars were returned; not complete coverage, SIP access, or streaming.
- Collector status: 2 cycles, 36 newly persisted observations, 0 errors in the last reported cycle, durable watermarks for SPY/QQQ/TTE/FMS on 1m/5m. Restart/recovery is not yet tested.
- User's saved coverage responses: SPY 1m = 394 bars/8 observed gaps; TTE 1m = 168 bars/91 observed gaps; TTE 5m = 75 bars/3 observed gaps. Compare exact timestamps and OHLCV against Thinkorswim before declaring errors.
- Added `POST /api/market-data/import-csv` for standard Alpaca/IEX CSV with timestamp/time, open, high, low, close, volume columns. Optional form fields `symbol` and `timeframe` fill absent columns. Output has all 17 parity columns; imported values fill Payload* only. Chart* remains blank and no parity is claimed until a separate chart export is uploaded.
- Dashboard JS now requests provider/collector status without cache and uses a versioned script URL. If still stale after pull, confirm dashboard URL is served by the same API origin, not a file preview or different app instance.

### Scanner blocker
The collector's configured universe is only SPY, QQQ, TTE, FMS; it is not market-wide discovery. No verified movers/scanner feed, catalyst/news data, quote/spread feed, or fully validated overnight scoring is wired in. Next implement a provider-backed discovery adapter and score only fresh candidates with liquidity, spread, event, extension, and session guards. Never relabel the four configured symbols as a full-market scan.

### User's next actions
1. Pull latest branch and run `dotnet test MomentumCommandCenterV2.sln --configuration Release`; confirm CI on the new head.
2. Run the API and open its printed URL (not a local `index.html` file). Hard reload once; check that provider cards say `CONNECTED · DATA RETURNED` and show latest returned bars.
3. In Swagger, test `POST /api/market-data/import-csv` with a small Alpaca/IEX CSV. Include form fields `symbol=TTE` and `timeframe=1m` if missing from CSV; confirm 17 parity columns and blank Chart* fields.
4. In Swagger, retest `POST /api/strategy-report/upload` with the real TOS StrategyReport; confirm explicit payload parity rows and grouped volume.
5. Upload the exact matching TTE Thinkorswim OHLCV/StrategyExport and all six candidate chart screenshots directly in this conversation. Repository PNG bytes were not accessible to the current analysis tools, so no limit-sell levels or candidate ranking have been made up.
6. Run one controlled API restart and verify the collector resumes from watermarks with overlap, without silently losing historical bars.

[Uncertainties]
- New parser/endpoint/test CI status is not yet known.
- The exact cause of the stale dashboard display is not proven; stale browser assets or a different origin/instance are the leading checks.
- Exact TTE parity cannot be assessed until the matching Thinkorswim export is available.
- No overnight shortlist or limit-sell prices are supported without the six chart images and entry/risk assumptions.



### Scanner MVP added after the handoff note
- New `GET /api/scanner/universe` scans a caller-supplied list of 1–50 symbols for a maximum 7-day window, on 1m or 5m, via selected configured provider. It returns observed bar count, first/latest timestamps, latest-bar age, window change %, observed range %, and summed volume; it persists retrieved bars.
- Endpoint is deliberately descriptive. It does not produce candidate-quality probabilities or entry signals and is not market-wide discovery. Relative volume baseline, quote/spread, catalyst/news, halt, sector, and Thinkorswim permission/entry state remain absent.
- SPY 5m capture file `data/HowToTestExtendedHoursCoverage_5m.txt` is empty; the response is Data not found in sources. Recapture it before claiming SPY 5m coverage.


### TTE 1m-to-5m consistency check
The saved TTE IEX response files were compared by UTC five-minute bucket. All 75 saved TTE 5m bars matched the OHLCV aggregation of the corresponding saved TTE 1m rows; 0 mismatches. This only proves internal consistency between these two saved IEX responses, not parity with Thinkorswim. SPY 5m response file was empty and remains missing.


### Dashboard scanner UI and CI status
- The Candidate discovery panel now has an API scan form for a comma-separated user universe, 1m/5m, and a start/end window in browser-local time (sent to API as UTC). Results display observed change, range, volume, freshness, and returned-bar count. It does not discover symbols market-wide or imply a trade signal.
- The first scanner API CI run caught nullable conditional inference errors in the new endpoint; fixed with explicit nullable doubles. GitHub Actions passed build+tests at `7eb26118f360ba0d9cc0b2dbc274ca346bc99240`. Current branch advanced afterward with frontend scanner UI and asset/style updates; latest-head Actions is in progress and still needs a green result.


## 2026-10-27 follow-up — dashboard, import, parity, solution visibility

- Fixed a JavaScript syntax error in the StrategyReport upload handler: a literal `\\n` had been written between statements. This parse error prevented the browser from executing the entire `app.js`, which explains why provider status stayed at “NOT CONNECTED / DATA NOT LOADED” and the scanner submit appeared to do nothing. Updated the script URL to force a fresh download: `app.js?v=20261027-upload-js-fix`.
- Added an explicit Swagger OpenAPI multipart schema to `POST /api/market-data/import-csv`: required binary `file`, optional `symbol`, and optional `timeframe` (1m/5m). Without this, Swagger generated a request body with no upload fields even though the endpoint code accepted multipart form data.
- Added solution folders `Documentation` and `Data and Validation Fixtures` to `MomentumCommandCenterV2.sln`, with the files under `docs/` and `data/` listed as Solution Items. They are reference/test assets, not separate executable .NET projects; keeping them as solution items is the appropriate fit.
- Inspected the saved TTE StrategyReport parse result (77 explicit payload bars) against the saved IEX 5m response (75 bars). 74 timestamps overlap; 3 payload timestamps are absent from IEX; all 74 overlapping bars have at least one OHLCV mismatch. Median absolute close difference is about $0.02, maximum $0.23. Total volume over overlapping timestamps is 705,686.6302 in payload vs 45,441 in IEX (about 15.53x). This is a material parity failure; do not treat IEX-only as interchangeable with the Thinkorswim chart feed. Full method/results: `docs/Validation/Alpaca-IEX-vs-Thinkorswim-TTE-20261026.md`.
- Reviewed six chart screenshots in `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026/`. None shows a clean overnight entry at the captured end-of-day state: BNC has 2.68 ATR extension and no 1m execution environment; RTO has 5m permission but the 1m state is EOD GET OUT / no swing-buy environment; MFM, SXC, SAP, and PANW show 1m/5m GET OUT or permission NO and/or substantial extension. No buy shortlist is justified from these end-of-day snapshots. RTO is the strongest *watch-only* chart of this set, not an entry recommendation; price is around $22.89, with nearby chart reference around $22.96 and session high $23.23, but a limit-sell cannot be set responsibly without a valid entry, next-session setup, and risk/invalidation plan.
- The user's listed red-X Actions runs were older commits that failed at build with CS0173 nullable conditional errors in the scanner endpoint. Those errors were fixed; subsequent workflows were green through the branch head observed before these latest dashboard/Swagger/solution changes. The new commits must complete CI before release.
- Collector recovery UI: Provider comparison > Alpaca card > “Gap / recovery test” displays collector cycles, new observations, and last-cycle errors. It explicitly says restart recovery still needs a controlled test. To verify persistence, capture `/api/market-data/collector/status` before restart, restart the API, then check the same endpoint again: watermarks should remain at/near the previous source timestamps and advance after the next successful cycle. Counters may reset on process restart; the checkpoint file and watermark values are the persistence evidence.


## Update — 2026-10-27: parity import, scanner freshness, and overnight chart re-review

### CSV import failure: root cause found and code changed
- User's 400 response was reproducible from the checked-in fixture shape: `data/TTE_ExportAttempt.csv` is **JSON content**, not delimited CSV. It is an Alpaca historical-bars response with root metadata (`symbol`, `timeframe`, `provider`, `feed`) and a `bars` array. The existing parser searched only for a delimited OHLCV header, so the 400 was expected for this mislabeled JSON export.
- Updated `AlpacaCsvParityParser` to accept both ordinary comma-delimited OHLCV CSV and Alpaca historical-bars JSON responses; added a fixture-based JSON parsing test; clarified the Swagger description. JSON still populates only Payload* values; chart values remain blank until a separate chart export is supplied.
- Relevant commits: parser `3e9f831ba42759657342d78f010b871fda9fa25a`, test `2e95de4b4a8decbd4e02aabab28a684042ddd722`, Swagger description `952215474ad7d8c5144402df1c08b40f67cfc9b6`. These are source commits, not proof of passing tests; CI for the latest head must be checked.
- Endpoint separation: use `POST /api/market-data/import-csv` for Alpaca OHLCV CSV or Alpaca bars JSON. Use `POST /api/strategy-report/upload` for Thinkorswim StrategyReport CSV (the report format begins with `Strategy report` and has semicolon-delimited trade columns). Do not send a StrategyReport to the Alpaca OHLCV parser.
- Follow-up: rename the JSON fixture to `.json` in a later cleanup and update the solution item path, or store an actual CSV fixture with a truthful extension. Current endpoint accepts only a `.csv` filename even when its content is JSON; this compatibility choice is temporary and confusing.

### Scanner output and collector
- User retested scanner: FMS/TTE/QQQ/SPY status is `OBSERVED`, with reported freshness 138.39/138.39/83.39/133.39 minutes. Treat all as stale historical observations, not current candidates. Score alone must never turn these rows into trade-ready decisions. Need an explicit max-freshness gate and a visible `STALE` / `NOT ACTIONABLE` result when exceeded.
- The response itself explains that relative-volume baseline, quote/spread, catalyst, halt, sector, and Thinkorswim eligibility are absent. These are required gaps for market-wide discovery/overnight qualification, not just UI polish.
- Collector status before/after restart shows watermark values unchanged, counters resetting from cycles=6 to cycles=1, newly persisted observations=6, lastCycleErrors=0, recentErrors empty. This is consistent with process restart and continued successful polling. It is not a fully isolated proof that the checkpoint file was read from disk. Verify the exact checkpoint file exists, record its last-write time/content before restart, restart API, compare persisted JSON and endpoint watermarks, then wait for a new eligible bar and confirm watermark advancement. Do not expect source timestamps to advance while the provider returns no newer bars.

### Overnight screenshots: corrected interpretation
- Added `docs/Validation/Overnight-Chart-ReReview-20261027.md` and linked it under the solution's Documentation items.
- RTO 5M is constructive (permission YES, bullish structure/regime, ~0.43 ATR extension, RVOL ~5.16), but its newer 1M panel explicitly ends with GET OUT / 5M permission NO / ~5.7 ATR extension and a large last impulse bar with blocked/expired execution labels. Treat it as a conditional watchlist candidate, not an entry-ready buy. The mismatch is timeframe-specific; neither blindly discard 5M nor override 1M.
- ATR extension is not an automatic veto because the strategy prefers “boring.” It is a risk/chase penalty. Here RTO's 1M extension is extreme and the latest 1M state does not confirm entry.
- MFM: top 5M runner/permission positive but low displayed RVOL (~0.13); lower 1M permission NO, ~2.67 ATR extension and GET OUT. BNC: 5M extension ~2.68 ATR and lower 1M ~10.24 ATR/GET OUT. SXC: 5M permission NO after a high-RVOL spike/fade. SAP: bearish/permission NO/~3.87 ATR. PANW: 5M transitioning/~3.35 ATR, lower 1M permission NO/~5.37 ATR. No screenshot alone qualifies a Boring Overnight BUY.
- Screenshots are historical captures, not current market state. Boring Overnight still requires independent historical overnight stats, daily liquidity, exclusions, fresh prices, and catalyst/news clearance. Data not found in sources for a complete current gate check.

### Solution/project loadability
- Reviewed `MomentumCommandCenterV2.sln`: Documentation and Data and Validation Fixtures are standard Visual Studio Solution Folder entries, not executable projects. Their paths are relative to the solution root and the project-type GUID is the standard Solution Folder GUID. The latest checked-in solution references existing documentation/data files from the repository listing.
- GitHub CI is the authoritative repository build check available here; it does not reproduce a user's Visual Studio local SDK/IDE state. If Visual Studio still shows actual `.csproj` entries as “incompatible” or “cannot load,” capture the exact project names and the first Output > Solution/Project load error. Do not remove valid project references or retarget frameworks without that error. Verify installed .NET 8 SDK and Visual Studio version locally.

### Next actions
1. Pull `feature/opportunity-quality-classification`, stop old API processes, restart the API, and upload `data/TTE_ExportAttempt.csv` to `/api/market-data/import-csv`. Expected: 75 parity rows, Symbol TTE, Timeframe 5m, payload values populated, chart values blank. This expectation follows from the checked-in JSON fixture's reported barCount; the endpoint response must be confirmed by user.
2. Upload an actual Thinkorswim report to `/api/strategy-report/upload`, not the Alpaca import endpoint.
3. Confirm CI status on the latest commit; run `dotnet test MomentumCommandCenterV2.sln --configuration Release` locally.
4. Make stale-data a hard scanner gate before market-wide discovery work; then add universe discovery, quote/spread/liquidity checks, catalyst/news with timestamps, halt/sector context, exclusion integration, and independently validated overnight scoring.
5. For RTO, do not place an overnight order based on these snapshots. Re-evaluate only with fresh 5M/1M data and all Boring Overnight hard gates.



## 2026-10-27 — Scan & Analyze and Arizona-local time entry

### Implemented on `feature/opportunity-quality-classification`
- Renamed the scanner action to **Scan & Analyze (1M + 5M)**. The form now retrieves `GET /api/market-data/bars` twice for each supplied symbol, once with `timeframe=1m` and once with `timeframe=5m`, using the existing Alpaca provider selection. Calls are bounded to 1–25 unique symbols per run and run in batches of three symbols to avoid launching an unbounded burst of requests.
- Each symbol's row reports both timeframe bar counts, first-to-last close change, observed high-low range relative to the first close, summed volume, latest source timestamp and latest-bar age. Per-timeframe errors and empty results remain visible. The results explicitly say `OBSERVED · NOT QUALIFIED` when both timeframes return bars, or `INSUFFICIENT DATA` when either timeframe has no bars.
- The UI accepts and labels start/end as **Arizona local time** and converts those wall-clock values to UTC ISO timestamps before calling the API. Conversion uses `Intl.DateTimeFormat` with `America/Phoenix`, not the workstation's local timezone and not a hard-coded UTC offset. The summary reports both the entered Arizona values and converted UTC boundaries.
- Updated the browser script cache-buster so the API-served dashboard loads the new implementation.
- Commits: `cb6da905c5c9a385c24a7cf4a52e6e662f3a865a` (form labels/identity) and `f2baaa28355eb68e19b300dc501049474f842cbf` (dual-timeframe implementation). These commits are source changes, not proof of a passing CI run or local browser test.

### Contract clarification
- `GET /api/market-data/bars` is the correct existing source for normalized Alpaca OHLCV bars; it returns bars and metadata including provider/feed, requested UTC window, source timestamps, gaps, and persistence count. It does **not** return the internal Thinkorswim 5M Permission or 1M EXECUTION READY state.
- Scan & Analyze is a first integrated data-gathering and descriptive-analysis step. It is not yet the full Boring Overnight qualification pipeline. It does not yet calculate validated relative volume, ATR extension parity, overnight historical win rate/median/5D/20D metrics, quote spread, market-wide discovery, timestamped catalyst clearance, halt/sector context, or verified TOS states. Missing inputs must not be interpreted as passes.
- Freshness is displayed as age; this change deliberately does not invent a universal stale threshold. Qualification needs a documented policy that distinguishes intraday entry freshness from completed-session overnight research.
- The date/time controls are wall-clock values in `America/Phoenix`; API parameters and persisted provider timestamps remain UTC. Verify this on the user's Windows browser because source edits alone do not execute JavaScript.

### User verification sequence
1. Pull `feature/opportunity-quality-classification`; confirm the branch and commit.
2. Run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
3. Start the API and open its hosted dashboard (not `file://` preview).
4. Enter a small universe (e.g. `SPY,QQQ,TTE,FMS`), select a window with known bars, and confirm one row per symbol includes both 1M and 5M summaries.
5. Check a known Arizona time manually: on October 27, 2026, Phoenix is UTC−07:00, so Arizona 09:30 should be sent as 16:30Z. Confirm the status text shows the entered local time and the exact UTC values.
6. Try a symbol with no data or invalid provider conditions and confirm the result is `INSUFFICIENT DATA` or displays a request error rather than claiming qualification.
7. Verify CI for the exact resulting branch head before calling the change released.

### Remaining user input / decisions
- Define the intended historical session window for Boring Overnight (including the chosen after-hours boundary and treatment of incomplete current sessions). The current form defaults to the last six hours in Arizona local time; change this default only after the desired workflow is agreed.
- Confirm the symbol source for each run: manual comma-separated list / local watchlist first; market-wide discovery requires a selected and validated discovery feed.
- Agree on explicit freshness policies by workflow. Do not use the same arbitrary age threshold for an intraday entry and a completed historical overnight analysis.
- Supply/confirm the exact installed Thinkorswim 5M and 1M study versions and matching parity evidence before representing API-derived indicators as equivalent to their state labels.

### Release status
- Source implemented; CI/browser execution for commits above is **not yet verified in this work session**. Local test suite was reported by the user as 100 succeeded before these new changes; that prior run does not cover this implementation.


## 2026-10-27 — To-Do execution pass and Arizona REASON timestamps

### Source change
- Updated `frontend/app.js` so the Scan & Analyze REASON text renders latest 1M and 5M source-bar timestamps in `America/Phoenix`, with an explicit “Arizona” label. The previous summary used `toISOString()`, which displayed UTC. API payloads, provider timestamps, storage, and the UTC request-window audit text remain UTC intentionally.
- Commit: `fc17f9febce848abee86f35bdf99232a4f5172dc`.
- New technical backlog companion: `docs/Implementation-Backlog-Scaffold-20261027.md`. It maps acceptance criteria and test cases across the canonical To-Do areas, distinguishes existing implementation from scaffold and blocked work, and states release gates.
- Canonical To-Do updated with per-workstream disposition, blockers, and post-pull validation steps. Commit: `5646ea90dbb491bc134e4f98c204a019d6f5d7ca`.

### Current disposition
- **Working source baseline:** Scan & Analyze requests both Alpaca 1M and 5M bars for each supplied symbol and now formats latest bar times in Arizona local time in its REASON column. UTC remains the canonical API/storage representation.
- **Not implemented by this pass:** full scanner qualification, market-wide discovery, quote/spread and catalyst feeds, exact ATR-extension parity, TOS Permission/READY state ingestion, validated Boring Overnight historical scoring, complete report persistence/ranking, and production-grade websocket/reconnect/backfill. These are recorded with interfaces and acceptance criteria in the implementation scaffold rather than represented as complete.
- **Study logic:** no changes to 5M Permission or 1M execution gates because existing evidence does not prove a defect; same-bar installed-study diagnostics remain required.
- **External dependencies:** live provider selection/entitlements/credentials; installed TOS source and diagnostics; full eligible overnight OHLCV and session-boundary decision; timestamped catalyst source; freshness policy; exact Visual Studio project-load error if still applicable.
- **Broker boundary:** no Schwab API or order placement; manual entry only.

### User verification after pulling
1. Pull `feature/opportunity-quality-classification` and record `git rev-parse HEAD`.
2. Run `dotnet test MomentumCommandCenterV2.sln --configuration Release`.
3. Start the API and use its hosted dashboard. Scan a known historical window with at least one bar in both timeframes.
4. In the REASON cell, verify each latest timestamp ends with “Arizona” and matches the expected local time. Completion status may still show UTC boundaries; that is intentional for request auditability.
5. Check Arizona conversion on a winter and summer date, including from a browser whose OS timezone is not Arizona.
6. Check the CI workflow attached to the exact head. The previous 100-test user result predates these changes; current-head results are **not verified** by source edits.

### Release status / uncertainty
Source and documentation are committed on the feature branch. Browser execution and current-head CI/test results are not verified here. Do not describe MCC as production/live-trading ready.


## 2026-10-28 — User feedback and sparse-bar scanner correction

### User-confirmed previous-head results
- User pulled the feature branch; API-hosted dashboard and Scan & Analyze work.
- User reports 1M/5M timestamps align, Arizona local timestamps look correct, `dotnet test MomentumCommandCenterV2.sln --configuration Release` passed 100 tests, and CI shows green checks for that prior head.
- The reported “Summer” example was dated 2026-10-26 (autumn), so true July-date validation remains open.

### Source changes in current pass
- Scan & Analyze filters bars with invalid timestamps and sorts observations chronologically before summaries.
- A minimum of two valid bars is required to compute close-to-close change or high-low range. A single observation is no longer treated as a zero-change period.
- Status is `INSUFFICIENT DATA` unless both 1M and 5M return at least two valid bars. Reason text includes one-bar/no-bar warnings and explicitly says change/range require at least two bars.
- Provider Comparison Test Date no longer defaults to a stale fixed date. An empty date field is initialized to today's date in `America/Phoenix`; user-selected dates are not overwritten. The Arizona day end includes the final millisecond.
- Technical backlog and Master To-Do now record acceptance criteria, user feedback, diagnosis boundaries and validation sequence.

### PMI diagnosis
The user's PMI request for 2026-10-26 Arizona 06:02–16:02 returned one bar per timeframe at 06:55 Arizona, volume 120. The previous 0.00% change/range came from comparing the lone bar to itself; it did not establish that the stock was flat. The UI now reports insufficient data and does not calculate those metrics.

**Root cause of the one-bar response remains unknown.** Data not found in sources to prove whether this is ticker support, feed/entitlement, source coverage, API behavior, or an upstream result. Inspect the actual `GET /api/market-data/bars` payload and Provider Comparison metadata before diagnosing.

### Required user validation after pull
1. Re-run PMI over the same 2026-10-26 06:02–16:02 Arizona window; confirm both timeframes show `INSUFFICIENT DATA`, change/range unavailable, and a coverage warning.
2. Inspect API response `provider`, `feed`, `barCount`, first/latest timestamps and `gapCount` for PMI; compare against Provider Comparison.
3. Run a known liquid symbol for the same window to confirm the multi-bar path still computes change/range.
4. Confirm Provider Comparison Test Date defaults to today's Arizona date on initial load and does not overwrite a manually selected date.
5. Test a real July date from a non-Arizona browser timezone. Arizona remains UTC−07:00 year-round.
6. Run Release tests and verify CI on the exact latest source head; the user's 100-test and green-CI confirmation applies to the prior head, not these latest edits.

### Remaining work
The unchecked To-Do items are not abandoned. Continue implementation where source/contract evidence is sufficient. Where the work requires credentials, licensed feed access, an operator decision, installed Thinkorswim diagnostics, a complete overnight dataset, or local machine proof, build the interface/contracts/fail-closed path and test scaffold, then leave the external proof explicitly blocked. Full list and release criteria: `docs/Implementation-Backlog-Scaffold-20261027.md` and `docs/Master-To-Do.md`.


- Follow-up source guard: change now requires finite first/last close values; missing high/low fields are excluded from range inputs. Commit: `286f195f787edde03a2ff7882eb0b68c6bbd0543`.
- Latest source-head CI status is not confirmed: GitHub status lookup returned no status records for that SHA. The user's green CI report applies to the prior head.


## Verification checkpoint — Provider Comparison date and Swagger CI — 2026-10-28

- [DONE — SOURCE COMMITTED] Fixed the Swagger tag mapping to use ordinary `if/else` conditions instead of the invalid switch-pattern arm that failed CI with CS8780. Workflow run [40732946327](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40732946327) had failed in Build; Test was skipped. The user's 100/100 local test result does not supersede that CI failure.
- [DONE — SOURCE COMMITTED] Fixed Provider Comparison date initialization. `providerDate` is an HTML `type=date` input and must receive `YYYY-MM-DD`; the previous code supplied a datetime-local value (`YYYY-MM-DDTHH:mm`) that the browser rejects as invalid, leaving the field blank. Initialization is placed before later dashboard rendering, and the script cache query is bumped to `20261028-provider-date-and-swagger-fix`.
- [PENDING — CI] Verify build and all tests on the new branch head. Do not mark CI green until a new workflow run completes successfully.
- [PENDING — USER RUNTIME] Refresh dashboard and confirm Provider Comparison displays the current Arizona date on initial load and after refresh; verify a manually chosen date is preserved.
- [PENDING — DIAGNOSTIC] BrowserLink warning `$.noConflict is not a function` remains unconfirmed. Compare a run with Visual Studio Browser Link disabled or outside debugging before changing application code.
- [DONE — USER VERIFIED BEFORE THIS CHECKPOINT] Swagger workflow groups appeared in UI; Scan & Analyze Reason text wrapped without widening the page.


### Immediate verification steps
1. Inspect the new GitHub Actions run for the latest feature branch head. Build must succeed before the test step can run.
2. Run `dotnet test MomentumCommandCenterV2.sln --configuration Release` locally; record the exact commit SHA tested.
3. Refresh the API-served dashboard with the new cache-busted script and confirm Provider Comparison date is populated in Arizona time. The expected input format is date-only, not datetime-local.
4. If the BrowserLink console warning persists, disable Browser Link and repeat to isolate injected tooling from application code.


## Implementation checkpoint — fail-closed validation hardening — 2026-10-28

- [DONE — SOURCE IN THIS COMMIT] Boring Overnight evaluator now rejects blank/invalid symbols and non-finite numeric values (NaN, positive infinity, negative infinity) as DATA? before gate evaluation. This closes a correctness hole because NaN comparisons can all evaluate false and accidentally let a malformed snapshot appear eligible.
- [DONE — TESTS ADDED IN THIS COMMIT] Added evaluator regression cases for NaN/infinities and invalid symbols, plus raw market-bar store tests for non-finite prices, impossible OHLC relationships, and negative volume.
- [PENDING — CI] Run the Release build/test workflow for this exact commit before treating the new cases as passing.
- [NOT IMPLEMENTED / BLOCKED BY EVIDENCE] Do not claim autonomous Boring Overnight qualification: daily/overnight metric computation, complete GTC_EXT session coverage, session-aware gap classification, catalyst source, and authoritative TOS states still require implementation and/or source evidence. The evaluator continues to accept caller-supplied snapshots only.
- [NOT IMPLEMENTED / BLOCKED BY EXTERNAL SETUP] Live scanner discovery and provider proof remain blocked by provider selection, credentials/entitlements, coverage/licensing, and real-data smoke tests. The current collector is explicit-symbol historical REST polling, not a market-wide scanner or websocket stream.
- [USER/LOCAL VALIDATION OPEN] Confirm the Provider Comparison date after browser refresh; source initialization and cache-bust are committed, but browser behavior has not been independently observed here.
- [OPEN] BrowserLink $.noConflict is not a function remains unisolated. Compare with Browser Link disabled before adding application workarounds.


## Follow-up correction — validation hardening compile fix — 2026-10-28

- [FIX COMMITTED] The first validation-hardening CI run failed during build with CS0136 because a local variable named reasons conflicted with a later declaration in the evaluator. Renamed the early-return variable to invalidReasons in commit 65c83f7918200e413a5b20fb7942392357c7d193.
- [PENDING — CURRENT HEAD] Re-run build and tests on the exact latest head after this fix. The previous run 40737130070 is a known failure and must not be reported as passing.
- [NEXT] Verify the new evaluator tests for NaN/infinite values and invalid symbols, and the store tests for invalid OHLCV, once the workflow completes.


## Follow-up — batch validation is atomic — 2026-10-28

- [DONE — SOURCE COMMITTED] The raw-bar store now validates every bar in a batch before opening the append stream. A malformed later row can no longer leave earlier rows from that same batch partially persisted.
- [TEST ADDED] Invalid-OHLCV tests now submit a valid bar followed by the invalid bar and assert the store creates no file.
- [PENDING — CI] Confirm the latest branch-head workflow passes build and tests; earlier validation commit 4208441 failed build, and commit 65c83f7 fixes that compiler issue. Neither failure is considered green.


## Verification resolution — validation hardening CI green — 2026-10-28

- [DONE — CURRENT SOURCE HEAD VERIFIED] Commit 9c1b677682f7dd659d355669ddceb99194a7d5fb passed GitHub Actions run [40737545891](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/40737545891).
- [PASS] Release build succeeded.
- [PASS] 108/108 tests passed, including the new Boring Overnight non-finite/symbol fail-closed tests and malformed OHLCV batch-validation tests.
- [DONE — SOURCE] The raw market-bar store validates the complete batch before opening its append stream; malformed later bars cannot leave earlier bars from that batch partially persisted.
- [STILL OPEN] Provider Comparison date must be confirmed in the user's browser after refresh; BrowserLink warning remains unisolated; live provider credentials/entitlements and source-backed overnight coverage remain external/data blockers.
- This verification applies to source commit 9c1b677682f7dd659d355669ddceb99194a7d5fb. Any later source or documentation commit requires checking its own CI run before calling that new head verified.




## Handoff — Overnight Scanner review, 2026-10-28

Read `docs/Overnight-Scanner-Review-2026-10-28.md` before continuing the candidate review. The scanner screenshot and all 11 chart screenshots are in `src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/` on `feature/opportunity-quality-classification`.

### Candidate review result
- First tier: EVR, RTO, SXC. EVR has the clearest bullish continuation/consolidation structure (5M permission yes, bullish regime, 1.88 ATR extension); RTO has the lowest supplied intraday range (1.29%) but remains structurally transitioning and its 1M chart ends `GET OUT`; SXC has a credible recovery and strong 5M state but is extended (3.87 ATR), RSI 73.6, 6.20% range and a large final volume bar.
- Secondary watch: AMT and OSW; both have strong late movement but elevated extension/range risk.
- Reject/wait: BNC (high scanner rank but weak intraday chart and 1M GET OUT), MANH (bearish), MFM (only 8 bars and stale), RNG (bearish/no permission/5.47 ATR), STNG (spike/fade, extended, 1M GET OUT), TRMD (WAIT/no permission/transitioning).
- User clarification: the 1M `EOD: GET OUT` at 12:55 Arizona is intentional dashboard exit behavior. Do not treat that label as a surprise or blanket scanner disqualification; interpret 5M setup separately from 1M execution/exit.
- Every supplied MCC row is `OBSERVED · NOT QUALIFIED`, with bar timestamps around 12:55–12:59 Arizona. These screenshots are not current live entry evidence. Catalyst/news clearance is Data not found in sources.

### Implementation follow-ups
1. Implement a candidate-review record preserving overnight metrics, 5M setup/permission, 1M execution state, sample completeness/freshness, extension/range and catalyst status; expose missing inputs rather than inventing values or weights.
2. Add explicit stale-data status and define the threshold from the application's documented data/session contract before enforcing it.
3. Keep overnight quality distinct from intraday execution eligibility. High scanner rank must not override bearish structure, stale data, missing permission, or 1M exit state.
4. No order was submitted. Refresh data and verify catalyst/event risk before any next-session decision.

## CI checkpoint — 2026-10-30

- [CI VERIFIED] GitHub Actions run **41095950730 / run 618** completed successfully for commit `7c7e20810b70f2a6620fb8f752844a15eed130c3`. This run includes the dashboard code, favicon, user manual, provider comparison note, browser runbook, and master To-Do updates committed through that SHA. The workflow is the repository's .NET build/test workflow; it does not execute browser UI tests.
- [BROWSER VALIDATION OPEN] Local browser acceptance remains outstanding for the new layout and favicon, and the BrowserLink `$.noConflict` error remains unresolved pending its injected script URL/stack trace.


## Handoff update — RKLX, dashboard layout, and frontend discoverability — 2026-10-30

### Current source changes
- Header is full viewport width; .topbar-layout no longer has the 1240px max-width. Header padding is updated to match the wider layout.
- Header demo indicator and WebSocket status chip no longer share the old generic span styling that caused overlap; both can wrap safely.
- Main Chart focused-symbol label is larger and more prominent.
- Opportunity Status is now a 2×2 metric grid to the right of #focusPriceChart, rather than occupying the Main Chart heading row.
- The Visual Studio solution now exposes the four frontend assets in a frontend Solution Items folder. Edit frontend/index.html, frontend/styles.css, and frontend/app.js directly.

### Architecture
- The current UI is a static HTML/CSS/JavaScript frontend served by the ASP.NET Core API from the repository's frontend directory using UseDefaultFiles and UseStaticFiles.
- It is not MVC/Razor. The solution folder makes the existing source editable and visible without a framework migration. No MVC conversion was performed.

### RKLX 2026-10-30 diagnosis
- The user-provided Alpaca/IEX 1M and 5M 19:00–19:35 UTC requests each returned four bars.
- At the approximately 19:26 UTC in-session stale assessment, the 19:25 1M bar had completed; the 19:25 5M bar had not. The last completed 5M bar was 19:05 (expected close 19:10), approximately 16 minutes old. No 5M bars were returned for 19:10, 19:15, or 19:20.
- This explains the stale 5M result without changing the 360-second threshold. The provider returned sparse IEX observations. Whether the gaps mean no IEX-eligible prints or a provider coverage issue is not established; no inference about trades on other venues is valid.
- The short-window endpoint and wider assessment are consistent on their overlapping time interval; do not compare their total bar counts as if they covered the same window.

### WebSocket evidence qualification
- The supplied status snapshot at 22:50 UTC was after the 20:00 UTC regular-session close. It showed connected/subscribed but zero received/persisted bars. This is not an in-session delivery test.
- Next test must occur during regular hours: capture status before and after 5–10 minutes; verify per-symbol counts/timestamps advance; then verify fresh assessments switch to ALPACA_WEBSOCKET_LATEST_BAR only after the 50-bar, freshness, and provenance checks pass.

### Outstanding acceptance
- Browser-test full-width header, chip wrapping, symbol label, chart-side 2×2 cards, Morning Brief modal, replay, Red Zone, and sound.
- Run the .NET Release build/test workflow for the exact current branch head.
- Schwab Trader API Individual approval and OAuth remain blocked by external approval/configuration. Order submission remains disabled.
- Canonical open items and disposition are at the top of docs/Master-To-Do.md.



### Additional RTO OHLCV reconciliation — completed 2026-10-30
- Located and parsed the existing RTO exports in data/validation/RTO.
- 1M: 30 consecutive valid candles, 12:30–12:59 Arizona. 5M: six consecutive valid candles, 12:30–12:55 Arizona. Neither file has duplicate timestamps, internal gaps, invalid OHLC, or negative volume.
- Aggregated five 1M candles for each 5M interval. All six 5M OHLCV candles match exactly across open, high, low, close, and summed volume.
- Embedded epoch-millisecond timestamps convert to UTC and Arizona bar-start times. StrategyReport Date/Time values are separate strategy event timestamps and must not be used as candle keys.
- This proves only six-window OHLCV aggregation parity. The exports do not contain per-bar Thinkorswim Permission, READY, RUNNER, WANING, SWING, GET OUT, or EOD states, so matched-study state replay remains open.
- Full evidence: docs/Validation/Runner-Exit-State-Validation-20261029.md, “RTO exported OHLCV reconciliation.”



## 2026-10-31 — Boring Overnight requirements clarification and dashboard change

- The 20-session requirement is 20 valid trading-session close/open pairs, not calendar days; retain 5D vs 20D metrics and report sample/gap counts.
- Overnight edge (prior RTH close → next RTH open) is distinct from the user's full 07:00–20:00 Eastern GTC_EXT sell window.
- Catalyst CLEAR requires a validated source with health/coverage and timestamped event review. No such validated source is connected to the history-backed evaluation route; it correctly remains UNKNOWN.
- The 5M/1M authority contract is established. Remaining parity work is a one-time certification of exact installed 5M study hash/input settings and matched-bar behavior; re-certify only when those change.
- Dashboard now includes a visible Calculated Limit Sell panel that shows DATA? until source-backed target distribution and ATR context are validated. No numeric limit was fabricated.
- Target policy: historical pre-decision GTC_EXT target quantile determines candidate return; ATR supplies volatility context and distance-in-ATR display, not an arbitrary multiplier. +0.5% remains a comparison heuristic.
- See docs/Validation/Boring-Overnight-Requirements-and-Limit-Policy-20261031.md.
- Current disposition remains NOT READY for actionable qualification; WATCH/manual research only.


### Sell-limit calculation engine — 2026-10-31

- Added BoringOvernightSellLimitCalculator. It takes the selected pre-decision historical quantile of per-session maximum favorable price movement during complete eligible sell windows, calculates a candidate limit from the current reference close, and reports historical touch rate plus target distance in ATR units.
- ATR is context only; no arbitrary ATR multiplier is used. The calculator requires at least 20 complete windows and exact ATR parity status MATCH before returning a numeric candidate. Incomplete windows return DATA?; unverified ATR parity returns NOT VERIFIED.
- Added unit tests for the nearest-rank quantile, in-sample touch rate, incomplete windows, short history, and ATR parity gate. CI/build has not yet been verified for this commit.
- This is the calculation engine only. The API does not yet assemble full eligible-window observations from persisted bars, and the dashboard does not yet call this calculator. The dashboard correctly remains DATA? until that integration and its source evidence exist.


## 2026-11-01 — Prototype dashboard and workflow clarification

### Source-control and comparison model
- Production remains `frontend/index.html` with shared `frontend/styles.css`.
- New `frontend/prototypeIndex.html` is a separate copy of the production page with prototype-only inline layout CSS and prototype controls. Open `http://localhost:53360/prototypeIndex.html` (or the matching HTTPS host/port) to compare; root `/` remains production.
- Future UI requests are applied to `prototypeIndex.html` only. Do not copy prototype changes into `index.html` until the user explicitly says to make them official.
- Prototype shows a fixed left dock with a horizontal focused-symbol text input and vertical workspace toggle. It removes RESEARCH WORKBENCH, places the market clock top-right above System Readiness, and moves the transport/demo indicator into System Readiness.
- System Readiness remains visible: it communicates current integration limitations. It is not obsolete until those states are replaced by trustworthy operational status.
- Shared app.js was made compatible with both the production symbol SELECT and prototype symbol INPUT when a Red Zone card is focused. This is a behavior compatibility change; production layout remains unchanged.

### Symbol field behavior
- Production `#symbol` is initially populated by the selected SPY option in `frontend/index.html`.
- `frontend/app.js` reads `#symbol.value` when refreshing focused assessment/chart and listens for its `change` event.
- Selecting a Red Zone card sets the focused symbol to that candidate. It does not clear the field. Reset workspace resets the workspace mode to Intraday Momentum; it does not clear the symbol.
- Prototype uses a text input initialized to SPY; editing commits on change/blur or Enter.

### Correct scanner → Red Zone → Watchlist flow
1. `Refresh Alpaca market movers` retrieves persisted Alpaca SIP-based discovery events from `/api/scanner/discoveries`. It does not initiate a full-market scan and the returned mover list is discovery, not qualification.
2. Selecting the live discovery group renders those discovery rows into the scanner and Red Zone preview (up to four rows).
3. Clicking `Watch` in a scanner row or Scan & Analyze result adds that ticker to the separate browser-local Watchlist (`localStorage` key `mcc.watchlist`).
4. `Watch` does **not** pin a ticker to Red Zone. Red Zone pinning is separate: use its Add a symbol / Pin to Red Zone control. The Red Zone tile can be clicked to focus the Main Chart.
5. A Watchlist entry does not automatically populate Red Zone, create 5M Permission, confirm 1M execution, or authorize an entry. Those remain separate checks.

### Installed Thinkorswim source inventory
- 5M Command Center: `docs/Validation/Installed-5M-Study.txt`, blob SHA `29e2ccf9c2896f9d4999bae9bcaff72503b19a00`, source header V4.1.7.
- 5M Authority Diagnostic: `docs/Validation/Installed-5M-Authority-Diagnostic.txt`, blob SHA `f2c57603b332b94283a297d647cf3fc2021fcb00`, source header V2.
- 1M Command Center: `docs/Validation/Installed-1M-Study.txt`, blob SHA `2553bcb4c4d5352316cbec3a884f7b8c2eafb65f`, source header V14.4.10 (source notes identify saved V14.4.8 BubbleLadder Fix base).
- Study identity/source documentation is complete. Only a matched-bar output parity replay remains if not already captured in another validation artifact. Do not ask the user to re-identify these studies.

### RTO boundary audit evidence
- 4,048 provider bars returned; 0 newly persisted; 38/40 exact boundaries covered.
- 2026-10-14 expected 13:30Z: no exact provider bar; nearest 13:34Z (+4 minutes); store also missing exact timestamp.
- 2026-10-21 expected 13:30Z: no exact provider bar; nearest 13:31Z (+1 minute); store also missing exact timestamp.
- Both statuses are `PROVIDER_RESPONSE_MISSING_EXACT_BAR`; store matches provider response. Do not impute neighboring bars or combine IEX and SIP observations.
- Compare those dates against independently entitled SIP or another validated provider. 60/120-pair windows are research extensions, not a workaround for missing required boundaries.

### Verification boundary
- Prototype HTML has unique IDs and every static opening element has an ID. The markup has unique IDs; the workspace toggle is synchronized with Reset. Browser rendering, visual acceptance, API interaction and CI on the new branch head remain to be run after pull.


## 2026-11-01 — Official NYSE calendar reconciliation

- Compared the RTO sample (2026-10-02 through 2026-10-30) with the official NYSE 2026 holiday/trading-hours schedule. The interval contains no NYSE holiday or early close; all included sessions are normal 09:30–16:00 Eastern. Expected 1m boundary bar starts are 09:30 ET (13:30Z during EDT) and 15:59 ET (19:59Z during EDT).
- Official reference: https://www.nyse.com/trade/hours-calendars.
- Found and corrected a real ruleset defect: the prior MCC calendar treated Friday 2027-12-31 as an observed New Year's Day closure when January 1, 2028 is Saturday. NYSE explicitly states no New Year's Day holiday is observed in 2028. Ruleset bumped from `MCC_US_EQUITIES_CALENDAR_V1` to `MCC_US_EQUITIES_CALENDAR_V2`; previous V1 captures remain historically identifiable.
- Added tests for 2027-12-31 being a trading session, Jan 1 2028 being a Saturday, Jan 3 2028 being a session, July 3 2026 observed Independence Day closure, and July 3 2028 early close.
- The 2026 RTO window check is complete. Full exceptional calendar certification across supported years remains open.

## 2026-11-01 — Source-only audit of remaining diagnostics

- Provider Comparison initialization is already implemented: if the date field is empty it uses `phoenixParts(new Date())`, which formats the current date in `America/Phoenix`; an existing selection is preserved. Remaining task is browser verification using a non-Arizona OS timezone and a July test date.
- PMI sparse-window regression could not be located in the current branch source using the available repository code search or dashboard JS audit. Data not found in sources. Keep this task open; do not invent the PMI calculation or claim it is fixed.
- The installed study identity/source inventory is complete. Only the aligned matched-bar state/output comparison remains, if it is not already recorded in a separate validation artifact.


### Official calendar certification horizon

The published NYSE 2026–2028 holiday closures and early closes are now represented in regression fixtures. The ruleset remains MCC-maintained and versioned; extend the supported horizon when NYSE publishes the next calendar. It is not a live exchange-calendar feed.


## Canonical checklist count correction

The active checklist now has 26 unchecked operational items: 3 Boring Overnight, 6 P0, 9 P1, 4 P2, and 4 P3. The two RTO-specific open items are alternate-feed comparison and confirmation of provider entitlement/coverage.

## 2026-11-01 — Additional dependency audit

### Morning Brief lineage already present
- `MorningBriefSnapshotStore` appends immutable snapshots with report version, generated/recorded timestamps, SHA-256 content hash, report payload, and source watermarks extracted from marketLandscape bar timestamps.
- Existing tests cover append/read, immutable history, content hash, UTC generation timestamp, and bar source-watermark extraction.
- Remaining gap is narrower: news/discovery watermarks, exact request windows/dependency manifest, raw provider response references and retention, and outcome linkage. The API does not currently expose a complete raw-response manifest or connect outcomes to each snapshot.

### Schwab exclusions implementation boundary
- `ExclusionWatchlistStore` persists a local JSON file at API `App_Data/exclusions-watchlist.json`; it supports local Add/Remove and is consulted by Boring Overnight snapshot/evaluate-with-history paths.
- It is not a Schwab account sync and does not seed the user's canonical exclusion list automatically. Scanner-wide integration and source-list reconciliation remain open.
- Do not claim the full exclusion list is active merely because the local store endpoint exists.


## November 4, 2026 — operator acceptance and active engineering handoff

**Branch:** feature/opportunity-quality-classification  
**Execution checklist:** docs/Master-To-Do.md

### Operator measurements
- Dashboard load 732 ms; DOMContentLoaded 638 ms. Pending localhost WebSocket is expected to remain open.
- Morning Brief waits for modal open; request observed at 6.49 s.
- Fetch & Analyze: RTO 105/91 ms, EVR 102 ms. These timings do not prove cache or fresh provider data.
- 20-session capture: RTO 9.12 s; EVR 6.22 s.
- RTO snapshot append passed and is labelled INPUT SNAPSHOT — NOT A DECISION. Summary shows dataFresh, catalyst, 5M permission, 5M structure and 1M state UNKNOWN.
- EVR has eight missing exact next-session-open boundaries (Oct 12, 14, 15, 22, 26, 30, Nov 3, Nov 4). Provider/store exact are NO; nearest bars 1–7 minutes late. No substitution.

### Immediate engineering priority
Implement validated Daily data independently of intraday 1m/5m bars: last eligible price, SMA20 and 20-session average regular-session volume. Require 20 completed daily sessions, explicit adjustment mode, provider/feed and source/retrieval timestamps. Fail closed to DATA? on insufficient, stale or ambiguous data. Add boundary and incomplete-history tests. Expose values/provenance to Boring Overnight evaluation/snapshots without bypassing catalyst/chart gates.

### Snapshot/UI follow-up
Add visible expected/valid pair counts and historical metric status/presence. Current compact table is insufficient to confirm whether all historical metrics were populated only with 20/20 valid pairs. Keep current-day gates UNKNOWN until sourced.

### Cache/freshness investigation
91–105 ms does not prove whether Fetch & Analyze reads cache, local persistence or upstream provider. Inspect route implementation and response metadata; expose data source, cache hit/miss, fetched/retrieved timestamp and provider/feed when known. Do not label as live if not verified.

### User inputs required
1. After Daily endpoint implementation, run local RTO and EVR smoke tests and return response bodies plus /api/providers/status output.
2. For bars freshness diagnosis, provide one response body per symbol and response headers if available.
3. Pull latest branch and run local build/test; report exact output. Browser tests remain separate from CI.

No additional EVR boundary export is required. Official opening-print parity remains unresolved; current 09:30 one-minute bar Open is only a proxy. No READY result or sell-target profitability claim is authorized by these measurements.

[Uncertainties]
- Cache/provider provenance for the 91–105 ms requests is not established.
- Snapshot table does not expose complete historical metrics or a 20/20 count.
- Daily provider entitlement, adjustment semantics and symbol coverage remain to be verified.
