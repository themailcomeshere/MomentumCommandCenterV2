namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Classifies exact session-boundary coverage without treating a future endpoint as a provider omission.
/// </summary>
public static class BoringOvernightBoundaryAuditPolicy
{
    public static string GetStatus(
        bool providerReturnedExactBar,
        bool storeHasExactBar,
        DateTimeOffset endpointAvailableAtUtc,
        DateTimeOffset auditedAtUtc)
    {
        if (auditedAtUtc < endpointAvailableAtUtc)
            return "PENDING_NOT_YET_AVAILABLE";

        if (providerReturnedExactBar)
            return storeHasExactBar ? "COVERED" : "PROVIDER_BAR_NOT_FOUND_IN_STORE";

        return storeHasExactBar
            ? "STORE_ONLY_NO_PROVIDER_REFRESH_BAR"
            : "PROVIDER_RESPONSE_MISSING_EXACT_BAR";
    }
}
