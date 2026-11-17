using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: CatalystAttestationStore
/// PURPOSE: Append-only operator catalyst sign-offs ("I checked these sources; nothing blocking") for one symbol and
/// one Boring Overnight buy session. A sign-off never relabels news as CLEAR; READY shows it as OPERATOR-ATTESTED.
/// Operator decision 2026-11-14: tick at least one source (Schwab news, SEC EDGAR, company IR, other); a note is
/// optional except that OTHER needs a note naming the source. A sign-off is valid only on the buy-session date (Eastern)
/// and expires at that session's buy deadline (regular close).
/// WHEN CALLED: By the sign-off endpoints and by BoringOvernightReadyService when evaluating READY.
/// </summary>
public sealed class CatalystAttestationStore(string? path = null)
{
    /// <summary>Source codes the operator can tick.</summary>
    public static readonly IReadOnlyList<string> AllowedSources = new[] { "SCHWAB_NEWS", "SEC_EDGAR", "COMPANY_IR", "OTHER" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = path
        ?? Environment.GetEnvironmentVariable("MCC_CATALYST_ATTESTATION_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MomentumCommandCenterV2", "catalyst-attestations.jsonl");

    /// <summary>
    /// METHOD: CreateRecord
    /// PURPOSE: Validates an input and builds the record for the buy session in effect at nowUtc. Throws ArgumentException
    /// when the symbol or sources are invalid, OTHER has no note, or the buy session is not today (Eastern) / already closed.
    /// WHEN CALLED: By AppendAsync, and by tests.
    /// </summary>
    public static CatalystAttestation CreateRecord(CatalystAttestationInput input, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);
        var symbol = (input.Symbol ?? string.Empty).Trim().ToUpperInvariant();
        if (symbol.Length is < 1 or > 8 || !symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1-8 letters, digits, dot, or hyphen.");
        var sources = (input.SourcesChecked ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal).ToArray();
        if (sources.Length == 0)
            throw new ArgumentException("Tick at least one source you checked.");
        var unknown = sources.Where(x => !AllowedSources.Contains(x)).ToArray();
        if (unknown.Length > 0)
            throw new ArgumentException($"Unknown source(s): {string.Join(", ", unknown)}. Allowed: {string.Join(", ", AllowedSources)}.");
        var note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        if (sources.Contains("OTHER") && note is null)
            throw new ArgumentException("OTHER needs a note naming the source you checked.");

        var clock = MarketSessionClock.GetSnapshot(nowUtc);
        var todayEastern = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, Eastern).DateTime);
        // The late buy window (operator decision 2026-11-16) extends sign-off acceptance and validity to close + 60 min.
        if (clock.OvernightBuySessionDate != todayEastern || nowUtc >= clock.OvernightLateBuyDeadlineUtc)
            throw new ArgumentException(
                $"A catalyst sign-off is accepted only on the buy-session day before the late buy deadline (close + 60 min). Next buy session: {clock.OvernightBuySessionDate:yyyy-MM-dd}.");

        return new CatalystAttestation(Guid.NewGuid().ToString("N"), symbol, clock.OvernightBuySessionDate,
            sources, note, nowUtc, clock.OvernightLateBuyDeadlineUtc);
    }

    /// <summary>
    /// METHOD: IsValidFor
    /// PURPOSE: True when the sign-off is for this symbol and buy session and has not expired at nowUtc.
    /// WHEN CALLED: By GetValidAsync and the READY catalyst gate.
    /// </summary>
    public static bool IsValidFor(CatalystAttestation attestation, string symbol, DateOnly buySessionDate, DateTimeOffset nowUtc) =>
        attestation.Symbol.Equals(symbol.Trim(), StringComparison.OrdinalIgnoreCase) &&
        attestation.BuySessionDate == buySessionDate &&
        attestation.AttestedAtUtc <= nowUtc && nowUtc < attestation.ExpiresAtUtc;

    /// <summary>
    /// METHOD: AppendAsync
    /// PURPOSE: Validates and appends one sign-off (append-only JSONL).
    /// WHEN CALLED: POST /api/boring-overnight/catalyst-attestations.
    /// </summary>
    public async Task<CatalystAttestation> AppendAsync(CatalystAttestationInput input, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var record = CreateRecord(input, nowUtc);
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine, ct);
            return record;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// METHOD: GetAsync
    /// PURPOSE: Returns sign-offs, newest first, optionally for one symbol.
    /// WHEN CALLED: GET /api/boring-overnight/catalyst-attestations and GetValidAsync.
    /// </summary>
    public async Task<IReadOnlyList<CatalystAttestation>> GetAsync(string? symbol, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1-500.");
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<CatalystAttestation>();
            var rows = new List<CatalystAttestation>();
            foreach (var line in await File.ReadAllLinesAsync(_path, ct))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var row = JsonSerializer.Deserialize<CatalystAttestation>(line, JsonOptions)
                    ?? throw new InvalidDataException("Catalyst attestation JSONL contains an empty record.");
                if (string.IsNullOrWhiteSpace(symbol) || row.Symbol.Equals(symbol.Trim(), StringComparison.OrdinalIgnoreCase))
                    rows.Add(row);
            }
            return rows.OrderByDescending(x => x.AttestedAtUtc).Take(limit).ToArray();
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// METHOD: GetValidAsync
    /// PURPOSE: Returns the newest sign-off valid for this symbol and buy session at nowUtc, or null.
    /// WHEN CALLED: By BoringOvernightReadyService.
    /// </summary>
    public async Task<CatalystAttestation?> GetValidAsync(string symbol, DateOnly buySessionDate, DateTimeOffset nowUtc, CancellationToken ct) =>
        (await GetAsync(symbol, 500, ct)).FirstOrDefault(x => IsValidFor(x, symbol, buySessionDate, nowUtc));
}

/// <summary>
/// TYPE: CatalystAttestationInput
/// PURPOSE: Operator sign-off request body.
/// WHEN CALLED: Bound from POST /api/boring-overnight/catalyst-attestations.
/// </summary>
public sealed record CatalystAttestationInput(string Symbol, IReadOnlyList<string>? SourcesChecked, string? Note);

/// <summary>
/// TYPE: CatalystAttestation
/// PURPOSE: A stored, expiring operator catalyst sign-off for one symbol and buy session.
/// WHEN CALLED: Persisted by CatalystAttestationStore; read by the READY catalyst gate.
/// </summary>
public sealed record CatalystAttestation(
    string AttestationId,
    string Symbol,
    DateOnly BuySessionDate,
    IReadOnlyList<string> SourcesChecked,
    string? Note,
    DateTimeOffset AttestedAtUtc,
    DateTimeOffset ExpiresAtUtc);
