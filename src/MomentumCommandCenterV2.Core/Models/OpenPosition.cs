namespace MomentumCommandCenterV2.Core.Models
{
    // =========================================================
    // MODEL: OpenPosition
    // PURPOSE:
    //   Represents a position that remains open after processing
    //   the available Schwab order history.
    //
    // RESPONSIBILITIES:
    //   - Identify the symbol.
    //   - Store entry time, entry price, and quantity.
    //
    // DOES NOT:
    //   - Determine whether the position should be held or sold.
    //   - Place orders.
    //
    // LAYER:
    //   Core
    // =========================================================
    /// <summary>
    /// A BUY that has not yet been matched with a SELL.
    /// </summary>
    public sealed record OpenPosition(
        string Symbol,
        DateTimeOffset EntryTime,
        decimal EntryPrice,
        int Quantity);
}
