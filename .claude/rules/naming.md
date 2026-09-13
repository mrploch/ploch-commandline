# Identifier Naming Standards

Naming rules for **identifiers inside code** — types, members, parameters, locals. For **project, assembly, namespace, and package** names, see [`project-naming.md`](./project-naming.md); the two rules do not overlap.

The workspace is primarily C#, so C# rules come first and are the default. Language-specific sections at the end cover the exceptions.

---

## C# — Casing

C# casing is **not a matter of taste**; it is fixed by the [.NET Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/naming-guidelines) and enforced by the analysers already enabled in every repo (StyleCop, Roslynator, `Microsoft.CodeAnalysis.NetAnalyzers`). Deviating produces build warnings, and `TreatWarningsAsErrors` turns those into build failures in test projects.

| Identifier | Casing | Example |
|---|---|---|
| Class, struct, record, enum, delegate | PascalCase | `ProfileRepository` |
| Interface | PascalCase, `I` prefix | `IUnitOfWork` |
| Method | PascalCase | `RemoveUserFromList` |
| Property, event | PascalCase | `CreatedTime` |
| Public / protected field (rare — prefer a property) | PascalCase | `Empty` |
| Private field | `_camelCase` | `_profileRepository` |
| `const` / `static readonly` | PascalCase — **never** `SCREAMING_CASE` | `DefaultTimeout` |
| Parameter, local variable | camelCase | `cancellationToken` |
| Generic type parameter | PascalCase, `T` prefix | `TEntity`, `TId` |
| Enum member | PascalCase | `DeleteBehavior.Cascade` |
| Local function | PascalCase | `static bool IsMatch(...)` |

**Never use camelCase for a method or property in C#.** `shouldLogUserOutAfterTransfer` is a JavaScript identifier; the C# form is `ShouldLogUserOutAfterTransfer`.

**Async methods end with `Async`** when they return `Task`/`Task<T>`/`ValueTask<T>` — `GetByIdAsync`, `CommitAsync` — matching the repository interfaces in `Ploch.Data.GenericRepository`. The suffix is omitted only where another contract fixes the name: overriding or implementing a member whose base or interface name has no suffix, and ASP.NET Core controller actions, whose method names form routes. An override of `ExecuteAsync` keeps the suffix, because the base member already has it.

---

## C# — Word Choice

These rules apply regardless of casing, and they are where most real naming defects live.

- **Methods start with a verb.** `RemoveUserFromList`, `CalculateTotal`, `ParseConnectionString` — not `UserRemoval` or `TotalCalculation`. A method *does* something; a name without a verb hides what.
- **Booleans read as an assertion.** Prefix with `Is`, `Are`, `Has`, `Can`, `Should`, or `Was`: `IsActive`, `HasChildren`, `CanExecute`, `ShouldLogUserOutAfterTransfer`. A boolean named `Status` or `Flag` forces the reader to open the definition.
- **No abbreviations or contractions.** The guidelines are explicit — `GetWindow`, not `GetWin`. Use `configuration` not `cfg`, `repository` not `repo`, `authentication` not `auth`, `count` not `cnt`. Widely accepted acronyms and abbreviations (`Id`, `Db`, `Api`, `Http`, `Xml`, `Json`, `UI`) are the exception.
- **Acronym casing follows the .NET guidelines:** two-letter acronyms are fully capitalised (`IO`, `UI`), three-or-more are PascalCased (`Xml`, `Html`, `Json`, `Http`). `Id`, `Ok` and `Db` are abbreviations rather than acronyms, so they are PascalCased like words: `UserId`, `DbContext`. So `HttpClient` and `XmlReader`, but `IOException` and `UIElement`. In camelCase positions the first acronym is lowercased whole: `ioStream`, `htmlParser`.
- **Say the domain, not the mechanism.** `profileRepository` beats `repo2`; `retryDelay` beats `ts`.
- **Avoid `Manager`, `Helper`, `Util`, `Processor`, `Handler`, `Info`, and `Data` as type-name suffixes.** They describe nothing — a `ProfileManager` could do anything. Name the responsibility: `ProfileValidator`, `ProfileImporter`, `ProfileCache`.
- **Do not encode the type in the name.** `strName`, `intCount`, and `lstProfiles` are Hungarian notation; C# has a type system.
- **Match the codebase's existing vocabulary.** If the domain already says "Entry", a new type is not an "Item".

### Names that collide

The same shadowing trap that governs namespace segments applies to type names: do not name a type after a BCL type it will sit alongside — `Task`, `File`, `Path`, `Type`, `Timer`, `Action`, `Event`, `Stream`, `Version`, `Index`, `Range`. Qualify with the domain instead (`WorkItem`, `TrackedFile`, `AuditEvent`). See [`project-naming.md`](./project-naming.md#names-that-collide) for the full list and the entity-naming table.

---

## Test Naming

Test class and method naming is governed by [`writing-dotnet-tests.md`](./writing-dotnet-tests.md) and deliberately **breaks** the PascalCase method rule above: test methods use `<Member>_should_<expected behaviour>` with lowercase words, because the method name is a sentence read in a test report, not an API surface. That exception is confined to test projects.

---

## Other Languages

- **PowerShell** (`scripts/`, setup and provisioning scripts): `Verb-Noun` for functions, using an [approved verb](https://learn.microsoft.com/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands) (`Install-SessionEndHook`, `Get-ProjectConfig`); PascalCase for parameters (`-WhatIf`, `-ConfigPath`); camelCase for local variables.
- **JavaScript / TypeScript** (`ploch-ai-site` and any web tooling): camelCase for functions, methods, properties and variables; PascalCase for classes, types and components; `SCREAMING_SNAKE_CASE` for module-level constants. The verb-first and boolean-prefix rules above still apply — they are about word choice, not casing.
- **SQL / EF Core column names:** follow whatever the entity configuration establishes for the repo; do not introduce a second convention.
