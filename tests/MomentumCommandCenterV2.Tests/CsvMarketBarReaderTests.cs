using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

/// <summary>
/// TESTS: CsvMarketBarReaderTests
/// PURPOSE: Protects the documented CSV input contract, including explicit timezone offsets and invalid-row handling.
/// WHEN CALLED: Runs in the full xUnit suite before changes to file-based ingestion are accepted.
/// </summary>
public sealed class CsvMarketBarReaderTests
{
    /// <summary>
    /// TEST: ReadParsesBarsAndPreservesTimestampOffset
    /// PURPOSE: Confirms valid 1M/5M rows are parsed with the original offset.
    /// WHEN CALLED: Runs with the market-data adapter tests.
    /// </summary>
    [Fact]
    public void ReadParsesBarsAndPreservesTimestampOffset()
    {
        const string csv = "Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume\n" +
            "DEMO,5m,2026-10-16T13:30:00-04:00,10,11,9,10.5,1000\n" +
            "DEMO,1m,2026-10-16T13:31:00-04:00,10.5,10.8,10.4,10.7,200\n";

        using var reader = new StringReader(csv);
        var bars = new CsvMarketBarReader().Read(reader);

        Assert.Equal(2, bars.Count);
        Assert.Equal(BarTimeframe.FiveMinute, bars[0].Timeframe);
        Assert.Equal(TimeSpan.FromHours(-4), bars[0].Timestamp.Offset);
        Assert.Equal(10.7m, bars[1].Close);
    }

    /// <summary>
    /// TEST: ReadRejectsTimestampWithoutOffset
    /// PURPOSE: Ensures local-machine timezone is never silently assumed.
    /// WHEN CALLED: Runs with the market-data adapter tests.
    /// </summary>
    [Fact]
    public void ReadRejectsTimestampWithoutOffset()
    {
        const string csv = "Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume\n" +
            "DEMO,1m,2026-10-16T13:31:00,10,11,9,10.5,1000\n";
        using var reader = new StringReader(csv);
        Assert.Throws<FormatException>(() => new CsvMarketBarReader().Read(reader));
    }

    /// <summary>
    /// TEST: ReadRejectsMalformedOhlc
    /// PURPOSE: Ensures invalid bar relationships fail before replay can consume them.
    /// WHEN CALLED: Runs with the market-data adapter tests.
    /// </summary>
    [Fact]
    public void ReadRejectsMalformedOhlc()
    {
        const string csv = "Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume\n" +
            "DEMO,1m,2026-10-16T13:31:00-04:00,10,9,8,10.5,1000\n";
        using var reader = new StringReader(csv);
        Assert.Throws<FormatException>(() => new CsvMarketBarReader().Read(reader));
    }
}
