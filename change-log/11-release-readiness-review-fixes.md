### Fixed

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
