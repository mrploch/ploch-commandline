### Changed

- **Breaking (log output):** both Serilog file sinks configured by
  `ConfigureSerilog` / `AddSerilog` / `SerilogConfigurationBundle` — the main log
  and the dedicated errors log — now format values with
  `CultureInfo.InvariantCulture` instead of `CultureInfo.CurrentCulture`. Log
  files are read by tools and aggregated across machines, so a value must be
  written the same way whatever the host's locale (#65).

  On a non-invariant locale the file contents change wherever a value is
  rendered through the format provider: a property with a format specifier, such
  as `{Amount:N2}` or `{When:d}`, and every property value in a custom output
  template that renders `{Message}` without the `j` (JSON) specifier. On `de-DE`,
  for example, `{Amount:N2}` was written as `1.234,50` and is now `1,234.50`, and
  `{When:d}` was `19.09.2026` and is now `09/19/2026`. Unformatted property
  values in the default templates (`{Message:lj}`) were already written as
  culture-independent JSON and do not change.

  The timestamp is rendered with the same culture. In both default templates the
  `:` in `HH:mm:ss` is the culture's time separator, so on a locale such as
  `fi-FI` or `da-DK` a time previously written as `14.36.16` is now `14:36:16`.

  Console output from the Spectre.Console sink is unchanged.

### Added

- `ConfigureSerilog`, `AddSerilog` and the `SerilogConfigurationBundle`
  constructor take an optional `CultureInfo? culture` parameter that controls
  the formatting of both log files. Pass `CultureInfo.CurrentCulture` to restore
  the previous locale-formatted output (#65).

  Adding the parameter changes the method signatures, so assemblies compiled
  against an earlier version must be recompiled; source that calls these methods
  compiles unchanged.
