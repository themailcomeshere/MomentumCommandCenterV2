using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: BoringOvernightV3CalculatorTests
/// PURPOSE: Verifies the V3 Boring Overnight reference calculator: hand-computed metrics and Boringness points on a
/// synthetic fixture, every hard gate, the WATCH rules, insufficient/invalid history, point-map anchors and
/// interpolation, band boundaries, and that the result is never READY. All bars are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class BoringOvernightV3CalculatorTests
{
    private static readonly decimal[] BaseReturns =
        new[] { -0.001m, -0.001m, -0.001m, -0.001m }.Concat(Enumerable.Repeat(0.002m, 16)).ToArray();

    /// <summary>
    /// PURPOSE: Builds 21 daily bars: closes rise 0.10 per session from 100; each open = previous close × (1 + r);
    /// high/low = max/min(open, close) ± 0.50.
    /// WHEN CALLED: By tests.
    /// </summary>
    private static List<V3DailyBar> Bars(IReadOnlyList<decimal> returns, long volume = 1_000_000, long? lastVolume = null, decimal scale = 1m)
    {
        var bars = new List<V3DailyBar>();
        var date = new DateOnly(2026, 10, 1);
        var first = 100m * scale;
        bars.Add(new V3DailyBar(date, first, first + 0.5m * scale, first - 0.5m * scale, first, volume));
        for (var i = 0; i < returns.Count; i++)
        {
            var previousClose = bars[^1].Close;
            var close = (100m + 0.1m * (i + 1)) * scale;
            var open = previousClose * (1m + returns[i]);
            bars.Add(new V3DailyBar(date.AddDays(i + 1), open, Math.Max(open, close) + 0.5m * scale, Math.Min(open, close) - 0.5m * scale,
                close, i == returns.Count - 1 ? lastVolume ?? volume : volume));
        }
        return bars;
    }

    /// <summary>PURPOSE: Hand-computed fixture: every metric, component and the total. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Fixture_MatchesHandComputedValues()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(BaseReturns));

        Assert.Equal("CANDIDATE", r.Status);
        Assert.Equal(3, r.StatusCode);
        Assert.Equal(new[] { "CANDIDATE_MCC_READY_CHECK" }, r.Reasons);
        Assert.Equal(80m, r.WinRatePct!.Value, 6);
        Assert.Equal(0.14m, r.AverageOvernightPct20!.Value, 6);
        Assert.Equal(0.20m, r.MedianOvernightPct20!.Value, 6);
        Assert.Equal(0.20m, r.AverageOvernightPct5!.Value, 6);
        Assert.Equal(0.06m, r.OvernightMomentumPp!.Value, 6);
        Assert.Equal(0.20m, r.CurrentOvernightPct!.Value, 6);
        Assert.Equal(80m, r.ConsistencyPct!.Value, 6);
        Assert.Equal(0.12m, r.VolatilityPct!.Value, 6);
        Assert.True(r.AboveSma20);
        Assert.Equal(1_000_000m, r.AverageVolumePrior20!.Value, 6);
        Assert.Equal(54.0m, r.OvernightEdgeScore!.Value, 6);
        Assert.True(r.EdgeGatesPass);

        var points = r.BoringnessComponents.ToDictionary(c => c.Key, c => c.Points);
        Assert.Equal(18m, points["winRate"], 4);
        Assert.Equal(12m, points["median"], 4);
        Assert.Equal(6.3333m, points["mean20"], 4);
        Assert.Equal(6.2m, points["fiveDay"], 4);
        Assert.Equal(8m, points["consistency"], 4);
        Assert.Equal(10m, points["volatility"], 4);
        Assert.Equal(3m, points["averageVolume"], 4);
        Assert.Equal(5m, points["price"], 4);
        Assert.Equal(68.5333m, r.BoringnessScore!.Value, 3);
        Assert.Equal("AGGRESSIVE", r.BoringnessBand);
    }

    /// <summary>PURPOSE: Fewer than 21 bars is DATA? with no metrics. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void TwentyBars_IsDataUnknown()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(BaseReturns).Skip(1).ToList());

        Assert.Equal("DATA?", r.Status);
        Assert.Equal("DATA_HISTORY_INSUFFICIENT", r.PrimaryReason);
        Assert.Null(r.BoringnessScore);
        Assert.Null(r.OvernightEdgeScore);
    }

    /// <summary>PURPOSE: A non-positive price is DATA?, never a pass. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void InvalidBar_IsDataUnknown()
    {
        var bars = Bars(BaseReturns);
        bars[10] = bars[10] with { Low = 0m };

        Assert.Equal("DATA_INVALID_BAR", BoringOvernightV3Calculator.Calculate(bars).PrimaryReason);
    }

    /// <summary>PURPOSE: Only the latest 21 bars are used. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ExtraOlderBars_DoNotChangeTheResult()
    {
        var bars = Bars(BaseReturns);
        var older = new V3DailyBar(new DateOnly(2026, 9, 30), 50m, 51m, 49m, 50m, 10);

        var withExtra = BoringOvernightV3Calculator.Calculate(new[] { older }.Concat(bars).ToList());
        var plain = BoringOvernightV3Calculator.Calculate(bars);

        Assert.Equal(plain.BoringnessScore, withExtra.BoringnessScore);
        Assert.Equal(plain.OvernightEdgeScore, withExtra.OvernightEdgeScore);
    }

    /// <summary>PURPOSE: Price at or below $5 blocks (MCC evaluator rule: price must be above $5). WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void LowPrice_Blocks()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(BaseReturns, scale: 0.04m));

        Assert.Equal("BLOCKED", r.Status);
        Assert.Contains("GATE_PRICE_BELOW_MIN", r.Reasons);
    }

    /// <summary>PURPOSE: Average volume below 500,000 blocks and scores 0 volume points. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void LowAverageVolume_Blocks()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(BaseReturns, volume: 400_000));

        Assert.Equal("BLOCKED", r.Status);
        Assert.Equal("GATE_AVG_VOLUME_BELOW_MIN", r.PrimaryReason);
        Assert.Equal(0m, r.BoringnessComponents.Single(c => c.Key == "averageVolume").Points);
    }

    /// <summary>PURPOSE: Win rate below 60% blocks and fails the edge gates. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void LowWinRate_Blocks()
    {
        var returns = Enumerable.Repeat(-0.001m, 9).Concat(Enumerable.Repeat(0.004m, 11)).ToArray();
        var r = BoringOvernightV3Calculator.Calculate(Bars(returns));

        Assert.Equal(55m, r.WinRatePct!.Value, 6);
        Assert.Contains("GATE_ON_WIN_RATE_BELOW_MIN", r.Reasons);
        Assert.False(r.EdgeGatesPass);
    }

    /// <summary>PURPOSE: 20D mean below +0.10% blocks. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void LowMean_Blocks()
    {
        var returns = new[] { -0.002m, -0.002m, -0.002m, -0.002m }.Concat(Enumerable.Repeat(0.0015m, 16)).ToArray();
        var r = BoringOvernightV3Calculator.Calculate(Bars(returns));

        Assert.Equal(0.08m, r.AverageOvernightPct20!.Value, 6);
        Assert.Contains("GATE_ON_MEAN_BELOW_MIN", r.Reasons);
    }

    /// <summary>PURPOSE: 5D mean equal to the 20D mean is not "above" and blocks; it scores 0 five-day points. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FiveDayEqualToTwentyDay_Blocks()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(Enumerable.Repeat(0.002m, 20).ToArray()));

        Assert.Contains("GATE_5D_NOT_ABOVE_20D", r.Reasons);
        Assert.Equal(0m, r.BoringnessComponents.Single(c => c.Key == "fiveDay").Points);
    }

    /// <summary>PURPOSE: A close not above the 20-session SMA blocks. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void CloseBelowSma_Blocks()
    {
        var bars = Bars(BaseReturns);
        var last = bars[^1];
        bars[^1] = last with { Close = 99m, Low = Math.Min(last.Low, 98.5m) };

        var r = BoringOvernightV3Calculator.Calculate(bars);

        Assert.False(r.AboveSma20);
        Assert.Contains("GATE_BELOW_20D_SMA", r.Reasons);
    }

    /// <summary>PURPOSE: A current overnight move of 3% or more is WATCH, with the range warning it causes. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void CurrentMoveAtThreePercent_IsWatch()
    {
        var returns = BaseReturns.ToArray();
        returns[^1] = 0.035m;
        var r = BoringOvernightV3Calculator.Calculate(Bars(returns));

        Assert.Equal("WATCH", r.Status);
        Assert.Equal("WATCH_CURRENT_ON_MOVE_HIGH", r.PrimaryReason);
        Assert.Contains("WATCH_RANGE_ABOVE_1_5_ATR", r.Reasons);
    }

    /// <summary>PURPOSE: Evaluation-day volume at 3x the prior 20-session average is WATCH. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void VolumeThreeTimesAverage_IsWatch()
    {
        var r = BoringOvernightV3Calculator.Calculate(Bars(BaseReturns, lastVolume: 3_000_000));

        Assert.Equal("WATCH", r.Status);
        Assert.Contains("WATCH_VOLUME_3X", r.Reasons);
        Assert.Equal(3m, r.VolumeVsAveragePrior20!.Value, 6);
    }

    /// <summary>PURPOSE: No fixture ever produces READY. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Status_IsNeverReady()
    {
        var fixtures = new[] { Bars(BaseReturns), Bars(BaseReturns, volume: 400_000), Bars(BaseReturns, lastVolume: 3_000_000) };

        Assert.All(fixtures, f => Assert.NotEqual("READY", BoringOvernightV3Calculator.Calculate(f).Status));
    }

    /// <summary>PURPOSE: Point-map anchors, interpolation and clamping. WHEN CALLED: By xUnit once per row.</summary>
    [Theory]
    [InlineData("win", 59, 0)] [InlineData("win", 60, 0)] [InlineData("win", 65, 5)] [InlineData("win", 90, 25)] [InlineData("win", 95, 25)]
    [InlineData("median", -0.1, 0)] [InlineData("median", 0.05, 4)] [InlineData("median", 0.5, 20)] [InlineData("median", 0.9, 20)]
    [InlineData("mean20", 0.09, 0)] [InlineData("mean20", 0.10, 5)] [InlineData("mean20", 0.5, 15)] [InlineData("mean20", 0.75, 12.5)] [InlineData("mean20", 2.5, 0)]
    [InlineData("fiveDay", 0, 0)] [InlineData("fiveDay", 0.05, 6)] [InlineData("fiveDay", 4, 1.5)] [InlineData("fiveDay", 6, 0)]
    [InlineData("consistency", 50, 0)] [InlineData("consistency", 75, 6.5)] [InlineData("consistency", 100, 10)]
    [InlineData("volatility", 0.1, 10)] [InlineData("volatility", 0.75, 6.5)] [InlineData("volatility", 3, 0)]
    [InlineData("volume", 499999, 0)] [InlineData("volume", 500000, 1)] [InlineData("volume", 1500000, 4)] [InlineData("volume", 3000000, 5)]
    [InlineData("price", 5, 0)] [InlineData("price", 17.5, 2)] [InlineData("price", 60, 5)]
    public void PointMaps_MatchTheDataContract(string map, double x, double expected)
    {
        var value = (decimal)x;
        var points = map switch
        {
            "win" => BoringOvernightV3Calculator.WinPoints(value),
            "median" => BoringOvernightV3Calculator.MedianPoints(value),
            "mean20" => BoringOvernightV3Calculator.Mean20Points(value),
            "fiveDay" => BoringOvernightV3Calculator.FiveDayPoints(value),
            "consistency" => BoringOvernightV3Calculator.ConsistencyPoints(value),
            "volatility" => BoringOvernightV3Calculator.VolatilityPoints(value),
            "volume" => BoringOvernightV3Calculator.VolumePoints(value),
            _ => BoringOvernightV3Calculator.PricePoints(value)
        };

        Assert.Equal((decimal)expected, points, 6);
    }

    /// <summary>PURPOSE: Converts fixture bars into provider sessions for the evaluation tests. WHEN CALLED: By tests.</summary>
    private static MomentumCommandCenterV2.API.MarketData.DailyRecentSessions Sessions(string feedCoverage, IReadOnlyList<DateOnly>? missing = null)
    {
        var bars = Bars(BaseReturns);
        var observations = bars.Select(b => new MomentumCommandCenterV2.API.MarketData.DailySessionObservation(
            b.SessionDate, new DateTimeOffset(b.SessionDate.ToDateTime(new TimeOnly(5, 0)), TimeSpan.Zero),
            (double)b.Open, (double)b.High, (double)b.Low, (double)b.Close, b.Volume)).ToArray();
        return new MomentumCommandCenterV2.API.MarketData.DailyRecentSessions("TSTA", "Alpaca",
            feedCoverage == "CONSOLIDATED_SIP" ? "sip" : "iex", feedCoverage, "split", DateTimeOffset.UtcNow, "TEST",
            bars.Select(b => b.SessionDate).ToArray(), missing ?? Array.Empty<DateOnly>(), observations);
    }

    /// <summary>PURPOSE: Complete consolidated sessions are calculated and parity-comparable. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Evaluation_CompleteSip_IsCalculatedAndComparable()
    {
        var evaluation = BoringOvernightV3ColumnEvaluation.FromSessions(Sessions("CONSOLIDATED_SIP"));

        Assert.True(evaluation.ParityComparable);
        Assert.Equal("CANDIDATE", evaluation.Calculation.Status);
        Assert.Equal(68.5333m, evaluation.Calculation.BoringnessScore!.Value, 3);
    }

    /// <summary>PURPOSE: A missing expected session is DATA? and never bridged. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Evaluation_MissingSession_IsDataUnknown()
    {
        var evaluation = BoringOvernightV3ColumnEvaluation.FromSessions(Sessions("CONSOLIDATED_SIP", new[] { new DateOnly(2026, 10, 8) }));

        Assert.False(evaluation.ParityComparable);
        Assert.Equal("DATA_SESSION_MISSING", evaluation.Calculation.PrimaryReason);
        Assert.Null(evaluation.Calculation.BoringnessScore);
    }

    /// <summary>PURPOSE: A partial (IEX) feed is DATA?, never treated as consolidated volume. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Evaluation_IexFeed_IsDataUnknown()
    {
        var evaluation = BoringOvernightV3ColumnEvaluation.FromSessions(Sessions("IEX_PARTIAL"));

        Assert.False(evaluation.ParityComparable);
        Assert.Equal("DATA_FEED_NOT_CONSOLIDATED", evaluation.Calculation.PrimaryReason);
    }

    /// <summary>PURPOSE: Band boundaries 59.99/60/89.99/90. WHEN CALLED: By xUnit once per row.</summary>
    [Theory]
    [InlineData(59.99, "REJECT")] [InlineData(60, "AGGRESSIVE")] [InlineData(70, "POSSIBLE")] [InlineData(80, "VERY GOOD")]
    [InlineData(89.99, "VERY GOOD")] [InlineData(90, "PERFECT BORING")] [InlineData(100, "PERFECT BORING")]
    public void Band_Boundaries(double score, string band)
    {
        Assert.Equal(band, BoringOvernightV3Calculator.Band((decimal)score));
    }
}
