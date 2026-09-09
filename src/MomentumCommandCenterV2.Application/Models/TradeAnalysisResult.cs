using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Models;

// =========================================================
// MODEL: TradeAnalysisResult
// PURPOSE:
//   Contains the complete output of historical trade
//   analysis.
//
// RESPONSIBILITIES:
//   - Preserve reconstructed trades.
//   - Preserve open positions and unmatched orders.
//   - Provide overall statistics.
//   - Provide symbol-level statistics.
//
// LAYER:
//   Application
// =========================================================
public sealed record TradeAnalysisResult(
    IReadOnlyList<Trade> Trades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    TradeStatistics Overall,
    IReadOnlyList<SymbolTradeStatistics> BySymbol);