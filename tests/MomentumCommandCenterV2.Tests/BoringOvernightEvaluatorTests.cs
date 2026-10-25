using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 15, 0, 0, TimeSpan.Zero);

    private static BoringOvernightEvaluator CreateEvaluator() =>
        new(new SchwabExclusionList());

    private static BoringOvernightInput GoodInput(string symbol = "AAPL") => new()
    {
        Symbol = symbol,
        CompletedOvernightReturnsPercent = Enumerable.Range(0, 20).Select(i => i < 5 ? 0.40 : 0.20).ToArray(),
        Price = 100,
        PriceTimestampUtc = Now,
        PriceIsFresh = true,
        AverageDailyVolume = 2_000_000,
        VolumeIsFresh = true,
        Sma20 = 95,
        CurrentOvernightMovePercent = 0.5,
        CurrentOvernightMoveTimestampUtc = Now,
        CurrentOvernightMoveIsFresh = true,
        CatalystStatus = BoringCatalystStatus.ClearWithSource,
        CatalystEvidenceReference = "manual-review://case-1",
        CatalystReviewedAtUtc = Now,
        FiveMinutePermission = true,
        StructureDeteriorating = false,
        ExtensionDanger = false,
        AbnormalVolumeDanger = false,
        EvaluatedAtUtc = Now
    };

    [Fact]
    public void FreshCompletePassingInputCanBeReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput());
        Assert.Equal(BoringOvernightStatus.Ready, result.Status);
        Assert.Equal(20, result.HistorySampleCount);
        Assert.Equal(BoringOvernightEvaluator.RulesVersion, result.RulesVersion);
        Assert.Contains("READY_ALL_GATES_PASSED", result.ReasonCodes);
        Assert.Equal(8, result.ScoreComponents.Count);
    }

    [Theory]
    [InlineData("AMHpG")]
    [InlineData("ATHpA")]
    [InlineData("NEEpT")]
    [InlineData("YJ")]
    public void ExcludedSymbolIsBlocked(string symbol)
    {
        var result = CreateEvaluator().Evaluate(GoodInput(symbol));
        Assert.Equal(BoringOvernightStatus.Blocked, result.Status);
        Assert.Contains("GATE_SCHWAB_EXCLUSION", result.ReasonCodes);
    }

    [Fact]
    public void ThreePercentCurrentOvernightMoveIsWatchNotReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with { CurrentOvernightMovePercent = 3.0 });
        Assert.Equal(BoringOvernightStatus.Watch, result.Status);
        Assert.Contains("WATCH_CURRENT_ON_MOVE_HIGH", result.ReasonCodes);
    }

    [Fact]
    public void UnknownCatalystCannotBecomeReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with
        {
            CatalystStatus = BoringCatalystStatus.Unknown,
            CatalystEvidenceReference = null,
            CatalystReviewedAtUtc = null
        });
        Assert.Equal(BoringOvernightStatus.DataUnknown, result.Status);
        Assert.Contains("DATA_CATALYST_REVIEW_REQUIRED", result.ReasonCodes);
    }

    [Fact]
    public void MissingHistoryCannotBecomeReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with
        {
            CompletedOvernightReturnsPercent = Enumerable.Repeat(0.25, 19).ToArray()
        });
        Assert.Equal(BoringOvernightStatus.DataUnknown, result.Status);
        Assert.Contains("DATA_HISTORY_INSUFFICIENT", result.ReasonCodes);
        Assert.Null(result.Score);
    }

    [Theory]
    [InlineData(4.99, "GATE_PRICE_BELOW_MIN")]
    [InlineData(499999, "GATE_AVG_VOLUME_BELOW_MIN")]
    public void HardGateFailureBlocks(double value, string expectedReason)
    {
        var input = expectedReason == "GATE_PRICE_BELOW_MIN"
            ? GoodInput() with { Price = value }
            : GoodInput() with { AverageDailyVolume = value };
        var result = CreateEvaluator().Evaluate(input);
        Assert.Equal(BoringOvernightStatus.Blocked, result.Status);
        Assert.Contains(expectedReason, result.ReasonCodes);
    }

    [Fact]
    public void NonPositiveHistoricalEdgeBlocks()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with
        {
            CompletedOvernightReturnsPercent = Enumerable.Repeat(-0.10, 20).ToArray()
        });
        Assert.Equal(BoringOvernightStatus.Blocked, result.Status);
        Assert.Contains("GATE_ON_WIN_RATE_BELOW_MIN", result.ReasonCodes);
        Assert.Contains("GATE_ON_MEAN_BELOW_MIN", result.ReasonCodes);
    }

    [Fact]
    public void PriceAtOrBelowSmaBlocks()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with { Price = 95, Sma20 = 95 });
        Assert.Equal(BoringOvernightStatus.Blocked, result.Status);
        Assert.Contains("GATE_BELOW_20D_SMA", result.ReasonCodes);
    }

    [Fact]
    public void MissingFiveMinutePermissionCannotBecomeReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with { FiveMinutePermission = null });
        Assert.Equal(BoringOvernightStatus.DataUnknown, result.Status);
        Assert.Contains("DATA_5M_STATE_UNAVAILABLE", result.ReasonCodes);
    }

    [Fact]
    public void ExtensionCautionProducesWatchRatherThanReady()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with { ExtensionDanger = true });
        Assert.Equal(BoringOvernightStatus.Watch, result.Status);
        Assert.Contains("WATCH_CURRENT_EXTENSION", result.ReasonCodes);
    }

    [Fact]
    public void HighScoreNeverOverridesHardGate()
    {
        var result = CreateEvaluator().Evaluate(GoodInput() with { Price = 4.99 });
        Assert.NotNull(result.Score);
        Assert.Equal(BoringOvernightStatus.Blocked, result.Status);
        Assert.Contains("GATE_PRICE_BELOW_MIN", result.ReasonCodes);
    }

    [Fact]
    public void DeterministicInputProducesSameOutput()
    {
        var input = GoodInput();
        var evaluator = CreateEvaluator();
        var first = evaluator.Evaluate(input);
        var second = evaluator.Evaluate(input);
        Assert.Equal(first.Symbol, second.Symbol);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.ReasonCodes, second.ReasonCodes);
        Assert.Equal(first.ScoreComponents.OrderBy(x => x.Key), second.ScoreComponents.OrderBy(x => x.Key));
    }
}
