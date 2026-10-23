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
