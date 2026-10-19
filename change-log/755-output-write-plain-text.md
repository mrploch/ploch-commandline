### Fixed

- `IOutput.Write(string)` and `WriteLine(string)` threw `InvalidOperationException`
  for any text that was not well-formed Spectre markup — a path such as
  `C:\logs[1].txt`, or user input echoed back — because every string was sent
  through the markup parser (PLO-755).

### Added

- `IOutput.Markup(string)` and `IOutput.MarkupLine(string)`: the explicit entry
  points for rendering a string as Spectre markup, alongside the existing
  `MarkupInterpolated` / `MarkupLineInterpolated` (PLO-755).

### Changed

- **Breaking:** `IOutput.Write(string)` and `WriteLine(string)` now write the text
  verbatim instead of parsing it as markup, following Spectre.Console's own split
  between `IAnsiConsole.Write(string)` and `IAnsiConsole.Markup(string)`. Code that
  relied on `Write("[green]ok[/]")` rendering green text should call `Markup` /
  `MarkupLine`, or `MarkupLineInterpolated` when the string carries values.
  `FormattableString` messages are unchanged and still render as interpolated
  markup with their holes escaped (PLO-755).

- **Breaking:** `IOutput` gains the `Markup` and `MarkupLine` members, so a
  custom implementation of the interface must add them (PLO-755).
