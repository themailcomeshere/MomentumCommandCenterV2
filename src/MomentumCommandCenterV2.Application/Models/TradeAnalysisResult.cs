using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Application.Models;

public sealed record TradeAnalysisResult(
    IReadOnlyList<Trade> Trades,
    IReadOnlyList<OpenPosition> OpenPositions,
    IReadOnlyList<UnmatchedOrder> UnmatchedOrders,
    TradeStatistics Overall,
    IReadOnlyList<SymbolTradeStatistics> BySymbol);