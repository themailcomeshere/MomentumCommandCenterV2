using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;
using MarketBar = MomentumCommandCenterV2.API.MarketData.MarketBar;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OneMinuteExecutionEventsTests
/// PURPOSE: Verifies the port of the 1M Main Study V14.4.13 green execution lane (operator decision 2026-11-25: MCC
/// EXECUTION READY is a 2-bar event using the study's triggers): a fresh trigger opens a 2-bar window that then expires
/// while conditions persist; a pause of 2 bars re-arms an in-trend trigger and a 1-bar pause does not; the extension
/// threshold blocks READY; the 5M execution context (latch, major failure, confirmed weakness) gates the candidate;
/// V2SignalEngine withholds EXECUTION READY when the window is closed and is unchanged without a window; the API series
/// builder returns a step for the latest 1m bar. All bars are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OneMinuteExecutionEventsTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-11-24T15:00:00Z"); // 10:00 ET
    private static readonly FiveMinuteExecutionContext FiveOk = new(true, false, false);

    /// <summary>
    /// PURPOSE: A quiet bar: above VWAP and EMA20 (rising), RSI 60, but MACD below signal, so no candidate; no trigger.
    /// WHEN CALLED: By the scenarios below.
    /// </summary>
    private static ExecutionEventBar Quiet(int i, FiveMinuteExecutionContext? five = null) =>
        new(Start.AddMinutes(i), 10.50m, 10.55m, 10.45m, 10.50m, 10.00m, 10.40m, 10.30m + 0.001m * i, 60m, 0.10m, 0.20m,
            1.2m, 1.0m, five ?? FiveOk, false);

    /// <summary>
    /// PURPOSE: A candidate bar (MACD above signal) at the quiet price: in-trend, no fresh trigger, doji (no confirmation).
    /// WHEN CALLED: By the scenarios below.
    /// </summary>
    private static ExecutionEventBar Hold(int i, FiveMinuteExecutionContext? five = null) =>
        Quiet(i, five) with { Macd = 0.20m, MacdSignal = 0.10m };

    /// <summary>
    /// PURPOSE: A breakout bar: close 10.80 above the prior 10-bar high (10.55), strong bullish candle, MACD rising.
    /// WHEN CALLED: By the scenarios below.
    /// </summary>
    private static ExecutionEventBar Breakout(int i, decimal extension = 1.0m, FiveMinuteExecutionContext? five = null) =>
        new(Start.AddMinutes(i), 10.75m, 10.85m, 10.70m, 10.80m, 10.00m, 10.40m, 10.30m + 0.001m * i, 62m, 0.30m, 0.10m,
            1.2m, extension, five ?? FiveOk, false);

    private static List<ExecutionEventBar> QuietRun(int count) => Enumerable.Range(0, count).Select(i => Quiet(i)).ToList();

    /// <summary>PURPOSE: A fresh breakout is READY on its bar and the next, then expires while conditions persist. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FreshTriggerOpensTwoBarWindowThenExpires()
    {
        var bars = QuietRun(12);
        bars.Add(Breakout(12));
        bars.Add(Hold(13) with { Close = 10.80m, Open = 10.80m, High = 10.82m, Low = 10.78m });
        bars.Add(Hold(14) with { Close = 10.80m, Open = 10.80m, High = 10.82m, Low = 10.78m });
        bars.Add(Hold(15) with { Close = 10.80m, Open = 10.80m, High = 10.82m, Low = 10.78m });

        var steps = OneMinuteExecutionEvents.Evaluate(bars);

        Assert.Contains("FRESH", steps[12].Trigger);
        Assert.Equal(0, steps[12].Age);
        Assert.True(steps[12].Ready);
        Assert.Equal(1, steps[13].Age);
        Assert.True(steps[13].Ready);
        Assert.False(steps[14].WindowActive);
        Assert.False(steps[14].Ready);
        Assert.True(steps[14].Candidate);   // conditions still hold, but READY is an event, not a state
        Assert.False(steps[15].Ready);
        Assert.All(steps.Take(12), s => Assert.False(s.Ready));
    }

    /// <summary>PURPOSE: After the window, a 2-bar pause re-arms an in-trend trigger; a 1-bar pause does not. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void PauseOfTwoBarsRearmsInTrendTrigger_OneBarDoesNot()
    {
        var flat = new Func<int, ExecutionEventBar>(i => Hold(i) with { Close = 10.80m, Open = 10.80m, High = 10.82m, Low = 10.78m });
        var pausedQuiet = new Func<int, ExecutionEventBar>(i => Quiet(i) with { Close = 10.80m, Open = 10.80m, High = 10.82m, Low = 10.78m });

        var twoBarPause = QuietRun(12);
        twoBarPause.Add(Breakout(12));
        twoBarPause.AddRange(new[] { flat(13), flat(14), pausedQuiet(15), pausedQuiet(16), flat(17), flat(18) });
        var a = OneMinuteExecutionEvents.Evaluate(twoBarPause);
        Assert.False(a[14].Ready);
        Assert.Equal("IN_TREND", a[17].Trigger);
        Assert.True(a[17].Ready);

        var oneBarPause = QuietRun(12);
        oneBarPause.Add(Breakout(12));
        oneBarPause.AddRange(new[] { flat(13), flat(14), pausedQuiet(15), flat(16), flat(17) });
        var b = OneMinuteExecutionEvents.Evaluate(oneBarPause);
        Assert.Equal("", b[16].Trigger);
        Assert.False(b[16].Ready);
    }

    /// <summary>PURPOSE: The extension threshold is a config input: 2.10 ATR blocks at 2.00, passes at 2.25. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ExtensionThresholdBlocksReady()
    {
        var bars = QuietRun(12);
        bars.Add(Breakout(12, extension: 2.10m));

        Assert.False(OneMinuteExecutionEvents.Evaluate(bars, new ExecutionEventConfig(ExtensionDangerAtr: 2.00m))[12].Ready);
        Assert.True(OneMinuteExecutionEvents.Evaluate(bars, new ExecutionEventConfig(ExtensionDangerAtr: 2.25m))[12].Ready);
    }

    /// <summary>PURPOSE: Without 5M execution permission there is no candidate, so a breakout is not READY. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void NoFiveMinuteExecutionPermissionMeansNoCandidate()
    {
        var blocked = new FiveMinuteExecutionContext(false, false, false);
        var bars = Enumerable.Range(0, 12).Select(i => Quiet(i, blocked)).ToList();
        bars.Add(Breakout(12, five: blocked));
        var steps = OneMinuteExecutionEvents.Evaluate(bars);
        Assert.False(steps[12].Candidate);
        Assert.False(steps[12].Ready);
    }

    /// <summary>PURPOSE: 5M context: latch, major failure on 2 bars and confirmed weakness gate execution permission. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FiveMinuteContextFollowsLatchMajorFailureAndConfirmedWeakness()
    {
        FiveMinuteStudyBar Healthy(int i) => new(Start.AddMinutes(-5 * i), 11m, 10.8m, 10.5m, 10m, 60m, 0.2m, 0.1m);
        var healthy = Enumerable.Range(0, 4).Select(Healthy).ToList();
        var ok = FiveMinuteExecutionContext.Evaluate(healthy, latchPermission: true, oneMinuteAboveVwap: true);
        Assert.True(ok.ExecutionPermission);
        Assert.True(ok.RunnerStructure);

        Assert.False(FiveMinuteExecutionContext.Evaluate(healthy, latchPermission: false, oneMinuteAboveVwap: true).ExecutionPermission);
        Assert.False(FiveMinuteExecutionContext.Evaluate(healthy, latchPermission: true, oneMinuteAboveVwap: false).ExecutionPermission);

        var weak = healthy.ToList();
        weak[0] = new FiveMinuteStudyBar(weak[0].Timestamp, 10.4m, 10.5m, 10.6m, 10m, 40m, 0.1m, 0.2m); // close < EMA9 < EMA20, RSI < 45
        Assert.False(FiveMinuteExecutionContext.Evaluate(weak, true, true).ExecutionPermission);

        var major = healthy.ToList();
        major[0] = new FiveMinuteStudyBar(major[0].Timestamp, 9.0m, 9.5m, 9.8m, 10m, 50m, 0.2m, 0.1m);
        major[1] = new FiveMinuteStudyBar(major[1].Timestamp, 9.1m, 9.6m, 9.9m, 10m, 50m, 0.2m, 0.1m);
        Assert.False(FiveMinuteExecutionContext.Evaluate(major, true, true).ExecutionPermission);

        Assert.False(FiveMinuteExecutionContext.Evaluate(healthy.Take(3).ToList(), true, true).ExecutionPermission);
    }

    /// <summary>PURPOSE: A snapshot where every MCC fresh-entry condition passes. WHEN CALLED: By the engine tests.</summary>
    private static CommandCenterSnapshot EntrySnapshot(ExecutionEventStep? window)
    {
        var ts = DateTimeOffset.Parse("2026-11-18T16:00:00Z"); // 11:00 ET, after any opening hold
        BarSnapshot Bar(decimal c, decimal v, decimal e9, decimal e20) =>
            new(ts, c, c, c, c, 1_000_000, 3, v, e9, e20, 100, 60, 2, 1, 2, 0.5m);
        return new CommandCenterSnapshot(ts, "TEST", Bar(110, 105, 108, 106), Bar(110, 100, 108, 105),
            MarketPhase.RegularSession, ExecutionWindow: window);
    }

    /// <summary>PURPOSE: The engine gives EXECUTION READY only with an open window; without a window it is unchanged. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void EngineWithholdsReadyWhenTheWindowIsClosed()
    {
        var engine = new V2SignalEngine();
        var ts = DateTimeOffset.Parse("2026-11-18T16:00:00Z");

        var open = engine.Evaluate(EntrySnapshot(new ExecutionEventStep(ts, true, "FRESH", 0, true, false, true)), false);
        Assert.Equal(SignalState.Buy, open.State);
        Assert.True(open.EntryAllowed);

        var expired = engine.Evaluate(EntrySnapshot(new ExecutionEventStep(ts, true, "", -1, false, false, false)), false);
        Assert.Equal(SignalState.PrepareBuy, expired.State);
        Assert.False(expired.EntryAllowed);
        Assert.True(expired.Evaluation!.ExecutionWindowWithheld);
        Assert.Contains("2-bar window", expired.Reason);

        var none = engine.Evaluate(EntrySnapshot(null), false);
        Assert.Equal(SignalState.Buy, none.State);

        // Held positions are never gated by the entry window.
        var held = engine.Evaluate(EntrySnapshot(new ExecutionEventStep(ts, true, "", -1, false, false, false)), true);
        Assert.NotEqual(SignalState.PrepareBuy, held.State);
    }

    /// <summary>PURPOSE: The API series builder returns one step per 1m bar from the 50th, ending on the latest bar. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void SeriesBuilderReturnsAStepForTheLatestOneMinuteBar()
    {
        var day = DateTimeOffset.Parse("2026-11-24T14:30:00Z");
        var five = Enumerable.Range(0, 120).Select(i => Mb(day.AddDays(-1).AddMinutes(5 * i), 100m + i * 0.05m, TimeSpan.FromMinutes(5))).ToList();
        var one = Enumerable.Range(0, 120).Select(i => Mb(day.AddMinutes(i), 106m + i * 0.01m, TimeSpan.FromMinutes(1))).ToList();
        var latch = FiveMinutePermissionLatch.Evaluate(LiveSymbolTechnicalCalculator.PermissionSeries(five));

        var steps = LiveSymbolTechnicalCalculator.ExecutionEventSeries(one, five, latch, new ExecutionEventConfig());
        Assert.Equal(one.Count - 49, steps.Count);
        Assert.Equal(one[^1].Timestamp, steps[^1].Timestamp);
        Assert.NotNull(LiveSymbolTechnicalCalculator.LatestExecutionStep(one, five, latch));
        Assert.Empty(LiveSymbolTechnicalCalculator.ExecutionEventSeries(one.Take(49).ToList(), five, latch, new ExecutionEventConfig()));
    }

    /// <summary>PURPOSE: One synthetic provider bar. WHEN CALLED: By the series builder test.</summary>
    private static MarketBar Mb(DateTimeOffset ts, decimal close, TimeSpan timeframe) =>
        new("TEST", ts, (double)(close - 0.02m), (double)(close + 0.05m), (double)(close - 0.05m), (double)close, 10_000,
            "Alpaca", "iex", timeframe == TimeSpan.FromMinutes(1) ? "1m" : "5m", "REST");
}
