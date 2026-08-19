---
name: maui-platform-features
description: Use when accessing device/platform features in .NET MAUI - runtime permissions, geolocation, camera/media picker, maps, sensors, app lifecycle events, platform-specific code, BlazorWebView/HybridWebView. Triggers on - permission, camera, photo, geolocation, gps, maps, sensors, lifecycle, platform specific, blazor hybrid, webview.
---

# Platform Features & Permissions in .NET MAUI

## Permissions — the universal pattern

```csharp
public async Task<bool> EnsurePermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
{
    var status = await Permissions.CheckStatusAsync<TPermission>();
    if (status == PermissionStatus.Granted) return true;

    if (Permissions.ShouldShowRationale<TPermission>())
        await Shell.Current.DisplayAlertAsync("Permission needed", "Explain why…", "OK");

    status = await Permissions.RequestAsync<TPermission>();   // MUST run on UI thread
    return status == PermissionStatus.Granted;
}
```

Rules:

- Request **at the moment of use**, never all at launch.
- Declare in **both** places: runtime request AND `AndroidManifest.xml` `<uses-permission>` / iOS `Info.plist` `NS*UsageDescription`. A missing iOS usage description = **instant crash**, not a denial dialog.
- Handle permanent denial (`Denied` + no rationale) by linking to `AppInfo.Current.ShowSettingsUI()`.

## Geolocation

```csharp
var location = await _geolocation.GetLocationAsync(new GeolocationRequest(
    GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
```

Inject `IGeolocation`; wrap in try/catch (`FeatureNotSupportedException`, `PermissionException`). `GetLastKnownLocationAsync()` is instant but stale — good default before a full fix.

## Camera & media

- **`MediaPicker`** (`CapturePhotoAsync`/`PickPhotoAsync`) hands off to the OS camera/gallery app — **no CAMERA permission required** (by design, via Intent).
- **`CommunityToolkit.Maui.Camera` `CameraView`** — embedded live preview; **requires** `CAMERA` manifest permission AND a runtime check before showing the view. Skipping the check causes SIGABRT crashes on iOS.
- Pick MediaPicker unless you truly need an in-app viewfinder.

## Maps

- `Microsoft.Maui.Controls.Maps`: Android (Google Maps — API key via `com.google.android.geo.API_KEY` meta-data in the manifest) + iOS (MapKit). **No Windows support** — `CommunityToolkit.Maui.Maps` fills the gap with a WebView Bing map.

## App lifecycle

Cross-platform events on `Window`: `Created`, `Activated`, `Deactivated`, `Stopped`, `Resumed`, `Destroying`.

| Cross-platform | Android | iOS |
|---|---|---|
| Activated / Deactivated | OnResume / OnPause | OnActivated / OnResignActivation |
| Stopped | OnStop | DidEnterBackground |
| Resumed | OnRestart | WillEnterForeground |

**Do not rely on `Destroying`/`OnSleep` for critical persistence** — bypassed on programmatic exit and OS kill; save state in `Deactivated`/`Stopped`. Fine-grained platform lifecycle: `builder.ConfigureLifecycleEvents(...)`.

## Platform-specific code

- Small branches: `#if ANDROID … #elif IOS … #endif` (+ `DeviceInfo.Platform` for runtime checks).
- Bigger pieces: partial class in shared code, per-platform implementations under `Platforms/<X>/` (compiled per-TFM automatically).
- Native control tweaks: handler mappers, e.g. remove Android Entry underline:

```csharp
Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
{
#if ANDROID
    handler.PlatformView.BackgroundTintList =
        Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
});
```

Prefer mapper customization (`AppendToMapping` / `PrependToMapping` / `ModifyMapping`) over full custom handlers; write a custom `ViewHandler<TVirtualView, TPlatformView>` only for genuinely new controls. Always implement `DisconnectHandler` cleanup in custom handlers (see maui-memory-leaks).

## Hybrid web content

- **BlazorWebView** — share Razor components with an existing Blazor web app (.NET 8+ "Hybrid + Web" template shares one RCL).
- **HybridWebView** — embed an existing non-Blazor HTML/JS bundle with a JS↔C# bridge (`InvokeJavaScriptAsync`, .NET 10 adds request interception via `WebResourceRequested`). Caveat: relies on dynamic JSON serialization — needs feature switches/care under full trimming.
