namespace MomentumCommandCenterV2.Core.Models.Enums
{
    /// <summary>
    /// TYPE: OpportunityReason
    /// PURPOSE: Defines the enum values or data structure named OpportunityReason.
    /// WHEN CALLED: Referenced by application workflows whenever this state or data contract is needed.
    /// </summary>
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
}
