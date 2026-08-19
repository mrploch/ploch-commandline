---
name: maui-auth-notifications
description: Use when adding authentication or push notifications to a .NET MAUI app - WebAuthenticator OAuth, MSAL/Entra ID, Auth0, token storage, FCM/APNs push, Azure Notification Hubs. Triggers on - login, oauth, authentication, msal, auth0, sign in, token, push notification, fcm, apns.
---

# Authentication & Push Notifications in .NET MAUI

## Authentication options (mid-2026)

| Scenario | Recommended |
|---|---|
| Entra ID / Microsoft accounts | **MSAL.NET** (`Microsoft.Identity.Client` ≥4.61 — MAUI-only, Xamarin dropped) + `.Broker` package for Authenticator/WAM broker |
| Auth0 tenant | `Auth0.OidcClient.MAUI` — the most complete, actively-maintained third-party MAUI SDK (Android/iOS/macOS/Windows) |
| Generic OIDC provider | `IdentityModel.OidcClient` + `WebAuthenticator` |
| Custom OAuth via your backend | built-in `WebAuthenticator` |
| Firebase Auth | weakest MAUI story — community `Plugin.Firebase` bindings only; avoid if you have a choice |

**Never embed client secrets in the app** — Microsoft's docs explicitly require the OAuth client role to live on a web backend; the app uses PKCE public-client flows.

## WebAuthenticator (built-in)

```csharp
var result = await WebAuthenticator.Default.AuthenticateAsync(
    new Uri("https://myapi.example.com/auth/login"),
    new Uri("myapp://callback"));
var accessToken = result.AccessToken;
```

Platform wiring required:

- **Android:** a `WebAuthenticatorCallbackActivity` subclass with an intent filter for the callback scheme; API 30+ also needs a `<queries>` package-visibility element in the manifest.
- **iOS/Mac Catalyst:** `CFBundleURLTypes` entry in `Info.plist` for the callback scheme.
- **Windows:** long-standing known-broken status (dotnet/maui#2702) — for Windows heads prefer MSAL/OidcClient which use their own brokers.

## Token storage & refresh

- Tokens go in **`SecureStorage`**, never `Preferences` (see maui-data-storage).
- Design refresh flows to tolerate `SecureStorage.GetAsync` returning `null` at any time — Android loses values after reinstall (Keystore invalidation), iOS may *keep* them across reinstalls (Keychain persists).
- On 401: attempt one silent refresh; on failure clear tokens and route to interactive sign-in. Serialize refreshes behind a `SemaphoreSlim` so parallel requests don't stampede the token endpoint.

## Push notifications

| Platform | Mechanism |
|---|---|
| Android | FCM — community standard: `Plugin.FirebasePushNotifications` (thomasgalliker); needs `google-services.json` |
| iOS | APNs — native registration in `AppDelegate`, capability + provisioning profile with push enabled |
| Multi-platform at scale | **Azure Notification Hubs** — still fully supported in 2026; MUST use the FCM **v1** (HTTP v1) credential flow (legacy FCM HTTP API was deprecated July 2024) |

Backend sends via FCM/APNs directly for small apps; ANH earns its keep for device-installation management and templated cross-platform sends.

Runtime permission: Android 13+ requires `POST_NOTIFICATIONS` runtime permission; iOS prompts via `UNUserNotificationCenter` registration. Request in context (e.g. after the user enables a notify feature), not at first launch.

## Local notifications & background work

For scheduled/local notifications and reliable background jobs (Android `WorkManager`, iOS `BGTaskScheduler`), **Shiny.NET** is the most complete option — prefer it over hand-rolled platform services.
