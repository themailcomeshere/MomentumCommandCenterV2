using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Core;

public sealed record TradeReview(Trade Trade, string Classification, string Finding, decimal? MfePercent, decimal? MaePercent);
public sealed class TradeAnalyzer
{
    public TradeReview Review(Trade trade, IReadOnlyList<CommandCenterSnapshot> snapshots, V2SignalEngine engine)
    {
        var during = snapshots.Where(s => s.Symbol.Equals(trade.Symbol, StringComparison.OrdinalIgnoreCase) && s.Timestamp >= trade.EntryTime && s.Timestamp <= trade.ExitTime).OrderBy(s => s.Timestamp).ToList();
        if (during.Count == 0) return new(trade, "INSUFFICIENT_DATA", "No snapshots cover the trade window.", null, null);
        var highs = during.Select(x => x.OneMinute.High); var lows = during.Select(x => x.OneMinute.Low);
        var mfe = trade.EntryPrice == 0
            ? (decimal?)null
            : (highs.Max() - trade.EntryPrice) / trade.EntryPrice * 100m;
        var mae = trade.EntryPrice == 0
            ? (decimal?)null
            : (lows.Min() - trade.EntryPrice) / trade.EntryPrice * 100m; var entry = engine.Evaluate(during.First(), false); var exit = engine.Evaluate(during.Last(), true);
        if (!entry.EntryAllowed) return new(trade, "ENTRY_FILTER_FAILURE", $"V2 would not permit a fresh long at the captured entry state. {entry.Reason}", mfe, mae);
        if (trade.PnlPercent < 0 && exit.HardExit) return new(trade, "EXIT_FAILURE", $"V2 identifies a hard-exit condition by the captured exit state. {exit.Reason}", mfe, mae);
        if (trade.PnlPercent > 0 && mfe.HasValue && mfe.Value > trade.PnlPercent + 1m) return new(trade, "EARLY_EXIT", "Trade captured profit but gave up materially more favorable excursion.", mfe, mae);
        return new(trade, "VALIDATE", "No obvious V2 defect from available snapshots; retain for validation.", mfe, mae);
    }
}
