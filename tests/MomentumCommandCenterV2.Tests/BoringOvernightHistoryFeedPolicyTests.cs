using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: BoringOvernightHistoryFeedPolicyTests
/// PURPOSE: Verifies the capture-history feed plan: delayed SIP history with IEX technicals when allowed, otherwise unchanged.
/// WHEN CALLED: Run by xUnit.
/// </summary>
public sealed class BoringOvernightHistoryFeedPolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T20:30:00Z");

    /// <summary>IEX + allowed: SIP history, requests end at now - 16 min, technicals stay on IEX.</summary>
    [Fact]
    public void IexWithDelayedSipAllowedUsesSipHistoryAndIexTechnicals()
    {
        var plan = BoringOvernightHistoryFeedPolicy.Resolve("iex", true, Now);
        Assert.True(plan.UseDelayedSip);
        Assert.Equal("DELAYED_HISTORICAL_SIP", plan.Mode);
        Assert.Equal("sip", plan.HistoryFeed);
        Assert.Equal(Now - AlpacaDailyMarketDataClient.SipDelay, plan.HistoryAsOfUtc);
        Assert.Equal(Now - AlpacaDailyMarketDataClient.SipDelay, plan.MaxRequestEndUtc);
        Assert.Equal("iex", plan.TechnicalFeed);
    }

    /// <summary>Not allowed, or already on SIP: the configured feed is used exactly as before.</summary>
    [Theory]
    [InlineData("iex", false)]
    [InlineData("sip", true)]
    [InlineData("SIP", false)]
    public void OtherwiseConfiguredFeedIsUnchanged(string feed, bool allowed)
    {
        var plan = BoringOvernightHistoryFeedPolicy.Resolve(feed, allowed, Now);
        Assert.False(plan.UseDelayedSip);
        Assert.Equal("CONFIGURED_FEED", plan.Mode);
        Assert.Null(plan.HistoryFeed);
        Assert.Null(plan.TechnicalFeed);
        Assert.Equal(Now, plan.HistoryAsOfUtc);
        Assert.Equal(Now.AddMinutes(1), plan.MaxRequestEndUtc);
    }

    /// <summary>Current ON % is the latest completed pair's return, and only when that exact pair is present.</summary>
    [Fact]
    public void CurrentOvernightPctComesOnlyFromTheExactLatestPair()
    {
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 13), 5);
        OvernightReturnObservation Pair(OvernightSessionBoundary b, decimal pct) =>
            new(b.PreviousSessionDate, b.NextSessionDate, b.PreviousCloseBarTimestampUtc, b.NextOpenBarTimestampUtc, 10m, 10m, pct, "c", "o");
        var withLatest = boundaries.Select((b, i) => Pair(b, i + 0.5m)).ToArray();
        var withoutLatest = withLatest.Take(4).ToArray();

        var (pct, ts) = BoringOvernightCaptureInputMapper.LatestCompletedOvernightPct(withLatest, boundaries[^1]);
        Assert.Equal(4.5m, pct);
        Assert.Equal(boundaries[^1].NextOpenBarTimestampUtc, ts);
        Assert.Null(BoringOvernightCaptureInputMapper.LatestCompletedOvernightPct(withoutLatest, boundaries[^1]).ReturnPct);
    }

    /// <summary>In SIP mode the current interval is judged as of now - 16 min: an open bar not yet fetchable is PENDING, not MISSING.</summary>
    [Fact]
    public void CurrentOpenWithinSipDelayIsPendingNotMissing()
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        DateTimeOffset Et(int y, int m, int d, int h, int min)
        {
            var local = new DateTime(y, m, d, h, min, 0, DateTimeKind.Unspecified);
            return new DateTimeOffset(local, eastern.GetUtcOffset(local)).ToUniversalTime();
        }
        var boundary = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 17), 5)[^1];
        var at0940 = Et(2026, 11, 17, 9, 40);
        var plan = BoringOvernightHistoryFeedPolicy.Resolve("iex", true, at0940);
        var close = new MarketBar("TEST", boundary.PreviousCloseBarTimestampUtc, 10, 10, 10, 10, 100, "Alpaca", "sip", "1m");

        var sipView = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(boundary, close, null, plan.HistoryAsOfUtc);
        var naiveView = BoringOvernightCaptureInputMapper.ClassifyCurrentInterval(boundary, close, null, at0940);

        Assert.Equal("PENDING", sipView.State);
        Assert.Equal("MISSING", naiveView.State);
    }
}
