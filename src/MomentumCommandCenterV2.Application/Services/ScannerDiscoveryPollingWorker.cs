using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Services;

/// <summary>
/// SERVICE: ScannerDiscoveryPollingWorker
/// PURPOSE: Provides a cancellable, provider-neutral polling loop that can be hosted by a future process.
/// WHEN CALLED: Started by a composition root after a real IScannerDiscoverySource is configured.
/// LIMITATION: No provider implementation or background host is wired into the application yet.
/// </summary>
public sealed class ScannerDiscoveryPollingWorker
{
    private readonly IScannerDiscoverySource _source;
    private readonly ScannerDiscoveryIngestionService _ingestion;
    private readonly TimeSpan _pollInterval;
    private readonly object _healthLock = new();
    private ScannerCollectorHealthSnapshot _health = ScannerCollectorHealthSnapshot.Unknown();

    /// <summary>
    /// METHOD: ScannerDiscoveryPollingWorker
    /// PURPOSE: Validates dependencies and configures a positive polling interval.
    /// WHEN CALLED: Constructed by a future host after provider selection.
    /// </summary>
    public ScannerDiscoveryPollingWorker(
        IScannerDiscoverySource source,
        ScannerDiscoveryIngestionService ingestion,
        TimeSpan pollInterval)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
        if (pollInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollInterval), "Polling interval must be positive.");
        _pollInterval = pollInterval;
    }

    /// <summary>
    /// PROPERTY: Health
    /// PURPOSE: Returns a thread-safe snapshot of collector status and recent errors.
    /// WHEN CALLED: Read by a future status endpoint, CLI, or Morning Report generator.
    /// </summary>
    public ScannerCollectorHealthSnapshot Health
    {
        get { lock (_healthLock) return _health; }
    }

    /// <summary>
    /// METHOD: RunAsync
    /// PURPOSE: Polls the configured source, persists each batch, and degrades health on errors without fabricating observations.
    /// WHEN CALLED: Invoked by a long-running host; cancellation requests a graceful stop.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        UpdateHealth(current => current with
        {
            Status = ScannerCollectorHealthStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            LastErrorCategory = null,
            LastErrorAt = null
        });

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var attemptAt = DateTimeOffset.UtcNow;
                UpdateHealth(current => current with { LastAttemptAt = attemptAt });

                try
                {
                    var discoveries = await _source.GetDiscoveriesAsync(attemptAt, cancellationToken).ConfigureAwait(false);
                    if (discoveries is null)
                        throw new InvalidDataException("Scanner source returned a null batch.");

                    var receivedAt = DateTimeOffset.UtcNow;
                    var result = await _ingestion.IngestAsync(discoveries, receivedAt, cancellationToken).ConfigureAwait(false);
                    UpdateHealth(current => current with
                    {
                        Status = ScannerCollectorHealthStatus.Running,
                        LastSuccessAt = receivedAt,
                        ConsecutiveFailures = 0,
                        LastErrorCategory = null,
                        LastErrorAt = null,
                        LastBatchReceivedCount = result.ReceivedCount,
                        LastBatchAppendedCount = result.AppendedCount,
                        LastBatchDuplicateCount = result.DuplicateCount
                    });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    UpdateHealth(current => current with
                    {
                        Status = ScannerCollectorHealthStatus.Degraded,
                        ConsecutiveFailures = current.ConsecutiveFailures + 1,
                        LastErrorCategory = exception.GetType().Name,
                        LastErrorAt = DateTimeOffset.UtcNow
                    });
                }

                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is an expected shutdown path; no error or fabricated success is recorded.
        }
        finally
        {
            UpdateHealth(current => current with { Status = ScannerCollectorHealthStatus.Stopped });
        }
    }

    /// <summary>
    /// METHOD: UpdateHealth
    /// PURPOSE: Applies a health-state transition under a lock so readers receive a consistent snapshot.
    /// WHEN CALLED: Used by startup, polling, error handling, and shutdown paths.
    /// </summary>
    private void UpdateHealth(Func<ScannerCollectorHealthSnapshot, ScannerCollectorHealthSnapshot> update)
    {
        lock (_healthLock)
            _health = update(_health);
    }
}

/// <summary>
/// ENUM: ScannerCollectorHealthStatus
/// PURPOSE: Describes the lifecycle state of the provider-neutral scanner collector.
/// WHEN CALLED: Exposed in diagnostics and eventually the Morning Report.
/// </summary>
public enum ScannerCollectorHealthStatus
{
    Unknown = 0,
    Running = 1,
    Degraded = 2,
    Stopped = 3
}

/// <summary>
/// TYPE: ScannerCollectorHealthSnapshot
/// PURPOSE: Captures the most recent collector health state without exposing credentials or raw exception text.
/// WHEN CALLED: Queried by the host, status endpoint, or report generator.
/// </summary>
public sealed record ScannerCollectorHealthSnapshot(
    ScannerCollectorHealthStatus Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessAt,
    int ConsecutiveFailures,
    string? LastErrorCategory,
    DateTimeOffset? LastErrorAt,
    int LastBatchReceivedCount,
    int LastBatchAppendedCount,
    int LastBatchDuplicateCount)
{
    /// <summary>
    /// METHOD: Unknown
    /// PURPOSE: Creates the initial health state before collection starts.
    /// WHEN CALLED: Used by worker construction.
    /// </summary>
    public static ScannerCollectorHealthSnapshot Unknown() =>
        new(ScannerCollectorHealthStatus.Unknown, null, null, null, 0, null, null, 0, 0, 0);
}
