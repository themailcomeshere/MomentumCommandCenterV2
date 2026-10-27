using System.Text;

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
        return new StrategyReportParseResult(headers, rows.Count, rows.Take(100).ToArray(), warnings,
            likelyTime, likelySymbol, likelyEvent,
            "Parsed CSV fields as exported; this upload does not reconstruct missing OHLCV bars or infer trades not represented in the report.");
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
    IReadOnlyList<string> Warnings,
    string? LikelyTimestampColumn,
    string? LikelySymbolColumn,
    string? LikelyEventColumn,
    string Interpretation);
