# Mcp.AspNetCore.MultiHost

Host multiple MCP (Model Context Protocol) servers with distinct tool sets and configurations in a single ASP.NET Core application.

## Features

- **Multiple MCP Hosts**: Run multiple isolated MCP servers in a single application
- **Per-Host DI Containers**: Each host has its own isolated dependency injection container
- **Service Bridging**: Share services from the main application with MCP hosts
- **Flexible Configuration**: Configure each host independently with different tools, prompts, and resources
- **Discovery Endpoint**: Optional endpoint to list all available MCP hosts
- **Security Helpers**: Origin validation for cross-origin protection

## Installation

```bash
dotnet add package Mcp.AspNetCore.MultiHost
```

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register MCP hosts
builder.Services.AddMcpMultiHost(options =>
{
    options.AddHost("admin", host =>
    {
        host.WithRoutePrefix("/mcp/admin")
            .ConfigureMcpServer(mcp => mcp
                .WithHttpTransport()
                .WithTools<AdminTools>());
    });

    options.AddHost("user", host =>
    {
        host.WithRoutePrefix("/mcp/user")
            .ConfigureMcpServer(mcp => mcp
                .WithHttpTransport()
                .WithTools<UserTools>());
    });
});

var app = builder.Build();

// Map all MCP hosts
app.MapMcpMultiHost();

app.Run();
```

## Configuration

### Host Configuration

Each host is configured using the fluent builder API:

```csharp
options.AddHost("myhost", host =>
{
    // Required: Set the route prefix
    host.WithRoutePrefix("/mcp/myhost");

    // Required: Configure MCP server
    host.ConfigureMcpServer(mcp => mcp
        .WithHttpTransport()
        .WithTools<MyTools>()
        .WithPrompts<MyPrompts>());

    // Optional: Add host-specific services
    host.ConfigureHostServices(services =>
    {
        services.AddSingleton<MyHostService>();
    });

    // Optional: Configure endpoint conventions
    host.ConfigureEndpoints(endpoints =>
    {
        endpoints.RequireAuthorization("AdminPolicy");
    });

    // Optional: Custom service bridging
    host.BridgeServices(bridge =>
    {
        bridge.ForwardAspNetCoreDefaults();
        bridge.ForwardSingleton<IMySharedService>();
    });
});
```

### Discovery Endpoint

Enable a discovery endpoint to list all available MCP hosts:

```csharp
app.MapMcpMultiHost(options =>
{
    options.MapDiscoveryEndpoint = true;
    options.DiscoveryEndpointPath = "/mcp/_hosts"; // default
});
```

Response format:
```json
{
  "hosts": [
    { "name": "admin", "routePrefix": "/mcp/admin" },
    { "name": "user", "routePrefix": "/mcp/user" }
  ]
}
```

## Service Bridging

By default, the following ASP.NET Core services are automatically bridged to each host:

- `ILoggerFactory` (with `ILogger<T>` support)
- `IConfiguration`
- `IHostEnvironment`
- `IHostApplicationLifetime`
- `IHttpContextAccessor` (if registered)

### Custom Bridging

Override the default bridging behavior:

```csharp
host.BridgeServices(bridge =>
{
    // Include ASP.NET Core defaults
    bridge.ForwardAspNetCoreDefaults();

    // Forward additional singletons
    bridge.ForwardSingleton<IMyService>();

    // Forward with factory
    bridge.Forward<IMyService>(sp => sp.GetRequiredService<IMyService>());
});
```

## Security

### Origin Validation

Protect MCP endpoints from unauthorized cross-origin requests:

```csharp
host.ConfigureEndpoints(endpoints =>
{
    endpoints.RequireSameOriginOrAllowed("https://trusted.example.com");
});
```

This filter:
- Allows requests without Origin header (non-browser clients)
- Allows same-origin requests
- Allows explicitly listed origins
- Blocks all other cross-origin requests with 403 Forbidden

### Authorization

Apply ASP.NET Core authorization to MCP endpoints:

```csharp
host.ConfigureEndpoints(endpoints =>
{
    endpoints.RequireAuthorization("MyPolicy");
});
```

## Accessing the Registry

Inject `IMcpHostRegistry` to access registered hosts at runtime:

```csharp
app.MapGet("/admin/hosts", (IMcpHostRegistry registry) =>
{
    return registry.Hosts.Select(h => new { h.Name, h.RoutePrefix });
});
```

## Requirements

- .NET 8.0 or later
- ModelContextProtocol.AspNetCore 0.1.0 or later

## Architecture

Each MCP host runs in isolation with its own:
- DI container
- Service registrations
- Tool definitions
- Prompt templates

Services can be shared from the main application through bridging, but each host maintains its own instances of host-specific services.

```
                    Main Application
                           |
            +--------------+--------------+
            |              |              |
        [Host A]       [Host B]       [Host C]
         /mcp/a         /mcp/b         /mcp/c
            |              |              |
        Tools A        Tools B        Tools C
```

## License

MIT

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
