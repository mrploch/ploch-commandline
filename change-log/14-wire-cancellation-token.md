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

- A cancelled run no longer pauses for input on the way out. With
  `PauseBeforeExit` set, `Run`/`RunAsync` printed "Press Enter to exit..." and
  blocked on stdin even after Ctrl+C, turning the shutdown the user had just
  requested into a hang. The prompt is now skipped when the token is already
  cancelled (#14).

- The interrupt handler cancels before it writes to the console. It runs on the
  `CancelKeyPress` thread, where console I/O can block or throw; writing first
  risked skipping the cancellation entirely after `e.Cancel` had already
  suppressed termination, leaving the application neither stopped nor killable
  from the keyboard (#32).

- An exception thrown by a consumer's cancellation callback no longer takes the
  process down. `CancellationTokenSource.Cancel` runs those callbacks
  synchronously and wraps anything they throw in an `AggregateException`, which
  surfaced on the `CancelKeyPress` thread where it was unhandled. It is now
  caught and reported, so the callback's own exception no longer escapes the
  handler (#32). Reporting it writes to the console, which on that thread is
  itself best-effort — the guarantee is that the callback cannot take the
  process down, not that no console failure ever could.

- An interrupt that arrives after the builder has been disposed no longer
  suppresses itself. `AppBuilder` became `IDisposable` on `main` and releases the
  cancellation source it owns, so `Cancel()` on the `CancelKeyPress` thread can
  race disposal and throw `ObjectDisposedException`. The handler now hands that
  interrupt back to the default path — `e.Cancel = false` — instead of
  suppressing a press that cancels nothing: the run it exists to interrupt is
  already over, and a suppressed press that does nothing is the unkillable
  behaviour this change set exists to remove (#32).

### Changed

- **Breaking:** `CommandAppExecutor` and `CommandAppConfigurator` take a
  `CancellationToken` as a second constructor argument. It is required rather
  than optional so that an application built through either type cannot
  silently end up without cancellation. Code constructing them directly must
  supply one; `AppBuilder` passes the token of the source it owns already (#14).

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
