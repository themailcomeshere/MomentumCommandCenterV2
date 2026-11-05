# November 5 — ATR V2 ThinkScript compile-error correction

- User reported `AggregationPeriod.ONE_MIN` unknown, `Expected double`, and incompatible string parameters.
- Root causes: ThinkScript's one-minute aggregation enum is `AggregationPeriod.MIN`; `def` is numeric and cannot hold string labels.
- Corrected `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`: replaced `ONE_MIN` with `MIN`; removed string-valued `def timeframeLabel`; embedded the conditional timeframe string directly in label and order-name expressions.
- Commit `39cacef3a68f88e2e3fc33467d260ac00f175b37` (subsequent Master To-Do documentation commit `56188b80ec61210a0ee5f94cef6b9712544ebba3`).
- GitHub Actions `build-and-test` for the ThinkScript/documentation commit was still in progress at last check. .NET CI cannot compile ThinkScript.
- **Not yet done:** Thinkorswim compile/export validation; V2 parser/tests; separate ATR persistence; production Wilder ATR seed parity; matched-bar parity service; sell-limit orchestration API/UI; SIP entitlement/history; walk-forward validation; broker-fill reconciliation.
- Existing V1 parser and research calculators are not completion of those deliverables. Full status is tracked in `docs/Master-To-Do.md`.

## Immediate operator validation
1. Pull the feature branch and open the corrected `MCC_OHLCV_ATR_V2.txt`.
2. Compile in Thinkorswim. If it still errors, send the complete new compiler output; do not edit the study manually.
3. On EVR 5m, set `chartAggregation=FIVE_MIN`, confirm chart aggregation is 5m and the label says MATCH.
4. Export a short date range and verify payload contains 11 pipe-delimited fields: marker, version, symbol, timeframe, epoch milliseconds, O, H, L, C, volume, ATR.
5. Do not use the export for a numerical sell limit yet.

# Handoff — November 5, 2026 (updated)

Branch: feature/opportunity-quality-classification

Priority: Calculated Limit Sell. Contract: `docs/Calculated-Limit-Sell-Implementation.md`. Master checklist: `docs/Master-To-Do.md`.

## Operator-confirmed Thinkorswim settings
- Extended-hours ON.
- Corporate-action adjustment ON.
- Chart timezone Phoenix local UTC-07:00.
- ATR length 14, Wilder average.
- User confirms the original V2 study compiles without errors.

## V2 aggregation selector change
- Updated `docs/Charts/Flexible Grid/OHLCV/MCC_OHLCV_ATR_V2.txt`.
- Added `chartAggregation` input and support for 1m, 5m, 15m, 30m, 1h, 4h.
- Payload timeframe is dynamic; mismatch between selected input and actual chart aggregation blocks export and is shown in labels.
- ThinkScript cannot change the host chart aggregation. Set both the study input and chart aggregation control to the same period. No source edit is needed to switch among supported periods.
- This revision still needs Thinkorswim compilation and validation at each selected aggregation.

## Provider status
User supplied `/api/providers/status`: Alpaca configured/validated, effective feed IEX, both appsettings and secrets specify IEX; Massive not configured. 34 bars in last successful validation. This does not establish SIP entitlement. Do not change feed blindly; GTC_EXT qualification remains blocked pending SIP entitlement/effective feed verification.

## ATR exports and calculated sell limit
- Nov 3 export: 81 records, one missing 5m timestamp interval; ATR14 present on every row.
- Nov 4 export: 79 contiguous 5m records; ATR14 present on every row.
- 160 records over two dates are enough to start parser/parity implementation, not enough for 20-session sell-limit qualification or 98-bar warmup for every comparison.
- Still missing: V2 reader and tests, separate TOS ATR persistence, server ATR series/parity, orchestration API/UI, 20/20 SIP GTC_EXT sessions, exact reference close, walk-forward tests and actual broker execution linkage.

## Next actions
1. Compile the updated aggregation-selector study in Thinkorswim.
2. Test 5m first, then each additional aggregation you intend to use. Set both input and chart controls identically; confirm the label says MATCH and payload timeframe matches.
3. Re-export EVR 5m and compare at least 20 consecutive rows with chart OHLCV and ATR.
4. Diagnose the missing November 3 5m timestamp.
5. Verify SIP entitlement without changing IEX configuration until confirmed.
6. Continue V2 parser/storage/parity implementation, then orchestration API/UI with fail-closed status.
7. Run `.\\scripts\\Get-Latest-And-Test.ps1`, restart MCC and browser-test after code implementation.

[Uncertainties]
The revised study has not yet been compiled in Thinkorswim. Compatibility of each aggregation enum in the user's installed TOS build, actual strategy-report payload preservation, ATR parity, SIP entitlement, GTC_EXT completeness and broker fills remain unverified.


# Handoff update — November 5, 2026 (latest V2.1 exports and candidate review)

The operator confirmed the aggregation-selector change works and supplied corrected V2.1 EVR Strategy Reports:
- `data/ScannerExports/StrategyReports_EVR_11032026_1M_ATR_v2_1.csv`: 346 ATR payload observations, 48 intraday gaps overall, 13 intraday gaps in latest 119.
- `data/ScannerExports/StrategyReports_EVR_11032026_5M_ATR_v2_1.csv`: 81 ATR payload observations, one 10-minute jump; insufficient for 99 warm-up + 20 comparison bars.
- The 1m last 20 post-warm-up numeric ATR values have mean absolute error ~0.00002603 and max ~0.00004669 against server Wilder calculation, within 0.00005 export tolerance. The latest 119 rows still contain 13 intraday gaps, so this is not certified MATCH. This was computed from the committed CSVs; Dashboard import/API verification remains to be done.

New code:
- `AtrTimestampGapDetector.cs` now excludes scheduled overnight/weekend date transitions from intraday gap counts in parser and parity service.
- Same-New-York-date missing intervals still block MATCH.
- Full-session completeness (detecting a completely absent trading session) remains an open item.
- Added regression tests for overnight/weekend transitions.
- Latest code commit: `c9003fd24d3b39817a874304195e01619c79de21`; CI [42121843682](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42121843682) succeeded: build succeeded and 225/225 tests passed.

SIP: Prior recent-history SIP requests returned HTTP 403 due subscription entitlement. IEX preflight rejection is a separate known limitation. Do not repeat SIP requests or ask the operator to repeat them; do not run GTC_EXT capture while entitlement remains denied. Continue IEX-only diagnostic development.

Button location:
Dashboard → Workspace dropdown → Boring Overnight → Calculated Limit Sell → Research target quantile (%) → **Assess sell-limit evidence gates**. HTML ID: `overnightCalculateSellLimit`. Do not click the separate GTC_EXT history capture button while SIP is blocked.

Candidate review:
`docs/Validation/Overnight-Candidate-Review-20261105.md` documents AVNT, BLMN, EVER, BLK and CBOE. CBOE is the preferred addition to the next ordinary Overnight review; BLK has the best historical win rate; AVNT is a post-earnings momentum watch; EVER is secondary; BLMN is excluded for the Nov 5→Nov 6 routine overnight because it reports earnings premarket Nov 6. These are research priorities, not READY decisions.

Next operator actions:
1. Pull latest branch and run `.\scripts\Get-Latest-And-Test.ps1` after CI is green.
2. Import the two V2.1 exports through Dashboard → StrategyReport; verify counts and parity states.
3. Export a longer 5m V2.1 history with at least 119 observations, allowing multiple sessions; no intraday gaps within a New York date in the comparison window.
4. Use the sell-limit evidence button only for diagnosis; numeric limit remains blocked by walk-forward validation and SIP GTC_EXT history.
