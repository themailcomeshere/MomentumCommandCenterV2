using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: MarketRegimeTests
/// PURPOSE: Verifies the header market chip: per-symbol trend rules, overall status, the event flag (today / next trading
/// session across a weekend), parsing of a live-assessment payload, and that the committed App_Data/macro-events.json
/// loads with the expected official dates. Payloads are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class MarketRegimeTests
{
    /// <summary>PURPOSE: Per-symbol states. WHEN CALLED: By xUnit once per row.</summary>
    [Theory]
    [InlineData(true, true, true, "BULLISH")]
    [InlineData(false, false, true, "BEARISH")]
    [InlineData(true, false, true, "NEUTRAL")]
    [InlineData(true, true, false, "DATA?")]
    [InlineData(null, true, true, "DATA?")]
    public void SymbolState_Rules(bool? aboveVwap, bool? emaStack, bool? fresh, string expected)
    {
        Assert.Equal(expected, MarketRegimeClassifier.SymbolState(aboveVwap, emaStack, fresh));
    }

    /// <summary>PURPOSE: Overall status. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Overall_Rules()
    {
        Assert.Equal("RISK_ON", MarketRegimeClassifier.Overall(new[] { "BULLISH", "BULLISH" }, true));
        Assert.Equal("RISK_OFF", MarketRegimeClassifier.Overall(new[] { "BEARISH", "BEARISH" }, true));
        Assert.Equal("MIXED", MarketRegimeClassifier.Overall(new[] { "BULLISH", "NEUTRAL" }, true));
        Assert.Equal("DATA?", MarketRegimeClassifier.Overall(new[] { "BULLISH", "DATA?" }, true));
        Assert.Equal("CLOSED", MarketRegimeClassifier.Overall(new[] { "BULLISH", "BULLISH" }, false));
    }

    /// <summary>PURPOSE: Event flag: today, next trading session across a weekend, none. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void EventRisk_TodayNextSessionNone()
    {
        var cpiMonday = new MacroEvent(new DateOnly(2026, 11, 16), "08:30", "CPI", "CPI", false);
        var events = new[] { cpiMonday };
        var fridayNoonEt = new DateTimeOffset(2026, 11, 13, 17, 0, 0, TimeSpan.Zero);
        var mondayNoonEt = new DateTimeOffset(2026, 11, 16, 17, 0, 0, TimeSpan.Zero);
        var tuesdayNoonEt = new DateTimeOffset(2026, 11, 17, 17, 0, 0, TimeSpan.Zero);

        Assert.Equal("NEXT_SESSION", MarketRegimeClassifier.EventRisk(events, fridayNoonEt).Risk);   // Fri -> Mon
        Assert.Equal("TODAY", MarketRegimeClassifier.EventRisk(events, mondayNoonEt).Risk);
        Assert.Equal("NONE", MarketRegimeClassifier.EventRisk(events, tuesdayNoonEt).Risk);
    }

    /// <summary>PURPOSE: Reads the 5m criteria, freshness and price from a live-assessment payload. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FromAssessment_ReadsCriteria()
    {
        var json = JsonSerializer.SerializeToElement(new
        {
            evaluationStatus = "EVALUATED",
            oneMinute = new { close = 600.5, vwap = 598.2 },
            freshnessDiagnostics = new { fiveMinute = new { isFresh = true } },
            fiveMinute = new
            {
                scoreBreakdown = new
                {
                    criteria = new object[]
                    {
                        new { key = "closeAboveVwap", passed = true },
                        new { key = "ema9AboveEma20", passed = true },
                        new { key = "closeAboveEma9", passed = false },
                        new { key = "ema20AboveSma50", passed = (bool?)null }
                    }
                }
            }
        });

        var state = MarketRegimeService.FromAssessment("SPY", json);

        Assert.Equal("BULLISH", state.State);
        Assert.False(state.CloseAboveEma9);
        Assert.Null(state.Ema20AboveSma50);
        Assert.Equal(600.5, state.LastClose);
    }

    /// <summary>PURPOSE: The committed event file loads, is sorted, and contains known official dates. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void CommittedEventFile_LoadsOfficialDates()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.sln").Any()) dir = dir.Parent;
        Assert.NotNull(dir);
        var path = Path.Combine(dir!.FullName, "src", "MomentumCommandCenterV2.API", "App_Data", "macro-events.json");

        var events = MarketRegimeClassifier.LoadEvents(path);

        Assert.Equal(44, events.Count);
        Assert.Equal(events.OrderBy(e => e.Date).Select(e => e.Date), events.Select(e => e.Date));
        Assert.Contains(events, e => e.Kind == "FOMC" && e.Date == new DateOnly(2026, 12, 9) && !e.Tentative);
        Assert.Contains(events, e => e.Kind == "CPI" && e.Date == new DateOnly(2026, 12, 10) && e.TimeEt == "08:30");
        Assert.Contains(events, e => e.Kind == "JOBS" && e.Date == new DateOnly(2026, 12, 4));
        Assert.All(events.Where(e => e.Kind == "FOMC" && e.Date.Year == 2027), e => Assert.True(e.Tentative));
        // 08:30 ET on 2026-12-10 (standard time) = 13:30 UTC = 06:30 Arizona; on 2027-06-10 (daylight) = 12:30 UTC = 05:30 Arizona.
        Assert.Equal(new DateTimeOffset(2026, 12, 10, 13, 30, 0, TimeSpan.Zero), events.Single(e => e.Kind == "CPI" && e.Date == new DateOnly(2026, 12, 10)).AtUtc);
        Assert.Equal(new DateTimeOffset(2027, 6, 10, 12, 30, 0, TimeSpan.Zero), events.Single(e => e.Kind == "CPI" && e.Date == new DateOnly(2027, 6, 10)).AtUtc);
        Assert.Null(events.First(e => e.Kind == "FOMC").AtUtc);
    }
}
