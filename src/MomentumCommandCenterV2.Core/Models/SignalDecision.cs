using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models;

public sealed record SignalDecision(
    SignalState State,
    MomentumAction Action,
    int Score,
    string Reason,
    bool LongPermission,
    bool EntryAllowed,
    bool RunnerAllowed,
    bool ExitWarning,
    bool HardExit)
{
    public bool IsEntrySignal =>
        State == SignalState.Buy &&
        Action == MomentumAction.BUY;

    public bool IsRunner =>
        State == SignalState.Runner ||
        State == SignalState.RunnerWaning;

    public bool IsExitState =>
        State == SignalState.PrepareSell ||
        State == SignalState.ConfirmedWeakness ||
        State == SignalState.Breakdown ||
        State == SignalState.Exit;

    public bool IsHardExit =>
        HardExit ||
        State == SignalState.Breakdown ||
        State == SignalState.Exit;
}