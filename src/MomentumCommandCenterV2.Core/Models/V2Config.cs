namespace MomentumCommandCenterV2.Core.Models;

// =========================================================
// CONFIGURATION: V2Config
// PURPOSE:
//   Contains configurable thresholds used by the V2 signal
//   engine.
//
// RESPONSIBILITIES:
//   - Define RVOL thresholds.
//   - Define RSI thresholds.
//   - Define ATR-extension thresholds.
//   - Define EOD behavior.
//
// NOTE:
//   Thresholds are configuration, not optimization targets.
//   V2 validation must be based on historical evidence rather
//   than tuning against individual trades.
//
// LAYER:
//   Core
// =========================================================
public sealed record V2Config(
    decimal MinRvol = 1.50m,
    decimal MinRsi = 50m,
    decimal WeakRsi = 45m,
    decimal MaxRsiForFreshEntry = 78m,

    decimal AtrExtensionWarning = 1.50m,
    decimal AtrExtensionHardExit = 2.25m,

    int SoftDeteriorationBars = 2,

    int EndOfDayExitHourEt = 15,
    int EndOfDayExitMinuteEt = 55);