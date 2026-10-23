namespace MomentumCommandCenterV2.Application.Interfaces;

/// <summary>
/// INTERFACE: IScannerDiscoveryEventStore
/// PURPOSE: Defines durable append-only storage for scanner observations.
/// WHEN CALLED: Ingestion writes a validated event before it is published to downstream workflows.
/// </summary>
public interface IScannerDiscoveryEventStore
{
    /// <summary>
    /// METHOD: AppendIfNewAsync
    /// PURPOSE: Appends an event only when its idempotency key has not been persisted before.
    /// WHEN CALLED: Called after source payload validation and before downstream ranking.
    /// RETURNS: True when appended; false when the event was already recorded.
    /// </summary>
    Task<bool> AppendIfNewAsync(
        PersistedScannerDiscoveryEnvelope envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// METHOD: ReadAllAsync
    /// PURPOSE: Reads persisted envelopes in append order without modifying the event log.
    /// WHEN CALLED: Used by recovery, report generation, and diagnostics.
    /// </summary>
    Task<IReadOnlyList<PersistedScannerDiscoveryEnvelope>> ReadAllAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TYPE: PersistedScannerDiscoveryEnvelope
/// PURPOSE: Wraps a discovery event with receipt time and a stable idempotency key.
/// WHEN CALLED: Created by ingestion immediately before durable persistence.
/// </summary>
public sealed record PersistedScannerDiscoveryEnvelope(
    string IdempotencyKey,
    DateTimeOffset ReceivedAt,
    ScannerDiscoveryEvent Discovery);
