using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Return live US-equities session and Boring Overnight process countdowns.
/// CALENDAR: Uses the versioned MCC US-equities calendar and America/New_York.
/// OVERNIGHT: Candidate evaluation opens 60 minutes before the regular close; buying must
/// be completed before that same session close. The schedule follows early closes.
/// LIMITATION: Exceptional unscheduled exchange closures are not represented by the maintained ruleset.
/// </summary>
public sealed record MarketSessionClockSnapshot(
    string Phase,
    string Label,
    DateTimeOffset TargetUtc,
    DateTimeOffset GeneratedAtUtc,
    string TargetEvent,
    string ExchangeTimeZone,
    string CalendarVersion,
    DateTimeOffset OvernightEvaluationStartUtc,
    DateTimeOffset OvernightBuyDeadlineUtc,
    bool OvernightEvaluationOpen);

public static class MarketSessionClock
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeOnly PreMarketStart = new(4, 0);
    private static readonly TimeOnly RegularOpen = new(9, 30);
    private static readonly TimeOnly AfterHoursEnd = new(20, 0);
    private static readonly TimeSpan OvernightEvaluationLead = TimeSpan.FromMinutes(60);

    public static MarketSessionClockSnapshot GetSnapshot(DateTimeOffset nowUtc)
    {
        var easternNow = TimeZoneInfo.ConvertTime(nowUtc, Eastern);
        var today = DateOnly.FromDateTime(easternNow.DateTime);
        var localTime = TimeOnly.FromDateTime(easternNow.DateTime);
        var todayIsSession = UsEquitiesSessionCalendar.IsTradingSession(today);
        var todayClose = todayIsSession ? UsEquitiesSessionCalendar.GetRegularClose(today) : (TimeOnly?)null;

        string phase;
        string label;
        TimeOnly targetTime;
        string targetEvent;

        if (todayClose is { } close && localTime >= RegularOpen && localTime < close)
        {
            phase = "REGULAR";
            label = "Remaining time in regular market";
            targetTime = close;
            targetEvent = "REGULAR_SESSION_CLOSE";
        }
        else if (todayClose is { } afterHoursClose && localTime >= afterHoursClose && localTime < AfterHoursEnd)
        {
            phase = "AFTER_HOURS";
            label = "Remaining time in After-Hours";
            targetTime = AfterHoursEnd;
            targetEvent = "AFTER_HOURS_END";
        }
        else if (todayClose is not null && localTime >= PreMarketStart && localTime < RegularOpen)
        {
            phase = "PRE_MARKET";
            label = "Until Market opens";
            targetTime = RegularOpen;
            targetEvent = "REGULAR_SESSION_OPEN";
        }
        else
        {
            phase = "CLOSED";
            label = "Until Market opens";
            targetTime = RegularOpen;
            targetEvent = "NEXT_REGULAR_SESSION_OPEN";
        }

        var nextSession = todayClose is not null && localTime < todayClose.Value
            ? today
            : today.AddDays(1);
        for (var i = 0; i < 15 && !UsEquitiesSessionCalendar.IsTradingSession(nextSession); i++)
            nextSession = nextSession.AddDays(1);

        if (!UsEquitiesSessionCalendar.IsTradingSession(nextSession))
            throw new InvalidOperationException("Could not find the next supported US-equities trading session.");

        var regularClose = UsEquitiesSessionCalendar.GetRegularClose(nextSession);
        var evaluationStart = regularClose.AddMinutes(-OvernightEvaluationLead.TotalMinutes);
        var evaluationOpen = nextSession == today &&
            localTime >= evaluationStart &&
            localTime < regularClose;

        return new MarketSessionClockSnapshot(
            phase,
            label,
            ToUtc(todayForPhase(phase, today, nextSession), targetTime),
            nowUtc.ToUniversalTime(),
            targetEvent,
            "America/New_York",
            UsEquitiesSessionCalendar.Version,
            ToUtc(nextSession, evaluationStart),
            ToUtc(nextSession, regularClose),
            evaluationOpen);
    }

    private static DateOnly todayForPhase(string phase, DateOnly today, DateOnly nextSession) =>
        phase == "CLOSED" ? nextSession : today;

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }
}
