### Fixed

- `services.AddSerilog()` called without an `IConfiguration` no longer throws
  `InvalidOperationException` ("configuration was not initialized"); the method's
  documentation always showed that call. `SerilogConfigurationBundle` registers the
  logger with its predefined sinks and the `Information` minimum level when no
  configuration is supplied (PLO-757). The bundle's `Configure` hook now runs in
  that case too, so a subclass override is applied with and without a
  configuration and receives `null` when there is none; previously it was never
  reached without one.

  **Breaking (source, nullability):** the override's parameter is now declared
  `IConfiguration?` to say so. With nullable warnings enabled, a subclass override
  declared with a non-nullable parameter produces CS8765 on recompilation (a
  build failure under warnings-as-errors); change its parameter to
  `IConfiguration?` and tolerate `null`. The CLR signature is unchanged, so
  already-compiled subclasses keep binding.
