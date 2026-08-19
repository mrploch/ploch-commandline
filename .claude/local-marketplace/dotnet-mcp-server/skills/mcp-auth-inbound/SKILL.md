---
name: mcp-auth-inbound
description: Use when securing an MCP server (inbound authentication) in .NET - OAuth 2.1 resource server model, Protected Resource Metadata, AddMcp authentication scheme, JWT bearer, Microsoft Entra ID, Azure APIM gateway, API keys, stdio credential model. Triggers on - mcp oauth, protected resource metadata, McpAuthenticationDefaults, AddMcp, RequireAuthorization MapMcp, Entra ID mcp, WWW-Authenticate resource_metadata, mcp api key.
---

# Inbound Authentication for .NET MCP Servers

## The model (spec revision 2025-11-25)

- Authorization is **optional** and applies to **HTTP transports only**. **stdio servers SHOULD NOT use OAuth** — they take credentials from the environment (env vars / user-secrets / OS keychain); the process boundary is the security boundary.
- Roles: MCP server = **OAuth 2.1 resource server**; the authorization server (Entra ID, Keycloak, Auth0…) is external. The server publishes **RFC 9728 Protected Resource Metadata** (`/.well-known/oauth-protected-resource`) and challenges with `WWW-Authenticate: Bearer resource_metadata="…"` on 401.
- Clients: PKCE S256 mandatory; `resource` parameter (RFC 8707) sent in both authorization and token requests = the server's canonical URI. Registration priority (2025-11-25): pre-registered → **CIMD** (Client ID Metadata Documents, now preferred) → Dynamic Client Registration (fallback) → prompt.
- Server MUST validate audience — accept **only** tokens issued for this server — and MUST NOT accept or transit any other token (see `mcp-auth-downstream` for the passthrough prohibition). Step-up: `403` + `WWW-Authenticate: Bearer error="insufficient_scope", scope="…"`.

## C# SDK wiring (ProtectedMcpServer sample, current API)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

builder.Services.AddAuthentication(o =>
{
    o.DefaultChallengeScheme    = McpAuthenticationDefaults.AuthenticationScheme; // MCP scheme issues RFC 9728 challenge
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;         // JWT bearer validates
})
.AddJwtBearer(o =>
{
    o.Authority = authorizationServerUrl;
    o.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidAudience = serverUrl,          // canonical MCP server URI — the RFC 8707 audience binding
        ValidIssuer = authorizationServerUrl,
    };
})
.AddMcp(o =>                                // serves /.well-known/oauth-protected-resource + 401 challenge
{
    o.ResourceMetadata = new()
    {
        AuthorizationServers = { authorizationServerUrl },
        ScopesSupported = ["mcp:tools"],    // minimal baseline — scope minimisation
    };
});

builder.Services.AddAuthorization();
builder.Services.AddMcpServer().WithTools<MyTools>().WithHttpTransport(o => o.Stateless = true);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();        // policies/scopes work as on any endpoint
```

Browser-based clients additionally need CORS with `WithExposedHeaders(HeaderNames.WWWAuthenticate)` and `MCP-Protocol-Version` allowed. Tools can take a `ClaimsPrincipal` parameter (auto-resolved) for the caller identity.

Client side: `HttpClientTransport` with `OAuth = new() { RedirectUri, AuthorizationCallbackHandler, DynamicClientRegistration = new() { ClientName = "…" } }` — the SDK drives 401 → PRM → AS metadata → registration → PKCE; you only supply the browser handler. rc.1 tightened issuer validation (MCP9007) and PKCE advertisement.

## Microsoft Entra ID scenario

1. **Server app registration**: expose an API (`api://<server-app-id>`), define a delegated scope (e.g. `Mcp.Tools`); app roles for app-only clients.
2. **Client registrations must be pre-created — Entra does not support DCR** (or broker DCR through APIM, below). Public client + loopback redirect for desktop clients.
3. Server config: plain JwtBearer (`Authority = https://login.microsoftonline.com/{tenant}/v2.0`, `ValidAudience = api://<server-app-id>`) or **Microsoft.Identity.Web** `AddMicrosoftIdentityWebApi(config)` combined with `.AddMcp(...)` as challenge scheme. Enforce `scp`/`roles` claims via authorization policies; for user scenarios accept only delegated tokens intended for this server.
4. **PaaS shortcuts (no code)**: App Service Easy Auth + `WEBSITE_AUTH_PRM_DEFAULT_WITH_SCOPES=api://<app-id>/user_impersonation` publishes PRM; Azure Functions MCP extension one-click "Turn on MCP authentication"; Container Apps built-in auth with `--unauthenticated-client-action Return401`.

## Gateway pattern (Azure API Management)

- Inbound: subscription keys for private cases, or `validate-azure-ad-token` policy for OAuth; PRM-based samples exist (`blackchoey/remote-mcp-apim-oauth-prm`).
- **DCR/consent brokering**: `Azure-Samples/remote-mcp-apim-functions-python` implements `/authorize`, `/token`, `/register` + PKCE against Entra *in APIM policies*; the MCP client only ever sees an encrypted session key — and it implements the per-client consent page that defeats the confused-deputy attack.
- Outbound from the gateway: credential manager injects backend OAuth tokens; managed identity to backends.

## API keys / private deployments

Static bearer/API key via custom authentication handler or gateway header is acceptable for private servers (clients attach static `headers` in `mcp.json`); same downstream rules still apply. Upgrade to OAuth before exposure widens.

## Related skills

`mcp-auth-downstream` (calling other services), `mcp-security-hardening` (threat checklist), `mcp-http-hosting-state`.
