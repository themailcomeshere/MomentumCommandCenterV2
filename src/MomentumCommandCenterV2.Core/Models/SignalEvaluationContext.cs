namespace MomentumCommandCenterV2.Core.Models;

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