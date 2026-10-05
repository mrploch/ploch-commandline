# Backlog — ploch-commandline

| Field | Value |
|---|---|
| Snapshot taken | 2026-10-05, repository at commit `6a2c931` |
| Sources | Linear project `ploch-commandline`; GitHub pull requests, issues and releases |
| Index | [`CLAUDE.md`](../../CLAUDE.md) |

This is a dated snapshot. **Linear is the source of truth**; when this file and Linear disagree,
Linear is right and this file needs a refresh.

## 1. Snapshot

| Measure | Count |
|---|---|
| Linear issues in the project | 64 |
| In progress | 2 |
| Backlog | 3 |
| Done | 58 |
| Cancelled | 1 |
| Open pull requests | 2, including the one that adds this file |
| Merged pull requests | 39 |
| Git tags, GitHub Releases, packages on nuget.org | 0 |

The project has one milestone, **Release 1.0**. Most issues carry no priority; only the release
batch from late September 2026 was prioritised.

## 2. In progress

| Issue | Priority | Summary | State |
|---|---|---|---|
| [PLO-620](https://linear.app/ploch/issue/PLO-620) | Urgent | Documentation snippets that do not compile; verify every snippet | [PR #93](https://github.com/mrploch/ploch-commandline/pull/93) open since 2026-09-24, checks green, fixes one snippet only |
| [PLO-654](https://linear.app/ploch/issue/PLO-654) | Medium | This knowledge base and the agent-configuration audit | The pull request that adds this file |

## 3. Planned

| Issue | Priority | Summary |
|---|---|---|
| [PLO-103](https://linear.app/ploch/issue/PLO-103) | None | Umbrella: reach a releasable state and release 1.0 |
| [PLO-585](https://linear.app/ploch/issue/PLO-585) | Low | Review `GH_PACKAGES_TOKEN` exposure to pull-request code; pin the sibling checkout |
| [PLO-655](https://linear.app/ploch/issue/PLO-655) | Medium | Triage the findings in section 6 |

What still stands between `main` and 1.0, from PLO-103:

1. Finish PLO-620.
2. The maintainer creates the Trusted Publishing policy on nuget.org for `release.yml`.
3. Dispatch `Release` with `dry_run=true`, check it, then run the real release.

The items under "Decide before 1.0" in section 6 are not on that list, but several of them change
the public API or the package dependency graph, which is cheap now and breaking later.

## 4. Recently completed — themes

All 58 completed issues date from August and September 2026, when the Spectre-based rewrite was
taken from first implementation to release-ready.

| Theme | Share | Examples |
|---|---|---|
| CI, release pipeline and feeds | about a third | PLO-558, PLO-569, PLO-539, PLO-522, PLO-523, PLO-565, PLO-622 |
| Static-analysis services | about a sixth | PLO-302, PLO-303, PLO-321, PLO-525, PLO-535, PLO-370 |
| `AppBuilder` ordering and lifetime bugs | about a tenth | PLO-300, PLO-304, PLO-570, PLO-315 |
| Output and markup bugs | small | PLO-299, PLO-314 |
| Command base and Serilog behaviour | small | PLO-316, PLO-551 |
| Sample application | about a tenth | PLO-348, PLO-583, PLO-373, PLO-298, PLO-575 |
| Documentation and packaging | small | PLO-277, PLO-560, PLO-621 |
| Security hygiene | small | PLO-330, PLO-538, PLO-580 |
| Clean-up of the legacy code line | small | PLO-278, PLO-559 |

## 5. Recurring problem areas

| Problem area | Evidence | Code area |
|---|---|---|
| Coupling to sibling repositories | Restore, fork builds, sample pin, Dependabot and release reproducibility each broke at least once | `Directory.Packages.props`, `nuget.config`, `samples/SampleApp/`, workflows |
| Third-party analysers disagreeing with the code | Six issues to scope, baseline or tune SonarCloud, Codacy and Qodana | `.codacy.yml`, `SonarLint.xml`, `.qodana/`, `build-dotnet.yml` |
| `AppBuilder` call-order semantics | Four bugs, two of them breaking fixes | `src/Spectre/CommandLine.Spectre/AppBuilder.cs` |
| Markup handling of caller data | Two bugs, one still open as a design point | `src/Spectre/CommandLine.Spectre/Output/` |
| Documentation drifting from the API | PLO-620 plus the mismatches in section 6 | `docs/`, `DocumentationSite/`, package READMEs |
| Committed credentials and vulnerable pins | One leaked key, two advisories | Agent configuration files, shared package versions |

The first two rows account for roughly half of all completed work. Neither is in the library
itself.

## 6. Technical debt register

Findings of the 2026-10-05 review that have no issue of their own. They are tracked together by
[PLO-655](https://linear.app/ploch/issue/PLO-655) until triaged.

### 6.1 Decide before 1.0

| Finding | Where |
|---|---|
| Core depends on the Serilog package; logging to files is on for everyone | `Ploch.CommandLine.Spectre.csproj`, `AppServicesBundle.cs` |
| Public types with no caller: `CommandAttribute`, `CommandInfo`, `CommandInfoFactory`, `CommandAppConfigurator` | `src/Spectre/CommandLine.Spectre/` |
| Host is built but never started; hosted services do not run | `DependencyInjectionTypeRegistrar.cs` |
| Commands and settings are singletons and capture transient services | `DependencyInjectionTypeRegistrar.cs` |
| Validation failure exits `-1`; `ExitCode.InvalidInput` is 2 | `ExitCode.cs`, `DocumentationSite/index.md` |
| `OperationCanceledException` is treated as cancellation without checking the token | `AppCommand.cs`, `AsyncAppCommand.cs` |
| `IOutput.Write(string)` parses markup and throws on `[` in user data | `AnsiConsoleMarkupOutput.cs` |
| Banner and "Executing command" lines always go to standard output | `AppBuilder.cs`, `AppCommand.cs` |

### 6.2 Correctness and documentation

| Finding | Where |
|---|---|
| `AddSerilog()` without a configuration appears to throw (`UNVERIFIED` against the package) | `SerilogLoggingConfigurator.cs` |
| XML docs still describe a plain console sink | Serilog package |
| Package README example uses the abstract `CommandSettings` | `src/Spectre/CommandLine.Spectre/README.md` |
| Guide lists Serilog as optional and says `Execute` exercises validation | `docs/GETTING_STARTED.md` |
| Missing null guards on three public entry points | `AppBuilder.cs`, `EnvironmentSettings.cs` |
| Two message writers are unreachable through `IOutput` | `Output/` |
| Reusing one executor for a second `Run` is untested | `CommandAppExecutor.cs` |

### 6.3 Build, CI and hygiene

| Finding | Where |
|---|---|
| The `main` ruleset has no required status checks | GitHub ruleset |
| `directory.build.targets` is lower-case and fully commented out | Repository root |
| Solution lists `NuGet.Config`; the file is `nuget.config` | `Ploch.CommandLine.Spectre.slnx` |
| Stale comments in three workflows and `.editorconfig` | `.github/workflows/`, `.editorconfig` |
| Fixture scripts need bash 4 | `.github/scripts/test-*.sh` |
| Vestigial build items and duplicated Serilog references | See [`TECH-STACK.md`](TECH-STACK.md) section 9 |
| Noisy `PrintSettings` target; Debug builds pack | `Directory.Build.props` |
| No concurrency cancellation for superseded runs | `.github/workflows/` |
| Three test files whose names do not match their classes | `tests/Spectre/CommandLine.Spectre.Tests/` |
| Generic pull request template | `.github/pull_request_template.md` |
| Outdated shared versions, three with a new major | `mrploch-development` |

## 7. Roadmap

1. **Release 1.0** — the only milestone. Everything on its checklist except PLO-620 and the
   nuget.org policy is done.
2. **After 1.0** — nothing is planned in Linear. The likely first work is whatever PLO-655 turns
   into, and catching up the shared dependency versions.

## 8. How to refresh

1. List the Linear project's issues in every state; update sections 1 to 3.
2. `gh pr list` for open, merged and closed pull requests; `gh release list`.
3. Re-read section 6 against PLO-655 and remove what has been triaged.
4. Update the snapshot date and commit.
