# Handoff — November 5, 2026

Branch: feature/opportunity-quality-classification

Priority: Calculated Limit Sell. Detailed implementation contract: docs/Calculated-Limit-Sell-Implementation.md. Master checklist updated in docs/Master-To-Do.md.

## Verified evidence
- Correct UI control is “Fetch / retry 20-session close/open history”; it is the 20-pair capture action. Leave From/To unchanged when retrying the same rolling window.
- Latest EVR result: 11/20 pairs valid; 9 opens missing; provider/store boundary coverage 31/40; 3,759 fetched bars; 0 newly stored; Alpaca/IEX. Rolling metrics correctly withheld.
- User confirms appsettings.json and secrets.json both specify IEX. GTC_EXT SIP capture is blocked under this effective feed.
- EVR 1M report: 365 OHLCV records with 21 inter-record gaps. EVR 5M report: 79 contiguous 5-minute OHLCV bars. Neither report includes ATR14 values or Thinkorswim study settings. Insufficient for parity certification.

## Implementation status
- Existing: exact GTC_EXT capture/audit (20 sessions, 780 exact 1m bars/session + prior close); fail-closed quantile calculator; adverse/flat windows correctly handled.
- Missing: orchestration API, verified reference close, server-computed ATR and timestamp-matched TOS parity, UI/provenance, walk-forward validation, broker fill integration.
- Numeric Calculated Limit Sell must remain DATA? / NOT CALCULATED until complete 20/20 SIP windows and ATR parity pass. OHLCV touches are not fills.

## Next actions
1. Keep IEX unchanged until SIP entitlement/configuration is verified. Do not run GTC_EXT SIP capture with IEX.
2. Build the server-side calculation endpoint and UI per docs/Calculated-Limit-Sell-Implementation.md. Do not accept caller-supplied history or a bare MATCH flag.
3. Obtain actual TOS ATR14 values/settings and a continuous matched 5M tape with 98 Wilder prefetch bars plus comparison bars.
4. Capture >20 complete SIP windows and run chronological walk-forward tests.
5. Integrate actual broker executions separately.
6. Pull updates and run .\scripts\Get-Latest-And-Test.ps1 from a clean checkout; restart MCC after tests pass.

[Uncertainties]
Cause of the nine missing opens, SIP entitlement, complete GTC_EXT coverage, ATR parity and actual fills are not yet verified.
