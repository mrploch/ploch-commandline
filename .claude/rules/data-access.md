# Data Access Standards

Rules for consuming `Ploch.Data.GenericRepository` libraries in MrPloch projects. Covers repository injection, Unit of Work usage, Specification pattern, and testing. For DbContext and entity configuration setup, see `data-project.md`. For entity design, see `domain-model.md`.

## Repository Interface Hierarchy

The `Ploch.Data.GenericRepository` package provides a layered interface hierarchy. Choose the most restrictive interface that satisfies the consumer's needs:

| Interface | Purpose | Use When |
|-----------|---------|----------|
| `IQueryableRepository<TEntity>` | Exposes `IQueryable<TEntity> Entities` and `GetPageQuery()` | Direct LINQ access needed (rare, prefer Specification) |
| `IReadRepositoryAsync<TEntity>` | Read operations without typed ID: `GetAllAsync()`, `FindFirstAsync()`, `CountAsync()`, `GetPageAsync()` | Reading entities where ID type does not matter |
| `IReadRepositoryAsync<TEntity, TId>` | Adds `GetByIdAsync(TId id, ...)` | Reading entities by typed primary key |
| `IWriteRepositoryAsync<TEntity, TId>` | `AddAsync()`, `UpdateAsync()`, `DeleteAsync()` | Write-only access (uncommon) |
| `IReadWriteRepositoryAsync<TEntity, TId>` | Combines read + write | Full CRUD access to a single entity type |

**Constraint:** All entities used with repositories **must** implement `IHasId<TId>` from `Ploch.Data.Model`.

## Choosing Between Repository and Unit of Work

### Direct Repository Injection

Inject `IReadRepositoryAsync<TEntity, TId>` or `IReadWriteRepositoryAsync<TEntity, TId>` directly when operating on a **single entity type** with no cross-entity transactional requirements:

```csharp
public class ListProfilesUseCase(IReadRepositoryAsync<SystemProfile, int> profileRepository)
{
    public async Task<IList<SystemProfile>> ExecuteAsync(CancellationToken ct = default)
    {
        return await profileRepository.GetAllAsync(cancellationToken: ct);
    }
}
```

- Prefer `IReadRepositoryAsync<TEntity, TId>` for read-only consumers.
- Prefer `IReadWriteRepositoryAsync<TEntity, TId>` only when the consumer needs both read and write on that entity.

### Unit of Work Injection

Inject `IUnitOfWork` when:
- **Multiple entity types** must be modified in a single atomic transaction.
- The consumer needs to **commit or rollback** explicitly.
- You want to **retrieve repositories dynamically** by entity type.

```csharp
public class CreateProfileUseCase(IUnitOfWork unitOfWork)
{
    public async Task<int> ExecuteAsync(CreateProfileRequest request, CancellationToken ct = default)
    {
        var profileRepo = unitOfWork.Repository<SystemProfile, int>();
        var tagRepo = unitOfWork.Repository<SystemProfileTag, int>();

        var profile = new SystemProfile { Name = request.Name };
        await profileRepo.AddAsync(profile, ct);

        foreach (var tagName in request.Tags)
        {
            await tagRepo.AddAsync(new SystemProfileTag { Name = tagName }, ct);
        }

        await unitOfWork.CommitAsync(ct);
        return profile.Id;
    }
}
```

### IUnitOfWork API

```csharp
public interface IUnitOfWork : IDisposable
{
    IReadWriteRepositoryAsync<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class, IHasId<TId>;

    TRepository Repository<TRepository, TEntity, TId>()
        where TRepository : IReadWriteRepositoryAsync<TEntity, TId>
        where TEntity : class, IHasId<TId>;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

## Read Operations

### GetAllAsync

Retrieve all entities, optionally with a filter predicate:

```csharp
var allProfiles = await repository.GetAllAsync(cancellationToken: ct);
var activeProfiles = await repository.GetAllAsync(p => p.IsActive, cancellationToken: ct);
```

### GetByIdAsync

Retrieve a single entity by typed primary key:

```csharp
var profile = await repository.GetByIdAsync(profileId, cancellationToken: ct);
if (profile is null)
    return Result.NotFound();
```

### FindFirstAsync

Find the first entity matching a predicate. Use the `onDbSet` parameter for eager loading:

```csharp
var existing = await repository.FindFirstAsync(
    s => s.Name == serviceName,
    cancellationToken: ct);
```

### GetPageAsync

Paginated queries with optional sorting and filtering:

```csharp
var page = await repository.GetPageAsync(
    pageNumber: 1,
    pageSize: 20,
    sortBy: p => p.Name,
    query: p => p.IsActive,
    cancellationToken: ct);
```

### CountAsync

Count entities with optional filter:

```csharp
var total = await repository.CountAsync(p => p.IsActive, ct);
```

### Eager Loading via onDbSet

Several read methods accept a `Func<IQueryable<TEntity>, IQueryable<TEntity>>? onDbSet` parameter to apply `Include()` calls:

```csharp
var profile = await repository.GetByIdAsync(
    profileId,
    onDbSet: q => q.Include(p => p.Tags).Include(p => p.Categories),
    cancellationToken: ct);
```

## Write Operations

All write operations are performed through repositories. Changes are persisted either implicitly (direct repository injection) or explicitly (via `IUnitOfWork.CommitAsync()`).

### Add

```csharp
var entity = new SystemProfile { Name = "New Profile" };
await repository.AddAsync(entity, ct);
// entity.Id is populated after save
```

### AddRange

```csharp
var entities = new[] { new Tag { Name = "A" }, new Tag { Name = "B" } };
await repository.AddRangeAsync(entities, ct);
```

### Update

```csharp
var existing = await repository.GetByIdAsync(id, ct);
existing.Name = "Updated Name";
await repository.UpdateAsync(existing, ct);
```

### Delete

By entity or by ID:

```csharp
await repository.DeleteAsync(entity, ct);
await repository.DeleteAsync(entityId, ct);
```

### Upsert Pattern

Check-then-add-or-update when a unique constraint exists beyond the primary key:

```csharp
var existing = await repository.FindFirstAsync(s => s.Name == name, cancellationToken: ct);
if (existing != null)
{
    existing.Value = newValue;
    await repository.UpdateAsync(existing, ct);
}
else
{
    await repository.AddAsync(new Entity { Name = name, Value = newValue }, ct);
}
await unitOfWork.CommitAsync(ct);
```

## Specification Pattern (Ardalis.Specification)

Use Specifications to encapsulate reusable, composable query logic. Prefer Specifications over inline LINQ predicates for queries that include eager loading or complex filtering.

### Required Packages

- `Ardalis.Specification` — base types
- `Ploch.Data.GenericRepository.EFCore` — integrates Specification with repositories

### Single vs Multiple Result Specifications

```csharp
// Returns multiple results
public class ProfileSearchSpecification : Specification<SystemProfile>
{
    public ProfileSearchSpecification(string? nameContains, IEnumerable<string>? tags)
    {
        Query.Include(x => x.Tags)
             .Where(x => x.Name.Contains(nameContains!), nameContains is not null)
             .Where(p => p.Tags!.Any(t => tags!.Contains(t.Name)), tags is not null && tags.Any());
    }
}

// Returns single result
public class GetProfileByIdOrNameSpecification : SingleResultSpecification<SystemProfile>
{
    public GetProfileByIdOrNameSpecification(int? id, string? name)
    {
        Query.Include(p => p.Tags)
             .Include(p => p.Actions!)
             .ThenInclude(a => a.ApplicationMatching)
             .Where(p => p.Name.Equals(name), name is not null)
             .Where(p => p.Id == id!.Value, id.HasValue);
    }
}
```

### Consuming Specifications

```csharp
// Multiple results
var profiles = await repository.GetAllBySpecificationAsync(
    new ProfileSearchSpecification(nameFilter, tagFilter), ct);

// Single result
var profile = await repository.GetBySpecificationAsync(
    new GetProfileByIdOrNameSpecification(id, name), ct);
```

### Specification Guidelines

- Inherit from `Specification<TEntity>` for multi-result queries.
- Inherit from `SingleResultSpecification<TEntity>` for queries expected to return zero or one result.
- Use conditional `Where` clauses with the boolean overload: `.Where(predicate, condition)`.
- Include related entities via `Query.Include()` and `ThenInclude()`.
- Keep Specifications in a `Specifications` folder/namespace within the consuming project (e.g. `UseCases/Specifications/`).

## DI Registration

### Standard Registration

In the Data project's `ServiceCollectionRegistrations` class, call `AddRepositories<TDbContext>()` to register all repository interfaces and `IUnitOfWork`:

```csharp
public static IServiceCollection AddDataServices(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> configureOptions,
    IConfiguration configuration)
{
    return services
        .AddDbContext<{Product}DbContext>(configureOptions)
        .AddRepositories<{Product}DbContext>(configuration);
}
```

This single call registers:
- `IQueryableRepository<TEntity>` as `QueryableRepository<TEntity>`
- `IReadRepositoryAsync<TEntity>` as `ReadRepositoryAsync<TEntity>`
- `IReadRepositoryAsync<TEntity, TId>` as `ReadRepositoryAsync<TEntity, TId>`
- `IReadWriteRepositoryAsync<TEntity, TId>` as `ReadWriteRepositoryAsync<TEntity, TId>`
- `IUnitOfWork` as `UnitOfWork<TDbContext>`

### ServicesBundle Registration

For applications using the `ServicesBundle` pattern from `ploch-common`, inherit from `GenericRepositoriesServicesBundle<TDbContext>`:

```csharp
public class MyDataBundle : GenericRepositoriesServicesBundle<MyDbContext>
{
    protected override Action<DbContextOptionsBuilder> GetOptionsBuilderAction(IConfiguration? configuration)
    {
        return options => options.UseSqlite(
            configuration.RequiredNotNull().GetConnectionString("DefaultConnection"));
    }
}
```

Register in application startup:

```csharp
services.AddServicesBundle(new MyDataBundle(), configuration);
```

### Custom Repository Registration

When extending the base repository with domain-specific logic, register the custom type explicitly:

```csharp
public class CustomListsRepository(DbContext dbContext, IAuditEntityHandler auditEntityHandler)
    : ReadWriteRepositoryAsync<List, int>(dbContext, auditEntityHandler)
{
    public override async Task UpdateAsync(List entity, CancellationToken ct = default)
    {
        // Custom logic before update
        await base.UpdateAsync(entity, ct);
    }
}

// Registration
services.AddScoped<IReadWriteRepositoryAsync<List, int>, CustomListsRepository>();
```

## Error Handling

Repository operations throw specific exceptions from `Ploch.Data.GenericRepository`:

| Exception | When | Handling |
|-----------|------|----------|
| `DataAccessException` | Read operation failure | Log and return error result |
| `DataUpdateException` | `CommitAsync()` or write failure | Log and return error result |
| `DataUpdateConcurrencyException` | Optimistic concurrency violation | Retry or notify user |

```csharp
try
{
    await unitOfWork.CommitAsync(ct);
}
catch (DataUpdateException ex)
{
    logger.LogError(ex, "Failed to save profile");
    return Result.Error(ex.Message);
}
```

## Testing with Repositories

### Integration Test Setup

Use an in-memory database for integration tests:

```csharp
public abstract class RepositoryTestFixture
{
    protected readonly MyDbContext DbContext;

    protected RepositoryTestFixture()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        DbContext = new MyDbContext(options);
    }

    protected IReadWriteRepositoryAsync<TEntity, int> GetRepository<TEntity>()
        where TEntity : class, IHasId<int>
    {
        return new ReadWriteRepositoryAsync<TEntity, int>(DbContext);
    }
}
```

### Unit Testing with Mocks

Mock the repository interface for unit testing use cases:

```csharp
[Fact]
public async Task ListProfiles_ReturnsAllProfiles()
{
    var mockRepo = new Mock<IReadRepositoryAsync<SystemProfile, int>>();
    mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemProfile> { new() { Name = "Test" } });

    var useCase = new ListProfilesUseCase(mockRepo.Object);
    var result = await useCase.ExecuteAsync();

    Assert.Single(result);
}
```

### Testing with IUnitOfWork

```csharp
[Fact]
public async Task CreateProfile_CommitsSuccessfully()
{
    var mockUow = new Mock<IUnitOfWork>();
    var mockRepo = new Mock<IReadWriteRepositoryAsync<SystemProfile, int>>();
    mockUow.Setup(u => u.Repository<SystemProfile, int>()).Returns(mockRepo.Object);
    mockUow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    var useCase = new CreateProfileUseCase(mockUow.Object);
    var result = await useCase.ExecuteAsync(new CreateProfileRequest("Test"));

    mockRepo.Verify(r => r.AddAsync(It.IsAny<SystemProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    mockUow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

## Application Layer Patterns

### Use Case Pattern

Encapsulate a single business operation in a use case class. Inject the narrowest repository interface needed:

```csharp
public class GetProfileDetailsUseCase(
    IReadRepositoryAsync<SystemProfile, int> profileRepository,
    ILogger<GetProfileDetailsUseCase> logger)
{
    public async Task<Result<ProfileDetails>> ExecuteAsync(int profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await profileRepository.GetByIdAsync(profileId, ct);
            if (profile is null)
                return Result.NotFound();
            return Result.Success(MapToDetails(profile));
        }
        catch (DataAccessException ex)
        {
            logger.LogError(ex, "Error retrieving profile {ProfileId}", profileId);
            return Result.Error(ex.Message);
        }
    }
}
```

### Storage/Service Pattern

For infrastructure services that persist state, inject `IUnitOfWork`:

```csharp
public class DbStateStorage(IUnitOfWork unitOfWork) : IStateStorage
{
    public async Task SaveStateAsync(IEnumerable<ServiceInfo> services, CancellationToken ct = default)
    {
        var repository = unitOfWork.Repository<KnownService, int>();

        foreach (var service in services)
        {
            var existing = await repository.FindFirstAsync(s => s.Name == service.Name, cancellationToken: ct);
            if (existing != null)
            {
                existing.Status = service.Status;
                await repository.UpdateAsync(existing, ct);
            }
            else
            {
                await repository.AddAsync(MapToEntity(service), ct);
            }
        }

        await unitOfWork.CommitAsync(ct);
    }
}
```

## Quick Reference

| I want to... | Use |
|--------------|-----|
| Read entities | `IReadRepositoryAsync<TEntity, TId>` |
| Read + write a single entity type | `IReadWriteRepositoryAsync<TEntity, TId>` |
| Write across multiple entity types atomically | `IUnitOfWork` |
| Encapsulate a complex query | `Specification<TEntity>` or `SingleResultSpecification<TEntity>` |
| Register all repositories | `services.AddRepositories<TDbContext>(configuration)` |
| Register with ServicesBundle | Inherit `GenericRepositoriesServicesBundle<TDbContext>` |
| Add custom repository logic | Inherit `ReadWriteRepositoryAsync<TEntity, TId>` |
