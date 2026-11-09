# CURRENT HANDOFF — 2026-11-09 (Overnight scanner formula integration)

## Current branch

- Branch: `feature/opportunity-quality-classification`
- Starting branch head when this work began: `dc9fee0b84f3a8b4e8955203466d779b46e17c2c` (operator's formula upload).
- Latest commits from this work are listed in the current Git history; query branch head before making further changes.
- Main Dashboard: `frontend/index.html` at `http://localhost:53360/`.
- Do not switch to master; do not use PrototypeIndex.

## Latest operator evidence

### BAYRY / Top 4
- Stream subscribe: 200 OK.
- Live assessment: 200 OK on initial load and after CSV import / Red Zone cleared.
- Daily history endpoint: 502 after CSV import and after Red Zone cleared.
- UI error: `Alpaca Daily response did not contain a bars array.`
- This isolates the reported issue to the Daily response contract/parsing path, not stream subscription or live assessment. A successful HTTP status with unexpected JSON shape is not retried by the transient HTTP retry.
- Top 4 evidence:
  - BAYRY: Daily ERROR / UNKNOWN; catalyst UNKNOWN; zero articles; Daily shape error.
  - EWTX: Daily complete 20/20, IEX_PARTIAL/FRESH; one UNCLASSIFIED_NEWS_REVIEW article.
  - ENOV: Daily complete 20/20, IEX_PARTIAL/FRESH; zero articles, catalyst UNKNOWN.
  - ARDT: Daily complete 20/20, IEX_PARTIAL/FRESH; zero articles, catalyst UNKNOWN.
- No-news remains UNKNOWN, never CLEAR. IEX partial feed does not qualify consolidated liquidity/price gates.

### Overnight Scanner import / screenshot
- Import: 495 rows; 185 PASS / 14 FAIL / 296 DATA?; exclusions 56 loaded / 0 matches.
- Missing custom-expression subscription history is DATA?, not historical FAIL.
- Screenshot shows top 50 of 419 sorted by Overnight Score. Formula audit lists the top 15 and explains ranking. It is a static scanner snapshot, not live quote/catalyst/entry evidence.
- BAYRY is rank 13 in the screenshot, but its MCC Daily endpoint error remains independent of the Thinkorswim scanner values.

## Formula source audit

Operator uploaded the original formula text files under `docs/Column Formulas`. All eight active formulas are now documented in [Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md](Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md).

Shared return:
`r = daily open / prior daily close - 1`

Columns:
1. ON WIN % = 20-sample average of positive-return indicator × 100.
2. AVG ON % = 20-sample mean return × 100.
3. Median ON % = 20-sample median return × 100.
4. 5D ON % = 5-sample mean return × 100.
5. ON Momentum = (5-sample mean − 20-sample mean) × 100.
6. Current ON % = current daily open / prior daily close − 1, × 100.
7. Above 20 SMA = current daily close > 20-sample simple average close.
8. Overnight Score = win20×50 + avg20×1000 + median20×1000 + (avg5−avg20)×1000 + aboveSMA×10.

The score is a ranking heuristic, not a probability. The 5D ON % display is not separately added; its value enters via momentum. The inactive Boring Winner formula is documented but not activated.

Important semantic gap: the code says daily `open` and prior daily `close`; it does not define the daily bar's extended-hours session boundary. It cannot by itself prove the operator's intended buy-at-time/sell endpoint or the whole AH Day 1 + PM Day 2 path. Verify TOS aggregation settings and match exported bars before implementing a substitute. Do not assume this is the exact 7:55 PM Arizona execution return.

## Implemented in this update

1. [x] Dashboard EXCLUSIONS · API-BACKED panel is wrapped in a native collapsible section. The Refresh button, add form, status and list remain inside it.
2. [x] Overnight qualification table now displays all eight source metrics: ON Win, 20D average, Median, 5D average, Momentum, Current ON, Above SMA, Overnight Score.
3. [x] Historical status requires all eight fields to be present; missing median/momentum/score keeps the row DATA?.
4. [x] Formula audit and operator walkthrough document created.
5. [x] Master To-Do updated with new code and runtime evidence.
6. [x] Existing transient Daily retries remain in code, but they do not address the current malformed/alternate successful-response shape.
7. [ ] Latest branch-head CI and local runtime verification still required. Do not claim these changes are runtime accepted.

## Exact MCC workflow for a candidate

1. Import the Overnight Scanner CSV into the main Dashboard.
2. Confirm import row count and counts of PASS / FAIL / DATA?. Keep DATA? separate from FAIL.
3. Expand EXCLUSIONS only when needed; compare MCC's exclusion list against the TOS personal watchlist manually.
4. Select MCC Top 4 (highest imported score, unique tickers) or My Top 4 (manual four-symbol group).
5. Click Fetch Daily + catalyst. Inspect the separate four-symbol evidence table; do not infer that missing article cards mean a clean symbol.
6. Require complete, fresh, consolidated SIP Daily history before price/SMA/ADV qualification. IEX is partial diagnostic coverage only.
7. Open each returned catalyst article and apply the six-label evidence workflow; unknown coverage stays unknown.
8. Read each candidate row's eight metrics, history, exclusion, price, 20D average volume, catalyst coverage and decision.
9. For finalists, manually inspect actual quote/spread, extended-hours liquidity, news and the operator's intended Arizona-local entry/sell process. The current score does not include those.
10. Treat historical PASS as a data/threshold result, not a trade instruction. MCC is decision-support-only; order submission is NOT IMPLEMENTED.

## All-workstream status snapshot

### P0 — Data integrity / sell-limit
- ATR V2 import, Wilder implementation, gap diagnostics and exact confirmed no-bar handling: DONE.
- EVR 5m parity: MATCH (327 observations, 20 compared, 0 unresolved gaps, 2 verified no-bar intervals).
- CBOE 5m parity: MATCH (338 observations, 20 compared, 0 unresolved gaps, 1 verified no-bar interval).
- Revised ThinkScript compile: OPEN.
- EVR 1m continuity: OPEN.
- 119 exact provider/TOS 5m OHLCV matches including volume: OPEN.
- Exact previous RTH close from same qualified source: OPEN.
- Walk-forward code exists; runtime qualification BLOCKED pending complete source-qualified 40-session dataset.
- SIP/GTC_EXT 40-session capture: PARKED after established 403 entitlement denial. Do not retry unless operator independently reports changed entitlement.

### P1 — Market data / strategy
- RTO/EVR independent OHLCV parity: FAILED on IEX; need independent entitled source, not another unchanged IEX run.
- Daily 20-session API and audit UI: implemented; IEX remains partial and unqualified.
- BAYRY Daily: OPEN; diagnose 200-response JSON shape/contract.
- Catalyst review store, six-label UI, validation and fail-closed evidence guards: implemented. Current sample: 0 eligible; no metrics.
- EWTX old sale-exploration article: source-backed adjudication OPEN. Don't reuse current investor-conference article's metadata.
- Opportunity Quality ranking code exists; historical calibration and representative outcome data OPEN.
- Overnight formula audit: DONE.
- All-eight-column Dashboard display and DATA? completeness gate: implemented, runtime pending.
- Exact TOS daily aggregation session semantics: OPEN.
- Official auction print vs first eligible trade: OPEN; needs authoritative entitled data.
- Exact operator Overnight buy/sell endpoints and historical path/extrema: OPEN.
- Separate V3 Boring Overnight TOS column/study: OPEN; no compile/parity proof.
- Boring Winner: inactive; no promotion.
- TOS 5m/1m authority and Arizona EOD verification: operator evidence OPEN.

### P1 — Dashboard / browser / collector
- Main Dashboard only: DONE.
- Four-symbol Top 4 evidence table: implemented; BAYRY result currently ERROR; runtime follow-up OPEN.
- Collapsible EXCLUSIONS section: implemented, runtime pending.
- Full browser/CDP acceptance: OPEN.
- Full Dashboard functional acceptance: OPEN.
- Regular-session WebSocket/collector proof: OPEN.
- Restart/sleep/wake/recovery/idempotency exercise: OPEN.
- HAR performance measurement: OPEN.

### P2 — integrations / deployment
- Broker fill reconciliation ledger: OPEN.
- Schwab read-only OAuth integration: blocked on external developer approval/app setup.
- Provider licensing, entitlement, coverage and retention assessment: OPEN.
- Packaging, backup/restore, log rotation, health monitoring and unattended recovery: OPEN.
- Order submission: NOT IMPLEMENTED by design.

### P3 — release governance
- Feature-branch workflow and technical runbook: DONE.
- Latest branch-head Release CI: VERIFY.
- Local pull/build/test/restart and runtime acceptance: OPEN.
- Keep Master To-Do and handoff synchronized: DONE for this update.

## Next operator steps

Run from `C:\Users\jampo\source\repos\MomentumCommandCenterV2`:

```powershell
git status --short --branch
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
.\scripts\Get-Latest-And-Test.ps1
```

Do not reset or discard local changes. Restart the API from the tested build.

Then:
1. Re-import the same 495-row Overnight Scanner CSV.
2. Verify the table shows Median ON %, ON Momentum and Overnight Score alongside the other five metrics.
3. Verify rows missing any of the eight formula fields show DATA?, not PASS/FAIL.
4. Expand/collapse EXCLUSIONS and verify Refresh/Add/Remove still work.
5. Fetch Daily + catalyst for MCC Top 4. Confirm four distinct symbols in the evidence table.
6. For BAYRY, capture the raw HTTP response body from the Alpaca Daily request via server-side safe diagnostics (redact keys). Record root JSON type, property names, bars property type/count, HTTP status and Alpaca request/correlation headers if present. Do not share API credentials. The current exception text is insufficient to identify the upstream contract variant.
7. Continue the separate TOS tasks only after verifying the daily aggregation settings against exported bars.

## [Uncertainties]

- Exact TOS daily bar extended-hours semantics remain unverified. Formulas alone do not settle the session boundary.
- Data not found in sources for the full 20-session per-symbol raw returns, complete scanner CSV, live spreads/RVOL and independent outcome labels.
- BAYRY's successful upstream payload body is not present in the supplied evidence, so its exact shape and root cause remain unknown.
- No official auction/eligible-trade feed or compiled V3 TOS study has been evidenced.
