namespace MomentumCommandCenterV2.Application.Models;

public sealed record TradeStatistics(
    int TradeCount,
    int WinningTrades,
    int LosingTrades,
    int BreakevenTrades,
    decimal TotalPnl,
    decimal AveragePnl,
    decimal AveragePnlPercent,
    decimal WinRate,
    decimal AverageHoldMinutes,
    decimal LargestWin,
    decimal LargestLoss,
    decimal GrossProfit,
    decimal GrossLoss)
{
    public decimal ProfitFactor =>
        GrossLoss == 0m
            ? GrossProfit > 0m
                ? decimal.MaxValue
                : 0m
            : GrossProfit / Math.Abs(GrossLoss);
}