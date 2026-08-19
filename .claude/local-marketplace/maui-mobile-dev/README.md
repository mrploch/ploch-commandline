# maui-mobile-dev — Claude Code plugin

End-to-end .NET MAUI mobile app development skills for the MrPloch workspace. Built 2026-07-25 from deep research (Microsoft Learn, dotnet/maui repo, community sources) plus machine-verified tooling workflows — the full pipeline (scaffold → `net10.0-android` build → headless emulator → deploy → app running → xUnit v3 ViewModel tests) was executed and confirmed on this machine.

## Install

```
/plugin marketplace add C:\DevNet\my\mrploch\.claude\local-marketplace   (already added if mrploch-dev is installed)
/plugin install maui-mobile-dev@mrploch-local
```

## Skills

| Skill | Covers |
|---|---|
| `maui-project-setup` | single-project model, TFMs, Resources pipeline, MauiProgram, .NET 10 features, version pinning |
| `maui-mvvm-toolkit` | CommunityToolkit.Mvvm 8.4+ partial properties, RelayCommand, WeakReferenceMessenger, threading |
| `maui-navigation-shell` | Shell routes, GoToAsync, IQueryAttributable parameters, modals, back-button interception |
| `maui-dependency-injection` | lifetimes for mobile (scoped trap), page+VM registration, DbContextFactory, HttpClientFactory |
| `maui-data-binding` | compiled bindings (x:DataType), surfacing silent binding failures, theming, C# markup |
| `maui-data-storage` | sqlite-net-pcl vs EF Core (iOS AOT constraints), Preferences vs SecureStorage caveats, offline-first |
| `maui-networking` | native handlers, resilience (Polly v8), Refit, gRPC limits, connectivity handling |
| `maui-auth-notifications` | WebAuthenticator, MSAL, Auth0, token storage/refresh, FCM/APNs/Notification Hubs |
| `maui-platform-features` | runtime permissions, geolocation, camera, maps, lifecycle, handler mappers, Blazor/Hybrid WebViews |
| `maui-testing` | plain-TFM unit tests (verified), Essentials mocking, DeviceRunners, Appium, CI emulators |
| `maui-memory-leaks` | event-handler leaks, DisconnectHandler, NSObject cycles, gcdump, leak regression tests |
| `maui-performance-trimming` | AOT/NativeAOT matrix, TrimMode, trim-unsafe APIs, startup tracing, CollectionView perf |
| `maui-platform-quirks` | Android 16 edge-to-edge, version pinning strategy, Hot Reload, fonts/icons, logcat, Sentry |
| `maui-publishing` | Android AAB/keystore signing, Play API-level policy, iOS provisioning, Windows MSIX/unpackaged |
| `maui-android-tooling` | this machine's SDK/JDK layout, sdkmanager/avdmanager/emulator/adb CLI workflows (verified) |

## Machine facts baked in (2026-07)

- .NET SDK 10.0.302; workloads `android`/`ios`/`maccatalyst`/`maui-windows` (VS 18.7-installed).
- Primary Android SDK: `C:\Program Files (x86)\Android\android-sdk` (read-only unelevated).
- Secondary user SDK (emulator, system images, AVDs): `%LOCALAPPDATA%\Android\Sdk`; AVD `pixel_api36` (API 36 google_apis x86_64).
- JDK: MS OpenJDK 21.0.8 at `C:\Program Files\Android\openjdk\jdk-21.0.8` — set `JAVA_HOME` for all Android CLI tools.

Research reports (full, with citations) archived in session scratchpad `maui-research-*.md`; distilled content lives in the skills.
