---
name: maui-networking
description: Use when calling APIs from a .NET MAUI app - HttpClient with native handlers, IHttpClientFactory, Polly resilience for mobile networks, Refit typed clients, gRPC limits, connectivity checks, offline handling. Triggers on - httpclient, api call, refit, polly, retry, network error, connectivity, grpc.
---

# Networking in .NET MAUI

## HttpClient — native handlers by default

MAUI routes `HttpClient` through platform-native handlers: `AndroidMessageHandler` (Android) and `NSUrlSessionHandler` (iOS/Mac Catalyst) — native TLS, cert pinning, OS connection pooling. Beware libraries that force `SocketsHttpHandler`; that bypasses platform TLS behaviour and causes platform-inconsistent bugs.

`IHttpClientFactory` **works in MAUI** (same `Microsoft.Extensions` DI); "don't use factory in MAUI" advice is stale:

```csharp
builder.Services.AddHttpClient<IWeatherApi, WeatherApiClient>(c =>
        c.BaseAddress = new Uri("https://api.example.com/"))
    .AddStandardResilienceHandler();   // NuGet: Microsoft.Extensions.Http.Resilience (Polly v8)
```

`AddStandardResilienceHandler()` = retry + total/attempt timeouts + circuit breaker + rate limiter. Tune for cellular:

```csharp
.AddResilienceHandler("mobile", rb => rb
    .AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
    })
    .AddTimeout(TimeSpan.FromSeconds(30)));   // cellular needs longer than LAN defaults
```

## Refit — typed REST clients

Works unmodified in MAUI:

```csharp
public interface IItemsApi
{
    [Get("/items")]              Task<List<ItemDto>> GetItemsAsync(CancellationToken ct = default);
    [Post("/items")]             Task<ItemDto> CreateAsync([Body] CreateItemRequest request);
    [Get("/items/{id}")]         Task<ItemDto> GetItemAsync(string id);
}

builder.Services.AddRefitClient<IItemsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddStandardResilienceHandler();
```

For full trimming/AOT, use Refit's source-generated mode and `System.Text.Json` source-gen contexts for DTOs.

## Connectivity awareness

Inject `IConnectivity` (registered from `Connectivity.Current`, mockable in tests):

```csharp
if (_connectivity.NetworkAccess != NetworkAccess.Internet)
{
    await _toast.ShowAsync("Offline - showing cached data");
    return await _localStore.GetItemsAsync();
}
_connectivity.ConnectivityChanged += OnConnectivityChanged;   // trigger sync on reconnect (unsubscribe on teardown!)
```

Check connectivity to *degrade gracefully*, not to gate requests — captive portals lie; the request itself is the real test.

## gRPC

- **Client works** (`Grpc.Net.Client` over HTTP/2) on Android 10+ and iOS. Known failure on Android ≤9 (HTTP/2 negotiation).
- **Cannot host a gRPC server** inside a MAUI app (needs Kestrel/ASP.NET Core).
- gRPC-Web is the fallback where HTTP/2 is problematic.

## Error-handling pattern

Mobile networks fail routinely — treat failure as a normal state:

```csharp
try
{
    return Result.Success(await _api.GetItemsAsync(ct));
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    return await RefreshTokenAndRetryAsync(ct);          // see maui-auth-notifications
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    _logger.LogWarning(ex, "[ItemsService] network failure, serving cache");
    return Result.Success(await _localStore.GetItemsAsync());   // offline fallback
}
```

Rules: always pass `CancellationToken`s (page navigation should cancel in-flight calls); never block UI on network (`async` all the way); surface user feedback (Toast/Snackbar from CommunityToolkit.Maui) on failures triggered by user actions.
