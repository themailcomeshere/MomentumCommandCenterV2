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
public sealed record CommandCenterSnapshot(
    DateTimeOffset Timestamp,
    string Symbol,
    BarSnapshot OneMinute,
    BarSnapshot FiveMinute,
    MarketPhase Phase,
    BarSnapshot? PreviousFiveMinute = null);