---
name: maui-mvvm-toolkit
description: Use when implementing MVVM in .NET MAUI with CommunityToolkit.Mvvm - ObservableObject, ObservableProperty partial properties, RelayCommand, WeakReferenceMessenger, validation. Triggers on - viewmodel, observable property, relay command, mvvm, messaging between viewmodels, INotifyPropertyChanged.
---

# MVVM in .NET MAUI with CommunityToolkit.Mvvm (8.4+)

## Core pattern — partial properties (recommended since Toolkit 8.4 / C# 13)

```csharp
public partial class ItemsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SearchText { get; set; }          // NEW partial-property style — prefer this

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial Item? SelectedItem { get; set; }

    [ObservableProperty]
    private string legacyField;                              // classic field style — still supported

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct) { … } // generates SaveCommand (AsyncRelayCommand)
    private bool CanSave() => SelectedItem is not null;
}
```

Why partial-property style: proper `required`/`override`/accessibility support, per-accessor attributes, better nullability, **Native AOT compatibility**. A VS code-fixer can bulk-migrate field-style usages.

## Rules and gotchas

- The class **must be `partial`** (and `ObservableObject`-derived) — forgetting `partial` is the #1 error.
- Never hand-write a property whose name collides with a generated one (field `name` → generates `Name`).
- Cascades are explicit: `[NotifyPropertyChangedFor(nameof(Computed))]`, `[NotifyCanExecuteChangedFor(nameof(SomeCommand))]` — easy to forget, causes "button never enables" bugs.
- `[RelayCommand]` on `Task`-returning methods generates `AsyncRelayCommand`; add a `CancellationToken` parameter for cancellation support. **Caution:** 2026 production reports associate `AsyncRelayCommand` with hard-to-reproduce crashes on some .NET 10 patch levels (`InvalidOperation_HandleIsNotInitialized`) — test on real devices and pin vetted patch levels.
- Keep ViewModels free of `Application.Current` / `Shell.Current` static access — inject abstractions instead (testability; see maui-testing).
- Validation: derive from `ObservableValidator`, use DataAnnotations + `ValidateProperty`/`ValidateAllProperties`.

## Messaging — WeakReferenceMessenger (MessagingCenter is dead)

`MessagingCenter` became **internal in .NET 10**. Use the Toolkit messenger — weak references mean no explicit unsubscription needed:

```csharp
public sealed class ItemSavedMessage(Item value) : ValueChangedMessage<Item>(value);

WeakReferenceMessenger.Default.Send(new ItemSavedMessage(item));
WeakReferenceMessenger.Default.Register<ItemSavedMessage>(this, (r, m) => ((ListViewModel)r).Refresh(m.Value));
```

Prefer messenger for cross-ViewModel notifications; prefer plain injected services for request/response logic.

## Wiring View ↔ ViewModel (View-first, the MAUI default)

```csharp
// MauiProgram.cs
builder.Services.AddTransient<ItemsPage>();
builder.Services.AddTransient<ItemsViewModel>();

// ItemsPage.xaml.cs — constructor injection; Shell resolves the page from DI
public partial class ItemsPage : ContentPage
{
    public ItemsPage(ItemsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
```

Set `x:DataType="vm:ItemsViewModel"` on the page for compiled bindings (see maui-data-binding).

ViewModel-first is also valid: a navigation service maps ViewModel types → pages (eShop's `MauiNavigationService` pattern). Either way, **abstract navigation behind an interface** so ViewModels never reference `Shell` directly:

```csharp
public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}
public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        => parameters is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, parameters);
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
```

## Threading in ViewModels

MAUI does NOT auto-marshal collection/property changes to the UI thread:

```csharp
var data = await _service.FetchAsync();          // background OK
await MainThread.InvokeOnMainThreadAsync(() =>   // UI-bound mutations on main thread
{
    Items.Clear();
    foreach (var d in data) Items.Add(d);
});
```

`MainThread.BeginInvokeOnMainThread` is safe to call when already on the main thread (direct invoke). In custom platform heads prefer `Dispatcher.Dispatch(...)` (known `NotImplementedInReferenceAssemblyException` edge cases with MainThread).

## Lifecycle hookup

Pages get `OnAppearing`/`OnDisappearing`/`OnNavigatedTo`; forward to the ViewModel via a small interface (`INavigationAware`-style) or use `IQueryAttributable` for parameter delivery (see maui-navigation-shell). Unsubscribe non-weak event handlers in `OnDisappearing` (see maui-memory-leaks).
