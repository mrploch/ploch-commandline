---
name: mcp-server-setup
description: Use when creating or configuring an MCP server in .NET - SDK packages/versions, minimal stdio and ASP.NET Core Streamable HTTP servers, hybrid dual-transport hosts, stdio logging hygiene, tool registration options. Triggers on - new mcp server, ModelContextProtocol package, AddMcpServer, WithStdioServerTransport, MapMcp, stdio logging, dotnet new mcpserver.
---

# .NET MCP Server Setup (mid-2026)

## Version landscape

- **SDK v2.0.0-rc.1 shipped 2026-07-25; stable 2.0.0 planned on/before 2026-07-28.** Latest 1.x is 1.4.1 (terminus). New projects: build against v2. Old blog posts describing v1 behaviour (stateful HTTP default, `McpClientFactory`, `SseClientTransport`) are wrong for v2.
- v2 targets protocol revision **`2026-07-28`**: removes `initialize` handshake (SEP-2575) and `Mcp-Session-Id` (SEP-2567); replaces server→client requests with **MRTR** (Multi Round-Trip Requests, SEP-2322); deprecates sampling and roots (SEP-2577).
- Packages (all: net10.0/net9.0/net8.0; Core also netstandard2.0):

| Package | Use for |
|---|---|
| `ModelContextProtocol` | **Start here** — hosting, DI, attribute-based discovery. Right for stdio servers and clients |
| `ModelContextProtocol.Core` | Client + low-level server, minimal deps |
| `ModelContextProtocol.AspNetCore` | Streamable HTTP hosting (`WithHttpTransport`, `MapMcp`) |
| `ModelContextProtocol.Extensions.Tasks` | Long-running "call-now, fetch-later" tool calls |
| `ModelContextProtocol.Extensions.Apps` | MCP Apps (interactive UI in hosts) |

- Project template: `dotnet new mcpserver` (`Microsoft.McpServer.ProjectTemplates`, preview) — scaffolds NuGet/dnx packaging, `.mcp/server.json`, AOT-ready publish.

## Minimal stdio server

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o =>
    o.LogToStandardErrorThreshold = LogLevel.Trace);   // MANDATORY — see stdio hygiene
builder.Services
    .AddMcpServer(o => { o.ServerInfo = new() { Name = "my-server", Version = "1.0.0" }; })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();                          // scans [McpServerToolType]
await builder.Build().RunAsync();
```

Packages: `ModelContextProtocol` + `Microsoft.Extensions.Hosting`.

## stdio hygiene (the #1 killer)

stdout is the JSON-RPC channel; **any stray byte corrupts the protocol** (symptom: client shows "connected" but tools never respond / parse errors / dead connection).

- `LogToStandardErrorThreshold = LogLevel.Trace` is mandatory, not optional — the default console provider writes Information and below to **stdout** (Warning+ already goes to stderr), so info logs corrupt the stream while warnings don't. Belt-and-braces: `Console.SetOut(Console.Error);` before host build, and `logging.ClearProviders()` first so config overrides can't re-add a stdout writer.
- Child processes inherit stdout — always `ProcessStartInfo.RedirectStandardOutput = true` when shelling out.
- Anchor file paths on `AppContext.BaseDirectory` or env vars, never `Environment.CurrentDirectory` — clients launch the server with an arbitrary cwd (works in `dotnet run`, fails when spawned).
- Disable config file watching: rebuild config sources with `reloadOnChange: false` (Host default is true; excel-mcp measured ~85% CPU under file-I/O storms in a stdio server).
- Smoke test: pipe one `initialize` request in, capture `> out 2> err`; stdout must contain **exactly one** well-formed JSON object. Automate this as a CI test.
- Lazy-init heavy resources (DB connections, indexes) on first tool call — eager init causes client connect-timeouts and retry loops.

## Minimal Streamable HTTP server (ASP.NET Core)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)   // DEFAULT in v2 (v1 defaulted stateful!) — set explicitly
    .WithToolsFromAssembly();
var app = builder.Build();
app.MapMcp();          // or app.MapMcp("/mcp")
app.Run("http://localhost:3001");
```

Package: `ModelContextProtocol.AspNetCore`. Local-server security: set `AllowedHosts` to loopback (Kestrel doesn't validate Host by default — DNS-rebinding defence), bind 127.0.0.1, only enable CORS deliberately with a restrictive policy. See `mcp-http-hosting-state` for stateless-vs-stateful and scaling; `mcp-auth-inbound` before exposing anything.

## Hybrid dual-transport host (recommended for anything that may go remote)

One binary, transport chosen at startup (microsoft/mcp-dotnet-samples "HybridApp" pattern) — pre-empts the recurring "please host this stdio server remotely" issue class:

```csharp
var useHttp = args.Contains("--http") || Environment.GetEnvironmentVariable("UseHttp") == "true";
IHostApplicationBuilder builder = useHttp
    ? WebApplication.CreateBuilder(args)
    : Host.CreateApplicationBuilder(args);

var mcp = builder.Services.AddMcpServer();
if (useHttp) mcp.WithHttpTransport(o => o.Stateless = true);
else         mcp.WithStdioServerTransport();
mcp.WithToolsFromAssembly();

var host = builder.Build();
if (host is WebApplication app) { app.MapMcp("/mcp"); await app.RunAsync(); }
else await ((IHost)host).RunAsync();
```

## Registration options

- `WithToolsFromAssembly()` / `WithPromptsFromAssembly()` / `WithResourcesFromAssembly()` — reflection scan (pass `Assembly.GetEntryAssembly()` explicitly in shared bootstrap code).
- `.WithTools<MyTools>()` / `.WithPrompts<T>()` / `.WithResources<T>()` — explicit generic registration; **AOT/trim-friendly**, no scan.
- `McpServerOptions`: `ServerInfo`, `ServerInstructions` (guidance sent at initialise — cross-tool workflows, reading order; do NOT duplicate tool descriptions), `ToolCollection` (mutable at runtime + list-changed notification), `Filters`.
- Server logging to the client: `server.AsClientLoggerProvider().CreateLogger(...)` (RFC 5424 levels; `Trace` dropped).

## Related skills

`mcp-tools-design` (tools), `mcp-http-hosting-state` (sessions/scaling/DI scopes), `mcp-auth-inbound`/`mcp-auth-downstream`, `mcp-testing-debugging`, `mcp-packaging-distribution`, `mcp-troubleshooting`.
