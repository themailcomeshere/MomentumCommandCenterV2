using System.Text.Json;
using MomentumCommandCenterV2.API.StrategyReports;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class StrategyReportAtrV2Tests
{
    [Fact]
    public void ParsesActualFullOneMinuteThinkorswimExport()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_EVR_11032026_FULL_1M.csv"));
        var result = StrategyReportAtrV2Parser.Parse(reader.ReadToEnd());

        Assert.Equal("EVR", result.Symbol);
        Assert.Equal("1m", result.Timeframe);
        Assert.Equal(346, result.ReportDataRowCount);
        Assert.Equal(346, result.AtrObservationCount);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T14:04:00Z"), result.FirstTimestampUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:58:00Z"), result.LastTimestampUtc);
        Assert.Equal(48, result.GapCount);
        Assert.Equal(303.26m, result.Observations[0].Open);
        Assert.Equal(305.76m, result.Observations[0].Close);
        Assert.Equal(367m, result.Observations[0].Volume);
        Assert.Equal(1.0681m, result.Observations[0].Atr);
        Assert.Equal(0.3839m, result.Observations[^1].Atr);
    }

    [Fact]
    public void ParsesActualFullFiveMinuteThinkorswimExport()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_EVR_11032026_FULL_5M.csv"));
        var result = StrategyReportAtrV2Parser.Parse(reader.ReadToEnd());

        Assert.Equal("EVR", result.Symbol);
        Assert.Equal("5m", result.Timeframe);
        Assert.Equal(81, result.ReportDataRowCount);
        Assert.Equal(81, result.AtrObservationCount);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T14:05:00Z"), result.FirstTimestampUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-03T20:50:00Z"), result.LastTimestampUtc);
        Assert.Equal(1, result.GapCount);
        Assert.Equal(1.2482m, result.Observations[0].Atr);
        Assert.Equal(0.6656m, result.Observations[^1].Atr);
    }

    [Fact]
    public void ReportsGapsWithoutImputingRows()
    {
        const string report = "Strategy report\nSymbol: EVR\nId;Strategy;Side;Amount;Price;Date/Time;\n" +
            "1;MCC_OHLCV_ATR_V2(MCC_OHLCV_ATR|v2|EVR|1m|1793714400000|10|11|9|10.5|100|0.25);Buy to Open;\n" +
            "2;MCC_OHLCV_ATR_V2(MCC_OHLCV_ATR|v2|EVR|1m|1793714520000|10.5|11|10|10.8|200|0.30);Buy to Open;\n";
        var result = StrategyReportAtrV2Parser.Parse(report);
        Assert.Equal(2, result.AtrObservationCount);
        Assert.Single(result.GapSamples);
        Assert.Equal(TimeSpan.FromMinutes(2), result.GapSamples[0].Duration);
    }

    [Fact]
    public void RejectsDuplicateTimestampsAndMalformedAtrPayloads()
    {
        const string duplicate = "Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            "1;MCC_OHLCV_ATR|v2|EVR|1m|1793714400000|10|11|9|10.5|100|0.25;\n" +
            "2;MCC_OHLCV_ATR|v2|EVR|1m|1793714400000|10|11|9|10.5|100|0.25;\n";
        Assert.Throws<FormatException>(() => StrategyReportAtrV2Parser.Parse(duplicate));
        const string malformed = "Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            "1;MCC_OHLCV_ATR|v2|EVR|1m|1793714400000|10|11|9|10.5|100;\n";
        Assert.Throws<FormatException>(() => StrategyReportAtrV2Parser.Parse(malformed));
    }

    [Fact]
    public async Task AtrStoreIsIdempotentAndRetainsCorrectionsSeparatelyFromProviderBars()
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_TOS_ATR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-tos-atr-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "atr.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_TOS_ATR_STORE_PATH", path);
            var store = new JsonlThinkorswimAtrObservationStore();
            var item = new StrategyReportAtrObservation("2", "EVR", "5m",
                DateTimeOffset.Parse("2026-11-03T14:05:00Z"), 10m, 11m, 9m, 10.5m, 100m, 0.25m);
            var metadata = Metadata();
            Assert.Equal(new ThinkorswimAtrAppendResult(1, 0, 0),
                await store.AppendAsync(new[] { item }, metadata, CancellationToken.None));
            Assert.Equal(new ThinkorswimAtrAppendResult(0, 1, 0),
                await store.AppendAsync(new[] { item }, Metadata() with { ImportBatchId = "second-batch", ImportedAtUtc = DateTimeOffset.UtcNow }, CancellationToken.None));
            var corrected = item with { Atr = 0.26m };
            Assert.Equal(new ThinkorswimAtrAppendResult(1, 0, 1),
                await store.AppendAsync(new[] { corrected }, metadata, CancellationToken.None));

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(2, lines.Length);
            var rows = await store.GetLatestAsync("EVR", "5m", item.SourceTimestampUtc, item.SourceTimestampUtc, 10, CancellationToken.None);
            var row = Assert.Single(rows);
            Assert.Equal(0.26m, row.Observation.Atr);
            Assert.NotNull(row.SupersedesObservationId);
            Assert.Equal("America/Phoenix", row.Metadata.ChartTimezone);
            Assert.True(row.Metadata.ExtendedHours);
            Assert.True(row.Metadata.CorporateActionAdjustment);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_TOS_ATR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static StrategyReportAtrImportMetadata Metadata() => new(
        "test.csv", "MCC_OHLCV_ATR_V2", 14, "Wilder", "America/Phoenix",
        true, true, "batch-1", DateTimeOffset.UtcNow);
}
