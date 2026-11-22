using System.Text.Json;
using MomentumCommandCenterV2.Core;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: IntradaySettings
/// PURPOSE: Operator settings for the INTRADAY MOMENTUM workspace. OpeningEntryHoldMinutes = minutes after the 09:30 ET
/// open during which EXECUTION READY is withheld (0 = off). Source is OPERATOR_SETTING when the operator saved it from
/// the Dashboard, otherwise APPSETTINGS_DEFAULT (MarketData:Intraday:OpeningEntryHoldMinutes).
/// WHEN CALLED: Read by LiveSymbolAssessmentService and the /api/intraday/settings endpoints.
/// </summary>
public sealed record IntradaySettings(int OpeningEntryHoldMinutes, string Source, DateTimeOffset? UpdatedAtUtc);

/// <summary>
/// TYPE: IntradaySettingsStore
/// PURPOSE: Keeps the operator's intraday settings in %LOCALAPPDATA%\MomentumCommandCenterV2\intraday-settings.json
/// (override: MCC_INTRADAY_SETTINGS_PATH or the test constructor) so a Dashboard change survives an API restart.
/// The value is held in memory; reads never touch the disk.
/// WHEN CALLED: Once by DI; read on every live assessment and replay; written by PUT /api/intraday/settings.
/// </summary>
public sealed class IntradaySettingsStore
{
    /// <summary>Configuration key for the default hold when the operator has not saved one.</summary>
    public const string DefaultHoldConfigKey = "MarketData:Intraday:OpeningEntryHoldMinutes";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IntradaySettings _current;

    /// <summary>PURPOSE: Resolves the file path and loads saved settings or the configured default. WHEN CALLED: Once by DI.</summary>
    public IntradaySettingsStore(IConfiguration configuration, ILogger<IntradaySettingsStore> logger)
        : this(
            Environment.GetEnvironmentVariable("MCC_INTRADAY_SETTINGS_PATH")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MomentumCommandCenterV2", "intraday-settings.json"),
            ReadDefaultHold(configuration, logger),
            logger)
    { }

    /// <summary>PURPOSE: Uses an explicit file path and default hold. WHEN CALLED: By tests and the DI constructor.</summary>
    public IntradaySettingsStore(string path, int defaultHoldMinutes, ILogger? logger = null)
    {
        if (!OpeningEntryHold.IsValidMinutes(defaultHoldMinutes))
            throw new ArgumentOutOfRangeException(nameof(defaultHoldMinutes), defaultHoldMinutes, $"Opening hold must be 0–{OpeningEntryHold.MaxMinutes} minutes.");
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _current = new IntradaySettings(defaultHoldMinutes, "APPSETTINGS_DEFAULT", null);
        try
        {
            if (File.Exists(path))
            {
                var saved = JsonSerializer.Deserialize<IntradaySettings>(File.ReadAllText(path));
                if (saved is not null && OpeningEntryHold.IsValidMinutes(saved.OpeningEntryHoldMinutes))
                    _current = saved with { Source = "OPERATOR_SETTING" };
                else
                    logger?.LogWarning("Ignoring invalid intraday settings file {Path}; using the configured default.", path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger?.LogWarning(ex, "Could not read intraday settings file {Path}; using the configured default.", path);
        }
    }

    /// <summary>Full path of the settings file (shown by the endpoint).</summary>
    public string FilePath => _path;

    /// <summary>PURPOSE: The current settings. WHEN CALLED: Per live assessment, replay and GET.</summary>
    public IntradaySettings Current => Volatile.Read(ref _current);

    /// <summary>
    /// PURPOSE: Saves a new opening hold (0–120 minutes) and returns the stored settings. Throws ArgumentOutOfRange
    /// for other values. WHEN CALLED: By PUT /api/intraday/settings.
    /// </summary>
    public async Task<IntradaySettings> SetOpeningEntryHoldAsync(int minutes, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!OpeningEntryHold.IsValidMinutes(minutes))
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, $"Opening hold must be 0–{OpeningEntryHold.MaxMinutes} minutes.");
        await _gate.WaitAsync(ct);
        try
        {
            var next = new IntradaySettings(minutes, "OPERATOR_SETTING", nowUtc);
            var temp = _path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(next, JsonOptions), ct);
            File.Move(temp, _path, overwrite: true);
            Volatile.Write(ref _current, next);
            return next;
        }
        finally { _gate.Release(); }
    }

    private static int ReadDefaultHold(IConfiguration configuration, ILogger logger)
    {
        var raw = configuration[DefaultHoldConfigKey];
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        if (int.TryParse(raw, out var minutes) && OpeningEntryHold.IsValidMinutes(minutes)) return minutes;
        logger.LogWarning("{Key}={Value} is not 0–{Max}; the opening hold defaults to off.", DefaultHoldConfigKey, raw, OpeningEntryHold.MaxMinutes);
        return 0;
    }
}
