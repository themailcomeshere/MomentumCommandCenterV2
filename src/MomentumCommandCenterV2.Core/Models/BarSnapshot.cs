namespace MomentumCommandCenterV2.Core.Models
{
    public sealed record BarSnapshot(DateTimeOffset Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume, decimal Rvol5m, decimal Vwap, decimal Ema9, decimal Ema20, decimal Sma50, decimal Rsi, decimal Macd, decimal MacdSignal, decimal Atr);
}
