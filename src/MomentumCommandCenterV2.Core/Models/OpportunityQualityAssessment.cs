namespace MomentumCommandCenterV2.Core.Models;

public enum OpportunityClassification
{
    DataUnknown,
    Blocked,
    WatchDeveloping,
    ReadyHighOpportunity,
    ReadyModerate,
    ReadyBoring,
    ReadyExtended
}

public enum OpportunityReason
{
    DataMissing,
    DataStale,
    EntryNotEligible,
    ExecutionEventInactive,
    LowRelativeVolume,
    MomentumNotConfirmed,
    LimitedRecentRange,
    ResistanceTooClose,
    EntryExtended,
    StrongParticipation,
    MomentumConfirmed,
    AdequateMovementCapacity
}

/// <summary>
/// Explainable diagnostic assessment. This is not a profit probability,
/// order instruction, or replacement for the V2 signal/position engine.
/// </summary>
public sealed record OpportunityQualityAssessment(
    string Symbol,
    OpportunityClassification Classification,
    IReadOnlyList<OpportunityReason> Reasons,
    DateTimeOffset EvaluatedAt)
{
    public string DisplayLabel => Classification switch
    {
        OpportunityClassification.ReadyHighOpportunity => "READY — HIGH",
        OpportunityClassification.ReadyModerate => "READY — MODERATE",
        OpportunityClassification.ReadyBoring => "READY — BORING",
        OpportunityClassification.ReadyExtended => "READY — EXTENDED",
        OpportunityClassification.WatchDeveloping => "WATCH — DEVELOPING",
        OpportunityClassification.Blocked => "BLOCKED",
        _ => "DATA?"
    };
}

/// <summary>
/// Explicitly provisional bands for diagnostic prototyping only.
/// Do not treat these defaults as calibrated trading thresholds.
/// </summary>
public sealed record OpportunityQualityConfig(
    TimeSpan MaxOneMinuteAge,
    TimeSpan MaxFiveMinuteAge,
    decimal MinimumRelativeVolume,
    decimal HighRelativeVolume,
    decimal ExtensionWarningAtr,
    decimal MinimumRecentRangeAtr,
    decimal ResistanceTooCloseAtr)
{
    public static OpportunityQualityConfig DiagnosticDefaults { get; } = new(
        MaxOneMinuteAge: TimeSpan.FromMinutes(2),
        MaxFiveMinuteAge: TimeSpan.FromMinutes(6),
        MinimumRelativeVolume: 1.5m,
        HighRelativeVolume: 2.5m,
        ExtensionWarningAtr: 1.5m,
        MinimumRecentRangeAtr: 1.0m,
        ResistanceTooCloseAtr: 0.5m);
}
