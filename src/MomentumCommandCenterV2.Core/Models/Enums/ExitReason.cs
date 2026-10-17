namespace MomentumCommandCenterV2.Core.Models.Enums
{
    /// <summary>
    /// TYPE: ExitReason
    /// PURPOSE: Defines the enum values used by the V2 decision model.
    /// WHEN CALLED: Referenced by signal evaluation, trade analysis, or serialization when this state/reason is needed.
    /// </summary>
    public enum ExitReason
    {
        Manual,
        Target,
        StructureBreak,
        MomentumLoss,
        VwapLoss,
        TimeStop,
        EndOfDay,
        RiskStop,
        Unknown
    }
}
