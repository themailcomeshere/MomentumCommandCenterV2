using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.Infrastructure.Alpaca;

// ============================================================
// CLASS: AlpacaApiClient
// PURPOSE:
//   Performs read-only Alpaca authentication and market-data calls.
// SAFETY:
//   This client exposes no order-placement methods.
// ============================================================

/// <summary>
/// Minimal market-data bar returned by Alpaca's latest-bar endpoint.
/// </summary>
public sealed record AlpacaLatestBar(
    string Symbol,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

/// <summary>
/// Minimal article metadata returned by Alpaca's News API.
/// </summary>
public sealed record AlpacaNewsArticle(
    string Id,
    string Headline,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Url,
    IReadOnlyList<string> Symbols);

/// <summary>
/// Safe summary of a successful account-authentication check.
/// Account identifiers and credential values are intentionally not returned.
/// </summary>
public sealed record AlpacaConnectionResult(
    bool IsConnected,
    string AccountStatus,
    string Message);

/// <summary>
/// Read-only API client used for the initial Alpaca provider smoke test.
/// </summary>
public sealed class AlpacaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AlpacaOptions _options;

    public AlpacaApiClient(HttpClient httpClient, AlpacaOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Calls GET /v2/account to verify the supplied credentials without submitting an order.
    /// </summary>
    public async Task<AlpacaConnectionResult> ValidateConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        _options.Validate();

        using var request = CreateRequest(
            HttpMethod.Get,
            CombineUrl(_options.TradingBaseUrl, "/v2/account"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Alpaca account check failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var accountStatus = document.RootElement.TryGetProperty("status", out var status)
            ? status.GetString() ?? "unknown"
            : "unknown";

        return new AlpacaConnectionResult(
            true,
            accountStatus,
            "Alpaca authentication succeeded. No order was submitted.");
    }

    /// <summary>
    /// Retrieves the latest bar for a symbol using the configured Alpaca market-data feed.
    /// </summary>
    public async Task<AlpacaLatestBar> GetLatestBarAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        _options.Validate();

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A stock symbol is required.", nameof(symbol));

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var escapedSymbol = Uri.EscapeDataString(normalizedSymbol);
        var escapedFeed = Uri.EscapeDataString(_options.MarketDataFeed);
        var url = CombineUrl(
            _options.MarketDataBaseUrl,
            $"/v2/stocks/{escapedSymbol}/bars/latest?feed={escapedFeed}");

        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Alpaca latest-bar request for {normalizedSymbol} failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("bar", out var bar) ||
            bar.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException(
                $"Alpaca returned no latest bar for {normalizedSymbol}.");
        }

        var timestampText = RequiredString(bar, "t");
        if (!DateTimeOffset.TryParse(
                timestampText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            throw new FormatException(
                $"Alpaca returned an invalid bar timestamp for {normalizedSymbol}.");
        }

        return new AlpacaLatestBar(
            normalizedSymbol,
            timestamp,
            RequiredDecimal(bar, "o"),
            RequiredDecimal(bar, "h"),
            RequiredDecimal(bar, "l"),
            RequiredDecimal(bar, "c"),
            RequiredInt64(bar, "v"));
    }

    /// <summary>
    /// Retrieves recent news for one symbol. A successful response verifies endpoint access,
    /// not complete news coverage or catalyst clearance.
    /// </summary>
    public async Task<IReadOnlyList<AlpacaNewsArticle>> GetLatestNewsAsync(
        string symbol,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        _options.Validate();

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A stock symbol is required.", nameof(symbol));
        if (limit is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(limit), "News limit must be between 1 and 50.");

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var start = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(-24).ToString("O", CultureInfo.InvariantCulture));
        var end = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        var escapedSymbol = Uri.EscapeDataString(normalizedSymbol);
        var url = CombineUrl(_options.MarketDataBaseUrl,
            $"/v1beta1/news?symbols={escapedSymbol}&limit={limit}&sort=desc&start={start}&end={end}");

        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Alpaca news request for {normalizedSymbol} failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("news", out var articles) ||
            articles.ValueKind != JsonValueKind.Array)
            throw new FormatException("Alpaca news response did not contain a news array.");

        var result = new List<AlpacaNewsArticle>();
        foreach (var article in articles.EnumerateArray())
        {
            var createdAt = RequiredTimestamp(article, "created_at");
            var updatedAt = article.TryGetProperty("updated_at", out var updated) &&
                updated.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(updated.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedUpdated)
                    ? parsedUpdated
                    : createdAt;
            var symbols = article.TryGetProperty("symbols", out var symbolsElement) &&
                symbolsElement.ValueKind == JsonValueKind.Array
                ? symbolsElement.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!)
                    .ToArray()
                : Array.Empty<string>();

            result.Add(new AlpacaNewsArticle(
                article.TryGetProperty("id", out var id) ? id.ToString() : string.Empty,
                RequiredString(article, "headline"),
                OptionalString(article, "summary"),
                createdAt,
                updatedAt,
                OptionalString(article, "url"),
                Array.AsReadOnly(symbols)));
        }

        return result.AsReadOnly();
    }

    private static DateTimeOffset RequiredTimestamp(JsonElement element, string propertyName)
    {
        var timestampText = RequiredString(element, propertyName);
        if (!DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            throw new FormatException($"Alpaca response contains invalid timestamp field '{propertyName}'.");
        return timestamp;
    }

    private static string? OptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("APCA-API-KEY-ID", _options.ApiKey);
        request.Headers.Add("APCA-API-SECRET-KEY", _options.ApiSecret);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string CombineUrl(string baseUrl, string path)
        => baseUrl.TrimEnd('/') + path;

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new FormatException($"Alpaca response is missing required field '{propertyName}'.");
        }

        return value.GetString()!;
    }

    private static decimal RequiredDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            !value.TryGetDecimal(out var number))
        {
            throw new FormatException($"Alpaca response is missing or has invalid numeric field '{propertyName}'.");
        }

        return number;
    }

    private static long RequiredInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            !value.TryGetInt64(out var number))
        {
            throw new FormatException($"Alpaca response is missing or has invalid integer field '{propertyName}'.");
        }

        return number;
    }
}
