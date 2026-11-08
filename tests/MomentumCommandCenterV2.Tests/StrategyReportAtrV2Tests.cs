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
        Assert.Equal("v2", result.PayloadVersion);
        Assert.Equal(692, result.ReportDataRowCount);
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
        Assert.Equal("v2", result.PayloadVersion);
        Assert.Equal(162, result.ReportDataRowCount);
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
        Assert.Equal(TimeSpan.FromMinutes(1), result.GapSamples[0].Duration);
    }

    [Fact]
    public void DoesNotCountScheduledOvernightAndWeekendClosureAsIntradayGap()
    {
        var fridayLastBar = DateTimeOffset.Parse("2026-11-07T00:59:00Z").ToUnixTimeMilliseconds();
        var mondayFirstBar = DateTimeOffset.Parse("2026-11-09T09:00:00Z").ToUnixTimeMilliseconds();
        var report = $"Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            $"1;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|1m|{fridayLastBar}|10|11|9|10.5|100|0.25);Buy to Open;\n" +
            $"2;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|1m|{mondayFirstBar}|10.5|11|10|10.8|200|0.30);Buy to Open;\n";

        var result = StrategyReportAtrV2Parser.Parse(report);

        Assert.Equal(0, result.GapCount);
    }

    [Fact]
    public void DoesNotCountSameDateGapWhollyInsideScheduledOvernightClosure()
    {
        var midnight = DateTimeOffset.Parse("2026-11-05T05:00:00Z").ToUnixTimeMilliseconds(); // 00:00 ET
        var afterMidnight = DateTimeOffset.Parse("2026-11-05T05:10:00Z").ToUnixTimeMilliseconds(); // 00:10 ET
        var report = $"Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            $"1;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{midnight}|10|11|9|10.5|100|0.25);Buy to Open;\n" +
            $"2;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{afterMidnight}|10.5|11|10|10.8|200|0.30);Buy to Open;\n";

        var result = StrategyReportAtrV2Parser.Parse(report);

        Assert.Equal(0, result.GapCount);
    }

    [Fact]
    public void CountsSameDateGapThatCrossesIntoPremarketSession()
    {
        var afterMidnight = DateTimeOffset.Parse("2026-11-05T05:10:00Z").ToUnixTimeMilliseconds(); // 00:10 ET
        var premarketBar = DateTimeOffset.Parse("2026-11-05T14:05:00Z").ToUnixTimeMilliseconds(); // 09:05 ET
        var report = $"Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            $"1;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{afterMidnight}|10|11|9|10.5|100|0.25);Buy to Open;\n" +
            $"2;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{premarketBar}|10.5|11|10|10.8|200|0.30);Buy to Open;\n";

        var result = StrategyReportAtrV2Parser.Parse(report);

        Assert.Equal(1, result.GapCount);
    }

    [Fact]
    public void CountsGapWithinAfterHoursSession()
    {
        var afterHoursStart = DateTimeOffset.Parse("2026-11-04T21:00:00Z").ToUnixTimeMilliseconds(); // 16:00 ET
        var afterHoursLater = DateTimeOffset.Parse("2026-11-04T23:40:00Z").ToUnixTimeMilliseconds(); // 18:40 ET
        var report = $"Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            $"1;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{afterHoursStart}|10|11|9|10.5|100|0.25);Buy to Open;\n" +
            $"2;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{afterHoursLater}|10.5|11|10|10.8|200|0.30);Buy to Open;\n";

        var result = StrategyReportAtrV2Parser.Parse(report);

        Assert.Equal(1, result.GapCount);
    }

    [Fact]
    public void DetectsCorrectedV21PayloadVersion()
    {
        const string report = "Strategy report\nSymbol: EVR\nId;Strategy;Side;\n" +
            "1;MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|1m|1793714400000|10|11|9|10.5|100|0.25);Buy to Open;\n";
        var result = StrategyReportAtrV2Parser.Parse(report);
        Assert.Equal("v2.1", result.PayloadVersion);
        Assert.Single(result.Observations);
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

    [Fact]
    public void LegacyNovemberThirdOneMinuteExportDoesNotPassCorrectedAtrParity()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_EVR_11032026_FULL_1M.csv"));
        var parsed = StrategyReportAtrV2Parser.Parse(reader.ReadToEnd());
        var metadata = Metadata() with { StudyVersion = "MCC_OHLCV_ATR_V2.0" };
        var stored = parsed.Observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored);

        Assert.Equal("NOT_MATCH", result.Status);
        Assert.Equal(346, result.SourceObservationCount);
        Assert.Equal(247, result.ComparedBarCount);
        Assert.True(result.MaximumAbsoluteError > 0.14m);
        Assert.InRange(result.MeanAbsoluteError!.Value, 0.063m, 0.064m);
        Assert.True(result.TimestampGapCount > 0);
    }

    [Fact]
    public void MatchingAtrValuesWithMissingTimestampsDoNotReturnMatch()
    {
        var metadata = Metadata();
        var start = DateTimeOffset.Parse("2026-11-03T14:00:00Z");
        var observations = Enumerable.Range(0, 130).Select(index => new StrategyReportAtrObservation(
            index.ToString(), "EVR", "1m", start.AddMinutes(index + (index >= 110 ? 1 : 0)),
            100m, 101m, 99m, 100m, 1000m, 2m)).ToArray();
        var stored = observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored);

        Assert.Equal("GAPS_PRESENT", result.Status);
        Assert.Equal(1, result.TimestampGapCount);
        Assert.Equal(31, result.ComparedBarCount);
        Assert.Equal(0m, result.MaximumAbsoluteError);
        var gap = Assert.Single(result.TimestampGaps!);
        Assert.Equal(start.AddMinutes(109), gap.PreviousBarAtUtc);
        Assert.Equal(start.AddMinutes(111), gap.CurrentBarAtUtc);
        Assert.Equal(start.AddMinutes(110), gap.ExpectedNextBarAtUtc);
        Assert.Equal(1, gap.MissingIntervalCount);
        Assert.Equal(TimeSpan.FromMinutes(1), gap.GapDuration);
        Assert.Equal(start.AddMinutes(110), gap.FirstExpectedMissingBarAtUtc);
        Assert.Equal(start.AddMinutes(110), gap.LastExpectedMissingBarAtUtc);
    }

    [Theory]
    [InlineData("EVR", "2026-11-05T05:10:00Z", "2026-11-05T14:05:00Z", 61, "2026-11-05T09:00:00Z", "2026-11-05T14:00:00Z")]
    [InlineData("EVR", "2026-11-05T14:05:00Z", "2026-11-05T14:30:00Z", 4, "2026-11-05T14:10:00Z", "2026-11-05T14:25:00Z")]
    [InlineData("CBOE", "2026-11-04T21:00:00Z", "2026-11-04T23:40:00Z", 31, "2026-11-04T21:05:00Z", "2026-11-04T23:35:00Z")]
    public void CountsOnlyExpectedEthSlotsAndExcludesScheduledOvernightClosure(
        string symbol,
        string previousText,
        string currentText,
        int expectedMissingCount,
        string firstMissingText,
        string lastMissingText)
    {
        var metadata = Metadata();
        var previous = DateTimeOffset.Parse(previousText);
        var current = DateTimeOffset.Parse(currentText);
        var timestamps = new[] { previous, current }
            .Concat(Enumerable.Range(0, 128).Select(index => current.AddMinutes(5 * (index + 1))))
            .ToArray();
        var observations = timestamps.Select((timestamp, index) => new StrategyReportAtrObservation(
            index.ToString(), symbol, "5m", timestamp, 100m, 101m, 99m, 100m, 1000m, 2m)).ToArray();
        var stored = observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored, warmupBars: 15, comparisonWindowBars: 130);

        Assert.Equal("MATCH", result.Status);
        Assert.Equal(0, result.TimestampGapCount);
        var gap = Assert.Single(result.VerifiedNoBarIntervals!);
        Assert.Equal(1, result.VerifiedNoBarIntervalCount);
        Assert.Equal(expectedMissingCount, gap.MissingIntervalCount);
        Assert.Equal(DateTimeOffset.Parse(firstMissingText), gap.FirstExpectedMissingBarAtUtc);
        Assert.Equal(DateTimeOffset.Parse(lastMissingText), gap.LastExpectedMissingBarAtUtc);
    }

    [Theory]
    [InlineData("EVR", "2026-11-05T05:10:00Z", "2026-11-05T14:05:00Z")]
    [InlineData("EVR", "2026-11-05T14:05:00Z", "2026-11-05T14:30:00Z")]
    [InlineData("CBOE", "2026-11-04T21:00:00Z", "2026-11-04T23:40:00Z")]
    public void OperatorConfirmedNoBarIntervalsDoNotBlockNumericAtrParity(
        string symbol, string previousText, string nextText)
    {
        var metadata = Metadata();
        var previous = DateTimeOffset.Parse(previousText);
        var next = DateTimeOffset.Parse(nextText);
        var timestamps = new[] { previous, next }
            .Concat(Enumerable.Range(1, 128).Select(index => next.AddMinutes(index * 5)))
            .ToArray();
        var observations = timestamps.Select((timestamp, index) => new StrategyReportAtrObservation(
            index.ToString(), symbol, "5m", timestamp, 100m, 101m, 99m, 100m, 1000m, 2m)).ToArray();
        var stored = observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored, warmupBars: 15, comparisonWindowBars: 130);

        Assert.Equal("MATCH", result.Status);
        Assert.Equal(0, result.TimestampGapCount);
        Assert.Equal(1, result.VerifiedNoBarIntervalCount);
        Assert.Contains("no-candle periods", result.Explanation);
        Assert.Single(result.VerifiedNoBarIntervals!);
    }

    [Fact]
    public void WilderParityIgnoresScheduledOvernightClosureBetweenSessionDates()
    {
        var metadata = Metadata();
        var firstSession = DateTimeOffset.Parse("2026-11-03T14:00:00Z");
        var secondSession = DateTimeOffset.Parse("2026-11-04T14:00:00Z");
        var observations = Enumerable.Range(0, 130).Select(index =>
        {
            var timestamp = index < 65
                ? firstSession.AddMinutes(index)
                : secondSession.AddMinutes(index - 65);
            return new StrategyReportAtrObservation(index.ToString(), "EVR", "1m",
                timestamp, 100m, 101m, 99m, 100m, 1000m, 2m);
        }).ToArray();
        var stored = observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored);

        Assert.Equal("MATCH", result.Status);
        Assert.Equal(0, result.TimestampGapCount);
        Assert.Equal(31, result.ComparedBarCount);
    }

    [Fact]
    public void LegacyNovemberThirdOneMinuteExportMatchesTheDoubleShiftedTrueRangeDefect()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_EVR_11032026_FULL_1M.csv"));
        var parsed = StrategyReportAtrV2Parser.Parse(reader.ReadToEnd());
        var rows = parsed.Observations;
        var trueRanges = new decimal[rows.Count];
        for (var i = 1; i < rows.Count; i++)
        {
            var previousClose = rows[Math.Max(0, i - 2)].Close;
            trueRanges[i] = Math.Max(rows[i].High - rows[i].Low,
                Math.Max(Math.Abs(rows[i].High - previousClose), Math.Abs(rows[i].Low - previousClose)));
        }
        var atr = trueRanges.Skip(1).Take(14).Average();
        var errors = new List<decimal>();
        for (var i = 15; i < rows.Count; i++)
        {
            atr = ((atr * 13m) + trueRanges[i]) / 14m;
            if (i >= 99) errors.Add(Math.Abs(atr - rows[i].Atr));
        }

        Assert.Equal(247, errors.Count);
        Assert.True(errors.Max() <= ThinkorswimAtrParityService.ExportRoundingTolerance);
        Assert.InRange(errors.Average(), 0.00002m, 0.00003m);
    }

    [Fact]
    public void ActualFiveMinuteExportNeedsMoreThanNinetyNineWarmupRows()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StrategyReports_EVR_11032026_FULL_5M.csv"));
        var parsed = StrategyReportAtrV2Parser.Parse(reader.ReadToEnd());
        var metadata = Metadata() with { StudyVersion = "MCC_OHLCV_ATR_V2.0" };
        var stored = parsed.Observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored);

        Assert.Equal("INSUFFICIENT_WARMUP", result.Status);
        Assert.Equal(81, result.SourceObservationCount);
        Assert.Equal(99, result.WarmupBars);
        Assert.Equal(0, result.ComparedBarCount);
    }

    [Fact]
    public void WilderAtrMatchesWhenExportedValuesUseTheSameSeedAndBars()
    {
        var metadata = Metadata();
        var observations = Enumerable.Range(0, 130).Select(index => new StrategyReportAtrObservation(
            index.ToString(), "EVR", "1m", DateTimeOffset.Parse("2026-11-03T14:00:00Z").AddMinutes(index),
            100m, 101m, 99m, 100m, 1000m, 2m)).ToArray();
        var stored = observations.Select((item, index) => new ThinkorswimAtrStoredObservation(
            "row-" + index, DateTimeOffset.UtcNow, metadata, item, "hash-" + index, null)).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored);

        Assert.Equal("MATCH", result.Status);
        Assert.Equal(31, result.ComparedBarCount);
        Assert.Equal(0m, result.MaximumAbsoluteError);
        Assert.Equal(0, result.TimestampGapCount);
    }

    [Fact]
    public void WilderParityUsesFullImportedHistoryAndComparesOnlyLatestWindow()
    {
        var metadata = Metadata();
        var start = DateTimeOffset.Parse("2026-11-03T14:00:00Z");
        var bars = Enumerable.Range(0, 346).Select(index => new
        {
            High = index < 15 ? 105m : 100.5m,
            Low = index < 15 ? 95m : 99.5m,
            Close = 100m
        }).ToArray();
        var trueRanges = new decimal[bars.Length];
        for (var i = 1; i < bars.Length; i++)
        {
            var previousClose = bars[i - 1].Close;
            trueRanges[i] = Math.Max(bars[i].High - bars[i].Low,
                Math.Max(Math.Abs(bars[i].High - previousClose), Math.Abs(bars[i].Low - previousClose)));
        }

        var atrValues = new decimal[bars.Length];
        var atr = trueRanges.Skip(1).Take(14).Average();
        atrValues[14] = atr;
        for (var i = 15; i < bars.Length; i++)
        {
            atr = ((atr * 13m) + trueRanges[i]) / 14m;
            atrValues[i] = atr;
        }

        var stored = bars.Select((bar, index) =>
        {
            var observation = new StrategyReportAtrObservation(index.ToString(), "EVR", "1m",
                start.AddMinutes(index), 100m, bar.High, bar.Low, bar.Close, 1000m,
                Math.Round(atrValues[index], 4, MidpointRounding.AwayFromZero));
            return new ThinkorswimAtrStoredObservation(
                "row-" + index, DateTimeOffset.UtcNow, metadata, observation, "hash-" + index, null);
        }).ToArray();

        var result = ThinkorswimAtrParityService.Compare(stored, comparisonWindowBars: 119);

        Assert.Equal("MATCH", result.Status);
        Assert.Equal("MCC-WILDER-ATR-PARITY-1.2", result.ComparisonVersion);
        Assert.Equal(346, result.SourceObservationCount);
        Assert.Equal(20, result.ComparedBarCount);
        Assert.Equal(0, result.TimestampGapCount);
        Assert.True(result.MaximumAbsoluteError <= ThinkorswimAtrParityService.ExportRoundingTolerance);
    }

    private static StrategyReportAtrImportMetadata Metadata() => new(
        "test.csv", "MCC_OHLCV_ATR_V2.1", 14, "Wilder", "America/Phoenix",
        true, true, "batch-1", DateTimeOffset.UtcNow);

    [Fact]
    public void ReportsLeadingAndTrailingExtendedHoursCoverageForPartialExport()
    {
        var start = DateTimeOffset.Parse("2026-11-06T14:30:00Z"); // 09:30 ET
        var rows = Enumerable.Range(0, 78).Select(index =>
        {
            var timestamp = start.AddMinutes(index * 5).ToUnixTimeMilliseconds();
            return $"{index + 1};MCC_OHLCV_ATR_V2.1(MCC_OHLCV_ATR|v2.1|EVR|5m|{timestamp}|100|101|99|100|1000|2.0);Buy to Open;";
        });
        var report = "Strategy report" + Environment.NewLine + "Symbol: EVR" + Environment.NewLine + "Id;Strategy;Side;" + Environment.NewLine + string.Join(Environment.NewLine, rows);

        var result = StrategyReportAtrV2Parser.Parse(report);

        var coverage = Assert.Single(result.SessionCoverage!);
        Assert.Equal(new DateOnly(2026, 11, 6), coverage.EasternSessionDate);
        Assert.Equal(66, coverage.MissingLeadingIntervalCount);
        Assert.Equal(48, coverage.MissingTrailingIntervalCount);
        Assert.Equal(DateTimeOffset.Parse("2026-11-06T09:00:00Z"), coverage.FirstMissingLeadingAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-06T14:25:00Z"), coverage.LastMissingLeadingAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-06T21:00:00Z"), coverage.FirstMissingTrailingAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-07T00:55:00Z"), coverage.LastMissingTrailingAtUtc);
    }


}
