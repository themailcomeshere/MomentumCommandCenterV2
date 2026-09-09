using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Interfaces;

public interface ITradeAnalysisService
{
    TradeAnalysisResult Analyze(
        TradeImportResult importResult);
}