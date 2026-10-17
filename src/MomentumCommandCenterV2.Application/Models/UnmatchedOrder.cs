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
/// <summary>
/// TYPE: UnmatchedOrder
/// PURPOSE: Defines the record contract or data structure named UnmatchedOrder.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
/// <summary>
/// METHOD: UnmatchedOrder
/// PURPOSE: Implements UnmatchedOrder for this component.
/// WHEN CALLED: Called by the workflow or caller that requests UnmatchedOrder.
/// </summary>
public sealed record UnmatchedOrder(
    string Symbol,
    string Action,
    decimal FillPrice,
    DateTimeOffset Timestamp,
    int Quantity,
    int SourceRowNumber);