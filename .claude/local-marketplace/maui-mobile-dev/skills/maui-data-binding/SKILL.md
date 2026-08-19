---
name: maui-data-binding
description: Use when writing XAML bindings, styles, or theming in .NET MAUI - compiled bindings with x:DataType, surfacing silent binding failures, DataTemplate binding, AppThemeBinding dark mode, styles/resource dictionaries, C# markup alternative. Triggers on - binding, x:DataType, compiled binding, binding not working, dark mode, AppThemeBinding, style, resource dictionary.
---

# Data Binding, Styling & Theming in .NET MAUI

## Compiled bindings — always

Compiled bindings resolve at build time — **8–20× faster** than reflection `{Binding}` and required for full trimming/NativeAOT. Set `x:DataType` wherever a `BindingContext` is set:

```xaml
<ContentPage xmlns:vm="clr-namespace:MyApp.ViewModels"
             x:DataType="vm:ItemsViewModel">
    <CollectionView ItemsSource="{Binding Items}">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="models:Item">   <!-- templates do NOT inherit outer x:DataType -->
                <Label Text="{Binding Name}" />
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>
```

Rules:

- **Every `DataTemplate` needs its own `x:DataType`** (the item type, not the page ViewModel).
- `x:DataType="x:Object"` silently disables compilation with no warning — never use it.
- To bind to something other than the declared type (e.g. page VM from inside a template): `{Binding Source={RelativeSource AncestorType={x:Type vm:ItemsViewModel}}, Path=DeleteCommand}` — the `RelativeSource` re-scopes the compile-time type.
- Enforce project-wide — surfaces the otherwise-suppressed XC0022 "binding could be compiled" warning:

```xml
<MauiStrictXamlCompilation>true</MauiStrictXamlCompilation>
```

## Binding failures are silent — surface them

Bad paths/converters don't throw; they emit Debug-Output `BindingDiagnostics` lines at most. The VS "XAML Binding Failures" window has a known bug (doesn't scan the initial page). Defence:

1. Compiled bindings turn typos into **build errors** — first line of defence.
2. In DEBUG (or Release + crash reporter), subscribe and log:

```csharp
#if DEBUG
Microsoft.Maui.Controls.BindingDiagnostics.BindingFailed += (_, e)
    => Debug.WriteLine($"[BINDING FAILED] {e.Message}");
#endif
```

Forward these to Sentry breadcrumbs in production builds so silent UI breakage is visible.

## Theming — light/dark

```xaml
<Label TextColor="{AppThemeBinding Light={StaticResource Gray900}, Dark={StaticResource White}}" />
```

- Centralize palette in `Resources/Styles/Colors.xaml` + `Styles.xaml` (template default) and use `AppThemeBinding` inside styles, not scattered per-element.
- Runtime switch: `Application.Current.UserAppTheme = AppTheme.Dark;` react via `Application.Current.RequestedThemeChanged`.
- Test both themes on Android and iOS — theme bugs are a top visual-regression source.

## Styles

Standard XAML styles/resource dictionaries work as in Xamarin.Forms: implicit (`TargetType` only) vs explicit (`x:Key`), `BasedOn` inheritance, merged dictionaries. Prefer implicit styles for app-wide look; explicit styles for variants.

## C# Markup alternative (CommunityToolkit.Maui.Markup)

Fluent no-XAML UI — full C# tooling, Hot Reload support, no XAML parsing:

```csharp
Content = new VerticalStackLayout
{
    Children =
    {
        new Label().Text("Welcome").Font(size: 24, bold: true),
        new Entry().Bind(Entry.TextProperty, nameof(vm.Name)),
        new Button().Text("Submit").BindCommand(nameof(vm.SubmitCommand)),
    }
};
```

Enable via `.UseMauiCommunityToolkitMarkup()`. With .NET 10's XAML source generation the perf gap is gone — choice is team preference. Don't mix styles arbitrarily within one app; pick one primary approach.
