namespace MomentumCommandCenterV2.API.BoringOvernight;

public sealed record BoringOvernightCaptureHistoryRequest(
    string Symbol,
    string ProviderName = "Alpaca",
    int RequiredWindow = 20);

