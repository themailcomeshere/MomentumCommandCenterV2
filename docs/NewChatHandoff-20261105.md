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
