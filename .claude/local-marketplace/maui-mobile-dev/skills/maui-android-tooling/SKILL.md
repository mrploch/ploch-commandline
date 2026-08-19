---
name: maui-android-tooling
description: Use when building, deploying, or debugging .NET MAUI apps for Android from the CLI - covers workloads, Android SDK/JDK locations on this machine, sdkmanager/avdmanager/emulator commands, adb workflows, and deploying to an emulator or device. Triggers on - maui android build, android emulator, avd, sdkmanager, adb, deploy maui app, run on android.
---

# .NET MAUI Android Tooling (CLI-first, Windows)

## This machine's verified layout (2026-07)

| Component | Location |
|---|---|
| .NET SDK | 10.0.302 (`dotnet`), workloads `android`, `ios`, `maccatalyst`, `maui-windows` installed via VS 18.7 |
| Primary Android SDK | `C:\Program Files (x86)\Android\android-sdk` (API 34–36, build-tools 35/36, platform-tools, cmdline-tools) — **read-only unelevated** |
| Secondary (user) SDK | `C:\Users\krzys\AppData\Local\Android\Sdk` — emulator, system images, writable |
| JDK | `C:\Program Files\Android\openjdk\jdk-21.0.8` (Microsoft OpenJDK 21) — **not on PATH, no JAVA_HOME set globally** |

**Always** set `JAVA_HOME` before any Android CLI tool:

```bash
export JAVA_HOME="C:\\Program Files\\Android\\openjdk\\jdk-21.0.8"   # Git Bash
$env:JAVA_HOME = 'C:\Program Files\Android\openjdk\jdk-21.0.8'       # PowerShell
```

## Build & deploy

```bash
# Scaffold
dotnet new maui -n MyApp

# Build for Android (verified: ~40 s incremental-clean on this machine)
dotnet build -f net10.0-android

# Build + deploy + launch on the running emulator / attached device
dotnet build -f net10.0-android -t:Run

# Target a specific device when several are attached
dotnet build -f net10.0-android -t:Run -p:AdbTarget="-s emulator-5554"

# Release build (AAB by default; APK for sideloading)
dotnet publish -f net10.0-android -c Release
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormats=apk
```

If MSBuild cannot find the SDK/JDK, pass them explicitly:
`-p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\openjdk\jdk-21.0.8"`.

## SDK management (sdkmanager)

`sdkmanager.bat` lives in `<sdk>\cmdline-tools\latest\bin`.

```bash
SDKMGR="/c/Program Files (x86)/Android/android-sdk/cmdline-tools/latest/bin/sdkmanager.bat"
"$SDKMGR" --list_installed
# Installing INTO Program Files requires elevation. Unelevated installs go to the user SDK:
yes | "$SDKMGR" --sdk_root="C:\\Users\\krzys\\AppData\\Local\\Android\\Sdk" --licenses
yes | "$SDKMGR" --sdk_root="C:\\Users\\krzys\\AppData\\Local\\Android\\Sdk" \
    "emulator" "platform-tools" "system-images;android-36;google_apis;x86_64"
```

**Gotcha (verified):** unelevated install into Program Files fails with only a warning
(`Failed to read or create install properties file`) and **exit code 0** — always verify the
package directory exists afterwards.

`dotnet build -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory=<path> -p:AcceptAndroidSDKLicenses=true`
can also provision missing SDK pieces for a project.

## Emulator (AVD) workflow

All emulator bits live in the **user** SDK (`%LOCALAPPDATA%\Android\Sdk`):

```bash
export JAVA_HOME="C:\\Program Files\\Android\\openjdk\\jdk-21.0.8"
USDK="/c/Users/krzys/AppData/Local/Android/Sdk"

# Create an AVD (once)
echo no | "$USDK/cmdline-tools/latest/bin/avdmanager.bat" create avd -n pixel_api36 \
    -k "system-images;android-36;google_apis;x86_64" -d pixel_7 --force
# If cmdline-tools are absent in the user SDK, run avdmanager from the Program Files SDK
# but with AVDs it still writes to %USERPROFILE%\.android\avd (user-writable), and pass
# ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk so it finds the system image.

# List / start (headless works for CI and agent-driven runs)
"$USDK/emulator/emulator.exe" -list-avds
"$USDK/emulator/emulator.exe" -avd pixel_api36 -no-snapshot -no-boot-anim &
"$USDK/emulator/emulator.exe" -avd pixel_api36 -no-window -no-audio &   # headless

# Wait until fully booted
"$USDK/platform-tools/adb.exe" wait-for-device shell 'while [ "$(getprop sys.boot_completed)" != "1" ]; do sleep 1; done'
```

The adb server is machine-global: an emulator started from the user SDK is visible to MAUI
builds that use the Program Files SDK.

## adb essentials

```bash
ADB="/c/Users/krzys/AppData/Local/Android/Sdk/platform-tools/adb.exe"
"$ADB" devices                                   # list targets
"$ADB" logcat -v time | grep -i "MyApp\|DOTNET\|mono"   # app logs
"$ADB" logcat -b crash                           # crash buffer only
"$ADB" install path/to/app.apk                   # sideload
"$ADB" shell am start -n com.companyname.myapp/crc64....MainActivity
"$ADB" uninstall com.companyname.myapp
"$ADB" emu kill                                  # stop emulator
```

## Diagnosing build problems

- `dotnet build -f net10.0-android -v:diag > build.log` for full MSBuild detail.
- `dotnet workload list` — MAUI builds need `android` (+ `maui-windows` for the Windows head).
- First build after workload/template updates does heavy AOT-prep work — allow several minutes; later builds are incremental.
- Fast deployment (Debug default) skips full APK packaging; Release behaves differently — always test Release before shipping (linker/trimming issues surface only there).
