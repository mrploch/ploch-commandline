### Added

- `ExceptionHandlerFailedException`, thrown by the command base classes when the
  registered `IExceptionHandler` throws while handling an exception; the
  application's Spectre handler reports it once and exits with `ExitCode.Error`
  (PLO-782).

### Changed

- **Breaking:** a command line `Spectre.Console.Cli` cannot parse or bind, and
  settings that fail `Validate`, exit with `ExitCode.InvalidInput` (2) instead
  of Spectre's own `-1` (`255` in a POSIX shell). `AppBuilder.ConfigureCommandApp`
  installs a Spectre exception handler that keeps Spectre's error rendering and
  maps `CommandParseException` and `CommandRuntimeException` to 2, a requested
  cancellation reaching Spectre to its configured cancellation exit code (130 by
  default; the base classes still return `ExitCode.Cancelled`), any other
  exception escaping a command to the registered `IExceptionHandler`, and a
  failure before the host exists, or an `IExceptionHandler` that throws, to
  `ExitCode.Error` (1). A script that tested
  for `255` tests for `2`; an application that wants a different mapping calls
  `SetExceptionHandler` or `PropagateExceptions()` in its `ConfigureCommandApp`
  delegate, which replaces the installed handler (PLO-782).
