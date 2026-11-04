## Updated operator guidance — November 3, 2026

- [x] Red Zone bulk clear, restore and fresh CSV re-import passed user browser acceptance.
- [x] 20-session capture duration improved to approximately 7–24 seconds in user testing.
- [ ] After latest pull, verify every 20-session capture appends one input snapshot; saved-snapshot table refreshes automatically.
- [ ] Confirm snapshot historical metrics are null when any exact pair is missing; price, Daily liquidity, catalyst and chart inputs remain UNKNOWN until sourced.
- [ ] Record Fetch & Analyze elapsed time and returned bar count; current observed RTO 1m request took approximately 28 seconds.
- [ ] Compare page-load Network timing after Morning Brief is deferred to modal-open; current observed page load approximately 45 seconds.
- [ ] Official 09:30 ET opening print is the selected strategy definition. Current 09:30 one-minute bar Open is only a proxy until official auction-print parity is validated.
- Use the Provider response vs persisted store table only when investigating missing exact boundaries or provider/store mismatch. Provider exact means returned in this fetch; Store exact means already persisted. The nearest timestamp is diagnostic only, never a substitute.
- Ticker-vs-SPY is optional troubleshooting for sparse/empty provider coverage. It is not required for routine candidate review or qualification.

---

## Red Zone recovery and import handling (added 2026-11-03)

- [ ] After pulling the latest branch, import the current scanner CSV.
- [ ] Confirm the import feedback reports the number of prior matching dismissals reset.
- [ ] Confirm up to four eligible candidates render. Historical CSV gates select the preview; current price, liquidity, catalyst, 5M and 1M evidence remain separate qualification gates.
- [ ] Use **Dismiss/Clear all Red Zone Candidates** to clear the selected scanner group's candidates.
- [ ] Use **Restore dismissed / repopulate** to bring the selected group's eligible candidates back.
- [ ] Re-importing a fresh CSV resets prior dismissals for symbols in that file; it does not mark them READY.

## 20-session capture acceptance (added 2026-11-03)

- [ ] Run capture once after API restart and record elapsed duration.
- [ ] Verify provider/feed, fetched bars, newly persisted count, exact provider/store boundary counts and missing timestamps.
- [ ] A zero newly persisted count can be normal when identical observations already exist.
- [ ] Do not treat a zero snapshot count as zero market bars; snapshots are evaluator-input audit records.
- [ ] Do not calculate rolling metrics with missing exact close/open pairs.
- [ ] Do not substitute a nearby bar without an explicit strategy-definition change and a separate validation of the revised metric.

---

# Overnight Process — Operational Checklist (2026-11-03)

## Purpose and controlling definition

This checklist implements the corrected Boring Overnight schedule. The strategy interval is **Day 1 after-hours plus Day 2 premarket**. Candidate selection must happen before the Day 1 regular-session close; starting after-hours is too late.

- Precompute historical metrics before the evaluation window.
- Start actionable review in the **final 60 minutes before the actual regular-session close**.
- Schedule from the exchange calendar, then display/use **America/Phoenix local time**. On ordinary sessions this is noon Arizona during Eastern Daylight Time and 1:00 PM Arizona during Eastern Standard Time. On early-close days, move the evaluation start to 60 minutes before the shortened close.
- Buy deadline is the actual Day 1 regular-session close.
- After the close, Day 1 after-hours remains part of the overnight holding interval. Day 2 premarket is also part of that interval.
- Keep all timestamps stored in UTC and render operator-facing schedule in Arizona local time.
- Decision support only. No automatic order submission.

## Operator sequence

### Before Day 1 final hour

- [ ] Pull latest `feature/opportunity-quality-classification`, run `.scripts\Get-Latest-And-Test.ps1`, and confirm the exact-head build/tests pass.
- [ ] Confirm API health and the market-session clock. Verify evaluation-start and buy-deadline values against the exchange calendar; do not hardcode a fixed Arizona time for early closes.
- [ ] Import the current Overnight Scanner CSV. Confirm the number of imported rows equals the file's data rows and inspect duplicate/parse diagnostics.
- [ ] Review historical qualification: ON win rate, median/mean ON returns, 5D vs 20D, current ON move, trend, sample count and exclusion status. Missing fields remain DATA?, not PASS.
- [ ] Resolve price and 20-session average-volume evidence from a timestamped daily source. The CSV alone does not provide these fields.
- [ ] Review catalyst/news status with source, publication time and review timestamp. UNKNOWN/STALE is not CLEAR.
- [ ] Keep a manual-review shortlist; do not equate historical PASS or Overnight Score rank with READY.

### Final 60 minutes before Day 1 close

- [ ] Refresh current-day qualification and current ON move.
- [ ] Refresh Daily trend/liquidity and inspect the 5M authoritative permission/structure output.
- [ ] Inspect 1M only for entry timing; 1M timing must not replace the 5M authority layer or historical overnight suitability.
- [ ] Confirm source freshness and session provenance for every required field.
- [ ] Record candidate status, all reason codes, as-of timestamp, source references and manual decision.
- [ ] Consider an entry only before the actual regular-session close and only after the user's separate broker/order eligibility checks. MCC does not submit orders.

### After Day 1 close through Day 2 premarket

- [ ] Preserve after-hours and premarket bars as distinct raw sessions; do not collapse them into regular-session OHLCV.
- [ ] Track any actual fill separately from the reference close used in historical studies.
- [ ] For an unfilled GTC_EXT sell, review actual order status and source-backed news/liquidity context. Never infer fill probability from a historical high touch.
- [ ] Capture the full eligible sell window according to the verified broker order qualifier/session. Do not assume all extended-hours bars are executable.
- [ ] Record actual fill, fees/costs, exit, realized P/L and timestamps separately from historical target-touch observations.

## Current verified implementation/evidence

- API clock exposes `overnightEvaluationStartUtc`, `overnightBuyDeadlineUtc`, and `overnightEvaluationOpen`; production and prototype show evaluation-start and buy-deadline countdowns.
- `MarketSessionClock` derives the final-hour start from the actual scheduled regular close and versioned exchange calendar; early-close regression coverage exists.
- Current user-confirmed CSV import: 198 rows; 187 historical PASS, 11 FAIL, 0 UNKNOWN, 0 exclusion matches, 0 final READY. This is a historical-gate result, not a complete trade-ready list.
- Current Red Zone expected preview from the imported data: top four by Overnight Score among historical-gate passes were LXU 68.60, OSW 67.48, MHK 66.79, PUK 65.13, subject to manual pins/dismissals. Browser confirmation after the startup-state fix remains pending.
- Exclusion API runtime returned 56 entries (55 canonical plus TEST); OSW/EVR/MHK are not canonical exclusions.
- Order submission remains NOT IMPLEMENTED.

## Remaining blockers — do not mark READY until resolved

- [ ] Browser acceptance after commit `4906d4c1119bedec6ec83f67cd21ff19d2c409fa`: no `redZoneAssessmentRefreshers` temporal-dead-zone exception; Imported CSV restored; expected Red Zone preview visible.
- [ ] Inspect the uploaded runtime-validation ZIP contents and establish per-symbol WebSocket provenance, freshness and stream deltas. ZIP is received; its JSON contents have not yet been analyzed. Do not infer from subscription acknowledgements.
- [ ] Complete source-backed daily price/SMA20/20D average-volume integration.
- [ ] Connect a validated catalyst source or retain UNKNOWN/manual review; do not default to CLEAR.
- [ ] Complete matched 5M/1M source evidence and Thinkorswim compile/parity acceptance.
- [ ] Build complete 20-session GTC_EXT-eligible high-excursion observations from source-backed bars before using the sell-limit calculator. Require 20 complete windows and ATR parity MATCH; keep high-touch distinct from actual fill probability.
- [ ] Validate broker order qualifier, eligible hours and symbol/account eligibility. No assumption that the broker permits every extended-hours interval.
- [ ] Capture actual position/fill/outcome records for walk-forward validation.

## Current disposition

**WATCH / manual research only; no candidate is certified READY by the currently available evidence.** The 198-row CSV historical gate is useful progress, but missing daily liquidity, catalyst, current chart and execution evidence remain hard blockers.

## [Uncertainties]

- The uploaded runtime ZIP's JSON fields have not been analyzed yet; WebSocket source selection and freshness remain unknown.
- Actual Schwab order eligibility and execution-window behavior require account/order evidence.
- The provisional score maps and target-touch approach still require historical out-of-sample validation.
