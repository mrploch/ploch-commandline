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

- A second Ctrl+C dispatched while the first was still being handled could be
  suppressed as well, because unsubscribing inside the handler does not remove
  the delegate from an invocation list a concurrent raise has already captured.
  Both invocations set `e.Cancel = true`, so the advertised "second interrupt
  terminates" needed a third press. The handler is now one-shot via
  `Interlocked.Exchange` (#32).

- An exception thrown by a consumer's cancellation callback no longer takes the
  process down. `CancellationTokenSource.Cancel` runs those callbacks
  synchronously and wraps anything they throw in an `AggregateException`, which
  surfaced on the `CancelKeyPress` thread where it was unhandled. It is now
  caught and reported, so a failing callback cannot turn a graceful shutdown
  into a crash (#32).

### Changed

- **Breaking:** `CommandAppExecutor` and `CommandAppConfigurator` take a
  `CancellationToken` as a second constructor argument. It is required rather
  than optional so that an application built through either type cannot
  silently end up without cancellation. Code constructing them directly must
  supply one; `AppBuilder` passes `cancellationTokenSource.Token` already (#14).

  A token is taken rather than the `CancellationTokenSource` behind it, for
  three reasons. Neither type ever calls `Cancel`, so the source advertises a
  capability that is never used. The conversion only goes one way — `.Token` is
  free, whereas turning a token back into a source needs
  `CreateLinkedTokenSource` and another disposable to own — so accepting a token
  accepts strictly more callers, including anyone holding one from
  `IHostApplicationLifetime` or an outer pipeline. And because the source's
  `.Token` was read at execution time rather than at construction, disposing it
  in between made `Run`/`RunAsync` throw `ObjectDisposedException`; a token
  captured up front stays usable after its source is disposed (#14).

  `AppBuilder` still creates, cancels and registers the `CancellationTokenSource`
  itself: a command resolving it from the container can legitimately request
  shutdown, so that registration is deliberately unchanged.
