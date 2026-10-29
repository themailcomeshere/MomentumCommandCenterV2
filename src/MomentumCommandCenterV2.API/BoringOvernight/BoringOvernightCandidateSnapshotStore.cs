using System.Text.Json;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Append-only audit storage for explicit Boring Overnight evaluation snapshots.
/// WHEN CALLED: A caller submits a candidate snapshot with source and as-of provenance.
/// IMPORTANT: This stores evidence, not truth; it does not validate a provider or infer missing fields.
/// </summary>
public sealed class BoringOvernightCandidateSnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Environment.GetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "boring-overnight-snapshots.jsonl");

    public async Task<BoringOvernightCandidateSnapshot> AppendAsync(
        BoringOvernightCandidateSnapshotInput input,
        CancellationToken cancellationToken)
    {
        Validate(input);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var snapshot = new BoringOvernightCandidateSnapshot(
                Guid.NewGuid().ToString("N"),
                input.Symbol.Trim().ToUpperInvariant(),
                input.AsOfUtc,
                DateTimeOffset.UtcNow,
                input.Source.Trim(),
                input.Provider?.Trim(),
                input.Feed?.Trim(),
                input.CalculationVersion.Trim(),
                input.FieldSourceTimestampsUtc,
                input.EvaluationRequest with { Symbol = input.Symbol.Trim().ToUpperInvariant() });
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(JsonSerializer.Serialize(snapshot, JsonOptions).AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<BoringOvernightCandidateSnapshot>> GetLatestAsync(
        string? symbol, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1–500.");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<BoringOvernightCandidateSnapshot>();
            var rows = new List<BoringOvernightCandidateSnapshot>();
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = JsonSerializer.Deserialize<BoringOvernightCandidateSnapshot>(line, JsonOptions)
                    ?? throw new InvalidDataException("Boring Overnight snapshot JSONL contains an empty record.");
                if (string.IsNullOrWhiteSpace(symbol) || row.Symbol.Equals(symbol.Trim(), StringComparison.OrdinalIgnoreCase))
                    rows.Add(row);
            }
            return rows.OrderByDescending(x => x.AsOfUtc).ThenByDescending(x => x.RecordedAtUtc).Take(limit).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void Validate(BoringOvernightCandidateSnapshotInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Symbol) || input.Symbol.Length > 8 ||
            !input.Symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1–8 letters, digits, dot, or hyphen.");
        if (input.AsOfUtc == default || input.AsOfUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("AsOfUtc must be a non-default UTC timestamp.");
        if (string.IsNullOrWhiteSpace(input.Source))
            throw new ArgumentException("Source is required; identify the source rather than implying validation.");
        if (string.IsNullOrWhiteSpace(input.CalculationVersion))
            throw new ArgumentException("CalculationVersion is required for reproducibility.");
        if (input.EvaluationRequest is null)
            throw new ArgumentException("EvaluationRequest is required.");
        if (!input.EvaluationRequest.Symbol.Equals(input.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Snapshot symbol and evaluation request symbol must match.");
    }
}

public sealed record BoringOvernightCandidateSnapshotInput(
    string Symbol,
    DateTimeOffset AsOfUtc,
    string Source,
    string? Provider,
    string? Feed,
    string CalculationVersion,
    IReadOnlyDictionary<string, DateTimeOffset> FieldSourceTimestampsUtc,
    BoringOvernightEvaluationRequest EvaluationRequest);

public sealed record BoringOvernightCandidateSnapshot(
    string SnapshotId,
    string Symbol,
    DateTimeOffset AsOfUtc,
    DateTimeOffset RecordedAtUtc,
    string Source,
    string? Provider,
    string? Feed,
    string CalculationVersion,
    IReadOnlyDictionary<string, DateTimeOffset> FieldSourceTimestampsUtc,
    BoringOvernightEvaluationRequest EvaluationRequest);
