# Boring Overnight technical staleness audit — 2026-11-12

## Operator response

The supplied capture returned:

- Provider/feed: Alpaca / IEX for 1m and 5m; configured feed IEX; `feedMatchesCapture=true`.
- Source transport: REST.
- Evaluation status: `STALE_MARKET_DATA`.
- 1m source bar start: `2026-11-12T20:59:00Z`; freshness false.
- 5m source bar start: `2026-11-12T20:55:00Z`; freshness false.
- 5m permission, 5m structure and 1m state: null.
- Extension ATR: 2.728123371991356; same-time-of-day 5m RVOL: 0.7608139855038779.
- Daily range and ATR: null, with SIP-only basis disclosed.
- No technical-assessment error.

## Interpretation

1. Feed consistency passed. This rules out the specific mixed-provider/feed problem for this response; it does **not** establish consolidated-market coverage. IEX is partial-exchange coverage.
2. The two intraday bars failed the live service freshness thresholds. The service calculates age from the completed bar time (bar-start timestamp plus one minute/five minutes) and requires 1m age <=120 seconds and 5m age <=360 seconds. The supplied payload does not include the actual age values or the assessment's `evaluatedAtUtc`, so the precise elapsed ages and capture delay cannot be reconstructed from this response alone.
3. Null permission/structure/state is correct: the V2 decision engine does not authorize decisions from stale bars.
4. Non-null extension ATR and RVOL were a qualification leak. They were diagnostic calculations derived from the same stale 1m/5m series, but the capture copied them into the evaluator input despite both freshness flags being false. That is now corrected: numeric intraday indicators require both timeframes fresh and feed provenance matched. V2 permission/structure/state additionally require `evaluationStatus=EVALUATED`.
5. Daily range/ATR null is correct for the disclosed SIP-only gate. The configured feed is IEX; IEX Daily values cannot populate qualification fields. A separate consolidated SIP Daily sample is required.
6. REST transport is not itself a freshness failure. Freshness is determined from completed-bar timestamps and thresholds, not the transport label.

## Code changes

- Added `BoringOvernightTechnicalInputPolicy` to separate fresh-bar eligibility from V2 decision eligibility.
- Capture now withholds extension ATR and same-time-of-day RVOL unless both 1m and 5m bars are fresh and both feeds/provider match the history capture.
- Capture withholds 5m permission, 5m structure and 1m state/action unless the above conditions hold **and** the V2 engine reports `EVALUATED`. Thus `OUTSIDE_REGULAR_SESSION`, `STALE_MARKET_DATA`, `RVOL_BASELINE_INSUFFICIENT` and `INSUFFICIENT_HISTORY` cannot supply V2 decision fields.
- Response now includes `intradayBarsUsable`, `v2DecisionUsable`, per-timeframe age seconds and the configured freshness limits, in addition to timestamps and freshness booleans.
- Added policy tests for matched/mismatched feeds, true/false/unknown freshness, stale/outside-session/insufficient-baseline statuses, and V2 decision eligibility.

## Commits

- `a5a089fb13bd63727c619ae335b9473ff4a25cfc` initial policy helper.
- `6e8248e664f6d40c51b5b1501cab0f6d86b44361` initial capture gate.
- `e4a6918a0b50a5421858316fb9e82f2b9e3bb235` initial policy tests.
- `a986e8107908bc626656732b7b03320a262b1170` split numeric indicator and V2 decision gates.
- `58ade3a9651304d75440df766bc0dba1495e0616` apply separate gates to capture mapping.
- `e39cd406092795ac553a56bccce36c5a961ccc05` regression tests for policy.
- `853fe72c6af3baa9187967d87b876eab78d2b264` expose age/threshold diagnostics.

## Required verification

- [ ] Run Release build and complete test suite on the latest branch head. No test/build result is available for these commits yet.
- [ ] Repeat a capture during regular session while the market is open; confirm both ages are within thresholds, `intradayBarsUsable=true`, and, only when the V2 engine reports `EVALUATED`, `v2DecisionUsable=true`.
- [ ] Repeat a capture after the regular session; confirm V2 decision fields remain null and `v2DecisionUsable=false`. Numeric indicators may remain populated only if their completed bars are still within the explicit freshness thresholds; otherwise they must be null.
- [ ] Confirm saved evaluator snapshot contains null stale intraday numeric fields (not merely a null display value).
- [ ] Obtain separate SIP Daily evidence before Daily range/ATR can populate qualification fields. Do not relabel IEX as SIP.
- [ ] Keep TOS ATR parity, official opening-auction verification, and candidate READY qualification as separate open gates.

## [Uncertainties]

- The supplied payload omits `evaluatedAtUtc`, age seconds and freshness limits; exact staleness duration is not calculable from it.
- It is not established whether the capture occurred during regular session, after close, or after a delayed REST response.
- No SIP Daily data or Thinkorswim parity evidence was supplied.
- The source changes are not yet build/test verified on their latest commit.
