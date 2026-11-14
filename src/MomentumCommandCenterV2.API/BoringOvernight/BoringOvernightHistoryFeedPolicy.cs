using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// TYPE: BoringOvernightHistoryFeedPolicy
/// PURPOSE: Decides the feeds for POST /api/boring-overnight/capture-history (operator decision 2026-11-14).
/// When the running Alpaca feed is not SIP and delayed historical SIP minutes are allowed, the 20-pair close/open
/// history is fetched on SIP with every request ending at least 16 minutes before now; the current interval is judged
/// as of that cutoff; live 1m/5m technical inputs stay on the configured feed. Otherwise everything uses the
/// configured feed, as before.
/// WHEN CALLED: At the start of each capture-history request; also by unit tests.
/// </summary>
public static class BoringOvernightHistoryFeedPolicy
{
    /// <summary>
    /// METHOD: Resolve
    /// PURPOSE: Returns the history mode, history as-of time, maximum request end and the expected technical feed.
    /// WHEN CALLED: Once per capture.
    /// </summary>
    public static HistoryFeedDecision Resolve(string configuredFeed, bool delayedSipAllowed, DateTimeOffset nowUtc)
    {
        var useSip = delayedSipAllowed && !string.Equals(configuredFeed?.Trim(), "sip", StringComparison.OrdinalIgnoreCase);
        var asOf = useSip ? nowUtc - AlpacaDailyMarketDataClient.SipDelay : nowUtc;
        return new HistoryFeedDecision(
            useSip,
            useSip ? "DELAYED_HISTORICAL_SIP" : "CONFIGURED_FEED",
            useSip ? "sip" : null,
            asOf,
            useSip ? asOf : nowUtc.AddMinutes(1),
            useSip ? configuredFeed!.Trim().ToLowerInvariant() : null);
    }
}

/// <summary>
/// TYPE: HistoryFeedDecision
/// PURPOSE: Feed plan for one capture. HistoryFeed/TechnicalFeed null mean "use the feed the provider returns" (old behaviour).
/// WHEN CALLED: Returned by BoringOvernightHistoryFeedPolicy.Resolve.
/// </summary>
public sealed record HistoryFeedDecision(
    bool UseDelayedSip,
    string Mode,
    string? HistoryFeed,
    DateTimeOffset HistoryAsOfUtc,
    DateTimeOffset MaxRequestEndUtc,
    string? TechnicalFeed);
