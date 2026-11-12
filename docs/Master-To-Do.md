> **Priority directive — 2026-11-11:** Schwab integration is the lowest-priority workstream and is explicitly parked. Do not request a Schwab export, begin Schwab API/OAuth work, or let either block current MCC engineering. Broker execution reconciliation is also parked until broker-record work is deliberately reprioritized and real source records are available. Continue work that can improve or validate the existing MCC independently: (1) performance diagnosis and JSONL cold-index optimization, (2) complete Boring Overnight workflow and candidate evidence review, (3) Opportunity Quality data-quality/coverage audit and deterministic fixtures, (4) Thinkorswim study/formula parity using available operator exports, (5) runtime/browser/collector acceptance and interactive Windows deployment/recovery, and (6) exact-head CI/release evidence. Distinguish repository work we can perform now from operator-side evidence gates; do not mark the latter complete without evidence.

---

## November 12 — Engineering progress and next acceptance gates

### Implemented in repository on `feature/opportunity-quality-classification`

- [x] Added JSONL cold-index stage instrumentation: file bytes, lines scanned, total elapsed time, deserialization elapsed time, index insertion elapsed time, unique indexed keys and load count.
- [x] Exposed the new measurements in live-assessment `performanceDiagnostics`; timings describe the most recent process-local index rebuild, not necessarily work performed by the request reporting them.
- [x] Added store regression assertions for the measured file/line counts and stage timings.
- [x] Corrected the diagnostics record contract after the first CI build exposed a missing record-property update. The initial instrumentation commits failed CI at compilation; the contract fix is now committed and a fresh exact-head CI run is required.
- [x] Re-audited Opportunity Quality implementation: it is a diagnostic overlay over existing entry eligibility, with missing/stale/future timestamps classified DATA? and ineligible candidates BLOCKED. Pool ranking is deterministic and explicitly uncalibrated. Existing unit tests cover several isolated states.
- [x] Re-audited Boring Overnight evaluator: its pure evaluator can return READY only when all supplied fields satisfy its gates. This does not prove that the production workflow supplies source-qualified inputs or that a READY alert transition is wired. Keep those integration/evidence tasks open.
- [x] Re-audited Alpaca Daily handling: normal array, symbol-keyed array, null/empty observations and transient HTTP retry paths have tests. BAYRY's actual IEX result remains 0/20 expected sessions; a parser change cannot create missing provider data.

### Still open — repository work we can continue without Schwab

- [ ] Add end-to-end Opportunity Quality fixtures that pass complete, incomplete, stale and excluded candidate snapshots through the actual API/UI workflow; current unit tests are not full-pool/browser acceptance.
- [ ] Refresh scanner-field availability denominators and per-symbol source timestamps from a current candidate pool. Historical count of 296 missing custom-expression rows is stale; no current denominator was found in the reviewed sources.
- [ ] Profile the actual local JSONL file and assessment stages after a fresh API restart. The new instrumentation has not yet been measured against the operator's file.
- [ ] Measure warm reads, append persistence and first-request/host-startup cold-load behavior separately. Do not change the append-only persistence format until the profile supports a safe design.
- [ ] Complete the Boring Overnight workflow and candidate evidence review, including import row reconciliation, shortlist/top-20, four distinct Top 4 requests, Daily/catalyst outcomes and sell-limit gate disclosure.
- [ ] Complete browser/CDP, WebSocket, collector recovery and interactive Windows start/second-start/graceful-stop acceptance.
- [ ] Finish the durable-path inventory before implementing backup/restore; exact snapshot filenames, imported source retention, logs and all overrides must be verified.
- [ ] Add exact-head release evidence after CI; local Windows build/runtime, browser, TOS compile and independent feed evidence remain separate gates.

### Operator/source gates — do not mark complete from repository code

- [ ] Pull the latest feature branch and run the detailed validation sequence in the current handoff below.
- [ ] Capture one fresh-process live-assessment response including the complete `performanceDiagnostics` object.
- [ ] Review CRBG, FG, IBN and HDB Daily/catalyst/price/volume/exclusion evidence; IEX_PARTIAL is not consolidated coverage and zero articles is UNKNOWN.
- [ ] Keep BAYRY, LYSDY and BNPQY qualification blocked until a genuinely supported source supplies validated expected sessions with documented provenance.
- [ ] Compile the exact current ATR V2 studies in Thinkorswim and retain compile/chart-settings evidence; compare matched TOS OHLC exports to MCC scanner values.
- [ ] Verify Overnight clock schedule against the operator's actual order ticket for standard time, daylight time and early close; do not infer fills or official auction prints from bars.
- [ ] Keep sell-limit output fail-closed until all source, feed, ATR, OHLCV, continuity, reference-close, 40-session and walk-forward requirements pass.

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
- [ ] Wire and validate a source-qualified Boring Overnight READY workflow and alert transition. The pure evaluator can return READY when all caller-supplied gates pass, but production input provenance and alert wiring are not yet evidenced. Never substitute Boring Winner PASS for READY.
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

**Updated:** 2026-11-10 (Boring Overnight five-metric browser acceptance; P0/P1/P2/P3 status recheck)  
**Working branch:** `feature/opportunity-quality-classification`  
**Main Dashboard:** `frontend/index.html` at `http://localhost:53360/`  
**Current handoff:** [NewChatHandoff-20261110.md](NewChatHandoff-20261110.md)  
**Detailed engineering acceptance criteria:** [Active-Engineering-Backlog-20261106.md](Active-Engineering-Backlog-20261106.md)  
**Catalyst review procedure:** [Catalyst-Human-Review-Workflow.md](Catalyst-Human-Review-Workflow.md)  
**Technical workflow:** [Technical-Workflow.md](Technical-Workflow.md)  
**Runtime acceptance:** [Runtime-Acceptance-Runbook.md](Runtime-Acceptance-Runbook.md)  
**Historical checklist archive:** [Master-To-Do-History-20261106.md](archive/Master-To-Do-History-20261106.md)

This is the current authoritative checklist. The detailed backlog contains implementation boundaries and acceptance criteria. Older historical checkbox states are archived and do not override the current statuses below. Continue on the feature branch; do not modify `master`. Main Dashboard only; do not return to PrototypeIndex.

## November 10 — Boring Overnight Main Chart acceptance received

- [x] Operator confirmed the new `app.js` cache key `20261110-overnight-main-chart-metrics-v1`.
- [x] Operator accepted all five Boring Overnight Main Chart metric fields: 1M CLOSE $35.8900; DAILY VWAP $35.9837 (2026-11-10 RTH, 285 volume bars); ATR14 $0.0314 (1,021 ranges available, 14-bar value); RELATIVE VOLUME NOT USED; ATR EXTENSION 2.98 ATR.
- [x] Operator confirmed Intraday Scanner Workbench is hidden in Boring Overnight and searchable by its exact heading in Intraday mode.
- [x] Five-metric and scanner-visibility browser acceptance is complete. Detailed evidence and limitations: [Boring Overnight Main Chart browser acceptance](Validation/Boring-Overnight-Main-Chart-Acceptance-20261110.md).
- [ ] Full Boring Overnight workflow remains open: import/current CSV row reconciliation, shortlist/top-20, Boring Top 4 Daily/catalyst fetch, per-symbol evidence review, and Calculated Limit Sell disclosure.
- [ ] P0 ATR/TOS, independent provider parity and SIP gates remain blocked by Thinkorswim/operator evidence and external feed entitlement; do not mark complete from this UI acceptance.

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
- [x] Bounded 10-minute regular-session sample: SPY/RKLX/ASTX/XRPN subscriptions acknowledged; completed-bar deltas SPY +10, RKLX +3, ASTX +3, XRPN +2; collector cycles 39→48 with zero cycle errors. This is one interval only, not coverage or recovery certification.
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
- No GitHub status checks or PR-triggered workflow runs were returned for the latest documentation branch head `86c2bb9fe32d5e810523cd6bc453f11e4e08550f`; exact-head CI is therefore NOT VERIFIED. Earlier code-bearing CI results do not automatically apply to later commits.
- SIP remains blocked by the previously confirmed HTTP 403 entitlement response; no change in entitlement is evidenced.
- Operator manually accepted the Boring Overnight five-metric cards and mode-specific scanner visibility on 2026-11-10. Full Dashboard/CDP acceptance remains open. A prior 10-minute regular-session stream/collector run showed SPY/RKLX/ASTX/XRPN completed-bar deltas and 9 error-free collector cycles; restart/sleep/wake/network/auth/replay recovery remains untested. Thinkorswim compilation remains unverified.


## November 10 follow-up — JSONL store latency and Calculated Limit Sell UI


### Operator evidence

- [x] RKLX Main Chart now displays the full ETF name after the focused-chart refresh fix.
- [x] Boring Overnight buy countdown now displays remaining regular-session time and escalates urgency colors.
- [ ] Separate final-hour evaluation clock transition to OPEN remains TBD; do not mark complete until observed at the scheduled Arizona-local start.
- [x] Subsequent operator retest: Calculated Limit Sell API assessment returned the full 1,600-row evidence payload and the expected BLOCKED gate set (SIP_REQUIRED, HISTORY_40_OF_40, ATR_STUDY_SETTINGS, ATR_PARITY, CONTINUOUS_5M_ATR_WARMUP, SAME_SOURCE_5M_OHLCV, CURRENT_5M_ATR_FRESHNESS, WALK_FORWARD_VALIDATION, SELL_LIMIT_CALCULATION). This supersedes the earlier no-Network / Assessment running observation.
- [ ] Browser disclosure acceptance remains open: verify GET `/api/providers/status` then POST `/api/boring-overnight/sell-limit`, and confirm every returned gate is visible in the quick-card disclosure.
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
- [x] The 7.1ms WebSocket-store read did not prove whether the process-local JSONL index rebuild occurred during that request. Added index-load count/state, elapsed time, unique keys, file bytes, lines scanned, deserialization time and index-insertion time to live-assessment diagnostics. [ ] Operator must measure these after a fresh API restart.

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
7. Verify the evaluation clock says OPEN at the scheduled final-hour start and returns to the countdown/closed state at the regular-session buy deadline. This remains a runtime acceptance item; it is not the only remaining dashboard or release gate.

### Guardrails

- Median/50th percentile is a visible research default only; it does not relax evidence gates or assert a validated trading target.
- Keep IEX as configured. No order submission, feed substitution, missing-data imputation, or qualification-gate changes.
- The local/browser checks above cannot be completed from repository CI alone.


## November 10 — watchlist review, sell-limit runtime evidence, and cold-index measurement

### Operator evidence received

- [x] Provider status confirms Alpaca is configured and validated on **IEX**; Massive is not configured. Keep IEX unchanged.
- [x] Calculated Limit Sell now returns the full evidence-gate response instead of remaining at “Assessment running.” This establishes that the API assessment executed and returned its 1,600-row evidence payload. The result is correctly BLOCKED: SIP_REQUIRED, HISTORY_40_OF_40, ATR_STUDY_SETTINGS, ATR_PARITY, CONTINUOUS_5M_ATR_WARMUP, SAME_SOURCE_5M_OHLCV, CURRENT_5M_ATR_FRESHNESS, WALK_FORWARD_VALIDATION and SELL_LIMIT_CALCULATION.
- [x] EXACT_REFERENCE_CLOSE passed at $10.71 from the exact 1m bar timestamp 2026-11-09T20:59:00Z (Alpaca/IEX).
- [x] Sell-limit gate outcome is fail-closed as designed. IEX is partial coverage and cannot qualify the 40 complete SIP GTC_EXT windows. Missing TOS ATR/parity and 5m warm-up observations also correctly prevent target publication. Do not relax gates or impute missing timestamps/reference closes.
- [x] Cold JSONL index rebuild has now been measured: index loaded=true, load count=1, 16,521.1ms most recent rebuild, 230,838 unique observation keys. This is the one-time process-local rebuild duration and may have occurred at API startup, not during the EVR request.
- [x] EVR live assessment measured 428.7ms client / 311.3ms server total; provider fetch 269.3ms; 1m 156.5ms; 5m 270.8ms; WebSocket store read 4.1ms; persistence 3.9ms; calculation 26.9ms. This is a fast warm assessment; do not conflate it with the 16.5-second cold rebuild.
- [x] Analyzed the supplied 50-row November 10 11:38 AM AZ Overnight Watchlist Scanner export. Added [Overnight Watchlist Analysis and MCC Diagnosis](Overnight-Watchlist-Analysis-and-MCC-Diagnosis-20261110.md) with a top-10 candidate-by-candidate review and the intended MCC workflow.
- [ ] Calculated Limit Sell browser acceptance is not fully closed until the operator confirms the Network sequence (GET /api/providers/status then POST /api/boring-overnight/sell-limit) and confirms every gate is rendered in the quick-card disclosure. The returned payload is strong API-path evidence, not proof of the full browser presentation.
- [ ] Cold-index performance optimization remains open. The diagnostic gap is closed; the measured 16.5-second rebuild is now a quantified startup/cold-load performance issue. Profile file size/line count, deserialization, key construction, allocation and index structure before changing persistence. Preserve append-only correction semantics and exact latest-observation selection. Do not introduce an unverified cache/snapshot format.
- [ ] Repeat EVR twice after a fresh API restart if startup timing needs confirmation; record the first request and second request separately. The current response indicates index build count 1, but does not tell whether the load occurred during host startup or the first request.
- [ ] Top-10 candidate diagnosis remains research-only until MCC Daily source completeness, catalyst coverage, exclusion, price/volume, spread/liquidity and chart context have been reviewed. The CSV contains no raw 20-session returns or live execution data.
- [ ] Keep the sell-limit blocked on IEX until a legitimately configured consolidated SIP source and all required persisted TOS ATR/OHLCV parity observations are available. Massive is currently NOT_CONFIGURED; do not assume its entitlement or coverage.

### Top-10 interpretation summary

- SMX is an extreme outlier (Current ON 92.41%, 5D ON 23.19%, AVG ON 6.23% versus Median ON 0.66%); investigate event/corporate-action/liquidity context separately.
- WRBY has the strongest balanced non-SMX ranking profile, but Current ON is -1.19%.
- SEZL has strong momentum (+4.93) but only 0.16% median.
- ETON has strong recent acceleration but -1.12% current gap and 0.20% median.
- IRWD/STEP/ACHC have current ON 0.00%; inspect full history and catalysts.
- OPRT has -0.95% current ON; ULCC has the strongest top-10 median (0.78%) and +0.96% current ON; ARDT has +0.61% current ON.
- All top 10 are Above 20 SMA, so that column does not discriminate within this cohort. Overnight Score is a ranking heuristic, not a probability or trade instruction.

### Intended MCC workflow reminder

Use BORING OVERNIGHT mode. Import the exact scanner CSV; review 50 imported rows; use **Use Overnight Top 20** in Daily data audit; click **Validate Daily data** and inspect source/feed/completeness/freshness; run **Fetch Daily + catalyst for MCC Top 4** (or My Top 4); inspect each candidate's complete evidence row; open Main Chart in Boring Overnight mode; use Calculated Limit Sell only as a fail-closed evidence diagnostic. IEX remains partial and cannot satisfy SIP qualification. No order submission is implemented.


## November 10 — Boring Overnight flow, Daily audit and new 13:44 export

- [x] Moved CSV import into the top of Overnight Candidate Workspace; retained the existing CSV parser and storage behavior. A fresh import now resets Red Zone to Automatic while preserving saved manual pins, preventing a persisted empty Manual view from suppressing the new candidates.
- [x] Hide Intraday Scanner Workbench controls in Boring Overnight mode so market movers, scan inputs and 1M/5M analysis are not required for the Overnight workflow.
- [x] Replaced Intraday Red Zone card content in Boring Overnight with historical Overnight metrics; no Intraday V2 5M permission / 1M execution labels or Intraday live-assessment calls in these cards.
- [x] Added Boring Overnight Shortlist ranking: existing historical screen plus complete source metrics, Median ON >= AVG ON, and absolute Current ON below 3%; win-rate-first, then median, average/median consistency and lower absolute momentum. Display-only; does not change qualification gates.
- [x] Candidate qualification automatic cohort now uses the same Boring Top 4. The action is "Fetch Daily + catalyst for Boring Top 4"; My Top 4 remains manual.
- [x] Daily Data Audit heading corrected; Missing Sessions NONE green, missing date list red, UNKNOWN neutral.
- [x] Added 13:44 scanner export top-three analysis to docs/Boring-Overnight-Scanner-Analysis-20261110-1344.md.
- [ ] Browser acceptance for new import placement, shortlist cards, Daily audit coloring and Boring Top 4 evidence fetch.
- [ ] Review Daily/catalyst/price/volume/exclusion evidence for CRBG, FG and IBN before any qualification decision.
- [ ] Continue SIP/ATR evidence blockers and 16.5-second JSONL cold-index performance work independently; do not weaken gates.


## November 10 — Candidate qualification and focused Main Chart loading-state follow-up

### Operator evidence

- [x] Candidate qualification queried CRBG, FG, IBN and HDB independently. All four Daily requests returned 20/20 expected sessions and FRESH on Alpaca IEX; the feed remains explicitly IEX_PARTIAL, so these are complete expected-session observations but not consolidated-market qualification.
- [x] Catalyst coverage is COMPLETE for all four query attempts. FG returned one article with MATERIAL_EVENT_REVIEW; CRBG, IBN and HDB returned zero articles and correctly remain UNKNOWN rather than CLEAR.
- [x] Daily Data Audit Missing Sessions coloring was operator-accepted: NONE green; missing dates red; UNKNOWN neutral.
- [x] LYSDY and BNPQY returned 0/20 expected sessions with the latest expected session missing. Their missing-date lists remain explicit; no backfill or inferred values.
- [ ] Investigate supported Daily data for LYSDY and BNPQY. Keep qualification blocked unless a source returns independently validated observations with documented feed/adjustment/session provenance.
- [ ] Review source-backed Daily/catalyst evidence for CRBG, FG, IBN and HDB before any qualification decision. IEX_PARTIAL is not consolidated coverage; zero articles remains UNKNOWN.

### Focused Main Chart — five metric cards stuck at Loading

- [x] Source inspection confirmed that, in INTRADAY MOMENTUM mode, 1M CLOSE, DAILY VWAP, ATR14, RELATIVE VOLUME and ATR EXTENSION are populated by the shared `/api/market-data/live-assessment` request. Boring Overnight has a separate historical renderer and must not call that Intraday permission endpoint. Existing success and error paths update all five cards, but the request had no client-side timeout; a fetch/body read that never settles could leave the initial Loading values indefinitely.
- [x] Added a 20-second AbortController timeout around the complete HTTP response and JSON-body read. A timeout cancels the request and enters the existing fail-closed error path, replacing metric Loading values with DATA? / UNKNOWN and showing a specific timeout explanation in the classification headline.
- [x] Preserved per-symbol in-flight request de-duplication and the existing request-sequence guard. No indicator formulas, qualification gates, provider/feed settings or READY logic were changed.
- [x] Bumped the frontend script cache key to `20261110-live-assessment-timeout-v1`.

- [x] Isolated V8 harness compiled the updated `fetchLiveAssessment()` function, simulated a stalled fetch/abort, observed the explicit 20-second timeout message, and verified the in-flight map entry is removed. This is a focused function-level check, not full browser, exact-head CI, or Windows runtime acceptance.
- [ ] Exact-head GitHub Actions syntax/build/test verification.
- [x] Operator browser acceptance: CRBG's five Main Chart metrics resolved successfully in Intraday mode.
- [x] Boring Overnight five-metric browser acceptance: operator confirms cache key loaded, all five cards populated (RVOL explicitly NOT USED), and Intraday Scanner Workbench mode visibility. See [acceptance evidence](Validation/Boring-Overnight-Main-Chart-Acceptance-20261110.md).
- [ ] Verify a timeout for one symbol does not prevent a later retry or another symbol's assessment; confirm the aborted in-flight entry is removed and a successful subsequent request repopulates all five metrics.
- [ ] If timeout reproduces, inspect the request duration and API `performanceDiagnostics`; do not treat the timeout as a root-cause fix for backend slowness.

### Acceptance and safety boundaries

- Keep Alpaca configured to IEX unless the operator reports a verified entitlement change. Do not retry SIP or infer consolidated coverage.
- No missing Daily sessions, prices, catalyst facts, ATR observations or timestamps may be imputed.
- No-news is UNKNOWN, not CLEAR. Complete expected session count does not upgrade IEX_PARTIAL to consolidated coverage.
- MCC remains decision-support-only; order submission is NOT IMPLEMENTED.


## November 10 — Boring Overnight Main Chart metrics correction

### Operator evidence and source diagnosis

- [x] Operator confirmed the new cache-key deployment produced no visible errors and the Intraday CRBG Main Chart test succeeded. A clean console alone is not definitive proof of the loaded script URL; verify the Network script request when convenient.
- [x] Operator confirmed all five Main Chart metric fields remained at Loading in Boring Overnight: 1M CLOSE, DAILY VWAP, ATR14, RELATIVE VOLUME and ATR EXTENSION.
- [x] Traced the mode split: `refreshDecisionExplanation()` deliberately returns early in Overnight mode, while `refreshOvernightPriceSummary()` previously populated only the separate Previous Close / Latest Price cards and the Decision Explanation. The shared five metric cards therefore had no Overnight writer. This is a distinct UI wiring defect, not the live-assessment timeout.
- [x] Confirmed Intraday Scanner Workbench is hidden by `body.boring-overnight-mode #scanner-workbench{display:none!important}` in `frontend/styles.css`. Its visible heading was renamed to **Intraday Scanner Workbench** so the feature is searchable when Intraday mode is active.

### Implemented correction

- [x] Added a dedicated Boring Overnight writer for all five Main Chart metric cards, using the existing historical 1M bars request and existing RTH VWAP / simple 1M ATR14 / ATR-extension diagnostic calculations.
- [x] 1M CLOSE is explicitly the latest returned historical 1M bar close, not a live quote or execution price.
- [x] DAILY VWAP is labelled as the last available RTH 1M typical-price VWAP proxy, with its session date and contributing volume-bar count.
- [x] ATR14 retains the existing simple average of the latest 14 returned 1M true ranges; the UI displays available-range count and DATA? if fewer than 14 exist.
- [x] RELATIVE VOLUME is explicitly **NOT USED** in Boring Overnight. Intraday same-time 5M RVOL is not fabricated or reused as an Overnight qualification metric.
- [x] ATR EXTENSION is explicitly a historical descriptive calculation: absolute latest 1M close minus RTH VWAP divided by the 1M ATR14. It is not an entry/exit signal.
- [x] Added a 20-second timeout for the Overnight historical bars request, fail-closed DATA? states on errors, a stale-symbol/request sequence guard, and cancellation of the previous summary request when a new one starts.
- [x] Selected the latest completed RTH VWAP session using the current New York session clock, so a missing 16:00 ET after-hours bar does not incorrectly hide the just-completed session's VWAP.
- [x] Kept the optional latest-trade and Yahoo reference requests separate from the five historical metrics. The five metrics are populated before those optional reference calls finish.
- [x] Updated the metric helper text and frontend cache key to `20261110-overnight-main-chart-metrics-v1`.
- [x] Operator browser acceptance: all five Boring Overnight Main Chart fields populated with expected provenance/limitations; scanner visibility verified in both modes.
- [ ] Exact-head GitHub Actions and operator Windows Release build/test remain separate release gates.

### Next operator acceptance

1. Pull `feature/opportunity-quality-classification`, run the repository test script and restart the API.
2. Hard-refresh the Dashboard. Confirm the loaded `app.js?v=20261110-overnight-main-chart-metrics-v1` request in DevTools Network.
3. Switch to **BORING OVERNIGHT**, select CRBG, and wait for the historical bars request.
4. Confirm 1M CLOSE shows a historical close with a Phoenix timestamp; DAILY VWAP shows a value or DATA? with the RTH session and contributing bar count; ATR14 shows a value or DATA? with the range count; RELATIVE VOLUME says NOT USED; ATR EXTENSION shows a value or DATA?.
5. Confirm none of the five fields remains Loading after the request settles. If the bars request hangs, it must time out within 20 seconds and show DATA? plus a clear message.
6. Switch to FG or IBN and back to CRBG. Confirm stale responses from the previous symbol do not overwrite the selected symbol's metrics.
7. Switch to INTRADAY MOMENTUM and confirm the Intraday metric path still works; verify 5M permission and 1M execution remain exclusive to Intraday mode.
8. In Boring Overnight, verify the section headed **Intraday Scanner Workbench** is absent. In Intraday mode, search for that exact heading if needed.

### Current operator-supplied Daily audit evidence

- [x] CRBG, FG and IBN each show 20/20 expected sessions, FRESH, latest expected/actual date 2026-11-10, Alpaca/IEX, split/dividend adjusted, and Missing Sessions NONE.
- [x] Their qualification remains `DATA? · IEX_PARTIAL`. Complete expected-session count does not establish SIP/consolidated coverage.
- [ ] Continue source-backed Daily/catalyst review for CRBG, FG, IBN and HDB. Zero articles remains UNKNOWN, not CLEAR.
- [ ] LYSDY and BNPQY remain blocked at 0/20 until an independently supported source returns validated observations.

### Guardrails

- Do not call the Overnight 1M ATR14 a daily ATR14. It is a 1-minute-bar simple average true range.
- Do not compute or infer Overnight RVOL from missing same-time baselines; it is NOT USED in this workspace.
- Do not promote any historical diagnostic to READY, execution permission, a quote, or a trade instruction.
- Keep IEX_PARTIAL, SIP, TOS ATR parity, and provider/TOS OHLCV blockers unchanged.


## November 10 operator acceptance — remaining work decomposition

### Accepted in the latest browser run

- [x] Boring Overnight Main Chart five-metric rendering and scanner visibility — see `docs/Validation/Boring-Overnight-Main-Chart-Acceptance-20261110.md`.
- [x] The metric values and provenance are captured exactly as reported by the operator; no independent calculation or quote status is inferred.

### P0 — external/operator evidence still required

- [ ] ATR V2 exact Thinkorswim compile: open the exact revised study in TOS, compile, and preserve the editor/compiler result plus chart/runtime settings. GitHub source and CI cannot establish TOS compilation.
- [ ] Independent provider/TOS OHLCV parity: use a genuinely independent, entitled source with exact symbol/session/timeframe/adjustment alignment. IEX is partial and the SIP 403 is already established. Do not retry SIP unless the operator reports an actual entitlement change.
- [ ] 1m ATR continuity: preserve 1m as an independent gate; existing EVR 1m gap history remains a separate blocker.
- [ ] GTC_EXT sell-limit remains blocked until qualified feed, 119 exact 5m OHLCV rows, ATR V2 settings/parity/warm-up, exact previous RTH close, complete 40-session eligible-window history and walk-forward validation all pass.

### P1 — operator/source/data work still required

- [ ] LYSDY and BNPQY Daily: configured IEX returned 0/20. Do not retry unchanged IEX or backfill. Identify a genuinely independent source only if available and document adjustment/session/coverage provenance; otherwise leave DATA?.
- [ ] CRBG/FG/IBN/HDB: perform source-backed Daily/catalyst/price/volume/exclusion reviews. 20/20 expected sessions and FRESH do not upgrade IEX_PARTIAL; no returned articles remain UNKNOWN.
- [ ] Opportunity Quality: refresh source-field availability denominators and timestamps, then obtain time-separated candidate snapshots/outcomes for calibration. No calibration dataset or measured out-of-sample results are currently present.
- [ ] Full Boring Overnight workflow: verify current import row count, shortlist/top-20, four distinct Boring Top 4 requests, per-symbol Daily/catalyst evidence, and candidate review.
- [ ] Calculated Limit Sell: verify browser Network sequence (provider status then sell-limit POST) and all returned PASS/BLOCKED gate rows in the disclosure. Existing API evidence is BLOCKED; this is not a usable price target.
- [ ] Overnight evaluation clock: compare displayed final-60-minute start and buy deadline against actual exchange close for a standard-time date, daylight-time date, and early-close date; compare the next-session GTC_EXT 7:00–20:00 ET display to the Schwab ticket. Unit tests do not replace this runtime comparison.

### P2/P3 — design, integration and release work

- [ ] Broker reconciliation: define and implement only against actual broker order/fill records; include partial fills, fees and cancel/correction lineage. OHLCV touches are not fills.
- [ ] Schwab read-only: wait for developer approval, app registration, callback and secure OAuth setup. No order submission.
- [ ] Deployment/recovery: define package/startup, data backup/restore, log rotation, health monitoring, sleep/wake/network/provider recovery, and unattended collector behavior; test on the operator's Windows machine.
- [ ] Release governance: exact branch-head GitHub Actions, local Release build/tests, runtime/browser acceptance, TOS compilation and source/entitlement evidence are separate required gates.


## November 10 — P2/P3 engineering execution plan

The detailed, evidence-gated implementation contract is now in [P2/P3 Engineering Execution Plan](Engineering/P2-P3-Execution-Plan-20261110.md). It supersedes the summary-only P2/P3 bullets above where detail or status differs.

### Broker reconciliation
- [ ] Extend the existing Schwab CSV importer into an append-only order/execution ledger; retain the current importer as a legacy input path until migration is tested.
- [ ] Add stable order/execution identities, import batch hashes, idempotency, partial-fill aggregation, fee provenance, and append-only correction/bust/cancel lineage.
- [ ] Preserve unmatched orders/executions and explicit reconciliation exceptions; derived FIFO lots must link to individual executions.
- [ ] Keep OHLCV target touches and Strategy Report events outside the broker-fill ledger.
- [ ] Obtain a redacted actual Schwab export with headers and representative partial-fill/fee/correction rows before fixing the provider schema. Data not found in sources: verified fee/execution/correction columns in a real export.

### Schwab read-only API
- [ ] Blocked pending developer approval, app registration, registered callback, and confirmed read-only scopes.
- [ ] After approval, implement server-side OAuth/token lifecycle and GET-only positions/orders adapter; no trading-write scopes or order endpoints.
- [ ] Verify token secrecy, expiry/revocation/rate-limit behavior, stale-data handling, and parity against the Schwab UI/export.

### Deployment and recovery
- [ ] Audit existing JSONL stores, hosted workers, startup and configuration before choosing a packaging/service design.
- [ ] Implement data-root separation, manifest/hash-backed backup, staged restore with integrity checks, structured redacted rolling logs, readiness/health and collector watermark monitoring.
- [ ] Test crash/restart, Windows sleep/wake, network/provider failures, disk-full/write failures, duplicate stream replay, backup during writes, and restore into a separate location.
- [ ] Freeze interactive-only vs unattended Windows operation with the operator before selecting installer/service technology.

### Release governance
- [ ] Treat exact-head CI, operator local Release build/tests, API process SHA/health, browser acceptance, TOS compilation, independent data evidence, and backup/restore as separate gates.
- [ ] Add an immutable per-release evidence bundle with exact SHA, workflow URL/status, local test totals, runtime SHA, browser diagnostics, TOS compile record, data provenance, backup/restore status and blockers.
- [ ] Supplied SHA `8304f274724a2f1c0d2e9cb2eefbfc222e1ef2b8`: operator reports Release build + 267/267 tests PASS. GitHub combined-status query returned no records; exact-head CI remains UNKNOWN / NOT VERIFIED by that query.

### Current branch checkpoint
- [x] Confirmed target branch is `feature/opportunity-quality-classification`; no master writes were made.
- [x] Added the detailed P2/P3 plan on the feature branch (commit `551b2748b2913beb34982563c424709e92be68e3`).
- [ ] Run/verify exact-head CI for the documentation update and record the resulting branch SHA.
- [ ] Continue with broker reconciliation only after the actual redacted broker export is available; do not guess CSV columns or fee semantics.


### Deployment decision — 2026-11-11
- [x] Operator selected **interactive Windows operation**: MCC runs only while logged in.
- [ ] Implement a user-session launcher with start/status/graceful-stop, browser launch, single-instance protection and visible startup failures.
- [ ] Do not build a Windows Service or unattended-after-sign-out operation.
- [ ] Enumerate all durable LocalApplicationData files and path overrides before defining backup scope.
- [ ] Verify sign-out stops the API; next login/start resumes collector watermarks without duplicate observations.


### Deployment source audit — 2026-11-11
- [x] Confirmed API hosted workers run inside the API process; interactive lifecycle can therefore stop collection by stopping the API process.
- [x] Identified current default durable stores: market bars, collector watermarks, scanner discoveries, TOS ATR observations, catalyst human reviews, Boring Overnight snapshots, Morning Brief snapshots, and the exclusion watchlist under API `App_Data`.
- [ ] Finish path inventory (exact Boring Overnight/Morning Brief filenames, remaining stores, logs, imported source files and environment overrides).
- [ ] Implement single-instance interactive launcher and reliable graceful shutdown mechanism before adding stop command.
- [ ] Build backup/restore only after path inventory is complete.


## November 12 — operator performance evidence, CRBG sell-limit gates, and Boring Overnight workflow

### Operator verification received
- [x] Operator reports Release build succeeded in 12.2s and all 267 tests passed (0 failed, 0 skipped; test duration 9.2s). This is operator-reported local evidence; the exact source SHA for this run was not included in the report.
- [x] Fresh-process JSONL diagnostics captured during EVR/RKLX/ASTX/SPY live assessments:
  - JSONL file: 130,772,422 bytes; 261,010 lines read; 243,384 unique keys at initial rebuild.
  - Cold index rebuild: 23,313.4ms total; deserialize 4,802.5ms; index insertion 1,106ms.
  - Index load count remained 1 across all four assessments.
  - Per-assessment total: EVR 391.6ms; RKLX 252.1ms; ASTX 240.4ms; SPY 586.2ms.
  - Provider wall time: 364.7ms, 237.2ms, 223.9ms and 562.8ms respectively. SPY 1M used WEBSOCKET_STORE while 5M used ALPACA_REST.
- [x] Interpretation: in this sample, the ~23.3s JSONL cold index rebuild is a one-time process-local cost, not repeated per-symbol assessment work. Normal assessment requests were 240–586ms. Do not describe the old ~51.8s operator latency as solved for all workloads; these four samples only establish the timings observed here.
- [ ] Continue measuring startup/cold-load separately from warm reads and append/read-back persistence. Do not replace JSONL, compact, or introduce a cache/snapshot without a measured design and migration/recovery tests.

### CRBG Calculated Limit Sell — current evidence
- [x] Operator supplied the November 12, 2026 16:01 UTC CRBG response. Overall status BLOCKED; numeric sell limit correctly withheld; no order submitted.
- [x] Exact prior-session reference close passed: $36.10 from the exact 1-minute bar at 2026-11-11 20:59 UTC.
- [x] All qualification blockers remain explicit:
  - SIP_REQUIRED: configured Alpaca IEX is partial-feed coverage, not consolidated SIP.
  - HISTORY_40_OF_40: 0/40 complete eligible GTC_EXT windows; all 40 training/test sessions incomplete.
  - ATR_STUDY_SETTINGS: required TOS ATR(14) Wilder, Phoenix timezone, extended-hours ON, corporate-action adjustment ON, MCC_OHLCV_ATR_V2.1 not evidenced.
  - ATR_PARITY: NO_DATA; zero persisted TOS ATR observations and zero compared bars.
  - CONTINUOUS_5M_ATR_WARMUP: 0/99 consecutive completed TOS 5M observations.
  - SAME_SOURCE_5M_OHLCV: 0/119 exact provider/TOS matches.
  - CURRENT_5M_ATR_FRESHNESS: no current TOS ATR observation.
  - WALK_FORWARD_VALIDATION: 0/20 complete training sessions and 0/20 complete test sessions.
  - SELL_LIMIT_CALCULATION remains blocked.
- [x] Keep exact reference close PASS separate from the failed qualification gates. Do not impute missing bars, use IEX for consolidated-market qualification, publish a numeric target, or infer executable quote/touch/fill from OHLCV.

### Boring Overnight workflow — operator-facing operating sequence
The controls visible to the operator are Symbol, Bar interval, From, To, Fetch & analyze bars, Refresh saved evaluations, and Fetch / retry 20-session close/open history.

1. Select one symbol and the intended bar interval.
2. Set From/To to the period needed for the bar-based analysis. Do not assume this range automatically controls the separate 20-session close/open history request.
3. Use Fetch & analyze bars to retrieve/analyze the selected symbol's bars. Inspect source/feed, returned interval, timestamps and completeness before relying on the result.
4. Use Refresh saved evaluations after relevant bar/input changes to refresh evaluations against the latest persisted observations. Do not treat refresh as a data-fetch substitute.
5. Use Fetch / retry 20-session close/open history when that separate close/open history is missing or incomplete. Inspect returned session count and missing dates. Retry only after a transient failure or when the source may now return missing observations; repeated retries do not fix unavailable feed coverage.
6. Review the saved evaluation and its source-qualified gates. Boring Winner PASS is research-only; it is not Boring Overnight READY, execution permission, or a sell instruction.

**Implementation-contract verification still needed:** the exact HTTP endpoints, whether each button persists results, whether Fetch & analyze bars automatically invokes Refresh saved evaluations, and exact date-range defaults were not verified from source in this documentation pass. Do not assume those side effects; capture browser Network requests and UI result timestamps during operator acceptance before changing button semantics.

### Remaining Boring Overnight acceptance
- [ ] Verify a single-symbol run end-to-end and record the Network request sequence for all three actions.
- [ ] Confirm exact symbol, interval, From/To, provider/feed, returned bar count, missing intervals, persisted evaluation timestamp and 20-session history count.
- [ ] Verify whether Refresh saved evaluations reads only persisted bars or also fetches provider data; document the observed contract.
- [ ] Verify whether 20-session close/open history is a distinct data set from the selected bar interval/range; document its date window and session semantics.
- [ ] Run import reconciliation, shortlist/top-20, four distinct Top 4 Daily/catalyst requests, candidate evidence review, and browser disclosure acceptance.
- [ ] Do not promote Boring Winner PASS to READY. Production source-qualified READY inputs and READY alert transition remain unverified.

### Next engineering priorities
1. Add full-pool Opportunity Quality API/UI fixtures for complete, incomplete, stale and excluded candidate snapshots.
2. Add a current scanner-field availability report with per-symbol source timestamps; historical missing-field counts are stale.
3. Complete Boring Overnight UI/API workflow contract tracing and acceptance instrumentation without changing calculation gates.
4. Finish interactive Windows launcher/recovery and durable path inventory; no Windows Service or unattended-after-sign-out mode.
5. Keep TOS compile/parity, SIP, independent feed, and operator browser/runtime evidence as explicit external gates.
6. Keep Schwab and broker reconciliation parked at the bottom priority.

[Uncertainties]
- Operator's 267/267 test and 12.2s build report did not include the exact source SHA in this message.
- The four performance samples establish observed request timings, not a universal latency guarantee.
- Button-to-endpoint/persistence side effects and default date-range behavior have not yet been verified from the implementation source or a Network capture.
- No consolidated SIP history or persisted Thinkorswim ATR evidence was supplied for CRBG.


### Boring Overnight control contract — source-verified follow-up (2026-11-12)

The active Dashboard source (frontend/index.html) and API route in src/MomentumCommandCenterV2.API/Program.cs were inspected after the initial operating note. This resolves part of the prior uncertainty:

- [x] Bar interval offers exactly 1 minute and 5 minutes. From / To are labelled Arizona-local inputs and are required for the Fetch & analyze bars form.
- [x] Fetch / retry 20-session close/open history is separate from the selected bar interval/date range. The UI explicitly says it fetches 1-minute provider bars, persists them, and calculates the 20 most recent regular close/open pairs.
- [x] API contract: POST /api/boring-overnight/capture-history; request must specify exactly 20 pairs and a configured provider (Alpaca or Massive). The server derives the 20 session boundaries from the U.S. equities calendar, requests 1-minute bars in chunks, de-duplicates by timestamp, persists returned bars, calculates close/open history, independently requests Daily metrics and catalyst assessment, then appends a timestamped candidate snapshot and evaluates it.
- [x] The capture also checks the exclusion watchlist and stores history audit, Daily evidence and catalyst evidence in the snapshot. Caller/source provenance is recorded; it is not independent verification of provider entitlement or completeness.
- [ ] Still trace the exact frontend request/response handlers for Fetch & analyze bars and Refresh saved evaluations before documenting their endpoint side effects.
- [ ] During browser acceptance, record configured provider/feed, returned bar count, persisted count, valid/missing pair counts, Daily/catalyst errors and snapshot as-of timestamp.



## November 12 — Overnight operator response audit and integration fix

- [x] Reviewed data/ChatResponse.md, including the CRBG 1m/5m responses, 20-session capture, and four-symbol Daily/catalyst results.
- [x] Identified capture snapshot bug: fresh Daily DataFresh was left null by the mapping expression.
- [x] Fixed DataFresh mapping to preserve the provider's explicit freshness value; null remains null when Daily retrieval fails.
- [x] Fixed catalyst status translation: MATERIAL_EVENT_REVIEW and UNCLASSIFIED_NEWS_REVIEW now map to RISK; RISK_PRESENT maps to RISK; only explicit CLEAR_WITH_SOURCE with nonempty articles and complete coverage can map to CLEAR; zero-news and incomplete/unknown evidence remain UNKNOWN.
- [x] Added regression tests for catalyst mapping.
- [ ] Run Release build and full tests on the latest branch head; do not claim the integration fix verified until CI/local tests pass.
- [ ] Re-run CRBG 20-session capture with source coverage sufficient to return all 20 exact prior-close/next-open boundaries. Current response: 13/20 valid, 7 missing.
- [ ] Replace IEX_PARTIAL qualification input with verified consolidated SIP or independent equivalent; Daily 20/20 freshness alone is not sufficient.
- [ ] Complete human catalyst review for CRBG MATERIAL_EVENT_REVIEW; review INGM's two UNCLASSIFIED articles. Zero-article FG/CSQR remains UNKNOWN.
- [ ] Wire timestamped live 5M permission/structure, 1M execution state, extension ATR, current volume-vs-average, daily range and ATR into snapshot evaluation; capture-history currently does not supply these.
- [ ] Obtain current Overnight interval observation separately; do not reuse the last completed historical pair as current.
- [ ] Decide whether official opening-auction data is required; the current 09:30 one-minute bar Open is only a proxy.

Detailed evidence and exact code changes: docs/Validation/Boring-Overnight-Integration-Audit-20261112.md.


### November 12 — saved Overnight evaluation refresh completed
- [x] Corrected the saved-evaluation refresh contract: GET /api/boring-overnight/snapshots now returns immutable input snapshots plus a freshly recalculated evaluation for each snapshot under the current evaluator rules.
- [x] Dashboard refresh now displays each saved snapshot's current evaluation status and eligibility flag; its status text reports how many evaluations were recalculated.
- [x] This remains a read/recalculate operation; it does not rewrite append-only snapshots, promote research-only Boring Winner results, or submit orders.
- [ ] Verify in browser that Refresh saved evaluations returns one evaluation per snapshot and that the visible status matches the returned API result.


## November 12 — CRBG Overnight retest and current-interval correction

Operator confirmed that the Step 2 table is the saved-evaluation row: persisted input snapshot plus a fresh evaluator result, not a trade/order record.

- [x] Recorded the latest CRBG 20-pair capture in `docs/Validation/Boring-Overnight-CRBG-Retest-20261112.md`.
- [x] Confirmed 6,676 IEX 1-minute bars fetched; 0 newly persisted because the observations were already present; 13/20 valid exact close/open pairs; 7 missing exact opens.
- [x] Confirmed the newly captured snapshot persists `dataFresh=true` and `catalystStatus=RISK` for a complete-coverage MATERIAL_EVENT_REVIEW article. Evaluator remains DATA? / eligible=false.
- [x] Confirmed Daily 20/20 expected sessions is still IEX_PARTIAL and qualificationUsable=false; observed close/SMA/volume are diagnostics only, not qualification values.
- [ ] **P1 correction:** separate the last 20 completed historical close/open pairs from the current Overnight interval. The current response counts the 2026-11-12 unfinished opening endpoint among 20 requested pair slots. It must be PENDING/current interval, not a completed historical missing return. Add API/UI distinction and regression tests; do not calculate rolling historical metrics using the current interval.
- [ ] Preserve exact 09:30 ET boundary rule. Six earlier historical opening bars are missing; do not substitute nearby 09:31–09:34 bars. Current-day 2026-11-12 opening bar is also absent in this IEX response.
- [ ] Wire source-qualified 5M permission/structure, 1M state, extension ATR, current volume-vs-average, daily range and ATR with per-field timestamps/freshness. Unknown remains fail-closed.
- [ ] Human-review the CRBG Wells Fargo analyst-action article and authoritative filings/news before any possible clearance. MATERIAL_EVENT_REVIEW remains RISK; zero articles remains UNKNOWN.
- [ ] Obtain independently validated consolidated Daily coverage and adjustment/session parity before populating qualification fields. IEX_PARTIAL remains diagnostic only.
- [ ] Keep the 09:30 one-minute Open explicitly labelled as a proxy; official opening-auction print remains unverified.

**Latest CRBG result:** 13/20 valid, incomplete history; current interval pending; Daily IEX partial; catalyst risk; live technical inputs missing; DATA? / not eligible; no order submitted. Detailed evidence and interpretation are in the validation note linked above.



## November 12 — Overnight capture runtime fix and top-10 research

- [x] Fixed the reported `Capture failed: calc is not defined` frontend exception. The 20-session capture handler referenced `calc.boringWinnerPass`, `calc.boringWinnerInRangeCount` and `calc.boringWinnerSampleStdDevPct` without declaring `calc`. It now initializes a safe response-derived fallback (`data.calculation || history.calculation || {}`) before rendering. Missing optional calculation fields remain DATA?/insufficient history; this does not fabricate a Boring Winner result.
- [x] Created `docs/Research/Overnight-Scanner-Top10-Debrief-20261112.md` from the uploaded 50-symbol scanner export. Includes the ranked top 10, deep dives on the top four by score, all ten catalyst notes, and comparison with MCC's CRBG/FB/CSQR/INGM Top 4.
- [x] Recorded key event risk: ADXN's +59.17% current Overnight move is an extreme outlier despite its #1 score; it is separated as an event-risk watch, not blindly promoted as a preferred trade.
- [x] Research surfaced recent company releases for CECO, SENS, AMD, CSQR, NET, STEP, HROW, DPC, INGM and CRBG. HSAI fresh earnings catalyst and FB instrument identity remain unverified.
- [ ] Run branch CI for frontend fix and confirm no regressions. Browser acceptance of capture retry remains open until operator pulls the fix and retries.
- [ ] Continue the history API semantic correction: 20 completed historical pairs must be separate from the current Overnight interval; add tests and UI labels.
- [ ] Continue source-qualified 5M/1M technical wiring and missing ATR/volume/range inputs. Keep SIP/independent coverage requirement fail-closed.
- [ ] Reconcile MCC Top 4 against scanner score ranking and record candidate-specific source evidence. Do not replace MCC selection based solely on this research note.

Research note: `docs/Research/Overnight-Scanner-Top10-Debrief-20261112.md`.
Frontend fix commit: `2876d1093d930fc1830b2953d101df9fc524d9e2`.
Research report commit: `584e7579547ef60fb2bde8bb09e9f9f867ecedfd`.



## November 12 — chart review, FG correction and capture runtime retest

- [x] Verified CI for capture handler fix: workflow run [43289911750](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/43289911750) completed SUCCESS on branch head `d047af1c86826e94b587468924e2cec3015cd3be`; frontend JavaScript syntax validation, .NET build and test job all passed.
- [x] Rechecked `frontend/app.js` on the feature branch: capture handler declares `const calc=data.calculation||h.calculation||{}` before the Boring Winner property reads.
- [ ] **Browser acceptance remains OPEN:** operator still reports `Capture failed: calc is not defined`. Confirm local branch/SHA and that the dashboard is serving the new frontend asset after a full API/dashboard restart and hard refresh. Do not mark fixed for operator until a fresh capture completes without this exception.
- [x] Calculated provisional CRBG valid-pair-only statistics from the 13 supplied returns: 12/13 positive (92.31%), mean +0.750%, median +1.031%, latest five valid observations mean +1.131%. Official 20-pair metrics remain DATA?; these provisional stats are not qualification inputs.
- [x] Recorded 7 missing exact open endpoints (six historical dates plus Nov 12 current session). Current Nov 12 open is an in-progress interval and must not occupy one of the 20 completed historical pair slots.
- [ ] **P1:** fix history semantics and UI/API contract: last 20 completed historical close/open pairs separate from current interval. Add missing historical open, missing current endpoint, DST, no nearest-bar substitution, and rolling calculations over scheduled completed intervals only.
- [x] Reviewed uploaded Thinkorswim charts: CSQR has strongest current technical posture (5M permission YES, runner/bullish regime, 1M EXECUTION READY, but 5M PREPARE TO BUY and swing-buy environment NO); AMD and CECO are bullish but extended; SENS is bearish with 5M permission NO and 1M execution environment NO.
- [x] Corrected MCC focused ticker typo: FG = F&G Annuities & Life (NYSE: FG), scanner rank #19 / score 71.42. Q3 adjusted earnings and sales declined despite higher AUM and record core retail sales; not an unqualified positive catalyst.
- [ ] Continue source-qualified 5M/1M technical wiring with per-field timestamps/freshness; chart screenshots are not a substitute for API-backed source qualification.
- [ ] Maintain SIP/independent consolidated coverage fail-closed; IEX_PARTIAL cannot qualify.
- [ ] No candidate declared READY; no order submission.

Updated research report: `docs/Research/Overnight-Scanner-Top10-Debrief-20261112.md`.
Research addendum commit: `f890a21bac4c55b3e49195912f29ec901dfc7c34`.
CI run: `43289911750` SUCCESS. Branch head at CI: `d047af1c86826e94b587468924e2cec3015cd3be`.

## November 12 — completed-history/current-interval separation implemented

### Repository changes committed on `feature/opportunity-quality-classification`

- [x] Added `UsEquitiesSessionCalendar.BuildCurrentHistoryWindow`: returns the latest 20 scheduled historical close/open boundaries ending at the prior completed session, plus a distinct current Overnight boundary.
- [x] Current interval rolls forward to the next trading session at/after the current session's regular close; it respects the calendar's early-close time and skips weekends/holidays.
- [x] Updated `POST /api/boring-overnight/capture-history` to calculate historical metrics from historical boundaries only. The current interval no longer consumes one of the 20 history slots.
- [x] Added separate current interval response with `PENDING`, `MISSING` or `OBSERVED`, exact boundary timestamps, available endpoint prices/return, and explicit `ONE_MINUTE_BAR_OPEN_PROXY` / `officialOpeningAuctionVerified=false` provenance.
- [x] Capture fetch range is bounded at the current time; it does not request future bars. Before the exact 09:30 bar is complete (09:31 ET), the current interval remains PENDING.
- [x] Boundary audit now includes 40 historical close/open timestamps plus the two current-interval timestamps.
- [x] Added Dashboard display for the current interval separately from the historical metrics and clarified the 42-row audit.
- [x] Added calendar regression tests for pre-close separation, post-close rollover, spring DST UTC boundaries, and weekend/Thanksgiving rollover.

Commits in this implementation sequence:
- `171ab05e4f617dafa78409e424c9b4fa4027e345` calendar window split
- `586f7b4a81881af96b6c0f0a4526a1c2759cf1ea` API integration
- `efb682386b106883111478f637cfd28f5858aab8` calendar regression tests
- `4bccd0df3884a54bfea71b1c79706175c8aad124` Dashboard current interval field
- `f13c9f539adcce3145c91472f36499fb63f5759a` Dashboard response rendering
- `895b734315205d4a1d54606a538a70154e497078` bound provider request to current time
- `f9f89b28722b4455764f5a41eae5714b888e2fe4` UI audit/description correction

**Verification status:** CI/local .NET build and tests have not been run in this session. Do not mark this change validated until exact-head CI or local Release build/tests pass. Browser acceptance and provider retest remain open.

### Next engineering work

1. Verify exact branch-head CI/build/tests and fix all failures.
2. Add unit tests for current interval state transitions (PENDING before 09:31 ET; MISSING after endpoint if exact open absent; OBSERVED only with exact close and open). Preserve no-nearest-bar substitution.
3. Verify UI renders current interval and historical history as separate sections; saved snapshot history audit must describe only the 20 historical pairs.
4. Continue source-qualified 5M permission/structure and 1M state plus extension ATR, volume ratio, Daily range and ATR with per-field timestamps/freshness. Do not infer from screenshots or partial IEX.
5. Keep IEX_PARTIAL non-qualifying, zero news UNKNOWN, and all execution/limit-sell gates fail-closed.

Follow-up test coverage added: `f259051f4d7ba2cc086476621eb8a255b683b99b` verifies rollover at the scheduled Thanksgiving Friday early close (13:00 ET), in addition to the weekend/DST cases. Latest implementation branch head before this documentation update: `f259051f4d7ba2cc086476621eb8a255b683b99b`.

## November 12 — Overnight calendar test correction and technical-input wiring

### Implemented on feature branch

- [x] Fixed the five reported CurrentWindow test failures. Root cause: BuildCurrentHistoryWindow called BuildRecentBoundaries(..., 1), but that historical builder intentionally enforces 5–40 pairs. The current single interval is now constructed directly from the immediately preceding and target trading sessions. The historical builder's validation remains unchanged.
- [x] Capture-history now requests the same live 1m/5m technical assessment used by Main Chart, through LiveSymbolAssessmentService, and maps source-returned values into the Boring Overnight evaluator inputs:
  - 5M permission from the independent five-minute score/permission output.
  - 5M structure from the V2 decision's five-minute structure result.
  - 1M state/action from the V2 decision.
  - 1M extension ATR from the completed 1m technical calculation.
  - 5M same-time-of-day relative volume from its provider-bar baseline.
- [x] Added per-field source timestamps from the exact raw 1m/5m bar-start timestamps. The response now exposes source/feed, source transport, evaluation status, per-timeframe freshness and the mapped technical inputs.
- [x] Added completed-session Daily range percentage and 14-session simple ATR percentage. Both are calculated only from a complete, fresh, qualification-usable SIP Daily sample. IEX is excluded from these inputs.
- [x] Combined Daily and intraday freshness conservatively. If any required freshness evidence is absent, evaluator DataFresh remains null/unknown rather than being converted to false or true.
- [x] Technical assessment failures are surfaced in the capture response and leave technical inputs missing; they are not backfilled from screenshots or user-entered values.

Commits:
- a54a0a5c0abe53744b6797919326345651fc7d10 direct construction of current interval boundary.
- 81cae4eb5ebcdf7ed95d9a069eb7bf27fb9418a6 initial technical-input mapping and daily range/ATR calculation.
- e0b799e5f87b068f266c93f27e3d7509db3e6e0a expose technical provenance and preserve unknown freshness.
- 26380617f2a7d380a99b29d556b82813dffc5ba5 correct freshness-diagnostics JSON path and expose feed/transport/status.
- 42ae6b6a471f8bd46f86c606cb9482bf461341fa record raw source bar-start timestamps.

### Validation still required — not complete

- [ ] Re-run Release build and all tests after pulling latest branch. The five reported failures are addressed in code, but no local test execution result is available to me.
- [ ] Add API-level regression tests for capture-history technical mapping (5M permission/structure, 1M state, extension ATR, same-slot relative volume, per-field timestamps, stale/missing data).
- [ ] Verify exact feed consistency between history capture and live assessment; do not allow mixed provider/feed data to become one qualified snapshot.
- [ ] Confirm live assessment returns a fresh 1m/5m result during the actual final-hour Overnight capture window. Outside regular session, missing permission/structure/state must remain DATA?.
- [ ] Confirm Daily range basis is latest completed session H-L divided by its close and ATR% is 14 completed-session simple mean true range divided by latest completed close. These are explicit proxies until matched against the exact Thinkorswim Daily study.
- [ ] Browser acceptance: inspect technical-input provenance, source timestamps, stale states and missing states.
- [ ] Preserve SIP-only Daily qualification; no IEX inference, no screenshot-derived values, no official-auction claims and no order submission.

### Current technical-input semantics

- currentVolumeVsAverage is the live service's same-time-of-day 5M relative-volume ratio, requiring its baseline observations. It is not a raw exchange-wide volume field.
- dailyRangePct is based on the latest completed SIP Daily bar, not an in-progress current-day range.
- atrPct is a 14-session simple mean true range, not a Wilder ATR. Thinkorswim parity remains unverified.


## November 12 — operator verification and strict feed provenance

- [x] Operator ran `.\\scripts\\Get-Latest-And-Test.ps1` on exact source SHA `5268846a1a95ec799a352b74032469baf33569a8`: Release build succeeded; **280 total / 280 passed / 0 failed / 0 skipped**; 7.6s tests, 8.0s build.
- [x] Added a capture-history guard requiring technical assessment provider, 1m feed and 5m feed to match the provider/feed used for the history capture. Missing or mismatched provenance withholds all mapped technical inputs from the evaluator snapshot and returns `TECHNICAL_FEED_MISMATCH`; observed feed labels remain visible for diagnosis.
- [ ] Add isolated regression tests for the feed-match guard and the complete technical-input mapping. Current feed guard is source-implemented but not yet test-verified.
- [ ] Verify exact API response and browser rendering on a fresh capture, including matched-feed flag, raw 1m/5m bar-start timestamps, freshness and explicit mismatch diagnostics.
- [ ] Validate the live assessment behavior at the actual Overnight capture time. The V2 permission/execution decision is regular-session-only; do not carry forward a stale regular-session permission into after-hours or late-night capture.
- [ ] Reconcile Daily range/ATR against the exact Thinkorswim study. The current ATR remains a 14-session simple mean true-range proxy, not Wilder ATR parity.
- [ ] Keep 20 completed historical pairs and current interval separate; preserve exact-boundary and no-nearest-bar rules.
- [ ] Keep IEX_PARTIAL non-qualifying, catalyst zero-news UNKNOWN, official opening-auction status unverified, and order submission disabled.

Feed-provenance guard commit: `195be63c690920b86d4d624e1b078017bcf4e3a4`.
Operator-verified parent SHA: `5268846a1a95ec799a352b74032469baf33569a8`.


## November 12 — technical staleness audit and evaluator-input correction

- [x] Audited operator's capture response: Alpaca IEX provenance matched across history/1m/5m (`feedMatchesCapture=true`), but live assessment status was `STALE_MARKET_DATA`; both 1m and 5m freshness flags were false.
- [x] Confirmed null 5m permission, 5m structure and 1m state are correct for stale bars.
- [x] Identified and fixed a qualification leak: stale 1m/5m-derived extension ATR and same-time-of-day RVOL were still being copied into the evaluator input. They are now withheld unless both bars are fresh and provider/feed provenance matches.
- [x] Separated eligibility rules: numeric intraday indicators require matched fresh bars; V2 permission/structure/state additionally require `evaluationStatus=EVALUATED`. Outside-session, stale, insufficient-history and RVOL-baseline-insufficient statuses cannot provide V2 decision fields.
- [x] Capture response now reports `intradayBarsUsable`, `v2DecisionUsable`, 1m/5m age seconds and freshness limits, alongside source timestamps and booleans.
- [x] Added policy regression tests for feed mismatch, stale/unknown timeframe freshness, outside-session, stale, insufficient-history and RVOL-baseline states.
- [x] Created validation note: `docs/Validation/Boring-Overnight-Technical-Staleness-Audit-20261112.md`.
- [ ] Run Release build and full test suite on the latest branch head; no result is available for these new commits yet.
- [ ] Regular-session browser retest: fresh matched bars should set `intradayBarsUsable=true`; V2 decision fields only if status is `EVALUATED`.
- [ ] After-hours browser retest: V2 decision fields must remain null; stale numeric indicators must also be null.
- [ ] Inspect the persisted snapshot/evaluation request, not just the response display, to confirm stale numeric values are actually absent.
- [ ] Obtain separate qualification-usable SIP Daily data for Daily range/ATR; IEX remains diagnostic only.
- [ ] Reconcile ATR formula against Thinkorswim and retain official-auction/READY gates.

Implementation commits: `a5a089fb13bd63727c619ae335b9473ff4a25cfc`, `6e8248e664f6d40c51b5b1501cab0f6d86b44361`, `e4a6918a0b50a5421858316fb9e82f2b9e3bb235`, `a986e8107908bc626656732b7b03320a262b1170`, `58ade3a9651304d75440df766bc0dba1495e0616`, `e39cd406092795ac553a56bccce36c5a961ccc05`, `853fe72c6af3baa9187967d87b876eab78d2b264`.
Validation note commit: `308853dcb077f0a473e5ef02d19b01a2912373bf`.
