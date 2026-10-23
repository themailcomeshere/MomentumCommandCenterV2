using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MomentumCommandCenterV2.Application.Interfaces;

namespace MomentumCommandCenterV2.Infrastructure.Persistence;

/// <summary>
/// ADAPTER: JsonlScannerDiscoveryEventStore
/// PURPOSE: Provides append-only local persistence for scanner discovery envelopes using newline-delimited JSON.
/// WHEN CALLED: Used by the provider-neutral ingestion scaffold before an external database is selected.
/// IMPORTANT: This is a durable local prototype, not a multi-process database or a market-data provider.
/// </summary>
public sealed class JsonlScannerDiscoveryEventStore : IScannerDiscoveryEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HashSet<string>? _knownKeys;

    /// <summary>
    /// METHOD: JsonlScannerDiscoveryEventStore
    /// PURPOSE: Initializes an append-only event log at an explicit local path.
    /// WHEN CALLED: Created by the composition root after configuration validation.
    /// </summary>
    public JsonlScannerDiscoveryEventStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A non-empty event-log path is required.", nameof(path));

        _path = Path.GetFullPath(path);
    }

    /// <summary>
    /// METHOD: AppendIfNewAsync
    /// PURPOSE: Serializes one envelope as a single JSON line, flushing it to disk before returning.
    /// WHEN CALLED: Called for each validated scanner observation.
    /// </summary>
    public async Task<bool> AppendIfNewAsync(
        PersistedScannerDiscoveryEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(envelope.IdempotencyKey))
            throw new ArgumentException("An idempotency key is required.", nameof(envelope));
        if (envelope.ReceivedAt == default)
            throw new ArgumentException("A receipt timestamp is required.", nameof(envelope));
        ArgumentNullException.ThrowIfNull(envelope.Discovery);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_knownKeys!.Contains(envelope.IdempotencyKey))
                return false;

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = new FileStream(
                _path, FileMode.Append, FileAccess.Write, FileShare.Read,
                bufferSize: 4096, options: FileOptions.Asynchronous | FileOptions.WriteThrough);
            await JsonSerializer.SerializeAsync(stream, envelope, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { (byte)'\n' }, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);

            _knownKeys.Add(envelope.IdempotencyKey);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// METHOD: ReadAllAsync
    /// PURPOSE: Reads all valid JSONL records in append order; malformed records fail loudly.
    /// WHEN CALLED: Used for deterministic recovery and report input reconstruction.
    /// </summary>
    public async Task<IReadOnlyList<PersistedScannerDiscoveryEnvelope>> ReadAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return Array.Empty<PersistedScannerDiscoveryEnvelope>();

            var events = new List<PersistedScannerDiscoveryEnvelope>();
            using var reader = new StreamReader(new FileStream(
                _path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 4096, options: FileOptions.Asynchronous | FileOptions.SequentialScan));
            var lineNumber = 0;
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                    throw new InvalidDataException($"Discovery event log contains an empty record at line {lineNumber}.");
                try
                {
                    var item = JsonSerializer.Deserialize<PersistedScannerDiscoveryEnvelope>(line, JsonOptions);
                    if (item is null || string.IsNullOrWhiteSpace(item.IdempotencyKey) || item.Discovery is null)
                        throw new JsonException("Required envelope fields are missing.");
                    events.Add(item);
                }
                catch (JsonException exception)
                {
                    throw new InvalidDataException($"Discovery event log is invalid at line {lineNumber}; no records were silently skipped.", exception);
                }
            }

            return events;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// METHOD: CreateFallbackKey
    /// PURPOSE: Builds a deterministic key when the provider does not supply a stable event identifier.
    /// WHEN CALLED: Used only as a documented fallback; source-event IDs are preferred.
    /// LIMITATION: Without a provider event ID, identical observations at different observation times remain distinct.
    /// </summary>
    public static string CreateFallbackKey(
        string sourceId,
        string symbol,
        string scannerGroup,
        DateTimeOffset? sourceTimestamp,
        DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(scannerGroup))
            throw new ArgumentException("Source, symbol, and scanner group are required to create a fallback key.");

        var timestamp = sourceTimestamp?.ToString("O") ?? observedAt.ToString("O");
        var value = $"{sourceId.Trim()}|{symbol.Trim().ToUpperInvariant()}|{scannerGroup.Trim()}|{timestamp}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    /// <summary>
    /// METHOD: EnsureIndexLoadedAsync
    /// PURPOSE: Rebuilds the duplicate index from durable records after process start.
    /// WHEN CALLED: Lazily invoked before the first append.
    /// </summary>
    private async Task EnsureIndexLoadedAsync(CancellationToken cancellationToken)
    {
        if (_knownKeys is not null)
            return;

        var records = await ReadAllWithoutGateAsync(cancellationToken).ConfigureAwait(false);
        _knownKeys = records.Select(record => record.IdempotencyKey).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// METHOD: ReadAllWithoutGateAsync
    /// PURPOSE: Loads existing records while the caller already owns the store gate.
    /// WHEN CALLED: Used during first-write index initialization to avoid re-entrant locking.
    /// </summary>
    private async Task<IReadOnlyList<PersistedScannerDiscoveryEnvelope>> ReadAllWithoutGateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return Array.Empty<PersistedScannerDiscoveryEnvelope>();

        var records = new List<PersistedScannerDiscoveryEnvelope>();
        using var reader = new StreamReader(new FileStream(
            _path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            bufferSize: 4096, options: FileOptions.Asynchronous | FileOptions.SequentialScan));
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                throw new InvalidDataException($"Discovery event log contains an empty record at line {lineNumber}.");
            try
            {
                var record = JsonSerializer.Deserialize<PersistedScannerDiscoveryEnvelope>(line, JsonOptions);
                if (record is null || string.IsNullOrWhiteSpace(record.IdempotencyKey) || record.Discovery is null)
                    throw new JsonException("Required envelope fields are missing.");
                records.Add(record);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Discovery event log is invalid at line {lineNumber}; no records were silently skipped.", exception);
            }
        }

        return records;
    }
}
