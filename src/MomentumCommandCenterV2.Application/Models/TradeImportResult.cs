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
public sealed record TradeImportResult(
    IReadOnlyList<Trade> CompletedTrades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    int ImportedOrders,
    IReadOnlyList<string> Errors);