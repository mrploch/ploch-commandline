### Changed

- `Ploch.CommandLine.Spectre` no longer declares the eight `Serilog.*` packages
  (`Serilog`, `.Enrichers.Thread`, `.Extensions.Hosting`, `.Extensions.Logging`,
  `.Settings.Configuration`, `.Sinks.Console`, `.Sinks.File`, `.Sinks.SpectreConsole`)
  as direct dependencies. They duplicated what `Ploch.CommandLine.Spectre.Serilog`,
  which the core package depends on, already declares at the same versions, so the
  package graph a consumer restores is unchanged (PLO-758).
