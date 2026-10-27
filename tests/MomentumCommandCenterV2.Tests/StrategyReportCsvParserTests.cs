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

    [Fact]
    public void ParsesRealThinkorswimSemicolonStrategyReportIntoParityTemplate()
    {
        const string csv = "Strategy report\\nSymbol: XRPN\\nWork Time: 10/19/26 10:30 AM - 10/19/26 10:50 AM\\n\\nId;Strategy;Side;Amount;Price;Date/Time;Trade P/L;P/L;Position;\\n" +
            "1;MCC_OHLCV_V1(MCC_OHLCV|v1|XRPN|1m|1,792,430,880,000|14.85|14.88|14.81|14.87|8,210);Buy to Open;1.0;$14.83;10/19/26 10:30 AM;;$0.00;0.0;\\n";

        var result = StrategyReportCsvParser.Parse(csv);

        Assert.Equal(1, result.ParsedRowCount);
        Assert.Single(result.ParityRows);
        var parity = result.ParityRows[0];
        Assert.Equal(17, parity.Count);
        Assert.Equal("1", parity["SourceRow"]);
        Assert.Equal("XRPN", parity["Symbol"]);
        Assert.Equal("1m", parity["Timeframe"]);
        Assert.Equal("14.85", parity["PayloadOpen"]);
        Assert.Equal("14.87", parity["PayloadClose"]);
        Assert.Equal("8210", parity["PayloadVolume"]);
        Assert.Equal("", parity["ChartTimestamp"]);
        Assert.Contains("order-event timestamp", parity["ChartTimestampMeaning"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("2026-10-19T13:28:00.0000000Z", DateTimeOffset.Parse(parity["PayloadTimestampUtc"]).UtcDateTime.ToString("O"));
    }

    [Fact]
    public void DoesNotInventParityRowsWhenNoExplicitPayloadExists()
    {
        const string csv = "Strategy report\\nSymbol: TTE\\nId;Strategy;Side;Amount;Price;Date/Time;P/L;\\n1;MyStrategy;Buy to Open;1;$90.00;10/27/26 9:30 AM;$0.00;\\n";
        var result = StrategyReportCsvParser.Parse(csv);
        Assert.Empty(result.ParityRows);
        Assert.Contains("no explicit MCC_OHLCV v1 payload", result.Interpretation, StringComparison.OrdinalIgnoreCase);
    }
}
