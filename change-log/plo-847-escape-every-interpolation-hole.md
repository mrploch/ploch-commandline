### Fixed

- `IOutput.MarkupInterpolated`, `MarkupLineInterpolated` and a `FormattableString`
  passed to `Write` / `WriteLine` escape every interpolation hole, not only the
  `string` ones. Spectre.Console's `MarkupInterpolated` escapes a hole only when
  its runtime value is a `string`; every other value was formatted and its text
  parsed as markup, so a value object rendering as `[Admin]` failed with *Could
  not find color or style 'Admin'*, a `StringBuilder` containing `[1]` or a hole
  such as `{42:[0]}` failed with *Unbalanced markup stack*, and a non-string
  value whose text happened to be a well-formed tag was silently styled instead
  of printed. Every hole is now formatted with the caller's provider and format
  specifier (an `ICustomFormatter` on the provider still receives the original
  value) and its text is escaped, so it prints verbatim; the template is still
  parsed as markup. A caller that worked around this by converting a value to a
  string first keeps working. Do not pre-escape a hole: `Markup.Escape(...)`
  inside an interpolation hole printed doubled brackets before and still does
  (PLO-847).
