using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightCandidateSnapshotStoreTests
{
    [Fact]
    public async Task AppendAndReadPreservesSnapshotProvenanceAndEvaluationInputs()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-bo-snapshots-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", path);
        try
        {
            var store = new BoringOvernightCandidateSnapshotStore();
            var asOf = new DateTimeOffset(2026, 10, 28, 19, 0, 0, TimeSpan.Zero);
            var fieldTimes = new Dictionary<string, DateTimeOffset>
            {
                ["price"] = asOf.AddMinutes(-1),
                ["overnightWinRatePct"] = asOf.AddDays(-1)
            };
            var audit = new BoringOvernightHistoryAudit(20, 20, 17, 3, false, false, "INCOMPLETE");
            var daily = new BoringOvernightDailyEvidence(
                "Alpaca", "iex", "all (split/dividend adjusted)", asOf, asOf.AddDays(-1),
                true, "COMPLETE_20_EXPECTED_SESSIONS", "IEX_PARTIAL", false, 10.5, 9.5, 250000);
            var catalyst = new BoringOvernightCatalystEvidence(
                "Alpaca News", "RISK_PRESENT", asOf.AddHours(-24), asOf, asOf,
                true, 1, 1, "MCC-CATALYST-TRIAGE-1.0",
                new[] { new BoringOvernightCatalystArticleEvidence(
                    "news-1", "Company announces public offering", asOf.AddMinutes(-10),
                    "https://example.test/news", "RISK", new[] { "CAPITAL_RAISE_OR_DILUTION" }, "test reason") });
            var input = new BoringOvernightCandidateSnapshotInput(
                "RTO", asOf, "test-fixture", "fixture-provider", "fixture-feed", "test-v1", fieldTimes,
                Request("RTO"), audit, daily, catalyst);

            var saved = await store.AppendAsync(input, CancellationToken.None);
            var read = await store.GetLatestAsync("rto", 10, CancellationToken.None);

            Assert.Equal("RTO", saved.Symbol);
            Assert.NotEmpty(saved.SnapshotId);
            Assert.Equal("test-fixture", saved.Source);
            Assert.Equal("test-v1", saved.CalculationVersion);
            Assert.Equal(asOf, saved.AsOfUtc);
            Assert.Single(read);
            Assert.Equal(saved.SnapshotId, read[0].SnapshotId);
            Assert.Equal(fieldTimes["price"], read[0].FieldSourceTimestampsUtc["price"]);
            Assert.Equal(0.4, read[0].EvaluationRequest.CurrentOvernightPct);
            var savedAudit = Assert.IsType<BoringOvernightHistoryAudit>(read[0].HistoricalCompleteness);
            var savedDaily = Assert.IsType<BoringOvernightDailyEvidence>(read[0].DailyEvidence);
            var savedCatalyst = Assert.IsType<BoringOvernightCatalystEvidence>(read[0].CatalystEvidence);
            Assert.Equal(20, savedAudit.RequestedPairCount);
            Assert.Equal(17, savedAudit.ValidPairCount);
            Assert.Equal(3, savedAudit.MissingPairCount);
            Assert.Equal("INCOMPLETE", savedAudit.Status);
            Assert.Equal("IEX_PARTIAL", savedDaily.FeedCoverage);
            Assert.False(savedDaily.QualificationUsable);
            Assert.Equal("RISK_PRESENT", savedCatalyst.Status);
            Assert.Equal("Company announces public offering", savedCatalyst.Articles.Single().Headline);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RejectsNonUtcAsOfTimestamp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-bo-snapshots-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", path);
        try
        {
            var store = new BoringOvernightCandidateSnapshotStore();
            var input = new BoringOvernightCandidateSnapshotInput(
                "RTO", new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.FromHours(-7)),
                "test-fixture", null, null, "test-v1",
                new Dictionary<string, DateTimeOffset>(), Request("RTO"));

            await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(input, CancellationToken.None));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RejectsMismatchedSnapshotAndEvaluationSymbols()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcc-bo-snapshots-{Guid.NewGuid():N}.jsonl");
        var previous = Environment.GetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH");
        Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", path);
        try
        {
            var store = new BoringOvernightCandidateSnapshotStore();
            var input = new BoringOvernightCandidateSnapshotInput(
                "RTO", DateTimeOffset.UtcNow, "test-fixture", null, null, "test-v1",
                new Dictionary<string, DateTimeOffset>(), Request("SXC"));

            await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(input, CancellationToken.None));
            Assert.False(File.Exists(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCC_BORING_OVERNIGHT_SNAPSHOT_PATH", previous);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static BoringOvernightEvaluationRequest Request(string symbol) => new(
        symbol, true, 10, 9, 1_000_000, 70, 0.3, 0.5, 0.25, 0.4, false, "UNKNOWN",
        true, "BULLISH", "WATCH", 0.8, 1.2, 1.5, 1.1);
}
