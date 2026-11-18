namespace MomentumCommandCenterV2.API.MarketData.AutoScan;

/// <summary>
/// TYPE: AutoScanBar
/// PURPOSE: One completed 5-minute regular-session bar used by the automatic scanner. <see cref="Vwap"/> is the
/// provider's per-bar volume-weighted price (Alpaca "vw") when supplied.
/// WHEN CALLED: Built by <see cref="AlpacaAutoScanClient"/> and consumed by <see cref="AutoMomentumScanRules"/>.
/// </summary>
public sealed record AutoScanBar(
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal? Vwap);

/// <summary>
/// TYPE: AutoScanUniverseRow
/// PURPOSE: One symbol in the scan universe with its live price (IEX latest trade), consolidated day volume (Alpaca
/// most-actives screener, SIP) and percent change from the prior completed Daily close. Null fields are unknown, never estimated.
/// WHEN CALLED: Built by <see cref="AutoMomentumScanService"/> before the rules run.
/// </summary>
public sealed record AutoScanUniverseRow(
    string Symbol,
    decimal? LastPrice,
    string? LastPriceSource,
    long? ConsolidatedDayVolume,
    decimal? PriorClose,
    string? PriorCloseSource,
    decimal? PercentChange,
    IReadOnlyList<string> Sources);

/// <summary>
/// TYPE: AutoScanIndicators
/// PURPOSE: Indicator values on the latest completed 5m bar (and the bar before it) used by the scanner conditions.
/// WHEN CALLED: Returned by <see cref="AutoMomentumScanRules.ComputeIndicators"/>.
/// </summary>
public sealed record AutoScanIndicators(
    int BarCount,
    DateTimeOffset? LatestBarTimestamp,
    decimal? Close,
    decimal? PreviousClose,
    decimal? Vwap,
    decimal? PreviousVwap,
    decimal? Ema9,
    decimal? Ema20,
    decimal? Sma50,
    decimal? Rsi14,
    long? Volume,
    long? PreviousVolume,
    decimal? AverageVolume20,
    decimal? PriorHigh20,
    int MissingSlotsInLast50,
    decimal? SessionOpen = null,
    decimal? SessionHigh = null,
    decimal? Atr14 = null);

/// <summary>
/// TYPE: AutoScanCondition
/// PURPOSE: One scanner condition. Passed is null when an input is unknown. UsesIexVolume marks conditions computed
/// from IEX bar volume (partial-exchange coverage; diagnostic only).
/// WHEN CALLED: Produced by <see cref="AutoMomentumScanRules.EvaluateScanners"/>.
/// </summary>
public sealed record AutoScanCondition(string Key, string Label, bool? Passed, bool UsesIexVolume);

/// <summary>
/// TYPE: AutoScanScannerResult
/// PURPOSE: The outcome of one of the operator's Thinkorswim scans: PASS when every condition is true, FAIL when any is
/// false, otherwise UNKNOWN.
/// WHEN CALLED: Produced by <see cref="AutoMomentumScanRules.EvaluateScanners"/>.
/// </summary>
public sealed record AutoScanScannerResult(
    string Code,
    string Name,
    string Result,
    bool ApproximateVolume,
    IReadOnlyList<AutoScanCondition> Conditions);

/// <summary>
/// TYPE: AutoMomentumScanRules
/// PURPOSE: Reproduces the operator's Thinkorswim intraday scans (docs/Scanners: 1 Momentum Hunter, 2A Momentum Bullish,
/// 2B Breakout Trigger, Momentum Pivot, D Market Movers Discovery) on completed 5m regular-session bars.
/// Thresholds are the TOS values as written; nothing is tuned. Thinkorswim formulas: ExpAverage seeded with the first
/// value, Average = simple, RSI = Wilder, VWAP() = session VWAP.
/// WHEN CALLED: By <see cref="AutoMomentumScanService"/> once per symbol per scan run, and by unit tests.
/// </summary>
public static class AutoMomentumScanRules
{
    /// <summary>Scanner code for 1 · Momentum Hunter.</summary>
    public const string MomentumHunter = "1";
    /// <summary>Scanner code for 2A · Momentum Bullish.</summary>
    public const string MomentumBullish = "2A";
    /// <summary>Scanner code for 2B · Breakout Trigger.</summary>
    public const string BreakoutTrigger = "2B";
    /// <summary>Scanner code for Momentum Pivot.</summary>
    public const string MomentumPivot = "PIV";
    /// <summary>Scanner code for D · Market Movers Discovery.</summary>
    public const string MarketMovers = "D";

    private static readonly TimeZoneInfo Eastern = ResolveEastern();

    /// <summary>
    /// PURPOSE: Computes the indicators on the latest bar of an ascending list of completed regular-session 5m bars.
    /// Values that need more history than supplied are null.
    /// WHEN CALLED: Once per symbol per scan run, before <see cref="EvaluateScanners"/>.
    /// </summary>
    public static AutoScanIndicators ComputeIndicators(IReadOnlyList<AutoScanBar> bars)
    {
        var n = bars.Count;
        if (n == 0)
            return new AutoScanIndicators(0, null, null, null, null, null, null, null, null, null, null, null, null, null, 0);

        var closes = bars.Select(b => b.Close).ToArray();
        var last = n - 1;
        var vwaps = SessionVwapSeries(bars);
        decimal? ema9 = n >= 20 ? TosEma(closes, 9) : null;
        decimal? ema20 = n >= 20 ? TosEma(closes, 20) : null;
        decimal? sma50 = n >= 50 ? closes.Skip(n - 50).Average() : null;
        decimal? rsi = n >= 15 ? WilderRsi(closes, 14) : null;
        decimal? avgVol = n >= 20 ? bars.Skip(n - 20).Average(b => (decimal)b.Volume) : null;
        decimal? priorHigh = n >= 21 ? bars.Skip(n - 21).Take(20).Max(b => b.High) : null;
        // Chase inputs (2026-11-18): today's regular-session open and high (bars from 09:30 ET on the latest bar's
        // Eastern date; pre-market excluded) and a 14-bar 5m ATR. Null when today has no regular-session bar yet.
        var today = EasternDate(bars[last].Timestamp);
        var session = bars.Where(b => EasternDate(b.Timestamp) == today && IsRegularSessionBar(b.Timestamp)).ToArray();
        decimal? sessionOpen = session.Length > 0 ? session[0].Open : null;
        decimal? sessionHigh = session.Length > 0 ? session.Max(b => b.High) : null;
        decimal? atr14 = null;
        if (n >= 15)
        {
            var trs = new List<decimal>();
            for (var i = n - 14; i < n; i++)
                trs.Add(Math.Max(bars[i].High - bars[i].Low, Math.Max(Math.Abs(bars[i].High - bars[i - 1].Close), Math.Abs(bars[i].Low - bars[i - 1].Close))));
            atr14 = trs.Average();
        }

        return new AutoScanIndicators(
            n,
            bars[last].Timestamp,
            closes[last],
            n >= 2 ? closes[last - 1] : null,
            vwaps[last],
            n >= 2 ? vwaps[last - 1] : null,
            ema9,
            ema20,
            sma50,
            rsi,
            bars[last].Volume,
            n >= 2 ? bars[last - 1].Volume : null,
            avgVol,
            priorHigh,
            CountMissingSlots(bars, 50),
            sessionOpen,
            sessionHigh,
            atr14);
    }

    /// <summary>Chase thresholds (operator approval 2026-11-18, "Yes, build it"; proposal replayed on SLS 2026-11-17).</summary>
    public const decimal ChaseGapPercent = 10m, ChaseOffHighPercent = 10m, ChaseExtensionAtr = 1.5m;

    /// <summary>
    /// PURPOSE: Why a candidate looks like a chase (the move is already over or stretched). Codes:
    ///  - GAP_FADING: % change vs the prior close is +10% or more and the price is below today's regular-session open
    ///    (the move happened before the open and is fading; SLS 2026-11-17);
    ///  - OFF_HIGH: the price is 10% or more below today's regular-session high;
    ///  - EXTENDED: the price is 1.5 ATR(14, 5m) or more above session VWAP (MCC's extension warning level).
    /// Unknown inputs add nothing (no code is guessed). Price = the live last trade, else the latest 5m close.
    /// WHEN CALLED: Once per candidate by AutoMomentumScanService.Evaluate.
    /// </summary>
    public static IReadOnlyList<string> ChaseReasons(AutoScanUniverseRow row, AutoScanIndicators x)
    {
        var reasons = new List<string>();
        var price = row.LastPrice ?? x.Close;
        if (price is null) return reasons;
        if (row.PercentChange is >= ChaseGapPercent && x.SessionOpen is > 0m && price.Value < x.SessionOpen.Value)
            reasons.Add("GAP_FADING");
        if (x.SessionHigh is > 0m && price.Value <= x.SessionHigh.Value * (1m - ChaseOffHighPercent / 100m))
            reasons.Add("OFF_HIGH");
        if (x.Vwap is not null && x.Atr14 is > 0m && (price.Value - x.Vwap.Value) / x.Atr14.Value >= ChaseExtensionAtr)
            reasons.Add("EXTENDED");
        return reasons;
    }

    /// <summary>PURPOSE: % move from today's regular-session open (today's momentum), or null. WHEN CALLED: Ranking and display.</summary>
    public static decimal? PercentFromOpen(AutoScanUniverseRow row, AutoScanIndicators x)
    {
        var price = row.LastPrice ?? x.Close;
        return price is null || x.SessionOpen is not > 0m ? null : Math.Round((price.Value / x.SessionOpen.Value - 1m) * 100m, 2);
    }

    /// <summary>
    /// PURPOSE: Evaluates the five scans for one symbol. "Last" price and day volume come from the universe row; every
    /// bar condition uses the latest completed 5m bar. Scanner 1 runs on 5m bars (confirmed by the operator 2026-11-16, like the others).
    /// WHEN CALLED: Once per symbol per scan run.
    /// </summary>
    public static IReadOnlyList<AutoScanScannerResult> EvaluateScanners(AutoScanUniverseRow row, AutoScanIndicators x)
    {
        var aboveVwap = Gt(x.Close, x.Vwap);
        var trend = All(Gt(x.Close, x.Ema9), Gt(x.Ema9, x.Ema20), Gt(x.Ema20, x.Sma50));
        var shortTrend = All(Gt(x.Close, x.Ema9), Gt(x.Ema9, x.Ema20));
        var volume15 = x.Volume is null || x.AverageVolume20 is null ? (bool?)null : x.Volume.Value > x.AverageVolume20.Value * 1.5m;
        var rvol2AtLeast = x.Volume is null || x.AverageVolume20 is null || x.AverageVolume20 == 0m ? (bool?)null : x.Volume.Value / x.AverageVolume20.Value >= 2.0m;
        var rvol2Above = x.Volume is null || x.AverageVolume20 is null || x.AverageVolume20 == 0m ? (bool?)null : x.Volume.Value / x.AverageVolume20.Value > 2.0m;
        var accel = x.Volume is null || x.PreviousVolume is null ? (bool?)null : x.Volume.Value > x.PreviousVolume.Value * 1.5m;
        var breakout = Gt(x.Close, x.PriorHigh20);
        var reclaim = All(Gt(x.Close, x.Vwap), Le(x.PreviousClose, x.PreviousVwap));
        var rsi55 = Gt(x.Rsi14, 55m);

        AutoScanCondition Price(decimal min, decimal max) => new("price", $"Last ${min:0.00}–${max:0.00}",
            row.LastPrice is null ? null : row.LastPrice.Value >= min && row.LastPrice.Value <= max, false);
        AutoScanCondition DayVolume(long min) => new("dayVolume", $"Day volume ≥ {min:N0} (consolidated)",
            row.ConsolidatedDayVolume is null ? null : row.ConsolidatedDayVolume.Value >= min, false);
        AutoScanCondition PercentChange(decimal min) => new("percentChange", $"% change ≥ +{min:0.00}%",
            row.PercentChange is null ? null : row.PercentChange.Value >= min, false);
        AutoScanCondition C(string key, string label, bool? passed, bool iex = false) => new(key, label, passed, iex);

        return new[]
        {
            Build(MomentumHunter, "1 · Momentum Hunter", new[]
            {
                Price(3m, 500m),
                C("aboveVwap", "Close > VWAP", aboveVwap),
                C("trend", "Close > EMA9 > EMA20 > SMA50", trend),
                C("volume15", "Volume > 1.5 × 20-bar average", volume15, true)
            }),
            Build(MomentumBullish, "2A · Momentum Bullish", new[]
            {
                Price(4m, 501m),
                DayVolume(500_000),
                PercentChange(2m),
                C("aboveVwap", "Close > VWAP", aboveVwap),
                C("rvol2", "Volume / 20-bar average ≥ 2.0", rvol2AtLeast, true),
                C("rvol2Accel", "Volume / 20-bar average > 2 and volume > 1.5 × previous bar", All(rvol2Above, accel), true),
                C("breakout20", "Close > highest high of prior 20 bars", breakout)
            }),
            Build(BreakoutTrigger, "2B · Breakout Trigger", new[]
            {
                Price(3m, 500m),
                C("aboveVwap", "Close > VWAP", aboveVwap),
                C("shortTrend", "Close > EMA9 > EMA20", shortTrend),
                C("volume15", "Volume > 1.5 × 20-bar average", volume15, true),
                C("breakout20", "Close > highest high of prior 20 bars", breakout)
            }),
            Build(MomentumPivot, "Momentum Pivot", new[]
            {
                Price(3.5m, 500m),
                DayVolume(500_000),
                C("breakoutOrReclaim", "Breakout over 20-bar high, or VWAP reclaim", Any(breakout, reclaim)),
                C("trend", "Close > EMA9 > EMA20 > SMA50", trend),
                C("aboveVwap", "Close > VWAP", aboveVwap),
                C("rsi55", "RSI(14) > 55", rsi55),
                C("volume15", "Volume > 1.5 × 20-bar average", volume15, true)
            }),
            Build(MarketMovers, "D · Market Movers Discovery", new[]
            {
                Price(3m, 500m),
                DayVolume(250_000),
                PercentChange(2.99m)
            })
        };
    }

    /// <summary>
    /// PURPOSE: Short tags for the Dashboard (scanners passed, approximate-volume and data-gap flags).
    /// WHEN CALLED: Once per candidate after <see cref="EvaluateScanners"/>.
    /// </summary>
    public static IReadOnlyList<string> Tags(IReadOnlyList<AutoScanScannerResult> scanners, AutoScanIndicators indicators)
    {
        var tags = scanners.Where(s => s.Result == "PASS").Select(s => s.Code).ToList();
        if (scanners.Any(s => s.Result == "PASS" && s.ApproximateVolume)) tags.Add("IEX_VOLUME_APPROX");
        if (indicators.MissingSlotsInLast50 > 0) tags.Add($"IEX_GAPS_{indicators.MissingSlotsInLast50}");
        return tags;
    }

    /// <summary>
    /// PURPOSE: Ranking key part: number of 5m scans passed (1, 2A, 2B, Pivot). Full order (2026-11-18): flagged, not a
    /// chase, this count, scan D, % move since today's open, % change, symbol.
    /// WHEN CALLED: By <see cref="AutoMomentumScanService"/> when ordering candidates.
    /// </summary>
    public static int MomentumPassCount(IReadOnlyList<AutoScanScannerResult> scanners) =>
        scanners.Count(s => s.Code != MarketMovers && s.Result == "PASS");

    /// <summary>
    /// PURPOSE: Session VWAP per bar (each bar's own Eastern session date), cumulative over that session's bars up to it.
    /// Uses the provider bar VWAP when present, otherwise the typical price (H+L+C)/3.
    /// WHEN CALLED: By <see cref="ComputeIndicators"/>.
    /// </summary>
    public static decimal?[] SessionVwapSeries(IReadOnlyList<AutoScanBar> bars)
    {
        var result = new decimal?[bars.Count];
        DateOnly? session = null;
        decimal pv = 0m, vol = 0m;
        for (var i = 0; i < bars.Count; i++)
        {
            var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(bars[i].Timestamp, Eastern).DateTime);
            if (session != date) { session = date; pv = 0m; vol = 0m; }
            var price = bars[i].Vwap ?? (bars[i].High + bars[i].Low + bars[i].Close) / 3m;
            pv += price * bars[i].Volume;
            vol += bars[i].Volume;
            result[i] = vol > 0m ? pv / vol : null;
        }
        return result;
    }

    /// <summary>
    /// PURPOSE: Thinkorswim ExpAverage: EMA seeded with the first value, alpha = 2 / (length + 1).
    /// WHEN CALLED: By <see cref="ComputeIndicators"/>.
    /// </summary>
    public static decimal TosEma(IReadOnlyList<decimal> values, int length)
    {
        var alpha = 2m / (length + 1m);
        var ema = values[0];
        for (var i = 1; i < values.Count; i++) ema += alpha * (values[i] - ema);
        return ema;
    }

    /// <summary>
    /// PURPOSE: Wilder RSI, seeded with the simple average of the first <paramref name="length"/> changes.
    /// WHEN CALLED: By <see cref="ComputeIndicators"/>.
    /// </summary>
    public static decimal WilderRsi(IReadOnlyList<decimal> closes, int length)
    {
        decimal gain = 0m, loss = 0m;
        for (var i = 1; i <= length; i++)
        {
            var change = closes[i] - closes[i - 1];
            gain += Math.Max(0m, change);
            loss += Math.Max(0m, -change);
        }
        gain /= length;
        loss /= length;
        for (var i = length + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            gain = (gain * (length - 1) + Math.Max(0m, change)) / length;
            loss = (loss * (length - 1) + Math.Max(0m, -change)) / length;
        }
        if (loss == 0m) return gain == 0m ? 50m : 100m;
        return 100m - 100m / (1m + gain / loss);
    }

    /// <summary>
    /// PURPOSE: Counts regular-session 5m slots with no bar within the span of the last <paramref name="window"/> bars
    /// (IEX has no bar when no IEX trade printed). Gaps are reported, never filled.
    /// WHEN CALLED: By <see cref="ComputeIndicators"/>.
    /// </summary>
    public static int CountMissingSlots(IReadOnlyList<AutoScanBar> bars, int window)
    {
        if (bars.Count < 2) return 0;
        var slice = bars.Skip(Math.Max(0, bars.Count - window)).ToArray();
        var missing = 0;
        for (var i = 1; i < slice.Length; i++)
        {
            var a = TimeZoneInfo.ConvertTime(slice[i - 1].Timestamp, Eastern);
            var b = TimeZoneInfo.ConvertTime(slice[i].Timestamp, Eastern);
            if (a.Date == b.Date)
                missing += Math.Max(0, (int)Math.Round((b - a).TotalMinutes / 5d) - 1);
        }
        return missing;
    }

    /// <summary>PURPOSE: True when the bar opens inside 09:30–16:00 Eastern on a weekday. WHEN CALLED: When filtering provider bars.</summary>
    public static bool IsRegularSessionBar(DateTimeOffset barOpen)
    {
        var local = TimeZoneInfo.ConvertTime(barOpen, Eastern);
        var t = local.TimeOfDay;
        return local.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
               t >= new TimeSpan(9, 30, 0) && t < new TimeSpan(16, 0, 0);
    }

    /// <summary>PURPOSE: Returns the Eastern calendar date of an instant. WHEN CALLED: When separating today's session from prior sessions.</summary>
    public static DateOnly EasternDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Eastern).DateTime);

    /// <summary>PURPOSE: Returns midnight Eastern of the given Eastern date as UTC. WHEN CALLED: For completed-Daily-bar request bounds.</summary>
    public static DateTimeOffset EasternMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>PURPOSE: Builds a scanner result (PASS / FAIL / UNKNOWN). WHEN CALLED: By <see cref="EvaluateScanners"/>.</summary>
    private static AutoScanScannerResult Build(string code, string name, IReadOnlyList<AutoScanCondition> conditions)
    {
        var result = conditions.Any(c => c.Passed == false) ? "FAIL"
            : conditions.All(c => c.Passed == true) ? "PASS"
            : "UNKNOWN";
        return new AutoScanScannerResult(code, name, result, conditions.Any(c => c.UsesIexVolume), conditions);
    }

    /// <summary>PURPOSE: Null-aware greater-than. WHEN CALLED: By condition evaluation.</summary>
    private static bool? Gt(decimal? a, decimal? b) => a is null || b is null ? null : a.Value > b.Value;

    /// <summary>PURPOSE: Null-aware less-or-equal. WHEN CALLED: By condition evaluation.</summary>
    private static bool? Le(decimal? a, decimal? b) => a is null || b is null ? null : a.Value <= b.Value;

    /// <summary>PURPOSE: Three-valued AND (false wins, then unknown). WHEN CALLED: By condition evaluation.</summary>
    private static bool? All(params bool?[] values) =>
        values.Any(v => v == false) ? false : values.All(v => v == true) ? true : null;

    /// <summary>PURPOSE: Three-valued OR (true wins, then unknown). WHEN CALLED: By condition evaluation.</summary>
    private static bool? Any(params bool?[] values) =>
        values.Any(v => v == true) ? true : values.All(v => v == false) ? false : null;

    /// <summary>PURPOSE: Resolves America/New_York on Windows or Linux. WHEN CALLED: Once at type initialisation.</summary>
    private static TimeZoneInfo ResolveEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }
}
