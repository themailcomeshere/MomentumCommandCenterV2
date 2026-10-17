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
/// <summary>
/// TYPE: TradeAnalysisResult
/// PURPOSE: Defines the record contract or data structure named TradeAnalysisResult.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
/// <summary>
/// METHOD: TradeAnalysisResult
/// PURPOSE: Implements TradeAnalysisResult for this component.
/// WHEN CALLED: Called by the workflow or caller that requests TradeAnalysisResult.
/// </summary>
public sealed record TradeAnalysisResult(
    IReadOnlyList<Trade> Trades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    TradeStatistics Overall,
    IReadOnlyList<SymbolTradeStatistics> BySymbol);