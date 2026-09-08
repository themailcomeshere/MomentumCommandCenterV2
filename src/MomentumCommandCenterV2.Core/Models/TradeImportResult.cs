using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Models;

public sealed record TradeImportResult(
    IReadOnlyList<Trade> CompletedTrades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<SchwabOrder> UnmatchedOrders,
    int ImportedOrders,
    IReadOnlyList<string> Errors);