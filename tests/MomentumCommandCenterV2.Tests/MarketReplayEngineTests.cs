using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using Xunit;

/// <summary>
/// TESTS: MarketReplayEngineTests
/// PURPOSE: Protects no-look-ahead and deterministic ordering guarantees of historical replay.
/// WHEN CALLED: Executed with the full xUnit suite before replay or adapter changes are accepted.
/// </summary>
public sealed class MarketReplayEngineTests
{
    /// <summary>
    /// TEST: ReplayAttachesOnlyFiveMinuteBarsAvailableAtFrameTime
    /// PURPOSE: Ensures a future 5-minute bar is never attached to an earlier 1-minute frame.
    /// WHEN CALLED: Runs in the unit-test suite for each replay-engine change.
    /// </summary>
    [Fact]
    public void ReplayAttachesOnlyFiveMinuteBarsAvailableAtFrameTime()
    {
        var start = new DateTimeOffset(2026, 10, 16, 16, 0, 0, TimeSpan.Zero);
        var bars = new[]
        {
            Bar(BarTimeframe.OneMinute, start.AddMinutes(6), 11m),
            Bar(BarTimeframe.FiveMinute, start.AddMinutes(5), 10m),
            Bar(BarTimeframe.OneMinute, start.AddMinutes(1), 10m),
            Bar(BarTimeframe.OneMinute, start, 9m)
        };

        var frames = new MarketReplayEngine().Replay(bars);
        Assert.Equal(3, frames.Count);
        Assert.Null(frames[0].FiveMinuteBar);
        Assert.Equal(start, frames[0].Timestamp);
        Assert.Null(frames[1].FiveMinuteBar);
        Assert.Equal(start.AddMinutes(5), frames[2].FiveMinuteBar?.Timestamp);
        Assert.Equal(frames.OrderBy(frame => frame.Timestamp).Select(frame => frame.Timestamp),
            frames.Select(frame => frame.Timestamp));
    }

    /// <summary>
    /// TEST: ReplayRejectsDuplicateBarKeys
    /// PURPOSE: Ensures ambiguous duplicate records fail instead of being silently selected.
    /// WHEN CALLED: Runs in unit-test coverage for malformed input.
    /// </summary>
    [Fact]
    public void ReplayRejectsDuplicateBarKeys()
    {
        var timestamp = DateTimeOffset.Parse("2026-10-16T16:00:00Z");
        var bars = new[] { Bar(BarTimeframe.OneMinute, timestamp, 10m), Bar(BarTimeframe.OneMinute, timestamp, 10m) };
        Assert.Throws<ArgumentException>(() => new MarketReplayEngine().Replay(bars));
    }

    /// <summary>
    /// TEST: ReplayRejectsInvalidOhlc
    /// PURPOSE: Ensures impossible OHLC relationships do not enter historical evaluation.
    /// WHEN CALLED: Runs in the unit-test suite for input validation coverage.
    /// </summary>
    [Fact]
    public void ReplayRejectsInvalidOhlc()
    {
        var bar = new MarketBar("TEST", BarTimeframe.OneMinute,
            DateTimeOffset.Parse("2026-10-16T16:00:00Z"), 10m, 9m, 8m, 10m, 100m);
        Assert.Throws<ArgumentException>(() => new MarketReplayEngine().Replay([bar]));
    }

    /// <summary>
    /// HELPER: Bar
    /// PURPOSE: Creates valid OHLCV fixtures for replay tests.
    /// WHEN CALLED: Called by individual test cases only.
    /// </summary>
    private static MarketBar Bar(BarTimeframe timeframe, DateTimeOffset timestamp, decimal close) =>
        new("TEST", timeframe, timestamp, close, close + 1m, close - 1m, close, 100m);
}
