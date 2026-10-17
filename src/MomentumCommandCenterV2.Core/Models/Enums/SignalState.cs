namespace MomentumCommandCenterV2.Core.Models.Enums;

/// <summary>
/// TYPE: SignalState
/// PURPOSE: Defines the enum values or data structure named SignalState.
/// WHEN CALLED: Referenced by application workflows whenever this state or data contract is needed.
/// </summary>
public enum SignalState
{
    NoTrade,

    // Pre-entry
    PrepareBuy,
    Buy,

    // Position management
    Runner,
    RunnerWaning,
    PrepareSell,
    ConfirmedWeakness,
    Breakdown,

    // Terminal
    Exit
}