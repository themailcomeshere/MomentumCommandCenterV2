// Replays the NEW V2 engine (1M execution window) at every minute close on stored bars dumped from the running MCC.
using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;

var dir = args[0];
var output = new Dictionary<string, object>();
foreach (var sym in args.Skip(1))
{
    var store = new MemoryStore();
    foreach (var tf in new[] { "1m", "5m" })
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "bars", $"{sym}_{tf}.json")));
        foreach (var o in doc.RootElement.GetProperty("observations").EnumerateArray())
        {
            var b = o.GetProperty("bar");
            store.Bars.Add(new MarketBar(sym, b.GetProperty("timestamp").GetDateTimeOffset(), b.GetProperty("open").GetDouble(),
                b.GetProperty("high").GetDouble(), b.GetProperty("low").GetDouble(), b.GetProperty("close").GetDouble(),
                b.GetProperty("volume").GetInt64(), "Alpaca", b.GetProperty("feed").GetString(), tf, "REST"));
        }
    }
    var replay = new HistoricalDecisionReplayService(store);
    var rows = new List<object>();
    foreach (var (day, end) in new[] { ("2026-11-24", "21:00"), ("2026-11-25", "17:40") })
    {
        for (var t = DateTimeOffset.Parse(day + "T14:31:00Z"); t <= DateTimeOffset.Parse(day + "T" + end + ":00Z"); t = t.AddMinutes(1))
        {
            var r = await replay.ReplayAsync(sym, "Alpaca", "iex", t, false, CancellationToken.None, 15);
            rows.Add(r);
        }
    }
    File.WriteAllText(Path.Combine(dir, "outN", $"mccnew_{sym}.json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    Console.WriteLine($"{sym} {rows.Count}");
}

sealed class MemoryStore : IMarketBarObservationStore
{
    public List<MarketBar> Bars { get; } = new();
    public Task<int> AppendAsync(IReadOnlyCollection<MarketBar> bars, CancellationToken ct) => Task.FromResult(0);
    public Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(string symbol, string? provider, DateTimeOffset fromUtc,
        DateTimeOffset toUtc, int limit, CancellationToken ct, string? timeframe = null, string? feed = null)
    {
        IReadOnlyList<MarketBarObservation> result = Bars
            .Where(b => b.Symbol == symbol && b.Timestamp >= fromUtc && b.Timestamp <= toUtc && (timeframe is null || b.Timeframe == timeframe))
            .OrderBy(b => b.Timestamp).TakeLast(limit)
            .Select(b => new MarketBarObservation("x", b.Timestamp, b.Provider, b.Symbol, b.Timestamp, b, "x", null)).ToList();
        return Task.FromResult(result);
    }
}
