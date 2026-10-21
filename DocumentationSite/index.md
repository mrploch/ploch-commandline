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
| `Ploch.CommandLine.Spectre.Serilog` | Serilog integration, including a `SerilogConfigurationBundle` for structured logging. The core package depends on it and `AppServicesBundle` registers the bundle, so it is installed and active without a separate `dotnet add package`. |
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

`AppCommand<TSettings>` takes the settings processor, the validator, the exception handler
and an `IOutput`, which it exposes as the `Output` property. Anything else the command
needs — a use case, your own services — is an ordinary constructor parameter resolved
from the container.

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

That is standard output. The start-up banner and an `Executing command greet` line go to standard
error, so the terminal shows them above the greeting while `greeter greet Alice --loud | cat`
prints just `HELLO, ALICE!`.

## Core concepts

### `AppBuilder`

`AppBuilder.Create(args)` starts the chain. It wraps a `HostBuilder`, so the
usual host-builder configuration entry points are available (the library never
starts the host; see below):

- `WithName`, `WithVersion`, `WithDescription` — application metadata, also used to render the startup banner.
- `ShowBanner(false)` — drop the startup banner and the `Executing command …` line written before
  each command. Both go to standard error, so neither reaches a pipe even when they are on. (The
  Ctrl+C notice and the `PauseBeforeExit` prompt still use standard output;
  [PLO-819](https://linear.app/ploch/issue/PLO-819) tracks them.)
- `ConfigureServices` — register services into the container that resolves your commands.
- `ConfigureAppConfiguration` — add configuration sources on top of the host's own ones. Each source
  added here outranks `appsettings.json`, the environment variables and the command line, so do not
  re-add `appsettings.json` through it; use `ConfigureHost(host => host.UseContentRoot(...))` when you
  only need the file to be found somewhere else.
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

The host is built once a run reaches command resolution — a parse failure or `--version` returns
before that — and the library never starts it. It exists for dependency injection, configuration
and logging; a CLI command is the unit of work, so the library neither starts nor stops an
`IHostedService` you register, and a host that was built is disposed when the run ends. Starting
the host around the command is a possible 1.x feature, tracked as
[PLO-785](https://linear.app/ploch/issue/PLO-785).

### Command base classes

| Base class | Use for |
|---|---|
| `AppCommand<TSettings>` | Synchronous commands. Implement `DoExecute`. |
| `AsyncAppCommand<TSettings>` | Asynchronous commands. Implement `DoExecuteAsync`. |
| `UseCaseAsyncCommand<...>` | Commands that delegate to an `IResultUseCase`. |

`Spectre.Console.Cli` calls each base class's `Validate` override first; `Execute` / `ExecuteAsync`
then runs the settings processor, invokes your implementation, and routes any
exception to the configured `IExceptionHandler`. Cancellation is handled separately from
failure: when the token the command received has been cancelled, an `OperationCanceledException`
returns `ExitCode.Cancelled` without reaching the exception handler. One raised while that token
is still live (an `HttpClient` timeout, a cancelled inner token) is a fault and goes to the
handler like any other exception.

Every base class also runs the settings-processing pipeline: `AppCommand<TSettings>`,
`AsyncAppCommand<TSettings>` and `UseCaseAsyncCommand<...>` all take a
`CommandArgumentsRootProcessor` and call it before your implementation. The
`Executing command …` line that announces the command is written to standard error by the
application before the command runs, not by the base class, which writes nothing of its own
to the output. The synchronous and asynchronous bases do the same framework work; only the
shape of your implementation differs.

### Exit codes

| Member | Value | Meaning |
|---|---|---|
| `ExitCode.Success` | 0 | Completed normally. |
| `ExitCode.Error` | 1 | Unhandled failure. |
| `ExitCode.InvalidInput` | 2 | The input was rejected — by the command (a value outside a supported set, a missing file, mutually exclusive flags) or by `Spectre.Console.Cli` while parsing, binding or validating it. |
| `ExitCode.Cancelled` | 130 | Cancelled before completing — the conventional shell code for termination by SIGINT (128 + 2). |

A failed `Validate` exits with `InvalidInput` too. `Spectre.Console.Cli` runs validation before the
command and reports the errors itself; the handler `AppBuilder.ConfigureCommandApp` installs maps
that failure, and every command line Spectre cannot parse or bind — an unknown command, a missing
required argument, a value that does not convert — to `InvalidInput` in place of Spectre's own `-1`
(`255` in a POSIX shell). `InvalidInput` therefore means "the input was rejected" whether your code
or Spectre decided so — with one caveat: Spectre raises the same exception type for a few failures
that are not input errors, such as a settings property with no type converter, and those exit 2
too. The [Getting Started guide](../docs/GETTING_STARTED.md#11-exit-codes) walks through the exit
codes that come from elsewhere: the `IExceptionHandler` return value and Spectre's own `1` for a
branch or default command invoked without what it needs.

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
