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
using Ploch.CommandLine.Spectre;
using Ploch.CommandLine.Spectre.Commands;
using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Cli;

// The builder owns the Ctrl+C handler, so dispose it once the run has returned.
using var appBuilder = AppBuilder.Create(args).WithName("My Tool");

var executor = appBuilder.ConfigureCommandApp(config => config.AddCommand<HelloCommand>("hello"));

return executor.Run(args);

public class HelloCommand(CommandArgumentsRootProcessor settingsProcessor,
                          ICommandSettingsValidator<CommandSettings> validator,
                          IExceptionHandler exceptionHandler,
                          IOutput output) : AppCommand<CommandSettings>(settingsProcessor, validator, exceptionHandler, output)
{
    protected override ExitCode DoExecute(CommandContext? context, CommandSettings settings, CancellationToken cancellationToken)
    {
        Output.MarkupLineInterpolated($"[bold cyan]Hello![/]");

        return ExitCode.Success;
    }
}
```

`AppCommand<TSettings>` validates the settings, runs the registered argument processors, and turns
exceptions and cancellation into exit codes, so `DoExecute` holds only the command's own work.

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

