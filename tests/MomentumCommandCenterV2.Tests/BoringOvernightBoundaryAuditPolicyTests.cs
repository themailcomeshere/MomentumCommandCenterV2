using MomentumCommandCenterV2.API.BoringOvernight;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class BoringOvernightBoundaryAuditPolicyTests
{
    private static readonly DateTimeOffset Endpoint = DateTimeOffset.Parse("2026-11-13T14:31:00Z");

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FutureEndpointIsPendingRegardlessOfCurrentProviderOrStoreContents(
        bool providerExact, bool storeExact)
    {
        var auditedAt = Endpoint.AddTicks(-1);

        Assert.Equal("PENDING_NOT_YET_AVAILABLE",
            BoringOvernightBoundaryAuditPolicy.GetStatus(providerExact, storeExact, Endpoint, auditedAt));
    }

    [Theory]
    [InlineData(true, true, "COVERED")]
    [InlineData(true, false, "PROVIDER_BAR_NOT_FOUND_IN_STORE")]
    [InlineData(false, true, "STORE_ONLY_NO_PROVIDER_REFRESH_BAR")]
    [InlineData(false, false, "PROVIDER_RESPONSE_MISSING_EXACT_BAR")]
    public void AvailableEndpointUsesExactCoverageState(
        bool providerExact, bool storeExact, string expected)
    {
        var auditedAt = Endpoint;

        Assert.Equal(expected,
            BoringOvernightBoundaryAuditPolicy.GetStatus(providerExact, storeExact, Endpoint, auditedAt));
    }

    [Fact]
    public void EndpointBecomesAuditableAtExactAvailabilityTime()
    {
        Assert.Equal("PROVIDER_RESPONSE_MISSING_EXACT_BAR",
            BoringOvernightBoundaryAuditPolicy.GetStatus(false, false, Endpoint, Endpoint));
    }
}
