namespace MomentumCommandCenterV2.Core.Models;

/// <summary>
/// MODEL: MarketBar
/// PURPOSE: Represents one completed OHLCV bar from an explicitly identified symbol and timeframe.
/// WHEN CALLED/USED: Created by a market-data adapter and consumed by deterministic replay and later indicator calculations.
/// BOUNDARY: Contains raw bar data only; it does not calculate indicators or infer trading decisions.
/// </summary>
public sealed record MarketBar(
    string Symbol,
    BarTimeframe Timeframe,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);

/// <summary>
/// ENUM: BarTimeframe
/// PURPOSE: Identifies the interval represented by a MarketBar.
/// WHEN USED: Used by ingestion, replay ordering, and adapter validation to distinguish 1-minute and 5-minute bars.
/// </summary>
public enum BarTimeframe
{
    OneMinute = 1,
    FiveMinute = 5
}
