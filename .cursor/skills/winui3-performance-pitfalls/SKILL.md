---
name: winui3-performance-pitfalls
description: WinUI 3 performance optimisation (virtualization, deferred loading, async patterns) and common pitfalls (threading, DispatcherQueue, memory leaks, COMExceptions, unhandled exceptions).
invocable: false
---

# WinUI 3 Performance and Common Pitfalls

## When to Use This Skill

Use when:
- Debugging threading issues or COMExceptions
- Optimising list/collection performance
- Handling background-thread UI updates
- Setting up global exception handling
- Investigating memory leaks

## Threading: The #1 Source of WinUI 3 Bugs

### The Rule

UI objects are thread-affine. Accessing them from a background thread throws:
> `COMException: The application called an interface that was marshalled for a different thread`

### DispatcherQueue Pattern

```csharp
public partial class LiveDataViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcher;

    public LiveDataViewModel()
    {
        // MUST capture on the UI thread (during construction or OnNavigatedTo)
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    private void OnBackgroundEvent(object? sender, DataEventArgs e)
    {
        // Marshal to UI thread
        _dispatcher.TryEnqueue(() =>
        {
            StatusText = e.Message;
            Items.Add(e.Item);
        });
    }

    // Helper for conditional marshalling
    private void SafeUpdateUI(Action action)
    {
        if (_dispatcher.HasThreadAccess)
            action();
        else
            _dispatcher.TryEnqueue(() => action());
    }
}
```

**Critical:** `DispatcherQueue.GetForCurrentThread()` returns `null` off the UI thread. Always capture during construction.

### Awaitable Dispatch (CommunityToolkit)

```csharp
// From Windows Community Toolkit:
await _dispatcher.EnqueueAsync(() =>
{
    Items.Clear();
    foreach (var item in newItems)
        Items.Add(item);
});
```

## Global Exception Handling

WinUI 3 requires **multiple handlers** — no single handler catches all exceptions:

```csharp
public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();

        // 1. XAML dispatch exceptions (UI thread)
        this.UnhandledException += (s, e) =>
        {
            _logger.LogCritical(e.Exception, "[App] Unhandled XAML exception");
            e.Handled = true;  // prevents crash (omit for unrecoverable errors)
        };

        // 2. Unobserved Task exceptions (fire-and-forget async)
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _logger.LogError(e.Exception, "[App] Unobserved task exception");
            e.SetObserved();  // prevents process termination
        };

        // 3. Background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            _logger.LogCritical("[App] Domain exception: {Ex}", e.ExceptionObject);
            // Cannot prevent termination when e.IsTerminating is true
        };
    }
}
```

**Known limitation:** `Application.UnhandledException` does NOT catch exceptions inside `DispatcherQueue.TryEnqueue()` callbacks (GitHub #8940). Wrap callback bodies in try/catch.

## Memory Leak Patterns

### 1. Event Handlers in DataTemplates (GitHub #6894)

Items in DataTemplates hold strong references through delegate targets.

**Fix:** Use methods on the DataTemplate's own scope, not parent class methods. Or use `x:Bind` with `Mode=OneWay` instead of event handlers.

### 2. ContentDialog Not Released (GitHub #4005)

ContentDialog instances remain in memory after closing.

**Fix:** Create dialogs fresh each time. Don't cache dialog instances.

### 3. x:Bind Event Handlers Preventing GC (GitHub #9960)

`x:Bind` event handlers (e.g., `Click="{x:Bind ViewModel.DoSomething}"`) create strong references.

**Fix:** Subscribe in `Loaded`/`OnNavigatedTo`, unsubscribe in `Unloaded`/`OnNavigatedFrom`.

### General Rule

```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    WeakReferenceMessenger.Default.Register<MyMessage>(this, HandleMessage);
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    base.OnNavigatedFrom(e);
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

## Performance: Virtualization

### ListView/GridView

Built-in virtualization — only visible items are in the visual tree. Enable incremental loading:

```csharp
public class IncrementalItemSource : ObservableCollection<Item>, ISupportIncrementalLoading
{
    public bool HasMoreItems => _hasMore;

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return AsyncInfo.Run(async ct =>
        {
            var items = await _service.GetPageAsync(_page++, (int)count, ct);
            foreach (var item in items) Add(item);
            _hasMore = items.Count == count;
            return new LoadMoreItemsResult { Count = (uint)items.Count };
        });
    }
}
```

### ItemsRepeater

No `ISupportIncrementalLoading`. Observe `ScrollViewer.ViewChanged` to detect scroll-near-end:

```csharp
scrollViewer.ViewChanged += (s, e) =>
{
    if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 100)
        _viewModel.LoadMoreCommand.Execute(null);
};
```

## Deferred Loading

### x:Load (Preferred)

Conditionally loads/unloads entire subtrees — more efficient than `Visibility=Collapsed`:

```xml
<StackPanel x:Load="{x:Bind ViewModel.ShowAdvancedOptions, Mode=OneWay}">
    <!-- Complex UI not created until condition is true -->
</StackPanel>
```

### x:DeferLoadStrategy="Lazy"

Defers element creation until `FindName()` is called:

```xml
<Grid x:Name="HeavyGrid" x:DeferLoadStrategy="Lazy">
    <!-- Created only when FindName("HeavyGrid") is called -->
</Grid>
```

## Common Gotchas Summary

| Gotcha | Symptom | Fix |
|--------|---------|-----|
| `x:Bind` default `OneTime` | UI doesn't update | Add `Mode=OneWay` |
| Missing `partial` keyword | Compilation error | Add `partial` to class and all parent types |
| Background thread UI access | COMException | Use `DispatcherQueue.TryEnqueue()` |
| ObservableCollection off-thread | COMException | Marshal Add/Remove to UI thread |
| `[ObservableObject]` attribute | MVVMTK0050 warning | Inherit from `ObservableObject` class |
| ContentDialog without XamlRoot | InvalidOperationException | Set `dialog.XamlRoot` |
| Release build crashes | DispatcherQueueSynchronizationContext | Test Release builds early |
| `HasErrors` before validation | Always false | Call `ValidateAllProperties()` first |
