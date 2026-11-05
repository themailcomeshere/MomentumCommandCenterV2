# Master To-Do Audit — 2026-11-05

## November 5 follow-up — corrected V2.1 export and candidate evidence

- The newly committed V2.1 1m/5m EVR exports were reviewed. 1m has 346 observations, 48 intraday gaps overall and 13 in its latest 119 rows. Its 20 post-warm-up numeric ATR comparisons are within export rounding tolerance (mean absolute error ~0.00002603; maximum ~0.00004669), but gaps prevent certification. 5m has 81 observations and one intraday gap, so it remains below warm-up.
- The parser and parity service now exclude normal overnight/weekend date transitions from intraday gap counts. Same-New-York-date missing intervals still block MATCH. Full-session completeness remains open.
- The operator does not need to repeat SIP requests: prior recent-history SIP access returned HTTP 403 from subscription entitlement; IEX GTC_EXT rejection is also documented. Continue non-SIP development and keep SIP qualification disabled.
- The evidence-assessment button is located in Boring Overnight → Calculated Limit Sell, below the research quantile input. Do not click the separate GTC_EXT capture button while SIP access remains blocked.
- Candidate review: CBOE preferred addition, BLK strongest historical win rate, AVNT post-earnings momentum watch, EVER secondary, BLMN excluded from the Nov 5→Nov 6 ordinary overnight because of Nov 6 premarket earnings. Full analysis: `docs/Validation/Overnight-Candidate-Review-20261105.md`.

This is the second-pass index for the unchecked entries in `docs/Master-To-Do.md`. The master file retains historical line items and repeated references to the same workstreams. Those entries should not be interpreted as an equal number of independent features. The current status at the top of Master-To-Do supersedes older historical notes.

## First pass — work implemented from available code and evidence

| Workstream | Current result | Evidence |
|---|---|---|
| Thinkorswim V2 ATR payload parser | Implemented; V1 unchanged; V2.0 and V2.1 are distinguished | Actual 1m/5m exports, parser tests |
| ATR persistence | Separate append-only JSONL store, idempotent import and correction linkage | Store tests and API |
| Server Wilder ATR parity | Versioned, timestamp-matched calculation with warm-up, comparison count, gap and error metrics | Actual legacy fixtures, synthetic MATCH fixture |
| Corrected ThinkScript source | Previous-close shift fixed; corrected payload marker is V2.1 | Source commit and measured legacy mismatch |
| Sell-limit orchestration | Fail-closed API reads persisted history, exact reference close, V2.1 ATR, 99 5m bars and provider/TOS OHLCV parity | Synthetic complete-SIP fixture, IEX rejection and provider-mismatch tests |
| Sell-limit UI | Target quantile input, assessment action, per-gate reasons, DATA? state | Frontend syntax test; browser acceptance still required |
| Overnight scanner evidence | Original and updated exports compared; top-20 score review and row completeness documented | `docs/Validation/Overnight-Scanner-Review-20261105.md` |

Exact code commit with green CI: `c990faf973fb9e3a14ec948386e210b9eb8b4fc5`; Actions run [42119562504](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42119562504), 223/223 tests passed. Subsequent documentation-only commits do not change that tested code.

## Second pass — check what the first pass unblocked

| Previously blocked work | Did it become unblocked? | Current action/status |
|---|---|---|
| ATR parity | Yes, at algorithm-diagnostic level | Service and API compare the latest 119 bars (99 warm-up + 20 comparisons) and return GAPS_PRESENT rather than MATCH when timestamps are missing. Legacy V2.0 exports show the old shifted formula; corrected V2.1 platform export still needed. |
| Sell-limit orchestration | Partially | API/UI now check the upstream evidence gates, including exact provider/TOS OHLCV across the full 119-bar parity window. Numeric output remains blocked by the explicit walk-forward gate and SIP qualification. |
| Provider/TOS 5m parity | Partially | Exact comparison code exists for the latest 119 bars (99 warm-up + 20 comparison). User/runtime SIP 5m bars and corrected V2.1 export are not present. |
| GTC_EXT history qualification | No | Effective Alpaca feed is IEX; prior SIP history request returned 403. No 20/20 SIP qualification evidence. |
| Walk-forward target validation | No | Train/test target-touch and adverse-excursion validation is not implemented. The API fails closed at this gate. |
| Actual execution reconciliation | No | No actual broker fill, quantity, fee and timestamp records were supplied for reconciliation. OHLCV highs remain theoretical touches only. |
| Daily qualification and catalyst classification | No | The supplied scanner exports do not provide SIP daily qualification or human-labelled catalyst outcomes. Earlier IEX daily results were partial and unusable for qualification. |
| Opportunity Quality full-pool ranking | Partially | Scanner score preview is available, but the updated export has 296 rows with at least one unavailable metric. No candidate bypasses current price, liquidity, catalyst, exclusion, 5m or 1m gates. |
| WebSocket/collector/performance/browser acceptance | No | CSV exports do not prove local runtime behavior. Requires local API/browser/network/console measurements. |
| Thinkorswim chart-state/auction-print parity | No | Corrected ATR source is not yet compiled/exported; official auction print and full matched state tape are not in the supplied files. |
| Schwab/broker integration and packaging | No | Requires separate OAuth/developer approval, execution design and release acceptance. MCC does not currently route orders. |

## User actions required

1. Pull the latest feature branch and run `.\scripts\Get-Latest-And-Test.ps1`.
2. Compile the corrected ThinkScript V2.1 in Thinkorswim. The corrected payload marker is `MCC_OHLCV_ATR|v2.1|`; legacy `v2` files are tagged V2.0 and excluded from sell-limit parity.
3. Export corrected EVR 1m and 5m Strategy Reports. The 5m report needs at least 119 consecutive completed observations (99 warm-up + 20 comparison); any gap in that window blocks MATCH.
4. Import both through Dashboard → StrategyReport. Confirm the parity state and source metadata.
5. Confirm actual Alpaca SIP historical entitlement before changing the effective feed. Do not attempt GTC_EXT qualification on IEX.
6. Use the Overnight **Assess sell-limit evidence gates** action to see the remaining blocked reasons. It will not issue a numeric limit until walk-forward validation is implemented.

## [Uncertainties]

- Thinkorswim V2.1 compilation and corrected export parity have not been operator-verified.
- SIP entitlement and 20/20 SIP GTC_EXT coverage are not established.
- Walk-forward thresholds have not been agreed or calibrated; target-touch results must not be represented as actual fills.
- Local browser acceptance, WebSocket recovery, collector schedule and performance are not established by repository CI.
