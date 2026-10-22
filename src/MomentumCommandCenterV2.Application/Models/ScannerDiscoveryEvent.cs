using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Application.Models;

/// <summary>
/// TYPE: ScannerDiscoveryEvent
/// PURPOSE: Preserves one provider-neutral scanner observation and its provenance.
/// WHEN CALLED: Created by a scanner adapter when a symbol is observed in a scanner group.
/// IMPORTANT: A discovery event is not a trading signal and does not imply 5M permission or 1M EXECUTION READY.
/// </summary>
public sealed record ScannerDiscoveryEvent(
    string Symbol,
    string ScannerGroup,
    string SourceId,
    string? SourceEventId,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset ObservedAt,
    DateTimeOffset? SourceTimestamp,
    string SchemaVersion,
    IReadOnlyDictionary<string, string?> RawFields,
    ScannerCandidateDisposition Disposition,
    IReadOnlyList<string> ReasonCodes,
    DateTimeOffset EvaluatedAt,
    string EvaluatorVersion);

/// <summary>
/// ENUM: ScannerCandidateDisposition
/// PURPOSE: Describes scanner-list presentation without replacing MCC trading gates.
/// WHEN CALLED: Used by scanner qualification and presentation workflows.
/// </summary>
public enum ScannerCandidateDisposition
{
    ActionableQueue = 0,
    WatchPullback = 1,
    FilteredExtended = 2,
    Blocked = 3,
    DataUnknown = 4
}
