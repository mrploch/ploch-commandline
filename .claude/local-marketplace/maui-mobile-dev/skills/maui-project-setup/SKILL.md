---
name: maui-project-setup
description: Use when creating or configuring a .NET MAUI project - single-project model, TFMs, Resources pipeline (icons, splash, images, fonts), MauiProgram/App startup, platform folders, version pinning strategy. Triggers on - new maui app, maui project structure, maui csproj, maui resources, app icon, splash screen, MauiProgram.
---

# .NET MAUI Project Setup (.NET 10 era)

## Version landscape (mid-2026)

- **.NET 10** is current stable for MAUI; TFMs: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`.
- MAUI has its **own support policy**: a major version is supported only ~6 months after its successor ships — plan annual upgrades.
- **Pin patch levels deliberately.** Early .NET 10 service releases had production-blocking regressions (Android 16 edge-to-edge rendering, iOS safe-area/rotation, an `AsyncRelayCommand` crash). Community consensus: 10.0.41+ was the first production-viable Android patch; 10.0.50–51 regressed again. Vet each SR before taking it — don't blindly float.
- Scaffold: `dotnet new maui -n MyApp` (also `maui-blazor`, `mauilib`, `maui-blazor-web`).

## Single-project model

One csproj multi-targets all platforms. Verified .NET 10 template shape:

```xml
<TargetFrameworks>net10.0-android</TargetFrameworks>
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('linux'))">$(TargetFrameworks);net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
<OutputType>Exe</OutputType>
<UseMaui>true</UseMaui>
<SingleProject>true</SingleProject>
<MauiXamlInflator>SourceGen</MauiXamlInflator>  <!-- .NET 10: XAML → C# at build time -->
<ApplicationId>com.companyname.myapp</ApplicationId>
<ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>  <!-- user-visible -->
<ApplicationVersion>1</ApplicationVersion>                   <!-- build/store integer, bump every release -->
<WindowsPackageType>None</WindowsPackageType>                <!-- unpackaged Windows dev -->
```

`SupportedOSPlatformVersion`: Android 21 template default (24+ recommended in .NET 10), iOS/MacCatalyst 15.0, Windows 10.0.17763.0.

### Platforms/ folder

Only the folder matching the build TFM is compiled (true multi-targeting):

```
Platforms/Android/  MainActivity.cs, MainApplication.cs, AndroidManifest.xml
Platforms/iOS/      AppDelegate.cs, Program.cs, Info.plist
Platforms/Windows/  App.xaml(.cs), Package.appxmanifest
```

- Inline branches in shared files: `#if ANDROID … #elif IOS … #elif WINDOWS … #endif`.
- Larger platform code: declare a `partial` class/method in shared code, implement per platform under `Platforms/<X>/`.

### Resources/ pipeline

Single source, per-platform outputs generated at build:

| Item | Build action | Notes |
|---|---|---|
| `Resources/AppIcon/appicon.svg` + `appiconfg.svg` | `MauiIcon` | background + foreground layers, `Color` attr |
| `Resources/Splash/splash.svg` | `MauiSplashScreen` | `BaseSize="128,128"` |
| `Resources/Images/*` | `MauiImage` | **use SVG sources** → density PNGs generated; reference as `myimage.png` (lowercase, no spaces/dashes in filenames) |
| `Resources/Fonts/*` | `MauiFont` | register in `ConfigureFonts` |
| `Resources/Raw/**` | `MauiAsset` | opened via `FileSystem.OpenAppPackageFileAsync` |

iOS Asset Catalogs are NOT supported in single-project. Platform-specific resources override shared ones when both exist.

## Startup

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        // DI registrations here (see maui-dependency-injection)
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
```

`App` no longer sets `MainPage` (.NET 9+) — override `CreateWindow`:

```csharp
public partial class App : Application
{
    public App() => InitializeComponent();
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}
```

## .NET 10 features worth enabling

- **XAML Source Generation** (`MauiXamlInflator=SourceGen`, template default): compile-time XAML errors, faster startup. Per-file opt-out: `<MauiXaml Update="MyPage.xaml" Inflator="Runtime" />`.
- **Strict XAML compilation** — surfaces the otherwise-suppressed XC0022 "binding not compiled" warning:
  `<MauiStrictXamlCompilation>true</MauiStrictXamlCompilation>` (recommended; see maui-data-binding).
- **Implicit global xmlns** (preview): assembly-level `[XmlnsDefinition]` in `GlobalXmlns.cs` kills per-file xmlns boilerplate (`MauiAllowImplicitXmlnsDeclaration` + `EnablePreviewFeatures`).
- **Deprecations**: `ListView`/`TableView`/cells → `CollectionView`; `MessagingCenter` is now internal → `WeakReferenceMessenger`; `FadeTo`→`FadeToAsync`, `DisplayAlert`→`DisplayAlertAsync` etc.

## Recommended baseline packages

| Package | Why |
|---|---|
| `CommunityToolkit.Mvvm` (8.4+) | source-gen MVVM (see maui-mvvm-toolkit) |
| `CommunityToolkit.Maui` | Snackbar/Toast, Popup, behaviors, FileSaver… (`UseMauiCommunityToolkit()`) |
| `Sentry.Maui` | crash reporting (App Center is retired) |
| `Microsoft.Extensions.Logging.Debug` | template default, DEBUG logging |

MrPloch repos: follow `Directory.Build.props` / central package management conventions from `mrploch-development` as with any other repo; MAUI apps are application repos (`src/`, `tests/` layout per project-structure rule).
