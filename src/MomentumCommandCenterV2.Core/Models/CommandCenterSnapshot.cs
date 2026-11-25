using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models;

// =========================================================
// MODEL: CommandCenterSnapshot
// PURPOSE:
//   Represents the complete 1M/5M market state supplied to
//   the V2 signal engine at a specific point in time.
//
// RESPONSIBILITIES:
//   - Hold the current 1M snapshot.
//   - Hold the current 5M snapshot.
//   - Hold market phase information.
//   - Optionally provide the previous 5M snapshot.
//
// DOES NOT:
//   - Calculate indicators.
//   - Generate the trading decision.
//
// LAYER:
//   Core
// =========================================================
/// <summary>
/// TYPE: CommandCenterSnapshot
/// PURPOSE: Defines the record contract or data structure named CommandCenterSnapshot.
/// WHEN CALLED: Used by the application workflow whenever this contract or type is referenced.
/// </summary>
/// <summary>
/// METHOD: CommandCenterSnapshot
/// PURPOSE: Implements CommandCenterSnapshot for this component. FiveMinutePermission, when supplied, is the latched
/// 5M permission from <see cref="FiveMinutePermissionLatch"/> (installed TOS study) and is the entry gate; when null
/// the engine falls back to the legacy stateless "score >= 5 of 6" rule. ExecutionWindow, when supplied, is the latest
/// 1M bar's step from <see cref="OneMinuteExecutionEvents"/> (1M study green lane); a fresh entry is EXECUTION READY
/// only when that step is Ready (operator decision 2026-11-25: READY is a 2-bar event). Null = not gated (tests and
/// callers without 1M history).
/// WHEN CALLED: Called by the workflow or caller that requests CommandCenterSnapshot.
/// </summary>
public sealed record CommandCenterSnapshot(
    DateTimeOffset Timestamp,
    string Symbol,
    BarSnapshot OneMinute,
    BarSnapshot FiveMinute,
    MarketPhase Phase,
    BarSnapshot? PreviousFiveMinute = null,
    bool? FiveMinutePermission = null,
    int? OneMinuteBarsPresent = null,
    int? OneMinuteBarsPossible = null,
    ExecutionEventStep? ExecutionWindow = null);