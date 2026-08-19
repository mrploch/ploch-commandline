---
name: mcp-advanced-capabilities
description: Use when an MCP tool needs progress reporting, cancellation, client logging, notifications, LLM sampling, user elicitation, long-running tasks, or filters/middleware in .NET. Triggers on - IProgress ProgressNotificationValue, CancellationToken mcp, sampling AsSamplingChatClient, elicitation ElicitAsync, MRTR InputRequiredException, Tasks extension WithTasks, mcp filters middleware.
---

# Advanced MCP Server Capabilities (.NET SDK v2)

## Progress

Works in stateless, stateful and stdio (notifications ride the POST response stream):

```csharp
[McpServerTool]
public static async Task<string> Import(IProgress<ProgressNotificationValue> progress, CancellationToken ct)
{
    for (var i = 0; i < 10; i++)
    {
        await Step(ct);
        progress.Report(new() { Progress = i + 1, Total = 10, Message = $"step {i + 1}" });
    }
    return "done";
}
```

- The SDK auto-wires `IProgress<>` parameters and echoes the caller's `progressToken`. Check `context.Params?.ProgressToken` before manual sends — not all clients send one.
- Client: pass `Progress<ProgressNotificationValue>` per call, or `RegisterNotificationHandler(NotificationMethods.ProgressNotification, ...)` and filter by token.

## Cancellation

- Add a `CancellationToken` parameter and **flow it everywhere**. Client cancellation sends `notifications/cancelled`; the handler's token fires; `OperationCanceledException` flows back as a cancellation response (treat it as graceful — excel-mcp exits 0 on it).
- Token source: stateless HTTP = `HttpContext.RequestAborted` (client disconnect cancels the handler); stateful = linked (request + shutdown + session disposal); stdio = the run token. Task-augmented calls use `tasks/cancel` instead.
- Known SDK weak spots (open issues): background task-store runners and disposal racing the transport — never let disposal race in your own code; test cancellation explicitly.

## Logging to the client

- Get a client-directed logger: `server.AsClientLoggerProvider().CreateLogger("MyTool")` — MCP RFC 5424 levels mapped to .NET `LogLevel` (`Trace` silently dropped). SDK auto-handles `logging/setLevel` into `McpServer.LoggingLevel`.
- Clients should call `SetLoggingLevelAsync(level)` — behaviour is unspecified otherwise — and register for `NotificationMethods.LoggingMessageNotification`.

## Sampling (server asks the client LLM) — deprecated path + MRTR replacement

Classic (stateful/stdio only; **deprecated by SEP-2577 in protocol 2026-07-28**):

```csharp
[McpServerTool, Description("Summarizes the given text")]
public static async Task<string> Summarize(McpServer server, string text, CancellationToken ct) =>
    $"Summary: {await server.AsSamplingChatClient()
        .GetResponseAsync([new(ChatRole.User, $"Briefly summarize:\n{text}")], new() { MaxOutputTokens = 256 }, ct)}";
```

`SampleAsync`/`ElicitAsync` throw `InvalidOperationException` on stateless servers. The portable v2 pattern is **MRTR**:

```csharp
if (server.IsMrtrSupported)
    throw new InputRequiredException(
        inputRequests: new() { ["llm_call"] = InputRequest.ForSampling(new CreateMessageRequestParams { Messages = [...], MaxTokens = 256 }) },
        requestState: stateToResumeWith);
// on the retried call:
var result = context.Params.InputResponses["llm_call"].Deserialize(InputResponse.CreateMessageResultJsonTypeInfo);
```

Client handler for classic sampling: `McpClientOptions.Handlers.SamplingHandler = chatClient.CreateSamplingHandler()` (any `IChatClient`).

## Elicitation (server asks the user)

- **Form mode** (in-band): `await server.ElicitAsync(new ElicitRequestParams { Message = "...", RequestedSchema = ... }, ct)` — schemas: `StringSchema`/`NumberSchema`/`BooleanSchema`, enum select schemas (`TitledSingleSelectEnumSchema` with `OneOf` `Const`/`Title`, multi-select variants), all with `Default`. **MUST NOT be used for secrets** (passwords, API keys) — data transits the client and LLM context.
- **URL mode** (out-of-band, 2025-11-25+): `Mode = "url"` + `Url` + `ElicitationId`, pointing at a server-hosted HTTPS page — the sanctioned path for credentials/OAuth/payments. Stateless-safe variant: throw `UrlElicitationRequiredException` (JSON-RPC `-32042`); client opens the URL with consent, optionally waits for `NotificationMethods.ElicitationCompleteNotification`, retries. MRTR variant: `InputRequest.ForElicitation(...)`.
- Client declares `ClientCapabilities.Elicitation { Form, Url }` and sets `Handlers.ElicitationHandler`; result `Action = "accept" | "decline" | "cancel"`.
- Security: bind the elicitation to the authenticated user and verify the user opening the URL == the user who triggered it (`sub` match); never put credentials in the URL. See `mcp-auth-downstream`.

## Long-running work — Tasks extension

`ModelContextProtocol.Extensions.Tasks`: `.WithTasks()` + `IMcpTaskStore` (`InMemoryMcpTaskStore` built in; implement durable stores for production). Client gets a task id immediately, polls `tasks/get` (statuses `working/input_required/completed/cancelled/failed`), cancels via `tasks/cancel`. Works **stateless** (shared store). Register `WithTasks` before ordinary call-tool filters (alternate-result filter ordering). Tasks decouple handlers from the POST → add rate limiting (backpressure opt-out). Alternative without the extension: return a job id + `get_job_status` polling tool.

## Notifications & filters

- Unsolicited notifications (`XxxListChangedNotification`, resource updates) need stateful HTTP or stdio, and are **silently dropped if the client never opened the GET stream** — best-effort by spec.
- Middleware: `WithMessageFilters(f => f.AddIncomingFilter(next => async (context, ct) => { ...; return await next(context, ct); }))` and `WithRequestFilters(f => f.AddCallToolFilter(...) / AddListToolsFilter(...))`. Registration order = execution order (first = outermost); skip by not calling `next`; pass data via `context.Items`. List-filters are how you do per-user/per-header dynamic tool filtering in stateless mode (no push available).

## Related skills

`mcp-http-hosting-state` (what stateless disables), `mcp-auth-downstream` (elicitation for credentials), `mcp-tools-design`.
