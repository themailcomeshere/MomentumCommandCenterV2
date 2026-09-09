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
    bool HardExtended);