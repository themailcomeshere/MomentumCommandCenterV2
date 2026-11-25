using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core;

// =========================================================
// CLASS: V2SignalEngine
// PURPOSE:
//   Converts a CommandCenterSnapshot into a V2 trading
//   decision.
//
// RESPONSIBILITIES:
//   - Evaluate 5M directional permission.
//   - Evaluate 1M execution confirmation.
//   - Manage open-position lifecycle states.
//   - Detect runner, weakening, breakdown, and EOD conditions.
//
// DOES NOT:
//   - Import broker data.
//   - Access Schwab.
//   - Place orders.
//
// IMPORTANT:
//   Fresh-entry logic and open-position management are
//   intentionally separated.
//
// LAYER:
//   Core
// =========================================================
/// <summary>
/// CLASS: V2SignalEngine
/// PURPOSE: Evaluates timestamped market snapshots for 5M permission, 1M entry confirmation, and open-position management.
/// WHEN CALLED: Called by a decision or replay workflow when a current market snapshot and position state are available.
/// </summary>
public sealed class V2SignalEngine
{
    private readonly V2Config _config;

    /// <summary>
    /// METHOD: V2SignalEngine
    /// PURPOSE: Initializes the signal engine with explicit or default rule thresholds.
    /// WHEN CALLED: Called when the application or a test creates an engine for signal evaluation.
    /// </summary>
    public V2SignalEngine(V2Config? config = null)
    {
        _config = config ?? new V2Config();
        if (!OpeningEntryHold.IsValidMinutes(_config.OpeningEntryHoldMinutes))
            throw new ArgumentOutOfRangeException(nameof(config), _config.OpeningEntryHoldMinutes,
                $"OpeningEntryHoldMinutes must be 0–{OpeningEntryHold.MaxMinutes}.");
    }

    /// <summary>
    /// PURPOSE: True when the snapshot reports 1-minute coverage below the configured share of possible minutes
    /// (present * 100 &lt; possible * MinOneMinuteCoveragePercent). text = "8 of 15 minutes".
    /// WHEN CALLED: By Evaluate before returning an entry (Buy) decision.
    /// </summary>
    public static bool OneMinuteCoverageThin(CommandCenterSnapshot snapshot, V2Config config, out string text)
    {
        text = "";
        if (snapshot.OneMinuteBarsPresent is not int present || snapshot.OneMinuteBarsPossible is not int possible || possible <= 0)
            return false;
        text = $"{present} of {possible} minutes";
        return present * 100 < possible * config.MinOneMinuteCoveragePercent;
    }

    /// <summary>
    /// PURPOSE: Calculate the independent six-condition 5M authority score.
    /// WHEN CALLED: Live assessment and decision evaluation need the same V2 score without 1M execution dependencies.
    /// IMPORTANT: Uses only completed 5M indicator inputs; freshness/history checks remain the caller's responsibility.
    /// </summary>
    public static int CalculateFiveMinuteScore(BarSnapshot five, V2Config? config = null)
    {
        ArgumentNullException.ThrowIfNull(five);
        var rules = config ?? new V2Config();
        var score = 0;
        if (five.Close > five.Vwap) score++;
        if (five.Close > five.Ema9) score++;
        if (five.Ema9 > five.Ema20) score++;
        if (five.Ema20 > five.Sma50) score++;
        if (five.Rsi >= rules.MinRsi) score++;
        if (five.Rvol5m >= rules.MinRvol) score++;
        return score;
    }

    /// <summary>
    /// METHOD: Evaluate
    /// PURPOSE: Evaluates the supplied snapshot and returns the deterministic signal decision.
    /// WHEN CALLED: Called by the signal workflow and historical trade/replay analysis.
    /// </summary>
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

        var score = CalculateFiveMinuteScore(five, _config);
        var reasons = new List<string>();

        if (five.Close > five.Vwap)
        {
            reasons.Add("5M > VWAP");
        }

        if (five.Close > five.Ema9)
        {
            reasons.Add("5M > 9 EMA");
        }

        if (five.Ema9 > five.Ema20)
        {
            reasons.Add("5M 9 EMA > 20 EMA");
        }

        if (five.Ema20 > five.Sma50)
        {
            reasons.Add("5M 20 EMA > 50 SMA");
        }

        if (five.Rsi >= _config.MinRsi)
        {
            reasons.Add("5M RSI supportive");
        }

        if (five.Rvol5m >= _config.MinRvol)
        {
            reasons.Add("5M RVOL supportive");
        }

        // 2026-11-17 (operator decision): the installed TOS study's latched 5M permission is the gate when the caller
        // supplies it; the stateless score >= 5 rule remains only as the fallback for callers without bar history.
        var fivePermission = snapshot.FiveMinutePermission ?? score >= 5;
        if (snapshot.FiveMinutePermission.HasValue)
        {
            reasons.Insert(0, fivePermission ? "5M permission YES (TOS latch)" : "5M permission NO (TOS latch)");
        }

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

        var evaluation = new SignalEvaluationContext(
            FiveMinuteScore: score,
            FiveMinutePermission: fivePermission,
            FiveMinuteStructureBull: fiveStructureBull,
            FiveMinuteStructureBroken: fiveStructureBroken,
            SoftDeterioration: softDeterioration,
            OneMinuteStructureBull: oneStructureBull,
            OneMinuteStructureWeak: oneStructureWeak,
            OneMinuteMomentumPositive: oneMomentumPositive,
            OneMinuteMomentumWeak: oneMomentumWeak,
            AtrExtension: atrExtension,
            Extended: extended,
            HardExtended: hardExtended);

        // Entry extension block (operator decision 2026-11-25): the 1M study's 2.00 ATR, separate from the 2.25 held exit.
        var entryExtended =
            atrExtension >=
            _config.AtrExtensionEntryBlock;

        // =========================================================
        // FRESH ENTRY
        // =========================================================

        // MCC's original state check. Used as the entry rule only when no 1M study window is supplied (tests, callers
        // without 1M history), and otherwise only to explain a PREPARE TO BUY.
        var legacyEntry =
            fivePermission &&
            fiveStructureBull &&
            oneStructureBull &&
            oneMomentumPositive &&
            one.Rsi < _config.MaxRsiForFreshEntry;

        // Operator decision 2026-11-25 (decision 4, "match the study"): with the 1M study window supplied, a fresh entry is
        // the study's EXECUTION READY on the 5M latch. MCC's extra checks (current 5M bar structure, 1M close > EMA9 and
        // EMA9 > EMA20, 1M MACD/RSI >= 50, RSI < 78) no longer gate entries. MCC's own safety gates below still apply
        // (opening hold, 1M data coverage, EOD); held-position management is unchanged.
        var window = snapshot.ExecutionWindow;
        var freshBuyExceptExtension = window is null
            ? legacyEntry
            : fivePermission && window.Ready;

        var freshBuy =
            freshBuyExceptExtension &&
            !entryExtended &&
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
                    true,
                    evaluation);
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
                    true,
                    evaluation);
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
                    true,
                    evaluation);
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
                    false,
                    evaluation);
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
                    false,
                    evaluation);
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
                    false,
                    evaluation);
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
                    false,
                    evaluation);
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
                false, evaluation);
        }

        // =========================================================
        // NO POSITION
        // =========================================================

        // Opening entry hold (operator request 2026-11-21): inside the first N minutes after the 09:30 ET open a fresh
        // entry is withheld (PREPARE TO BUY) with an OPENING HOLD reason. The underlying conditions are kept in the
        // evaluation context (OpeningHoldWithheld = true) so held entries can be studied later. Exits never wait.
        if (freshBuy && _config.OpeningEntryHoldMinutes > 0 &&
            OpeningEntryHold.Evaluate(snapshot.Timestamp, _config.OpeningEntryHoldMinutes) is { Active: true } hold)
        {
            return Decision(
                SignalState.PrepareBuy,
                MomentumAction.PREPARE_BUY,
                score,
                BuildReason(
                    $"OPENING HOLD: entry conditions met inside the first {hold.Minutes} min after the 09:30 ET open; EXECUTION READY withheld until {OpeningEntryHold.FormatEasternAndArizona(hold.ReleaseUtc)}.",
                    reasons),
                true,
                false,
                false,
                false,
                false,
                evaluation with { OpeningHoldWithheld = true });
        }

        // 1M data coverage gate (operator decision 2026-11-18, MWH): too few 1-minute bars in the last 15 minutes means
        // the 1M EMAs/RSI/momentum rest on a handful of prints, so EXECUTION READY is withheld (PREPARE TO BUY instead).
        // Exits never wait for coverage. Unknown coverage (callers that do not supply it) does not block.
        if (freshBuy && OneMinuteCoverageThin(snapshot, _config, out var coverageText))
        {
            return Decision(
                SignalState.PrepareBuy,
                MomentumAction.PREPARE_BUY,
                score,
                BuildReason(
                    $"Entry conditions met, but 1M coverage is thin ({coverageText}); EXECUTION READY withheld. Check TOS 1M.",
                    reasons),
                true,
                false,
                false,
                false,
                false, evaluation);
        }

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

        // Every other entry condition passes, but the 1M close is at or beyond the 2.00 ATR entry block (with a study
        // window, the window itself also refuses it; the trend checks then explain the PREPARE TO BUY).
        if ((freshBuyExceptExtension || (window is { Ready: false } && legacyEntry)) && (entryExtended || hardExtended))
        {
            return Decision(
                SignalState.PrepareBuy,
                MomentumAction.PREPARE_BUY,
                score,
                BuildReason(
                    $"Entry conditions met, but 1M is {atrExtension:0.00} ATR from VWAP (entry block {_config.AtrExtensionEntryBlock:0.00}); EXECUTION READY withheld.",
                    reasons),
                true,
                false,
                false,
                false,
                false,
                evaluation with { EntryExtended = true });
        }

        // 1M execution window (operator decision 2026-11-25): EXECUTION READY is a 2-bar event, as in the 1M study. MCC's
        // trend checks pass but the study has no READY window open on this bar (no new trigger in the last 2 bars, or the
        // study's own READY conditions fail): PREPARE TO BUY · WAIT FOR 1M TRIGGER on the Dashboard.
        if (window is { Ready: false } && legacyEntry)
        {
            return Decision(
                SignalState.PrepareBuy,
                MomentumAction.PREPARE_BUY,
                score,
                BuildReason(
                    $"Entry conditions met, but no 1M execution window: {DescribeWindow(window)}; EXECUTION READY needs a new 1M trigger (2-bar window).",
                    reasons),
                true,
                false,
                false,
                false,
                false,
                evaluation with { ExecutionWindowWithheld = true });
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
                false, evaluation);
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
                false, evaluation);
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
            false, evaluation);
    }

    /// <summary>
    /// METHOD: DescribeWindow
    /// PURPOSE: Short text for why the 1M study's green lane is not READY on this bar ("window expired 2 bars after an
    /// IN_TREND trigger", "no 1M trigger", "study READY conditions fail").
    /// WHEN CALLED: By Evaluate when the execution window withholds an entry.
    /// </summary>
    private static string DescribeWindow(ExecutionEventStep step)
    {
        if (step.WindowActive) return "study READY conditions fail (5M execution permission, EMA20, VWAP, RSI, MACD or extension)";
        if (!step.Candidate) return "no 1M execution candidate (EMA20 not rising, below EMA20, or MACD/RSI)";
        return "no new 1M trigger, or the 2-bar window expired";
    }

    /// <summary>
    /// METHOD: HasSoftDeterioration
    /// PURPOSE: Detects soft deterioration that persists across the current and previous 5-minute bars.
    /// WHEN CALLED: Called by Evaluate while assembling the signal-evaluation context and position-management decision.
    /// </summary>
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

    /// <summary>
    /// METHOD: GetAtrExtension
    /// PURPOSE: Uses the supplied ATR extension when present, otherwise derives absolute close-to-session-VWAP distance in ATR units.
    /// WHEN CALLED: Called by Evaluate before extension warning and hard-exit gates are applied.
    /// </summary>
    public static decimal CalculateAtrExtension(decimal close, decimal dailyVwap, decimal atr)
    {
        return atr > 0m ? Math.Abs(close - dailyVwap) / atr : 0m;
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

// Match the installed Thinkorswim 1M extension reference.
        return CalculateAtrExtension(bar.Close, bar.Vwap, bar.Atr);
    }

    /// <summary>
    /// METHOD: BuildReason
    /// PURPOSE: Combines the primary decision explanation with up to three supporting condition reasons.
    /// WHEN CALLED: Called by Evaluate when constructing a decision that benefits from concise supporting evidence.
    /// </summary>
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

    /// <summary>
    /// METHOD: Decision
    /// PURPOSE: Creates a SignalDecision while consistently preserving state, action, permission, lifecycle flags, and evaluation context.
    /// WHEN CALLED: Called by Evaluate on each terminal signal-state branch.
    /// </summary>
    private static SignalDecision Decision(
        SignalState state,
        MomentumAction action,
        int score,
        string reason,
        bool longPermission,
        bool entryAllowed,
        bool runnerAllowed,
        bool exitWarning,
        bool hardExit,
        SignalEvaluationContext? evaluation = null)
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
            hardExit,
            evaluation);
    }

    /// <summary>
    /// METHOD: IsEndOfDay
    /// PURPOSE: Converts the input timestamp to Eastern Time and compares it with the configured end-of-day exit threshold.
    /// WHEN CALLED: Called at the start of Evaluate to enforce the configured EOD exit window.
    /// </summary>
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