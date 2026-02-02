# MultiHost Minimal API Sample

This sample demonstrates hosting multiple MCP servers in a single ASP.NET Core application using `Mcp.AspNetCore.MultiHost`.

## Features Demonstrated

- **Two MCP Hosts**: Admin and User hosts with different tool sets
- **Per-Host DI**: Each host has its own `HostConfig` service
- **Service Bridging**: `ISharedGreetingService` is shared between hosts
- **Discovery Endpoint**: Lists all available hosts at `/mcp/_hosts`

## Running the Sample

```bash
cd samples/MultiHostMinimalApi
dotnet run
```

The server starts at `http://localhost:5000` with:
- Admin MCP server: `http://localhost:5000/mcp/admin`
- User MCP server: `http://localhost:5000/mcp/user`
- Discovery endpoint: `http://localhost:5000/mcp/_hosts`

## Testing with Claude Desktop

### Using mcp-remote Bridge

For users without remote MCP support, use `mcp-remote` as a stdio-to-HTTP bridge:

1. **Install mcp-remote:**
   ```bash
   npm install -g mcp-remote
   ```

2. **Configure Claude Desktop:**

   Edit your config file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

   ```json
   {
     "mcpServers": {
       "multihost-admin": {
         "command": "npx",
         "args": ["mcp-remote", "http://localhost:5000/mcp/admin"]
       },
       "multihost-user": {
         "command": "npx",
         "args": ["mcp-remote", "http://localhost:5000/mcp/user"]
       }
     }
   }
   ```

### Steps to Connect

1. **Start the Sample Server:**
   ```bash
   dotnet run --project samples/MultiHostMinimalApi
   ```

2. **Add the config** (using one of the options above)

3. **Restart Claude Desktop** to load the new MCP servers

4. **Verify Connection** - you should see the tools available:

**Admin Host Tools:**
- `get_system_status` - Get system status information
- `list_environment_variables` - List environment variables
- `greet_admin` - Greet an admin user

**User Host Tools:**
- `get_current_time` - Get current date and time
- `echo` - Echo a message back
- `greet_user` - Greet a user
- `calculate` - Perform calculations

## Testing with curl

You can also test the MCP protocol directly using curl:

### Check Discovery Endpoint

```bash
curl http://localhost:5000/mcp/_hosts
```

Response:
```json
{
  "hosts": [
    { "name": "admin", "routePrefix": "/mcp/admin" },
    { "name": "user", "routePrefix": "/mcp/user" }
  ]
}
```

### Initialize MCP Session (Admin Host)

```bash
curl -X POST http://localhost:5000/mcp/admin \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": { "name": "test-client", "version": "1.0.0" }
    }
  }'
```

### List Available Tools

```bash
curl -X POST http://localhost:5000/mcp/admin \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

## Tool Examples

Once connected, try these prompts in Claude Desktop:

**With Admin Host:**
- "What's the system status?"
- "Show me some environment variables"
- "Greet admin John"

**With User Host:**
- "What time is it?"
- "Echo hello world"
- "Calculate 42 multiply 2"
- "Greet user Alice"

## Architecture

```
┌─────────────────────────────────────────────────┐
│              ASP.NET Core App                    │
│                                                  │
│  ┌──────────────┐      ┌──────────────┐         │
│  │ Admin Host   │      │  User Host   │         │
│  │ /mcp/admin   │      │  /mcp/user   │         │
│  │              │      │              │         │
│  │ AdminTools   │      │  UserTools   │         │
│  │ - GetStatus  │      │  - GetTime   │         │
│  │ - ListEnvVars│      │  - Echo      │         │
│  │ - GreetAdmin │      │  - Calculate │         │
│  └──────┬───────┘      └──────┬───────┘         │
│         │                     │                  │
│         └──────────┬──────────┘                  │
│                    │                             │
│         ┌──────────▼──────────┐                 │
│         │ Shared Services     │                 │
│         │ - GreetingService   │                 │
│         │ - ILoggerFactory    │                 │
│         └─────────────────────┘                 │
└─────────────────────────────────────────────────┘
```

## Troubleshooting

### Claude Desktop doesn't show the tools

1. Ensure the sample is running (`dotnet run`)
2. Check the URL is correct in the config
3. Restart Claude Desktop after config changes
4. Check Claude Desktop logs for connection errors

### Connection refused errors

The sample runs on `http://localhost:5000` by default. If that port is in use, update `Properties/launchSettings.json` and your Claude Desktop config accordingly.

### Tools not working

Check the server console for errors. Each tool requires proper DI setup - the sample demonstrates this with `HostConfig` and `ISharedGreetingService`.
