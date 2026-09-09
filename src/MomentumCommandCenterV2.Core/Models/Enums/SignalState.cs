namespace MomentumCommandCenterV2.Core.Models.Enums;

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