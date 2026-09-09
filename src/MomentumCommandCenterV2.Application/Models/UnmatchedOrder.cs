namespace MomentumCommandCenterV2.Application.Models;

// =========================================================
// MODEL: UnmatchedOrder
// PURPOSE:
//   Represents a filled broker order that could not be
//   matched into a completed trade.
//
// RESPONSIBILITIES:
//   - Preserve the original symbol, action, price, time,
//     quantity, and source row.
//
// PURPOSE IN VALIDATION:
//   Unmatched orders must remain visible rather than being
//   silently discarded.
//
// LAYER:
//   Application
// =========================================================
public sealed record UnmatchedOrder(
    string Symbol,
    string Action,
    decimal FillPrice,
    DateTimeOffset Timestamp,
    int Quantity,
    int SourceRowNumber);