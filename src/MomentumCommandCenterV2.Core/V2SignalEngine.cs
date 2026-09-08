namespace MomentumCommandCenterV2.Core;
public sealed class V2SignalEngine
{
    private readonly V2Config _config;
    public V2SignalEngine(V2Config? config = null) => _config = config ?? new();
    public SignalDecision Evaluate(CommandCenterSnapshot snapshot, bool hasPosition)
    {
        var o = snapshot.OneMinute; var f = snapshot.FiveMinute;
        if (IsEndOfDay(snapshot.Timestamp)) return new(SignalState.Exit, 0, "EOD GET OUT window reached.", false, false, false, true, true);
        var score = 0; var reasons = new List<string>();
        if (f.Close > f.Vwap) { score++; reasons.Add("5M > VWAP"); }
        if (f.Close > f.Ema9) { score++; reasons.Add("5M > 9 EMA"); }
        if (f.Ema9 > f.Ema20) { score++; reasons.Add("5M 9 EMA > 20 EMA"); }
        if (f.Ema20 > f.Sma50) { score++; reasons.Add("5M 20 EMA > 50 SMA"); }
        if (f.Rsi >= _config.MinRsi) { score++; reasons.Add("5M RSI supportive"); }
        if (f.Rvol5m >= _config.MinRvol) { score++; reasons.Add("5M RVOL supportive"); }
        var permission = score >= 5;
        var oneBull = o.Close > o.Vwap && o.Close > o.Ema9 && o.Ema9 > o.Ema20;
        var momentum = o.Macd >= o.MacdSignal && o.Rsi >= _config.MinRsi;
        var extended = o.Atr > 0 && (o.Close - o.Ema9) / o.Atr >= _config.AtrExtensionWarning;
        var hardExtended = o.Atr > 0 && (o.Close - o.Ema9) / o.Atr >= _config.AtrExtensionHardExit;
        var entry = permission && oneBull && momentum && f.Rvol5m >= _config.MinRvol && o.Rsi < _config.MaxRsiForFreshEntry && !hardExtended;
        var structureLoss = o.Close < o.Vwap || o.Close < o.Ema9 || o.Ema9 < o.Ema20;
        var momentumLoss = o.Macd < o.MacdSignal || o.Rsi < 45m;
        var fiveFailure = f.Close < f.Vwap || f.Ema9 < f.Ema20;

        if (hasPosition)
        {
            if (hardExtended || fiveFailure) return new(SignalState.Exit, score, hardExtended ? "Position materially ATR-extended." : "5M trend structure failed.", permission, false, false, true, true);
            if (structureLoss && momentumLoss) return new(SignalState.Exit, score, "1M structure and momentum both deteriorated.", permission, false, false, true, true);
            if (structureLoss || momentumLoss || extended) return new(SignalState.Weakening, score, "Position is weakening; protect gains and do not add.", permission, false, true, true, false);
            if (permission && oneBull && momentum) return new(SignalState.Runner, score, "Trend intact; runner permitted.", true, false, true, false, false);
            return new(SignalState.Hold, score, "Position remains open but confirmation is mixed.", permission, false, false, false, false);
        }

        if (entry) return new(SignalState.EntryReady, score, "5M permission + 1M execution confirmation.", true, true, true, false, false);
        if (permission) return new(SignalState.Setup, score, "5M permission exists; 1M execution confirmation incomplete.", true, false, false, false, false);
        if (score >= 3) return new(SignalState.Watch, score, "Partial alignment; wait for confirmation.", false, false, false, false, false);
        return new(SignalState.NoTrade, score, "Insufficient directional alignment.", false, false, false, false, false);
    }
    private bool IsEndOfDay(DateTimeOffset t) => t.TimeOfDay >= new TimeSpan(_config.EndOfDayExitHourEt, _config.EndOfDayExitMinuteEt, 0);
}
