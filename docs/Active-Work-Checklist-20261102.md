# Active Work Checklist — 2026-11-02

## Completed from latest user feedback
- [x] Identified IEX configuration override: local secrets.json overrode appsettings.json.
- [x] Confirmed SIP can be selected by configuration, then captured Alpaca HTTP 403: recent SIP data is not permitted by the current subscription.
- [x] Restored IEX and confirmed provider validation returned 62 bars.
- [x] User visually accepted the prototype Main Chart layout without horizontal scrolling.
- [x] Corrected Main Chart timestamp display: show completedAtUtc (bar close) and separately show the bar-start-to-close interval.
- [x] Recorded latest INO TOS/MCC readings and the fact that TOS 24.6 ATR was a live label, not a historical per-bar value.
- [x] Recorded Schwab Trader API — Individual approval; app registration and OAuth remain separate.
- [x] Defined RTO audit as exact 40-boundary coverage across 20 valid sessions.
- [x] Fixed prototype header structure: System Readiness is now a sibling of topbar-brand, matching production's two-column header.
- [x] Overrode anywhere-wrapping for marketTransportLabel so the text wraps at word boundaries rather than character-by-character.
- [x] Fixed scanner CSV import so it immediately re-renders Red Zone as well as the scanner table.
- [x] Documented Red Zone refresh cadence: 15-second live 1M/5M card assessment refresh; 30-second Alpaca discovery refresh when its group is selected.
- [x] Added technical update/build/API verification workflow and `scripts/Get-Latest-And-Test.ps1`.
- [x] Added Overnight Scanner Quick Start and November 2 candidate/news review.

## Open P0 — runtime acceptance
- [x] Release build and tests passed on branch head `83cd9780daa9f8e94628e8d24f4bc6ab4b03c415`: GitHub Actions run [41514111035](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41514111035), 0 warnings, 0 errors, 179/179 tests.
- [ ] Hard-refresh prototype and verify the repaired header, transport label wrapping, and timestamp display on desktop and mobile.
- [ ] During regular market hours, capture live-assessment JSON and calculate evaluatedAtUtc minus oneMinute.completedAtUtc. Investigate if >=60 seconds for a liquid symbol.
- [ ] Complete WebSocket delivery test for SPY, RKLX, ASTX and XRPN using advancing per-symbol bar counts and timestamps.
- [ ] Complete controlled collector failure/recovery, restart/checkpoint reload and sleep/wake acceptance.

## Open P1 — strategy and data correctness
- [ ] Match TOS and MCC on the exact same completed 1M candle; obtain five-decimal Close, VWAP, ATR14 and historical ExtensionATR from the Data Box.
- [ ] Reconcile VWAP/ATR differences only after timestamp, session scope and feed match. Do not tune thresholds to force parity.
- [ ] Complete Red Zone source automation and versioned opportunity-quality ranking with validated participation/liquidity inputs, explanations, stable re-ranking and tests. CSV import and periodic card assessments are implemented; automatic Thinkorswim scan ingestion is not.
- [ ] Complete source-backed catalyst, daily SMA20/average volume, exclusion reconciliation and immutable Boring Overnight snapshots. Boring Overnight remains WATCH/manual, not READY.
- [ ] Complete GTC_EXT history, target-touch/time-to-touch/adverse-excursion analysis, walk-forward target quantile and Calculated Limit Sell integration.
- [ ] Complete matched TOS 1M/5M state replay; OHLCV alone cannot prove study states.
- [ ] Extend Morning Brief lineage with news/discovery watermarks, raw-response retention, dependency manifest and outcome linkage.

## Open P2 — integrations and delivery
- [x] User reports Schwab Trader API — Individual approval.
- [ ] Create MCC Schwab app and attach Accounts and Trading Production.
- [ ] Confirm app Active/Ready status and exact callback URL.
- [ ] Configure app credentials in local server-side secret storage only.
- [ ] Implement OAuth, protected token persistence/refresh and read-only account/position retrieval.
- [ ] Persist normalized position snapshots with provenance; distinguish flat from missing/stale/failed data.
- [ ] Keep order-writing endpoints out of Phase 1/2.
- [ ] Validate scanner provider coverage, licensing, rate limits, backfill and retention. Current Alpaca movers source is SIP-based and the user's recent-SIP request returned HTTP 403.
- [ ] Decide desktop packaging/startup and data retention/backup policy.

## Open P3 — older validation and UX
- [ ] Re-run PMI sparse-window regression with a liquid control; one-bar sample must be INSUFFICIENT DATA.
- [ ] Browser-verify date handling under a non-Arizona OS timezone.
- [ ] Complete Morning Brief modal/history, replay/HTTP 400, Red Zone responsive layout, sound permission and clean reload checks.

## RTO audit status
The RTO audit checks 40 exact prior-close/next-open 1-minute timestamps across 20 valid sessions, separately recording provider-response and store presence. Prior capture: 38/40 exact on IEX. Missing next-session opens: 2026-10-14 13:30Z (nearest 13:34Z) and 2026-10-21 13:30Z (nearest 13:31Z). The file named RTO-SIP-20261101.json actually reported feed=iex. A SIP audit is blocked by the explicit recent-SIP HTTP 403. Keep IEX and SIP histories separate; do not impute missing bars.

## Current operating decisions
- Use IEX for current usable Alpaca data. Do not call SIP validated until a recent SIP request succeeds.
- Treat completed-bar timestamp correction as a display fix, not proof that provider latency is resolved.
- Do not claim numerical TOS parity from rounded inputs or a live EXT label.
- Keep Boring Overnight manual-only and Schwab integration read-only.


## New workflow documents (2026-11-02)
- [x] `docs/Technical-Workflow.md` is the operator's standard get-latest/build/restart/API smoke-test sequence; `docs/Runtime-Acceptance-Runbook.md` remains the detailed acceptance source of truth.
- [x] `scripts/Get-Latest-And-Test.ps1` safely checks for a clean tree, fetches/switches/pulls the project branch, and runs Release restore/build/tests.
- [x] `docs/Overnight-Scanner-Quick-Start.md` documents the TOS scan → hard gates/news → CSV import → Red Zone → chart review process.
- [x] `docs/Validation/Overnight-Scanner-Review-20261102.md` records the MHK/OSW/EVR shortlist and catalyst review.
- [ ] User visually verifies the header/transport label fix after pulling the latest branch.
- [ ] User uploads Daily and 5M charts for OSW, EVR, and MHK for final chart-level review.

## [Uncertainties]
- Actual INO bar age is not measured; exact date/time and raw API response for the reported delay were not supplied.
- Exact unrounded TOS values and historical ExtensionATR for the same MCC candle are unavailable.
- Schwab app status, attached products, callback URL and credentials are not confirmed.
- Current Alpaca subscription denied recent SIP requests; future entitlement changes may alter this.