using MomentumCommandCenterV2.Core;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class SchwabTradeImporterTests
{
    [Fact]
    public void Import_UsesFillPrice_NotLimitPrice()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Buy,30.30,LIMIT,30.17,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.42,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Single(result.CompletedTrades);

        var trade = result.CompletedTrades[0];

        Assert.Equal("NBIL", trade.Symbol);
        Assert.Equal(30.17m, trade.EntryPrice);
        Assert.Equal(30.42m, trade.ExitPrice);
        Assert.Equal(1, trade.Quantity);
    }

    [Fact]
    public void Import_MatchesBuyAndSellIntoCompletedTrade()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Buy,30.30,LIMIT,30.17,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.42,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Single(result.CompletedTrades);
        Assert.Empty(result.OpenPositions);
        Assert.Empty(result.UnmatchedOrders);

        var trade = result.CompletedTrades[0];

        Assert.Equal("NBIL", trade.Symbol);
        Assert.Equal(30.17m, trade.EntryPrice);
        Assert.Equal(30.42m, trade.ExitPrice);
        Assert.Equal(1, trade.Quantity);
        Assert.Equal(0.25m, trade.Pnl);
    }

    [Fact]
    public void Import_TreatsOpenBuyAsOpenPosition()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            SST,Filled,Buy,5.10,LIMIT,5.10,Yes,3:08 PM 09/08/2026,3:08 PM 09/08/2026
            SST,Open,Sell,5.11,LIMIT,-,No,3:09 PM 09/08/2026,3:09 PM 09/08/2026
            SST,Canceled,Sell,6.00,LIMIT,-,No,3:10 PM 09/08/2026,3:10 PM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Empty(result.CompletedTrades);
        Assert.Single(result.OpenPositions);
        Assert.Empty(result.UnmatchedOrders);

        var position = result.OpenPositions[0];

        Assert.Equal("SST", position.Symbol);
        Assert.Equal(5.10m, position.EntryPrice);
        Assert.Equal(1, position.Quantity);
    }

    [Fact]
    public void Import_IgnoresCanceledAndOpenOrders()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Canceled,Buy,30.30,LIMIT,-,No,9:30 AM 09/08/2026,9:30 AM 09/08/2026
            NBIL,Open,Buy,30.20,LIMIT,-,No,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            NBIL,Filled,Buy,30.30,LIMIT,30.17,Yes,9:32 AM 09/08/2026,9:32 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.42,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Single(result.CompletedTrades);
        Assert.Empty(result.OpenPositions);
        Assert.Empty(result.UnmatchedOrders);
    }

    [Fact]
    public void Import_ProcessesMultipleTradesForSameSymbol()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Buy,30.00,LIMIT,30.00,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.50,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            NBIL,Filled,Buy,31.00,LIMIT,31.00,Yes,10:01 AM 09/08/2026,10:01 AM 09/08/2026
            NBIL,Filled,Sell,31.75,LIMIT,31.75,Yes,10:05 AM 09/08/2026,10:05 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Equal(2, result.CompletedTrades.Count);
        Assert.Empty(result.OpenPositions);
        Assert.Empty(result.UnmatchedOrders);

        Assert.Equal(30.00m, result.CompletedTrades[0].EntryPrice);
        Assert.Equal(30.50m, result.CompletedTrades[0].ExitPrice);

        Assert.Equal(31.00m, result.CompletedTrades[1].EntryPrice);
        Assert.Equal(31.75m, result.CompletedTrades[1].ExitPrice);
    }

    [Fact]
    public void Import_ProcessesMultipleSymbolsIndependently()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Buy,30.00,LIMIT,30.00,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            SST,Filled,Buy,5.10,LIMIT,5.10,Yes,9:32 AM 09/08/2026,9:32 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.50,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            SST,Filled,Sell,5.25,LIMIT,5.25,Yes,9:36 AM 09/08/2026,9:36 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Equal(2, result.CompletedTrades.Count);
        Assert.Empty(result.OpenPositions);

        var nbilTrade =
            result.CompletedTrades.Single(
                trade => trade.Symbol == "NBIL");

        var sstTrade =
            result.CompletedTrades.Single(
                trade => trade.Symbol == "SST");

        Assert.Equal(30.00m, nbilTrade.EntryPrice);
        Assert.Equal(30.50m, nbilTrade.ExitPrice);

        Assert.Equal(5.10m, sstTrade.EntryPrice);
        Assert.Equal(5.25m, sstTrade.ExitPrice);
    }

    [Fact]
    public void Import_ReturnsUnmatchedSellWhenNoPriorBuyExists()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Sell,30.50,LIMIT,30.50,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Empty(result.CompletedTrades);
        Assert.Empty(result.OpenPositions);

        Assert.Single(result.UnmatchedOrders);

        var unmatched = result.UnmatchedOrders[0];

        Assert.Equal("NBIL", unmatched.Symbol);
        Assert.Equal("Sell", unmatched.Action);
        Assert.Equal(30.50m, unmatched.FillPrice);
        Assert.Equal(1, unmatched.Quantity);
    }

    [Fact]
    public void Import_OrdersCompletedTradesByEntryTime()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            SST,Filled,Buy,5.10,LIMIT,5.10,Yes,10:05 AM 09/08/2026,10:05 AM 09/08/2026
            SST,Filled,Sell,5.20,LIMIT,5.20,Yes,10:10 AM 09/08/2026,10:10 AM 09/08/2026
            NBIL,Filled,Buy,30.00,LIMIT,30.00,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            NBIL,Filled,Sell,30.50,LIMIT,30.50,Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Equal(2, result.CompletedTrades.Count);

        Assert.Equal("NBIL", result.CompletedTrades[0].Symbol);
        Assert.Equal("SST", result.CompletedTrades[1].Symbol);
    }

    [Fact]
    public void Import_RejectsEmptyCsv()
    {
        var importer = new SchwabTradeImporter();

        Assert.Throws<ArgumentException>(
            () => importer.Import(string.Empty));
    }

    [Fact]
    public void Import_RejectsCsvWithNoDataRows()
    {
        const string csv =
            "Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)";

        var importer = new SchwabTradeImporter();

        Assert.Throws<FormatException>(
            () => importer.Import(csv));
    }

    [Fact]
    public void Import_RejectsMissingRequiredColumn()
    {
        const string csv = """
        Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average
        NBIL,Filled,Buy,30.30,LIMIT,30.17,Yes
        """;

        var importer = new SchwabTradeImporter();

        var exception =
            Assert.Throws<FormatException>(
                () => importer.Import(csv));

        Assert.Contains(
            "Time and Date(ET)",
            exception.Message);
    }

    [Fact]
    public void Import_RejectsInvalidFillPrice()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            NBIL,Filled,Buy,30.30,LIMIT,NOT_A_PRICE,Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
            """;

        var importer = new SchwabTradeImporter();

        var exception =
            Assert.Throws<FormatException>(
                () => importer.Import(csv));

        Assert.Contains(
            "Invalid Schwab fill price",
            exception.Message);
    }

    [Fact]
    public void Import_HandlesQuotedCsvFields()
    {
        const string csv = """
            Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
            "NBIL","Filled","Buy","30.30","LIMIT","30.17","Yes","9:31 AM 09/08/2026","9:31 AM 09/08/2026"
            "NBIL","Filled","Sell","30.50","LIMIT","30.42","Yes","9:35 AM 09/08/2026","9:35 AM 09/08/2026"
            """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Single(result.CompletedTrades);

        var trade = result.CompletedTrades[0];

        Assert.Equal("NBIL", trade.Symbol);
        Assert.Equal(30.17m, trade.EntryPrice);
        Assert.Equal(30.42m, trade.ExitPrice);
    }

    [Fact]
    public void Import_HandlesDollarSignsAndThousandsSeparators()
    {
        const string csv = """
        Symbol,Status,Action,Price,Timing,Fill Price,Fill Price is Average,Time and Date(ET),Last Activity Date(ET)
        TEST,Filled,Buy,"$1,234.56",LIMIT,"$1,234.56",Yes,9:31 AM 09/08/2026,9:31 AM 09/08/2026
        TEST,Filled,Sell,"$1,250.00",LIMIT,"$1,250.00",Yes,9:35 AM 09/08/2026,9:35 AM 09/08/2026
        """;

        var importer = new SchwabTradeImporter();

        var result = importer.Import(csv);

        Assert.Single(result.CompletedTrades);

        var trade = result.CompletedTrades[0];

        Assert.Equal(1234.56m, trade.EntryPrice);
        Assert.Equal(1250.00m, trade.ExitPrice);
    }
}