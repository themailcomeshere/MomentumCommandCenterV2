using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models
{
    public sealed record SignalDecision(SignalState State, int Score, string Reason, bool LongPermission, bool EntryAllowed, bool RunnerAllowed, bool ExitWarning, bool HardExit);
}
