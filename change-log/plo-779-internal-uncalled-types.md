### Changed

- **Breaking:** `CommandAttribute`, `CommandInfo`, `CommandInfoFactory`,
  `CommandAppConfigurator`, `ICommandAppConfigurator` and
  `TypeMessageFormatter<TMessage>.IsWriter` are `internal`. Nothing on the
  library's runtime path uses them (`AppBuilder` never went through the
  configurator), so they are withdrawn from the 1.0 surface: widening later is
  non-breaking, narrowing after 1.0 is not (PLO-779). Code that configured a
  command application it owns through `CommandAppConfigurator` does what the
  configurator did: call `ICommandApp.Configure` on that application and
  construct `CommandAppExecutor` over it directly. An application built through
  `AppBuilder` uses `AppBuilder.ConfigureCommandApp`, which creates and
  configures its own command application. The command metadata types and
  `IsWriter` have no public replacement, as nothing in the library reads them.

### Removed

- **Breaking:** `Win32ExceptionMessageFormatter.GetFormattedExceptionText`, a
  `protected static` helper that nothing called (PLO-779). A subclass that used it
  builds the same text from `Win32Exception.NativeErrorCode` and `Message`;
  `GetExceptionText` remains the override point.
