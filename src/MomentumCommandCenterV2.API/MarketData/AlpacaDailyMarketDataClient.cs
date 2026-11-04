using System.Net.Http.Headers;
using System.Text.Json;
using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Fetches split/dividend-adjusted Alpaca daily bars and derives completed-session liquidity inputs.
/// Daily bars are deliberately separate from the intraday 1m/5m contract.
/// </summary>
public sealed class AlpacaDailyMarketDataClient(HttpClient client, IConfiguration configuration)
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
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
        if (nowUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("nowUtc must use UTC.", nameof(nowUtc));

        var start = nowUtc.AddDays(-60).UtcDateTime.ToString("O");
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

        var newYorkNow = TimeZoneInfo.ConvertTime(nowUtc, Eastern);
        var today = DateOnly.FromDateTime(newYorkNow.Date);
        var completedBySession = parsed
            .Where(bar => bar.TimestampUtc <= nowUtc)
            .Select(bar => new { Bar = bar, SessionDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(bar.TimestampUtc, Eastern).Date) })
            .Where(item => UsEquitiesSessionCalendar.IsTradingSession(item.SessionDate))
            .Where(item => item.SessionDate < today ||
                (item.SessionDate == today && newYorkNow.TimeOfDay >= UsEquitiesSessionCalendar.GetRegularClose(item.SessionDate).ToTimeSpan()))
            .GroupBy(item => item.SessionDate)
            .ToDictionary(group => group.Key, group => group.Last().Bar);

        // Build the exact latest 20 expected sessions from MCC's versioned calendar.
        // A missing session is a data-quality gap, not a zero-volume day or a reason to
        // reach farther back and silently change the rolling window.
        var latestExpected = today;
        if (!UsEquitiesSessionCalendar.IsTradingSession(latestExpected) ||
            newYorkNow.TimeOfDay < UsEquitiesSessionCalendar.GetRegularClose(latestExpected).ToTimeSpan())
        {
            do { latestExpected = latestExpected.AddDays(-1); }
            while (!UsEquitiesSessionCalendar.IsTradingSession(latestExpected));
        }

        var expectedDates = new List<DateOnly>(20);
        for (var date = latestExpected; expectedDates.Count < 20; date = date.AddDays(-1))
            if (UsEquitiesSessionCalendar.IsTradingSession(date)) expectedDates.Add(date);
        expectedDates.Reverse();

        var present = expectedDates.Where(completedBySession.ContainsKey).ToArray();
        var latest = completedBySession.TryGetValue(latestExpected, out var latestBar) ? latestBar : null;
        var complete = present.Length == 20;
        var latestTimestamp = latest?.TimestampUtc;
        var fresh = latest is not null && latestExpected == completedBySession.Keys.Max() &&
            latestTimestamp <= nowUtc && nowUtc - latestTimestamp <= TimeSpan.FromDays(5);
        var sma20 = complete ? expectedDates.Average(date => completedBySession[date].Close) : (double?)null;
        var averageVolume20 = complete ? expectedDates.Average(date => (double)completedBySession[date].Volume) : (double?)null;

        return new DailyMarketMetrics(
            symbol.ToUpperInvariant(), "Alpaca", _feed, "all (split/dividend adjusted)",
            nowUtc, latestTimestamp, latest?.Close, sma20, averageVolume20,
            present.Length, completedBySession.Count, fresh,
            complete ? "COMPLETE_20_EXPECTED_SESSIONS" : "INCOMPLETE_EXPECTED_SESSIONS",
            $"Metrics use the exact latest 20 expected sessions from {UsEquitiesSessionCalendar.Version}. Missing expected sessions are not backfilled with older sessions. Current session is included only after its scheduled regular close (including early closes). adjustment=all requests split/dividend-adjusted bars. Feed entitlement, source coverage and adjustment parity still require runtime validation.");
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
