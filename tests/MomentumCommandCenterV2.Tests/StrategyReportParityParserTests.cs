using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class StrategyReportParityParserTests
{
    [Fact]
    public void Parse_ExtractsPayloadRowsAndIgnoresTransportCloseRows()
    {
        const string report = """
            Strategy report
            Symbol: XRPN
            Work Time: 10/19/26 6:32 AM - 10/19/26 1:02 PM


            Id;Strategy;Side;Amount;Price;Date/Time;Trade P/L;P/L;Position;
            1;MCC_OHLCV_V1(MCC_OHLCV_TRANSPORT_CLOSE);Sell to Close;-1.0;$14.37;10/19/26 6:32 AM;$0.00;($0.18);-1.0;
            2;MCC_OHLCV_V1(MCC_OHLCV|v1|XRPN|1m|1,792,416,600,000|15.06|15.07|14.26|14.5501|62,346);Buy to Open;1.0;$14.37;10/19/26 6:32 AM;;$0.00;0.0;
            3;MCC_OHLCV_V1(MCC_OHLCV|v1|XRPN|1m|1,792,416,660,000|14.51|14.6|14.3|14.37|14,623);Buy to Open;1.0;$14.55;10/19/26 6:33 AM;;$0.00;0.0;
            """;

        var rows = StrategyReportParityParser.Parse(report);

        Assert.Equal(2, rows.Count);
        Assert.Equal("XRPN", rows[0].Symbol);
        Assert.Equal("1m", rows[0].Timeframe);
        Assert.Equal(15.06m, rows[0].Open);
        Assert.Equal(15.07m, rows[0].High);
        Assert.Equal(14.26m, rows[0].Low);
        Assert.Equal(14.5501m, rows[0].Close);
        Assert.Equal(62346L, rows[0].Volume);
        Assert.True(rows[0].Timestamp < rows[1].Timestamp);
    }

    [Fact]
    public void ToParityCsv_WritesStableNormalizedColumns()
    {
        var row = new StrategyReportParityRow(
            "AAPL",
            "5m",
            DateTimeOffset.Parse("2026-10-27T14:30:00Z"),
            10m,
            12m,
            9m,
            11m,
            1000);

        var csv = StrategyReportParityParser.ToParityCsv(new[] { row });

        Assert.StartsWith("Symbol;Timeframe;TimestampUtc;Open;High;Low;Close;Volume", csv);
        Assert.Contains("AAPL;5m;2026-10-27T14:30:00.0000000+00:00;10;12;9;11;1000", csv);
    }

    [Fact]
    public void Parse_RejectsMissingStrategyReportHeader()
    {
        Assert.Throws<FormatException>(() => StrategyReportParityParser.Parse("not a strategy report"));
    }

    [Fact]
    public void Parse_RejectsInvalidOhlcBounds()
    {
        const string report = """
            Id;Strategy;Side;Amount;Price;Date/Time;Trade P/L;P/L;Position;
            1;MCC_OHLCV_V1(MCC_OHLCV|v1|AAPL|1m|1,792,416,600,000|10|9|8|9.5|100);Buy to Open;1.0;$9.50;10/19/26 6:32 AM;;$0.00;0.0;
            """;

        Assert.Throws<FormatException>(() => StrategyReportParityParser.Parse(report));
    }
}
