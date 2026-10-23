using System.Security.Cryptography;
using System.Text;
using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Services;

/// <summary>
/// SERVICE: ScannerDiscoveryIngestionService
/// PURPOSE: Validates provider-neutral scanner events and persists them before downstream use.
/// WHEN CALLED: Called by a future polling/streaming worker after a source adapter returns observations.
/// IMPORTANT: This service does not poll a provider, rank candidates, or create trading permission.
/// </summary>
public sealed class ScannerDiscoveryIngestionService
{
    private readonly IScannerDiscoveryEventStore _store;

    /// <summary>
    /// METHOD: ScannerDiscoveryIngestionService
    /// PURPOSE: Binds ingestion to the durable event-store abstraction.
    /// WHEN CALLED: Constructed by the application composition root.
    /// </summary>
    public ScannerDiscoveryIngestionService(IScannerDiscoveryEventStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// METHOD: IngestAsync
    /// PURPOSE: Validates, assigns an idempotency key, and persists a batch of scanner discoveries.
    /// WHEN CALLED: Invoked by a future provider polling/streaming worker.
    /// </summary>
    public async Task<ScannerDiscoveryIngestionResult> IngestAsync(
        IReadOnlyCollection<ScannerDiscoveryEvent> discoveries,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(discoveries);
        if (receivedAt == default)
            throw new ArgumentException("Receipt time must be explicitly supplied.", nameof(receivedAt));

        var appended = 0;
        var duplicates = 0;
        foreach (var discovery in discoveries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Validate(discovery);
            var key = CreateIdempotencyKey(discovery);
            var envelope = new PersistedScannerDiscoveryEnvelope(key, receivedAt, discovery);
            if (await _store.AppendIfNewAsync(envelope, cancellationToken).ConfigureAwait(false))
                appended++;
            else
                duplicates++;
        }

        return new ScannerDiscoveryIngestionResult(discoveries.Count, appended, duplicates, receivedAt);
    }

    /// <summary>
    /// METHOD: Validate
    /// PURPOSE: Rejects malformed events rather than silently repairing missing source data.
    /// WHEN CALLED: Before an event is passed to persistence.
    /// </summary>
    private static void Validate(ScannerDiscoveryEvent discovery)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        if (string.IsNullOrWhiteSpace(discovery.Symbol))
            throw new ArgumentException("Discovery symbol is required.", nameof(discovery));
        if (string.IsNullOrWhiteSpace(discovery.ScannerGroup))
            throw new ArgumentException("Scanner group is required.", nameof(discovery));
        if (string.IsNullOrWhiteSpace(discovery.SourceId))
            throw new ArgumentException("Source identifier is required.", nameof(discovery));
        if (string.IsNullOrWhiteSpace(discovery.SchemaVersion))
            throw new ArgumentException("Schema version is required.", nameof(discovery));
        if (discovery.FirstSeenAt == default || discovery.ObservedAt == default || discovery.EvaluatedAt == default)
            throw new ArgumentException("First-seen, observed, and evaluated timestamps must be explicit.", nameof(discovery));
        if (discovery.ObservedAt < discovery.FirstSeenAt)
            throw new ArgumentException("ObservedAt cannot precede FirstSeenAt.", nameof(discovery));
        if (discovery.EvaluatedAt < discovery.ObservedAt)
            throw new ArgumentException("EvaluatedAt cannot precede ObservedAt.", nameof(discovery));
        if (discovery.RawFields is null || discovery.ReasonCodes is null)
            throw new ArgumentException("Raw fields and reason codes must be present; use empty collections when none exist.", nameof(discovery));
    }

    /// <summary>
    /// METHOD: CreateIdempotencyKey
    /// PURPOSE: Prefers provider event IDs and otherwise creates a stable composite key from source identity and timestamp.
    /// WHEN CALLED: Called before each persistence attempt.
    /// LIMITATION: Without a stable provider event ID, two identical events at different source/observation timestamps remain distinct.
    /// </summary>
    private static string CreateIdempotencyKey(ScannerDiscoveryEvent discovery)
    {
        if (!string.IsNullOrWhiteSpace(discovery.SourceEventId))
            return $"{discovery.SourceId.Trim()}:{discovery.SourceEventId.Trim()}";

        var timestamp = discovery.SourceTimestamp?.ToString("O") ?? discovery.ObservedAt.ToString("O");
        var composite = $"{discovery.SourceId.Trim()}|{discovery.Symbol.Trim().ToUpperInvariant()}|{discovery.ScannerGroup.Trim()}|{timestamp}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(composite)));
    }
}

/// <summary>
/// TYPE: ScannerDiscoveryIngestionResult
/// PURPOSE: Reports how many events were accepted and deduplicated for a single ingestion batch.
/// WHEN CALLED: Returned to collector health and diagnostics.
/// </summary>
public sealed record ScannerDiscoveryIngestionResult(
    int ReceivedCount,
    int AppendedCount,
    int DuplicateCount,
    DateTimeOffset ReceivedAt);
