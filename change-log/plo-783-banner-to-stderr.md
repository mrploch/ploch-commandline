### Added

- `AppBuilder.ShowBanner(bool)` and `ConsoleAppInfo.ShowBanner`, to drop the
  start-up banner and the `Executing command …` line together (PLO-783).

### Changed

- **Breaking:** the start-up banner and the `Executing command …` line are written
  to standard error rather than standard output, so `myapp list | cat` carries only
  what the command rendered. The line names the command as invoked (`add`) rather
  than its settings type, is written by the application once the settings are
  bound and before the command's `Validate` runs, and appears for every command,
  not only those deriving from the base classes. `AppCommand<TSettings>` and
  `AsyncAppCommand<TSettings>` write nothing to `IOutput` of their own (the
  `Processing arguments…` line is gone), and a failure to write the announcement
  no longer reaches the command's `IExceptionHandler`: it stops the run before the
  command, as any failure ahead of execution does. The library registers the
  announcement as an `ICommandInterceptor` ahead of your registrations, so one you
  register through `ConfigureServices` or Spectre's `SetInterceptor` runs after it.
  `ConsoleAppInfoExtensions.PrintAppInfo` takes the `IAnsiConsole` to print to;
  the parameterless overload, which wrote to standard output, is gone (PLO-783).
