using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Fetches normalized historical stock bars from Alpaca's market-data REST API.
/// WHEN CALLED: Provider comparison or Morning Brief when Alpaca credentials are configured.
/// IMPORTANT: Uses server-side credentials only; account feed entitlements are not assumed.
/// </summary>
public sealed class AlpacaMarketDataAdapter(HttpClient httpClient, IConfiguration configuration) : IMarketDataProvider
{
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
    public IReadOnlyList<string> Capabilities { get; } = new[]
    {
        "Historical 1m/5m OHLCV bars", "REST only; no websocket yet", "Feed selection configured server-side",
        "Scanner/movers endpoint not implemented"
    };

    /// <summary>
    /// PURPOSE: Requests historical OHLCV bars and preserves provider timestamps as UTC instants.
    /// WHEN CALLED: API requests compare the same symbol, timeframe, and UTC window across providers.
    /// </summary>
    public async Task<IReadOnlyList<MarketBar>> GetBarsAsync(
        string symbol, TimeSpan timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Set MarketData:Alpaca:ApiKey and ApiSecret (or ALPACA_API_KEY / ALPACA_API_SECRET).");
        var minutes = timeframe == TimeSpan.FromMinutes(1) ? 1 : timeframe == TimeSpan.FromMinutes(5) ? 5 : 0;
        if (minutes == 0) throw new ArgumentOutOfRangeException(nameof(timeframe), "Only 1m and 5m are supported.");

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"v2/stocks/{Uri.EscapeDataString(symbol)}/bars?timeframe={minutes}Min&start={Uri.EscapeDataString(from.UtcDateTime.ToString("O"))}&end={Uri.EscapeDataString(to.UtcDateTime.ToString("O"))}&limit=10000&feed={Uri.EscapeDataString(_feed)}&sort=asc");
        request.Headers.Add("APCA-API-KEY-ID", _key!);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret!);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpaca returned {(int)response.StatusCode} {response.ReasonPhrase}: {TrimError(body)}");
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("bars", out var bars) || bars.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Alpaca response did not contain a bars array.");
        var result = new List<MarketBar>();
        foreach (var item in bars.EnumerateArray())
            result.Add(new MarketBar(symbol, item.GetProperty("t").GetDateTimeOffset(),
                item.GetProperty("o").GetDouble(), item.GetProperty("h").GetDouble(),
                item.GetProperty("l").GetDouble(), item.GetProperty("c").GetDouble(),
                item.GetProperty("v").GetInt64(), ProviderName, _feed));
        RecordSuccessfulValidation(result.Count);
        return result;
    }

    private static string TrimError(string body) => body.Length <= 500 ? body : body[..500];
}
