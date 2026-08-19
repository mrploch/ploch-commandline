---
name: mcp-tools-design
description: Use when writing or reviewing MCP tools in .NET - McpServerTool attributes, parameter binding, DI into tool classes, return types and structured output, error handling, tool annotations, and agent-first design (naming, descriptions, response token budgets, idempotency). Triggers on - McpServerToolType, McpServerTool, tool description, structured output, outputSchema, IsError, McpException, tool naming, tool annotations.
---

# MCP Tool Design in .NET (SDK v2, mid-2026)

## Defining tools

```csharp
[McpServerToolType]
public class OrderTools(IOrderService orders, ILogger<OrderTools> logger)  // instance class: ctor DI works
{
    [McpServerTool(Name = "list_orders", Title = "List orders",
                   ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists orders for the authenticated customer, optionally filtered by status and date range. Use when the user asks about order history. Returns id, status, total and created date per order.")]
    public async Task<OrderSummary[]> ListOrders(
        [Description("Filter by status")] OrderStatus? status,
        [Description("ISO 8601 date lower bound")] DateOnly? from,
        CancellationToken ct)
        => await orders.ListAsync(status, from, ct);
}
```

- Tool classes are **façades — logic lives in injected services** (universal pattern in microsoft/mcp, Azure MCP, excel-mcp). Interface-front the tool class for mockability.
- Attribute properties and their defaults: `Destructive` (true), `Idempotent` (false), `OpenWorld` (true), `ReadOnly` (false), `UseStructuredContent` (false), `Name`, `Title`, `IconSource`. Set them honestly — clients MUST treat them as untrusted hints, so also state "This is a WRITE OPERATION" in the description text of destructive tools.
- Other definition mechanisms: `McpServerTool.Create(delegate)`, derive from `McpServerTool`/`DelegatingMcpServerTool`, custom handlers/filters.

## Parameter binding

- Parameters deserialise from JSON args; `[Description]` populates the generated JSON Schema (2020-12); C# defaults become schema defaults.
- **Auto-resolved special parameters** (never appear in the schema): `McpServer`, `IProgress<ProgressNotificationValue>`, `CancellationToken`, `ClaimsPrincipal`, `RequestContext<CallToolRequestParams>`, and **any DI-registered service**.
- Validate and default server-side — some clients omit optional args entirely (real Azure MCP bug class #742).
- `[McpHeader]` (protocol 2026-07-28): mirrors a primitive parameter as `Mcp-Param-{Name}` HTTP header for L7 routing; server validates header==body.

## Return types

| Return | Becomes |
|---|---|
| `string` | `TextContentBlock` |
| `ContentBlock` / `IEnumerable<ContentBlock>` | passed through — `ImageContentBlock.FromBytes(bytes, "image/png")`, `AudioContentBlock.FromBytes`, `EmbeddedResourceBlock` |
| `AIContent` (M.E.AI) | converted; `DataContent` maps by MIME; `ErrorContent` sets `IsError` |
| `CallToolResult` | verbatim (full control) |
| any other object | JSON text; with `UseStructuredContent = true` also `structuredContent` + generated `outputSchema` |

Structured output is **opt-in** (`UseStructuredContent = true`). Content blocks accept `Annotations { Audience = [Role.Assistant], Priority = 0.3f }`.

## Error handling — two planes

- **Tool execution errors** → `CallToolResult.IsError = true` (LLM-visible, recoverable). Any thrown exception is caught: `McpException` messages ARE shown to the model; any other exception produces a generic "An error occurred invoking '{tool}'." (no detail leak — by design). Throw `McpException` when you *want* the model to see the message.
- **Protocol errors** → `McpProtocolException` (v2; e.g. `new McpProtocolException("Missing input", McpErrorCode.InvalidParams)`) becomes a JSON-RPC error, not a tool result. `OperationCanceledException` propagates as cancellation.
- Write errors as steering input: `"unknown field 'stauts' — did you mean 'status'?"` beats `"invalid query"`. Include `retry_after` in rate-limit errors so agents back off.

## Agent-first design rules (evidence-backed)

- **Few workflow tools, not one per API endpoint**: consolidate (`schedule_event`, not `list_users`+`list_events`+`create_event`). Budget **5–15 tools per server, ≤8 parameters per tool**; at ~200 tools models measurably pick wrong tools.
- **Naming**: `verb_noun` snake_case (`list_orders`); no versions in names (`_v2` reads as a different concept); namespace with a consistent service prefix to avoid cross-server collisions; unambiguous param names (`user_id`, not `user`).
- **Descriptions are the firing predicate** — answer: what it does, when to use it, what it returns. Add negative guidance ("Do NOT use for bulk operations over 100 records"). Teams report 40–60% fewer misrouted calls after rewrites. Prefer **schema over prose**: enums prevent wrong values at protocol level; report `defaults_applied` in responses.
- **Response token budgets**: everything returned is re-read every turn; Claude Code caps tool responses at 25k tokens. Cap server-side (`truncated: true` + a hint like "add a WHERE clause"); offer `response_format: "concise" | "detailed"` (Anthropic measured ~⅔ reduction); CSV/TSV ≈ half the tokens of JSON for tables; paginate **by token budget, not record count**; move bytes out of band (presigned URL, not file content); layer reads (index → detail → body) and deliberately omit `get_full_report`.
- **Idempotency**: the model retries — every write tool accepts an `idempotency_key` or dedupes on natural keys.
- **Composability**: return names alongside IDs; same ID formats in and out; explicit pagination cursors.
- List-changed: `await server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, new ToolListChangedNotificationParams());` (needs stateful HTTP or stdio; silently dropped if the client never opened the GET stream).

## Related skills

`mcp-prompts-resources` (when a tool should be a resource/prompt instead), `mcp-advanced-capabilities` (progress/cancellation in tools), `mcp-security-hardening` (input validation, output sanitisation).
