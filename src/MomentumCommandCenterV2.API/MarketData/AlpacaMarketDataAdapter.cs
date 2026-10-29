using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Historical Alpaca REST adapter with bounded pagination and transient retry handling.
/// Credentials remain server-side. A successful response containing bars is not proof of full-session coverage.
/// </summary>
public sealed class AlpacaMarketDataAdapter(HttpClient httpClient, IConfiguration configuration) : IMarketDataProvider
{
    private const int PageSize = 10000;
    private const int MaxPages = 100;
    private const int MaxAttempts = 3;
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"];
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"];
    private readonly string _feed = configuration["MarketData:Alpaca:Feed"] ?? "iex";
    private long _lastSuccessfulValidationUnixMilliseconds;
    private int _lastSuccessfulValidationBarCount;

    public string ProviderName => "Alpaca";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_key) && !string.IsNullOrWhiteSpace(_secret);
    public DateTimeOffset? LastSuccessfulDataValidationUtc
    {
        get
        {
            var timestamp = Interlocked.Read(ref _lastSuccessfulValidationUnixMilliseconds);
            return timestamp == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        }
    }
    public bool HasSuccessfulDataValidation => LastSuccessfulDataValidationUtc.HasValue;
    public int LastSuccessfulDataValidationBarCount => Volatile.Read(ref _lastSuccessfulValidationBarCount);

    private void RecordSuccessfulValidation(int barCount)
    {
        if (barCount <= 0) return;
        Volatile.Write(ref _lastSuccessfulValidationBarCount, barCount);
        Interlocked.Exchange(ref _lastSuccessfulValidationUnixMilliseconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public IReadOnlyList<string> Capabilities => new[]
    {
        "Historical 1m/5m OHLCV bars",
        "REST with bounded pagination and transient retries",
        $"Configured feed: {_feed}",
        "Extended-hours bars included only when provider returns them for this feed/entitlement",
        "Scanner/movers discovery still depends on configured scanner source"
    };

    public async Task<IReadOnlyList<MarketBar>> GetBarsAsync(
        string symbol, TimeSpan timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Set MarketData:Alpaca:ApiKey and ApiSecret (or ALPACA_API_KEY / ALPACA_API_SECRET).");
        var minutes = timeframe == TimeSpan.FromMinutes(1) ? 1 : timeframe == TimeSpan.FromMinutes(5) ? 5 : 0;
        if (minutes == 0) throw new ArgumentOutOfRangeException(nameof(timeframe), "Only 1m and 5m are supported.");
        if (from >= to) throw new ArgumentException("from must be earlier than to.");
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8) throw new ArgumentException("Symbol must be 1-8 characters.", nameof(symbol));

        var result = new List<MarketBar>();
        string? pageToken = null;
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; page < MaxPages; page++)
        {
            var query = $"v2/stocks/{Uri.EscapeDataString(symbol.ToUpperInvariant())}/bars?timeframe={minutes}Min" +
                $"&start={Uri.EscapeDataString(from.UtcDateTime.ToString("O"))}" +
                $"&end={Uri.EscapeDataString(to.UtcDateTime.ToString("O"))}" +
                $"&limit={PageSize}&feed={Uri.EscapeDataString(_feed)}&sort=asc" +
                (string.IsNullOrWhiteSpace(pageToken) ? "" : $"&page_token={Uri.EscapeDataString(pageToken)}");
            using var response = await SendWithRetryAsync(query, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Alpaca returned {(int)response.StatusCode} {response.ReasonPhrase}: {TrimError(body)}");
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (!TryGetBarsArray(root, symbol, out var bars))
                throw new InvalidOperationException(DescribeUnexpectedBarsPayload(root));
            foreach (var item in bars.EnumerateArray())
            {
                var timestamp = item.GetProperty("t").GetDateTimeOffset();
                if (timestamp < from || timestamp > to) continue;
                result.Add(new MarketBar(symbol.ToUpperInvariant(), timestamp,
                    item.GetProperty("o").GetDouble(), item.GetProperty("h").GetDouble(),
                    item.GetProperty("l").GetDouble(), item.GetProperty("c").GetDouble(),
                    item.GetProperty("v").GetInt64(), ProviderName, _feed));
            }

            pageToken = root.TryGetProperty("next_page_token", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString() : null;
            if (string.IsNullOrWhiteSpace(pageToken)) break;
            if (!seenTokens.Add(pageToken))
                throw new InvalidOperationException("Alpaca pagination repeated a page token; stopping to avoid duplicate/infinite retrieval.");
            if (page == MaxPages - 1)
                throw new InvalidOperationException($"Alpaca result exceeded the safety limit of {MaxPages} pages; narrow the requested time window.");
        }

        var normalized = result.OrderBy(b => b.Timestamp).GroupBy(b => b.Timestamp).Select(g => g.First()).ToArray();
        RecordSuccessfulValidation(normalized.Length);
        return normalized;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Add("APCA-API-KEY-ID", _key!);
            request.Headers.Add("APCA-API-SECRET-KEY", _secret!);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                continue;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                continue;
            }

            if ((response.StatusCode == HttpStatusCode.RequestTimeout ||
                 (int)response.StatusCode == 429 || (int)response.StatusCode >= 500) && attempt < MaxAttempts)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(250 * attempt);
                response.Dispose();
                await Task.Delay(retryAfter > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : retryAfter, cancellationToken);
                continue;
            }
            return response;
        }
    }

    private static bool TryGetBarsArray(JsonElement root, string symbol, out JsonElement bars)
    {
        bars = default;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("bars", out var barsElement))
            return false;

        // Alpaca commonly returns a single-symbol bars array. Some response shapes wrap
        // per-symbol arrays under bars.{SYMBOL}; accept the requested symbol only.
        if (barsElement.ValueKind == JsonValueKind.Array)
        {
            bars = barsElement;
            return true;
        }

        if (barsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in barsElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, symbol, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (property.Value.ValueKind != JsonValueKind.Array)
                    return false;
                bars = property.Value;
                return true;
            }
        }

        return false;
    }

    private static string DescribeUnexpectedBarsPayload(JsonElement root)
    {
        var keys = root.ValueKind == JsonValueKind.Object
            ? string.Join(", ", root.EnumerateObject().Select(property => property.Name).Take(12))
            : root.ValueKind.ToString();
        var code = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("code", out var codeElement) ? codeElement.ToString() : null;
        var message = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var messageElement) ? messageElement.ToString()
            : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var errorElement) ? errorElement.ToString() : null;
        if (!string.IsNullOrWhiteSpace(message))
            message = message.Length <= 300 ? message : message[..300];
        var detail = string.IsNullOrWhiteSpace(message) ? "" : $" Provider message: {message}";
        var codeDetail = string.IsNullOrWhiteSpace(code) ? "" : $" Provider code: {code}.";
        return $"Alpaca HTTP response was successful but did not contain a usable bars array for the requested symbol. Top-level keys: [{keys}].{codeDetail}{detail} Check the requested URL/window, feed entitlement, and provider response schema.";
    }

    private static string TrimError(string body) => body.Length <= 500 ? body : body[..500];
}
