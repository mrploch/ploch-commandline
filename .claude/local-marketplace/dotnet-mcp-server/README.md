# dotnet-mcp-server — Claude Code plugin

Knowledge-base plugin for building **MCP servers in .NET** with the official `ModelContextProtocol` C# SDK. Compiled 2026-07-25 from the SDK docs/repo (v2.0.0-rc.1, protocol `2026-07-28`), the MCP specification (revision 2025-11-25), Microsoft Learn, and production open-source servers (microsoft/mcp, Azure MCP, sbroenne/mcp-server-excel, petabridge/memorizer).

## Skills

| Skill | Covers |
|---|---|
| `mcp-server-setup` | Packages/versions, minimal stdio + HTTP servers, hybrid host, stdio hygiene |
| `mcp-tools-design` | Tool attributes, DI, return types, structured output, errors, agent-first design |
| `mcp-prompts-resources` | Prompts, resources, URI templates, subscriptions, completion, instructions |
| `mcp-advanced-capabilities` | Progress, cancellation, client logging, sampling/MRTR, elicitation, Tasks, filters |
| `mcp-http-hosting-state` | Stateless vs stateful, sessions, scaling, DI scopes, legacy SSE |
| `mcp-auth-inbound` | OAuth 2.1 resource server, PRM, JwtBearer + AddMcp, Entra ID, APIM, API keys |
| `mcp-auth-downstream` | OBO, client credentials, managed identity, secrets, per-user creds, token-passthrough prohibition |
| `mcp-security-hardening` | Threat model, injection defences, rate limiting, supply chain, checklist |
| `mcp-testing-debugging` | Inspector, in-memory pipe tests, contract/snapshot tests, evals |
| `mcp-packaging-distribution` | NuGet McpServer type, dnx, server.json/registry, containers, AOT |
| `mcp-client-dotnet` | McpClient, transports, Microsoft.Extensions.AI integration |
| `mcp-project-structure` | Repo blueprints (attribute façade / command-pattern monorepo), lifecycle |
| `mcp-troubleshooting` | Symptom-indexed diagnosis table |

## Installation

This plugin lives in the `mrploch-local` directory marketplace (`C:\DevNet\my\mrploch\.claude\local-marketplace`), registered with Claude Code as a `directory` source. It is enabled for the whole mrploch workspace via `C:\DevNet\my\mrploch\.claude\settings.json`:

```json
"enabledPlugins": { "dotnet-mcp-server@mrploch-local": true }
```

## Deploying to another system

1. Copy the `local-marketplace` directory (or clone the repo that contains it).
2. Register it: `claude plugin marketplace add <path-to-local-marketplace>`
3. Enable: `claude plugin install dotnet-mcp-server@mrploch-local` — or add the `enabledPlugins` entry above to the target machine's user (`~/.claude/settings.json`) or project settings.

Alternatively push the marketplace directory to a GitHub repo and register it with `"source": "github"` in `extraKnownMarketplaces`.

## Maintenance

The SDK moves fast (v2.0.0 stable lands ~2026-07-28). When updating skills, re-verify against: https://github.com/modelcontextprotocol/csharp-sdk (docs/concepts), https://modelcontextprotocol.io/specification/latest, and bump `version` in `.claude-plugin/plugin.json` + the marketplace entry.
