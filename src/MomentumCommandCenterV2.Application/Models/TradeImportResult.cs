using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Models;

public sealed record TradeImportResult(
    IReadOnlyList<Trade> CompletedTrades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    int ImportedOrders,
    IReadOnlyList<string> Errors);