using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: LiveSignalLogStoreTests
/// PURPOSE: Verifies the live signal audit log: the first observation of a symbol is FIRST_SEEN, an unchanged state is not
/// written again, a change is written with the previous key, entries persist across store instances, and reads filter by
/// Eastern date and symbol. Synthetic observations (2026-11-18, Eastern standard time).
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class LiveSignalLogStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mcc-signal-log-" + Guid.NewGuid().ToString("N"));

    /// <summary>PURPOSE: Removes the temporary folder. WHEN CALLED: By xUnit after each test.</summary>
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private static LiveSignalObservation Obs(string symbol, string at, string? state, string? held = "SELL - BREAKDOWN") => new(
        symbol, DateTimeOffset.Parse(at), "EVALUATED", state, state == "EXECUTION READY", held, 5, true, 28.13m, 8, 15,
        DateTimeOffset.Parse(at).AddMinutes(-1), DateTimeOffset.Parse(at).AddMinutes(-5), "iex", "test reason",
        state == "EXECUTION READY" ? "ReadyHighOpportunity" : "WatchDeveloping");

    /// <summary>PURPOSE: Change detection, persistence and filtering. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public async Task RecordsOnlyChangesAndFiltersByEasternDate()
    {
        var store = new LiveSignalLogStore(_dir);
        var now = DateTimeOffset.Parse("2026-11-18T15:30:00Z");

        var first = await store.RecordAsync(Obs("mwh", "2026-11-18T15:28:00Z", "WAIT"), now, CancellationToken.None);
        var same = await store.RecordAsync(Obs("MWH", "2026-11-18T15:28:15Z", "WAIT"), now, CancellationToken.None);
        var changed = await store.RecordAsync(Obs("MWH", "2026-11-18T15:30:00Z", "PREPARE TO BUY"), now, CancellationToken.None);
        await store.RecordAsync(Obs("AVGO", "2026-11-18T15:30:00Z", "WAIT"), now, CancellationToken.None);
        await store.RecordAsync(Obs("MWH", "2026-11-19T15:00:00Z", "EXECUTION READY"), now, CancellationToken.None);

        Assert.Equal("FIRST_SEEN", first!.Change);
        Assert.Equal("MWH", first.Observation.Symbol);
        Assert.Null(same);
        Assert.Equal("CHANGED", changed!.Change);
        Assert.Equal("EVALUATED|WAIT|SELL - BREAKDOWN", changed.PreviousKey);

        var reopened = new LiveSignalLogStore(_dir);
        var mwhToday = await reopened.ReadAsync(new DateOnly(2026, 11, 18), "mwh", CancellationToken.None);
        Assert.Equal(new[] { "WAIT", "PREPARE TO BUY" }, mwhToday.Select(e => e.Observation.State));
        Assert.Equal(3, (await reopened.ReadAsync(new DateOnly(2026, 11, 18), null, CancellationToken.None)).Count);
        var ready = Assert.Single(await reopened.ReadAsync(new DateOnly(2026, 11, 19), "MWH", CancellationToken.None)).Observation;
        Assert.Equal("EXECUTION READY", ready.State);
        Assert.Equal("ReadyHighOpportunity", ready.Quality);
    }

    /// <summary>
    /// PURPOSE: A quality change alone does not write a line, and lines written before the Quality field existed still
    /// read back (Quality null). WHEN CALLED: By xUnit.
    /// </summary>
    [Fact]
    public async Task QualityIsRecordedButNotPartOfTheChangeKey()
    {
        var store = new LiveSignalLogStore(_dir);
        var now = DateTimeOffset.Parse("2026-11-18T15:30:00Z");
        await store.RecordAsync(Obs("AVGO", "2026-11-18T15:28:00Z", "EXECUTION READY") with { Quality = "ReadyModerate" }, now, CancellationToken.None);
        var flip = await store.RecordAsync(Obs("AVGO", "2026-11-18T15:29:00Z", "EXECUTION READY") with { Quality = "ReadyBoring" }, now, CancellationToken.None);
        Assert.Null(flip);

        await File.AppendAllTextAsync(store.FilePath,
            "{\"loggedAtUtc\":\"2026-11-18T15:31:00+00:00\",\"change\":\"FIRST_SEEN\",\"previousKey\":null,\"observation\":{\"symbol\":\"OLD\",\"evaluatedAtUtc\":\"2026-11-18T15:31:00+00:00\",\"evaluationStatus\":\"EVALUATED\",\"state\":\"WAIT\"}}" + Environment.NewLine);
        var entries = await store.ReadAsync(new DateOnly(2026, 11, 18), null, CancellationToken.None);
        Assert.Equal("ReadyModerate", entries.Single(e => e.Observation.Symbol == "AVGO").Observation.Quality);
        Assert.Null(entries.Single(e => e.Observation.Symbol == "OLD").Observation.Quality);
    }
}
