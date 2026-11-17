using System.Globalization;
using System.Text.Json;
using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: MacroEvent
/// PURPOSE: One scheduled high-impact US event (FOMC decision day, CPI, jobs report) from App_Data/macro-events.json.
/// TimeEt is null when the official source gives no time; AtUtc is TimeEt on that date in America/New_York, as UTC.
/// WHEN CALLED: Loaded by <see cref="MarketRegimeService"/>; returned with the regime.
/// </summary>
public sealed record MacroEvent(DateOnly Date, string? TimeEt, string Kind, string Label, bool Tentative, DateTimeOffset? AtUtc = null);

/// <summary>
/// TYPE: RegimeSymbolState
/// PURPOSE: The trend reading for one index ETF: BULLISH, BEARISH, NEUTRAL or DATA?, with the inputs used.
/// WHEN CALLED: Part of <see cref="MarketRegimeResult"/>.
/// </summary>
public sealed record RegimeSymbolState(
    string Symbol,
    string State,
    bool? CloseAboveVwap,
    bool? Ema9AboveEma20,
    bool? CloseAboveEma9,
    bool? Ema20AboveSma50,
    bool? FiveMinuteFresh,
    string? EvaluationStatus,
    double? LastClose,
    double? Vwap);

/// <summary>
/// TYPE: MarketRegimeResult
/// PURPOSE: The header market chip: overall trend status (RISK_ON, MIXED, RISK_OFF, CLOSED, DATA?) and a separate event
/// flag (TODAY, NEXT_SESSION, NONE) with the next scheduled events.
/// WHEN CALLED: Returned by GET /api/market/regime.
/// </summary>
public sealed record MarketRegimeResult(
    string Status,
    IReadOnlyList<RegimeSymbolState> Symbols,
    string EventRisk,
    IReadOnlyList<MacroEvent> EventsFlagged,
    IReadOnlyList<MacroEvent> UpcomingEvents,
    DateTimeOffset EvaluatedAtUtc,
    string RulesVersion,
    string Note);

/// <summary>
/// TYPE: MarketRegimeClassifier
/// PURPOSE: Pure rules (MCC-REGIME-0.1, provisional, untested against outcomes):
/// per symbol, BULLISH = 5m close above session VWAP and EMA9 above EMA20; BEARISH = both false; NEUTRAL = they disagree;
/// DATA? = an input is missing or the 5m bar is stale. Overall: all BULLISH = RISK_ON, all BEARISH = RISK_OFF, any DATA? =
/// DATA?, otherwise MIXED; outside the regular session = CLOSED. Events are flagged separately: TODAY when one falls on
/// today's Eastern date, NEXT_SESSION when one falls on the next trading session (risk for an overnight hold).
/// Awareness only: no gate in MCC reads this.
/// WHEN CALLED: By <see cref="MarketRegimeService"/> and unit tests.
/// </summary>
public static class MarketRegimeClassifier
{
    /// <summary>Rules version shown with every result.</summary>
    public const string RulesVersion = "MCC-REGIME-0.1";

    /// <summary>PURPOSE: Trend state for one symbol. WHEN CALLED: Once per symbol.</summary>
    public static string SymbolState(bool? closeAboveVwap, bool? ema9AboveEma20, bool? fiveMinuteFresh)
    {
        if (closeAboveVwap is null || ema9AboveEma20 is null || fiveMinuteFresh != true) return "DATA?";
        if (closeAboveVwap.Value && ema9AboveEma20.Value) return "BULLISH";
        if (!closeAboveVwap.Value && !ema9AboveEma20.Value) return "BEARISH";
        return "NEUTRAL";
    }

    /// <summary>PURPOSE: Overall status from the symbol states. WHEN CALLED: Once per evaluation.</summary>
    public static string Overall(IReadOnlyList<string> states, bool regularSession)
    {
        if (!regularSession) return "CLOSED";
        if (states.Count == 0 || states.Any(s => s == "DATA?")) return "DATA?";
        if (states.All(s => s == "BULLISH")) return "RISK_ON";
        if (states.All(s => s == "BEARISH")) return "RISK_OFF";
        return "MIXED";
    }

    /// <summary>
    /// PURPOSE: Event flag and the flagged events: TODAY (an event on today's Eastern date), otherwise NEXT_SESSION (an event
    /// on the next trading session's date), otherwise NONE.
    /// WHEN CALLED: Once per evaluation.
    /// </summary>
    public static (string Risk, IReadOnlyList<MacroEvent> Flagged) EventRisk(IReadOnlyList<MacroEvent> events, DateTimeOffset nowUtc)
    {
        var today = OvernightHistoryExportService.EasternDate(nowUtc);
        var todays = events.Where(e => e.Date == today).ToArray();
        if (todays.Length > 0) return ("TODAY", todays);
        var next = today.AddDays(1);
        for (var i = 0; i < 10 && !UsEquitiesSessionCalendar.IsTradingSession(next); i++) next = next.AddDays(1);
        var nexts = events.Where(e => e.Date == next).ToArray();
        return nexts.Length > 0 ? ("NEXT_SESSION", nexts) : ("NONE", Array.Empty<MacroEvent>());
    }

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>PURPOSE: An Eastern wall-clock time on a date, as UTC (handles DST); null without a time. WHEN CALLED: By LoadEvents.</summary>
    public static DateTimeOffset? ToUtc(DateOnly date, string? timeEt)
    {
        if (timeEt is null || !TimeOnly.TryParseExact(timeEt, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) return null;
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>PURPOSE: Reads the event list from the JSON file; an unreadable file gives an empty list. WHEN CALLED: Each evaluation.</summary>
    public static IReadOnlyList<MacroEvent> LoadEvents(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("events").EnumerateArray()
                .Select(e => new MacroEvent(
                    DateOnly.ParseExact(e.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    e.TryGetProperty("timeEt", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null,
                    e.GetProperty("kind").GetString() ?? "EVENT",
                    e.GetProperty("label").GetString() ?? "Event",
                    e.TryGetProperty("tentative", out var tent) && tent.ValueKind == JsonValueKind.True))
                .Select(e => e with { AtUtc = ToUtc(e.Date, e.TimeEt) })
                .OrderBy(e => e.Date).ToArray();
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or FormatException or UnauthorizedAccessException)
        {
            return Array.Empty<MacroEvent>();
        }
    }
}

/// <summary>
/// TYPE: MarketRegimeService
/// PURPOSE: Builds the header market chip from MCC's own live assessment of SPY and QQQ (IEX 5m bars; index ETFs trade
/// heavily on IEX, so price/VWAP/EMA readings are representative) and the scheduled-event file. Cached for 30 seconds.
/// WHEN CALLED: GET /api/market/regime (the Dashboard polls every 60 seconds).
/// </summary>
public sealed class MarketRegimeService(LiveSymbolAssessmentService assessments, string eventsPath)
{
    private static readonly string[] Symbols = { "SPY", "QQQ" };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private MarketRegimeResult? _cached;

    /// <summary>PURPOSE: Returns the cached or freshly computed regime. WHEN CALLED: Each request.</summary>
    public async Task<MarketRegimeResult> GetAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cached is not null && now - _cached.EvaluatedAtUtc < TimeSpan.FromSeconds(30)) return _cached;
        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null && now - _cached.EvaluatedAtUtc < TimeSpan.FromSeconds(30)) return _cached;
            var states = new List<RegimeSymbolState>();
            foreach (var symbol in Symbols)
            {
                try
                {
                    var json = JsonSerializer.SerializeToElement(await assessments.GetAsync(symbol, "Alpaca", ct));
                    states.Add(FromAssessment(symbol, json));
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or ArgumentException or ProviderNotConfiguredException)
                {
                    states.Add(new RegimeSymbolState(symbol, "DATA?", null, null, null, null, null, "REQUEST_FAILED: " + ex.Message, null, null));
                }
            }
            var events = MarketRegimeClassifier.LoadEvents(eventsPath);
            var (risk, flagged) = MarketRegimeClassifier.EventRisk(events, now);
            var today = OvernightHistoryExportService.EasternDate(now);
            var upcoming = events.Where(e => e.Date >= today && e.Date <= today.AddDays(14)).Take(4).ToArray();
            _cached = new MarketRegimeResult(
                MarketRegimeClassifier.Overall(states.Select(s => s.State).ToArray(), LiveSymbolTechnicalCalculator.IsRegularSession(now)),
                states, risk, flagged, upcoming, now, MarketRegimeClassifier.RulesVersion,
                "Awareness only; no MCC gate reads this. Trend = 5m close vs session VWAP and EMA9 vs EMA20 for SPY and QQQ (IEX bars). " +
                "Events = official FOMC/BLS schedules in App_Data/macro-events.json (re-check sources when extending). Rules are provisional and untested against outcomes.");
            return _cached;
        }
        finally { _gate.Release(); }
    }

    /// <summary>PURPOSE: Reads the 5m criteria, freshness and last price from a live-assessment payload. WHEN CALLED: Per symbol and in tests.</summary>
    public static RegimeSymbolState FromAssessment(string symbol, JsonElement a)
    {
        bool? Criterion(string key)
        {
            if (!a.TryGetProperty("fiveMinute", out var five) || !five.TryGetProperty("scoreBreakdown", out var sb) ||
                !sb.TryGetProperty("criteria", out var criteria) || criteria.ValueKind != JsonValueKind.Array) return null;
            foreach (var c in criteria.EnumerateArray())
                if (c.TryGetProperty("key", out var k) && k.GetString() == key)
                    return c.TryGetProperty("passed", out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False ? p.GetBoolean() : null;
            return null;
        }
        bool? fresh = a.TryGetProperty("freshnessDiagnostics", out var fd) && fd.TryGetProperty("fiveMinute", out var f5) &&
                      f5.TryGetProperty("isFresh", out var isFresh) && isFresh.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? isFresh.GetBoolean() : null;
        double? Num(string parent, string name) =>
            a.TryGetProperty(parent, out var p) && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
        var aboveVwap = Criterion("closeAboveVwap");
        var emaStack = Criterion("ema9AboveEma20");
        var status = a.TryGetProperty("evaluationStatus", out var es) && es.ValueKind == JsonValueKind.String ? es.GetString() : null;
        return new RegimeSymbolState(symbol, MarketRegimeClassifier.SymbolState(aboveVwap, emaStack, fresh), aboveVwap, emaStack,
            Criterion("closeAboveEma9"), Criterion("ema20AboveSma50"), fresh, status, Num("oneMinute", "close"), Num("oneMinute", "vwap"));
    }
}
