using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Reproduces the supplied Thinkorswim Overnight Scanner formulas from explicit daily OHLC observations.
/// Version MCC-TOS-DAILY-GAP-1.0. Session semantics are preserved as metadata, never inferred.
/// </summary>
public static class ThinkorswimDailyGapCalculator
{
    public const string CalculationVersion = "MCC-TOS-DAILY-GAP-1.0";

    public static ThinkorswimDailyGapResult Calculate(ThinkorswimDailyGapImport request)
    {
        Validate(request);
        var bars = request.Bars.OrderBy(x => x.SessionDate).ToArray();
        if (bars.Select(x => x.SessionDate).Distinct().Count() != bars.Length)
            throw new InvalidDataException("Duplicate daily session dates are not allowed.");
        var returns = new List<decimal>();
        for (var i = 1; i < bars.Length; i++)
        {
            if (bars[i - 1].Close <= 0 || bars[i].Open <= 0)
                throw new InvalidDataException("Daily open and previous close must be positive.");
            returns.Add((bars[i].Open / bars[i - 1].Close) - 1m);
        }
        if (returns.Count < 20)
            return new(request.Symbol.Trim().ToUpperInvariant(), CalculationVersion, request.Source,
                request.Aggregation, request.ExtendedHours, request.CorporateActionAdjusted, request.ChartTimeZone,
                bars.Length, returns.Count, "INSUFFICIENT_HISTORY", null, null, null, null, null, null, null, null,
                Array.Empty<decimal>(), "At least 21 daily OHLC bars are required for 20 daily-gap observations.");

        var last20 = returns.TakeLast(20).ToArray();
        var last5 = returns.TakeLast(5).ToArray();
        var avg20 = last20.Average();
        var avg5 = last5.Average();
        var median20 = Median(last20);
        var win20 = last20.Count(x => x > 0) * 100m / 20m;
        var currentOn = returns[^1] * 100m;
        var above20 = bars[^1].Close > bars.TakeLast(20).Average(x => x.Close);
        var momentum = (avg5 - avg20) * 100m;
        var score = win20 / 100m * 50m + avg20 * 100m * 1000m
            + median20 * 100m * 1000m + (avg5 - avg20) * 1000m
            + (above20 ? 10m : 0m);
        return new(request.Symbol.Trim().ToUpperInvariant(), CalculationVersion, request.Source,
            request.Aggregation, request.ExtendedHours, request.CorporateActionAdjusted, request.ChartTimeZone,
            bars.Length, returns.Count, "COMPLETE_20_GAPS",
            win20, avg20 * 100m, median20 * 100m, avg5 * 100m, momentum, currentOn,
            above20 ? 1 : 0, score, last20,
            "Exact supplied formulas calculated from TOS daily OHLC. Aggregation/session semantics are metadata only and are not independently verified by this calculation.");
    }

    public static void Validate(ThinkorswimDailyGapImport request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Symbol) || request.Symbol.Length > 8 ||
            !request.Symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("A valid ticker symbol is required.");
        if (string.IsNullOrWhiteSpace(request.Source) || !request.Source.Contains("Thinkorswim", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source must explicitly identify Thinkorswim reference data.");
        if (string.IsNullOrWhiteSpace(request.Aggregation) || string.IsNullOrWhiteSpace(request.ChartTimeZone))
            throw new ArgumentException("Aggregation and chart timezone are required provenance.");
        if (request.Bars is null || request.Bars.Count < 1 || request.Bars.Count > 1000)
            throw new ArgumentException("Supply 1–1000 daily OHLC observations.");
        foreach (var bar in request.Bars)
        {
            if (bar.SessionDate == default || bar.Open <= 0 || bar.High <= 0 || bar.Low <= 0 || bar.Close <= 0 ||
                bar.Low > Math.Min(bar.Open, bar.Close) || bar.High < Math.Max(bar.Open, bar.Close) || bar.Low > bar.High)
                throw new InvalidDataException("Invalid daily OHLC observation.");
        }
    }

    private static decimal Median(IReadOnlyCollection<decimal> values)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        return sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2m : sorted[sorted.Length / 2];
    }
}

public sealed record ThinkorswimDailyGapBar(DateOnly SessionDate, decimal Open, decimal High, decimal Low, decimal Close);
public sealed record ThinkorswimDailyGapImport(
    string Symbol, string Source, string Aggregation, bool ExtendedHours,
    bool CorporateActionAdjusted, string ChartTimeZone, IReadOnlyList<ThinkorswimDailyGapBar> Bars);
public sealed record ThinkorswimDailyGapResult(
    string Symbol, string CalculationVersion, string Source, string Aggregation, bool ExtendedHours,
    bool CorporateActionAdjusted, string ChartTimeZone, int BarCount, int ReturnCount, string Status,
    decimal? OnWinPct, decimal? AvgOnPct, decimal? MedianOnPct, decimal? FiveDayOnPct,
    decimal? OnMomentum, decimal? CurrentOnPct, int? Above20Sma, decimal? OvernightScore,
    IReadOnlyList<decimal> Last20ReturnsDecimal, string ProvenanceNote);

/// <summary>Append-only TOS daily reference observations. Corrections append new imports; no source rows are overwritten.</summary>
public sealed class ThinkorswimDailyGapReferenceStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Environment.GetEnvironmentVariable("MCC_TOS_DAILY_GAP_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "tos-daily-gap-reference.jsonl");

    public async Task<ThinkorswimDailyGapStoredImport> AppendAsync(ThinkorswimDailyGapImport request, CancellationToken ct)
    {
        var result = ThinkorswimDailyGapCalculator.Calculate(request);
        var row = new ThinkorswimDailyGapStoredImport(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow,
            request, result, Fingerprint(request));
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read, 16384, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteLineAsync(JsonSerializer.Serialize(row, Options).AsMemory(), ct);
            await writer.FlushAsync(ct);
            return row;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<ThinkorswimDailyGapStoredImport>> LatestAsync(string? symbol, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit));
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<ThinkorswimDailyGapStoredImport>();
            var rows = new List<ThinkorswimDailyGapStoredImport>();
            using var reader = new StreamReader(new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var item = JsonSerializer.Deserialize<ThinkorswimDailyGapStoredImport>(line, Options)
                    ?? throw new InvalidDataException("TOS daily-gap reference store contains an invalid record.");
                if (string.IsNullOrWhiteSpace(symbol) || item.Request.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)) rows.Add(item);
            }
            return rows.OrderByDescending(x => x.ImportedAtUtc).Take(limit).ToArray();
        }
        finally { _gate.Release(); }
    }

    private static string Fingerprint(ThinkorswimDailyGapImport request)
    {
        var canonical = request.Symbol.ToUpperInvariant() + "|" + request.Aggregation + "|" + request.ExtendedHours + "|" +
            request.CorporateActionAdjusted + "|" + request.ChartTimeZone + "|" +
            string.Join(";", request.Bars.OrderBy(x => x.SessionDate).Select(x => string.Join(",",
                x.SessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x.Open, x.High, x.Low, x.Close)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed record ThinkorswimDailyGapStoredImport(string ImportId, DateTimeOffset ImportedAtUtc,
    ThinkorswimDailyGapImport Request, ThinkorswimDailyGapResult Calculation, string ContentFingerprint);
