### Fixed

- The cancellation token a command receives is now connected to the application.
  `CommandAppExecutor` called Spectre's `Run(args)` and `RunAsync(args)`
  overloads, which take no token, so the `CancellationTokenSource` that
  `AppBuilder.Create` builds and cancels on Ctrl+C never reached any command:
  it was registered in the container and otherwise unused. Commands were handed
  a token that could never be cancelled, which made the whole feature inert.
  The executor now passes that source's token to
  `ICommandApp.Run(args, cancellationToken)` and
  `ICommandApp.RunAsync(args, cancellationToken)` (#14).

- Ctrl+C can interrupt the application again. The handler set `e.Cancel = true`
  unconditionally, so a command that did not observe its token — a blocking
  call, or a third-party library in a tight loop — left the process
  unkillable from the keyboard. The handler now detaches itself, so the first
  interrupt cancels cooperatively and a second one terminates the process (#32).

### Changed

- **Breaking:** `CommandAppExecutor` and `CommandAppConfigurator` take a
  `CancellationTokenSource` as a second constructor argument. It is required
  rather than optional so that an application built through either type cannot
  silently end up without cancellation. Code constructing them directly must
  supply one; `AppBuilder` does this already (#14).
