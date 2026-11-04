using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Retrieves ticker-scoped Alpaca news and applies transparent keyword triage.
/// This is not a validated catalyst classifier and can never assert CLEAR.
/// </summary>
public sealed class AlpacaCatalystAssessmentClient(HttpClient client, IConfiguration configuration)
{
    private const int PageSize = 50;
    private const int MaxPages = 10;
    private const string RuleSetVersion = "MCC-CATALYST-TRIAGE-1.0";
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"] ?? Environment.GetEnvironmentVariable("ALPACA_API_KEY");
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"] ?? Environment.GetEnvironmentVariable("ALPACA_API_SECRET");

    private static readonly (string Category, string[] Terms)[] SevereRisk =
    {
        ("CAPITAL_RAISE_OR_DILUTION", new[] { "public offering", "registered offering", "at-the-market offering", "dilutive", "dilution", "convertible notes" }),
        ("REGULATORY_OR_LEGAL_RISK", new[] { "sec investigation", "sec charges", "fraud investigation", "class action", "lawsuit", "litigation", "subpoena" }),
        ("SOLVENCY_OR_LISTING_RISK", new[] { "bankruptcy", "chapter 11", "chapter 7", "default", "delisting", "trading halt", "halted" }),
        ("ADVERSE_PRODUCT_OR_REGULATORY_EVENT", new[] { "fda rejection", "clinical hold", "product recall", "safety recall" })
    };

    private static readonly (string Category, string[] Terms)[] MaterialEvents =
    {
        ("EARNINGS_OR_GUIDANCE", new[] { "earnings", "quarterly results", "full-year results", "guidance", "outlook", "revenue forecast" }),
        ("M_AND_A", new[] { "merger", "acquisition", "to acquire", "acquires", "takeover", "buyout" }),
        ("FDA_OR_CLINICAL", new[] { "fda", "clinical trial", "phase 1", "phase 2", "phase 3", "clinical data" }),
        ("CONTRACT_OR_CORPORATE_ACTION", new[] { "awarded contract", "contract win", "dividend", "stock split", "restructuring", "layoff" }),
        ("ANALYST_ACTION", new[] { "price target", "upgraded", "downgraded", "initiated coverage" })
    };

    public async Task<CatalystAssessment> AssessAsync(string symbol, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_key) || string.IsNullOrWhiteSpace(_secret))
            throw new ProviderNotConfiguredException("Catalyst news requires server-side Alpaca API credentials.");
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8 ||
            !symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1-8 letters, digits, dot, or hyphen.", nameof(symbol));

        var from = nowUtc.AddHours(-24);
        var articles = new List<CatalystNewsArticle>();
        string? pageToken = null;
        var pages = 0;
        do
        {
            var url = $"v1beta1/news?symbols={Uri.EscapeDataString(symbol.ToUpperInvariant())}" +
                $"&start={Uri.EscapeDataString(from.UtcDateTime.ToString("O"))}" +
                $"&end={Uri.EscapeDataString(nowUtc.UtcDateTime.ToString("O"))}" +
                $"&limit={PageSize}&sort=desc" +
                (string.IsNullOrWhiteSpace(pageToken) ? "" : $"&page_token={Uri.EscapeDataString(pageToken)}");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("APCA-API-KEY-ID", _key);
            request.Headers.Add("APCA-API-SECRET-KEY", _secret);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Alpaca ticker news returned {(int)response.StatusCode} {response.ReasonPhrase}: {(body.Length > 400 ? body[..400] : body)}");
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (!root.TryGetProperty("news", out var news) || news.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Alpaca ticker news response did not contain a news array.");

            foreach (var item in news.EnumerateArray())
            {
                var headline = item.TryGetProperty("headline", out var h) ? h.GetString() : null;
                if (string.IsNullOrWhiteSpace(headline)) continue;
                var published = item.TryGetProperty("created_at", out var created) &&
                    DateTimeOffset.TryParse(created.GetString(), out var parsed) ? parsed : (DateTimeOffset?)null;
                var symbols = item.TryGetProperty("symbols", out var symbolArray) && symbolArray.ValueKind == JsonValueKind.Array
                    ? symbolArray.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
                    : Array.Empty<string>();
                articles.Add(new CatalystNewsArticle(
                    item.TryGetProperty("id", out var id) ? id.ToString() : null,
                    headline,
                    item.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                    published,
                    item.TryGetProperty("url", out var articleUrl) ? articleUrl.GetString() : null,
                    symbols));
            }
            pageToken = root.TryGetProperty("next_page_token", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString() : null;
            pages++;
        } while (!string.IsNullOrWhiteSpace(pageToken) && pages < MaxPages);

        var coverageComplete = string.IsNullOrWhiteSpace(pageToken);
        var unique = articles.GroupBy(x => x.Id ?? x.Headline, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()).OrderByDescending(x => x.PublishedAtUtc).ToArray();
        var assessments = unique.Select(Classify).ToArray();
        var severe = assessments.Where(x => x.Severity == "RISK").ToArray();
        var material = assessments.Where(x => x.Severity == "MATERIAL").ToArray();
        var status = severe.Length > 0 ? "RISK_PRESENT" :
            material.Length > 0 ? "MATERIAL_EVENT_REVIEW" :
            unique.Length > 0 ? "UNCLASSIFIED_NEWS_REVIEW" : "UNKNOWN";

        return new CatalystAssessment(
            symbol.ToUpperInvariant(), "Alpaca News", from, nowUtc, nowUtc, coverageComplete,
            pages, unique.Length, status, RuleSetVersion, assessments,
            "PROVISIONAL TRIAGE ONLY. No article-level human validation or historical classifier validation is represented. No news returned is UNKNOWN, not CLEAR. This endpoint never emits CLEAR_WITH_SOURCE; review primary filings and other authoritative sources before any clearance.");
    }

    private static CatalystArticleAssessment Classify(CatalystNewsArticle article)
    {
        var text = (article.Headline + " " + article.Summary).ToLowerInvariant();
        var severe = SevereRisk.Where(group => group.Terms.Any(text.Contains)).Select(group => group.Category).ToArray();
        if (severe.Length > 0)
            return new CatalystArticleAssessment(article, "RISK", severe, "Rule-based severe-risk phrase match; human review required.");
        var material = MaterialEvents.Where(group => group.Terms.Any(text.Contains)).Select(group => group.Category).ToArray();
        if (material.Length > 0)
            return new CatalystArticleAssessment(article, "MATERIAL", material, "Potentially material event phrase match; human review required.");
        return new CatalystArticleAssessment(article, "UNCLASSIFIED", Array.Empty<string>(),
            "Ticker-linked news found but no configured phrase matched; manual review required.");
    }
}

public sealed record CatalystNewsArticle(string? Id, string Headline, string? Summary,
    DateTimeOffset? PublishedAtUtc, string? Url, IReadOnlyList<string> Symbols);
public sealed record CatalystArticleAssessment(CatalystNewsArticle Article, string Severity,
    IReadOnlyList<string> Categories, string Reason);
public sealed record CatalystAssessment(string Symbol, string Provider, DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc, DateTimeOffset RetrievedAtUtc, bool CoverageComplete,
    int PagesRead, int ArticleCount, string Status, string RuleSetVersion,
    IReadOnlyList<CatalystArticleAssessment> Articles, string Note);
