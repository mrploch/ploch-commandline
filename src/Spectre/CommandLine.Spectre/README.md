# Ploch.CommandLine.Spectre

## Overview

**Ploch.CommandLine.Spectre** is an opinionated library for building console applications on .NET (the packages target
.NET 10). It builds on top of the [Spectre.Console](https://spectreconsole.net/) library, providing a few additional features
and prescribed ways of doing things.

## Features

[Spectre.Console](https://spectreconsole.net/) provides a rich set of features for building console applications. It includes composing command line interfaces,
rich UI elements, and more.

**Ploch.CommandLine.Spectre** builds on top of that and provides a few additional features and prescribed ways of doing things.

It includes:

- App construction and configuration, including service registrations
- Unified command interfaces providing an opinionated way of organising your app
- Command validation
- Command execution
- Exception handling
- Logging
- Configuration
- Output formatting

## Installation

```bash
dotnet add package Ploch.CommandLine.Spectre
```

## Quick example

```csharp
using System.ComponentModel;
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

// The builder owns the Ctrl+C handler, so dispose it once the run has returned.
using var appBuilder = AppBuilder.Create(args).WithName("My Tool");

var executor = appBuilder.ConfigureCommandApp(config => config.AddCommand<HelloCommand>("hello"));

return executor.Run(args);

// CommandSettings is abstract, so every command needs a concrete settings class of its own.
public class HelloCommandSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Who to greet.")]
    public string Name { get; init; } = "world";
}

public class HelloCommand(CommandArgumentsRootProcessor settingsProcessor,
                          ICommandSettingsValidator<HelloCommandSettings> validator,
                          IExceptionHandler exceptionHandler,
                          IOutput output) : AppCommand<HelloCommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override ExitCode DoExecute(CommandContext context, HelloCommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[bold cyan]Hello, {settings.Name}![/]");

        return ExitCode.Success;
    }
}
```

`ConfigureCommandApp` returns an executor that permits one execution attempt; call it again to
build another executor for another run. Cancellation is permanent: once the builder's cancellation
source has been cancelled, `ConfigureCommandApp` throws `InvalidOperationException`, so a run after
Ctrl+C starts from a new builder (`AppBuilder.Create`) or a fresh `CancellationTokenSource` passed
to the constructor.

`AppCommand<TSettings>` runs the registered argument processors and turns exceptions and
cancellation into exit codes — cancellation only when the command's own token was cancelled; see
the Getting Started guide's Cancellation section — so `DoExecute` holds only the command's own work.
Its `Validate` override hands the settings to the `ICommandSettingsValidator<TSettings>`;
`Spectre.Console.Cli` calls it before `Execute`, and a failure exits with `ExitCode.InvalidInput`,
as does a command line Spectre could not parse or bind: `ConfigureCommandApp` installs a Spectre
exception handler that keeps Spectre's error rendering and replaces its `-1` exit code.

Standard output belongs to the command: the start-up banner and the `Executing command <name>`
line the application writes before each command go to standard error, so `myapp list | cat` carries
only what the command rendered, and `AppBuilder.ShowBanner(false)` drops both for a tool whose
standard error is captured too.

`IOutput.Write` and `WriteLine` print a string as-is, so a path or user input containing `[` is safe
to pass straight through. Markup is an explicit opt-in: `Markup` / `MarkupLine` parse their argument
as Spectre markup, and `MarkupInterpolated` / `MarkupLineInterpolated` escape the interpolated string values
(a non-string value is formatted and its text parsed, so convert it to a string first).

## Getting Started

See the [Getting Started guide](https://github.com/mrploch/ploch-commandline/blob/main/docs/GETTING_STARTED.md),
which walks through a first command, dependency injection, configuration, validation and logging.
A complete runnable example lives in
[`samples/SampleApp`](https://github.com/mrploch/ploch-commandline/tree/main/samples/SampleApp).

## Dependency Injection

`Ploch.CommandLine.Spectre` uses the `Microsoft.Extensions.DependencyInjection` library for dependency injection. It also relies on
`Ploch.Common.DependencyInjection` package which defines a **Services Bundle** concept - a class that groups related service registrations.

### Services Bundles

This project provides following services bundles:

- `AppServicesBundle` - registers services required for the app to run
- `OutputServicesBundle` - registers services required for output formatting

Neither bundle chooses a logging provider: an application built with `AppBuilder` resolves `ILogger<T>` from the
host's default providers, and the core package creates no application log file. Serilog is opt-in — add the `Ploch.CommandLine.Spectre.Serilog`
package and call `UseSerilog` on the builder to log through Serilog, with the log name, directory, output template
and culture as optional arguments.

