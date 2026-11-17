using MomentumCommandCenterV2.API.Watchlists;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: PendingCatalystStoreTests
/// PURPOSE: Verifies the pending binary-catalyst store: add, replace by symbol (case-insensitive), find, remove, persistence
/// across instances, and an empty list when the file does not exist. Entries are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class PendingCatalystStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "mcc-pending-catalysts-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "pending-catalysts.json");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T17:00:00Z");

    /// <summary>PURPOSE: Removes the temporary folder. WHEN CALLED: By xUnit after each test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    /// <summary>PURPOSE: Add, replace, find, persist, remove. WHEN CALLED: By xUnit.</summary>
    [Fact]
    public async Task AddReplaceFindRemove()
    {
        var store = new PendingCatalystStore(FilePath);
        Assert.Empty(await store.GetAsync(CancellationToken.None));

        await store.AddAsync(new AddPendingCatalystRequest(" abc ", "Phase 3 topline", "Q4 2026", "https://example.test/a", null), Now, CancellationToken.None);
        await store.AddAsync(new AddPendingCatalystRequest("XYZ", "PDUFA date", "Data not found in sources.", null, "note"), Now, CancellationToken.None);
        var replaced = await store.AddAsync(new AddPendingCatalystRequest("ABC", "Phase 3 final analysis", "Q1 2027", null, null), Now, CancellationToken.None);

        Assert.Equal(new[] { "ABC", "XYZ" }, replaced.Select(x => x.Symbol));
        Assert.Equal("Phase 3 final analysis", replaced[0].Event);

        var reopened = new PendingCatalystStore(FilePath);
        var found = await reopened.FindAsync("abc", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("Q1 2027", found!.ExpectedWindow);
        Assert.Null(await reopened.FindAsync("NONE", CancellationToken.None));

        var afterRemove = await reopened.RemoveAsync("abc", CancellationToken.None);
        Assert.Equal("XYZ", Assert.Single(afterRemove).Symbol);
    }
}
