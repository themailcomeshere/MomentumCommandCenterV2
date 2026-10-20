using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

/// <summary>
/// TESTS: ThinkorswimOhlcvStrategyReportReaderTests
/// PURPOSE: Protects the owned MCC strategy-report payload and rejects ambiguous legacy exports.
/// WHEN CALLED: Runs with ingestion tests before a report is normalized for replay.
/// </summary>
public sealed class ThinkorswimOhlcvStrategyReportReaderTests
{
    /// <summary>
    /// TEST: ReadParsesOwnedPayloadWithActualVolume
    /// PURPOSE: Confirms the v1 payload preserves timestamp, aggregation, OHLC, and volume.
    /// WHEN CALLED: Runs against a representative strategy-report envelope.
    /// </summary>
    [Fact]
    public void ReadParsesOwnedPayloadWithActualVolume()
    {
        const string report = "Strategy report\nSymbol: XRPN\nWork Time: 10/19/26\n\nId;Strategy;Side;Amount;Price;Date/Time;Trade P/L;P/L;Position;\n" +
            "1;MCC_OHLCV|v1|XRPN|1m|2026-10-19T09:31:00-04:00|14.30|14.55|14.20|14.50|123456;Buy to Open;1;14.50;10/19/26 9:32 AM;;;;\n";

        var bars = new ThinkorswimOhlcvStrategyReportReader().Read(new StringReader(report), "XRPN", BarTimeframe.OneMinute);

        var bar = Assert.Single(bars);
        Assert.Equal(123456m, bar.Volume);
        Assert.Equal(DateTimeOffset.Parse("2026-10-19T09:31:00-04:00"), bar.Timestamp);
    }

    /// <summary>
    /// TEST: ReadRejectsLegacyStrategyReportWithoutIdentifiableVolume
    /// PURPOSE: Ensures the existing kg_EveryTickSOHLCP fixture cannot be treated as complete OHLCV.
    /// WHEN CALLED: Runs against the current XRPN legacy export shape.
    /// </summary>
    [Fact]
    public void ReadRejectsLegacyStrategyReportWithoutIdentifiableVolume()
    {
        using var report = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_XRPN_101926.csv"));

        var exception = Assert.Throws<FormatException>(() => new ThinkorswimOhlcvStrategyReportReader().Read(report));

        Assert.Contains("no complete", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST: ReadParsesObservedWrappedPayloadAndIgnoresOtherStrategies
    /// PURPOSE: Confirms the observed Thinkorswim wrapper, thousands separators, and mixed report rows are handled.
    /// WHEN CALLED: Runs against the newly captured XRPN report.
    /// </summary>
    [Fact]
    public void ReadParsesObservedWrappedPayloadAndIgnoresOtherStrategies()
    {
        using var report = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_XRPN_101926_2115.csv"));

        var bars = new ThinkorswimOhlcvStrategyReportReader().Read(report, "XRPN", BarTimeframe.OneMinute);

        Assert.Equal(2, bars.Count);
        Assert.Equal(2m, bars[0].Volume);
        Assert.Equal(3170m, bars[1].Volume);
        Assert.Equal(TimeSpan.Zero, bars[0].Timestamp.Offset);
    }

    /// <summary>
    /// TEST: ReadRejectsLatestLegacyOnlyReport
    /// PURPOSE: Confirms a larger report is not accepted merely because it has many strategy rows.
    /// WHEN CALLED: Runs against the latest XRPN upload, which contains no MCC payload rows.
    /// </summary>
    [Fact]
    public void ReadRejectsLatestLegacyOnlyReport()
    {
        using var report = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_XRPN_101926_2127.csv"));

        Assert.Throws<FormatException>(() => new ThinkorswimOhlcvStrategyReportReader().Read(report));
    }

    /// <summary>
    /// TEST: ReadParsesLatestMccOnlyReport
    /// PURPOSE: Confirms the latest exported MCC-only report is accepted with identifiable volume.
    /// WHEN CALLED: Runs against the newly uploaded single-row XRPN export.
    /// </summary>
    [Fact]
    public void ReadParsesLatestMccOnlyReport()
    {
        using var report = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_XRPN_101926_2147.csv"));

        var bars = new ThinkorswimOhlcvStrategyReportReader().Read(report, "XRPN", BarTimeframe.OneMinute);

        var bar = Assert.Single(bars);
        Assert.Equal(15.43m, bar.Open);
        Assert.Equal(1m, bar.Volume);
    }

    /// <summary>
    /// TEST: ReadRejectsTransportOnlyReport
    /// PURPOSE: Confirms a transport close row without an MCC payload is not market data.
    /// WHEN CALLED: Runs against the report produced when the close order was declared first.
    /// </summary>
    [Fact]
    public void ReadRejectsTransportOnlyReport()
    {
        using var report = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_XRPN_102026_901.csv"));

        Assert.Throws<FormatException>(() => new ThinkorswimOhlcvStrategyReportReader().Read(report));
    }

    /// <summary>
    /// TEST: ReadRejectsMalformedAndDuplicatePayloads
    /// PURPOSE: Ensures malformed OHLC and duplicate timestamps are rejected without repair.
    /// WHEN CALLED: Runs against parser validation cases.
    /// </summary>
    [Theory]
    [InlineData("2026-10-19T09:31:00-04:00|10|9|8|8.5|100")]
    [InlineData("2026-10-19T09:31:00-04:00|10|11|9|10.5|-1")]
    [InlineData("2026-10-19T09:31:00|10|11|9|10.5|100")]
    public void ReadRejectsMalformedAndDuplicatePayloads(string payloadFields)
    {
        var report = Report($"MCC_OHLCV|v1|XRPN|1m|{payloadFields}", $"MCC_OHLCV|v1|XRPN|1m|{payloadFields}");

        Assert.Throws<FormatException>(() => new ThinkorswimOhlcvStrategyReportReader().Read(new StringReader(report)));
    }

    /// <summary>
    /// METHOD: Report
    /// PURPOSE: Builds the smallest valid Thinkorswim report envelope for parser tests.
    /// WHEN CALLED: Used by malformed and duplicate-row tests.
    /// </summary>
    private static string Report(params string[] payloads) =>
        "Strategy report\nSymbol: XRPN\nWork Time: 10/19/26\n\nId;Strategy;Side;Amount;Price;Date/Time;Trade P/L;P/L;Position;\n" +
        string.Join("\n", payloads.Select((payload, index) => $"{index + 1};{payload};Buy to Open;1;14.50;10/19/26 9:32 AM;;;;"));
}
