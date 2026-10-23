using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Application.Services;
using MomentumCommandCenterV2.Infrastructure.Persistence;
using Xunit;

/// <summary>
/// TESTS: ScannerDiscoveryPollingWorkerTests
/// PURPOSE: Protects worker cancellation and health-state reporting without a live provider.
/// WHEN CALLED: Runs in the offline .NET test suite.
/// </summary>
public sealed class ScannerDiscoveryPollingWorkerTests
{
    /// <summary>
    /// TEST: RunAsyncRecordsSuccessAndStopsOnCancellation
    /// PURPOSE: Confirms a successful empty poll is not fabricated into events and shutdown reports STOPPED.
    /// WHEN CALLED: Runs with provider-neutral collector tests.
    /// </summary>
    [Fact]
    public async Task RunAsyncRecordsSuccessAndStopsOnCancellation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCC-tests", Guid.NewGuid().ToString("N"));
        using var cancellation = new CancellationTokenSource();
        try
        {
            var store = new JsonlScannerDiscoveryEventStore(Path.Combine(directory, "events.jsonl"));
            var ingestion = new ScannerDiscoveryIngestionService(store);
            var source = new CancelAfterFirstPollSource(cancellation);
            var worker = new ScannerDiscoveryPollingWorker(source, ingestion, TimeSpan.FromHours(1));

            await worker.RunAsync(cancellation.Token);

            Assert.Equal(ScannerCollectorHealthStatus.Stopped, worker.Health.Status);
            Assert.Equal(1, source.CallCount);
            Assert.NotNull(worker.Health.LastSuccessAt);
            Assert.Equal(0, worker.Health.LastBatchReceivedCount);
            Assert.Equal(0, worker.Health.LastBatchAppendedCount);
            Assert.Equal(0, worker.Health.ConsecutiveFailures);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// TYPE: CancelAfterFirstPollSource
    /// PURPOSE: Provides one empty successful poll and then requests cancellation.
    /// WHEN CALLED: Used only to test worker lifecycle without network access.
    /// </summary>
    private sealed class CancelAfterFirstPollSource(CancellationTokenSource cancellation) : IScannerDiscoverySource
    {
        public int CallCount { get; private set; }

        /// <summary>
        /// METHOD: GetDiscoveriesAsync
        /// PURPOSE: Returns an empty source batch and cancels the worker's next wait.
        /// WHEN CALLED: Invoked once by the polling-worker test.
        /// </summary>
        public Task<IReadOnlyList<ScannerDiscoveryEvent>> GetDiscoveriesAsync(
            DateTimeOffset observedAt,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellation.Cancel();
            return Task.FromResult<IReadOnlyList<ScannerDiscoveryEvent>>(Array.Empty<ScannerDiscoveryEvent>());
        }
    }
}
