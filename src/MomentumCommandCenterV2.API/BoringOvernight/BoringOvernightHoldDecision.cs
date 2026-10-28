namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Converts a completed qualification snapshot into the time-gated Boring Overnight
/// position-management decision shown by the chart bubble. This class does not place orders.
/// </summary>
public static class BoringOvernightHoldDecision
{
    public static BoringOvernightHoldDecisionResult Evaluate(
        BoringOvernightEvaluation evaluation,
        DateTimeOffset evaluatedAt,
        TimeZoneInfo localTimeZone,
        TimeOnly? windowStart = null,
        TimeOnly? windowEnd = null)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(localTimeZone);

        var start = windowStart ?? new TimeOnly(12, 30);
        var end = windowEnd ?? new TimeOnly(12, 55);
        if (start >= end)
            throw new ArgumentException("Decision window start must be earlier than its end.");

        var local = TimeZoneInfo.ConvertTime(evaluatedAt, localTimeZone);
        var localTime = TimeOnly.FromDateTime(local.DateTime);
        // Inclusive endpoints: the 12:30 and 12:55 local bars are in-window.
        if (localTime < start || localTime > end)
        {
            return new BoringOvernightHoldDecisionResult(
                evaluation.Symbol, "OUTSIDE_WINDOW", "NO DECISION",
                "NEUTRAL", false, local, start, end,
                "The Boring Overnight decision window is closed. Do not infer HOLD OVERNIGHT or EOD GET OUT outside the configured window.");
        }

        if (evaluation.Status == "READY" && evaluation.Eligible)
        {
            return new BoringOvernightHoldDecisionResult(
                evaluation.Symbol, "HOLD_OVERNIGHT", "HOLD OVERNIGHT",
                "GREEN", true, local, start, end,
                "All supplied qualification gates passed inside the configured local-time window. This is a decision aid, not an order.");
        }

        var status = evaluation.Status == "DATA?" ? "DATA?" : "EOD_GET_OUT";
        var label = evaluation.Status == "DATA?" ? "DATA? — NO HOLD" : "EOD GET OUT";
        var color = evaluation.Status == "DATA?" ? "YELLOW" : "RED";
        return new BoringOvernightHoldDecisionResult(
            evaluation.Symbol, status, label, color, false, local, start, end,
            evaluation.Status == "DATA?"
                ? "Required qualification evidence is missing or invalid. Do not hold overnight based on an unverified decision."
                : "One or more qualification gates failed or raised a warning. Do not hold overnight under the configured boring-first rules.");
    }
}

public sealed record BoringOvernightHoldDecisionResult(
    string Symbol,
    string Status,
    string BubbleText,
    string Color,
    bool HoldOvernight,
    DateTimeOffset EvaluatedAtLocal,
    TimeOnly WindowStartLocal,
    TimeOnly WindowEndLocal,
    string Explanation);
