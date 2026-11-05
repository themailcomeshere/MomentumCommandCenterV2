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
