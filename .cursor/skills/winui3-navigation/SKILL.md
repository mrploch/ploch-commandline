---
name: winui3-navigation
description: Implement NavigationView shell patterns, Frame navigation, page parameter passing, back navigation, and NavigationService abstractions in WinUI 3 desktop apps.
invocable: false
---

# WinUI 3 Navigation Patterns

## When to Use This Skill

Use when:
- Building a NavigationView shell with Frame content navigation
- Implementing page-to-page navigation with parameters
- Creating a NavigationService abstraction
- Handling back navigation and navigation history

## Shell Page Pattern (NavigationView + Frame)

### XAML

```xml
<Page x:Class="MyApp.Views.ShellPage">
    <NavigationView x:Name="NavView"
                    IsBackButtonVisible="Visible"
                    IsBackEnabled="{x:Bind ContentFrame.CanGoBack, Mode=OneWay}"
                    BackRequested="NavView_BackRequested"
                    ItemInvoked="NavView_ItemInvoked">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Home" Tag="MyApp.Views.HomePage" Icon="Home" />
            <NavigationViewItem Content="Items" Tag="MyApp.Views.ItemsPage" Icon="List" />
            <NavigationViewItem Content="Settings" Tag="MyApp.Views.SettingsPage" Icon="Setting" />
        </NavigationView.MenuItems>
        <NavigationView.FooterMenuItems>
            <NavigationViewItem Content="Help" Tag="MyApp.Views.HelpPage" Icon="Help" />
        </NavigationView.FooterMenuItems>
        <Frame x:Name="ContentFrame" />
    </NavigationView>
</Page>
```

### Code-Behind

```csharp
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(HomePage));
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == typeof(HomePage).FullName);
    }

    private void NavView_ItemInvoked(NavigationView sender,
        NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateTo(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer?.Tag is string pageTypeName)
        {
            var pageType = Type.GetType(pageTypeName);
            if (pageType != null) NavigateTo(pageType);
        }
    }

    private void NavView_BackRequested(NavigationView sender,
        NavigationViewBackRequestedEventArgs args) => TryGoBack();

    private void NavigateTo(Type pageType, object? parameter = null)
    {
        if (ContentFrame.CurrentSourcePageType == pageType) return; // avoid duplicate
        ContentFrame.Navigate(pageType, parameter);
    }

    private bool TryGoBack()
    {
        if (!ContentFrame.CanGoBack) return false;
        ContentFrame.GoBack();
        return true;
    }
}
```

## Passing Parameters

```csharp
// Navigate with parameter
ContentFrame.Navigate(typeof(DetailPage), selectedItem);

// Receive in target page
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    if (e.Parameter is MyEntity entity)
    {
        _viewModel = App.Current.Services.GetRequiredService<DetailViewModel>();
        _viewModel.Load(entity.Id);
        this.DataContext = _viewModel;
    }
}
```

## Page Caching

By default, each navigation creates a new page instance. To cache pages:

```csharp
// In the page constructor or XAML
this.NavigationCacheMode = NavigationCacheMode.Enabled;  // cached up to Frame.CacheSize
this.NavigationCacheMode = NavigationCacheMode.Required; // always cached, ignores CacheSize
```

Use sparingly — cached pages hold memory and may show stale data.

## NavigationService Abstraction (Template Studio Pattern)

```csharp
public interface INavigationService
{
    bool CanGoBack { get; }
    Frame? Frame { get; set; }
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);
    bool GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    public Frame? Frame { get; set; }
    public bool CanGoBack => Frame?.CanGoBack ?? false;

    public NavigationService(IPageService pageService) => _pageService = pageService;

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        var pageType = _pageService.GetPageType(pageKey);
        if (Frame?.CurrentSourcePageType == pageType) return false;

        var navigated = Frame!.Navigate(pageType, parameter);
        if (navigated && clearNavigation)
            Frame.BackStack.Clear();
        return navigated;
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        Frame!.GoBack();
        return true;
    }
}
```

## INavigationAware for ViewModel Lifecycle

```csharp
public interface INavigationAware
{
    void OnNavigatedTo(object parameter);
    void OnNavigatedFrom();
}
```

Wire this in the NavigationService's `Frame.Navigated` event to call lifecycle methods on ViewModels.

## Adaptive Display Modes

NavigationView automatically switches between expanded, compact, and minimal modes:

```xml
<!-- Default breakpoints -->
<NavigationView CompactModeThresholdWidth="640"
                ExpandedModeThresholdWidth="1008" />

<!-- Always compact on medium screens -->
<NavigationView CompactModeThresholdWidth="1007"
                ExpandedModeThresholdWidth="1007" />
```

## TitleBar Integration (Recommended)

Place a `TitleBar` control above NavigationView and let it own the back/pane toggle buttons:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <TitleBar x:Name="AppTitleBar" Grid.Row="0" />
    <NavigationView Grid.Row="1"
                    IsBackButtonVisible="Collapsed"
                    IsPaneToggleButtonVisible="False">
        <!-- ... -->
    </NavigationView>
</Grid>
```
