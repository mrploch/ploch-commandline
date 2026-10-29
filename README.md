# Ploch CommandLine Applications

[![NuGet](https://img.shields.io/nuget/v/Ploch.CommandLine.Spectre.svg)](https://www.nuget.org/packages/Ploch.CommandLine.Spectre)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ploch.CommandLine.Spectre.svg)](https://www.nuget.org/packages/Ploch.CommandLine.Spectre)
[![Build](https://github.com/mrploch/ploch-commandline/actions/workflows/build-dotnet.yml/badge.svg)](https://github.com/mrploch/ploch-commandline/actions/workflows/build-dotnet.yml)
[![Publish Docs](https://github.com/mrploch/ploch-commandline/actions/workflows/publish-docs.yml/badge.svg)](https://commandline.github.ploch.dev/)
[![Qodana](https://github.com/mrploch/ploch-commandline/actions/workflows/qodana_code_quality.yml/badge.svg)](https://github.com/mrploch/ploch-commandline/actions/workflows/qodana_code_quality.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=mrploch_ploch-commandline&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=mrploch_ploch-commandline)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mrploch_ploch-commandline&metric=coverage)](https://sonarcloud.io/summary/new_code?id=mrploch_ploch-commandline)
[![Codacy Grade](https://app.codacy.com/project/badge/Grade/96834038202e40868af2cb827f145301)](https://app.codacy.com/gh/mrploch/ploch-commandline/dashboard)
[![Codacy Coverage](https://app.codacy.com/project/badge/Coverage/96834038202e40868af2cb827f145301)](https://app.codacy.com/gh/mrploch/ploch-commandline/dashboard)

## Overview

**Ploch.CommandLine.Spectre** is an opinionated framework for building console applications on .NET
(the packages target .NET 10). It layers [Spectre.Console.Cli](https://spectreconsole.net/cli/) on top
of `Microsoft.Extensions.Hosting`, so a CLI gets dependency injection, configuration, logging,
validation and output formatting from one builder chain.

| Package | Purpose |
|---|---|
| [`Ploch.CommandLine.Spectre`](https://www.nuget.org/packages/Ploch.CommandLine.Spectre) | Core framework: `AppBuilder`, the command base classes, the settings pipeline and the output pipeline. |
| [`Ploch.CommandLine.Spectre.Serilog`](https://www.nuget.org/packages/Ploch.CommandLine.Spectre.Serilog) | Opt-in Serilog logging, switched on with `UseSerilog` on the builder. |
| [`Ploch.CommandLine.Spectre.FluentValidation`](https://www.nuget.org/packages/Ploch.CommandLine.Spectre.FluentValidation) | Validation of command settings with FluentValidation validators. |
| [`Ploch.CommandLine.UseCases`](https://www.nuget.org/packages/Ploch.CommandLine.UseCases) | Commands that delegate to an `IResultUseCase` built on `Ardalis.Result`. |

## Quick start

```bash
dotnet new console -n greeter
cd greeter
dotnet add package Ploch.CommandLine.Spectre
```

Define a settings class and a command (`GreetCommand.cs`):

```csharp
using System.ComponentModel;
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

public class GreetSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The name to greet.")]
    public string Name { get; init; } = string.Empty;

    [CommandOption("-l|--loud")]
    [Description("Shout the greeting.")]
    public bool Loud { get; init; }
}

public class GreetCommand(
    CommandArgumentsRootProcessor settingsProcessor,
    ICommandSettingsValidator<GreetSettings> validator,
    IExceptionHandler exceptionHandler,
    IOutput output)
    : AppCommand<GreetSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override ExitCode DoExecute(
        CommandContext context, GreetSettings settings, CancellationToken cancellationToken)
    {
        var greeting = $"Hello, {settings.Name}!";
        Output.WriteLine(settings.Loud ? greeting.ToUpperInvariant() : greeting);

        return ExitCode.Success;
    }
}
```

`AppCommand<TSettings>` takes the settings processor, the validator, the exception handler and an
`IOutput`, which it exposes as the `Output` property. Anything else the command needs is an ordinary
constructor parameter resolved from the container.

Wire it up in `Program.cs`:

```csharp
using Ploch.CommandLine.Spectre;
using Spectre.Console.Cli;

// The builder owns the Ctrl+C handler and cancellation source it creates; dispose it after the run.
using var appBuilder = AppBuilder.Create(args)
                                 .WithName("greeter")
                                 .WithVersion(new Version(1, 0, 0))
                                 .WithDescription("A greeting utility.");

var executor = appBuilder.ConfigureCommandApp(config =>
{
    config.SetApplicationName("greeter");
    config.AddCommand<GreetCommand>("greet")
          .WithDescription("Greet someone by name.")
          .WithExample("greet", "Alice", "--loud");
});

return executor.Run(args);
```

```console
$ dotnet run -- greet Alice --loud
HELLO, ALICE!
```

That is standard output. The start-up banner and the `Executing command greet` line go to standard
error, so `greeter greet Alice --loud | cat` prints just `HELLO, ALICE!`. A command line Spectre
cannot parse, and settings that fail validation, exit with code 2; the
[exit codes](https://commandline.github.ploch.dev/#exit-codes) are part of the contract.

## Documentation

- [Documentation site](https://commandline.github.ploch.dev/) — overview, core concepts and the
  generated API reference.
- [Getting started](docs/GETTING_STARTED.md) — build a CLI from an empty directory, one feature at
  a time: commands, configuration, dependency injection, sub-commands, validation, token expansion,
  use cases, exit codes, cancellation, logging and testing.
- [Sample application](samples/SampleApp/README.md) — a complete multi-level CLI that exercises
  every feature, with the output and exit code of every command.
- [Release notes](RELEASE_NOTES.md) — what changed in each version.
- [Dependency updates](docs/dependency-updates.md) — how NuGet and GitHub Actions
  dependencies are kept current, and why NuGet is handled by a scheduled workflow rather
  than by Dependabot.
- [AI knowledge base](docs/ai/README.md) — architecture, stack, conventions and backlog written
  for AI agents; [`CLAUDE.md`](CLAUDE.md) is its entry point.

## Building this repository

Clone `mrploch-development` as a sibling directory first — `Directory.Packages.props` imports the
shared package versions from it, so restore fails without it:

```bash
git clone https://github.com/mrploch/ploch-commandline.git
git clone https://github.com/mrploch/mrploch-development.git
cd ploch-commandline
dotnet build Ploch.CommandLine.Spectre.slnx
dotnet test  Ploch.CommandLine.Spectre.slnx
```

**The main solution needs no GitHub Packages token.** It only ever references stable `Ploch.*`
versions, and those are on nuget.org, so a build with `GH_PACKAGES_TOKEN` unset normally restores
successfully — the `401 (Unauthorized)` warnings from the unauthenticated GitHub Packages feed are
expected. That "stable versions only" rule is a policy, enforced by a check in CI.

Tokenless restore is best-effort rather than guaranteed: the GitHub Packages source stays eligible
while unauthenticated, and NuGet can surface its failure as `NU1301` before nuget.org answers. If
you hit that, retry, or set a token. The
[sample application](samples/SampleApp/README.md) is a separate case — it pins a prerelease build
of this repository's own packages and does need a token in its default mode.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the reasoning, the escape hatch for working against
unreleased `ploch-common` code, and the rest of the contributor setup.
