namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Qualification policy for live intraday technical inputs. Diagnostic calculations may still
/// be present in the live-assessment response when their source bars are stale, but they must
/// not be copied into an evaluator snapshot unless both timeframes are fresh, provenance matches,
/// and the V2 engine actually evaluated the current regular-session bars.
/// </summary>
public static class BoringOvernightTechnicalInputPolicy
{
    public static bool AreIntradayInputsUsable(
        bool feedMatchesCapture,
        bool? oneMinuteFresh,
        bool? fiveMinuteFresh,
        string? evaluationStatus) =>
        feedMatchesCapture &&
        oneMinuteFresh == true &&
        fiveMinuteFresh == true &&
        string.Equals(evaluationStatus, "EVALUATED", StringComparison.OrdinalIgnoreCase);
}
