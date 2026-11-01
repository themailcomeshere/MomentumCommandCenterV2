using MomentumCommandCenterV2.API.BoringOvernight;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Return a live countdown to the next regular-session transition.
/// CALENDAR: Uses the versioned MCC US-equities calendar and America/New_York.
/// LIMITATION: Exceptional unscheduled exchange closures are not represented by the maintained ruleset.
/// </summary>
public sealed record MarketSessionClockSnapshot(
    string Phase,
    string Label,
    DateTimeOffset TargetUtc,
    DateTimeOffset GeneratedAtUtc,
    string TargetEvent,
    string ExchangeTimeZone,
    string CalendarVersion);

public static class MarketSessionClock
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeOnly PreMarketStart = new(4, 0);
    private static readonly TimeOnly RegularOpen = new(9, 30);
    private static readonly TimeOnly AfterHoursEnd = new(20, 0);

    public static MarketSessionClockSnapshot GetSnapshot(DateTimeOffset nowUtc)
    {
        var easternNow = TimeZoneInfo.ConvertTime(nowUtc, Eastern);
        var today = DateOnly.FromDateTime(easternNow.DateTime);
        var localTime = TimeOnly.FromDateTime(easternNow.DateTime);

        if (UsEquitiesSessionCalendar.IsTradingSession(today))
        {
            var close = UsEquitiesSessionCalendar.GetRegularClose(today);
            if (localTime >= RegularOpen && localTime < close)
                return Create("REGULAR", "Remaining time in regular market", today, close, "REGULAR_SESSION_CLOSE", nowUtc);

            if (localTime >= close && localTime < AfterHoursEnd)
                return Create("AFTER_HOURS", "Remaining time in After-Hours", today, AfterHoursEnd, "AFTER_HOURS_END", nowUtc);

            if (localTime >= PreMarketStart && localTime < RegularOpen)
                return Create("PRE_MARKET", "Until Market opens", today, RegularOpen, "REGULAR_SESSION_OPEN", nowUtc);
        }

        var nextSession = localTime < PreMarketStart && UsEquitiesSessionCalendar.IsTradingSession(today)
            ? today
            : today.AddDays(1);
        for (var i = 0; i < 15 && !UsEquitiesSessionCalendar.IsTradingSession(nextSession); i++)
            nextSession = nextSession.AddDays(1);

        if (!UsEquitiesSessionCalendar.IsTradingSession(nextSession))
            throw new InvalidOperationException("Could not find the next supported US-equities trading session.");

        return Create("CLOSED", "Until Market opens", nextSession, RegularOpen, "NEXT_REGULAR_SESSION_OPEN", nowUtc);
    }

    private static MarketSessionClockSnapshot Create(
        string phase, string label, DateOnly date, TimeOnly time, string targetEvent, DateTimeOffset nowUtc)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        var targetUtc = new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
        return new MarketSessionClockSnapshot(
            phase, label, targetUtc, nowUtc.ToUniversalTime(), targetEvent,
            "America/New_York", UsEquitiesSessionCalendar.Version);
    }
}
