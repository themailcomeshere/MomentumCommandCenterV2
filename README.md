# Momentum Command Center V2 Prototype

C#/.NET 8 research and decision-engine prototype. **Not an auto-trader.**

## Architecture
5M Direction/Permission -> 1M Execution Confirmation -> Position Lifecycle -> Runner / Weakening / Exit.

States: NoTrade, Watch, Setup, EntryReady, Runner, Hold, Weakening, Exit.

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

## Future V2 work
Screenshot/trade import, MFE/MAE, scanner-event tracking, daily journal, V1-vs-V2 comparison, configurable scoring, exact parity with frozen ThinkScript, then a market-data adapter. Live order placement is intentionally absent.
