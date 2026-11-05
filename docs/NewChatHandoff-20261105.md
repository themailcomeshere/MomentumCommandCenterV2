# Handoff — November 5, 2026 (updated after V2 ATR exports)

Branch: feature/opportunity-quality-classification

Priority: Calculated Limit Sell. Implementation contract and current evidence: `docs/Calculated-Limit-Sell-Implementation.md`. Master checklist: `docs/Master-To-Do.md`.

## New V2 export evidence

- 11/03 export: 81 EVR 5m V2 records, all include OHLCV and ATR14. Timestamps 14:05Z–20:50Z; one 10-minute step indicates one missing 5m interval; other intervals are 5m. ATR range 0.4962–2.0107.
- 11/04 export: 79 EVR 5m V2 records, all include OHLCV and ATR14. Timestamps 14:30Z–21:00Z; contiguous 5m steps. ATR range 0.7834–2.5238.
- Total: 160 records across two dates. Enough to implement/test V2 parsing and start ATR parity engineering; not enough for 98-bar warmup coverage at every comparison point or 20-session sell-limit qualification.
- User-recorded settings: extended-hours ON; corporate-action adjustment ON; chart timezone Phoenix local (UTC-07:00); ATR length 14; Wilder average. V2 defaults atrLength=14 and computes Wilder ATR.
- Export presence is not proof of parity. At least 20 consecutive chart-plot/OHLCV comparisons remain to be explicitly verified. Do not set parity MATCH from export ingestion alone.

## Current blockers and implementation status

- Existing: exact GTC_EXT capture/audit (20 sessions, 780 exact 1m bars/session plus exact prior close); fail-closed quantile calculator; adverse/flat handling.
- Missing: V2 ATR report reader/storage, server-calculated ATR series and timestamp-matched parity audit, end-to-end orchestration API, UI/provenance, walk-forward validation, broker-fill linkage.
- Current effective Alpaca feed is IEX. GTC_EXT qualification requires SIP; verify entitlement and effective runtime config before changing feed or attempting capture.
- Numeric Calculated Limit Sell remains DATA? / NOT VERIFIED until 20/20 SIP windows, exact reference close, adequate same-source 5m ATR history and server-derived parity MATCH all pass.

## Next actions

1. Compile the V2 ThinkScript in Thinkorswim and confirm ATR length 14 / Wilder average type. Keep the chart settings recorded above.
2. Compare at least 20 consecutive exported rows against the chart's ATR plot and OHLCV. Include rows around the 11/03 missing 5m interval; confirm the gap is genuinely absent rather than a report-export omission.
3. Implement the V2 ATR parser and tests against both committed CSV exports, retaining V1 reader compatibility.
4. Verify Alpaca SIP entitlement and effective feed configuration. Do not switch blindly; confirm `/api/providers/status` after any controlled configuration change.
5. Capture/audit 20 complete GTC_EXT SIP sessions (780 exact 1m bar starts per session plus exact previous regular close).
6. Implement the server-side calculation endpoint/UI. Parity must be computed from stored, timestamp-matched TOS and server ATR values; never accept a caller-supplied MATCH flag.
7. Run chronological walk-forward validation with independent future sessions; separately integrate actual broker executions for fill/outcome analysis.
8. Pull latest branch and run `.\\scripts\\Get-Latest-And-Test.ps1` from a clean checkout; restart MCC and browser-test the new endpoint/UI after tests pass.

[Uncertainties]
- The ThinkScript V2 study has not been independently compiled in Thinkorswim in this pass.
- The 11/03 10-minute step is a missing source timestamp; whether it reflects an absent chart bar or export omission is not established.
- The 20-row visual chart comparison, 20/20 SIP coverage, SIP entitlement, exact current close, parity tolerance result and actual broker fills remain unverified.
