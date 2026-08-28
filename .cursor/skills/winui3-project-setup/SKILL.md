---
name: winui3-project-setup
description: Set up WinUI 3 (Windows App SDK) desktop application projects with correct TFMs, packaging modes, project structure, and NuGet dependencies. Covers packaged (MSIX), unpackaged, and self-contained deployment models.
invocable: false
---

# WinUI 3 Project Setup

## When to Use This Skill

Use when:
- Creating a new WinUI 3 desktop application project
- Configuring TFMs, packaging mode, or deployment model
- Setting up solution structure for a multi-project WinUI 3 app
- Choosing between packaged, unpackaged, or self-contained deployment

## Reference Files

- [deployment-models.md](deployment-models.md): Detailed comparison of packaging and deployment options

## Target Framework Monikers

WinUI 3 uses platform-qualified TFMs:

```xml
<!-- Minimum supported (Windows 10 2004) -->
<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>

<!-- Windows 11 24H2 — unlocks latest APIs -->
<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
```

The version suffix is the **minimum OS version**, not the target runtime. Use `19041` for broadest compatibility, `26100` for latest APIs.

## Minimal Packaged App .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>MSIX</WindowsPackageType>
    <EnablePreviewMsixTooling>true</EnablePreviewMsixTooling>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.*" />
  </ItemGroup>
</Project>
```

## Unpackaged App

Set `WindowsPackageType` to `None` and remove `Package.appxmanifest`:

```xml
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
</PropertyGroup>
```

- Behaves like traditional Win32/WPF app
- Requires Windows App SDK runtime pre-installed on target machine
- Cannot use APIs requiring package identity (push notifications, custom context menus)
- Silent launch failure if runtime is missing — no error dialog

## Self-Contained App

Bundles the Windows App SDK runtime (~200 MB overhead):

```xml
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

## Recommended Solution Structure

```
src/
  MyApp/                              # WinUI 3 packaged app
    MyApp.csproj
    App.xaml / App.xaml.cs
    MainWindow.xaml / MainWindow.xaml.cs
    Pages/
      ShellPage.xaml
      HomePage.xaml
      SettingsPage.xaml
    Controls/                         # Custom UserControls
    Themes/
      Generic.xaml                    # TemplatedControl styles
    appsettings.json
    Package.appxmanifest
  MyApp.ViewModels/                   # Platform-agnostic ViewModel library
    MyApp.ViewModels.csproj           # Targets net9.0 or net10.0 (no -windows)
    MainViewModel.cs
    HomeViewModel.cs
  MyApp.Core/                         # Domain/business logic
    MyApp.Core.csproj
tests/
  MyApp.ViewModels.Tests/             # Standard xUnit/NUnit test project
    MyApp.ViewModels.Tests.csproj
```

**Key principle:** Keep ViewModels in a separate class library targeting plain `net10.0` (no `-windows` suffix). This ensures ViewModels are testable with standard test frameworks without WinUI dependencies.

## Essential NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.WindowsAppSDK` | WinUI 3 runtime and controls |
| `CommunityToolkit.Mvvm` | MVVM source generators and base types |
| `CommunityToolkit.WinUI.Controls.DataGrid` | DataGrid control (optional) |
| `Microsoft.Extensions.DependencyInjection` | DI container |
| `Microsoft.Extensions.Hosting` | Full host with config, logging (optional) |

## Visual Studio Templates

- **Blank App, Packaged (WinUI 3 in Desktop)** — minimal starting point
- **Template Studio** — generates complete multi-page apps with NavigationView, MVVM, DI

## Windows App SDK Versions (as of March 2026)

| Version | Status |
|---------|--------|
| 1.8.x | Current stable (recommended for new projects) |
| 2.0 Preview | Available but not production-ready |
| 1.7.x | Maintenance (end of support March 2026) |
