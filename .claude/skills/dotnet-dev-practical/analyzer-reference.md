# Analyzer ID Reference

Quick-lookup for all analyzers enforced in the MrPloch workspace.

## Analyzer Stack

All analyzers are enforced globally via `GlobalPackageReference` in `mrploch-development/dependencies/Analyzers.Global.Packages.props`:

| Package | Prefix | Focus Area |
|---------|--------|------------|
| **StyleCop.Analyzers** | `SA****` | Code style, spacing, documentation, naming, ordering |
| **Roslynator.Analyzers** | `RCS****` | Code simplification, redundancy, readability |
| **SonarAnalyzer.CSharp** | `S****` | Code smells, bugs, security vulnerabilities |
| **Microsoft.CodeAnalysis.NetAnalyzers** | `CA****` | Design, globalisation, performance, security, usage |
| **codecracker.CSharp** | `CC****` | Code style, performance, design (mostly disabled in workspace) |
| **Microsoft.VisualStudio.Threading.Analyzers** | `VSTHRD***` | Async/await correctness, thread safety |
| **C# Compiler** | `CS****` | Language rules, nullable reference types |
| **IDE Analyzers** | `IDE****` | Code style preferences (enforced via `EnforceCodeStyleInBuild`) |
| **EF Core** | `EF****` | Entity Framework Core usage warnings |
| **NuGet** | `NU****` | Package restore and dependency warnings |

## ID Prefix Quick Reference

### StyleCop (SA)

| Range | Category | Common Suppressions |
|-------|----------|-------------------|
| SA10xx | Spacing | SA1009 (closing paren spacing — conflicts with nullable `?`) |
| SA11xx | Readability | SA1101 (this. qualifier — disabled in workspace) |
| SA12xx | Ordering | SA1200 (using placement — disabled) |
| SA13xx | Naming | SA1309 (underscore prefix — disabled, workspace uses `_camelCase`) |
| SA14xx | Maintainability | SA1402 (single type per file — disabled) |
| SA15xx | Layout | SA1501, SA1502, SA1503 (brace rules) |
| SA16xx | Documentation | SA1600 (element docs), SA1633 (file headers — disabled) |
| SA1649 | Naming | File name must match first type (warning) |

### Roslynator (RCS)

| Range | Category | Common Suppressions |
|-------|----------|-------------------|
| RCS0xxx | Formatting | RCS0023 (parentheses in conditionals) |
| RCS1xxx | Analyzers | RCS1169 (make field read-only — suggestion level) |
| RCS12xx | Simplification | RCS1251 (remove braces — disabled) |

### SonarAnalyzer (S)

| ID | Description | Notes |
|----|-------------|-------|
| S1075 | URIs should not be hardcoded | Common in config/test code |
| S1309 | Track uses of in-source issue suppression | Disabled in workspace |
| S1451 | Track file header compliance | Disabled (no file headers) |
| S1699 | Constructor calls overridable method | Common in test fixtures |
| S3236 | Caller info args should not be provided | Disabled (Guard clauses) |
| S4487 | Unread private members | Check before suppressing |

### Microsoft CA Rules

| Range | Category | Common Suppressions |
|-------|----------|-------------------|
| CA1xxx | Design | CA1062 (validate args), CA1716 (keyword clash) |
| CA17xx | Naming | CA1707 (underscores in names — disabled for tests) |
| CA18xx | Performance | CA1851 (multiple enumeration — disabled) |
| CA2xxx | Security/Reliability | CA2243 (attribute validity) |

### VS Threading (VSTHRD)

| ID | Description | Notes |
|----|-------------|-------|
| VSTHRD002 | Avoid problematic synchronous waits | Never suppress — fix the code |
| VSTHRD100 | Avoid `async void` methods | Never suppress — fix the code |
| VSTHRD101 | Avoid unsupported async delegates | Rare, investigate before suppressing |
| VSTHRD110 | Observe result of async calls | Never suppress — fix the code |
| VSTHRD111 | Use `ConfigureAwait(false)` in library code | Required in library projects |
| VSTHRD200 | Use `Async` suffix for async methods | Sometimes suppressed for interface contracts |

### CodeCracker (CC)

Most CodeCracker rules are disabled in the workspace `.editorconfig` due to false positives:

| ID | Description | Status in Workspace |
|----|-------------|-------------------|
| CC0001 | Always use `var` | Disabled |
| CC0022 | Disposable object not disposed | Disabled (false positives) |
| CC0031 | Verify delegate is not null before invoking | Disabled |
| CC0057 | Unused parameters | Disabled |
| CC0091 | Make static | Disabled |

### Compiler (CS)

| Range | Category | Notes |
|-------|----------|-------|
| CS1591 | Missing XML comment for public member | Warning level; suppressed in test projects |
| CS8600-CS8777 | Nullable reference type warnings | **All elevated to ERROR** in workspace |
| CS9057 | Analyzer incompatibility | Suppressed via NoWarn |

### NuGet (NU)

| ID | Description | Notes |
|----|-------------|-------|
| NU1507 | Multiple package sources without mapping | Suppressed in some repos |
| NU1603 | Package version fallback | Suppressed globally (cross-repo deps) |
| NU1605 | Package downgrade detected | **Elevated to ERROR** |

## Workspace Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| `Analyzers.Global.Packages.props` | `mrploch-development/dependencies/` | Which analyzers are enforced |
| `Directory.Build.props` | Each repo root | `NoWarn`, `WarningsAsErrors`, `AnalysisLevel` |
| `.editorconfig` | Each repo root (+ nested in `tests/`) | Per-rule severity overrides |
| `stylecop.json` | Each repo root | StyleCop behaviour configuration |
| `GlobalSuppressions.cs` | Per source project | Assembly-level `[SuppressMessage]` |

## Severity Precedence

When multiple sources configure the same diagnostic, this is the precedence (highest wins):

1. `#pragma warning` in source code
2. `[SuppressMessage]` attribute on member
3. `.editorconfig` rule (`dotnet_diagnostic.XXXX.severity`)
4. `GlobalSuppressions.cs` (`[assembly: SuppressMessage]`)
5. `<NoWarn>` in `.csproj` / `Directory.Build.props`
6. Analyzer default severity

## Rules That Should Never Be Suppressed

These indicate real bugs or dangerous patterns:

- **VSTHRD002** — Synchronous waits on async code (deadlock risk)
- **VSTHRD100** — `async void` (unobservable exceptions)
- **VSTHRD110** — Unobserved async call results
- **CS8600-CS8777** — Nullable reference type violations (elevated to error in workspace)
- **CA2100** — SQL injection vulnerability
- **CA2153** — Catching corrupted state exceptions
- **S2068** — Hard-coded credentials
- **S3329** — Weak crypto algorithm
