using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class OpportunityQualityPoolRankerTests
{
    [Fact]
    public void RanksEverySuppliedCandidateByClassificationThenRawMetrics()
    {
        var ranked = OpportunityQualityPoolRanker.Rank(new[]
        {
            Candidate("DATA", "DATA?", "DataUnknown", null, null),
            Candidate("BORING", "DIAGNOSTIC_ONLY", "ReadyBoring", 1.2m, 2m),
            Candidate("MODERATE", "DIAGNOSTIC_ONLY", "ReadyModerate", 1.1m, 2m),
            Candidate("HIGH2", "DIAGNOSTIC_ONLY", "ReadyHighOpportunity", 2.2m, 4m),
            Candidate("HIGH1", "DIAGNOSTIC_ONLY", "ReadyHighOpportunity", 1.5m, 3m),
            Candidate("BLOCKED", "DIAGNOSTIC_ONLY", "Blocked", 3m, 5m)
        });

        Assert.Equal(6, ranked.Count);
        Assert.Equal(new[] { "HIGH2", "HIGH1", "MODERATE", "BORING", "BLOCKED", "DATA" },
            ranked.Select(x => x.Symbol));
        Assert.Equal(Enumerable.Range(1, 6), ranked.Select(x => x.Rank));
    }

    [Fact]
    public void RejectsDuplicateSymbolsAndNegativeMetrics()
    {
        Assert.Throws<ArgumentException>(() => OpportunityQualityPoolRanker.Rank(new[]
        {
            Candidate("EVR", "DATA?", "DataUnknown", null, null),
            Candidate("evr", "DATA?", "DataUnknown", null, null)
        }));
        Assert.Throws<ArgumentException>(() => OpportunityQualityPoolRanker.Rank(new[]
        {
            Candidate("EVR", "DIAGNOSTIC_ONLY", "ReadyModerate", -0.1m, 1m)
        }));
    }

    private static OpportunityQualityPoolCandidate Candidate(
        string symbol, string status, string classification, decimal? rvol, decimal? range) =>
        new(symbol, status, classification, classification, rvol, range, null,
            null, status != "DATA?", Array.Empty<string>());
}
