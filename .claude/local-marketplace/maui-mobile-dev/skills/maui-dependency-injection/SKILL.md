---
name: maui-dependency-injection
description: Use when registering or resolving services in a .NET MAUI app - MauiProgram service registration, lifetimes for mobile (singleton/transient, why scoped is a trap), page+viewmodel registration, EF Core DbContextFactory, Essentials interface registration. Triggers on - AddSingleton, AddTransient, register service, DI, service lifetime, inject into page.
---

# Dependency Injection in .NET MAUI

MAUI uses the standard `Microsoft.Extensions.DependencyInjection` container built in `MauiProgram.CreateMauiApp()`. Same API as ASP.NET Core — **different lifetime semantics** because a mobile app is one long-lived process with no requests.

## Lifetimes for mobile

| Lifetime | Behaviour in MAUI | Use for |
|---|---|---|
| `AddSingleton` | One instance for process lifetime | services, settings, connectivity, navigation service, HttpClient factories, DB connection/factory |
| `AddTransient` | New instance per resolution | **Pages and ViewModels** — fresh state per navigation |
| `AddScoped` | **Behaves like singleton** — there is no per-request scope | **Avoid.** A "scoped" DbContext silently becomes a shared singleton, unsafe across concurrent ViewModels |

Align page/ViewModel lifetimes: a singleton page + transient ViewModel means the page captures the *first* ViewModel forever → stale data. Transient page + transient ViewModel is the safe default; use singleton pages only for tab roots you deliberately want to keep alive.

## Standard registrations

```csharp
var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>();

// MAUI Essentials — register the interface so ViewModels stay mockable (see maui-testing)
builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
builder.Services.AddSingleton<IPreferences>(Preferences.Default);
builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

// App services
builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
builder.Services.AddSingleton<IItemRepository, SqliteItemRepository>();

// Pages + ViewModels — transient pairs
builder.Services.AddTransient<ItemsPage>();
builder.Services.AddTransient<ItemsViewModel>();
builder.Services.AddTransient<ItemDetailsPage>();
builder.Services.AddTransient<ItemDetailsViewModel>();
```

Shell + DI compose automatically: pages registered in the container and routes registered via `Routing.RegisterRoute` are resolved from DI during `GoToAsync`, with constructor injection (including the ViewModel).

Unlike WinUI 3, **constructor injection into pages works** — no service-locator needed.

## EF Core: factory, never scoped

```csharp
builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(_ =>
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={Path.Combine(FileSystem.AppDataDirectory, "app.db")}")
        .Options;
    return new PooledDbContextFactory<AppDbContext>(options);
});

// consumer:
await using var db = await _dbContextFactory.CreateDbContextAsync();
```

Short-lived contexts per operation — a long-lived context accumulates tracked entities (memory) and is not thread-safe. See maui-data-storage for SQLite options and iOS/AOT caveats.

## HttpClient

Use `IHttpClientFactory` (works fine in MAUI — "don't use it in MAUI" advice is stale):

```csharp
builder.Services.AddHttpClient<IApiClient, ApiClient>(c => c.BaseAddress = new Uri("https://api.example.com/"))
                .AddStandardResilienceHandler();   // Microsoft.Extensions.Http.Resilience
```

See maui-networking for handler/resilience details.

## Resolving outside constructors (last resort)

`IServiceProvider` is reachable via `IPlatformApplication.Current.Services` or a handler's `MauiContext.Services`. Treat as an escape hatch (platform callbacks, lifecycle events) — never as the normal pattern inside pages/ViewModels.

## ServicesBundle (MrPloch)

For apps in this workspace, group registrations with ploch-common's `ServicesBundle` exactly as in other app types:

```csharp
builder.Services.AddServicesBundle(new MyAppDataBundle(), configuration);
```
