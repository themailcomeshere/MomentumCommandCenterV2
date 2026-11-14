# Boring Overnight READY semantics — decision record

**Created:** 2026-11-14. **Status:** all operator questions answered on 2026-11-14. Two items carry Claude's recommendation pending a final operator yes: MATERIAL_EVENT blocking, and shadow mode before the live signal. READY is not wired yet; this record is its specification.

## Frozen by the operator (2026-11-14)

| # | Decision | Exact meaning | Already in code? |
|---|---|---|---|
| 1 | **Buy time: any time in the final regular hour of Day 1** | From regular close − 60 min to the regular close, America/New_York. Normal day: 15:00–16:00 ET, which is **13:00–14:00 Arizona in standard time (Nov–Mar)** and 12:00–13:00 Arizona in daylight time. On an early close, the hour before the early close. | Yes. `MarketSessionClock` uses `buyClose.AddHours(-1)` → `buyClose` (`MarketSessionClock.cs:57-61`), including early closes. |
| 2 | **Exit: GTC_EXT limit sell, measured on Day 2, 07:00–20:00 ET** | A limit order working through the extended window: **05:00–18:00 Arizona in standard time**, 04:00–17:00 in daylight time. Research success = the window's high reaches the limit. **A touch is not a fill.** | Yes: sell window, 40-window SIP history (`e48b4d2`, `0b4fbc1`), walk-forward gate. |
| 3 | **Official opening-auction print: not required** | The 09:30 close/open capture (`ONE_MINUTE_BAR_OPEN_PROXY`) stays a separately named research series, never the strategy endpoint. | Already labelled a proxy. |
| 4 | **Historical SIP minute bars: allowed** | Completed GTC_EXT windows on SIP, request end ≥ 16 min old. | `e48b4d2` |
| 5 | **NO_TRADE minutes: accepted with safeguards** | On SIP, a minute with no bar is counted, never priced. This applies only when the reference close and regular-session bars are present; otherwise the minute stays MISSING. | `0b4fbc1`; RTO 40/40 |
| 6 | **Unfilled limit at 20:00 ET: the GTC_EXT order stays working** | The order is Good-Till-Cancelled, so it carries into Day 3 and later sessions. **Measurement stays Day 2 only:** a Day 2 touch counts as success, and no touch on Day 2 counts as a miss. Days 3+ are not modelled. A carried order is a multi-day position, outside the overnight edge MCC measures. | No change needed. |
| 7 | **Catalyst: an operator attestation counts**, with the rules below | Zero news stays UNKNOWN and is never relabelled CLEAR. READY may pass the catalyst gate on a recorded attestation. | Not built (READY wiring). |
| 8 | **Daily parity tolerance: ±$0.01 per OHLC price, ±0.5% volume** | Data-quality check between two sources, not a trading parameter. | `DailySourceCrossCheck` defaults and `Test-DailyMarketDataQualification.ps1` (`-PriceTolerance`, `-VolumeRelativeTolerance`). |
| 9 | **READY is a clear go signal** | When every gate passes inside the buy window, MCC shows one unambiguous **READY — BUY WINDOW OPEN** state for that symbol, with the calculated limit sell. Position size stays the operator's decision. MCC never submits orders. | Not built. |

### Clarification on decision 6

MCC has two separate measurements:
- **Close → next 09:30 open** (the 20-pair capture-history). This is a research series about the overnight gap. It is not your exit.
- **Day 2, 07:00–20:00 ET sell window.** This is what the calculated limit sell is tested on: did the price reach the limit at any time from 07:00 to 20:00 ET on Day 2? It covers premarket, regular session and after-hours, not just the open.

### Catalyst gate rules (decision 7)

| Situation | Result |
|---|---|
| Triage `RISK_PRESENT`, or any article human-labelled `MATERIAL_RISK` | **BLOCKED.** An attestation cannot override it. |
| Any article human-labelled `MATERIAL_EVENT` (earnings, guidance, M&A, FDA, major contract) | **BLOCKED** (Claude's recommendation: a known event makes the overnight not "boring"). *Operator to confirm.* |
| Articles only `NON_MATERIAL` / `IRRELEVANT` / `DUPLICATE` after human review, or zero articles (UNKNOWN) | **Passes only with an operator attestation**, recorded as `CATALYST: OPERATOR-ATTESTED`, never `CLEAR`. |
| Any article still unreviewed (`UNCLASSIFIED` / `MATERIAL` triage without a human label) | **Not ready.** Review the article first. |

An attestation must:
- name the symbol and buy session;
- include a note of at least 40 characters listing the sources checked (for example "Checked Schwab news, SEC EDGAR filings, company IR page — no news since last close");
- be timestamped, append-only, and expire at that session's buy deadline.

### Daily tolerance evidence (decision 8)

The tolerance has to cover rounding (reference sites publish 2 decimals) and small vendor volume differences (late-reported trades). It must still catch a wrong source or wrong session. A 10% tolerance would hide real data problems; a missing day or a wrong day typically differs by whole percent.

Measured 2026-11-14 (`artifacts/daily-data-qualification/20261114T195621Z-sip-delayed/`): SIP Daily against a fresh download of the independent reference (S&P Global via Stock Analysis), RTO and EVR, 2026-11-03 … 11-13, 18 sessions:
- O/H/L/C within $0.005;
- volume within 0.021%.

The earlier 2026-11-06 close differences are gone: that older reference row had been captured before the final close was published. All 18 pass at ±$0.01 / ±0.5%.

### READY signal (decision 9), recommended rollout (*operator to confirm*)

1. **Shadow mode first.** MCC computes READY every session and logs it with all gate evidence. It shows the signal as `READY (SHADOW — not yet validated)`, with no sound. Next-day outcomes (limit touched on Day 2 or not) are recorded automatically from SIP history.
2. **Go live** after 20 shadow sessions with outcomes reviewed: the signal becomes `READY — BUY WINDOW OPEN` with the sound alert.

## READY gate list (all must pass, evaluated inside the buy window)

1. Daily qualification usable: consolidated SIP, 20/20 sessions, fresh (`DailyFeed=sip_delayed`).
2. Scanner and history thresholds as already specified (5D > 20D mean, ADV20 ≥ 500K, price/SMA rules).
3. Exclusion watchlist not matched.
4. Catalyst gate per decision 7.
5. Inside the buy window (decision 1), with fresh intraday data.
6. Calculated limit sell issued: 40/40 SIP history, walk-forward touch-rate and adverse-excursion gates, and TOS ATR parity gates. If no limit can be issued, the result is **not READY**. It shows the failing gate; it never shows a guessed limit.

Boring Winner PASS is never READY. MCC never submits an order.

## Engineering defaults (unchanged)

- **Research entry reference:** the regular-session close (last regular 1m bar), labelled `ENTRY_PROXY_REGULAR_CLOSE`, plus the final-hour high as a conservative worst case. Neither is a fill price.
- **Live entry:** an operator-recorded actual buy replaces the proxy for that trade.
