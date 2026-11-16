using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: OvernightHistoryExportServiceTests
/// PURPOSE: Verifies the Dashboard history export: files written, scanner rows archived, status "ran today" from both
/// Dashboard and script manifests, an older run not counting as today, input validation and data-root resolution.
/// All sessions and symbols are synthetic test data; files go to a temporary folder.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class OvernightHistoryExportServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mcc-export-test-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 21, 30, 0, TimeSpan.Zero); // 16:30 ET

    /// <summary>PURPOSE: Deletes the temporary folder. WHEN CALLED: After each test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    /// <summary>PURPOSE: A fake history fetch: three sessions per symbol. WHEN CALLED: By the service under test.</summary>
    private static Task<DailyRecentSessions> Fake(string symbol, int days, CancellationToken ct)
    {
        var dates = new[] { new DateOnly(2026, 11, 11), new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13) };
        var sessions = dates.Select((d, i) => new DailySessionObservation(d, new DateTimeOffset(d.ToDateTime(new TimeOnly(5, 0)), TimeSpan.Zero),
            100 + i, 102 + i, 99 + i, 101 + i, 1_000_000)).ToArray();
        return Task.FromResult(new DailyRecentSessions(symbol, "Alpaca", "sip", "CONSOLIDATED_SIP", "split", Now, "TEST", dates, Array.Empty<DateOnly>(), sessions));
    }

    /// <summary>PURPOSE: A run writes all files, archives the rows and makes the status "ran today". WHEN CALLED: By xUnit.</summary>
    [Fact]
    public async Task Run_WritesFiles_AndStatusIsRanToday()
    {
        var service = new OvernightHistoryExportService(_root, Fake);
        var rows = new List<Dictionary<string, string?>> { new() { ["symbol"] = "TSTA", ["score"] = "61.2" } };

        var run = await service.RunAsync(new HistoryExportRequest(new[] { "tsta", "TSTB" }, "scan.csv", rows, 730), Now, "abc123", CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal("dashboard", run!.Source);
        Assert.Equal(2, run.SymbolsSucceeded);
        Assert.Equal(4, run.PairRows);                                  // 2 pairs per symbol
        foreach (var file in new[] { "pairs.csv", "weekday-by-symbol.csv", "weekday-pooled.csv", "manifest.json" })
            Assert.True(File.Exists(Path.Combine(run.Folder, file)), file);
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "scanner-exports")));
        Assert.Contains(run.Pooled, p => p.BuyWeekday == "All" && p.Count == 4);

        var status = service.GetStatus(Now.AddMinutes(5));
        Assert.True(status.RanToday);
        Assert.Equal(run.RunId, status.Latest!.RunId);
        Assert.False(service.GetStatus(Now.AddDays(1)).RanToday);       // tomorrow: not yet run
    }

    /// <summary>PURPOSE: A manifest written by the PowerShell script also counts. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void ScriptManifest_CountsForStatus()
    {
        var dir = Path.Combine(_root, "overnight-history", "20261116-213000");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"),
            "{\"createdAtUtc\":\"2026-11-16T21:30:00.0000000Z\",\"symbolsRequested\":50,\"symbolsSucceeded\":50,\"pairRows\":23651,\"errors\":[]}");

        var status = new OvernightHistoryExportService(_root, Fake).GetStatus(Now);

        Assert.True(status.RanToday);
        Assert.Equal("script", status.Latest!.Source);
        Assert.Equal(23651, status.Latest.PairRows);
    }

    /// <summary>PURPOSE: No symbols is refused. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public async Task Run_WithoutSymbols_IsRefused()
    {
        var service = new OvernightHistoryExportService(_root, Fake);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(new HistoryExportRequest(Array.Empty<string>(), null, null, null), Now, null, CancellationToken.None));
    }

    /// <summary>PURPOSE: The data root is the MCC-Data folder next to the folder holding the .sln. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public void DataRoot_IsNextToTheRepository()
    {
        var repo = Path.Combine(_root, "repos", "MCC");
        var content = Path.Combine(repo, "src", "Api");
        Directory.CreateDirectory(content);
        File.WriteAllText(Path.Combine(repo, "MCC.sln"), "");

        Assert.Equal(Path.Combine(_root, "repos", "MCC-Data"), OvernightHistoryExportService.ResolveDataRoot(null, content));
        Assert.Equal(@"X:\custom", OvernightHistoryExportService.ResolveDataRoot(@"X:\custom", content));
    }
}
