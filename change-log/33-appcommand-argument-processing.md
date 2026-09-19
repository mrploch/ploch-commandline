### Fixed

- Synchronous commands now get settings argument processing. `AppCommand<TSettings>`
  never ran the `CommandArgumentsRootProcessor`, so every registered
  `ICommandSettingsProcessor` — token expansion included — was silently skipped
  for commands derived from it, while `AsyncAppCommand<TSettings>` applied them.
  The two bases now do the same framework work around the implementation: print
  the `Executing command …` / `Processing arguments…` banner, run the settings
  processor before `DoExecute`, and route any exception (including one thrown by a
  settings processor) to the `IExceptionHandler`, with cancellation still reported
  as `ExitCode.Cancelled` (#33).

### Changed

- **Breaking:** `AppCommand<TSettings>` takes the same constructor arguments as
  `AsyncAppCommand<TSettings>`:
  `(CommandArgumentsRootProcessor settingsProcessor, ICommandSettingsValidator<TSettings> validator, IExceptionHandler exceptionHandler, IOutput output)`.
  Derived commands must add the processor and the `IOutput` to their own
  constructor and pass them to the base; both are already registered by
  `AppServicesBundle`, so container-resolved commands need no registration
  changes. The output is exposed as a protected `Output` property — use it rather
  than capturing the constructor parameter, which the compiler warns about
  (CS9107) (#33).
