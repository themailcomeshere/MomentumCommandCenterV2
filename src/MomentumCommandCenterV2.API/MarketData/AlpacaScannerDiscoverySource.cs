using System.Net.Http.Headers;
using System.Text.Json;
using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Retrieves Alpaca's top stock gainers and losers. The endpoint is SIP-based;
/// authentication/entitlement failures are returned as degraded source health.
/// Discovery events are not trading signals.
/// </summary>
public sealed class AlpacaScannerDiscoverySource(HttpClient client, IConfiguration configuration) : IScannerDiscoverySource
{
    private readonly string? _key = configuration["MarketData:Alpaca:ApiKey"] ?? Environment.GetEnvironmentVariable("ALPACA_API_KEY");
    private readonly string? _secret = configuration["MarketData:Alpaca:ApiSecret"] ?? Environment.GetEnvironmentVariable("ALPACA_API_SECRET");
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_key) && !string.IsNullOrWhiteSpace(_secret);

    public async Task<IReadOnlyList<ScannerDiscoveryEvent>> GetDiscoveriesAsync(DateTimeOffset observedAt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new ProviderNotConfiguredException("Alpaca screener requires server-side API credentials.");
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1beta1/screener/stocks/movers?top=50");
        request.Headers.Add("APCA-API-KEY-ID", _key!);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret!);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpaca movers returned {(int)response.StatusCode}: {(body.Length > 400 ? body[..400] : body)}");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        DateTimeOffset? sourceTime = null;
        if (root.TryGetProperty("last_updated", out var updated) &&
            DateTimeOffset.TryParse(updated.GetString(), out var parsed))
            sourceTime = parsed;

        var events = new List<ScannerDiscoveryEvent>();
        AddGroup(root, "gainers", "ALPACA_TOP_GAINERS", observedAt, sourceTime, events);
        AddGroup(root, "losers", "ALPACA_TOP_LOSERS", observedAt, sourceTime, events);
        return events;
    }

    private static void AddGroup(JsonElement root, string propertyName, string group,
        DateTimeOffset observedAt, DateTimeOffset? sourceTime, List<ScannerDiscoveryEvent> events)
    {
        if (!root.TryGetProperty(propertyName, out var items) || items.ValueKind != JsonValueKind.Array) return;
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("symbol", out var symbolValue)) continue;
            var symbol = symbolValue.GetString();
            if (string.IsNullOrWhiteSpace(symbol)) continue;
            var fields = new Dictionary<string, string?>();
            foreach (var name in new[] { "change", "percent_change", "price" })
                if (item.TryGetProperty(name, out var value)) fields[name] = value.ToString();

            var timestamp = sourceTime ?? observedAt;
            events.Add(new ScannerDiscoveryEvent(
                symbol.ToUpperInvariant(), group, "alpaca:screener:stocks:movers",
                $"{group}:{symbol.ToUpperInvariant()}:{timestamp:O}", timestamp, observedAt, sourceTime,
                "alpaca-movers-v1", fields, ScannerCandidateDisposition.DataUnknown,
                new[] { "ALPACA_TOP_MOVER", "NOT_A_TRADE_SIGNAL" }, observedAt, "mcc-alpaca-discovery-v1"));
        }
    }
}
