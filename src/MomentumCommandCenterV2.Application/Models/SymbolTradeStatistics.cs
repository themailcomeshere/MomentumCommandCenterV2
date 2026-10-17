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
/// <summary>
/// TYPE: SymbolTradeStatistics
/// PURPOSE: Defines the record contract or data structure named SymbolTradeStatistics.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
/// <summary>
/// METHOD: SymbolTradeStatistics
/// PURPOSE: Implements SymbolTradeStatistics for this component.
/// WHEN CALLED: Called by the workflow or caller that requests SymbolTradeStatistics.
/// </summary>
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