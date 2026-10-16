using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class OpportunityQualityAssessorTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-16T10:00:00-07:00");

    private static OpportunityQualityInput Input(
        bool eligible = true,
        bool active = true,
        decimal? rvol = 3m,
        bool momentum = true,
        decimal? extension = 0.5m,
        decimal? rangeAtr = 1.5m,
        decimal? resistanceAtr = 2m,
        DateTimeOffset? oneTimestamp = null,
        DateTimeOffset? fiveTimestamp = null) =>
        new(
            "TEST",
            Now,
            oneTimestamp ?? Now.AddMinutes(-1),
            fiveTimestamp ?? Now.AddMinutes(-5),
            eligible,
            active,
            rvol,
            momentum,
            extension,
            rangeAtr,
            resistanceAtr);

    [Fact]
    public void StrongEligibleFreshCandidateIsHighOpportunity()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input());

        Assert.Equal(OpportunityClassification.ReadyHighOpportunity, result.Classification);
        Assert.Equal("READY — HIGH", result.DisplayLabel);
        Assert.Contains(OpportunityReason.StrongParticipation, result.Reasons);
    }

    [Fact]
    public void FailedEligibilityDominatesStrongQualityMetrics()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(eligible: false));

        Assert.Equal(OpportunityClassification.Blocked, result.Classification);
        Assert.Contains(OpportunityReason.EntryNotEligible, result.Reasons);
        Assert.DoesNotContain(OpportunityReason.StrongParticipation, result.Reasons);
    }

    [Fact]
    public void InactiveExecutionEventIsWatchNotReady()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(active: false));

        Assert.Equal(OpportunityClassification.WatchDeveloping, result.Classification);
        Assert.Contains(OpportunityReason.ExecutionEventInactive, result.Reasons);
    }

    [Fact]
    public void MissingRequiredMetricProducesDataUnknown()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(rangeAtr: null));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Equal("DATA?", result.DisplayLabel);
        Assert.Contains(OpportunityReason.DataMissing, result.Reasons);
    }

    [Fact]
    public void StaleFiveMinuteSnapshotProducesDataUnknown()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(fiveTimestamp: Now.AddMinutes(-7)));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Contains(OpportunityReason.DataStale, result.Reasons);
    }

    [Fact]
    public void ExtendedEntryDowngradesOtherwiseStrongCandidate()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(extension: 1.5m));

        Assert.Equal(OpportunityClassification.ReadyExtended, result.Classification);
        Assert.Contains(OpportunityReason.EntryExtended, result.Reasons);
    }

    [Theory]
    [InlineData(1.2, true, 1.5)]
    [InlineData(3.0, false, 1.5)]
    [InlineData(3.0, true, 0.5)]
    public void WeakParticipationMomentumOrRangeIsReadyBoring(
        decimal rvol,
        bool momentum,
        decimal rangeAtr)
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(rvol: rvol, momentum: momentum, rangeAtr: rangeAtr));

        Assert.Equal(OpportunityClassification.ReadyBoring, result.Classification);
    }

    [Fact]
    public void ModerateParticipationWithHealthyMomentumIsModerate()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(rvol: 2m));

        Assert.Equal(OpportunityClassification.ReadyModerate, result.Classification);
    }

    [Fact]
    public void ResistanceTooCloseIsEntryTimingWarning()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(resistanceAtr: 0.25m));

        Assert.Equal(OpportunityClassification.ReadyExtended, result.Classification);
        Assert.Contains(OpportunityReason.ResistanceTooClose, result.Reasons);
    }

    [Fact]
    public void FutureDatedMarketSnapshotIsNotFresh()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(oneTimestamp: Now.AddSeconds(1)));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Contains(OpportunityReason.DataStale, result.Reasons);
    }
}
