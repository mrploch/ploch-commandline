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
- **Breaking:** an `ICommandAppExecutor` permits one execution attempt. A second
  `Run` or `RunAsync` on the same executor — whatever the first attempt's
  outcome — throws `InvalidOperationException`, synchronously and before any
  task is returned from `RunAsync`. Running again takes a new executor over a
  freshly configured command application: `AppBuilder.ConfigureCommandApp`
  provides one when the application was built through `AppBuilder`, while an
  executor constructed directly or through `CommandAppConfigurator` needs a
  newly configured `ICommandApp`, because `Configure` wraps the same one. After
  a cancelled attempt the same builder's cancellation source stays cancelled,
  so `ConfigureCommandApp` on that builder throws `InvalidOperationException`
  instead of building an executor over an already-cancelled token; a run after
  Ctrl+C starts from a new builder or a fresh source.
  Previously the second run reached Spectre, which, once the first run had
  built the host, rendered the host builder's "Build can only be called once"
  error and returned -1; an attempt may build that host and it can only be
  built once, so the executor now says so up front.

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
- **Breaking:** `AppBuilder` added `appsettings.json` a second time on top of
  `Host.CreateDefaultBuilder`'s sources, so its keys overrode
  `appsettings.{Environment}.json`, environment variables and command-line
  arguments. Standard .NET configuration precedence now applies.
- **Breaking:** `AppCommand<TSettings>` and `AsyncAppCommand<TSettings>` reported every
  `OperationCanceledException` as exit code 130, so a `TaskCanceledException`
  from an `HttpClient` timeout, or an inner token the command cancelled itself,
  was presented as "the user cancelled" and never reached `IExceptionHandler`.
  Cancellation is now reported only when the token the command received has
  cancellation requested; any other `OperationCanceledException` goes to the
  handler like every other exception.
- `AppBuilder(ConsoleAppInfo, CancellationTokenSource)`,
  `AppBuilder.ConfigureCommandApp` and `EnvironmentSettings.Initialize` throw
  `ArgumentNullException` for a null argument instead of storing it and failing
  later, far from the caller.
