using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Returns the live US-equities session plus the configured Boring Overnight schedule.
/// The candidate is selected and purchased during the final regular-session hour.
/// The Overnight measurement begins at the regular-session close and includes the
/// Buy Day after-hours period and the following Sell Day premarket period.
/// The next trading session's GTC_EXT eligible sell window is 7:00 AM–8:00 PM Eastern.
/// These are schedule windows only; per-symbol broker eligibility and actual fills are not verified.
/// Exceptional unscheduled exchange closures are not represented by the maintained calendar.
/// </summary>
public sealed record MarketSessionClockSnapshot(
    string Phase, string Label, DateTimeOffset TargetUtc, DateTimeOffset GeneratedAtUtc,
    string TargetEvent, string ExchangeTimeZone, string CalendarVersion,
    DateTimeOffset OvernightEvaluationStartUtc, DateTimeOffset OvernightBuyDeadlineUtc,
    bool OvernightEvaluationOpen, DateTimeOffset OvernightBuyWindowStartUtc,
    bool OvernightBuyWindowOpen, DateOnly OvernightBuySessionDate, DateOnly OvernightSellSessionDate,
    DateTimeOffset OvernightSellWindowStartUtc, DateTimeOffset OvernightSellWindowEndUtc,
    string OvernightScheduleNote);

public static class MarketSessionClock
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeZoneInfo Phoenix = TimeZoneInfo.FindSystemTimeZoneById("America/Phoenix");
    private static readonly TimeOnly PreMarketStart = new(4, 0), RegularOpen = new(9, 30), AfterHoursEnd = new(20, 0);
    private static readonly TimeOnly SellWindowStart = new(7, 0), SellWindowEnd = new(20, 0);

    public static MarketSessionClockSnapshot GetSnapshot(DateTimeOffset nowUtc)
    {
        nowUtc = nowUtc.ToUniversalTime();
        var easternNow = TimeZoneInfo.ConvertTime(nowUtc, Eastern);
        var today = DateOnly.FromDateTime(easternNow.DateTime);
        var localTime = TimeOnly.FromDateTime(easternNow.DateTime);
        var todayClose = UsEquitiesSessionCalendar.IsTradingSession(today)
            ? UsEquitiesSessionCalendar.GetRegularClose(today) : (TimeOnly?)null;

        string phase, label, targetEvent;
        TimeOnly targetTime;
        if (todayClose is { } close && localTime >= RegularOpen && localTime < close)
        { phase = "REGULAR"; label = "Remaining time in regular market"; targetTime = close; targetEvent = "REGULAR_SESSION_CLOSE"; }
        else if (todayClose is { } afterHoursClose && localTime >= afterHoursClose && localTime < AfterHoursEnd)
        { phase = "AFTER_HOURS"; label = "Remaining time in After-Hours"; targetTime = AfterHoursEnd; targetEvent = "AFTER_HOURS_END"; }
        else if (todayClose is not null && localTime >= PreMarketStart && localTime < RegularOpen)
        { phase = "PRE_MARKET"; label = "Until Market opens"; targetTime = RegularOpen; targetEvent = "REGULAR_SESSION_OPEN"; }
        else
        { phase = "CLOSED"; label = "Until Market opens"; targetTime = RegularOpen; targetEvent = "NEXT_REGULAR_SESSION_OPEN"; }

        var nextSession = todayClose is not null && localTime < todayClose.Value ? today : today.AddDays(1);
        for (var i = 0; i < 15 && !UsEquitiesSessionCalendar.IsTradingSession(nextSession); i++) nextSession = nextSession.AddDays(1);
        if (!UsEquitiesSessionCalendar.IsTradingSession(nextSession))
            throw new InvalidOperationException("Could not find the next supported US-equities trading session.");

        var buySession = FindNextBuySession(today, localTime);
        var buyClose = UsEquitiesSessionCalendar.GetRegularClose(buySession);
        var buyWindowStartEastern = buyClose.AddHours(-1);
        var sellSession = FindNextTradingSession(buySession.AddDays(1));
        var evaluationStartUtc = ToUtc(Eastern, buySession, buyWindowStartEastern);
        var buyStartUtc = evaluationStartUtc;
        var buyDeadlineUtc = ToUtc(Eastern, buySession, buyClose);
        var sellStartUtc = ToUtc(Eastern, sellSession, SellWindowStart);
        var sellEndUtc = ToUtc(Eastern, sellSession, SellWindowEnd);
        var evaluationOpen = nowUtc >= evaluationStartUtc && nowUtc < buyDeadlineUtc;
        var buyWindowOpen = evaluationOpen;

        return new MarketSessionClockSnapshot(
            phase, label, ToUtc(Eastern, phase == "CLOSED" ? nextSession : today, targetTime),
            nowUtc, targetEvent, "America/New_York", UsEquitiesSessionCalendar.Version,
            evaluationStartUtc, buyDeadlineUtc, evaluationOpen, buyStartUtc, buyWindowOpen,
            buySession, sellSession, sellStartUtc, sellEndUtc,
            "Candidate research and purchase: final 60 minutes of the regular session, ending at the regular-session close. Overnight measurement then includes Buy Day after-hours and Sell Day premarket. Next-session GTC_EXT window: 7:00 AM–8:00 PM Eastern. Schedule only; broker eligibility and fills are not verified.");
    }

    private static DateOnly FindNextBuySession(DateOnly today, TimeOnly easternTime)
    {
        var candidate = today;
        for (var i = 0; i < 20; i++, candidate = candidate.AddDays(1))
        {
            if (!UsEquitiesSessionCalendar.IsTradingSession(candidate) ||
                candidate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (candidate == today && easternTime >= UsEquitiesSessionCalendar.GetRegularClose(candidate)) continue;
            return candidate;
        }
        throw new InvalidOperationException("Could not find the next Monday–Thursday Boring Overnight buy session.");
    }

    private static DateOnly FindNextTradingSession(DateOnly afterDate)
    {
        var candidate = afterDate;
        for (var i = 0; i < 15; i++, candidate = candidate.AddDays(1))
            if (UsEquitiesSessionCalendar.IsTradingSession(candidate)) return candidate;
        throw new InvalidOperationException("Could not find the next US-equities sell session.");
    }

    private static DateTimeOffset ToUtc(TimeZoneInfo zone, DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
