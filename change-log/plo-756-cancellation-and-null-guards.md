### Fixed

- **Breaking:** `AppCommand<TSettings>` and `AsyncAppCommand<TSettings>` reported every
  `OperationCanceledException` as exit code 130, so a `TaskCanceledException`
  from an `HttpClient` timeout, or an inner token the command cancelled itself,
  was presented as "the user cancelled" and never reached `IExceptionHandler`.
  Cancellation is now reported only when the token the command received has
  cancellation requested; any other `OperationCanceledException` goes to the
  handler like every other exception (PLO-756).

- `AppBuilder(ConsoleAppInfo, CancellationTokenSource)`,
  `AppBuilder.ConfigureCommandApp` and `EnvironmentSettings.Initialize` throw
  `ArgumentNullException` for a null argument instead of storing it and failing
  later, far from the caller (PLO-756).

### Changed

- **Breaking:** an `ICommandAppExecutor` permits one execution attempt. A second
  `Run` or `RunAsync` on the same executor — whatever the first attempt's
  outcome — throws `InvalidOperationException`, synchronously and before any
  task is returned from `RunAsync`. Running again takes a new executor over a
  freshly configured command application: `AppBuilder.ConfigureCommandApp`
  provides one when the application was built through `AppBuilder`, while an
  executor constructed directly or through `CommandAppConfigurator` needs a
  newly configured `ICommandApp`, because `Configure` wraps the same one. After
  a cancelled attempt the same builder's cancellation source stays cancelled,
  so a run after Ctrl+C starts from a new builder or a fresh source.
  Previously the second run reached Spectre, which, once the first run had
  built the host, rendered the host builder's "Build can only be called once"
  error and returned -1; an attempt may build that host and it can only be
  built once, so the executor now says so up front (PLO-756).
