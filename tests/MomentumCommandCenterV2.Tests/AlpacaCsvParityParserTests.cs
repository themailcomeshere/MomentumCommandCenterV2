using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class AlpacaCsvParityParserTests
{
    [Fact]
    public void MapsAlpacaOhlcvToAllSeventeenParityColumnsWithoutInventingChartValues()
    {
        const string csv = "timestamp,open,high,low,close,volume\n2026-10-26T13:30:00Z,92.85,93.05,92.82,93.01,1612\n";
        var rows = AlpacaCsvParityParser.Parse(csv, "TTE", "5m");
        var row = Assert.Single(rows);
        Assert.Equal(17, row.Count);
        Assert.Equal("TTE", row["Symbol"]);
        Assert.Equal("5m", row["Timeframe"]);
        Assert.Equal("2026-10-26T13:30:00.0000000Z", DateTimeOffset.Parse(row["PayloadTimestampUtc"]).UtcDateTime.ToString("O"));
        Assert.Equal("1612", row["PayloadVolume"]);
        Assert.Equal("", row["ChartTimestamp"]);
        Assert.Equal("", row["ChartClose"]);
    }

    [Fact]
    public void AcceptsSymbolAndTimeframeColumnsAndQuotedCsvFields()
    {
        const string csv = "symbol,timeframe,datetime,open,high,low,close,volume,note\nTTE,1m,2026-10-26T13:30:00Z,92.85,93.05,92.82,93.01,1,\"IEX, test\"\n";
        var row = Assert.Single(AlpacaCsvParityParser.Parse(csv));
        Assert.Equal("TTE", row["Symbol"]);
        Assert.Equal("1m", row["Timeframe"]);
    }

    [Fact]
    public void RejectsCsvWithoutRequiredOhlcvHeader()
    {
        Assert.Throws<FormatException>(() => AlpacaCsvParityParser.Parse("symbol,price\nTTE,92.85\n", "TTE", "1m"));
    }

    [Fact]
    public void ParsesCheckedInAlpacaHistoricalBarsJsonEvenWhenNamedCsv()
    {
        const string json = """
            {
              "symbol":"TTE",
              "timeframe":"5m",
              "provider":"Alpaca",
              "feed":"iex",
              "bars":[
                {"symbol":"TTE","timestamp":"2026-10-26T13:30:00+00:00","open":92.85,"high":93.05,"low":92.82,"close":93.01,"volume":1612}
              ]
            }
            """;
        var row = Assert.Single(AlpacaCsvParityParser.Parse(json));
        Assert.Equal("TTE", row["Symbol"]);
        Assert.Equal("5m", row["Timeframe"]);
        Assert.Equal("93.01", row["PayloadClose"]);
        Assert.Equal("1612", row["PayloadVolume"]);
        Assert.Equal("", row["ChartClose"]);
    }
}
