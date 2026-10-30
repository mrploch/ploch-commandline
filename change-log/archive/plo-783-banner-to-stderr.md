### Added

- `AppBuilder.ShowBanner(bool)` and `ConsoleAppInfo.ShouldShowBanner`, to drop the
  start-up banner and the `Executing command …` line together (PLO-783).

### Changed

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
  the parameterless overload, which wrote to standard output, is gone (PLO-783).
