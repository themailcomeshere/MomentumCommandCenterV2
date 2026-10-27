using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.Infrastructure.Alpaca;

// ============================================================
/// <summary>
/// One historical OHLCV bar returned by Alpaca's stock-bars endpoint.
/// Timestamps are retained as UTC-offset-aware values from the provider.
/// </summary>
public sealed record AlpacaHistoricalBar(
    string Symbol,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

// ============================================================
/// <summary>
/// Read-only historical stock-bar retrieval with page-token traversal and bounded retries.
/// This client does not persist data or place orders.
/// </summary>
public sealed class AlpacaHistoricalBarsClient
{
    private readonly HttpClient _httpClient;
    private readonly AlpacaOptions _options;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public AlpacaHistoricalBarsClient(
        HttpClient httpClient,
        AlpacaOptions options,
        int maxAttempts = 3,
        TimeSpan? retryDelay = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (maxAttempts < 1 || maxAttempts > 10)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Attempts must be between 1 and 10.");

        _maxAttempts = maxAttempts;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(250);

        if (_retryDelay < TimeSpan.Zero || _retryDelay > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be between zero and 30 seconds.");
    }

    /// <summary>
    /// Retrieves every page for one symbol and interval in the requested half-open time range.
    /// The caller must select a feed with the required session coverage; this method does not
    /// infer missing bars or claim that a feed matches Schwab's eligible trading sessions.
    /// </summary>
    public async Task<IReadOnlyList<AlpacaHistoricalBar>> GetBarsAsync(
        string symbol,
        string timeframe,
        DateTimeOffset start,
        DateTimeOffset end,
        int pageSize = 10000,
        CancellationToken cancellationToken = default)
    {
        _options.Validate();

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A stock symbol is required.", nameof(symbol));
        if (string.IsNullOrWhiteSpace(timeframe))
            throw new ArgumentException("A timeframe is required.", nameof(timeframe));
        if (end <= start)
            throw new ArgumentException("End must be later than start.", nameof(end));
        if (pageSize is < 1 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 10000.");

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedTimeframe = timeframe.Trim();
        var startText = Uri.EscapeDataString(start.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        var endText = Uri.EscapeDataString(end.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        var feed = Uri.EscapeDataString(_options.MarketDataFeed);
        var token = (string?)null;
        var bars = new List<AlpacaHistoricalBar>();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = $"/v2/stocks/{Uri.EscapeDataString(normalizedSymbol)}/bars" +
                $"?timeframe={Uri.EscapeDataString(normalizedTimeframe)}" +
                $"&start={startText}&end={endText}&limit={pageSize}&feed={feed}&sort=asc";

            if (!string.IsNullOrEmpty(token))
                query += $"&page_token={Uri.EscapeDataString(token)}";

            using var document = await SendJsonWithRetryAsync(
                CombineUrl(_options.MarketDataBaseUrl, query),
                normalizedSymbol,
                cancellationToken).ConfigureAwait(false);

            var root = document.RootElement;
            if (!root.TryGetProperty("bars", out var page) || page.ValueKind != JsonValueKind.Array)
                throw new FormatException("Alpaca historical-bars response did not contain a bars array.");

            foreach (var item in page.EnumerateArray())
            {
                var timestamp = RequiredTimestamp(item, "t");
                bars.Add(new AlpacaHistoricalBar(
                    normalizedSymbol,
                    timestamp,
                    RequiredDecimal(item, "o"),
                    RequiredDecimal(item, "h"),
                    RequiredDecimal(item, "l"),
                    RequiredDecimal(item, "c"),
                    RequiredInt64(item, "v")));
            }

            token = root.TryGetProperty("next_page_token", out var next) &&
                next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(token) && !seenTokens.Add(token))
                throw new FormatException("Alpaca repeated a historical-bars page token; pagination stopped to prevent a loop.");
        }
        while (!string.IsNullOrWhiteSpace(token));

        return bars
            .OrderBy(bar => bar.Timestamp)
            .ToArray();
    }

    private async Task<JsonDocument> SendJsonWithRetryAsync(
        string url,
        string symbol,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("APCA-API-KEY-ID", _options.ApiKey);
                request.Headers.Add("APCA-API-SECRET-KEY", _options.ApiSecret);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (IsTransient(response.StatusCode) && attempt < _maxAttempts)
                {
                    var delay = GetRetryDelay(response, attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Alpaca historical-bars request for {symbol} failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                        null,
                        response.StatusCode);
                }

                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);

                return await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < _maxAttempts)
            {
                await Task.Delay(Backoff(attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta >= TimeSpan.Zero)
            return delta <= TimeSpan.FromSeconds(30) ? delta : TimeSpan.FromSeconds(30);

        if (retryAfter?.Date is { } date)
        {
            var untilDate = date - DateTimeOffset.UtcNow;
            if (untilDate > TimeSpan.Zero)
                return untilDate <= TimeSpan.FromSeconds(30) ? untilDate : TimeSpan.FromSeconds(30);
        }

        return Backoff(attempt);
    }

    private TimeSpan Backoff(int attempt)
    {
        var multiplier = Math.Pow(2, Math.Min(attempt - 1, 6));
        var delay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * multiplier);
        return delay <= TimeSpan.FromSeconds(30) ? delay : TimeSpan.FromSeconds(30);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static string CombineUrl(string baseUrl, string path) =>
        baseUrl.TrimEnd('/') + path;

    private static DateTimeOffset RequiredTimestamp(JsonElement element, string propertyName)
    {
        var value = RequiredString(element, propertyName);
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            throw new FormatException($"Alpaca response contains invalid timestamp field '{propertyName}'.");
        return timestamp;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new FormatException($"Alpaca response is missing required field '{propertyName}'.");
        return value.GetString()!;
    }

    private static decimal RequiredDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetDecimal(out var number))
            throw new FormatException($"Alpaca response is missing or has invalid numeric field '{propertyName}'.");
        return number;
    }

    private static long RequiredInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || !value.TryGetInt64(out var number))
            throw new FormatException($"Alpaca response is missing or has invalid integer field '{propertyName}'.");
        return number;
    }
}
