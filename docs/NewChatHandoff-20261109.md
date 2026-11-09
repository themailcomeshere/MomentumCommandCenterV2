# CURRENT HANDOFF — 2026-11-09 (Overnight priorities + Daily/Top-4 reliability)

## Latest operator evidence

- Imported scanner: 495 rows; historical gates 185 PASS / 14 FAIL / 296 DATA?; exclusions 56 loaded / 0 matches. Custom-expression subscription-limit/missing history is DATA?, not FAIL.
- MCC Top 4 did query four symbols. Only EWTX appears in the article cards. Cards are returned-article-driven; the new per-symbol evidence table is intended to show all four query outcomes independently.
- BAYRY Daily API returned HTTP 502 at `https://localhost:53359/api/market-data/daily?symbol=BAYRY`.
- Catalyst validation: raw 7, eligible 0, excluded 7, conflicts 0, non-evaluable 2, superseded 3, inadequateReviewEvidence 2. Metrics are unavailable and must remain withheld.
- EWTX assessment for the current 24h window returned article 117314249 (investor conferences), ruleset `MCC-CATALYST-TRIAGE-1.0`. This does not establish the saved ruleset or source content for older sale-exploration article 117008554.

## New implementation on branch

- Dashboard now has a distinct four-symbol live evidence table: Daily result/feed/completeness/error, catalyst result/article count/coverage/error. Human-review article cards remain article-only; no-news remains UNKNOWN.
- Alpaca Daily client retries transient network/timeouts, 429 and 5xx up to three attempts with bounded delay/Retry-After. Request is cloned per attempt. Non-transient 4xx is not retried.
- Daily API preserves provider status classes: 422 for unsupported symbol/request, 503 for rate limit, 502 for upstream gateway/server failure.
- Regression tests cover transient 502 recovery and non-transient 422 no-retry.
- Latest code-bearing commit `59719e6263e7e51a3eafaacf86a0507817f3e2ef` passed Release build/tests: [CI run 42659428224](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42659428224). Documentation commits after that code commit were still running CI; check exact current branch head.

## Overnight truth audit

The current `capture-history` / `BoringOvernightHistoryCalculator` uses exact prior regular-session close and next-session 09:30 one-minute bar Open. That net endpoint interval spans Day 1 after-hours plus Day 2 premarket, but it does NOT calculate a buy at the operator's actual buy-time, full path, intraperiod highs/lows, or sell-window target touch. The 09:30 Open is explicitly `ONE_MINUTE_BAR_OPEN_PROXY`, `officialOpeningAuctionVerified=false`.

The calendar is `MCC_US_EQUITIES_CALENDAR_V2`, a maintained ruleset, not a live official exchange calendar. No official auction feed or source-backed eligible-trade comparison has been established. Do not call either task complete.

The exact operator-local buy-time and sell/measurement endpoint must be frozen against the original Thinkorswim Overnight scanner formula and the operator's confirmed order workflow before changing the history calculation. Do not substitute a close/open proxy and relabel it as the strategy's actual trade return.

## Workstream status / order

### Priority P1 — Market data and strategy

- RTO/EVR independent OHLCV parity: FAILED on IEX; needs genuinely independent entitled reference source.
- Qualified independent Daily source: OPEN. IEX is not consolidated; SIP access/source equivalence not qualified. BAYRY 502 now has retry handling; operator must retest.
- Catalyst representative dataset/reviewer agreement: OPEN. No fabricated labels. Current 0 eligible is correct.
- Opportunity Quality historical calibration: OPEN. Need point-in-time snapshots joined to independently observed outcomes; no qualifying labelled dataset in sources.
- Official auction vs first eligible trade: OPEN; requires authoritative source with auction/eligible-trade semantics.
- Exact Overnight endpoints: OPEN; freeze actual local buy time and sell/measurement point against source formula before implementation.
- Separate V3 Boring Overnight column: OPEN; no TOS compile evidence. Existing Boring Overnight C# evaluator is not the TOS watchlist column.
- Thinkorswim 5M/1M authority/EOD: source architecture exists; installed study hash/input/bar parity and exact Arizona EOD boundary still need operator evidence.

### Priority P0 — Data integrity/sell limit

- Revised ThinkScript compile: OPEN.
- EVR 1m continuity: OPEN.
- 119-bar provider/TOS OHLCV equality: OPEN.
- Exact previous RTH close: OPEN.
- Runtime walk-forward: BLOCKED by complete source-qualified 40-session requirements.
- SIP GTC_EXT 40-session dataset: PARKED; previously confirmed 403. Do not retry without operator evidence of entitlement change.

### Priority P1 — Dashboard/browser/collector

- Four-symbol evidence table: IMPLEMENTED, runtime pending.
- Full browser/CDP acceptance: OPEN.
- Full Dashboard acceptance: OPEN.
- Regular-session WebSocket delivery: OPEN.
- Collector recovery: OPEN.
- Network/HAR performance: OPEN.

## Operator next steps

1. Pull/build/test latest feature branch, restart API.
2. Repeat MCC Top 4 fetch. Confirm four distinct symbols in the evidence table. Capture BAYRY's final Daily error/result after retries; do not hide persistent 502.
3. Keep 296 DATA? historical rows out of FAIL counts; inspect the scanner CSV History cells/source fields separately.
4. Do not adjudicate EWTX sale-exploration article based on the conference article. Retrieve the original article 117008554 and saved prediction metadata. Correct active weak reviews only with article-specific evidence, superseding both weak active records.
5. Provide/confirm the exact local buy-time and sell/measurement endpoint from the original Overnight scanner formula if not already represented in source; then implement a dedicated strategy-return calculator separate from the existing close/open net-return proxy.
6. Compile and verify the V3 TOS column and 5M/1M studies in Thinkorswim. Code generation is not compile evidence.
7. Only pursue independent source qualification if provider entitlement/licensing allows exact same-symbol/session/adjustment comparisons. Do not retry SIP absent entitlement change.
8. For collector proof, capture during regular session; after-hours/no-session tests cannot prove WebSocket delivery.

## Standard local update/build/test

```powershell
git status --short --branch
git switch feature/opportunity-quality-classification
git pull --ff-only origin feature/opportunity-quality-classification
.\scripts\Get-Latest-And-Test.ps1
```

Do not reset or discard local changes.

## [Uncertainties]

- Data not found in sources for an authoritative opening-auction print / first eligible trade dataset.
- Exact operator buy/sell endpoint semantics need confirmation against the original scanner source and actual order workflow before changing production calculations.
- Current BAYRY 502's upstream cause is not known; retry behavior is implemented but runtime outcome is pending.
- No eligible human catalyst labels or time-separated Opportunity Quality outcomes exist for statistical qualification.
- TOS compilation and all browser/collector operator evidence remain unverified.
