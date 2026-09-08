using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Interfaces;

public interface ITradeImporter
{
    TradeImportResult ImportFile(string filePath);
}