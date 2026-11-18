using MomentumCommandCenterV2.API.MarketData.AutoScan;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: AutoMomentumScanTests
/// PURPOSE: Verifies the automatic scanner: Thinkorswim formula details, each scan's PASS / FAIL / UNKNOWN outcome,
/// freshness and exclusion flags, universe merging, provider-response parsing and the TOS side-by-side comparison.
/// All bars and symbols are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class AutoMomentumScanTests
{
    // 2026-11-13 (Friday) 09:30 ET = 14:30 UTC (Eastern standard time).
    private static readonly DateTimeOffset SessionOpen = new(2026, 11, 13, 14, 30, 0, TimeSpan.Zero);

    /// <summary>PURPOSE: Builds steadily rising synthetic 5m bars with a volume spike and breakout on the last bar. WHEN CALLED: By tests.</summary>
    private static List<AutoScanBar> RisingBars(int count = 60, long lastVolume = 40_000)
    {
        var bars = new List<AutoScanBar>();
        for (var i = 0; i < count; i++)
        {
            var close = 10m + 0.05m * i;
            bars.Add(new AutoScanBar(SessionOpen.AddMinutes(5 * i), close - 0.01m, close + 0.02m, close - 0.05m, close,
                i == count - 1 ? lastVolume : 10_000, null));
        }
        return bars;
    }

    /// <summary>PURPOSE: Builds a universe row. WHEN CALLED: By tests.</summary>
    private static AutoScanUniverseRow Row(decimal? last = 13m, long? dayVolume = 600_000, decimal? pct = 5m) =>
        new("TSTA", last, "iex_latest_trade", dayVolume, 12.38m, "sip_delayed_daily", pct, new[] { "MOST_ACTIVE_VOLUME" });

    /// <summary>PURPOSE: Finds one scanner result by code. WHEN CALLED: By tests.</summary>
    private static string Result(IReadOnlyList<AutoScanScannerResult> results, string code) => results.Single(r => r.Code == code).Result;

    /// <summary>PURPOSE: Thinkorswim ExpAverage is seeded with the first value. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void TosEma_SeedsWithFirstValue()
    {
        Assert.Equal(10.4m, AutoMomentumScanRules.TosEma(new[] { 10m, 12m }, 9));
    }

    /// <summary>PURPOSE: A rising trend with a volume spike and breakout passes every scan. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void RisingBreakoutWithVolumeSpike_PassesAllFiveScans()
    {
        var indicators = AutoMomentumScanRules.ComputeIndicators(RisingBars());
        var results = AutoMomentumScanRules.EvaluateScanners(Row(), indicators);

        foreach (var code in new[] { "1", "2A", "2B", "PIV", "D" })
            Assert.Equal("PASS", Result(results, code));
        Assert.Equal(4, AutoMomentumScanRules.MomentumPassCount(results));
        Assert.Contains("IEX_VOLUME_APPROX", AutoMomentumScanRules.Tags(results, indicators));
    }

    /// <summary>PURPOSE: Unknown consolidated day volume makes volume-gated scans UNKNOWN, never PASS. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void UnknownDayVolume_LeavesVolumeGatedScansUnknown()
    {
        var results = AutoMomentumScanRules.EvaluateScanners(Row(dayVolume: null), AutoMomentumScanRules.ComputeIndicators(RisingBars()));

        Assert.Equal("UNKNOWN", Result(results, "2A"));
        Assert.Equal("UNKNOWN", Result(results, "PIV"));
        Assert.Equal("UNKNOWN", Result(results, "D"));
        Assert.Equal("PASS", Result(results, "1"));
    }

    /// <summary>PURPOSE: Price outside the scan range fails even when bars pass. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void PriceBelowMinimum_FailsScans()
    {
        var results = AutoMomentumScanRules.EvaluateScanners(Row(last: 2.5m), AutoMomentumScanRules.ComputeIndicators(RisingBars()));

        Assert.All(results, r => Assert.Equal("FAIL", r.Result));
    }

    /// <summary>PURPOSE: A falling series fails the trend scans. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void FallingSeries_FailsTrendScans()
    {
        var bars = RisingBars().Select((b, i) => b with { Close = 20m - 0.05m * i, High = 20.02m - 0.05m * i, Low = 19.95m - 0.05m * i }).ToList();
        var results = AutoMomentumScanRules.EvaluateScanners(Row(), AutoMomentumScanRules.ComputeIndicators(bars));

        Assert.Equal("FAIL", Result(results, "1"));
        Assert.Equal("FAIL", Result(results, "2B"));
        Assert.Equal("FAIL", Result(results, "PIV"));
    }

    /// <summary>PURPOSE: Too little history leaves indicator conditions unknown instead of guessing. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ShortHistory_LeavesIndicatorConditionsUnknown()
    {
        var indicators = AutoMomentumScanRules.ComputeIndicators(RisingBars(10));
        var results = AutoMomentumScanRules.EvaluateScanners(Row(), indicators);

        Assert.Null(indicators.Sma50);
        Assert.Null(indicators.AverageVolume20);
        Assert.Equal("UNKNOWN", Result(results, "1"));
        Assert.Equal("PASS", Result(results, "D"));
    }

    /// <summary>PURPOSE: Session VWAP resets at each Eastern session and uses the provider bar VWAP when present. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void SessionVwap_ResetsEachSessionAndUsesBarVwap()
    {
        var bars = new List<AutoScanBar>
        {
            new(SessionOpen, 10m, 11m, 9m, 10m, 100, 10m),
            new(SessionOpen.AddMinutes(5), 10m, 13m, 11m, 12m, 300, 12m),
            new(SessionOpen.AddDays(3), 20m, 21m, 19m, 20m, 50, null)
        };
        var vwap = AutoMomentumScanRules.SessionVwapSeries(bars);

        Assert.Equal(10m, vwap[0]);
        Assert.Equal(11.5m, vwap[1]);
        Assert.Equal(20m, vwap[2]);
    }

    /// <summary>PURPOSE: Missing IEX 5m slots are counted, not filled. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void MissingSlots_AreCounted()
    {
        var bars = RisingBars();
        bars.RemoveAt(40);

        Assert.Equal(1, AutoMomentumScanRules.CountMissingSlots(bars, 50));
        Assert.Contains("IEX_GAPS_1", AutoMomentumScanRules.Tags(Array.Empty<AutoScanScannerResult>(), AutoMomentumScanRules.ComputeIndicators(bars)));
    }

    /// <summary>PURPOSE: Fresh, passing, not excluded = flagged; stale or excluded = not flagged. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Evaluate_FlagsOnlyFreshNotExcludedPasses()
    {
        var bars = RisingBars();
        var lastClose = bars[^1].Timestamp.AddMinutes(5);

        var fresh = AutoMomentumScanService.Evaluate(Row(), bars, excluded: false, lastClose.AddSeconds(60));
        var stale = AutoMomentumScanService.Evaluate(Row(), bars, excluded: false, lastClose.AddMinutes(30));
        var excluded = AutoMomentumScanService.Evaluate(Row(), bars, excluded: true, lastClose.AddSeconds(60));

        Assert.True(fresh.Flagged);
        Assert.False(stale.Flagged);
        Assert.Contains("STALE_BARS", stale.Tags);
        Assert.False(excluded.Flagged);
        Assert.Contains("EXCLUDED_WATCHLIST", excluded.Tags);
    }

    /// <summary>PURPOSE: The universe merges the three screener lists and keeps consolidated volume. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void MergeUniverse_CombinesListsAndKeepsVolume()
    {
        var merged = AutoMomentumScanService.MergeUniverse(
            new[] { new AutoScanScreenerRow("AAA", 900_000, 5000, null, null) },
            new[] { new AutoScanScreenerRow("AAA", 900_000, 5000, null, null), new AutoScanScreenerRow("BBB", 300_000, 9000, null, null) },
            new[] { new AutoScanScreenerRow("CCC", null, null, 12m, 5m) });

        Assert.Equal(3, merged.Count);
        Assert.Equal(900_000, merged["AAA"].Volume);
        Assert.Equal(new[] { "MOST_ACTIVE_VOLUME", "MOST_ACTIVE_TRADES" }, merged["AAA"].Sources);
        Assert.Null(merged["CCC"].Volume);
    }

    /// <summary>PURPOSE: The comparison separates found, seen-not-flagged and never-seen TOS symbols. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Compare_SplitsTosSymbolsByWhatMccSaw()
    {
        var session = new DateOnly(2026, 11, 13);
        var runs = new[]
        {
            new AutoScanLogEntry(SessionOpen.AddMinutes(10), session, true, "COMPLETED", new[] { "AAA", "BBB", "EEE" }, new[] { "AAA", "EEE" }, new Dictionary<string, string>())
        };
        var comparison = AutoMomentumScanService.Compare(new[] { "aaa", "BBB", "CCC", "DDD" }, runs, session, "watchlist.csv", SessionOpen.AddMinutes(20));

        Assert.Equal(new[] { "AAA" }, comparison.FlaggedByMcc);
        Assert.Equal(new[] { "BBB" }, comparison.SeenButNotFlagged);
        Assert.Equal(new[] { "CCC", "DDD" }, comparison.NotInMccUniverse);
        Assert.Equal(new[] { "EEE" }, comparison.FlaggedOnlyByMcc);
        Assert.Equal(1, comparison.ScanRunsThatSession);
    }

    /// <summary>PURPOSE: Screener and snapshot responses parse into rows. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void Parsers_ReadScreenerAndSnapshotResponses()
    {
        var actives = AlpacaAutoScanClient.ParseMostActives("""{"most_actives":[{"symbol":"aaa","volume":1234567,"trade_count":890}],"last_updated":"2026-11-13T15:00:00Z"}""");
        var gainers = AlpacaAutoScanClient.ParseGainers("""{"gainers":[{"symbol":"BBB","percent_change":12.5,"change":0.5,"price":4.5}],"losers":[{"symbol":"ZZZ","percent_change":-9}]}""");
        var snaps = AlpacaAutoScanClient.ParseSnapshots("""{"AAA":{"latestTrade":{"p":12.34,"t":"2026-11-13T15:00:01Z"},"prevDailyBar":{"c":11.9}}}""");

        Assert.Equal(("AAA", 1234567L, 890L), (actives[0].Symbol, actives[0].Volume!.Value, actives[0].TradeCount!.Value));
        Assert.Single(gainers);
        Assert.Equal(12.5m, gainers[0].PercentChange);
        Assert.Equal(12.34m, snaps["AAA"].LastPrice);
        Assert.Equal(11.9m, snaps["AAA"].IexPreviousClose);
    }

    /// <summary>PURPOSE: The bar parser keeps only completed regular-session bars and returns the page token. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ParseBars_KeepsCompletedRegularSessionBarsOnly()
    {
        const string body = """
        {"bars":{"AAA":[
          {"t":"2026-11-13T14:00:00Z","o":1,"h":1,"l":1,"c":1,"v":10,"vw":1},
          {"t":"2026-11-13T14:30:00Z","o":2,"h":2,"l":2,"c":2,"v":20,"vw":2},
          {"t":"2026-11-13T14:35:00Z","o":3,"h":3,"l":3,"c":3,"v":30,"vw":3}
        ]},"next_page_token":"abc"}
        """;
        var into = new Dictionary<string, List<AutoScanBar>>();
        var token = AlpacaAutoScanClient.ParseBars(body, into, new DateTimeOffset(2026, 11, 13, 14, 38, 0, TimeSpan.Zero), TimeSpan.FromMinutes(5));

        Assert.Equal("abc", token);
        var bar = Assert.Single(into["AAA"]);
        Assert.Equal(2m, bar.Close);
    }

    /// <summary>
    /// PURPOSE: Chase rules (2026-11-18). An SLS-like gap that fades (open 24.11, sold to ~17, back to 20.60, +175% vs
    /// the prior close) is GAP_FADING and OFF_HIGH; a fresh, choppy riser near its high is not a chase. Synthetic bars.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void ChaseReasons_GapFadingAndFreshMover()
    {
        var gap = new List<AutoScanBar>();
        for (var i = 0; i < 30; i++)
        {
            var close = i < 6 ? 24.11m - 1.2m * i : 17m + 0.15m * (i - 6);   // 24.11 → ~18 → 20.60
            var open = i == 0 ? 24.11m : close + 0.1m;
            gap.Add(new AutoScanBar(SessionOpen.AddMinutes(5 * i), open, Math.Max(open, close) + 0.3m, Math.Min(open, close) - 0.3m, close, 50_000, null));
        }
        var gapRow = new AutoScanUniverseRow("GAPF", 20.60m, "iex_latest_trade", 5_000_000, 7.47m, "sip_delayed_daily", 175.77m, new[] { "TOP_GAINERS" });
        var gapReasons = AutoMomentumScanRules.ChaseReasons(gapRow, AutoMomentumScanRules.ComputeIndicators(gap));
        Assert.Contains("GAP_FADING", gapReasons);
        Assert.Contains("OFF_HIGH", gapReasons);

        var fresh = new List<AutoScanBar>();
        for (var i = 0; i < 30; i++)
        {
            var close = 10m + 0.01m * i + (i % 2 == 0 ? 0.15m : -0.15m);
            fresh.Add(new AutoScanBar(SessionOpen.AddMinutes(5 * i), close - 0.05m, close + 0.2m, close - 0.2m, close, 20_000, null));
        }
        var freshIndicators = AutoMomentumScanRules.ComputeIndicators(fresh);
        Assert.Empty(AutoMomentumScanRules.ChaseReasons(Row(last: fresh[^1].Close, pct: 5m), freshIndicators));
        Assert.Equal(10.10m, freshIndicators.SessionOpen);   // first bar: close 10.15, open close - 0.05
        Assert.NotNull(freshIndicators.Atr14);
    }

    /// <summary>PURPOSE: EXTENDED when price is 1.5 ATR or more above VWAP; unknown inputs add nothing. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ChaseReasons_ExtendedAndUnknown()
    {
        var x = new AutoScanIndicators(30, SessionOpen, 12m, 11.9m, 10m, 10m, null, null, null, null, null, null, null, null, 0,
            SessionOpen: 9.5m, SessionHigh: 12.1m, Atr14: 1m);
        Assert.Equal(new[] { "EXTENDED" }, AutoMomentumScanRules.ChaseReasons(Row(last: 11.6m, pct: 5m), x));      // (11.6-10)/1 = 1.6
        Assert.Empty(AutoMomentumScanRules.ChaseReasons(Row(last: 11.4m, pct: 5m), x));                          // 1.4
        var unknown = x with { SessionOpen = null, SessionHigh = null, Atr14 = null };
        Assert.Empty(AutoMomentumScanRules.ChaseReasons(Row(last: 20m, pct: 50m), unknown));
    }

    /// <summary>
    /// PURPOSE: Ranking: a flagged chase with a bigger % change ranks after a flagged fresh candidate; among fresh
    /// candidates with equal scans, the larger move since the open ranks first. WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void Rank_ChasesAfterFreshCandidates()
    {
        AutoScanCandidate C(string symbol, decimal pct, decimal? fromOpen, params string[] chase) => new(
            new AutoScanUniverseRow(symbol, 10m, "iex_latest_trade", 1_000_000, 9m, "sip_delayed_daily", pct, new[] { "TOP_GAINERS" }),
            AutoMomentumScanRules.ComputeIndicators(new List<AutoScanBar>()),
            new[] { new AutoScanScannerResult(AutoMomentumScanRules.MarketMovers, "D", "PASS", false, Array.Empty<AutoScanCondition>()) },
            Array.Empty<string>(), 0, true, false, 60, chase, fromOpen);

        var ranked = AutoMomentumScanService.Rank(new[]
        {
            C("SLSX", 175m, -15m, "GAP_FADING", "OFF_HIGH"),
            C("SLOW", 4m, 1m),
            C("FAST", 6m, 5m),
        });
        Assert.Equal(new[] { "FAST", "SLOW", "SLSX" }, ranked.Select(c => c.Universe.Symbol));
    }
}
