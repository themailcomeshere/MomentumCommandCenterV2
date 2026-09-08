using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models
{
    public sealed record Trade(string Symbol, DateTimeOffset EntryTime, decimal EntryPrice, DateTimeOffset ExitTime, decimal ExitPrice, int Quantity, ExitReason ExitReason = ExitReason.Unknown) { public decimal Pnl => (ExitPrice - EntryPrice) * Quantity; public decimal PnlPercent => EntryPrice == 0 ? 0 : (ExitPrice - EntryPrice) / EntryPrice * 100m; public TimeSpan HoldTime => ExitTime - EntryTime; }
}
