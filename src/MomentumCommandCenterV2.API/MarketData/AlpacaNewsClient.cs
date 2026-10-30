using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Retrieves timestamped Alpaca news items. Headlines are exposed as unclassified source material;
/// no catalyst type, sentiment, or trading decision is inferred from headline text.
/// </summary>
public sealed class AlpacaNewsClient(HttpClient client, IConfiguration configuration)
{
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"] ?? Environment.GetEnvironmentVariable("ALPACA_API_KEY");
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"] ?? Environment.GetEnvironmentVariable("ALPACA_API_SECRET");
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_key) && !string.IsNullOrWhiteSpace(_secret);

    public async Task<IReadOnlyList<AlpacaNewsItem>> GetLatestAsync(int limit, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Alpaca news requires server-side API credentials.");
        if (limit is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(limit));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1beta1/news?limit={limit}&sort=desc");
        request.Headers.Add("APCA-API-KEY-ID", _key!);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret!);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpaca news returned {(int)response.StatusCode}: {(body.Length > 400 ? body[..400] : body)}");
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("news", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Alpaca news response did not contain a news array.");

        var result = new List<AlpacaNewsItem>();
        foreach (var item in items.EnumerateArray())
        {
            var headline = item.TryGetProperty("headline", out var h) ? h.GetString() : null;
            if (string.IsNullOrWhiteSpace(headline)) continue;
            var created = item.TryGetProperty("created_at", out var ca) && DateTimeOffset.TryParse(ca.GetString(), out var createdAt)
                ? createdAt : (DateTimeOffset?)null;
            var updated = item.TryGetProperty("updated_at", out var ua) && DateTimeOffset.TryParse(ua.GetString(), out var updatedAt)
                ? updatedAt : (DateTimeOffset?)null;
            var symbols = item.TryGetProperty("symbols", out var symbolArray) && symbolArray.ValueKind == JsonValueKind.Array
                ? symbolArray.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
                : Array.Empty<string>();
            result.Add(new AlpacaNewsItem(
                item.TryGetProperty("id", out var id) ? id.ToString() : null,
                headline,
                item.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                item.TryGetProperty("author", out var author) ? author.GetString() : null,
                created, updated,
                item.TryGetProperty("url", out var url) ? url.GetString() : null,
                symbols,
                "UNCLASSIFIED — SOURCE HEADLINE ONLY"));
        }
        return result;
    }
}

public sealed record AlpacaNewsItem(
    string? Id, string Headline, string? Summary, string? Author,
    DateTimeOffset? PublishedAtUtc, DateTimeOffset? UpdatedAtUtc,
    string? Url, IReadOnlyList<string> Symbols, string Classification);
