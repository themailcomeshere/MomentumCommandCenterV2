using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core;

public sealed class V2SignalEngine
{
    private readonly V2Config _config;

    public V2SignalEngine(V2Config? config = null)
    {
        _config = config ?? new V2Config();
    }

    public SignalDecision Evaluate(
        CommandCenterSnapshot snapshot,
        bool hasPosition)
    {
        var one = snapshot.OneMinute;
        var five = snapshot.FiveMinute;
        var previousFive = snapshot.PreviousFiveMinute;

        // =========================================================
        // EOD
        // =========================================================
        if (IsEndOfDay(snapshot.Timestamp))
        {
            return Decision(
                SignalState.Exit,
                MomentumAction.SELL,
                0,
                "EOD GET OUT window reached.",
                false,
                false,
                false,
                true,
                true);
        }

        // =========================================================
        // 5M DIRECTION / PERMISSION
        // =========================================================

        var score = 0;
        var reasons = new List<string>();

        if (five.Close > five.Vwap)
        {
            score++;
            reasons.Add("5M > VWAP");
        }

        if (five.Close > five.Ema9)
        {
            score++;
            reasons.Add("5M > 9 EMA");
        }

        if (five.Ema9 > five.Ema20)
        {
            score++;
            reasons.Add("5M 9 EMA > 20 EMA");
        }

        if (five.Ema20 > five.Sma50)
        {
            score++;
            reasons.Add("5M 20 EMA > 50 SMA");
        }

        if (five.Rsi >= _config.MinRsi)
        {
            score++;
            reasons.Add("5M RSI supportive");
        }

        if (five.Rvol5m >= _config.MinRvol)
        {
            score++;
            reasons.Add("5M RVOL supportive");
        }

        var fivePermission = score >= 5;

        // =========================================================
        // 5M STRUCTURE
        // =========================================================

        var fiveStructureBull =
            five.BullStructure;

        var fiveStructureBroken =
            five.Close < five.Vwap &&
            five.Ema9 < five.Ema20;

        // =========================================================
        // MULTI-BAR SOFT DETERIORATION
        // =========================================================

        var softDeterioration =
            HasSoftDeterioration(
                five,
                previousFive);

        // =========================================================
        // 1M EXECUTION
        // =========================================================

        var oneStructureBull =
            one.Close > one.Vwap &&
            one.Close > one.Ema9 &&
            one.Ema9 > one.Ema20;

        var oneMomentumPositive =
            one.Macd >= one.MacdSignal &&
            one.Rsi >= _config.MinRsi;

        var oneMomentumWeak =
            one.Macd < one.MacdSignal ||
            one.Rsi < _config.WeakRsi;

        var oneStructureWeak =
            one.Close < one.Vwap ||
            one.Close < one.Ema9 ||
            one.Ema9 < one.Ema20;

        // =========================================================
        // ATR EXTENSION
        // =========================================================

        var atrExtension =
            GetAtrExtension(one);

        var extended =
            atrExtension >=
            _config.AtrExtensionWarning;

        var hardExtended =
            atrExtension >=
            _config.AtrExtensionHardExit;

        // =========================================================
        // FRESH ENTRY
        // =========================================================

        var freshBuy =
            fivePermission &&
            fiveStructureBull &&
            oneStructureBull &&
            oneMomentumPositive &&
            one.Rsi < _config.MaxRsiForFreshEntry &&
            !hardExtended;

        // =========================================================
        // POSITION MANAGEMENT
        // =========================================================

        if (hasPosition)
        {
            // -----------------------------------------------------
            // HIGHEST PRIORITY:
            // confirmed breakdown / hard failure
            // -----------------------------------------------------

            if (hardExtended)
            {
                return Decision(
                    SignalState.Breakdown,
                    MomentumAction.SELL,
                    score,
                    "Position materially ATR-extended.",
                    fivePermission,
                    false,
                    false,
                    true,
                    true);
            }

            if (fiveStructureBroken)
            {
                return Decision(
                    SignalState.Breakdown,
                    MomentumAction.SELL,
                    score,
                    "5M trend structure failed.",
                    false,
                    false,
                    false,
                    true,
                    true);
            }

            // -----------------------------------------------------
            // CONFIRMED WEAKNESS
            // -----------------------------------------------------

            if (softDeterioration &&
                oneStructureWeak &&
                oneMomentumWeak)
            {
                return Decision(
                    SignalState.ConfirmedWeakness,
                    MomentumAction.SELL,
                    score,
                    "5M soft deterioration confirmed by 1M structure and momentum.",
                    fivePermission,
                    false,
                    false,
                    true,
                    true);
            }

            // -----------------------------------------------------
            // RUNNER WANING
            //
            // A runner is still fundamentally healthy, but the
            // current 1M condition is becoming extended or fragile.
            //
            // IMPORTANT:
            // RUNNER WANING is NEVER a SELL.
            // -----------------------------------------------------

            var runnerWaning =
                fivePermission &&
                fiveStructureBull &&
                (
                    extended ||
                    (oneStructureWeak && !oneMomentumWeak) ||
                    (!oneStructureWeak && oneMomentumWeak)
                );

            if (runnerWaning)
            {
                return Decision(
                    SignalState.RunnerWaning,
                    MomentumAction.HOLD,
                    score,
                    BuildReason(
                        "Runner waning; trend remains intact.",
                        reasons),
                    fivePermission,
                    false,
                    true,
                    true,
                    false);
            }

            // -----------------------------------------------------
            // PREPARE SELL
            //
            // Conditions are deteriorating, but not enough to
            // justify a confirmed weakness / breakdown.
            // -----------------------------------------------------

            if (softDeterioration ||
                oneStructureWeak ||
                oneMomentumWeak)
            {
                return Decision(
                    SignalState.PrepareSell,
                    MomentumAction.PREPARE_SELL,
                    score,
                    BuildReason(
                        "Conditions weakening; prepare to protect gains.",
                        reasons),
                    fivePermission,
                    false,
                    false,
                    true,
                    false);
            }

            // -----------------------------------------------------
            // HEALTHY RUNNER
            // -----------------------------------------------------

            if (fivePermission &&
                fiveStructureBull &&
                oneStructureBull &&
                oneMomentumPositive)
            {
                return Decision(
                    SignalState.Runner,
                    MomentumAction.HOLD,
                    score,
                    "Trend intact; runner permitted.",
                    true,
                    false,
                    true,
                    false,
                    false);
            }

            // -----------------------------------------------------
            // INTACT TREND
            // -----------------------------------------------------

            if (fivePermission &&
                fiveStructureBull)
            {
                return Decision(
                    SignalState.Runner,
                    MomentumAction.HOLD,
                    score,
                    "5M trend remains intact.",
                    true,
                    false,
                    true,
                    false,
                    false);
            }

            // -----------------------------------------------------
            // MIXED POSITION
            // -----------------------------------------------------

            return Decision(
                SignalState.PrepareSell,
                MomentumAction.HOLD,
                score,
                "Position remains open but confirmation is mixed.",
                fivePermission,
                false,
                false,
                false,
                false);
        }

        // =========================================================
        // NO POSITION
        // =========================================================

        // BUY is entry-only.
        if (freshBuy)
        {
            return Decision(
                SignalState.Buy,
                MomentumAction.BUY,
                score,
                BuildReason(
                    "5M permission + 1M execution confirmation.",
                    reasons),
                true,
                true,
                false,
                false,
                false);
        }

        // 5M is good, but 1M has not confirmed execution.
        if (fivePermission &&
            fiveStructureBull)
        {
            return Decision(
                SignalState.PrepareBuy,
                MomentumAction.PREPARE_BUY,
                score,
                BuildReason(
                    "5M permission exists; wait for 1M execution confirmation.",
                    reasons),
                true,
                false,
                false,
                false,
                false);
        }

        // Partial alignment.
        if (score >= 3)
        {
            return Decision(
                SignalState.NoTrade,
                MomentumAction.NONE,
                score,
                BuildReason(
                    "Partial alignment; wait for confirmation.",
                    reasons),
                false,
                false,
                false,
                false,
                false);
        }

        return Decision(
            SignalState.NoTrade,
            MomentumAction.NONE,
            score,
            BuildReason(
                "Insufficient directional alignment.",
                reasons),
            false,
            false,
            false,
            false,
            false);
    }

    private bool HasSoftDeterioration(
        BarSnapshot current,
        BarSnapshot? previous)
    {
        if (previous is null)
        {
            return false;
        }

        var currentWeak =
            current.Close < current.Vwap ||
            current.Close < current.Ema9 ||
            current.Ema9 < current.Ema20 ||
            current.Rsi < _config.MinRsi ||
            current.Macd < current.MacdSignal;

        var previousWeak =
            previous.Close < previous.Vwap ||
            previous.Close < previous.Ema9 ||
            previous.Ema9 < previous.Ema20 ||
            previous.Rsi < _config.MinRsi ||
            previous.Macd < previous.MacdSignal;

        return currentWeak &&
               previousWeak;
    }

    private static decimal GetAtrExtension(
        BarSnapshot bar)
    {
        if (bar.AtrExtension > 0)
        {
            return bar.AtrExtension;
        }

        if (bar.Atr <= 0)
        {
            return 0;
        }

        return Math.Max(
            0,
            (bar.Close - bar.Ema9) / bar.Atr);
    }

    private static string BuildReason(
        string primary,
        IEnumerable<string> reasons)
    {
        var supporting =
            string.Join(
                ", ",
                reasons.Take(3));

        return string.IsNullOrWhiteSpace(supporting)
            ? primary
            : $"{primary} {supporting}.";
    }

    private static SignalDecision Decision(
        SignalState state,
        MomentumAction action,
        int score,
        string reason,
        bool longPermission,
        bool entryAllowed,
        bool runnerAllowed,
        bool exitWarning,
        bool hardExit)
    {
        return new SignalDecision(
            state,
            action,
            score,
            reason,
            longPermission,
            entryAllowed,
            runnerAllowed,
            exitWarning,
            hardExit);
    }

    private bool IsEndOfDay(
        DateTimeOffset timestamp)
    {
        var easternTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "Eastern Standard Time"
                    : "America/New_York");

        var eastern =
            TimeZoneInfo.ConvertTime(
                timestamp,
                easternTimeZone);

        return eastern.TimeOfDay >=
               new TimeSpan(
                   _config.EndOfDayExitHourEt,
                   _config.EndOfDayExitMinuteEt,
                   0);
    }
}