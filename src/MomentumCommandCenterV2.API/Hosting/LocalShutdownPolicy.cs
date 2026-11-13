using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MomentumCommandCenterV2.API.Hosting;

/// <summary>
/// PURPOSE: Authorizes the interactive launcher's graceful-stop request.
/// WHEN CALLED: POST /api/admin/shutdown.
/// IMPORTANT: Disabled unless the launcher started the API with MCC_SHUTDOWN_TOKEN. The request must
/// come from a loopback address and carry the same token (constant-time comparison). There is no
/// remote or unauthenticated shutdown path.
/// </summary>
public static class LocalShutdownPolicy
{
    public const string TokenEnvironmentVariable = "MCC_SHUTDOWN_TOKEN";
    public const string TokenHeader = "X-MCC-Shutdown-Token";
    public const int MinimumTokenLength = 32;

    public static LocalShutdownDecision Evaluate(string? configuredToken, string? suppliedToken, IPAddress? remoteAddress)
    {
        if (string.IsNullOrWhiteSpace(configuredToken) || configuredToken.Length < MinimumTokenLength)
            return LocalShutdownDecision.Disabled;
        if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
            return LocalShutdownDecision.Forbidden;
        if (string.IsNullOrEmpty(suppliedToken) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configuredToken), Encoding.UTF8.GetBytes(suppliedToken)))
            return LocalShutdownDecision.Forbidden;
        return LocalShutdownDecision.Allowed;
    }
}

public enum LocalShutdownDecision
{
    Disabled,
    Forbidden,
    Allowed
}
