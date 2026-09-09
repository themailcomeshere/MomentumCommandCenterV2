using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Application.Services;
using MomentumCommandCenterV2.Core.Models;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class TradeAnalysisServiceTests
{
    [Fact]
    public void CalculatesOverallStatistics()
    {
        var trades = new List<Trade>
        {
            new(
                "AAA",
                new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero),
                10m,
                new DateTimeOffset(2026, 9, 8, 10, 10, 0, TimeSpan.Zero),
                11m,
                1),

            new(
                "BBB",
                new DateTimeOffset(2026, 9, 8, 11, 0, 0, TimeSpan.Zero),
                20m,
                new DateTimeOffset(2026, 9, 8, 11, 20, 0, TimeSpan.Zero),
                19m,
                1),

            new(
                "CCC",
                new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero),
                30m,
                new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero),
                30m,
                1)
        };

        var importResult = new TradeImportResult(
            trades,
            Array.Empty<OpenPosition>(),
            Array.Empty<UnmatchedOrder>(),
            6,
            Array.Empty<string>());

        var service = new TradeAnalysisService();

        var result = service.Analyze(importResult);

        Assert.Equal(3, result.Overall.TradeCount);
        Assert.Equal(1, result.Overall.WinningTrades);
        Assert.Equal(1, result.Overall.LosingTrades);
        Assert.Equal(1, result.Overall.BreakevenTrades);

        Assert.Equal(0m, result.Overall.TotalPnl);
        Assert.Equal(0m, result.Overall.AveragePnl);
        Assert.Equal(5m / 3m, result.Overall.AveragePnlPercent);

        Assert.InRange(
            result.Overall.WinRate,
            33.33332m,
            33.33334m);

        Assert.Equal(20m, result.Overall.AverageHoldMinutes);

        Assert.Equal(1m, result.Overall.LargestWin);
        Assert.Equal(-1m, result.Overall.LargestLoss);

        Assert.Equal(1m, result.Overall.GrossProfit);
        Assert.Equal(-1m, result.Overall.GrossLoss);

        Assert.Equal(1m, result.Overall.ProfitFactor);
    }

    [Fact]
    public void GroupsStatisticsBySymbol()
    {
        var start =
            new DateTimeOffset(
                2026,
                9,
                8,
                14,
                0,
                0,
                TimeSpan.Zero);

        var trades =
            new List<Trade>
            {
                new(
                    "NBIL",
                    start,
                    30m,
                    start.AddMinutes(2),
                    31m,
                    1),

                new(
                    "NBIL",
                    start.AddMinutes(5),
                    32m,
                    start.AddMinutes(8),
                    30m,
                    1),

                new(
                    "SST",
                    start.AddMinutes(10),
                    5m,
                    start.AddMinutes(12),
                    5.50m,
                    1)
            };

        var result =
            new TradeAnalysisService()
                .Analyze(
                    new TradeImportResult(
                        trades,
                        [],
                        [],
                        6,
                        []));

        Assert.Equal(2, result.BySymbol.Count);

        var nbil =
            result.BySymbol
                .Single(stat => stat.Symbol == "NBIL");

        Assert.Equal(2, nbil.TradeCount);
        Assert.Equal(1, nbil.WinningTrades);
        Assert.Equal(1, nbil.LosingTrades);
        Assert.Equal(-1m, nbil.TotalPnl);

        var sst =
            result.BySymbol
                .Single(stat => stat.Symbol == "SST");

        Assert.Equal(1, sst.TradeCount);
        Assert.Equal(1, sst.WinningTrades);
        Assert.Equal(0.50m, sst.TotalPnl);
    }

    [Fact]
    public void PreservesOpenPositionsAndUnmatchedOrders()
    {
        var open =
            new OpenPosition(
                "SST",
                new DateTimeOffset(
                    2026,
                    9,
                    8,
                    15,
                    8,
                    0,
                    TimeSpan.Zero),
                5.10m,
                1);

        var unmatched =
            new UnmatchedOrder(
                "XYZ",
                "Sell",
                10m,
                new DateTimeOffset(
                    2026,
                    9,
                    8,
                    15,
                    10,
                    0,
                    TimeSpan.Zero),
                1,
                99);

        var result =
            new TradeAnalysisService()
                .Analyze(
                    new TradeImportResult(
                        [],
                        [open],
                        [unmatched],
                        2,
                        []));

        Assert.Single(result.OpenPositions);
        Assert.Equal("SST", result.OpenPositions[0].Symbol);

        Assert.Single(result.UnmatchedOrders);
        Assert.Equal("XYZ", result.UnmatchedOrders[0].Symbol);

        Assert.Empty(result.Trades);
        Assert.Equal(0, result.Overall.TradeCount);
    }
}