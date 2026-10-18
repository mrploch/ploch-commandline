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
- `IOutput.Markup(string)` and `IOutput.MarkupLine(string)`, the explicit entry
  points for rendering a string as Spectre markup, next to the existing
  `MarkupInterpolated` / `MarkupLineInterpolated`.

### Changed

- **Breaking:** `IOutput.Write(string)` and `WriteLine(string)` write the text
  verbatim instead of parsing it as Spectre markup, following Spectre.Console's
  own split between `Write(string)` and `Markup(string)`. A caller that relied on
  `Write("[green]ok[/]")` rendering green text should call `Markup` /
  `MarkupLine`, or `MarkupLineInterpolated` for a string with values in it.
  `FormattableString` messages are unchanged and still render as interpolated
  markup with their holes escaped.
- **Breaking:** `IOutput` gains the two `Markup` / `MarkupLine` members, so a
  custom implementation of the interface must add them.

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
- **Breaking:** `AppBuilder.ConfigureServices`, `ConfigureHost`, and
  `ConfigureAppConfiguration` are additive — every delegate is applied, in the
  order it was added — matching the `IHostBuilder` methods they wrap. They
  previously kept only the last delegate.
- **Breaking:** those three methods share one sequence, applied to the host in
  the order the calls were made. `ConfigureHost` delegates previously ran after
  every `ConfigureServices` and `ConfigureAppConfiguration` delegate whatever
  the call order, so a later `ConfigureServices` registration could lose to an
  earlier `ConfigureHost` one. The application's `CancellationTokenSource` is
  now registered after every caller service delegate, including those added
  through `ConfigureHost`.
- `Ploch.CommandLine.Spectre` no longer declares the eight `Serilog.*` packages as
  direct dependencies. They were duplicates of what its `Ploch.CommandLine.Spectre.Serilog`
  dependency already declares, so the resolved package graph of a consumer is unchanged;
  only the package's own dependency list is shorter.

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

- `services.AddSerilog()` called without an `IConfiguration` threw
  `InvalidOperationException` ("configuration was not initialized"), although the
  method's documentation showed exactly that call. `SerilogConfigurationBundle` now
  registers the logger with its predefined sinks and the `Information` minimum level
  when no configuration is supplied; its `Configure` hook runs in that case too, so a
  subclass override receives `null` instead of being skipped. **Breaking (source,
  nullability):** the override's parameter is now declared `IConfiguration?`; with
  nullable warnings enabled, a subclass override with a non-nullable parameter produces
  CS8765 on recompilation and should declare `IConfiguration?` as well.
- `ConvertibleMessageFormatter` was registered in DI but threw
  `NotImplementedException`, so writing any `IConvertible` — `int`, `bool`,
  `DateTime` — crashed.
- `CommandInfoFactory.CreateFromType` threw on its primary path.
- Cancellation was accepted everywhere and honoured nowhere; Ctrl+C cancelled a
  token nothing observed.
- `AnsiConsoleMarkupOutput.Write` printed every writer-handled message twice.
- `DefaultExceptionHandler` could throw while reporting a `Win32Exception`, whose
  text contains `[` sequences that Spectre parses as markup.
- `IOutput.Write(string)` and `WriteLine(string)` threw `InvalidOperationException`
  for any text that was not well-formed markup — a path such as `C:\logs[1].txt`
  or user input echoed back — because every string was sent through the markup
  parser.
- The Serilog error-log sink sat outside its filtered sub-logger, so the "errors"
  file received every event.
- `IOutput.Write` threw `InvalidCastException` whenever the registered writer for
  the message expected a type a `string` could not be cast to — writing an
  `Exception` through `Write` always crashed.
- **Breaking:** `AppBuilder` added `appsettings.json` a second time on top of
  `Host.CreateDefaultBuilder`'s sources, so its keys overrode
  `appsettings.{Environment}.json`, environment variables and command-line
  arguments. Standard .NET configuration precedence now applies.
