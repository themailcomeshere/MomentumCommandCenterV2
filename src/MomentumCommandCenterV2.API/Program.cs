using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using MomentumCommandCenterV2.API.MarketData;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IMarketBarObservationStore, JsonlMarketBarObservationStore>();
builder.Services.AddHttpClient<AlpacaMarketDataAdapter>(client =>
{
    client.BaseAddress = new Uri("https://data.alpaca.markets/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<MassiveMarketDataAdapter>(client =>
{
    client.BaseAddress = new Uri("https://api.massive.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddSingleton<IMarketDataProvider>(sp => sp.GetRequiredService<AlpacaMarketDataAdapter>());
builder.Services.AddSingleton<IMarketDataProvider>(sp => sp.GetRequiredService<MassiveMarketDataAdapter>());

var app = builder.Build();
app.UseExceptionHandler();
var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend"));
if (Directory.Exists(frontendPath))
{
    var frontendProvider = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });
}

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "MomentumCommandCenterV2.API",
    status = "OK",
    mode = "decision-support-only",
    orderSubmission = "NOT IMPLEMENTED",
    timestampUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/providers/status", (IEnumerable<IMarketDataProvider> providers) =>
    Results.Ok(providers.Select(p => new
    {
        provider = p.ProviderName,
        configured = p.IsConfigured,
        status = p.IsConfigured ? "CONFIGURED_NOT_YET_VALIDATED" : "NOT_CONFIGURED",
        capabilities = p.Capabilities,
        credentialLocation = "server environment / user secrets; never frontend"
    })));

app.MapGet("/api/market-data/compare", async (
    string symbol,
    string timeframe,
    DateTimeOffset from,
    DateTimeOffset to,
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore barStore,
    CancellationToken cancellationToken) =>
{
    if (!IsValidSymbol(symbol))
        return Results.BadRequest(new { error = "Symbol must be 1-8 letters, digits, dot, or hyphen." });
    if (!TryGetTimeframe(timeframe, out var span))
        return Results.BadRequest(new { error = "Timeframe must be 1m or 5m." });
    if (from >= to || to - from > TimeSpan.FromDays(31))
        return Results.BadRequest(new { error = "Require from < to and a window of 31 days or less." });

    var results = await Task.WhenAll(providers.Select(async provider =>
    {
        try
        {
            var bars = await provider.GetBarsAsync(symbol.ToUpperInvariant(), span, from, to, cancellationToken);
            var ordered = bars.OrderBy(b => b.Timestamp).ToArray();
            var persisted = await barStore.AppendAsync(ordered, cancellationToken);
            var duplicates = ordered.GroupBy(b => b.Timestamp).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            var gaps = FindGaps(ordered, span);
            return new ProviderComparisonResult(provider.ProviderName, provider.IsConfigured,
                ordered.Length, persisted, ordered.FirstOrDefault(), ordered.LastOrDefault(), duplicates.Length,
                gaps.Count, gaps.Take(25).ToArray(), null, provider.Capabilities);
        }
        catch (ProviderNotConfiguredException ex)
        {
            return ProviderComparisonResult.Unavailable(provider.ProviderName, false, ex.Message, provider.Capabilities);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            return ProviderComparisonResult.Unavailable(provider.ProviderName, provider.IsConfigured,
                $"{ex.GetType().Name}: {ex.Message}", provider.Capabilities);
        }
    }));
    return Results.Ok(new
    {
        symbol = symbol.ToUpperInvariant(),
        timeframe = timeframe.ToLowerInvariant(),
        from,
        to,
        generatedAtUtc = DateTimeOffset.UtcNow,
        results,
        interpretation = "Gap counts are timestamp intervals between returned bars, not proof of provider failure or zero trading. Compare only like-for-like feeds, sessions, entitlements, and aggregation rules."
    });
});

app.MapGet("/api/morning-brief", async (IEnumerable<IMarketDataProvider> providers, CancellationToken cancellationToken) =>
{
    var configured = providers.Where(p => p.IsConfigured).ToArray();
    var benchmarkSymbols = new[] { "SPY", "QQQ", "IWM" };
    var benchmarkRows = new List<object>();
    foreach (var symbol in benchmarkSymbols)
    {
        var observations = new List<object>();
        foreach (var provider in configured)
        {
            try
            {
                var end = DateTimeOffset.UtcNow;
                var bars = (await provider.GetBarsAsync(symbol, TimeSpan.FromMinutes(5), end.AddHours(-8), end, cancellationToken))
                    .OrderBy(b => b.Timestamp).ToArray();
                observations.Add(new
                {
                    provider = provider.ProviderName,
                    status = bars.Length == 0 ? "DATA?" : "OBSERVATION_ONLY",
                    barCount = bars.Length,
                    latest = bars.LastOrDefault(),
                    changeFromFirstPercent = bars.Length > 1 && bars[0].Close != 0
                        ? Math.Round((bars[^1].Close / bars[0].Close - 1) * 100, 3)
                        : (double?)null
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
            {
                observations.Add(new { provider = provider.ProviderName, status = "UNKNOWN", error = ex.Message });
            }
        }
        benchmarkRows.Add(new { symbol, observations });
    }

    return Results.Ok(new
    {
        generatedAtUtc = DateTimeOffset.UtcNow,
        displayTimezone = "America/Phoenix",
        status = configured.Length == 0 ? "DATA? — NO PROVIDER CONFIGURED" : "PARTIAL — MARKET SNAPSHOT ONLY",
        marketLandscape = benchmarkRows,
        sectorFlow = new { status = "DATA NOT FOUND IN SOURCES", reason = "No sector classification / breadth feed is implemented." },
        movers = new { status = "DATA NOT FOUND IN SOURCES", reason = "Broad-market scanner endpoint is not yet implemented." },
        catalysts = new { status = "DATA NOT FOUND IN SOURCES", reason = "No news / catalyst provider is configured." },
        historicalExpectations = new { status = "INSUFFICIENT HISTORY", reason = "No validated out-of-sample outcome dataset is wired into this API." },
        redZone = new { status = "NOT CALCULATED", reason = "No validated discovery, 5M authority, and 1M execution pipeline is connected." },
        overnight = new { status = "NOT CALCULATED", reason = "Boring Overnight scoring and persistence are not wired to live market data." },
        warnings = new[]
        {
            "This brief reports observed bars only; it does not generate buy/sell signals.",
            "No market regime, sector leadership, catalyst, or probability is inferred from missing inputs.",
            "Data freshness, feed entitlements, extended-hours coverage, and bar parity must be validated before trading reliance."
        }
    });
});

app.Run();

static bool IsValidSymbol(string symbol) =>
    !string.IsNullOrWhiteSpace(symbol) && symbol.Length <= 8 &&
    symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-');

static bool TryGetTimeframe(string timeframe, out TimeSpan span)
{
    span = timeframe.ToLowerInvariant() switch
    {
        "1m" => TimeSpan.FromMinutes(1),
        "5m" => TimeSpan.FromMinutes(5),
        _ => TimeSpan.Zero
    };
    return span != TimeSpan.Zero;
}

static List<DateTimeOffset> FindGaps(IReadOnlyList<MarketBar> bars, TimeSpan interval)
{
    var gaps = new List<DateTimeOffset>();
    for (var i = 1; i < bars.Count; i++)
    {
        var expected = bars[i - 1].Timestamp + interval;
        if (bars[i].Timestamp - bars[i - 1].Timestamp > interval + TimeSpan.FromSeconds(1))
            gaps.Add(expected);
    }
    return gaps;
}
