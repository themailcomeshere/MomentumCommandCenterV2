using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Builds explicit diagnostic-only Opportunity Quality inputs from completed market bars.
/// The 30-bar range and prior-high proximity are transparent research proxies, not calibrated
/// resistance/volatility predictors. They must not override the V2 eligibility decision.
/// </summary>
public static class OpportunityQualitySnapshotAdapter
{
    public const int RangeLookbackBars = 30;
    public const int ResistanceLookbackBars = 30;

    public static OpportunityQualitySnapshot Create(
        string symbol,
        DateTimeOffset evaluatedAtUtc,
        DateTimeOffset oneMinuteCompletedAtUtc,
        DateTimeOffset fiveMinuteCompletedAtUtc,
        bool existingEntryEligible,
        bool executionEventActive,
        decimal? relativeVolume,
        bool momentumConfirmed,
        decimal? atrExtension,
        decimal? oneMinuteAtr,
        IReadOnlyList<MarketBar> completedOneMinuteBars)
    {
        ArgumentNullException.ThrowIfNull(completedOneMinuteBars);
        var bars = completedOneMinuteBars.OrderBy(x => x.Timestamp).ToArray();

        decimal? recentRangeInAtr = null;
        decimal? resistanceDistanceInAtr = null;
        if (oneMinuteAtr is > 0m && bars.Length >= RangeLookbackBars)
        {
            var rangeBars = bars.TakeLast(RangeLookbackBars).ToArray();
            var range = (decimal)rangeBars.Max(x => x.High) - (decimal)rangeBars.Min(x => x.Low);
            recentRangeInAtr = range / oneMinuteAtr.Value;
        }

        if (oneMinuteAtr is > 0m && bars.Length >= ResistanceLookbackBars + 1)
        {
            var currentClose = (decimal)bars[^1].Close;
            var priorHighs = bars.Take(bars.Length - 1)
                .TakeLast(ResistanceLookbackBars)
                .Select(x => (decimal)x.High)
                .Where(high => high > currentClose)
                .ToArray();
            if (priorHighs.Length > 0)
                resistanceDistanceInAtr = (priorHighs.Min() - currentClose) / oneMinuteAtr.Value;
        }

        var input = new OpportunityQualityInput(
            symbol,
            evaluatedAtUtc,
            oneMinuteCompletedAtUtc,
            fiveMinuteCompletedAtUtc,
            existingEntryEligible,
            executionEventActive,
            relativeVolume,
            momentumConfirmed,
            atrExtension,
            recentRangeInAtr,
            resistanceDistanceInAtr);

        return new OpportunityQualitySnapshot(input, new OpportunityQualityMarketMetrics(
            relativeVolume,
            momentumConfirmed,
            atrExtension,
            oneMinuteAtr,
            recentRangeInAtr,
            resistanceDistanceInAtr,
            RangeLookbackBars,
            ResistanceLookbackBars,
            bars.Length));
    }
}

public sealed record OpportunityQualityMarketMetrics(
    decimal? RelativeVolume,
    bool MomentumConfirmed,
    decimal? AtrExtension,
    decimal? OneMinuteAtr,
    decimal? RecentRangeInAtr,
    decimal? ResistanceDistanceInAtr,
    int RangeLookbackBars,
    int ResistanceLookbackBars,
    int AvailableOneMinuteBars);

public sealed record OpportunityQualitySnapshot(
    OpportunityQualityInput Input,
    OpportunityQualityMarketMetrics Metrics);
