# WinUI 3 Deployment Models

## Comparison

| Model | WindowsPackageType | Self-Contained | Size | Runtime Dependency | Store Distribution | Full API Access |
|-------|-------------------|----------------|------|-------------------|-------------------|----------------|
| **Packaged (MSIX)** | MSIX | No | Small | Runtime installed separately | Yes | Yes |
| **Unpackaged** | None | No | Smallest | Runtime must be pre-installed | No | Limited |
| **Self-contained** | None | Yes | +~200 MB | None | No | Limited |

## Packaged (MSIX) — Recommended Default

- MSIX install/uninstall with clean removal
- Microsoft Store distribution eligible
- Full access to all Windows App SDK APIs including identity-dependent ones
- Code signing required (Azure Trusted Signing recommended)
- Auto-update via `.appinstaller` file for sideloaded apps

### MSIX Auto-Update for Sideloaded Apps

Create an `.appinstaller` XML file hosted on an HTTP endpoint:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller Uri="https://myserver.com/MyApp.appinstaller" Version="1.0.0.0"
              xmlns="http://schemas.microsoft.com/appx/appinstaller/2018">
  <MainPackage Name="MyApp" Version="1.0.0.0" Publisher="CN=MyPublisher"
               Uri="https://myserver.com/MyApp.msix" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="24" />
    <AutomaticBackgroundTask />
  </UpdateSettings>
</AppInstaller>
```

## Unpackaged

- Distributed as simple EXE or via MSI
- **Critical issue:** silent launch failure if Windows App SDK runtime is missing
- Cannot use: push notifications, custom context menu extensions, certain shell integration APIs
- Simplest for internal/enterprise tools where runtime can be pre-deployed

## Self-Contained

- Bundles Windows App SDK libraries (~200 MB overhead)
- No external runtime dependency — true xcopy deployment
- Same API restrictions as unpackaged
- Best for standalone tools distributed outside controlled environments

## CI/CD Pipeline Pattern

1. `dotnet restore`
2. `dotnet build -c Release`
3. `dotnet test`
4. `dotnet publish` (generates MSIX or self-contained output)
5. Code signing (Azure Trusted Signing)
6. Publish to GitHub Releases / Microsoft Store / internal feed
