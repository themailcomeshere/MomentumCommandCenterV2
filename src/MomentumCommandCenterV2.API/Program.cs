using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using MomentumCommandCenterV2.API.MarketData;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Keep Swagger UI organized by workflow rather than the default minimal-API group.
    options.TagActionsBy(api =>
    {
        var path = "/" + (api.RelativePath ?? string.Empty).Split('?')[0].TrimStart('/');
        var tag = path switch
        {
            "/api/health" or "/api/providers/status" => "System & Provider Health",
            "/api/market-data/compare" or "/api/market-data/bars" or "/api/market-data/import-csv" => "Market Data",
            "/api/scanner/universe" => "Scanner & Discovery",
            "/api/market-data/collector/status" => "Collection & Operations",
            "/api/watchlists/exclusions" => "Watchlists & Exclusions",
            _ when path.StartsWith("/api/watchlists/exclusions/", StringComparison.OrdinalIgnoreCase) => "Watchlists & Exclusions",
            "/api/strategy-report/upload" => "Strategy Reports",
            "/api/boring-overnight/evaluate" => "Boring Overnight",
            "/api/morning-brief" => "Morning Report",
            _ => "Other"
        };
        return new[] { tag };
    });
});
builder.Services.AddSingleton<IMarketBarObservationStore, JsonlMarketBarObservationStore>();
builder.Services.AddSingleton<MomentumCommandCenterV2.API.MarketData.MarketDataCollectionWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MomentumCommandCenterV2.API.MarketData.MarketDataCollectionWorker>());
builder.Services.AddSingleton<MomentumCommandCenterV2.API.Watchlists.ExclusionWatchlistStore>();
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
app.UseSwagger();
app.UseSwaggerUI();
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
}))
.WithSummary("Check API liveness")
.WithDescription("Example response: { \"status\": \"OK\", \"mode\": \"decision-support-only\", \"orderSubmission\": \"NOT IMPLEMENTED\" }.");

app.MapGet("/api/providers/status", (IEnumerable<IMarketDataProvider> providers) =>
    Results.Ok(providers.Select(p => new
    {
        provider = p.ProviderName,
        configured = p.IsConfigured,
        validated = p.HasSuccessfulDataValidation,
        status = !p.IsConfigured
            ? "NOT_CONFIGURED"
            : p.HasSuccessfulDataValidation
                ? "VALIDATED_DATA_RETURNED"
                : "CONFIGURED_NOT_YET_VALIDATED",
        lastSuccessfulDataValidationUtc = p.LastSuccessfulDataValidationUtc,
        lastSuccessfulDataValidationBarCount = p.LastSuccessfulDataValidationBarCount,
        capabilities = p.Capabilities,
        credentialLocation = "server environment / user secrets; never frontend"
    })))
.WithSummary("Show configured provider status")
.WithDescription("Example: Alpaca configured=true, status=VALIDATED_DATA_RETURNED after a non-empty response in this process. This does not prove streaming or complete coverage.");

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
})
.WithSummary("Compare historical OHLCV bars across configured providers")
.WithDescription("Example query: symbol=SPY, timeframe=5m, from=2026-10-23T13:30:00Z, to=2026-10-23T13:50:00Z. Maximum window 31 days.");

app.MapGet("/api/scanner/universe", async (
    string symbols,
    string timeframe,
    DateTimeOffset from,
    DateTimeOffset to,
    string? providerName,
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore barStore,
    CancellationToken cancellationToken) =>
{
    var requestedSymbols = symbols.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' },
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => s.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    if (requestedSymbols.Length == 0 || requestedSymbols.Length > 50 || requestedSymbols.Any(s => !IsValidSymbol(s)))
        return Results.BadRequest(new { error = "Provide 1–50 valid comma-separated symbols (1–8 letters, digits, dot, or hyphen)." });
    if (!TryGetTimeframe(timeframe, out var span))
        return Results.BadRequest(new { error = "Timeframe must be 1m or 5m." });
    if (from >= to || to - from > TimeSpan.FromDays(7))
        return Results.BadRequest(new { error = "Require from < to and a window of 7 days or less for universe scans." });
    var provider = providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName ?? "Alpaca", StringComparison.OrdinalIgnoreCase));
    if (provider is null) return Results.BadRequest(new { error = "Unknown provider. Use Alpaca or Massive." });

    var results = await Task.WhenAll(requestedSymbols.Select(async symbol =>
    {
        try
        {
            var bars = (await provider.GetBarsAsync(symbol, span, from, to, cancellationToken))
                .OrderBy(b => b.Timestamp).GroupBy(b => b.Timestamp).Select(g => g.First()).ToArray();
            var persisted = await barStore.AppendAsync(bars, cancellationToken);
            if (bars.Length == 0)
                return new {
                    symbol, provider = provider.ProviderName, timeframe, status = "NO_BARS_RETURNED",
                    barCount = 0, persistedObservationCount = persisted, firstTimestampUtc = (DateTimeOffset?)null,
                    latestTimestampUtc = (DateTimeOffset?)null, latestBarAgeMinutes = (double?)null,
                    changePercent = (double?)null, observedRangePercent = (double?)null, summedVolume = (long?)null,
                    interpretation = "No bars returned for this symbol/window; not proof that the symbol had zero trading activity."
                };
            var first = bars[0]; var latest = bars[^1];
            var high = bars.Max(b => b.High); var low = bars.Min(b => b.Low);
            var change = first.Close == 0 ? (double?)null : (latest.Close - first.Close) / first.Close * 100.0;
            var range = first.Close == 0 ? (double?)null : (high - low) / first.Close * 100.0;
            return new {
                symbol, provider = provider.ProviderName, timeframe, status = "OBSERVED",
                barCount = bars.Length, persistedObservationCount = persisted, firstTimestampUtc = (DateTimeOffset?)first.Timestamp,
                latestTimestampUtc = (DateTimeOffset?)latest.Timestamp,
                latestBarAgeMinutes = (double?)Math.Round(Math.Max(0, (DateTimeOffset.UtcNow - latest.Timestamp).TotalMinutes), 2),
                changePercent = change is null ? (double?)null : Math.Round(change.Value, 4),
                observedRangePercent = range is null ? (double?)null : Math.Round(range.Value, 4),
                summedVolume = (long?)bars.Sum(b => b.Volume),
                interpretation = "Observed historical bars only. No relative-volume baseline, quote/spread, catalyst, halt, sector, or Thinkorswim eligibility data is included."
            };
        }
        catch (Exception ex) when (ex is ProviderNotConfiguredException or HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            return new {
                symbol, provider = provider.ProviderName, timeframe, status = "REQUEST_FAILED",
                barCount = 0, persistedObservationCount = 0, firstTimestampUtc = (DateTimeOffset?)null,
                latestTimestampUtc = (DateTimeOffset?)null, latestBarAgeMinutes = (double?)null,
                changePercent = (double?)null, observedRangePercent = (double?)null, summedVolume = (long?)null,
                interpretation = ex.Message
            };
        }
    }));
    return Results.Ok(new {
        generatedAtUtc = DateTimeOffset.UtcNow,
        provider = provider.ProviderName,
        timeframe,
        fromUtc = from,
        toUtc = to,
        requestedSymbolCount = requestedSymbols.Length,
        scannedSymbolCount = results.Length,
        scope = "USER_SUPPLIED_UNIVERSE_ONLY",
        results = results.OrderByDescending(r => Math.Abs(r.changePercent ?? 0)).ToArray(),
        limitations = new[] {
            "This is not market-wide discovery; only supplied symbols are scanned.",
            "Change/range/volume are descriptive, not a recommendation or probability.",
            "No relative-volume baseline, spread/liquidity, catalyst/news, halt, sector, or Thinkorswim state is available.",
            "Do not treat missing bars as zero activity or a clean session."
        }
    });
})
.WithName("ScanUserSuppliedUniverse")
.WithSummary("Scan a user-supplied symbol universe using historical OHLCV")
.WithDescription("GET /api/scanner/universe?symbols=SPY,QQQ,TTE&timeframe=5m&from=2026-10-26T13:30:00Z&to=2026-10-26T20:00:00Z&providerName=Alpaca. This is a bounded, descriptive scan of supplied symbols, not market-wide movers discovery or a trade signal.")
.ProducesProblem(400);

app.MapGet("/api/market-data/collector/status", (MomentumCommandCenterV2.API.MarketData.MarketDataCollectionWorker worker) =>
    Results.Ok(worker.Snapshot()))
.WithSummary("Show background historical collector state")
.WithDescription("Configure MarketData:Collector:Symbols as a comma- or semicolon-separated list to enable. Collector polls overlapping recent 1m/5m windows, persists idempotently, and retries via provider adapter. It is not a market-wide scanner or websocket.");

app.MapGet("/api/market-data/bars", async (
    string symbol,
    string timeframe,
    DateTimeOffset from,
    DateTimeOffset to,
    string? providerName,
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore barStore,
    CancellationToken cancellationToken) =>
{
    if (!IsValidSymbol(symbol)) return Results.BadRequest(new { error = "Symbol must be 1-8 letters, digits, dot, or hyphen." });
    if (!TryGetTimeframe(timeframe, out var span)) return Results.BadRequest(new { error = "Timeframe must be 1m or 5m." });
    if (from >= to || to - from > TimeSpan.FromDays(31)) return Results.BadRequest(new { error = "Require from < to and a window of 31 days or less." });
    var selected = providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName ?? "Alpaca", StringComparison.OrdinalIgnoreCase));
    if (selected is null) return Results.BadRequest(new { error = "Unknown provider. Use Alpaca or Massive." });
    try
    {
        var bars = (await selected.GetBarsAsync(symbol.ToUpperInvariant(), span, from, to, cancellationToken))
            .OrderBy(b => b.Timestamp).ToArray();
        var persisted = await barStore.AppendAsync(bars, cancellationToken);
        var gaps = FindGaps(bars, span);
        return Results.Ok(new
        {
            symbol = symbol.ToUpperInvariant(), timeframe = timeframe.ToLowerInvariant(),
            provider = selected.ProviderName, feed = bars.FirstOrDefault()?.Feed,
            requestedFromUtc = from, requestedToUtc = to, retrievedAtUtc = DateTimeOffset.UtcNow,
            barCount = bars.Length, newlyPersistedObservationCount = persisted,
            firstSourceTimestampUtc = bars.FirstOrDefault()?.Timestamp,
            latestSourceTimestampUtc = bars.LastOrDefault()?.Timestamp,
            gapCount = gaps.Count, sampleGapStartsUtc = gaps.Take(25).ToArray(),
            sessionNote = "Provider timestamps are preserved in UTC. Historical bars may include extended-hours trades; confirm feed entitlement and requested window. Gaps are not automatically treated as zero activity.",
            bars
        });
    }
    catch (ProviderNotConfiguredException ex) { return Results.Problem(ex.Message, statusCode: 503); }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
    { return Results.Problem($"{ex.GetType().Name}: {ex.Message}", statusCode: 502); }
})
.WithSummary("Retrieve and persist historical 1m/5m bars from one provider")
.WithDescription("Example: symbol=TTE, timeframe=1m, from=2026-10-26T11:00:00Z, to=2026-10-26T20:00:00Z, providerName=Alpaca. Returns source timestamps, feed, gaps, raw normalized bars, and newly persisted observation count. Supports up to 31 days per request.");

app.MapGet("/api/morning-brief", async (IEnumerable<IMarketDataProvider> providers, IMarketBarObservationStore barStore, CancellationToken cancellationToken) =>
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
                var persistedObservationCount = await barStore.AppendAsync(bars, cancellationToken);
                observations.Add(new
                {
                    provider = provider.ProviderName,
                    status = bars.Length == 0 ? "DATA?" : "OBSERVATION_ONLY",
                    barCount = bars.Length,
                    persistedObservationCount,
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
})
.WithSummary("Get the partial market Morning Brief")
.WithDescription("No parameters. Example status: PARTIAL — MARKET SNAPSHOT ONLY; missing sector flow, movers, catalysts, and scoring remain DATA? until providers are wired.");

app.MapPost("/api/boring-overnight/evaluate", async (MomentumCommandCenterV2.API.BoringOvernight.BoringOvernightEvaluationRequest request, MomentumCommandCenterV2.API.Watchlists.ExclusionWatchlistStore exclusionStore, CancellationToken ct) =>
{
    if (!IsValidSymbol(request.Symbol))
        return Results.BadRequest(new { error = "Symbol must be 1-8 letters, digits, dot, or hyphen." });
    if (request.CatalystStatus is not null &&
        !new[] { "CLEAR", "RISK", "UNKNOWN" }.Contains(request.CatalystStatus, StringComparer.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "catalystStatus must be CLEAR, RISK, or UNKNOWN." });

    var exclusions = await exclusionStore.GetAsync(ct);
    var isExcluded = exclusions.Any(x => x.Symbol.Equals(request.Symbol, StringComparison.OrdinalIgnoreCase));
    var result = MomentumCommandCenterV2.API.BoringOvernight.BoringOvernightEvaluator.Evaluate(request with { Excluded = isExcluded });
    return Results.Ok(result);
})
.WithName("EvaluateBoringOvernight")
.WithSummary("Evaluate Boring Overnight hard gates from a supplied, timestamped candidate snapshot")
.WithDescription("Example: { \"symbol\": \"SPY\", \"dataFresh\": true, \"price\": 600, \"sma20\": 590, \"averageDailyVolume20\": 1000000, \"overnightWinRatePct\": 75, \"averageOvernightPct20\": 0.35, \"averageOvernightPct5\": 0.55, \"medianOvernightPct20\": 0.3, \"currentOvernightPct\": 0.25, \"excluded\": false, \"catalystStatus\": \"CLEAR\", \"fiveMinutePermission\": true, \"fiveMinuteStructure\": \"BULLISH\", \"oneMinuteState\": \"EXECUTION READY\", \"extensionAtr\": 0.8, \"currentVolumeVsAverage\": 1.2, \"dailyRangePct\": 1.8, \"atrPct\": 1.5 }. Percent values are percentage points (0.5 = 0.5%). DATA? means missing critical inputs; BLOCKED means hard-gate failure; WATCH means a warning. Caller-supplied data only; no orders are submitted.")
.Produces<MomentumCommandCenterV2.API.BoringOvernight.BoringOvernightEvaluation>()
.ProducesValidationProblem(400)
.WithOpenApi(operation =>
{
    if (operation.RequestBody?.Content.TryGetValue("application/json", out var body) == true)
        body.Example = OpenApiAnyFactory.CreateFromJson("""{"symbol":"SPY","dataFresh":true,"price":600,"sma20":590,"averageDailyVolume20":1000000,"overnightWinRatePct":75,"averageOvernightPct20":0.35,"averageOvernightPct5":0.55,"medianOvernightPct20":0.3,"currentOvernightPct":0.25,"excluded":false,"catalystStatus":"CLEAR","fiveMinutePermission":true,"fiveMinuteStructure":"BULLISH","oneMinuteState":"EXECUTION READY","extensionAtr":0.8,"currentVolumeVsAverage":1.2,"dailyRangePct":1.8,"atrPct":1.5}""");
    return operation;
});

app.MapPost("/api/market-data/import-csv", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Use multipart/form-data with a file field named 'file', plus optional symbol and timeframe fields." });
    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Upload a non-empty Alpaca/IEX OHLCV CSV in the 'file' field." });
    if (file.Length > 10 * 1024 * 1024)
        return Results.BadRequest(new { error = "Maximum upload size is 10 MB." });
    if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only .csv files are accepted." });
    try
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var parityRows = MomentumCommandCenterV2.API.MarketData.AlpacaCsvParityParser.Parse(
            csv, form["symbol"].ToString(), form["timeframe"].ToString());
        return Results.Ok(new
        {
            fileName = Path.GetFileName(file.FileName),
            uploadedBytes = file.Length,
            parsedAtUtc = DateTimeOffset.UtcNow,
            headers = MomentumCommandCenterV2.API.MarketData.AlpacaCsvParityParser.ParityHeaders,
            rowCount = parityRows.Count,
            parityRows,
            interpretation = "Alpaca/IEX export values populate Payload* only. Chart* fields remain blank until an independent Thinkorswim/chart export is supplied; this import does not assert parity."
        });
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (FormatException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("ImportAlpacaCsvForParity")
.WithSummary("Import Alpaca/IEX OHLCV CSV into MCC parity rows")
.WithDescription("Upload multipart/form-data with file field 'file'. Accepts a comma-delimited OHLCV CSV or an Alpaca historical-bars JSON response (a JSON response may have a .csv filename). Optional form fields: symbol and timeframe (1m or 5m) if absent from the payload. The response has all 17 canonical parity columns. It does not synthesize chart values.")
.ProducesProblem(400)
.WithOpenApi(operation =>
{
    operation.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content =
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Required = new HashSet<string> { "file" },
                    Properties =
                    {
                        ["file"] = new OpenApiSchema { Type = "string", Format = "binary", Description = "Alpaca/IEX OHLCV CSV, max 10 MB." },
                        ["symbol"] = new OpenApiSchema { Type = "string", Description = "Optional fallback symbol when absent from CSV." },
                        ["timeframe"] = new OpenApiSchema { Type = "string", Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny> { new OpenApiString("1m"), new OpenApiString("5m") }, Description = "Optional fallback timeframe when absent from CSV." }
                    }
                }
            }
        }
    };
    return operation;
});

app.MapPost("/api/strategy-report/upload", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Use multipart/form-data with a file field named 'file'." });
    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Upload a non-empty CSV in the 'file' field." });
    if (file.Length > 10 * 1024 * 1024)
        return Results.BadRequest(new { error = "Maximum upload size is 10 MB." });
    if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only .csv files are accepted." });
    try
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var parsed = MomentumCommandCenterV2.API.StrategyReports.StrategyReportCsvParser.Parse(csv);
        return Results.Ok(new
        {
            fileName = Path.GetFileName(file.FileName),
            uploadedBytes = file.Length,
            parsedAtUtc = DateTimeOffset.UtcNow,
            parsed
        });
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (FormatException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("UploadStrategyReportCsv")
.WithSummary("Upload and parse a Thinkorswim StrategyReport CSV")
.WithDescription("Swagger example: choose multipart file field 'file' and upload a CSV exported from Thinkorswim. Example response includes headers, row count, up to 100 preview rows, detected likely timestamp/symbol/event columns, and warnings. This preserves report rows; it does not invent missing OHLCV bars or infer unreported fills.")
.Produces<MomentumCommandCenterV2.API.StrategyReports.StrategyReportParseResult>()
.ProducesProblem(400)
.WithOpenApi(operation =>
{
    operation.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content =
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Required = new HashSet<string> { "file" },
                    Properties = { ["file"] = new OpenApiSchema { Type = "string", Format = "binary", Description = "Thinkorswim StrategyReport CSV, max 10 MB." } }
                }
            }
        }
    };
    return operation;
});

app.MapGet("/api/watchlists/exclusions", async (MomentumCommandCenterV2.API.Watchlists.ExclusionWatchlistStore store, CancellationToken ct) =>
    Results.Ok(new { count = (await store.GetAsync(ct)).Count, symbols = await store.GetAsync(ct) }))
.WithSummary("List MCC exclusion-watchlist symbols")
.WithDescription("Example response: { \"count\": 2, \"symbols\": [{ \"symbol\": \"XYZ\", \"reason\": \"User excluded\", \"updatedAtUtc\": \"2026-10-27T15:00:00Z\" }] }. This is MCC's local persisted list, not a sync to Thinkorswim.");

app.MapPost("/api/watchlists/exclusions", async (MomentumCommandCenterV2.API.Watchlists.ExclusionWatchlistStore store, MomentumCommandCenterV2.API.Watchlists.AddExclusionRequest request, CancellationToken ct) =>
{
    if (!IsValidSymbol(request.Symbol)) return Results.BadRequest(new { error = "Symbol must be 1-8 letters, digits, dot, or hyphen." });
    var entries = await store.AddAsync(request.Symbol, request.Reason, ct);
    return Results.Ok(new { added = request.Symbol.ToUpperInvariant(), count = entries.Count, symbols = entries });
})
.WithSummary("Add a symbol to MCC's exclusion watchlist")
.WithDescription("Example request: { \"symbol\": \"TTE\", \"reason\": \"Manual exclusion\" }. This prevents the symbol from qualifying once scanner integration reads this list. It does not modify a Thinkorswim watchlist.");

app.MapDelete("/api/watchlists/exclusions/{symbol}", async (string symbol, MomentumCommandCenterV2.API.Watchlists.ExclusionWatchlistStore store, CancellationToken ct) =>
{
    if (!IsValidSymbol(symbol)) return Results.BadRequest(new { error = "Symbol must be 1-8 letters, digits, dot, or hyphen." });
    var entries = await store.RemoveAsync(symbol, ct);
    return Results.Ok(new { removed = symbol.ToUpperInvariant(), count = entries.Count, symbols = entries });
})
.WithSummary("Remove a symbol from MCC's exclusion watchlist")
.WithDescription("Example: DELETE /api/watchlists/exclusions/TTE. This only edits MCC's local list; it does not remove anything from Thinkorswim.");

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
