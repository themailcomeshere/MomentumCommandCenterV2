using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models
{
    // =========================================================
    // MODEL: Trade
    // PURPOSE:
    //   Represents a completed buy-to-sell trade reconstructed
    //   from broker order history.
    //
    // RESPONSIBILITIES:
    //   - Store entry and exit information.
    //   - Calculate dollar P&L.
    //   - Calculate percentage P&L.
    //   - Calculate holding time.
    //
    // DOES NOT:
    //   - Determine why the trade was entered.
    //   - Determine whether the exit was optimal.
    //   - Calculate MFE/MAE without market data.
    //
    // LAYER:
    //   Core
    // =========================================================
    public sealed record Trade(string Symbol, DateTimeOffset EntryTime, decimal EntryPrice, DateTimeOffset ExitTime, decimal ExitPrice, int Quantity, ExitReason ExitReason = ExitReason.Unknown) { public decimal Pnl => (ExitPrice - EntryPrice) * Quantity; public decimal PnlPercent => EntryPrice == 0 ? 0 : (ExitPrice - EntryPrice) / EntryPrice * 100m; public TimeSpan HoldTime => ExitTime - EntryTime; }
}
