using System.Globalization;
using MomentumCommandCenterV2.Application.Interfaces;
using MomentumCommandCenterV2.Application.Models;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Infrastructure
{

    /// <summary>
    /// Imports Schwab Order History CSV exports and converts filled BUY/SELL
    /// orders into the V2 Trade model.
    ///
    /// The current Schwab export supplied for this project does not contain
    /// quantity, so each filled order is treated as one unit.
    /// </summary>
    public sealed class SchwabTradeImporter : ITradeImporter
    {
        private const string SymbolHeader = "Symbol";
        private const string StatusHeader = "Status";
        private const string ActionHeader = "Action";
        private const string PriceHeader = "Price";
        private const string TimingHeader = "Timing";
        private const string FillPriceHeader = "Fill Price";
        private const string TimestampHeader = "Time and Date(ET)";

        private static readonly string[] RequiredHeaders =
        {
        SymbolHeader,
        StatusHeader,
        ActionHeader,
        PriceHeader,
        TimingHeader,
        FillPriceHeader,
        TimestampHeader
    };

        /// <summary>
        /// Imports Schwab CSV text.
        /// </summary>
        public SchwabImportResult Import(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                throw new ArgumentException(
                    "Schwab CSV content cannot be empty.",
                    nameof(csv));
            }

            var rows = ParseRows(csv);

            if (rows.Count == 0)
            {
                throw new FormatException(
                    "Schwab CSV contains no data rows.");
            }

            var filledOrders = rows
                .Where(IsFilledTradeOrder)
                .Select((row, index) => ParseFilledOrder(row, index + 2))
                .OrderBy(order => order.Timestamp)
                .ThenBy(order => order.SourceRowNumber)
                .ToList();

            var completedTrades = new List<Trade>();
            var openPositions = new List<OpenPosition>();
            var unmatchedOrders = new List<SchwabOrder>();

            foreach (var symbolGroup in filledOrders
                         .GroupBy(
                             order => order.Symbol,
                             StringComparer.OrdinalIgnoreCase))
            {
                // FIFO queue of currently open BUY lots.
                var openLots = new Queue<OpenLot>();

                foreach (var order in symbolGroup)
                {
                    if (order.Action.Equals(
                            "Buy",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        openLots.Enqueue(
                            new OpenLot(
                                order.Timestamp,
                                order.FillPrice,
                                order.Quantity));

                        continue;
                    }

                    if (!order.Action.Equals(
                            "Sell",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var remainingQuantity = order.Quantity;

                    while (remainingQuantity > 0 && openLots.Count > 0)
                    {
                        var lot = openLots.Dequeue();

                        var matchedQuantity =
                            Math.Min(
                                remainingQuantity,
                                lot.Quantity);

                        completedTrades.Add(
                            new Trade(
                                order.Symbol,
                                lot.EntryTime,
                                lot.EntryPrice,
                                order.Timestamp,
                                order.FillPrice,
                                matchedQuantity,
                                ExitReason.Unknown));

                        remainingQuantity -= matchedQuantity;

                        if (lot.Quantity > matchedQuantity)
                        {
                            openLots.Enqueue(
                                lot with
                                {
                                    Quantity = lot.Quantity - matchedQuantity
                                });
                        }
                    }

                    // A SELL with no corresponding BUY is retained for review
                    // rather than silently discarded.
                    if (remainingQuantity > 0)
                    {
                        unmatchedOrders.Add(
                            order with
                            {
                                Quantity = remainingQuantity
                            });
                    }
                }

                // Anything remaining in the BUY queue is an open position.
                while (openLots.Count > 0)
                {
                    var lot = openLots.Dequeue();

                    openPositions.Add(
                        new OpenPosition(
                            symbolGroup.Key,
                            lot.EntryTime,
                            lot.EntryPrice,
                            lot.Quantity));
                }
            }

            return new SchwabImportResult(
                completedTrades
                    .OrderBy(trade => trade.EntryTime)
                    .ToList(),

                openPositions
                    .OrderBy(position => position.EntryTime)
                    .ToList(),

                unmatchedOrders
                    .OrderBy(order => order.Timestamp)
                    .ToList());
        }

        /// <summary>
        /// Imports a Schwab CSV file directly from disk.
        /// </summary>
        public SchwabImportResult ImportFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "CSV file path cannot be empty.",
                    nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Schwab CSV file was not found.",
                    filePath);
            }

            return Import(File.ReadAllText(filePath));
        }

        private static bool IsFilledTradeOrder(
            Dictionary<string, string> row)
        {
            var status = GetRequired(row, StatusHeader);
            var action = GetRequired(row, ActionHeader);
            var fillPrice = GetRequired(row, FillPriceHeader);

            return
                status.Equals(
                    "Filled",
                    StringComparison.OrdinalIgnoreCase)
                &&
                (
                    action.Equals(
                        "Buy",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    action.Equals(
                        "Sell",
                        StringComparison.OrdinalIgnoreCase)
                )
                &&
                !string.IsNullOrWhiteSpace(fillPrice)
                &&
                !fillPrice.Equals(
                    "-",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static SchwabOrder ParseFilledOrder(
            Dictionary<string, string> row,
            int sourceRowNumber)
        {
            var symbol =
                GetRequired(row, SymbolHeader).Trim();

            var action =
                GetRequired(row, ActionHeader).Trim();

            var fillPriceText =
                GetRequired(row, FillPriceHeader).Trim();

            var timestampText =
                GetRequired(row, TimestampHeader).Trim();

            if (!decimal.TryParse(
                    CleanMoney(fillPriceText),
                    NumberStyles.Number |
                    NumberStyles.AllowCurrencySymbol,
                    CultureInfo.InvariantCulture,
                    out var fillPrice))
            {
                throw new FormatException(
                    $"Invalid Schwab fill price '{fillPriceText}' " +
                    $"on CSV row {sourceRowNumber}.");
            }

            var timestamp =
                ParseEasternTimestamp(timestampText);

            // The current Schwab export does not contain quantity.
            const int quantity = 1;

            return new SchwabOrder(
                symbol,
                action,
                fillPrice,
                timestamp,
                quantity,
                sourceRowNumber);
        }

        private static DateTimeOffset ParseEasternTimestamp(
            string value)
        {
            if (!DateTime.TryParseExact(
                    value,
                    "h:mm tt MM/dd/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var localEasternTime))
            {
                throw new FormatException(
                    $"Invalid Schwab ET timestamp '{value}'. " +
                    "Expected format: 'h:mm tt MM/dd/yyyy'.");
            }

            var easternTimeZone =
                GetEasternTimeZone();

            var offset =
                easternTimeZone.GetUtcOffset(localEasternTime);

            return new DateTimeOffset(
                localEasternTime,
                offset);
        }

        private static TimeZoneInfo GetEasternTimeZone()
        {
            // Windows
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Linux/macOS
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/New_York");
            }
        }

        private static string CleanMoney(string value)
        {
            var cleaned = value.Trim();

            if (cleaned.StartsWith("$", StringComparison.Ordinal))
            {
                cleaned = cleaned[1..];
            }

            return cleaned.Replace(
                ",",
                string.Empty,
                StringComparison.Ordinal);
        }

        private static string GetRequired(
            Dictionary<string, string> row,
            string header)
        {
            if (!row.TryGetValue(header, out var value))
            {
                throw new FormatException(
                    $"Schwab CSV is missing required column '{header}'.");
            }

            return value;
        }

        private static List<Dictionary<string, string>> ParseRows(
            string csv)
        {
            using var reader =
                new StringReader(csv);

            var headerLine =
                reader.ReadLine();

            if (headerLine is null)
            {
                return [];
            }

            var headers =
                ParseCsvLine(headerLine)
                    .Select(header => header.Trim())
                    .ToArray();

            foreach (var requiredHeader in RequiredHeaders)
            {
                if (!headers.Contains(
                        requiredHeader,
                        StringComparer.OrdinalIgnoreCase))
                {
                    throw new FormatException(
                        $"Schwab CSV is missing required column " +
                        $"'{requiredHeader}'.");
                }
            }

            var rows =
                new List<Dictionary<string, string>>();

            var sourceRowNumber = 1;

            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                sourceRowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values =
                    ParseCsvLine(line);

                if (values.Count != headers.Length)
                {
                    throw new FormatException(
                        $"Schwab CSV row {sourceRowNumber} " +
                        $"has {values.Count} fields; " +
                        $"expected {headers.Length}.");
                }

                var row =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < headers.Length; i++)
                {
                    row[headers[i]] =
                        values[i].Trim();
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// Small CSV parser supporting quoted fields and escaped quotes.
        /// This avoids adding a third-party CSV dependency to the V2 core.
        /// </summary>
        private static List<string> ParseCsvLine(
            string line)
        {
            var values =
                new List<string>();

            var current =
                new List<char>();

            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];

                if (character == '"')
                {
                    // CSV escaped quote: ""
                    if (
                        inQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        current.Add('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (
                    character == ',' &&
                    !inQuotes)
                {
                    values.Add(
                        new string(current.ToArray()));

                    current.Clear();

                    continue;
                }

                current.Add(character);
            }

            if (inQuotes)
            {
                throw new FormatException(
                    "Schwab CSV contains an unterminated " +
                    "quoted field.");
            }

            values.Add(
                new string(current.ToArray()));

            return values;
        }

        TradeImportResult ITradeImporter.ImportFile(string filePath)
        {
            throw new NotImplementedException();
        }

        private sealed record OpenLot(
            DateTimeOffset EntryTime,
            decimal EntryPrice,
            int Quantity);
    }

    /// <summary>
    /// Result of importing a Schwab order-history export.
    /// </summary>
    public sealed record SchwabImportResult(
        IReadOnlyList<Trade> CompletedTrades,
        IReadOnlyList<OpenPosition> OpenPositions,
        IReadOnlyList<SchwabOrder> UnmatchedOrders);

}
