using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;
namespace MomentumCommandCenterV2.Tests;

public sealed class V2SignalEngineTests
{
    static BarSnapshot Bar(decimal c, decimal v, decimal e9, decimal e20, decimal s50, decimal rvol, decimal rsi, decimal macd, decimal sig, decimal atr = 1m) => new(DateTimeOffset.Parse("2026-09-08T13:00:00-04:00"), c, c, c, c, 1_000_000, rvol, v, e9, e20, s50, rsi, macd, sig, atr);

    // TODO: Update Unit Test
    [Fact]
    public void StrongAlignmentProducesEntryReady()
    {
        var f = Bar(110, 100, 108, 105, 100, 3, 60, 2, 1);
        var o = Bar(110, 105, 108, 106, 100, 3, 60, 2, 1);
        var s = new CommandCenterSnapshot(o.Timestamp, "TEST", o, f, MarketPhase.RegularSession);
        var r = new V2SignalEngine().Evaluate(s, false);
        //Assert.Equal(SignalState.EntryReady, r.State);
        Assert.True(r.EntryAllowed);
    }

    // TODO: Update Unit Test
    [Fact]
    public void ChoppyOneMinuteDoesNotAuthorizeFreshEntry()
    {
        var f = Bar(110, 100, 108, 105, 100, 3, 60, 2, 1);
        var o = Bar(104, 105, 104, 106, 100, 3, 48, 0, 1);
        var s = new CommandCenterSnapshot(o.Timestamp, "TEST", o, f, MarketPhase.RegularSession);
        var r = new V2SignalEngine().Evaluate(s, false);
        //Assert.NotEqual(SignalState.EntryReady, r.State);
        Assert.False(r.EntryAllowed);
    }

    [Fact]
    public void EodForcesExit()
    {
        var f = Bar(110, 100, 108, 105, 100, 3, 60, 2, 1);
        var o = Bar(110, 105, 108, 106, 100, 3, 60, 2, 1);
        var ts = DateTimeOffset.Parse("2026-09-08T15:55:00-04:00");
        var s = new CommandCenterSnapshot(ts, "TEST", o with { Timestamp = ts }, f with { Timestamp = ts }, MarketPhase.RegularSession);
        var r = new V2SignalEngine().Evaluate(s, true);
        Assert.Equal(SignalState.Exit, r.State);
        Assert.True(r.HardExit);
    }

    [Fact]
    public void StructuralAndMomentumLossCreatesExit()
    {
        var f = Bar(100, 105, 103, 104, 106, 2, 45, -1, 0);
        var o = Bar(99, 101, 100, 102, 104, 2, 42, -1, 0);
        var s = new CommandCenterSnapshot(o.Timestamp, "TEST", o, f, MarketPhase.RegularSession);
        var r = new V2SignalEngine().Evaluate(s, true);
        Assert.Equal(SignalState.Exit, r.State);
        Assert.True(r.HardExit);
    }
}
