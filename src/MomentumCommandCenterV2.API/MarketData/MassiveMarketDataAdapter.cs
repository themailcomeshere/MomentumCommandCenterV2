using System.Globalization;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Fetches normalized historical aggregates from the Massive Stocks REST API.
/// WHEN CALLED: Provider comparison or Morning Brief when the API key is configured.
/// IMPORTANT: Credentials remain server-side; endpoint and subscription entitlement must be verified.
/// </summary>
public sealed class MassiveMarketDataAdapter(HttpClient httpClient, IConfiguration configuration) : IMarketDataProvider
{
    private readonly string? _apiKey = string.IsNullOrWhiteSpace(configuration["MarketData:Massive:ApiKey"]) ? Environment.GetEnvironmentVariable("MASSIVE_API_KEY") : configuration["MarketData:Massive:ApiKey"];
    private long _lastSuccessfulValidationUnixMilliseconds;
    private int _lastSuccessfulValidationBarCount;

    public string ProviderName => "Massive";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
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
    public IReadOnlyList<string> Capabilities { get; } = new[]
    {
        "Historical 1m/5m OHLCV aggregates", "REST only; no websocket yet", "Scanner/movers endpoint not implemented",
        "Plan/endpoint entitlements must be verified"
    };

    /// <summary>
    /// PURPOSE: Requests aggregate bars for a date window and normalizes Unix-millisecond timestamps.
    /// WHEN CALLED: API requests compare the same symbol, timeframe, and UTC window across providers.
    /// </summary>
    public async Task<IReadOnlyList<MarketBar>> GetBarsAsync(
        string symbol, TimeSpan timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Set MarketData:Massive:ApiKey or MASSIVE_API_KEY.");
        var multiplier = timeframe == TimeSpan.FromMinutes(1) ? 1 : timeframe == TimeSpan.FromMinutes(5) ? 5 : 0;
        if (multiplier == 0) throw new ArgumentOutOfRangeException(nameof(timeframe), "Only 1m and 5m are supported.");

        var fromDate = from.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toDate = to.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = $"v2/aggs/ticker/{Uri.EscapeDataString(symbol)}/range/{multiplier}/minute/{fromDate}/{toDate}?adjusted=false&sort=asc&limit=50000";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Massive returned {(int)response.StatusCode} {response.ReasonPhrase}: {TrimError(body)}");
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("status", out var status) && status.GetString() is "ERROR" or "NOT_AUTHORIZED")
            throw new HttpRequestException($"Massive provider status: {status.GetString()}: {TrimError(body)}");
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Massive response did not contain a results array.");

        var result = new List<MarketBar>();
        foreach (var item in results.EnumerateArray())
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("t").GetInt64());
            if (timestamp < from || timestamp > to) continue;
            result.Add(new MarketBar(symbol, timestamp, item.GetProperty("o").GetDouble(),
                item.GetProperty("h").GetDouble(), item.GetProperty("l").GetDouble(),
                item.GetProperty("c").GetDouble(), item.GetProperty("v").GetInt64(), ProviderName, "Massive aggregate", $"{multiplier}m"));
        }
        RecordSuccessfulValidation(result.Count);
        return result;
    }

    private static string TrimError(string body) => body.Length <= 500 ? body : body[..500];
}
