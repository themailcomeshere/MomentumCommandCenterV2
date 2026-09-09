namespace MomentumCommandCenterV2.Application.Models;

// =========================================================
// MODEL: TradeStatistics
// PURPOSE:
//   Contains aggregate statistics for a collection of
//   completed trades.
//
// RESPONSIBILITIES:
//   - Track winners, losers, and breakeven trades.
//   - Calculate P&L and P&L percentages.
//   - Calculate win rate.
//   - Calculate holding time.
//   - Calculate gross profit/loss and profit factor.
//
// LAYER:
//   Application
// =========================================================
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