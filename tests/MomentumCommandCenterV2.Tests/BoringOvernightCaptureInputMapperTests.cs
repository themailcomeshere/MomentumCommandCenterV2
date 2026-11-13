using System.Text.Json;
using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// Regression coverage for the capture-history technical-input mapping, Daily range/ATR proxies
/// and current Overnight interval classification (previously inline in Program.cs).
/// All values below are synthetic test fixtures.
/// </summary>
public sealed class BoringOvernightCaptureInputMapperTests
{
    private static JsonElement Assessment(
        string provider = "Alpaca",
        string? oneMinuteFeed = "iex",
        string? fiveMinuteFeed = "iex",
        bool? oneMinuteFresh = true,
        bool? fiveMinuteFresh = true,
        string evaluationStatus = "EVALUATED")
    {
        static object? Bool(bool? value) => value;
        var json = JsonSerializer.Serialize(new
        {
            provider,
            configuredFeed = "iex",
            evaluationStatus,
            oneMinute = new { feed = oneMinuteFeed, sourceTransport = "WEBSOCKET_STORE", extensionAtr = 1.25, sourceTimestampUtc = "2026-11-13T15:00:00+00:00" },
            fiveMinute = new { feed = fiveMinuteFeed, relativeVolume = 1.8, sourceTimestampUtc = "2026-11-13T14:55:00+00:00", scoreBreakdown = new { permission = true } },
            decision = new { fiveMinuteStructureBull = true, state = "EXECUTION READY", action = "BUY" },
            freshnessDiagnostics = new
            {
                oneMinute = new { isFresh = Bool(oneMinuteFresh), ageSeconds = 30.0, limitSeconds = 120.0 },
                fiveMinute = new { isFresh = Bool(fiveMinuteFresh), ageSeconds = 90.0, limitSeconds = 360.0 }
            }
        });
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public void FreshMatchedEvaluatedAssessmentMapsEveryTechnicalField()
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(Assessment(), "Alpaca", "iex");

        Assert.True(result.FeedMatchesCapture);
        Assert.True(result.IntradayBarsUsable);
        Assert.True(result.V2DecisionUsable);
        Assert.True(result.FiveMinutePermission);
        Assert.Equal("BULLISH", result.FiveMinuteStructure);
        Assert.Equal("EXECUTION READY/BUY", result.OneMinuteState);
        Assert.Equal(1.25, result.ExtensionAtr);
        Assert.Equal(1.8, result.CurrentVolumeVsAverage);
        Assert.Equal(DateTimeOffset.Parse("2026-11-13T15:00:00Z"), result.OneMinuteSourceTimestampUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-11-13T14:55:00Z"), result.FiveMinuteSourceTimestampUtc);
        Assert.Equal(30.0, result.OneMinuteAgeSeconds);
        Assert.Equal(360.0, result.FiveMinuteFreshnessLimitSeconds);
        Assert.Equal("WEBSOCKET_STORE", result.SourceTransport);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("Massive", "iex", "iex")]
    [InlineData("Alpaca", "sip", "iex")]
    [InlineData("Alpaca", "iex", "sip")]
    [InlineData("Alpaca", null, "iex")]
    public void ProviderOrFeedMismatchWithholdsAllTechnicalValues(string provider, string? oneFeed, string? fiveFeed)
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(
            Assessment(provider, oneFeed, fiveFeed), "Alpaca", "iex");

        Assert.False(result.FeedMatchesCapture);
        Assert.False(result.IntradayBarsUsable);
        Assert.False(result.V2DecisionUsable);
        Assert.Null(result.FiveMinutePermission);
        Assert.Null(result.FiveMinuteStructure);
        Assert.Null(result.OneMinuteState);
        Assert.Null(result.ExtensionAtr);
        Assert.Null(result.CurrentVolumeVsAverage);
        Assert.Null(result.OneMinuteSourceTimestampUtc);
        Assert.Null(result.OneMinuteFresh);
        Assert.StartsWith("TECHNICAL_FEED_MISMATCH", result.Error);
        // Observed labels remain visible for diagnosis.
        Assert.Equal(provider, result.AssessmentProvider);
        Assert.Equal(oneFeed, result.OneMinuteFeed);
    }

    [Fact]
    public void UnknownCaptureFeedIsNeverAMatch()
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(Assessment(), "Alpaca", null);

        Assert.False(result.FeedMatchesCapture);
        Assert.Null(result.ExtensionAtr);
        Assert.StartsWith("TECHNICAL_FEED_MISMATCH", result.Error);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(null, true)]
    public void StaleOrUnknownFreshnessWithholdsNumericAndDecisionFields(bool? oneFresh, bool? fiveFresh)
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(
            Assessment(oneMinuteFresh: oneFresh, fiveMinuteFresh: fiveFresh), "Alpaca", "iex");

        Assert.True(result.FeedMatchesCapture);
        Assert.False(result.IntradayBarsUsable);
        Assert.Null(result.ExtensionAtr);
        Assert.Null(result.CurrentVolumeVsAverage);
        Assert.Null(result.FiveMinutePermission);
        Assert.Null(result.OneMinuteState);
        // Source timestamps stay visible so the staleness is explainable.
        Assert.NotNull(result.OneMinuteSourceTimestampUtc);
    }

    [Theory]
    [InlineData("OUTSIDE_REGULAR_SESSION")]
    [InlineData("STALE_MARKET_DATA")]
    [InlineData("INSUFFICIENT_HISTORY")]
    [InlineData("RVOL_BASELINE_INSUFFICIENT")]
    public void FreshBarsWithoutEvaluatedStatusKeepNumericButWithholdDecisionFields(string status)
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(
            Assessment(evaluationStatus: status), "Alpaca", "iex");

        Assert.True(result.IntradayBarsUsable);
        Assert.False(result.V2DecisionUsable);
        Assert.Equal(1.25, result.ExtensionAtr);
        Assert.Null(result.FiveMinutePermission);
        Assert.Null(result.FiveMinuteStructure);
        Assert.Null(result.OneMinuteState);
    }

    [Fact]
    public void MissingAssessmentPreservesUpstreamError()
    {
        var result = BoringOvernightCaptureInputMapper.MapTechnicalInputs(null, "Alpaca", "iex", "HttpRequestException: timeout");

        Assert.False(result.FeedMatchesCapture);
        Assert.Equal("HttpRequestException: timeout", result.Error);
        Assert.Null(result.ExtensionAtr);
    }

    private static DailyMarketMetrics Daily(int sessions, bool qualificationUsable = true, bool fresh = true)
    {
        var list = Enumerable.Range(0, sessions).Select(i =>
        {
            var date = new DateOnly(2026, 10, 1).AddDays(i);
            var close = 100d + i;
            return new DailySessionObservation(date, new DateTimeOffset(date.ToDateTime(new TimeOnly(20, 0)), TimeSpan.Zero),
                close - 0.5, close + 1, close - 1, close, 1_000_000);
        }).ToArray();
        return new DailyMarketMetrics("TEST", "Alpaca", qualificationUsable ? "sip" : "iex", "all",
            DateTimeOffset.UtcNow, list.LastOrDefault()?.TimestampUtc, 100, 100, 1_000_000, 100, 100, 1_000_000,
            sessions, sessions, fresh, "COMPLETE", qualificationUsable ? "SIP" : "IEX_PARTIAL", qualificationUsable, "fixture",
            list.Last().SessionDate, list.Select(x => x.SessionDate).ToArray(), Array.Empty<DateOnly>(), list);
    }

    [Fact]
    public void DailyTechnicalsUseLatestRangeAndFourteenSessionSimpleTrueRange()
    {
        var result = BoringOvernightCaptureInputMapper.CalculateDailyTechnicals(Daily(20));

        // Latest close 119, high 120, low 118 → range 2/119.
        Assert.Equal(2d / 119d * 100d, result.DailyRangePct!.Value, 10);
        // Each true range = max(2, |high - prevClose| = 2, |low - prevClose| = 0) = 2.
        Assert.Equal(2d / 119d * 100d, result.AtrPct!.Value, 10);
        Assert.Equal(new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero), result.SourceTimestampUtc);
    }

    [Theory]
    [InlineData(20, false, true)]
    [InlineData(20, true, false)]
    [InlineData(14, true, true)]
    public void DailyTechnicalsFailClosedWithoutQualifiedFreshFifteenSessions(int sessions, bool usable, bool fresh)
    {
        var result = BoringOvernightCaptureInputMapper.CalculateDailyTechnicals(Daily(sessions, usable, fresh));

        Assert.Null(result.DailyRangePct);
        Assert.Null(result.AtrPct);
        Assert.Null(result.SourceTimestampUtc);
    }

    [Fact]
    public void NullDailyMetricsYieldNoDailyTechnicals()
    {
        Assert.Null(BoringOvernightCaptureInputMapper.CalculateDailyTechnicals(null).AtrPct);
    }

    private static readonly OvernightSessionBoundary Boundary = new(
        new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13),
        DateTimeOffset.Parse("2026-11-12T20:59:00Z"), DateTimeOffset.Parse("2026-11-13T14:30:00Z"));

    private static MarketBar Bar(DateTimeOffset timestamp, double open, double close) =>
        new("TEST", timestamp, open, Math.Max(open, close), Math.Min(open, close), close, 100, "Alpaca", "iex", "1m");

    [Fact]
    public void CurrentIntervalIsPendingBeforeExactOpenBarCompletes()
    {
        var close = Bar(Boundary.PreviousCloseBarTimestampUtc, 10, 10);
        var open = Bar(Boundary.NextOpenBarTimestampUtc, 10.5, 10.6);

        // 09:30:59 ET: the 09:30 bar exists in some feeds but has not completed.
        var result = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(
            Boundary, close, open, DateTimeOffset.Parse("2026-11-13T14:30:59Z"));

        Assert.Equal("PENDING", result.State);
        Assert.Equal(DateTimeOffset.Parse("2026-11-13T14:31:00Z"), result.EndpointAvailableAtUtc);
    }

    [Fact]
    public void CurrentIntervalIsPendingWithoutBarsBeforeAvailability()
    {
        var result = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(
            Boundary, null, null, DateTimeOffset.Parse("2026-11-13T02:00:00Z"));

        Assert.Equal("PENDING", result.State);
        Assert.Null(result.ReturnPct);
    }

    [Fact]
    public void CurrentIntervalIsObservedAtExactAvailabilityWithBothExactBars()
    {
        var close = Bar(Boundary.PreviousCloseBarTimestampUtc, 10, 10);
        var open = Bar(Boundary.NextOpenBarTimestampUtc, 10.5, 10.6);

        var result = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(
            Boundary, close, open, DateTimeOffset.Parse("2026-11-13T14:31:00Z"));

        Assert.Equal("OBSERVED", result.State);
        Assert.Equal(10d, result.PreviousClose);
        Assert.Equal(10.5d, result.NextOpen);
        Assert.Equal(5m, Math.Round(result.ReturnPct!.Value, 10));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CurrentIntervalIsMissingAfterAvailabilityWithoutBothExactBars(bool hasClose, bool hasOpen)
    {
        var close = hasClose ? Bar(Boundary.PreviousCloseBarTimestampUtc, 10, 10) : null;
        var open = hasOpen ? Bar(Boundary.NextOpenBarTimestampUtc, 10.5, 10.6) : null;

        var result = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(
            Boundary, close, open, DateTimeOffset.Parse("2026-11-13T16:00:00Z"));

        Assert.Equal("MISSING", result.State);
        Assert.Null(result.ReturnPct);
    }

    [Fact]
    public void CurrentIntervalRejectsNearestBarSubstitution()
    {
        var nearbyOpen = Bar(Boundary.NextOpenBarTimestampUtc.AddMinutes(1), 10.5, 10.6);

        Assert.Throws<ArgumentException>(() => BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(
            Boundary, null, nearbyOpen, DateTimeOffset.Parse("2026-11-13T16:00:00Z")));
    }
}
