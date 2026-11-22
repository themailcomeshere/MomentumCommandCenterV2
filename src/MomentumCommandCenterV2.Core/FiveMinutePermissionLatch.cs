namespace MomentumCommandCenterV2.Core;

/// <summary>
/// TYPE: PermissionLatchBar
/// PURPOSE: The completed-5m-bar inputs the installed Thinkorswim 5M study uses for Permission: close, daily VWAP,
/// EMA9, EMA20, SMA50 and RSI(14). Timestamp is the bar's start time (Thinkorswim GetTime()).
/// WHEN CALLED: Built per completed 5m bar by the live assessment and the historical replay, then passed to
/// <see cref="FiveMinutePermissionLatch.Evaluate"/>.
/// </summary>
public sealed record PermissionLatchBar(
    DateTimeOffset Timestamp,
    decimal Close,
    decimal Vwap,
    decimal Ema9,
    decimal Ema20,
    decimal Sma50,
    decimal Rsi);

/// <summary>
/// TYPE: PermissionLatchConfig
/// PURPOSE: Inputs of the Thinkorswim 5M study (docs/Charts/5M/MainStudy.txt, V4.1.8): permissionFailureBars = 2,
/// majorFailureBars = 2, minimumPermissionRSI = 50, eodTimeEasternDaylight = 1555, eodTimeEasternStandard = 1555.
/// The EOD is 15:55 ET in both seasons (operator decision 2026-11-22, option A), the same as the V2 engine's
/// EOD GET OUT (V2Config.EndOfDayExitHourEt/MinuteEt). The installed V4.1.7 values were 1855 / 1955.
/// ReleaseAtNewEasternDate models the operator's "1 D" 5m chart, where the latch starts at NO on each day's first
/// bar (CompoundValue initial value).
/// WHEN CALLED: Passed to <see cref="FiveMinutePermissionLatch.Evaluate"/>; defaults are the V4.1.8 study's values.
/// </summary>
public sealed record PermissionLatchConfig(
    int PermissionFailureBars = 2,
    int MajorFailureBars = 2,
    decimal MinimumPermissionRsi = 50m,
    int EodEasternDaylightHhmm = 1555,
    int EodEasternStandardHhmm = 1555,
    bool ReleaseAtNewEasternDate = true);

/// <summary>
/// TYPE: PermissionLatchStep
/// PURPOSE: The latch state after one completed 5m bar, with every diagnostic the study computes:
/// Raw (bullishPermissionRaw), StructuralDamage / MajorFailure and their persistent forms, the EOD window, the
/// resulting Permission, how it got there (Transition) and the study's permissionBlockCode (0–8).
/// WHEN CALLED: Produced by <see cref="FiveMinutePermissionLatch.Evaluate"/> for each bar.
/// </summary>
public sealed record PermissionLatchStep(
    DateTimeOffset Timestamp,
    bool Raw,
    bool StructuralDamage,
    bool StructuralDamagePersistent,
    bool MajorFailure,
    bool MajorFailurePersistent,
    bool EodWindow,
    bool Permission,
    string Transition,
    int BlockCode);

/// <summary>
/// TYPE: PermissionLatchResult
/// PURPOSE: All per-bar steps, the latest step, and when the current YES started (null when the latest is NO).
/// WHEN CALLED: Returned by <see cref="FiveMinutePermissionLatch.Evaluate"/>.
/// </summary>
public sealed record PermissionLatchResult(
    IReadOnlyList<PermissionLatchStep> Steps,
    PermissionLatchStep? Latest,
    DateTimeOffset? GrantedSinceUtc);

/// <summary>
/// TYPE: FiveMinutePermissionLatch
/// PURPOSE: Exact port of the installed Thinkorswim 5M study's PERMISSION STATE (a latch), replacing MCC's former
/// stateless "5 of 6" score as the entry gate (operator decision 2026-11-17):
/// <code>
/// bullishPermissionRaw = close > dailyVWAP and close > ema9 and ema9 >= ema20 and rsi >= 50
/// structuralDamage     = close &lt; dailyVWAP and ema9 &lt; ema20              (persistent = 2 bars in a row)
/// majorFailureRaw      = close &lt; dailyVWAP and close &lt; ema9 and ema9 &lt; ema20 and ema20 &lt; sma50 (persistent = 2 bars)
/// permission = eod ? 0 : majorFailurePersistent ? 0 : structuralDamagePersistent ? 0 : raw ? 1 : permission[1]
/// </code>
/// The EOD window is at or after 15:55 ET in daylight and standard time (5M study V4.1.8, operator decision
/// 2026-11-22), matching the V2 engine's EOD GET OUT. That is 12:55 AZ in daylight time and 13:55 AZ in standard
/// time. The study keeps its date-based DST branch, so both inputs remain configurable.
/// Before 2026-11-22 the port used the installed V4.1.7 values (18:55 ET daylight / 19:55 ET standard).
/// WHEN CALLED: By the live assessment and the historical replay before V2SignalEngine evaluates a snapshot.
/// </summary>
public static class FiveMinutePermissionLatch
{
    /// <summary>Rules version shown wherever the latch result is reported.</summary>
    public const string RulesVersion = "TOS-5M-PERMISSION-LATCH-2 (5M study V4.1.8, EOD 15:55 ET)";

    private static readonly TimeZoneInfo Eastern = ResolveEastern();

    /// <summary>
    /// PURPOSE: Runs the latch over completed 5m bars in time order and returns each step.
    /// WHEN CALLED: Once per assessment or replay; bars must be completed and ascending by Timestamp.
    /// </summary>
    public static PermissionLatchResult Evaluate(IReadOnlyList<PermissionLatchBar> bars, PermissionLatchConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(bars);
        var rules = config ?? new PermissionLatchConfig();
        var steps = new List<PermissionLatchStep>(bars.Count);
        var damageRun = 0;
        var majorRun = 0;
        var permission = false;
        DateOnly? previousDate = null;
        DateTimeOffset? grantedSince = null;

        foreach (var bar in bars)
        {
            var eastern = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern);
            var date = DateOnly.FromDateTime(eastern.DateTime);
            var newDay = previousDate is not null && date != previousDate;
            if (newDay && rules.ReleaseAtNewEasternDate)
            {
                // Operator's 1 D chart: each day's first bar starts from CompoundValue's initial 0 and Sum() has no prior bar.
                permission = false;
                grantedSince = null;
                damageRun = 0;
                majorRun = 0;
            }
            previousDate = date;

            var raw = bar.Close > bar.Vwap && bar.Close > bar.Ema9 && bar.Ema9 >= bar.Ema20 && bar.Rsi >= rules.MinimumPermissionRsi;
            var damage = bar.Close < bar.Vwap && bar.Ema9 < bar.Ema20;
            var major = bar.Close < bar.Vwap && bar.Close < bar.Ema9 && bar.Ema9 < bar.Ema20 && bar.Ema20 < bar.Sma50;
            damageRun = damage ? damageRun + 1 : 0;
            majorRun = major ? majorRun + 1 : 0;
            var damagePersistent = damageRun >= rules.PermissionFailureBars;
            var majorPersistent = majorRun >= rules.MajorFailureBars;
            var eod = IsEodWindow(eastern, rules);

            var before = permission;
            string transition;
            if (eod) { permission = false; transition = before ? "RELEASED_EOD" : "EOD_WINDOW"; }
            else if (majorPersistent) { permission = false; transition = before ? "RELEASED_MAJOR_FAILURE" : "BLOCKED_MAJOR_FAILURE"; }
            else if (damagePersistent) { permission = false; transition = before ? "RELEASED_STRUCTURAL_DAMAGE" : "BLOCKED_STRUCTURAL_DAMAGE"; }
            else if (raw) { permission = true; transition = before ? "RAW_YES" : "GRANTED"; }
            else { transition = before ? "HELD" : "NOT_GRANTED"; }

            if (permission && !before) grantedSince = bar.Timestamp;
            if (!permission) grantedSince = null;

            var blockCode = permission ? 0
                : eod ? 1
                : majorPersistent ? 2
                : damagePersistent ? 3
                : !(bar.Close > bar.Vwap) ? 4
                : !(bar.Close > bar.Ema9) ? 5
                : !(bar.Ema9 >= bar.Ema20) ? 6
                : !(bar.Rsi >= rules.MinimumPermissionRsi) ? 7
                : 8;

            steps.Add(new PermissionLatchStep(bar.Timestamp, raw, damage, damagePersistent, major, majorPersistent, eod, permission, transition, blockCode));
        }

        return new PermissionLatchResult(steps, steps.LastOrDefault(), grantedSince);
    }

    /// <summary>
    /// PURPOSE: The study's afterEOD test: the bar's Eastern time is at or after the configured EOD (15:55 ET by default
    /// in both seasons).
    /// WHEN CALLED: Per bar by <see cref="Evaluate"/>; public for tests.
    /// </summary>
    public static bool IsEodWindow(DateTimeOffset easternTime, PermissionLatchConfig rules)
    {
        var date = DateOnly.FromDateTime(easternTime.DateTime);
        var hhmm = StudyDaylightTime(date) ? rules.EodEasternDaylightHhmm : rules.EodEasternStandardHhmm;
        var minutes = easternTime.Hour * 60 + easternTime.Minute;
        return minutes >= hhmm / 100 * 60 + hhmm % 100;
    }

    /// <summary>
    /// PURPOSE: The study's date-based DST rule: daylight from the second Sunday of March through the day before the first
    /// Sunday of November.
    /// WHEN CALLED: By <see cref="IsEodWindow"/>.
    /// </summary>
    public static bool StudyDaylightTime(DateOnly date)
    {
        static int NthSunday(int year, int month, int n)
        {
            var first = new DateOnly(year, month, 1);
            var offset = ((int)DayOfWeek.Sunday - (int)first.DayOfWeek + 7) % 7;
            return 1 + offset + (n - 1) * 7;
        }
        var secondSundayMarch = NthSunday(date.Year, 3, 2);
        var firstSundayNovember = NthSunday(date.Year, 11, 1);
        return (date.Month > 3 && date.Month < 11)
               || (date.Month == 3 && date.Day >= secondSundayMarch)
               || (date.Month == 11 && date.Day < firstSundayNovember);
    }

    /// <summary>
    /// PURPOSE: Short operator text for a block code, matching the study's diagnostic list.
    /// WHEN CALLED: By the live assessment when the latch reads NO.
    /// </summary>
    public static string BlockLabel(int code) => code switch
    {
        0 => "Permission YES",
        1 => "EOD exit window",
        2 => "persistent major failure",
        3 => "persistent structural damage (2 bars below VWAP with EMA9 < EMA20)",
        4 => "price not above daily VWAP",
        5 => "price not above EMA9",
        6 => "EMA9 below EMA20",
        7 => "RSI below 50",
        _ => "no listed blocker / initialization edge"
    };

    private static TimeZoneInfo ResolveEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }
}
