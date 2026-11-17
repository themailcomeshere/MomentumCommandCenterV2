using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: EarningsCalendarTests
/// PURPOSE: Verifies parsing of the Nasdaq earnings calendar response shape: timing mapping, market cap, EPS estimate,
/// skipped blank symbols, and a null "rows" (weekend or empty day) as an empty list. Synthetic responses only.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class EarningsCalendarTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T22:00:00Z");

    /// <summary>PURPOSE: Maps the three timing values and parses numbers. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ParsesRowsAndTimings()
    {
        const string json = """
        {"data":{"rows":[
          {"symbol":"AAA","name":"Aaa Corp","time":"time-after-hours","marketCap":"$3,762,974,100,000","epsForecast":"$2.49","fiscalQuarterEnding":"Oct/2026"},
          {"symbol":"bbb","name":"Bbb Inc","time":"time-pre-market","marketCap":"$40,000,000","epsForecast":""},
          {"symbol":"CCC","name":"Ccc Ltd","time":"time-not-supplied","marketCap":"N/A"},
          {"symbol":"","name":"Blank"}
        ]}}
        """;
        var day = EarningsCalendarService.Parse(new DateOnly(2026, 11, 17), json, Now);

        Assert.Equal(new[] { "AAA", "BBB", "CCC" }, day.Rows.Select(r => r.Symbol));
        Assert.Equal(EarningsTiming.AfterClose, day.Rows[0].Timing);
        Assert.Equal(3_762_974_100_000m, day.Rows[0].MarketCap);
        Assert.Equal("$2.49", day.Rows[0].EpsForecast);
        Assert.Equal(EarningsTiming.BeforeOpen, day.Rows[1].Timing);
        Assert.Null(day.Rows[1].EpsForecast);
        Assert.Equal(EarningsTiming.NotSupplied, day.Rows[2].Timing);
        Assert.Null(day.Rows[2].MarketCap);
    }

    /// <summary>PURPOSE: A null rows array is an empty day, not an error. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void NullRowsIsEmptyDay()
    {
        var day = EarningsCalendarService.Parse(new DateOnly(2026, 11, 21), """{"data":{"rows":null}}""", Now);
        Assert.Empty(day.Rows);
    }
}
