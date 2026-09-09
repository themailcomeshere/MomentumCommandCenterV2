using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Services;

public sealed class TradeAnalysisService : ITradeAnalysisService
{
    public TradeAnalysisResult Analyze(
        TradeImportResult importResult)
    {
        ArgumentNullException.ThrowIfNull(importResult);

        var trades =
            importResult.CompletedTrades
                .OrderBy(trade => trade.EntryTime)
                .ToList();

        var overall =
            BuildOverallStatistics(trades);

        var bySymbol =
            trades
                .GroupBy(
                    trade => trade.Symbol,
                    StringComparer.OrdinalIgnoreCase)
                .Select(BuildSymbolStatistics)
                .OrderByDescending(stat => stat.TotalPnl)
                .ThenBy(stat => stat.Symbol)
                .ToList();

        return new TradeAnalysisResult(
            trades,
            importResult.OpenPositions,
            importResult.UnmatchedOrders,
            overall,
            bySymbol);
    }

    private static TradeStatistics BuildOverallStatistics(
        IReadOnlyList<Trade> trades)
    {
        if (trades.Count == 0)
        {
            return new TradeStatistics(
                0,
                0,
                0,
                0,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m);
        }

        var winners =
            trades
                .Where(trade => trade.Pnl > 0m)
                .ToList();

        var losers =
            trades
                .Where(trade => trade.Pnl < 0m)
                .ToList();

        var breakeven =
            trades.Count -
            winners.Count -
            losers.Count;

        var totalPnl =
            trades.Sum(trade => trade.Pnl);

        var averagePnl =
            totalPnl / trades.Count;

        var averagePnlPercent =
            trades.Average(trade => trade.PnlPercent);

        var averageHoldMinutes =
            trades.Average(
                trade => trade.HoldTime.TotalMinutes);

        var largestWin =
            winners.Count == 0
                ? 0m
                : winners.Max(trade => trade.Pnl);

        var largestLoss =
            losers.Count == 0
                ? 0m
                : losers.Min(trade => trade.Pnl);

        var grossProfit =
            winners.Sum(trade => trade.Pnl);

        var grossLoss =
            losers.Sum(trade => trade.Pnl);

        return new TradeStatistics(
            trades.Count,
            winners.Count,
            losers.Count,
            breakeven,
            totalPnl,
            averagePnl,
            averagePnlPercent,
            (decimal)winners.Count / trades.Count * 100m,
            (decimal)averageHoldMinutes,
            largestWin,
            largestLoss,
            grossProfit,
            grossLoss);
    }

    private static SymbolTradeStatistics BuildSymbolStatistics(
        IGrouping<string, Trade> group)
    {
        var trades =
            group.ToList();

        var winners =
            trades
                .Where(trade => trade.Pnl > 0m)
                .ToList();

        var losers =
            trades
                .Where(trade => trade.Pnl < 0m)
                .ToList();

        return new SymbolTradeStatistics(
            group.Key,
            trades.Count,
            winners.Count,
            losers.Count,
            trades.Sum(trade => trade.Pnl),
            trades.Average(trade => trade.Pnl),
            (decimal)winners.Count / trades.Count * 100m,
            (decimal)trades.Average(
                trade => trade.HoldTime.TotalMinutes),
            winners.Count == 0
                ? 0m
                : winners.Max(trade => trade.Pnl),
            losers.Count == 0
                ? 0m
                : losers.Min(trade => trade.Pnl));
    }
}