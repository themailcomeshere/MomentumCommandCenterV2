using System.Net;
using MomentumCommandCenterV2.API.Hosting;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class LocalShutdownPolicyTests
{
    private const string Token = "0123456789abcdef0123456789abcdef"; // test-only fixture

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short-token")]
    public void ShutdownIsDisabledWithoutAStrongConfiguredToken(string? configured)
    {
        Assert.Equal(LocalShutdownDecision.Disabled,
            LocalShutdownPolicy.Evaluate(configured, configured, IPAddress.Loopback));
    }

    [Fact]
    public void LoopbackRequestWithMatchingTokenIsAllowed()
    {
        Assert.Equal(LocalShutdownDecision.Allowed, LocalShutdownPolicy.Evaluate(Token, Token, IPAddress.Loopback));
        Assert.Equal(LocalShutdownDecision.Allowed, LocalShutdownPolicy.Evaluate(Token, Token, IPAddress.IPv6Loopback));
    }

    [Fact]
    public void NonLoopbackRequestIsForbiddenEvenWithTheToken()
    {
        Assert.Equal(LocalShutdownDecision.Forbidden,
            LocalShutdownPolicy.Evaluate(Token, Token, IPAddress.Parse("192.168.1.20")));
        Assert.Equal(LocalShutdownDecision.Forbidden, LocalShutdownPolicy.Evaluate(Token, Token, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0123456789abcdef0123456789abcdeX")]
    public void WrongOrMissingTokenIsForbidden(string? supplied)
    {
        Assert.Equal(LocalShutdownDecision.Forbidden, LocalShutdownPolicy.Evaluate(Token, supplied, IPAddress.Loopback));
    }
}
