---
name: dotnet-dev-practical
description: Practical .NET library development guide — analyzer warning suppression techniques (#pragma, SuppressMessage, ReSharper comments, .editorconfig, GlobalSuppressions.cs, NoWarn), library packaging, and day-to-day development patterns for reusable NuGet libraries. Use when writing C# code that triggers analyzer warnings, when deciding how to suppress a diagnostic, or when developing .NET libraries for NuGet distribution.
---

# Practical .NET Library Development

Day-to-day development reference for building reusable .NET libraries. Covers analyzer warning management, library conventions, and development workflow patterns.

For related skills, see [related-skills.md](related-skills.md).
For the full analyzer ID reference, see [analyzer-reference.md](analyzer-reference.md).

---

## Warning Suppression Techniques

There are multiple ways to suppress analyzer diagnostics in .NET projects. Each has a different scope and purpose. Choose the narrowest scope that fits the situation.

### Decision Guide

| Scope | Technique | When to Use |
|-------|-----------|-------------|
| Single expression/statement | `#pragma warning disable` | One-off false positive on a specific line |
| Single expression/statement | `// ReSharper disable once ...` | ReSharper/Rider-specific diagnostic on one line |
| Single member | `[SuppressMessage]` attribute | False positive on a method/property; preserves intent via `Justification` |
| Single file (top) | `#pragma warning disable` (no restore) | Generated or imported file where fixing is impractical |
| Entire project | `<NoWarn>` in `.csproj` | Diagnostic is irrelevant for the entire project (e.g. test projects) |
| All projects in repo | `<NoWarn>` in `Directory.Build.props` | Diagnostic is universally irrelevant across the solution |
| All projects in repo | `.editorconfig` severity override | Change severity (error → warning → suggestion → none) per rule |
| Entire assembly | `GlobalSuppressions.cs` with `[assembly: SuppressMessage]` | Broad suppression with auditable justifications |
| IDE only (not build) | `// ReSharper disable ...` | ReSharper/Rider-only warnings not enforced in CI |

**Rule of thumb:** Start narrow, go wider only when the same suppression appears in 3+ places.

---

### 1. `#pragma warning disable / restore`

Suppresses any C# compiler or Roslyn analyzer diagnostic by ID. Scoped to the lines between `disable` and `restore`.

```csharp
#pragma warning disable SA1600 // Elements should be documented
public class InternalHelper
{
    // ...
}
#pragma warning restore SA1600

// Multiple IDs on one line
#pragma warning disable CS1591, SA1600
public void UndocumentedMethod() { }
#pragma warning restore CS1591, SA1600
```

**When to use:**
- One-off false positives where fixing the code is wrong or impractical
- Around EF Core internal API usage (`EF1001`)
- Around generated code that triggers style rules

**When NOT to use:**
- Don't leave `disable` without `restore` (except at file top for generated files)
- Don't suppress broadly — always specify the diagnostic ID
- If you're suppressing the same ID in 3+ places, use `.editorconfig` instead

---

### 2. `[SuppressMessage]` Attribute

From `System.Diagnostics.CodeAnalysis`. Supports `Justification` — mandatory in this workspace.

```csharp
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "FlagArgument", Justification = "Guard clause requires bool parameter")]
public static void NotNull<T>(T value, bool allowEmpty = false) { }

[SuppressMessage("Style", "VSTHRD200:Use Async suffix",
    Justification = "Interface contract does not use Async suffix")]
public Task Execute(CancellationToken ct) { }

[SuppressMessage("Critical Code Smell", "S1699:Constructors should only call non-overridable methods",
    Justification = "Test fixture base class — overrides are controlled")]
protected DataIntegrationTest() { }
```

**Category values by analyzer:**

| Analyzer | Category Format | Example |
|----------|----------------|---------|
| ReSharper | `"ReSharper"` | `"ReSharper", "PossibleMultipleEnumeration"` |
| SonarAnalyzer | `"Critical Code Smell"`, `"Major Code Smell"`, etc. | `"Major Code Smell", "S1075"` |
| StyleCop | `"StyleCop.CSharp.DocumentationRules"`, etc. | `"StyleCop.CSharp.SpacingRules", "SA1009"` |
| VS Threading | `"Style"` | `"Style", "VSTHRD200"` |
| Microsoft CA | `"Design"`, `"Performance"`, `"Security"`, etc. | `"Design", "CA1062"` |
| Roslynator | `"Roslynator"` | `"Roslynator", "RCS1169"` |

**When to use:**
- Method-level or class-level suppressions with clear justification
- When you want the suppression to be visible in API documentation tooling
- When the suppression reason needs to be auditable

---

### 3. `GlobalSuppressions.cs`

Assembly-level `[SuppressMessage]` attributes in a dedicated file. Conventions:

```csharp
// GlobalSuppressions.cs
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1636:File header copyright text should match",
    Justification = "Organisation-wide header differs from StyleCop default")]

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header",
    Justification = "No file headers required in this project")]

// Targeted to a specific member
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly",
    Scope = "module",
    Target = "Ploch.Apps.Model.dll",
    Justification = "False positive with nullable syntax")]
```

**When to use:**
- Suppressing a rule across the entire assembly
- When the suppression applies to generated or imported code you don't own
- For rules that are categorically wrong for the project (but `.editorconfig` is preferred)

**File location:** Root of each source project (e.g. `src/Common/GlobalSuppressions.cs`).

---

### 4. `.editorconfig` Severity Overrides

Change diagnostic severity without suppressing at code level. This is the **preferred** approach for rules you want to adjust across the entire repo.

```ini
# .editorconfig (at repo root or in src/ or tests/ subdirectories)

[*.cs]

# Disable a rule entirely
dotnet_diagnostic.SA1633.severity = none      # File headers not required
dotnet_diagnostic.SA1101.severity = none      # 'this.' qualification not required
dotnet_diagnostic.CA1707.severity = none      # Allow underscores in test method names

# Downgrade to suggestion (green squiggle, no build warning)
dotnet_diagnostic.RCS1169.severity = suggestion   # Make field read-only

# Upgrade to error (breaks the build)
dotnet_diagnostic.CS8600.severity = error     # Null assigned to non-nullable

# Test-specific overrides
[*Tests/**/*.cs]
dotnet_diagnostic.SA1600.severity = none      # No XML docs required in tests
dotnet_diagnostic.CA1707.severity = none      # Underscores in test names OK
```

**Severity values:** `error` | `warning` | `suggestion` | `silent` | `none`
- `none` = completely disabled (not reported, not enforced)
- `silent` = reported in IDE but not in build output
- `suggestion` = green squiggle in IDE, visible in build

**When to use:**
- The primary way to configure diagnostic severity
- Test-specific overrides (put a nested `.editorconfig` in `tests/`)
- Adjusting rules that conflict with project conventions

**Inheritance:** `.editorconfig` files inherit from parent directories. A file in `tests/` inherits from the repo root `.editorconfig` and can override specific rules.

---

### 5. `<NoWarn>` in MSBuild

Suppress diagnostics at the project or solution level via MSBuild properties.

#### Per-project (`.csproj`)

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);NU1507;CS9057</NoWarn>
</PropertyGroup>
```

#### All projects (`Directory.Build.props`)

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);NU1603</NoWarn>
</PropertyGroup>
```

#### Test projects only (`Directory.Build.props` with condition)

```xml
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('Tests'))">
    <NoWarn>$(NoWarn);CS1591;SA1600</NoWarn>
</PropertyGroup>
```

**Important:** Always use `$(NoWarn);ID` (append) rather than just `ID` (replace), to preserve suppressions from imported props files.

**When to use:**
- NuGet-specific warnings (`NU*`) that are project-level concerns
- Compiler warnings that are genuinely inapplicable to the entire project
- Prefer `.editorconfig` over `<NoWarn>` for analyzer rules — `.editorconfig` is more granular

---

### 6. ReSharper / Rider Comments

JetBrains-specific comment directives. These are **IDE-only** — not enforced by `dotnet build`.

```csharp
// Disable for a single line (next line)
// ReSharper disable once PossibleMultipleEnumeration
var items = collection.ToList();

// Disable for a region
// ReSharper disable FlagArgument
public static void Guard(object value, bool allowNull = false) { }
public static void Check(object value, bool throwOnFail = true) { }
// ReSharper restore FlagArgument

// Disable for the entire file (place at top)
// ReSharper disable UnusedAutoPropertyAccessor.Global

// Common ReSharper inspection IDs used in this workspace:
// - PossibleMultipleEnumeration
// - FlagArgument
// - TooManyArguments
// - UnusedAutoPropertyAccessor.Global
// - MemberCanBePrivate.Global
// - ClassNeverInstantiated.Global
// - UnusedMember.Global
// - InconsistentNaming
```

**When to use:**
- JetBrains Rider/ReSharper users who see inspections not covered by Roslyn analyzers
- When the diagnostic is Rider-only and won't appear in CI builds

**Alternative — JetBrains Annotations:**

```csharp
using JetBrains.Annotations;

[UsedImplicitly]           // Suppresses "unused member" — member is used via reflection/DI
public class MyService { }

[PublicAPI]                 // Marks as public API — suppresses "can be internal"
public static class Extensions { }

[Pure]                      // Method has no side effects
public int Calculate() => 42;

[MustUseReturnValue]       // Warn if return value is discarded
public Result Process() { }
```

---

### 7. SonarQube / SonarCloud Suppression

SonarAnalyzer.CSharp rules (prefixed `S`) can be suppressed via `[SuppressMessage]` or `.editorconfig`. SonarCloud also supports inline comments:

```csharp
var password = "test123"; // NOSONAR — test fixture constant, not a real credential
```

**Prefer `[SuppressMessage]` over `// NOSONAR`** — it provides justification and is visible to all tooling.

---

## Library Development Patterns

### Package README

NuGet supports embedding a README in packages. Add to your `.csproj`:

```xml
<PropertyGroup>
    <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
<ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### InternalsVisibleTo for Test Projects

Expose `internal` members to test projects without making them `public`:

```csharp
// In the source project's AssemblyInfo.cs or any .cs file
[assembly: InternalsVisibleTo("Ploch.Common.Tests")]
```

Or in `.csproj`:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Ploch.Common.Tests" />
</ItemGroup>
```

### ConfigureAwait(false) in Library Code

Library code should always use `ConfigureAwait(false)` to avoid deadlocks when consumed by UI applications:

```csharp
public async Task<T> GetAsync(int id, CancellationToken ct = default)
{
    var result = await _repository.FindAsync(id, ct).ConfigureAwait(false);
    return result ?? throw new NotFoundException();
}
```

The VS Threading Analyzer (`VSTHRD111`) enforces this.

### API Surface Discipline

For reusable libraries:
- Mark classes as `sealed` unless designed for inheritance
- Use `internal` by default; only expose what consumers need
- Avoid exposing implementation types — return interfaces
- Think twice before adding `public` — it's a permanent API commitment

### WarningsAsErrors for Key Rules

Consider elevating critical rules to errors in library projects:

```xml
<PropertyGroup>
    <WarningsAsErrors>$(WarningsAsErrors);NU1605;Nullable</WarningsAsErrors>
</PropertyGroup>
```

This ensures nullable reference type violations and NuGet dependency downgrades break the build.

---

## Quick Reference: Common Suppressions in This Workspace

| ID | Analyzer | What It Flags | Typical Justification |
|----|----------|---------------|----------------------|
| `EF1001` | EF Core | Internal API usage | Required for advanced EF Core scenarios |
| `SA1600` | StyleCop | Missing XML docs | Test projects; internal types |
| `SA1633` | StyleCop | Missing file header | Not required by project convention |
| `SA1101` | StyleCop | Missing `this.` qualifier | Project convention: no `this.` prefix |
| `SA1309` | StyleCop | Field begins with underscore | Project convention: `_camelCase` fields |
| `CS1591` | C# Compiler | Missing XML comment | Internal types; test projects |
| `CA1707` | MS Analyzers | Underscore in identifier | Test method names use underscores |
| `S1699` | SonarAnalyzer | Constructor calls overridable method | Test fixture base classes |
| `VSTHRD200` | VS Threading | Async suffix missing | Interface contract constraint |
| `IDE0130` | .NET IDE | Namespace doesn't match folder | Legacy namespace structure |
| `NU1603` | NuGet | Package version downgrade | Cross-repo dependency resolution |

---

## Additional Reference

- [analyzer-reference.md](analyzer-reference.md) — Full analyzer ID prefixes, common diagnostic IDs, and workspace configuration
- [related-skills.md](related-skills.md) — Cross-references to existing skills and plugins for .NET development
