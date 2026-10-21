### Changed

- `IOutput.Write` and `WriteLine` hand a `string` and a `FormattableString` to the
  registered message writers instead of rendering them themselves, so the built-in
  `StringMessageWriter` and `FormattableStringMessageWriter` are now the code that
  writes them, and a consumer writer registered for either type with
  `AddMessageWriter<TMessage, TWriter>()` replaces the built-in one. The rendered
  output is unchanged: a string is still written verbatim and a
  `FormattableString` (passed as `Write<FormattableString>($"...")`; a bare
  `$"..."` argument infers `string`) still renders as markup with its holes
  escaped and formatted by `string.Format`, so `{total:N2}` and an
  `ICustomFormatter` on the provider are honoured (PLO-784).
- `FormattableStringMessageWriter` no longer pre-renders the interpolation holes
  through the registered formatters. It did so only when called directly, and
  doing it on the `IOutput` path would have dropped format specifiers and
  changed how an exception or a collection in a hole renders. A writer that
  wants the formatters applied calls
  `IMessageFormatterProcessor.GetMessageText(FormattableString, ...)` itself
  (PLO-784).
- `MessageFormatterProcessor` selects the later of two writers or formatters
  registered for the same message type. The built-in bundle registers before
  any `ConfigureServices` delegate, so a consumer registration for `string`,
  `FormattableString`, `Exception` or `IEnumerable` now takes effect where it
  was previously ignored. Unrelated types that both accept a message still keep
  the first registration (PLO-784).
