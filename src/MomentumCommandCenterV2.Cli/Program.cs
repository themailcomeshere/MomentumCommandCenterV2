using System.Globalization;
using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.Core.Models.Enums;
using MomentumCommandCenterV2.Infrastructure.Alpaca;
using MomentumCommandCenterV2.Infrastructure.MarketData;

if (args.Length > 0 &&
    args[0].Equals("overnight-limit-sell", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4 ||
        !decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var entryPrice))
    {
        Console.Error.WriteLine("Usage: overnight-limit-sell <SYMBOL> <ENTRY_PRICE> <overnight-high-excursions.csv> [desired-touch-percent] [minimum-net-percent] [estimated-costs-percent]");
        Console.Error.WriteLine("CSV must contain verified historical overnight high-excursion percentages, one per row, with an optional 'excursion_percent' header.");
        return 2;
    }

    var desiredTouch = args.Length > 4 &&
        double.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTouch)
            ? parsedTouch
            : OvernightLimitSellCalculator.DefaultDesiredHistoricalTouchProbabilityPercent;
    var minimumNet = args.Length > 5 &&
        double.TryParse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMinimumNet)
            ? parsedMinimumNet
            : OvernightLimitSellCalculator.DefaultMinimumNetProfitPercent;
    var estimatedCosts = args.Length > 6 &&
        double.TryParse(args[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedCosts)
            ? parsedCosts
            : OvernightLimitSellCalculator.DefaultEstimatedRoundTripCostsPercent;

    try
    {
        var rows = await File.ReadAllLinesAsync(args[3]);
        var excursions = new List<double>();
        foreach (var row in rows)
        {
            var value = row.Trim();
            if (value.Length == 0 || value.Equals("excursion_percent", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var excursion) ||
                !double.IsFinite(excursion))
            {
                Console.Error.WriteLine($"Invalid overnight excursion row: '{value}'. No recommendation calculated.");
                return 2;
            }
            excursions.Add(excursion);
        }

        var recommendation = new OvernightLimitSellCalculator().Calculate(new OvernightLimitSellInput
        {
            Symbol = args[1],
            EntryPrice = entryPrice,
            CompletedOvernightHighExcursionsPercent = excursions,
            DesiredHistoricalTouchProbabilityPercent = desiredTouch,
            MinimumNetProfitPercent = minimumNet,
            EstimatedRoundTripCostsPercent = estimatedCosts
        });

        Console.WriteLine($"Overnight limit-sell reference ({recommendation.RulesVersion})");
        Console.WriteLine($"Symbol: {recommendation.Symbol}");
        Console.WriteLine($"Historical samples: {recommendation.SampleCount}");
        Console.WriteLine($"Requested historical touch rate: {recommendation.DesiredHistoricalTouchProbabilityPercent:F1}%");
        if (!recommendation.IsCalculable || recommendation.LimitPrice is null)
        {
            Console.WriteLine("Limit price: NOT CALCULABLE");
            Console.WriteLine($"Reasons: {string.Join(", ", recommendation.ReasonCodes)}");
            return 1;
        }

        Console.WriteLine($"Limit-sell reference: {recommendation.LimitPrice.Value.ToString("0.00", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Return from entry before estimated costs: {recommendation.TargetReturnPercent:F3}%");
        Console.WriteLine($"Estimated return after configured costs: {recommendation.EstimatedNetReturnPercent:F3}%");
        Console.WriteLine($"Observed historical high-touch rate at this threshold: {recommendation.ObservedHistoricalTouchRatePercent:F1}%");
        Console.WriteLine($"Excursion percentile used: {recommendation.PercentileUsed:F1}th");
        Console.WriteLine($"Reasons: {string.Join(", ", recommendation.ReasonCodes)}");
        Console.WriteLine("This is a historical decision-support reference, not a fill-probability guarantee or personalized financial advice.");
        Console.WriteLine("Confirm actual entry price, overnight order eligibility, spread/liquidity, and cost assumptions. No order was submitted.");
        return 0;
    }
    catch (IOException exception)
    {
        Console.Error.WriteLine($"Could not read the overnight excursion file: {exception.Message}");
        return 1;
    }
}

if (args.Length > 0 &&
    args[0].Equals("alpaca-news-check", StringComparison.OrdinalIgnoreCase))
{
    var symbol = args.Length > 1 ? args[1] : "AAPL";
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(Program).Assembly, optional: true)
        .Build();

    // Environment variables override local User Secrets. Never log either value.
    var options = new AlpacaOptions
    {
        ApiKey = Environment.GetEnvironmentVariable(AlpacaOptions.ApiKeyEnvironmentVariable)
            ?? configuration["Alpaca:ApiKey"],
        ApiSecret = Environment.GetEnvironmentVariable(AlpacaOptions.ApiSecretEnvironmentVariable)
            ?? configuration["Alpaca:ApiSecret"],
        TradingBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_TRADING_BASE_URL")
            ?? "https://paper-api.alpaca.markets",
        MarketDataBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_MARKET_DATA_BASE_URL")
            ?? "https://data.alpaca.markets",
        MarketDataFeed = Environment.GetEnvironmentVariable("MCC_ALPACA_DATA_FEED")
            ?? "iex"
    };

    try
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var articles = await new AlpacaApiClient(httpClient, options).GetLatestNewsAsync(symbol, 5);
        Console.WriteLine($"Alpaca News API access: OK ({symbol.Trim().ToUpperInvariant()})");
        Console.WriteLine($"Articles returned for the last 24 hours: {articles.Count}");
        foreach (var article in articles)
        {
            Console.WriteLine($"{article.CreatedAt:O} | {article.Headline}");
            if (!string.IsNullOrWhiteSpace(article.Url))
                Console.WriteLine($"  {article.Url}");
        }
        Console.WriteLine("Read-only news check complete. No order was submitted.");
        Console.WriteLine("Important: endpoint access and returned articles do not prove complete coverage or catalyst clearance.");
        return 0;
    }
    catch (Exception exception) when (
        exception is InvalidOperationException or
        HttpRequestException or
        FormatException or
        TaskCanceledException or
        ArgumentOutOfRangeException)
    {
        Console.Error.WriteLine($"Alpaca News API check failed: {exception.Message}");
        return 1;
    }
}

if (args.Length > 0 &&
    args[0].Equals("alpaca-check", StringComparison.OrdinalIgnoreCase))
{
    var symbol = args.Length > 1 ? args[1] : "AAPL";
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(Program).Assembly, optional: true)
        .Build();

    // Environment variables override local User Secrets. Never log either value.
    var options = new AlpacaOptions
    {
        ApiKey = Environment.GetEnvironmentVariable(AlpacaOptions.ApiKeyEnvironmentVariable)
            ?? configuration["Alpaca:ApiKey"],
        ApiSecret = Environment.GetEnvironmentVariable(AlpacaOptions.ApiSecretEnvironmentVariable)
            ?? configuration["Alpaca:ApiSecret"],
        TradingBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_TRADING_BASE_URL")
            ?? "https://paper-api.alpaca.markets",
        MarketDataBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_MARKET_DATA_BASE_URL")
            ?? "https://data.alpaca.markets",
        MarketDataFeed = Environment.GetEnvironmentVariable("MCC_ALPACA_DATA_FEED")
            ?? "iex"
    };

    try
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        var alpaca = new AlpacaApiClient(httpClient, options);
        var connection = await alpaca.ValidateConnectionAsync();

        Console.WriteLine($"Alpaca connection: {(connection.IsConnected ? "OK" : "FAILED")}");
        Console.WriteLine($"Account status: {connection.AccountStatus}");
        Console.WriteLine(connection.Message);

        var bar = await alpaca.GetLatestBarAsync(symbol);
        Console.WriteLine($"Latest {bar.Symbol} bar ({options.MarketDataFeed} feed)");
        Console.WriteLine($"Timestamp (UTC): {bar.Timestamp:O}");
        Console.WriteLine($"Open: {bar.Open}  High: {bar.High}  Low: {bar.Low}  Close: {bar.Close}  Volume: {bar.Volume}");
        Console.WriteLine("Read-only test complete. No order was submitted.");
        return 0;
    }
    catch (Exception exception) when (
        exception is InvalidOperationException or
        HttpRequestException or
        FormatException or
        TaskCanceledException)
    {
        Console.Error.WriteLine($"Alpaca check failed: {exception.Message}");
        return 1;
    }
}

Console.WriteLine("Momentum Command Center V2 Prototype");
Console.WriteLine("5M permission -> 1M execution -> position lifecycle -> exit protection");
Console.WriteLine();

foreach (var state in Enum.GetValues<SignalState>())
{
    Console.WriteLine(state);
}

Console.WriteLine();
Console.WriteLine("Read-only Alpaca provider test:");
Console.WriteLine("  dotnet run --project src/MomentumCommandCenterV2.Cli -- alpaca-check [SYMBOL]");
Console.WriteLine("  dotnet run --project src/MomentumCommandCenterV2.Cli -- alpaca-news-check [SYMBOL]");
Console.WriteLine("  dotnet run --project src/MomentumCommandCenterV2.Cli -- overnight-limit-sell <SYMBOL> <ENTRY_PRICE> <overnight-high-excursions.csv> [desired-touch%] [minimum-net%] [estimated-costs%]");
Console.WriteLine("Configure Alpaca:ApiKey and Alpaca:ApiSecret with .NET User Secrets, or use the MCC_ALPACA_API_KEY and MCC_ALPACA_API_SECRET environment variables.");
return 0;
