namespace MomentumCommandCenterV2.Core.Models;

// =========================================================
// MODEL: SignalEvaluationContext
// PURPOSE:
//   Preserves the underlying 1M/5M conditions that produced
//   a V2 signal decision.
//
// RESPONSIBILITIES:
//   - Capture 5M permission and structure.
//   - Capture 1M structure and momentum.
//   - Capture deterioration conditions.
//   - Capture ATR extension state.
//
// PURPOSE IN VALIDATION:
//   Allows later analysis to determine not only what decision
//   V2 made, but why it made that decision.
//
// LAYER:
//   Core
// =========================================================
/// <summary>
/// MODEL: SignalEvaluationContext
/// PURPOSE: Preserves the technical conditions and ATR extension used by one signal evaluation. OpeningHoldWithheld is
/// true when every fresh-entry condition passed but the opening entry hold turned EXECUTION READY into PREPARE TO BUY,
/// so later analysis can compare held entries against what happened after the hold. ExecutionWindowWithheld is true when
/// every MCC fresh-entry condition passed but the 1M study's green lane had no READY window open on this bar (no new
/// trigger in the last 2 bars, or the study's own READY conditions failed).
/// WHEN CALLED: Constructed by V2SignalEngine and consumed by diagnostics and historical validation.
/// </summary>
public sealed record SignalEvaluationContext(
    int FiveMinuteScore,
    bool FiveMinutePermission,
    bool FiveMinuteStructureBull,
    bool FiveMinuteStructureBroken,
    bool SoftDeterioration,
    bool OneMinuteStructureBull,
    bool OneMinuteStructureWeak,
    bool OneMinuteMomentumPositive,
    bool OneMinuteMomentumWeak,
    decimal AtrExtension,
    bool Extended,
    bool HardExtended,
    bool OpeningHoldWithheld = false,
    bool ExecutionWindowWithheld = false);