---
name: mcp-prompts-resources
description: Use when adding MCP prompts or resources to a .NET server, or deciding between tools vs resources vs prompts - McpServerPromptType, McpServerResourceType, URI templates, subscriptions, completion, server instructions. Triggers on - mcp prompt, mcp resource, resource template, UriTemplate, resource subscription, ServerInstructions, tools vs resources.
---

# MCP Prompts & Resources in .NET

## Choosing the right primitive

- **Tools** = *model-controlled* actions. **Resources** = *application-controlled* read-only context (URIs + MIME types). **Prompts** = *user-controlled* templates (slash commands / command palettes).
- Don't expose passive reference data as a tool if the host supports resources; don't make the model "discover" a canned workflow that belongs in a prompt. (Caveat: many hosts still surface only tools — check your target clients before moving data to resources.)
- **`ServerInstructions`** (`McpServerOptions`) is sent at initialise: put cross-tool workflow guidance there (reading order, "consult search_docs before answering") — never duplicate individual tool descriptions. Datadog uses instructions to steer agents to a RAG docs tool instead of bloating descriptions.

## Prompts

```csharp
[McpServerPromptType]
public class ReviewPrompts
{
    [McpServerPrompt(Name = "review_code"), Description("Reviews code for quality and style")]
    public static ChatMessage Review([Description("The code to review")] string code) =>
        new(ChatRole.User, $"Review this code:\n{code}");
}
// registration: .WithPrompts<ReviewPrompts>() or .WithPromptsFromAssembly()
```

- Return `ChatMessage` / `IEnumerable<ChatMessage>` (Microsoft.Extensions.AI — `TextContent`/`DataContent` auto-mapped), or `PromptMessage` for protocol types (`PromptMessage { Role = Role.User, Content = new EmbeddedResourceBlock(...) }`).
- Same special parameters as tools (McpServer, DI services, CancellationToken).
- Generation pattern worth stealing (excel-mcp): MSBuild/source-gen prompts from `skills/*.md` markdown so playbooks live as reviewable docs, not string literals.
- Client side: `ListPromptsAsync()` → `McpClientPrompt`; `GetPromptAsync(name, args)` → `.Messages`. `PromptListChangedNotification` mirrors tools.

## Resources

```csharp
[McpServerResourceType]
public class DocResources
{
    // Direct resource — fixed URI, appears in resources/list
    [McpServerResource(UriTemplate = "config://app/settings", Name = "App settings", MimeType = "application/json")]
    public static string Settings() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "settings.json"));

    // Template resource — RFC 6570; listed via resources/templates/list; params bind template variables
    [McpServerResource(UriTemplate = "docs://articles/{id}", Name = "Article")]
    public static async Task<TextResourceContents> Article(IDocStore store, string id, CancellationToken ct)
    {
        var doc = await store.GetAsync(id, ct) ?? throw new McpException($"Article '{id}' not found");
        return new TextResourceContents { Uri = $"docs://articles/{id}", MimeType = "text/markdown", Text = doc.Body };
    }
}
// registration: .WithResources<DocResources>() or .WithResourcesFromAssembly()
```

- Return `string`, `TextResourceContents`, `BlobResourceContents.FromBytes(data, uri, mime)`, or `ResourceContents`. Throw `McpException` for not-found.
- Publish entity JSON Schemas as resources so agents produce valid JSON Patch edits first try (token-efficient edit pattern).

## Subscriptions (stateful transports only)

- Client: `await client.SubscribeToResourceAsync(uri, handler)` → `IAsyncDisposable`.
- Server: `.WithSubscribeToResourcesHandler((ctx, ct) => ...)` / `.WithUnsubscribeFromResourcesHandler(...)`, then push `NotificationMethods.ResourceUpdatedNotification` with `ResourceUpdatedNotificationParams { Uri = ... }`. `ResourceListChangedNotification` for list changes.
- Stateless HTTP: no subscriptions, no unsolicited notifications (GET returns 405 by design). Choose stateful only if you need push — see `mcp-http-hosting-state`.

## Completion

Auto-complete prompt arguments and resource-template parameters via the completion capability — register with `McpServerHandlers`' `WithCompleteHandler` (an `AddCompleteFilter` also exists for middleware). Client resolves via `client.Completion` APIs.

## Related skills

`mcp-tools-design`, `mcp-advanced-capabilities` (notifications), `mcp-http-hosting-state` (why subscriptions need stateful).
