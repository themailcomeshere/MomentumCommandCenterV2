using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// PURPOSE: Prove the raw market-bar store deduplicates identical observations and retains corrections.
/// WHEN CALLED: CI executes the automated test suite.
/// </summary>
public sealed class JsonlMarketBarObservationStoreTests
{
    [Fact]
    public async Task IdenticalObservationIsIdempotentAcrossStoreRestart()
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-bar-store-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "bars.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", path);
            var bar = CreateBar();
            Assert.Equal(1, await new JsonlMarketBarObservationStore().AppendAsync(new[] { bar }, CancellationToken.None));
            Assert.Equal(0, await new JsonlMarketBarObservationStore().AppendAsync(new[] { bar }, CancellationToken.None));
            var lines = await File.ReadAllLinesAsync(path);
            Assert.Single(lines);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ChangedObservationIsAppendedAsCorrectionWithoutOverwritingPriorRecord()
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-bar-store-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "bars.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", path);
            var store = new JsonlMarketBarObservationStore();
            var original = CreateBar();
            var corrected = original with { High = 11.2, Close = 10.8 };
            Assert.Equal(1, await store.AppendAsync(new[] { original }, CancellationToken.None));
            Assert.Equal(1, await store.AppendAsync(new[] { corrected }, CancellationToken.None));

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(2, lines.Length);
            var observations = lines.Select(line => JsonSerializer.Deserialize<MarketBarObservation>(line,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!).ToArray();
            Assert.NotEqual(observations[0].ContentHash, observations[1].ContentHash);
            Assert.Equal(observations[0].ObservationId, observations[1].SupersedesObservationId);
            Assert.Equal(10.5, observations[0].Bar.Close);
            Assert.Equal(10.8, observations[1].Bar.Close);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidBars))]
    public async Task InvalidOhlcvIsRejectedBeforeItCanBePersisted(MarketBar invalidBar)
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-bar-store-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "bars.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", path);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new JsonlMarketBarObservationStore().AppendAsync(new[] { CreateBar(), invalidBar }, CancellationToken.None));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }


    [Fact]
    public async Task QueryReturnsLatestCorrectionForEachTimestampAndFiltersProviderWindow()
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-bar-store-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "bars.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", path);
            var store = new JsonlMarketBarObservationStore();
            var first = CreateBar();
            var corrected = first with { High = 11.2, Close = 10.8 };
            var second = first with { Timestamp = first.Timestamp.AddMinutes(1), Open = 10.5, High = 11.5, Low = 10.2, Close = 11.1 };
            var otherProvider = second with { Provider = "OtherProvider" };
            Assert.Equal(1, await store.AppendAsync(new[] { first }, CancellationToken.None));
            Assert.Equal(1, await store.AppendAsync(new[] { corrected }, CancellationToken.None));
            Assert.Equal(2, await store.AppendAsync(new[] { second, otherProvider }, CancellationToken.None));

            var result = await store.GetLatestAsync(
                "test", "TestProvider", first.Timestamp, second.Timestamp, 100, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal(10.8, result[0].Bar.Close);
            Assert.Equal(10.0, result[0].Bar.Open);
            Assert.Equal(11.1, result[1].Bar.Close);
            Assert.NotNull(result[0].SupersedesObservationId);
            Assert.Equal(first.Timestamp.AddMinutes(1), result[1].SourceTimestamp);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task QueryRejectsInvalidWindowAndLimit()
    {
        var previousPath = Environment.GetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH");
        var directory = Path.Combine(Path.GetTempPath(), "mcc-bar-store-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "bars.jsonl");
        try
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", path);
            var store = new JsonlMarketBarObservationStore();
            var now = DateTimeOffset.UtcNow;
            await Assert.ThrowsAsync<ArgumentException>(() =>
                store.GetLatestAsync("TEST", null, now.AddMinutes(1), now, 10, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                store.GetLatestAsync("TEST", null, now, now, 0, CancellationToken.None));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MARKET_BAR_STORE_PATH", previousPath);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    public static IEnumerable<object[]> InvalidBars()
    {
        var valid = CreateBar();
        yield return new object[] { valid with { Close = double.NaN } };
        yield return new object[] { valid with { High = 8.0 } };
        yield return new object[] { valid with { Volume = -1 } };
        yield return new object[] { valid with { Low = 12.0 } };
    }

    private static MarketBar CreateBar() => new(
        "TEST", new DateTimeOffset(2026, 10, 22, 16, 0, 0, TimeSpan.Zero),
        10.0, 11.0, 9.0, 10.5, 1000, "TestProvider", "test-feed");
}
