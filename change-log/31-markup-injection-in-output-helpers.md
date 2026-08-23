### Fixed

- `IOutput.WriteError`, `WriteErrorLine`, `WriteBold` and `WriteBoldLine` threw
  `InvalidOperationException` for any message containing a `[` that was not a
  valid Spectre style tag — `WriteError("Value [archive] is invalid")` crashed
  with *Could not find color or style 'archive'*. The content is now escaped
  before the library wraps it in a markup tag (#31).

- A format specifier on an interpolated argument was silently dropped:
  `$"total: {1234.5:N2}"` rendered as `total: 1234.5` because every argument was
  stringified before the string was composed, leaving `N2` applied to a `string`.
  Arguments no formatter claims are now carried through as the original object (#31).

### Changed

- `IOutput.WriteLine<TMessage>` now dispatches through the same path as
  `IOutput.Write<TMessage>`, so registered `IMessageFormatter` and
  `IMessageWriter` instances and `IRenderable` messages are honoured on both
  methods rather than only on `Write` (#31).

  A message that is neither a `string` nor a `FormattableString` and has no
  registered formatter or writer is now written as plain text instead of being
  parsed as markup. Callers relying on `WriteLine(someObject)` to render markup
  from `ToString()` should pass a `FormattableString`, or use
  `MarkupLineInterpolated`.

Markup written by the caller is unaffected: `Write`, `WriteLine`,
`MarkupInterpolated` and `MarkupLineInterpolated` still interpret markup in a
string the caller supplies. Only the tag this library adds on the caller's
behalf now escapes the content it wraps.
