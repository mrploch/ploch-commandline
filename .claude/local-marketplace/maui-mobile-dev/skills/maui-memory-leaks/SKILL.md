---
name: maui-memory-leaks
description: Use when a .NET MAUI app leaks memory, pages are not garbage collected, or memory grows during navigation - event handler leaks, DisconnectHandler, WeakEventManager, NSObject cycles, dotnet-gcdump detection, leak regression tests. Triggers on - memory leak, page not collected, memory grows, gcdump, disconnect handler, event unsubscribe.
---

# Memory Leaks in .NET MAUI

Leaks are MAUI's most systemic quality issue (official wiki: dotnet/maui "Memory-Leaks"). Symptom: pages/ViewModels stay alive after navigation; memory grows monotonically; gcdump shows N live instances of a page that should have 0–1.

## The four root causes

1. **Event subscriptions from long-lived sources to short-lived subscribers.** If `App`, a singleton service, a static event, or anything in `Application.Resources` holds a handler pointing at a Page/ViewModel, that page can never be collected.
2. **Delegate/`Func<T>` properties** — assigning a parent's method to a child control's callback keeps the parent alive via the delegate target.
3. **iOS/Mac Catalyst NSObject cycles** — a C# `NSObject` subclass subscribing to an event of another native-backed object creates a cycle neither ref-counting nor GC can break. Fix: `static` handlers, or a proxy class that does NOT inherit `NSObject`.
4. **`DisconnectHandler` is never called automatically** — by design. Custom handlers' native cleanup only runs if you invoke it.

## Prevention patterns

```csharp
// 1. Unsubscribe symmetrically
protected override void OnAppearing()  { _service.Updated += OnUpdated; }
protected override void OnDisappearing() { _service.Updated -= OnUpdated; }

// 2. Disconnect handlers when a page leaves the nav stack
protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
{
    Handler?.DisconnectHandler();
    base.OnNavigatedFrom(args);
}

// 3. Custom handler cleanup — mirror Connect/Disconnect
protected override void ConnectHandler(EditText native)
{
    native.AddTextChangedListener(_watcher);
    base.ConnectHandler(native);
}
protected override void DisconnectHandler(EditText native)
{
    native.RemoveTextChangedListener(_watcher);
    base.DisconnectHandler(native);
}
```

- Use `WeakReferenceMessenger` (weak by design) instead of events for VM↔VM communication.
- Use `WeakEventManager` for events you expose on long-lived objects; a `WeakEventProxy` for external `INotifyPropertyChanged` sources.
- Never subscribe a Page/VM to `App`/static events directly.
- Explicitly detach Behaviors on `CollectionView` item templates — not auto-detached on page pop (known leak class, e.g. issue #32403 native-handler leak).

## Detection

```bash
dotnet tool install -g dotnet-gcdump
dotnet-gcdump collect -p <pid>          # Android: direct
dotnet-dsrouter ios                      # iOS: router first, then collect
```

- **Disable XAML Hot Reload while collecting** — it pins extra references and fakes leaks.
- Cheap tracer: add a finalizer `~MyPage() => Console.WriteLine("MyPage finalized");` and watch logcat.

## Leak regression test (put in the unit suite)

```csharp
[Fact]
public async Task MyPage_should_be_garbage_collected()
{
    WeakReference reference;
    {
        var page = new MyPage();
        reference = new WeakReference(page);
    }
    await Task.Yield();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    reference.IsAlive.Should().BeFalse();
}
```

Run one of these per page type that has custom events/handlers.
