using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Core;

/// <summary>
/// Deterministic, diagnostic-only opportunity ranking layered beside the
/// existing V2 decision. It never mutates or replaces that decision.
/// </summary>
public sealed class OpportunityQualityAssessor
{
    private readonly OpportunityQualityConfig _config;

    public OpportunityQualityAssessor(OpportunityQualityConfig? config = null)
    {
        _config = config ?? OpportunityQualityConfig.DiagnosticDefaults;
        ValidateConfig(_config);
    }

    public OpportunityQualityAssessment Evaluate(OpportunityQualityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var reasons = new List<OpportunityReason>();

        if (string.IsNullOrWhiteSpace(input.Symbol) ||
            input.RelativeVolume is null ||
            input.AtrExtension is null ||
            input.RecentRangeInAtr is null)
        {
            reasons.Add(OpportunityReason.DataMissing);
            return Result(input, OpportunityClassification.DataUnknown, reasons);
        }

        if (!IsFresh(input.EvaluatedAt, input.OneMinuteTimestamp, _config.MaxOneMinuteAge) ||
            !IsFresh(input.EvaluatedAt, input.FiveMinuteTimestamp, _config.MaxFiveMinuteAge))
        {
            reasons.Add(OpportunityReason.DataStale);
            return Result(input, OpportunityClassification.DataUnknown, reasons);
        }

        if (input.RelativeVolume.Value < 0m ||
            input.AtrExtension.Value < 0m ||
            input.RecentRangeInAtr.Value < 0m ||
            (input.ResistanceDistanceInAtr is < 0m))
        {
            reasons.Add(OpportunityReason.DataMissing);
            return Result(input, OpportunityClassification.DataUnknown, reasons);
        }

        // Eligibility and active execution-event gates always dominate ranking.
        if (!input.ExistingEntryEligible)
        {
            reasons.Add(OpportunityReason.EntryNotEligible);
            return Result(input, OpportunityClassification.Blocked, reasons);
        }

        if (!input.ExecutionEventActive)
        {
            reasons.Add(OpportunityReason.ExecutionEventInactive);
            return Result(input, OpportunityClassification.WatchDeveloping, reasons);
        }

        if (input.AtrExtension.Value >= _config.ExtensionWarningAtr ||
            (input.ResistanceDistanceInAtr is not null &&
             input.ResistanceDistanceInAtr.Value <= _config.ResistanceTooCloseAtr))
        {
            reasons.Add(OpportunityReason.EntryExtended);

            if (input.ResistanceDistanceInAtr is not null &&
                input.ResistanceDistanceInAtr <= _config.ResistanceTooCloseAtr)
            {
                reasons.Add(OpportunityReason.ResistanceTooClose);
            }

            return Result(input, OpportunityClassification.ReadyExtended, reasons);
        }

        var lowParticipation = input.RelativeVolume.Value < _config.MinimumRelativeVolume;
        var limitedRange = input.RecentRangeInAtr.Value < _config.MinimumRecentRangeAtr;
        var weakMomentum = !input.MomentumConfirmed;

        if (lowParticipation)
        {
            reasons.Add(OpportunityReason.LowRelativeVolume);
        }

        if (weakMomentum)
        {
            reasons.Add(OpportunityReason.MomentumNotConfirmed);
        }
        else
        {
            reasons.Add(OpportunityReason.MomentumConfirmed);
        }

        if (limitedRange)
        {
            reasons.Add(OpportunityReason.LimitedRecentRange);
        }
        else
        {
            reasons.Add(OpportunityReason.AdequateMovementCapacity);
        }

        if (lowParticipation || weakMomentum || limitedRange)
        {
            return Result(input, OpportunityClassification.ReadyBoring, reasons);
        }

        if (input.RelativeVolume.Value >= _config.HighRelativeVolume)
        {
            reasons.Add(OpportunityReason.StrongParticipation);
            return Result(input, OpportunityClassification.ReadyHighOpportunity, reasons);
        }

        return Result(input, OpportunityClassification.ReadyModerate, reasons);
    }

    private static bool IsFresh(
        DateTimeOffset evaluatedAt,
        DateTimeOffset sourceTimestamp,
        TimeSpan maxAge)
    {
        var age = evaluatedAt - sourceTimestamp;
        return age >= TimeSpan.Zero && age <= maxAge;
    }

    private static OpportunityQualityAssessment Result(
        OpportunityQualityInput input,
        OpportunityClassification classification,
        IReadOnlyList<OpportunityReason> reasons) =>
        new(input.Symbol, classification, reasons, input.EvaluatedAt);

    private static void ValidateConfig(OpportunityQualityConfig config)
    {
        if (config.MaxOneMinuteAge <= TimeSpan.Zero ||
            config.MaxFiveMinuteAge <= TimeSpan.Zero ||
            config.MinimumRelativeVolume < 0m ||
            config.HighRelativeVolume < config.MinimumRelativeVolume ||
            config.ExtensionWarningAtr <= 0m ||
            config.MinimumRecentRangeAtr < 0m ||
            config.ResistanceTooCloseAtr < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config),
                "Opportunity-quality thresholds must be non-negative and internally consistent.");
        }
    }
}
