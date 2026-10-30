namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Normalized OHLCV observation returned by a market-data provider.
/// WHEN CALLED: API comparisons and Morning Brief market snapshots.
/// </summary>
public sealed record MarketBar(
    string Symbol,
    DateTimeOffset Timestamp,
    double Open,
    double High,
    double Low,
    double Close,
    long Volume,
    string Provider,
    string? Feed,
    string Timeframe = "UNKNOWN");

/// <summary>
/// PURPOSE: Server-side contract shared by provider adapters.
/// WHEN CALLED: API comparison and Morning Brief requests.
/// </summary>
public interface IMarketDataProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    /// <summary>True only after this process has successfully retrieved at least one market bar.</summary>
    bool HasSuccessfulDataValidation { get; }
    DateTimeOffset? LastSuccessfulDataValidationUtc { get; }
    int LastSuccessfulDataValidationBarCount { get; }
    IReadOnlyList<string> Capabilities { get; }
    Task<IReadOnlyList<MarketBar>> GetBarsAsync(
        string symbol,
        TimeSpan timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
}

/// <summary>
/// PURPOSE: Explicitly represents a provider that cannot make a request because credentials are absent.
/// WHEN CALLED: An API endpoint requests data from an unconfigured adapter.
/// </summary>
public sealed class ProviderNotConfiguredException(string message) : InvalidOperationException(message);

/// <summary>
/// PURPOSE: Summarizes provider results without hiding missing observations or gaps.
/// WHEN CALLED: The parallel provider-comparison endpoint.
/// </summary>
public sealed record ProviderComparisonResult(
    string Provider,
    bool Configured,
    int BarCount,
    int PersistedObservationCount,
    MarketBar? FirstBar,
    MarketBar? LatestBar,
    int DuplicateTimestampCount,
    int GapCount,
    IReadOnlyList<DateTimeOffset> SampleGapStarts,
    string? Error,
    IReadOnlyList<string> Capabilities)
{
    /// <summary>
    /// PURPOSE: Creates a truthful unavailable result while preserving provider configuration state.
    /// WHEN CALLED: A provider has no credentials or a request fails.
    /// </summary>
    public static ProviderComparisonResult Unavailable(
        string provider,
        bool configured,
        string error,
        IReadOnlyList<string> capabilities) =>
        new(provider, configured, 0, 0, null, null, 0, 0, Array.Empty<DateTimeOffset>(), error, capabilities);
}
