## 2026-11-05 Arizona — latest import evidence and Dashboard entry point

- The sole Dashboard entry point is now `frontend/index.html`; `frontend/prototypeIndex.html` was removed after its approved layout was migrated. UI implementation and browser acceptance must use the local API root URL `/`.
- The main page now includes the fixed symbol/workspace dock, vertical workspace toggle, responsive readiness header, integrated classification card, completed 1M close timestamp, Daily VWAP, ATR14, RVOL and ATR-extension metrics. The toggle is bridged to the existing `#mode` state.
- The migration explicitly retained all five ATR import provenance controls and the calculated sell-limit evidence controls from the newer main page. The active app.js cache key remains `20261105-atr-import-control-recovery-v3`.
- Operator import result: EVR 1m Nov 3 V2.1 = 346 appended, 48 timestamp gaps, 20 matched timestamps, mean absolute error 0.000029, max 0.000061, `NOT_MATCH`. The max exceeds the 0.00005 tolerance.
- Operator import result: EVR 5m Nov 3 V2.1 = 81 appended, one 5-minute gap, `INSUFFICIENT_WARMUP` (99 observations required before 20 comparisons).
- EVR 5m Nov 4 V2.2 = 82 identical skipped, 0 appended, 0 corrections. Idempotent re-import confirmed; no additional history was added.
- Missing bars are not imputed. Numeric sell-limit output remains withheld pending continuous ATR warm-up/parity and all independent gates.

[Uncertainties]
The local browser has not yet accepted the migrated page. The cause of the 1m parity error and the timestamp gaps is not diagnosed by the import summary; no tolerance relaxation or gap interpolation is justified.

---

## November 5 follow-up — import-control recovery and updated export evidence

- Current checked-in HTML contains the five ATR provenance controls; operator's running DOM reports all five missing. Root cause of the runtime mismatch is not yet identified.
- Frontend now creates missing controls at runtime with confirmed settings (14, Wilder, America/Phoenix, ETH ON, corporate-action adjustment ON) before the import handler runs. Cache key is bumped. Requires local restart/browser acceptance.
- V2.2 5m export contains 82 source observations, 09:05–16:00 ET, one 15-minute gap, and no payload later than 16:00 ET. This is insufficient for 119-bar parity validation and does not prove consistent after-hours data.
- Re-import the Nov 3 reports after local pull/restart; then export a longer 5m report and inspect embedded source timestamps. Do not infer source-bar coverage from StrategyReport Work Time or order-event timestamps.


---

## 2026-11-05 current implementation status — supersedes the earlier planning status below

**Status:** ATR V2 ingestion/parity and sell-limit orchestration API/UI exist. Chronological walk-forward validation is now implemented in code, but CI and operator runtime acceptance are pending. The numeric limit remains fail-closed behind all independent evidence gates.

### New import and chart-window findings
- Dashboard import failed before upload because the frontend dereferenced missing ATR metadata controls. The handler now checks all five DOM controls and emits the missing IDs; HTML cache key is bumped. Operator must hard-refresh and retry.
- Nov 4 EVR 5m V2.1 report has 82 payload observations from 09:05 to 16:00 ET, with a 15-minute gap at 09:15–09:30 ET.
- The study's former `endTime=1600` excluded after-hours bars even when the chart's extended-hours display was enabled. The study defaults now use `startTime=0700`, `endTime=2000`; ThinkScript time-window functions use the EST/Eastern clock. Reload/compile and re-export to verify.

### Current sell-limit validation contract
- The capture/audit history requirement is now 40 complete sell sessions: 20 chronological training + 20 later unseen test sessions.
- The training set alone determines the nearest-rank target quantile.
- The unseen test set reports target-touch rate, mean adverse excursion and maximum adverse excursion.
- Pass requires 100% coverage, test touch rate at least the training empirical touch rate, and test maximum adverse excursion no greater than the training adverse-excursion P95 bound.
- The sell-limit calculator receives only the first 20 training sessions. The test set is never used to tune the candidate.
- A failed/insufficient walk-forward gate blocks the numeric limit.
- Actual broker fills are separate from theoretical OHLCV touches. No broker-fill integration was added.

Design: `docs/Validation/Walk-Forward-Validation-Design-20261105.md`.

### Operator verification
1. Pull feature branch and run `.scripts\Get-Latest-And-Test.ps1`.
2. Hard-refresh Dashboard and retry both V2.1 imports.
3. Compile/reload the revised ThinkScript and export 5m history through 20:00 ET.
4. Verify after-hours observations, gap list, import counts and parity.
5. Keep SIP capture disabled until entitlement/effective feed changes; prior SIP request returned HTTP 403.


---

# Calculated Limit Sell — implementation plan and current evidence

**Working branch:** feature/opportunity-quality-classification  
**Status:** calculator and GTC_EXT capture/audit primitives exist; end-to-end API/UI is not implemented. A numeric result must remain DATA? / NOT CALCULATED until every source gate passes.

## Current operator evidence (2026-11-05)

- The visible button **Fetch / retry 20-session close/open history** is the existing 20-pair capture action. The earlier phrase “Capture 20-session Overnight history” was a descriptive name, not the visible label.
- EVR capture: 11/20 valid pairs; 9 missing next-session opens; exact-boundary provider coverage 31/40; persisted coverage 31/40; 3,759 bars fetched; 0 newly stored. Effective feed Alpaca/IEX. Rolling win rate and return metrics are correctly withheld.
- The operator confirmed both appsettings.json and secrets.json specify IEX. The runtime output agrees. GTC_EXT SIP capture is correctly blocked while effective feed is IEX.
- Repeating the same 20-session rolling request does not require changing From/To. Change dates only for a separately scoped diagnostic.
- EVR 1M StrategyReport contains 365 encoded OHLCV records with 21 gaps between adjacent records. It is not a continuous 1M tape.
- EVR 5M StrategyReport contains 79 contiguous 5-minute OHLCV bars. Neither StrategyReport includes Thinkorswim ATR14 output, ATR study inputs, or an independent ATR value. The 5M sample is insufficient for 98 Wilder prefetch bars plus the comparison bar.

## Existing implementation

### GTC_EXT history capture and audit

- POST /api/boring-overnight/capture-sell-window-history fetches configured-provider 1m bars in bounded chunks and persists raw observations.
- GET /api/boring-overnight/sell-window-history audits persisted observations without another fetch.
- Each of 20 sell sessions requires all 780 exact minute bar-start timestamps from 07:00 through 19:59 America/New_York and the exact previous regular-session close bar.
- Source, provider, timeframe and feed are isolated. Missing minutes are not imputed.
- IEX is partial-exchange diagnostic data and is never qualification-usable for consolidated target research; SIP is required.
- Existing BoringOvernightSellLimitCalculator calculates a nearest-rank quantile of per-session maximum eligible-window return, applies that return to the current reference close, and reports an in-sample descriptive touch rate. It correctly treats complete flat/adverse samples as valid but returns NO_POSITIVE_TARGET when the selected quantile is non-positive.
- The calculator withholds a numeric target unless it receives >=20 complete windows, positive ATR, a UTC ATR timestamp and ATR parity status MATCH.

## Required end-to-end calculation contract

Add POST /api/boring-overnight/calculate-sell-limit and a UI control only after the service contract below is implemented.

1. **History source:** read the latest persisted bars from IMarketBarObservationStore. Do not accept client-supplied history observations or calculate from an unpersisted request payload.
2. **Feed:** require Alpaca SIP and matching source/feed metadata throughout the complete 20-window sample. Reject IEX even if timestamps are complete.
3. **Window coverage:** require 20/20 complete sessions, 780/780 exact 1m bars per session, and exact previous-session close. Include every session's missing timestamp count and a bounded sample of missing timestamps in diagnostics.
4. **Reference close:** read the exact required 1m regular-session close bar from the persisted store; return its timestamp, close, provider, feed and timeframe. Never accept a bare user-entered close as source evidence.
5. **ATR source:** calculate Wilder ATR(14) from persisted, continuous 5m bars with the same provider/feed/adjustment/session context as the Thinkorswim comparison. Enforce the required warm-up context (98 Wilder prefetch bars plus the comparison bar). Reject duplicates, gaps, mixed sources, stale bars and incomplete warm-up.
6. **Thinkorswim parity:** require an independently supplied timestamp-matched TOS ATR14 value and study metadata (study identity, period, average type, extended-hours setting, adjustment mode, timeframe and displayed precision). Compare multiple consecutive completed bars. Tolerance must be justified by the exported/displayed precision; never hardcode an arbitrary tolerance. A caller-supplied string MATCH is not sufficient.
7. **Calculation:** only after gates 1–6 pass, invoke BoringOvernightSellLimitCalculator with complete observations, verified reference close, computed ATR and a server-derived MATCH result. Return quantile method/rank, sample dates, target return, limit, ATR distance, and in-sample touch count/rate.
8. **Provenance:** include calculation version, timestamp, symbol, provider/feed, exact source timestamps, adjustment mode, sample session dates, coverage, reference-close provenance, ATR provenance/parity differences, and explicit reason codes.
9. **UI:** show DATA? / NOT VERIFIED with reason codes until all gates pass. Show a numeric limit only after every gate passes. Clearly label the touch rate as in-sample historical descriptive data, not fill probability. This is research output, not an order or an instruction to submit a sell.
10. **Validation:** unit/API tests for incomplete sessions, IEX, missing exact close, stale/mixed ATR bars, warm-up insufficiency, ATR mismatch, duplicate dates, adverse/flat windows, quantile rank, reference-close mismatch and provenance completeness. Browser acceptance required after CI.
11. **Walk-forward:** collect more than 20 complete windows. Use chronological training/test splits (e.g., preceding 20 sessions for training, following sessions as independent tests). Report target touches, time-to-touch and adverse excursion separately. Never tune and report on the same sample as out-of-sample performance.
12. **Actual fills:** integrate broker execution records separately (order ID/client ID, symbol, side, quantity, fill price/time, fees, status). A bar high touching a limit is not evidence of an actual fill.

## Operator next steps

1. Keep IEX configured for now; do not attempt GTC_EXT SIP qualification on this feed.
2. For a retry of the same rolling close/open history request, use **Fetch / retry 20-session close/open history** and leave From/To unchanged.
3. For GTC_EXT, first resolve and verify entitled SIP configuration, restart the API, then confirm GET /api/providers/status reports the intended effective feed before capture.
4. Supply a Thinkorswim export with actual ATR14 values on matched 5M bars plus study settings. The two current StrategyReports are OHLCV/execution reports, not ATR-value exports.
5. Pull updates with .\scripts\Get-Latest-And-Test.ps1 from a clean working tree on feature/opportunity-quality-classification; restart MCC after tests pass.

## [Uncertainties]

- The reason for the nine missing 09:30 open bars is not established by the summary alone. Use per-boundary nearest-provider offsets to distinguish provider absence from a persistence mismatch.
- Current runtime SIP entitlement, 20/20 GTC_EXT SIP coverage, exact current reference close, TOS ATR14 output series, and broker execution records have not been supplied.


## 2026-11-05 — V2 ATR export evidence received

### Export audit
- `data/StrategyReports_EVR_11032026_5M_ATR.csv`: 81 V2 payload records, all with the expected 9 pipe-delimited fields after the V2 prefix (symbol, timeframe, epoch-ms, OHLC, volume, ATR14). Source timestamps span 2026-11-03 14:05Z through 20:50Z. There is one 10-minute timestamp step (one missing 5-minute bar); the remaining steps are 5 minutes. ATR values are present on all 81 rows (0.4962–2.0107).
- `data/StrategyReports_EVR_11042026_5M_ATR.csv`: 79 V2 payload records with the same 9-field shape. Timestamps span 2026-11-04 14:30Z through 21:00Z with contiguous 5-minute steps. ATR values are present on all 79 rows (0.7834–2.5238).
- Combined: 160 timestamped EVR 5m OHLCV+ATR14 records across two dates. These are useful parser/parity fixtures and prove the export carries an ATR field. They are not 20 sessions and do not provide 98 prefetch bars preceding every required comparison point.
- User-recorded chart settings: extended-hours ON; corporate-action adjustment ON; chart timezone Phoenix local time (UTC-07:00); ATR length 14; average type Wilder. The V2 script's default input is atrLength=14 and uses ThinkScript Wilder averaging. User confirmation is consistent with the script default.
- The report rows are syntactically populated. A chart-plot-to-export comparison of at least 20 rows has not been independently documented in the evidence received here. Do not mark ATR parity MATCH solely because the export contains ATR values.

### Orchestration readiness decision
The exports are sufficient to implement and test the V2 report parser, store timestamped TOS ATR observations, and build the parity-comparison path against matching server-calculated 5m ATR. They are **not sufficient to certify or run a numeric Calculated Limit Sell**. The service still needs source-isolated 20/20 complete GTC_EXT windows from SIP (780 exact 1m starts per session plus exact prior regular close), verified current reference close, continuous 5m OHLCV warmup/anchor, chart-to-export spot checks, and server-derived parity MATCH. The current runtime feed is IEX; do not request SIP capture until effective SIP configuration/entitlement is verified.

### Implementation checklist
- [ ] Add a V2 ATR report reader without changing the V1 OHLCV reader contract; validate exact payload schema, symbol, 5m aggregation, epoch timestamps, numeric ATR > 0, duplicate timestamps, and source-row errors.
- [ ] Add tests/fixtures for both supplied exports, including the 11/03 missing 5m interval; do not silently fill it.
- [ ] Persist TOS ATR observations with explicit source metadata and chart settings; do not store these as provider OHLCV bars.
- [ ] Implement server-side ATR series calculation and timestamp-matched parity audit. Require consecutive matched bars and documented precision tolerance; caller cannot submit a MATCH flag.
- [ ] Implement orchestration endpoint using persisted SIP GTC_EXT history and persisted reference close only; return reason-coded DATA?/NOT VERIFIED until every gate passes.
- [ ] Add UI provenance/status and walk-forward evaluation. Keep touch rate labelled in-sample descriptive, not fill probability.


## 2026-11-05 — configurable Thinkorswim chart aggregation
The ATR V2 study now exposes a `chartAggregation` input with 1m, 5m, 15m, 30m, 1h and 4h choices. The selected period is written into the payload and the study blocks export unless the actual chart aggregation matches. ThinkScript cannot change the host chart's aggregation itself: the operator sets the chart period in Thinkorswim's chart aggregation control and selects the same value in the study input. This removes source-code edits when switching supported periods. The revised script still requires compilation and validation in Thinkorswim for each desired aggregation.

Operator-confirmed defaults remain extended-hours ON, corporate-action adjustment ON, Phoenix timezone, ATR14 Wilder. Current provider status is Alpaca IEX configured/validated; Massive is unconfigured. SIP entitlement is not established, so GTC_EXT SIP capture remains blocked.
