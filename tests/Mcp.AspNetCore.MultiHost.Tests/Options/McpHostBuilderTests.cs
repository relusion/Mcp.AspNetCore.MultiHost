using FluentAssertions;
using Mcp.AspNetCore.MultiHost;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Options;

public class McpHostBuilderTests
{
    [Fact]
    public void WithRoutePrefix_ValidPrefix_SetsPrefix()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var result = builder.WithRoutePrefix("/mcp/test");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithRoutePrefix_NullOrWhitespace_ThrowsArgumentException(string? prefix)
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.WithRoutePrefix(prefix!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithRoutePrefix_NotStartingWithSlash_ThrowsArgumentException()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.WithRoutePrefix("mcp/test");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Route prefix must start with '/'*");
    }

    [Fact]
    public void ConfigureMcpServer_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.ConfigureMcpServer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfigureHostServices_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.ConfigureHostServices(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfigureEndpoints_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.ConfigureEndpoints(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BridgeServices_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new McpHostBuilder();

        // Act
        var act = () => builder.BridgeServices(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_WithoutRoutePrefix_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.ConfigureMcpServer(_ => { });

        // Act
        var act = () => builder.Build("testhost");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*testhost*route prefix*WithRoutePrefix()*");
    }

    [Fact]
    public void Build_WithoutConfigureMcpServer_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/mcp/test");

        // Act
        var act = () => builder.Build("testhost");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*testhost*configure MCP server*ConfigureMcpServer()*");
    }

    [Fact]
    public void Build_WithValidConfiguration_ReturnsDefinition()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/mcp/test");
        builder.ConfigureMcpServer(_ => { });

        // Act
        var definition = builder.Build("testhost");

        // Assert
        definition.Should().NotBeNull();
        definition.Name.Should().Be("testhost");
        definition.RoutePrefix.Should().Be("/mcp/test");
        definition.ConfigureMcpServer.Should().NotBeNull();
        definition.ConfigureHostServices.Should().BeNull();
        definition.ConfigureEndpoints.Should().BeNull();
        definition.BridgeServices.Should().BeNull();
    }

    [Fact]
    public void Build_WithAllOptions_ReturnsCompleteDefinition()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/mcp/test");
        builder.ConfigureHostServices(_ => { });
        builder.ConfigureMcpServer(_ => { });
        builder.ConfigureEndpoints(_ => { });
        builder.BridgeServices(_ => { });

        // Act
        var definition = builder.Build("testhost");

        // Assert
        definition.Should().NotBeNull();
        definition.ConfigureHostServices.Should().NotBeNull();
        definition.ConfigureMcpServer.Should().NotBeNull();
        definition.ConfigureEndpoints.Should().NotBeNull();
        definition.BridgeServices.Should().NotBeNull();
    }

    [Fact]
    public void WithRoutePrefix_TrailingSlash_IsNormalized()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/mcp/test/");
        builder.ConfigureMcpServer(_ => { });

        // Act
        var definition = builder.Build("testhost");

        // Assert
        definition.RoutePrefix.Should().Be("/mcp/test");
    }

    [Fact]
    public void WithRoutePrefix_MultipleTrailingSlashes_AreNormalized()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/mcp/test///");
        builder.ConfigureMcpServer(_ => { });

        // Act
        var definition = builder.Build("testhost");

        // Assert
        definition.RoutePrefix.Should().Be("/mcp/test");
    }

    [Fact]
    public void WithRoutePrefix_RootSlash_IsPreserved()
    {
        // Arrange
        var builder = new McpHostBuilder();
        builder.WithRoutePrefix("/");
        builder.ConfigureMcpServer(_ => { });

        // Act
        var definition = builder.Build("testhost");

        // Assert - Root "/" should not be normalized to empty string
        definition.RoutePrefix.Should().Be("/");
    }
}
