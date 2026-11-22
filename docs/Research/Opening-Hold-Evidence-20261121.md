# Opening entry hold: evidence review (2026-11-21)

**Decision (operator, 2026-11-21):** default opening entry hold = **15 minutes** (09:30–09:45 ET; 07:30–07:45 AZ in standard time, 06:30–06:45 AZ in daylight time). This replaces the 60-minute default from `b05bb98`. The hold is set by `MarketData:Intraday:OpeningEntryHoldMinutes` and the **4 Red Zone → Opening hold** dropdown in INTRADAY MOMENTUM.

**Status:** this is a synthesis of published research plus MCC's own mechanics. No peer-reviewed study tests "wait N minutes before entering" for a 5M/1M momentum system like MCC's. Whether 15 minutes improves MCC outcomes: Data not found in sources. The signal log records every withheld entry so this can be measured.

## 1. Costs and noise are worst right at the open

- **McInish & Wood (1992), *Journal of Finance* 47(2).** Minute-by-minute NYSE spreads form a reverse-J over the day: widest at the open, then falling.
- **Chan, Chung & Johnson (1995), *JFQA*.** NYSE stock spreads are U-shaped.
- **Chung, Van Ness & Van Ness, *Journal of Financial Markets*.** NYSE spreads drop abruptly after the open. Nasdaq spreads decline more gradually.
- **Why:**
  - Overnight news builds up while the market is closed.
  - Market makers do not yet know who is informed (adverse selection), so they quote wide.
  - Prices are still being discovered.
- **Andersen & Bollerslev (1997), *Journal of Empirical Finance* 4.** Equity volatility has a strong, regular intraday pattern that tracks volume and spreads.

## 2. Most opening price discovery happens within minutes

- **Busse & Green (2002), *Journal of Financial Economics*.** Cites earlier work finding that pre-open buy recommendations are reflected in prices within 5–15 minutes after the opening. In their own CNBC data, positive news is absorbed in about a minute and negative news over about 15 minutes.
- **Santosh, "The Speed of Price Discovery" (SSRN working paper, not peer-reviewed).**
  - After earnings news, about 81% of the price adjustment is done within three minutes and over 90% within an hour.
  - Small firms lag at the open, but the gap largely closes within 30–60 minutes.
- **Practice note:** some microstructure studies avoid the first minutes' quotes as noisy and use the midquote 15 minutes after the open (AEA 2021 conference paper, not peer-reviewed).

## 3. The first half hour also carries the momentum signal

- **Gao, Han, Li & Zhou (2018), "Market intraday momentum," *Journal of Financial Economics* 129(2).** The first half-hour return, measured from the previous close, predicts the last half-hour return (S&P 500 ETF, 1993–2013, plus ten other ETFs).
- **Heston, Korajczyk & Sadka (2010), *Journal of Finance* 65(4).**
  - Returns continue at the same half-hour of later days, most strongly in the first and last half-hours.
  - Short-term reversals come from temporary liquidity imbalances lasting under an hour.
  - Timing trades can save about one effective spread in execution cost.
- **Zarattini, Barbon & Aziz (2024), SSRN / Swiss Finance Institute working paper (not peer-reviewed).** A 5-minute opening-range breakout on unusually active stocks was profitable net of costs. A practitioner replication on index CFDs found the gross result but zero net.

## 4. MCC mechanics that also point to about 15 minutes

- **1M coverage gate (`a31b8a5`).** It counts bars over the last 15 regular-session minutes. Before 09:45 ET the window holds fewer than 15 minutes, so the gate's judgement rests on less data.
- **5M permission latch.** It starts at NO each Eastern day. Structural damage and major failure need two consecutive bars. By 09:45 ET three completed 5m bars exist, so a failed open can register as damage before entries are allowed.
- **Session VWAP.** It starts from the first prints. After 15 minutes it reflects the opening volume rather than a few trades.
- **Thin symbols.** Stock-by-stock thin data is already handled by the coverage gate. The time hold only needs to cover the market-wide opening disorder.

## 5. Why 15 and not 0, 30 or 60

| Hold | Avoids | Gives up |
|---|---|---|
| 0 | nothing | entries in the widest-spread, least-discovered minutes, and the coverage gate runs on fewer than 15 minutes |
| **15** | the steepest part of the spread/volatility spike, most opening price discovery, and the MCC indicator warm-up | entries in the first 3 five-minute bars |
| 30 | more small-cap price discovery | half of the first half hour, the window the momentum studies use as the signal |
| 60 | nearly all opening disorder | the full first hour, where much intraday momentum starts; later entries are more often ATR-extended |

For thin small caps, 30 minutes is defensible (Santosh). In MCC the 1M coverage gate already withholds those stock by stock, so the default stays at 15.

## 6. How we will test it

1. Signal log: every withheld entry is a PREPARE TO BUY line whose reason starts `OPENING HOLD`.
2. Replay: `GET /api/market-data/replay?...&openingHoldMinutes=0` vs `=15` vs `=30` on the same timestamps.
3. After 10+ sessions, compare the outcomes of entries signalled at 09:30–09:45, 09:45–10:00 and after 10:00 ET. Then decide whether to keep 15.

## Sources

- McInish, T. & Wood, R. (1992). An Analysis of Intraday Patterns in Bid/Ask Spreads for NYSE Stocks. *Journal of Finance* 47(2), 753–764.
- Chan, K., Chung, Y.P. & Johnson, H. (1995). The Intraday Behavior of Bid-Ask Spreads for NYSE Stocks and CBOE Options. *JFQA*.
- Chung, K., Van Ness, B. & Van Ness, R. Order handling rules, tick size, and the intraday pattern of bid-ask spreads. *Journal of Financial Markets*.
- Andersen, T. & Bollerslev, T. (1997). Intraday periodicity and volatility persistence in financial markets. *Journal of Empirical Finance* 4, 115–158.
- Busse, J. & Green, T.C. (2002). Market efficiency in real time. *Journal of Financial Economics*.
- Gao, L., Han, Y., Li, S. & Zhou, G. (2018). Market intraday momentum. *Journal of Financial Economics* 129(2), 394–414.
- Heston, S., Korajczyk, R. & Sadka, R. (2010). Intraday Patterns in the Cross-Section of Stock Returns. *Journal of Finance* 65(4), 1369–1407.
- Santosh, S. The Speed of Price Discovery: Trade Time vs Clock Time. SSRN 2567486 (working paper).
- Zarattini, C., Barbon, A. & Aziz, A. (2024). A Profitable Day Trading Strategy for the U.S. Equity Market. SSRN 4729284 (working paper).
