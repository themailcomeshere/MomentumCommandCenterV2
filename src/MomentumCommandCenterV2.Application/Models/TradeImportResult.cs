using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Models;

// =========================================================
// MODEL: TradeImportResult
// PURPOSE:
//   Contains the complete result of a broker trade-history
//   import.
//
// RESPONSIBILITIES:
//   - Provide reconstructed completed trades.
//   - Provide remaining open positions.
//   - Provide unmatched orders.
//   - Report imported-row count and import errors.
//
// LAYER:
//   Application
// =========================================================
/// <summary>
/// TYPE: TradeImportResult
/// PURPOSE: Defines the record contract or data structure named TradeImportResult.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
/// <summary>
/// METHOD: TradeImportResult
/// PURPOSE: Implements TradeImportResult for this component.
/// WHEN CALLED: Called by the workflow or caller that requests TradeImportResult.
/// </summary>
public sealed record TradeImportResult(
    IReadOnlyList<Trade> CompletedTrades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    int ImportedOrders,
    IReadOnlyList<string> Errors);