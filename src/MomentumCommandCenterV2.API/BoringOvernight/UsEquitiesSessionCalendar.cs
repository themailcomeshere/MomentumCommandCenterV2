namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Generate US listed-equity regular-session boundaries using an explicit versioned
/// NYSE holiday/early-close ruleset and America/New_York timezone conversion.
/// IMPORTANT: This is an MCC-maintained calendar ruleset, not a live exchange calendar feed.
/// Review its rules against the official exchange calendar when the ruleset version changes.
/// </summary>
public static class UsEquitiesSessionCalendar
{
    public const string Version = "MCC_US_EQUITIES_CALENDAR_V1";
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static IReadOnlyList<OvernightSessionBoundary> BuildRecentBoundaries(
        DateOnly lastIncludedSessionDate,
        int requiredPairs = 20)
    {
        if (requiredPairs is < 5 or > 20)
            throw new ArgumentOutOfRangeException(nameof(requiredPairs), "Required pairs must be 5–20.");

        var sessions = new List<DateOnly>();
        for (var offset = 0; offset <= 65 && sessions.Count < requiredPairs + 1; offset++)
        {
            var date = lastIncludedSessionDate.AddDays(-offset);
            if (IsTradingSession(date)) sessions.Add(date);
        }
        if (sessions.Count < requiredPairs + 1)
            throw new InvalidOperationException("Could not build the required recent session window from the supported calendar rules.");

        sessions.Reverse();
        var boundaries = new List<OvernightSessionBoundary>(requiredPairs);
        for (var i = 1; i < sessions.Count; i++)
        {
            var previous = sessions[i - 1];
            var next = sessions[i];
            var closeTime = GetRegularClose(previous);
            boundaries.Add(new(
                previous,
                next,
                ToUtc(previous, closeTime.AddMinutes(-1)),
                ToUtc(next, new TimeOnly(9, 30))));
        }
        return boundaries;
    }

    public static bool IsTradingSession(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !IsMarketHoliday(date);

    public static TimeOnly GetRegularClose(DateOnly date)
    {
        if (!IsTradingSession(date))
            throw new ArgumentException("Date is not a supported US equities trading session.", nameof(date));
        return IsEarlyClose(date) ? new TimeOnly(13, 0) : new TimeOnly(16, 0);
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = Eastern.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    private static bool IsMarketHoliday(DateOnly date)
    {
        var year = date.Year;
        var newYears = new DateOnly(year, 1, 1);
        // NYSE observes New Year's Day on Monday when January 1 is Sunday.
        if (date == newYears || (newYears.DayOfWeek == DayOfWeek.Sunday && date == newYears.AddDays(1))) return true;
        if (date == NthWeekday(year, 1, DayOfWeek.Monday, 3)) return true; // MLK
        if (date == NthWeekday(year, 2, DayOfWeek.Monday, 3)) return true; // Presidents
        if (date == EasterSunday(year).AddDays(-2)) return true; // Good Friday
        if (date == LastWeekday(year, 5, DayOfWeek.Monday)) return true; // Memorial Day

        if (year >= 2022)
        {
            var juneteenth = new DateOnly(year, 6, 19);
            if (date == ObservedFixedHoliday(juneteenth)) return true;
        }
        var independence = new DateOnly(year, 7, 4);
        if (date == ObservedFixedHoliday(independence)) return true;
        if (date == NthWeekday(year, 9, DayOfWeek.Monday, 1)) return true; // Labor Day
        if (date == NthWeekday(year, 11, DayOfWeek.Thursday, 4)) return true; // Thanksgiving

        var christmas = new DateOnly(year, 12, 25);
        if (date == ObservedFixedHoliday(christmas)) return true;
        return false;
    }

    private static DateOnly ObservedFixedHoliday(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => date.AddDays(-1),
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date
    };

    private static bool IsEarlyClose(DateOnly date)
    {
        var thanksgiving = NthWeekday(date.Year, 11, DayOfWeek.Thursday, 4);
        if (date == thanksgiving.AddDays(1)) return true;
        if (date.Month == 12 && date.Day == 24) return true;
        // NYSE's commonly scheduled pre-Independence early close when July 3 is a session.
        return date.Month == 7 && date.Day == 3;
    }

    private static DateOnly NthWeekday(int year, int month, DayOfWeek day, int occurrence)
    {
        var first = new DateOnly(year, month, 1);
        var delta = ((int)day - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(delta + (occurrence - 1) * 7);
    }

    private static DateOnly LastWeekday(int year, int month, DayOfWeek day)
    {
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var delta = ((int)last.DayOfWeek - (int)day + 7) % 7;
        return last.AddDays(-delta);
    }

    private static DateOnly EasterSunday(int year)
    {
        // Gregorian computus (Meeus/Jones/Butcher).
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
