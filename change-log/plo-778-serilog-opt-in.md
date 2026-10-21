### Changed

- **Breaking:** Serilog is opt-in (PLO-778). `Ploch.CommandLine.Spectre` no longer
  depends on `Ploch.CommandLine.Spectre.Serilog`, and `AppServicesBundle` no longer
  registers `SerilogConfigurationBundle`, so an application that references only
  the core package resolves `ILogger<T>` from the host's default providers and
  writes no log file. To keep the Serilog logging of previous builds, add the
  Serilog package and opt in on the builder, once:
  `dotnet add package Ploch.CommandLine.Spectre.Serilog`, then
  `AppBuilder.Create(args).UseSerilog()` (`using Ploch.CommandLine.Spectre.Serilog;`),
  with `logName:`, `logPath:`, `template:` and `culture:` as optional arguments.
  An application that already calls `services.AddSerilog(context.Configuration, ...)`
  from `ConfigureServices` keeps working and needs no `UseSerilog`. The Serilog
  package now depends on the core package rather than the other way round.

### Added

- `AppBuilderSerilogExtensions.UseSerilog` in `Ploch.CommandLine.Spectre.Serilog`:
  queues `services.AddSerilog(context.Configuration, ...)` through
  `AppBuilder.ConfigureServices`, so Serilog reads the `Serilog` section of the
  host configuration (PLO-778).

### Fixed

- A consumer's `AddSerilog` call is now the only Serilog registration in the host
  rather than a second one on top of the core bundle's, so the container holds one
  `Serilog.ILogger` descriptor and the resolved `ILoggerFactory` is Serilog's
  (PLO-765). The host's own default `ILoggerFactory` descriptor stays registered,
  as it does with Serilog's `IHostBuilder.UseSerilog`; the Serilog factory is the
  one resolved.
