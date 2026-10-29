# Release Notes

All notable changes to the `Ploch.CommandLine` packages are recorded here. Each
released version also has a [GitHub Release](https://github.com/mrploch/ploch-commandline/releases)
generated from the entries in [`change-log/`](./change-log/README.md).

## v1.0 — First supported release (2026-10-29)

The first supported release of the library. `Ploch.CommandLine.Spectre` replaces
the earlier `Ploch.Common.CommandLine` packages, which were never published and
have been retired.

### Packages

| Package | Description |
|---|---|
| `Ploch.CommandLine.Spectre` | Core framework: `AppBuilder`, command base classes, settings pipeline, output pipeline. |
| `Ploch.CommandLine.Spectre.Serilog` | Opt-in Serilog integration for structured logging, switched on with `UseSerilog`. |
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

- `AppBuilder` implements `IDisposable`: a builder created with `AppBuilder.Create` owns the
  `Console.CancelKeyPress` handler and the `CancellationTokenSource` it installs and releases
  both on `Dispose`, where previously every `Create` call left one more handler subscribed for
  the life of the process. Hold the builder in a `using` declaration that ends *after* the run
  returns, because the token stays live for the whole run; a builder constructed over a
  caller-supplied source leaves that source alone. `ConfigureCommandApp` on a disposed builder
  throws `ObjectDisposedException` (#11).
- `IMessageWriter.WritesLineTerminator`, which a writer uses to declare that its output already
  ends with a line break so `IOutput.WriteLine` does not add another. It is a default interface
  member returning `false`, so an existing writer that renders inline needs no change; only a
  writer that already emits a trailing newline returns `true`, as `EnumerableMessageWriter` and
  `ExceptionMessageWriter` now do, so `WriteLine(aCollection)` no longer leaves a blank line
  after the last item (#31).
- A `culture` parameter on `UseSerilog`, `AddSerilog`, `ConfigureSerilog` and the
  `SerilogConfigurationBundle` constructor, controlling how values are formatted in both log
  files. Source that calls these methods compiles unchanged, but the signatures changed, so an
  assembly compiled against an earlier build must be rebuilt (#65).
- `ExceptionHandlerFailedException`, thrown by `AppCommand<TSettings>` and
  `AsyncAppCommand<TSettings>` when the registered `IExceptionHandler` throws while
  handling an exception. The application's Spectre handler reports it once and exits
  with `ExitCode.Error`; an application that calls `PropagateExceptions()` catches it
  instead, with the handler's exception as `InnerException` and the one it was handling
  as `Original` ([PLO-782](https://linear.app/ploch/issue/PLO-782)).

### Changed

- **Breaking:** `IOutput.Write(string)` and `WriteLine(string)` write the text
  verbatim instead of parsing it as Spectre markup, following Spectre.Console's
  own split between `Write(string)` and `Markup(string)`. A caller that relied on
  `Write("[green]ok[/]")` rendering green text should call `Markup` /
  `MarkupLine`, or `MarkupLineInterpolated` for a string with values in it.
  `FormattableString` messages are unchanged and still render as interpolated
  markup with their string holes escaped.
- **Breaking:** `IOutput` gains the two `Markup` / `MarkupLine` members, so a
  custom implementation of the interface must add them.
- **Breaking:** Serilog is opt-in. `Ploch.CommandLine.Spectre` no longer depends
  on `Ploch.CommandLine.Spectre.Serilog`, and `AppServicesBundle` no longer
  registers `SerilogConfigurationBundle`, so an application that references only
  the core package resolves `ILogger<T>` from the host's default providers and
  creates no application log file. To keep the Serilog logging of previous
  builds, add the
  Serilog package and opt in on the builder, once:
  `dotnet add package Ploch.CommandLine.Spectre.Serilog`, then
  `AppBuilder.Create(args).UseSerilog()` (`using Ploch.CommandLine.Spectre.Serilog;`),
  with `logName:`, `logPath:`, `template:` and `culture:` as optional arguments.
  An application that already calls `services.AddSerilog(context.Configuration, ...)`
  from `ConfigureServices` needs no `UseSerilog`, but it must reference
  `Ploch.CommandLine.Spectre.Serilog` directly, as the package no longer arrives
  through the core package; with that reference the call compiles unchanged and is
  now the only Serilog registration rather than a second one on top of the core
  bundle's, so the container holds one `Serilog.ILogger` descriptor
  ([PLO-778](https://linear.app/ploch/issue/PLO-778),
  [PLO-765](https://linear.app/ploch/issue/PLO-765)). The Serilog package
  depends on the core package rather than the other way round.

- **Breaking:** `AppCommand<TSettings>.DoExecute` and
  `AsyncAppCommand<TSettings>.DoExecuteAsync` accept a `CancellationToken`.
- **Breaking:** `IMessageFormatterProcessor.WriteMessage` returns the `IMessageWriter` that
  rendered the message, or `null` when none could handle it (it returned nothing, then a `bool`,
  in unpublished builds); a caller needs the writer to consult `WritesLineTerminator`, and tests
  the result with `is not null` (#20, #31).
- **Breaking:** `IFormatProvider` is threaded through the whole output pipeline.
  `IMessageFormatter.GetMessage`, `IMessageFormatterProcessor.GetMessageText` and
  `WriteMessage`, and `IMessageWriter.Write` each take a trailing optional
  `IFormatProvider? formatProvider = null`. Source-compatible for callers; a type that
  implements or overrides these members must add the parameter, and compiled consumers must
  be rebuilt (#11).
- **Breaking:** `AppCommand<TSettings>` takes the same constructor arguments as
  `AsyncAppCommand<TSettings>` — `(CommandArgumentsRootProcessor settingsProcessor,
  ICommandSettingsValidator<TSettings> validator, IExceptionHandler exceptionHandler, IOutput output)`
  — and exposes the output as the protected `Output` property. A derived command adds the
  processor and the `IOutput` to its own constructor and passes them to the base; both are
  registered by `AppServicesBundle`, so container-resolved commands need no registration change
  (#33).
- **Breaking:** `UseCaseAsyncCommand<...>` no longer echoes the settings before running the use
  case: the echo printed every public settings property, password or token options included.
  It is opt-in through `protected virtual bool EchoSettings`, which defaults to `false`; a
  command whose settings carry nothing sensitive overrides it to return `true` (#11).
- **Breaking:** `FluentCommandSettingsValidator<TSettings>` takes an `IServiceScopeFactory`
  instead of an optional `IValidator<TSettings>` and resolves the validator inside a scope per
  validation, so a consumer validator may keep FluentValidation's scoped lifetime and depend on
  a `DbContext` or another scoped service. Only code that constructed the validator by hand is
  affected (#11).
- **Breaking (log output):** both Serilog file sinks — the main log and the errors log — format
  values with `CultureInfo.InvariantCulture` instead of the current culture, so a file reads the
  same whatever the locale of the machine that wrote it: on `de-DE`, `{Amount:N2}` is now
  `1,234.50` rather than `1.234,50`, and the timestamp's time separator is always `:`. Pass
  `culture: CultureInfo.CurrentCulture` to restore locale-formatted files; console output is
  unchanged (#65).
- `IOutput.WriteLine<TMessage>` dispatches through the same path as `Write<TMessage>`, so the
  registered writers and `IRenderable` messages are honoured on both; a registered formatter
  applies where the selected writer consults it, as `EnumerableMessageWriter` does, and in the
  decorating members (`WriteBold`, `WriteError` and their line forms). A message that is not an
  `IRenderable` and that no writer accepts is written as plain text rather than parsed as markup
  (#31).
- Message formatters and writers prefer the most derived matching handler over a broader one,
  rather than the first registered; two handlers for the same type resolve to the later
  registration, and matching handlers for unrelated types keep registration order. So
  `Win32Exception` reaches `Win32ExceptionMessageFormatter`, and a consumer formatter for its
  own exception type is no longer shadowed by the built-in `ExceptionMessageFormatter`
  registered before it (#11).
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
  stops the run before the command, as any failure ahead of execution does; the
  application's Spectre handler passes it to the registered `IExceptionHandler`,
  whose return value is the exit code (`ExitCode.Error` by default, see PLO-782),
  rather than the command's own catch. The library registers the
  announcement as an `ICommandInterceptor` ahead of every other registration, so
  one you register through a services bundle, `ConfigureServices` or Spectre's
  `SetInterceptor` runs after it.
  `ConsoleAppInfoExtensions.PrintAppInfo` takes the `IAnsiConsole` to print to;
  the parameterless overload, which wrote to standard output, is gone
  ([PLO-783](https://linear.app/ploch/issue/PLO-783)).
- **Breaking:** a command line `Spectre.Console.Cli` cannot parse or bind, and settings
  that fail `Validate`, exit with `ExitCode.InvalidInput` (2) instead of Spectre's own
  `-1` (`255` in a POSIX shell). `AppBuilder.ConfigureCommandApp` installs a Spectre
  exception handler that keeps Spectre's error rendering and maps the exception to the
  documented exit codes: `CommandParseException` and `CommandRuntimeException` — the
  types Spectre raises for an unknown command or option, a missing required argument, a
  value that does not convert, and a validation failure — exit 2; a requested
  cancellation that reaches Spectre, from a command outside the base classes, exits
  with Spectre's configured cancellation exit code (130 by default; the base classes
  still return `ExitCode.Cancelled`); any other exception that escapes a command goes
  to the registered `IExceptionHandler`, whose return value is the exit code; and a
  failure before the host exists — a `ConfigureServices` delegate that throws — is
  reported and exits with `ExitCode.Error` (1), where it was also `-1`, as is a
  `CommandRuntimeException` Spectre raises before the host exists, such as a command
  whose settings type it cannot determine. An
  `IExceptionHandler` that throws is reported once and the run exits 1; the base
  classes no longer let its exception escape to Spectre. Spectre uses `CommandRuntimeException`
  for a few failures that are not input errors (a missing type converter, a dependency
  it could not resolve); they exit 2 as well. A script that tested for `255` tests for
  `2`; an application that wants a different mapping calls `SetExceptionHandler` or
  `PropagateExceptions()` in its `ConfigureCommandApp` delegate, which replaces the
  installed handler. Help, and Spectre's own `1` for a branch or default command invoked
  without what it needs, are unchanged
  ([PLO-782](https://linear.app/ploch/issue/PLO-782)).
- **Breaking:** `CommandAppExecutor` takes a `CancellationToken` as a second constructor
  argument, required rather than optional so that an application built through it cannot
  silently run without cancellation. Code constructing the executor directly must supply one;
  `AppBuilder.ConfigureCommandApp` passes the token of the builder's `CancellationTokenSource`,
  whether the builder created it or the caller supplied it (#14).
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
  still renders as markup with its string holes escaped and formatted by
  `string.Format`, so a format specifier such as `{total:N2}` and an
  `ICustomFormatter` on the provider are honoured.
- `Ploch.CommandLine.Spectre` declares no `Serilog.*` package as a dependency.
  It first dropped the eight direct `Serilog.*` references, which duplicated what
  its `Ploch.CommandLine.Spectre.Serilog` dependency already declared, and then
  that dependency itself when Serilog became opt-in (see the breaking entry
  above); a consumer that wants Serilog restores the same graph by adding the
  Serilog package.

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
- `UseCaseAsyncCommand` no longer throws when a command that opts in to `EchoSettings` has a
  settings value of a non-string type whose text contains `[` (a value object, a `StringBuilder`,
  a formatted number): Spectre.Console escapes only holes whose runtime value is a `string`, so
  such a value was parsed as markup (a valid tag changed the rendered style, an invalid one
  threw), while string values were already safe. Every value is now
  rendered with composite formatting, as before, and passed as a string, so it is escaped and
  printed verbatim ([PLO-824](https://linear.app/ploch/issue/PLO-824)).
- `IOutput.MarkupInterpolated` and `MarkupLineInterpolated` throw `ArgumentNullException` for a
  `null` argument, as documented and as `Markup` / `MarkupLine` already did, instead of letting
  the null reach Spectre.Console as a `NullReferenceException` ([PLO-824](https://linear.app/ploch/issue/PLO-824)).
- `AppCommand<TSettings>` never ran the `CommandArgumentsRootProcessor`, so every registered
  settings processor — token expansion included — was silently skipped for synchronous commands
  while `AsyncAppCommand<TSettings>` applied them. Both bases now do the same framework work
  around the implementation (#33).
- `IOutput.WriteError`, `WriteErrorLine`, `WriteBold` and `WriteBoldLine` threw
  `InvalidOperationException` for a message containing a `[` that was not a valid style tag
  (`WriteError("Value [archive] is invalid")` crashed with *Could not find color or style
  'archive'*). The content is escaped before the library wraps it in its own tag (#31).
- A format specifier on an interpolated argument was dropped: `$"total: {1234.5:N2}"` rendered
  as `total: 1234.5`, because every argument was stringified before the string was composed.
  Arguments no formatter claims are carried through as the original object (#31).
- `IOutput.Write` honours the supplied `IFormatProvider` for every value, not only for a
  `FormattableString`: `Write(1234.5, germanCulture)` rendered `1234.5`, and the provider was
  lost before it reached `EnumerableMessageWriter` and the formatters it consults (#11).
- `UseCaseAsyncCommand` renders a failed result's validation errors. `Result.Invalid(...)` stores
  its messages in `ValidationErrors` and leaves `Errors` empty, and only `Errors` was read, so an
  invalid request reached the console as `Use case failed:` with nothing after it (#11).
- The start-up banner no longer crashes on an application name or description containing `[`:
  `Widget [Dev]` threw *Could not find color or style 'Dev'* while rendering its own banner (#11).
- `EnumerableMessageFormatter` renders each item's text when no `IMessageFormatterProcessor` is
  supplied; every line was a bare emoji and the item itself was dropped (#11).
- `EnvironmentSettings.Current` used a double-checked lock whose fast path read a non-volatile
  field, so another thread could observe a half-initialised instance on ARM64 (#11).
- `FluentCommandSettingsValidator<TSettings>` was a singleton that took the scoped FluentValidation
  validator by constructor, a captive dependency that threw *Cannot consume scoped service ...
  from singleton* once scope validation was on, as it is in Development by default (#11).
- `AddCommandLineSettingsFluentValidation` registered the `ICommandSettingsValidator<>` mapping
  twice, so `IEnumerable<ICommandSettingsValidator<T>>` resolved two identical instances (#11).
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
- `AppBuilder(ConsoleAppInfo, CancellationTokenSource)`, `AppBuilder.ConfigureCommandApp`,
  `ConfigureServices`, `ConfigureHost` and `ConfigureAppConfiguration` (both overloads of the
  two that have them) and `EnvironmentSettings.Initialize` throw `ArgumentNullException` for a
  null argument instead of storing it and failing later, far from the caller (#22,
  [PLO-756](https://linear.app/ploch/issue/PLO-756)).
