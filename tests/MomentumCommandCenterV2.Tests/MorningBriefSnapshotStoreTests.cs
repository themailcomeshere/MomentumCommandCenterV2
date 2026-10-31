using System.Text.Json;
using MomentumCommandCenterV2.API.MorningBrief;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class MorningBriefSnapshotStoreTests
{
    [Fact]
    public async Task AppendAndReadPreservesVersionedImmutableReportAndHash()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-morning-brief-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", path);
        try
        {
            var store = new MorningBriefSnapshotStore();
            using var report = JsonDocument.Parse("""
                {"generatedAtUtc":"2026-10-30T20:00:00Z","status":"PARTIAL","catalysts":{"status":"DATA NOT FOUND IN SOURCES"}}
                """);

            var saved = await store.AppendAsync(report.RootElement, "morning-brief-v1", CancellationToken.None);
            var read = await store.GetLatestAsync(10, CancellationToken.None);

            Assert.NotEmpty(saved.SnapshotId);
            Assert.Equal("morning-brief-v1", saved.ReportVersion);
            Assert.Equal(new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero), saved.GeneratedAtUtc);
            Assert.Equal(64, saved.ContentSha256.Length);
            Assert.Single(read);
            Assert.Equal(saved.SnapshotId, read[0].SnapshotId);
            Assert.Equal(saved.ContentSha256, read[0].ContentSha256);
            Assert.Equal("PARTIAL", read[0].Report.GetProperty("status").GetString());
            Assert.Equal("DATA NOT FOUND IN SOURCES",
                read[0].Report.GetProperty("catalysts").GetProperty("status").GetString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task AppendsEveryGeneratedReportWithoutOverwritingEarlierSnapshots()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-morning-brief-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", path);
        try
        {
            var store = new MorningBriefSnapshotStore();
            using var first = JsonDocument.Parse("""{"generatedAtUtc":"2026-10-30T20:00:00Z","status":"PARTIAL"}""");
            using var second = JsonDocument.Parse("""{"generatedAtUtc":"2026-10-30T20:01:00Z","status":"PARTIAL","revision":2}""");

            var firstSaved = await store.AppendAsync(first.RootElement, "morning-brief-v1", CancellationToken.None);
            var secondSaved = await store.AppendAsync(second.RootElement, "morning-brief-v1", CancellationToken.None);
            var read = await store.GetLatestAsync(10, CancellationToken.None);

            Assert.Equal(2, read.Count);
            Assert.Equal(secondSaved.SnapshotId, read[0].SnapshotId);
            Assert.Equal(firstSaved.SnapshotId, read[1].SnapshotId);
            Assert.NotEqual(firstSaved.ContentSha256, secondSaved.ContentSha256);
            Assert.Equal(2, File.ReadAllLines(path).Length);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RejectsNonUtcOrMissingGenerationTimestampWithoutWriting()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-morning-brief-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", path);
        try
        {
            var store = new MorningBriefSnapshotStore();
            using var nonUtc = JsonDocument.Parse("""{"generatedAtUtc":"2026-10-30T13:00:00-07:00"}""");
            using var missing = JsonDocument.Parse("""{"status":"PARTIAL"}""");

            await Assert.ThrowsAsync<ArgumentException>(() =>
                store.AppendAsync(nonUtc.RootElement, "morning-brief-v1", CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                store.AppendAsync(missing.RootElement, "morning-brief-v1", CancellationToken.None));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_MORNING_BRIEF_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RejectsInvalidHistoryLimit()
    {
        var store = new MorningBriefSnapshotStore();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.GetLatestAsync(0, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.GetLatestAsync(201, CancellationToken.None));
    }
}
