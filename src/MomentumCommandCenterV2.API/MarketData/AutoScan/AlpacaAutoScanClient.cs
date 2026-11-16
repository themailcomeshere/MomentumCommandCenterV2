using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData.AutoScan;

/// <summary>
/// TYPE: AutoScanSourceStatus
/// PURPOSE: Outcome of one provider request in a scan run (shown on the Dashboard so a refused or failed source is visible).
/// WHEN CALLED: Recorded by <see cref="AlpacaAutoScanClient"/> for every request.
/// </summary>
public sealed record AutoScanSourceStatus(string Source, string Feed, int? HttpStatus, bool Ok, int Items, string? Error);

/// <summary>
/// TYPE: AutoScanScreenerRow
/// PURPOSE: One row from Alpaca's most-actives (consolidated day volume and trade count) or movers (percent change) screener.
/// WHEN CALLED: Returned by <see cref="AlpacaAutoScanClient.GetMostActivesAsync"/> and <see cref="AlpacaAutoScanClient.GetGainersAsync"/>.
/// </summary>
public sealed record AutoScanScreenerRow(string Symbol, long? Volume, long? TradeCount, decimal? PercentChange, decimal? Price);

/// <summary>
/// TYPE: AutoScanSnapshot
/// PURPOSE: IEX latest trade for one symbol (live "Last" price), with its timestamp.
/// WHEN CALLED: Returned by <see cref="AlpacaAutoScanClient.GetIexLatestTradesAsync"/>.
/// </summary>
public sealed record AutoScanSnapshot(string Symbol, decimal? LastPrice, DateTimeOffset? LastTradeUtc, decimal? IexPreviousClose);

/// <summary>
/// TYPE: AlpacaAutoScanClient
/// PURPOSE: Free-plan data access for the automatic scanner.
/// - Universe: Alpaca screener most-actives (by volume and by trades, top 100 each) and movers (top 50 gainers). Alpaca
///   documents these as based on real-time SIP data; they are lists, not bar requests.
/// - Live price: IEX latest trade (multi-symbol snapshots).
/// - 5m bars: IEX (partial-exchange coverage; volume conditions are approximate and labelled).
/// - Prior close: completed SIP Daily bars with the request end at least 16 minutes old (operator exception
///   2026-11-14, MarketData:Alpaca:DailyFeed=sip_delayed); otherwise the IEX snapshot's previous daily bar, labelled.
/// No recent SIP bar requests are made.
/// WHEN CALLED: By <see cref="AutoMomentumScanService"/> once per scan run.
/// </summary>
public sealed class AlpacaAutoScanClient(HttpClient client, IConfiguration configuration)
{
    private const int SymbolChunk = 100;
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"] ?? Environment.GetEnvironmentVariable("ALPACA_API_KEY");
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"] ?? Environment.GetEnvironmentVariable("ALPACA_API_SECRET");
    private readonly string _dailyFeed = (configuration["MarketData:Alpaca:DailyFeed"] ?? "").Trim().ToLowerInvariant();

    /// <summary>PURPOSE: True when Alpaca credentials are configured. WHEN CALLED: Before each scan run.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_key) && !string.IsNullOrWhiteSpace(_secret);

    /// <summary>PURPOSE: True when completed Daily bars may be requested on delayed SIP. WHEN CALLED: When choosing the prior-close source.</summary>
    public bool UsesDelayedSipDaily => _dailyFeed == "sip_delayed";

    /// <summary>
    /// PURPOSE: Reads the most-actives screener ranked by "volume" or "trades" (top 1–100).
    /// WHEN CALLED: Twice per scan run.
    /// </summary>
    public async Task<IReadOnlyList<AutoScanScreenerRow>> GetMostActivesAsync(string by, int top, List<AutoScanSourceStatus> log, CancellationToken ct)
    {
        var source = $"screener most-actives by {by}";
        var (status, body) = await SendAsync($"v1beta1/screener/stocks/most-actives?by={by}&top={top}", ct);
        if (status != HttpStatusCode.OK) { log.Add(Fail(source, "sip (screener)", status, body)); return Array.Empty<AutoScanScreenerRow>(); }
        var rows = ParseMostActives(body);
        log.Add(new AutoScanSourceStatus(source, "sip (screener)", (int)status, true, rows.Count, null));
        return rows;
    }

    /// <summary>
    /// PURPOSE: Reads the movers screener and returns the gainers.
    /// WHEN CALLED: Once per scan run.
    /// </summary>
    public async Task<IReadOnlyList<AutoScanScreenerRow>> GetGainersAsync(int top, List<AutoScanSourceStatus> log, CancellationToken ct)
    {
        const string source = "screener movers (gainers)";
        var (status, body) = await SendAsync($"v1beta1/screener/stocks/movers?top={top}", ct);
        if (status != HttpStatusCode.OK) { log.Add(Fail(source, "sip (screener)", status, body)); return Array.Empty<AutoScanScreenerRow>(); }
        var rows = ParseGainers(body);
        log.Add(new AutoScanSourceStatus(source, "sip (screener)", (int)status, true, rows.Count, null));
        return rows;
    }

    /// <summary>
    /// PURPOSE: Reads IEX snapshots (latest trade and IEX previous daily bar) for many symbols.
    /// WHEN CALLED: Once per scan run for the whole universe.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, AutoScanSnapshot>> GetIexLatestTradesAsync(IReadOnlyList<string> symbols, List<AutoScanSourceStatus> log, CancellationToken ct)
    {
        var result = new Dictionary<string, AutoScanSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in symbols.Chunk(SymbolChunk))
        {
            var (status, body) = await SendAsync($"v2/stocks/snapshots?symbols={Uri.EscapeDataString(string.Join(',', chunk))}&feed=iex", ct);
            if (status != HttpStatusCode.OK) { log.Add(Fail("snapshots (latest trade)", "iex", status, body)); continue; }
            var parsed = ParseSnapshots(body);
            foreach (var pair in parsed) result[pair.Key] = pair.Value;
            log.Add(new AutoScanSourceStatus("snapshots (latest trade)", "iex", (int)status, true, parsed.Count, null));
        }
        return result;
    }

    /// <summary>
    /// PURPOSE: Reads completed 5m IEX bars for many symbols between from and to (paged), keeping only regular-session
    /// bars whose close time is at or before <paramref name="nowUtc"/>.
    /// WHEN CALLED: Once per scan run for symbols that pass the price prefilter.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, List<AutoScanBar>>> GetIexFiveMinuteBarsAsync(
        IReadOnlyList<string> symbols, DateTimeOffset from, DateTimeOffset nowUtc, List<AutoScanSourceStatus> log, CancellationToken ct)
    {
        var result = new Dictionary<string, List<AutoScanBar>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in symbols.Chunk(SymbolChunk))
        {
            string? pageToken = null;
            var pages = 0;
            do
            {
                var url = $"v2/stocks/bars?symbols={Uri.EscapeDataString(string.Join(',', chunk))}&timeframe=5Min" +
                          $"&start={Uri.EscapeDataString(from.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                          $"&end={Uri.EscapeDataString(nowUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                          "&limit=10000&feed=iex&adjustment=split&sort=asc" +
                          (pageToken is null ? "" : "&page_token=" + Uri.EscapeDataString(pageToken));
                var (status, body) = await SendAsync(url, ct);
                if (status != HttpStatusCode.OK) { log.Add(Fail("5m bars", "iex", status, body)); break; }
                pageToken = ParseBars(body, result, nowUtc, TimeSpan.FromMinutes(5));
                pages++;
            } while (pageToken is not null && pages < 20);
            log.Add(new AutoScanSourceStatus("5m bars", "iex", 200, true, chunk.Count(s => result.ContainsKey(s)), pages >= 20 ? "Stopped after 20 pages." : null));
        }
        return result;
    }

    /// <summary>
    /// PURPOSE: Reads the latest completed Daily close before <paramref name="sessionDate"/> on delayed SIP. The request
    /// end is midnight Eastern of the session date, capped at now − 16 minutes, so no recent SIP data is requested.
    /// WHEN CALLED: Once per scan run when <see cref="UsesDelayedSipDaily"/> is true.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, decimal>> GetPriorSipClosesAsync(
        IReadOnlyList<string> symbols, DateOnly sessionDate, DateTimeOffset nowUtc, List<AutoScanSourceStatus> log, CancellationToken ct)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var end = AutoMomentumScanRules.EasternMidnightUtc(sessionDate);
        var cap = nowUtc - AlpacaDailyMarketDataClient.SipDelay;
        if (end > cap) end = cap;
        var start = end.AddDays(-10);
        foreach (var chunk in symbols.Chunk(SymbolChunk))
        {
            var bars = new Dictionary<string, List<AutoScanBar>>(StringComparer.OrdinalIgnoreCase);
            var url = $"v2/stocks/bars?symbols={Uri.EscapeDataString(string.Join(',', chunk))}&timeframe=1Day" +
                      $"&start={Uri.EscapeDataString(start.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                      $"&end={Uri.EscapeDataString(end.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                      "&limit=10000&feed=sip&adjustment=split&sort=asc";
            var (status, body) = await SendAsync(url, ct);
            if (status != HttpStatusCode.OK) { log.Add(Fail("prior Daily close", "sip (delayed ≥16 min)", status, body)); continue; }
            ParseBars(body, bars, null, null);
            foreach (var (symbol, list) in bars)
            {
                var prior = list.Where(b => AutoMomentumScanRules.EasternDate(b.Timestamp) < sessionDate).LastOrDefault();
                if (prior is not null) result[symbol] = prior.Close;
            }
            log.Add(new AutoScanSourceStatus("prior Daily close", "sip (delayed ≥16 min)", (int)status, true, chunk.Count(s => result.ContainsKey(s)), null));
        }
        return result;
    }

    /// <summary>PURPOSE: Parses a most-actives response. WHEN CALLED: By <see cref="GetMostActivesAsync"/> and unit tests.</summary>
    public static List<AutoScanScreenerRow> ParseMostActives(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var rows = new List<AutoScanScreenerRow>();
        if (!doc.RootElement.TryGetProperty("most_actives", out var items) || items.ValueKind != JsonValueKind.Array) return rows;
        foreach (var item in items.EnumerateArray())
        {
            var symbol = item.TryGetProperty("symbol", out var s) ? s.GetString() : null;
            if (string.IsNullOrWhiteSpace(symbol)) continue;
            rows.Add(new AutoScanScreenerRow(symbol.ToUpperInvariant(), Long(item, "volume"), Long(item, "trade_count"), null, null));
        }
        return rows;
    }

    /// <summary>PURPOSE: Parses the gainers of a movers response. WHEN CALLED: By <see cref="GetGainersAsync"/> and unit tests.</summary>
    public static List<AutoScanScreenerRow> ParseGainers(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var rows = new List<AutoScanScreenerRow>();
        if (!doc.RootElement.TryGetProperty("gainers", out var items) || items.ValueKind != JsonValueKind.Array) return rows;
        foreach (var item in items.EnumerateArray())
        {
            var symbol = item.TryGetProperty("symbol", out var s) ? s.GetString() : null;
            if (string.IsNullOrWhiteSpace(symbol)) continue;
            rows.Add(new AutoScanScreenerRow(symbol.ToUpperInvariant(), null, null, Dec(item, "percent_change"), Dec(item, "price")));
        }
        return rows;
    }

    /// <summary>PURPOSE: Parses a multi-symbol snapshots response (top-level object keyed by symbol). WHEN CALLED: By <see cref="GetIexLatestTradesAsync"/> and unit tests.</summary>
    public static Dictionary<string, AutoScanSnapshot> ParseSnapshots(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var result = new Dictionary<string, AutoScanSnapshot>(StringComparer.OrdinalIgnoreCase);
        var root = doc.RootElement.TryGetProperty("snapshots", out var nested) ? nested : doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return result;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object) continue;
            decimal? price = null; DateTimeOffset? time = null; decimal? prevClose = null;
            if (property.Value.TryGetProperty("latestTrade", out var trade) && trade.ValueKind == JsonValueKind.Object)
            {
                price = Dec(trade, "p");
                if (trade.TryGetProperty("t", out var t) && DateTimeOffset.TryParse(t.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) time = parsed;
            }
            if (property.Value.TryGetProperty("prevDailyBar", out var prev) && prev.ValueKind == JsonValueKind.Object) prevClose = Dec(prev, "c");
            result[property.Name.ToUpperInvariant()] = new AutoScanSnapshot(property.Name.ToUpperInvariant(), price, time, prevClose);
        }
        return result;
    }

    /// <summary>
    /// PURPOSE: Parses a multi-symbol bars page into <paramref name="into"/>; with a timeframe, keeps only regular-session
    /// bars whose close (open + timeframe) is at or before <paramref name="nowUtc"/>. Returns the next page token.
    /// WHEN CALLED: By the bar readers and unit tests.
    /// </summary>
    public static string? ParseBars(string body, Dictionary<string, List<AutoScanBar>> into, DateTimeOffset? nowUtc, TimeSpan? timeframe)
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("bars", out var bars) && bars.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in bars.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array) continue;
                var symbol = property.Name.ToUpperInvariant();
                if (!into.TryGetValue(symbol, out var list)) into[symbol] = list = new List<AutoScanBar>();
                foreach (var bar in property.Value.EnumerateArray())
                {
                    if (!bar.TryGetProperty("t", out var t) ||
                        !DateTimeOffset.TryParse(t.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts)) continue;
                    var open = Dec(bar, "o"); var high = Dec(bar, "h"); var low = Dec(bar, "l"); var close = Dec(bar, "c"); var volume = Long(bar, "v");
                    if (open is null || high is null || low is null || close is null || volume is null) continue;
                    if (timeframe is not null)
                    {
                        if (!AutoMomentumScanRules.IsRegularSessionBar(ts)) continue;
                        if (nowUtc is not null && ts + timeframe.Value > nowUtc.Value) continue;
                    }
                    list.Add(new AutoScanBar(ts, open.Value, high.Value, low.Value, close.Value, volume.Value, Dec(bar, "vw")));
                }
            }
        }
        return doc.RootElement.TryGetProperty("next_page_token", out var token) && token.ValueKind == JsonValueKind.String
            ? token.GetString()
            : null;
    }

    /// <summary>PURPOSE: Sends one authenticated GET. WHEN CALLED: By every reader.</summary>
    private async Task<(HttpStatusCode Status, string Body)> SendAsync(string relativeUrl, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add("APCA-API-KEY-ID", _key!);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret!);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    /// <summary>PURPOSE: Builds a failed-source record with a trimmed body. WHEN CALLED: On a non-200 response.</summary>
    private static AutoScanSourceStatus Fail(string source, string feed, HttpStatusCode status, string body) =>
        new(source, feed, (int)status, false, 0, body.Length > 300 ? body[..300] : body);

    /// <summary>PURPOSE: Reads an optional decimal property. WHEN CALLED: By the parsers.</summary>
    private static decimal? Dec(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var d) ? d : null;

    /// <summary>PURPOSE: Reads an optional integer property. WHEN CALLED: By the parsers.</summary>
    private static long? Long(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number) return null;
        if (value.TryGetInt64(out var l)) return l;
        return value.TryGetDecimal(out var d) ? (long)d : null;
    }
}
