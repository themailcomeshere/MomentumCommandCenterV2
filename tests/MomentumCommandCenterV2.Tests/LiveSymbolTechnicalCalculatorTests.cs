using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// PURPOSE: Verifies the live OHLCV indicator calculations used by MCC 5M permission / 1M execution.
/// </summary>
public sealed class LiveSymbolTechnicalCalculatorTests
{
    [Fact]
    public void CalculateBuildsIndicatorSnapshotFromFiftyCompletedBars()
    {
        var start = DateTimeOffset.Parse("2026-10-28T09:30:00-04:00");
        var bars = Enumerable.Range(0, 60)
            .Select(index =>
            {
                var close = 20d + index * 0.05d;
                return new MarketBar("TEST", start.AddMinutes(index), close - 0.02d,
                    close + 0.08d, close - 0.08d, close, 10_000 + index,
                    "Alpaca", "iex", "1m");
            })
            .ToArray();

        var snapshot = LiveSymbolTechnicalCalculator.Calculate(
            bars, bars.Length - 1, TimeSpan.FromMinutes(1), 1.75m);

        Assert.True(snapshot.Ema9 > 0m);
        Assert.True(snapshot.Ema20 > 0m);
        Assert.True(snapshot.Sma50 > 0m);
        Assert.InRange(snapshot.Rsi, 0m, 100m);
        Assert.True(snapshot.Atr > 0m);
        Assert.Equal(1.75m, snapshot.Rvol5m);
        Assert.True(snapshot.Vwap > 0m);
    }

    [Fact]
    public void SameTimeOfDayRelativeVolumeRequiresFivePriorSessionSamples()
    {
        var dates = new[]
        {
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 2),
            new DateTime(2026, 10, 5),
            new DateTime(2026, 10, 6),
            new DateTime(2026, 10, 7),
            new DateTime(2026, 10, 8)
        };
        var bars = dates.Select((date, index) =>
        {
            var timestamp = new DateTimeOffset(date.Year, date.Month, date.Day, 9, 30, 0, TimeSpan.FromHours(-4));
            return new MarketBar("TEST", timestamp, 10d, 10.1d, 9.9d, 10d,
                index == dates.Length - 1 ? 2_000 : 1_000, "Alpaca", "sip", "5m");
        }).ToArray();

        var result = LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(bars, bars.Length - 1);

        Assert.Equal(5, result.Samples);
        Assert.Equal(2m, result.Value);
    }

    [Fact]
    public void SameTimeOfDayRelativeVolumeDoesNotInventBaselineWithFewerThanFiveSessions()
    {
        var dates = new[]
        {
            new DateTime(2026, 10, 5),
            new DateTime(2026, 10, 6),
            new DateTime(2026, 10, 7),
            new DateTime(2026, 10, 8)
        };
        var bars = dates.Select(date =>
        {
            var timestamp = new DateTimeOffset(date.Year, date.Month, date.Day, 9, 30, 0, TimeSpan.FromHours(-4));
            return new MarketBar("TEST", timestamp, 10d, 10.1d, 9.9d, 10d,
                1_000, "Alpaca", "sip", "5m");
        }).ToArray();

        var result = LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(bars, bars.Length - 1);

        Assert.Null(result.Value);
        Assert.Equal(3, result.Samples);
    }

    [Theory]
    [InlineData("2026-10-28T09:30:00-04:00", true)]
    [InlineData("2026-10-28T15:59:00-04:00", true)]
    [InlineData("2026-10-28T16:00:00-04:00", false)]
    [InlineData("2026-10-28T08:00:00-04:00", false)]
    [InlineData("2026-10-31T10:00:00-04:00", false)]
    public void RegularSessionUsesEasternClockAndExcludesWeekends(string timestamp, bool expected)
    {
        Assert.Equal(expected, LiveSymbolTechnicalCalculator.IsRegularSession(DateTimeOffset.Parse(timestamp)));
    }
}
