---
name: winui3-dependency-injection
description: Configure dependency injection in WinUI 3 apps using Microsoft.Extensions.DependencyInjection or Microsoft.Extensions.Hosting. Covers App.xaml.cs bootstrapping, ViewModel injection into Pages, service registration lifetimes, and IHost integration.
invocable: false
---

# WinUI 3 Dependency Injection

## When to Use This Skill

Use when:
- Bootstrapping DI in a WinUI 3 App.xaml.cs
- Injecting ViewModels into Pages
- Choosing between ServiceCollection and full IHost
- Registering services with correct lifetimes for desktop apps

## Basic Pattern: ServiceCollection in App.xaml.cs

```csharp
public partial class App : Application
{
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // Data
        services.AddDbContextFactory<AppDbContext>(opts =>
            opts.UseSqlite("DataSource=app.db"));

        // ViewModels — transient ensures clean state per navigation
        services.AddTransient<MainViewModel>();
        services.AddTransient<DetailViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
```

## Injecting ViewModels into Pages

**WinUI 3 limitation:** Pages are instantiated by XAML via parameterless constructors. You cannot use constructor injection into Pages directly.

**Standard pattern — resolve in OnNavigatedTo:**

```csharp
public sealed partial class ItemsPage : Page
{
    private ItemsViewModel? _viewModel;

    public ItemsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _viewModel = App.Current.Services.GetRequiredService<ItemsViewModel>();

        if (e.Parameter is int itemId)
            _viewModel.LoadItem(itemId);

        this.DataContext = _viewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
```

**Alternative — resolve in constructor:**

```csharp
public ItemsPage()
{
    this.InitializeComponent();
    _viewModel = App.Current.Services.GetRequiredService<ItemsViewModel>();
}
```

This is a service locator call, not pure DI, but it is the accepted WinUI 3 pattern used in Microsoft's own TemplateStudio.

## Constructor Injection for MainWindow

The MainWindow CAN use constructor injection if resolved from the container:

```csharp
// In App.OnLaunched:
var window = Services.GetRequiredService<MainWindow>();
window.Activate();

// MainWindow.cs:
public sealed partial class MainWindow : Window
{
    private readonly INavigationService _nav;

    public MainWindow(INavigationService nav)
    {
        _nav = nav;
        this.InitializeComponent();
    }
}
```

## Full IHost Integration

For apps needing hosted services, structured logging, layered configuration:

```xml
<!-- In .csproj -->
<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>
```

```csharp
// Program.cs
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var builder = Host.CreateApplicationBuilder(args);

        // Register services on builder.Services
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<MainViewModel>();

        var host = builder.Build();
        var app = host.Services.GetRequiredService<App>();

        app.Services = host.Services;
        app.Run();
    }
}
```

**When to use IHost vs. bare ServiceCollection:**
- **ServiceCollection** — simpler, sufficient for DI + basic config
- **IHost** — needed for `IHostedService`, Serilog integration, `IConfiguration` with multiple providers, `IHostApplicationLifetime`

## Registration Lifetime Guide for Desktop Apps

| Type | Lifetime | Reason |
|------|----------|--------|
| Navigation services | Singleton | One instance shared across the app |
| Dialog services | Singleton | Needs XamlRoot reference |
| HTTP clients | Singleton (via IHttpClientFactory) | Connection pooling |
| ViewModels | Transient | Fresh state per page navigation |
| DbContext | **Do NOT register directly** | Use `IDbContextFactory` instead (see winui3-efcore-desktop skill) |
| Settings/preferences service | Singleton | Shared state |
| Logging (ILogger<T>) | From Host | Auto-registered by IHost |

## Exposing XamlRoot for Dialogs

ContentDialog requires `XamlRoot`. Expose it via an interface:

```csharp
public interface IXamlRootProvider
{
    XamlRoot? XamlRoot { get; }
}

// In MainWindow or ShellPage:
public class XamlRootProvider : IXamlRootProvider
{
    private readonly Window _window;
    public XamlRootProvider(Window window) => _window = window;
    public XamlRoot? XamlRoot => _window.Content?.XamlRoot;
}

// Register after window creation:
services.AddSingleton<IXamlRootProvider>(new XamlRootProvider(mainWindow));
```
