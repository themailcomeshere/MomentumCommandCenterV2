using System.Globalization;
using System.Text;

namespace MomentumCommandCenterV2.API.StrategyReports;

/// <summary>
/// Parses the MCC_OHLCV_ATR v2 payload carried in Thinkorswim Strategy Report order names.
/// Does not infer chart timestamps from the Strategy Report Date/Time order-event column.
/// </summary>
public static class StrategyReportAtrV2Parser
{
    private const string Marker = "MCC_OHLCV_ATR|v2|";

    public static StrategyReportAtrImportResult Parse(string csv, int maxRows = 10000)
    {
        if (string.IsNullOrWhiteSpace(csv)) throw new ArgumentException("CSV file is empty.", nameof(csv));
        if (maxRows < 1) throw new ArgumentOutOfRangeException(nameof(maxRows));

        var records = ParseRecords(csv);
        var symbolHeader = csv.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n').Select(x => x.Trim().TrimStart('\uFEFF'))
            .FirstOrDefault(x => x.StartsWith("Symbol:", StringComparison.OrdinalIgnoreCase));
        var reportSymbol = symbolHeader?["Symbol:".Length..].Trim().ToUpperInvariant();
        var observations = new List<StrategyReportAtrObservation>();
        var dataRows = 0;
        foreach (var record in records)
        {
            if (record.Count < 2 || !int.TryParse(record[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                continue;
            dataRows++;
            if (dataRows > maxRows) throw new FormatException($"Strategy Report exceeds the {maxRows} data-row limit.");
            var strategy = record[1].Trim();
            var markerIndex = strategy.IndexOf(Marker, StringComparison.Ordinal);
            if (markerIndex < 0) continue;

            var end = strategy.IndexOf(')', markerIndex);
            var payload = end >= 0 ? strategy[markerIndex..end] : strategy[markerIndex..];
            var fields = payload.Split('|');
            if (fields.Length != 11 || fields[0] != "MCC_OHLCV_ATR" || fields[1] != "v2")
                throw new FormatException($"Strategy Report row {record[0]} contains a malformed MCC_OHLCV_ATR v2 payload; expected exactly 11 pipe-delimited fields.");

            var symbol = fields[2].Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 8 ||
                symbol.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '-'))
                throw new FormatException($"Strategy Report row {record[0]} has an invalid symbol.");
            if (reportSymbol is not null && !string.Equals(reportSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"Payload symbol {symbol} does not match Strategy Report header symbol {reportSymbol}.");

            var timeframe = fields[3].Trim().ToLowerInvariant();
            if (timeframe is not ("1m" or "5m"))
                throw new FormatException($"Strategy Report row {record[0]} has unsupported timeframe '{timeframe}'. Only 1m and 5m are accepted.");

            var epochText = fields[4].Replace(",", "", StringComparison.Ordinal).Trim();
            if (!long.TryParse(epochText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) || epoch <= 0)
                throw new FormatException($"Strategy Report row {record[0]} has an invalid Unix-millisecond timestamp.");
            DateTimeOffset timestamp;
            try { timestamp = DateTimeOffset.FromUnixTimeMilliseconds(epoch); }
            catch (ArgumentOutOfRangeException ex) { throw new FormatException($"Strategy Report row {record[0]} timestamp is outside the supported range.", ex); }

            decimal Number(string name, string value)
            {
                if (!decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    throw new FormatException($"Strategy Report row {record[0]} has an invalid {name} decimal.");
                return parsed;
            }

            var open = Number("Open", fields[5]);
            var high = Number("High", fields[6]);
            var low = Number("Low", fields[7]);
            var close = Number("Close", fields[8]);
            var volume = Number("Volume", fields[9].Replace(",", "", StringComparison.Ordinal));
            var atr = Number("ATR", fields[10]);
            if (open <= 0 || high <= 0 || low <= 0 || close <= 0 || high < Math.Max(open, close) ||
                low > Math.Min(open, close) || low > high || volume < 0 || atr < 0)
                throw new FormatException($"Strategy Report row {record[0]} contains invalid OHLCV/ATR values.");

            observations.Add(new StrategyReportAtrObservation(
                record[0].Trim(), symbol, timeframe, timestamp, open, high, low, close, volume, atr));
        }

        if (dataRows == 0) throw new FormatException("Strategy Report contains no numbered data rows.");
        if (observations.Count == 0) throw new FormatException("Strategy Report contains no MCC_OHLCV_ATR v2 payload rows.");
        if (observations.Select(x => (x.Symbol, x.Timeframe)).Distinct().Count() != 1)
            throw new FormatException("One Strategy Report import must contain exactly one symbol and one timeframe.");
        var ordered = observations.OrderBy(x => x.SourceTimestampUtc).ToArray();
        var duplicateTimestamps = ordered.GroupBy(x => x.SourceTimestampUtc).Where(g => g.Count() > 1)
            .Select(g => g.Key).ToArray();
        if (duplicateTimestamps.Length > 0)
            throw new FormatException($"Strategy Report contains {duplicateTimestamps.Length} duplicate source timestamp(s).");

        var interval = ordered[0].Timeframe == "1m" ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5);
        var gaps = ordered.Zip(ordered.Skip(1), (left, right) => new { Expected = left.SourceTimestampUtc + interval, Actual = right.SourceTimestampUtc })
            .Where(x => x.Actual - x.Expected > TimeSpan.FromSeconds(1))
            .Select(x => new StrategyReportAtrGap(x.Expected, x.Actual, x.Actual - x.Expected))
            .ToArray();

        return new StrategyReportAtrImportResult(
            ordered[0].Symbol, ordered[0].Timeframe, dataRows, ordered.Length,
            ordered[0].SourceTimestampUtc, ordered[^1].SourceTimestampUtc,
            gaps.Length, gaps.Take(100).ToArray(), ordered,
            "V2 ATR observations parsed from explicit completed-bar payloads. Gaps are reported, not imputed; the Strategy Report Date/Time order-event timestamp is not used as a bar timestamp.");
    }

    private static List<List<string>> ParseRecords(string csv)
    {
        var input = csv.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var records = new List<List<string>>();
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
            else if (c == ';') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\n') { row.Add(field.ToString()); field.Clear(); records.Add(row); row = new List<string>(); }
            else field.Append(c);
        }
        if (quoted) throw new FormatException("Strategy Report contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row); }
        return records;
    }
}

public sealed record StrategyReportAtrObservation(
    string SourceRow,
    string Symbol,
    string Timeframe,
    DateTimeOffset SourceTimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    decimal Atr);

public sealed record StrategyReportAtrGap(
    DateTimeOffset ExpectedTimestampUtc,
    DateTimeOffset ActualTimestampUtc,
    TimeSpan Duration);

public sealed record StrategyReportAtrImportResult(
    string Symbol,
    string Timeframe,
    int ReportDataRowCount,
    int AtrObservationCount,
    DateTimeOffset FirstTimestampUtc,
    DateTimeOffset LastTimestampUtc,
    int GapCount,
    IReadOnlyList<StrategyReportAtrGap> GapSamples,
    IReadOnlyList<StrategyReportAtrObservation> Observations,
    string Interpretation);
