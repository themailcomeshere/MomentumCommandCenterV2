using System.Net.Http.Headers;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// Optional extended-hours reference snapshot from Yahoo Finance chart metadata.
/// This is an indicative secondary source, not consolidated NBBO or executable quote data.
/// </summary>
public sealed class YahooFinanceQuoteClient(HttpClient client)
{
    public async Task<YahooFinanceQuote?> GetQuoteAsync(string symbol, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8 ||
            !symbol.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-'))
            throw new ArgumentException("Symbol must be 1-8 letters, digits, dot, or hyphen.", nameof(symbol));

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"v8/finance/chart/{Uri.EscapeDataString(symbol.ToUpperInvariant())}?range=1d&interval=1m&includePrePost=true");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Yahoo Finance quote returned {(int)response.StatusCode} {response.ReasonPhrase}.", null, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var chart = document.RootElement.GetProperty("chart");
        if (chart.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException("Yahoo Finance chart returned an error: " + error.ToString());
        if (!chart.TryGetProperty("result", out var results) || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            return null;

        var meta = results[0].GetProperty("meta");
        string? ReadString(string name) => meta.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        decimal? ReadDecimal(string name) => meta.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) ? number : null;
        DateTimeOffset? ReadUnix(string name) => meta.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;

        return new YahooFinanceQuote(
            symbol.ToUpperInvariant(),
            ReadString("longName") ?? ReadString("shortName"),
            ReadDecimal("regularMarketPrice"),
            ReadUnix("regularMarketTime"),
            ReadDecimal("postMarketPrice"),
            ReadUnix("postMarketTime"),
            DateTimeOffset.UtcNow,
            "Yahoo Finance chart metadata");
    }
}

public sealed record YahooFinanceQuote(
    string Symbol,
    string? Name,
    decimal? RegularMarketPrice,
    DateTimeOffset? RegularMarketTimeUtc,
    decimal? PostMarketPrice,
    DateTimeOffset? PostMarketTimeUtc,
    DateTimeOffset RetrievedAtUtc,
    string Source);
