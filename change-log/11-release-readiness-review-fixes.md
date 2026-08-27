### Fixed

- `IOutput.Write` honours the supplied `IFormatProvider` for a value a registered
  writer handles, not only for a plain scalar. The provider stopped at
  `WriteMessage`, so `Write(new[] { 1234.5 }, germanCulture)` reached
  `EnumerableMessageWriter` and its nested `ConvertibleMessageFormatter` with no
  provider and formatted with the current culture — rendering `1234.5` while the
  caller had asked for `1234,5`. The provider now travels with the message to the
  writer and on to any formatter the writer consults, and is applied when the
  interpolated string is finally rendered — that last step is where its holes are
  formatted, so a provider not applied there was silently discarded (#11).

- `UseCaseAsyncCommand` renders a failed result's validation errors.
  `Result.Invalid(...)` stores its messages in `ValidationErrors` and leaves
  `Errors` empty, and only `Errors` was read — so an invalid request reached the
  console as `Use case failed:` with nothing after it. Both collections are now
  rendered; a validation error carrying only an identifier falls back to that
  identifier, and a failure with no message at all reports its status rather than
  leaving the reason blank (#11).

- The start-up banner no longer crashes on an application name or description
  containing `[`. `PrintAppInfo` interpolated consumer-supplied metadata straight
  into a Spectre markup string, so an application called `Widget [Dev]` threw
  *Could not find color or style 'Dev'* while rendering its own banner. Both the
  name/version line and the description are now escaped, matching the contract
  settled in #31: markup is honoured where the caller wrote it and escaped where
  this library adds the tag (#11).

- Message formatters and writers are selected by specificity rather than by
  registration order, so `Win32Exception` reaches `Win32ExceptionMessageFormatter`
  and reports its native error code again. Selection was first-registered-wins over
  an `IsInstanceOfType` check, which made the built-in `ExceptionMessageFormatter`
  shadow every more specific formatter behind it — including any a consumer
  registers for their own exception type, since bundles register before consumer
  code. The processor now picks the most derived matching handler; registration
  order only breaks ties between unrelated types such as `IEnumerable` and
  `IConvertible` (#11).

- `IOutput.Write` honours the supplied `IFormatProvider` for ordinary values. The
  provider was applied only on the `FormattableString` path; every other value fell
  through to a parameterless `ToString()`, so `Write(1234.5, germanCulture)`
  rendered `1234.5` rather than `1234,5`. Omitting a provider still formats with
  the current culture, so the default is unchanged (#11).

- `EnvironmentSettings.Current` no longer uses a broken double-checked lock. The
  fast path read `_current` outside the lock while the field was not `volatile`, so
  another thread could observe a non-null reference before the writes that
  initialised it were visible. Benign on x86, not on ARM64 (#11).

- `FluentCommandSettingsValidator<TSettings>` resolves its FluentValidation
  validator per validation from a scope instead of taking one by constructor. It is
  registered as a singleton because Spectre resolves commands from the root
  provider, so injecting `IValidator<T>` — which `AddValidatorsFromAssemblies`
  registers as scoped by default — made it a captive dependency: resolving it threw
  *Cannot consume scoped service ... from singleton* once scope validation was on,
  as it is by default in Development. Consumer validators keep their default scoped
  lifetime and may depend on a `DbContext`, a repository or a scoped user context
  as usual (#11).

- `AddCommandLineSettingsFluentValidation` registers the
  `ICommandSettingsValidator<>` mapping once instead of twice. It registered the
  mapping itself and then invoked the bundle, which registers the same mapping;
  `AddSingleton` appends rather than replaces, so
  `IEnumerable<ICommandSettingsValidator<T>>` resolved two identical instances (#11).

### Changed

- **Breaking.** `IFormatProvider` is threaded through the whole output pipeline.
  `IMessageFormatter.GetMessage`, `IMessageFormatterProcessor.GetMessageText` and
  `WriteMessage`, and `IMessageWriter.Write` each take a trailing optional
  `IFormatProvider? formatProvider = null`. Source-compatible for callers, since
  the parameter is optional; binary-breaking for both callers and implementers,
  because the signatures changed — consumers must rebuild, and any type
  implementing or overriding these members must add the parameter (#11).

- **Breaking:** `UseCaseAsyncCommand<...>` no longer echoes the settings before
  running the use case. It printed every public settings property unconditionally,
  and a derived command is free to add a password, an API token or a connection
  string as an option — values a consumer never chose to disclose, and which
  console output routinely carries into CI logs. The echo is now opt-in via
  `protected virtual bool EchoSettings`, which defaults to `false`. A command whose
  settings carry nothing sensitive restores the previous behaviour by overriding it
  to return `true` (#11).

- **Breaking:** `FluentCommandSettingsValidator<TSettings>` takes an
  `IServiceScopeFactory` instead of an optional `IValidator<TSettings>`. It is
  resolved from the container rather than constructed directly, so this affects
  only code that instantiated it by hand (#11).

- The quick-start in the documentation site now compiles. It passed four arguments
  to `AppCommand<TSettings>`, which takes two, and omitted the `using` for
  `IOutput`. The corrected sample is compiled verbatim from the markdown as part of
  the review (#11).

- The documentation no longer claims every command base runs the
  settings-processing pipeline. `AppCommand<TSettings>` takes no
  `CommandArgumentsRootProcessor` and validates then executes directly; only the
  asynchronous bases run that pipeline (#11).

- `AddSerilog`'s `<remarks>` described a second, direct Serilog configuration step
  that was deliberately removed because it silently dropped the output template.
  The documentation now matches the implementation (#11).

### Fixed (second review round)

- **Behavioural change / bug fix:** `EnumerableMessageFormatter` renders each item
  again when no `IMessageFormatterProcessor` is supplied. Output that was
  previously a bare emoji per line now carries the item text, so a caller that
  wants empty item bodies must supply a processor returning an empty string. The null-conditional call returned
  `null`, so every line was a bare emoji and the item's own text was dropped —
  contradicting both the parameter being optional and the method's own
  documentation. It now falls back to the item's `ToString()` (#11).

### Changed (second review round)

- The standalone sample restores and builds again. `Ploch.Common` and
  `Ploch.Common.DependencyInjection` were pinned to `2.0.1` while the
  `Ploch.CommandLine.Spectre` package requires `>= 4.0.20-prerelease`, which
  failed with `NU1109` and then `CS7069`. Both are now `4.0.21-prerelease`.
  `PlochPackagesVersion` floats across the `1.0` prerelease line instead of a
  fixed `0.0.1-prerelease` that did not exist — NuGet was silently substituting
  the oldest published build under `NU1603` (#11, closes #46).

- The getting-started guide is reachable from the documentation site. It was
  built into the site but linked from neither the navigation nor the home page,
  and the package README still pointed at a TODO issue. Two pre-existing broken
  links to `samples/SampleApp/README.md` are now absolute GitHub URLs, since
  `samples/` is not part of the docfx content set (#11).

- Captured console output in `docs/GETTING_STARTED.md` and
  `samples/SampleApp/README.md` no longer publishes a contributor's workstation
  path and machine name (#11).

- The `summaries` rule, in both its `.claude` and `.cursor` copies, no longer
  hard-codes one contributor's absolute Windows path and a Windows-only viewer
  command. It now resolves a repository-relative `temp/` directory and lists the
  per-platform open command (#11).

- `Ploch.CommandLine.Spectre.FluentValidation.Tests` runs on xUnit v3, matching
  the other three Spectre test projects and the repository testing rule. It was
  the only project still on xUnit 2 with the XUnit2 AutoMoq package (#11).
