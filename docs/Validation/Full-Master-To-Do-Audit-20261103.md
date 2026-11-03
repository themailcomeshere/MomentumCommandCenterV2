# Full Master To-Do Audit — 2026-11-03

Branch: `feature/opportunity-quality-classification`  
Audit head: `89b9fe73408cee06f0ddfedc1486b7038d335a2a`

This audit reconciles all 138 unchecked Master-To-Do entries by workstream. Repeated/historical entries are not independently re-run or blindly marked complete. A source implementation, unit test, runtime test, browser acceptance, and Thinkorswim acceptance are separate evidence levels.

## Completed in this pass
- [x] User ran `Get-Latest-And-Test.ps1` at `0917a90...`: Release build succeeded, 179/179 tests passed, 0 failed, 0 skipped.
- [x] User confirmed the committed 50-row Overnight Scanner CSV imported and Red Zone refreshed; MHK, OSW, EVR and EHC appeared.
- [x] User accepted prior prototype title/clock/transport-label layout.
- [x] Fixed prototype horizontal overflow at desktop (104px dock) and mobile (78px dock), commit `3aac28cf638228fd0901251e59969d08af248e17`.
- [x] Corrected standard-time EOD constant to 17:55 Eastern in repository 1M and 5M chart-study copies; daylight remains 18:55 Eastern.
- [x] Exact-head Actions run [41575143773](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41575143773) passed restore/build/test on `89b9fe73408cee06f0ddfedc1486b7038d335a2a`.

## 1. EOD parity — important source identity correction
Both repository study copies use the same US DST calendar rule and now map Arizona 3:55 PM to 18:55 EDT / 17:55 EST. However, `docs/Charts/1M/MainStudy.txt` identifies itself as V14.4.11, not the full V14.5.1 Library study previously identified as the normal execution baseline. The full `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` was not found in the repo tree or Project file search. Do not install V14.4.11 as a replacement.

- [x] Repository 5M V4.1.7 source constant corrected.
- [x] Repository 1M V14.4.11 source copy corrected (not the current full V14.5.1 baseline).
- [ ] Export/upload the full V14.5.1 source; patch its standard-time constant to 1755 while retaining 1855 daylight and the independent 1M VWAP execution gate.
- [ ] Compile full V14.5.1 and V4.1.7 in Thinkorswim.
- [ ] Matched-bar diagnostics in both regimes: Arizona 15:54 / 15:55 / 15:56; Eastern 17:54 / 17:55 / 17:56 EST and 18:54 / 18:55 / 18:56 EDT. Record bar intervals, DST flag, selected cutoff and EOD state on both studies.
- [ ] Isolate screenshot bubbles by toggling one study at a time. A screenshot bubble alone does not identify its generating study.

## 2. Overnight shortlist — source-backed review
The committed CSV has no current price or 20D volume columns. External historical/statistics pages provide a cross-check only; no fresh Daily/5M/1M charts were supplied for November 3.

| Symbol | Price evidence | 20D average volume | Gate | Supplied Nov 2 5M state | Disposition |
|---|---:|---:|---|---|---|
| OSW | $30.22 close Nov 2 | 806,703 | price/volume pass | WAIT, Permission NO, BEARISH; close 30.02, VWAP 30.37, RSI 29.5, RVOL 16.52, extension 3.35 ATR | WATCH/REJECT: Oct 28 earnings/guidance catalyst; ~31.85% 20-session gain, RSI 84.71 |
| EVR | $303.26 close Nov 2 | 641,313 | price/volume pass | WAIT, Permission NO, BEARISH; close 303.26, VWAP 308.69, RSI 32.8, RVOL 21.95, extension 2.98 ATR | WATCH/REJECT: Oct 28 earnings catalyst and sharp post-results move |
| MHK | $150.01 reported Nov 3 | 1,336,567 | price/volume pass | WAIT, Permission NO, NEUTRAL; close 148.60, VWAP 148.59, RSI 51, extension 0.25 ATR | WATCH only: Oct 29 earnings/weak demand outlook plus buyback; current ON 3.13%, 5D ON mean 2.49% vs 20D 0.31% |

Sources: OSW https://stockanalysis.com/stocks/osw/history/ and https://stockanalysis.com/stocks/osw/statistics/; EVR https://ca.finance.yahoo.com/quote/EVR/history/ and https://stockanalysis.com/stocks/evr/statistics/; MHK https://www.marketscreener.com/quote/stock/MOHAWK-INDUSTRIES-INC-13521/quotes/ and https://stockanalysis.com/stocks/mhk/statistics/.

- [x] Price >$5 cross-check passes for all three.
- [x] 20D average volume >=500K cross-check passes for all three.
- [x] No candidate has 5M Permission YES in supplied screenshots.
- [x] Recent earnings catalysts identified for all three.
- [ ] Exclusion status: Data not found in sources. Local `App_Data/exclusions-watchlist.json` is runtime-only and complete Schwab list is not reconciled.
- [ ] Obtain current Daily/5M/1M charts for a current-session re-review.
- [ ] Obtain/import the full 248-row CSV; the committed file contains only 50 rows.

**Disposition: zero qualified BUY/HOLD candidates.** OSW/EVR have bearish 5M structure and Permission NO. MHK has neutral structure, Permission NO and an overly hot overnight move.

## 3. Thinkorswim scan ingestion — impediment
The public Schwab Trader API is a separate developer product describing market data, account information, orders and transactions; it does not document retrieval of custom saved Thinkorswim scan results or custom scanner columns. No public supported TOS scanner API/webhook was found in official developer materials: https://developer.schwab.com/products/trader-api--individual and https://developer.schwab.com/user-guides. Do not scrape the desktop UI.

Feasible automation bridge: a local watched-folder CSV adapter in MCC (configurable drop folder, atomic file handoff, hash/idempotency, archive/reject folders, parse diagnostics, source timestamp, audit record, status endpoint); keep manual upload fallback. It is not implemented yet. Alpaca movers are not a substitute for the custom Overnight scan.

## 4. Alpaca collector / SIP
- [x] Source calls `v1beta1/screener/stocks/movers?top=50`; this is bounded top gainers/losers, not a full-market scan.
- [x] Poll interval is clamped to 30–3600 seconds; events are persisted through provider-neutral ingestion.
- [x] User identified `secrets.json` as the feed override; SIP request returned HTTP 403 entitlement denial; IEX restored and returned validated data (62 bars).
- [ ] During regular market hours, capture `/api/scanner/discoveries` twice 5–10 minutes apart and verify last success/source timestamp, status, failures, symbol timestamps and persistence.
- [ ] SIP acceptance remains blocked until Alpaca grants recent SIP entitlement; do not label SIP healthy from configuration alone.
- [ ] Validate complete-market coverage, terms, rate limits and backfill before production scanner sign-off.

## 5. Boring Overnight / Red Zone truth
Already implemented: fail-closed hard-gate evaluator; explicit 20-session close/open history calculator; history service that overwrites caller-entered historical values or nulls them; candidate snapshots; local exclusion CRUD; quantile sell-limit calculator that requires 20 complete windows and ATR parity MATCH; Alpaca news client that returns unclassified headlines.

Still missing: daily history provider for price/SMA20/20D volume; complete reconciled exclusions; validated catalyst classification/coverage; matched current 5M/1M source evidence; immutable full-universe snapshots; GTC_EXT 07:00–20:00 ET outcome coverage; walk-forward target quantile; integration to Red Zone.

The existing `OpportunityQualityAssessor` is a diagnostic classifier, not a versioned automatic top-four ranker. Required remaining ranking work: version/config, 5M same-slot RVOL, daily liquidity, 1M execution state, momentum/extension/freshness, explicit reasons, Manual/Automatic mode, rank hysteresis and alert deduplication. Keep this rank separate from Boring Overnight qualification and 5M Permission.

- [ ] Catalyst remains UNKNOWN until a validated source is connected; caller assertion must not set CLEAR.
- [ ] Known exclusion subset in docs: SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE; not verified exhaustive and not currently seeded into runtime store.
- [ ] Boring Overnight remains WATCH/manual research, not READY.
- [ ] Calculated Limit Sell remains DATA?/not calculated. +0.5% is a heuristic, not a target.

## 6. Other Master-To-Do workstreams (138 unchecked entries reconciled)

| Workstream | Current source truth | Remaining acceptance |
|---|---|---|
| WebSocket | Subscription/worker implementation exists; after-hours SUBSCRIBED is not delivery proof | Regular-session per-symbol counts/timestamps for SPY/RKLX/ASTX/XRPN; verify eligible 1M WS selection and 5M REST authority |
| Collector recovery | Durable observations, watermarks, overlap polling, retries/checkpoints exist | Controlled outage/restart/checkpoint reload/sleep-wake on target host |
| Main Chart/feed parity | Feed-source diagnostics exist; SIP entitlement denied | Current feed status, bar age, exact TOS/MCC matched Close/VWAP/ATR14/extension |
| Morning Brief | Immutable version + SHA-256 snapshot store and market-bar watermarks exist | News/discovery watermarks, raw response references/retention, dependency manifest, outcome linkage; browser modal/export tests |
| PMI/provider comparison | Sparse-data protections implemented in code | Re-run one-bar insufficient-data case, liquid control, provider/feed/gap metadata, date persistence and July/non-Arizona timezone test |
| Schwab positions | Individual API approval reported | User app creation, Accounts & Trading Production, exact callback, Active/Ready status, server-side OAuth and read-only positions; no order writing |
| Packaging/retention | Browser-hosted API is current | Decide desktop/local service, installer/startup, data directory, retention, backup/restore |
| Production scanner/provider | Alpaca movers bounded to top 50; SIP denied | Licensing, entitlement, full-market coverage, backfill, rate limits and representative-symbol validation |

## Second pass — newly unblocked vs still blocked
- [x] Local Windows get-latest/build/test closed by user evidence (179/179).
- [x] 50-row CSV import and Red Zone refresh closed by user evidence.
- [x] Prototype overflow code fix committed; browser confirmation pending.
- [x] EOD standard-time source constant corrected in repository study copies; CI passed.
- [ ] Full V14.5.1 1M source, TOS compilation and matched-bar outputs still require user-supplied study/TOS evidence.
- [ ] Full 248-row export still requires user export/commit.
- [ ] TOS scan API remains unsupported/documented; watched-folder CSV bridge is future implementation.
- [ ] SIP entitlement requires Alpaca account change.
- [ ] Catalyst/exclusion/daily history/GTC_EXT and target validation require source data.
- [ ] Regular-session runtime/browser/recovery acceptance requires user's running Windows environment.
- [ ] Schwab OAuth requires app/callback setup and secure user-side credentials.

## Next operator actions
1. Pull latest branch and hard-refresh prototype; confirm scrollbar gone.
2. Export/upload the full V14.5.1 1M ThinkScript source. Do not send credentials.
3. Export/commit the complete 248-row TOS CSV with the original metadata preamble intact.
4. In TOS, compile V4.1.7 and full V14.5.1; run the before/at/after DST matrix above and provide the diagnostic output.
5. During regular hours, capture collector status/discoveries before and after 5–10 minutes.
6. Upload current Daily, 5M and 1M charts for OSW/EVR/MHK if they remain on the shortlist.
7. Continue only with a source-backed exclusion list and validated catalyst source; otherwise all three remain non-qualified.

## [Uncertainties]
- Full V14.5.1 source was not retrievable from repository or Project file search; V14.4.11 is not a substitute.
- Public market statistics are secondary-source cross-checks, not MCC authoritative feed data.
- Current exclusion status for OSW/EVR/MHK is unknown.
- No supported public TOS saved-scan API/webhook was found; a private Schwab arrangement could change this.


## 2026-11-03 follow-up — new evidence and second dependency pass

## 2026-11-03 — latest Overnight timing, scanner export, 1M source and market clock

**This dated update supersedes stale status statements below where they conflict.** Detailed 195-row findings: [Overnight Scanner Full-Universe Review — 2026-11-03](Validation/Overnight-Scanner-Full-Universe-20261103.md).

- [x] Located and reviewed the newer November 3 Overnight procedure documents on `master`. The 20:00 ET start proposal is withdrawn. Overnight includes Buy Day after-hours plus Sell Day premarket; candidate review begins in the final 60 minutes of regular trading.
- [x] Defined schedule in exchange-local terms: evaluation starts at regular close minus 60 minutes; buy deadline is the regular close. America/Phoenix display therefore normally shows noon during EDT and 1:00 PM during EST. The schedule follows the calendar's early-close time.
- [x] Implemented API clock fields for Overnight evaluation start, buy deadline, and whether the evaluation window is open. Added two countdown rows to both the production dashboard and prototype header.
- [x] Added DST/standard-time, early-close, premarket, weekend and after-hours tests. Exact-head CI run [41679069855](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41679069855) passed all 182 tests. The first test attempt failed because its early-close fixture was 30 minutes before the evaluation window; the fixture was corrected, not the production logic.
- [x] User uploaded `CurrentStudy_CouldBeWrong.txt` (1,424 lines, V14.5 header). Created a separate candidate `docs/Charts/1M/MainStudy_V14.5.2.txt` (1,456 lines) with exact 5M V4.1.7 Permission inputs, including Daily VWAP, and DST-aware 3:55 PM Arizona EOD mapping (18:55 EDT / 17:55 EST). Original upload preserved unchanged.
- [x] Exact-head CI on source commit `7c265a8777eab3e9f0e2093e89674f98a8f7b75c`, run [41679267305](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41679267305), passed build and 182/182 tests. CI does not compile ThinkScript.
- [x] Parsed the uploaded November 3 scanner CSV: 195 unique ticker rows, no duplicates, metadata preamble intact.
- [x] Full-universe historical-metric pass: 107 rows pass all five available numeric gates; 81 are incomplete due to `loading`; 7 have known gate failures. The file has 195 rows, not the reported 248. No current price or 20D volume columns exist.
- [x] Located canonical 55-symbol Schwab exclusion source on `master`; OSW, EVR and MHK are not in that canonical list.
- [ ] Live Schwab tradability and feature runtime `App_Data/exclusions-watchlist.json` remain unverified; the feature branch local store is not seeded from the canonical 55-list provider.
- [ ] Browser-import/Red Zone acceptance for the 195-row file is pending. The importer source supports the preamble/header and maps Overnight Score, but browser behavior was not run here.
- [ ] Obtain the actual 248-row export; 53 ticker rows are absent from the uploaded file.
- [ ] Current Daily/5M/1M charts, catalyst clearance, price and 20D volume for the full universe remain outstanding.
- [ ] Compile V14.5.2 and normal 5M V4.1.7 in Thinkorswim; run matched-bar EOD and Permission parity diagnostics. Do not install V14.5.2 as authoritative until TOS compile and chart validation pass.
- [ ] Runtime test on 2026-11-03: SPY advanced 5 bars and XRPN 1; RKLX and ASTX did not advance. RKLX idempotency comparison reported false for both timeframes. Preserve raw JSON artifacts with exact SHA/window and inspect source transport/timestamps before sign-off.
- [ ] RTO 20-session output shows three missing exact next-session opens: Oct 14 (+4 min), Oct 21 (+1 min), Nov 3 (+1 min). All displayed previous-session close boundaries were exact. Feed provenance for this new capture is not included in the pasted table; do not label it SIP without inspecting the JSON.
- [ ] Merge remains HOLD. Branch compare reports 1,194 commits ahead / 160 behind and 300 changed files; PR #7 was explicitly closed as incorrectly based on master. Master has the newer Overnight docs and canonical exclusion provider; reconcile selectively before creating a merge PR. Do not wholesale merge or force-update either branch.

## 2026-11-03 — Overnight timing / scanner / 1M source follow-up

- [x] Overnight start corrected: final 60 minutes before regular close; includes Buy Day after-hours + Sell Day premarket.
- [x] API clock calculates final-hour evaluation start and regular-close buy deadline with early-close handling.
- [x] Production and prototype headers include evaluation and buy countdowns.
- [x] Clock tests pass in run 41679267305 (182/182).
- [x] Created separate 1M V14.5.2 candidate with 5M V4.1.7 Permission parity and DST-aware 3:55 PM Arizona EOD.
- [ ] TOS compile and matched-bar EOD/Permission parity for V14.5.2.
- [x] Parsed November 3 scanner export: 195 unique rows; 107 pass available numeric gates, 81 incomplete, 7 fail.
- [ ] Obtain actual 248-row export; 53 rows missing from current file.
- [ ] Browser-test 195-row import and Red Zone update.
- [x] Canonical 55-symbol exclusion source located; OSW/EVR/MHK absent from canonical list.
- [ ] Verify live Schwab tradability and actual feature runtime exclusion file; do not assume local list is seeded.
- [ ] Obtain current price, 20D volume, catalyst and Daily/5M/1M chart evidence for the full-universe shortlist.
- [ ] Review market-hours JSON artifacts and RKLX idempotency; preserve branch SHA and exact test window.
- [ ] Resolve three missing exact RTO next-session opens (Oct 14, Oct 21, Nov 3) from source/feed evidence; no nearest-bar imputation.
- [ ] Selectively reconcile master and feature; branch is diverged and must not be wholesale merged.

Detailed scanner report: [Overnight Scanner Full-Universe Review](Validation/Overnight-Scanner-Full-Universe-20261103.md).



## 2026-11-03 follow-up — scanner importer and full-universe exclusion cross-check

- [x] Compared all 195 scanner symbols against the canonical 55-symbol Schwab exclusion source on master: **zero overlaps**. This is a list-membership result, not live broker-tradability proof.
- [x] Extended the CSV importer to preserve ON win %, 20D average ON %, median ON %, 5D ON %, ON momentum, current ON %, and above-20-SMA in `overnightMetrics`; the existing reason column now displays a compact summary. Commit `649361834984292eab66fa577e960237d6ff1e7e`.
- [x] Executed the importer-equivalent parsing logic against the actual CSV: header row 3, comma delimiter, 195 rows, 195 unique symbols, zero duplicates.
- [ ] Browser upload/Red Zone rendering remains pending. Importer code commit `649361834984292eab66fa577e960237d6ff1e7e` passed build and 182/182 tests in Actions run 41679681456.


## Latest code verification — 2026-11-03

- [x] Exact-head code commit `7583f5233c7dbd5d937fd7504218dd0235dc4677` passed Release build and 182/182 tests in Actions run [41680258800](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41680258800).
- [x] Updated `docs/Overnight-Scanner-Quick-Start.md` for final-hour evaluation and preservation of all seven Overnight metrics during CSV import.
- [x] Updated `docs/Validation/Thinkorswim-MCC-Extension-Parity-20261102.md` to document V14.5.2 candidate source and TOS validation gates.
- [ ] Browser import/clock acceptance and Thinkorswim compile/parity remain open.


## 2026-11-03 — V14.5.1 Library source found and V14.5.2 rebased

- [x] Project Library search found and full-file read `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (1,260 lines, V14.5.1). The uploaded `CurrentStudy_CouldBeWrong.txt` is V14.5 (1,424 lines), not the same file; substantial differences exist in execution-window, reconstructed 5M, state and bubble logic.
- [x] Replaced the initial V14.5.2 draft with a V14.5.2 derived from the actual V14.5.1 Library source: `docs/Charts/1M/MainStudy_V14.5.2.txt` (1,298 lines). The uploaded V14.5 file remains unchanged.
- [x] V14.5.2 adds 5M V4.1.7 Permission parity (5M close > Daily VWAP, 5M close > EMA9, EMA9 >= EMA20, RSI >= floor) while retaining the separate 1M VWAP execution gate; it also adds DST-aware 3:55 PM Arizona EOD mapping.
- [ ] TOS compile and matched-bar parity remain mandatory; .NET CI does not compile ThinkScript.
## 2026-11-03 — V14.5.1 Library baseline checkpoint

- [x] Found the actual 1M V14.5.1 source in the Project Library; the uploaded V14.5 source is materially different and is not a replacement for it.
- [x] Rebased `docs/Charts/1M/MainStudy_V14.5.2.txt` on the V14.5.1 Library source. The rebase commit `d877cf90949ca5e480e45b5c242808ad8a770da5` passed Release build and 182/182 tests in [Actions run 41680840299](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41680840299).
- [x] Added explicit V14.5.1 source provenance and scope comments to the V14.5.2 header in commit `600415fce8b3d7d390e7cf7eebdf4053bfc508ff`; exact-head CI [41681045730](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681045730) passed.
- [ ] TOS compile and matched-bar validation remain mandatory before installing V14.5.2.

## 2026-11-03 — production and prototype clock UI verified

- [x] Added evaluation-start and buy-deadline countdown rows to both `frontend/index.html` (production) and `frontend/prototypeIndex.html`, with matching styles in `frontend/styles.css`.
- [x] Countdown switches to `OPEN` at the exact evaluation-window start using client epoch time; API also reports `overnightEvaluationOpen`.
- [x] Exact-head CI for production markup commit `95c95a5217ce9b3fc168e7f3e94532fd43e82923` passed in run 41681383369; CSS commit `61d08a86e969186a6d4e9468714c0f7120531699` passed in [run 41681395871](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681395871).
- [ ] Browser visual acceptance remains pending.



## 2026-11-03 follow-up audit — 10:52 export, corrected process, exclusion seed, runtime JSON

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
- [x] Updated importer success feedback to report actual rendered scanner row count and actual Red Zone card count/symbols. Red Zone subtitle now clarifies imported preview is top four by raw Overnight Score, not a Boring Overnight rank. Code CI passed in run [41693652415](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41693652415).
- [ ] Browser-verify import feedback, `tbody#scannerRows`, and actual Red Zone `#redZoneGrid`. `#mcc-static-0278-div` is only the static scanner-table wrapper; it is not the Red Zone element and is not expected to be replaced.
- [ ] Re-export all 248 scanner rows; current 10:52 export still has 198 (50 missing).
- [ ] Current price >$5, 20D average daily volume >=500K, catalysts, Daily charts, 5M Permission/structure, and 1M execution evidence are not in this CSV. No Boring Overnight READY/BUY decision is supported yet.
- [ ] Thinkorswim cutoff test remains inconclusive: no bars were available at Arizona 15:54/15:55/15:56 for the supplied RTO/SPY chart views. Bubbles/arrows on other after-hours bars do not establish the exact cutoff bar or correct source study.
- [ ] After pulling the exclusion seed, restart API and verify `GET /api/watchlists/exclusions` returns 56 entries (55 canonical + TEST).


### Second-pass disposition of the remaining checklist

The current Master-To-Do contains 164 unchecked boxes. I re-read the full checklist and grouped repeated/historical entries into the current workstreams rather than treating each repeated checkbox as a separate deliverable. This follow-up unblocked and completed: source-of-truth Overnight document sync, canonical exclusion seed, new export parsing/metric-gate review, importer feedback instrumentation, RKLX persisted-content diagnosis, RTO feed/boundary diagnosis, and the WebSocket freshness interpretation. The remaining boxes are not all newly actionable code tasks: several require the user's local browser/TOS/API session, provider entitlements, missing source data, or external product/broker decisions. The next pass should not mark these as done until their evidence is captured.
