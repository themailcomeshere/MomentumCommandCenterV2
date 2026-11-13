using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// Full-pool deterministic fixtures: each synthetic candidate passes through the real
/// snapshot adapter → assessor → live-assessment JSON shape → pool mapper → ranker path used by
/// GET /api/market-data/opportunity-quality/rank. All symbols and values are test-only fixtures.
/// </summary>
public sealed class OpportunityQualityFullPoolFixtureTests
{
    private static readonly DateTimeOffset EvaluatedAt = DateTimeOffset.Parse("2026-11-13T15:00:30Z");
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed record Fixture(
        string Symbol,
        decimal? RelativeVolume,
        decimal RangeAtr,
        decimal Extension,
        int BarCount = 31,
        bool Eligible = true,
        bool ExecutionActive = true,
        bool Momentum = true,
        int OneMinuteAgeSeconds = 30,
        bool Excluded = false);

    private static readonly Fixture[] Pool =
    {
        new("HIGHA", 3.0m, 3m, 0.5m),
        new("HIGHB", 2.6m, 3m, 0.5m),
        new("MODER", 2.0m, 3m, 0.5m),
        new("BORNG", 1.2m, 3m, 0.5m),
        new("EXTND", 3.0m, 3m, 2.0m),
        new("WATCH", 3.0m, 3m, 0.5m, ExecutionActive: false),
        new("BLOCK", 2.0m, 3m, 0.5m, Eligible: false),
        new("EXCLD", 5.0m, 3m, 0.5m, Excluded: true),
        new("INCMP", 2.0m, 3m, 0.5m, BarCount: 10),
        new("STALE", 2.0m, 3m, 0.5m, OneMinuteAgeSeconds: 600),
    };

    private static OpportunityQualityPoolCandidate Candidate(Fixture f)
    {
        // Rising closes with high == low == close: the 30-bar range equals RangeAtr × ATR(1.0)
        // and the latest close is the high, so no prior-high resistance exists.
        var start = EvaluatedAt.AddMinutes(-f.BarCount - 1);
        var bars = Enumerable.Range(0, f.BarCount).Select(i =>
        {
            var close = 100d + (double)f.RangeAtr * i / Math.Max(1, Math.Min(f.BarCount, 30) - 1);
            return new MarketBar(f.Symbol, start.AddMinutes(i), close, close, close, close, 1000, "Alpaca", "iex", "1m");
        }).ToArray();
        var oneMinuteTs = EvaluatedAt.AddSeconds(-f.OneMinuteAgeSeconds);
        var fiveMinuteTs = EvaluatedAt.AddSeconds(-120);
        var snapshot = OpportunityQualitySnapshotAdapter.Create(
            f.Symbol, EvaluatedAt, oneMinuteTs, fiveMinuteTs, f.Eligible, f.ExecutionActive,
            f.RelativeVolume, f.Momentum, f.Extension, 1.0m, bars);
        var assessment = new OpportunityQualityAssessor().Evaluate(snapshot.Input);

        // Same field names as LiveSymbolAssessmentService's response.
        var json = JsonSerializer.SerializeToElement(new
        {
            provider = "Alpaca",
            feed = "iex",
            evaluatedAtUtc = EvaluatedAt,
            dataFresh = f.OneMinuteAgeSeconds <= 120,
            freshnessDiagnostics = new
            {
                oneMinute = new { sourceTimestampUtc = oneMinuteTs },
                fiveMinute = new { sourceTimestampUtc = fiveMinuteTs }
            },
            opportunityQuality = new
            {
                status = assessment.Classification == OpportunityClassification.DataUnknown ? "DATA?" : "DIAGNOSTIC_ONLY",
                classification = assessment.Classification.ToString(),
                displayLabel = assessment.DisplayLabel,
                reasons = assessment.Reasons.Select(x => x.ToString()).ToArray(),
                metrics = snapshot.Metrics
            },
            decision = new { entryAllowed = f.Eligible }
        }, Web);
        return OpportunityQualityPoolCandidateMapper.FromAssessment(f.Symbol, json, f.Excluded);
    }

    private static OpportunityQualityPoolCandidate[] BuildPool() =>
        Pool.Select(Candidate)
            .Append(OpportunityQualityPoolCandidateMapper.FromFailure("FAILD", new HttpRequestException("fixture"), false, "Alpaca"))
            .ToArray();

    [Fact]
    public void MixedPoolRanksEveryCandidateInDisclosedOrder()
    {
        var ranked = OpportunityQualityPoolRanker.Rank(BuildPool());

        Assert.Equal(
            new[] { "HIGHA", "HIGHB", "MODER", "BORNG", "EXTND", "WATCH", "EXCLD", "BLOCK", "STALE", "INCMP", "FAILD" },
            ranked.Select(x => x.Symbol));
        Assert.Equal(Enumerable.Range(1, 11), ranked.Select(x => x.Rank));
    }

    [Fact]
    public void ClassificationsMatchTheAssessorGates()
    {
        var bySymbol = OpportunityQualityPoolRanker.Rank(BuildPool()).ToDictionary(x => x.Symbol);

        Assert.Equal("ReadyHighOpportunity", bySymbol["HIGHA"].Classification);
        Assert.Equal("ReadyModerate", bySymbol["MODER"].Classification);
        Assert.Equal("ReadyBoring", bySymbol["BORNG"].Classification);
        Assert.Equal("ReadyExtended", bySymbol["EXTND"].Classification);
        Assert.Equal("WatchDeveloping", bySymbol["WATCH"].Classification);
        Assert.Equal("Blocked", bySymbol["BLOCK"].Classification);
        Assert.Contains("EntryNotEligible", bySymbol["BLOCK"].Reasons);
    }

    [Fact]
    public void IncompleteStaleAndFailedCandidatesAreDataUnknownAndNeverPromoted()
    {
        var bySymbol = OpportunityQualityPoolRanker.Rank(BuildPool()).ToDictionary(x => x.Symbol);

        Assert.All(new[] { "INCMP", "STALE", "FAILD" }, symbol =>
        {
            Assert.Equal("DATA?", bySymbol[symbol].Status);
            Assert.Equal("DataUnknown", bySymbol[symbol].Classification);
        });
        Assert.Contains("DataMissing", bySymbol["INCMP"].Reasons);
        Assert.Null(bySymbol["INCMP"].RecentRangeInAtr);
        Assert.Contains("DataStale", bySymbol["STALE"].Reasons);
        Assert.False(bySymbol["STALE"].DataFresh);
        Assert.Contains("ASSESSMENT_FAILED", bySymbol["FAILD"].Reasons);
    }

    [Fact]
    public void ExcludedCandidateIsHardBlockedEvenWithTheStrongestMetrics()
    {
        var excluded = OpportunityQualityPoolRanker.Rank(BuildPool()).Single(x => x.Symbol == "EXCLD");

        Assert.True(excluded.Excluded);
        Assert.Equal("BLOCKED", excluded.Status);
        Assert.Equal("Blocked", excluded.Classification);
        Assert.Equal("QUALITY — EXCLUDED", excluded.DisplayLabel);
        Assert.Equal(OpportunityQualityPoolCandidateMapper.ExcludedReason, excluded.Reasons[0]);
        Assert.Equal(5.0m, excluded.RelativeVolume);
    }

    [Fact]
    public void ExcludedFailureIsBlockedRatherThanDataUnknown()
    {
        var candidate = OpportunityQualityPoolCandidateMapper.FromFailure("EXFAIL", new TaskCanceledException(), true, "Alpaca");

        Assert.Equal("BLOCKED", candidate.Status);
        Assert.Equal(new[] { "EXCLUDED_WATCHLIST", "ASSESSMENT_FAILED", "TaskCanceledException" }, candidate.Reasons);
    }

    [Fact]
    public void RankingIsDeterministicRegardlessOfInputOrder()
    {
        var baseline = OpportunityQualityPoolRanker.Rank(BuildPool()).Select(x => x.Symbol).ToArray();
        var random = new Random(20261113);
        for (var i = 0; i < 20; i++)
        {
            var shuffled = BuildPool().OrderBy(_ => random.Next()).ToArray();
            Assert.Equal(baseline, OpportunityQualityPoolRanker.Rank(shuffled).Select(x => x.Symbol));
        }
    }

    [Fact]
    public void EveryAssessedRowCarriesPerSymbolProvenance()
    {
        var ranked = OpportunityQualityPoolRanker.Rank(BuildPool());

        Assert.All(ranked.Where(x => x.Symbol != "FAILD"), row =>
        {
            Assert.Equal("Alpaca", row.Provider);
            Assert.Equal("iex", row.Feed);
            Assert.NotNull(row.OneMinuteSourceTimestampUtc);
            Assert.NotNull(row.FiveMinuteSourceTimestampUtc);
            Assert.Equal(EvaluatedAt, row.EvaluatedAtUtc);
        });
        Assert.Equal(EvaluatedAt.AddSeconds(-600), ranked.Single(x => x.Symbol == "STALE").OneMinuteSourceTimestampUtc);
        var failed = ranked.Single(x => x.Symbol == "FAILD");
        Assert.Equal("Alpaca", failed.Provider);
        Assert.Null(failed.Feed);
        Assert.Null(failed.OneMinuteSourceTimestampUtc);
    }

    [Fact]
    public void MissingOpportunityQualityObjectMapsToDataUnknown()
    {
        var json = JsonSerializer.SerializeToElement(new { provider = "Alpaca", feed = "iex" }, Web);

        var candidate = OpportunityQualityPoolCandidateMapper.FromAssessment("NOOQ", json, false);

        Assert.Equal("DATA?", candidate.Status);
        Assert.Equal("DataUnknown", candidate.Classification);
        Assert.Null(candidate.RelativeVolume);
        Assert.Null(candidate.EntryAllowed);
    }
}
