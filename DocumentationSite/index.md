# Ploch.CommandLine.Spectre

An opinionated framework for building .NET console applications, layering
[Spectre.Console.Cli](https://spectreconsole.net/cli/) on top of
`Microsoft.Extensions.Hosting` so that a CLI gets dependency injection,
configuration, logging, and validation with a single builder chain.

## Install

```bash
dotnet add package Ploch.CommandLine.Spectre
```

Prerelease builds are published to GitHub Packages on every push to `main`;
stable releases go to [NuGet.org](https://www.nuget.org/profiles/mrploch).

## Packages

| Package | Purpose |
|---|---|
| `Ploch.CommandLine.Spectre` | Core framework — `AppBuilder`, command base classes, settings pipeline, output pipeline. |
| `Ploch.CommandLine.Spectre.Serilog` | Serilog integration, including a `SerilogConfigurationBundle` for structured logging. |
| `Ploch.CommandLine.Spectre.FluentValidation` | Declarative validation of command settings using FluentValidation validators. |
| `Ploch.CommandLine.UseCases` | Bridges commands to `IResultUseCase` for a Clean Architecture style, built on `Ardalis.Result`. |

## Quick start

Define a settings class and a command:

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
    ICommandSettingsValidator<GreetSettings> validator,
    IExceptionHandler exceptionHandler,
    IOutput output)
    : AppCommand<GreetSettings>(validator, exceptionHandler)
{
    protected override ExitCode DoExecute(
        CommandContext context, GreetSettings settings, CancellationToken cancellationToken)
    {
        var greeting = $"Hello, {settings.Name}!";
        output.WriteLine(settings.Loud ? greeting.ToUpperInvariant() : greeting);

        return ExitCode.Success;
    }
}
```

`AppCommand<TSettings>` takes only the validator and the exception handler. Anything else
the command needs — an `IOutput`, a use case, your own services — is an ordinary constructor
parameter resolved from the container, as `IOutput` is here.

Wire it up in `Program.cs`:

```csharp
using Ploch.CommandLine.Spectre;

// The builder owns the Ctrl+C handler and cancellation source it creates; dispose it after the run.
using var appBuilder = AppBuilder.Create(args)
                                 .WithName("greeter")
                                 .WithVersion(new Version(1, 0, 0))
                                 .WithDescription("A greeting utility.")
                                 .ConfigureServices(services => services.AddSingleton<IClock, SystemClock>());

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
$ greeter greet Alice --loud
HELLO, ALICE!
```

## Core concepts

### `AppBuilder`

`AppBuilder.Create(args)` starts the chain. It wraps a `HostBuilder`, so the
usual hosting extension points are available:

- `WithName`, `WithVersion`, `WithDescription` — application metadata, also used to render the startup banner.
- `ConfigureServices` — register services into the container that resolves your commands.
- `ConfigureAppConfiguration` — add configuration sources such as `appsettings.json`.
- `ConfigureHost` — reach the underlying `IHostBuilder` directly.
- `Dispose` — release the `Console.CancelKeyPress` handler and the `CancellationTokenSource` that
  `Create` installed. A builder constructed directly with your own source owns neither and leaves
  both alone.
- `AddServicesBundle<TBundle>` — register a `ServicesBundle` from `Ploch.Common.DependencyInjection`.

The token behind that source is handed to Spectre and reaches every command. The first Ctrl+C cancels
it cooperatively; a second one terminates the process, so a command that ignores its token cannot
leave the application unkillable from the keyboard.

`ConfigureCommandApp` terminates the chain and returns an `ICommandAppExecutor`,
which exposes `Run` and `RunAsync`.

### Command base classes

| Base class | Use for |
|---|---|
| `AppCommand<TSettings>` | Synchronous commands. Implement `DoExecute`. |
| `AsyncAppCommand<TSettings>` | Asynchronous commands. Implement `DoExecuteAsync`. |
| `UseCaseAsyncCommand<...>` | Commands that delegate to an `IResultUseCase`. |

Each base class validates the settings, invokes your implementation, and routes any
exception to the configured `IExceptionHandler`. Cancellation is handled separately from
failure: an `OperationCanceledException` returns `ExitCode.Cancelled` and never reaches the
exception handler.

The settings-processing pipeline is run by the **asynchronous** bases only.
`AsyncAppCommand<TSettings>` and `UseCaseAsyncCommand<...>` take a
`CommandArgumentsRootProcessor` and call it before your implementation;
`AppCommand<TSettings>` does not take one and validates then executes directly. A
synchronous command that needs the pipeline should take the processor itself, or derive
from `AsyncAppCommand<TSettings>` instead.

### Exit codes

| Member | Value | Meaning |
|---|---|---|
| `ExitCode.Success` | 0 | Completed normally. |
| `ExitCode.Error` | 1 | Unhandled failure. |
| `ExitCode.InvalidInput` | 2 | Settings failed validation. |
| `ExitCode.Cancelled` | 130 | Cancelled before completing — the conventional shell code for termination by SIGINT (128 + 2). |

### Token substitution

Mark a settings property with `[SupportsTokens]` and `TokensArgumentsProcessor`
expands `{date}` and `{datetime}` placeholders before the command runs:

```csharp
[CommandOption("-o|--output")]
[SupportsTokens]
public string OutputPath { get; init; } = "./reports/{date}/report.txt";
```

### Output pipeline

`IOutput` abstracts console writing so commands stay testable. Messages pass
through `IMessageFormatterProcessor`, which selects a registered formatter by
message type and falls back to `ToString()` when none matches.

## Learn more

- [Getting Started](../docs/GETTING_STARTED.md) — build a CLI from an empty directory, one feature at a time.
- [Articles](articles/intro.md) — task-focused guides.
- [API documentation](api/index.md) — generated reference for every public type.
- [Sample application](https://github.com/mrploch/ploch-commandline/tree/main/samples) — a complete multi-level CLI showcase.
