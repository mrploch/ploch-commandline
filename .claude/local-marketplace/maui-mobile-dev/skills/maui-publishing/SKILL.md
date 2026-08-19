---
name: maui-publishing
description: Use when publishing/releasing a .NET MAUI app - Android AAB/APK signing and keystores, Google Play requirements, iOS provisioning and App Store, Windows MSIX/unpackaged publish, versioning. Triggers on - publish maui, release build, sign apk, keystore, aab, play store, app store, msix, distribute app.
---

# Publishing .NET MAUI Apps

## Versioning (all platforms, in csproj)

```xml
<ApplicationDisplayVersion>1.2.0</ApplicationDisplayVersion> <!-- user-visible (versionName / CFBundleShortVersionString) -->
<ApplicationVersion>12</ApplicationVersion>                  <!-- integer, MUST increase per store submission -->
```

**Always scope `dotnet publish` to the app csproj, never the solution** (solution publish tries every project and breaks).

## Android

```bash
# One-time keystore
keytool -genkeypair -v -keystore myapp.keystore -alias myapp -keyalg RSA -keysize 2048 -validity 10000

# Signed release (produces AAB + APK by default in Release)
dotnet publish MyApp/MyApp.csproj -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=myapp.keystore \
  -p:AndroidSigningKeyAlias=myapp \
  -p:AndroidSigningKeyPass=$KEY_PASS \
  -p:AndroidSigningStorePass=$STORE_PASS

# APK-only for sideloading
-p:AndroidPackageFormats=apk
```

Gotchas:

- **Keystore path is relative to the PROJECT, not CWD** — classic CI failure.
- Google Play requires **AAB**; sideloading/testing uses APK.
- **Play policy 2026:** new apps/updates must target **API 36 from 31 Aug 2026**; existing apps ≥ API 35 (extension to 1 Nov 2026). API 36 is the .NET 10 default.
- Lose the keystore = lose the app identity — back it up; in CI store as base64 secret, decode at build time.
- Release uses AOT + trimming — behaviours differ from Debug; full Release test pass on a real device is mandatory before shipping (see maui-performance-trimming).

## iOS

- Build from Windows requires a paired Mac (Hot Restart removed in VS 2026); Mac needs the Xcode version matching your MAUI SR (Xcode 26.x for .NET 10).
- Provisioning: prefer **automatic provisioning** (Apple ID in IDE). Distribution requires a distribution certificate + App Store provisioning profile.
- `dotnet publish -f net10.0-ios -c Release -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="..." -p:CodesignProvision="..."` → `.ipa` → upload via Transporter/altool.
- `MissingEntitlement` after adding capabilities → regenerate the provisioning profile, don't just re-download.

## Windows

```bash
# Packaged (MSIX) — needs a signing cert (self-signed for dev/sideload)
dotnet publish MyApp/MyApp.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64

# Unpackaged (xcopy-style)
dotnet publish MyApp/MyApp.csproj -f net10.0-windows10.0.19041.0 -c Release \
  -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
```

- **.NET 10 RID change:** `win10-x64` is invalid — use `win-x64`.
- Set `WindowsPackageType` explicitly (especially Blazor Hybrid).
- Known bug: unpackaged builds can fail to load images/fonts that work packaged — test the unpackaged output specifically.
- Unpackaged apps need Windows App SDK runtime + VC++ redist on the target machine unless `WindowsAppSDKSelfContained=true`.

## CI release pipeline shape

1. Version bump (`ApplicationVersion`++) → tag.
2. `windows-latest`: Android AAB (signed via base64-keystore secret) + Windows MSIX; `macos-latest`: iOS IPA.
3. Store uploads: Play Console API / App Store Connect API (fastlane or GitHub Actions store actions).
4. Attach APK/MSIX artifacts to the GitHub Release for sideloading.
