---
name: winui3-mrploch-app
description: Opinionated guide for building WinUI 3 apps in the MrPloch workspace — integrates GenericRepository, IUnitOfWork, ServicesBundle, CommunityToolkit.Mvvm, and existing entity model interfaces.
invocable: false
---

# Building WinUI 3 Apps in the MrPloch Workspace

## When to Use This Skill

Use when:
- Creating a new WinUI 3 application in the MrPloch workspace
- Wiring up GenericRepository / IUnitOfWork with WinUI 3 ViewModels
- Setting up DI using the ServicesBundle pattern for a desktop app
- Building master-detail CRUD pages against `Ploch.Data.Model` entities

## Reference Skills

This skill is opinionated and specific to the MrPloch workspace. For general WinUI 3 patterns, see:
- `winui3-project-setup` — TFMs, SDK versions, packaging
- `winui3-mvvm-toolkit` — ObservableProperty, RelayCommand, validation
- `winui3-dependency-injection` — DI setup patterns
- `winui3-efcore-desktop` — DbContext lifetime, IDbContextFactory
- `winui3-navigation` — NavigationView shell, page routing
- `winui3-data-binding` — x:Bind, converters, gotchas
- `winui3-controls-layouts` — master-detail, ContentDialog, DataGrid

## Project Structure

A MrPloch WinUI 3 app always has two new projects:

```
src/{Product}/
  ViewModels/
    Ploch.{Product}.ViewModels.csproj    # net10.0 (no -windows suffix)
    {Entity}ViewModel.cs                  # One per CRUD entity
    MainViewModel.cs                      # Shell navigation state
  Apps/
    {AppName}/
      Ploch.{Product}.{AppName}.csproj   # net10.0-windows10.0.26100
      App.xaml / App.xaml.cs              # DI bootstrap + global error handling
      MainWindow.xaml / MainWindow.xaml.cs # NavigationView shell
      Pages/
        {Entity}Page.xaml / .xaml.cs      # One per entity
      appsettings.json                    # Connection strings
```

**Key separation:** ViewModels target plain `net10.0` — no Windows dependency. This enables unit testing with standard xUnit without WinUI 3 infrastructure.

## .csproj Templates

### ViewModels Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="CommunityToolkit.Mvvm" />
    </ItemGroup>
    <ItemGroup>
        <!-- Reference the Model project for entity types -->
        <ProjectReference Include="..\Model\Ploch.{Product}.Model.csproj" />
        <!-- Reference ploch-data for repository interfaces -->
        <ProjectReference Include="..\..\..\..\ploch-data\src\Data.GenericRepository\Ploch.Data.GenericRepository.csproj" />
    </ItemGroup>
</Project>
```

### WinUI 3 App Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
        <UseWinUI>true</UseWinUI>
        <WindowsPackageType>MSIX</WindowsPackageType>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.WindowsAppSDK" />
        <PackageReference Include="Microsoft.Extensions.Hosting" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\ViewModels\Ploch.{Product}.ViewModels.csproj" />
        <ProjectReference Include="..\Data\Ploch.{Product}.Data.csproj" />
        <ProjectReference Include="..\Data.SQLite\Ploch.{Product}.Data.SQLite.csproj" />
    </ItemGroup>
    <ItemGroup>
        <Content Include="appsettings.json">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </Content>
    </ItemGroup>
</Project>
```

## App.xaml.cs — DI Bootstrap

The app host wires together all layers using `Microsoft.Extensions.Hosting`:

```csharp
public partial class App : Application
{
    private readonly IHost _host;

    public static IServiceProvider Services => ((App)Current)._host.Services;

    public App()
    {
        this.InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                // Data layer — uses AddRepositories<TDbContext>() from ploch-data
                var connectionString = context.Configuration
                    .GetConnectionString("DefaultConnection");
                services.AddDataServices(
                    options => options.UseSqlite(connectionString),
                    context.Configuration);

                // ViewModels
                services.AddTransient<TagsViewModel>();
                services.AddTransient<CategoriesViewModel>();
                services.AddTransient<MainViewModel>();

                // Pages (transient — created fresh on each navigation)
                services.AddTransient<TagsPage>();
                services.AddTransient<CategoriesPage>();
                services.AddTransient<MainWindow>();

                // Services
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
            })
            .Build();

        // Global exception handlers (see winui3-performance-pitfalls skill)
        this.UnhandledException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[App] Unhandled: {e.Exception}");
            e.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[App] Unobserved: {e.Exception}");
            e.SetObserved();
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }
}
```

### Using ServicesBundle (Alternative)

If the Data project provides a `ServicesBundle`, use `AddServicesBundle` instead:

```csharp
services.AddServicesBundle(new MyDataBundle(), context.Configuration);
```

## ViewModel Pattern with GenericRepository

ViewModels inherit from `ObservableObject` (class, not attribute) and use `IReadRepositoryAsync` for reads and `IUnitOfWork` for writes:

```csharp
public partial class TagsViewModel : ObservableObject
{
    private readonly IReadRepositoryAsync<Tag, int> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TagsViewModel(
        IReadRepositoryAsync<Tag, int> tagRepository,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public ObservableCollection<Tag> Tags { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Tag? _selectedTag;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var tags = await _tagRepository.GetAllAsync();
            Tags.Clear();
            foreach (var tag in tags)
                Tags.Add(tag);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            var repo = _unitOfWork.Repository<Tag, int>();
            if (SelectedTag!.Id == 0)
                await repo.AddAsync(SelectedTag);
            else
                await repo.UpdateAsync(SelectedTag);
            await _unitOfWork.CommitAsync();
            await LoadAsync();  // refresh list
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanSave() =>
        SelectedTag is not null && !string.IsNullOrWhiteSpace(SelectedTag.Name);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        try
        {
            var repo = _unitOfWork.Repository<Tag, int>();
            await repo.DeleteAsync(SelectedTag!);
            await _unitOfWork.CommitAsync();
            SelectedTag = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanDelete() => SelectedTag is not null && SelectedTag.Id != 0;

    [RelayCommand]
    private void CreateNew()
    {
        SelectedTag = new Tag { Name = string.Empty };
    }
}
```

### Critical Rules

1. **Inherit from `ObservableObject` class** — never use `[ObservableObject]` attribute (WinRT/AOT incompatible, causes MVVMTK0049/0050).
2. **Entities must implement `IHasId<TId>`** — required by all `ploch-data` repository interfaces.
3. **Use `IReadRepositoryAsync<T, TId>` for read-only consumers** — prefer the narrowest interface.
4. **Use `IUnitOfWork` when writes span multiple entity types** or when explicit commit/rollback is needed.

## Page Pattern — Resolving ViewModels

WinUI 3 pages cannot use constructor injection (XAML creates pages via parameterless constructor during navigation). Use the service locator in `OnNavigatedTo`:

```csharp
public sealed partial class TagsPage : Page
{
    public TagsViewModel ViewModel { get; private set; } = null!;

    public TagsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = App.Services.GetRequiredService<TagsViewModel>();
        await ViewModel.LoadCommand.ExecuteAsync(null);
    }
}
```

### Master-Detail XAML

```xml
<Page x:Class="MyApp.Pages.TagsPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Master list -->
        <ListView Grid.Column="0"
                  ItemsSource="{x:Bind ViewModel.Tags, Mode=OneWay}"
                  SelectedItem="{x:Bind ViewModel.SelectedTag, Mode=TwoWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="models:Tag">
                    <TextBlock Text="{x:Bind Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <!-- Detail panel -->
        <StackPanel Grid.Column="1" Padding="24"
                    Visibility="{x:Bind ViewModel.SelectedTag,
                                 Mode=OneWay,
                                 Converter={StaticResource NullToVisibilityConverter}}">
            <TextBox Header="Name"
                     Text="{x:Bind ViewModel.SelectedTag.Name, Mode=TwoWay,
                            UpdateSourceTrigger=PropertyChanged}" />
            <TextBox Header="Description"
                     Text="{x:Bind ViewModel.SelectedTag.Description, Mode=TwoWay}" />
            <StackPanel Orientation="Horizontal" Spacing="8" Margin="0,16,0,0">
                <Button Content="Save"
                        Command="{x:Bind ViewModel.SaveCommand}"
                        Style="{ThemeResource AccentButtonStyle}" />
                <Button Content="Delete"
                        Command="{x:Bind ViewModel.DeleteCommand}" />
                <Button Content="New"
                        Command="{x:Bind ViewModel.CreateNewCommand}" />
            </StackPanel>
        </StackPanel>

        <!-- Error display -->
        <InfoBar Grid.ColumnSpan="2"
                 IsOpen="{x:Bind ViewModel.ErrorMessage, Mode=OneWay,
                          Converter={StaticResource StringToBoolConverter}}"
                 Severity="Error"
                 Title="Error"
                 Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}"
                 VerticalAlignment="Bottom" />
    </Grid>
</Page>
```

## Hierarchical Entities (Categories)

For entities implementing `IHierarchicalParentChildrenComposite<T>` (like `Category<TCategory>`), bind the `Parent` property to a ComboBox:

```csharp
// In CategoriesViewModel
[ObservableProperty]
private Category? _selectedParent;

// When loading, exclude the current item from parent candidates
public IEnumerable<Category> AvailableParents =>
    Categories.Where(c => c.Id != SelectedCategory?.Id);
```

```xml
<ComboBox Header="Parent Category"
          ItemsSource="{x:Bind ViewModel.AvailableParents, Mode=OneWay}"
          SelectedItem="{x:Bind ViewModel.SelectedParent, Mode=TwoWay}"
          DisplayMemberPath="Name" />
```

## Database Provider Switching

The Data layer follows the MrPloch provider project pattern (`Data.SQLite` / `Data.SqlServer`). To switch providers, change the `configureOptions` lambda in `App.xaml.cs`:

```csharp
// SQLite (default for desktop)
services.AddDataServices(
    options => options.UseSqlite(connectionString), configuration);

// SQL Server (for shared/enterprise scenarios)
services.AddDataServices(
    options => options.UseSqlServer(connectionString), configuration);
```

Connection strings live in `appsettings.json` and follow the same pattern as other MrPloch data projects.

## Testing Strategy

Because ViewModels target plain `net10.0`, test them with standard xUnit + Moq:

```csharp
public class TagsViewModelTests
{
    private readonly Mock<IReadRepositoryAsync<Tag, int>> _mockReadRepo = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();

    [Fact]
    public async Task LoadAsync_PopulatesTags()
    {
        _mockReadRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<Tag> { new() { Name = "Test" } });

        var vm = new TagsViewModel(_mockReadRepo.Object, _mockUow.Object);
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Tags);
    }

    [Fact]
    public async Task SaveAsync_NewTag_CallsAddAndCommit()
    {
        var mockRepo = new Mock<IReadWriteRepositoryAsync<Tag, int>>();
        _mockUow.Setup(u => u.Repository<Tag, int>()).Returns(mockRepo.Object);

        var vm = new TagsViewModel(_mockReadRepo.Object, _mockUow.Object);
        vm.SelectedTag = new Tag { Id = 0, Name = "New Tag" };

        await vm.SaveCommand.ExecuteAsync(null);

        mockRepo.Verify(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Checklist: New MrPloch WinUI 3 App

1. Create `Ploch.{Product}.ViewModels` project targeting `net10.0`
2. Create `Ploch.{Product}.{AppName}` WinUI 3 project targeting `net10.0-windows10.0.26100`
3. Add both projects to the solution file
4. Wire DI in `App.xaml.cs` using `AddDataServices()` or `AddServicesBundle()`
5. Create `MainWindow` with `NavigationView` shell
6. Create one page per entity following master-detail pattern
7. Create one ViewModel per entity using `IReadRepositoryAsync` + `IUnitOfWork`
8. Add global exception handlers in `App.xaml.cs` (3 handlers)
9. Set up `appsettings.json` with connection string
10. Add unit tests for ViewModels in a separate `net10.0` test project
