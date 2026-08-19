---
name: mcp-http-hosting-state
description: Use when hosting an MCP server over Streamable HTTP in ASP.NET Core - stateless vs stateful sessions, scaling out, load balancers, DI scopes per request, legacy SSE, idle timeouts, Data Protection keys. Triggers on - Stateless true, Mcp-Session-Id, 404 session not found, sticky sessions, WithHttpTransport options, SSE endpoint, session timeout, MCP scale out.
---

# MCP Streamable HTTP Hosting & State (.NET SDK v2)

## Stateless vs stateful — the central decision

**`Stateless = true` is the v2 default (v1 defaulted stateful!) and the right choice unless you provably need push.** Set it explicitly either way.

| | Stateless (default) | Stateful |
|---|---|---|
| Scale-out | any topology — LB, serverless, Functions | sticky sessions required |
| Deploys/restarts | seamless | all in-memory sessions die |
| Sampling/elicitation (classic) | ✗ (`InvalidOperationException`) — use MRTR / `UrlElicitationRequiredException` | ✓ |
| Unsolicited notifications, subscriptions | ✗ (GET returns 405 by design) | ✓ (still dropped if client never opens GET stream) |
| Progress notifications | ✓ (inline with request) | ✓ |
| Tasks extension | ✓ (shared store) | ✓ |
| Per-client isolated state | ✗ — requests indistinguishable | ✓ |

## HttpServerTransportOptions (v2)

- `Stateless` (default true), `IdleTimeout` (2 h), `MaxIdleSessionCount` (10,000 — monitor memory on busy stateful servers), `ConfigureSessionOptions` (per-session in stateful; **per-HTTP-request in stateless** — the hook for auth/header-based tool filtering with `HttpContext`), `SessionMigrationHandler`, `EventStreamStore` (SSE resumability via `Last-Event-ID`), `EnableLegacySse` (default **false**, obsolete MCP9004, requires `Stateless = false` — combining with stateless throws at startup).
- Protocol `2026-07-28` removes `initialize` and `Mcp-Session-Id`; a stateful server refuses `2026-07-28` requests with `-32022` so dual-path clients downgrade.
- Session lifecycle (stateful): created by `initialize` without a session header; ends on client `DELETE`, idle timeout, max-idle pruning, shutdown. Sessions auto-bind to the user (`sub`/`nameidentifier`/`upn` claims) → 403 on mismatch.

## Scaling out

- **Stateless**: nothing to do — any instance serves any POST. This is why it's the default.
- **Stateful behind a load balancer**: symptom `404 Session not found` — sessions are per-instance memory. Sticky routing must key on the **response** `Mcp-Session-Id` header (the first request has none); deploys still disconnect everyone. Prefer redesigning to stateless.
- **Data Protection**: multi-instance ASP.NET Core needs a shared key ring (auto on Azure App Service; else configure explicitly) or auth cookies/anti-forgery break across instances.
- Backpressure: POST responses are held open while handlers run; HTTP/2 `MaxStreamsPerConnection` (100) naturally bounds concurrency. Legacy SSE, `EventStreamStore` + polling, and Tasks decouple handlers from the POST → unbounded; add `AddRateLimiter` when using them.

## DI scopes

- Stateful HTTP & stdio: app-level provider, `ScopeRequests = true` default → **fresh DI scope per handler invocation** (scoped services per tool call).
- **Stateless HTTP: uses `HttpContext.RequestServices`; `ScopeRequests` forced false** — a tool call behaves exactly like a minimal-API endpoint; middleware-set scoped state is visible in the tool.
- Concurrency: tool calls run concurrently (parallel HTTP requests; pipelined stdio). Keep tool methods stateless; per-request state in scoped services; shared caches via `ConcurrentDictionary`; **never key user data off static fields**.

## Legacy SSE clients

Upgrading the server to v2 breaks SSE-only clients unless `EnableLegacySse = true` + `Stateless = false`. Client migration: point `HttpClientTransport` at the root `MapMcp` URL (`TransportMode = HttpTransportMode.AutoDetect` handles the rest) — not `/sse`.

## Security must-dos for HTTP hosting

Validate `Origin` (403 on invalid — DNS rebinding), bind local servers to 127.0.0.1, set `AllowedHosts`, TLS at ingress in production, authenticate everything (see `mcp-auth-inbound`). Session IDs are **never** authentication — verify the bearer token on every request; key shared state `<user_id>:<session_id>`.

## Related skills

`mcp-server-setup`, `mcp-advanced-capabilities` (MRTR/Tasks for stateless interactivity), `mcp-auth-inbound`, `mcp-security-hardening`.
