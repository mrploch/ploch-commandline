---
name: mcp-packaging-distribution
description: Use when packaging, publishing or distributing a .NET MCP server - NuGet McpServer package type, dnx execution, server.json and the official MCP registry, containers, MCPB bundles, AOT/single-file publishing. Triggers on - PackAsTool mcp, dnx, server.json, mcp registry publish, McpServer package type, mcp docker, mcp-publisher, Native AOT mcp.
---

# Packaging & Distributing .NET MCP Servers

## The canonical local-distribution path: NuGet + dnx

- MCP server = **.NET tool package**; clients run it with `dnx <PackageId>@<version> --yes` (dnx ships with the .NET 10 SDK). VS Code/VS generate this into `mcp.json`.
- NuGet.org gives MCP-tailored UX when the package has the `McpServer` package type **and** an embedded `.mcp/server.json`.
- Scaffold all of it: `dotnet new mcpserver` (`Microsoft.McpServer.ProjectTemplates`, preview).

Production csproj (excel-mcp, including the SDK workaround):

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>mcp-myserver</ToolCommandName>
<EnablePackageValidation>true</EnablePackageValidation>
<!-- SDK forces PackageType=DotnetTool when PackAsTool=true; append McpServer via target: -->
<Target Name="_AddMcpServerPackageType" BeforeTargets="GenerateNuspec;Pack">
  <PropertyGroup><PackageType>$(PackageType);McpServer</PackageType></PropertyGroup>
</Target>
<ItemGroup>
  <Content Include=".mcp/server.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Pack>true</Pack><PackagePath>.mcp/server.json</PackagePath>
  </Content>
</ItemGroup>
```

Gotchas: NETSDK1146 with `PackAsTool` + `net10.0-windows` (TargetPlatformIdentifier workaround); **never combine `<RuntimeIdentifiers>` with `PackAsTool`** if CI packs with `--no-build`.

## server.json + official MCP registry

```json
{
  "$schema": "https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json",
  "name": "io.github.<owner>/<server>",
  "title": "My MCP Server",
  "version": "1.0.0",
  "packages": [{
    "registryType": "nuget", "identifier": "MyCompany.MyServer", "version": "1.0.0",
    "transport": { "type": "stdio" },
    "packageArguments": [],
    "environmentVariables": [{ "name": "MYAPI_KEY", "isSecret": true }]
  }],
  "repository": { "url": "https://github.com/<owner>/<server>", "source": "github" }
}
```

- Publish flow: `mcp-publisher init/validate/login/publish`. Namespace auth: GitHub OAuth for `io.github.*`, DNS/HTTP challenge for `com.yourdomain/*` (the anti-tool-squatting mechanism).
- **NuGet ownership verification surprise**: the packed README must embed a hidden marker `<!-- mcp-name: io.github.<owner>/<server> -->` or the registry publish fails; publish the package first, **await NuGet.org validation** (poll in CI), then `mcp-publisher publish`. Keep server.json version in lockstep with the NuGet version.
- Container-only alternative: `"registryType": "oci", "identifier": "ghcr.io/...:tag"`.

## Containers

Publish outside Docker, runtime-only image (Azure MCP pattern):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
WORKDIR /app
COPY ${PUBLISH_DIR} .
ENTRYPOINT ["dotnet", "myserver.dll"]
```

HTTP servers on Azure: Container Apps via `azd` + Bicep (mcp-dotnet-samples ships per-sample `-azure` Dockerfile variants); Functions has a dedicated MCP extension.

## Multi-channel (Microsoft's approach, when you need reach)

One csproj drives six channels via MSBuild properties (`microsoft/mcp` Template.Mcp.Server): `DnxPackageId` (NuGet/dnx), `NpmPackageName` (npm wrapper), `PypiPackageName`, `DockerImageName`, `McpbPlatforms` (MCPB bundles for Claude Desktop: `win-x64;linux-x64;osx-arm64;…`), plus a VSIX wrapper. server.json lists nuget+npm+pypi packages with `<<Version>>` placeholders substituted by the release pipeline.

## AOT / single-file / trimming

- Native AOT dramatically cuts stdio cold start — the delay the user directly perceives at client startup. The template supports AOT + self-contained publish.
- Requirements: explicit registration (`WithTools<T>()`, not assembly scan), source-generated JSON everywhere — chain the SDK resolver first: `new JsonSerializerOptions { TypeInfoResolverChain = { McpJsonUtilities.DefaultOptions.TypeInfoResolver!, MyContext.Default } }`; per-area `JsonSerializerContext` at scale (Azure MCP). Turn on `IsAotCompatible` + `EnableAotAnalyzer/EnableTrimAnalyzer/EnableSingleFileAnalyzer`.
- Embed `appsettings.json` as `EmbeddedResource` for single-file safety.
- Don't trim when you can't (COM interop — excel-mcp documents why); default dnx package stays framework-dependent/portable (no RID).

## Versioning discipline

Tool name/parameter/response-shape changes are **breaking** — version the package + server.json, document in release notes, never `_v2` tool names (see `mcp-tools-design`). A `tools/list` snapshot test is the CI gate (see `mcp-testing-debugging`).

## Related skills

`mcp-server-setup`, `mcp-project-structure`, `mcp-testing-debugging`.
