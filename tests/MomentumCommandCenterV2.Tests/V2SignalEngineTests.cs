using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: V2SignalEngineTests
/// PURPOSE: Defines the class test fixture for V2SignalEngineTests.
/// WHEN CALLED: Used by the xUnit test runner when this test type is discovered.
/// </summary>
public sealed class V2SignalEngineTests
{
    /// <summary>
    /// METHOD: Bar
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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

    /// <summary>
    /// METHOD: Snapshot
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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

    /// <summary>
    /// METHOD: StrongFive
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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

    /// <summary>
    /// METHOD: StrongOne
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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

    /// <summary>
    /// METHOD: StrongOneExtended
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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
    /// <summary>
    /// METHOD: StrongAlignmentProducesBuy
    /// PURPOSE: Creates deterministic signal-test fixture data.
    /// WHEN CALLED: Called by individual test cases to construct controlled inputs.
    /// </summary>
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
    /// <summary>
    /// METHOD: ChoppyOneMinuteProducesPrepareBuy
    /// PURPOSE: Verifies the expected signal-engine behavior for ChoppyOneMinuteProducesPrepareBuy.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: HealthyPositionProducesRunner
    /// PURPOSE: Verifies the expected signal-engine behavior for HealthyPositionProducesRunner.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: RunnerWaningDoesNotSell
    /// PURPOSE: Verifies the expected signal-engine behavior for RunnerWaningDoesNotSell.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: ConfirmedWeaknessProducesSell
    /// PURPOSE: Verifies the expected signal-engine behavior for ConfirmedWeaknessProducesSell.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: FiveMinuteStructureBreakProducesBreakdown
    /// PURPOSE: Verifies the expected signal-engine behavior for FiveMinuteStructureBreakProducesBreakdown.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: HardAtrExtensionProducesBreakdown
    /// PURPOSE: Verifies the expected signal-engine behavior for HardAtrExtensionProducesBreakdown.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: PrepareSellIsNotHardExit
    /// PURPOSE: Verifies the expected signal-engine behavior for PrepareSellIsNotHardExit.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: BuyIsEntryOnlyAndNeverRunnerManagement
    /// PURPOSE: Verifies the expected signal-engine behavior for BuyIsEntryOnlyAndNeverRunnerManagement.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: EodForcesExitAt355Eastern
    /// PURPOSE: Verifies the expected signal-engine behavior for EodForcesExitAt355Eastern.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: EodUsesEasternTimeWhenTimestampIsUtc
    /// PURPOSE: Verifies the expected signal-engine behavior for EodUsesEasternTimeWhenTimestampIsUtc.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: BeforeEodDoesNotForceExit
    /// PURPOSE: Verifies the expected signal-engine behavior for BeforeEodDoesNotForceExit.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: HealthyRunnerDoesNotSetExitWarning
    /// PURPOSE: Verifies the expected signal-engine behavior for HealthyRunnerDoesNotSetExitWarning.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: RunnerWaningIsNotConfirmedWeakness
    /// PURPOSE: Verifies the expected signal-engine behavior for RunnerWaningIsNotConfirmedWeakness.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: SimultaneousOneMinuteStructureAndMomentumWeaknessProducesConfirmedWeakness
    /// PURPOSE: Verifies the expected signal-engine behavior for SimultaneousOneMinuteStructureAndMomentumWeaknessProducesConfirmedWeakness.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: FiveMinuteBreakdownOverridesRunnerWaning
    /// PURPOSE: Verifies the expected signal-engine behavior for FiveMinuteBreakdownOverridesRunnerWaning.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: HardAtrExtensionOverridesRunnerWaning
    /// PURPOSE: Verifies the expected signal-engine behavior for HardAtrExtensionOverridesRunnerWaning.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: PrepareSellDoesNotBecomeRunnerWaningWhenBothOneMinuteSignalsAreWeak
    /// PURPOSE: Verifies the expected signal-engine behavior for PrepareSellDoesNotBecomeRunnerWaningWhenBothOneMinuteSignalsAreWeak.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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
    /// <summary>
    /// METHOD: HealthyPositionProducesExpectedEvaluationContext
    /// PURPOSE: Verifies the expected signal-engine behavior for HealthyPositionProducesExpectedEvaluationContext.
    /// WHEN CALLED: Invoked by xUnit as part of the signal-engine regression suite.
    /// </summary>
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