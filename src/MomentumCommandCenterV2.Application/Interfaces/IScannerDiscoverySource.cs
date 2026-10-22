using MomentumCommandCenterV2.Application.Models;

namespace MomentumCommandCenterV2.Application.Interfaces;

/// <summary>
/// INTERFACE: IScannerDiscoverySource
/// PURPOSE: Defines a provider-neutral boundary for retrieving scanner discoveries.
/// WHEN CALLED: An application workflow requests a timestamped batch from a configured scanner adapter.
/// IMPLEMENTATION NOTE: Provider-specific SDKs, authentication, rate limits, and payload parsing belong in Infrastructure.
/// </summary>
public interface IScannerDiscoverySource
{
    /// <summary>
    /// METHOD: GetDiscoveriesAsync
    /// PURPOSE: Retrieves a batch of source observations without converting them into trade signals.
    /// WHEN CALLED: Invoked by an application workflow at a configured polling or refresh boundary.
    /// </summary>
    Task<IReadOnlyList<ScannerDiscoveryEvent>> GetDiscoveriesAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
