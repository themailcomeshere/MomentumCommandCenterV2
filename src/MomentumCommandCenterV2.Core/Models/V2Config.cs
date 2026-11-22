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
/// <summary>
/// CONFIGURATION: V2Config
/// PURPOSE: Defines thresholds used by the V2 signal engine.
/// WHEN CALLED: Supplied when constructing the engine or defaults are used for prototype evaluation.
/// </summary>
public sealed record V2Config(
    decimal MinRvol = 1.50m,
    decimal MinRsi = 50m,
    decimal WeakRsi = 45m,
    decimal MaxRsiForFreshEntry = 78m,

    decimal AtrExtensionWarning = 1.50m,
    decimal AtrExtensionHardExit = 2.25m,

    int SoftDeteriorationBars = 2,

    int EndOfDayExitHourEt = 15,
    int EndOfDayExitMinuteEt = 55,

    // 1M data coverage gate (operator decision 2026-11-18, after MWH): EXECUTION READY needs at least this share (percent)
    // of the last 15 regular-session minutes to have a 1-minute bar. Liquid names have 15/15 on IEX; thin names 4–8/15.
    int MinOneMinuteCoveragePercent = 80,

    // Opening entry hold (operator request 2026-11-21): minutes after the 09:30 ET open during which a fresh entry is
    // shown as PREPARE TO BUY (OPENING HOLD) instead of EXECUTION READY. 0 = off. Exits are never held. The engine
    // default is 0 so replays and tests are unchanged unless a caller passes the operator setting; the API passes the
    // operator's Dashboard setting (default MarketData:Intraday:OpeningEntryHoldMinutes = 15, operator decision
    // 2026-11-21 after the evidence review in docs/Research/Opening-Hold-Evidence-20261121.md).
    int OpeningEntryHoldMinutes = 0);