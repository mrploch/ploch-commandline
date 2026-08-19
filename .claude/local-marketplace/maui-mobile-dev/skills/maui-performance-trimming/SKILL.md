---
name: maui-performance-trimming
description: Use when optimizing .NET MAUI startup/size or configuring AOT and trimming - runtime/compilation matrix per platform, NativeAOT status, TrimMode, trim-unsafe APIs and their replacements, profiled AOT, CollectionView performance, startup tracing. Triggers on - slow startup, app size, aot, nativeaot, trimming, trim warning, publishaot, collectionview slow, profiling.
---

# Performance, AOT & Trimming in .NET MAUI

## Runtime/compilation matrix (.NET 10 defaults)

| Platform | Debug | Release |
|---|---|---|
| Android | Mono JIT + interpreter | Mono + Mono AOT (profiled) |
| iOS | Mono AOT + interpreter (device) | **Full Mono AOT — mandatory** (Apple forbids JIT) |
| Mac Catalyst | Mono | Mono AOT |
| Windows | CoreCLR JIT | CoreCLR + ReadyToRun |

- **NativeAOT** (`<PublishAot>true</PublishAot>`): stable for iOS/MacCatalyst since .NET 9 (~2× faster startup, ~½ size); **experimental on Android** in .NET 10 (matures in .NET 11, where CoreCLR also becomes Android's Release default).
- Never combine `TrimMode` with `PublishAot`; never set `PublishTrimmed` manually.
- `AndroidAotEnableLazyLoad` (default true) helps cold start.

## Trimming

Defaults: Android/MacCatalyst trim `partial` in Release; **iOS partially trims every device build**. `<TrimMode>full</TrimMode>` for max size reduction — then these break:

| Trim-unsafe | Replacement |
|---|---|
| String-path `{Binding}` without `x:DataType` | compiled bindings (see maui-data-binding) |
| `[QueryProperty]` | `IQueryAttributable` |
| Implicit conversion operators in XAML | `TypeConverter` + `[TypeConverter]` |
| `LoadFromXaml` runtime loading | compile-time XAML only |
| `SearchHandler.DisplayMemberName` | `ItemTemplate` |
| `OnPlatform`/`OnIdiom` markup extensions | `OnPlatform<T>` / `OnIdiom<T>` |
| HybridWebView dynamic JSON | feature switch + STJ source-gen |

- Preserve reflection-reached code: `[DynamicDependency]`, `TrimmerRootAssembly`, `TrimmerRootDescriptor` (sparingly).
- **Treat trim warnings as CI-breaking** for full-trim/NativeAOT targets; re-verify after EVERY SDK bump (real-world regression: .NET 10 trimmer stripped CommunityToolkit MediaElement constructors despite preservation rules).
- Libraries: `<IsTrimmable>true</IsTrimmable>`, `<SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>`, `<TrimmerSingleWarn>false</TrimmerSingleWarn>`.

## Startup tracing & profiling

```bash
dotnet tool install -g dotnet-trace dotnet-dsrouter dotnet-gcdump
dotnet-trace collect --dsrouter android-emu --format speedscope
dotnet build -t:Run -f net10.0-android -p:DiagnosticSuspend=true   # pause until profiler attaches
```

Open speedscope JSON at speedscope.app. .NET 10 also exposes `ActivitySource("Microsoft.Maui")` + `Meter` instrumentation (layout Measure/Arrange) — OpenTelemetry/Aspire-compatible.

## CollectionView performance (chronic hot spot, worst on Android)

- **Never nest CollectionView in a ScrollView** — kills virtualization.
- Keep `DataTemplate`s shallow — flatten with `Grid` rows/columns instead of nested stacks; template complexity is the dominant scroll-perf factor.
- No per-item async work in template constructors; no heavy converters per cell.
- Detach behaviors/triggers on templates when pages pop (leak + perf).
- `ListView`/`TableView` are deprecated — CollectionView is the only path; measure with the .NET 10 layout meters when scroll jank appears.

## Quick wins checklist

1. Compiled bindings everywhere + `MauiStrictXamlCompilation`.
2. XAML source generation on (.NET 10 template default).
3. Release-build testing on real devices — Debug (interpreter) hides AOT/trim/perf issues entirely.
4. SVG images with correct `BaseSize` (oversized rasters are a memory+jank source).
5. Startup: defer non-critical service init; don't block `CreateMauiApp` on I/O.
