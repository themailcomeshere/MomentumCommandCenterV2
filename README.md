# Momentum Command Center V2 Prototype

C#/.NET 8 research and decision-support prototype. **Not an auto-trader.**

## Timezone
Arizona. This means sometimes the timezone will be MDT and sometimes PST.

## Architecture

5M Direction/Permission → 1M Execution Confirmation → Position Lifecycle → Runner / Weakening / Exit.

Core contains deterministic decision/replay logic; Application contains use-case contracts; Infrastructure contains file/provider adapters; CLI exposes local commands. The repository currently is not an ABP Framework application.

## Current prototype rules

5M permission scores six conditions: price > VWAP, price > 9 EMA, 9 EMA > 20 EMA, 20 EMA > 50 SMA, RSI >= 50, RVOL >= 1.5. Five or more = long permission.

Fresh long requires 5M permission plus 1M price/VWAP/EMA alignment, MACD confirmation, RSI >= 50, RVOL >= 1.5, RSI < 78, and no severe ATR extension.

While in a position, V2 can remain RUNNER while trend/momentum are intact, switch to WEAKENING when conditions deteriorate, and issue EXIT on hard deterioration or EOD. EOD is 3:55 PM ET.

## Opportunity quality

A valid `EXECUTION READY` event is not a ranking of expected opportunity quality. See [Opportunity Quality and Ranking](docs/Opportunity-Quality-and-Ranking.md). The current assessor is diagnostic only, with provisional bands; it does not replace eligibility or claim profitability.

## Market data and historical replay

See [Market-Data and Historical-Replay Contract](docs/Market-Data-and-Replay-Contract.md).

The current engineering slice adds raw timestamped 1M/5M OHLCV bars, a timezone-aware CSV reader, deterministic replay frames that only attach a 5M bar timestamped at or before the replay point, duplicate/malformed-bar validation, tests, a CLI replay command, and a synthetic sample CSV.

Run local CSV replay:

```bash
dotnet run --project src/MomentumCommandCenterV2.Cli -- replay samples/market-data/sample-bars.csv
```

This is a data foundation, not a full backtest. It does not yet calculate indicators, reproduce ThinkOrSwim study outputs, simulate fills, or connect to live market data. Timestamp open/close semantics and session handling must be agreed with the eventual provider before integration.

## Front-end workbench

Open [frontend/index.html](frontend/index.html) directly in a browser. It is a local research workbench with illustrative scanner groups, local CSV candidate import, and browser-local watchlist persistence. No provider, broker, news, or order API is connected. See [Scanner and Watchlist Workbench](docs/Scanner-and-Watchlist-Workbench.md). The next production step is a provider-neutral replay/scanner contract after data and indicator parity are established.

## Build and test

Use Visual Studio 2022 and .NET 8 SDK. Open `MomentumCommandCenterV2.sln`, build the solution, and run the xUnit test project.

## Validation principles

- Keep V1 as a control when comparing decisions.
- Compare signal outputs with source ThinkOrSwim studies and screenshots.
- Do not optimize thresholds after one trade.
- Do not infer MFE/MAE from trade-only data.
- Missing/stale data is UNKNOWN, not READY.
- Automated order placement and autonomous Schwab submission remain out of scope.

## Project roadmap and current checklist

- The canonical project checklist is [docs/Master-To-Do.md](docs/Master-To-Do.md). Commit Message should state the To-Do item's Category and explanation. End full Commit Message with the currently remaining To-Do item count.
- The original README is preserved as [an archived source](docs/archive/README_ORIGINAL.md) after reconciliation.