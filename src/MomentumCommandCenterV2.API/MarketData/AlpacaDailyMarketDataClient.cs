using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Fetches split/dividend-adjusted Alpaca daily bars and derives completed-session liquidity inputs.
/// Daily bars are deliberately separate from the intraday 1m/5m contract.
/// </summary>
public sealed class AlpacaDailyMarketDataClient(HttpClient client, IConfiguration configuration)
{
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"] ?? Environment.GetEnvironmentVariable("ALPACA_API_KEY");
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"] ?? Environment.GetEnvironmentVariable("ALPACA_API_SECRET");
    private readonly string _feed = (configuration["MarketData:Alpaca:Feed"] ?? "sip").Trim().ToLowerInvariant();

    public async Task<DailyMarketMetrics> GetMetricsAsync(string symbol, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_key) || string.IsNullOrWhiteSpace(_secret))
            throw new ProviderNotConfiguredException("Daily Alpaca data requires server-side API credentials.");
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8 ||
            !symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1-8 letters, digits, dot, or hyphen.", nameof(symbol));

        var start = nowUtc.AddDays(-55).UtcDateTime.ToString("O");
        var end = nowUtc.UtcDateTime.ToString("O");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"v2/stocks/{Uri.EscapeDataString(symbol.ToUpperInvariant())}/bars?timeframe=1Day&start={Uri.EscapeDataString(start)}&end={Uri.EscapeDataString(end)}&limit=1000&feed={Uri.EscapeDataString(_feed)}&adjustment=all&sort=asc");
        request.Headers.Add("APCA-API-KEY-ID", _key);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpaca Daily bars returned {(int)response.StatusCode} {response.ReasonPhrase}: {(body.Length > 400 ? body[..400] : body)}");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (!root.TryGetProperty("bars", out var bars) || bars.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Alpaca Daily response did not contain a bars array.");

        var parsed = bars.EnumerateArray().Select(item => new DailyBar(
                item.GetProperty("t").GetDateTimeOffset(),
                item.GetProperty("o").GetDouble(),
                item.GetProperty("h").GetDouble(),
                item.GetProperty("l").GetDouble(),
                item.GetProperty("c").GetDouble(),
                item.GetProperty("v").GetInt64()))
            .OrderBy(x => x.TimestampUtc)
            .GroupBy(x => x.TimestampUtc)
            .Select(group => group.First())
            .ToArray();

        // Alpaca daily bars are stamped at the regular-session open. Exclude today's bar
        // until after the regular close in New York; this prevents partial-day volume/close
        // from contaminating 20-session averages.
        var newYorkNow = TimeZoneInfo.ConvertTime(nowUtc, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        var completed = parsed.Where(bar =>
        {
            var sessionDate = TimeZoneInfo.ConvertTime(bar.TimestampUtc, TimeZoneInfo.FindSystemTimeZoneById("America/New_York")).Date;
            return sessionDate < newYorkNow.Date || (sessionDate == newYorkNow.Date && newYorkNow.TimeOfDay >= TimeSpan.FromHours(16));
        }).ToArray();

        var last = completed.LastOrDefault();
        var sample = completed.TakeLast(20).ToArray();
        var fresh = last is not null && nowUtc - last.TimestampUtc <= TimeSpan.FromDays(7);
        return new DailyMarketMetrics(
            symbol.ToUpperInvariant(), "Alpaca", _feed, "all (split/dividend adjusted)",
            nowUtc, last?.TimestampUtc, last?.Close,
            sample.Length == 20 ? sample.Average(x => x.Close) : null,
            sample.Length == 20 ? sample.Average(x => (double)x.Volume) : null,
            sample.Length, completed.Length, fresh,
            sample.Length == 20 ? "COMPLETE_20_SESSIONS" : "INSUFFICIENT_DAILY_HISTORY",
            "Daily bars use adjustment=all. Metrics use the latest 20 completed regular-session bars; the current session is excluded until 16:00 America/New_York. Provider coverage, feed entitlement and adjustment parity still require operator validation.");
    }

    private sealed record DailyBar(DateTimeOffset TimestampUtc, double Open, double High, double Low, double Close, long Volume);
}

public sealed record DailyMarketMetrics(
    string Symbol,
    string Provider,
    string Feed,
    string Adjustment,
    DateTimeOffset RetrievedAtUtc,
    DateTimeOffset? LatestCompletedSessionTimestampUtc,
    double? Price,
    double? Sma20,
    double? AverageDailyVolume20,
    int ValidSessionCount,
    int AvailableCompletedSessionCount,
    bool DataFresh,
    string Completeness,
    string Note);
