using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// TYPE: LiveDecisionProjection
/// PURPOSE: Projects a V2 <see cref="SignalDecision"/> into the live-assessment JSON shape, including the
/// Thinkorswim-style state label and colour tone that the Dashboard Red Zone cards display.
/// WHEN CALLED: By <see cref="LiveSymbolAssessmentService"/> for the no-position decision and for the
/// operator-declared holding decision on every uncached live assessment.
/// </summary>
public static class LiveDecisionProjection
{
    /// <summary>
    /// PURPOSE: Maps an engine state to the label wording of the installed 1M/5M Thinkorswim studies
    /// and a colour tone (wait = grey, prepare/waning = yellow, ready/runner = green, sell = red).
    /// EXECUTION READY requires <paramref name="entryAllowed"/>; a Buy state without it is shown as PREPARE TO BUY.
    /// WHEN CALLED: By <see cref="Project"/>, and directly by unit tests.
    /// </summary>
    public static (string Label, string Tone) ToStateLabel(SignalState state, bool entryAllowed) => state switch
    {
        SignalState.NoTrade => ("WAIT", "wait"),
        SignalState.PrepareBuy => ("PREPARE TO BUY", "prepare"),
        SignalState.Buy => entryAllowed ? ("EXECUTION READY", "ready") : ("PREPARE TO BUY", "prepare"),
        SignalState.Runner => ("RUNNER", "runner"),
        SignalState.RunnerWaning => ("RUNNER WANING", "waning"),
        SignalState.PrepareSell => ("PREPARE TO SELL", "waning"),
        SignalState.ConfirmedWeakness => ("SELL - CONFIRMED WEAKNESS", "sell"),
        SignalState.Breakdown => ("SELL - BREAKDOWN", "sell"),
        SignalState.Exit => ("GET OUT", "sell"),
        _ => ("UNKNOWN", "data")
    };

    /// <summary>
    /// PURPOSE: Builds the anonymous JSON object for one decision, or null when the engine was not evaluated
    /// (stale data, insufficient history, missing RVOL baseline or outside the regular session).
    /// WHEN CALLED: By <see cref="LiveSymbolAssessmentService"/> while building the assessment response.
    /// </summary>
    public static object? Project(SignalDecision? decision)
    {
        if (decision is null)
            return null;

        var (label, tone) = ToStateLabel(decision.State, decision.EntryAllowed);
        return new
        {
            state = decision.State.ToString(),
            stateLabel = label,
            stateTone = tone,
            action = decision.Action.ToString(),
            score = decision.Score,
            fiveMinutePermission = decision.LongPermission,
            fiveMinuteStructureBull = decision.Evaluation?.FiveMinuteStructureBull,
            oneMinuteStructureBull = decision.Evaluation?.OneMinuteStructureBull,
            oneMinuteMomentumPositive = decision.Evaluation?.OneMinuteMomentumPositive,
            oneMinuteExecution = decision.EntryAllowed ? "CONFIRMED" :
                decision.LongPermission ? "WAITING_FOR_1M_CONFIRMATION" : "BLOCKED_BY_5M_PERMISSION",
            entryAllowed = decision.EntryAllowed,
            runnerAllowed = decision.RunnerAllowed,
            exitWarning = decision.ExitWarning,
            hardExit = decision.HardExit,
            atrExtension = decision.Evaluation?.AtrExtension,
            reason = decision.Reason
        };
    }
}
