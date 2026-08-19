---
name: winui3-data-binding
description: WinUI 3 data binding patterns including x:Bind vs Binding, compiled bindings, two-way binding, converters, collection binding with ObservableCollection, and common binding gotchas.
invocable: false
---

# WinUI 3 Data Binding

## When to Use This Skill

Use when:
- Binding UI controls to ViewModel properties
- Choosing between x:Bind and Binding
- Working with collection bindings (ListView, ItemsRepeater)
- Creating value converters
- Debugging binding failures

## x:Bind vs {Binding}

| Feature | `{x:Bind}` | `{Binding}` |
|---------|-----------|------------|
| **Evaluation** | Compile-time | Runtime reflection |
| **Performance** | ~5x faster | Baseline |
| **Default mode** | **OneTime** | OneWay |
| **Type safety** | Compile errors | Silent runtime failures |
| **DataContext** | Code-behind class | Inherited DataContext |
| **Method calls** | Supported | Not supported |

**Critical rule:** `{x:Bind}` defaults to `OneTime`. Always specify `Mode=OneWay` or `Mode=TwoWay` for mutable properties.

```xml
<!-- WRONG: binds once and never updates -->
<TextBlock Text="{x:Bind ViewModel.Title}" />

<!-- CORRECT: updates when property changes -->
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />

<!-- CORRECT: two-way for editable fields -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

## Property Binding Patterns

### Simple Properties

```xml
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
<CheckBox IsChecked="{x:Bind ViewModel.IsActive, Mode=TwoWay}" />
<ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />
```

### Command Binding

```xml
<Button Content="Save" Command="{x:Bind ViewModel.SaveCommand}" />
<Button Content="Delete" Command="{x:Bind ViewModel.DeleteCommand}"
        CommandParameter="{x:Bind ViewModel.SelectedItem, Mode=OneWay}" />
```

### Async Command Loading State

```xml
<Button Content="Load" Command="{x:Bind ViewModel.LoadCommand}" />
<ProgressRing IsActive="{x:Bind ViewModel.LoadCommand.IsRunning, Mode=OneWay}" />
```

### Visibility Binding

```xml
<!-- With converter -->
<StackPanel Visibility="{x:Bind ViewModel.HasItems, Mode=OneWay,
                         Converter={StaticResource BoolToVisibilityConverter}}" />

<!-- With method call (x:Bind exclusive feature) -->
<TextBlock Visibility="{x:Bind ViewModel.GetVisibility(), Mode=OneWay}" />
```

## Collection Binding

### ListView

```xml
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
          SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="models:Item">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{x:Bind Name}" />
                <TextBlock Text="{x:Bind Description}"
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Important:** Inside `DataTemplate`, `x:Bind` binds to the data item type specified by `x:DataType`. The `x:DataType` attribute is **required** for `x:Bind` in templates.

### ItemsRepeater (Low-Level, High Performance)

```xml
<ScrollViewer>
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
        <ItemsRepeater.Layout>
            <StackLayout Orientation="Vertical" Spacing="4" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="models:Item">
                <TextBlock Text="{x:Bind Name}" />
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

No built-in selection — must implement selection logic manually.

## UpdateSourceTrigger

Controls when two-way bindings push changes back to the source:

```xml
<!-- Updates on each keystroke (immediate feedback) -->
<TextBox Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay,
                UpdateSourceTrigger=PropertyChanged}" />

<!-- Updates when focus leaves the TextBox (default for TextBox) -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
```

Use `PropertyChanged` when you need live validation or search-as-you-type.

## Value Converters

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}
```

Register in App.xaml or page resources:

```xml
<Page.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter" />
</Page.Resources>
```

## Common Binding Gotchas

1. **`x:Bind` OneTime default** — most common source of "UI doesn't update" bugs
2. **`x:DataType` required in DataTemplate** — `x:Bind` inside templates needs explicit type
3. **ObservableCollection must be modified on UI thread** — use `DispatcherQueue.TryEnqueue()` from background threads
4. **Replacing the entire collection** — replacing `Items = new ObservableCollection<T>()` requires `Mode=OneWay` on `ItemsSource` binding. Prefer `Items.Clear()` + re-add to avoid rebinding
5. **Null propagation** — `{x:Bind ViewModel.Item.Name}` throws if `Item` is null at bind time. Use `FallbackValue` or ensure non-null
