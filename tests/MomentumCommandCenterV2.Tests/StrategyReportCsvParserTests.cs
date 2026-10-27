using MomentumCommandCenterV2.API.StrategyReports;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class StrategyReportCsvParserTests
{
    [Fact]
    public void ParsesQuotedFieldsAndPreservesHeaders()
    {
        const string csv = "Symbol,Time,Event,Note\r\nTTE,2026-10-27 15:55:00,SELL,\"comma, inside\"\r\n";
        var result = StrategyReportCsvParser.Parse(csv);
        Assert.Equal(1, result.ParsedRowCount);
        Assert.Equal("TTE", result.PreviewRows[0]["Symbol"]);
        Assert.Equal("comma, inside", result.PreviewRows[0]["Note"]);
        Assert.Equal("Time", result.LikelyTimestampColumn);
        Assert.Equal("Symbol", result.LikelySymbolColumn);
        Assert.Equal("Event", result.LikelyEventColumn);
    }

    [Fact]
    public void RejectsUnterminatedQuotedField()
    {
        Assert.Throws<FormatException>(() => StrategyReportCsvParser.Parse("Symbol,Note\nTTE,\"broken"));
    }

    [Fact]
    public void WarnsAndSkipsRowsWithWrongFieldCount()
    {
        var result = StrategyReportCsvParser.Parse("Symbol,Price\nTTE,90\nFMS\n");
        Assert.Equal(1, result.ParsedRowCount);
        Assert.Single(result.Warnings);
    }
}
