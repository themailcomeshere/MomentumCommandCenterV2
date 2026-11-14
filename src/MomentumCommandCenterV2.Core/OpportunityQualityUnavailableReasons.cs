using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core;

/// <summary>
/// TYPE: OpportunityQualityUnavailableReasons
/// PURPOSE: Maps a live-assessment evaluation status that produced no V2 decision to the
/// Opportunity Quality reasons shown on a DATA? row, so stale or after-hours data is not
/// reported as missing data.
/// WHEN CALLED: Called by the live symbol assessment when no V2 decision exists and the
/// Opportunity Quality classification is therefore DataUnknown.
/// </summary>
public static class OpportunityQualityUnavailableReasons
{
    /// <summary>
    /// METHOD: For
    /// PURPOSE: Returns the reason list for a DataUnknown row. STALE_MARKET_DATA maps to
    /// DataStale; INSUFFICIENT_HISTORY, RVOL_BASELINE_INSUFFICIENT and unknown statuses map to
    /// DataMissing. OutsideRegularSession is appended whenever the evaluation time is outside
    /// the US regular session, including OUTSIDE_REGULAR_SESSION itself.
    /// WHEN CALLED: Called once per live assessment that has no V2 decision.
    /// </summary>
    public static IReadOnlyList<OpportunityReason> For(string evaluationStatus, bool regularSession)
    {
        var reasons = new List<OpportunityReason>();
        switch (evaluationStatus)
        {
            case "STALE_MARKET_DATA":
                reasons.Add(OpportunityReason.DataStale);
                break;
            case "OUTSIDE_REGULAR_SESSION":
                break;
            default:
                reasons.Add(OpportunityReason.DataMissing);
                break;
        }

        if (!regularSession)
        {
            reasons.Add(OpportunityReason.OutsideRegularSession);
        }

        return reasons;
    }
}
