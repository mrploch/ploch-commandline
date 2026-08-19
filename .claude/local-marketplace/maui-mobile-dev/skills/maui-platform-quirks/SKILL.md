---
name: maui-platform-quirks
description: Use when hitting .NET MAUI platform-specific bugs or planning version upgrades - Android 16 edge-to-edge changes, SafeAreaEdges, iOS layout/rotation bugs, version pinning strategy, Hot Reload reliability, resource/font/icon pitfalls, debugging with logcat. Triggers on - maui bug, edge to edge, safe area, status bar, upgrade maui, hot reload broken, font not showing, icon wrong, logcat, maui version.
---

# .NET MAUI Platform Quirks & Known Issues (mid-2026)

## Version strategy — pin, don't float

- MAUI majors are supported only ~6 months past their successor — annual upgrades are forced.
- Early .NET 10 service releases had production-blocking regressions; community consensus: **10.0.41+ first Android-viable patch, 10.0.50–51 regressed again**. Pin an explicitly vetted patch; re-run a smoke checklist on every bump.
- Smoke checklist after every SDK/patch bump: navigate every Shell route; rotate device on key screens; background/foreground the app; scroll a 100+ item CollectionView; check status/nav bar rendering; run in BOTH themes. Most regressions are visual and invisible to automated tests.

## Android 16 edge-to-edge (the big 2026 disruptor)

- Android 16 **removed the edge-to-edge opt-out**; .NET 10 flips `ContentPage` to edge-to-edge by default — a breaking visual change on upgrade (content under status/nav bars).
- Control insets with `SafeAreaEdges` (.NET 10, on Layout/ContentPage/Border/ScrollView…): `None`, `SoftInput`, `Container`, `Default`, `All`. Known inconsistencies across API levels (issues #32498, #33237 keyboard overlay).
- Google Play 2026 deadline: **new apps/updates must target API 36 from 31 Aug 2026** (existing apps API 35; extensions to 1 Nov). API 36 is the .NET 10 default target.

## Platform bug skew

- **iOS**: visual/layout — rotation, safe-area after backgrounding, tabs disappearing until restart.
- **Android**: rendering/perf — CollectionView, Shell tab bar spacing (#33444), edge-to-edge fallout.
- Notorious: native crash `0xc0000374` heap corruption (#25837); `AsyncRelayCommand` crashes on some patch levels. Search dotnet/maui issues before assuming your code is at fault.

## iOS from Windows

- **Hot Restart is REMOVED in VS 2026** — Pair to Mac (physical or cloud Mac, e.g. MacStadium) is mandatory for iOS builds from Windows.
- .NET MAUI 10 requires **Xcode 26.x** on the Mac (latest SRs need 26.6 / macOS Tahoe 26.2+) — pin and audit the Mac agent's Xcode on every MAUI bump.
- Provisioning: prefer automatic provisioning; on `MissingEntitlement`, regenerate (not just re-download) profiles after capability changes.

## Hot Reload reliability

- XAML and C# Hot Reload can fail independently — **restart the debug session before assuming your change is wrong**.
- Requirements: start with F5 (debugger attached), latest tooling, iOS linker "Don't Link" in Debug.
- Consult dotnet/maui wiki "Diagnosing Hot Reload" before filing issues.

## Resources: fonts, icons, splash

- Font registration name must match the file name exactly **including case** — top cause of "font works on Android, not iOS". Fonts can silently fail to embed on Android: delete `bin`/`obj` first when troubleshooting.
- Image/icon/splash filenames: lowercase, alphanumeric+underscore, start/end with a letter.
- Android 12+ adaptive icon mask: with background = 240×240dp content inside 160dp circle; without = 288×288dp inside 192dp circle. Use `BaseSize` on SVGs; `MauiSplashScreen` `Resize` is unreliable — some teams ship a minimal native splash + custom in-app splash page.

## Debug logging per platform

| Platform | Command |
|---|---|
| Android | `adb logcat -v time` (filter `mono\|DOTNET\|AndroidRuntime\|<appname>`); crashes: `adb logcat -b crash` |
| iOS | Mac Console.app / Xcode device console (no VS equivalent) |
| Windows | VS Diagnostic Tools |

Field diagnostics: `Sentry.Maui` (`builder.UseSentry(o => o.Dsn = ...)`) — native + managed crashes, lifecycle breadcrumbs. App Center is retired. Forward `BindingDiagnostics.BindingFailed` into Sentry breadcrumbs (see maui-data-binding).

## Threading edge cases

- `MainThread.BeginInvokeOnMainThread` from a background thread on WinUI can throw `Unable to find main thread` (#2451); in custom platform heads it can throw `NotImplementedInReferenceAssemblyException` — prefer `Dispatcher.Dispatch` in those contexts.
