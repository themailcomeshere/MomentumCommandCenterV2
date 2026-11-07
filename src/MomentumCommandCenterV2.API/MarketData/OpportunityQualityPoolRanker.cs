namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Deterministic diagnostic ordering for an explicitly supplied candidate pool.
/// It uses existing assessor classifications and raw component values; it does not
/// invent a composite score or imply calibrated probability/profitability.
/// </summary>
public static class OpportunityQualityPoolRanker
{
    public const string Version = "MCC-OPPORTUNITY-POOL-ORDER-1.0";

    public static IReadOnlyList<OpportunityQualityPoolRankedCandidate> Rank(
        IReadOnlyCollection<OpportunityQualityPoolCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var normalized = candidates.Select(candidate =>
        {
            if (string.IsNullOrWhiteSpace(candidate.Symbol))
                throw new ArgumentException("Every candidate must have a symbol.", nameof(candidates));
            if (candidate.RelativeVolume is < 0m ||
                candidate.RecentRangeInAtr is < 0m ||
                candidate.ResistanceDistanceInAtr is < 0m)
                throw new ArgumentException($"Candidate {candidate.Symbol} contains a negative diagnostic metric.", nameof(candidates));
            return candidate with { Symbol = candidate.Symbol.Trim().ToUpperInvariant() };
        }).ToArray();

        if (normalized.GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new ArgumentException("Candidate symbols must be unique within a pool.", nameof(candidates));

        return normalized
            .OrderBy(x => Priority(x.Classification, x.Status))
            .ThenByDescending(x => x.RelativeVolume ?? decimal.MinValue)
            .ThenByDescending(x => x.RecentRangeInAtr ?? decimal.MinValue)
            .ThenByDescending(x => x.ResistanceDistanceInAtr ?? decimal.MinValue)
            .ThenBy(x => x.Symbol, StringComparer.Ordinal)
            .Select((candidate, index) => new OpportunityQualityPoolRankedCandidate(
                index + 1,
                candidate.Symbol,
                candidate.Status,
                candidate.Classification,
                candidate.DisplayLabel,
                candidate.RelativeVolume,
                candidate.RecentRangeInAtr,
                candidate.ResistanceDistanceInAtr,
                candidate.EntryAllowed,
                candidate.DataFresh,
                candidate.Reasons))
            .ToArray();
    }

    private static int Priority(string classification, string status)
    {
        if (string.Equals(status, "DATA?", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(classification, "DataUnknown", StringComparison.OrdinalIgnoreCase)) return 6;

        return classification switch
        {
            "ReadyHighOpportunity" => 0,
            "ReadyModerate" => 1,
            "ReadyBoring" => 2,
            "ReadyExtended" => 3,
            "WatchDeveloping" => 4,
            "Blocked" => 5,
            _ => 6
        };
    }
}

public sealed record OpportunityQualityPoolCandidate(
    string Symbol,
    string Status,
    string Classification,
    string DisplayLabel,
    decimal? RelativeVolume,
    decimal? RecentRangeInAtr,
    decimal? ResistanceDistanceInAtr,
    bool? EntryAllowed,
    bool DataFresh,
    IReadOnlyList<string> Reasons);

public sealed record OpportunityQualityPoolRankedCandidate(
    int Rank,
    string Symbol,
    string Status,
    string Classification,
    string DisplayLabel,
    decimal? RelativeVolume,
    decimal? RecentRangeInAtr,
    decimal? ResistanceDistanceInAtr,
    bool? EntryAllowed,
    bool DataFresh,
    IReadOnlyList<string> Reasons);
