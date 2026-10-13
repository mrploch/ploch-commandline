### Changed

- **Breaking:** `CommandAttribute`, `CommandInfo`, `CommandInfoFactory`,
  `CommandAppConfigurator`, `ICommandAppConfigurator` and
  `TypeMessageFormatter<TMessage>.IsWriter` are `internal`. Nothing on the
  library's runtime path uses them (`AppBuilder` never went through the
  configurator), so they are withdrawn from the 1.0 surface: widening later is
  non-breaking, narrowing after 1.0 is not (PLO-779).

### Removed

- **Breaking:** `Win32ExceptionMessageFormatter.GetFormattedExceptionText`, a
  `protected static` helper that nothing called (PLO-779).
