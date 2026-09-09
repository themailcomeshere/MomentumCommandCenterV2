using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Interfaces;

// =========================================================
// INTERFACE: ITradeImporter
// PURPOSE:
//   Defines the application-level contract for importing
//   broker trade history.
//
// RESPONSIBILITIES:
//   - Convert a broker-history file into application models.
//
// DOES NOT:
//   - Define Schwab-specific implementation details.
//
// LAYER:
//   Application
// =========================================================
public interface ITradeImporter
{
    TradeImportResult ImportFile(string filePath);
}