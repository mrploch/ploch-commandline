# ploch-commandline — agent entry point

Opinionated .NET library family for building console applications. `AppBuilder` wraps
`Microsoft.Extensions.Hosting` and `Spectre.Console.Cli` so a CLI gets dependency injection,
configuration, logging, validation and output formatting from one fluent chain.

Public repository, Apache-2.0, **pre-1.0**: prerelease packages go to GitHub Packages, nothing is
on nuget.org yet.

This file holds the essentials. The verified detail lives in [`docs/ai/`](docs/ai/README.md) —
read the document that matches the task before changing anything.

## Index

| Document | Read it when you need |
|---|---|
| [`docs/ai/ARCHITECTURE.md`](docs/ai/ARCHITECTURE.md) | Components, key types, flows, invariants, sharp edges |
| [`docs/ai/TECH-STACK.md`](docs/ai/TECH-STACK.md) | Toolchain, verified commands, dependencies, CI/CD, release |
| [`docs/ai/CONVENTIONS.md`](docs/ai/CONVENTIONS.md) | Code, test, commit, branch, PR and issue conventions |
| [`docs/ai/BACKLOG.md`](docs/ai/BACKLOG.md) | Open work, themes, technical debt, roadmap |
| [`docs/ai/AGENT-CONFIG.md`](docs/ai/AGENT-CONFIG.md) | What agent configuration exists and what should change |
| [`docs/ai/README.md`](docs/ai/README.md) | The document standard and how to keep it current |

Human-facing documentation: [`README.md`](README.md), [`CONTRIBUTING.md`](CONTRIBUTING.md),
[`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md), [`samples/SampleApp/README.md`](samples/SampleApp/README.md).

## Layout

```text
src/Spectre/CommandLine.Spectre/                   Ploch.CommandLine.Spectre (core)
src/Spectre/CommandLine.Spectre.Serilog/           Ploch.CommandLine.Spectre.Serilog
src/Spectre/CommandLine.Spectre.FluentValidation/  Ploch.CommandLine.Spectre.FluentValidation
src/Spectre/CommandLine.UseCases/                  Ploch.CommandLine.UseCases
tests/Spectre/<Project>.Tests/                     one test project per source project
samples/SampleApp/                                 standalone solution, own package versions
docs/, DocumentationSite/                          guides and the DocFX site
change-log/                                        one file per user-visible change
.github/workflows/                                 build, CodeQL, Qodana, docs, dependency report, release
```

## Build and test

Prerequisite: `../mrploch-development` checked out as a sibling. The target framework (`net10.0`)
and most package versions are defined there, so restore fails without it.

```bash
dotnet build Ploch.CommandLine.Spectre.slnx
dotnet test  Ploch.CommandLine.Spectre.slnx

# Sample application: separate solution. This is the mode CI uses; it needs ../ploch-common
# and no feed token.
dotnet build samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx \
  -p:UsePlochProjectReferences=true -p:GeneratePackageOnBuild=false
dotnet test  samples/SampleApp/Ploch.CommandLine.Spectre.SampleApp.slnx \
  -p:UsePlochProjectReferences=true -p:GeneratePackageOnBuild=false
```

More commands, with observed results, are in [`docs/ai/TECH-STACK.md`](docs/ai/TECH-STACK.md).

## Rules that break the build or the release

- **Warnings are errors.** A missing XML doc comment on a public member in `src/` fails the build
  (CS1591). So do 54 nullable-flow diagnostics raised to error in `.editorconfig`.
- **Never reference a prerelease `Ploch.*` version** in the main solution. CI checks this with the
  `VerifyPlochPackageVersionsAreStable` target.
- **Never commit a change that only builds with `-p:UsePlochProjectReferences=true`.**
- **A user-visible change needs a `change-log/` entry.** See
  [`change-log/README.md`](change-log/README.md).
- **Breaking changes** use `!` in the commit header plus a `BREAKING CHANGE:` footer.
- **Do not add `_ =` discards** to silence IDE0058; it is disabled in `.editorconfig`.
- **Never commit a credential.** Tokens reach builds through environment variables only.

## Behaviour that surprises people

- Serilog is always on: the core package depends on the Serilog package and writes log files next
  to the binaries by default.
- The host is built but never started, so `IHostedService` implementations do not run.
- Commands and settings are registered as singletons.
- `IOutput.Write(string)` parses Spectre markup. Use the interpolated overloads for user data.
- A parse or validation failure exits with `-1`, not `ExitCode.InvalidInput` (2).
- Configuration sources added through `ConfigureAppConfiguration` outrank the command line.

Each of these is explained, with file references, in
[`docs/ai/ARCHITECTURE.md`](docs/ai/ARCHITECTURE.md).

## Workflow

- Issues live in Linear (team Ploch, project `ploch-commandline`, identifiers `PLO-<n>`).
- One task, one git worktree, one branch named `<type>/plo-<n>-<short-description>`.
- Conventional Commits with a `Refs: PLO-<n>` footer. Pull requests are squash-merged.
- A pull request is finished only when every check is green and every review thread is answered.

The rule files committed under `.claude/rules/` predate the move to Linear and still ask for
GitHub issue numbers (`Refs: #<n>`, `<type>/<n>-…` branches). Where they disagree with this
section, this section describes current practice; the differences are listed in
[`docs/ai/CONVENTIONS.md`](docs/ai/CONVENTIONS.md) section 10 and their replacement is proposed in
[`docs/ai/AGENT-CONFIG.md`](docs/ai/AGENT-CONFIG.md).

Detail and the evidence for each convention: [`docs/ai/CONVENTIONS.md`](docs/ai/CONVENTIONS.md).
