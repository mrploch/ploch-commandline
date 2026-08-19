---
name: winui3-efcore-desktop
description: EF Core in WinUI 3 desktop apps — IDbContextFactory pattern, lifetime management, thread-safe scoping, migrations in separate projects, connection string configuration.
invocable: false
---

# EF Core in WinUI 3 Desktop Applications

## When to Use This Skill

Use when:
- Adding EF Core data access to a WinUI 3 app
- Choosing between DbContext registration strategies for desktop
- Handling background thread database operations
- Setting up migrations in a separate project
- Configuring connection strings for desktop apps

## Why Desktop Is Different from ASP.NET

In ASP.NET, `DbContext` is registered as **Scoped** — one instance per HTTP request, automatically disposed. Desktop apps have no request scope. The app runs as a single long-lived process, so:

- **Singleton DbContext** — stale cache, memory growth, no concurrent access safety. Never use.
- **Transient DbContext** — works but hard to coordinate Unit of Work across services.
- **IDbContextFactory** — the recommended pattern. Create short-lived contexts on demand.

## IDbContextFactory Pattern (Recommended)

### Registration

```csharp
// In App.xaml.cs or DI setup
services.AddDbContextFactory<MyDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
```

This registers both `IDbContextFactory<MyDbContext>` (singleton factory) and `MyDbContext` (transient, for cases where direct injection is acceptable).

### Usage in ViewModels

```csharp
public partial class ItemsViewModel : ObservableObject
{
    private readonly IDbContextFactory<MyDbContext> _dbFactory;

    public ItemsViewModel(IDbContextFactory<MyDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.Items.ToListAsync();
        Items = new ObservableCollection<Item>(items);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Items.Update(SelectedItem!);
        await db.SaveChangesAsync();
    }
}
```

**Key pattern:** Each operation creates and disposes its own `DbContext`. This prevents stale data, avoids threading issues, and keeps memory bounded.

### When Direct DbContext Injection Is Acceptable

If a ViewModel performs multiple operations that must share a change tracker (e.g. load, modify, save the same entity graph without re-querying), inject `DbContext` directly as transient:

```csharp
services.AddDbContext<MyDbContext>(options =>
    options.UseSqlite(connectionString),
    ServiceLifetime.Transient);  // explicit transient
```

**Trade-off:** Simpler code but the ViewModel must manage the DbContext lifetime. Dispose it when the page navigates away.

## Thread Safety

EF Core's `DbContext` is **not thread-safe**. In WinUI 3, background operations are common (async commands, background services). Rules:

1. Never share a `DbContext` instance across threads.
2. Use `IDbContextFactory` to create per-operation contexts — each runs on whatever thread calls it.
3. If updating UI-bound collections from a background query, marshal to the UI thread via `DispatcherQueue.TryEnqueue()`.

```csharp
[RelayCommand]
private async Task RefreshAsync()
{
    // Runs on thread pool — safe because we create a fresh DbContext
    var items = await Task.Run(async () =>
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Items.AsNoTracking().ToListAsync();
    });

    // Marshal to UI thread
    _dispatcher.TryEnqueue(() =>
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
    });
}
```

## Using with Ploch.Data.GenericRepository

When using `IUnitOfWork` and `IReadRepositoryAsync` from `ploch-data`, the repositories wrap `DbContext` internally. The `AddRepositories<TDbContext>()` registration handles lifetime management:

```csharp
services.AddDbContext<MyDbContext>(options =>
    options.UseSqlite(connectionString));
services.AddRepositories<MyDbContext>(configuration);
```

ViewModels then inject repositories directly:

```csharp
public partial class ItemsViewModel(
    IReadRepositoryAsync<Item, int> itemRepository,
    IUnitOfWork unitOfWork) : ObservableObject
{
    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await itemRepository.GetAllAsync();
        Items = new ObservableCollection<Item>(items);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var repo = unitOfWork.Repository<Item, int>();
        await repo.UpdateAsync(SelectedItem!);
        await unitOfWork.CommitAsync();
    }
}
```

**Note:** With the repository pattern, the DbContext lifetime is managed by the DI container. For desktop apps, consider configuring `DbContext` as transient to avoid long-lived instances.

## Connection String Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=myapp.db;Cache=Shared"
  }
}
```

### Loading Configuration in WinUI 3

```csharp
// In App.xaml.cs
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();
```

Ensure `appsettings.json` is set to **Copy to Output Directory: PreserveNewest** in the `.csproj`:

```xml
<ItemGroup>
    <Content Include="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

### SQLite File Location

For packaged apps (MSIX), write to `ApplicationData.Current.LocalFolder`. For unpackaged apps, use `AppContext.BaseDirectory` or `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)`:

```csharp
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MyApp", "data.db");
var connectionString = $"DataSource={dbPath};Cache=Shared";
```

## Migrations in a Separate Project

EF Core migrations should live in a provider-specific project, not in the WinUI 3 app project. This follows the MrPloch `Data.SQLite` / `Data.SqlServer` pattern:

```
src/
  Data/                    # DbContext + configurations
  Data.SQLite/             # Design-time factory + migrations
  Apps/
    MyWinUI3App/           # WinUI 3 app — NO migrations here
```

The WinUI 3 app references the provider project for runtime migration application:

```csharp
// Apply pending migrations at startup (development only)
await using var db = await dbFactory.CreateDbContextAsync();
await db.Database.MigrateAsync();
```

**Production:** Ship migrations with the MSIX package. Apply them on first launch or version upgrade.

## Change Tracking Considerations

Desktop apps often keep entities alive longer than web apps. Watch for:

1. **Stale data** — If another process modifies the database, your cached entities are outdated. Use `AsNoTracking()` for read-only queries, or re-query before editing.
2. **Large change trackers** — Loading thousands of entities keeps them all tracked. Use `AsNoTracking()` for list views, only track entities being edited.
3. **Detached entity updates** — When using `IDbContextFactory`, entities loaded by one context are detached from the next. Use `db.Update(entity)` or `db.Attach(entity)` to re-associate.

```csharp
// Load with one context (read-only list)
await using (var db = await _dbFactory.CreateDbContextAsync())
{
    Items = new ObservableCollection<Item>(
        await db.Items.AsNoTracking().ToListAsync());
}

// Save with a fresh context (entity is detached)
await using (var db = await _dbFactory.CreateDbContextAsync())
{
    db.Update(SelectedItem!);  // re-attaches as Modified
    await db.SaveChangesAsync();
}
```
