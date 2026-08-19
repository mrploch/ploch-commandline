# Code Quality Standards

- Write minimal, readable, maintainable code.
- Split responsibilities across modules following existing conventions.
- Remove unused code.
- Minimise state; derive values when possible.
- Handle all possibilities; don't assume optionality.
- Error handling: fail fast on unrecoverable errors; no silent failures. Always log. For user-initiated actions, show user feedback.
- Comments: explain "why" for non-obvious logic.
- Logging: Use appropriate levels - error for unrecoverable failures, warn for recoverable issues with fallbacks, info for important state changes, debug for logic flow (not spammy). Include context in messages. Format: `[ModuleName] Message`.
- Maintain backward compatibility for stored state; implement migrations when required.
- Clean up local data on logout.
- Avoid nested ternaries.
- Never commit PII or potential PII to source code (names, emails, phone numbers, addresses, etc.). Use anonymised or fake data for tests and examples.

## Unused Expression Values (IDE0058 and similar)

- **Never add `_ =` discards to silence unused-expression-value diagnostics (IDE0058, or the same finding surfaced via SonarCloud) on fluent/chained API calls** — configuration builders (`optionsBuilder.UseSqlite(...)`, `services.AddX(...)`), guard extensions (`arg.NotNull(...)`), `StringBuilder.Append` chains, and similar methods whose return value exists only for chaining. The discard adds noise without expressing intent.
- Instead, **disable the diagnostic in `.editorconfig`** with a rationale comment: `dotnet_diagnostic.IDE0058.severity = none`. This is the established convention in `ploch-common` and `ploch-data` (both disable it repo-wide).
- Rationale: IDE0058 has no per-method/per-type exclusion mechanism — `excluded_symbol_names` only exists for `CAxxxx` quality rules, and callee-scoped suppression for IDE0058 is an open, unimplemented Roslyn request (dotnet/roslyn#57297, dotnet/roslyn#47832). Per-call-site discards or pragmas do not scale.
- Legitimate discard uses remain fine: fire-and-forget async (`_ = LoadAsync();`), intentionally touching a member for its side effect in tests, and out-parameter discards (`out _`).
