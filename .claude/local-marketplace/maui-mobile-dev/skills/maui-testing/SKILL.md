---
name: maui-testing
description: Use when testing .NET MAUI apps - unit testing ViewModels (plain-TFM trick, mocking Essentials), device tests (DeviceRunners/XHarness), Appium UI automation with AutomationId, CI pipelines with headless Android emulators. Triggers on - maui test, unit test viewmodel, appium, ui test, device test, test maui app, emulator ci.
---

# Testing .NET MAUI Apps

Test pyramid that works for MAUI: **many unit tests** (plain TFM, milliseconds, run everywhere) → some integration tests → **thin device-test layer** (DeviceRunners) → **few Appium UI tests** (critical journeys only, nightly/release cadence). Plus a manual smoke checklist after every SDK bump — most MAUI regressions are visual and evade automated tests.

## 1. Unit tests — the plain-TFM trick

xUnit cannot reference MAUI TFMs. Two options:

**A (preferred): extract ViewModels/services into a class library** multi-targeting a plain TFM (matches MrPloch `src/Model` + `src/Data` layering):

```xml
<!-- MyApp.Core.csproj -->
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<UseMaui>true</UseMaui>
```

**B: add the plain TFM to the app project** and neutralize `OutputType` for it:

```xml
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<OutputType Condition="'$(TargetFramework)' != 'net10.0'">Exe</OutputType>
```

The test project (per MrPloch rules: xUnit **v3**, FluentAssertions, AutoFixture, Moq) targets `net10.0` and references the library — only the plain slice is compiled into tests.

**Verified on this machine (2026-07):** option B + `xunit.v3` 3.0.0 + FluentAssertions against a `net10.0;net10.0-android` MAUI app builds and tests green. Gotcha: with xUnit v3's Microsoft.Testing.Platform runner (`UseMicrosoftTestingPlatformRunner=true`, `OutputType=Exe`), run tests with **`dotnet run`** — plain `dotnet test` silently does nothing unless you add `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` (global.json/dotnet.config MTP opt-in also works).

## 2. Making ViewModels testable

- Inject Essentials **interfaces**, never call statics: `IConnectivity`, `IGeolocation`, `IPreferences`, `ISecureStorage` (register `.Current`/`.Default` in `MauiProgram`; see maui-dependency-injection).
- No `Shell.Current` / `Application.Current` in ViewModels — inject `INavigationService`.
- CommunityToolkit.Mvvm ViewModels are plain C# after source-gen — test like any class.

```csharp
public class ItemsViewModelTests
{
    [Fact]
    public async Task LoadItemsCommand_should_fall_back_to_cache_when_offline()
    {
        var connectivity = new Mock<IConnectivity>();
        connectivity.Setup(c => c.NetworkAccess).Returns(NetworkAccess.None);
        var store = new Mock<IItemStore>();
        store.Setup(s => s.GetItemsAsync()).ReturnsAsync([new Item { Name = "cached" }]);

        var vm = new ItemsViewModel(connectivity.Object, store.Object, Mock.Of<IItemsApi>());
        await vm.LoadItemsCommand.ExecuteAsync(null);

        vm.Items.Should().ContainSingle(i => i.Name == "cached");
    }
}
```

Page-leak regression test pattern (see maui-memory-leaks): hold a `WeakReference` to a page, force GC, assert `!IsAlive`.

## 3. Device tests (on emulator/simulator)

- **DeviceRunners** (mattleibow/DeviceRunners) — the current first-class option, linked from MS Learn: visual xUnit runner + XHarness CI runner; `dotnet test MyApp.DeviceTests.csproj -f net10.0-android`.
- XHarness itself is alive (dotnet/maui uses it internally) but low-level; prefer DeviceRunners.
- The old `shinyorg/xunit-maui` runner is archived — don't use.
- Keep this layer thin: platform-behaviour checks (SecureStorage, permissions plumbing) that mocks can't cover.

## 4. Appium UI tests (Microsoft's recommended UI automation)

Drivers: Android → **UIAutomator2**, iOS → XCUITest, Windows → Windows driver (WinAppDriver 1.2.1 exactly), Mac → Mac2. Stack per the official sample (BasicAppiumNunitSample): `Appium.WebDriver` 8.x + **NUnit** (deliberately NUnit at this layer even though unit layer is xUnit).

Essentials:

- Set **`AutomationId`** on every element a test touches — never locate by text/position.
- Android: `[Register("com.companyname.myapp.MainActivity")]` on `MainActivity` so Appium can activate the app.
- Page-object pattern; shared test project + per-platform setup fixtures sharing one namespace (NUnit `[SetUpFixture]` requirement).

```csharp
// Android capabilities
var options = new AppiumOptions
{
    AutomationName = "UIAutomator2",
    PlatformName = "Android",
    App = "path/to/app-Signed.apk",
};
options.AddAdditionalAppiumOption("appPackage", "com.companyname.myapp");
options.AddAdditionalAppiumOption("appActivity", "com.companyname.myapp.MainActivity");
driver = new AndroidDriver(new Uri("http://127.0.0.1:4723"), options);

// Locate by AutomationId
var counter = driver.FindElement(MobileBy.AccessibilityId("CounterBtn"));
counter.Click();
```

Flakiness defence: explicit `WebDriverWait` (never `Thread.Sleep`), disable animations on the AVD, `NoReset=true` for fast-deployment debug runs, retries as safety net only.

## 5. CI (GitHub Actions)

- Unit tests (plain TFM): `ubuntu-latest`, no MAUI workload needed — cheap and fast.
- Android/Windows builds: `windows-latest`; iOS/MacCatalyst: `macos-latest` (`dotnet workload install maui` needs Windows/macOS).
- Headless Android UI tests: `ReactiveCircus/android-emulator-runner@v2` with `-no-window -gpu swiftshader_indirect -noaudio`, `disable-animations: true`, KVM enable step on Linux, AVD snapshot caching via `actions/cache`.
- Android signing in CI: base64 keystore secret → decode to file → pass `AndroidSigningKeyStore`/`...KeyAlias`/`...KeyPass`/`...StorePass` (keystore path is relative to the **project**).

## 6. Local emulator runs (this machine)

See maui-android-tooling for the verified local workflow: AVD `pixel_api36`, headless boot, `dotnet build -f net10.0-android -t:Run -p:AdbTarget="-s emulator-5554"`, `adb exec-out screencap -p > shot.png` for visual verification.
