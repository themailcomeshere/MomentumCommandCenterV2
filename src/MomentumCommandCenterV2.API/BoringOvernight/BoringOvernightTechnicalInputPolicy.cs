namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Qualification policy for live intraday technical inputs. Diagnostic calculations may still
/// be present when source bars are stale. Numeric indicators require matched, fresh source bars;
/// V2 permission/structure/state additionally require a successful regular-session V2 evaluation.
/// </summary>
public static class BoringOvernightTechnicalInputPolicy
{
    public static bool AreIntradayBarsUsable(
        bool feedMatchesCapture,
        bool? oneMinuteFresh,
        bool? fiveMinuteFresh) =>
        feedMatchesCapture && oneMinuteFresh == true && fiveMinuteFresh == true;

    public static bool IsV2DecisionUsable(
        bool intradayBarsUsable,
        string? evaluationStatus) =>
        intradayBarsUsable &&
        string.Equals(evaluationStatus, "EVALUATED", StringComparison.OrdinalIgnoreCase);
}
