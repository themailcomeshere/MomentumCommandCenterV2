## November 10 — Operator feedback, price provenance, and blocker work

### Implemented on `feature/opportunity-quality-classification`

- [x] Added a color-coded classification directly below the focused-symbol input. Intraday displays the separate Opportunity Quality diagnostic; Boring Overnight displays the imported Boring Winner PASS/FAIL as RESEARCH ONLY, or NOT CALCULATED. It never promotes the research flag to READY.
- [x] Main Chart heading now resolves the instrument's display name from Alpaca asset metadata, with Yahoo Finance name fallback.
- [x] Added Alpaca latest-trade REST retrieval on the configured feed and an optional Yahoo Finance extended-hours reference endpoint. The Overnight price card selects the newest timestamp among the returned bar, Alpaca trade and Yahoo post-market reference, and shows source/time/staleness. Neither provider trade nor Yahoo reference is NBBO or guaranteed executable.
- [x] Replaced the incorrect implication that a latest 1M bar close is an after-hours quote. If no newer provider trade/reference exists, the card explicitly labels the old value as the last returned bar/trade and shows its age.
- [x] Added an expandable failed-evidence list directly inside the focused Calculated Limit Sell card. The quick button still delegates to the existing evidence-gated API and submits no order.
- [x] Overnight Scan & Analyze defaults to a five-calendar-day history window; Intraday retains the six-hour window. Results identify the actual provider/feed and explain that zero IEX bars are not proof of no market activity.
- [x] Alpaca Daily response parsing now accepts either a normal bars array or a symbol-keyed `bars.{SYMBOL}` array. Explicit null/missing-symbol results become incomplete DATA? observations; unexpected JSON shapes still fail with a shape-specific error. Added regression coverage.
- [x] Successful Alpaca stream authentication/subscription clears the previous connection error so a stale `connection limit exceeded` message is not shown as the current failure after a later successful connection.
- [x] Boring Winner import UI now explains that the expected input is a Daily OHLC CSV with Date/Open/High/Low/Close, not the 1M/5M ATR parity report. Extended Hours defaults OFF; corporate-action adjustment defaults ON; chart timezone defaults to America/Phoenix.
- [x] Added tests for timestamped Alpaca latest trades, symbol-keyed Daily bars, and Yahoo after-hours metadata.
- [x] Fixed the Dashboard startup `decisionRefreshSequence` temporal-dead-zone error by initializing the counter before the first `render()`; bumped the `app.js` cache key so browsers request the corrected script.
- [x] Restored Overnight candidate selection/purchase to the final 60 minutes of regular trading, with the buy deadline at the actual regular-session close. Added standard-time, daylight-time, after-hours, holiday, and early-close schedule tests.

### Operator evidence from November 10

- [x] API health returned OK and explicitly reported decision-support-only / order submission NOT IMPLEMENTED.
- [x] Alpaca IEX provider status returned VALIDATED_DATA_RETURNED with 1,204 bars.
- [x] Alpaca minute-bar stream reported a prior authentication `connection limit exceeded` error while its snapshot also reported SUBSCRIBED and zero received bars. A zero bar count after the U.S. extended session is not by itself proof of a broken stream; recheck during regular hours. Latest code clears stale error text after a successful reconnect.
- [x] The historical RKLX bar shown by the UI was 11/9 2:46 PM Arizona at $9.7800. It was not a current after-hours quote.
- [x] Scanner Workbench returned zero 1M/5M bars for the supplied 20-symbol set on the configured IEX feed/window. This is feed/window coverage evidence, not a claim that those securities had no market activity.
- [x] Later regular-session stream acceptance: 10-minute operator test acknowledged SPY/RKLX/ASTX/XRPN at start and end; completed-bar deltas were SPY +10, RKLX +3, ASTX +3, XRPN +2. Collector cycles advanced 39→48 with zero cycle errors. This establishes delivery for these symbols in that interval, not full IEX coverage or TOS parity.
- [x] Scanner follow-up: operator imported 50 local rows and completed Scan & Analyze (1M + 5M) on 20 candidates. All returned rows were OBSERVED · NOT QUALIFIED; 5M counts ranged 5–27 bars and 1M counts 6–75 bars. This is a completed diagnostic scan, not Overnight qualification or Boring Winner calculation.
- [x] BAYRY Daily parser re-test completed: API returned 0 valid / 0 available sessions and all 20 expected dates missing (2026-10-13 through 2026-11-09). The parser fix is deployed but does not resolve missing provider observations; keep BAYRY blocked and do not backfill older sessions.
- [x] Corrected countdown state wiring: assign the API's buy/sell timestamps and session dates. During REGULAR phase, the buy countdown now targets the actual regular-session close and applies escalating urgency colors. Final-hour evaluation still begins separately at the scheduled start.
- [x] Corrected Main Chart instrument-name wiring: the metadata refresh function is now called when the focused chart refreshes. Operator UI acceptance is still pending.
- [x] Corrected quick sell-limit evidence visibility: clicking the focused-symbol action opens the local disclosure and mirrors each API-returned PASS/BLOCKED gate with its explanation.
- [x] Added server-side live-assessment stage timings to the response for separate 1m and 5m fetch durations/source paths, combined provider wall time, WebSocket store read, persistence, calculation and total elapsed time.
- [ ] Operator reported approximately 51.8 seconds per live-assessment request for EVR/RKLX/ASTX/SPY. This is an unacceptable UX latency. The dominant stage is not yet established; use the new `performanceDiagnostics` breakdown before selecting an optimization.

- [x] Implemented calendar-backed Overnight execution-window calculator: candidate research and buying are confined to the final 60 minutes of the regular session, ending at that session's actual close (including early closes). This preserves the intended Overnight measurement beginning at the close and including Buy Day after-hours plus Sell Day premarket. Next trading session GTC_EXT sell window remains 7:00 AM–8:00 PM Eastern. Arizona display times are derived from America/New_York and America/Phoenix with DST, Monday–Thursday buy-day selection, Friday exclusion, weekends and holidays handled by the versioned calendar. Schedule only; per-symbol GTC_EXT eligibility and actual fills remain unverified.
- [ ] Validate the displayed schedule against the operator's Schwab order ticket for representative EST/EDT dates and early-close sessions.

### Still blocked — do not fabricate or waive evidence

- [ ] RKLX live/after-hours price source acceptance: verify local Yahoo post-market reference and Alpaca latest IEX trade timestamps against an independently observed quote. Yahoo is secondary indicative data; IEX is partial.
- [ ] Sell-limit remains fail-closed on IEX. The 5-day 1M bars returned by `/api/market-data/bars` do not equal the required 40 complete GTC_EXT sessions, SIP feed, exact reference close, 119-bar 5M TOS ATR parity, exact provider/TOS OHLCV and walk-forward evidence. Use the new disclosure to record every blocked gate.
- [ ] Scanner symbols with zero IEX observations require a properly entitled consolidated source or a clearly labelled alternate feed. Do not change the effective feed silently.
- [x] BAYRY parser fix was re-tested locally; response contained 0/20 expected sessions. This is a genuine empty/incomplete observation result, not a parser exception. [ ] Find an independently supported Daily source or keep qualification blocked; do not retry the unchanged IEX request or backfill older sessions.
- [ ] Run exact-head CI and operator Windows build/runtime/browser acceptance for the latest countdown/title/gate-visibility/performance-diagnostics commits.
- [ ] Compile and verify the revised ThinkScript in Thinkorswim; source changes in GitHub do not prove TOS compilation.

### Boring Winner import — precise input contract

The Dashboard importer expects one CSV row per completed **Daily** bar and the columns `Date, Open, High, Low, Close`. The existing 1M/5M ATR StrategyReport is not the intended input. The current `MCC_OHLCV_ATR_V2.1` StrategyReport is NOT the Daily OHLC import and will not automatically calculate Boring Winner. No dedicated Boring Winner Daily OHLC export study has been delivered yet. The Dashboard importer currently requires a separate CSV with one completed daily bar per row and Date/Open/High/Low/Close columns.

Initial comparison settings: Daily aggregation; Extended Hours OFF so daily Open is the regular-session open used by `open / close[1] - 1`; corporate-action adjustment ON; chart timezone America/Phoenix. Import at least 21 completed daily bars (40–60 is preferred for review). The import records these settings as provenance but does not independently prove TOS session semantics. Boring Winner remains RESEARCH ONLY.

## Boring Overnight UI implementation — 2026-11-09

- [x] Added Previous Regular-Session Close, Latest Price (identified as extended-hours when latest 1M bar is outside RTH), and Calculated Limit Sell to the focused-symbol panel.
- [x] Price fields show Phoenix source timestamps; latest 1M close is not represented as a live quote. Previous Regular-Session Close excludes the in-progress RTH session; exact 16:00 ET wording is used only when the 15:59 ET bar is present.
- [x] Added focused-symbol sell-limit action reusing the existing evidence-gated API and quantile. No duplicate calculator and no order submission.
- [x] Moved 1M close / Daily VWAP / ATR14 / extension diagnostics into the collapsible Decision Explanation. They are calculated from returned 1M bars, labelled diagnostic-only, and carry provider/feed and timestamps. Red Zone hides those redundant metric paragraphs in Boring Overnight mode.
- [x] Boring Overnight Red Zone now uses historical 5M bars and marks intraday 5M/1M gates NOT USED rather than displaying Intraday stale status.
- [x] Manual pinned symbols are polled during Intraday Automatic mode; sound requires a known false-to-true EXECUTION READY transition and enabled sound. Boring Overnight currently has no READY state (the evaluator deliberately keeps READY at 0), so it must not emit an equivalent sound until a validated Overnight-ready definition exists.
- [x] Scanner Workbench Symbols auto-populates with up to 20 unique imported Overnight symbols sorted by Overnight Score; manual Use Overnight Top 20 action is available. It does not submit a scan.
- [x] Removed obsolete Red Zone static chip, redundant label ID, and obsolete explanatory paragraph. Preserved `.gate-warning` and Ticker placeholder.
- [x] Decision Explanation and Timeframe Handoff are collapsible with pointer cursor. Timeframe Handoff is hidden in Boring Overnight mode.
- [ ] Define a source-qualified Boring Overnight READY state and corresponding alert transition; current evaluator intentionally remains fail-closed at READY=0. Do not substitute Boring Winner PASS for READY.
- [x] Latest code-bearing commit `aa60685580c266410df1149779d70d4bc15268ef` passed GitHub Actions JavaScript syntax validation, .NET Release build, and tests (run `42836168264`). The later cache-key-only commit `176b545dd5ef256cb7842e5bd414c5641f6f1f2a` was still running its own CI at the time this note was written.
- [ ] Operator Windows build/run, browser verification, and API/runtime acceptance remain required.

## Operator evidence and implementation — 2026-11-09 (Overnight top-20, mode isolation, TOS formula engine)

- [x] Operator confirmed all eight scanner columns display after prior deployment; some HISTORY values remain DATA? while PRICE and 20D AVG VOL are populated. These evidence fields remain independent.
- [x] Added automatic population of Daily data audit Symbols with the top 20 unique imported Overnight symbols, sorted by Overnight Score, on import and restored import.
- [x] Added a manual **Use Overnight Top 20** action; audit accepts up to 20 symbols and runs no more than four Daily requests concurrently. Population does not automatically launch API requests.
- [x] Made EXCLUSIONS disclosure summary visibly interactive with pointer cursor, hover affordance and keyboard focus outline.
- [x] Persisted workspace mode and restore it before startup requests; Boring Overnight no longer starts the Intraday live-assessment path.
- [x] In Boring Overnight mode, Main Chart uses 5-minute historical bars over a rolling five-day window, updates on selected-symbol changes, and refreshes while visible. Intraday permission/execution cards are hidden and status explicitly says NOT USED.
- [x] Added versioned `MCC-TOS-DAILY-GAP-1.0` calculator reproducing all eight supplied Thinkorswim scanner formulas from explicit TOS daily OHLC bars.
- [x] Added append-only TOS daily reference store, import/read API endpoints, CSV import UI, provenance fields (aggregation, extended-hours, adjustment, timezone), and per-column displayed-precision parity results.
- [x] Added regression tests for all eight fields, score decomposition, insufficient history, duplicate dates and source metadata validation.
- [ ] CI for this code set and operator local build/runtime acceptance are pending.
- [ ] Import matched TOS OHLC exports and compare with scanner CSV for representative symbols; until then formula parity is implemented but empirical per-symbol parity is not yet demonstrated.
- [ ] Exact V3 trade-window Boring Overnight study remains blocked on frozen Arizona-local buy timestamp, sell/measurement endpoint, and official auction vs first eligible trade semantics. Do not substitute daily-gap scanner formula for executable trade return.
- [ ] BAYRY Daily response contract diagnosis remains open.
- [ ] Remaining P0/P1/P2/P3 checklist below remains current; do not mark unresolved external data/entitlement/operator evidence tasks complete.

## Operator evidence and implementation — 2026-11-09 (Overnight formulas + BAYRY)

- [x] Operator supplied all eight active Overnight Scanner ThinkScript columns and the inactive Boring Winner formula in `docs/Column Formulas`; reviewed exact source.
- [x] Documented formula definitions, Overnight Score weighting, Boring Winner gates, screenshot candidates, and a step-by-step MCC workflow in [Overnight Scanner Formula Audit and MCC Workflow](Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md).
- [x] Main Dashboard Overnight qualification table now displays all eight imported scanner metrics (ON Win %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, Overnight Score).
- [x] Historical row status now requires all eight source columns to be present; a missing Median/Momentum/Score field remains DATA?, not PASS or FAIL.
- [x] API-backed EXCLUSIONS section is now collapsible via native details/summary; its refresh/add/remove controls and API behavior remain intact.
- [x] Operator verified BAYRY stream subscription and live assessment both return 200 OK. Daily history request still returns 502 after CSV import and after clearing Red Zone; UI detail: `Alpaca Daily response did not contain a bars array.`
- [ ] Diagnose BAYRY's HTTP-200 upstream payload shape (or unexpected response envelope) and handle valid empty/unsupported-symbol responses explicitly. Current retry handles transient transport/status failures only; it cannot repair a successful response with a missing/wrong bars shape.
- [ ] Operator pull/build/test/restart and runtime verify the collapsible exclusions panel and all-eight-column table.
- [ ] Confirm TOS daily aggregation/session setting: formulas are exactly `open / close[1] - 1`, but code does not specify whether daily `open` reflects regular-hours only or includes extended hours. Match exported TOS bars to MCC data before declaring overnight-window parity.
- [x] Implemented the documented Boring Winner formula as a separate result in the TOS Daily OHLC reference calculator: 20-session win rate, mean return band, count of returns within +0.10%…+3.00%, sample standard deviation, and close above SMA20. Added unit coverage for a qualifying sample. RESEARCH ONLY; not connected to READY.
- [ ] Validate Boring Winner on chronological source-qualified historical samples and confirm daily-bar session/extended-hours semantics before production use. No TOS V3 Boring Overnight study has been compiled.

## Operator evidence — 2026-11-09 (Top-4, Daily provider, catalyst validation)

- [x] Operator confirms MCC Top 4 made requests for exactly four symbols; the article queue currently shows only EWTX.
- [x] Clarified that article cards are article-driven, not a four-symbol query roster. No returned article for another symbol is UNKNOWN, not CLEAR.
- [x] Added a separate four-symbol evidence table so every selected ticker shows Daily outcome, feed/completeness, Daily error, catalyst outcome, article count, catalyst coverage and catalyst error.
- [x] Operator reports BAYRY Daily API returned HTTP 502. Added bounded three-attempt retry for transient network/timeout/429/5xx responses and preserved provider HTTP status in exceptions. API maps provider 422 to 422, 429 to 503, and upstream 5xx to 502. Runtime retry behavior still needs operator verification.
- [x] Operator catalyst validation now shows raw 7, eligible 0, excluded 7, conflicts 0, non-evaluable 2, superseded 3, inadequate evidence 2. No classifier metrics exist; this is correct fail-closed behavior.
- [x] Latest EWTX assessment response establishes the actual triage ruleset as `MCC-CATALYST-TRIAGE-1.0` for the newly returned investor-conference article 117314249. It does not establish the ruleset stored with the older sale-exploration article 117008554.
- [ ] The original EWTX sale-exploration article still requires source-backed adjudication; do not reuse the new conference article's metadata/label.

## Overnight engineering audit — 2026-11-09

- [x] Reviewed current Boring Overnight API, history calculator, evaluator, calendar, Dashboard, source scanner CSV and V3 column requirements.
- [x] Confirmed the current 20-pair calculator computes prior regular-session close to next session 09:30 one-minute bar Open. It includes Day 1 after-hours and Day 2 premarket only in the *net endpoint interval*; it does not calculate the operator's actual buy-time entry or any path/extrema.
- [x] Confirmed the 09:30 one-minute Open is explicitly a proxy, not a verified official opening-auction print.
- [x] Confirmed the current calendar is an MCC-maintained holiday/early-close ruleset, not a live official exchange-calendar source.
- [x] Preserved the scanner CSV's custom-expression subscription-limit messages as missing data, not historical FAIL. Latest operator import: 495 rows, 185 PASS / 14 FAIL / 296 DATA?, exclusions 56 loaded / 0 matches.
- [ ] Exact intended Arizona-local buy-time and sell/measurement endpoints must be frozen against the original TOS scanner formula and operator's confirmed order workflow before replacing the current close/open research metric. The current calculator must not be relabelled as actual trade-window return.
- [ ] Official auction print vs first eligible observed trade remains unverified; Alpaca minute-bar Open cannot certify the auction print. Obtain an authoritative auction/eligible-trade source with entitlement and timestamp semantics.
- [ ] Complete independent RTO/EVR OHLCV parity; prior IEX comparison failed and IEX remains partial-exchange diagnostic data.
- [ ] Qualified independent Daily source remains open. Alpaca SIP entitlement and corporate-action/adjustment equivalence must be established; BAYRY 502 requires operator runtime retry/error verification.
- [ ] Catalyst representative dataset, reviewer agreement and classifier qualification remain open; do not synthesize labels.
- [ ] Opportunity Quality historical calibration remains open; no time-separated outcome-labelled candidate dataset is present in reviewed sources.
- [ ] Thinkorswim V3 Boring Overnight column/chart deliverable remains unimplemented/uncompiled. Do not claim it is done by writing a draft script without TOS compilation and parity.
- [ ] 5M/1M installed-study authority and EOD verification remain operator-side evidence gates.

- [x] Daily transient retry, per-symbol evidence table, and status mapping code/tests passed Release CI run [42659428224](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42659428224) on code commit `59719e6263e7e51a3eafaacf86a0507817f3e2ef`.

## Dashboard runtime follow-up

- [x] Added all-four-symbol live evidence display independently of the catalyst article cards.
- [x] Added transient retry and accurate upstream status mapping for Daily requests.
- [ ] Operator must pull latest branch, build/test, restart API, and repeat MCC Top 4 fetch; verify all four rows populate and inspect BAYRY outcome.
- [ ] Full browser/CDP acceptance, full Dashboard acceptance, regular-session WebSocket proof, collector recovery, and HAR performance measurement remain open.

---

# Momentum Command Center — Master To-Do

**Updated:** 2026-11-09 (operator validation recheck, EWTX rationale hardening, MCC Top-4 distinct-symbol fix)  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261109.md](NewChatHandoff-20261109.md)  
**Detailed engineering acceptance criteria:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Catalyst review procedure:** [Catalyst-Human-Review-Workflow.md](Catalyst-Human-Review-Workflow.md)  
**Technical workflow:** [Technical-Workflow.md](Technical-Workflow.md)  
**Runtime acceptance:** [Runtime-Acceptance-Runbook.md](Runtime-Acceptance-Runbook.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)

This is the current authoritative checklist. The detailed backlog contains implementation boundaries and acceptance criteria. Older historical checkbox states are archived and do not override the current statuses below. Continue on the feature branch; do not modify `master`. Main Dashboard only; do not return to PrototypeIndex.

## Current operator evidence — 2026-11-09

- [x] Operator confirmed the restored six-label catalyst legend looks good.
- [x] Operator deployed the previous rationale guard and supplied validation: raw reviews 6; included unique 0; excluded 6; conflicts 0; training/holdout 0/0; non-evaluable labels 2; superseded reviews 3; inadequate evidence 1.
- [x] This confirms the earlier assistant-appeal rationale is excluded. The validator is correctly withholding metrics.
- [x] Operator supplied new EWTX review `fbc839fc9c2740c1ad87e6bc641e2a77`. It is not an acceptable correction: generic confirmation, not article-specific facts; supersedes `8405880...` rather than weak review `f496393...`; and uses the human-validation version as its `ruleSetVersion`.
- [x] API/historical validator/UI now reject the observed generic confirmation phrases as well as short, placeholder and assistant-appeal rationales. Regression test includes the exact submitted notes. This is a heuristic, not proof of factual accuracy.
- [x] MCC Top 4 now sorts rows by score and deduplicates by ticker, preserving the highest-scoring row per symbol. Duplicate rows can no longer occupy multiple Top-4 slots.
- [ ] Operator pull/build/test/restart and runtime verification of the distinct-symbol Top 4 are pending.
- [ ] EWTX still needs an independently researched correction superseding both active weak reviews `f496393...` and `fbc839...`; see P1.2 below.

## P0 — Data integrity and sell-limit qualification

### ATR V2 / Thinkorswim parity

- [x] ATR V2 StrategyReport import, provenance capture and persisted observations.
- [x] Wilder ATR(14) recurrence and 5m parity comparison with 99-bar warm-up and 20-bar comparison window.
- [x] Timestamp-gap diagnostics scoped to expected 04:00–20:00 ET chart-session slots.
- [x] Exact operator-confirmed no-bar intervals encoded; no synthetic bars or imputation.
- [x] Operator runtime parity: EVR 5m MATCH (327 observations, 20 compared, 0 unresolved gaps, 2 verified no-bar intervals; MAE 0.0000294330, max error 0.0000495349).
- [x] Operator runtime parity: CBOE 5m MATCH (338 observations, 20 compared, 0 unresolved gaps, 1 verified no-bar interval; MAE 0.0000203866, max error 0.0000426076).
- [ ] Compile the revised ThinkScript in Thinkorswim and retain compile/runtime evidence for the exact current source.
- [ ] Complete independent 1m continuity; EVR 1m previously had 13 gaps. Keep 1m independent of the 5m authority gate.
- [ ] Revalidate any future, previously unrecognized timestamp gaps against actual source/chart evidence. Existing verified no-bar windows do not authorize blanket gap suppression.

### Provider/TOS OHLCV parity and sell-limit gate

- [x] Provider comparison orchestration and fail-closed sell-limit gates are implemented.
- [x] Evidence runner distinguishes configured, response and row-level feed identity.
- [ ] Obtain 119 exact provider/TOS 5m OHLCV matches, including exact volume, and the exact previous RTH close from the same qualified feed.
- [ ] Keep sell-limit numeric output blocked until ATR, source provenance, continuity, exact provider/TOS comparison, reference close and all freshness/session gates pass.
- [x] Walk-forward validator implementation exists.
- [ ] Runtime qualify the validator only after a complete source-qualified 40-session dataset exists; report all train/test counts, exclusions and metrics.

### SIP / GTC_EXT history — parked external blocker

- [x] SIP HTTP 403 entitlement denial is established by repeated operator evidence; effective Alpaca feed is IEX.
- [x] IEX is diagnostic-only and is not substituted for SIP.
- [ ] PARKED: no further SIP request, entitlement check, configuration change or capture unless the operator independently reports a genuine entitlement change.
- [ ] If entitlement changes, collect 40 complete sell sessions (20 training + 20 unseen test), each with 780 exact 1m bar-start timestamps and exact previous RTH close; verify source/feed and persisted read-back.
- [ ] Only then run walk-forward runtime qualification. No nearest-bar substitution or imputation.

## P1 — Market data, strategy correctness and dashboard

### Daily market data and liquidity

- [x] Daily API returns completed-session close, SMA20, ADV20, expected/missing session dates, freshness and provenance.
- [x] Daily Dashboard audit panel and PowerShell qualification/evidence runner implemented.
- [x] 2026-11-06 RTO/EVR run passed internal 20-session arithmetic, coverage and freshness checks.
- [x] Same run failed independent consolidated OHLCV parity for both symbols; Alpaca IEX correctly returned `IEX_PARTIAL`, `qualificationUsable=false`, and null qualification fields.
- [ ] Do not repeat the unchanged IEX comparison. Find a genuinely independent, appropriately licensed source/feed or keep qualification blocked.
- [ ] Capture browser screenshot showing the Daily audit UI matches its API evidence.
- [ ] Recheck corporate-action adjustment equivalence before any new independent comparison.

### Catalyst human review and classifier validation

- [x] Ticker-scoped news retrieval and deterministic keyword triage exist; no automatic CLEAR is permitted.
- [x] Append-only human review store, saved prediction capture, conflict diagnostics and explicit supersession exist.
- [x] Chronological holdout validation endpoint and six-label Dashboard legend exist.
- [x] Source/placeholder/short/assistant-appeal rationale guards and historical evidence exclusion exist.
- [x] Latest operator validation correctly reports zero eligible reviews and zero conflicts.
- [x] Generic EWTX affirmation submitted by operator is now explicitly rejected by the strengthened rationale heuristic.
- [ ] Pull/build/restart latest branch and verify generic rationale rejection at runtime.
- [ ] Independently open the original EWTX source article; identify concrete facts, original publisher, and the actual triage ruleset version saved with the prediction. Data not found in sources for the article body and exact saved triage version.
- [ ] Append a source-backed EWTX correction superseding both currently active weak records `f496393535df431ab275231b57f8530a` and `fbc839fc9c2740c1ad87e6bc641e2a77`. Preserve prediction `UNCLASSIFIED` and categories `[]`; independently choose the label.
- [ ] Re-query validation; require conflict=0 and inadequateReviewEvidence=0 for the active EWTX record.
- [ ] Keep ENOV/ARDT and other unsupported articles UNCERTAIN; do not relabel to increase sample size.
- [ ] Collect a substantially larger source-backed sample across MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL and IRRELEVANT. Six eligible records is only a structural floor.
- [ ] Double-review a subset, measure reviewer agreement, and ensure both truth classes occur in the chronological holdout.
- [ ] Measure per-class precision/recall, false positives/negatives, source coverage and time-window coverage. No classifier promotion until representative evidence supports it.

### Opportunity Quality ranking

- [x] Deterministic Opportunity Quality assessment and explicit-symbol API ranking endpoint (1–50 symbols).
- [x] Dashboard top-50 diagnostic ranking control wired to imported scanner-score candidates.
- [x] Candidate results disclose provisional classification/reasons; DATA? and BLOCKED are not promoted.
- [ ] Audit current scanner-field availability by symbol and report missing-field denominators/source timestamps; last historical audit count of 296 missing custom-expression rows is stale.
- [ ] Expand beyond bounded top-50 prefilter only after candidate-pool completeness is evidenced.
- [ ] Calibrate thresholds/rank stability using time-separated candidate snapshots and outcomes; current ranking is diagnostic, not a calibrated probability or market-wide discovery engine.
- [ ] Add full-pool complete/incomplete/stale/excluded deterministic fixtures and UI acceptance.

### Overnight boundary / Boring Overnight

- [x] 09:30 1m open is explicitly labelled `ONE_MINUTE_BAR_OPEN_PROXY`; `officialOpeningAuctionVerified=false`.
- [x] Close-to-next-open endpoint is documented as spanning Day 1 after-hours and Day 2 premarket; it does not reconstruct path/extrema.
- [ ] Verify official auction print versus first eligible observed trade from a properly entitled source. IEX cannot certify consolidated/auction coverage.
- [ ] Resolve exact historical buy/sell endpoints against the original Thinkorswim Overnight scanner formula.
- [ ] Implement separate V3 Boring Overnight watchlist column only after endpoint/session semantics and source-backed metric fields are frozen. Keep it distinct from MCC Opportunity Quality.
- [ ] Keep Overnight Edge and Boringness scores separate; validate score weights and hard gates against known fixtures before use.

### Thinkorswim 5m/1m authority and EOD

- [ ] Compile exact current 5m/1m studies in TOS.
- [ ] Verify 5m Permission remains authoritative and 1m is execution timing only.
- [ ] Export matched completed-bar state labels; OHLCV-only rows cannot prove Permission/READY/RUNNER/WANING/SWING/GET OUT.
- [ ] Verify exact Arizona-local EOD cutoff using bars immediately before, at and after cutoff, including DST/standard-time cases.

### Dashboard, browser, WebSocket and collector

- [x] Main Dashboard is the only supported UI; PrototypeIndex is retired.
- [x] Edge discovery/launch diagnostics and browser CDP capture script implemented.
- [x] Regular-session stream/collector evidence script implemented.
- [x] Dashboard load/API latency instrumentation work was started; prior measurement showed a 10.24s live-assessment request and a pending WebSocket connection, so no overall performance pass is claimed.
- [x] Four-symbol evidence table implemented; [ ] verify MCC Top 4 selects/fetches four distinct tickers after latest pull; if the imported file has fewer than four unique symbols, show the source-data limitation.
- [x] Separate four-symbol evidence table implemented; [ ] verify the post-fetch status lists four distinct queried symbols and the evidence table shows each result. The human-review queue lists only symbols with returned articles, not every symbol queried.
- [ ] Execute browser diagnostics on operator machine; retain screenshot, console exceptions and sanitized network evidence.
- [ ] Complete full Dashboard acceptance: import/parity, Red Zone, exclusions, Morning Brief, replay, sell-limit gate explanations, responsive layout and CDP console/network health.
- [ ] During a confirmed regular session, capture 5–10 minutes of SPY/RKLX/ASTX/XRPN stream/collector data. Verify per-symbol subscriptions, completed bars, timestamps, received/persisted deltas and watermarks.
- [ ] Exercise restart, sleep/wake, network/provider failure, duplicate/replayed events, session boundaries and idempotent recovery.
- [ ] Measure document/load/DOMContentLoaded/largest request/Fetch & Analyze durations from browser Network/HAR; compare to a baseline before claiming improvement.

## P2 — Integrations, execution and deployment

- [ ] Implement a broker execution reconciliation ledger from actual broker order/fill records, including partial fills, quantities, fees, timestamps and cancel/correction lineage. Never infer fills from OHLCV.
- [ ] Schwab read-only integration is blocked on developer approval, app registration, callback and secure OAuth setup. No order submission.
- [ ] Document provider entitlement, scanner universe coverage, licensing, rate limits, retention and symbol/session coverage before production-source selection. Massive remains unconfigured.
- [ ] Define packaging/startup, API lifecycle, local data backup/restore, log rotation, health monitoring and unattended collector recovery before choosing installer technology.
- [ ] Keep order routing explicitly NOT IMPLEMENTED. MCC remains decision-support-only.

## P3 — Release governance and operating workflow

- [x] Main feature branch and technical workflow documented.
- [x] Reliable pull/build/test sequence recorded in the handoff.
- [ ] Operator must run the exact latest branch-head Release build/test and restart local API before runtime acceptance of new code.
- [ ] Every code-bearing branch head must have passing CI; ThinkScript compilation, browser acceptance and operator runtime evidence are separate gates.
- [ ] Update this checklist and the current handoff after each material implementation/evidence change.
- [ ] Never mark external, operator-side or source-dependent work complete from code/CI alone.

## Immediate operator sequence

1. Run the PowerShell sequence in the current handoff from the repository root. Do not reset or discard local changes.
2. Confirm the script reports the latest feature-branch SHA and all Release tests pass; restart the API from that build.
3. In the Dashboard, confirm MCC Top 4 contains four distinct tickers. Click Fetch Daily + catalyst; verify four distinct queried symbols and inspect the per-symbol table. The article review queue may contain only EWTX if the other three returned no articles; no-news is UNKNOWN, not CLEAR.
4. Query catalyst validation again. Expected from current six-record history: zero conflicts, zero included until EWTX is corrected; new generic record should be counted as inadequate evidence after deploying the latest guard.
5. Independently review the EWTX article. Verify the original publisher and saved triage ruleset; append a correction superseding both weak active review IDs. Do not invent source facts or alter the saved prediction.
6. Continue the remaining P0/P1 runtime and evidence tasks above. Do not request another SIP entitlement check.

## [Uncertainties]

- The exact current contents of the user's imported scanner CSV are not available here. The duplicate-row Top-4 defect is fixed, but the import may still contain fewer than four unique symbols.
- Data not found in sources for the EWTX article body, confirmed original publisher, and exact saved prediction ruleset version.
- The latest code changes have individual passing CI runs; exact latest documentation branch-head CI is still in progress at handoff time.
- SIP remains blocked by the previously confirmed HTTP 403 entitlement response; no change in entitlement is evidenced.
- Browser/CDP acceptance, local runtime after latest commit, Thinkorswim compilation, and regular-session collector delivery remain unverified.


## November 10 follow-up — JSONL store latency and Calculated Limit Sell UI


### Operator evidence

- [x] RKLX Main Chart now displays the full ETF name after the focused-chart refresh fix.
- [x] Boring Overnight buy countdown now displays remaining regular-session time and escalates urgency colors.
- [ ] Separate final-hour evaluation clock transition to OPEN remains TBD; do not mark complete until observed at the scheduled Arizona-local start.
- [ ] Calculated Limit Sell quick action still fails operator acceptance: disclosure shows `Assessment running…` and `DATA? — NOT CALCULATED`; complete gate list was not observed. Keep this item open.
- [x] One EVR live-assessment timing sample supplied: client elapsed 12.046s; server diagnostics report provider fetch 487ms (1m 234ms, 5m 491ms), WebSocket-store read 11.832s, persistence 39.284s, calculation 7ms, server total 51.622s. Alpaca REST fetch and indicator calculation are not the measured bottlenecks. The client/server elapsed mismatch remains unexplained.

### Implementation committed on feature branch

- [x] Reworked `JsonlMarketBarObservationStore` to build the latest-observation index once per API process, index keys by symbol, and answer subsequent reads from memory instead of rescanning/deserializing the entire JSONL file for every query.
- [x] Preserve append-only JSONL audit history, idempotent duplicate handling, correction lineage, provider/feed/timeframe isolation, and timestamp ordering.
- [x] Added a regression test for reading existing JSONL after store restart, then appending and reading a correction through the warmed in-memory index.
- [x] Added stage timings to Calculated Limit Sell responses: 40-window history read, exact reference-close read, ATR store read, provider parity-bar read, walk-forward validation, and total assessment duration.
- [x] Dashboard now appends assessment timing details to the result status when the API returns them; cache key bumped to force updated JavaScript.
- [ ] The first store access after API restart still has to rebuild the in-memory index from the existing JSONL file. Measure this cold-start cost separately from warm reads. No database migration or deletion/compaction of source observations was performed.
- [ ] Verify warm-read performance and append persistence on the operator's actual data file; do not claim the latency issue is resolved from code inspection alone.
- [ ] Verify Calculated Limit Sell now completes, renders every PASS/BLOCKED gate, and exposes stage timings. If still slow, use the new stage breakdown to isolate the remaining stage.
- [x] Latest code-bearing commit 1f5275c9448eb3b1608fadee847d37ac855044e8 passed GitHub Actions frontend JavaScript syntax validation, .NET Release build, and .NET tests.
- [ ] Documentation-only follow-up commits are newer; their latest workflow and local Windows/API/browser acceptance remain pending.

### Updated acceptance order

1. Pull `feature/opportunity-quality-classification`, verify clean status and branch, then run ` .\\scripts\\Get-Latest-And-Test.ps1` from the repository root.
2. Restart the API and hard-refresh the Dashboard. The script cache key is `20261110-jsonl-index-sell-limit-timings-v1`.
3. Run one EVR live-assessment request first to trigger the one-time JSONL index rebuild after restart; record it as the cold/warm-up request.
4. In **BORING OVERNIGHT** mode, click **Assess calculated limit sell** once and allow the request to finish. Confirm the disclosure shows all gate rows, including explicit BLOCKED reasons. Record `performanceDiagnostics` from the response or the timing summary displayed in the status.
5. Run one further EVR live-assessment request to measure the warm path. Record client elapsed and all server diagnostics; do not compare the cold read directly with the warm request.
6. Verify the final-hour evaluation clock changes to OPEN at its scheduled start. This remains the only outstanding clock UI check.

### Guardrails

- Do not change Alpaca IEX, switch to SIP without a confirmed entitlement change, reduce sell-limit evidence gates, impute missing timestamps, or treat IEX as consolidated coverage.
- No order is submitted by the sell-limit assessment. PROVISIONAL remains research-only.
- Existing completed stream, scanner, BAYRY, and RKLX-name checks do not need to be repeated unless the relevant code/data changes.


## November 10 follow-up — measured store improvement and Calculated Limit Sell correction

### Operator measurements after the in-memory JSONL index change

- [x] Cold/warm-up EVR request: client 0.649s; server total 475.2ms; WebSocket-store read 7.1ms; persistence 3.5ms; calculation 30.9ms.
- [x] Warm EVR request: client 0.399s; server total 391.5ms; WebSocket-store read 4.1ms; persistence 6.7ms; calculation 3.5ms.
- [x] Confirmed warm-path improvement against the prior sample: client latency fell from 12.046s to 0.399s; the previous server report of 51.622s is now 391.5ms. This is an observed before/after sample, not a multi-symbol performance guarantee.
- [x] The newer client/server timing boundaries are close: 174ms difference on the first request and about 8ms on the warm request.
- [ ] The 7.1ms WebSocket-store read does not by itself prove that the process-local JSONL index rebuild was performed during that request. The store may have been initialized earlier by startup/background activity. Added explicit jsonlIndexLoadCount, jsonlIndexLastLoadElapsedMs, jsonlIndexUniqueObservationKeys, and jsonlIndexLoaded fields to the live-assessment diagnostics so the next restart test measures the actual one-time load.

### Calculated Limit Sell root cause and fix

- [x] Traced the no-Network click behavior to the frontend path: the quick button displayed “Assessment running…” and programmatically clicked a second button; that handler returned before its first fetch when the quantile field was blank or the symbol invalid. The quantile input had no default and was located away from the quick action. This is the source-code explanation consistent with no Network activity; the exact operator DOM value at the failed click was not captured.
- [x] Moved the quantile input into the quick card and set a clearly labelled 50th-percentile (median) default. The user can change it before assessment.
- [x] Removed the synthetic button click. Both quick and detailed actions now call the same async assessment function directly.
- [x] Validation failures now appear in the quick disclosure/status rather than leaving a false “Assessment running…” state. The quick action disables during the request and re-enables on completion/error.
- [x] Success and failure paths mirror API gate details into the quick disclosure; a completed BLOCKED response remains a valid assessment result and is not presented as a calculated price.
- [ ] Operator browser retest is still required to verify the request is sent and all gates are rendered.

### Cold index instrumentation and regression coverage

- [x] Added process-local index diagnostics to JsonlMarketBarObservationStore: loaded flag, number of index builds, most recent build duration, and number of unique source keys indexed.
- [x] Added those values to live-assessment performanceDiagnostics. jsonlIndexLastLoadElapsedMs describes the most recent process-local rebuild and may have happened before the current request; compare it with jsonlIndexLoadCount after a fresh API restart.
- [x] Extended store regression test to assert one index build, one unique source key, and no second rebuild during a later correction append/read.
- [x] Added exact-boundary market-session clock test: evaluation opens at the exact final-hour start and closes at the exact buy deadline.
- [x] Existing API clock tests already cover before/inside the final hour, DST, after-hours, holiday/weekend skips, and early close.
- [ ] Actual browser clock transition on the operator's machine remains unverified. The Dashboard recalculates once per second; runtime observation at the scheduled Arizona-local boundary is still required.

### Next operator acceptance

1. Pull the latest feature branch, run the Release build/test script, and restart the API.
2. Hard-refresh the Dashboard. Cache key: 20261110-sell-limit-quick-action-index-diagnostics-v3.
3. In BORING OVERNIGHT mode, select RKLX and confirm the quick card shows the Research target quantile (%) field defaulted to 50. Change it only if a different percentile is intended.
4. Click Assess calculated limit sell once. Confirm Network shows GET /api/providers/status followed by POST /api/boring-overnight/sell-limit. Confirm the quick disclosure contains every API gate. Record status and timing diagnostics. If no request occurs, inspect console for a JS exception and capture the field value.
5. After API restart, run one EVR live-assessment request and record jsonlIndexLoadCount, jsonlIndexLastLoadElapsedMs, jsonlIndexUniqueObservationKeys, jsonlIndexLoaded, websocketStoreReadElapsedMs, persistenceElapsedMs, and client elapsed. Expect index load count 1 for a freshly initialized store instance; if already loaded at API startup, the request reports the startup rebuild duration.
6. Run a second EVR request to confirm index load count stays 1 and warm-read times remain low.
7. Verify the evaluation clock says OPEN at the scheduled final-hour start and returns to the countdown/closed state at the regular-session buy deadline.

### Guardrails

- Median/50th percentile is a visible research default only; it does not relax evidence gates or assert a validated trading target.
- Keep IEX as configured. No order submission, feed substitution, missing-data imputation, or qualification-gate changes.
- The local/browser checks above cannot be completed from repository CI alone.
