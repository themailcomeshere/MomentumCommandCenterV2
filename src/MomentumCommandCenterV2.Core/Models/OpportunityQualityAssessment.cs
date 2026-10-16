using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models;

/// <summary>
/// Explainable diagnostic assessment. This is not a profit probability,
/// order instruction, or replacement for the V2 signal/position engine.
/// </summary>
public sealed record OpportunityQualityAssessment(
    string Symbol,
    OpportunityClassification Classification,
    IReadOnlyList<OpportunityReason> Reasons,
    DateTimeOffset EvaluatedAt)
{
    public string DisplayLabel => Classification switch
    {
        OpportunityClassification.ReadyHighOpportunity => "READY — HIGH",
        OpportunityClassification.ReadyModerate => "READY — MODERATE",
        OpportunityClassification.ReadyBoring => "READY — BORING",
        OpportunityClassification.ReadyExtended => "READY — EXTENDED",
        OpportunityClassification.WatchDeveloping => "WATCH — DEVELOPING",
        OpportunityClassification.Blocked => "BLOCKED",
        _ => "DATA?"
    };
}
