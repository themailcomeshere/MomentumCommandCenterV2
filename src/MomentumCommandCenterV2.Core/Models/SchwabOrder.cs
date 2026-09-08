namespace MomentumCommandCenterV2.Core.Models
{
    /// <summary>
    /// A single filled BUY or SELL order from Schwab.
    /// </summary>
    public sealed record SchwabOrder(
        string Symbol,
        string Action,
        decimal FillPrice,
        DateTimeOffset Timestamp,
        int Quantity,
        int SourceRowNumber);

}
