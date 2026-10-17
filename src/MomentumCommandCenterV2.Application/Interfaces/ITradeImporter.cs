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
/// <summary>
/// TYPE: ITradeImporter
/// PURPOSE: Defines the interface contract or data structure named ITradeImporter.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
public interface ITradeImporter
{
    TradeImportResult ImportFile(string filePath);
}