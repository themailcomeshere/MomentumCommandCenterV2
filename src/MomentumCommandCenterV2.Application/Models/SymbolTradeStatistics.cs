namespace MomentumCommandCenterV2.Application.Models;

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