using Microsoft.Extensions.Configuration;
using MomentumCommandCenterV2.Core.Models.Enums;
using MomentumCommandCenterV2.Infrastructure.Alpaca;

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
Console.WriteLine("Configure Alpaca:ApiKey and Alpaca:ApiSecret with .NET User Secrets, or use the MCC_ALPACA_API_KEY and MCC_ALPACA_API_SECRET environment variables.");
return 0;
