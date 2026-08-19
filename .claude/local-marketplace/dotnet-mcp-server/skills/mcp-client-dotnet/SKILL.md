---
name: mcp-client-dotnet
description: Use when building an MCP client in .NET or integrating MCP tools with an LLM - McpClient.CreateAsync, StdioClientTransport, HttpClientTransport, calling tools, Microsoft.Extensions.AI IChatClient integration, notification handlers. Triggers on - McpClient, StdioClientTransport, HttpClientTransport, ListToolsAsync, CallToolAsync, IChatClient tools, mcp client csharp.
---

# Building MCP Clients in .NET (SDK v2)

## Entry point

v2: **`McpClient.CreateAsync(transport, options?, loggerFactory?, ct)`** (v1's `McpClientFactory`/`IMcpClient` naming is gone; `SseClientTransport` → `HttpClientTransport`).

```csharp
// stdio — launches the server as a subprocess
var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Everything",
    Command = "npx", Arguments = ["-y", "@modelcontextprotocol/server-everything"],
    ShutdownTimeout = TimeSpan.FromSeconds(10),
});
await using var client = await McpClient.CreateAsync(transport);

// Streamable HTTP
var http = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri("https://my-server.example.com/mcp"),   // the MapMcp root — NOT /sse
    TransportMode = HttpTransportMode.AutoDetect,               // or StreamableHttp / Sse
    MaxReconnectionAttempts = 5,
    DefaultReconnectionInterval = TimeSpan.FromSeconds(1),
    // AdditionalHeaders, EnableStandaloneGetStream, OwnsSession, KnownSessionId, OAuth = new() {...}
});
await using var client2 = await McpClient.CreateAsync(http);
```

## Operations

- `ListToolsAsync()` → `IList<McpClientTool>`; `CallToolAsync(name, new Dictionary<string, object?> { ... }, ct)` → `CallToolResult` — check `result.IsError is true`, pattern-match `Content` blocks (`TextContentBlock`, `ImageContentBlock.DecodedData`, …); or `tool.CallAsync(args)`.
- Prompts: `ListPromptsAsync()` / `GetPromptAsync(name, args)`. Resources: `ListResourcesAsync()` / `ListResourceTemplatesAsync()` / `ReadResourceAsync(uri)` / `ReadResourceAsync("file:///{path}", new() { ["path"] = ... })` / `SubscribeToResourceAsync(uri, handler)`.
- Logging: check `client.ServerCapabilities.Logging`, call `SetLoggingLevelAsync(level)` (do it — unspecified behaviour otherwise), handle `NotificationMethods.LoggingMessageNotification` via `RegisterNotificationHandler`.
- Session recovery (HTTP): 404 means session expired — reconnect via `CreateAsync` or resume with `KnownSessionId`; client disposal sends `DELETE` unless `OwnsSession = false`.

## Client options

`McpClientOptions`: `ClientInfo`, `Capabilities` (Roots/Sampling/Elicitation), `Handlers` (`SamplingHandler` — e.g. `chatClient.CreateSamplingHandler()`; `ElicitationHandler` returning `ElicitResult { Action = "accept" | "decline" | "cancel", Content = … }`), `ProtocolVersion` (pin `"2026-07-28"` to forbid initialize fallback; clients otherwise probe then fall back, cached per transport). stdio clients probe with `server/discover` (5 s timeout).

Elicitation handler duties (security): show the full URL for URL-mode, get explicit consent, never pre-fetch, open outside inspectable webviews, allow only http(s) URLs (reject `javascript:`/`data:`/`file:` — a malicious server can inject these).

## Microsoft.Extensions.AI integration

`McpClientTool : AIFunction`, so MCP tools plug straight into any `IChatClient`:

```csharp
IList<McpClientTool> tools = await client.ListToolsAsync();
var response = await chatClient.GetResponseAsync("Summarise open orders", new ChatOptions { Tools = [.. tools] });
```

Inversely, `chatClient.CreateSamplingHandler()` lets your client serve server-side sampling requests with any M.E.AI model. `client.AddKnownTools([tool])` pre-loads schemas so `[McpHeader]` headers are sent without a `ListToolsAsync` round-trip.

## Trust boundaries (client side)

Treat tool descriptions and results from third-party servers as **untrusted input**: pin/review tool definitions (rug-pull defence), alert on description changes, gate destructive calls on user consent, sandbox locally-launched servers. See `mcp-security-hardening`.

## Related skills

`mcp-server-setup`, `mcp-testing-debugging` (in-memory client harness), `mcp-security-hardening`.
