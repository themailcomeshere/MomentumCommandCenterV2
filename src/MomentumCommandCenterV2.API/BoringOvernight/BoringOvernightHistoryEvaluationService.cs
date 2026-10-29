namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Bind independently calculated overnight history to the qualification evaluator.
/// WHEN CALLED: API workflows have calculated history from persisted bars and explicit boundaries.
/// IMPORTANT: Incomplete history deliberately nulls the evaluator's historical fields; caller-entered
/// historical metrics are never retained as fallback values.
/// </summary>
public static class BoringOvernightHistoryEvaluationService
{
    public static BoringOvernightEvaluationRequest ApplyHistory(
        BoringOvernightEvaluationRequest request,
        BoringOvernightHistoryResult history)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(history);
        if (!request.Symbol.Equals(history.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Evaluation symbol and calculated-history symbol must match.", nameof(history));

        var usable = history.AllPairsComplete && history.EnoughHistory &&
            history.ValidPairCount >= history.RequiredWindow &&
            history.WinRatePct20D.HasValue &&
            history.AverageReturnPct20D.HasValue &&
            history.AverageReturnPct5D.HasValue;

        if (!usable)
        {
            return request with
            {
                OvernightWinRatePct = null,
                AverageOvernightPct20 = null,
                AverageOvernightPct5 = null,
                MedianOvernightPct20 = null
            };
        }

        var lastWindow = history.Returns.OrderBy(x => x.NextSessionDate)
            .TakeLast(history.RequiredWindow).Select(x => x.ReturnPct).OrderBy(x => x).ToArray();
        var middle = lastWindow.Length / 2;
        var median = lastWindow.Length % 2 == 0
            ? (lastWindow[middle - 1] + lastWindow[middle]) / 2m
            : lastWindow[middle];

        return request with
        {
            OvernightWinRatePct = (double)history.WinRatePct20D!.Value,
            AverageOvernightPct20 = (double)history.AverageReturnPct20D!.Value,
            AverageOvernightPct5 = (double)history.AverageReturnPct5D!.Value,
            MedianOvernightPct20 = (double)median
        };
    }
}

/// <summary>Request to compute historical metrics, apply them to the evaluator, and persist the resulting evidence.</summary>
public sealed record BoringOvernightHistoryEvaluationRequest(
    string Symbol,
    string Provider,
    string? Feed,
    IReadOnlyList<OvernightSessionBoundary> Boundaries,
    int RequiredWindow,
    DateTimeOffset AsOfUtc,
    string Source,
    string CalculationVersion,
    IReadOnlyDictionary<string, DateTimeOffset> FieldSourceTimestampsUtc,
    BoringOvernightEvaluationRequest EvaluationRequest);
