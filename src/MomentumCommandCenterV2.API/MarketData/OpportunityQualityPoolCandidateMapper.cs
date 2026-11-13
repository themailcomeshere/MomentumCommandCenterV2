using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Converts one serialized live assessment into an Opportunity Quality pool candidate.
/// WHEN CALLED: GET /api/market-data/opportunity-quality/rank, once per supplied symbol.
/// IMPORTANT: Exclusion-watchlist symbols are hard-blocked regardless of their diagnostic class.
/// Missing fields remain null/DATA?; per-symbol provider, feed and 1m/5m source timestamps are carried
/// through so every ranked row is traceable to its evidence.
/// </summary>
public static class OpportunityQualityPoolCandidateMapper
{
    public const string ExcludedReason = "EXCLUDED_WATCHLIST";

    public static OpportunityQualityPoolCandidate FromAssessment(string symbol, JsonElement root, bool excluded)
    {
        var quality = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("opportunityQuality", out var q) &&
                      q.ValueKind == JsonValueKind.Object ? q : default;
        var metrics = Property(quality, "metrics");

        decimal? Metric(string name) =>
            Property(metrics, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetDecimal(out var number)
                ? number : null;

        var decision = Property(root, "decision");
        bool? entryAllowed = Property(decision, "entryAllowed") is { ValueKind: JsonValueKind.True or JsonValueKind.False } entry
            ? entry.GetBoolean() : null;
        var reasons = Property(quality, "reasons") is { ValueKind: JsonValueKind.Array } reasonArray
            ? reasonArray.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
            : new List<string>();

        var status = String(Property(quality, "status")) ?? "DATA?";
        var classification = String(Property(quality, "classification")) ?? "DataUnknown";
        var label = String(Property(quality, "displayLabel")) ?? "QUALITY — UNKNOWN";
        if (excluded)
        {
            status = "BLOCKED";
            classification = "Blocked";
            label = "QUALITY — EXCLUDED";
            reasons.Insert(0, ExcludedReason);
        }

        var freshness = Property(root, "freshnessDiagnostics");
        return new OpportunityQualityPoolCandidate(
            symbol,
            status,
            classification,
            label,
            Metric("relativeVolume"),
            Metric("recentRangeInAtr"),
            Metric("resistanceDistanceInAtr"),
            entryAllowed,
            Property(root, "dataFresh") is { ValueKind: JsonValueKind.True },
            reasons)
        {
            Provider = String(Property(root, "provider")),
            Feed = String(Property(root, "feed")),
            OneMinuteSourceTimestampUtc = Timestamp(Property(Property(freshness, "oneMinute"), "sourceTimestampUtc")),
            FiveMinuteSourceTimestampUtc = Timestamp(Property(Property(freshness, "fiveMinute"), "sourceTimestampUtc")),
            EvaluatedAtUtc = Timestamp(Property(root, "evaluatedAtUtc")),
            Excluded = excluded
        };
    }

    public static OpportunityQualityPoolCandidate FromFailure(string symbol, Exception error, bool excluded, string? provider)
    {
        var reasons = new List<string> { "ASSESSMENT_FAILED", error.GetType().Name };
        if (excluded) reasons.Insert(0, ExcludedReason);
        return new OpportunityQualityPoolCandidate(
            symbol,
            excluded ? "BLOCKED" : "DATA?",
            excluded ? "Blocked" : "DataUnknown",
            excluded ? "QUALITY — EXCLUDED" : "QUALITY — UNKNOWN",
            null, null, null, null, false, reasons)
        {
            Provider = provider,
            Excluded = excluded
        };
    }

    private static JsonElement Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;

    private static string? String(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    private static DateTimeOffset? Timestamp(JsonElement element) =>
        element.ValueKind == JsonValueKind.String && element.TryGetDateTimeOffset(out var value) ? value.ToUniversalTime() : null;
}
