# CURRENT HANDOFF — 2026-11-09 (Overnight top-20, chart mode, TOS parity engine)

## Branch and operating rules
- Branch: `feature/opportunity-quality-classification`. Do not switch to master.
- Main Dashboard: `frontend/index.html`; do not use PrototypeIndex.
- Local repo: `C:\Users\jampo\source\repos\MomentumCommandCenterV2`.
- Preserve local uncommitted changes. API is decision-support-only; order submission is NOT IMPLEMENTED.
- Master checklist: [Master-To-Do.md](Master-To-Do.md). Detailed scanner procedure: [Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md](Overnight-Scanner-Formula-Audit-and-MCC-Workflow-20261109.md).

## Latest operator verification
- All eight Overnight Scanner columns now appear in the qualification table.
- Some HISTORY values are DATA? (missing custom-expression history); all PRICE and 20D AVG VOL values populated in the operator's verification. Keep history, price and liquidity evidence separate.
- Exclusions section works; operator requested hand cursor on disclosure toggle. Pointer/hover/focus affordance added.
- BAYRY stream subscription and live assessment return 200. Daily history still returns 502 with `Alpaca Daily response did not contain a bars array.`
- Top 4 results: BAYRY Daily ERROR; EWTX/ENOV/ARDT Daily 20/20 IEX_PARTIAL/FRESH. EWTX one unclassified article; ENOV/ARDT no article results remain UNKNOWN, not CLEAR.

## New implementation

### Daily data audit top 20
- On scanner CSV import or local restore, the Daily data audit input is populated with top 20 unique symbols by Overnight Score.
- Manual **Use Overnight Top 20** button added.
- Max 20 symbols; request concurrency capped at four. Filling the input does not auto-run the Daily audit; operator clicks Validate Daily data.
- Ranking uses imported score and preserves distinct symbols.

### Boring Overnight Main Chart
- Workspace selection persists in localStorage and is restored at the top of app.js before startup requests.
- Boring Overnight mode skips the live-assessment endpoint and dynamic 1m subscription.
- Main Chart requests historical 5m bars over the last five days, refreshes on symbol changes and at the existing visible-page interval.
- Intraday 5M permission and 1M execution status cards are hidden; status reads NOT USED rather than STALE_MARKET_DATA.
- The chart reports latest returned historical bar and source/feed. It does not represent a live executable quote.
- If the workspace was previously selected as Overnight, the page should restore it before initial SPY assessment; this specifically avoids the slow initial SPY live-assessment call in that mode.

### TOS daily-gap engine
- New `ThinkorswimDailyGapCalculator.cs`, version `MCC-TOS-DAILY-GAP-1.0`.
- Implements exact supplied formulas:
  - `r = daily open / prior daily close - 1`
  - win20 = mean of positive-return indicator over 20
  - avg20, median20, avg5
  - momentum = avg5 - avg20
  - current gap = latest return
  - aboveSMA = latest close > mean of latest 20 closes
  - score = win20*50 + avg20*1000 + median20*1000 + (avg5-avg20)*1000 + aboveSMA*10
- Requires 21 daily OHLC bars for 20 gap returns; missing history emits no calculated statistics.
- Append-only store: `MCC_TOS_DAILY_GAP_PATH` or LocalApplicationData/MomentumCommandCenterV2/tos-daily-gap-reference.jsonl.
- API:
  - POST `/api/overnight-scanner/thinkorswim-daily-bars`
  - GET `/api/overnight-scanner/thinkorswim-daily-bars?symbol=WRBY&limit=50`
- Dashboard import accepts CSV headers Date, Open, High, Low, Close; symbol, aggregation, extended-hours flag, adjustment flag and chart timezone are explicit metadata.
- Dashboard displays each of eight per-column comparisons to imported scanner values at display precision.
- TOS session/adjustment checkboxes default unchecked; operator must select actual TOS scanner settings. No Alpaca/IEX substitution is allowed.
- Tests added for eight outputs, score decomposition, insufficient history, duplicate dates and source metadata.
- Formula arithmetic is implemented; matched TOS exports and empirical symbol parity are still pending.

## Critical semantic boundary
The eight supplied scanner formulas are daily-bar gap metrics. They do not define the operator's exact buy time, sell/measurement endpoint, path/extrema, or auction/first-eligible-trade convention. A separate V3 trade-window Boring Overnight study must not be invented until those details are frozen. The earlier close-to-next-09:30-open history calculator remains a different research proxy and must not be relabelled as actual trade return.

## Outstanding issues and workstreams

### P0 — Data integrity / sell limit
- ATR EVR 5m parity MATCH; CBOE 5m parity MATCH, verified no-bar intervals explicit and no bars imputed.
- Revised ThinkScript compile, EVR 1m continuity, 119 exact provider/TOS 5m OHLCV+volume comparisons, exact previous RTH close: OPEN.
- Walk-forward runtime qualification BLOCKED on complete source-qualified 40-session dataset.
- SIP/GTC_EXT 40-session capture PARKED after known 403; do not retry unless entitlement changes.

### P1 — Market data / strategy
- RTO/EVR independent OHLCV parity failed on IEX; IEX is partial diagnostic only.
- Qualified independent Daily source OPEN; do not assume SIP.
- BAYRY Daily response payload diagnosis OPEN; inspect safe redacted JSON root/properties/bars type/count and provider correlation IDs.
- Catalyst human review infrastructure exists; current eligible dataset is zero. EWTX original article adjudication OPEN; no classifier calibration without eligible labels.
- Opportunity Quality historical calibration OPEN.
- TOS daily-gap formula engine DONE in code; empirical parity from matched TOS exports OPEN.
- V3 Boring Overnight execution-window metric/study OPEN, blocked on exact AZ local buy/sell endpoints and official auction/eligible-trade semantics.
- Boring Winner remains inactive.
- 5m/1m TOS authority and Arizona EOD verification OPEN.

### P1 — Dashboard / collector
- Top-20 Daily audit field auto-population and manual action implemented; operator runtime verification pending.
- Overnight chart mode bypass implemented; verify saved workspace restores and SPY live-assessment is not requested in Overnight mode.
- Full browser/CDP acceptance, full Dashboard acceptance, regular-session WebSocket delivery, collector recovery/idempotency and HAR timing OPEN.
- Collapsible exclusions pointer affordance implemented; verify runtime.

### P2/P3
- Broker fill reconciliation ledger OPEN.
- Schwab read-only OAuth blocked on developer approval/app setup.
- Provider entitlements/licensing/coverage and deployment hardening OPEN.
- Latest exact-head CI and operator local build/test/runtime acceptance OPEN.

## Operator sequence
From repository root:
```powershell
git status --short --branch
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
.\scripts\Get-Latest-And-Test.ps1
```
Do not reset or discard local changes. Restart API using the tested build.

Then:
1. Re-import the scanner CSV. Verify Daily data audit input fills with the top 20 unique tickers by score. Confirm the Use Overnight Top 20 button works.
2. Click Validate Daily data. Inspect each row; IEX remains diagnostic only.
3. Select Boring Overnight mode, refresh page, and confirm it restores. Inspect Network: there should be no `/api/market-data/live-assessment?symbol=SPY` request while in Overnight mode. Main Chart should call `/api/market-data/bars` with timeframe=5m and the selected symbol.
4. Change symbol to a candidate (e.g. WRBY). Confirm Main Chart title, data and symbol badge update; classification remains BORING OVERNIGHT · RESEARCH; intraday cards are hidden; no stale permission state is shown.
5. Expand/collapse EXCLUSIONS; cursor should be a hand over the summary heading/marker.
6. Export a TOS daily OHLC CSV with at least 21 rows for one candidate, headers Date/Open/High/Low/Close. Select the exact actual aggregation/session and corporate-action settings. Import via Thinkorswim Daily OHLC reference panel. Review all eight MATCH/MISMATCH results.
7. If mismatch occurs, retain the imported result and source CSV; do not change tolerances to force a match.
8. Capture BAYRY Daily response shape separately, redacting credentials.

## [Uncertainties]
- No actual matched TOS daily OHLC export was supplied in this turn, so empirical parity remains unverified.
- TOS scanner aggregation session/extended-hours semantics remain unconfirmed.
- Exact operator Arizona-local trade entry and sell/measurement endpoints remain unresolved; do not invent a separate execution study.
- Official auction versus first eligible trade data source is not evidenced.
- BAYRY raw upstream Daily response body is unavailable.
- CI and local runtime verification for this newest code set have not yet been established.
