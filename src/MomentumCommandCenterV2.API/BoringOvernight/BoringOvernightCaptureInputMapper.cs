using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// PURPOSE: Pure mapping rules used by POST /api/boring-overnight/capture-history.
/// WHEN CALLED: After the capture has fetched history, Daily metrics and the live 1m/5m assessment.
/// IMPORTANT: Extracted from Program.cs so the fail-closed rules are unit-testable. Missing,
/// stale or mismatched evidence yields null fields; nothing is inferred or substituted.
/// </summary>
public static class BoringOvernightCaptureInputMapper
{
    /// <summary>
    /// Maps a serialized LiveSymbolAssessmentService result into evaluator technical inputs.
    /// Provider and both 1m/5m feeds must equal the capture provider/feed; missing provenance is a mismatch.
    /// Numeric indicators require fresh matched bars; V2 fields additionally require EVALUATED.
    /// </summary>
    public static BoringOvernightCaptureTechnicalInputs MapTechnicalInputs(
        JsonElement? assessment,
        string captureProvider,
        string? captureFeed,
        string? assessmentError = null)
    {
        var assessmentProvider = JsonString(JsonPath(assessment, "provider"));
        var oneMinuteFeed = JsonString(JsonPath(assessment, "oneMinute", "feed"));
        var fiveMinuteFeed = JsonString(JsonPath(assessment, "fiveMinute", "feed"));
        var configuredFeed = JsonString(JsonPath(assessment, "configuredFeed"));
        var sourceTransport = JsonString(JsonPath(assessment, "oneMinute", "sourceTransport"));

        var feedMatches =
            assessment is not null &&
            captureFeed is not null &&
            string.Equals(assessmentProvider, captureProvider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(oneMinuteFeed, captureFeed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fiveMinuteFeed, captureFeed, StringComparison.OrdinalIgnoreCase);

        var error = assessmentError;
        var source = assessment;
        if (assessment is not null && !feedMatches)
        {
            error =
                $"TECHNICAL_FEED_MISMATCH: capture provider/feed={captureProvider}/{captureFeed ?? "UNKNOWN"}; " +
                $"assessment provider/1m feed/5m feed={assessmentProvider ?? "UNKNOWN"}/" +
                $"{oneMinuteFeed ?? "UNKNOWN"}/{fiveMinuteFeed ?? "UNKNOWN"}. " +
                "Technical fields were withheld rather than combining observations from different sources.";
            source = null;
        }

        var rawPermission = JsonBoolean(JsonPath(source, "fiveMinute", "scoreBreakdown", "permission"));
        var rawStructureBull = JsonBoolean(JsonPath(source, "decision", "fiveMinuteStructureBull"));
        var stateValue = JsonPath(source, "decision", "state");
        var actionValue = JsonPath(source, "decision", "action");
        var rawState = stateValue is { ValueKind: JsonValueKind.String } state
            ? state.GetString() + (actionValue is { ValueKind: JsonValueKind.String } action ? "/" + action.GetString() : string.Empty)
            : null;
        var rawExtensionAtr = JsonNumber(JsonPath(source, "oneMinute", "extensionAtr"));
        var rawRelativeVolume = JsonNumber(JsonPath(source, "fiveMinute", "relativeVolume"));
        var oneMinuteTimestamp = JsonTimestamp(JsonPath(source, "oneMinute", "sourceTimestampUtc"));
        var fiveMinuteTimestamp = JsonTimestamp(JsonPath(source, "fiveMinute", "sourceTimestampUtc"));
        var oneMinuteFresh = JsonBoolean(JsonPath(source, "freshnessDiagnostics", "oneMinute", "isFresh"));
        var fiveMinuteFresh = JsonBoolean(JsonPath(source, "freshnessDiagnostics", "fiveMinute", "isFresh"));
        var evaluationStatus = JsonString(JsonPath(source, "evaluationStatus"));

        var barsUsable = BoringOvernightTechnicalInputPolicy.AreIntradayBarsUsable(feedMatches, oneMinuteFresh, fiveMinuteFresh);
        var v2Usable = BoringOvernightTechnicalInputPolicy.IsV2DecisionUsable(barsUsable, evaluationStatus);
        var structureBull = v2Usable ? rawStructureBull : null;

        return new BoringOvernightCaptureTechnicalInputs(
            AssessmentProvider: assessmentProvider,
            OneMinuteFeed: oneMinuteFeed,
            FiveMinuteFeed: fiveMinuteFeed,
            ConfiguredFeed: configuredFeed,
            SourceTransport: sourceTransport,
            FeedMatchesCapture: feedMatches,
            IntradayBarsUsable: barsUsable,
            V2DecisionUsable: v2Usable,
            EvaluationStatus: evaluationStatus,
            FiveMinutePermission: v2Usable ? rawPermission : null,
            FiveMinuteStructure: structureBull == true ? "BULLISH" : structureBull == false ? "NOT_BULLISH" : null,
            OneMinuteState: v2Usable ? rawState : null,
            ExtensionAtr: barsUsable ? rawExtensionAtr : null,
            CurrentVolumeVsAverage: barsUsable ? rawRelativeVolume : null,
            OneMinuteSourceTimestampUtc: oneMinuteTimestamp,
            FiveMinuteSourceTimestampUtc: fiveMinuteTimestamp,
            OneMinuteFresh: oneMinuteFresh,
            FiveMinuteFresh: fiveMinuteFresh,
            OneMinuteAgeSeconds: JsonNumber(JsonPath(source, "freshnessDiagnostics", "oneMinute", "ageSeconds")),
            FiveMinuteAgeSeconds: JsonNumber(JsonPath(source, "freshnessDiagnostics", "fiveMinute", "ageSeconds")),
            OneMinuteFreshnessLimitSeconds: JsonNumber(JsonPath(source, "freshnessDiagnostics", "oneMinute", "limitSeconds")),
            FiveMinuteFreshnessLimitSeconds: JsonNumber(JsonPath(source, "freshnessDiagnostics", "fiveMinute", "limitSeconds")),
            Error: error);
    }

    /// <summary>
    /// Daily range % and 14-session simple mean true range % from a complete, fresh,
    /// qualification-usable Daily sample only (IEX_PARTIAL is never qualification-usable).
    /// Requires at least 15 completed sessions so every true range has a prior close.
    /// </summary>
    public static BoringOvernightDailyTechnicals CalculateDailyTechnicals(DailyMarketMetrics? daily)
    {
        if (daily is not { QualificationUsable: true, DataFresh: true } || daily.Sessions.Count < 15)
            return new BoringOvernightDailyTechnicals(null, null, null);

        var sessions = daily.Sessions.OrderBy(x => x.SessionDate).ToArray();
        var latest = sessions[^1];
        if (!(latest.Close > 0))
            return new BoringOvernightDailyTechnicals(null, null, null);

        var rangePct = (latest.High - latest.Low) / latest.Close * 100d;
        var trueRanges = new double[14];
        for (var i = 0; i < 14; i++)
        {
            var session = sessions[sessions.Length - 14 + i];
            var previousClose = sessions[sessions.Length - 15 + i].Close;
            trueRanges[i] = Math.Max(session.High - session.Low,
                Math.Max(Math.Abs(session.High - previousClose), Math.Abs(session.Low - previousClose)));
        }
        double? atrPct = trueRanges.All(double.IsFinite) ? trueRanges.Average() / latest.Close * 100d : null;
        return new BoringOvernightDailyTechnicals(double.IsFinite(rangePct) ? rangePct : null, atrPct, latest.TimestampUtc);
    }

    /// <summary>
    /// Current Overnight interval state. PENDING until the exact 09:30 ET one-minute bar has completed
    /// (bar start + 1 minute); then OBSERVED only with the exact close and exact open bars, else MISSING.
    /// No nearest-bar substitution: callers must pass only exact-timestamp bars.
    /// </summary>
    public static BoringOvernightCurrentIntervalResult ClassifyCurrentInterval(
        OvernightSessionBoundary boundary,
        MarketBar? exactCloseBar,
        MarketBar? exactOpenBar,
        DateTimeOffset nowUtc)
    {
        if (exactCloseBar is not null && exactCloseBar.Timestamp != boundary.PreviousCloseBarTimestampUtc)
            throw new ArgumentException("Close bar must match the exact previous-close boundary timestamp.", nameof(exactCloseBar));
        if (exactOpenBar is not null && exactOpenBar.Timestamp != boundary.NextOpenBarTimestampUtc)
            throw new ArgumentException("Open bar must match the exact next-open boundary timestamp.", nameof(exactOpenBar));

        var endpointAvailableAtUtc = boundary.NextOpenBarTimestampUtc.AddMinutes(1);
        var both = exactCloseBar is not null && exactOpenBar is not null && exactCloseBar.Close > 0 && exactOpenBar.Open > 0;
        var state = nowUtc < endpointAvailableAtUtc ? "PENDING" : both ? "OBSERVED" : "MISSING";
        decimal? returnPct = both ? (decimal?)((exactOpenBar!.Open / exactCloseBar!.Close - 1d) * 100d) : null;
        return new BoringOvernightCurrentIntervalResult(state, endpointAvailableAtUtc, exactCloseBar?.Close, exactOpenBar?.Open, returnPct);
    }

    private static JsonElement? JsonPath(JsonElement? root, params string[] path)
    {
        if (root is null) return null;
        var current = root.Value;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }
        return current;
    }

    private static string? JsonString(JsonElement? value) =>
        value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : value.Value.ToString();

    private static double? JsonNumber(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.Number } element && element.TryGetDouble(out var number) && double.IsFinite(number)
            ? number : null;

    private static bool? JsonBoolean(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.True } ? true :
        value is { ValueKind: JsonValueKind.False } ? false : null;

    private static DateTimeOffset? JsonTimestamp(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.String } element && element.TryGetDateTimeOffset(out var timestamp) && timestamp.Offset == TimeSpan.Zero
            ? timestamp : null;
}

public sealed record BoringOvernightCaptureTechnicalInputs(
    string? AssessmentProvider,
    string? OneMinuteFeed,
    string? FiveMinuteFeed,
    string? ConfiguredFeed,
    string? SourceTransport,
    bool FeedMatchesCapture,
    bool IntradayBarsUsable,
    bool V2DecisionUsable,
    string? EvaluationStatus,
    bool? FiveMinutePermission,
    string? FiveMinuteStructure,
    string? OneMinuteState,
    double? ExtensionAtr,
    double? CurrentVolumeVsAverage,
    DateTimeOffset? OneMinuteSourceTimestampUtc,
    DateTimeOffset? FiveMinuteSourceTimestampUtc,
    bool? OneMinuteFresh,
    bool? FiveMinuteFresh,
    double? OneMinuteAgeSeconds,
    double? FiveMinuteAgeSeconds,
    double? OneMinuteFreshnessLimitSeconds,
    double? FiveMinuteFreshnessLimitSeconds,
    string? Error);

public sealed record BoringOvernightDailyTechnicals(double? DailyRangePct, double? AtrPct, DateTimeOffset? SourceTimestampUtc);

public sealed record BoringOvernightCurrentIntervalResult(
    string State,
    DateTimeOffset EndpointAvailableAtUtc,
    double? PreviousClose,
    double? NextOpen,
    decimal? ReturnPct);
