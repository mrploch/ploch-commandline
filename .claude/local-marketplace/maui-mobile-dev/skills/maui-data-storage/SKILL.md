---
name: maui-data-storage
description: Use when persisting data locally in a .NET MAUI app - SQLite options (sqlite-net-pcl vs EF Core, iOS AOT constraints), file system paths, Preferences vs SecureStorage and their platform caveats. Triggers on - sqlite, local database, ef core mobile, save data, preferences, secure storage, app data directory, offline data.
---

# Local Data & Storage in .NET MAUI

## Choosing a SQLite stack

| Option | iOS full-AOT safe | Model | Choose when |
|---|---|---|---|
| `sqlite-net-pcl` (+`SQLitePCLRaw.bundle_green`) | **Yes** | micro-ORM, attribute POCOs | flat CRUD, local cache, AOT/trimming targets — the low-risk default |
| `Microsoft.Data.Sqlite` | Yes | raw ADO.NET | full SQL control, no ORM |
| EF Core (`Microsoft.EntityFrameworkCore.Sqlite`) | **Problematic** (reflection/dynamic codegen, `[RequiresDynamicCode]`) | full ORM, migrations, LINQ | complex relational model AND you accept Mono interpreter/AOT-fallback on iOS; use compiled models to tame trimming |
| `System.Data.SQLite` | **NEVER** — needs `dlopen`, blocked on iOS | — | never in MAUI |
| LiteDB | unverified for AOT/trim; stable 5.0.21 is 2+ years old, big issue backlog | doc store | avoid for new work |

EF Core works fine on Android/Windows and for most iOS CRUD apps (Microsoft documents it first-class), but is incompatible with full Native AOT on iOS. If the domain model is relational and shared with server code (e.g. reusing `Ploch.Data` GenericRepository patterns), EF Core + `IDbContextFactory` is reasonable; otherwise default to sqlite-net-pcl.

## sqlite-net-pcl pattern

```csharp
public class ItemStore
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;
        var path = Path.Combine(FileSystem.AppDataDirectory, "app.db3");
        _db = new SQLiteAsyncConnection(path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<Item>();
        return _db;
    }

    public async Task<List<Item>> GetItemsAsync() => await (await GetDbAsync()).Table<Item>().ToListAsync();
    public async Task<int> SaveAsync(Item item) =>
        item.Id != 0 ? await (await GetDbAsync()).UpdateAsync(item)
                     : await (await GetDbAsync()).InsertAsync(item);
}
```

## EF Core pattern (when chosen)

Register a **singleton `IDbContextFactory`**, create short-lived contexts per operation — never a scoped/long-lived context (see maui-dependency-injection). On mobile prefer `db.Database.EnsureCreated()` or bundled migrations applied at startup; run migrations off the UI thread with a loading state.

## File system

- `FileSystem.AppDataDirectory` — persistent app-private data (databases, settings files).
- `FileSystem.CacheDirectory` — OS may purge; caches only.
- `FileSystem.OpenAppPackageFileAsync("seed.json")` — read bundled `Resources/Raw` assets (read-only; copy to AppData to modify).
- Never hardcode platform paths.

## Preferences vs SecureStorage

| | `Preferences.Default` | `SecureStorage.Default` |
|---|---|---|
| Backing | plain key/value | Android Keystore-encrypted file, iOS/macOS Keychain, Windows DPAPI |
| Use for | non-sensitive settings, UI state, flags | tokens, secrets — **never store tokens in Preferences** |
| Types | primitives only | strings |

**Platform caveats (design for `GetAsync` returning null!):**

- **Android:** uninstall invalidates the Keystore key → previously stored values are permanently unreadable after reinstall.
- **iOS:** Keychain data often **survives** uninstall/reinstall (opposite behaviour) — don't treat presence as "fresh install".
- **iOS Simulator:** throws "Missing Entitlement" unless `Platforms/iOS/Entitlements.plist` declares the Keychain entitlement + access group.
- Token-refresh flows must tolerate `SecureStorage.GetAsync` returning null at any time and fall back to interactive sign-in.

```csharp
Preferences.Default.Set("last_sync", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
await SecureStorage.Default.SetAsync("refresh_token", token);
var token = await SecureStorage.Default.GetAsync("refresh_token");   // may be null - handle it
```

## Offline-first sync

Local SQLite as source of truth; sync via your own API with explicit conflict resolution (not last-write-wins). `Dotmim.Sync` exists but its maintainers call it beta/single-maintainer — most teams hand-roll an outbox/inbox pattern. Queue writes locally, replay on `IConnectivity.ConnectivityChanged`.
