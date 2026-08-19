---
name: maui-navigation-shell
description: Use when implementing navigation in .NET MAUI - Shell routes, GoToAsync, passing parameters between pages, IQueryAttributable, modal pages, back-button handling, flyout/tabs. Triggers on - shell navigation, GoToAsync, route, navigate to page, pass data between pages, query property, modal, back button.
---

# .NET MAUI Shell Navigation

Shell is the default navigation container: URI-based routes, flyout/tab structure, DI-integrated page creation. `TabbedPage` is **incompatible** with Shell — never mix.

## Structure & routes

```xaml
<Shell x:Class="MyApp.AppShell" ...>
    <TabBar>
        <ShellContent Title="Items" Route="items" ContentTemplate="{DataTemplate views:ItemsPage}" />
        <ShellContent Title="Settings" Route="settings" ContentTemplate="{DataTemplate views:SettingsPage}" />
    </TabBar>
</Shell>
```

Detail pages not in the visual hierarchy → **global routes** in `AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute("itemdetails", typeof(ItemDetailsPage));
```

`ContentTemplate="{DataTemplate ...}"` gives lazy page creation; Shell resolves pages from the DI container (constructor injection works — see maui-dependency-injection).

## Navigating — ALWAYS await

```csharp
await Shell.Current.GoToAsync("itemdetails");        // relative (pushes onto stack)
await Shell.Current.GoToAsync("//items");            // absolute (resets stack)
await Shell.Current.GoToAsync("..");                 // back; chainable "../.."
```

Fire-and-forget `GoToAsync` causes race conditions (missing parameters, stale `CurrentPage`, silent failures). Note: long-session reports exist of Shell navigation degrading over time — keep navigation centralized in one service so instrumentation/retry is possible.

## Passing data

```csharp
await Shell.Current.GoToAsync("itemdetails", new Dictionary<string, object> { ["Item"] = item });
```

Receive with **`IQueryAttributable`** on the ViewModel:

```csharp
public partial class ItemDetailsViewModel : ObservableObject, IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Item", out var value) && value is Item item)
            Item = item;
    }
}
```

Critical choices:

- **`[QueryProperty]` is NOT trim/NativeAOT-safe** — use `IQueryAttributable` (forward-compatible, works with full trimming).
- Dictionary parameters are **retained for the page's lifetime** and re-applied on every navigation back to that page. For single-use data pass a `ShellNavigationQueryParameters` instance (auto-cleared after first delivery).
- Strings via `IQueryAttributable` are **not URL-decoded** automatically (unlike `[QueryProperty]`) — `HttpUtility.UrlDecode` if the value came through a URI.

## Modal navigation

```csharp
await Navigation.PushModalAsync(new SignInPage());
await Navigation.PopModalAsync();
```

Or mark a Shell page `Shell.PresentationMode="ModalAnimated"`. .NET 10: modals can present as popovers on iOS/Mac Catalyst (`ModalPresentationStyle = Popover` + `ModalPopoverSourceView`).

## Back button

Customize/intercept per page:

```xaml
<Shell.BackButtonBehavior>
    <BackButtonBehavior Command="{Binding BackCommand}" IconOverride="back.png" />
</Shell.BackButtonBehavior>
```

Cancelable navigation (unsaved changes) — Android hardware/gesture back flows through the same pipeline:

```csharp
protected override async void OnNavigating(ShellNavigatingEventArgs args)   // in AppShell
{
    base.OnNavigating(args);
    if (args.Source != ShellNavigationSource.Pop) return;
    var deferral = args.GetDeferral();
    var result = await DisplayActionSheetAsync("Discard changes?", "Cancel", "Discard");
    if (result != "Discard") args.Cancel();
    deferral.Complete();
}
```

## Recommended architecture

- One `INavigationService` abstraction injected into ViewModels (never `Shell.Current` in a ViewModel).
- Register every detail route once at startup; route names as constants.
- Avoid service-locator escapes like `Application.Current.Handler.MauiContext.Services.GetService<T>()`.
- Pages/ViewModels **transient** so each navigation gets fresh state (see maui-dependency-injection).
