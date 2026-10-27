using System.Text;
using System.Globalization;

namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Parses a Thinkorswim StrategyReport CSV export without assuming it is OHLCV.
/// Called by the upload endpoint to preserve report columns and produce readable rows.
/// </summary>
public static class StrategyReportCsvParser
{
    public static StrategyReportParseResult Parse(string csv, int maxRows = 10000)
    {
        if (string.IsNullOrWhiteSpace(csv))
            throw new ArgumentException("CSV file is empty.", nameof(csv));

        var records = ParseRecords(csv);
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
                warnings.Add($"Skipped row {rows.Count + 2}: found {record.Count} fields, expected {headers.Length}.");
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
        var likelyTime = headers.FirstOrDefault(h => h.Contains("time", StringComparison.OrdinalIgnoreCase) || h.Contains("date", StringComparison.OrdinalIgnoreCase));
        var likelySymbol = headers.FirstOrDefault(h => h.Contains("symbol", StringComparison.OrdinalIgnoreCase));
        var likelyEvent = headers.FirstOrDefault(h => h.Contains("signal", StringComparison.OrdinalIgnoreCase) || h.Contains("strategy", StringComparison.OrdinalIgnoreCase) || h.Contains("order", StringComparison.OrdinalIgnoreCase) || h.Contains("event", StringComparison.OrdinalIgnoreCase));
        var parityRows = BuildParityRows(rows, warnings);
        return new StrategyReportParseResult(headers, rows.Count, rows.Take(100).ToArray(), rows, parityRows, warnings,
            likelyTime, likelySymbol, likelyEvent,
            "Parsed CSV fields as exported; this upload does not reconstruct missing OHLCV bars or infer trades not represented in the report.");
    }

    private static readonly string[] ParityHeaders =
    {
        "SourceRow", "Symbol", "Timeframe", "PayloadTimestampUtc", "PayloadOpen", "PayloadHigh",
        "PayloadLow", "PayloadClose", "PayloadVolume", "ChartTimestamp", "ChartOpen", "ChartHigh",
        "ChartLow", "ChartClose", "ChartVolume", "ChartTimestampMeaning", "Finding"
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildParityRows(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows, List<string> warnings)
    {
        var output = new List<IReadOnlyDictionary<string, string>>();
        foreach (var row in rows)
        {
            var payloadField = row.FirstOrDefault(pair => pair.Value.Contains("MCC_OHLCV|v1|", StringComparison.Ordinal));
            if (string.IsNullOrEmpty(payloadField.Value)) continue;
            var text = payloadField.Value;
            var start = text.IndexOf("MCC_OHLCV|v1|", StringComparison.Ordinal);
            var end = text.IndexOf(')', start);
            if (end >= 0) text = text[start..end];
            else text = text[start..];
            var parts = text.Split('|');
            if (parts.Length != 10 || parts[0] != "MCC_OHLCV" || parts[1] != "v1")
            {
                warnings.Add("A row contained an MCC OHLCV marker but did not match the v1 parity payload format.");
                continue;
            }
            if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch))
            {
                warnings.Add("A parity payload had an invalid Unix-millisecond timestamp.");
                continue;
            }
            var result = ParityHeaders.ToDictionary(h => h, _ => "", StringComparer.OrdinalIgnoreCase);
            result["SourceRow"] = (output.Count + 1).ToString(CultureInfo.InvariantCulture);
            result["Symbol"] = parts[2].Trim().ToUpperInvariant();
            result["Timeframe"] = parts[3].Trim();
            result["PayloadTimestampUtc"] = DateTimeOffset.FromUnixTimeMilliseconds(epoch).UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            result["PayloadOpen"] = parts[5].Trim();
            result["PayloadHigh"] = parts[6].Trim();
            result["PayloadLow"] = parts[7].Trim();
            result["PayloadClose"] = parts[8].Trim();
            result["PayloadVolume"] = parts[9].Trim();
            output.Add(result);
        }
        return output;
    }

    private static List<List<string>> ParseRecords(string input)
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
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
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
