# Boring Overnight evaluator implementation note

**Implementation:** `BORING-0.1-PROVISIONAL`  
**Scope:** Pure deterministic C# scoring/status evaluator with the shared Schwab exclusion provider.  
**Not included:** live quote/history retrieval, exchange calendar/session normalization, event/news provider, persistent decision snapshots, Thinkorswim column wiring, or order submission.

## Inputs and definitions

- Supply the last 20 **completed** close-to-open overnight return percentages, newest first. The evaluator does not create session boundaries or repair missing bars.
- The first 5 returns are used for the 5-session mean. Fewer than 20 returns yields `DATA?`; the implementation does not silently shorten the 20-session window.
- ON win means return > 0. Consistency means return in [0%, +2%], matching the provisional mapping.
- Volatility is population standard deviation of the 20 returns, in percentage points.
- The input producer must provide fresh price/volume/current-ON-move timestamps, a 20D SMA, catalyst status with evidence, and current-condition checks. Missing required fields never produce READY.
- A `ClearWithSource` catalyst state requires an evidence reference and review timestamp. A known incompatible catalyst blocks. Unknown/stale catalyst status yields DATA?.
- Exclusion match blocks. The shared list is case-insensitive and trims input whitespace.
- Current ON move >=3%, 5D mean >=5%, conflicting catalyst evidence, or caution flags produce WATCH if no hard blocker/data gap takes precedence.
- The score maps and the current-condition flags are provisional engineering rules, not validated trading advice. The score cannot override hard gates.

## Order-price distinction

Median ON return is a historical statistic, not a price target or guarantee. Multiplying the prior regular-session close by `1 + median/100` yields a historical median-based reference price only. It does not account for spread, commissions/fees, gap distribution, slippage, current catalyst risk, or the fact that a limit sell can remain unfilled. Never use this metric alone to set an order.

## Validation remaining

1. Run the xUnit suite after pulling the feature branch.
2. Compare calculated returns and score components to a hand-calculated fixture and historical labelled examples.
3. Implement market-calendar-aware close/open pairing, split adjustment and missing-bar policy.
4. Connect current quote, volume/SMA, and catalyst sources with explicit freshness checks.
5. Persist each evaluation with raw inputs, timestamps, source IDs, rules version, score components and reason codes.
6. Only after that wire this evaluator into scanner, Morning Report and persistent MarketWatch qualification. Each surface must consume the same result rather than recalculate its own score.
7. Validate score maps and status thresholds on labelled historical examples before production reliance.
