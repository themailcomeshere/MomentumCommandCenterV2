using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models;

/// <summary>
/// Explainable diagnostic assessment. This is not a profit probability,
/// order instruction, or replacement for the V2 signal/position engine.
/// WHEN CALLED: Created by OpportunityQualityAssessor and consumed by diagnostic presentation or validation.
/// </summary>
public sealed record OpportunityQualityAssessment(
    string Symbol,
    OpportunityClassification Classification,
    IReadOnlyList<OpportunityReason> Reasons,
    DateTimeOffset EvaluatedAt)
{
    public string DisplayLabel => Classification switch
    {
        OpportunityClassification.ReadyHighOpportunity => "QUALITY — HIGH",
        OpportunityClassification.ReadyModerate => "QUALITY — MODERATE",
        OpportunityClassification.ReadyBoring => "QUALITY — LOW",
        OpportunityClassification.ReadyExtended => "TIMING — EXTENDED",
        OpportunityClassification.WatchDeveloping => "WATCH — DEVELOPING",
        OpportunityClassification.Blocked => "BLOCKED",
        _ => "DATA?"
    };
}
