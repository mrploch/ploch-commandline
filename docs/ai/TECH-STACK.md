# Technology stack — ploch-commandline

| Field | Value |
|---|---|
| Last verified | 2026-10-05, against commit `6a2c931` |
| Verified by | Running the commands in section 3 on macOS arm64; reading every build and workflow file |
| Index | [`CLAUDE.md`](../../CLAUDE.md) |

## 1. Summary

| Item | Value | Defined in |
|---|---|---|
| Language | C#, `LangVersion=default` | `Directory.Build.props` |
| Target framework | `net10.0` for every project | `../mrploch-development`, see section 2 |
| SDK | `10.0.100`, roll forward `latestFeature` | `global.json` |
| Nullable | Enabled | `Directory.Build.props` |
| Warnings | `TreatWarningsAsErrors=true`; `NU1901`, `NU1902` stay warnings | `Directory.Build.props` |
| Package management | Central (`Directory.Packages.props`) | Root, plus shared files |
| Versioning | Nerdbank.GitVersioning, `1.0-prerelease` | `version.json` |
| Tests | xUnit v3, FluentAssertions, Moq, AutoFixture, Coverlet | Shared files |
| Docs site | DocFX 2.78.5 | `DocumentationSite/docfx.json` |
| Licence | Apache-2.0 | `LICENSE`, `Directory.Build.props` |

There is no database, container image or external runtime service.

## 2. Build prerequisites

- **Sibling checkout `../mrploch-development` is mandatory.** `Directory.Packages.props` imports
  shared version files from `../mrploch-development/dependencies/` unconditionally. The target
  framework itself comes from there: `TargetFrameworkVersion` is set to `net10.0` in
  `MicrosoftExtensions.Net9.Packages.props` (the file name is historical).
- **Sibling checkout `../ploch-common`** is needed only with `-p:UsePlochProjectReferences=true`.
- **`GH_PACKAGES_TOKEN`** (environment variable, read by `nuget.config`) is optional for the main
  solution and required for the sample's default package mode. See
  [`CONTRIBUTING.md`](../../CONTRIBUTING.md) for why a tokenless restore is best-effort.
- **Local tools:** `dotnet tool restore` installs `nbgv`, `docfx`, `dotnet-sonarscanner` and
  `reportgenerator` from `.config/dotnet-tools.json`.

## 3. Commands

Observed on 2026-10-05 with SDK 10.0.401 and no `GH_PACKAGES_TOKEN`.

| Command | Result |
|---|---|
| `dotnet restore Ploch.CommandLine.Spectre.slnx` | Succeeds; warns `401` from GitHub Packages, falls back to nuget.org |
| `dotnet build Ploch.CommandLine.Spectre.slnx` | 0 warnings, 0 errors, about 5 s; also packs 8 packages |
| `dotnet test Ploch.CommandLine.Spectre.slnx` | 289 passed, about 5 s |
| Sample build, CI mode (below) | Succeeds, about 18 s |
| Sample test, CI mode | 49 passed |
| `dotnet restore` on the sample, default mode | Fails with `NU1301` without a token |
| Stable-version guard (below) | Passes |
| `dotnet tool restore` then `dotnet nbgv get-version` | Prints the computed version |
| `dotnet docfx DocumentationSite/docfx.json` | 0 warnings, about 7 s |

```bash
# Sample application, the way CI builds it
dotnet build samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx -c Release \
  -p:UsePlochProjectReferences=true -p:GeneratePackageOnBuild=false

# Guard: no prerelease Ploch.* version in the main solution
dotnet msbuild src/Spectre/CommandLine.Spectre/Ploch.CommandLine.Spectre.csproj \
  -t:VerifyPlochPackageVersionsAreStable

# Tests with coverage, the way CI runs them
dotnet test Ploch.CommandLine.Spectre.slnx -c Release /p:CollectCoverage=true \
  /p:CoverletOutput=./CoverageResults/ "/p:CoverletOutputFormat=cobertura%2copencover"

# Dependency queries need an explicit source when no token is set
dotnet list Ploch.CommandLine.Spectre.slnx package --outdated \
  --source https://api.nuget.org/v3/index.json
```

Things that do not work as you might expect:

- `dotnet list package --outdated`, `--vulnerable` and `--deprecated` exit 1 without a token unless
  `--source` is given.
- `.github/scripts/test-*.sh` need bash 4 or later (`mapfile`); they fail under the stock macOS
  bash 3.2.
- Every build packs NuGet packages, Debug included (`GeneratePackageOnBuild=true` for non-test
  projects).

## 4. Dependencies

Version sources: **L** is this repository's `Directory.Packages.props`. **S/x** is
`../mrploch-development/dependencies/x.Packages.props`. Versions are what resolved on the
verification date; "newer" is the latest stable on nuget.org that day.

### 4.1 Runtime

| Package | Version | Source | Used by | Newer |
|---|---|---|---|---|
| Spectre.Console | 0.54.0 | S/Spectre.Console | core, Serilog | 0.57.2 |
| Spectre.Console.Cli | 0.53.1 | S/Spectre.Console | core | 0.57.2 |
| Microsoft.Extensions.Hosting | 10.0.5 | S/MicrosoftExtensions.Net9 | core | 10.0.12 |
| Microsoft.Extensions.Configuration.Json | 10.0.5 | S/MicrosoftExtensions.Net9 | core | 10.0.12 |
| Serilog | 4.3.0 | S/Serilog.Logging | core, Serilog | 4.4.0 |
| Serilog.Settings.Configuration | 10.0.0 | S/Serilog.Logging | core, Serilog | 10.0.1 |
| Serilog.Sinks.SpectreConsole | 0.3.3 | S/Serilog.Logging | core, Serilog | 0.4.0 |
| Serilog.Sinks.File, .Sinks.Console, .Enrichers.Thread | 7.0.0, 6.1.1, 4.0.0 | S/Serilog.Logging | core, Serilog | current |
| Serilog.Extensions.Hosting, .Extensions.Logging | 10.0.0 | S/Serilog.Logging | core, Serilog | current |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | L | FluentValidation | current |
| Ardalis.Result | 10.1.0 | S/Common | UseCases | current |
| Ploch.Common, Ploch.Common.DependencyInjection | 4.0.47 | S/Ploch | core, Serilog, FluentValidation | current |
| Ploch.Common.Apps.Shared | 4.0.47 | L (override) | core | current |

### 4.2 Build, test and analysis

| Package | Version | Source | Newer |
|---|---|---|---|
| Nerdbank.GitVersioning | 3.10.94 | L | current |
| xunit.v3 | 3.2.2 | S/Testing | 4.0.1 (major) |
| xunit.runner.visualstudio | 3.1.5 | S/Testing | 4.0.0 (major) |
| Microsoft.NET.Test.Sdk | 18.6.0 | S/Testing | 18.10.1 |
| coverlet.msbuild | 8.0.0 | S/Common | 10.1.0 |
| Ploch.TestingSupport.XUnit3.AutoMoq | 4.0.47 | S/Ploch | current |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.201 | S/Analyzers.Global | 10.0.401 |
| SonarAnalyzer.CSharp | 10.21.0 | S/Analyzers.Global | 10.35.0 |
| Roslynator.Analyzers | 4.15.0 | S/Analyzers.Global | 5.0.1 (major) |
| Microsoft.VisualStudio.Threading.Analyzers | 17.14.15 | S/Analyzers.Global | 18.7.23 |
| StyleCop.Analyzers | 1.1.118 | S/Analyzers.Global | current stable |
| codecracker.CSharp | 1.1.0 | S/Analyzers.Global | current |
| Spectre.Console.Analyzer | 1.0.0 | S/Spectre.Console | current |

FluentAssertions, Moq and AutoFixture arrive transitively through
`Ploch.TestingSupport.XUnit3.AutoMoq`.

### 4.3 Notes

- **No vulnerable or deprecated package** in the main solution, transitive included (nuget.org
  data).
- **Updates to shared versions are made in `mrploch-development`,** not here. Only the two rows
  marked L are bumped in this repository.
- **The sample pins its own versions** in `samples/SampleApp/Directory.Packages.props`, including a
  prerelease build of this repository's packages (`1.0.20-prerelease` on the verification date,
  while `main` built `1.0.25`). `dependency-report.yml` watches that pin weekly.
- **Dependabot handles GitHub Actions only.** NuGet is covered by the weekly dependency report;
  see [`docs/dependency-updates.md`](../dependency-updates.md).

## 5. Static analysis and code style

| Layer | What | Where |
|---|---|---|
| Compiler | Six analyser packages as `GlobalPackageReference`; `AnalysisLevel=latest-Recommended` | Shared `Analyzers.Global` file |
| Style | `EnforceCodeStyleInBuild=true`; naming rules at warning | `.editorconfig` |
| Errors | 54 nullable-flow `CS8xxx` diagnostics and `IDE0002` | `.editorconfig` |
| Documentation | `CS1591` at warning, so an error under warnings-as-errors | `.editorconfig` |
| Disabled | `IDE0058`, 22 StyleCop rules, a few CA, CC, RCS and S rules | `.editorconfig` |
| Tests | `CS1591` off | `tests/.editorconfig` |
| SonarCloud | Project `mrploch_ploch-commandline`; configured in workflow arguments | `build-dotnet.yml` |
| Codacy | Rule allow-list and path scope | `SonarLint.xml`, `.codacy.yml`, `.checkov.yaml` |
| Qodana | Baseline; fail threshold 0 new problems | `qodana.yaml`, `.qodana/` |
| CodeQL | `csharp` and `actions`, `build-mode: none` | `codeql.yml` |

`SonarLint.xml` is input for Codacy's SonarC# engine, not for MSBuild. There is no `stylecop.json`
and no `sonar-project.properties`.

## 6. CI/CD

All workflows are in `.github/workflows/`. Actions are pinned to commit SHAs, except
`github/codeql-action`.

| Workflow | Runs on | Does |
|---|---|---|
| `build-dotnet.yml` | Push to `main`, every PR | Guard, restore, SonarCloud, Release build, tests with coverage, sample build and test, coverage upload, DocFX smoke test, publish to GitHub Packages |
| `codeql.yml` | Push to `main`, PR, weekly | Code scanning |
| `qodana_code_quality.yml` | Push to `main`, PR | Qodana against the committed baseline |
| `publish-docs.yml` | Push to `main` | DocFX build and GitHub Pages deployment |
| `dependency-report.yml` | Weekly | Outdated and vulnerable report; builds the sample against its pin |
| `release.yml` | Manual, `main` only | Stable release, see section 7 |

Facts worth knowing:

- CI clones `mrploch-development` at its moving `main`, so a merge there changes this build with no
  commit here. `release.yml` pins it to a SHA input instead.
- The `main` ruleset requires a pull request, resolved threads, CodeQL and code-quality results.
  It has **no required status checks**, so `build`, SonarCloud, Qodana and Codacy do not block a
  merge at platform level.
- Secrets used, by name: `GH_PACKAGES_TOKEN`, `SONAR_TOKEN`, `CODACY_PROJECT_TOKEN`, a Qodana
  token, `GH_TOKEN`. SonarCloud and coverage upload are skipped for forks and Dependabot.
- Review bots that comment on pull requests are configured outside the repository.
- On the verification date every workflow's recent history was green.

## 7. Versioning and release

- `version.json` sets `1.0-prerelease`. The patch number is the git height. `main` and `v*` tags
  are public release refs; other refs get a `.g<sha>` suffix.
- Every push to `main` publishes `1.0.<height>-prerelease` packages to GitHub Packages. Pull
  requests targeting `main` publish `-pr.<n>.<branch>` versions.
- A stable release is `release.yml`, dispatched by hand with `release_version` and optionally
  `dry_run`. It sets the version, runs the guard, builds, tests, packs, tests the sample against
  the fresh packages, tags, publishes to nuget.org through Trusted Publishing (OIDC), creates the
  GitHub Release from `change-log/*.md`, then opens a post-release pull request that bumps the
  version and archives the change-log entries.
- `RELEASE_NOTES.md` is maintained by hand; no workflow edits it.
- **State on the verification date:** no tags, no GitHub Releases, nothing on nuget.org. The only
  release run was a dry run, so the real publish path has never executed.

## 8. External services

| Service | Used for |
|---|---|
| GitHub Actions, Packages, Pages | CI, prerelease feed, documentation site |
| nuget.org | Stable packages (not yet published) |
| SonarCloud, Codacy, Qodana Cloud, CodeQL | Static analysis and coverage |
| Linear | Issue tracking, see [`CONVENTIONS.md`](CONVENTIONS.md) |

## 9. Risks and debt

| Item | Detail |
|---|---|
| Cross-repository coupling | Target framework and versions come from another repository's moving branch |
| Misleading names | `MicrosoftExtensions.Net9.Packages.props` defines net10; `TargetFrameworkVersion` shadows a well-known MSBuild name |
| File-name casing | `directory.build.targets` is lower-case and fully commented out; the solution lists `NuGet.Config` for `nuget.config` |
| Enforcement gap | Workflow comments call SonarCloud a required gate; the ruleset does not require it |
| Stale comments | NBGV version in `build-dotnet.yml`, feed description in `codeql.yml`, branch name in `release.yml` |
| Linux-only scripts | `mapfile` in `.github/scripts/` |
| Vestigial items | `samples/Directory.Packages.props`, `IsReleaseBuild`, an empty `Models` folder entry, eight duplicated Serilog references in core |
| Noisy build | `PrintSettings` prints ten lines per project; every Debug build packs |
| Pending majors | xunit.v3 4, Roslynator 5; StyleCop 1.1.118 predates C# 12 |
| No concurrency cancellation | Superseded PR runs are not cancelled |
