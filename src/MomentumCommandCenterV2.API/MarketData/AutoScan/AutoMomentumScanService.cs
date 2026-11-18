using System.Text.Json;
using MomentumCommandCenterV2.API.Watchlists;

namespace MomentumCommandCenterV2.API.MarketData.AutoScan;

/// <summary>
/// TYPE: AutoScanCandidate
/// PURPOSE: One evaluated symbol: universe facts, indicators, the five scanner results and tags.
/// WHEN CALLED: Produced by <see cref="AutoMomentumScanService.RunAsync"/>.
/// </summary>
public sealed record AutoScanCandidate(
    AutoScanUniverseRow Universe,
    AutoScanIndicators Indicators,
    IReadOnlyList<AutoScanScannerResult> Scanners,
    IReadOnlyList<string> Tags,
    int MomentumPassCount,
    bool Flagged,
    bool Excluded,
    double? LatestBarAgeSeconds,
    IReadOnlyList<string>? ChaseReasons = null,
    decimal? PercentFromOpen = null);

/// <summary>
/// TYPE: AutoScanRun
/// PURPOSE: One complete automatic scan run with provenance (sources, feeds, counts, timestamps).
/// WHEN CALLED: Returned by <see cref="AutoMomentumScanService.RunAsync"/> and the scan endpoints.
/// </summary>
public sealed record AutoScanRun(
    string Status,
    string? StatusDetail,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    DateOnly SessionDate,
    bool RegularSession,
    int UniverseCount,
    int EvaluatedCount,
    int FlaggedCount,
    string PriorCloseSource,
    IReadOnlyList<AutoScanSourceStatus> Sources,
    IReadOnlyList<AutoScanCandidate> Candidates,
    string Notes);

/// <summary>
/// TYPE: AutoScanComparison
/// PURPOSE: One side-by-side record: the operator's TOS watchlist import vs what MCC flagged and saw that session.
/// WHEN CALLED: Created by <see cref="AutoMomentumScanService.RecordTosComparisonAsync"/>.
/// </summary>
public sealed record AutoScanComparison(
    DateTimeOffset RecordedAtUtc,
    DateOnly SessionDate,
    string? FileName,
    IReadOnlyList<string> TosSymbols,
    IReadOnlyList<string> FlaggedByMcc,
    IReadOnlyList<string> SeenButNotFlagged,
    IReadOnlyList<string> NotInMccUniverse,
    IReadOnlyList<string> FlaggedOnlyByMcc,
    int ScanRunsThatSession);

/// <summary>
/// TYPE: AutoMomentumScanService
/// PURPOSE: Runs the operator's TOS intraday scans automatically on free Alpaca data, keeps the latest result, appends a
/// compact log of each run, and records side-by-side comparisons with the operator's TOS watchlist imports.
/// Discovery only: a flagged symbol is never READY, a quote or an order instruction.
/// WHEN CALLED: By <see cref="AutoMomentumScanWorker"/> every interval during the regular session, and by the scan endpoints.
/// </summary>
public sealed class AutoMomentumScanService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly AlpacaAutoScanClient _client;
    private readonly ExclusionWatchlistStore _exclusions;
    private readonly ILogger<AutoMomentumScanService> _logger;
    private readonly string _scanLogPath;
    private readonly string _comparisonPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AutoScanRun? _latest;

    /// <summary>
    /// PURPOSE: Creates the service; log paths default to %LOCALAPPDATA%\MomentumCommandCenterV2 (override with
    /// MCC_AUTO_SCAN_DIRECTORY for tests).
    /// WHEN CALLED: Once by dependency injection.
    /// </summary>
    public AutoMomentumScanService(AlpacaAutoScanClient client, ExclusionWatchlistStore exclusions, ILogger<AutoMomentumScanService> logger, string? directory = null)
    {
        _client = client;
        _exclusions = exclusions;
        _logger = logger;
        var dir = directory
            ?? Environment.GetEnvironmentVariable("MCC_AUTO_SCAN_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MomentumCommandCenterV2");
        Directory.CreateDirectory(dir);
        _scanLogPath = Path.Combine(dir, "auto-momentum-scans.jsonl");
        _comparisonPath = Path.Combine(dir, "auto-momentum-tos-comparisons.jsonl");
    }

    /// <summary>PURPOSE: The most recent completed run, or null before the first run. WHEN CALLED: By GET /api/scanner/auto-momentum.</summary>
    public AutoScanRun? Latest => Volatile.Read(ref _latest);

    /// <summary>
    /// PURPOSE: Runs one scan unless the latest run is younger than <paramref name="minimumAge"/>; concurrent callers
    /// share one run.
    /// WHEN CALLED: By the worker and by GET /api/scanner/auto-momentum?refresh=true.
    /// </summary>
    public async Task<AutoScanRun> RunIfDueAsync(TimeSpan minimumAge, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var latest = Latest;
            if (latest is not null && DateTimeOffset.UtcNow - latest.CompletedAtUtc < minimumAge) return latest;
            var run = await RunAsync(DateTimeOffset.UtcNow, ct);
            Volatile.Write(ref _latest, run);
            await AppendScanLogAsync(run, ct);
            return run;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// PURPOSE: Builds the universe, reads prices, prior closes and 5m bars, and evaluates every scanner.
    /// WHEN CALLED: By <see cref="RunIfDueAsync"/>.
    /// </summary>
    public async Task<AutoScanRun> RunAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        var sessionDate = AutoMomentumScanRules.EasternDate(nowUtc);
        var regular = AutoMomentumScanRules.IsRegularSessionBar(nowUtc);
        var log = new List<AutoScanSourceStatus>();
        if (!_client.IsConfigured)
            return Empty("NOT_CONFIGURED", "Alpaca API credentials are not configured on the server.", nowUtc, sessionDate, regular, log);

        try
        {
            // 1. Universe from the free screeners (consolidated volume / % change lists).
            var byVolume = await _client.GetMostActivesAsync("volume", 100, log, ct);
            var byTrades = await _client.GetMostActivesAsync("trades", 100, log, ct);
            var gainers = await _client.GetGainersAsync(50, log, ct);
            var universe = MergeUniverse(byVolume, byTrades, gainers);
            if (universe.Count == 0)
                return Empty("NO_UNIVERSE", "Every screener request failed or returned no symbols. See sources.", nowUtc, sessionDate, regular, log);

            var symbols = universe.Keys.OrderBy(s => s, StringComparer.Ordinal).ToArray();

            // 2. Live price (IEX latest trade) and prior completed close.
            var snapshots = await _client.GetIexLatestTradesAsync(symbols, log, ct);
            IReadOnlyDictionary<string, decimal> priorSip = new Dictionary<string, decimal>();
            if (_client.UsesDelayedSipDaily)
                priorSip = await _client.GetPriorSipClosesAsync(symbols, sessionDate, nowUtc, log, ct);
            var priorSource = _client.UsesDelayedSipDaily ? "Alpaca SIP Daily (delayed ≥16 min, completed sessions)" : "IEX previous daily bar (partial exchange; diagnostic)";

            var rows = symbols.Select(symbol =>
            {
                var u = universe[symbol];
                snapshots.TryGetValue(symbol, out var snap);
                decimal? prior = priorSip.TryGetValue(symbol, out var p) ? p : null;
                var priorLabel = prior is not null ? "sip_delayed_daily" : null;
                if (prior is null && !_client.UsesDelayedSipDaily && snap?.IexPreviousClose is not null) { prior = snap.IexPreviousClose; priorLabel = "iex_prev_daily"; }
                var last = snap?.LastPrice;
                decimal? pct = last is not null && prior is > 0m ? Math.Round((last.Value / prior.Value - 1m) * 100m, 2) : null;
                return new AutoScanUniverseRow(symbol, last, last is null ? null : "iex_latest_trade", u.Volume, prior, priorLabel, pct, u.Sources);
            }).ToList();

            // 3. 5m IEX bars for symbols whose live price is inside the widest scan price range ($3–$501).
            var barSymbols = rows.Where(r => r.LastPrice is >= 3m and <= 501m).Select(r => r.Symbol).Take(150).ToArray();
            var bars = barSymbols.Length == 0
                ? new Dictionary<string, List<AutoScanBar>>()
                : await _client.GetIexFiveMinuteBarsAsync(barSymbols, nowUtc.AddDays(-6), nowUtc, log, ct);

            // 4. Exclusion watchlist (hard block).
            HashSet<string> excluded;
            try { excluded = (await _exclusions.GetAsync(ct)).Select(e => e.Symbol.Trim().ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase); }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                log.Add(new AutoScanSourceStatus("exclusion watchlist", "local", null, false, 0, ex.Message));
            }

            var candidates = Rank(rows.Select(row => Evaluate(row, bars.TryGetValue(row.Symbol, out var list) ? list : new List<AutoScanBar>(), excluded.Contains(row.Symbol), nowUtc)));

            var status = log.Any(s => !s.Ok) ? "COMPLETED_WITH_SOURCE_ERRORS" : "COMPLETED";
            return new AutoScanRun(status, regular ? null : "Outside the regular session: bars are from the last session, so results are stale.",
                nowUtc, DateTimeOffset.UtcNow, sessionDate, regular, rows.Count, barSymbols.Length, candidates.Count(c => c.Flagged),
                priorSource, log, candidates, Notes);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Automatic momentum scan failed.");
            return Empty("FAILED", $"{ex.GetType().Name}: {ex.Message}", nowUtc, sessionDate, regular, log);
        }
    }

    /// <summary>
    /// PURPOSE: Orders candidates: flagged first; then not a chase (2026-11-18: gap fading, 10% off the high, or 1.5 ATR
    /// above VWAP rank after every fresh candidate); then 5m scans passed; then scan D; then % move since today's open;
    /// then % change vs the prior close; then symbol.
    /// WHEN CALLED: By RunAsync and unit tests.
    /// </summary>
    public static List<AutoScanCandidate> Rank(IEnumerable<AutoScanCandidate> candidates) => candidates
        .OrderByDescending(c => c.Flagged)
        .ThenBy(c => c.ChaseReasons is { Count: > 0 })
        .ThenByDescending(c => c.MomentumPassCount)
        .ThenByDescending(c => c.Scanners.Any(s => s.Code == AutoMomentumScanRules.MarketMovers && s.Result == "PASS"))
        .ThenByDescending(c => c.PercentFromOpen ?? decimal.MinValue)
        .ThenByDescending(c => c.Universe.PercentChange ?? decimal.MinValue)
        .ThenBy(c => c.Universe.Symbol, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// PURPOSE: Evaluates one universe row against its bars. Flagged = passes at least one scan (1, 2A, 2B, Pivot or D),
    /// is not excluded and has a completed bar from the current session that closed within the last 10 minutes.
    /// WHEN CALLED: By <see cref="RunAsync"/> and unit tests.
    /// </summary>
    public static AutoScanCandidate Evaluate(AutoScanUniverseRow row, IReadOnlyList<AutoScanBar> bars, bool excluded, DateTimeOffset nowUtc)
    {
        var indicators = AutoMomentumScanRules.ComputeIndicators(bars);
        var scanners = AutoMomentumScanRules.EvaluateScanners(row, indicators);
        var tags = AutoMomentumScanRules.Tags(scanners, indicators).ToList();
        double? age = indicators.LatestBarTimestamp is null ? null : (nowUtc - (indicators.LatestBarTimestamp.Value + TimeSpan.FromMinutes(5))).TotalSeconds;
        var fresh = age is not null && age <= 600 && indicators.LatestBarTimestamp is not null &&
                    AutoMomentumScanRules.EasternDate(indicators.LatestBarTimestamp.Value) == AutoMomentumScanRules.EasternDate(nowUtc);
        if (!fresh) tags.Add("STALE_BARS");
        if (excluded) tags.Add("EXCLUDED_WATCHLIST");
        var passCount = AutoMomentumScanRules.MomentumPassCount(scanners);
        var anyPass = scanners.Any(s => s.Result == "PASS");
        var chase = AutoMomentumScanRules.ChaseReasons(row, indicators);
        if (chase.Count > 0) tags.Add("CHASE");
        tags.AddRange(chase);
        return new AutoScanCandidate(row, indicators, scanners, tags, passCount, anyPass && fresh && !excluded, excluded, age,
            chase, AutoMomentumScanRules.PercentFromOpen(row, indicators));
    }

    /// <summary>
    /// PURPOSE: Merges the three screener lists into one universe keyed by symbol, keeping the consolidated volume and
    /// the list names each symbol came from.
    /// WHEN CALLED: By <see cref="RunAsync"/> and unit tests.
    /// </summary>
    public static Dictionary<string, (long? Volume, IReadOnlyList<string> Sources)> MergeUniverse(
        IReadOnlyList<AutoScanScreenerRow> byVolume, IReadOnlyList<AutoScanScreenerRow> byTrades, IReadOnlyList<AutoScanScreenerRow> gainers)
    {
        var volume = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        var sources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        void Add(IEnumerable<AutoScanScreenerRow> list, string name)
        {
            foreach (var row in list)
            {
                if (!sources.TryGetValue(row.Symbol, out var s)) sources[row.Symbol] = s = new List<string>();
                if (!s.Contains(name)) s.Add(name);
                if (row.Volume is not null) volume[row.Symbol] = row.Volume;
                else volume.TryAdd(row.Symbol, null);
            }
        }
        Add(byVolume, "MOST_ACTIVE_VOLUME");
        Add(byTrades, "MOST_ACTIVE_TRADES");
        Add(gainers, "TOP_GAINERS");
        return sources.ToDictionary(p => p.Key, p => (volume.GetValueOrDefault(p.Key), (IReadOnlyList<string>)p.Value), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PURPOSE: Compares the operator's TOS watchlist symbols with every symbol MCC flagged or saw during that session
    /// (from the scan log), then appends the comparison.
    /// WHEN CALLED: By POST /api/scanner/auto-momentum/tos-comparison when the operator imports a TOS CSV in INTRADAY MOMENTUM.
    /// </summary>
    public async Task<AutoScanComparison> RecordTosComparisonAsync(IReadOnlyList<string> tosSymbols, string? fileName, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var sessionDate = AutoMomentumScanRules.EasternDate(nowUtc);
        var runs = (await ReadScanLogAsync(ct)).Where(r => r.SessionDate == sessionDate).ToList();
        var comparison = Compare(tosSymbols, runs, sessionDate, fileName, nowUtc);
        await AppendLineAsync(_comparisonPath, JsonSerializer.Serialize(comparison, Json), ct);
        return comparison;
    }

    /// <summary>
    /// PURPOSE: Pure comparison of TOS symbols with logged scan runs from one session.
    /// WHEN CALLED: By <see cref="RecordTosComparisonAsync"/> and unit tests.
    /// </summary>
    public static AutoScanComparison Compare(IReadOnlyList<string> tosSymbols, IReadOnlyList<AutoScanLogEntry> runs, DateOnly sessionDate, string? fileName, DateTimeOffset nowUtc)
    {
        var tos = tosSymbols.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        var flagged = runs.SelectMany(r => r.Flagged).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = runs.SelectMany(r => r.Universe).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tosSet = tos.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new AutoScanComparison(
            nowUtc, sessionDate, fileName, tos,
            tos.Where(flagged.Contains).ToList(),
            tos.Where(s => !flagged.Contains(s) && seen.Contains(s)).ToList(),
            tos.Where(s => !seen.Contains(s)).ToList(),
            flagged.Where(s => !tosSet.Contains(s)).OrderBy(s => s, StringComparer.Ordinal).ToList(),
            runs.Count);
    }

    /// <summary>
    /// PURPOSE: Returns the latest comparison per session (most recent first) for the side-by-side report.
    /// WHEN CALLED: By GET /api/scanner/auto-momentum/tos-comparison.
    /// </summary>
    public async Task<IReadOnlyList<AutoScanComparison>> GetComparisonReportAsync(CancellationToken ct)
    {
        if (!File.Exists(_comparisonPath)) return Array.Empty<AutoScanComparison>();
        var items = new List<AutoScanComparison>();
        foreach (var line in await File.ReadAllLinesAsync(_comparisonPath, ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try { if (JsonSerializer.Deserialize<AutoScanComparison>(line, Json) is { } c) items.Add(c); }
            catch (JsonException) { /* a damaged line is skipped, never repaired */ }
        }
        return items.GroupBy(c => c.SessionDate).Select(g => g.OrderBy(c => c.RecordedAtUtc).Last())
            .OrderByDescending(c => c.SessionDate).ToList();
    }

    /// <summary>PURPOSE: Reads the compact scan log. WHEN CALLED: By the comparison.</summary>
    public async Task<IReadOnlyList<AutoScanLogEntry>> ReadScanLogAsync(CancellationToken ct)
    {
        if (!File.Exists(_scanLogPath)) return Array.Empty<AutoScanLogEntry>();
        var items = new List<AutoScanLogEntry>();
        foreach (var line in await File.ReadAllLinesAsync(_scanLogPath, ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try { if (JsonSerializer.Deserialize<AutoScanLogEntry>(line, Json) is { } e) items.Add(e); }
            catch (JsonException) { /* skipped */ }
        }
        return items;
    }

    /// <summary>PURPOSE: Appends one compact line per completed run. WHEN CALLED: After every run.</summary>
    private async Task AppendScanLogAsync(AutoScanRun run, CancellationToken ct)
    {
        if (run.Candidates.Count == 0) return;
        var entry = new AutoScanLogEntry(run.CompletedAtUtc, run.SessionDate, run.RegularSession, run.Status,
            run.Candidates.Select(c => c.Universe.Symbol).ToList(),
            run.Candidates.Where(c => c.Flagged).Select(c => c.Universe.Symbol).ToList(),
            run.Candidates.Where(c => c.Flagged).ToDictionary(c => c.Universe.Symbol, c => string.Join(' ', c.Tags)));
        await AppendLineAsync(_scanLogPath, JsonSerializer.Serialize(entry, Json), ct);
    }

    /// <summary>PURPOSE: Appends one line to a JSONL file. WHEN CALLED: By the log writers.</summary>
    private static async Task AppendLineAsync(string path, string line, CancellationToken ct) =>
        await File.AppendAllTextAsync(path, line + Environment.NewLine, ct);

    /// <summary>PURPOSE: Builds a run with no candidates. WHEN CALLED: On configuration or source failure.</summary>
    private static AutoScanRun Empty(string status, string detail, DateTimeOffset now, DateOnly session, bool regular, IReadOnlyList<AutoScanSourceStatus> log) =>
        new(status, detail, now, DateTimeOffset.UtcNow, session, regular, 0, 0, 0, "none", log, Array.Empty<AutoScanCandidate>(), Notes);

    private const string Notes =
        "Discovery only, not READY or an order instruction. Thresholds are the operator's TOS scans as written (docs/Scanners). " +
        "Universe = Alpaca most-actives (volume, trades; top 100 each) + top 50 gainers: stocks outside those lists are not scanned. " +
        "Day volume is consolidated (screener); bar volume conditions use IEX 5m bars and are approximate (IEX_VOLUME_APPROX). " +
        "Only completed 5m bars are used; Thinkorswim also evaluates the forming bar. All five scans use 5m bars (Scanner 1 confirmed by the operator 2026-11-16).";
}

/// <summary>
/// TYPE: AutoScanLogEntry
/// PURPOSE: Compact JSONL record of one scan run: every symbol evaluated and the flagged symbols with their tags.
/// WHEN CALLED: Written after each run; read for TOS comparisons.
/// </summary>
public sealed record AutoScanLogEntry(
    DateTimeOffset CompletedAtUtc,
    DateOnly SessionDate,
    bool RegularSession,
    string Status,
    IReadOnlyList<string> Universe,
    IReadOnlyList<string> Flagged,
    IReadOnlyDictionary<string, string> FlaggedTags);

/// <summary>
/// TYPE: AutoMomentumScanWorker
/// PURPOSE: Runs the automatic scan every MarketData:AutoScan:IntervalSeconds (default 120, minimum 60) during the
/// regular session (09:30–16:00 ET; 07:30–14:00 AZ in standard time). Disabled with MarketData:AutoScan:Enabled=false.
/// WHEN CALLED: Started by the host at API start-up.
/// </summary>
public sealed class AutoMomentumScanWorker(AutoMomentumScanService service, IConfiguration configuration, ILogger<AutoMomentumScanWorker> logger) : BackgroundService
{
    /// <summary>PURPOSE: The polling loop. WHEN CALLED: By the host.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("MarketData:AutoScan:Enabled", true)) return;
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("MarketData:AutoScan:IntervalSeconds", 120), 60, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (AutoMomentumScanRules.IsRegularSessionBar(DateTimeOffset.UtcNow))
                    await service.RunIfDueAsync(interval - TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Automatic momentum scan worker iteration failed.");
            }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
