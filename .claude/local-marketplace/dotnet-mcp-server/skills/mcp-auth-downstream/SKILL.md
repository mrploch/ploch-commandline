---
name: mcp-auth-downstream
description: Use when an MCP server must call downstream services that need authentication - On-Behalf-Of flow, client credentials, managed identity, API keys, secret storage, per-user credentials, elicitation for credential collection, token passthrough anti-pattern. Triggers on - mcp downstream api, on-behalf-of OBO, token exchange RFC 8693, managed identity mcp, IDownstreamApi, token passthrough, per-user api key, mcp call graph.
---

# Downstream Authentication from .NET MCP Servers

## The prohibition first: token passthrough is forbidden

Spec: "MCP servers MUST NOT accept any tokens that were not explicitly issued for the MCP server" and "The MCP server MUST NOT pass through the token it received from the MCP client" to upstream APIs. Forwarding the client's token (a) bypasses downstream rate limiting/validation keyed on audience, (b) destroys audit trails, (c) creates the confused-deputy condition, (d) widens stolen-token blast radius. Correct model: **validate the inbound token (audience = this server), then mint a separate token for the downstream audience** via one of the flows below.

## Decision table

| Scenario | Approach |
|---|---|
| stdio (local) server → any API | Credentials from environment / user-secrets / OS keychain; managed identity if on Azure compute |
| HTTP server, act **as the signed-in user** vs Entra-protected API (Graph, your APIs) | **On-Behalf-Of** via Microsoft.Identity.Web |
| Same, non-Entra IdP | **RFC 8693 token exchange** at the IdP (Keycloak/Duende support it); else URL-mode elicitation account-linking |
| App-only call to Entra-protected API | **Client credentials** (cert/federated credential > secret) |
| App-only call to Azure resources | **Managed identity** (`DefaultAzureCredential`) — no secrets at all |
| Downstream API-key service, one shared key | Key Vault (+ managed identity) → `IConfiguration` → named `HttpClient` |
| Downstream API-key service, per-user keys | **URL-mode elicitation** to collect; encrypt & store server-side keyed by verified `sub` |

## On-Behalf-Of (delegated) — Microsoft.Identity.Web

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("PartnerApi", builder.Configuration.GetSection("PartnerApi"))
    .AddInMemoryTokenCaches();          // use a distributed cache for scaled-out/stateless servers

[McpServerToolType]
public sealed class PartnerTools(IDownstreamApi downstreamApi)
{
    [McpServerTool]
    public async Task<UserData?> GetUserData() =>
        await downstreamApi.GetForUserAsync<UserData>("PartnerApi", "api/users/me");
        // internally: OBO exchange of the inbound bearer token for a PartnerApi-audience token
}
```

- Requirements: server registered as confidential client (cert/secret/federated identity credential); downstream API permission granted + consent. Handle `MicrosoftIdentityWebChallengeUserException` (consent/step-up needed) — surface as an MCP error or URL-mode elicitation.
- Lower-level: MSAL `AcquireTokenOnBehalfOf(scopes, new UserAssertion(inboundToken))`; Azure SDK/Graph: `OnBehalfOfCredential`.
- OBO needs the inbound token per request — fits stateless MCP naturally; long-running background work needs the long-running-OBO session pattern.

## App-only flows

- **Client credentials**: `IDownstreamApi.GetForAppAsync<T>("MyApi")` with `.default` scope, or `ITokenAcquirer.GetTokenForAppAsync`. Prefer certificates/federated identity credentials over client secrets.
- **Managed identity**: `DefaultAzureCredential` for Key Vault/Storage/etc.; SQL: `Authentication=Active Directory Managed Identity` in the connection string. Microsoft.Identity.Web supports MI as the client credential for Entra-protected APIs.

## API keys & secret management

- Never hardcode. Local dev: user-secrets (`UserSecretsId` in csproj) or env vars; production: Key Vault via managed identity → `IConfiguration` → options-bound, injected into tools; attach via named `HttpClient` (`AddHttpClient("Api", c => c.DefaultRequestHeaders.Add("X-Api-Key", …))`).
- **Per-user credentials**: store server-side, encrypted (Data Protection / Key Vault), **keyed by the verified `sub` claim from the validated MCP token — never by `Mcp-Session-Id`** (sessions are not authentication). Gateway alternative: APIM credential manager holds per-user connections and injects tokens by policy.
- Redact keys from logs and error text — tool-call output lands in client transcripts.

## Collecting credentials via elicitation

- **Form-mode elicitation MUST NOT request secrets** (spec) — form data transits the client and the LLM context.
- **URL mode** is the sanctioned path: server sends `mode:"url"` + `elicitationId` pointing at a server-hosted HTTPS page where the user enters the key or completes a third-party OAuth flow; on tool calls throw `UrlElicitationRequiredException` (`-32042`), complete via `notifications/elicitation/complete`, client retries.
- Bind the elicitation to the authenticated user and **verify the user completing the URL flow == the user who triggered it** (`sub` from MCP token vs page session — anti-phishing). Never put credentials or pre-authed capability URLs in the elicitation URL. Third-party tokens obtained this way are stored server-side and MUST NOT be sent to the MCP client.

## Related skills

`mcp-auth-inbound`, `mcp-security-hardening` (confused deputy, audience validation), `mcp-advanced-capabilities` (elicitation mechanics).
