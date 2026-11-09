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
            var supersedes = (input.SupersedesReviewIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal).ToArray();
            if (supersedes.Length > 0)
            {
                if (!File.Exists(_path))
                    throw new ArgumentException("Superseded review IDs must reference existing saved reviews.", nameof(input));
                var existing = File.ReadLines(_path)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => JsonSerializer.Deserialize<CatalystHumanReviewRecord>(line, JsonOptions))
                    .Where(x => x is not null).ToArray();
                foreach (var reviewId in supersedes)
                {
                    var prior = existing.SingleOrDefault(x => x!.ReviewId.Equals(reviewId, StringComparison.Ordinal));
                    if (prior is null)
                        throw new ArgumentException($"Superseded review ID '{reviewId}' was not found.", nameof(input));
                    var sameArticle = prior.Symbol.Equals(input.Symbol.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrWhiteSpace(prior.ArticleId) ? prior.Url : prior.ArticleId)
                            .Equals(string.IsNullOrWhiteSpace(input.ArticleId) ? input.Url : input.ArticleId, StringComparison.OrdinalIgnoreCase);
                    if (!sameArticle)
                        throw new ArgumentException($"Review ID '{reviewId}' does not belong to this symbol/article.", nameof(input));
                }
            }
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
                DateTimeOffset.UtcNow,
                string.IsNullOrWhiteSpace(input.PredictedSeverity) ? null : input.PredictedSeverity.Trim().ToUpperInvariant(),
                input.PredictedCategories?.Select(x => x.Trim().ToUpperInvariant()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>(),
                supersedes);
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
        if (string.IsNullOrWhiteSpace(input.Source) || input.Source.Length > 200 || IsPlaceholder(input.Source))
            throw new ArgumentException("Source must identify the actual publisher/provider; placeholder values such as 'string' are not accepted.", nameof(input));
        if (!string.IsNullOrWhiteSpace(input.RuleSetVersion) && IsPlaceholder(input.RuleSetVersion))
            throw new ArgumentException("RuleSetVersion must identify the actual triage ruleset; placeholder values such as 'string' are not accepted.", nameof(input));
        if (input.Notes is not null && !HasReviewRationale(input.Notes))
            throw new ArgumentException("Notes must contain at least 40 characters of article-specific evidence and reasoning; placeholder text or appeals to an assistant's recommendation are not accepted.", nameof(input));
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Article URL must be an absolute HTTP or HTTPS URL.", nameof(input));
        if (!AllowedLabels.Contains(input.Label ?? string.Empty))
            throw new ArgumentException("Label must be MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT, DUPLICATE, or UNCERTAIN.", nameof(input));
        if (input.Notes?.Length > 2000)
            throw new ArgumentException("Notes must be at most 2000 characters.", nameof(input));
        if (input.PredictedSeverity is not null &&
            !new[] { "RISK", "MATERIAL", "UNCLASSIFIED" }.Contains(input.PredictedSeverity.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("PredictedSeverity must be RISK, MATERIAL, or UNCLASSIFIED.", nameof(input));
        if (input.PredictedCategories?.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100 || IsPlaceholder(x)) == true)
            throw new ArgumentException("PredictedCategories must contain actual non-placeholder category values of at most 100 characters.", nameof(input));
        if (input.SupersedesReviewIds?.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 64) == true)
            throw new ArgumentException("SupersedesReviewIds must contain non-empty review IDs of at most 64 characters.", nameof(input));
        if (input.SupersedesReviewIds?.Count > 0 && !HasReviewRationale(input.Notes))
            throw new ArgumentException("A source-based, article-specific rationale of at least 40 characters is required when superseding prior reviews.", nameof(input));
    }

    internal static bool HasReviewRationale(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 40 || IsPlaceholder(value))
            return false;
        var normalized = value.Trim().ToLowerInvariant();
        return !new[]
        {
            "because you said", "because chatgpt said", "because the assistant said",
            "you told me", "your recommendation", "assistant recommendation"
        }.Any(normalized.Contains);
    }

    internal static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var normalized = value.Trim();
        return normalized.Equals("string", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("test", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("todo", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("placeholder", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("n/a", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("na", StringComparison.OrdinalIgnoreCase);
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
    string? Notes = null,
    string? PredictedSeverity = null,
    IReadOnlyList<string>? PredictedCategories = null,
    IReadOnlyList<string>? SupersedesReviewIds = null);

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
    DateTimeOffset ReviewedAtUtc,
    string? PredictedSeverity = null,
    IReadOnlyList<string>? PredictedCategories = null,
    IReadOnlyList<string>? SupersedesReviewIds = null);
