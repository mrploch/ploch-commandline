### Fixed

- `UseCaseAsyncCommand` no longer throws when a command that opts in to
  `EchoSettings` has a settings value of a non-string type whose text
  contains `[`: a value object, a `StringBuilder`, a number with a format
  that adds brackets. The echo interpolated each value as it was, and
  Spectre.Console escapes only holes whose runtime value is a `string`, so
  such a value was parsed as markup and failed with *Could not find color or
  style ...*; string values were already escaped. Every value is now rendered
  with composite formatting (the same current-culture `IFormattable` path as
  before) and passed as a string, so it is escaped and printed verbatim
  (PLO-824).
- `IOutput.MarkupInterpolated` and `MarkupLineInterpolated` throw
  `ArgumentNullException` for a `null` argument, as their documentation
  states and as `Markup` / `MarkupLine` already did; previously the null
  reached Spectre.Console and surfaced as a `NullReferenceException`
  (PLO-824).
