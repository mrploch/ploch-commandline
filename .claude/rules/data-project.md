# Data Project Standards

Rules for creating and structuring EF Core Data projects in MrPloch repositories. A Data project contains the `DbContext`, entity type configurations, and DI registration for a domain model.

## Project Structure

```
src/
  Data/
    Configurations/
      {Entity}Configuration.cs        # One per entity
    {Product}DbContext.cs              # The DbContext class
    ServiceCollectionRegistrations.cs  # DI extension methods
    Ploch.{Product}.Data.csproj        # Project file
```

- **Project name:** `Ploch.{Product}.Data` (e.g. `Ploch.Lists.Data`, `Ploch.Tools.SystemProfiles.Data`).
- **Namespace:** `Ploch.{Product}.Data`.
- Entity configurations go in a `Configurations` subfolder with namespace `Ploch.{Product}.Data.Configurations`.
- The project **must** reference the corresponding Model project (`Ploch.{Product}.Model` or `Ploch.{Product}.Domain.Db`).

## Project File (.csproj)

Required package references:
- `Microsoft.EntityFrameworkCore` — always required.
- `Microsoft.EntityFrameworkCore.Relational` — if using relational-specific features (e.g. `HasConversion`, `HasIndex`).
- `Microsoft.EntityFrameworkCore.Tools` — if EF Core migrations will be managed in this project (set `PrivateAssets=all`).

Optional references:
- `Ploch.Data.GenericRepository.EFCore` or `Ploch.Data.EFCore` — for generic repository and Unit of Work integration.
- `Microsoft.EntityFrameworkCore.Proxies` — only if lazy loading proxies are required.

## DbContext Class

### Naming

- Name the class `{Product}DbContext` (e.g. `ListsDbContext`, `SystemProfilesDbContext`, `EditorConfigDbContext`).

### Constructors

```csharp
public class {Product}DbContext : DbContext
{
    protected {Product}DbContext()
    { }

    public {Product}DbContext(DbContextOptions<{Product}DbContext> options) : base(options)
    { }
}
```

- Include a `protected` parameterless constructor for EF Core tooling (migrations, design-time factory).
- The primary constructor takes `DbContextOptions<TContext>` — always use the strongly-typed generic variant, not `DbContextOptions`.
- If the project uses ASP.NET Identity, inherit from `IdentityDbContext<TUser>` instead of `DbContext`.

### DbSet Properties

- Declare a `DbSet<TEntity>` property for **every entity** that should be directly queryable.
- Use plural names for DbSet properties (e.g. `Lists`, `ListItems`, `SystemProfiles`).
- Include DbSet properties for derived types in TPH hierarchies if they need to be queried directly.

```csharp
public DbSet<List> Lists { get; set; }
public DbSet<ListItem> ListItems { get; set; }
```

### OnModelCreating

- **Always** use assembly scanning — do not configure entities inline.
- Call `base.OnModelCreating()` after applying configurations (required when inheriting from `IdentityDbContext`; good practice for plain `DbContext`).

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof({Product}DbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

### Audit Timestamp Tracking

If any entities implement `IHasAuditProperties` or `IHasAuditTimeProperties`, override `SaveChanges` and `SaveChangesAsync` to automatically set timestamps:

```csharp
public override int SaveChanges()
{
    SetAuditTimestamps();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    SetAuditTimestamps();
    return base.SaveChangesAsync(cancellationToken);
}

private void SetAuditTimestamps()
{
    var now = DateTimeOffset.UtcNow;
    foreach (var entry in ChangeTracker.Entries<IHasAuditTimeProperties>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedTime = now;
                entry.Entity.ModifiedTime = now;
                break;
            case EntityState.Modified:
                entry.Entity.ModifiedTime = now;
                break;
        }
    }
}
```

## Entity Type Configurations

### One Class Per Entity

- Create one configuration class per entity implementing `IEntityTypeConfiguration<TEntity>`.
- Name the class `{Entity}Configuration` (e.g. `ListConfiguration`, `ProjectConfiguration`).
- Mark the class as `internal` — configurations are implementation details of the Data project.

```csharp
internal class ListConfiguration : IEntityTypeConfiguration<List>
{
    public void Configure(EntityTypeBuilder<List> builder)
    {
        // Configuration here
    }
}
```

### What to Configure

**Always configure in Fluent API (in the configuration class):**
- Relationships (`HasOne`, `HasMany`, `WithOne`, `WithMany`).
- Delete behaviour (`OnDelete`) — always set explicitly; do not rely on EF Core conventions.
- Discriminators for TPH inheritance (`HasDiscriminator`).
- Indexes (`HasIndex`).
- Many-to-many join tables (`HasMany(...).WithMany(...)`).
- Enum-to-string conversions (`HasConversion<string>()`).

**Prefer Data Annotations on the entity (in the Model project):**
- `[Key]` for primary keys (when not following EF Core naming conventions).
- `[Required]` for required properties.
- `[MaxLength]` for string length constraints.

**Do not duplicate** — if a constraint is expressed via a Data Annotation on the entity, do not repeat it in the Fluent API configuration.

### Relationship Configuration Patterns

```csharp
// One-to-many
builder.HasMany(e => e.Items)
       .WithOne(e => e.List)
       .OnDelete(DeleteBehavior.Cascade);

// Many-to-many
builder.HasMany(e => e.Tags)
       .WithMany(e => e.SystemProfiles);

// Optional relationship
builder.HasOne(e => e.Parent)
       .WithMany(e => e.Children)
       .IsRequired(false);

// Self-referential hierarchy (for entities implementing IHierarchicalParentChildrenComposite)
builder.HasOne(e => e.Parent)
       .WithMany(e => e.Children)
       .IsRequired(false);
```

### TPH Discriminator Pattern

For entity inheritance hierarchies, configure the discriminator in the base entity's configuration:

```csharp
builder.HasDiscriminator<string>("discriminator_column")
       .HasValue<DerivedTypeA>(nameof(DerivedTypeA))
       .HasValue<DerivedTypeB>(nameof(DerivedTypeB));
```

### Enum Conversion Pattern

Store enums as strings for readability:

```csharp
builder.Property(e => e.Status)
       .IsRequired()
       .HasConversion<string>()
       .HasMaxLength(32);
```

## DI Registration

- Create a static class `ServiceCollectionRegistrations` (or `ServiceCollectionRegistration`) with extension methods.
- Register the DbContext and optionally the generic repositories from `ploch-data`.

```csharp
public static class ServiceCollectionRegistrations
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        IConfiguration configuration)
    {
        return services
            .AddDbContext<{Product}DbContext>(configureOptions)
            .AddRepositories<{Product}DbContext>(configuration);
    }
}
```

- The `AddRepositories<TContext>()` method comes from `Ploch.Data.GenericRepository.EFCore` and registers the generic repository and Unit of Work. See `data-access.md` for the full repository and Unit of Work consumption patterns.
- If generic repositories are not needed, register just the DbContext.

## Naming Summary

| Item | Naming Pattern | Example |
|------|---------------|---------|
| Project | `Ploch.{Product}.Data` | `Ploch.Lists.Data` |
| DbContext | `{Product}DbContext` | `ListsDbContext` |
| Configuration | `{Entity}Configuration` | `ListConfiguration` |
| DI class | `ServiceCollectionRegistrations` | — |
| DI method | `Add{Product}DataServices` or `AddDataServices` | `AddDataServices` |
| DbSet property | Plural entity name | `Lists`, `ListItems` |
