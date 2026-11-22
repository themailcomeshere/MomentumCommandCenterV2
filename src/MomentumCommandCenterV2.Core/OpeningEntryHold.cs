namespace MomentumCommandCenterV2.Core;

/// <summary>
/// TYPE: OpeningEntryHoldStatus
/// PURPOSE: Where an instant sits relative to the opening entry hold: the configured minutes, whether the hold is on,
/// whether the instant is inside it, the 09:30 ET open and release instants for that Eastern date (UTC), the whole
/// minutes left (rounded up; 0 when not active) and the Eastern calendar date the window belongs to.
/// WHEN CALLED: Returned by <see cref="OpeningEntryHold.Evaluate"/>.
/// </summary>
public sealed record OpeningEntryHoldStatus(
    int Minutes,
    bool Enabled,
    bool Active,
    DateTimeOffset RegularOpenUtc,
    DateTimeOffset ReleaseUtc,
    int MinutesRemaining,
    DateOnly EasternDate);

/// <summary>
/// TYPE: OpeningEntryHold
/// PURPOSE: Intraday Momentum opening entry hold (operator request 2026-11-21). For the first N minutes after the
/// 09:30 ET regular open, a fresh entry (EXECUTION READY) is withheld and shown as PREPARE TO BUY with an OPENING HOLD
/// reason. Exits and held-position states are never delayed. N = 0 turns the hold off.
/// The open is 09:30 ET on every US-equities trading day, early-close days included, so only the Eastern clock time is
/// needed here. This type does not know the holiday calendar: callers decide whether the date is a trading session
/// (the V2 engine only runs inside the regular session).
/// WHEN CALLED: By <see cref="V2SignalEngine.Evaluate"/> before returning a fresh Buy, and by the API to report the
/// hold window to the Dashboard.
/// </summary>
public static class OpeningEntryHold
{
    /// <summary>Largest accepted hold, in minutes (the first two hours of the session).</summary>
    public const int MaxMinutes = 120;

    /// <summary>Rules version shown wherever the hold is reported.</summary>
    public const string RulesVersion = "MCC-OPENING-HOLD-1";

    private static readonly TimeSpan RegularOpenEastern = new(9, 30, 0);
    private static readonly TimeZoneInfo Eastern = ResolveEastern();

    /// <summary>
    /// PURPOSE: True when minutes is within 0..<see cref="MaxMinutes"/>.
    /// WHEN CALLED: By the engine constructor, the settings store and the settings endpoint.
    /// </summary>
    public static bool IsValidMinutes(int minutes) => minutes is >= 0 and <= MaxMinutes;

    /// <summary>
    /// PURPOSE: Computes the hold window for the Eastern date of the timestamp and whether the timestamp is inside
    /// [09:30 ET, 09:30 ET + minutes). Throws when minutes is outside 0..<see cref="MaxMinutes"/>.
    /// WHEN CALLED: Per engine evaluation and per settings/assessment response.
    /// </summary>
    public static OpeningEntryHoldStatus Evaluate(DateTimeOffset timestamp, int minutes)
    {
        if (!IsValidMinutes(minutes))
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, $"Opening hold must be 0–{MaxMinutes} minutes.");

        var eastern = TimeZoneInfo.ConvertTime(timestamp, Eastern);
        var openLocal = DateTime.SpecifyKind(eastern.Date + RegularOpenEastern, DateTimeKind.Unspecified);
        var openUtc = new DateTimeOffset(openLocal, Eastern.GetUtcOffset(openLocal)).ToUniversalTime();
        var releaseUtc = openUtc.AddMinutes(minutes);
        var active = minutes > 0 && timestamp >= openUtc && timestamp < releaseUtc;
        var remaining = active ? (int)Math.Ceiling((releaseUtc - timestamp).TotalMinutes) : 0;
        return new OpeningEntryHoldStatus(minutes, minutes > 0, active, openUtc, releaseUtc, remaining,
            DateOnly.FromDateTime(eastern.DateTime));
    }

    /// <summary>
    /// PURPOSE: "10:30 ET (08:30 AZ)" for an instant, used in decision reasons. Arizona has no DST, so the AZ offset
    /// from Eastern is 2 h in standard time and 3 h in daylight time; both are converted, never assumed.
    /// WHEN CALLED: By <see cref="V2SignalEngine.Evaluate"/> when it withholds an entry.
    /// </summary>
    public static string FormatEasternAndArizona(DateTimeOffset instant)
    {
        var et = TimeZoneInfo.ConvertTime(instant, Eastern);
        var az = TimeZoneInfo.ConvertTime(instant, ResolvePhoenix());
        return $"{et:HH:mm} ET ({az:HH:mm} AZ)";
    }

    private static TimeZoneInfo ResolveEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }

    private static TimeZoneInfo ResolvePhoenix()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Phoenix"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("US Mountain Standard Time"); }
    }
}
