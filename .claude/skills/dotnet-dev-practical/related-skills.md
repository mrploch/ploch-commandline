# Related Skills & Plugins for .NET Development

This is a navigation guide to help you find the right skill for your .NET development task. Skills are spread across personal skills, workspace skills, and installed plugins.

## Skill Map by Development Activity

### Writing C# Code

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-skills:csharp-coding-standards` | Plugin | Records, pattern matching, `Span<T>`, value objects, async patterns |
| `dotnet-skills:csharp-api-design` | Plugin | Extend-only public API design for NuGet libraries |
| `dotnet-skills:csharp-type-design-performance` | Plugin | Sealed classes, readonly structs, type layout for performance |
| `dotnet-skills:csharp-concurrency-patterns` | Plugin | async/await, `Channel<T>`, `SemaphoreSlim`, actor model, parallel patterns |
| `dotnet-claude-kit:modern-csharp` | Plugin | C# 14 features: primary constructors, collection expressions, `field` keyword |
| `dotnet-dev-practical` | Personal | Warning suppression, analyzer management, library development tips |

### Building & Packaging Libraries

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `nuget-publishing` | Personal | Trusted Publishing (OIDC), SourceLink, deterministic builds, `.snupkg` symbols |
| `dotnet-ci-pipeline` | Personal | `dotnet` CLI build chain, Coverlet coverage, SonarCloud, multi-targeting |
| `dotnet-skills:package-management` | Plugin | Central Package Management (CPM), `dotnet` CLI package commands |
| `dotnet-skills:project-structure` | Plugin | `.slnx` format, `Directory.Build.props`, project layout conventions |
| `dotnet-claude-kit:project-structure` | Plugin | Similar to above, focused on .NET 10 |
| `nuke-build` | Personal | C# build automation with NUKE (targets, parameters, CI generation) |
| `github-actions-security` | Personal | SHA pinning, OIDC, supply chain security for CI workflows |

### Testing

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:testing` | Plugin | xUnit v3, `WebApplicationFactory`, test isolation, snapshot testing |
| `dotnet-skills:testcontainers` | Plugin | Integration tests with Docker containers (SQL Server, PostgreSQL, etc.) |
| `dotnet-skills:snapshot-testing` | Plugin | Verify library for snapshot/approval testing |
| `dotnet-skills:crap-analysis` | Plugin | CRAP scores, identifying under-tested complex methods |
| `dotnet-skills:aspire-integration-testing` | Plugin | `.NET Aspire` test harness |
| `dotnet-skills:playwright-blazor` | Plugin | UI tests for Blazor apps |

### Data Access & EF Core

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-skills:efcore-patterns` | Plugin | NoTracking, query splitting, compiled queries, interceptors |
| `dotnet-skills:database-performance` | Plugin | N+1 prevention, read/write separation, indexing |
| `dotnet-claude-kit:ef-core` | Plugin | .NET 10 EF Core: DbContext config, migrations, query optimisation |
| `dotnet-claude-kit:migration-workflow` | Plugin | Safe EF Core migration workflows |

**Also see workspace rules:**
- `.claude/rules/data-access.md` — Repository + UoW patterns specific to `Ploch.Data.GenericRepository`
- `.claude/rules/data-project.md` — DbContext and entity configuration conventions
- `.claude/rules/data-provider-project.md` — SQLite/SQL Server provider project setup
- `.claude/rules/domain-model.md` — Entity design with `Ploch.Data.Model` interfaces

### Architecture & Patterns

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:clean-architecture` | Plugin | 4-project layout (Domain/Application/Infrastructure/Presentation) |
| `dotnet-claude-kit:vertical-slice` | Plugin | Vertical Slice Architecture with MediatR |
| `dotnet-claude-kit:ddd` | Plugin | Aggregates, value objects, domain events |
| `dotnet-claude-kit:dependency-injection` | Plugin | Service lifetimes, keyed services, factory patterns |
| `dotnet-skills:microsoft-extensions-dependency-injection` | Plugin | DI registration organisation |
| `dotnet-skills:microsoft-extensions-configuration` | Plugin | Options pattern, `IValidateOptions`, strongly-typed config |
| `dotnet-skills:serialization` | Plugin | JSON vs MessagePack vs Protobuf, schema-based formats |
| `dotnet-claude-kit:architecture-advisor` | Plugin | Structured questionnaire for choosing architecture |

### API Development

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:minimal-api` | Plugin | .NET 10 minimal APIs, `MapGroup`, endpoint filters |
| `dotnet-claude-kit:openapi` | Plugin | Built-in OpenAPI document generation |
| `dotnet-claude-kit:scalar` | Plugin | Scalar API documentation UI |
| `dotnet-claude-kit:api-versioning` | Plugin | `Asp.Versioning`, URL/header/query strategies |
| `dotnet-claude-kit:authentication` | Plugin | JWT, OAuth2, policy-based authorisation |
| `dotnet-claude-kit:resilience` | Plugin | Polly v8 retry, circuit breaker, timeout |
| `dotnet-claude-kit:httpclient-factory` | Plugin | `IHttpClientFactory`, typed clients, Polly integration |

### Observability & Monitoring

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:logging` | Plugin | Serilog + OpenTelemetry combined |
| `dotnet-claude-kit:serilog` | Plugin | Serilog specifically: sinks, enrichment, two-stage bootstrap |
| `dotnet-claude-kit:opentelemetry` | Plugin | Traces, metrics, OTLP export |
| `dotnet-claude-kit:error-handling` | Plugin | Result pattern, ProblemDetails, exception middleware |

### Cloud-Native & Deployment

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:aspire` | Plugin | .NET Aspire AppHost, service discovery, orchestration |
| `dotnet-claude-kit:docker` | Plugin | Multi-stage builds, distroless images |
| `dotnet-claude-kit:container-publish` | Plugin | Dockerfile-less `dotnet publish` container images |
| `dotnet-claude-kit:ci-cd` | Plugin | GitHub Actions + Azure DevOps pipelines |
| `dotnet-skills:aspire-service-defaults` | Plugin | Shared `ServiceDefaults` project |

### Code Review & Quality

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-claude-kit:code-review-workflow` | Plugin | Structured review with Roslyn MCP tools |
| `dotnet-claude-kit:80-20-review` | Plugin | Focus on the 20% of code causing 80% of issues |
| `dotnet-claude-kit:convention-learner` | Plugin | Auto-detect project conventions |
| `dotnet-skills:slopwatch` | Plugin | Detect LLM reward hacking in code changes |
| `dotnet-skills:ilspy-decompile` | Plugin | Decompile assemblies to understand internals |

### WinUI 3 Desktop Apps

All 11 `winui3-*` skills in the workspace `.claude/skills/` directory — see the MEMORY.md entry for the full list.

### Akka.NET (Actor Model)

| Skill | Source | What It Covers |
|-------|--------|----------------|
| `dotnet-skills:akka-best-practices` | Plugin | EventStream, DistributedPubSub, actor patterns |
| `dotnet-skills:akka-testing-patterns` | Plugin | Actor unit/integration tests |
| `dotnet-skills:akka-hosting-actor-patterns` | Plugin | Entity actors, GenericChildPerEntity |
| `dotnet-skills:akka-aspire-configuration` | Plugin | Akka.NET + .NET Aspire integration |
| `dotnet-skills:akka-management` | Plugin | Cluster bootstrapping, service discovery |

## Plugin Overview

Three plugin sources provide .NET skills:

| Plugin | Focus | Skill Count |
|--------|-------|-------------|
| **dotnet-skills** | Broad .NET ecosystem (C#, EF Core, Akka.NET, Aspire, testing, DevOps) | ~30 skills |
| **dotnet-claude-kit** | Opinionated .NET 10 application patterns (APIs, architecture, CI/CD) | ~30 skills |
| **dotnet-contribution** | C#/.NET backend patterns for MCP servers and APIs | 1 skill |

### MCP Tools (Roslyn Navigator)

The `dotnet-claude-kit` plugin also provides **Roslyn MCP tools** for code analysis:

- `find_symbol` — Find types/members across the solution
- `find_callers` / `find_references` — Call graph analysis
- `find_implementations` / `find_overrides` — Interface/abstract implementations
- `get_diagnostics` — Build diagnostics from Roslyn
- `detect_antipatterns` — Automated anti-pattern detection
- `find_dead_code` — Unused code detection
- `get_type_hierarchy` — Inheritance tree
- `get_public_api` — Public API surface extraction

These are available via `mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__*` tools.

## Workspace Rules (Always Loaded)

These `.claude/rules/` files in the workspace root are always active and complement the skills:

| Rule File | Covers |
|-----------|--------|
| `code-quality.md` | General code quality standards |
| `naming.md` | Naming conventions |
| `commits.md` | Conventional Commits format |
| `writing-dotnet-tests.md` | xUnit v3 + FluentAssertions + AutoFixture |
| `data-access.md` | Repository + UoW usage patterns |
| `data-project.md` | DbContext project setup |
| `data-provider-project.md` | SQLite/SQL Server provider projects |
| `domain-model.md` | Entity design with `Ploch.Data.Model` |
| `project-structure.md` | Repo layout conventions |
| `dependencies.md` | Dependency upgrade process |
| `documentation.md` | XML docs + markdown docs |
| `pr-descriptions.md` | PR body standards |
| `agent.md` | Agent workflow (pre/post code) |
