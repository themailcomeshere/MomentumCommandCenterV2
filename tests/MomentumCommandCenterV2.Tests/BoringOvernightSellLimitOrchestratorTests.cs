using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.API.StrategyReports;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightSellLimitOrchestratorTests
{
    [Fact]
    public async Task ReturnsProvisionalResearchLimitOnlyWhenEveryEvidenceGatePasses()
    {
        var asOf = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(16, 42));
        var bars = BuildCompleteSIPBars(asOf);
        var atrRows = BuildMatchedTosAtrRows(asOf);
        var orchestrator = new BoringOvernightSellLimitOrchestrator(
            new MemoryMarketBarStore(bars),
            new MemoryAtrStore(atrRows));

        var result = await orchestrator.CalculateAsync(
            new BoringOvernightSellLimitRequest("EVR", "Alpaca", "sip", 75),
            asOf, CancellationToken.None);

        Assert.Equal("BLOCKED", result.Status);
        Assert.Equal(20, result.History.CompleteWindowCount);
        Assert.True(result.History.QualificationUsable);
        Assert.Equal(100.5m, result.ReferenceClose);
        Assert.Equal(2m, result.CurrentAtr);
        Assert.Equal("MATCH", result.AtrParity.Status);
        Assert.Equal(99, result.TOSWarmupObservationCount);
        Assert.Equal(99, result.ProviderWarmupObservationCount);
        Assert.Equal(0, result.ProviderBarMismatchCount);
        Assert.Null(result.Calculation?.CalculatedLimitSell);
        Assert.Contains(result.Gates, gate => gate.Code == "WALK_FORWARD_VALIDATION" && !gate.Passed);
        Assert.All(result.Gates.Where(gate => gate.Code is not ("WALK_FORWARD_VALIDATION" or "SELL_LIMIT_CALCULATION")),
            gate => Assert.True(gate.Passed, gate.Code + ": " + gate.Detail));
    }

    [Fact]
    public async Task IexCannotProduceNumericSellLimitEvenWithNoOtherEvidence()
    {
        var asOf = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(16, 42));
        var orchestrator = new BoringOvernightSellLimitOrchestrator(
            new MemoryMarketBarStore(Array.Empty<MarketBarObservation>()),
            new MemoryAtrStore(Array.Empty<ThinkorswimAtrStoredObservation>()));

        var result = await orchestrator.CalculateAsync(
            new BoringOvernightSellLimitRequest("EVR", "Alpaca", "iex", 75),
            asOf, CancellationToken.None);

        Assert.Equal("BLOCKED", result.Status);
        Assert.Null(result.Calculation?.CalculatedLimitSell);
        Assert.Contains(result.Gates, x => x.Code == "SIP_REQUIRED" && !x.Passed);
        Assert.Contains(result.Gates, x => x.Code == "HISTORY_20_OF_20" && !x.Passed);
        Assert.Contains(result.Gates, x => x.Code == "ATR_PARITY" && !x.Passed);
    }

    [Fact]
    public async Task ProviderBarMismatchBlocksOtherwiseCompleteSellLimitEvidence()
    {
        var asOf = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(16, 42));
        var bars = BuildCompleteSIPBars(asOf).ToList();
        var target = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(16, 35));
        var index = bars.FindIndex(x => x.Bar.Timeframe == "5m" && x.SourceTimestamp == target);
        Assert.True(index >= 0);
        bars[index] = bars[index] with { Bar = bars[index].Bar with { Close = 100.25 } };

        var orchestrator = new BoringOvernightSellLimitOrchestrator(
            new MemoryMarketBarStore(bars),
            new MemoryAtrStore(BuildMatchedTosAtrRows(asOf)));

        var result = await orchestrator.CalculateAsync(
            new BoringOvernightSellLimitRequest("EVR", "Alpaca", "sip", 75),
            asOf, CancellationToken.None);

        Assert.Equal("BLOCKED", result.Status);
        Assert.Contains(result.Gates, x => x.Code == "SAME_SOURCE_5M_OHLCV" && !x.Passed);
        Assert.Null(result.Calculation?.CalculatedLimitSell);
    }

    private static IReadOnlyList<MarketBarObservation> BuildCompleteSIPBars(DateTimeOffset receivedAt)
    {
        var output = new List<MarketBarObservation>();
        var boundaries = UsEquitiesSessionCalendar.BuildRecentBoundaries(new DateOnly(2026, 11, 4), 20);
        foreach (var boundary in boundaries)
        {
            AddBar(output, boundary.PreviousCloseBarTimestampUtc, "1m", 100, 101, 99, 100, 1000, receivedAt);
            var start = EasternUtc(boundary.NextSessionDate, new TimeOnly(7, 0));
            for (var minute = 0; minute < 780; minute++)
                AddBar(output, start.AddMinutes(minute), "1m", 100, 101, 99, 100, 1000, receivedAt);
        }

        var currentReference = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(15, 59));
        AddBar(output, currentReference, "1m", 100.5, 100.5, 100.5, 100.5, 1000, receivedAt);

        var fiveMinuteStart = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(6, 45));
        for (var index = 0; index < 119; index++)
            AddBar(output, fiveMinuteStart.AddMinutes(index * 5), "5m", 100, 101, 99, 100, 1000, receivedAt);
        return output;
    }

    private static IReadOnlyList<ThinkorswimAtrStoredObservation> BuildMatchedTosAtrRows(DateTimeOffset receivedAt)
    {
        var metadata = new StrategyReportAtrImportMetadata(
            "EVR-corrected-5m.csv", "MCC_OHLCV_ATR_V2.1", 14, "Wilder",
            "America/Phoenix", true, true, "test-batch", receivedAt);
        var start = EasternUtc(new DateOnly(2026, 11, 5), new TimeOnly(6, 45));
        return Enumerable.Range(0, 119).Select(index =>
        {
            var observation = new StrategyReportAtrObservation(
                (index + 1).ToString(), "EVR", "5m", start.AddMinutes(index * 5),
                100m, 101m, 99m, 100m, 1000m, 2m);
            return new ThinkorswimAtrStoredObservation(
                "tos-" + index, receivedAt, metadata, observation, "hash-" + index, null);
        }).ToArray();
    }

    private static void AddBar(
        List<MarketBarObservation> output, DateTimeOffset timestamp, string timeframe,
        double open, double high, double low, double close, long volume, DateTimeOffset receivedAt)
    {
        var bar = new MarketBar("EVR", timestamp, open, high, low, close, volume, "Alpaca", "sip", timeframe);
        output.Add(new MarketBarObservation(
            Guid.NewGuid().ToString("N"), receivedAt, "Alpaca", "EVR", timestamp, bar,
            "test-hash", null));
    }

    private static DateTimeOffset EasternUtc(DateOnly date, TimeOnly time)
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, eastern.GetUtcOffset(local)).ToUniversalTime();
    }

    private sealed class MemoryMarketBarStore(IReadOnlyList<MarketBarObservation> observations) : IMarketBarObservationStore
    {
        public Task<int> AppendAsync(IReadOnlyCollection<MarketBar> bars, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<MarketBarObservation>> GetLatestAsync(
            string symbol, string? provider, DateTimeOffset fromUtc, DateTimeOffset toUtc,
            int limit, CancellationToken cancellationToken, string? timeframe = null, string? feed = null)
        {
            IReadOnlyList<MarketBarObservation> result = observations
                .Where(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(x => provider is null || x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.SourceTimestamp >= fromUtc && x.SourceTimestamp <= toUtc)
                .Where(x => timeframe is null || x.Bar.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase))
                .Where(x => feed is null || string.Equals(x.Bar.Feed, feed, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.SourceTimestamp)
                .Select(g => g.Last())
                .OrderBy(x => x.SourceTimestamp)
                .TakeLast(limit).ToArray();
            return Task.FromResult(result);
        }
    }

    private sealed class MemoryAtrStore(IReadOnlyList<ThinkorswimAtrStoredObservation> observations) : IThinkorswimAtrObservationStore
    {
        public Task<ThinkorswimAtrAppendResult> AppendAsync(
            IReadOnlyCollection<StrategyReportAtrObservation> items,
            StrategyReportAtrImportMetadata metadata, CancellationToken cancellationToken) =>
            Task.FromResult(new ThinkorswimAtrAppendResult(0, 0, 0));

        public Task<IReadOnlyList<ThinkorswimAtrStoredObservation>> GetLatestAsync(
            string? symbol, string? timeframe, DateTimeOffset? fromUtc, DateTimeOffset? toUtc,
            int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<ThinkorswimAtrStoredObservation> result = observations
                .Where(x => symbol is null || x.Observation.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .Where(x => timeframe is null || x.Observation.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase))
                .Where(x => fromUtc is null || x.Observation.SourceTimestampUtc >= fromUtc.Value)
                .Where(x => toUtc is null || x.Observation.SourceTimestampUtc <= toUtc.Value)
                .OrderBy(x => x.Observation.SourceTimestampUtc)
                .TakeLast(limit).ToArray();
            return Task.FromResult(result);
        }
    }
}
