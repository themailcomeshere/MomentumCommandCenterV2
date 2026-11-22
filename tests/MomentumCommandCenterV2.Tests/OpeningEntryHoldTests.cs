using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OpeningEntryHoldTests
/// PURPOSE: Verifies the Intraday Momentum opening entry hold (operator request 2026-11-21): the window is
/// [09:30 ET, 09:30 ET + N min) in both standard and daylight time; inside it a fresh entry that meets every condition
/// becomes PREPARE TO BUY with an OPENING HOLD reason and OpeningHoldWithheld = true; outside it, or with N = 0, the
/// engine is unchanged; held positions and exits are never delayed; invalid minutes are refused; the settings store
/// persists the operator's value. Synthetic fixtures only.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OpeningEntryHoldTests
{
    // 2026-11-18 is Eastern standard time: 09:30 ET = 14:30 UTC = 07:30 AZ.
    private static readonly DateTimeOffset StandardOpen = DateTimeOffset.Parse("2026-11-18T14:30:00Z");
    // 2026-09-08 is Eastern daylight time: 09:30 ET = 13:30 UTC = 06:30 AZ.
    private static readonly DateTimeOffset DaylightOpen = DateTimeOffset.Parse("2026-09-08T13:30:00Z");

    /// <summary>PURPOSE: Builds a bar snapshot with the given indicator values. WHEN CALLED: By the fixtures below.</summary>
    private static BarSnapshot Bar(decimal c, decimal v, decimal e9, decimal e20, decimal s50, decimal rvol, decimal rsi,
        decimal macd, decimal sig, decimal atr, DateTimeOffset ts) =>
        new(ts, c, c, c, c, 1_000_000, rvol, v, e9, e20, s50, rsi, macd, sig, atr, atr > 0m ? Math.Max(0m, (c - e9) / atr) : 0m);

    /// <summary>PURPOSE: A snapshot where every fresh-entry condition passes (BUY without the hold). WHEN CALLED: By tests.</summary>
    private static CommandCenterSnapshot EntrySnapshot(DateTimeOffset ts) => new(
        ts, "TEST",
        Bar(110, 105, 108, 106, 100, 3, 60, 2, 1, 2, ts),
        Bar(110, 100, 108, 105, 100, 3, 60, 2, 1, 1, ts),
        MarketPhase.RegularSession);

    private static V2SignalEngine Engine(int holdMinutes) => new(new V2Config(OpeningEntryHoldMinutes: holdMinutes));

    /// <summary>PURPOSE: Window boundaries in standard time, including the minute count. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void WindowStartsAtTheOpenAndReleasesAfterTheConfiguredMinutes_StandardTime()
    {
        var atOpen = OpeningEntryHold.Evaluate(StandardOpen, 60);
        Assert.True(atOpen.Active);
        Assert.Equal(StandardOpen, atOpen.RegularOpenUtc);
        Assert.Equal(StandardOpen.AddMinutes(60), atOpen.ReleaseUtc);
        Assert.Equal(60, atOpen.MinutesRemaining);
        Assert.Equal(new DateOnly(2026, 11, 18), atOpen.EasternDate);

        Assert.False(OpeningEntryHold.Evaluate(StandardOpen.AddSeconds(-1), 60).Active);   // pre-market
        Assert.Equal(1, OpeningEntryHold.Evaluate(StandardOpen.AddMinutes(59).AddSeconds(30), 60).MinutesRemaining);
        Assert.False(OpeningEntryHold.Evaluate(StandardOpen.AddMinutes(60), 60).Active);   // released at 10:30 ET exactly
    }

    /// <summary>PURPOSE: The open is found from the Eastern clock in daylight time too (no fixed offset). WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void WindowFollowsEasternDaylightTime()
    {
        var status = OpeningEntryHold.Evaluate(DaylightOpen.AddMinutes(15), 60);
        Assert.True(status.Active);
        Assert.Equal(DaylightOpen, status.RegularOpenUtc);
        Assert.Equal(DaylightOpen.AddMinutes(60), status.ReleaseUtc);
    }

    /// <summary>PURPOSE: 0 minutes turns the hold off; values outside 0–120 are refused. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ZeroIsOffAndOutOfRangeMinutesAreRefused()
    {
        var off = OpeningEntryHold.Evaluate(StandardOpen.AddMinutes(5), 0);
        Assert.False(off.Enabled);
        Assert.False(off.Active);
        Assert.Equal(0, off.MinutesRemaining);

        Assert.Throws<ArgumentOutOfRangeException>(() => OpeningEntryHold.Evaluate(StandardOpen, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OpeningEntryHold.Evaluate(StandardOpen, OpeningEntryHold.MaxMinutes + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Engine(OpeningEntryHold.MaxMinutes + 1));
        Assert.True(OpeningEntryHold.IsValidMinutes(OpeningEntryHold.MaxMinutes));
    }

    /// <summary>
    /// PURPOSE: Inside the hold a qualifying entry is PREPARE TO BUY with the OPENING HOLD reason (ET and AZ release time)
    /// and OpeningHoldWithheld; the same snapshot is BUY with the hold off, after release, or with a shorter hold.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void QualifyingEntryInsideHoldIsWithheld()
    {
        var at1000 = EntrySnapshot(StandardOpen.AddMinutes(30));           // 10:00 ET / 08:00 AZ

        var held = Engine(60).Evaluate(at1000, false);
        Assert.Equal(SignalState.PrepareBuy, held.State);
        Assert.Equal(MomentumAction.PREPARE_BUY, held.Action);
        Assert.False(held.EntryAllowed);
        Assert.True(held.LongPermission);
        Assert.True(held.Evaluation!.OpeningHoldWithheld);
        Assert.StartsWith("OPENING HOLD:", held.Reason);
        Assert.Contains("until 10:30 ET (08:30 AZ)", held.Reason);

        Assert.Equal(SignalState.Buy, Engine(0).Evaluate(at1000, false).State);
        Assert.Equal(SignalState.Buy, new V2SignalEngine().Evaluate(at1000, false).State);   // engine default is off
        Assert.Equal(SignalState.Buy, Engine(30).Evaluate(at1000, false).State);              // released at 10:00 ET
        Assert.Equal(SignalState.Buy, Engine(60).Evaluate(EntrySnapshot(StandardOpen.AddMinutes(60)), false).State);
    }

    /// <summary>PURPOSE: The AZ release time is converted for daylight time (07:30 AZ, not 08:30). WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void DaylightReasonShowsArizonaReleaseTime()
    {
        var held = Engine(60).Evaluate(EntrySnapshot(DaylightOpen.AddMinutes(15)), false);
        Assert.Equal(SignalState.PrepareBuy, held.State);
        Assert.Contains("until 10:30 ET (07:30 AZ)", held.Reason);
    }

    /// <summary>
    /// PURPOSE: Held positions are evaluated exactly as without the hold (exits never wait), and a snapshot that does
    /// not qualify for entry is unchanged by the hold.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void HoldNeverChangesHeldPositionsOrNonQualifyingEntries()
    {
        var ts = StandardOpen.AddMinutes(10);
        var entry = EntrySnapshot(ts);
        var withHold = Engine(60);
        var noHold = Engine(0);

        Assert.Equal(noHold.Evaluate(entry, true).State, withHold.Evaluate(entry, true).State);

        var extended = entry with { OneMinute = entry.OneMinute with { AtrExtension = 5m } };   // hard ATR extension
        var exit = withHold.Evaluate(extended, true);
        Assert.Equal(MomentumAction.SELL, exit.Action);
        Assert.True(exit.HardExit);

        var weakOne = entry with { OneMinute = entry.OneMinute with { Macd = 0m, MacdSignal = 1m } };   // 1M not confirmed
        var a = withHold.Evaluate(weakOne, false);
        var b = noHold.Evaluate(weakOne, false);
        Assert.Equal(b.State, a.State);
        Assert.Equal(b.Reason, a.Reason);
        Assert.False(a.Evaluation?.OpeningHoldWithheld ?? false);
    }

    /// <summary>
    /// PURPOSE: The committed appsettings default is 15 minutes (operator decision 2026-11-21 after the evidence review)
    /// and a store built from it holds 09:30–09:45 ET; 09:45 ET is EXECUTION READY again.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public void CommittedDefaultIsFifteenMinutes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.sln").Any()) dir = dir.Parent;
        Assert.NotNull(dir);
        var appsettings = Path.Combine(dir!.FullName, "src", "MomentumCommandCenterV2.API", "appsettings.json");
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(appsettings));
        var minutes = json.RootElement.GetProperty("MarketData").GetProperty("Intraday").GetProperty("OpeningEntryHoldMinutes").GetInt32();
        Assert.Equal(15, minutes);

        Assert.Equal(SignalState.PrepareBuy, Engine(minutes).Evaluate(EntrySnapshot(StandardOpen.AddMinutes(14)), false).State);
        Assert.Equal(SignalState.Buy, Engine(minutes).Evaluate(EntrySnapshot(StandardOpen.AddMinutes(15)), false).State);
        Assert.Contains("until 09:45 ET (07:45 AZ)", Engine(minutes).Evaluate(EntrySnapshot(StandardOpen.AddMinutes(5)), false).Reason);
    }

    /// <summary>
    /// PURPOSE: The settings store starts from the configured default, persists the operator's value across instances,
    /// refuses out-of-range values and ignores a corrupt file.
    /// WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public async Task SettingsStorePersistsOperatorValue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mcc-intraday-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "intraday-settings.json");
        try
        {
            var store = new IntradaySettingsStore(path, 60);
            Assert.Equal(60, store.Current.OpeningEntryHoldMinutes);
            Assert.Equal("APPSETTINGS_DEFAULT", store.Current.Source);

            var now = DateTimeOffset.Parse("2026-11-21T20:00:00Z");
            var saved = await store.SetOpeningEntryHoldAsync(30, now, CancellationToken.None);
            Assert.Equal(30, saved.OpeningEntryHoldMinutes);
            Assert.Equal(30, store.Current.OpeningEntryHoldMinutes);

            var reloaded = new IntradaySettingsStore(path, 60);
            Assert.Equal(30, reloaded.Current.OpeningEntryHoldMinutes);
            Assert.Equal("OPERATOR_SETTING", reloaded.Current.Source);
            Assert.Equal(now, reloaded.Current.UpdatedAtUtc);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.SetOpeningEntryHoldAsync(121, now, CancellationToken.None));
            Assert.Equal(30, store.Current.OpeningEntryHoldMinutes);

            await File.WriteAllTextAsync(path, "{ not json");
            Assert.Equal(60, new IntradaySettingsStore(path, 60).Current.OpeningEntryHoldMinutes);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
