using System.Globalization;
using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.Infrastructure.MarketData;

/// <summary>
/// ADAPTER: ThinkorswimOhlcvStrategyReportReader
/// PURPOSE: Normalizes the MCC-owned strategy-report payload into completed MarketBar records.
/// WHEN CALLED: Used after exporting the MCC_OHLCV_V1 strategy report from Thinkorswim.
/// DOES NOT: Infer volume, repair timestamps, fill gaps, or accept third-party strategy encodings.
/// </summary>
public sealed class ThinkorswimOhlcvStrategyReportReader
{
    private const string PayloadPrefix = "MCC_OHLCV|v1|";

    /// <summary>
    /// METHOD: Read
    /// PURPOSE: Reads MCC payloads from a Thinkorswim semicolon-delimited strategy report.
    /// WHEN CALLED: Invoked by the normalization workflow before replay.
    /// VALIDATION: Requires an explicit timestamp, symbol, aggregation, OHLC, and actual nonnegative volume.
    /// </summary>
    public IReadOnlyList<MarketBar> Read(
        TextReader reader,
        string? expectedSymbol = null,
        BarTimeframe? expectedTimeframe = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var bars = new List<MarketBar>();
        string? reportSymbol = null;
        var lineNumber = 0;
        var sawDataRow = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Strategy report", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Work Time:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Max trade", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Total ", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("Symbol:", StringComparison.OrdinalIgnoreCase))
            {
                reportSymbol = line["Symbol:".Length..].Trim().ToUpperInvariant();
                continue;
            }

            var fields = SplitSemicolonRow(line);
            if (fields.Length == 0 || !string.Equals(fields[0].Trim(), "Id", StringComparison.OrdinalIgnoreCase) && !int.TryParse(fields[0].Trim(), out _))
                continue;
            if (string.Equals(fields[0].Trim(), "Id", StringComparison.OrdinalIgnoreCase))
                continue;

            sawDataRow = true;
            if (fields.Length < 2)
                throw InvalidRow(lineNumber, "A strategy-report data row has no strategy payload.");

            var strategy = fields[1].Trim();
            if (strategy.StartsWith("MCC_OHLCV_V1(", StringComparison.Ordinal) && strategy.EndsWith(")", StringComparison.Ordinal))
                strategy = strategy["MCC_OHLCV_V1(".Length..^1];
            if (!strategy.StartsWith(PayloadPrefix, StringComparison.Ordinal))
                continue;

            bars.Add(ParsePayload(strategy, lineNumber));
        }

        if (!sawDataRow)
            throw new FormatException("Thinkorswim strategy report contains no data rows.");
        if (bars.Count == 0)
            throw new FormatException("Thinkorswim strategy report contains no complete MCC OHLCV payloads.");

        var symbol = expectedSymbol?.ToUpperInvariant();
        var timeframe = expectedTimeframe;
        if (reportSymbol is not null && bars.Any(bar => !string.Equals(bar.Symbol, reportSymbol, StringComparison.OrdinalIgnoreCase)))
            throw new FormatException($"Strategy payload symbol does not match the report header symbol {reportSymbol}.");
        if (symbol is not null && bars.Any(bar => !string.Equals(bar.Symbol, symbol, StringComparison.OrdinalIgnoreCase)))
            throw new FormatException($"Strategy report contains a symbol other than {symbol}.");
        if (timeframe is not null && bars.Any(bar => bar.Timeframe != timeframe.Value))
            throw new FormatException($"Strategy report contains a timeframe other than {timeframe.Value}.");

        try
        {
            _ = new MomentumCommandCenterV2.Core.MarketReplayEngine().Replay(bars);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException($"MCC strategy report failed OHLCV or duplicate-key validation: {exception.Message}", exception);
        }

        return bars.OrderBy(bar => bar.Timestamp).ToArray();
    }

    /// <summary>
    /// METHOD: ParsePayload
    /// PURPOSE: Converts the documented MCC_OHLCV v1 pipe payload into one completed bar.
    /// WHEN CALLED: Called for each owned strategy row after report framing is removed.
    /// FORMAT: MCC_OHLCV|v1|symbol|timeframe|timestamp|open|high|low|close|volume.
    /// </summary>
    private static MarketBar ParsePayload(string payload, int lineNumber)
    {
        var fields = payload.Split('|');
        if (fields.Length != 10)
            throw InvalidRow(lineNumber, "MCC_OHLCV v1 requires symbol, timeframe, timestamp, open, high, low, close, and volume.");

        var symbol = fields[2].Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(symbol))
            throw InvalidRow(lineNumber, "Symbol is required.");

        var timeframe = fields[3].Trim() switch
        {
            "1m" => BarTimeframe.OneMinute,
            "5m" => BarTimeframe.FiveMinute,
            _ => throw InvalidRow(lineNumber, "Timeframe must be 1m or 5m.")
        };

        DateTimeOffset timestamp;
        if (long.TryParse(fields[4], NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var epochMilliseconds))
        {
            if (epochMilliseconds <= 0)
                throw InvalidRow(lineNumber, "Timestamp must be positive.");
            try
            {
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw InvalidRow(lineNumber, $"Timestamp is outside the supported epoch range: {exception.Message}");
            }
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(fields[4], @"(?:Z|[+-]\d{2}:\d{2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
                 DateTimeOffset.TryParse(fields[4], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTimestamp) && parsedTimestamp > DateTimeOffset.UnixEpoch)
        {
            timestamp = parsedTimestamp;
        }
        else
        {
            throw InvalidRow(lineNumber, "Timestamp must be a positive Unix epoch millisecond value or ISO-8601 value with an explicit offset.");
        }

        decimal Number(string name, string value)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                throw InvalidRow(lineNumber, $"{name} is not an invariant decimal.");
            return number;
        }

        var open = Number("Open", fields[5]);
        var high = Number("High", fields[6]);
        var low = Number("Low", fields[7]);
        var close = Number("Close", fields[8]);
        var volume = Number("Volume", fields[9]);
        if (volume < 0m)
            throw InvalidRow(lineNumber, "Volume cannot be negative.");
        if (high < low || high < open || high < close || low > open || low > close)
            throw InvalidRow(lineNumber, "OHLC invariants are invalid.");

        return new MarketBar(symbol, timeframe, timestamp, open, high, low, close, volume);
    }

    /// <summary>
    /// METHOD: SplitSemicolonRow
    /// PURPOSE: Splits the fixed Thinkorswim report row format while respecting quoted fields.
    /// WHEN CALLED: Used for report headers and order rows.
    /// LIMITATION: Embedded newlines are not supported by the exported report format.
    /// </summary>
    private static string[] SplitSemicolonRow(string line)
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
            else if (character == ';' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else field.Append(character);
        }
        if (quoted) throw new FormatException("Strategy report row contains an unclosed quoted field.");
        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private static FormatException InvalidRow(int lineNumber, string message) =>
        new($"Invalid MCC OHLCV strategy report at line {lineNumber}: {message}");
}
