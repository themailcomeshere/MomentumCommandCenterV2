namespace MomentumCommandCenterV2.Core.Models.Enums
{
    /// <summary>
    /// TYPE: MomentumAction
    /// PURPOSE: Defines the enum values used by the V2 decision model.
    /// WHEN CALLED: Referenced by signal evaluation, trade analysis, or serialization when this state/reason is needed.
    /// </summary>
    public enum MomentumAction
    {
        NONE,
        PREPARE_BUY,
        BUY,
        HOLD,
        PREPARE_SELL,
        SELL
    }
}
