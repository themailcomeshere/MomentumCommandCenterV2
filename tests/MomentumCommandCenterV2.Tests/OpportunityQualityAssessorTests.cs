using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OpportunityQualityAssessorTests
/// PURPOSE: Defines the class test or data contract named OpportunityQualityAssessorTests.
/// WHEN CALLED: Used by the test runner or owning workflow when this type is needed.
/// </summary>
public sealed class OpportunityQualityAssessorTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-16T10:00:00-07:00");

    /// <summary>
    /// METHOD: Input
    /// PURPOSE: Verifies the expected behavior of Input.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
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
    /// <summary>
    /// METHOD: StrongEligibleFreshCandidateIsHighOpportunity
    /// PURPOSE: Verifies the expected behavior of StrongEligibleFreshCandidateIsHighOpportunity.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void StrongEligibleFreshCandidateIsHighOpportunity()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input());

        Assert.Equal(OpportunityClassification.ReadyHighOpportunity, result.Classification);
        Assert.Equal("QUALITY — HIGH", result.DisplayLabel);
        Assert.Contains(OpportunityReason.StrongParticipation, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: FailedEligibilityDominatesStrongQualityMetrics
    /// PURPOSE: Verifies the expected behavior of FailedEligibilityDominatesStrongQualityMetrics.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void FailedEligibilityDominatesStrongQualityMetrics()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(eligible: false));

        Assert.Equal(OpportunityClassification.Blocked, result.Classification);
        Assert.Contains(OpportunityReason.EntryNotEligible, result.Reasons);
        Assert.DoesNotContain(OpportunityReason.StrongParticipation, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: InactiveExecutionEventIsWatchNotReady
    /// PURPOSE: Verifies the expected behavior of InactiveExecutionEventIsWatchNotReady.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void InactiveExecutionEventIsWatchNotReady()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(active: false));

        Assert.Equal(OpportunityClassification.WatchDeveloping, result.Classification);
        Assert.Contains(OpportunityReason.ExecutionEventInactive, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: MissingRequiredMetricProducesDataUnknown
    /// PURPOSE: Verifies the expected behavior of MissingRequiredMetricProducesDataUnknown.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void MissingRequiredMetricProducesDataUnknown()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(rangeAtr: null));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Equal("DATA?", result.DisplayLabel);
        Assert.Contains(OpportunityReason.DataMissing, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: StaleFiveMinuteSnapshotProducesDataUnknown
    /// PURPOSE: Verifies the expected behavior of StaleFiveMinuteSnapshotProducesDataUnknown.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void StaleFiveMinuteSnapshotProducesDataUnknown()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(fiveTimestamp: Now.AddMinutes(-7)));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Contains(OpportunityReason.DataStale, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: ExtendedEntryDowngradesOtherwiseStrongCandidate
    /// PURPOSE: Verifies the expected behavior of ExtendedEntryDowngradesOtherwiseStrongCandidate.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
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
    /// <summary>
    /// METHOD: WeakParticipationMomentumOrRangeIsReadyBoring
    /// PURPOSE: Verifies the expected behavior of WeakParticipationMomentumOrRangeIsReadyBoring.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
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
    /// <summary>
    /// METHOD: ModerateParticipationWithHealthyMomentumIsModerate
    /// PURPOSE: Verifies the expected behavior of ModerateParticipationWithHealthyMomentumIsModerate.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void ModerateParticipationWithHealthyMomentumIsModerate()
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(rvol: 2m));

        Assert.Equal(OpportunityClassification.ReadyModerate, result.Classification);
    }

    [Fact]
    /// <summary>
    /// METHOD: ResistanceTooCloseIsEntryTimingWarning
    /// PURPOSE: Verifies the expected behavior of ResistanceTooCloseIsEntryTimingWarning.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void ResistanceTooCloseIsEntryTimingWarning()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(resistanceAtr: 0.25m));

        Assert.Equal(OpportunityClassification.ReadyExtended, result.Classification);
        Assert.Contains(OpportunityReason.ResistanceTooClose, result.Reasons);
    }

    [Theory]
    [InlineData(-0.1, 1.5, 1.0, 2.0)]
    [InlineData(1.5, -0.1, 1.0, 2.0)]
    [InlineData(1.5, 1.0, -0.1, 2.0)]
    [InlineData(1.5, 1.0, 1.0, -0.1)]
    /// <summary>
    /// METHOD: NegativeMetricProducesDataUnknown
    /// PURPOSE: Rejects negative relative volume, extension, recent range, and resistance distance rather than ranking invalid input.
    /// WHEN CALLED: Invoked by xUnit to verify invalid numeric input fails closed.
    /// </summary>
    public void NegativeMetricProducesDataUnknown(
        decimal rvol,
        decimal extension,
        decimal rangeAtr,
        decimal resistanceAtr)
    {
        var result = new OpportunityQualityAssessor().Evaluate(Input(
            rvol: rvol,
            extension: extension,
            rangeAtr: rangeAtr,
            resistanceAtr: resistanceAtr));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Contains(OpportunityReason.DataMissing, result.Reasons);
    }

    [Fact]
    /// <summary>
    /// METHOD: FutureDatedMarketSnapshotIsNotFresh
    /// PURPOSE: Verifies the expected behavior of FutureDatedMarketSnapshotIsNotFresh.
    /// WHEN CALLED: Invoked by xUnit as part of the test suite or by a test helper.
    /// </summary>
    public void FutureDatedMarketSnapshotIsNotFresh()
    {
        var result = new OpportunityQualityAssessor().Evaluate(
            Input(oneTimestamp: Now.AddSeconds(1)));

        Assert.Equal(OpportunityClassification.DataUnknown, result.Classification);
        Assert.Contains(OpportunityReason.DataStale, result.Reasons);
    }
}
