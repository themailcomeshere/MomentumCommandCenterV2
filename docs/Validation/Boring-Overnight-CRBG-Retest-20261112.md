# Boring Overnight CRBG operator retest — 2026-11-12

Branch: `feature/opportunity-quality-classification`

## Operator confirmation

The operator confirmed the saved-evaluation table is the intended Step 2 output. The displayed row is a persisted input snapshot plus its freshly recalculated evaluator result; it is not a broker/order record.

Snapshot row:
- Symbol CRBG; snapshot as-of 2026-11-12T16:30:03.7432259Z; recorded 2026-11-12T16:30:03.7453527Z.
- Alpaca/IEX; EVALUATION / DATA? / eligible=false.
- Historical pairs 13/20, 7 missing, INCOMPLETE.
- Daily qualification fields UNKNOWN; feed IEX_PARTIAL; qualificationUsable=false.
- Catalyst/news: one article, coverage complete, MATERIAL_EVENT_REVIEW.
- 5M permission/structure and 1M state UNKNOWN.

## New 20-pair capture response

Request: CRBG, Alpaca IEX, 1-minute, 2026-10-15T19:59Z through 2026-11-12T14:31Z.
- 6,676 bars fetched.
- 0 newly persisted observations (idempotent result; bars were already in the store).
- 20 close/open pairs requested; 13 valid, 7 missing; allPairsComplete=false; enoughHistory=false.
- Missing exact next-session 09:30 ET opens: 2026-10-16, 2026-10-22, 2026-10-27, 2026-11-05, 2026-11-09, 2026-11-10, 2026-11-12.
- The six earlier missing dates are incomplete historical pairs. 2026-11-12 is the current next-session opening endpoint and must be tracked as the current Overnight interval, not treated as a completed historical return.
- Provider's nearest bars are 4, 1, 2, 4, 1 and 1 minutes after the exact opening boundary for the six historical gaps. None is substituted. The 2026-11-12 query returned no current-day open; its nearest returned bar was the previous session close (−1,051 minutes).
- Existing 13 return observations explicitly use ONE_MINUTE_BAR_OPEN_PROXY. officialOpeningAuctionVerified=false for every pair.

The endpoint's current 20-pair request includes the current unfinished close-to-open interval in the 20 requested slots. Therefore, it reports 13/20 and seven missing, but the 20th slot for 2026-11-12 is not yet a completed historical observation. Do not count the current interval as a historical loss/miss in performance statistics. Keep separate fields/labels for (a) the last 20 completed historical pairs and (b) the current Overnight interval. The present response does not provide a complete 20 completed-pair history.

## Daily and catalyst evidence in the latest snapshot

- Daily returned 20/20 expected sessions, dataFresh=true, adjustment=all, but feed=IEX_PARTIAL and qualificationUsable=false.
- Observed diagnostics: close 36.10, SMA20 35.09275, average daily volume 514,554.3. Qualification fields price/SMA20/avgVol20 remain null by design because IEX is partial.
- Catalyst coverage complete, one MATERIAL article: Wells Fargo maintains Overweight and raises Corebridge Financial target to $41. Rule-set triage is MATERIAL_EVENT_REVIEW. This remains human-review-required, not a cleared catalyst.
- Saved evaluation request now correctly contains dataFresh=true and catalystStatus=RISK. This confirms the earlier mapping fixes are reflected in a newly captured snapshot.
- Evaluator remains DATA?, eligible=false. Missing inputs include all qualification Daily fields, historical/current Overnight metrics, 5M permission/structure, 1M state, extension ATR, current volume vs average, daily range and ATR%. Material catalyst risk is a blocker.

## Interpretation and next engineering requirement

1. The operator's Step 2 table is the correct saved-evaluation row and should be read as an evaluation, not a raw source record or trade.
2. Keep the strict exact-boundary rule. Do not replace a missing 09:30 bar with the nearest 09:31–09:34 bar.
3. Separate completed historical pairs from the live current interval in API semantics and UI. The current interval is previous RTH close through the next RTH 09:30 one-minute bar Open proxy; until that endpoint exists it is PENDING, not a completed missing historical return.
4. Add tests for the historical-window/current-interval distinction, including the current session endpoint missing, and ensure rolling win-rate/return metrics use only completed historical pairs.
5. Wire source-qualified live technical inputs (5M permission/structure, 1M state, extension ATR, volume ratio, daily range, ATR) with source timestamps/freshness. Unknown remains fail-closed.
6. IEX Daily observations remain diagnostic only. SIP/independent consolidated coverage and adjustment parity are not established.
7. The 09:30 one-minute bar Open is still a proxy, not the official opening auction print.

## Current outcome

No CRBG qualification. No order submitted. No code behavior is claimed changed by this evidence-only note.

## [Uncertainties]

- Data does not establish whether Alpaca IEX omitted these exact opening bars because of feed coverage, symbol-level activity, request behavior, or provider aggregation; root cause is unknown.
- No official opening-auction print source is present.
- The current capture's response schema has not yet been changed to separate the 19 completed historical transitions from the current in-progress interval.
