# Monday Operating Readiness — 2026-11-02 (Arizona)

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Branch:** `feature/opportunity-quality-classification`  
**Operating mode:** read-only decision support; no order submission.

## Executive decision

| Workflow | Monday status | Permitted use |
|---|---|---|
| Red Zone / V2 5M score | **READY FOR LIVE OBSERVATION, subject to Monday runtime checks** | Display the independent 0–6 V2 5M score, six criteria, freshness, and 5M permission. 5+ means 5M permission only; it is not a 1M entry or an order instruction. |
| 1M execution | **CONDITIONAL** | Use only when the completed 1M history and freshness checks pass, and the combined V2 decision reports its own execution state. REST fallback remains allowed; WebSocket provenance is used only when its gates pass. |
| Boring Overnight | **NOT READY for automated or READY qualification** | Manual watchlist/research only. Missing history, complete eligible-window coverage, catalyst clearance, and validated session policy must remain DATA? / WATCH / BLOCKED. Zero position is valid. |

The Red Zone 5M score was previously coupled to the combined 1M/5M decision object. It is now calculated independently from the six existing V2 5M conditions, using the same canonical score function as the V2 engine. This prevents missing/stale 1M execution data from hiding a valid current 5M score. The score is still withheld when the 5M technical snapshot is absent/stale or the same-clock-slot RVOL baseline is unavailable.

**Scope clarification:** this is readiness of the independent V2 5M score for live observation only. It does not mean the Red Zone automatic excitement rank, Manual/Automatic toggle, low-volume explanation/penalty, or four-stock dynamic prioritization is implemented. Current Red Zone ordering is not volume-aware; see the canonical Red Zone requirements in `docs/Master-To-Do.md` and `docs/Opportunity-Quality-and-Ranking.md`.

## 1. Daylight-saving conversion

US daylight saving time ends Sunday, November 1, 2026. Phoenix remains UTC−07:00; Eastern becomes UTC−05:00. Phoenix is therefore two hours behind Eastern from Monday, November 2 until the spring change.

| Event | Phoenix / Arizona | Eastern |
|---|---:|---:|
| Regular-session open | 07:30 | 09:30 |
| User's 08:30 start | 08:30 | 10:30 |
| Regular-session close | 14:00 | 16:00 |
| GTC_EXT window described in project notes (07:00–20:00 ET) | 05:00–18:00 | 07:00–20:00 |

Use `America/Phoenix` for local display and `America/New_York` for exchange-session conversion. Do not hardcode a fixed UTC offset for Eastern. Persist and exchange timestamps as UTC. Source: NIST DST rules and Arizona exemption.

## 2. Monday preflight

Before the regular open:

1. Pull the latest branch and verify GitHub Actions is green for the exact HEAD. The latest score implementation is not release-verified until that check passes.
2. Start the API using the Development profile. Local URLs: `http://localhost:53360` or `https://localhost:53359`; do not mix scheme and port.
3. Check `GET /api/health`, `GET /api/providers/status`, `GET /api/market-data/collector/status`, and `GET /api/market-data/stream/status`.
4. Hard-refresh the browser. Confirm no MCC application JavaScript errors. The remaining `browserLink:33 $.noConflict` console error is Visual Studio Browser Link injection, not an MCC source file; disable Browser Link for the debug session if it remains.
5. Confirm the dashboard's displayed timestamps are Arizona-local and the source timestamps remain UTC.

## 3. Red Zone / V2 5M score — how to use it Monday

The score is the existing six-condition V2 5M authority score:

| Condition | Pass rule |
|---|---|
| 1 | 5M close > session VWAP |
| 2 | 5M close > EMA 9 |
| 3 | EMA 9 > EMA 20 |
| 4 | EMA 20 > SMA 50 |
| 5 | 5M RSI >= 50 |
| 6 | Same-regular-session-clock-slot 5M RVOL >= 1.50 |

- **5–6/6:** 5M long Permission threshold is met, if the score status is CURRENT.
- **3–4/6:** partial alignment; Permission is not met.
- **0–2/6:** low alignment.
- Any unavailable criterion or stale/inadequate 5M source means score UNKNOWN, not zero and not a pass.
- RVOL is a same-time-of-day 5M baseline, not a vendor-provided consolidated RVOL field. The implementation requires at least five prior-session samples for the slot.
- 5M Permission remains REST-backed.
- 1M execution is a separate gate. Even a 6/6 score does not mean EXECUTION READY. The UI may only say EXECUTION READY when the combined V2 decision's `entryAllowed=true`.
- A 5M score is current only when there are at least 50 completed 5M bars, a fresh latest completed 5M bar (<=360 seconds), and a valid same-slot RVOL value. Combined V2 decision evaluation has additional 1M/previous-5M requirements.
- 1M freshness limit is 120 seconds. WebSocket 1M is selected only with acknowledged subscription, at least 50 completed persisted WebSocket bars, correct feed, and fresh latest bar; otherwise the service uses REST fallback.
- Do not weaken freshness or history thresholds to force a score.

### Monday runtime acceptance

After 07:30 Arizona and during the regular session:

1. Select SPY first to establish a liquid reference, then check RKLX, ASTX, and XRPN.
2. Capture each full `GET /api/market-data/live-assessment?symbol=...` response.
3. Confirm `fiveMinute.scoreBreakdown.status=CURRENT`, `score` is 0–6, all six criteria are true/false (not null), and `permission` agrees with score >=5.
4. Confirm the five-minute source timestamp is recent and the feed is the intended Alpaca feed.
5. Confirm `oneMinute` freshness and history separately. If WebSocket is selected, require `sourceTransport=ALPACA_WEBSOCKET_LATEST_BAR`; otherwise REST fallback must be clearly identified.
6. Confirm `decision.entryAllowed` is not inferred from 5M score. It must come from the full combined engine.
7. Preserve JSON, symbol, local test time, UTC source timestamps, branch SHA, and browser screenshot.

The user's Saturday 2026-10-31 capture showed all four WebSocket subscriptions acknowledged but zero per-symbol bars and zero new bars. Because it was Saturday, this is neither a pass nor a fail for regular-session delivery. Repeat during market hours.

## 4. Boring Overnight — current launch blockers

Boring Overnight is **not ready for actionable READY qualification** on Monday. The evaluator and audit endpoints exist, but they do not yet constitute a live, source-validated qualification pipeline.

Current hard blockers:

1. **Historical edge/boringness:** no verified, complete, consistently paired 20-session dataset with the required close/open boundary observations has been accepted for the Monday candidate universe. The current history calculator requires explicit caller-supplied boundary timestamps and fails closed when a pair is missing.
2. **Session policy:** regular close-to-next-open history and the user's GTC_EXT 07:00–20:00 ET order-eligibility/target-touch window are different calculations. The current calculator does not validate full GTC_EXT window coverage or order fills. Do not conflate overnight return with target-touch/fill probability.
3. **Catalysts:** no connected, validated catalyst provider is used by the history-backed evaluator. `/api/boring-overnight/evaluate-with-history` deliberately forces `catalystStatus=UNKNOWN`. Unknown is not clear.
4. **Other qualification inputs:** daily volume, price/SMA, current-day heat, 5M state, and 1M state require source/timestamp validation. Caller-supplied fields are not independently verified.
5. **Scanner integration:** the Red Zone/Morning Brief preview is not a validated Boring Overnight ranked universe. Do not promote a preview score into overnight qualification.

### Allowed Monday workflow

- Keep candidate names in WATCH / manual research only.
- For each candidate, verify price >$5, 20-day average daily volume >=500,000, above 20-day SMA, 20D overnight win rate >=60%, 20D mean >=+0.10%, and unrounded 5D mean strictly greater than 20D mean.
- Verify the maintained Schwab exclusion list.
- Verify current earnings, corporate actions, material news, regulatory/government events, M&A, and sector/regime risks. If not affirmatively sourced, record UNKNOWN.
- Review current 5M permission/structure and 1M state from live assessment; GET OUT / SELL / BLOCK blocks a fresh overnight entry.
- Record exact data source, feed, source timestamps, session boundaries, and missing intervals.
- If any critical input is missing/stale, the result is DATA?; if a hard gate fails, BLOCKED; if heat/caution is present, WATCH. Do not force READY to fill a list.
- No automated order placement, cancel/replace, or position handling.

### What is required to make it launch-ready

- [ ] Choose and approve the precise session definitions: regular close/open, GTC_EXT window, ETH/RTH inclusion, holidays/early closes, and bar timestamp semantics.
- [ ] Obtain source-backed daily bars for 20D SMA/average volume and complete 1M history for each close/open boundary.
- [ ] Validate at least 20 complete overnight pairs per symbol, with missing pairs retained as DATA_GAP.
- [ ] Validate provider/feed entitlement and complete GTC_EXT coverage for any sell-target research separately.
- [ ] Connect and validate catalyst/news coverage with freshness and source links; UNKNOWN remains blocking.
- [ ] Reconcile the complete maintained Schwab exclusion sources.
- [ ] Validate the six-condition 5M state against installed Thinkorswim study diagnostics, not OHLCV alone.
- [ ] Add a reproducible scanner candidate universe and persistent decision snapshots.
- [ ] Shadow-test winners, losers, catalyst spikes, and no-trade outcomes; measure false positives and missed candidates before changing weights.
- [ ] Complete manual/browser acceptance and exact-HEAD CI.

## 5. User-supplied validation evidence — 2026-10-31

Market-data validation script, second run:

- Requested RKLX / Alpaca / IEX from 2026-10-30 13:00Z to 19:35Z.
- Stored counts reported: 25 1M and 23 5M.
- Automated idempotency comparison: 1M=true and 5M=true. This means every provider-returned bar compared by the script had an exact stored match on timestamp and bar/provenance content.
- Artifact folder: `artifacts/runtime-validation/20261031T202332Z`.
- WebSocket: SPY/RKLX/ASTX/XRPN all acknowledged, but each had barCount 0 before and after, with zero new bars. Test was Saturday, outside regular hours; regular-session delivery remains untested.
- Collector checkpoint capture folder: `artifacts/collector-checkpoint/20261031T202543Z`.
- Checkpoint copied from `C:\Users\jampo\AppData\Local\MomentumCommandCenterV2\collector-watermarks.json`.
- SHA-256: `8E3AFBD5DAD5C3502DB625F0133DE697F9C8615BB35BDA09609A4CF7FAE44500`.
- Capture status: 3 cycles, 0 last-cycle errors. This is a baseline capture, not a controlled failure/recovery or restart/reload test.

## 6. Release boundaries

MCC remains read-only decision support. A 5M score is not a probability, profitability estimate, buy instruction, or order. Boring Overnight remains non-launch-ready until the evidence gates above pass. Preserve UNKNOWN/DATA? for absent or stale evidence.

## [Uncertainties]

- The JSON artifacts in the user's local output directories were not uploaded into this chat; this record relies on the console summary. Individual source timestamps should be checked from those files if an audit requires them.
- Monday provider feed coverage and WebSocket delivery cannot be established before the live session.
- Boring Overnight's exact final session policy still requires operator confirmation; no automatic boundary generator has been enabled.
