using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Append-only, idempotent local persistence for Thinkorswim ATR V2 observations.
/// Kept separate from provider OHLCV so chart observations cannot be mistaken for SIP/IEX bars.
/// </summary>
public sealed class JsonlThinkorswimAtrObservationStore : IThinkorswimAtrObservationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Environment.GetEnvironmentVariable("MCC_TOS_ATR_STORE_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "thinkorswim-atr-observations.jsonl");
    private readonly Dictionary<string, ThinkorswimAtrStoredObservation> _latest = new(StringComparer.Ordinal);
    private bool _loaded;

    public async Task<ThinkorswimAtrAppendResult> AppendAsync(
        IReadOnlyCollection<StrategyReportAtrObservation> observations,
        StrategyReportAtrImportMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(metadata);
        if (observations.Count == 0) return new(0, 0, 0);
        ValidateMetadata(metadata);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var appended = 0;
            var skipped = 0;
            var corrections = 0;
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            foreach (var item in observations)
            {
                ValidateObservation(item);
                var key = Identity(item, metadata);
                var hash = Hash(item, metadata);
                if (_latest.TryGetValue(key, out var previous) && previous.ContentHash == hash)
                {
                    skipped++;
                    continue;
                }
                var stored = new ThinkorswimAtrStoredObservation(
                    Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, metadata, item, hash,
                    previous?.ObservationId);
                await writer.WriteLineAsync(JsonSerializer.Serialize(stored, JsonOptions).AsMemory(), cancellationToken);
                _latest[key] = stored;
                appended++;
                if (stored.SupersedesObservationId is not null) corrections++;
            }
            await writer.FlushAsync(cancellationToken);
            return new(appended, skipped, corrections);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<ThinkorswimAtrStoredObservation>> GetLatestAsync(
        string? symbol, string? timeframe, DateTimeOffset? fromUtc, DateTimeOffset? toUtc,
        int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1–10000.");
        if (fromUtc is not null && fromUtc.Value.Offset != TimeSpan.Zero ||
            toUtc is not null && toUtc.Value.Offset != TimeSpan.Zero ||
            fromUtc is not null && toUtc is not null && fromUtc > toUtc)
            throw new ArgumentException("Optional time bounds must be UTC and fromUtc must not exceed toUtc.");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _latest.Values
                .Where(x => symbol is null || x.Observation.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(x => timeframe is null || x.Observation.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase))
                .Where(x => fromUtc is null || x.Observation.SourceTimestampUtc >= fromUtc.Value)
                .Where(x => toUtc is null || x.Observation.SourceTimestampUtc <= toUtc.Value)
                .OrderBy(x => x.Observation.SourceTimestampUtc)
                .TakeLast(limit).ToArray();
        }
        finally { _gate.Release(); }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var stored = JsonSerializer.Deserialize<ThinkorswimAtrStoredObservation>(line, JsonOptions)
                    ?? throw new InvalidDataException("Thinkorswim ATR JSONL contains an empty record.");
                _latest[Identity(stored.Observation, stored.Metadata)] = stored;
            }
        }
        _loaded = true;
    }

    private static string Identity(StrategyReportAtrObservation item, StrategyReportAtrImportMetadata metadata) =>
        string.Join("|", item.Symbol.ToUpperInvariant(), item.Timeframe, item.SourceTimestampUtc.ToUniversalTime().ToString("O"),
            metadata.AtrPeriod, metadata.AtrAverageType.Trim().ToUpperInvariant(), metadata.StudyVersion.Trim(),
            metadata.ChartTimezone.Trim(), metadata.ExtendedHours, metadata.CorporateActionAdjustment);

    private static string Hash(StrategyReportAtrObservation item, StrategyReportAtrImportMetadata metadata)
    {
        var value = string.Join("|", Identity(item, metadata),
            item.Open.ToString(CultureInfo.InvariantCulture),
            item.High.ToString(CultureInfo.InvariantCulture),
            item.Low.ToString(CultureInfo.InvariantCulture),
            item.Close.ToString(CultureInfo.InvariantCulture),
            item.Volume.ToString(CultureInfo.InvariantCulture),
            item.Atr.ToString(CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static void ValidateMetadata(StrategyReportAtrImportMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.SourceFileName) || string.IsNullOrWhiteSpace(metadata.StudyVersion) ||
            string.IsNullOrWhiteSpace(metadata.ChartTimezone) || metadata.AtrPeriod is < 1 or > 200 ||
            !metadata.AtrAverageType.Equals("Wilder", StringComparison.OrdinalIgnoreCase) ||
            metadata.ImportedAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(metadata.ImportBatchId))
            throw new InvalidDataException("ATR import metadata requires source filename, study version, chart timezone, UTC import time, batch ID, ATR period 1–200 and Wilder average type.");
    }

    private static void ValidateObservation(StrategyReportAtrObservation item)
    {
        if (string.IsNullOrWhiteSpace(item.Symbol) || item.SourceTimestampUtc.Offset != TimeSpan.Zero ||
            item.Open <= 0 || item.High < Math.Max(item.Open, item.Close) || item.Low > Math.Min(item.Open, item.Close) ||
            item.Low <= 0 || item.Low > item.High || item.Close <= 0 || item.Volume < 0 || item.Atr < 0)
            throw new InvalidDataException($"Invalid Thinkorswim ATR observation for {item.Symbol} at {item.SourceTimestampUtc:O}.");
    }
}

public interface IThinkorswimAtrObservationStore
{
    Task<ThinkorswimAtrAppendResult> AppendAsync(
        IReadOnlyCollection<StrategyReportAtrObservation> observations,
        StrategyReportAtrImportMetadata metadata,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ThinkorswimAtrStoredObservation>> GetLatestAsync(
        string? symbol, string? timeframe, DateTimeOffset? fromUtc, DateTimeOffset? toUtc,
        int limit, CancellationToken cancellationToken);
}

public sealed record StrategyReportAtrImportMetadata(
    string SourceFileName,
    string StudyVersion,
    int AtrPeriod,
    string AtrAverageType,
    string ChartTimezone,
    bool ExtendedHours,
    bool CorporateActionAdjustment,
    string ImportBatchId,
    DateTimeOffset ImportedAtUtc);

public sealed record ThinkorswimAtrStoredObservation(
    string ObservationId,
    DateTimeOffset ReceivedAtUtc,
    StrategyReportAtrImportMetadata Metadata,
    StrategyReportAtrObservation Observation,
    string ContentHash,
    string? SupersedesObservationId);

public sealed record ThinkorswimAtrAppendResult(int AppendedCount, int IdenticalSkippedCount, int CorrectionCount);
