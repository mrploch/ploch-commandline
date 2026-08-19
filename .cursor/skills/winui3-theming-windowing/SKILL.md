---
name: winui3-theming-windowing
description: WinUI 3 theming (Mica, Acrylic, light/dark mode, resource dictionaries), title bar customisation, AppWindow API, multiple windows, and window management patterns.
invocable: false
---

# WinUI 3 Theming and Window Management

## When to Use This Skill

Use when:
- Applying Mica or Acrylic background materials
- Customising the title bar
- Managing multiple windows
- Implementing light/dark mode switching
- Working with AppWindow API

## Background Materials

### Mica (Recommended Default on Windows 11)

Semi-opaque material incorporating desktop wallpaper. Sampled once at startup — very performant:

```csharp
// In MainWindow constructor or OnLaunched:
window.SystemBackdrop = new MicaBackdrop();

// Mica Alt variant (slightly different opacity):
window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
```

**Not available on Windows 10** — falls back to solid colour.

### Acrylic (Transient Surfaces)

Blurred translucent overlay. More expensive than Mica:

```csharp
window.SystemBackdrop = new DesktopAcrylicBackdrop();
```

Use for flyouts, context menus, and transient UI. Not recommended for main window backgrounds.

### Layered Material Model

| Surface | Resource |
|---------|----------|
| Window base | Mica or Acrylic via `SystemBackdrop` |
| Commanding areas (NavigationView) | `LayerOnMicaBaseAltFillColorDefaultBrush` |
| Container backgrounds | `LayerFillColorDefaultBrush` |

## Light/Dark Theme

WinUI 3 respects system theme automatically. To override per-window:

```csharp
// Set on the root FrameworkElement
if (window.Content is FrameworkElement root)
{
    root.RequestedTheme = ElementTheme.Dark;  // or Light, or Default (system)
}
```

### Theme-Aware Resource Dictionaries

```xml
<ResourceDictionary>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Light">
            <SolidColorBrush x:Key="MyAppBackground" Color="#FFFFFF" />
        </ResourceDictionary>
        <ResourceDictionary x:Key="Dark">
            <SolidColorBrush x:Key="MyAppBackground" Color="#1E1E1E" />
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

Always provide both `Light` and `Dark` dictionaries. Use semantic resource names (`TextFillColorPrimaryBrush`, etc.) from WinUI's built-in theme resources.

## Title Bar Customisation

### Minor Customisation (Colours Only)

```csharp
var titleBar = window.AppWindow.TitleBar;
titleBar.ForegroundColor = Colors.White;
titleBar.BackgroundColor = Color.FromArgb(255, 32, 32, 32);
titleBar.ButtonForegroundColor = Colors.White;
titleBar.ButtonBackgroundColor = Colors.Transparent;
```

### Full Custom Title Bar (Extend XAML Into Title Bar)

```csharp
// In MainWindow constructor:
ExtendsContentIntoTitleBar = true;
SetTitleBar(AppTitleBar); // AppTitleBar is a UIElement in XAML
```

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="48" />  <!-- Title bar row -->
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <Grid x:Name="AppTitleBar" Grid.Row="0">
        <TextBlock Text="My App" VerticalAlignment="Center" Margin="16,0" />
    </Grid>

    <!-- App content in Row 1 -->
</Grid>
```

### TitleBar Control (SDK 1.6+)

The newer `TitleBar` control simplifies custom title bars with built-in caption button handling.

## AppWindow API

Access from any WinUI 3 `Window` via `window.AppWindow` (SDK 1.4+):

```csharp
var appWindow = window.AppWindow;

// Resize and position
appWindow.Resize(new SizeInt32(1200, 800));
appWindow.Move(new PointInt32(100, 100));

// Title and icon
appWindow.Title = "My Application";
appWindow.SetIcon("Assets/icon.ico");

// Listen for changes
appWindow.Changed += (sender, args) =>
{
    if (args.DidSizeChange) { /* handle resize */ }
    if (args.DidPositionChange) { /* handle move */ }
};
```

### Window Presenters

```csharp
// Standard overlapped window with customisation
var presenter = OverlappedPresenter.Create();
presenter.IsMaximizable = true;
presenter.IsMinimizable = true;
presenter.IsResizable = true;
presenter.IsAlwaysOnTop = false;
appWindow.SetPresenter(presenter);

// Full-screen
appWindow.SetPresenter(FullScreenPresenter.Create());

// Compact overlay (picture-in-picture)
appWindow.SetPresenter(CompactOverlayPresenter.Create());
```

## Multiple Windows

Each `new Window()` creates a new HWND and AppWindow:

```csharp
var secondWindow = new Window();
secondWindow.Content = new SecondaryPage();
secondWindow.Activate();
```

**Known memory leak:** Closing a window may not release all references if event handlers are not unsubscribed (GitHub #9063). Explicitly unsubscribe all events before closing.

**Modal windows** require Win32 interop to set the owner — no pure WinRT API exists for this.
