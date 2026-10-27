using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MomentumCommandCenterV2.API.MarketData;

/// <summary>
/// PURPOSE: Convert an Alpaca/IEX OHLCV CSV export into MCC's canonical 17-column parity shape.
/// WHEN CALLED: POST /api/market-data/import-csv receives a CSV file.
/// IMPORTANT: Export values populate Payload* only. Chart* and ChartTimestamp remain blank until an independent chart export is supplied.
/// </summary>
public static class AlpacaCsvParityParser
{
    public static readonly string[] ParityHeaders =
    {
        "SourceRow", "Symbol", "Timeframe", "PayloadTimestampUtc", "PayloadOpen", "PayloadHigh",
        "PayloadLow", "PayloadClose", "PayloadVolume", "ChartTimestamp", "ChartOpen", "ChartHigh",
        "ChartLow", "ChartClose", "ChartVolume", "ChartTimestampMeaning", "Finding"
    };

    public static IReadOnlyList<IReadOnlyDictionary<string, string>> Parse(
        string csv, string? defaultSymbol = null, string? defaultTimeframe = null)
    {
        if (string.IsNullOrWhiteSpace(csv)) throw new ArgumentException("CSV file is empty.", nameof(csv));
        var trimmed = csv.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try { return ParseAlpacaBarsJson(trimmed, defaultSymbol, defaultTimeframe); }
            catch (JsonException ex) { throw new FormatException("JSON import is not a valid Alpaca historical-bars response.", ex); }
        }

        var records = ParseRecords(csv);
        var headerIndex = records.FindIndex(row =>
        {
            var names = row.Select(NormalizeHeader).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return names.Contains("timestamp") || names.Contains("time") || names.Contains("datetime")
                ? names.Contains("open") && names.Contains("high") && names.Contains("low")
                    && names.Contains("close") && names.Contains("volume")
                : false;
        });
        if (headerIndex < 0) throw new FormatException("Could not find an Alpaca OHLCV CSV header. Required columns: timestamp/time, open, high, low, close, volume.");

        var headers = records[headerIndex].Select(NormalizeHeader).ToArray();
        int Index(params string[] choices)
        {
            for (var i = 0; i < headers.Length; i++)
                if (choices.Contains(headers[i], StringComparer.OrdinalIgnoreCase)) return i;
            return -1;
        }
        var timeIndex = Index("timestamp", "time", "datetime");
        var openIndex = Index("open"); var highIndex = Index("high"); var lowIndex = Index("low");
        var closeIndex = Index("close"); var volumeIndex = Index("volume");
        var symbolIndex = Index("symbol", "ticker");
        var timeframeIndex = Index("timeframe", "interval", "aggregation");
        var result = new List<IReadOnlyDictionary<string, string>>();
        for (var rowIndex = headerIndex + 1; rowIndex < records.Count; rowIndex++)
        {
            var row = records[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Count != headers.Length) throw new FormatException($"CSV row {rowIndex + 1} has {row.Count} fields; expected {headers.Length}.");
            if (!DateTimeOffset.TryParse(row[timeIndex], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
                throw new FormatException($"CSV row {rowIndex + 1} has an invalid timestamp: '{row[timeIndex]}'.");
            var symbol = symbolIndex >= 0 ? row[symbolIndex].Trim() : defaultSymbol?.Trim() ?? "";
            var timeframe = timeframeIndex >= 0 ? row[timeframeIndex].Trim() : defaultTimeframe?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(symbol))
                throw new FormatException($"CSV row {rowIndex + 1} has no symbol. Include a symbol/ticker column or supply the symbol form field.");
            if (timeframe is not ("1m" or "5m"))
                throw new FormatException($"CSV row {rowIndex + 1} has unsupported or missing timeframe '{timeframe}'. Include a timeframe/interval column or supply 1m or 5m.");
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SourceRow"] = (rowIndex + 1).ToString(CultureInfo.InvariantCulture),
                ["Symbol"] = symbol.ToUpperInvariant(),
                ["Timeframe"] = timeframe,
                ["PayloadTimestampUtc"] = timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["PayloadOpen"] = Number(row[openIndex], rowIndex + 1, "open"),
                ["PayloadHigh"] = Number(row[highIndex], rowIndex + 1, "high"),
                ["PayloadLow"] = Number(row[lowIndex], rowIndex + 1, "low"),
                ["PayloadClose"] = Number(row[closeIndex], rowIndex + 1, "close"),
                ["PayloadVolume"] = Number(row[volumeIndex], rowIndex + 1, "volume"),
                ["ChartTimestamp"] = "", ["ChartOpen"] = "", ["ChartHigh"] = "",
                ["ChartLow"] = "", ["ChartClose"] = "", ["ChartVolume"] = "",
                ["ChartTimestampMeaning"] = "Blank: no independent chart export was supplied.",
                ["Finding"] = "Imported Alpaca/IEX payload; chart parity is unverified."
            };
            result.Add(values);
        }
        if (result.Count == 0) throw new FormatException("No OHLCV data rows were found after the CSV header.");
        return result;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ParseAlpacaBarsJson(
        string json, string? defaultSymbol, string? defaultTimeframe)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("bars", out var bars)
            || bars.ValueKind != JsonValueKind.Array)
            throw new FormatException("JSON import must be an Alpaca historical-bars response containing a 'bars' array.");

        var rootSymbol = JsonText(root, "symbol") ?? defaultSymbol?.Trim();
        var rootTimeframe = JsonText(root, "timeframe") ?? defaultTimeframe?.Trim();
        var result = new List<IReadOnlyDictionary<string, string>>();
        var sourceRow = 0;
        foreach (var bar in bars.EnumerateArray())
        {
            sourceRow++;
            var symbol = JsonText(bar, "symbol") ?? rootSymbol ?? "";
            var timeframe = JsonText(bar, "timeframe") ?? rootTimeframe ?? "";
            if (string.IsNullOrWhiteSpace(symbol))
                throw new FormatException($"JSON bar {sourceRow} has no symbol. Include a symbol in the response or supply the symbol form field.");
            if (timeframe is not ("1m" or "5m"))
                throw new FormatException($"JSON bar {sourceRow} has unsupported or missing timeframe '{timeframe}'. Include timeframe in the response or supply 1m or 5m.");
            var timestampText = JsonText(bar, "timestamp") ?? JsonText(bar, "t");
            if (!DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
                throw new FormatException($"JSON bar {sourceRow} has an invalid timestamp: '{timestampText}'.");

            string Numeric(string name) => Number(JsonText(bar, name) ?? "", sourceRow, name);
            result.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SourceRow"] = sourceRow.ToString(CultureInfo.InvariantCulture),
                ["Symbol"] = symbol.ToUpperInvariant(),
                ["Timeframe"] = timeframe,
                ["PayloadTimestampUtc"] = timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                ["PayloadOpen"] = Numeric("open"), ["PayloadHigh"] = Numeric("high"),
                ["PayloadLow"] = Numeric("low"), ["PayloadClose"] = Numeric("close"),
                ["PayloadVolume"] = Numeric("volume"), ["ChartTimestamp"] = "",
                ["ChartOpen"] = "", ["ChartHigh"] = "", ["ChartLow"] = "",
                ["ChartClose"] = "", ["ChartVolume"] = "",
                ["ChartTimestampMeaning"] = "Blank: no independent chart export was supplied.",
                ["Finding"] = "Imported Alpaca historical-bars JSON; chart parity is unverified."
            });
        }
        if (result.Count == 0) throw new FormatException("No OHLCV data rows were found in the JSON bars array.");
        return result;
    }

    private static string? JsonText(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
    }

    private static string Number(string value, int rowNumber, string field)
    {
        if (!decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
            throw new FormatException($"CSV row {rowNumber} has an invalid {field} value: '{value}'.");
        return parsed.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private static string NormalizeHeader(string header) =>
        header.Trim().TrimStart('\uFEFF').ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("-", "");

    private static List<List<string>> ParseRecords(string text)
    {
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var records = new List<List<string>>();
        var row = new List<string>(); var field = new StringBuilder(); var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted) { row.Add(field.ToString()); field.Clear(); }
            else if (ch == '\n' && !quoted) { row.Add(field.ToString()); field.Clear(); records.Add(row); row = new List<string>(); }
            else field.Append(ch);
        }
        if (quoted) throw new FormatException("CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row); }
        return records;
    }
}
