---
name: mcp-testing-debugging
description: Use when testing or debugging a .NET MCP server - MCP Inspector, in-memory pipe integration tests, testing the real Program.cs, WebApplicationFactory for HTTP, contract/snapshot tests, evals. Triggers on - mcp inspector, test mcp server, StreamServerTransport pipe, ClientServerTestBase, tools/list snapshot, mcp integration test, debug stdio.
---

# Testing & Debugging .NET MCP Servers

## MCP Inspector

- Interactive: `npx @modelcontextprotocol/inspector dotnet run --project ./src/MyServer` — UI :6274, proxy :6277; stdio + streamable-http + SSE.
- CI/scripting: `npx @modelcontextprotocol/inspector --cli https://host/mcp --transport http --method tools/list` (custom headers supported; JSON output).
- Windows/.NET alternative: `devproxy stdio dotnet run --project ...` — proxies STDIN/STDOUT/STDERR into Chrome DevTools (STDIN=requests, STDOUT=200, STDERR=500), can mock responses and inject latency.
- Raw smoke test (automate it): pipe one `initialize` request in, `> out 2> err` — stdout must be **exactly one** well-formed JSON object; anything else is the stdout-pollution bug.

## In-memory integration tests (the standard harness — no process, no network)

SDK's own pattern (`ClientServerTestBase` over `System.IO.Pipelines`):

```csharp
public class MyToolTests : ClientServerTestBase
{
    protected override void ConfigureServices(ServiceCollection services, IMcpServerBuilder builder)
    {
        builder.WithTools<MyTools>();
        services.AddSingleton<IMyService, FakeMyService>();   // full DI — inject fakes
    }

    [Fact]
    public async Task ListOrders_should_return_orders()
    {
        await using var client = await CreateMcpClientForServer();
        var result = await client.CallToolAsync("list_orders", new() { ["status"] = "active" });
        result.IsError.Should().NotBe(true);
    }
}
```

Manual wiring without the base class: two `Pipe`s; server `.WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream())` + `Server.RunAsync(ct)`; client `McpClient.CreateAsync(new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()))`. Milliseconds per test; sampling/elicitation/roots work over the bidirectional pipe.

## Testing the REAL Program.cs (excel-mcp pattern)

Expose `ConfigureTestTransport(Pipe, Pipe)` / `ResetTestTransport()` on `Program` (lock + generation counter so leaked state fails fast); inside `Main`, substitute `WithStreamServerTransport(testPipes…)` for `WithStdioServerTransport()`. Tests exercise the production host — logging config, DI, shutdown — not a rebuilt lookalike. Pair with an `IAsyncLifetime` base that tracks sessions and enforces timeouts.

## HTTP servers

`WebApplicationFactory<Program>` (add `public partial class Program {}`), POST raw JSON-RPC `initialize` then `tools/call`, assert on the response body; test the health endpoint too.

## Contract & behavioural tests to include

- **stdout purity** (stdio) — see smoke test above.
- **`tools/list` snapshot test** — fails CI when a schema/description changes unintentionally; this is your version gate against silent breaking changes (silent renames break every connected agent).
- Per tool: one success case, one empty/error case, 1–3 edge cases; assert `isError: true` paths return actionable text.
- Output conforms to declared `outputSchema`.
- **Cancellation**: cancel `CallToolAsync`'s token; assert `OperationCanceledException` propagates and cleanup ran.
- Client-compat schema tests if you target multiple hosts (excel-mcp keeps Gemini-specific schema tests — different clients choke on different JSON-schema constructs).
- Command-pattern servers (Azure MCP style): unit-test per command via `Parser.Parse([...])` + mocked service, deserialising `response.Results` into the DTO.

## Evals

Track not just accuracy but total runtime, number of tool calls, token consumption, and tool-error counts. Redundant calls ⇒ fix pagination; parameter errors ⇒ fix descriptions. (`mehrandvd/skunit` exists for semantic assertions against `IChatClient`/MCP.)

## Debugging quick hits

- Crash logs vanish → stderr buffered by a custom logger; `AutoFlush` file loggers.
- "Works in `dotnet run`, fails from client" → cwd assumption; anchor on `AppContext.BaseDirectory`.
- First-call timeout → eager init; lazy-init connections.
- OTel: `AddSource("Experimental.ModelContextProtocol")` + `AddMeter("Experimental.ModelContextProtocol")` — failed calls surface as activities with `ActivityStatusCode.Error` + `rpc.jsonrpc.error_code`; trace context propagates via `_meta.traceparent`.

## Related skills

`mcp-server-setup` (stdio hygiene), `mcp-troubleshooting` (symptom index), `mcp-project-structure` (test project layout).
