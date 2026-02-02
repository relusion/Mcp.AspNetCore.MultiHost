using FluentAssertions;
using Mcp.AspNetCore.MultiHost;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Options;

public class McpMultiHostMapOptionsTests
{
    [Fact]
    public void MapDiscoveryEndpoint_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var options = new McpMultiHostMapOptions();

        // Assert
        options.MapDiscoveryEndpoint.Should().BeFalse();
    }

    [Fact]
    public void DiscoveryEndpointPath_DefaultValue_IsMcpHosts()
    {
        // Arrange & Act
        var options = new McpMultiHostMapOptions();

        // Assert
        options.DiscoveryEndpointPath.Should().Be("/mcp/_hosts");
    }

    [Fact]
    public void MapDiscoveryEndpoint_CanBeSetToTrue()
    {
        // Arrange
        var options = new McpMultiHostMapOptions();

        // Act
        options.MapDiscoveryEndpoint = true;

        // Assert
        options.MapDiscoveryEndpoint.Should().BeTrue();
    }

    [Fact]
    public void DiscoveryEndpointPath_CanBeChanged()
    {
        // Arrange
        var options = new McpMultiHostMapOptions();

        // Act
        options.DiscoveryEndpointPath = "/api/mcp/hosts";

        // Assert
        options.DiscoveryEndpointPath.Should().Be("/api/mcp/hosts");
    }
}
