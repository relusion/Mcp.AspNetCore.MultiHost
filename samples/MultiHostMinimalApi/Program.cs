using Mcp.AspNetCore.MultiHost;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Add a shared service that will be bridged to MCP hosts
builder.Services.AddSingleton<ISharedGreetingService, SharedGreetingService>();

// Configure multiple MCP hosts
builder.Services.AddMcpMultiHost(options =>
{
    // Host 1: Admin tools for system management
    options.AddHost("admin", host =>
    {
        host.WithRoutePrefix("/mcp/admin")
            .ConfigureHostServices(services =>
            {
                // Host-specific service
                services.AddSingleton(new HostConfig("Admin Host", "v1.0"));
            })
            .BridgeServices(bridge =>
            {
                bridge.ForwardAspNetCoreDefaults();
                bridge.ForwardSingleton<ISharedGreetingService>();
            })
            .ConfigureMcpServer(mcp => mcp
                .WithHttpTransport()
                .WithTools<AdminTools>());
    });

    // Host 2: User tools with different capabilities
    options.AddHost("user", host =>
    {
        host.WithRoutePrefix("/mcp/user")
            .ConfigureHostServices(services =>
            {
                services.AddSingleton(new HostConfig("User Host", "v1.0"));
            })
            .BridgeServices(bridge =>
            {
                bridge.ForwardAspNetCoreDefaults();
                bridge.ForwardSingleton<ISharedGreetingService>();
            })
            .ConfigureMcpServer(mcp => mcp
                .WithHttpTransport()
                .WithTools<UserTools>());
    });
});

var app = builder.Build();

// Map a simple health check endpoint
app.MapGet("/", () => "MCP Multi-Host Sample is running. See /mcp/_hosts for available hosts.");

// Map all MCP hosts with discovery enabled
app.MapMcpMultiHost(options =>
{
    options.MapDiscoveryEndpoint = true;
});

app.Run();

// ==================== Shared Services ====================

/// <summary>
/// Interface for a shared greeting service available to all hosts.
/// </summary>
public interface ISharedGreetingService
{
    string GetGreeting(string name);
}

/// <summary>
/// Implementation of the shared greeting service.
/// </summary>
public class SharedGreetingService : ISharedGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}! Welcome to MCP Multi-Host.";
}

/// <summary>
/// Host-specific configuration record.
/// </summary>
public record HostConfig(string HostName, string Version);

// ==================== Admin Tools ====================

/// <summary>
/// Admin-specific tools for system management.
/// </summary>
[McpServerToolType]
public class AdminTools
{
    private readonly HostConfig _config;
    private readonly ISharedGreetingService _greeting;

    public AdminTools(HostConfig config, ISharedGreetingService greeting)
    {
        _config = config;
        _greeting = greeting;
    }

    [McpServerTool]
    [Description("Get system status information (admin only)")]
    public object GetSystemStatus()
    {
        return new
        {
            Host = _config.HostName,
            Version = _config.Version,
            Uptime = TimeSpan.FromSeconds(Environment.TickCount64 / 1000).ToString(),
            MachineName = Environment.MachineName,
            Processors = Environment.ProcessorCount,
            Timestamp = DateTime.UtcNow
        };
    }

    [McpServerTool]
    [Description("List all environment variables (admin only)")]
    public Dictionary<string, string> ListEnvironmentVariables()
    {
        return Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Take(10) // Limit for safety
            .ToDictionary(e => e.Key.ToString()!, e => e.Value?.ToString() ?? "");
    }

    [McpServerTool]
    [Description("Greet an admin user")]
    public string GreetAdmin([Description("The admin's name")] string name)
    {
        return _greeting.GetGreeting($"Admin {name}");
    }
}

// ==================== User Tools ====================

/// <summary>
/// User-specific tools for general operations.
/// </summary>
[McpServerToolType]
public class UserTools
{
    private readonly HostConfig _config;
    private readonly ISharedGreetingService _greeting;

    public UserTools(HostConfig config, ISharedGreetingService greeting)
    {
        _config = config;
        _greeting = greeting;
    }

    [McpServerTool]
    [Description("Get the current date and time")]
    public object GetCurrentTime()
    {
        return new
        {
            Host = _config.HostName,
            UtcTime = DateTime.UtcNow,
            LocalTime = DateTime.Now,
            TimeZone = TimeZoneInfo.Local.DisplayName
        };
    }

    [McpServerTool]
    [Description("Echo a message back to the user")]
    public string Echo([Description("The message to echo")] string message)
    {
        return $"[{_config.HostName}] Echo: {message}";
    }

    [McpServerTool]
    [Description("Greet a user")]
    public string GreetUser([Description("The user's name")] string name)
    {
        return _greeting.GetGreeting(name);
    }

    [McpServerTool]
    [Description("Perform a simple calculation")]
    public object Calculate(
        [Description("First number")] double a,
        [Description("Second number")] double b,
        [Description("Operation: add, subtract, multiply, divide")] string operation)
    {
        double result = operation.ToLower() switch
        {
            "add" => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide" => b != 0 ? a / b : throw new ArgumentException("Cannot divide by zero"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };

        return new { a, b, operation, result };
    }
}
