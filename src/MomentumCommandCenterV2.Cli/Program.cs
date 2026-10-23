using MomentumCommandCenterV2.Core.Models.Enums;
using MomentumCommandCenterV2.Infrastructure.Alpaca;

if (args.Length > 0 &&
    args[0].Equals("alpaca-check", StringComparison.OrdinalIgnoreCase))
{
    var symbol = args.Length > 1 ? args[1] : "AAPL";
    var options = AlpacaOptions.FromEnvironment();

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
Console.WriteLine("Configure MCC_ALPACA_API_KEY and MCC_ALPACA_API_SECRET locally before running.");
return 0;
