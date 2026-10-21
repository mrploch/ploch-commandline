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
- `AppBuilder.ShowBanner(bool)` and `ConsoleAppInfo.ShouldShowBanner`, to drop the
  start-up banner and the `Executing command …` line together ([PLO-783](https://linear.app/ploch/issue/PLO-783)).

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
- **Breaking:** `CommandAttribute`, `CommandInfo`, `CommandInfoFactory`,
  `CommandAppConfigurator`, `ICommandAppConfigurator` and
  `TypeMessageFormatter<TMessage>.IsWriter` are `internal`. Nothing on the
  library's runtime path uses them (`AppBuilder` never went through the
  configurator), so they are withdrawn from the 1.0 surface: widening later is
  non-breaking, narrowing after 1.0 is not. Code that configured a
  command application it owns through `CommandAppConfigurator` does what the
  configurator did: call `ICommandApp.Configure` on that application and
  construct `CommandAppExecutor` over it directly. An application built through
  `AppBuilder` uses `AppBuilder.ConfigureCommandApp`, which creates and
  configures its own command application. The command metadata types and
  `IsWriter` have no public replacement, as nothing in the library reads them.
- **Breaking:** commands, settings and everything else Spectre registers through
  `ITypeRegistrar.Register` are transient rather than singletons, and so is
  `CommandArgumentsRootProcessor`, which now matches the transient
  `TokensArgumentsProcessor` it aggregates; a processor you register keeps the
  lifetime you give it. A
  singleton command captured its transient dependencies for the life of the
  host, and every command shared one `TokensArgumentsProcessor` with its mutable
  state. `RegisterInstance` and
  `RegisterLazy` registrations, the console, `IOutput`, the message formatter
  processor and the `CancellationTokenSource` stay singletons. State that must
  outlive one resolution belongs in a singleton service the command depends on;
  re-registering the command type from `ConfigureServices` has no effect, because
  Spectre's registration is queued later and wins. A command that
  constructor-injects its own settings type now receives a fresh, unbound instance
  rather than the bound one Spectre passes to `Execute`
  ([PLO-781](https://linear.app/ploch/issue/PLO-781)).
- **Breaking:** the start-up banner and the `Executing command …` line are written
  to standard error rather than standard output, so `myapp list | cat` carries only
  what the command rendered. The line names the command by its registered leaf
  name (`add`; an alias by the name it aliases) rather than its settings type (a
  default command as "the default command"), is
  written by the application once the settings are bound and before the
  command's `Validate` runs, and appears for every command, not only those
  deriving from the base classes. `AppCommand<TSettings>` and
  `AsyncAppCommand<TSettings>` write nothing to `IOutput` of their own (the
  `Processing arguments…` line is gone), and a failure to write the announcement
  no longer reaches the command's `IExceptionHandler`: it stops the run before the
  command, as any failure ahead of execution does. The library registers the
  announcement as an `ICommandInterceptor` ahead of every other registration, so
  one you register through a services bundle, `ConfigureServices` or Spectre's
  `SetInterceptor` runs after it.
  `ConsoleAppInfoExtensions.PrintAppInfo` takes the `IAnsiConsole` to print to;
  the parameterless overload, which wrote to standard output, is gone
  ([PLO-783](https://linear.app/ploch/issue/PLO-783)).
- **Breaking:** an `ICommandAppExecutor` permits one execution attempt. A second
  `Run` or `RunAsync` on the same executor — whatever the first attempt's
  outcome — throws `InvalidOperationException`, synchronously and before any
  task is returned from `RunAsync`. Running again takes a new executor over a
  freshly configured command application: `AppBuilder.ConfigureCommandApp`
  provides one when the application was built through `AppBuilder`, while an
  executor constructed directly needs a newly configured `ICommandApp`. After
  a cancelled attempt the same builder's cancellation source stays cancelled,
  so `ConfigureCommandApp` on that builder throws `InvalidOperationException`
  instead of building an executor over an already-cancelled token; a run after
  Ctrl+C starts from a new builder or a fresh source.
  Previously the second run reached Spectre, which, once the first run had
  built the host, rendered the host builder's "Build can only be called once"
  error and returned -1; an attempt may build that host and it can only be
  built once, so the executor now says so up front.
- **Breaking:** `FormattableStringMessageWriter` no longer pre-renders the
  interpolation holes through the registered formatters, which it did only when
  called directly with a processor, so a hole holding an exception or a
  collection renders through `string.Format` instead. Code that relied on the
  formatters calls `IMessageFormatterProcessor.GetMessageText(FormattableString, ...)`
  itself, from its own writer.
- **Breaking:** `MessageFormatterProcessor` selects the later of two writers or formatters
  registered for the same message type, rather than the earlier one. The
  built-in bundle registers before any `ConfigureServices` delegate, so a
  consumer registration for a type the bundle covers now takes effect where it
  was previously ignored. Unrelated types that both accept a message still keep
  the first registration. A consumer that registered a second handler for a type
  and relied on the first one winning removes the registration it does not want.
- `IOutput.Write` and `WriteLine` hand a `string` and a `FormattableString` to the
  registered message writers instead of rendering them themselves, so the built-in
  `StringMessageWriter` and `FormattableStringMessageWriter` are now the code that
  writes them and a consumer writer registered for either type replaces the
  built-in one. The rendered output is unchanged: a string is still written
  verbatim and a `FormattableString` (an interpolated string passed as
  `Write<FormattableString>($"...")`; a bare `$"..."` argument infers `string`)
  still renders as markup with its holes escaped and formatted by
  `string.Format`, so a format specifier such as `{total:N2}` and an
  `ICustomFormatter` on the provider are honoured.
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
- **Breaking:** `Win32ExceptionMessageFormatter.GetFormattedExceptionText`, a
  `protected static` helper that nothing called. A subclass that used it
  builds the same text from `Win32Exception.NativeErrorCode` and `Message`;
  `GetExceptionText` remains the override point.

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
