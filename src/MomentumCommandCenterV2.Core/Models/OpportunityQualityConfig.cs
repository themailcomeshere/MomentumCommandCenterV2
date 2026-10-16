namespace MomentumCommandCenterV2.Core.Models
{
    /// <summary>
    /// Explicitly provisional bands for diagnostic prototyping only.
    /// Do not treat these defaults as calibrated trading thresholds.
    /// </summary>
    public sealed record OpportunityQualityConfig(
        TimeSpan MaxOneMinuteAge,
        TimeSpan MaxFiveMinuteAge,
        decimal MinimumRelativeVolume,
        decimal HighRelativeVolume,
        decimal ExtensionWarningAtr,
        decimal MinimumRecentRangeAtr,
        decimal ResistanceTooCloseAtr)
    {
        public static OpportunityQualityConfig DiagnosticDefaults { get; } = new(
            MaxOneMinuteAge: TimeSpan.FromMinutes(2),
            MaxFiveMinuteAge: TimeSpan.FromMinutes(6),
            MinimumRelativeVolume: 1.5m,
            HighRelativeVolume: 2.5m,
            ExtensionWarningAtr: 1.5m,
            MinimumRecentRangeAtr: 1.0m,
            ResistanceTooCloseAtr: 0.5m);
    }
}
