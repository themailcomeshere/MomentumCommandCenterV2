using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MorningBrief;

/// <summary>
/// PURPOSE: Persist immutable, versioned Morning Brief response snapshots for audit and later comparison.
/// WHEN CALLED: A newly generated Morning Brief is returned by the API.
/// IMPORTANT: A snapshot records the report as generated; it does not upgrade partial or unknown source data.
/// </summary>
public sealed class MorningBriefSnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Environment.GetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "morning-brief-snapshots.jsonl");

    /// <summary>
    /// PURPOSE: Append one immutable report payload with schema, generation time, and content hash.
    /// WHEN CALLED: After a complete report object has been assembled and before it is cached.
    /// </summary>
    public async Task<MorningBriefSnapshot> AppendAsync(
        JsonElement report,
        string reportVersion,
        CancellationToken cancellationToken)
    {
        if (report.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Morning Brief report must be a JSON object.", nameof(report));
        if (string.IsNullOrWhiteSpace(reportVersion))
            throw new ArgumentException("Report version is required.", nameof(reportVersion));
        if (!report.TryGetProperty("generatedAtUtc", out var generatedAtElement) ||
            generatedAtElement.ValueKind != JsonValueKind.String ||
            !generatedAtElement.TryGetDateTimeOffset(out var generatedAtUtc) ||
            generatedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Report generatedAtUtc must be an ISO-8601 UTC timestamp.", nameof(report));

        var reportJson = report.GetRawText();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reportJson)));
        var snapshot = new MorningBriefSnapshot(
            Guid.NewGuid().ToString("N"),
            generatedAtUtc,
            DateTimeOffset.UtcNow,
            reportVersion.Trim(),
            hash,
            ExtractSourceWatermarksUtc(report),
            report.Clone());

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteLineAsync(JsonSerializer.Serialize(snapshot, JsonOptions).AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }


    /// <summary>
    /// PURPOSE: Capture source-bar watermarks embedded in the generated market-landscape section.
    /// WHEN CALLED: A report snapshot is created.
    /// IMPORTANT: Only explicit UTC source timestamps with provider/symbol/timeframe are recorded.
    /// </summary>
    private static IReadOnlyDictionary<string, DateTimeOffset> ExtractSourceWatermarksUtc(JsonElement report)
    {
        var watermarks = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        if (!report.TryGetProperty("marketLandscape", out var landscape) || landscape.ValueKind != JsonValueKind.Array)
            return watermarks;

        foreach (var row in landscape.EnumerateArray())
        {
            var symbol = ReadString(row, "symbol");
            if (symbol is null || !row.TryGetProperty("observations", out var observations) ||
                observations.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var observation in observations.EnumerateArray())
            {
                var provider = ReadString(observation, "provider");
                if (provider is null || !observation.TryGetProperty("latest", out var latest) ||
                    latest.ValueKind != JsonValueKind.Object ||
                    !latest.TryGetProperty("timestamp", out var timestampElement) ||
                    !timestampElement.TryGetDateTimeOffset(out var timestampUtc) ||
                    timestampUtc.Offset != TimeSpan.Zero)
                    continue;

                var timeframe = ReadString(latest, "timeframe");
                if (timeframe is null) continue;
                var feed = ReadString(latest, "feed") ?? "UNKNOWN";
                var transport = ReadString(latest, "sourceTransport") ?? "UNKNOWN";
                var key = $"{provider}/{symbol.ToUpperInvariant()}/{timeframe}/{feed}/{transport}";
                if (!watermarks.TryGetValue(key, out var previous) || timestampUtc > previous)
                    watermarks[key] = timestampUtc;
            }
        }

        return watermarks;
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// PURPOSE: Read the newest immutable report snapshots without modifying the JSONL history.
    /// WHEN CALLED: GET /api/morning-brief/history.
    /// </summary>
    public async Task<IReadOnlyList<MorningBriefSnapshot>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1–200.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<MorningBriefSnapshot>();
            var snapshots = new List<MorningBriefSnapshot>();
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var snapshot = JsonSerializer.Deserialize<MorningBriefSnapshot>(line, JsonOptions)
                    ?? throw new InvalidDataException("Morning Brief snapshot JSONL contains an empty record.");
                snapshots.Add(snapshot);
            }

            return snapshots
                .OrderByDescending(x => x.GeneratedAtUtc)
                .ThenByDescending(x => x.RecordedAtUtc)
                .Take(limit)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed record MorningBriefSnapshot(
    string SnapshotId,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset RecordedAtUtc,
    string ReportVersion,
    string ContentSha256,
    IReadOnlyDictionary<string, DateTimeOffset> SourceWatermarksUtc,
    JsonElement Report);
