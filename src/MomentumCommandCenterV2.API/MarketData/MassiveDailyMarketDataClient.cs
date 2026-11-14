using System.Globalization;
using System.Net;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: MassiveDailyMarketDataClient
/// PURPOSE: Fetches split-adjusted Daily aggregates from Massive as an independent cross-check source for
/// Alpaca Daily bars. It never feeds qualification fields; it only supplies comparison rows.
/// WHEN CALLED: By GET /api/market-data/daily/cross-check after the Alpaca Daily request.
/// NOTE: The free Massive Stocks Basic plan (checked 2026-11-14) is end-of-day only, 5 calls/minute, 2 years;
/// a session becomes available after midnight Eastern, and the current day returns 403.
/// </summary>
public sealed class MassiveDailyMarketDataClient(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private readonly string? _apiKey = string.IsNullOrWhiteSpace(configuration["MarketData:Massive:ApiKey"])
        ? Environment.GetEnvironmentVariable("MASSIVE_API_KEY")
        : configuration["MarketData:Massive:ApiKey"];

    /// <summary>True when a Massive API key is configured server-side.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// METHOD: GetSessionsAsync
    /// PURPOSE: Returns one observation per Eastern session date between fromDate and toDate (inclusive),
    /// using adjusted=true (split-adjusted). Duplicate dates keep the first row; no row is synthesized.
    /// WHEN CALLED: Once per cross-check request.
    /// </summary>
    public async Task<MassiveDailyResult> GetSessionsAsync(string symbol, DateOnly fromDate, DateOnly toDate, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Set MarketData:Massive:ApiKey or MASSIVE_API_KEY for the Daily cross-check.");
        if (toDate < fromDate) throw new ArgumentException("toDate must not be before fromDate.", nameof(toDate));
        var normalized = symbol.Trim().ToUpperInvariant();
        var url = $"v2/aggs/ticker/{Uri.EscapeDataString(normalized)}/range/1/day/" +
                  $"{fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
                  "?adjusted=true&sort=asc&limit=5000";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Massive Daily aggregates returned {(int)response.StatusCode}: {(body.Length > 400 ? body[..400] : body)}", null, response.StatusCode);
        return Parse(normalized, body, nowUtc);
    }

    /// <summary>
    /// METHOD: Parse
    /// PURPOSE: Converts a Massive aggregates body into Eastern-dated session observations. A missing or empty
    /// results array is a valid empty result (no sessions), not an error; status ERROR/NOT_AUTHORIZED throws.
    /// WHEN CALLED: By GetSessionsAsync and by unit tests with fixture bodies.
    /// </summary>
    public static MassiveDailyResult Parse(string symbol, string body, DateTimeOffset nowUtc)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String &&
            status.GetString() is "ERROR" or "NOT_AUTHORIZED")
            throw new HttpRequestException($"Massive provider status {status.GetString()}.");

        var sessions = new List<DailySessionObservation>();
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            var seen = new HashSet<DateOnly>();
            foreach (var item in results.EnumerateArray())
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("t").GetInt64());
                var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timestamp, Eastern).Date);
                if (!seen.Add(date)) continue;
                sessions.Add(new DailySessionObservation(date, timestamp,
                    item.GetProperty("o").GetDouble(), item.GetProperty("h").GetDouble(),
                    item.GetProperty("l").GetDouble(), item.GetProperty("c").GetDouble(),
                    (long)Math.Round(item.GetProperty("v").GetDouble(), MidpointRounding.AwayFromZero)));
            }
        }

        return new MassiveDailyResult(symbol, "Massive", "split (adjusted=true)", nowUtc,
            sessions.OrderBy(x => x.SessionDate).ToArray());
    }
}

/// <summary>
/// TYPE: MassiveDailyResult
/// PURPOSE: Massive Daily rows returned for a cross-check, with provider and adjustment labels.
/// WHEN CALLED: Returned by MassiveDailyMarketDataClient and consumed by DailySourceCrossCheck.
/// </summary>
public sealed record MassiveDailyResult(
    string Symbol,
    string Provider,
    string Adjustment,
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<DailySessionObservation> Sessions);
