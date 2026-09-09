# Momentum Command Center V2 Prototype

C#/.NET 8 research and decision-engine prototype. **Not an auto-trader.**

## Architecture
5M Direction/Permission -> 1M Execution Confirmation -> Position Lifecycle -> Runner / Weakening / Exit.

Core
  ↑
Application
  ↑
Infrastructure
  ↑
CLI

### Current prototype rules
5M permission scores six conditions: price > VWAP, price > 9 EMA, 9 EMA > 20 EMA, 20 EMA > 50 SMA, RSI >= 50, RVOL >= 1.5. Five or more = long permission.

Fresh long requires 5M permission plus 1M price/VWAP/EMA alignment, MACD confirmation, RSI >= 50, RVOL >= 1.5, RSI < 78, and no severe ATR extension.

While in a position, V2 can remain RUNNER while trend/momentum are intact, switch to WEAKENING when conditions deteriorate, and issue EXIT on hard deterioration or EOD. EOD is 3:55 PM ET.

## Why this is V2
The key change is trade lifecycle management. We are explicitly separating fresh-entry logic from management of an already-open position. This addresses the NBIL lesson from 2026-09-08: a new long can become invalid after the clean trend phase, and an open position must not simply remain open while structure deteriorates.

## Validation plan
Build now. Keep V1 as the control. During the next ~10 trading days, compare V1 and V2 against screenshots and complete Schwab trade history. Do not optimize thresholds after one trade.

## Build
Visual Studio 2022 + .NET 8 SDK. Open the .sln, Build Solution, then run the xUnit test project. The CLI prints the available state machine states.

## Future V2 Work

### Phase 1 — Historical Trade Foundation
- [x] Schwab Order History CSV importer
- [x] Use Schwab Fill Price rather than order/limit Price
- [x] ET timestamp parsing
- [x] FIFO trade matching
- [x] Open-position detection
- [x] Unmatched-order detection
- [x] Trade evaluation context
- [x] Trade statistics and symbol-level analysis
- [ ] Daily trading-session grouping

### Phase 2 — Trade Quality Analysis
- [ ] MFE / MAE
- [ ] Hold-time analysis
- [ ] Entry-to-peak analysis
- [ ] Exit-to-peak giveback analysis
- [ ] Runner identification
- [ ] Early-exit detection
- [ ] Missed-runner detection
- [ ] Limit-price analysis

### Phase 3 — Market/Scanner Correlation
- [ ] Scanner-event tracking
- [ ] Scan A / B / C event model
- [ ] Momentum Active snapshot model
- [ ] 1M / 5M signal snapshot storage
- [ ] News/catalyst context
- [ ] Correlate scanner state with actual fills

### Phase 4 — V1 vs V2 Validation
- [ ] Replay historical trades
- [ ] Compare V1 decision vs V2 decision
- [ ] Identify false exits
- [ ] Identify missed entries
- [ ] Identify missed runners
- [ ] Measure drawdown
- [ ] Do not optimize thresholds from a single trade

### Phase 5 — Historical Replay Engine
- [ ] Sequential market snapshot replay
- [ ] Position lifecycle reconstruction
- [ ] Signal-state transition history
- [ ] Simulated V2 decisions
- [ ] Compare simulated decisions with actual Schwab execution

### Phase 6 — Market Data Adapter
- [ ] Market-data abstraction
- [ ] 1M bar ingestion
- [ ] 5M bar ingestion
- [ ] Indicator calculation/validation
- [ ] Live decision snapshots

### Explicitly Out of Scope
- Automated order placement
- Autonomous trading
- Automatic Schwab order submission

### Documentation
- [ ] /docs/V2-Signal-Rules.md
- [ ] /docs/Trade-Analysis.md
- [ ] /docs/Validation-Plan.md


### What we're doing next

This keeps us on the roadmap rather than wandering into another signal-engine rewrite:

**Phase 1 is now essentially complete except for daily session grouping.**

Then we move to **Phase 2: Trade Quality Analysis**.

And importantly, our next work should be based on the actual problem you've been trying to solve with the TOS system: **did we enter well, did we exit too early, did the position become a runner, and were our limit prices unnecessarily aggressive?**

That's where the Schwab history starts becoming much more valuable than simply calculating win rate.



### TODO: Determine if this section stays in here or if there should be another file for this
A position can remain in RUNNER while the underlying trend and momentum
remain healthy.

A position can move to RUNNER WANING when deterioration begins without
yet meeting the criteria for a confirmed exit.

Hard structural deterioration, severe ATR extension, or EOD can force an
exit.

Current V2 Prototype Rules

5M permission scores six conditions:

Price > VWAP
Price > 9 EMA
9 EMA > 20 EMA
20 EMA > 50 SMA
RSI >= 50
RVOL >= 1.5

Five or more conditions produce long permission.

Fresh long entry additionally requires:

5M permission
5M bullish structure
1M bullish structure
1M momentum confirmation
RSI below the fresh-entry ceiling
No severe ATR extension

While in a position, V2 evaluates the position separately from fresh-entry
logic.

Historical Trade Foundation

The application can now consume the actual Schwab Order History CSV format.

The importer currently:

Uses Schwab Fill Price, not the submitted order/limit price.
Parses Schwab timestamps as Eastern Time.
Matches filled buys and sells using FIFO.
Reconstructs completed trades.
Identifies remaining open positions.
Preserves unmatched sell orders.
Does not silently discard anomalies.

The current Schwab export does not provide an explicit quantity field, so
the importer currently treats each filled row as quantity 1.

This limitation will be addressed only when the actual broker data requires
a more sophisticated quantity model.

Trade Analysis

The application now provides:

Overall trade count.
Winning / losing / breakeven counts.
Total P&L.
Average P&L.
Average P&L percentage.
Win rate.
Average holding time.
Largest win.
Largest loss.
Gross profit.
Gross loss.
Profit factor.
Symbol-level statistics.

The statistical analysis is intentionally deterministic. It does not
attempt to infer whether an exit was "good" or "bad" yet.

That requires historical market-data replay.

Validation Philosophy

Build first. Validate against actual historical behavior.

V1 remains the control.

During the validation period:

Compare V1 and V2 against actual screenshots.
Compare V1 and V2 against completed Schwab trade history.
Preserve actual execution behavior.
Do not optimize thresholds based on one trade.
Do not manufacture MFE/MAE from trade-only data.
Do not infer market behavior that cannot be supported by historical
market snapshots.

The purpose of V2 is to explain and improve the trading process, not to
retroactively make historical trades look better.