# Delayed historical SIP — GTC_EXT sell-window capture, RTO (2026-11-14)

**Run by:** Claude on the operator machine (Development secrets, MCC launcher). Code: `0e02e6a` plus the uncommitted delayed-SIP minute change, committed in the same pass.
**Config:** `Feed=iex`, `AllowDelayedHistoricalSipMinutes=true`. Request: `POST /api/boring-overnight/capture-sell-window-history {"symbol":"RTO","providerName":"Alpaca","feed":"sip"}`.
**Operator decision:** 2026-11-14, delayed historical SIP minute bars allowed for the 40-window sell history.

## Result

- The free account accepted the SIP 1m requests: 15,201 bars fetched and persisted with feed `sip`, in 3.8 s, with no refusal.
- `feedCoverage=CONSOLIDATED_SIP`; reference close present for all 40 windows.
- **Complete windows: 0/40.** Observed minutes per window: 302–424 of the 780 required (07:00–19:59 ET).

## Why nothing is "complete"

Bars per Eastern hour on 2026-11-12, as stored: 07h 3 · 08h 2 · 09h 33 · 10h 60 · 11h 58 · 12h 59 · 13h 59 · 14h 49 · 15h 57 · 16h 1 · 17h–19h 0.

Alpaca returns a 1m bar only for minutes that had a trade. RTO trades in almost every regular-session minute but rarely in extended hours. The current rule requires a bar for every one of the 780 minutes, so an absent bar counts as missing data. For thinly traded extended hours that rule can effectively never pass.

**Interpretation:** a successful SIP response with no bar for a minute most likely means no eligible trade in that minute, not lost data. MCC cannot prove this from bars alone. Whether absent minutes inside a successful, unpaginated, consolidated response may be classified as `NO_TRADE` (counted and shown, never filled with a price) is an operator decision. A minute with no trade cannot touch a limit, so it does not change target-touch results. It does change the "complete window" gate.

Raw audit: `RTO-sell-window-history.json` (as-of = capture time − 16 min).

## Update — NO_TRADE rule applied (operator decision 2026-11-14)

The rule: on consolidated SIP, a minute with no bar is `NO_TRADE` (counted, never priced). It applies only when the window's exact reference close is present and the sell session's regular session has bars. Otherwise the minute stays `MISSING`.

- Re-audit of the same stored bars (`RTO-sell-window-history-no-trade-rule.json`): **40/40 complete**, qualification usable. 14,910 minutes with a bar, 16,290 NO_TRADE, 0 MISSING. The rule applied in all 40 windows.
- Dashboard button **Fetch / retry GTC_EXT sell-window history**, checked in the browser: the same result, 0 newly stored (idempotent).
- Sell-limit assessment at the 50th percentile (`RTO-sell-limit-p50.json`): **BLOCKED**.
  - Passing: SIP, history 40/40, reference close 23.90.
  - Failing: the TOS ATR provenance, parity, warm-up and current-ATR gates (no Thinkorswim ATR observations are saved).
  - Walk-forward: target +0.7736% (training P50), training touch rate 55%, unseen-test touch rate 65% (13/20). Touch-rate gate PASS. Adverse-excursion gate FAIL: unseen max 5.85% against a training P95 of 3.36%.
- Research only: touches are inferred from OHLCV highs, never from fills. No limit is issued.
