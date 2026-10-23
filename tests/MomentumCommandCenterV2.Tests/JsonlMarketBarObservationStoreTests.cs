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

    private static MarketBar CreateBar() => new(
        "TEST", new DateTimeOffset(2026, 10, 22, 16, 0, 0, TimeSpan.Zero),
        10.0, 11.0, 9.0, 10.5, 1000, "TestProvider", "test-feed");
}
