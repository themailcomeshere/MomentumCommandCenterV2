namespace MomentumCommandCenterV2.Core.Models;

/// <summary>
/// Diagnostic-only inputs for ranking an already evaluated entry candidate.
/// Values must come from a timestamped market-data snapshot; do not infer
/// unavailable measurements from a single OHLCV bar.
/// </summary>
public sealed record OpportunityQualityInput(
    string Symbol,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset OneMinuteTimestamp,
    DateTimeOffset FiveMinuteTimestamp,
    bool ExistingEntryEligible,
    bool ExecutionEventActive,
    decimal? RelativeVolume,
    bool MomentumConfirmed,
    decimal? AtrExtension,
    decimal? RecentRangeInAtr,
    decimal? ResistanceDistanceInAtr);
