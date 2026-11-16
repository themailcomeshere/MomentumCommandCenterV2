using System.Globalization;
using System.Text;
using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: HistoryExportRequest
/// PURPOSE: Body of POST /api/research/overnight-history/export: the imported scanner symbols, the scanner file name and
/// its rows (archived as the day's candidate snapshot), and the history length in calendar days (default 730).
/// WHEN CALLED: Sent by the Dashboard history-export button.
/// </summary>
public sealed record HistoryExportRequest(
    IReadOnlyList<string>? Symbols,
    string? ScannerFileName,
    IReadOnlyList<Dictionary<string, string?>>? ScannerRows,
    int? Days);

/// <summary>
/// TYPE: HistoryExportRun
/// PURPOSE: Summary of one export run (from the Dashboard or scripts/Export-OvernightHistory.ps1), read from its manifest.
/// WHEN CALLED: Returned by the export and status endpoints.
/// </summary>
public sealed record HistoryExportRun(
    string RunId,
    DateTimeOffset CreatedAtUtc,
    DateOnly SessionDateEastern,
    string Source,
    string? ScannerFile,
    int SymbolsRequested,
    int SymbolsSucceeded,
    int PairRows,
    string Folder,
    IReadOnlyList<string> Errors,
    IReadOnlyList<OvernightWeekdayStats> Pooled);

/// <summary>
/// TYPE: HistoryExportStatus
/// PURPOSE: Whether an export has run on today's Eastern date, and the latest run.
/// WHEN CALLED: Returned by GET /api/research/overnight-history/export-status for the header button's colour.
/// </summary>
public sealed record HistoryExportStatus(bool RanToday, DateOnly TodayEastern, bool Running, HistoryExportRun? Latest, string DataRoot, string Note);

/// <summary>
/// TYPE: OvernightHistoryExportService
/// PURPOSE: Server-side version of scripts/Export-OvernightHistory.ps1, so the Dashboard can run the daily research export
/// with one button. Writes the same files to the same folders: &lt;DataRoot&gt;\overnight-history\&lt;UTC stamp&gt;\{pairs.csv,
/// weekday-by-symbol.csv, weekday-pooled.csv, manifest.json} and &lt;DataRoot&gt;\scanner-exports\ (the day's imported
/// scanner rows). DataRoot is outside the repository (Alpaca's storage terms are not yet reviewed). Research only.
/// WHEN CALLED: By the export and status endpoints; one run at a time.
/// </summary>
public sealed class OvernightHistoryExportService(string dataRoot, Func<string, int, CancellationToken, Task<DailyRecentSessions>> fetchHistory)
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly string[] Weekdays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>PURPOSE: The folder that holds research data. WHEN CALLED: Reported by the status endpoint.</summary>
    public string DataRoot => dataRoot;

    /// <summary>PURPOSE: True while an export is running. WHEN CALLED: By the status endpoint.</summary>
    public bool Running => _gate.CurrentCount == 0;

    /// <summary>
    /// PURPOSE: Resolves the data root: setting Research:DataRoot, otherwise the MCC-Data folder next to the repository
    /// folder (found by walking up from the content root to the folder holding a .sln file), otherwise %LOCALAPPDATA%.
    /// WHEN CALLED: Once at start-up.
    /// </summary>
    public static string ResolveDataRoot(string? configured, string contentRoot)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        for (var dir = new DirectoryInfo(contentRoot); dir is not null; dir = dir.Parent)
        {
            if (dir.EnumerateFiles("*.sln").Any())
                return Path.Combine(dir.Parent?.FullName ?? dir.FullName, "MCC-Data");
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MomentumCommandCenterV2", "research");
    }

    /// <summary>
    /// PURPOSE: Runs one export: fetches and analyzes each symbol, writes the CSVs, the manifest and the scanner snapshot.
    /// Returns null when another export is already running.
    /// WHEN CALLED: By POST /api/research/overnight-history/export.
    /// </summary>
    public async Task<HistoryExportRun?> RunAsync(HistoryExportRequest request, DateTimeOffset nowUtc, string? sourceSha, CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct)) return null;
        try
        {
            var symbols = (request.Symbols ?? Array.Empty<string>())
                .Select(s => (s ?? "").Trim().ToUpperInvariant())
                .Where(s => s.Length is > 0 and <= 8 && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
                .Distinct().Take(200).ToArray();
            if (symbols.Length == 0) throw new ArgumentException("Provide at least one valid symbol (import a TOS scanner CSV first).");
            var days = request.Days ?? 730;
            if (days is < 30 or > 1460) throw new ArgumentException("Days must be 30-1460.");

            var stamp = nowUtc.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var runDir = Path.Combine(dataRoot, "overnight-history", stamp);
            Directory.CreateDirectory(runDir);

            string? archive = null;
            if (request.ScannerRows is { Count: > 0 })
            {
                var archiveDir = Path.Combine(dataRoot, "scanner-exports");
                Directory.CreateDirectory(archiveDir);
                var baseName = Path.GetFileNameWithoutExtension(SafeFileName(request.ScannerFileName) ?? "dashboard-import");
                archive = Path.Combine(archiveDir, $"{baseName}-imported-{stamp}.csv");
                await File.WriteAllTextAsync(archive, ToCsv(request.ScannerRows), Encoding.UTF8, ct);
            }

            var results = new List<OvernightHistoryResult>();
            var errors = new List<string>();
            foreach (var symbol in symbols)
            {
                try { results.Add(OvernightHistoryAnalyzer.Analyze(await fetchHistory(symbol, days, ct))); }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException or ProviderNotConfiguredException or TaskCanceledException)
                { errors.Add($"{symbol}: {ex.Message}"); }
            }

            var pairsCsv = new StringBuilder("\"Symbol\",\"BuySessionDate\",\"NextSessionDate\",\"BuyWeekday\",\"CalendarGapDays\",\"BuyClose\",\"NextOpen\",\"ReturnPct\",\"Feed\"\r\n");
            var weekdayCsv = new StringBuilder("\"Symbol\",\"BuyWeekday\",\"Count\",\"WinRatePct\",\"MeanPct\",\"MedianPct\",\"StdDevPct\",\"TStat\",\"Feed\",\"FeedCoverage\"\r\n");
            foreach (var r in results)
            {
                foreach (var p in r.Pairs)
                    pairsCsv.Append(Row(r.Symbol, D(p.BuySessionDate), D(p.NextSessionDate), p.BuyWeekday, N(p.CalendarGapDays), N(p.BuyClose), N(p.NextOpen), N(p.ReturnPct), r.Feed));
                foreach (var w in r.ByWeekday.Append(r.AllNights))
                    weekdayCsv.Append(Row(r.Symbol, w.BuyWeekday, N(w.Count), N(w.WinRatePct), N(w.MeanPct), N(w.MedianPct), N(w.StdDevPct), N(w.TStat), r.Feed, r.FeedCoverage));
            }
            // Pooled statistics use consolidated (SIP) symbols only, like the script.
            var consolidated = results.Where(r => r.FeedCoverage == "CONSOLIDATED_SIP").ToArray();
            var pooled = Weekdays.Select(day => OvernightHistoryAnalyzer.Stats(day, consolidated.SelectMany(r => r.Pairs).Where(p => p.BuyWeekday == day).Select(p => p.ReturnPct).ToArray()))
                .Append(OvernightHistoryAnalyzer.Stats("All", consolidated.SelectMany(r => r.Pairs).Select(p => p.ReturnPct).ToArray()))
                .ToArray();
            var pooledCsv = new StringBuilder("\"BuyWeekday\",\"Count\",\"WinRatePct\",\"MeanPct\",\"MedianPct\",\"StdDevPct\",\"TStat\"\r\n");
            foreach (var w in pooled) pooledCsv.Append(Row(w.BuyWeekday, N(w.Count), N(w.WinRatePct), N(w.MeanPct), N(w.MedianPct), N(w.StdDevPct), N(w.TStat)));

            await File.WriteAllTextAsync(Path.Combine(runDir, "pairs.csv"), pairsCsv.ToString(), Encoding.UTF8, ct);
            await File.WriteAllTextAsync(Path.Combine(runDir, "weekday-by-symbol.csv"), weekdayCsv.ToString(), Encoding.UTF8, ct);
            await File.WriteAllTextAsync(Path.Combine(runDir, "weekday-pooled.csv"), pooledCsv.ToString(), Encoding.UTF8, ct);
            var manifest = new
            {
                createdAtUtc = nowUtc.UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
                source = "dashboard",
                mccSourceSha = sourceSha,
                scannerCsv = request.ScannerFileName,
                scannerArchive = archive,
                days,
                symbolsRequested = symbols.Length,
                symbolsSucceeded = results.Count,
                consolidatedSymbols = consolidated.Length,
                pairRows = results.Sum(r => r.Pairs.Count),
                errors,
                pooled,
                perSymbol = results.Select(r => new
                {
                    symbol = r.Symbol, feed = r.Feed, feedCoverage = r.FeedCoverage, requestStatus = r.RequestStatus,
                    expectedSessions = r.ExpectedSessionCount, sessions = r.SessionCount, pairs = r.Pairs.Count,
                    missingSessionDates = r.MissingSessionDates, pairsSkippedForMissingSessions = r.PairsSkippedForMissingSessions,
                    firstObservedSessionDate = r.FirstObservedSessionDate, preListingSessions = r.PreListingSessionCount
                }),
                note = "RESEARCH ONLY. Overnight return = next open / buy close - 1 (completed Daily bars, split-adjusted). Pooled stats use consolidated (SIP) symbols only. Weekday effects are hypotheses, not signals. Data stored outside the repository until Alpaca's storage terms are reviewed."
            };
            await File.WriteAllTextAsync(Path.Combine(runDir, "manifest.json"),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8, ct);

            return ReadRun(Path.Combine(runDir, "manifest.json"));
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// PURPOSE: The latest run (from either the Dashboard or the script) and whether one ran on today's Eastern date.
    /// WHEN CALLED: By GET /api/research/overnight-history/export-status.
    /// </summary>
    public HistoryExportStatus GetStatus(DateTimeOffset nowUtc)
    {
        var today = EasternDate(nowUtc);
        var root = Path.Combine(dataRoot, "overnight-history");
        var latest = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "manifest.json", SearchOption.AllDirectories)
                .Select(ReadRun).Where(r => r is not null).OrderByDescending(r => r!.CreatedAtUtc).FirstOrDefault()
            : null;
        return new HistoryExportStatus(latest?.SessionDateEastern == today, today, Running, latest, dataRoot,
            "Best run after the regular close + 16 minutes (14:16 AZ in standard time), so today's session is included. Research only.");
    }

    /// <summary>
    /// PURPOSE: Reads a run summary from a manifest written by this service or by the PowerShell script; null if unreadable.
    /// WHEN CALLED: By GetStatus and RunAsync.
    /// </summary>
    public static HistoryExportRun? ReadRun(string manifestPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = doc.RootElement;
            if (!root.TryGetProperty("createdAtUtc", out var created) ||
                !DateTimeOffset.TryParse(created.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt))
                return null;
            int Int(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var errors = root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(x => x.ToString()).ToArray() : Array.Empty<string>();
            var pooled = root.TryGetProperty("pooled", out var p) && p.ValueKind == JsonValueKind.Array
                ? p.Deserialize<OvernightWeekdayStats[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? Array.Empty<OvernightWeekdayStats>()
                : Array.Empty<OvernightWeekdayStats>();
            var folder = Path.GetDirectoryName(manifestPath) ?? manifestPath;
            return new HistoryExportRun(Path.GetFileName(folder), createdAt.ToUniversalTime(), EasternDate(createdAt), Str("source") ?? "script",
                Str("scannerCsv"), Int("symbolsRequested"), Int("symbolsSucceeded"), Int("pairRows"), folder, errors, pooled);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return null; }
    }

    /// <summary>PURPOSE: Eastern calendar date of an instant. WHEN CALLED: For "ran today".</summary>
    public static DateOnly EasternDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Eastern).DateTime);

    /// <summary>PURPOSE: Writes rows (dictionaries) as CSV with the union of their keys as the header. WHEN CALLED: For the scanner snapshot.</summary>
    private static string ToCsv(IReadOnlyList<Dictionary<string, string?>> rows)
    {
        var keys = rows.SelectMany(r => r.Keys).Distinct(StringComparer.Ordinal).ToArray();
        var sb = new StringBuilder(Row(keys));
        foreach (var r in rows) sb.Append(Row(keys.Select(k => r.TryGetValue(k, out var v) ? v : null).ToArray()));
        return sb.ToString();
    }

    /// <summary>PURPOSE: One quoted CSV line. WHEN CALLED: By the CSV writers.</summary>
    private static string Row(params string?[] values) =>
        string.Join(",", values.Select(v => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"")) + "\r\n";

    /// <summary>PURPOSE: Invariant number text, empty for null. WHEN CALLED: By the CSV writers.</summary>
    private static string N(double? v) => v is null ? "" : v.Value.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>PURPOSE: ISO date text. WHEN CALLED: By the CSV writers.</summary>
    private static string D(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>PURPOSE: Keeps only a safe file name (no folders); null when nothing usable remains. WHEN CALLED: For the scanner archive name.</summary>
    private static string? SafeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var leaf = Path.GetFileName(name.Trim());
        var safe = new string(leaf.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        return safe.Length == 0 ? null : safe;
    }
}
