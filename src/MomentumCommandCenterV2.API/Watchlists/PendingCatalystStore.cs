using System.Text.Json;

namespace MomentumCommandCenterV2.API.Watchlists;

/// <summary>
/// TYPE: PendingCatalystStore
/// PURPOSE: The operator-maintained list of symbols with a pending binary catalyst (for example a Phase 3 readout,
/// FDA decision, or court ruling) whose date and outcome are unknown. A listed symbol is BLOCKED from Boring Overnight
/// READY and flagged on intraday Red Zone cards. Entries stay until the operator removes them (readouts slip; nothing
/// expires automatically). Stored in App_Data/pending-catalysts.json, which is git-ignored so operator edits never
/// dirty the working tree.
/// WHEN CALLED: By the pending-catalyst endpoints and BoringOvernightReadyService.
/// </summary>
public sealed class PendingCatalystStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>PURPOSE: Resolves the file under the API content root's App_Data folder. WHEN CALLED: Once, by DI.</summary>
    public PendingCatalystStore(IWebHostEnvironment environment)
        : this(Path.Combine(environment.ContentRootPath, "App_Data", "pending-catalysts.json")) { }

    /// <summary>PURPOSE: Uses an explicit file path. WHEN CALLED: By tests.</summary>
    public PendingCatalystStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    /// <summary>PURPOSE: All entries, sorted by symbol. WHEN CALLED: By the list endpoint and READY.</summary>
    public async Task<IReadOnlyList<PendingCatalystEntry>> GetAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return (await ReadAsync(ct)).OrderBy(x => x.Symbol).ToArray(); }
        finally { _gate.Release(); }
    }

    /// <summary>PURPOSE: The entry for one symbol, or null. WHEN CALLED: By READY.</summary>
    public async Task<PendingCatalystEntry?> FindAsync(string symbol, CancellationToken ct) =>
        (await GetAsync(ct)).FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

    /// <summary>PURPOSE: Adds or replaces the entry for a symbol. WHEN CALLED: By the POST endpoint.</summary>
    public async Task<IReadOnlyList<PendingCatalystEntry>> AddAsync(AddPendingCatalystRequest request, DateTimeOffset nowUtc, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var entries = await ReadAsync(ct);
            var symbol = request.Symbol.Trim().ToUpperInvariant();
            entries.RemoveAll(x => x.Symbol == symbol);
            entries.Add(new PendingCatalystEntry(symbol, request.Event.Trim(), request.ExpectedWindow.Trim(),
                string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(), nowUtc));
            await WriteAsync(entries, ct);
            return entries.OrderBy(x => x.Symbol).ToArray();
        }
        finally { _gate.Release(); }
    }

    /// <summary>PURPOSE: Removes a symbol's entry. WHEN CALLED: By the DELETE endpoint, after the readout.</summary>
    public async Task<IReadOnlyList<PendingCatalystEntry>> RemoveAsync(string symbol, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var entries = await ReadAsync(ct);
            entries.RemoveAll(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            await WriteAsync(entries, ct);
            return entries.OrderBy(x => x.Symbol).ToArray();
        }
        finally { _gate.Release(); }
    }

    private async Task<List<PendingCatalystEntry>> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return new();
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<PendingCatalystEntry>>(stream, JsonOptions, ct) ?? new();
    }

    private async Task WriteAsync(IReadOnlyList<PendingCatalystEntry> entries, CancellationToken ct)
    {
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, ct);
        File.Move(temp, _path, true);
    }
}

/// <summary>
/// TYPE: PendingCatalystEntry
/// PURPOSE: One pending binary catalyst: symbol, what the event is, the expected window as the source states it
/// (free text such as "Q4 2026"; never a guessed date), an optional source URL and notes, and when it was added.
/// WHEN CALLED: Stored by PendingCatalystStore; returned by the endpoints; read by READY and the Dashboard.
/// </summary>
public sealed record PendingCatalystEntry(string Symbol, string Event, string ExpectedWindow, string? SourceUrl, string? Notes, DateTimeOffset AddedAtUtc);

/// <summary>
/// TYPE: AddPendingCatalystRequest
/// PURPOSE: POST body for adding a pending binary catalyst.
/// WHEN CALLED: By POST /api/watchlists/pending-catalysts.
/// </summary>
public sealed record AddPendingCatalystRequest(string Symbol, string Event, string ExpectedWindow, string? SourceUrl, string? Notes);
