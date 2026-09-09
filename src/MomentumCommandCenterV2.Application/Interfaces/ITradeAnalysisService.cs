using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Interfaces;

// =========================================================
// INTERFACE: ITradeAnalysisService
// PURPOSE:
//   Defines the application-level contract for analyzing
//   imported historical trades.
//
// RESPONSIBILITIES:
//   - Accept imported trade data.
//   - Produce aggregate and symbol-level analysis.
//
// LAYER:
//   Application
// =========================================================
public interface ITradeAnalysisService
{
    TradeAnalysisResult Analyze(
        TradeImportResult importResult);
}