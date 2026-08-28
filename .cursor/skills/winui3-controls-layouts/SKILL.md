---
name: winui3-controls-layouts
description: WinUI 3 controls and layout patterns — master-detail, DataGrid, ContentDialog, custom controls, UserControl, TemplatedControl, DependencyProperty, and key layout panels.
invocable: false
---

# WinUI 3 Controls and Layouts

## When to Use This Skill

Use when:

- Building master-detail page layouts
- Using DataGrid, ContentDialog, InfoBar, TreeView
- Creating custom UserControls or TemplatedControls
- Choosing between ListView, ItemsRepeater, and ItemsView
- Implementing dialog patterns from ViewModels

## Reference Files

- [custom-controls.md](custom-controls.md): UserControl and TemplatedControl patterns with DependencyProperty

## Layout Panels

| Panel               | Use Case                                                       |
| ------------------- | -------------------------------------------------------------- |
| `Grid`              | Primary 2D layout with rows/columns (star, auto, fixed sizing) |
| `StackPanel`        | Single-axis stacking. Use `Spacing` property for gaps          |
| `RelativePanel`     | Position elements relative to each other                       |
| `UniformGridLayout` | Equal-sized cells (use with ItemsRepeater)                     |

## Collection Control Selection

| Control         | Selection | Virtualization | Customisation | Use When                            |
| --------------- | --------- | -------------- | ------------- | ----------------------------------- |
| `ListView`      | Built-in  | Built-in       | Medium        | Standard lists with selection       |
| `GridView`      | Built-in  | Built-in       | Medium        | Grid layouts with selection         |
| `ItemsView`     | Built-in  | Built-in       | High          | Modern lists (SDK 1.2+)             |
| `ItemsRepeater` | Manual    | Via layout     | Full          | Custom layouts, no selection needed |

## Master-Detail Pattern

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="280" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Master list -->
    <ListView Grid.Column="0"
              ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
              SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="models:Item">
                <TextBlock Text="{x:Bind Name}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>

    <!-- Detail panel -->
    <StackPanel Grid.Column="1" Padding="24"
                Visibility="{x:Bind ViewModel.HasSelection, Mode=OneWay,
                             Converter={StaticResource BoolToVisibilityConverter}}">
        <TextBox Header="Name"
                 Text="{x:Bind ViewModel.SelectedItem.Name, Mode=TwoWay,
                        UpdateSourceTrigger=PropertyChanged}" />
        <TextBox Header="Description"
                 Text="{x:Bind ViewModel.SelectedItem.Description, Mode=TwoWay}" />
        <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,16,0,0">
            <Button Content="Save" Command="{x:Bind ViewModel.SaveCommand}" Style="{ThemeResource AccentButtonStyle}" />
            <Button Content="Delete" Command="{x:Bind ViewModel.DeleteCommand}" />
            <Button Content="New" Command="{x:Bind ViewModel.CreateNewCommand}" />
        </StackPanel>
    </StackPanel>
</Grid>
```

## ContentDialog Patterns

### Simple Confirmation from ViewModel

Use a dialog service to avoid coupling ViewModels to XAML types:

```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
}

internal class DialogService(IXamlRootProvider xamlRootProvider) : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Confirm",
            CloseButtonText = "Cancel",
            XamlRoot = xamlRootProvider.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
```

**Known limitation:** Only one ContentDialog can be open at a time.
**Known memory leak:** ContentDialog instances may not be GC'd after closing (GitHub #4005). Create fresh instances rather than reusing.

## InfoBar (Inline Notifications)

```xml
<InfoBar IsOpen="{x:Bind ViewModel.ShowSuccess, Mode=OneWay}"
         Severity="Success"
         Title="Saved"
         Message="Item saved successfully."
         IsClosable="True" />

<InfoBar IsOpen="{x:Bind ViewModel.ShowError, Mode=OneWay}"
         Severity="Error"
         Title="Error"
         Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />
```

## DataGrid (CommunityToolkit)

```xml
<!-- Package: CommunityToolkit.WinUI.Controls.DataGrid -->
<controls:DataGrid ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
                   AutoGenerateColumns="False"
                   IsReadOnly="True"
                   SelectionMode="Single"
                   SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}">
    <controls:DataGrid.Columns>
        <controls:DataGridTextColumn Header="Name" Binding="{Binding Name}" />
        <controls:DataGridTextColumn Header="Description" Binding="{Binding Description}" />
    </controls:DataGrid.Columns>
</controls:DataGrid>
```

Note: DataGrid uses `{Binding}` (not `x:Bind`) for column definitions.

## TreeView

```xml
<TreeView ItemsSource="{x:Bind ViewModel.Categories, Mode=OneWay}"
          SelectionMode="Single"
          ItemInvoked="TreeView_ItemInvoked">
    <TreeView.ItemTemplate>
        <DataTemplate x:DataType="models:Category">
            <TreeViewItem Content="{x:Bind Name}"
                          ItemsSource="{x:Bind Children}" />
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```
