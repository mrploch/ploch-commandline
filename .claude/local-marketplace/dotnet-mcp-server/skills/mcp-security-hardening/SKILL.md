---
name: mcp-security-hardening
description: Use when reviewing or hardening an MCP server for security - tool poisoning, prompt injection, command injection, path traversal, SSRF, session hijacking, DNS rebinding, confused deputy, secrets leakage, rate limiting, supply chain. Triggers on - mcp security, tool poisoning, prompt injection mcp, confused deputy, DNS rebinding, session hijacking, SSRF, mcp secrets, rate limit mcp.
---

# Security Hardening for .NET MCP Servers

## Threat model quick reference

| Threat | Mitigation (server side unless noted) |
|---|---|
| Token audience abuse | `ValidateAudience = true` + `ValidAudience` = canonical server URI; accept only tokens minted for this server |
| Token passthrough | Forbidden — mint separate downstream tokens (see `mcp-auth-downstream`) |
| Confused deputy (OAuth-proxy servers) | Per-client consent **before** forwarding to the third-party AS; exact-match redirect URIs; `__Host-` Secure/HttpOnly/SameSite cookies bound to client_id; single-use post-consent `state` |
| Session hijacking | Sessions are NEVER auth — validate the bearer token on every request; CSPRNG session IDs; key state `<user_id>:<session_id>` |
| DNS rebinding (local HTTP) | Validate `Origin` (403), bind 127.0.0.1, set `AllowedHosts`; prefer stdio locally |
| Tool poisoning / rug pull | Never interpolate untrusted content into your own tool descriptions; immutable definitions per release; publish via registry with verified namespace. (Consumers: pin/hash tool definitions; MCPTox measured up to 72.8% attack success) |
| Indirect prompt injection via tool **results** | Constrain to structured output conforming to `outputSchema`; delimit/datamark relayed third-party text; enforce restrictions at the execution layer, never via system-prompt rules; keep privileged tools out of contexts consuming untrusted content |
| Over-broad scopes | Minimal `ScopesSupported`; per-tool scopes; step-up via `403 insufficient_scope`; no `mcp.all` |
| Elicitation phishing | Verify user completing a URL elicitation == user who triggered it (`sub` match); no secrets in URLs |

## Input-side hardening (spec: servers MUST validate all tool inputs)

- **Command injection**: `ProcessStartInfo { UseShellExecute = false }` + `ArgumentList` (never string-concatenated `Arguments` from model input); executable allowlist; redirect child stdout (protocol hygiene too).
- **Path traversal**: `Path.GetFullPath` then verify the result is under an allowed root; reject symlink escapes; expose explicit roots rather than accepting arbitrary paths.
- **SSRF (URL-fetching tools, OAuth metadata)**: HTTPS-only; block private/link-local ranges incl. `169.254.169.254` using a vetted library (encoding tricks defeat naive IP parsing); validate every redirect hop; consider an egress proxy; pin DNS between check and use.
- Schema-level: enums + `maxLength`; validate before any side effect (data annotations / FluentValidation).

## Output-side hardening

- Sanitise outputs (spec MUST): strip secrets/PII before returning; error messages must not echo connection strings; never log full tool inputs at info level — tool-call logs land in client transcripts (Claude Code saves stderr).
- Structured output (`outputSchema` conformance) doubles as an injection-surface constraint.

## Abuse & blast radius

- **Inbound rate limiting**: ASP.NET Core `AddRateLimiter` + `UseRateLimiter()` on the MCP endpoint, partitioned by user/API key and optionally tool name; structured error with `retry_after` so the agent backs off. "A runaway agent calling `send_email` 10,000×/min is a disaster; a rate-limited one is an incident."
- Destructive tools: honest `Destructive`/`ReadOnly` annotations + "WRITE OPERATION" in description; consider elicitation/human-approval gates and per-tool kill switches.
- Append-only audit log of every tool call (name, caller, params hash, latency, outcome).

## Supply chain

- Pin NuGet versions; secret/dependency/CodeQL scanning in CI.
- Treat your **own tool descriptions as reviewed supply-chain assets** — code-review every change (CSA guidance 2026-07).
- Publish via the official registry with verified namespace (`io.github.*` or DNS-verified) so consumers can authenticate provenance.
- Consumer-side awareness (for the org): IDEs auto-launch repo-defined MCP servers (CurXecute CVE-2025-54135, MCPoison CVE-2025-54136, Amazon Q CVE-2026-12957/8; Miasma worm) — review `.cursor/mcp.json`-style files in PRs like code; sandbox local servers; allowlist approved servers.

## Hardening checklist (condensed)

- [ ] stdio: all logging to stderr; no library writes stdout
- [ ] HTTP: TLS at ingress; `Origin` validated; loopback bind for local; `Stateless = true` unless sessions needed
- [ ] OAuth RS per spec: PRM + audience validation; no token passthrough; scopes minimal + per-tool
- [ ] Inputs validated before side effects; shell-outs via ArgumentList; paths canonicalised; URL fetches SSRF-guarded
- [ ] Outputs sanitised; secrets redacted from logs/errors
- [ ] Rate limiting per tool/user; audit log; kill switches for destructive tools
- [ ] Dependencies pinned + scanned; tool descriptions code-reviewed; registry-verified namespace

## Related skills

`mcp-auth-inbound`, `mcp-auth-downstream`, `mcp-http-hosting-state`, `mcp-tools-design` (error/response design).
