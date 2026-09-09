using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models;

// =========================================================
// MODEL: SignalDecision
// PURPOSE:
//   Represents the V2 engine's trading-state decision for
//   the current market snapshot.
//
// RESPONSIBILITIES:
//   - Store signal state and action.
//   - Store scoring and permission information.
//   - Identify entry, runner, and exit states.
//   - Preserve the evaluation context used to reach the decision.
//
// DOES NOT:
//   - Submit orders.
//   - Access Schwab.
//   - Execute trades.
//
// LAYER:
//   Core
// =========================================================
public sealed record SignalDecision(
    SignalState State,
    MomentumAction Action,
    int Score,
    string Reason,
    bool LongPermission,
    bool EntryAllowed,
    bool RunnerAllowed,
    bool ExitWarning,
    bool HardExit,
    SignalEvaluationContext? Evaluation = null)
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