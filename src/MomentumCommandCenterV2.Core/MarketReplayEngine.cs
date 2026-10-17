using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Core;

/// <summary>
/// SERVICE: MarketReplayEngine
/// PURPOSE: Converts timestamped 1-minute and 5-minute bars into point-in-time replay frames without look-ahead.
/// WHEN CALLED: Invoked by a historical validation workflow after source bars have been parsed and validated.
/// DOES NOT: Fetch data, calculate indicators, simulate fills, or claim that a signal would have been profitable.
/// </summary>
public sealed class MarketReplayEngine
{
    /// <summary>
    /// METHOD: Replay
    /// PURPOSE: Emits one frame for each 1-minute bar, attaching only the latest 5-minute bar available at that timestamp.
    /// WHEN CALLED: Used by tests, offline analysis, and future replay UI workflows.
    /// VALIDATION: Rejects blank symbols, invalid OHLC relationships, negative volume, and duplicate symbol/timeframe/timestamp keys.
    /// </summary>
    public IReadOnlyList<MarketReplayFrame> Replay(IEnumerable<MarketBar> sourceBars)
    {
        ArgumentNullException.ThrowIfNull(sourceBars);
        var bars = sourceBars.ToArray();
        ValidateBars(bars);
        var frames = new List<MarketReplayFrame>();

        foreach (var symbolGroup in bars.GroupBy(bar => bar.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            var fiveMinuteBars = symbolGroup.Where(bar => bar.Timeframe == BarTimeframe.FiveMinute)
                .OrderBy(bar => bar.Timestamp).ToArray();
            var fiveMinuteIndex = -1;
            MarketBar? latestFiveMinute = null;

            foreach (var oneMinuteBar in symbolGroup.Where(bar => bar.Timeframe == BarTimeframe.OneMinute)
                         .OrderBy(bar => bar.Timestamp))
            {
                while (fiveMinuteIndex + 1 < fiveMinuteBars.Length &&
                       fiveMinuteBars[fiveMinuteIndex + 1].Timestamp <= oneMinuteBar.Timestamp)
                {
                    latestFiveMinute = fiveMinuteBars[++fiveMinuteIndex];
                }

                frames.Add(new MarketReplayFrame(oneMinuteBar.Symbol, oneMinuteBar.Timestamp,
                    oneMinuteBar, latestFiveMinute));
            }
        }

        return frames.OrderBy(frame => frame.Timestamp)
            .ThenBy(frame => frame.Symbol, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// METHOD: ValidateBars
    /// PURPOSE: Rejects malformed or ambiguous bar sets before replay begins.
    /// WHEN CALLED: Called by Replay before any frame is emitted so invalid input cannot yield a partial replay.
    /// </summary>
    private static void ValidateBars(IReadOnlyCollection<MarketBar> bars)
    {
        var keys = new HashSet<(string Symbol, BarTimeframe Timeframe, DateTimeOffset Timestamp)>();
        foreach (var bar in bars)
        {
            if (bar is null)
                throw new ArgumentException("Market bar collection cannot contain null entries.", nameof(bars));
            if (string.IsNullOrWhiteSpace(bar.Symbol))
                throw new ArgumentException("Every market bar must have a symbol.", nameof(bars));
            if (bar.Timeframe is not BarTimeframe.OneMinute and not BarTimeframe.FiveMinute)
                throw new ArgumentException($"Unsupported timeframe: {bar.Timeframe}.", nameof(bars));
            if (bar.High < bar.Low || bar.High < bar.Open || bar.High < bar.Close ||
                bar.Low > bar.Open || bar.Low > bar.Close || bar.Volume < 0m)
                throw new ArgumentException($"Invalid OHLCV values for {bar.Symbol} at {bar.Timestamp:O}.", nameof(bars));
            if (!keys.Add((bar.Symbol.ToUpperInvariant(), bar.Timeframe, bar.Timestamp)))
                throw new ArgumentException($"Duplicate {bar.Timeframe} bar for {bar.Symbol} at {bar.Timestamp:O}.", nameof(bars));
        }
    }
}
