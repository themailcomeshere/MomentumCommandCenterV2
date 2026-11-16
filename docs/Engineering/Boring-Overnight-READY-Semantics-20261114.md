# Boring Overnight READY semantics — decision record

**Created:** 2026-11-14. **Status:** all decisions made 2026-11-14 and **built** (`4eafc9b`, `8365f7e`, `02e88a3`), running in shadow mode. The IEX close/open history gap found while building was fixed with delayed SIP (`6fe6446`).

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

### Catalyst gate rules (decision 7, revised 2026-11-14 on Claude's recommendation and built in `8365f7e`)

| Situation | Result |
|---|---|
| Any article human-labelled `MATERIAL_RISK`, or an **unreviewed** article with a triage risk phrase | **BLOCKED.** A sign-off cannot override it. A `NON_MATERIAL` / `IRRELEVANT` review lifts a risk-phrase article. |
| Human `MATERIAL_EVENT` that is **earnings/guidance or FDA/clinical**, or whose event type is unknown | **BLOCKED.** These are scheduled overnight events. |
| Human `MATERIAL_EVENT` that is **M&A, a contract, or an analyst action** | **Allowed with sign-off**; the card shows a yellow EVENT warning with the headline. |
| Any article unreviewed (other than risk phrase) or labelled `UNCERTAIN` | **Not ready.** Review it first. |
| Only `NON_MATERIAL` / `IRRELEVANT` / `DUPLICATE`, or zero news (UNKNOWN) | **Passes only with today's sign-off**, shown as `CATALYST: OPERATOR-ATTESTED`, never `CLEAR`. |

**Sign-off:** tick at least one source you checked: Schwab news, SEC EDGAR, company IR, or Other (Other needs a note). The note is otherwise optional. It is accepted only on the buy-session day, expires at the buy deadline, and is append-only (`catalyst-attestations.jsonl`).
### Daily tolerance evidence (decision 8)

The tolerance has to cover rounding (reference sites publish 2 decimals) and small vendor volume differences (late-reported trades). It must still catch a wrong source or wrong session. A 10% tolerance would hide real data problems; a missing day or a wrong day typically differs by whole percent.

Measured 2026-11-14 (`artifacts/daily-data-qualification/20261114T195621Z-sip-delayed/`): SIP Daily against a fresh download of the independent reference (S&P Global via Stock Analysis), RTO and EVR, 2026-11-03 … 11-13, 18 sessions:
- O/H/L/C within $0.005;
- volume within 0.021%.

The earlier 2026-11-06 close differences are gone: that older reference row had been captured before the final close was published. All 18 pass at ±$0.01 / ±0.5%.

### READY signal (decision 9, built)

- **Shadow mode, open-ended, minimum 10 sessions.** READY is computed and logged on every in-window check and shown as `READY (SHADOW — not yet validated)`, with no sound. Each signal's Day 2 outcome (limit touched or not on the SIP 07:00–20:00 ET window) is resolved automatically in **Shadow log and Day 2 outcomes**.
- **Going live:** set `BoringOvernight:ReadySignalMode` to `live` (for example `dotnet user-secrets set "BoringOvernight:ReadySignalMode" "live" --project src\MomentumCommandCenterV2.API`) and restart. MCC honours it only after **10 distinct buy sessions** are in the shadow log. Then the label is `READY — BUY WINDOW OPEN`, and a sound plays on a new READY if you clicked **Enable READY sound**.

## READY gate list (as built; all must pass inside the buy window)

1. **BUY_WINDOW:** final regular hour (`MarketSessionClock`).
2. **QUALIFICATION_SNAPSHOT:** a capture-history snapshot at most 10 minutes old. The READY panel runs the capture before each check.
3. **QUALIFICATION:** the existing qualification evaluator must return READY (not WATCH): price at least $5 (exactly $5.00 passes, decided 2026-11-16) and above SMA20, ADV20 ≥ 500K, overnight win rate 60–100%, 20D average ≥ +0.10%, 5D > 20D, 5M permission YES, 5M structure BULLISH, 1M state not SELL/GET OUT/BLOCK, fresh Daily and intraday data, no warnings (extension ≥ 2 ATR, volume ≥ 3×, range > 1.5 ATR, current overnight ≥ 3%).
4. **EXCLUSION:** not on the exclusion watchlist.
5. **CATALYST:** per decision 7.
6. **SELL_LIMIT (revised 2026-11-14):** requires the SIP 40-window history, the reference close and the walk-forward gates (touch rate and adverse excursion). The four Thinkorswim ATR gates are **informational** (`4eafc9b`); ATR never changed the price. During the buy window the day's close does not exist yet, so the tested target % is applied to the **latest trade (at most 2 minutes old)**: limit = latest trade × (1 + target %). Re-check it against your actual fill.

Status precedence: BLOCKED, then DATA?, then NOT_READY, then OUTSIDE_BUY_WINDOW, then READY. Boring Winner PASS is never READY. MCC never submits an order.

## Answered 2026-11-14: close/open history on delayed SIP (`6fe6446`)

The 20-pair close/open history was incomplete on IEX (RTO 14/20, CRBG 13/20). It now uses delayed historical SIP 1m bars, with live 1M/5M inputs staying on IEX. Result: **RTO 20/20 and CRBG 20/20**.

Also fixed: the capture never set `currentOvernightPct`, so qualification was always DATA?. It is now the latest completed pair's return (the scanner's Current ON %).

On 2026-11-14 (Saturday) both symbols fail a real rule, *5-day average must exceed the 20-day average*: RTO 0.12% vs 0.44%, CRBG 0.68% vs 0.72%. The only DATA? inputs left are the live 1M/5M fields, which exist only during the session.
## Engineering defaults (unchanged)

- **Research entry reference:** the regular-session close (last regular 1m bar), labelled `ENTRY_PROXY_REGULAR_CLOSE`, plus the final-hour high as a conservative worst case. Neither is a fill price.
- **Live entry:** an operator-recorded actual buy replaces the proxy for that trade.
