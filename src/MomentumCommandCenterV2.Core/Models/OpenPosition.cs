namespace MomentumCommandCenterV2.Core.Models
{
    /// <summary>
    /// A BUY that has not yet been matched with a SELL.
    /// </summary>
    public sealed record OpenPosition(
        string Symbol,
        DateTimeOffset EntryTime,
        decimal EntryPrice,
        int Quantity);
}
