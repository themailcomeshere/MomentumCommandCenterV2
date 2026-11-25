namespace MomentumCommandCenterV2.Core;

/// <summary>
/// TYPE: FiveMinuteStudyBar
/// PURPOSE: One completed 5M bar's indicator values as the 1M study's 5M layer reads them (close, EMA9, EMA20, SMA50,
/// RSI14, MACD 12/26/9). Used to build the 5M execution context for each 1M bar.
/// WHEN CALLED: Built by the API's technical calculator from completed 5m bars; consumed by FiveMinuteExecutionContext.
/// </summary>
public sealed record FiveMinuteStudyBar(
    DateTimeOffset Timestamp,
    decimal Close,
    decimal Ema9,
    decimal Ema20,
    decimal Sma50,
    decimal Rsi,
    decimal Macd,
    decimal MacdSignal)
{
    /// <summary>PURPOSE: MACD histogram (MACD − signal). WHEN CALLED: By the 5M weakening / falling checks.</summary>
    public decimal MacdHist => Macd - MacdSignal;
}

/// <summary>
/// TYPE: FiveMinuteExecutionContext
/// PURPOSE: The three 5M inputs the 1M study's green lane reads on every 1M bar (V14.4.13): fiveExecutionPermission,
/// fiveSwingZoneRaw and fiveRunnerStructure. Permission is the 5M study latch (operator decision 2026-11-25).
/// WHEN CALLED: Built per 1M bar by FiveMinuteExecutionContext.Evaluate and carried on ExecutionEventBar.
/// </summary>
public sealed record FiveMinuteExecutionContext(
    bool ExecutionPermission,
    bool SwingZone,
    bool RunnerStructure)
{
    /// <summary>
    /// PURPOSE: Port of the 1M study's 5M layer on completed 5M bars. bars[0] is the latest completed 5M bar, bars[1..3]
    /// the three before it (fewer = not enough history, everything false). Definitions follow docs/Charts/1M/MainStudy.txt:
    ///   majorFailurePersistent = 2 bars of close &lt; EMA9 &lt; EMA20 &lt; SMA50;
    ///   confirmedWeakness      = close &lt; EMA9 &lt; EMA20 and (RSI &lt; rsiWeak or MACD &lt; signal);
    ///   executionPermission    = latch and 1M price above Daily VWAP and no major failure and no confirmed weakness;
    ///   runnerStructure        = latch and close &gt; EMA9 &gt; EMA20 &gt; SMA50 and 1M price above VWAP;
    ///   swingZone              = swing structure intact and runner waning on 2 bars and swing momentum weakening.
    /// The study reads the developing 5M bar; MCC uses completed 5M bars only (no look-ahead, same as the latch).
    /// WHEN CALLED: By the API once per 1M bar while building the execution-event series.
    /// </summary>
    public static FiveMinuteExecutionContext Evaluate(
        IReadOnlyList<FiveMinuteStudyBar> latestFirst,
        bool latchPermission,
        bool oneMinuteAboveVwap,
        decimal rsiStrong = 55m,
        decimal rsiWeak = 45m)
    {
        ArgumentNullException.ThrowIfNull(latestFirst);
        if (latestFirst.Count < 4) return new FiveMinuteExecutionContext(false, false, false);
        FiveMinuteStudyBar B(int o) => latestFirst[o];

        bool Major(int o) => B(o).Close < B(o).Ema9 && B(o).Ema9 < B(o).Ema20 && B(o).Ema20 < B(o).Sma50;
        var majorPersistent = Major(0) && Major(1);
        var c = B(0);
        var confirmedWeakness = c.Close < c.Ema9 && c.Ema9 < c.Ema20 && (c.Rsi < rsiWeak || c.Macd < c.MacdSignal);
        var bullStack = c.Close > c.Ema9 && c.Ema9 > c.Ema20 && c.Ema20 > c.Sma50;

        bool RsiWeakening(int o) => B(o).Rsi < B(o + 1).Rsi && B(o + 1).Rsi <= B(o + 2).Rsi;
        bool MacdWeakening(int o) => B(o).MacdHist < B(o + 1).MacdHist && B(o + 1).MacdHist < B(o + 2).MacdHist;
        bool WaningRaw(int o)
        {
            var context = latchPermission && oneMinuteAboveVwap && B(o).Ema9 > B(o).Ema20 && !majorPersistent;
            return context && (
                RsiWeakening(o) ||
                MacdWeakening(o) ||
                (B(o).Close < B(o).Ema9 && (B(o).Rsi < rsiStrong || B(o).MacdHist < B(o + 1).MacdHist)) ||
                (B(o).Close < B(o + 1).Close && !(B(o).Ema9 > B(o + 1).Ema9) && B(o).Rsi < B(o + 1).Rsi));
        }
        var waningPersistent = WaningRaw(0) && WaningRaw(1);
        var swingIntact = latchPermission && c.Close > c.Ema20 && c.Ema20 >= c.Sma50 && oneMinuteAboveVwap;
        var swingMomentumWeakening = (RsiWeakening(0) && MacdWeakening(0)) || (c.Rsi < rsiStrong && c.MacdHist < B(1).MacdHist);
        var swingZone = swingIntact && waningPersistent && swingMomentumWeakening && !majorPersistent;

        return new FiveMinuteExecutionContext(
            latchPermission && oneMinuteAboveVwap && !majorPersistent && !confirmedWeakness,
            swingZone,
            latchPermission && bullStack && oneMinuteAboveVwap);
    }
}

/// <summary>
/// TYPE: ExecutionEventBar
/// PURPOSE: One completed 1M bar with the indicator values and 5M context the 1M study's green execution lane reads.
/// Vwap is MCC's regular-session VWAP; AtrExtension = |close − VWAP| / SMA(TrueRange, 14) (the study formula).
/// WHEN CALLED: Built by the API's technical calculator; consumed by OneMinuteExecutionEvents.Evaluate.
/// </summary>
public sealed record ExecutionEventBar(
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Vwap,
    decimal Ema9,
    decimal Ema20,
    decimal Rsi,
    decimal Macd,
    decimal MacdSignal,
    decimal Rvol,
    decimal AtrExtension,
    FiveMinuteExecutionContext Five,
    bool EodWindow);

/// <summary>
/// TYPE: ExecutionEventConfig
/// PURPOSE: Inputs of the 1M study's green lane (V14.4.13 defaults). ExtensionDangerAtr is the study's 2.00; MCC passes
/// V2Config.AtrExtensionEntryBlock, also 2.00 (operator decision 2026-11-25).
/// WHEN CALLED: Passed to OneMinuteExecutionEvents.Evaluate.
/// </summary>
public sealed record ExecutionEventConfig(
    int BreakoutLength = 10,
    int ExecutionWindowBars = 2,
    int CandidateRearmBars = 2,
    int ContinuationLookbackBars = 4,
    int ContinuationPauseBarsRequired = 2,
    decimal ContinuationMinimumRsi = 50m,
    int ContinuationCooldownBars = 3,
    decimal MinimumExecutionRsi = 50m,
    decimal MinimumExecutionRvol = 1.00m,
    decimal RsiStrong = 55m,
    decimal RsiWeak = 45m,
    decimal ExtensionDangerAtr = 2.00m);

/// <summary>
/// TYPE: ExecutionEventStep
/// PURPOSE: The green-lane result for one 1M bar. Trigger lists the events that fired on this bar (FRESH, IN_TREND,
/// CONTINUATION, CONFIRMATION). Age = bars since the last new trigger (0 = trigger bar, -1 = no open window).
/// Ready = the study's EXECUTION READY before the opening hold (window open, 5M execution permission, not extended,
/// not invalidated).
/// WHEN CALLED: Returned by OneMinuteExecutionEvents.Evaluate; the latest step gates V2 EXECUTION READY.
/// </summary>
public sealed record ExecutionEventStep(
    DateTimeOffset Timestamp,
    bool Candidate,
    string Trigger,
    int Age,
    bool WindowActive,
    bool Invalidated,
    bool Ready);

/// <summary>
/// TYPE: OneMinuteExecutionEvents
/// PURPOSE: Port of the 1M Main Study V14.4.13 green execution lane (docs/Charts/1M/MainStudy.txt, "FRESH 1M TRIGGERS"
/// through "EXECUTION READY"): an EXECUTION READY needs a new trigger event (fresh breakout/reclaim/retest, in-trend
/// candidate, continuation after a pause, or a confirmation candle), with rearm and cooldown, and lasts at most
/// ExecutionWindowBars bars. Operator decision 2026-11-25: MCC READY is a 2-bar event, using the study's triggers.
/// The AGG. BUY lane, labels and bubbles are not ported.
/// WHEN CALLED: By the live assessment and the historical replay over every completed 1M bar in the window.
/// </summary>
public static class OneMinuteExecutionEvents
{
    public const string RulesVersion = "TOS-1M-EXECUTION-WINDOW-1 (1M study V14.4.13 green lane)";

    /// <summary>
    /// PURPOSE: Runs the green-lane state machine over the bars in time order and returns one step per bar (the first
    /// BreakoutLength bars only warm up and return Ready = false). State carries across dates like the study on a
    /// multi-day chart; the continuation arm resets in the EOD window.
    /// WHEN CALLED: Once per assessment/replay with the full 1M history (3 days in MCC).
    /// </summary>
    public static IReadOnlyList<ExecutionEventStep> Evaluate(IReadOnlyList<ExecutionEventBar> bars, ExecutionEventConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(bars);
        var p = config ?? new ExecutionEventConfig();
        var steps = new List<ExecutionEventStep>(bars.Count);

        var contArmed = false;
        var contCooldownAge = p.ContinuationCooldownBars;
        var pauseCount = 0;
        var everActive = false;
        var age = -1;
        bool prevFresh = false, prevInTrend = false, prevContQual = false, prevConfRaw = false, prevOpp = false, prevRearm = true;
        var start = Math.Max(p.BreakoutLength, p.ContinuationLookbackBars);

        for (var i = 0; i < bars.Count; i++)
        {
            var b = bars[i];
            if (i < start)
            {
                steps.Add(new ExecutionEventStep(b.Timestamp, false, "", -1, false, true, false));
                continue;
            }
            var pb = bars[i - 1];
            var price = b.Close;
            var aboveVwap = price > b.Vwap;
            var hist = b.Macd - b.MacdSignal;
            var prevHist = pb.Macd - pb.MacdSignal;
            var macdBullish = b.Macd > b.MacdSignal;
            var macdBearish = b.Macd < b.MacdSignal;
            var rsiRising = b.Rsi > pb.Rsi;
            var macdRising = hist > prevHist;
            var ema20Rising = b.Ema20 > pb.Ema20;
            var ema9Rising = b.Ema9 > pb.Ema9;
            var rvolHealthy = b.Rvol >= 1.00m;
            var bullishExtensionDanger = aboveVwap && b.AtrExtension >= p.ExtensionDangerAtr;

            decimal priorHigh = decimal.MinValue;
            for (var j = i - p.BreakoutLength; j < i; j++) priorHigh = Math.Max(priorHigh, bars[j].High);
            var freshTrigger =
                price > priorHigh ||
                (price > b.Vwap && pb.Close <= pb.Vwap) ||
                (price > b.Ema9 && pb.Close <= pb.Ema9) ||
                (b.Low <= b.Ema9 && price > b.Ema9 && price > b.Open) ||
                (b.Low <= b.Vwap && price > b.Vwap && price > b.Open);

            var momentumAccelerating = (rsiRising && macdRising) || (rsiRising && rvolHealthy) || (macdRising && rvolHealthy);
            var qualityBullishCandle = price > b.Open && b.High > b.Low && (price - b.Low) / (b.High - b.Low) >= 0.60m;
            var executionVolume = b.Rvol >= p.MinimumExecutionRvol;

            var candidateEnvironment = b.Five.ExecutionPermission && !b.Five.SwingZone && aboveVwap && price > b.Ema20 && ema20Rising;
            var candidateMomentum = b.Rsi >= p.MinimumExecutionRsi && macdBullish &&
                (price > b.Ema9 || price > pb.Close || rsiRising || macdRising || rvolHealthy);
            var candidate = candidateEnvironment && candidateMomentum;

            var confirmationRaw = candidate && qualityBullishCandle &&
                (momentumAccelerating || price > pb.High || b.Rsi >= p.RsiStrong) && !bullishExtensionDanger;

            var pauseEvidenceCount = 0;
            for (var j = i - p.ContinuationLookbackBars + 1; j <= i; j++)
            {
                var cur = bars[j]; var prv = bars[j - 1];
                if (cur.Close <= prv.Close || cur.High <= prv.High || !(cur.Rsi > prv.Rsi) ||
                    !(cur.Macd - cur.MacdSignal > prv.Macd - prv.MacdSignal))
                    pauseEvidenceCount++;
            }
            var continuationPause = pauseEvidenceCount >= p.ContinuationPauseBarsRequired;
            var continuationResume = price > b.Ema9 && ema9Rising && price > pb.High && b.Rsi >= p.ContinuationMinimumRsi &&
                macdBullish && (rsiRising || macdRising || rvolHealthy);
            var continuationBase = b.Five.RunnerStructure && b.Five.ExecutionPermission && !b.Five.SwingZone;

            var armedBefore = contArmed;
            contArmed = !b.EodWindow && continuationBase && !continuationResume && (continuationPause || contArmed);
            var continuationRaw = continuationBase && armedBefore && continuationResume && executionVolume &&
                qualityBullishCandle && !bullishExtensionDanger;
            var cooldownBefore = contCooldownAge;
            contCooldownAge = continuationRaw ? 0 : cooldownBefore >= 0 ? cooldownBefore + 1 : p.ContinuationCooldownBars;
            var continuationQualified = continuationRaw && cooldownBefore >= p.ContinuationCooldownBars;

            var pauseRaw = !candidate && b.Five.ExecutionPermission && aboveVwap && price > b.Ema20;
            pauseCount = pauseRaw ? pauseCount + 1 : 0;
            var everBefore = everActive;
            everActive = everActive || candidate;
            var rearmNow = !everBefore || pauseCount >= p.CandidateRearmBars;

            var freshEvent = candidate && freshTrigger && !prevFresh && prevRearm;
            var inTrendCandidate = candidate && !freshTrigger;
            var inTrendEvent = inTrendCandidate && !prevInTrend && prevRearm;
            var continuationEvent = continuationQualified && !prevContQual;
            var confirmationEvent = confirmationRaw && !prevConfRaw;
            var opportunity = freshEvent || inTrendEvent || continuationEvent || confirmationEvent;
            var newTrigger = opportunity && !prevOpp;

            age = newTrigger ? 0 : age >= 0 && age < p.ExecutionWindowBars ? age + 1 : -1;
            var windowActive = age >= 0 && age < p.ExecutionWindowBars;
            var invalidated = !b.Five.ExecutionPermission || price < b.Ema20 || price < b.Vwap || b.Rsi < p.RsiWeak ||
                macdBearish || bullishExtensionDanger;
            var ready = windowActive && b.Five.ExecutionPermission && !bullishExtensionDanger && !invalidated;

            var trigger = string.Join("+", new[]
            {
                freshEvent ? "FRESH" : null, inTrendEvent ? "IN_TREND" : null,
                continuationEvent ? "CONTINUATION" : null, confirmationEvent ? "CONFIRMATION" : null
            }.Where(x => x is not null));
            steps.Add(new ExecutionEventStep(b.Timestamp, candidate, trigger, age, windowActive, invalidated, ready));

            prevFresh = freshTrigger; prevInTrend = inTrendCandidate; prevContQual = continuationQualified;
            prevConfRaw = confirmationRaw; prevOpp = opportunity; prevRearm = rearmNow;
        }
        return steps;
    }
}
