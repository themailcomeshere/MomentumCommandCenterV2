namespace MomentumCommandCenterV2.Core.Models;

/// <summary>
/// MODEL: MarketReplayFrame
/// PURPOSE: Represents the information available for one symbol at a one-minute replay timestamp.
/// WHEN USED: Emitted by MarketReplayEngine for historical inspection, later signal evaluation, and UI replay.
/// SAFETY: FiveMinuteBar is the latest bar whose timestamp is not later than the frame; consumers must still check its age.
/// </summary>
public sealed record MarketReplayFrame(
    string Symbol,
    DateTimeOffset Timestamp,
    MarketBar OneMinuteBar,
    MarketBar? FiveMinuteBar)
{
    /// <summary>
    /// PROPERTY: FiveMinuteAge
    /// PURPOSE: Reports the elapsed time since the 5-minute bar available at this replay point.
    /// WHEN USED: Used by downstream consumers to enforce an explicit freshness policy instead of silently trusting stale context.
    /// </summary>
    public TimeSpan? FiveMinuteAge =>
        FiveMinuteBar is null ? null : Timestamp - FiveMinuteBar.Timestamp;
}
