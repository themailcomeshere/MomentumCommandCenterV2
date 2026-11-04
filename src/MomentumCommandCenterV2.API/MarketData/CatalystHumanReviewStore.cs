using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Append-only human labels for source-backed catalyst articles.
/// Labels are research data only; this store never changes an assessment to CLEAR.
/// </summary>
public sealed class CatalystHumanReviewStore(string? path = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "MATERIAL_RISK",
        "MATERIAL_EVENT",
        "NON_MATERIAL",
        "IRRELEVANT",
        "DUPLICATE",
        "UNCERTAIN"
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = path
        ?? Environment.GetEnvironmentVariable("MCC_CATALYST_REVIEW_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "catalyst-human-reviews.jsonl");

    public async Task<CatalystHumanReviewRecord> AppendAsync(
        CatalystHumanReviewInput input,
        CancellationToken cancellationToken)
    {
        Validate(input);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var record = new CatalystHumanReviewRecord(
                Guid.NewGuid().ToString("N"),
                input.Symbol.Trim().ToUpperInvariant(),
                input.ArticleId?.Trim(),
                input.Headline.Trim(),
                input.Url.Trim(),
                input.PublishedAtUtc,
                input.Source.Trim(),
                input.Label.Trim().ToUpperInvariant(),
                input.RuleSetVersion?.Trim() ?? "MCC-CATALYST-TRIAGE-1.0",
                input.Notes?.Trim(),
                DateTimeOffset.UtcNow);
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read,
                16 * 1024, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(JsonSerializer.Serialize(record, JsonOptions).AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            return record;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<CatalystHumanReviewRecord>> GetLatestAsync(
        string? symbol,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1–500.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path))
                return Array.Empty<CatalystHumanReviewRecord>();

            var rows = new List<CatalystHumanReviewRecord>();
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var record = JsonSerializer.Deserialize<CatalystHumanReviewRecord>(line, JsonOptions)
                    ?? throw new InvalidDataException("Catalyst review JSONL contains an empty record.");
                if (!string.IsNullOrWhiteSpace(symbol) &&
                    !record.Symbol.Equals(symbol.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                rows.Add(record);
            }

            return rows.OrderByDescending(x => x.ReviewedAtUtc).Take(limit).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void Validate(CatalystHumanReviewInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Symbol) || input.Symbol.Length > 8 ||
            !input.Symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1–8 letters, digits, dot, or hyphen.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.Headline) || input.Headline.Length > 1000)
            throw new ArgumentException("Headline is required and must be at most 1000 characters.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.Source) || input.Source.Length > 200)
            throw new ArgumentException("Source is required and must be at most 200 characters.", nameof(input));
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Article URL must be an absolute HTTP or HTTPS URL.", nameof(input));
        if (!AllowedLabels.Contains(input.Label ?? string.Empty))
            throw new ArgumentException("Label must be MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT, DUPLICATE, or UNCERTAIN.", nameof(input));
        if (input.Notes?.Length > 2000)
            throw new ArgumentException("Notes must be at most 2000 characters.", nameof(input));
    }
}

public sealed record CatalystHumanReviewInput(
    string Symbol,
    string? ArticleId,
    string Headline,
    string Url,
    DateTimeOffset? PublishedAtUtc,
    string Source,
    string Label,
    string? RuleSetVersion = null,
    string? Notes = null);

public sealed record CatalystHumanReviewRecord(
    string ReviewId,
    string Symbol,
    string? ArticleId,
    string Headline,
    string Url,
    DateTimeOffset? PublishedAtUtc,
    string Source,
    string Label,
    string RuleSetVersion,
    string? Notes,
    DateTimeOffset ReviewedAtUtc);
