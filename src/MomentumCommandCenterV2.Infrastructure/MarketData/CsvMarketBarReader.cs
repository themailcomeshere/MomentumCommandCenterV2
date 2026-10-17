using System.Globalization;
using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Infrastructure.MarketData;

/// <summary>
/// ADAPTER: CsvMarketBarReader
/// PURPOSE: Reads raw OHLCV bars from a provider-neutral CSV format.
/// WHEN CALLED: Used for local historical fixtures and file-based replay; a provider adapter can later implement the same contract.
/// LIMITATION: Does not calculate indicators, scanner events, catalyst status, or fills.
/// </summary>
public sealed class CsvMarketBarReader
{
    private static readonly string[] RequiredHeaders =
        ["Symbol", "Timeframe", "Timestamp", "Open", "High", "Low", "Close", "Volume"];

    /// <summary>
    /// METHOD: Read
    /// PURPOSE: Parses CSV rows into validated MarketBar records.
    /// WHEN CALLED: Called before replay; timestamps must contain an explicit timezone offset.
    /// FORMAT: Header names are case-insensitive and numeric values use invariant culture.
    /// </summary>
    /// <summary>
    /// METHOD: Read
    /// PURPOSE: Parses and validates provider-neutral OHLCV CSV rows.
    /// WHEN CALLED: Invoked by local replay workflows before frames are built.
    /// </summary>
    public IReadOnlyList<MarketBar> Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new FormatException("Market-data CSV is empty or has no header row.");

        var headers = ParseCsvLine(headerLine);
        var indexes = RequiredHeaders.ToDictionary(header => header,
            header => Array.FindIndex(headers, value => string.Equals(value.Trim(), header, StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);
        var missing = RequiredHeaders.Where(header => indexes[header] < 0).ToArray();
        if (missing.Length > 0)
            throw new FormatException($"Market-data CSV is missing required columns: {string.Join(", ", missing)}.");

        var bars = new List<MarketBar>();
        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var fields = ParseCsvLine(line);
                string Field(string name)
                {
                    var index = indexes[name];
                    if (index >= fields.Length) throw new FormatException($"Missing value for {name}.");
                    return fields[index].Trim();
                }

                var timeframe = Field("Timeframe") switch
                {
                    "1" or "1m" or "1M" or "OneMinute" => BarTimeframe.OneMinute,
                    "5" or "5m" or "5M" or "FiveMinute" => BarTimeframe.FiveMinute,
                    var value => throw new FormatException($"Unsupported timeframe '{value}'. Use 1m or 5m.")
                };
                var timestampText = Field("Timestamp");
                if (!System.Text.RegularExpressions.Regex.IsMatch(timestampText, @"(?:Z|[+-]\d{2}:\d{2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                    !DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var timestamp))
                    throw new FormatException("Timestamp must be ISO-8601 with an explicit timezone offset.");

                decimal Number(string name) => decimal.Parse(Field(name), NumberStyles.Number, CultureInfo.InvariantCulture);
                bars.Add(new MarketBar(Field("Symbol").ToUpperInvariant(), timeframe, timestamp,
                    Number("Open"), Number("High"), Number("Low"), Number("Close"), Number("Volume")));
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
            {
                throw new FormatException($"Invalid market-data CSV at line {lineNumber}: {exception.Message}", exception);
            }
        }

        // Reuse replay validation so malformed or duplicate records are rejected before the data is returned.
        try
        {
            _ = new MomentumCommandCenterV2.Core.MarketReplayEngine().Replay(bars);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException($"Market-data CSV failed OHLCV or duplicate-key validation: {exception.Message}", exception);
        }

        return bars;
    }

    /// <summary>
    /// METHOD: ParseCsvLine
    /// PURPOSE: Splits a CSV row while respecting quoted commas and escaped double quotes.
    /// WHEN CALLED: Used for the header and each non-empty data row.
    /// LIMITATION: Newlines embedded inside quoted fields are not supported by this fixture format.
    /// </summary>
    /// <summary>
    /// METHOD: ParseCsvLine
    /// PURPOSE: Parses a single CSV line with quoted-field support.
    /// WHEN CALLED: Called for the header and each non-empty market-data row.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else field.Append(character);
        }
        if (quoted) throw new FormatException("CSV row contains an unclosed quoted field.");
        fields.Add(field.ToString());
        return fields.ToArray();
    }
}
