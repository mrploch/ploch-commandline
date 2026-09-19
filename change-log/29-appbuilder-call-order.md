### Changed

- **Breaking:** `AppBuilder.ConfigureServices`, `AppBuilder.ConfigureAppConfiguration`,
  and `AppBuilder.ConfigureHost` record into one sequence, applied to the host
  in the order the calls were made (#29). When two calls register the same
  service, the later call wins, whether it used `ConfigureServices` or
  `host.ConfigureServices` inside `ConfigureHost`. The same goes for application
  configuration sources supplying the same key, whether added with
  `ConfigureAppConfiguration` or `host.ConfigureAppConfiguration`. The host
  builder's own phases are unchanged: host configuration still runs before
  application configuration, and `ConfigureContainer` still runs after every
  service delegate, whatever the call order.

  Previously every `ConfigureHost` delegate ran after all `ConfigureServices`
  and `ConfigureAppConfiguration` delegates, whatever the call order, so

  ```csharp
  builder.ConfigureHost(host => host.ConfigureServices(s => s.AddSingleton<IThing, FromHost>()))
         .ConfigureServices(s => s.AddSingleton<IThing, FromServices>());
  ```

  resolved `IThing` to `FromHost`. It now resolves to `FromServices`. Code that
  relied on `ConfigureHost` winning must call it after the method it should
  override.
- **Breaking:** the application's `CancellationTokenSource` is registered after
  every caller service delegate, including those added through `ConfigureHost`,
  so a `ConfigureServices` call can no longer replace it in the container (#29).
  Commands always received the builder's own token, so a replaced registration
  only ever handed them a source that cancelled nothing.

  The builder's other defaults are unchanged: `appsettings.json` is still added
  before any caller configuration source and services bundles are still
  configured before any caller service registration, so both remain
  overridable.
