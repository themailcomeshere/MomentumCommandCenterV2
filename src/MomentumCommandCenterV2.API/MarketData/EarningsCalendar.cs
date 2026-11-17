using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: EarningsCalendarService
/// PURPOSE: Reads the public Nasdaq earnings calendar (api.nasdaq.com/api/calendar/earnings?date=YYYY-MM-DD) one Eastern
/// date at a time and caches each day in memory: 2 hours for today and later dates, 24 hours for past dates. Only the date
/// is sent; no operator data. Operator request 2026-11-17 (week view, today highlighted; READY earnings gate). The feed is
/// unofficial: timings are Nasdaq's ("before open", "after close", "not supplied") and coverage is not guaranteed.
/// WHEN CALLED: By GET /api/market/earnings/week and by BoringOvernightReadyService for the EARNINGS gate.
/// </summary>
public sealed class EarningsCalendarService(IHttpClientFactory httpClientFactory, ILogger<EarningsCalendarService> logger)
{
    /// <summary>Name of the HttpClient registered for the Nasdaq calendar.</summary>
    public const string HttpClientName = "NasdaqEarnings";

    private readonly ConcurrentDictionary<DateOnly, (DateTimeOffset FetchedUtc, EarningsDay Day)> _cache = new();

    /// <summary>
    /// PURPOSE: Returns the calendar for one Eastern date, from cache when fresh. Throws HttpRequestException or
    /// JsonException when the source fails, so callers can fail closed.
    /// WHEN CALLED: By GetRangeAsync and the week endpoint.
    /// </summary>
    public async Task<EarningsDay> GetDayAsync(DateOnly date, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var todayEastern = EasternDate(nowUtc);
        var ttl = date >= todayEastern ? TimeSpan.FromHours(2) : TimeSpan.FromHours(24);
        if (_cache.TryGetValue(date, out var hit) && nowUtc - hit.FetchedUtc < ttl) return hit.Day;

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/calendar/earnings?date={date:yyyy-MM-dd}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Nasdaq earnings calendar returned {(int)response.StatusCode} for {date:yyyy-MM-dd}.", null, response.StatusCode);
        var day = Parse(date, body, nowUtc);
        _cache[date] = (nowUtc, day);
        logger.LogInformation("Earnings calendar {Date}: {Count} reports", date, day.Rows.Count);
        return day;
    }

    /// <summary>
    /// PURPOSE: Returns every calendar day from <paramref name="from"/> to <paramref name="to"/> inclusive, or the first
    /// error. WHEN CALLED: By the READY service (buy-session date through sell-session date).
    /// </summary>
    public async Task<(IReadOnlyList<EarningsDay>? Days, string? Error)> GetRangeAsync(DateOnly from, DateOnly to, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var days = new List<EarningsDay>();
        try
        {
            for (var d = from; d <= to; d = d.AddDays(1)) days.Add(await GetDayAsync(d, nowUtc, ct));
            return (days, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Earnings calendar unavailable for {From}..{To}", from, to);
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// PURPOSE: Parses one Nasdaq calendar response. A null "rows" (weekends, holidays, empty days) is an empty list.
    /// Timing: time-pre-market = BEFORE_OPEN, time-after-hours = AFTER_CLOSE, anything else = NOT_SUPPLIED.
    /// WHEN CALLED: By GetDayAsync; directly by tests.
    /// </summary>
    public static EarningsDay Parse(DateOnly date, string json, DateTimeOffset retrievedUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = new List<EarningsRow>();
        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("rows", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                string? S(string name) => item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var symbol = (S("symbol") ?? "").Trim().ToUpperInvariant();
                if (symbol.Length == 0) continue;
                var timing = S("time") switch
                {
                    "time-pre-market" => EarningsTiming.BeforeOpen,
                    "time-after-hours" => EarningsTiming.AfterClose,
                    _ => EarningsTiming.NotSupplied
                };
                decimal? cap = decimal.TryParse((S("marketCap") ?? "").Replace("$", "").Replace(",", ""),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out var c) ? c : null;
                rows.Add(new EarningsRow(symbol, S("name") ?? symbol, timing, cap, NullIfEmpty(S("epsForecast")), NullIfEmpty(S("fiscalQuarterEnding"))));
            }
        }
        return new EarningsDay(date, rows, "Nasdaq earnings calendar (api.nasdaq.com, unofficial public feed)", retrievedUtc);
    }

    private static string? NullIfEmpty(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    /// <summary>PURPOSE: The America/New_York calendar date of an instant. WHEN CALLED: Cache TTL and the week endpoint.</summary>
    public static DateOnly EasternDate(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById("America/New_York")).DateTime);
}

/// <summary>Earnings timing as Nasdaq reports it.</summary>
public static class EarningsTiming
{
    /// <summary>Reported before the regular open (time-pre-market).</summary>
    public const string BeforeOpen = "BEFORE_OPEN";
    /// <summary>Reported after the regular close (time-after-hours).</summary>
    public const string AfterClose = "AFTER_CLOSE";
    /// <summary>No time given; could be either side of the session.</summary>
    public const string NotSupplied = "NOT_SUPPLIED";
}

/// <summary>
/// TYPE: EarningsRow
/// PURPOSE: One company's scheduled report on one date: symbol, name, timing, market cap (USD), EPS estimate and fiscal
/// quarter as Nasdaq lists them. WHEN CALLED: Built by EarningsCalendarService.Parse.
/// </summary>
public sealed record EarningsRow(string Symbol, string Name, string Timing, decimal? MarketCap, string? EpsForecast, string? FiscalQuarterEnding);

/// <summary>
/// TYPE: EarningsDay
/// PURPOSE: All reports listed for one Eastern date, with source and retrieval time. WHEN CALLED: Returned by the service.
/// </summary>
public sealed record EarningsDay(DateOnly Date, IReadOnlyList<EarningsRow> Rows, string Source, DateTimeOffset RetrievedUtc);
