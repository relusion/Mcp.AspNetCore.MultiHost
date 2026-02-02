using FluentAssertions;
using Mcp.AspNetCore.MultiHost;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Options;

public class McpMultiHostOptionsTests
{
    [Fact]
    public void Hosts_InitiallyEmpty()
    {
        // Arrange & Act
        var options = new McpMultiHostOptions();

        // Assert
        options.Hosts.Should().BeEmpty();
    }

    [Fact]
    public void AddHost_ValidConfiguration_AddsHostToList()
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // Act
        var definition = options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        options.Hosts.Should().HaveCount(1);
        options.Hosts[0].Should().BeSameAs(definition);
        definition.Name.Should().Be("testhost");
        definition.RoutePrefix.Should().Be("/mcp/test");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddHost_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // Act
        var act = () => options.AddHost(name!, host =>
        {
            host.WithRoutePrefix("/mcp/test");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddHost_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // Act
        var act = () => options.AddHost("testhost", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHost_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new McpMultiHostOptions();
        options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test1");
            host.ConfigureMcpServer(_ => { });
        });

        // Act
        var act = () => options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test2");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Host 'testhost' is already registered.");
    }

    [Fact]
    public void AddHost_DuplicateNameCaseInsensitive_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new McpMultiHostOptions();
        options.AddHost("TestHost", host =>
        {
            host.WithRoutePrefix("/mcp/test1");
            host.ConfigureMcpServer(_ => { });
        });

        // Act
        var act = () => options.AddHost("TESTHOST", host =>
        {
            host.WithRoutePrefix("/mcp/test2");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TESTHOST*already registered*");
    }

    [Fact]
    public void AddHost_MultipleHosts_AddsAllToList()
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // Act
        options.AddHost("core", host =>
        {
            host.WithRoutePrefix("/mcp/core");
            host.ConfigureMcpServer(_ => { });
        });

        options.AddHost("admin", host =>
        {
            host.WithRoutePrefix("/mcp/admin");
            host.ConfigureMcpServer(_ => { });
        });

        options.AddHost("tenant", host =>
        {
            host.WithRoutePrefix("/mcp/tenant");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        options.Hosts.Should().HaveCount(3);
        options.Hosts.Select(h => h.Name).Should().Equal("core", "admin", "tenant");
    }

    [Fact]
    public void AddHost_BuilderThrows_DoesNotAddToList()
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // Act - Missing required configuration
        var act = () => options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test");
            // Missing ConfigureMcpServer - will throw
        });

        // Assert
        act.Should().Throw<InvalidOperationException>();
        options.Hosts.Should().BeEmpty();
    }

    [Fact]
    public void AddHost_AfterBuildFailure_CanReuseNameForNewHost()
    {
        // Arrange
        var options = new McpMultiHostOptions();

        // First attempt fails
        var firstAct = () => options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test");
            // Missing ConfigureMcpServer
        });
        firstAct.Should().Throw<InvalidOperationException>();

        // Act - Second attempt with valid configuration should work
        var definition = options.AddHost("testhost", host =>
        {
            host.WithRoutePrefix("/mcp/test");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        options.Hosts.Should().HaveCount(1);
        definition.Name.Should().Be("testhost");
    }

    [Fact]
    public void AddHost_DuplicateRoutePrefix_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new McpMultiHostOptions();
        options.AddHost("host1", host =>
        {
            host.WithRoutePrefix("/mcp/shared");
            host.ConfigureMcpServer(_ => { });
        });

        // Act
        var act = () => options.AddHost("host2", host =>
        {
            host.WithRoutePrefix("/mcp/shared");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Route prefix '/mcp/shared' is already registered*");
    }

    [Fact]
    public void AddHost_DuplicateRoutePrefixCaseInsensitive_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new McpMultiHostOptions();
        options.AddHost("host1", host =>
        {
            host.WithRoutePrefix("/mcp/Shared");
            host.ConfigureMcpServer(_ => { });
        });

        // Act
        var act = () => options.AddHost("host2", host =>
        {
            host.WithRoutePrefix("/MCP/SHARED");
            host.ConfigureMcpServer(_ => { });
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Route prefix*already registered*");
    }

    [Fact]
    public void AddHost_DuplicateRoutePrefixWithNormalization_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new McpMultiHostOptions();
        options.AddHost("host1", host =>
        {
            host.WithRoutePrefix("/mcp/shared/");  // trailing slash
            host.ConfigureMcpServer(_ => { });
        });

        // Act
        var act = () => options.AddHost("host2", host =>
        {
            host.WithRoutePrefix("/mcp/shared");  // no trailing slash
            host.ConfigureMcpServer(_ => { });
        });

        // Assert - Should detect as duplicate after normalization
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Route prefix '/mcp/shared' is already registered*");
    }
}
