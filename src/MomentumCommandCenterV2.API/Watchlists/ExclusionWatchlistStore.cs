using System.Text.Json;

namespace MomentumCommandCenterV2.API.Watchlists;

/// <summary>
/// Stores MCC's local exclusion watchlist in the API data directory. It is not a Thinkorswim account sync.
/// </summary>
public sealed class ExclusionWatchlistStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ExclusionWatchlistStore(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "exclusions-watchlist.json");
    }

    /// <summary>Reads the current persisted exclusion list for scanner gating.</summary>
    public async Task<IReadOnlyList<ExclusionWatchlistEntry>> GetAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return await ReadAsync(ct); }
        finally { _gate.Release(); }
    }

    /// <summary>Adds or updates a symbol in the local exclusion list.</summary>
    public async Task<IReadOnlyList<ExclusionWatchlistEntry>> AddAsync(string symbol, string? reason, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var entries = (await ReadAsync(ct)).ToList();
            var normalized = symbol.ToUpperInvariant();
            var existing = entries.FindIndex(x => x.Symbol == normalized);
            var entry = new ExclusionWatchlistEntry(normalized, string.IsNullOrWhiteSpace(reason) ? "User excluded" : reason.Trim(), DateTimeOffset.UtcNow);
            if (existing >= 0) entries[existing] = entry; else entries.Add(entry);
            await WriteAsync(entries, ct);
            return entries.OrderBy(x => x.Symbol).ToArray();
        }
        finally { _gate.Release(); }
    }

    /// <summary>Removes a symbol from the local exclusion list.</summary>
    public async Task<IReadOnlyList<ExclusionWatchlistEntry>> RemoveAsync(string symbol, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var entries = (await ReadAsync(ct)).Where(x => x.Symbol != symbol.ToUpperInvariant()).ToArray();
            await WriteAsync(entries, ct);
            return entries.OrderBy(x => x.Symbol).ToArray();
        }
        finally { _gate.Release(); }
    }

    private async Task<List<ExclusionWatchlistEntry>> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return new();
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<ExclusionWatchlistEntry>>(stream, JsonOptions, ct) ?? new();
    }

    private async Task WriteAsync(IReadOnlyList<ExclusionWatchlistEntry> entries, CancellationToken ct)
    {
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, ct);
        File.Move(temp, _path, true);
    }
}

public sealed record ExclusionWatchlistEntry(string Symbol, string Reason, DateTimeOffset UpdatedAtUtc);
