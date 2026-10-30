using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Services;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Hosts the provider-neutral discovery polling worker in the API process.
/// </summary>
public sealed class AlpacaScannerDiscoveryHostedService : BackgroundService
{
    private readonly ScannerDiscoveryPollingWorker _worker;

    public AlpacaScannerDiscoveryHostedService(
        IScannerDiscoverySource source,
        ScannerDiscoveryIngestionService ingestion,
        IConfiguration configuration)
    {
        var seconds = Math.Clamp(configuration.GetValue("MarketData:Scanner:IntervalSeconds", 60), 30, 3600);
        _worker = new ScannerDiscoveryPollingWorker(source, ingestion, TimeSpan.FromSeconds(seconds));
    }

    public ScannerCollectorHealthSnapshot Health => _worker.Health;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _worker.RunAsync(stoppingToken);
}
