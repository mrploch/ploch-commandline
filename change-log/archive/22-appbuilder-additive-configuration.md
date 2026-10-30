### Changed

- **Breaking:** `AppBuilder.ConfigureServices`, `AppBuilder.ConfigureHost`, and
  `AppBuilder.ConfigureAppConfiguration` are additive: every delegate passed to
  them is applied, in the order it was added. Previously each call replaced the
  delegate recorded by the previous one, so all but the last was silently
  discarded (#22).

  This matches `IHostBuilder.ConfigureServices` and
  `IHostBuilder.ConfigureAppConfiguration`, which `AppBuilder` wraps, and
  `AppBuilder.AddServicesBundle`, which was already additive. Both overloads of
  `ConfigureServices` and `ConfigureAppConfiguration` record into the same
  sequence. Code that called one of these methods more than once and relied on
  last-call-wins must now collapse those calls into a single delegate.
- **Breaking:** those three methods, and both overloads of the two that have
  them, throw `ArgumentNullException` for a `null` delegate rather than
  recording it and failing later while the host is built (#22).
