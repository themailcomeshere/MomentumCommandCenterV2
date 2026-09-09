using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class V2SignalEngineTests
{
    private static BarSnapshot Bar(
        decimal c,
        decimal v,
        decimal e9,
        decimal e20,
        decimal s50,
        decimal rvol,
        decimal rsi,
        decimal macd,
        decimal sig,
        decimal atr = 1m,
        DateTimeOffset? timestamp = null)
    {
        return new BarSnapshot(
            timestamp ?? DateTimeOffset.Parse("2026-09-08T13:00:00-04:00"),
            c,
            c,
            c,
            c,
            1_000_000,
            rvol,
            v,
            e9,
            e20,
            s50,
            rsi,
            macd,
            sig,
            atr);
    }

    private static CommandCenterSnapshot Snapshot(
        BarSnapshot one,
        BarSnapshot five,
        BarSnapshot? previousFive = null,
        DateTimeOffset? timestamp = null)
    {
        var ts = timestamp ?? one.Timestamp;

        return new CommandCenterSnapshot(
            ts,
            "TEST",
            one with { Timestamp = ts },
            five with { Timestamp = ts },
            MarketPhase.RegularSession,
            previousFive);
    }

    private static BarSnapshot StrongFive(
        DateTimeOffset? timestamp = null)
    {
        return Bar(
            110,
            100,
            108,
            105,
            100,
            3,
            60,
            2,
            1,
            1,
            timestamp);
    }

    private static BarSnapshot StrongOne(
        DateTimeOffset? timestamp = null)
    {
        return Bar(
            110,
            105,
            108,
            106,
            100,
            3,
            60,
            2,
            1,
            2,
            timestamp);
    }

    private static BarSnapshot StrongOneExtended(
    decimal close,
    decimal atr,
    DateTimeOffset? timestamp = null)
    {
        return Bar(
            close,
            105,
            108,
            106,
            100,
            3,
            60,
            2,
            1,
            atr,
            timestamp);
    }

    [Fact]
    public void StrongAlignmentProducesBuy()
    {
        var f = StrongFive();
        var o = StrongOne();

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, false);

        Assert.Equal(SignalState.Buy, r.State);
        Assert.Equal(MomentumAction.BUY, r.Action);

        Assert.True(r.EntryAllowed);
        Assert.False(r.RunnerAllowed);
        Assert.False(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.True(r.IsEntrySignal);
        Assert.False(r.IsRunner);
        Assert.False(r.IsExitState);
    }

    [Fact]
    public void ChoppyOneMinuteProducesPrepareBuy()
    {
        var f = StrongFive();

        var o = Bar(
            104,
            105,
            104,
            106,
            100,
            3,
            48,
            0,
            1);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, false);

        Assert.Equal(SignalState.PrepareBuy, r.State);
        Assert.Equal(MomentumAction.PREPARE_BUY, r.Action);

        Assert.True(r.LongPermission);
        Assert.False(r.EntryAllowed);
        Assert.False(r.RunnerAllowed);
        Assert.False(r.HardExit);
    }

    [Fact]
    public void HealthyPositionProducesRunner()
    {
        var f = StrongFive();
        var o = StrongOne();

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Runner, r.State);
        Assert.Equal(MomentumAction.HOLD, r.Action);

        Assert.True(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);
        Assert.False(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.True(r.IsRunner);
        Assert.False(r.IsExitState);
        Assert.False(r.IsHardExit);
    }

    [Fact]
    public void RunnerWaningDoesNotSell()
    {
        var f = StrongFive();

        // 2 ATR above EMA9:
        // warning >= 1.5
        // hard exit < 2.25
        var o = StrongOneExtended(
            close: 112,
            atr: 2);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.RunnerWaning, r.State);
        Assert.Equal(MomentumAction.HOLD, r.Action);

        Assert.True(r.RunnerAllowed);
        Assert.True(r.ExitWarning);

        Assert.False(r.EntryAllowed);
        Assert.False(r.HardExit);

        Assert.True(r.IsRunner);
        Assert.False(r.IsExitState);
        Assert.False(r.IsHardExit);
    }

    [Fact]
    public void ConfirmedWeaknessProducesSell()
    {
        var previous = Bar(
            104,
            105,
            106,
            104,
            100,
            3,
            48,
            0,
            1);

        var current = Bar(
            103,
            105,
            106,
            104,
            100,
            3,
            44,
            -1,
            0);

        var one = Bar(
            103,
            105,
            104,
            106,
            100,
            3,
            44,
            -1,
            0);

        var s = Snapshot(
            one,
            current,
            previous);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.ConfirmedWeakness, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);

        Assert.True(r.IsExitState);
        Assert.True(r.IsHardExit);
    }

    [Fact]
    public void FiveMinuteStructureBreakProducesBreakdown()
    {
        var f = Bar(
            100,
            105,
            103,
            104,
            106,
            2,
            45,
            -1,
            0);

        var o = Bar(
            99,
            101,
            100,
            102,
            104,
            2,
            42,
            -1,
            0);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Breakdown, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);

        Assert.True(r.IsExitState);
        Assert.True(r.IsHardExit);
    }

    [Fact]
    public void HardAtrExtensionProducesBreakdown()
    {
        var f = StrongFive();

        // 2.5 ATR above EMA9.
        var o = StrongOneExtended(
            close: 113,
            atr: 2);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Breakdown, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);
        Assert.False(r.RunnerAllowed);
    }

    [Fact]
    public void PrepareSellIsNotHardExit()
    {
        // Five-minute permission is deliberately lost,
        // but the structure is not a confirmed breakdown.
        var f = Bar(
            106,
            105,
            105,
            104,
            100,
            1,
            52,
            1,
            0);

        var o = Bar(
            104,
            105,
            105,
            106,
            100,
            1,
            48,
            0,
            1);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.PrepareSell, r.State);
        Assert.Equal(MomentumAction.PREPARE_SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);
    }

    [Fact]
    public void BuyIsEntryOnlyAndNeverRunnerManagement()
    {
        var f = StrongFive();
        var o = StrongOne();

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, false);

        Assert.Equal(SignalState.Buy, r.State);
        Assert.Equal(MomentumAction.BUY, r.Action);

        Assert.True(r.IsEntrySignal);
        Assert.False(r.IsRunner);
        Assert.False(r.IsExitState);

        Assert.True(r.EntryAllowed);
        Assert.False(r.RunnerAllowed);
        Assert.False(r.HardExit);
    }

    [Fact]
    public void EodForcesExitAt355Eastern()
    {
        var f = StrongFive();
        var o = StrongOne();

        var ts =
            DateTimeOffset.Parse(
                "2026-09-08T15:55:00-04:00");

        var s = Snapshot(
            o,
            f,
            timestamp: ts);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Exit, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.EntryAllowed);
        Assert.False(r.RunnerAllowed);

        Assert.True(r.IsExitState);
        Assert.True(r.IsHardExit);
    }

    [Fact]
    public void EodUsesEasternTimeWhenTimestampIsUtc()
    {
        var f = StrongFive();
        var o = StrongOne();

        // 19:55 UTC = 15:55 ET during daylight saving time.
        var ts =
            DateTimeOffset.Parse(
                "2026-09-08T19:55:00+00:00");

        var s = Snapshot(
            o,
            f,
            timestamp: ts);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Exit, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);
        Assert.True(r.HardExit);
    }

    [Fact]
    public void BeforeEodDoesNotForceExit()
    {
        var f = StrongFive();
        var o = StrongOne();

        var ts =
            DateTimeOffset.Parse(
                "2026-09-08T15:54:59-04:00");

        var s = Snapshot(
            o,
            f,
            timestamp: ts);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.NotEqual(SignalState.Exit, r.State);
        Assert.NotEqual(MomentumAction.SELL, r.Action);
        Assert.False(r.HardExit);
    }

    [Fact]
    public void HealthyRunnerDoesNotSetExitWarning()
    {
        var f = StrongFive();
        var o = StrongOne();

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Runner, r.State);
        Assert.Equal(MomentumAction.HOLD, r.Action);

        Assert.True(r.RunnerAllowed);
        Assert.False(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.True(r.IsRunner);
        Assert.False(r.IsExitState);
        Assert.False(r.IsHardExit);
    }

    [Fact]
    public void RunnerWaningIsNotConfirmedWeakness()
    {
        var f = StrongFive();

        // 2 ATR extension:
        // above warning threshold,
        // below hard-exit threshold.
        var o = Bar(
            112,
            105,
            108,
            106,
            100,
            3,
            60,
            2,
            1,
            2);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.RunnerWaning, r.State);
        Assert.Equal(MomentumAction.HOLD, r.Action);

        Assert.True(r.RunnerAllowed);
        Assert.True(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.True(r.IsRunner);
        Assert.False(r.IsExitState);
        Assert.False(r.IsHardExit);
    }

    [Fact]
    public void SimultaneousOneMinuteStructureAndMomentumWeaknessProducesConfirmedWeakness()
    {
        var previous = Bar(
            104,
            105,
            106,
            104,
            100,
            3,
            48,
            0,
            1);

        var current = Bar(
            103,
            105,
            106,
            104,
            100,
            3,
            44,
            -1,
            0);

        var one = Bar(
            103,
            105,
            104,
            106,
            100,
            3,
            44,
            -1,
            0);

        var s = Snapshot(
            one,
            current,
            previous);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.ConfirmedWeakness, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);

        Assert.True(r.IsExitState);
        Assert.True(r.IsHardExit);
    }

    [Fact]
    public void FiveMinuteBreakdownOverridesRunnerWaning()
    {
        var f = Bar(
            100,
            105,
            103,
            104,
            106,
            2,
            45,
            -1,
            0);

        var o = Bar(
            112,
            105,
            108,
            106,
            100,
            3,
            60,
            2,
            1,
            2);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Breakdown, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.IsRunner);
    }

    [Fact]
    public void HardAtrExtensionOverridesRunnerWaning()
    {
        var f = StrongFive();

        var o = Bar(
            113,
            105,
            108,
            106,
            100,
            3,
            60,
            2,
            1,
            2);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.Breakdown, r.State);
        Assert.Equal(MomentumAction.SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.True(r.HardExit);

        Assert.False(r.RunnerAllowed);
    }

    [Fact]
    public void PrepareSellDoesNotBecomeRunnerWaningWhenBothOneMinuteSignalsAreWeak()
    {
        var f = Bar(
            106,
            105,
            105,
            104,
            100,
            1,
            52,
            1,
            0);

        var o = Bar(
            104,
            105,
            105,
            106,
            100,
            1,
            48,
            0,
            1);

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.Equal(SignalState.PrepareSell, r.State);
        Assert.Equal(MomentumAction.PREPARE_SELL, r.Action);

        Assert.True(r.ExitWarning);
        Assert.False(r.HardExit);

        Assert.False(r.RunnerAllowed);
        Assert.False(r.EntryAllowed);
    }

    [Fact]
    public void HealthyPositionProducesExpectedEvaluationContext()
    {
        var f = StrongFive();
        var o = StrongOne();

        var s = Snapshot(o, f);

        var r = new V2SignalEngine().Evaluate(s, true);

        Assert.NotNull(r.Evaluation);

        Assert.Equal(6, r.Evaluation!.FiveMinuteScore);
        Assert.True(r.Evaluation.FiveMinutePermission);
        Assert.True(r.Evaluation.FiveMinuteStructureBull);
        Assert.False(r.Evaluation.FiveMinuteStructureBroken);

        Assert.True(r.Evaluation.OneMinuteStructureBull);
        Assert.False(r.Evaluation.OneMinuteStructureWeak);
        Assert.True(r.Evaluation.OneMinuteMomentumPositive);
        Assert.False(r.Evaluation.OneMinuteMomentumWeak);

        Assert.Equal(1m, r.Evaluation.AtrExtension);
        Assert.False(r.Evaluation.Extended);
        Assert.False(r.Evaluation.HardExtended);

        Assert.Equal(SignalState.Runner, r.State);
        Assert.Equal(MomentumAction.HOLD, r.Action);
    }
}