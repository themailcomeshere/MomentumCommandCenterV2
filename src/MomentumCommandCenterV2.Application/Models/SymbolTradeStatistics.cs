namespace MomentumCommandCenterV2.Application.Models;

// =========================================================
// MODEL: SymbolTradeStatistics
// PURPOSE:
//   Contains trade-performance statistics for one symbol.
//
// RESPONSIBILITIES:
//   - Aggregate trades by symbol.
//   - Measure symbol-level profitability,
//     win rate, and holding behavior.
//
// LAYER:
//   Application
// =========================================================
public sealed record SymbolTradeStatistics(
    string Symbol,
    int TradeCount,
    int WinningTrades,
    int LosingTrades,
    decimal TotalPnl,
    decimal AveragePnl,
    decimal WinRate,
    decimal AverageHoldMinutes,
    decimal LargestWin,
    decimal LargestLoss);