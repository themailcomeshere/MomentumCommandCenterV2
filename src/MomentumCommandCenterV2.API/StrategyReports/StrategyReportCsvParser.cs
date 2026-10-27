using System.Globalization;
using System.Text;

namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// PURPOSE: Parse Thinkorswim StrategyReport exports and project explicit MCC OHLCV payloads
/// into the repository's canonical 17-column parity-template schema.
/// WHEN CALLED: POST /api/strategy-report/upload receives a user-exported StrategyReport CSV.
/// IMPORTANT: This does not infer bars from order prices or synthesize missing chart values.
/// </summary>
public static class StrategyReportCsvParser
{
    private static readonly string[] ParityHeaders =
    {
        "SourceRow", "Symbol", "Timeframe", "PayloadTimestampUtc", "PayloadOpen", "PayloadHigh",
        "PayloadLow", "PayloadClose", "PayloadVolume", "ChartTimestamp", "ChartOpen", "ChartHigh",
        "ChartLow", "ChartClose", "ChartVolume", "ChartTimestampMeaning", "Finding"
    };

    public static StrategyReportParseResult Parse(string csv, int maxRows = 10000)
    {
        if (string.IsNullOrWhiteSpace(csv))
            throw new ArgumentException("CSV file is empty.", nameof(csv));
        if (maxRows < 1) throw new ArgumentOutOfRangeException(nameof(maxRows));

        var preamble = new List<string>();
        var dataStart = FindHeaderStart(csv, preamble);
        if (dataStart < 0)
            throw new FormatException("Could not find a StrategyReport table header or a CSV header row.");

        var tableText = csv[dataStart..];
        var delimiter = DetectDelimiter(tableText);
        var records = ParseRecords(tableText, delimiter);
        if (records.Count < 2)
            throw new FormatException("CSV must contain a header row and at least one data row.");

        var headers = records[0].Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column{i + 1}" : h.Trim()).ToArray();
        if (headers.Length == 0 || headers.Length > 200)
            throw new FormatException("CSV header count must be between 1 and 200.");

        var rows = new List<IReadOnlyDictionary<string, string>>();
        var warnings = new List<string>();
        foreach (var record in records.Skip(1))
        {
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            if (rows.Count >= maxRows) throw new FormatException($"CSV exceeds the {maxRows} row limit.");
            if (record.Count != headers.Length)
            {
                warnings.Add($"Skipped data row {rows.Count + 2}: found {record.Count} fields, expected {headers.Length}.");
                continue;
            }
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var key = headers[i];
                if (row.ContainsKey(key)) key = $"{key}_{i + 1}";
                row[key] = record[i].Trim();
            }
            rows.Add(row);
        }
        if (rows.Count == 0) throw new FormatException("No valid data rows found after parsing.");

        var metadataSymbol = preamble.Select(ParseMetadataSymbol).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        var likelyTime = headers.FirstOrDefault(h => h.Contains("time", StringComparison.OrdinalIgnoreCase) || h.Contains("date", StringComparison.OrdinalIgnoreCase));
        var likelySymbol = headers.FirstOrDefault(h => h.Contains("symbol", StringComparison.OrdinalIgnoreCase));
        var likelyEvent = headers.FirstOrDefault(h => h.Contains("signal", StringComparison.OrdinalIgnoreCase) || h.Contains("strategy", StringComparison.OrdinalIgnoreCase) || h.Contains("order", StringComparison.OrdinalIgnoreCase) || h.Contains("event", StringComparison.OrdinalIgnoreCase));
        var parityRows = BuildParityRows(rows, headers, metadataSymbol, warnings);
        return new StrategyReportParseResult(headers, rows.Count, rows.Take(100).ToArray(), rows, parityRows, warnings,
            likelyTime, likelySymbol, likelyEvent,
            parityRows.Count > 0
                ? "Parsed Thinkorswim StrategyReport rows and projected explicit MCC_OHLCV v1 payloads into the repository's 17-column parity-template schema. Chart-side fields remain blank unless present in the source."
                : "Parsed StrategyReport rows, but no explicit MCC_OHLCV v1 payload was found; parity rows were not fabricated. Ordinary order prices are not OHLCV bars.");
    }

    private static int FindHeaderStart(string csv, List<string> preamble)
    {
        var offset = 0;
        while (offset <= csv.Length)
        {
            var newline = csv.IndexOf('\\n', offset);
            var length = newline < 0 ? csv.Length - offset : newline - offset;
            var line = csv.Substring(offset, length).TrimEnd('\\r');
            var trimmed = line.TrimStart('\\uFEFF').Trim();
            if (trimmed.Length > 0 && (trimmed.StartsWith("Id;Strategy;Side;", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Id,Strategy,Side,", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("SourceRow,", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Symbol,", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Symbol;", StringComparison.OrdinalIgnoreCase)))
                return offset;
            if (trimmed.Length > 0) preamble.Add(trimmed);
            if (newline < 0) break;
            offset = newline + 1;
        }
        return -1;
    }

    private static string DetectDelimiter(string text)
    {
        var line = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        var commas = line.Count(c => c == ',');
        var semicolons = line.Count(c => c == ';');
        return semicolons > commas ? ";" : ",";
    }

    private static string? ParseMetadataSymbol(string line)
    {
        const string prefix = "Symbol:";
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var value = line[prefix.Length..].Trim();
        return value.Length > 0 ? value.ToUpperInvariant() : null;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildParityRows(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyList<string> headers,
        string? metadataSymbol,
        List<string> warnings)
    {
        var output = new List<IReadOnlyDictionary<string, string>>();
        foreach (var row in rows)
        {
            var payloadField = row.FirstOrDefault(pair => pair.Value.Contains("MCC_OHLCV|v1|", StringComparison.Ordinal));
            if (string.IsNullOrEmpty(payloadField.Value)) continue;

            var text = payloadField.Value;
            var start = text.IndexOf("MCC_OHLCV|v1|", StringComparison.Ordinal);
            var end = text.IndexOf(')', start);
            text = end >= 0 ? text[start..end] : text[start..];
            var parts = text.Split('|');
            if (parts.Length != 10 || parts[0] != "MCC_OHLCV" || parts[1] != "v1")
            {
                warnings.Add("A row contained an MCC OHLCV marker but did not match the v1 parity payload format.");
                continue;
            }

            var epochText = parts[4].Replace(",", "", StringComparison.Ordinal).Trim();
            if (!long.TryParse(epochText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch))
            {
                warnings.Add("A parity payload had an invalid Unix-millisecond timestamp.");
                continue;
            }

            var result = ParityHeaders.ToDictionary(h => h, _ => "", StringComparer.OrdinalIgnoreCase);
            var sourceRow = GetValue(row, "Id") ?? (output.Count + 1).ToString(CultureInfo.InvariantCulture);
            result["SourceRow"] = sourceRow;
            result["Symbol"] = parts[2].Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(result["Symbol"])) result["Symbol"] = metadataSymbol ?? "";
            result["Timeframe"] = parts[3].Trim();
            result["PayloadTimestampUtc"] = DateTimeOffset.FromUnixTimeMilliseconds(epoch).UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            result["PayloadOpen"] = parts[5].Trim();
            result["PayloadHigh"] = parts[6].Trim();
            result["PayloadLow"] = parts[7].Trim();
            result["PayloadClose"] = parts[8].Trim();
            result["PayloadVolume"] = parts[9].Trim().TrimEnd(';');

            // StrategyReport Date/Time is an order-event timestamp, not proof of the chart bar timestamp.
            // Only fill ChartTimestamp when the source explicitly labels a chart timestamp.
            var chartTime = GetValue(row, "ChartTimestamp");
            if (!string.IsNullOrWhiteSpace(chartTime)) result["ChartTimestamp"] = chartTime;
            var chartMeaning = GetValue(row, "ChartTimestampMeaning");
            result["ChartTimestampMeaning"] = chartMeaning ??
                (GetValue(row, "Date/Time") is not null
                    ? "StrategyReport Date/Time is an order-event timestamp; chart-bar timestamp not supplied."
                    : "Chart-bar timestamp not supplied.");
            var finding = GetValue(row, "Finding");
            result["Finding"] = finding ??
                "Payload OHLCV extracted. Chart-side OHLCV and chart-bar timestamp require separate chart/template evidence.";
            output.Add(result);
        }
        return output;
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static List<List<string>> ParseRecords(string input, string delimiter)
    {
        var result = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < input.Length && input[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (delimiter.Length == 1 && c == delimiter[0]) { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\n')
            {
                row.Add(field.ToString().TrimEnd('\r')); field.Clear();
                result.Add(row); row = new List<string>();
            }
            else field.Append(c);
        }
        if (quoted) throw new FormatException("CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString().TrimEnd('\r')); result.Add(row); }
        return result;
    }
}

public sealed record StrategyReportParseResult(
    IReadOnlyList<string> Headers,
    int ParsedRowCount,
    IReadOnlyList<IReadOnlyDictionary<string, string>> PreviewRows,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    IReadOnlyList<IReadOnlyDictionary<string, string>> ParityRows,
    IReadOnlyList<string> Warnings,
    string? LikelyTimestampColumn,
    string? LikelySymbolColumn,
    string? LikelyEventColumn,
    string Interpretation);
