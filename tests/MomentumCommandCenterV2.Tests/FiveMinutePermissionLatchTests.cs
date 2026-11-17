using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;
using MarketBar = MomentumCommandCenterV2.API.MarketData.MarketBar;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: FiveMinutePermissionLatchTests
/// PURPOSE: Verifies the port of the installed TOS 5M study's PERMISSION STATE latch: grant on the raw condition, hold
/// through a pullback below VWAP while EMA9 >= EMA20 (the SLS case from 2026-11-17), release on 2 bars of structural
/// damage or major failure, the study's EOD window and DST rule, the daily restart, block codes, the O(n) indicator
/// series matching LiveSymbolTechnicalCalculator.Calculate exactly, and the engine using the supplied latch value.
/// All bars are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class FiveMinutePermissionLatchTests
{
    private static readonly DateTimeOffset Open = DateTimeOffset.Parse("2026-11-17T09:30:00-05:00");

    /// <summary>PURPOSE: One latch input bar at Open + 5 minutes × index. WHEN CALLED: By the tests below.</summary>
    private static PermissionLatchBar B(int index, decimal close, decimal vwap, decimal ema9, decimal ema20, decimal sma50 = 10m, decimal rsi = 55m) =>
        new(Open.AddMinutes(5 * index), close, vwap, ema9, ema20, sma50, rsi);

    private static PermissionLatchBar Raw(int i) => B(i, 12m, 11m, 11.5m, 11m);           // all raw conditions true
    private static PermissionLatchBar Pullback(int i) => B(i, 10.8m, 11m, 11.2m, 11m);    // below VWAP and EMA9, EMA9 >= EMA20: no damage
    private static PermissionLatchBar Damage(int i) => B(i, 10.5m, 11m, 10.9m, 11m);      // below VWAP and EMA9 < EMA20 (SMA50 below: not major)

    /// <summary>PURPOSE: NO until the raw condition, then GRANTED with grantedSince. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void GrantsOnRawCondition()
    {
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Pullback(0), Raw(1) });
        Assert.False(result.Steps[0].Permission);
        Assert.Equal("NOT_GRANTED", result.Steps[0].Transition);
        Assert.Equal(4, result.Steps[0].BlockCode);   // close 10.8 is not above VWAP 11 (checked before EMA9)
        Assert.True(result.Latest!.Permission);
        Assert.Equal("GRANTED", result.Latest.Transition);
        Assert.Equal(Open.AddMinutes(5), result.GrantedSinceUtc);
    }

    /// <summary>PURPOSE: SLS case: below VWAP and EMA9 but EMA9 >= EMA20 keeps the latch YES. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void HoldsThroughPullbackWithoutStructuralDamage()
    {
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), Pullback(1), Pullback(2), Pullback(3) });
        Assert.All(result.Steps, s => Assert.True(s.Permission));
        Assert.Equal("HELD", result.Latest!.Transition);
        Assert.Equal(Open, result.GrantedSinceUtc);
        Assert.False(result.Latest.Raw);
    }

    /// <summary>PURPOSE: One damaged bar holds; the second consecutive one releases. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ReleasesAfterTwoBarsOfStructuralDamage()
    {
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), Damage(1), Damage(2), Raw(3) });
        Assert.True(result.Steps[1].Permission);
        Assert.Equal("HELD", result.Steps[1].Transition);
        Assert.False(result.Steps[2].Permission);
        Assert.Equal("RELEASED_STRUCTURAL_DAMAGE", result.Steps[2].Transition);
        Assert.Equal(3, result.Steps[2].BlockCode);
        Assert.True(result.Steps[3].Permission);          // raw again re-grants
        Assert.Equal(Open.AddMinutes(15), result.GrantedSinceUtc);
    }

    /// <summary>PURPOSE: Damage that is interrupted does not count as persistent. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void NonConsecutiveDamageDoesNotRelease()
    {
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), Damage(1), Pullback(2), Damage(3) });
        Assert.All(result.Steps, s => Assert.True(s.Permission));
    }

    /// <summary>PURPOSE: Two bars of major failure release with block code 2 (checked before structural damage). WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void MajorFailureReleasesWithCodeTwo()
    {
        var major = (int i) => B(i, 10m, 11m, 10.5m, 10.8m, sma50: 11m, rsi: 40m);
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), major(1), major(2) });
        Assert.Equal("RELEASED_MAJOR_FAILURE", result.Latest!.Transition);
        Assert.Equal(2, result.Latest.BlockCode);
        Assert.True(result.Latest.StructuralDamagePersistent);
    }

    /// <summary>PURPOSE: EOD window as installed: 19:55 ET in standard time, 18:55 ET in daylight time. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void EodWindowFollowsInstalledStudy()
    {
        var rules = new PermissionLatchConfig();
        Assert.False(FiveMinutePermissionLatch.IsEodWindow(DateTimeOffset.Parse("2026-11-17T19:50:00-05:00"), rules));
        Assert.True(FiveMinutePermissionLatch.IsEodWindow(DateTimeOffset.Parse("2026-11-17T19:55:00-05:00"), rules));
        Assert.False(FiveMinutePermissionLatch.IsEodWindow(DateTimeOffset.Parse("2026-07-15T18:50:00-04:00"), rules));
        Assert.True(FiveMinutePermissionLatch.IsEodWindow(DateTimeOffset.Parse("2026-07-15T18:55:00-04:00"), rules));
        Assert.False(FiveMinutePermissionLatch.IsEodWindow(DateTimeOffset.Parse("2026-11-17T15:55:00-05:00"), rules));

        var bars = new[] { Raw(0), new PermissionLatchBar(DateTimeOffset.Parse("2026-11-17T19:55:00-05:00"), 12m, 11m, 11.5m, 11m, 10m, 55m) };
        var result = FiveMinutePermissionLatch.Evaluate(bars);
        Assert.Equal("RELEASED_EOD", result.Latest!.Transition);
        Assert.Equal(1, result.Latest.BlockCode);
    }

    /// <summary>PURPOSE: The study's date-based DST rule for 2026 (Mar 8 and Nov 1 are the switch Sundays). WHEN CALLED: By xUnit.</summary>
    [Theory]
    [InlineData("2026-03-07", false)]
    [InlineData("2026-03-08", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-11-01", false)]
    [InlineData("2026-11-17", false)]
    public void StudyDaylightRule(string date, bool expected)
    {
        Assert.Equal(expected, FiveMinutePermissionLatch.StudyDaylightTime(DateOnly.Parse(date)));
    }

    /// <summary>PURPOSE: A new Eastern date restarts at NO (1 D chart) and clears the damage counter. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void NewEasternDateRestartsAtNo()
    {
        var nextMorning = new PermissionLatchBar(Open.AddDays(1), 10.8m, 11m, 11.2m, 11m, 10m, 55m);
        var result = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), nextMorning });
        Assert.False(result.Latest!.Permission);
        Assert.Null(result.GrantedSinceUtc);

        var carried = FiveMinutePermissionLatch.Evaluate(new[] { Raw(0), nextMorning }, new PermissionLatchConfig(ReleaseAtNewEasternDate: false));
        Assert.True(carried.Latest!.Permission);
    }

    /// <summary>PURPOSE: The O(n) permission series equals Calculate() at every index (exact decimals), incl. pre-market bars and two dates. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void PermissionSeriesMatchesCalculateExactly()
    {
        var bars = new List<MarketBar>();
        foreach (var day in new[] { "2026-11-16", "2026-11-17" })
        {
            var start = DateTimeOffset.Parse(day + "T08:00:00-05:00");   // 18 pre-market bars, then the session
            for (var i = 0; i < 18 + 78; i++)
            {
                var close = 10d + 0.37d * Math.Sin(i / 4.0) + 0.01d * (i % 7) + (day.EndsWith("17") ? 0.5d : 0d);
                bars.Add(new MarketBar("TEST", start.AddMinutes(5 * i), close - 0.03d, close + 0.06d, close - 0.07d, close,
                    i % 11 == 0 ? 0 : 1_000 + 37 * i, "Alpaca", "iex", "5m"));
            }
        }

        var series = LiveSymbolTechnicalCalculator.PermissionSeries(bars);
        Assert.Equal(bars.Count - 49, series.Count);
        for (var i = 49; i < bars.Count; i++)
        {
            var expected = LiveSymbolTechnicalCalculator.Calculate(bars, i, TimeSpan.FromMinutes(5), 0m);
            var actual = series[i - 49];
            Assert.Equal(expected.Timestamp, actual.Timestamp);
            Assert.Equal(expected.Close, actual.Close);
            Assert.Equal(expected.Vwap, actual.Vwap);
            Assert.Equal(expected.Ema9, actual.Ema9);
            Assert.Equal(expected.Ema20, actual.Ema20);
            Assert.Equal(expected.Sma50, actual.Sma50);
            Assert.Equal(expected.Rsi, actual.Rsi);
        }
    }

    /// <summary>PURPOSE: The engine uses a supplied latch value over the score, and falls back to score >= 5 without one. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void EngineUsesSuppliedLatchPermission()
    {
        var ts = DateTimeOffset.Parse("2026-09-08T13:00:00-04:00");
        // Score 4/6: close below VWAP and EMA9; EMA9 > EMA20 > SMA50; RSI 55; RVOL 3.
        var five = new BarSnapshot(ts, 10.8m, 10.8m, 10.8m, 10.8m, 1_000_000, 3m, 11m, 11.2m, 11m, 10m, 55m, 0.1m, 0.05m, 0.5m, 0.4m);
        var one = new BarSnapshot(ts, 10.8m, 10.8m, 10.8m, 10.8m, 1_000_000, 3m, 11m, 11.2m, 11m, 10m, 55m, 0.1m, 0.05m, 0.5m, 0.4m);
        Assert.Equal(4, V2SignalEngine.CalculateFiveMinuteScore(five));

        var latched = new V2SignalEngine().Evaluate(new CommandCenterSnapshot(ts, "TEST", one, five, MarketPhase.RegularSession, five, true), false);
        var legacy = new V2SignalEngine().Evaluate(new CommandCenterSnapshot(ts, "TEST", one, five, MarketPhase.RegularSession, five), false);

        Assert.True(latched.Evaluation!.FiveMinutePermission);
        Assert.False(legacy.Evaluation!.FiveMinutePermission);
        Assert.Contains("5M permission YES (TOS latch)", latched.Reason);
        Assert.DoesNotContain("TOS latch", legacy.Reason);
    }
}
