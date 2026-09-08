namespace MomentumCommandCenterV2.Core;

public enum MarketPhase { PreMarket, RegularSession, PostMarket }
public enum SignalState { NoTrade, Watch, Setup, EntryReady, Runner, Hold, Weakening, Exit }
public enum ExitReason { Manual, Target, StructureBreak, MomentumLoss, VwapLoss, TimeStop, EndOfDay, RiskStop, Unknown }
public sealed record BarSnapshot(DateTimeOffset Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume, decimal Rvol5m, decimal Vwap, decimal Ema9, decimal Ema20, decimal Sma50, decimal Rsi, decimal Macd, decimal MacdSignal, decimal Atr);
public sealed record CommandCenterSnapshot(DateTimeOffset Timestamp, string Symbol, BarSnapshot OneMinute, BarSnapshot FiveMinute, MarketPhase Phase);
public sealed record Trade(string Symbol, DateTimeOffset EntryTime, decimal EntryPrice, DateTimeOffset ExitTime, decimal ExitPrice, int Quantity, ExitReason ExitReason = ExitReason.Unknown) { public decimal Pnl => (ExitPrice-EntryPrice)*Quantity; public decimal PnlPercent => EntryPrice==0 ? 0 : (ExitPrice-EntryPrice)/EntryPrice*100m; public TimeSpan HoldTime => ExitTime-EntryTime; }
public sealed record SignalDecision(SignalState State, int Score, string Reason, bool LongPermission, bool EntryAllowed, bool RunnerAllowed, bool ExitWarning, bool HardExit);
public sealed record V2Config(decimal MinRvol=1.5m, decimal MinRsi=50m, decimal MaxRsiForFreshEntry=78m, decimal AtrExtensionWarning=1.50m, decimal AtrExtensionHardExit=2.25m, int EndOfDayExitHourEt=15, int EndOfDayExitMinuteEt=55);
