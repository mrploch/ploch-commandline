# Ploch.CommandLine.Spectre

## Overview

**Ploch CommandLine Applications** is an opinionated library for building console applications in .NET Core. It builds on top of
the [Spectre.Console](https://spectreconsole.net/) library, providing a few additional features and prescribed ways of doing things.

## Features

[Spectre.Console](https://spectreconsole.net/) provides a rich set of features for building console applications. It includes composing command line interfaces,
rich UI elements, and more.

**Ploch.CommandLine.Spectre** builds on top of that and provides a few additional features and prescribed ways of doing things.

It includes:

- App construction and configuration, including service registrations
- Unified command interfaces providing opinionated way of organizing your app
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

`AppCommand<TSettings>` runs the registered argument processors and turns exceptions and
cancellation into exit codes, so `DoExecute` holds only the command's own work. Its `Validate`
override hands the settings to the `ICommandSettingsValidator<TSettings>`; `Spectre.Console.Cli`
calls it before `Execute`, and a failure exits with Spectre's own `-1` rather than `ExitCode.InvalidInput`.

`IOutput.Write` and `WriteLine` print a string as-is, so a path or user input containing `[` is safe
to pass straight through. Markup is an explicit opt-in: `Markup` / `MarkupLine` parse their argument
as Spectre markup, and `MarkupInterpolated` / `MarkupLineInterpolated` escape the interpolated values.

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

`AppServicesBundle` depends on `SerilogConfigurationBundle` from `Ploch.CommandLine.Spectre.Serilog`, which this
package references, so every application built with `AppBuilder` logs through Serilog without adding another
package. Call `AddSerilog` to set the log name, directory, output template or culture.

