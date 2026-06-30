using Fcs.Bff.WebApi.Proxy;
using FluentAssertions;

namespace Fcs.Bff.UnitTests.Proxy;

public sealed class HeaderForwardingTests
{
    [Theory]
    [InlineData("Connection")]
    [InlineData("Transfer-Encoding")]
    [InlineData("Host")]
    public void ShouldSkipHeader_WhenHopByHopHeader_ReturnsTrue(string headerName)
    {
        HeaderForwarding.ShouldSkipHeader(headerName).Should().BeTrue();
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("X-Correlation-ID")]
    public void ShouldSkipHeader_WhenEndToEndHeader_ReturnsFalse(string headerName)
    {
        HeaderForwarding.ShouldSkipHeader(headerName).Should().BeFalse();
    }

    [Theory]
    [InlineData("Content-Type")]
    [InlineData("Content-Length")]
    public void IsContentHeader_WhenContentHeader_ReturnsTrue(string headerName)
    {
        HeaderForwarding.IsContentHeader(headerName).Should().BeTrue();
    }
}
