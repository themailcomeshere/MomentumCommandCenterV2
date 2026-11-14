namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Builds the market-bar JSONL index in the background as soon as the API starts, so the first
/// Dashboard request does not pay the multi-second cold rebuild.
/// WHEN CALLED: Once at host startup (registered before the stream, scanner and collector workers).
/// IMPORTANT: Read-only warm-up. It changes no data and no query result; it only moves the one-time
/// rebuild earlier. Disable with MarketData:JsonlIndexWarmupOnStartup=false.
/// </summary>
public sealed class JsonlMarketBarIndexWarmupService(
    IMarketBarObservationStore store,
    IConfiguration configuration,
    ILogger<JsonlMarketBarIndexWarmupService> logger) : BackgroundService
{
    /// <summary>
    /// PURPOSE: Run the warm-up off the host-startup thread and log its measured stages.
    /// WHEN CALLED: By the generic host after StartAsync.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (store is not JsonlMarketBarObservationStore jsonlStore) return;
        if (!configuration.GetValue("MarketData:JsonlIndexWarmupOnStartup", true))
        {
            logger.LogInformation("JSONL market-bar index warm-up disabled by configuration.");
            return;
        }

        try
        {
            // The rebuild is synchronous I/O + CPU; keep it off the startup path.
            await Task.Run(() => jsonlStore.WarmUpAsync(stoppingToken), stoppingToken);
            var diagnostics = jsonlStore.GetDiagnostics();
            logger.LogInformation(
                "JSONL market-bar index ready: trigger {Trigger}, total {TotalMs:F0} ms (read {ReadMs:F0}, deserialize {DeserializeMs:F0}, index {IndexMs:F0}), {Lines} lines, {Keys} keys.",
                diagnostics.LastIndexLoadTrigger, diagnostics.LastIndexLoadElapsedMs, diagnostics.LastIndexLoadReadMs,
                diagnostics.LastIndexLoadDeserializeMs, diagnostics.LastIndexLoadIndexMs,
                diagnostics.LastIndexLoadLinesRead, diagnostics.IndexedUniqueKeyCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            // A failed warm-up is not fatal: the first request retries the load and surfaces the error.
            logger.LogError(ex, "JSONL market-bar index warm-up failed; the first request will retry the load.");
        }
    }
}
