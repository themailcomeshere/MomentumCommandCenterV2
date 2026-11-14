# Boring Overnight READY semantics — decision record

**Created:** 2026-11-14. **Status:** three core decisions frozen by the operator; five follow-up questions open (below). READY is still not wired; this record is its specification input.

## Frozen by the operator (2026-11-14)

| # | Decision | Exact meaning | Already in code? |
|---|---|---|---|
| 1 | **Buy time: any time in the final regular hour of Day 1** | From regular close − 60 min to the regular close, America/New_York. Normal day: 15:00–16:00 ET, which is **13:00–14:00 Arizona in standard time (Nov–Mar)** and 12:00–13:00 Arizona in daylight time. On an early close, the hour before the early close. | Yes. `MarketSessionClock` uses `buyClose.AddHours(-1)` → `buyClose` (`MarketSessionClock.cs:57-61`), including early closes from `UsEquitiesSessionCalendar`. |
| 2 | **Exit: GTC_EXT limit sell on Day 2, 07:00–20:00 ET** | A limit order working through the full extended window: **05:00–18:00 Arizona in standard time**, 04:00–17:00 in daylight time. Research success = the window's high reaches the limit. **A touch is not a fill.** | Window yes (`SellWindowStart`/`SellWindowEnd`). Limit derivation = Calculated Limit Sell, still fail-closed (needs 40 complete SIP GTC_EXT windows). |
| 3 | **Official opening-auction print: not required** | The exit is a limit in the extended window, not an open print. The 09:30 close/open capture series (`ONE_MINUTE_BAR_OPEN_PROXY`) stays a separately named research series. It is never the strategy endpoint. | The capture is already labelled a proxy (`officialOpeningAuctionVerified=false`). |

**Consequence for the To-Do:** "Decide whether official opening-auction data is required" is resolved (not required). "Freeze buy time / endpoint / auction choice" is resolved for the three parts above. V3 study work is unblocked except for the open questions below.

## Engineering defaults that follow (proposed; change them if they are wrong)

- **Research entry reference:** because the real buy time varies inside the hour, historical studies use the **regular-session close** (last regular 1m bar close) as the central entry proxy, labelled `ENTRY_PROXY_REGULAR_CLOSE`. They also report the **final-hour high** as the conservative worst-case entry. Neither is a fill price.
- **Live entry:** when the operator records an actual buy (time and price), that record replaces the proxy for that trade.
- **Sell-window coverage:** a Day 2 window counts only if every 07:00–19:59 ET minute is present on a consolidated feed (no imputation). This is the existing `HISTORY_40_OF_40` gate.

## Open questions for the operator

1. **Unfilled limit at 20:00 ET.** What do you do: cancel and hold, re-enter on Day 3, or sell at market? Data not found in sources.
2. **Catalyst gate.** Zero news is UNKNOWN and can never be CLEAR. Should READY accept your manual attestation ("I checked; nothing material"), recorded with your note and timestamp, as the catalyst pass? If not, READY can never pass on a no-news symbol.
3. **Historical SIP minute bars.** Delayed historical SIP works for Daily on the free account (2026-11-14 evidence). The 40-window GTC_EXT sell history needs consolidated **minute** bars. May MCC request those the same way (end ≥ 16 min old, completed windows only)? Today's rule allows Daily only.
4. **Daily parity tolerance.** SIP Daily matches the independent reference within $0.005 on prices (reference rounds to 2 decimals) and within 0.11% on volume. The current rule is ±$0.0001 and exact volume, so it still FAILs. Accept ±$0.005 and ±0.2% volume?
5. **Position size / ZERO POSITION.** BoringOvernight.md lists ZERO POSITION as a valid outcome. Is a READY candidate always bought, or is READY one input to your own sizing decision?

## What READY will require once the questions are answered (gate list)

1. Daily qualification usable (consolidated SIP, 20/20 sessions, fresh), as now possible with `DailyFeed=sip_delayed`.
2. Scanner and history thresholds as already specified (5D > 20D mean, ADV20 ≥ 500K, price/SMA rules).
3. Exclusion watchlist not matched.
4. Catalyst gate per question 2.
5. Evaluation inside the buy window (decision 1), with fresh intraday data.
6. Sell limit calculable (40/40 complete windows), or READY shows the limit as DATA? and the operator sets it manually. To be decided with question 3.

No sound alert and no READY label until these are wired and tested. Boring Winner PASS is never READY.
