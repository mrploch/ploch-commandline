### Changed

- **Breaking:** `DependencyInjectionTypeRegistrar.Register` registers the types
  Spectre hands it — commands, settings, type converters, pair deconstructors —
  as transient rather than singletons. A singleton command captured its transient
  dependencies for the life of the host, and every command in a host shared one
  `TokensArgumentsProcessor`, whose collected properties are per-invocation
  state. `RegisterInstance` still returns the instance it was given and
  `RegisterLazy` is still a lazily created singleton, as Spectre's contract
  requires. State that must outlive one resolution belongs in a singleton service
  the command depends on; re-registering the command type as a singleton from
  `ConfigureServices` has no effect, because Spectre's registration is queued
  after every `ConfigureServices` delegate and the last registration wins. A
  command that constructor-injects its own settings type now receives a fresh,
  unbound instance rather than the bound one Spectre passes to `Execute`
  (PLO-781).
- **Breaking:** `AppServicesBundle` registers `CommandArgumentsRootProcessor` as
  transient, like the `ICommandSettingsProcessor` instances it aggregates, so
  one command's settings processing can no longer leak into the next (PLO-781).
