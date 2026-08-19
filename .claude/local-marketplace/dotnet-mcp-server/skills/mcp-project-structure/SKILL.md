---
name: mcp-project-structure
description: Use when structuring a .NET MCP server repository or reviewing its architecture - layering (tool facades over services), attribute-based vs command-pattern blueprints, hybrid CLI+MCP hosts, config binding, shutdown cleanup. Triggers on - mcp project structure, mcp solution layout, tool class organization, IAreaSetup, mcp monorepo, mcp architecture.
---

# .NET MCP Server Project Structure (distilled from microsoft/mcp, Azure MCP, excel-mcp, memorizer)

## Universal rules

- **Tool classes are façades; logic lives in injected services.** The MCP layer stays mechanical — every surveyed production server does this. Interface-front tools for mockability; excel-mcp even source-generates `[McpServerToolType]` classes from annotated Core service interfaces.
- Domain/Core project has **no MCP dependency** — reusable from a CLI or API front-end.
- Ship the hybrid stdio+HTTP host from day one (see `mcp-server-setup`) — "host this remotely" is the most recurring issue class in stdio-first servers.

## Blueprint A — attribute-based, small/medium server

```
<repo>/
  src/
    <Product>.McpServer/            # host: Program.cs, transport wiring, telemetry
      .mcp/server.json              # embedded registry manifest (packed into nupkg)
      Tools/<Domain>Tool.cs         # [McpServerToolType] façades, thin
      Prompts/                      # [McpServerPromptType]
    <Product>.Core/                 # domain services — ALL logic here, no MCP reference
    <Product>.CLI/                  # optional second front-end reusing Core
  tests/
    <Product>.Core.Tests/           # plain unit tests
    <Product>.McpServer.Tests/
      Unit/                         # logging config, transport lifecycle
      Integration/Tools/            # real server over in-memory pipes + real McpClient
  Directory.Build.props / Directory.Packages.props / global.json / Dockerfile
```

## Blueprint B — command-pattern monorepo at scale (Azure MCP / microsoft/mcp)

```
<repo>/
  core/src/<Product>.Mcp.Core/      # CommandFactory, CommandGroup, transports, telemetry, server start
  areas/<service>/                  # vertical slice per domain
    src/<Product>.<Service>/
      <Service>Setup.cs             # IAreaSetup: DI + command-tree registration
      Commands/<Noun>/<NounVerb>Command.cs   # Name/Description/Title + ToolMetadata{ReadOnly,Destructive}
      Options/                      # System.CommandLine-bound option records
      Services/                     # the ONLY place SDK/API clients live
      Commands/<Service>JsonContext.cs       # STJ source-gen context per area (trim/AOT)
    tests/  *.UnitTests/ (1:1 with Commands) + *.LiveTests/ + test-resources.bicep
  servers/<Name>.Mcp.Server/        # thin host registering IAreaSetup[]; server.json; mcpb/; vscode/
  eng/                              # shared build/publish pipeline
```

- Every tool doubles as a CLI command (`azmcp storage blob list`) routed into the same `ExecuteAsync(CommandContext, ParseResult)` — free manual verification and free unit tests via `Parser.Parse([...])`.
- `IAreaSetup` example: `ConfigureServices` registers `IStorageService`; `RegisterCommands` builds `CommandGroup("storage") → ("blob") → AddCommand("list", new BlobListCommand(...))`.
- **Dual-DI-container trap**: in CLI+server hybrids, `ConfigureServices` feeds both the command-picking container AND the transport host container built inside the server-start command — a stdio-only registration can silently miss the HTTP host. microsoft/mcp warns about this in Program.cs doc-comments.

## Config binding

- `AddEnvironmentVariables("MYSERVER_")` prefix + options binding (`Configuration.GetSection("Cors").Get<CorsSettings>()`) — memorizer pattern.
- Embed `appsettings.json` as `EmbeddedResource` for single-file publishing (microsoft/mcp).
- stdio servers: rebuild config sources with `reloadOnChange: false` — file watchers caused ~85% CPU in excel-mcp.

## Lifecycle & cleanup

- **Shutdown without cleanup silently loses work**: put session auto-save/resource cleanup in a `finally` around the host run (excel-mcp: without it, client disconnect discards all unsaved Excel work). Monitor stdin for client disappearance (`StdinPipeMonitor`) to trigger graceful shutdown.
- Treat `OperationCanceledException` at top level as graceful (exit 0).
- SDK awaits in-flight handlers on disposal; ASP.NET Core shutdown bounded by `HostOptions.ShutdownTimeout` (30 s default).

## Observability

- OTel built in: `AddSource("Experimental.ModelContextProtocol")` + `AddMeter("Experimental.ModelContextProtocol")` (GenAI MCP semconv; name experimental). App Insights (excel-mcp) or OTLP (memorizer).
- stderr console logging even in HTTP mode is a harmless good habit (memorizer does it).

## Related skills

`mcp-server-setup`, `mcp-tools-design`, `mcp-testing-debugging`, `mcp-packaging-distribution`.
