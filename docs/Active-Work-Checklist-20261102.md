## 2026-11-03 — production and prototype clock UI verified

- [x] Added evaluation-start and buy-deadline countdown rows to both `frontend/index.html` (production) and `frontend/prototypeIndex.html`, with matching styles in `frontend/styles.css`.
- [x] Countdown switches to `OPEN` at the exact evaluation-window start using client epoch time; API also reports `overnightEvaluationOpen`.
- [x] Exact-head CI for production markup commit `95c95a5217ce9b3fc168e7f3e94532fd43e82923` passed in run 41681383369; CSS commit `61d08a86e969186a6d4e9468714c0f7120531699` passed in [run 41681395871](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681395871).
- [ ] Browser visual acceptance remains pending.

## 2026-11-03 — V14.5.1 Library baseline checkpoint

- [x] Found the actual 1M V14.5.1 source in the Project Library; the uploaded V14.5 source is materially different and is not a replacement for it.
- [x] Rebased `docs/Charts/1M/MainStudy_V14.5.2.txt` on the V14.5.1 Library source. The rebase commit `d877cf90949ca5e480e45b5c242808ad8a770da5` passed Release build and 182/182 tests in [Actions run 41680840299](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41680840299).
- [x] Added explicit V14.5.1 source provenance and scope comments to the V14.5.2 header in commit `600415fce8b3d7d390e7cf7eebdf4053bfc508ff`; exact-head CI [41681045730](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41681045730) passed.
- [ ] TOS compile and matched-bar validation remain mandatory before installing V14.5.2.

## 2026-11-03 — latest verified checkpoint

- [x] Latest code head `7583f5233c7dbd5d937fd7504218dd0235dc4677` passed Release build and 182/182 tests in Actions run 41680258800.
- [x] Updated Overnight Quick Start and Thinkorswim/MCC parity validation notes.
- [ ] Browser acceptance and TOS compile/parity remain open.

---

## 2026-11-03 follow-up — CSV metrics / exclusions

- [x] Compared all 195 scanner symbols to the canonical 55-symbol exclusion list: zero overlaps (list membership only; live tradability unknown).
- [x] CSV importer now retains all seven Overnight metric columns in candidate data and summarizes them in the reason cell (commit `649361834984292eab66fa577e960237d6ff1e7e`).
- [x] Importer-equivalent parse: header row 3, 195 rows, 195 unique, 0 duplicates.
- [ ] Browser import and Red Zone visual acceptance.

---

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


---

## 2026-11-03 — Scanner CSV import retry

The synced export is now present at `data/ScannerExports/2026-11-02-OvernightWatchListScanner.csv`. It contains two metadata preamble lines (`Watchlist Scanner`, `Results`) followed by the actual comma-delimited header on line 3. The importer previously assumed line 1 was the header, which caused the reported missing Symbol/Ticker error.

`frontend/app.js` now searches the first 25 nonblank rows for a normalized Symbol/Ticker header, then imports the data rows after it. The uploaded file was parsed against the same delimiter/header rules: comma delimiter, header row 3, **50 nonblank ticker rows**, first three MHK/OSW/EVR with Overnight Scores 73.42/70.97/68.25. Browser interaction has not yet been exercised, so final UI acceptance remains pending. Do not re-upload or edit the CSV to remove its preamble.

Current importer fix commit: `b05c63713452cbbbb5632a87c639f655ad027e73`. GitHub Actions run 41549757022 is still running at the time this note was added.

# Active Work Checklist — 2026-11-02


- [x] Audited EOD time conversion: 1M V14.5.1's 15:55 Eastern fires 2–3 hours early versus 3:55 PM Arizona; 5M V4.1.7's 19:55 Eastern standard-time mapping is two hours late in Arizona (correct is 17:55 EST). Both studies require a targeted shared DST mapping and matched-bar validation.
- [ ] Correct and compile both ThinkScript studies' EOD clock mapping; test before/at/after 3:55 PM Arizona across DST and standard-time dates. Do not trust cross-study EOD/GET OUT parity until complete.


- [x] Confirmed latest full 1M Library source is `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (V14.5.1); V14.4.10 is an isolated aggressive-lane audit-only artifact. V14.5.1 retains both the 5M VWAP proxy gap and the EOD clock mismatch.

## November 2 follow-up — operator script, scanner import, prototype header, and 1M parity

- [x] Fixed PowerShell refspec interpolation in `scripts/Get-Latest-And-Test.ps1`: use `${Branch}` before the colon so PowerShell does not parse `$Branch:` as a scoped variable.
- [x] Hardened scanner CSV parsing for UTF-8 BOM, comma/semicolon/tab delimiters, quoted fields, `Symbol`, `Ticker`, and `Symbol/Ticker` header variants; map `Overnight Score` to the imported score.
- [x] Moved `marketSessionClock` directly beside the title inside the `mcc-static-0010-div` parent in the prototype; made the transport-label text background transparent while retaining the status dot.
- [ ] Browser-verify the CSV using the user's original committed export, plus desktop/mobile header layout. The current active branch tree did not contain a dedicated Overnight Scanner export CSV at review time; the exact source file/format remains unverified.
- [ ] Run the corrected PowerShell script locally and record the Windows Release restore/build/test output.
- [x] Exact-head GitHub Actions run 41546963701 passed .NET build/test on commit `825276a844a5b3079aa3ea4bd1cb8ccbd90e283b`.
- [ ] Browser acceptance remains pending for the prototype header and original scanner CSV.
- [x] Audited Library full 1M baseline V14.5.1 against normal 5M V4.1.7. V14.5's reconstructed 5M raw Permission omits the 5M Daily VWAP condition; the 1M VWAP gate is not equivalent. See `Validation/Thinkorswim-MCC-Extension-Parity-20261102.md`.
- [ ] Create a targeted 1M parity revision only after the 5M Daily VWAP comparison is implemented and compiled; validate on matched completed 5M bars. Keep V14.5 as baseline until then.
- [x] Reviewed MHK, OSW, and EVR chart screenshots: no candidate meets the combined overnight + current 5M/1M decision requirements. The supplied charts are 1D/5m (not 1m): EVR and OSW show visible EOD GET OUT bubbles on their 5m charts, but the generating study/source has not been isolated; their top 5M labels remain WAIT/Permission NO/BEARISH; MHK shows 5M WAIT/Permission NO/neutral structure and no visible EOD GET OUT. No 1M chart was supplied.
- [ ] Do not publish a numeric Calculated Limit Sell until 20 complete eligible GTC_EXT windows, walk-forward target quantile/touch analysis, and ATR parity are available. Current disposition: no new overnight buy/hold; limit sell DATA?/not calculated.


## Completed from latest user feedback
- [x] Identified IEX configuration override: local secrets.json overrode appsettings.json.
- [x] Confirmed SIP can be selected by configuration, then captured Alpaca HTTP 403: recent SIP data is not permitted by the current subscription.
- [x] Restored IEX and confirmed provider validation returned 62 bars.
- [x] User visually accepted the prototype Main Chart layout without horizontal scrolling.
- [x] Corrected Main Chart timestamp display: show completedAtUtc (bar close) and separately show the bar-start-to-close interval.
- [x] Recorded latest INO TOS/MCC readings and the fact that TOS 24.6 ATR was a live label, not a historical per-bar value.
- [x] Recorded Schwab Trader API — Individual approval; app registration and OAuth remain separate.
- [x] Defined RTO audit as exact 40-boundary coverage across 20 valid sessions.
- [x] Fixed prototype header structure: System Readiness is now a sibling of topbar-brand, matching production's two-column header.
- [x] Overrode anywhere-wrapping for marketTransportLabel so the text wraps at word boundaries rather than character-by-character.
- [x] Fixed scanner CSV import so it immediately re-renders Red Zone as well as the scanner table.
- [x] Documented Red Zone refresh cadence: 15-second live 1M/5M card assessment refresh; 30-second Alpaca discovery refresh when its group is selected.
- [x] Added technical update/build/API verification workflow and `scripts/Get-Latest-And-Test.ps1`.
- [x] Added Overnight Scanner Quick Start and November 2 candidate/news review.

## Open P0 — runtime acceptance
- [x] Latest source head `237fcbfc71f9b03bda64b1e6047d61355c849447` passed Release build and 179/179 tests with 0 warnings/0 errors in [Actions run 41540235754](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41540235754).
- [ ] Hard-refresh prototype and verify the repaired header, transport label wrapping, and timestamp display on desktop and mobile.
- [ ] During regular market hours, capture live-assessment JSON and calculate evaluatedAtUtc minus oneMinute.completedAtUtc. Investigate if >=60 seconds for a liquid symbol.
- [ ] Complete WebSocket delivery test for SPY, RKLX, ASTX and XRPN using advancing per-symbol bar counts and timestamps.
- [ ] Complete controlled collector failure/recovery, restart/checkpoint reload and sleep/wake acceptance.

## Open P1 — strategy and data correctness
- [ ] Match TOS and MCC on the exact same completed 1M candle; obtain five-decimal Close, VWAP, ATR14 and historical ExtensionATR from the Data Box.
- [ ] Reconcile VWAP/ATR differences only after timestamp, session scope and feed match. Do not tune thresholds to force parity.
- [ ] Complete Red Zone source automation and versioned opportunity-quality ranking with validated participation/liquidity inputs, explanations, stable re-ranking and tests. CSV import and periodic card assessments are implemented; automatic Thinkorswim scan ingestion is not.
- [ ] Complete source-backed catalyst, daily SMA20/average volume, exclusion reconciliation and immutable Boring Overnight snapshots. Boring Overnight remains WATCH/manual, not READY.
- [ ] Complete GTC_EXT history, target-touch/time-to-touch/adverse-excursion analysis, walk-forward target quantile and Calculated Limit Sell integration.
- [ ] Complete matched TOS 1M/5M state replay; OHLCV alone cannot prove study states.
- [ ] Extend Morning Brief lineage with news/discovery watermarks, raw-response retention, dependency manifest and outcome linkage.

## Open P2 — integrations and delivery
- [x] User reports Schwab Trader API — Individual approval.
- [ ] Create MCC Schwab app and attach Accounts and Trading Production. Suggested app name: `Momentum Command Center`; description: `Read-only account and position synchronization for Momentum Command Center. No order placement or trading actions are performed.` Defer submission until the actual HTTPS OAuth callback route/host is implemented; do not guess a callback URL.
- [ ] Confirm app Active/Ready status and exact callback URL.
- [ ] Configure app credentials in local server-side secret storage only.
- [ ] Implement OAuth, protected token persistence/refresh and read-only account/position retrieval.
- [ ] Persist normalized position snapshots with provenance; distinguish flat from missing/stale/failed data.
- [ ] Keep order-writing endpoints out of Phase 1/2.
- [ ] Validate scanner provider coverage, licensing, rate limits, backfill and retention. Current Alpaca movers source is SIP-based and the user's recent-SIP request returned HTTP 403.
- [ ] Decide desktop packaging/startup and data retention/backup policy.

## Open P3 — older validation and UX
- [ ] Re-run PMI sparse-window regression with a liquid control; one-bar sample must be INSUFFICIENT DATA.
- [ ] Browser-verify date handling under a non-Arizona OS timezone.
- [ ] Complete Morning Brief modal/history, replay/HTTP 400, Red Zone responsive layout, sound permission and clean reload checks.

## RTO audit status
The RTO audit checks 40 exact prior-close/next-open 1-minute timestamps across 20 valid sessions, separately recording provider-response and store presence. Prior capture: 38/40 exact on IEX. Missing next-session opens: 2026-10-14 13:30Z (nearest 13:34Z) and 2026-10-21 13:30Z (nearest 13:31Z). The file named RTO-SIP-20261101.json actually reported feed=iex. A SIP audit is blocked by the explicit recent-SIP HTTP 403. Keep IEX and SIP histories separate; do not impute missing bars.

## Current operating decisions
- Use IEX for current usable Alpaca data. Do not call SIP validated until a recent SIP request succeeds.
- Treat completed-bar timestamp correction as a display fix, not proof that provider latency is resolved.
- Do not claim numerical TOS parity from rounded inputs or a live EXT label.
- Keep Boring Overnight manual-only and Schwab integration read-only.


## November 2 after-hours WebSocket result

- [x] API health returned OK.
- [x] WebSocket reported enabled/connected/SUBSCRIBED with SPY, RKLX, ASTX, XRPN acknowledged.
- [ ] Delivery acceptance remains open: the user ran this after 3 PM Arizona; `lastBarUtc=null`, `receivedBars=0`, `persistedBars=0`, and `barsBySymbol={}`. This is a connection/subscription pass only, not a regular-session delivery test. Repeat the five-minute before/after test during market hours.

## New workflow documents (2026-11-02)
- [x] `docs/Technical-Workflow.md` is the operator's standard get-latest/build/restart/API smoke-test sequence; `docs/Runtime-Acceptance-Runbook.md` remains the detailed acceptance source of truth.
- [x] `scripts/Get-Latest-And-Test.ps1` safely checks for a clean tree, fetches/switches/pulls the project branch, and runs Release restore/build/tests.
- [x] `docs/Overnight-Scanner-Quick-Start.md` documents the TOS scan → hard gates/news → CSV import → Red Zone → chart review process.
- [x] `docs/Validation/Overnight-Scanner-Review-20261102.md` records the MHK/OSW/EVR shortlist and catalyst review.
- [ ] User visually verifies the header/transport label fix after pulling the latest branch.
- [ ] User uploads Daily and 5M charts for OSW, EVR, and MHK for final chart-level review.

## [Uncertainties]
- Actual INO bar age is not measured; exact date/time and raw API response for the reported delay were not supplied.
- Exact unrounded TOS values and historical ExtensionATR for the same MCC candle are unavailable.
- Schwab app status, attached products, callback URL and credentials are not confirmed.
- Current Alpaca subscription denied recent SIP requests; future entitlement changes may alter this.

## 2026-11-03 — V14.5.1 Library source found and V14.5.2 rebased

- [x] Project Library search found and full-file read `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` (1,260 lines, V14.5.1). The uploaded `CurrentStudy_CouldBeWrong.txt` is V14.5 (1,424 lines), not the same file; substantial differences exist in execution-window, reconstructed 5M, state and bubble logic.
- [x] Replaced the initial V14.5.2 draft with a V14.5.2 derived from the actual V14.5.1 Library source: `docs/Charts/1M/MainStudy_V14.5.2.txt` (1,298 lines). The uploaded V14.5 file remains unchanged.
- [x] V14.5.2 adds 5M V4.1.7 Permission parity (5M close > Daily VWAP, 5M close > EMA9, EMA9 >= EMA20, RSI >= floor) while retaining the separate 1M VWAP execution gate; it also adds DST-aware 3:55 PM Arizona EOD mapping.
- [ ] TOS compile and matched-bar parity remain mandatory; .NET CI does not compile ThinkScript.
