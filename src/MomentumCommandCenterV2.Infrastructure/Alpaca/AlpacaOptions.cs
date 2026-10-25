// ============================================================
// CLASS: AlpacaOptions
// PURPOSE:
//   Holds non-secret Alpaca endpoints and reads API credentials
//   from the local process environment.
// SECURITY:
//   Never store API keys or secrets in appsettings.json or source control.
// ============================================================
namespace MomentumCommandCenterV2.Infrastructure.Alpaca;

/// <summary>
/// Local configuration for read-only Alpaca connectivity checks.
/// </summary>
public sealed class AlpacaOptions
{
    public const string ApiKeyEnvironmentVariable = "MCC_ALPACA_API_KEY";
    public const string ApiSecretEnvironmentVariable = "MCC_ALPACA_API_SECRET";

    public string TradingBaseUrl { get; init; } = "https://paper-api.alpaca.markets";
    public string MarketDataBaseUrl { get; init; } = "https://data.alpaca.markets";
    public string MarketDataFeed { get; init; } = "iex";
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }

    /// <summary>
    /// Loads credentials from environment variables while retaining safe defaults.
    /// </summary>
    public static AlpacaOptions FromEnvironment()
    {
        return new AlpacaOptions
        {
            ApiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable),
            ApiSecret = Environment.GetEnvironmentVariable(ApiSecretEnvironmentVariable),
            TradingBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_TRADING_BASE_URL")
                ?? "https://paper-api.alpaca.markets",
            MarketDataBaseUrl = Environment.GetEnvironmentVariable("MCC_ALPACA_MARKET_DATA_BASE_URL")
                ?? "https://data.alpaca.markets",
            MarketDataFeed = Environment.GetEnvironmentVariable("MCC_ALPACA_DATA_FEED")
                ?? "iex"
        };
    }

    /// <summary>
    /// Validates required credentials and endpoint configuration before a request is sent.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException(
                $"Missing Alpaca API key. Set the {ApiKeyEnvironmentVariable} environment variable.");

        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new InvalidOperationException(
                $"Missing Alpaca API secret. Set the {ApiSecretEnvironmentVariable} environment variable.");

        if (!Uri.TryCreate(TradingBaseUrl, UriKind.Absolute, out var tradingUri) ||
            tradingUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Alpaca trading base URL must be an absolute HTTPS URL.");

        if (!Uri.TryCreate(MarketDataBaseUrl, UriKind.Absolute, out var dataUri) ||
            dataUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Alpaca market-data base URL must be an absolute HTTPS URL.");

        if (string.IsNullOrWhiteSpace(MarketDataFeed))
            throw new InvalidOperationException("Alpaca market-data feed cannot be empty.");
    }
}
