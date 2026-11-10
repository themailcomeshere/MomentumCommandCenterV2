using System.Net;
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
    private const int MaxAttempts = 3;
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
        using var response = await SendWithRetryAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpaca Daily bars returned {(int)response.StatusCode} {response.ReasonPhrase}: {(body.Length > 400 ? body[..400] : body)}", null, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (!root.TryGetProperty("bars", out var bars))
            throw new InvalidOperationException($"Alpaca Daily response is missing the 'bars' property (root JSON kind: {root.ValueKind}).");

        // Alpaca's single-symbol response is normally an array. Some response envelopes
        // key the array by symbol; accept only the requested symbol and preserve empty/null
        // as an explicit no-observation result rather than converting it into an API 502.
        if (bars.ValueKind == JsonValueKind.Object)
        {
            if (!bars.TryGetProperty(symbol.ToUpperInvariant(), out var symbolBars))
                bars = JsonDocument.Parse("[]").RootElement.Clone();
            else if (symbolBars.ValueKind == JsonValueKind.Array || symbolBars.ValueKind == JsonValueKind.Null)
                bars = symbolBars;
            else
                throw new InvalidOperationException($"Alpaca Daily response bars.{symbol.ToUpperInvariant()} has unexpected JSON kind {symbolBars.ValueKind}.");
        }
        else if (bars.ValueKind == JsonValueKind.Null)
        {
            bars = JsonDocument.Parse("[]").RootElement.Clone();
        }
        else if (bars.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Alpaca Daily response bars has unexpected JSON kind {bars.ValueKind} for symbol {symbol.ToUpperInvariant()}.");
        }

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
            .ToDictionary(group => group.Key, group => group.Last());

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
        var latestTimestamp = latest?.Bar.TimestampUtc;
        // The requested latest expected session must itself be present. Avoid Max() on
        // an empty collection: a valid empty provider response is DATA?, not an API 500.
        var fresh = latest is not null && latest.Bar.TimestampUtc <= nowUtc &&
            nowUtc - latest.Bar.TimestampUtc <= TimeSpan.FromDays(5);
        var observedSma20 = complete ? expectedDates.Average(date => completedBySession[date].Bar.Close) : (double?)null;
        var observedAverageVolume20 = complete ? expectedDates.Average(date => (double)completedBySession[date].Bar.Volume) : (double?)null;
        var consolidatedFeed = _feed.Equals("sip", StringComparison.OrdinalIgnoreCase);
        var qualificationUsable = complete && fresh && consolidatedFeed;
        var sessionObservations = expectedDates
            .Where(completedBySession.ContainsKey)
            .Select(date =>
            {
                var item = completedBySession[date];
                return new DailySessionObservation(date, item.Bar.TimestampUtc, item.Bar.Open,
                    item.Bar.High, item.Bar.Low, item.Bar.Close, item.Bar.Volume);
            })
            .ToArray();
        var missingSessionDates = expectedDates.Where(date => !completedBySession.ContainsKey(date)).ToArray();

        // IEX is a partial-exchange feed. Its price/volume bars must not be passed into
        // the Boring Overnight price/SMA/market-wide liquidity gates as consolidated values.
        return new DailyMarketMetrics(
            symbol.ToUpperInvariant(), "Alpaca", _feed, "all (split/dividend adjusted)",
            nowUtc, latestTimestamp, consolidatedFeed ? latest?.Bar.Close : null,
            consolidatedFeed ? observedSma20 : null,
            consolidatedFeed ? observedAverageVolume20 : null,
            latest?.Bar.Close, observedSma20, observedAverageVolume20,
            present.Length, completedBySession.Count, fresh,
            complete ? "COMPLETE_20_EXPECTED_SESSIONS" : "INCOMPLETE_20_EXPECTED_SESSIONS",
            consolidatedFeed ? "CONSOLIDATED_SIP" : "IEX_PARTIAL",
            qualificationUsable,
            $"Metrics use the exact latest 20 expected sessions from {UsEquitiesSessionCalendar.Version}. Missing expected sessions are not backfilled with older sessions. Current session is included only after its scheduled regular close (including early closes). adjustment=all requests split/dividend-adjusted bars. IEX is partial-exchange coverage and is diagnostic only; qualification fields are null unless feed=sip. SIP feed entitlement, source coverage and adjustment parity still require runtime validation.",
            latestExpected, expectedDates, missingSessionDates, sessionObservations);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var attemptRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                attemptRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            HttpResponseMessage response;
            try { response = await client.SendAsync(attemptRequest, HttpCompletionOption.ResponseHeadersRead, ct); }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            { await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct); continue; }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < MaxAttempts)
            { await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct); continue; }
            if ((response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode == 429 || (int)response.StatusCode >= 500) && attempt < MaxAttempts)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(300 * attempt);
                response.Dispose();
                await Task.Delay(delay > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : delay, ct);
                continue;
            }
            return response;
        }
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
    double? ObservedClose,
    double? ObservedSma20,
    double? ObservedAverageDailyVolume20,
    int ValidSessionCount,
    int AvailableCompletedSessionCount,
    bool DataFresh,
    string Completeness,
    string FeedCoverage,
    bool QualificationUsable,
    string Note,
    DateOnly LatestExpectedSessionDate,
    IReadOnlyList<DateOnly> ExpectedSessionDates,
    IReadOnlyList<DateOnly> MissingSessionDates,
    IReadOnlyList<DailySessionObservation> Sessions);

public sealed record DailySessionObservation(
    DateOnly SessionDate,
    DateTimeOffset TimestampUtc,
    double Open,
    double High,
    double Low,
    double Close,
    long Volume);
