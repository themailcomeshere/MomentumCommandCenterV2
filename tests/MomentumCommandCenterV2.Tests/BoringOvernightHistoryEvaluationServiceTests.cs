using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightHistoryEvaluationServiceTests
{
    [Fact]
    public void CompleteHistoryOverridesCallerEnteredHistoricalMetrics()
    {
        var request = Request() with
        {
            OvernightWinRatePct = 99,
            AverageOvernightPct20 = 9,
            AverageOvernightPct5 = 10,
            MedianOvernightPct20 = 8
        };
        var returns = Enumerable.Range(1, 20).Select(i => new OvernightReturnObservation(
            new DateOnly(2026, 9, 1).AddDays(i - 1),
            new DateOnly(2026, 9, 2).AddDays(i - 1),
            new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero).AddDays(i - 1),
            new DateTimeOffset(2026, 9, 2, 13, 30, 0, TimeSpan.Zero).AddDays(i - 1),
            100m, 100m + i, i, $"close-{i}", $"open-{i}")).ToArray();
        var history = new BoringOvernightHistoryResult(
            "TEST", "Alpaca", "iex", DateTimeOffset.UtcNow, 20, 20, 20, 0,
            true, true, 65m, 0.5m, 10.5m, 0.5m, 1.5m, 60m,
            returns, Array.Empty<OvernightMissingBoundary>(), "test");

        var effective = BoringOvernightHistoryEvaluationService.ApplyHistory(request, history);

        Assert.Equal(60d, effective.OvernightWinRatePct);
        Assert.Equal(0.5d, effective.AverageOvernightPct20);
        Assert.Equal(1.5d, effective.AverageOvernightPct5);
        Assert.Equal(10.5d, effective.MedianOvernightPct20);
    }

    [Fact]
    public void IncompleteHistoryClearsAllHistoricalEvaluatorFieldsInsteadOfUsingCallerValues()
    {
        var request = Request() with
        {
            OvernightWinRatePct = 99,
            AverageOvernightPct20 = 9,
            AverageOvernightPct5 = 10,
            MedianOvernightPct20 = 8
        };
        var history = new BoringOvernightHistoryResult(
            "TEST", "Alpaca", "iex", DateTimeOffset.UtcNow, 20, 20, 19, 1,
            false, false, null, null, null, null, null, null,
            Array.Empty<OvernightReturnObservation>(),
            new[] { new OvernightMissingBoundary(new DateOnly(2026, 10, 2), "NEXT_SESSION_OPEN_MISSING",
                new DateTimeOffset(2026, 10, 2, 13, 30, 0, TimeSpan.Zero)) }, "incomplete");

        var effective = BoringOvernightHistoryEvaluationService.ApplyHistory(request, history);

        Assert.Null(effective.OvernightWinRatePct);
        Assert.Null(effective.AverageOvernightPct20);
        Assert.Null(effective.AverageOvernightPct5);
        Assert.Null(effective.MedianOvernightPct20);
    }

    [Fact]
    public void RejectsHistoryForDifferentSymbol()
    {
        var history = new BoringOvernightHistoryResult(
            "OTHER", "Alpaca", null, DateTimeOffset.UtcNow, 20, 20, 0, 20,
            false, false, null, null, null, null, null, null,
            Array.Empty<OvernightReturnObservation>(), Array.Empty<OvernightMissingBoundary>(), "test");

        Assert.Throws<ArgumentException>(() =>
            BoringOvernightHistoryEvaluationService.ApplyHistory(Request(), history));
    }

    private static BoringOvernightEvaluationRequest Request() => new(
        "TEST", true, 100, 90, 1_000_000, null, null, null, null, 0.2,
        false, "UNKNOWN", true, "BULLISH", "EXECUTION READY", 0.5, 1, 1.2, 1.1);
}
