# Release Notes

All notable changes to the `Ploch.CommandLine` packages are recorded here. Each
released version also has a [GitHub Release](https://github.com/mrploch/ploch-commandline/releases)
generated from the entries in [`change-log/`](./change-log/README.md).

## Unreleased — towards 1.0

The first supported release of the library. `Ploch.CommandLine.Spectre` replaces
the earlier `Ploch.Common.CommandLine` packages, which were never published and
have been retired.

### Packages

| Package | Description |
|---|---|
| `Ploch.CommandLine.Spectre` | Core framework: `AppBuilder`, command base classes, settings pipeline, output pipeline. |
| `Ploch.CommandLine.Spectre.Serilog` | Serilog integration for structured logging. |
| `Ploch.CommandLine.Spectre.FluentValidation` | Declarative validation of command settings. |
| `Ploch.CommandLine.UseCases` | Clean Architecture use-case commands built on `Ardalis.Result`. |

### Added

- `AppBuilder`, wrapping `Microsoft.Extensions.Hosting` with `Spectre.Console.Cli`,
  giving a CLI dependency injection, configuration, and logging from one chain.
- `AppCommand<TSettings>` and `AsyncAppCommand<TSettings>` base classes with
  built-in settings validation, exception handling, and cancellation.
- A settings-processing pipeline, including `TokensArgumentsProcessor` for
  `{date}` and `{datetime}` substitution via `[SupportsTokens]`.
- An output pipeline built on `IOutput` and `IMessageFormatterProcessor`, with
  type-based formatters and writers.
- `UseCaseAsyncCommand`, bridging commands to `IResultUseCase`.

### Changed

- **Breaking:** `AppCommand<TSettings>.DoExecute` and
  `AsyncAppCommand<TSettings>.DoExecuteAsync` accept a `CancellationToken`.
- **Breaking:** `IMessageFormatterProcessor.WriteMessage` returns `bool` rather
  than `void`, so a caller can tell whether the message was handled.
- **Breaking:** `EnvironmentSettings.DevRuntimeVariables` contains only
  `DEV_RUNTIME`-prefixed variables rather than the entire environment block,
  which routinely carries secrets.
- **Breaking:** `EnvironmentSettings.PauseBeforeExit` defaults to `false`, so an
  ordinary invocation no longer appears to hang waiting for Enter.
- **Breaking:** `EnvironmentSettings.Initialize` throws if called after `Current`
  has already been read, instead of silently doing nothing.
- **Breaking:** `IMessageFormatterProcessor.WriteMessage` hands the writer the
  original message and the processor, rather than the formatted text, so a writer
  selected by message type receives a value of that type and formats it itself.

### Removed

- **Breaking:** the `Ploch.Common.CommandLine` packages and their Autofac,
  Hosting, and Serilog companions, built on `McMaster.Extensions.CommandLineUtils`.
  They were never published, so no consumer is affected.
- **Breaking:** `ConsoleAppInfo.AppNameColorSys`, `AppNameInfoColorSys`,
  `AppDescriptionColorSys`, and `ConsoleAppInfoExtensions.FromSysColor` — use the
  `Spectre.Console.Color` properties instead.
- **Breaking:** `AnsiConsoleMarkupOutput.WriteMarkupLineInterpolated`, which was
  absent from `IOutput` and silently discarded non-`FormattableString` messages.
  Use `MarkupLineInterpolated`.

### Fixed

- `ConvertibleMessageFormatter` was registered in DI but threw
  `NotImplementedException`, so writing any `IConvertible` — `int`, `bool`,
  `DateTime` — crashed.
- `CommandInfoFactory.CreateFromType` threw on its primary path.
- Cancellation was accepted everywhere and honoured nowhere; Ctrl+C cancelled a
  token nothing observed.
- `AnsiConsoleMarkupOutput.Write` printed every writer-handled message twice.
- `DefaultExceptionHandler` could throw while reporting a `Win32Exception`, whose
  text contains `[` sequences that Spectre parses as markup.
- The Serilog error-log sink sat outside its filtered sub-logger, so the "errors"
  file received every event.
- `IOutput.Write` threw `InvalidCastException` whenever the registered writer for
  the message expected a type a `string` could not be cast to — writing an
  `Exception` through `Write` always crashed.
