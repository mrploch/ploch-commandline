# Domain Model Standards

Domain entities in MrPloch projects are **simple POCO types** that implement interfaces from the `Ploch.Data.Model` package to standardise common property names. This enables reusable UI components, generic repository operations, and consistent API shapes across projects.

## Core Principle

- Entities **must** implement `Ploch.Data.Model` interfaces for any common property (`Id`, `Name`, `Title`, `Description`, `Contents`, etc.) rather than defining these properties ad-hoc.
- Entities are plain data carriers — no business logic in entity classes.

## Required Interface Usage

| Property | Interface | Notes |
|----------|-----------|-------|
| `Id` | `IHasId<TId>` | Every entity must implement this. Default `TId` is `int`; use `Guid` or `string` where appropriate. |
| `Name` | `INamed` | For entities with a name. Use `INamedReadOnly` if the name should not be settable. |
| `Title` | `IHasTitle` | For entities with a title. Use `IHasTitleReadOnly` for read-only. |
| `Description` | `IHasDescription` | Nullable `string?`. |
| `Contents` | `IHasContents` | Nullable `string?` for textual content. |
| `Notes` | `IHasNotes` | Nullable `string?`. |
| `Value` | `IHasValue<TValue>` | For entities that hold a typed value. |

## Audit Properties

- Use `IHasAuditProperties` for entities that need full audit tracking (`CreatedTime`, `ModifiedTime`, `AccessedTime`, `CreatedBy`, `LastModifiedBy`, `LastAccessedBy`).
- Use `IHasAuditTimeProperties` if only timestamps are needed (no user tracking).
- Use individual interfaces (`IHasCreatedTime`, `IHasModifiedBy`, etc.) if only specific audit fields are needed.

## Common Base Types

- **Category entities** must inherit from `Category<TCategory, TId>` (or `Category<TCategory>` for `int` IDs) from `Ploch.Data.Model.CommonTypes`. Do not create custom category classes from scratch — the base type provides `Id`, `Name`, `Parent`, and `Children` with the correct hierarchical structure.
- **Tag entities** must inherit from `Tag<TId>` (or `Tag` for `int` IDs) from `Ploch.Data.Model.CommonTypes`. The base type provides `Id`, `Name`, and `Description`.

## Hierarchical Entities

- For tree structures (parent/children of the same type), implement `IHierarchicalParentChildrenComposite<TEntity>`.
- For entities that only need a parent reference, use `IHierarchicalWithParent<TParent>` or `IHierarchicalWithParentComposite<TParent>` (self-referential).
- For entities that only need children, use `IHierarchicalWithChildren<TChildren>` or `IHierarchicalWithChildrenComposite<TChildren>` (self-referential).
- Mark `Parent` and `Children` navigation properties as `virtual` for EF Core lazy loading support.

## Categorisation and Tagging

- Entities with categories must implement `IHasCategories<TCategory>` (or `IHasCategories<TCategory, TCategoryId>` for non-`int` IDs). The `TCategory` type must inherit from `Category<TCategory, TCategoryId>`.
- Entities with tags must implement `IHasTags<TTag>` (or `IHasTags<TTag, TTagId>` for non-`int` IDs). The `TTag` type must inherit from `Tag<TTagId>`.

## Entity Style Rules

- Entities are **classes** (not records), unless there is a specific reason for value semantics.
- Use auto-properties with `{ get; set; }`.
- Use `= null!` for required reference-type properties (EF Core will populate them).
- Use `= []` or `= null!` for collection properties.
- Nullable properties (`string?`, `ICollection<T>?`) for optional fields.
- Mark navigation properties as `virtual` when lazy loading may be used.
- Use `[Key]`, `[Required]`, `[MaxLength]` from `System.ComponentModel.DataAnnotations` where appropriate — do not rely solely on Fluent API for basic constraints.
- Keep entities in a dedicated `Model` or `Models` namespace (e.g. `Ploch.Lists.Model`, `Ploch.EditorConfigTools.Models`).
