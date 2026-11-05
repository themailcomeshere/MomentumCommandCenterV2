# Master To-Do Audit — 2026-11-05

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

Exact code commit with green CI: `f3f384392843ce8d3f4b434a8e5687e0dd1d7125`; Actions run [42118758103](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42118758103), 222/222 tests passed. Subsequent documentation-only commits do not change that tested code.

## Second pass — check what the first pass unblocked

| Previously blocked work | Did it become unblocked? | Current action/status |
|---|---|---|
| ATR parity | Yes, at algorithm-diagnostic level | Service and API implemented. Legacy V2.0 exports show the old shifted formula; corrected V2.1 platform export still needed. |
| Sell-limit orchestration | Partially | API/UI now check the upstream evidence gates. Numeric output remains blocked by the explicit walk-forward gate and SIP qualification. |
| Provider/TOS 5m parity | Partially | Exact comparison code exists for the latest 99 bars. User/runtime SIP 5m bars and corrected V2.1 export are not present. |
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
3. Export corrected EVR 1m and 5m Strategy Reports. The 5m report needs at least 119 completed observations (99 warm-up + 20 comparison) and a continuous latest 99-bar segment.
4. Import both through Dashboard → StrategyReport. Confirm the parity state and source metadata.
5. Confirm actual Alpaca SIP historical entitlement before changing the effective feed. Do not attempt GTC_EXT qualification on IEX.
6. Use the Overnight **Assess sell-limit evidence gates** action to see the remaining blocked reasons. It will not issue a numeric limit until walk-forward validation is implemented.

## [Uncertainties]

- Thinkorswim V2.1 compilation and corrected export parity have not been operator-verified.
- SIP entitlement and 20/20 SIP GTC_EXT coverage are not established.
- Walk-forward thresholds have not been agreed or calibrated; target-touch results must not be represented as actual fills.
- Local browser acceptance, WebSocket recovery, collector schedule and performance are not established by repository CI.
