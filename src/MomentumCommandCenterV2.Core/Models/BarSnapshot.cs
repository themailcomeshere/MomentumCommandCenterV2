namespace MomentumCommandCenterV2.Core.Models;

// =========================================================
// MODEL: BarSnapshot
// PURPOSE:
//   Represents the market and indicator state for a single
//   1-minute or 5-minute bar.
//
// RESPONSIBILITIES:
//   - Store OHLCV data.
//   - Store calculated indicators.
//   - Expose basic structure and momentum relationships.
//
// DOES NOT:
//   - Calculate indicators.
//   - Make trading decisions.
//   - Access market-data providers.
//
// LAYER:
//   Core
// =========================================================
public sealed record BarSnapshot(
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    decimal Rvol5m,
    decimal Vwap,
    decimal Ema9,
    decimal Ema20,
    decimal Sma50,
    decimal Rsi,
    decimal Macd,
    decimal MacdSignal,
    decimal Atr,

    // Derived / optional context.
    decimal AtrExtension = 0m,
    bool Reclaim = false,
    bool Breakout = false,
    bool Retest = false,
    bool Failure = false)
{
    public bool AboveVwap =>
        Close > Vwap;

    public bool AboveEma9 =>
        Close > Ema9;

    public bool Ema9AboveEma20 =>
        Ema9 > Ema20;

    public bool Ema20AboveSma50 =>
        Ema20 > Sma50;

    public bool BullStructure =>
        AboveVwap &&
        AboveEma9 &&
        Ema9AboveEma20;

    public bool BearStructure =>
        Close < Vwap ||
        Close < Ema9 ||
        Ema9 < Ema20;

    public bool MomentumPositive =>
        Macd >= MacdSignal;

    public bool MomentumNegative =>
        Macd < MacdSignal;
}