using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Infrastructure.Persistence;
using Xunit;

/// <summary>
/// TESTS: JsonlScannerDiscoveryEventStoreTests
/// PURPOSE: Protects durable append-only event persistence, restart idempotency, and strict corruption handling.
/// WHEN CALLED: Runs in the standard .NET test suite.
/// </summary>
public sealed class JsonlScannerDiscoveryEventStoreTests
{
    /// <summary>
    /// TEST: AppendIfNewAsyncPersistsAndDeduplicatesAcrossStoreInstances
    /// PURPOSE: Confirms the first event is durable and a restarted store rejects the same idempotency key.
    /// WHEN CALLED: Runs as an offline persistence test without provider credentials or network access.
    /// </summary>
    [Fact]
    public async Task AppendIfNewAsyncPersistsAndDeduplicatesAcrossStoreInstances()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCC-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "events.jsonl");
        try
        {
            var firstStore = new JsonlScannerDiscoveryEventStore(path);
            var envelope = CreateEnvelope("stable-event-1");

            Assert.True(await firstStore.AppendIfNewAsync(envelope));
            Assert.False(await firstStore.AppendIfNewAsync(envelope));

            var restartedStore = new JsonlScannerDiscoveryEventStore(path);
            Assert.False(await restartedStore.AppendIfNewAsync(envelope));

            var events = await restartedStore.ReadAllAsync();
            var saved = Assert.Single(events);
            Assert.Equal("stable-event-1", saved.IdempotencyKey);
            Assert.Equal("DEMO", saved.Discovery.Symbol);
            Assert.Equal(TimeSpan.FromHours(-7), saved.Discovery.ObservedAt.Offset);
            Assert.Equal(TimeSpan.FromHours(-7), saved.ReceivedAt.Offset);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// TEST: ReadAllAsyncRejectsMalformedLinesInsteadOfSilentlySkippingThem
    /// PURPOSE: Ensures a partial/corrupt log is surfaced as a data-integrity failure.
    /// WHEN CALLED: Runs as an offline persistence test.
    /// </summary>
    [Fact]
    public async Task ReadAllAsyncRejectsMalformedLinesInsteadOfSilentlySkippingThem()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCC-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "events.jsonl");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(path, "{not-json}\n");
            var store = new JsonlScannerDiscoveryEventStore(path);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAllAsync());
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// METHOD: CreateEnvelope
    /// PURPOSE: Creates a deterministic test discovery with explicit Arizona offsets.
    /// WHEN CALLED: Used by offline store tests only.
    /// </summary>
    private static PersistedScannerDiscoveryEnvelope CreateEnvelope(string idempotencyKey)
    {
        var observedAt = new DateTimeOffset(2026, 10, 23, 8, 30, 0, TimeSpan.FromHours(-7));
        var discovery = new ScannerDiscoveryEvent(
            "DEMO",
            "Test Movers",
            "FixtureSource",
            idempotencyKey,
            observedAt,
            observedAt,
            observedAt,
            "1",
            new Dictionary<string, string?> { ["lastPrice"] = "10.25" },
            ScannerCandidateDisposition.DataUnknown,
            new[] { "FIXTURE_ONLY" },
            observedAt,
            "test-1");
        return new PersistedScannerDiscoveryEnvelope(idempotencyKey, observedAt, discovery);
    }
}
