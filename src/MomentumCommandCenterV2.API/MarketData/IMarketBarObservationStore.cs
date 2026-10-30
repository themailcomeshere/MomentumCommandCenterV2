namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Persist provider observations without overwriting source history.
/// WHEN CALLED: Historical comparisons and future live collector ingestion.
/// </summary>
public interface IMarketBarObservationStore
{
    /// <summary>
    /// PURPOSE: Append unseen bars and preserve corrected values as new observations.
    /// WHEN CALLED: A provider adapter returns a batch of normalized OHLCV bars.
    /// </summary>
    Task<int> AppendAsync(IReadOnlyCollection<MarketBar> bars, CancellationToken cancellationToken);

    /// <summary>
    /// PURPOSE: Query the latest known version of each stored provider/symbol/timestamp bar.
    /// WHEN CALLED: Historical calculations, parity checks, and audit endpoints read persisted observations.
    /// IMPORTANT: Returned bars are raw source observations, not session-paired Overnight returns.
    /// </summary>
    Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(
        string symbol,
        string? provider,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken,
        string? timeframe = null);
}

/// <summary>
/// PURPOSE: Immutable audit envelope around one provider OHLCV observation.
/// WHEN CALLED: Written to the append-only JSONL raw-bar store.
/// </summary>
public sealed record MarketBarObservation(
    string ObservationId,
    DateTimeOffset ReceivedAt,
    string Provider,
    string Symbol,
    DateTimeOffset SourceTimestamp,
    MarketBar Bar,
    string ContentHash,
    string? SupersedesObservationId);
