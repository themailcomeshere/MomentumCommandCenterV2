using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Re-evaluates a historical point-in-time snapshot from persisted raw OHLCV using the same V2 engine.
/// WHEN CALLED: POST /api/market-data/replay.
/// SAFETY: Every input bar is bounded by its completed timestamp at or before AsOfUtc; future observations are excluded.
/// </summary>
public sealed class HistoricalDecisionReplayService(IMarketBarObservationStore store)
{
    public async Task<object> ReplayAsync(
        string symbol,
        string provider,
        string? feed,
        DateTimeOffset asOfUtc,
        bool hasPosition,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        if (asOfUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("asOfUtc must be an ISO-8601 UTC timestamp ending in Z.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("provider is required.");
        if (string.IsNullOrWhiteSpace(feed))
            throw new ArgumentException("feed is required so replay never mixes IEX, SIP, or other source observations.");

        var oneObservations = await store.GetLatestAsync(
            normalizedSymbol, provider, asOfUtc.AddDays(-3), asOfUtc, 10000, cancellationToken, "1m");
        var fiveObservations = await store.GetLatestAsync(
            normalizedSymbol, provider, asOfUtc.AddDays(-21), asOfUtc, 10000, cancellationToken, "5m");

        var oneBars = oneObservations
            .Where(x => string.Equals(x.Bar.Feed, feed, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Bar)
            .Where(x => x.Timestamp.AddMinutes(1) <= asOfUtc)
            .OrderBy(x => x.Timestamp)
            .GroupBy(x => x.Timestamp).Select(x => x.Last()).ToArray();
        var fiveBars = fiveObservations
            .Where(x => feed is null || string.Equals(x.Bar.Feed, feed, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Bar)
            .Where(x => x.Timestamp.AddMinutes(5) <= asOfUtc)
            .OrderBy(x => x.Timestamp)
            .GroupBy(x => x.Timestamp).Select(x => x.Last()).ToArray();

        if (oneBars.Length < 50 || fiveBars.Length < 51)
        {
            return new
            {
                symbol = normalizedSymbol,
                provider,
                feed,
                asOfUtc,
                status = "INSUFFICIENT_STORED_HISTORY",
                oneMinuteBars = oneBars.Length,
                fiveMinuteBars = fiveBars.Length,
                requiredOneMinuteBars = 50,
                requiredFiveMinuteBars = 51,
                decision = (object?)null,
                note = "Replay uses only observations already persisted by this MCC API instance. Missing history is not fetched or fabricated."
            };
        }

        var rvol = LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveBars, fiveBars.Length - 1);
        var previousRvol = LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveBars, fiveBars.Length - 2);
        if (rvol.Value is null || previousRvol.Value is null)
        {
            return new
            {
                symbol = normalizedSymbol,
                provider,
                feed,
                asOfUtc,
                status = "RVOL_BASELINE_INSUFFICIENT",
                oneMinuteBars = oneBars.Length,
                fiveMinuteBars = fiveBars.Length,
                currentRvolSamples = rvol.Samples,
                previousRvolSamples = previousRvol.Samples,
                decision = (object?)null,
                note = "At least five prior same-clock-slot session observations are required for both current and previous 5M RVOL inputs."
            };
        }

        var one = LiveSymbolTechnicalCalculator.Calculate(oneBars, oneBars.Length - 1, TimeSpan.FromMinutes(1), 0m);
        var five = LiveSymbolTechnicalCalculator.Calculate(fiveBars, fiveBars.Length - 1, TimeSpan.FromMinutes(5), rvol.Value.Value);
        var previousFive = LiveSymbolTechnicalCalculator.Calculate(fiveBars, fiveBars.Length - 2, TimeSpan.FromMinutes(5), previousRvol.Value.Value);

        if (!LiveSymbolTechnicalCalculator.IsRegularSession(asOfUtc))
        {
            return new
            {
                symbol = normalizedSymbol,
                provider,
                feed,
                asOfUtc,
                status = "OUTSIDE_REGULAR_SESSION",
                oneMinuteSourceTimestampUtc = oneBars[^1].Timestamp,
                fiveMinuteSourceTimestampUtc = fiveBars[^1].Timestamp,
                decision = (object?)null,
                note = "The V2 intraday engine is only evaluated for regular-session timestamps. No decision was manufactured for an out-of-session replay point."
            };
        }

        var snapshot = new CommandCenterSnapshot(
            asOfUtc, normalizedSymbol, one, five, MarketPhase.RegularSession, previousFive);
        var result = new V2SignalEngine().Evaluate(snapshot, hasPosition);
        return new
        {
            symbol = normalizedSymbol,
            provider,
            feed,
            asOfUtc,
            status = "REPLAYED",
            calculationVersion = "V2SignalEngine-default-config",
            hasPosition,
            oneMinuteBars = oneBars.Length,
            fiveMinuteBars = fiveBars.Length,
            oneMinuteSourceTimestampUtc = oneBars[^1].Timestamp,
            fiveMinuteSourceTimestampUtc = fiveBars[^1].Timestamp,
            previousFiveMinuteSourceTimestampUtc = fiveBars[^2].Timestamp,
            decision = new
            {
                state = result.State.ToString(),
                action = result.Action.ToString(),
                score = result.Score,
                longPermission = result.LongPermission,
                entryAllowed = result.EntryAllowed,
                runnerAllowed = result.RunnerAllowed,
                exitWarning = result.ExitWarning,
                hardExit = result.HardExit,
                reason = result.Reason,
                evaluation = result.Evaluation
            },
            noLookahead = new
            {
                asOfUtc,
                latestAllowedOneMinuteCloseUtc = asOfUtc,
                latestAllowedFiveMinuteCloseUtc = asOfUtc,
                futureBarsExcluded = true
            },
            note = "Deterministic point-in-time replay from the latest persisted observation per source timestamp. It uses the current V2 engine/configuration version, not a historical configuration archive. No orders are submitted."
        };
    }
}
