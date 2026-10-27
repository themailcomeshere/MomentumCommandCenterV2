using System.Globalization;
using System.Text;

namespace MomentumCommandCenterV2.Infrastructure.MarketData;

// ============================================================
/// <summary>
/// One OHLCV observation decoded from a Thinkorswim StrategyReport transport row.
/// The timestamp is the embedded epoch timestamp, not the StrategyReport Date/Time column.
/// </summary>
public sealed record StrategyReportParityRow(
    string Symbol,
    string Timeframe,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

/// <summary>
/// Parses MCC_OHLCV transport payloads embedded in Thinkorswim StrategyReport exports.
/// Rows without an OHLCV payload (including transport-close rows) are ignored.
/// No missing bars are synthesized and no chart timestamp semantics are inferred.
/// </summary>
public static class StrategyReportParityParser
{
    private const string PayloadMarker = "MCC_OHLCV|";

    /// <summary>
    /// Parses a semicolon-delimited StrategyReport export into normalized OHLCV parity rows.
    /// Supports CRLF/LF files and quoted semicolon-delimited fields with escaped quotes.
    /// </summary>
    public static IReadOnlyList<StrategyReportParityRow> Parse(string reportText)
    {
        ArgumentNullException.ThrowIfNull(reportText);

        var rows = new List<StrategyReportParityRow>();
        var lines = reportText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var tableStarted = false;

        foreach (var line in lines)
        {
            if (!tableStarted)
            {
                if (line.StartsWith("Id;Strategy;", StringComparison.OrdinalIgnoreCase))
                    tableStarted = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseSemicolonRow(line);
            if (fields.Count < 2)
                continue;

            var strategy = fields[1];
            var markerIndex = strategy.IndexOf(PayloadMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
                continue;

            var payloadStart = markerIndex + PayloadMarker.Length;
            var payloadEnd = strategy.IndexOf(')', payloadStart);
            if (payloadEnd < 0)
                throw new FormatException("StrategyReport contains an unterminated MCC_OHLCV payload.");

            var payload = strategy[payloadStart..payloadEnd];
            var parts = payload.Split('|');
            if (parts.Length != 8)
                throw new FormatException(
                    $"MCC_OHLCV payload must contain 8 fields after its marker; found {parts.Length}.");

            var version = parts[0];
            if (!string.Equals(version, "v1", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"Unsupported MCC_OHLCV payload version '{version}'.");

            var symbol = parts[1].Trim().ToUpperInvariant();
            var timeframe = parts[2].Trim();
            if (symbol.Length == 0 || timeframe.Length == 0)
                throw new FormatException("MCC_OHLCV payload requires both symbol and timeframe.");

            if (!long.TryParse(NormalizeInteger(parts[3]), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var epochMilliseconds))
                throw new FormatException("MCC_OHLCV payload contains an invalid epoch-millisecond timestamp.");

            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds);
            var open = ParseDecimal(parts[4], "open");
            var high = ParseDecimal(parts[5], "high");
            var low = ParseDecimal(parts[6], "low");
            var close = ParseDecimal(parts[7], "close");
            var volume = ParseDecimal(parts[8], "volume");
            if (volume < 0 || decimal.Truncate(volume) != volume || volume > long.MaxValue)
                throw new FormatException("MCC_OHLCV payload contains an invalid volume.");

            if (high < low || high < open || high < close || low > open || low > close)
                throw new FormatException("MCC_OHLCV payload violates OHLC high/low bounds.");

            rows.Add(new StrategyReportParityRow(
                symbol,
                timeframe,
                timestamp,
                open,
                high,
                low,
                close,
                decimal.ToInt64(volume)));
        }

        if (!tableStarted)
            throw new FormatException("StrategyReport table header 'Id;Strategy;' was not found.");

        return rows
            .OrderBy(row => row.Timestamp)
            .ToArray();
    }

    /// <summary>
    /// Writes normalized parity data as a deterministic semicolon-delimited CSV.
    /// This is a stable interchange format; apply the user's canonical worksheet layout
    /// at the presentation boundary once that exact template is available in the repository.
    /// </summary>
    public static string ToParityCsv(IEnumerable<StrategyReportParityRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var builder = new StringBuilder();
        builder.AppendLine("Symbol;Timeframe;TimestampUtc;Open;High;Low;Close;Volume");

        foreach (var row in rows.OrderBy(row => row.Timestamp))
        {
            builder.Append(row.Symbol).Append(';')
                .Append(row.Timeframe).Append(';')
                .Append(row.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append(';')
                .Append(row.Open.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(row.High.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(row.Low.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(row.Close.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(row.Volume.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> ParseSemicolonRow(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ';' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        if (inQuotes)
            throw new FormatException("StrategyReport contains an unterminated quoted field.");

        fields.Add(field.ToString());
        return fields;
    }

    private static decimal ParseDecimal(string value, string fieldName)
    {
        var normalized = NormalizeInteger(value);
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"MCC_OHLCV payload contains invalid {fieldName} value.");
        return result;
    }

    private static string NormalizeInteger(string value) => value.Replace(",", string.Empty, StringComparison.Ordinal);
}
