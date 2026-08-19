---
name: mcp-troubleshooting
description: Use when an MCP server misbehaves - symptom-indexed diagnosis for connection failures, hangs, 404 sessions, corrupted protocol, schema errors, high CPU, memory creep, lost work. Triggers on - mcp server not responding, mcp connection failed, parse error stdio, 404 session not found, mcp timeout, tools not listed, mcp high cpu, mcp debug.
---

# .NET MCP Server Troubleshooting (symptom → cause → fix)

| Symptom | Root cause | Fix |
|---|---|---|
| Client shows "connected" but tools never respond; parse errors; dead connection | **stdout pollution** — stray `Console.WriteLine`, default console logger (Information and below go to stdout!), library banner | `logging.ClearProviders(); logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);` (+ `Console.SetOut(Console.Error)`); smoke-test stdout purity |
| Protocol corrupts only when a tool shells out | Child process inherits parent stdout (the MCP channel) | `ProcessStartInfo.RedirectStandardOutput = true` always |
| Works in `dotnet run`, fails when launched by the client; "file not found" | Client launches with arbitrary cwd | Anchor paths on `AppContext.BaseDirectory`/env vars, never `Environment.CurrentDirectory` |
| Client times out at startup; connect-retry loops | Heavy init (DB, indexes) in startup path | Lazy-init on first tool call; keep process start light; consider Native AOT for cold start |
| `404 Session not found` behind a load balancer | Stateful sessions are per-instance memory | `Stateless = true` (v2 default); if stateful needed: sticky routing on the **response** `Mcp-Session-Id` header |
| All clients disconnected after every deploy | In-memory sessions die on restart | Stateless mode, or accept re-initialisation |
| GET returns 405 / no notifications arrive | Stateless mode has no SSE stream — by design | Choose stateful only if you need push; progress still works inline |
| Sampling/elicitation throws `InvalidOperationException` | Classic server→client requests unsupported in stateless | MRTR (`InputRequiredException`) / `UrlElicitationRequiredException`; or stateful transport |
| Auth cookies/anti-forgery break across instances | Data Protection key ring per machine | Share the key ring (auto on Azure App Service; else configure) |
| Cross-request data bleed / races | Singleton tool classes with mutable fields; concurrent calls | Stateless tool methods; scoped services for per-request state; `ConcurrentDictionary` for shared caches; never static per-user state |
| Duplicate writes after retries | Non-idempotent write tools — the model retries | `idempotency_key` param or natural-key dedupe |
| Long tool blocks; client gives up | No progress/cancellation; minutes-long synchronous work | `IProgress<ProgressNotificationValue>` + honour `CancellationToken`; job-id + polling tool or Tasks extension |
| ~85% CPU in a stdio server | Config `reloadOnChange: true` file watchers under IO storms | Rebuild config sources with `reloadOnChange: false` |
| Memory creep on stateful server | Idle sessions: default 2 h × up to 10,000 | Tune `IdleTimeout`/`MaxIdleSessionCount`; monitor idle count |
| Unsaved work lost on client disconnect | No shutdown cleanup | `finally`-block auto-save; stdin monitor → graceful shutdown; `OperationCanceledException` = exit 0 |
| Crash logs vanish | Buffered custom file logger | `AutoFlush`; stderr is unbuffered via `Console.Error` |
| Tool schema wrong for one client (e.g. Gemini) | Clients choke on different JSON-schema constructs | Client-compat schema tests; compat wrapper for the offending client |
| `double`/edge-type missing from generated schema | SDK schema-generation edge cases (known issues) | Verify generated schemas in a `tools/list` snapshot test; hand-author schema via `CallToolResult`/custom `AIFunction` if needed |
| Model picks the wrong tool / fills context before user asks | Too many tools, verbose schemas (30–50% of context in definitions) | 5–15 tools/server; lean schemas (~200 tokens/tool); consolidate; opt-in toolsets |
| One tool call injects 50k tokens | Uncapped response (DB query, file read) | Server-side caps + `truncated: true` + guidance; `concise|detailed` toggle; out-of-band URLs for bytes |
| Agent loops re-fetching same data | No caching; ambiguous pagination | Cache stable reads; explicit cursors; alert on 3+ identical calls per turn |
| SSE-only client broke after SDK v2 upgrade | Legacy SSE off by default (MCP9004) | `EnableLegacySse = true` + `Stateless = false`, or migrate client to root MapMcp URL |
| Stateful server rejects requests with `-32022` | Protocol `2026-07-28` client vs stateful server | Expected — client downgrades; go stateless for `2026-07-28` |

Diagnostic tools: MCP Inspector (`npx @modelcontextprotocol/inspector`), `devproxy stdio`, stdout-purity smoke test, OTel traces (`Experimental.ModelContextProtocol`). See `mcp-testing-debugging`.

## Related skills

`mcp-server-setup`, `mcp-http-hosting-state`, `mcp-security-hardening`, `mcp-testing-debugging`.
