using System.Collections.Concurrent;
using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Retrieves current Alpaca 1m/5m observations and evaluates the existing V2 signal engine.
/// WHEN CALLED: The MCC live-symbol assessment endpoint is requested by Main Chart or Red Zone.
/// IMPORTANT: Uses bounded historical REST polling, not websocket/tick streaming. Permission is withheld when source evidence is incomplete.
/// </summary>
public sealed class LiveSymbolAssessmentService(
    IEnumerable<IMarketDataProvider> providers,
    IMarketBarObservationStore barStore,
    AlpacaMinuteBarStreamWorker streamWorker,
    IConfiguration configuration,
    ILogger<LiveSymbolAssessmentService> logger)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<string, (DateTimeOffset ExpiresAt, object Value)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// PURPOSE: Retrieves source bars, persists observations, computes indicators, and evaluates 5M permission / 1M execution.
    /// WHEN CALLED: GET /api/market-data/live-assessment?symbol=...
    /// </summary>
    public async Task<object> GetAsync(string symbol, string providerName, CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var provider = providers.FirstOrDefault(x =>
            string.Equals(x.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
            throw new ArgumentException($"Unknown provider '{providerName}'.");
        if (!provider.IsConfigured)
            throw new ProviderNotConfiguredException($"{provider.ProviderName} is not configured. Configure provider credentials on the API server.");

        var cacheKey = provider.ProviderName + "/" + normalizedSymbol;
        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now)
            return cached.Value;

        var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_cache.TryGetValue(cacheKey, out cached) && cached.ExpiresAt > now)
                return cached.Value;

            // 21 calendar days normally contain at least 10 regular sessions, including weekends/holidays.
            // A 3-day 1m window supplies enough history for the current EMA/SMA/ATR calculations.
            IReadOnlyList<MarketBar> oneMinute = Array.Empty<MarketBar>();
            var oneMinuteFromWebSocket = false;
            var configuredFeed = configuration["MarketData:Alpaca:Feed"] ?? "iex";
            if (string.Equals(provider.ProviderName, "Alpaca", StringComparison.OrdinalIgnoreCase) &&
                streamWorker.IsSubscribed &&
                string.Equals(streamWorker.Feed, configuredFeed, StringComparison.OrdinalIgnoreCase))
            {
                var storedOneMinute = await barStore.GetLatestAsync(
                    normalizedSymbol, provider.ProviderName, now.AddDays(-3), now, 10000, cancellationToken, "1m");
                var streamBars = CompletedBars(
                    storedOneMinute
                        .Where(x => string.Equals(x.Bar.Feed, streamWorker.Feed, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Bar).ToArray(),
                    TimeSpan.FromMinutes(1), now);
                var streamLatest = streamBars.LastOrDefault();
                var streamAge = streamLatest is null
                    ? (double?)null
                    : (now - streamLatest.Timestamp.AddMinutes(1)).TotalSeconds;
                if (streamBars.Count >= 50 && streamAge is >= 0 and <= 120)
                {
                    oneMinute = streamBars;
                    oneMinuteFromWebSocket = true;
                }
            }

            var oneMinuteTask = oneMinuteFromWebSocket
                ? Task.FromResult(oneMinute)
                : provider.GetBarsAsync(normalizedSymbol, TimeSpan.FromMinutes(1),
                    now.AddDays(-3), now, cancellationToken);
            var fiveMinuteTask = provider.GetBarsAsync(normalizedSymbol, TimeSpan.FromMinutes(5),
                now.AddDays(-21), now, cancellationToken);
            await Task.WhenAll(oneMinuteTask, fiveMinuteTask);

            if (!oneMinuteFromWebSocket)
                oneMinute = CompletedBars(await oneMinuteTask, TimeSpan.FromMinutes(1), now);
            var fiveMinute = CompletedBars(await fiveMinuteTask, TimeSpan.FromMinutes(5), now);
            await barStore.AppendAsync(oneMinute, cancellationToken);
            await barStore.AppendAsync(fiveMinute, cancellationToken);

            var evaluatedAt = DateTimeOffset.UtcNow;
            var one = oneMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.Calculate(oneMinute, oneMinute.Count - 1, TimeSpan.FromMinutes(1), 0m)
                : null;
            var rvol = fiveMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveMinute, fiveMinute.Count - 1)
                : (Value: (decimal?)null, Samples: 0);
            var five = fiveMinute.Count >= 50
                ? LiveSymbolTechnicalCalculator.Calculate(fiveMinute, fiveMinute.Count - 1, TimeSpan.FromMinutes(5), rvol.Value ?? 0m)
                : null;
            var previousRvol = fiveMinute.Count >= 51
                ? LiveSymbolTechnicalCalculator.CalculateSameTimeOfDayRelativeVolume(fiveMinute, fiveMinute.Count - 2)
                : (Value: (decimal?)null, Samples: 0);
            var previousFive = fiveMinute.Count >= 51
                ? LiveSymbolTechnicalCalculator.Calculate(fiveMinute, fiveMinute.Count - 2, TimeSpan.FromMinutes(5), previousRvol.Value ?? 0m)
                : null;

            var oneLatest = oneMinute.LastOrDefault();
            var fiveLatest = fiveMinute.LastOrDefault();
            var oneCloseAt = oneLatest?.Timestamp.AddMinutes(1);
            var fiveCloseAt = fiveLatest?.Timestamp.AddMinutes(5);
            var oneAgeSeconds = oneCloseAt.HasValue ? (evaluatedAt - oneCloseAt.Value).TotalSeconds : (double?)null;
            var fiveAgeSeconds = fiveCloseAt.HasValue ? (evaluatedAt - fiveCloseAt.Value).TotalSeconds : (double?)null;
            var oneFresh = oneAgeSeconds is >= 0 and <= 120;
            var fiveFresh = fiveAgeSeconds is >= 0 and <= 360;
            var regularSession = LiveSymbolTechnicalCalculator.IsRegularSession(evaluatedAt);

            SignalDecision? decision = null;
            string evaluationStatus;
            string evaluationReason;
            if (one is null || five is null || previousFive is null)
            {
                evaluationStatus = "INSUFFICIENT_HISTORY";
                evaluationReason = $"Need at least 50 completed 1m bars and 51 completed 5m bars; received {oneMinute.Count} 1m and {fiveMinute.Count} 5m.";
            }
            else if (!oneFresh || !fiveFresh)
            {
                evaluationStatus = "STALE_MARKET_DATA";
                var staleDetails = new List<string>();
                if (!oneFresh) staleDetails.Add($"1m bar age {oneAgeSeconds?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}s (limit 120s; source {oneLatest?.Timestamp:O}; expected close {oneCloseAt:O})");
                if (!fiveFresh) staleDetails.Add($"5m bar age {fiveAgeSeconds?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}s (limit 360s; source {fiveLatest?.Timestamp:O}; expected close {fiveCloseAt:O})");
                evaluationReason = "STALE_MARKET_DATA: " + string.Join("; ", staleDetails) + $". Feed={oneLatest?.Feed ?? fiveLatest?.Feed ?? "unknown"}. Permission and execution are not evaluated.";
            }
            else if (rvol.Value is null)
            {
                evaluationStatus = "RVOL_BASELINE_INSUFFICIENT";
                evaluationReason = "A same-time-of-day 5m relative-volume baseline requires at least five prior regular-session observations for this 5m slot. 5M permission remains UNKNOWN.";
            }
            else if (!regularSession)
            {
                evaluationStatus = "OUTSIDE_REGULAR_SESSION";
                evaluationReason = "The V2 intraday permission/execution engine is evaluated only during the US regular session. Historical bars remain available.";
            }
            else
            {
                var snapshot = new CommandCenterSnapshot(
                    evaluatedAt,
                    normalizedSymbol,
                    one,
                    five,
                    MarketPhase.RegularSession,
                    previousFive);
                decision = new V2SignalEngine().Evaluate(snapshot, false);
                evaluationStatus = "EVALUATED";
                evaluationReason = "Calculated from completed Alpaca OHLCV bars using the existing V2SignalEngine. This is decision support, not an order instruction.";
            }

            var status = !oneFresh || !fiveFresh
                ? "STALE"
                : one is null || five is null
                    ? "INSUFFICIENT_HISTORY"
                    : "LIVE_OBSERVATIONS";
            var scoringConfig = new V2Config();
            var scoreCriteria = new[]
            {
                new { key = "closeAboveVwap", label = "5M close > VWAP", passed = five is null ? (bool?)null : five.Close > five.Vwap },
                new { key = "closeAboveEma9", label = "5M close > EMA 9", passed = five is null ? (bool?)null : five.Close > five.Ema9 },
                new { key = "ema9AboveEma20", label = "5M EMA 9 > EMA 20", passed = five is null ? (bool?)null : five.Ema9 > five.Ema20 },
                new { key = "ema20AboveSma50", label = "5M EMA 20 > SMA 50", passed = five is null ? (bool?)null : five.Ema20 > five.Sma50 },
                new { key = "rsiSupportive", label = $"5M RSI >= {scoringConfig.MinRsi:0}", passed = five is null ? (bool?)null : five.Rsi >= scoringConfig.MinRsi },
                new { key = "rvolSupportive", label = $"5M RVOL >= {scoringConfig.MinRvol:0.00}", passed = rvol.Value.HasValue ? rvol.Value.Value >= scoringConfig.MinRvol : (bool?)null }
            };

            var result = new
            {
                symbol = normalizedSymbol,
                provider = provider.ProviderName,
                feed = oneLatest?.Feed ?? fiveLatest?.Feed,
                evaluatedAtUtc = evaluatedAt,
                status,
                evaluationStatus,
                evaluationReason,
                dataFresh = oneFresh && fiveFresh,
                freshnessDiagnostics = new
                {
                    oneMinute = new { isFresh = oneFresh, ageSeconds = oneAgeSeconds, limitSeconds = 120, sourceTimestampUtc = oneLatest?.Timestamp, expectedCloseUtc = oneCloseAt },
                    fiveMinute = new { isFresh = fiveFresh, ageSeconds = fiveAgeSeconds, limitSeconds = 360, sourceTimestampUtc = fiveLatest?.Timestamp, expectedCloseUtc = fiveCloseAt },
                    staleTimeframes = new[] { !oneFresh ? "1m" : null, !fiveFresh ? "5m" : null }.Where(x => x is not null).ToArray()
                },
                oneMinute = new
                {
                    barCount = oneMinute.Count,
                    sourceTransport = oneMinuteFromWebSocket ? "ALPACA_WEBSOCKET_PERSISTED" : "ALPACA_REST_HISTORICAL",
                    sourceTimestampUtc = oneLatest?.Timestamp,
                    completedAtUtc = oneCloseAt,
                    ageSeconds = oneAgeSeconds,
                    freshness = FreshnessLabel(oneFresh, oneAgeSeconds),
                    close = oneLatest?.Close,
                    ema9 = one?.Ema9,
                    ema20 = one?.Ema20,
                    vwap = one?.Vwap,
                    rsi = one?.Rsi,
                    macd = one?.Macd,
                    macdSignal = one?.MacdSignal,
                    atr = one?.Atr,
                    extensionAtr = one?.AtrExtension,
                    chartBars = oneMinute.TakeLast(180).Select(ToChartBar).ToArray()
                },
                fiveMinute = new
                {
                    barCount = fiveMinute.Count,
                    sourceTimestampUtc = fiveLatest?.Timestamp,
                    completedAtUtc = fiveCloseAt,
                    ageSeconds = fiveAgeSeconds,
                    freshness = FreshnessLabel(fiveFresh, fiveAgeSeconds),
                    close = fiveLatest?.Close,
                    ema9 = five?.Ema9,
                    ema20 = five?.Ema20,
                    vwap = five?.Vwap,
                    rsi = five?.Rsi,
                    macd = five?.Macd,
                    macdSignal = five?.MacdSignal,
                    atr = five?.Atr,
                    relativeVolume = rvol.Value,
                    relativeVolumeBaselineSamples = rvol.Samples,
                    scoreBreakdown = new
                    {
                        score = decision?.Score,
                        maxScore = 6,
                        permissionThreshold = 5,
                        criteria = scoreCriteria
                    }
                },
                decision = decision is null ? null : new
                {
                    state = decision.State.ToString(),
                    action = decision.Action.ToString(),
                    score = decision.Score,
                    fiveMinutePermission = decision.LongPermission,
                    fiveMinuteStructureBull = decision.Evaluation?.FiveMinuteStructureBull,
                    oneMinuteStructureBull = decision.Evaluation?.OneMinuteStructureBull,
                    oneMinuteMomentumPositive = decision.Evaluation?.OneMinuteMomentumPositive,
                    oneMinuteExecution = decision.EntryAllowed ? "CONFIRMED" :
                        decision.LongPermission ? "WAITING_FOR_1M_CONFIRMATION" : "BLOCKED_BY_5M_PERMISSION",
                    entryAllowed = decision.EntryAllowed,
                    runnerAllowed = decision.RunnerAllowed,
                    exitWarning = decision.ExitWarning,
                    hardExit = decision.HardExit,
                    atrExtension = decision.Evaluation?.AtrExtension,
                    reason = decision.Reason
                },
                methodology = new
                {
                    indicatorHistory = "EMA9/EMA20, SMA50, RSI14, MACD(12,26,9), ATR14 and session VWAP are calculated from provider OHLCV.",
                    relativeVolume = "5m current volume divided by average volume for the same regular-session 5m clock slot across prior sessions; minimum five prior samples. This is not a vendor-supplied consolidated RVOL field.",
                    freshness = "Age is measured from the expected bar close (bar timestamp + timeframe), not the bar-open timestamp.",
                    limitations = new[]
                    {
                        oneMinuteFromWebSocket
                            ? "1m execution bars are read from the confirmed Alpaca WebSocket append-only store; 5m permission bars remain historical REST."
                            : "1m/5m decision bars are sourced through historical REST; the optional WebSocket is not currently subscribed or lacks a fresh 50-bar 1m history.",
                        "Provider feed and entitlement determine coverage.",
                        "No catalyst, halt, spread, order-book, or Thinkorswim study data is inferred.",
                        "A true signal remains unavailable when history, freshness, or same-slot volume baseline is insufficient."
                    }
                }
            };

            _cache[cacheKey] = (evaluatedAt.Add(CacheLifetime), result);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Live symbol assessment failed for {Symbol} using {Provider}.", normalizedSymbol, provider.ProviderName);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    private static IReadOnlyList<MarketBar> CompletedBars(
        IReadOnlyList<MarketBar> bars, TimeSpan timeframe, DateTimeOffset now) =>
        bars.Where(x => x.Timestamp + timeframe <= now)
            .OrderBy(x => x.Timestamp)
            .GroupBy(x => x.Timestamp)
            .Select(x => x.Last())
            .ToArray();

    private static object ToChartBar(MarketBar bar) => new
    {
        timestamp = bar.Timestamp,
        open = bar.Open,
        high = bar.High,
        low = bar.Low,
        close = bar.Close,
        volume = bar.Volume
    };

    private static string FreshnessLabel(bool fresh, double? ageSeconds) =>
        ageSeconds is null ? "UNKNOWN" : ageSeconds < 0 ? "FUTURE_TIMESTAMP" : fresh ? "FRESH" : "STALE";
}

/// <summary>
/// PURPOSE: Computes the technical inputs consumed by the V2 signal engine from timestamped OHLCV.
/// WHEN CALLED: LiveSymbolAssessmentService builds the latest 1m/5m snapshots.
/// </summary>
public static class LiveSymbolTechnicalCalculator
{
    private static readonly TimeZoneInfo Eastern = ResolveEasternTimeZone();

    /// <summary>
    /// PURPOSE: Builds one fully populated V2 BarSnapshot using only bars through the requested index.
    /// WHEN CALLED: For the current and previous completed 5m bar and the current completed 1m bar.
    /// </summary>
    public static BarSnapshot Calculate(
        IReadOnlyList<MarketBar> bars,
        int index,
        TimeSpan timeframe,
        decimal rvol5m)
    {
        if (index < 49 || index >= bars.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "At least 50 bars through index are required.");

        var closes = bars.Take(index + 1).Select(x => (decimal)x.Close).ToArray();
        var latest = bars[index];
        var ema9 = Ema(closes, 9);
        var ema20 = Ema(closes, 20);
        var sma50 = closes.TakeLast(50).Average();
        var rsi = Rsi(closes, 14);
        var (macd, macdSignal) = Macd(closes);
        var atr = Atr(bars, index, 14);
        var vwap = SessionVwap(bars, index);
        var extension = atr > 0m ? Math.Max(0m, ((decimal)latest.Close - ema9) / atr) : 0m;

        return new BarSnapshot(
            latest.Timestamp,
            (decimal)latest.Open,
            (decimal)latest.High,
            (decimal)latest.Low,
            (decimal)latest.Close,
            latest.Volume,
            rvol5m,
            vwap,
            ema9,
            ema20,
            sma50,
            rsi,
            macd,
            macdSignal,
            atr,
            extension);
    }

    /// <summary>
    /// PURPOSE: Computes same-clock-slot relative volume from prior regular-session days.
    /// WHEN CALLED: For the current and previous 5m bar before the V2 permission evaluation.
    /// </summary>
    public static (decimal? Value, int Samples) CalculateSameTimeOfDayRelativeVolume(
        IReadOnlyList<MarketBar> bars,
        int index)
    {
        if (index < 0 || index >= bars.Count)
            return (null, 0);

        var current = bars[index];
        var local = TimeZoneInfo.ConvertTime(current.Timestamp, Eastern);
        var slot = SessionSlot(local, 5);
        if (slot is null)
            return (null, 0);

        var currentDate = DateOnly.FromDateTime(local.DateTime);
        var samples = bars.Take(index)
            .Select(bar => new
            {
                Bar = bar,
                Local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern)
            })
            .Where(x => DateOnly.FromDateTime(x.Local.DateTime) < currentDate &&
                        SessionSlot(x.Local, 5) == slot)
            .GroupBy(x => DateOnly.FromDateTime(x.Local.DateTime))
            .OrderByDescending(group => group.Key)
            .Take(10)
            .Select(group => group.OrderByDescending(x => x.Bar.Timestamp).First().Bar.Volume)
            .Where(volume => volume > 0)
            .ToArray();

        if (samples.Length < 5)
            return (null, samples.Length);

        var average = samples.Average(x => (decimal)x);
        return average <= 0m ? (null, samples.Length) : ((decimal)current.Volume / average, samples.Length);
    }

    /// <summary>
    /// PURPOSE: Determines whether the evaluation clock is inside the US regular equity session.
    /// WHEN CALLED: V2 intraday decisions are withheld outside 09:30–16:00 Eastern on weekdays.
    /// </summary>
    public static bool IsRegularSession(DateTimeOffset timestamp)
    {
        var local = TimeZoneInfo.ConvertTime(timestamp, Eastern);
        var time = local.TimeOfDay;
        return local.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
               time >= new TimeSpan(9, 30, 0) && time < new TimeSpan(16, 0, 0);
    }

    private static decimal Ema(IReadOnlyList<decimal> values, int period)
    {
        if (values.Count < period) return 0m;
        var ema = values.Take(period).Average();
        var multiplier = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
            ema = (values[i] - ema) * multiplier + ema;
        return ema;
    }

    private static decimal Rsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period) return 50m;
        decimal averageGain = 0m, averageLoss = 0m;
        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain += Math.Max(0m, change);
            averageLoss += Math.Max(0m, -change);
        }
        averageGain /= period;
        averageLoss /= period;
        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            averageGain = (averageGain * (period - 1) + Math.Max(0m, change)) / period;
            averageLoss = (averageLoss * (period - 1) + Math.Max(0m, -change)) / period;
        }
        if (averageLoss == 0m) return averageGain == 0m ? 50m : 100m;
        var relativeStrength = averageGain / averageLoss;
        return 100m - 100m / (1m + relativeStrength);
    }

    private static (decimal Macd, decimal Signal) Macd(IReadOnlyList<decimal> closes)
    {
        if (closes.Count < 35) return (0m, 0m);
        var ema12 = EmaSeries(closes, 12);
        var ema26 = EmaSeries(closes, 26);
        var line = Enumerable.Range(0, closes.Count).Select(i => ema12[i] - ema26[i]).ToArray();
        var signal = Ema(line, 9);
        return (line[^1], signal);
    }

    private static decimal[] EmaSeries(IReadOnlyList<decimal> values, int period)
    {
        var result = new decimal[values.Count];
        if (values.Count == 0) return result;
        var seed = values.Take(Math.Min(period, values.Count)).Average();
        for (var i = 0; i < Math.Min(period, values.Count); i++) result[i] = seed;
        var multiplier = 2m / (period + 1m);
        for (var i = period; i < values.Count; i++)
            result[i] = (values[i] - result[i - 1]) * multiplier + result[i - 1];
        return result;
    }

    private static decimal Atr(IReadOnlyList<MarketBar> bars, int index, int period)
    {
        if (index + 1 < period) return 0m;
        decimal initialTotal = 0m;
        decimal atr = 0m;
        for (var i = 0; i <= index; i++)
        {
            var current = bars[i];
            var previousClose = i > 0 ? (decimal)bars[i - 1].Close : (decimal)current.Close;
            var high = (decimal)current.High;
            var low = (decimal)current.Low;
            var trueRange = Math.Max(high - low,
                Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
            if (i < period)
            {
                initialTotal += trueRange;
                if (i == period - 1) atr = initialTotal / period;
            }
            else
            {
                atr = (atr * (period - 1) + trueRange) / period;
            }
        }
        return atr;
    }

    private static decimal SessionVwap(IReadOnlyList<MarketBar> bars, int index)
    {
        var current = bars[index];
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(current.Timestamp, Eastern).DateTime);
        var sessionBars = bars.Take(index + 1)
            .Where(bar =>
            {
                var local = TimeZoneInfo.ConvertTime(bar.Timestamp, Eastern);
                return DateOnly.FromDateTime(local.DateTime) == localDate && SessionSlot(local, 1) is not null;
            })
            .ToArray();
        var volume = sessionBars.Sum(x => (decimal)x.Volume);
        if (volume <= 0m) return (decimal)current.Close;
        return sessionBars.Sum(x => (((decimal)x.High + (decimal)x.Low + (decimal)x.Close) / 3m) * x.Volume) / volume;
    }

    private static int? SessionSlot(DateTimeOffset easternTimestamp, int intervalMinutes)
    {
        var minutes = (int)easternTimestamp.TimeOfDay.TotalMinutes;
        var start = 9 * 60 + 30;
        var end = 16 * 60;
        return minutes < start || minutes >= end ? null : (minutes - start) / intervalMinutes;
    }

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }
}
