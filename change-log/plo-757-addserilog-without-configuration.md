### Fixed

- `services.AddSerilog()` called without an `IConfiguration` no longer throws
  `InvalidOperationException` ("configuration was not initialized"); the method's
  documentation always showed that call. `SerilogConfigurationBundle` registers the
  logger with its predefined sinks and the `Information` minimum level when no
  configuration is supplied (PLO-757).
