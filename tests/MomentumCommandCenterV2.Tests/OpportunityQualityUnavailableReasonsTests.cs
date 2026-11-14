using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OpportunityQualityUnavailableReasonsTests
/// PURPOSE: Verifies that DATA? rows report stale and after-hours data distinctly from missing data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OpportunityQualityUnavailableReasonsTests
{
    /// <summary>
    /// METHOD: StaleAfterTheCloseReportsStaleAndOutsideSession
    /// PURPOSE: Friday-evening case: last bars from the close are stale and the session is closed.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void StaleAfterTheCloseReportsStaleAndOutsideSession()
    {
        var reasons = OpportunityQualityUnavailableReasons.For("STALE_MARKET_DATA", regularSession: false);
        Assert.Equal(new[] { OpportunityReason.DataStale, OpportunityReason.OutsideRegularSession }, reasons);
    }

    /// <summary>
    /// METHOD: StaleDuringTheSessionReportsStaleOnly
    /// PURPOSE: Stale bars during regular hours must not claim the session is closed.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void StaleDuringTheSessionReportsStaleOnly()
    {
        var reasons = OpportunityQualityUnavailableReasons.For("STALE_MARKET_DATA", regularSession: true);
        Assert.Equal(new[] { OpportunityReason.DataStale }, reasons);
    }

    /// <summary>
    /// METHOD: OutsideSessionWithFreshBarsReportsOutsideSessionOnly
    /// PURPOSE: Fresh extended-hours bars are not missing data; only the session gate applies.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void OutsideSessionWithFreshBarsReportsOutsideSessionOnly()
    {
        var reasons = OpportunityQualityUnavailableReasons.For("OUTSIDE_REGULAR_SESSION", regularSession: false);
        Assert.Equal(new[] { OpportunityReason.OutsideRegularSession }, reasons);
    }

    /// <summary>
    /// METHOD: MissingHistoryAndBaselineReportDataMissing
    /// PURPOSE: Too few bars, no RVOL baseline or an unknown status remain DataMissing.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Theory]
    [InlineData("INSUFFICIENT_HISTORY")]
    [InlineData("RVOL_BASELINE_INSUFFICIENT")]
    [InlineData("SOMETHING_NEW")]
    public void MissingHistoryAndBaselineReportDataMissing(string status)
    {
        Assert.Equal(new[] { OpportunityReason.DataMissing }, OpportunityQualityUnavailableReasons.For(status, regularSession: true));
        Assert.Equal(
            new[] { OpportunityReason.DataMissing, OpportunityReason.OutsideRegularSession },
            OpportunityQualityUnavailableReasons.For(status, regularSession: false));
    }
}
