using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class TradeAnalyzerTests
{
    private static readonly DateTimeOffset BaseTime =
        DateTimeOffset.Parse("2026-09-08T13:00:00-04:00");

    private static BarSnapshot Bar(
        DateTimeOffset timestamp,
        decimal close,
        decimal high,
        decimal low,
        decimal vwap,
        decimal ema9,
        decimal ema20,
        decimal sma50,
        decimal rvol,
        decimal rsi,
        decimal macd,
        decimal macdSignal,
        decimal atr = 1m)
    {
        return new BarSnapshot(
            timestamp,
            close,
            high,
            low,
            close,
            1_000_000,
            rvol,
            vwap,
            ema9,
            ema20,
            sma50,
            rsi,
            macd,
            macdSignal,
            atr);
    }

    private static CommandCenterSnapshot Snapshot(
        DateTimeOffset timestamp,
        string symbol,
        BarSnapshot oneMinute,
        BarSnapshot fiveMinute)
    {
        return new CommandCenterSnapshot(
            timestamp,
            symbol,
            oneMinute,
            fiveMinute,
            MarketPhase.RegularSession);
    }

    private static CommandCenterSnapshot StrongSnapshot(
        DateTimeOffset timestamp,
        string symbol = "TEST",
        decimal close = 110m,
        decimal high = 110m,
        decimal low = 109m)
    {
        var oneMinute = Bar(
            timestamp,
            close,
            high,
            low,
            close - 10m,   // VWAP
            close - 2m,    // EMA9
            close - 5m,    // EMA20
            close - 10m,   // SMA50
            3m,            // RVOL
            60m,           // RSI
            2m,            // MACD
            1m,            // MACD signal
            1m);           // ATR

        var fiveMinute = Bar(
            timestamp,
            close,
            high,
            low,
            close - 10m,   // VWAP
            close - 2m,    // EMA9
            close - 5m,    // EMA20
            close - 10m,   // SMA50
            3m,            // RVOL
            60m,           // RSI
            2m,            // MACD
            1m,            // MACD signal
            1m);           // ATR

        return Snapshot(
            timestamp,
            symbol,
            oneMinute,
            fiveMinute);
    }

    [Fact]
    public void Review_ReturnsInsufficientData_WhenNoSnapshotsCoverTrade()
    {
        var trade = new Trade(
            "TEST",
            BaseTime,
            100m,
            BaseTime.AddMinutes(5),
            102m,
            1);

        var snapshots = Array.Empty<CommandCenterSnapshot>();

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "INSUFFICIENT_DATA",
            result.Classification);

        Assert.Equal(
            "No snapshots cover the trade window.",
            result.Finding);

        Assert.Null(result.MfePercent);
        Assert.Null(result.MaePercent);
    }

    [Fact]
    public void Review_ReturnsInsufficientData_WhenSnapshotsAreOutsideTradeWindow()
    {
        var trade = new Trade(
            "TEST",
            BaseTime,
            100m,
            BaseTime.AddMinutes(5),
            102m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                BaseTime.AddMinutes(-5)),

            StrongSnapshot(
                BaseTime.AddMinutes(10))
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "INSUFFICIENT_DATA",
            result.Classification);

        Assert.Null(result.MfePercent);
        Assert.Null(result.MaePercent);
    }

    [Fact]
    public void Review_CalculatesMfeAndMaeFromOneMinuteHighsAndLows()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            104m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                entryTime,
                close: 100m,
                high: 101m,
                low: 99m),

            StrongSnapshot(
                entryTime.AddMinutes(1),
                close: 103m,
                high: 108m,
                low: 98m),

            StrongSnapshot(
                exitTime,
                close: 104m,
                high: 106m,
                low: 100m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            8m,
            result.MfePercent);

        Assert.Equal(
            -2m,
            result.MaePercent);
    }

    [Fact]
    public void Review_IgnoresSnapshotsForOtherSymbols()
    {
        var trade = new Trade(
            "TEST",
            BaseTime,
            100m,
            BaseTime.AddMinutes(5),
            104m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                BaseTime,
                symbol: "OTHER"),

            StrongSnapshot(
                BaseTime.AddMinutes(5),
                symbol: "OTHER")
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "INSUFFICIENT_DATA",
            result.Classification);

        Assert.Null(result.MfePercent);
        Assert.Null(result.MaePercent);
    }

    [Fact]
    public void Review_IgnoresSnapshotsOutsideTradeWindow()
    {
        var trade = new Trade(
            "TEST",
            BaseTime.AddMinutes(5),
            100m,
            BaseTime.AddMinutes(10),
            104m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                BaseTime,
                high: 500m,
                low: 1m),

            StrongSnapshot(
                BaseTime.AddMinutes(5),
                high: 105m,
                low: 98m),

            StrongSnapshot(
                BaseTime.AddMinutes(10),
                high: 104m,
                low: 100m),

            StrongSnapshot(
                BaseTime.AddMinutes(15),
                high: 500m,
                low: 1m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            5m,
            result.MfePercent);

        Assert.Equal(
            -2m,
            result.MaePercent);
    }

    [Fact]
    public void Review_ReturnsEntryFilterFailure_WhenEntryStateDoesNotPermitLong()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            102m,
            1);

        var weakOneMinute = Bar(
            entryTime,
            100m,
            101m,
            99m,
            105m,
            104m,
            103m,
            100m,
            1m,
            45m,
            0m,
            1m);

        var weakFiveMinute = Bar(
            entryTime,
            100m,
            101m,
            99m,
            105m,
            104m,
            103m,
            100m,
            1m,
            45m,
            0m,
            1m);

        var exitOneMinute = Bar(
            exitTime,
            102m,
            103m,
            101m,
            100m,
            101m,
            99m,
            98m,
            2m,
            55m,
            2m,
            1m);

        var exitFiveMinute = Bar(
            exitTime,
            102m,
            103m,
            101m,
            100m,
            101m,
            99m,
            98m,
            2m,
            55m,
            2m,
            1m);

        var snapshots = new[]
        {
            Snapshot(
                entryTime,
                "TEST",
                weakOneMinute,
                weakFiveMinute),

            Snapshot(
                exitTime,
                "TEST",
                exitOneMinute,
                exitFiveMinute)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "ENTRY_FILTER_FAILURE",
            result.Classification);

        Assert.Contains(
            "V2 would not permit a fresh long",
            result.Finding);

        Assert.NotNull(result.MfePercent);
        Assert.NotNull(result.MaePercent);
    }

    [Fact]
    public void Review_ReturnsExitFailure_WhenLosingTradeEndsWithHardExit()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            110m,
            exitTime,
            100m,
            1);

        var entry = StrongSnapshot(
            entryTime,
            close: 110m,
            high: 111m,
            low: 109m);

        var exitFiveMinute = Bar(
            exitTime,
            100m,
            101m,
            99m,
            105m,
            103m,
            104m,
            106m,
            2m,
            45m,
            -1m,
            0m);

        var exitOneMinute = Bar(
            exitTime,
            99m,
            100m,
            98m,
            100m,
            100m,
            102m,
            104m,
            2m,
            42m,
            -1m,
            0m);

        var snapshots = new[]
        {
            entry,

            Snapshot(
                exitTime,
                "TEST",
                exitOneMinute,
                exitFiveMinute)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "EXIT_FAILURE",
            result.Classification);

        Assert.Contains(
            "V2 identifies a hard-exit condition",
            result.Finding);

        Assert.True(
            result.MfePercent.HasValue);

        Assert.True(
            result.MaePercent.HasValue);
    }

    [Fact]
    public void Review_ReturnsEarlyExit_WhenProfitWasMuchSmallerThanMfe()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            102m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                entryTime,
                close: 100m,
                high: 100m,
                low: 99m),

            StrongSnapshot(
                entryTime.AddMinutes(1),
                close: 105m,
                high: 110m,
                low: 100m),

            StrongSnapshot(
                exitTime,
                close: 102m,
                high: 103m,
                low: 101m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "EARLY_EXIT",
            result.Classification);

        Assert.Equal(
            10m,
            result.MfePercent);

        Assert.Equal(
            -1m,
            result.MaePercent);

        Assert.Equal(
            2m,
            trade.PnlPercent);
    }

    [Fact]
    public void Review_ReturnsValidate_WhenTradeHasNoObviousV2Defect()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            103m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                entryTime,
                close: 100m,
                high: 100m,
                low: 99m),

            StrongSnapshot(
                entryTime.AddMinutes(1),
                close: 103m,
                high: 104m,
                low: 101m),

            StrongSnapshot(
                exitTime,
                close: 102m,
                high: 103m,
                low: 101m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            "VALIDATE",
            result.Classification);

        Assert.Equal(
            "No obvious V2 defect from available snapshots; retain for validation.",
            result.Finding);

        Assert.Equal(
            4m,
            result.MfePercent);

        Assert.Equal(
            -1m,
            result.MaePercent);
    }

    [Fact]
    public void Review_UsesChronologicalSnapshots_NotInputOrder()
    {
        var entryTime = BaseTime;
        var middleTime = BaseTime.AddMinutes(1);
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            103m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                exitTime,
                close: 103m,
                high: 104m,
                low: 102m),

            StrongSnapshot(
                middleTime,
                close: 102m,
                high: 108m,
                low: 98m),

            StrongSnapshot(
                entryTime,
                close: 100m,
                high: 101m,
                low: 99m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            8m,
            result.MfePercent);

        Assert.Equal(
            -2m,
            result.MaePercent);
    }

    [Fact]
    public void Review_IncludesBoundarySnapshots()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(2);

        var trade = new Trade(
            "TEST",
            entryTime,
            100m,
            exitTime,
            103m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                entryTime,
                close: 100m,
                high: 105m,
                low: 99m),

            StrongSnapshot(
                exitTime,
                close: 103m,
                high: 106m,
                low: 98m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Equal(
            6m,
            result.MfePercent);

        Assert.Equal(
            -2m,
            result.MaePercent);
    }

    [Fact]
    public void Review_HandlesZeroEntryPriceWithoutInvalidMfeOrMae()
    {
        var entryTime = BaseTime;
        var exitTime = BaseTime.AddMinutes(1);

        var trade = new Trade(
            "TEST",
            entryTime,
            0m,
            exitTime,
            1m,
            1);

        var snapshots = new[]
        {
            StrongSnapshot(
                entryTime,
                close: 0m,
                high: 1m,
                low: -1m),

            StrongSnapshot(
                exitTime,
                close: 1m,
                high: 2m,
                low: 0m)
        };

        var result =
            new TradeAnalyzer().Review(
                trade,
                snapshots,
                new V2SignalEngine());

        Assert.Null(result.MfePercent);
        Assert.Null(result.MaePercent);
    }
}