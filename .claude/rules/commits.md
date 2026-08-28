# Commit Message Standards

All commit messages **must** follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.

## Format

```
<type>(<scope>): <subject>

<body>

[BREAKING CHANGE: <description>]
Refs: #<issue-number>
```

## Structure Rules

- **Header** (`<type>(<scope>): <subject>`): Required. Max 72 characters.
- **Body**: Include when the change is non-trivial. Briefly describe *what* changed and *why*. Wrap at 72 characters.
- **Footer**: Always include `Refs: #<issue-number>`. This is **mandatory** — every commit must reference a GitHub issue. See [Associated issue](#associated-issue) for how to find the right issue number. Do not fabricate issue numbers.
- **Breaking changes**: If any change breaks backward compatibility (public API signature change, removed/renamed public member, configuration key change, behavioural contract change), add a `BREAKING CHANGE:` footer with a description of what consumers must change. Also add `!` after the type/scope in the header: `feat(api)!: ...`.

## Types

| Type | When to use |
|---|---|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, whitespace, semicolons — no logic change |
| `refactor` | Code restructuring without behaviour change |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `build` | Build system, CI, or dependency changes |
| `chore` | Maintenance tasks (tooling, config, housekeeping) |
| `ci` | CI/CD pipeline changes |
| `revert` | Reverting a previous commit |

## Scope

- Use the **project or module name** affected (e.g. `common`, `data`, `lists-api`, `solution`, `ci`).
- For changes spanning the entire repo or solution, use `solution` or the repo short name.
- Keep scope lowercase, hyphen-separated if multi-word.

## Subject Line

- Use **imperative mood** ("Add feature", not "Added feature" or "Adds feature").
- Start with a capital letter.
- No trailing period.

## Detecting Breaking Changes

Before writing the commit message, analyse the staged changes for:

- Removed or renamed public types, methods, properties, or interfaces.
- Changed method signatures (parameter types, return types, parameter order).
- Removed or renamed configuration keys, environment variables, or connection string names.
- Changed default behaviour that existing consumers rely on.
- Removed or renamed NuGet package IDs.
- Changed serialisation format of persisted data.

If any of these are detected, the commit **must** include the `BREAKING CHANGE:` footer.

## Associated Issue

Every commit **must** include a `Refs: #<issue-number>` footer linking to a GitHub issue. Follow this lookup order:

1. **Check the open PR** for the current branch (`gh pr view`). If the PR body or linked issues reference an issue, use that.
2. **Search repository issues** (`gh issue list` or the GitHub MCP tools) for an existing issue that matches the change. If there is a clear candidate, use it — and if there is an open PR without an issue link, associate the issue with the PR.
3. **Ask the user** if no matching issue is found. The user may want to create a new issue for the changes. Do not guess or omit the `Refs` footer — always ask rather than commit without an issue reference.

## Examples

### Simple feature

```
feat(common): Add StringExtensions.ContainsAny method

Added a new extension method that checks whether a string contains
any of the specified substrings.

Refs: #162
```

### Breaking change

```
chore(solution)!: Update ContainsAny namespace

Moved the public API method Strings.ContainsAny to the
StringExtensions class under a new namespace.

BREAKING CHANGE: Ploch.Common.Strings.ContainsAny moved to
Ploch.Common.Extensions.StringExtensions.ContainsAny. Update
using directives accordingly.
Refs: #162
```

### Bug fix

```
fix(data): Prevent duplicate entity on concurrent upsert

Added optimistic concurrency check in the upsert path to avoid
inserting a duplicate when two requests race on the same key.

Refs: #187
```

### Multi-scope refactor

```
refactor(solution): Extract shared audit timestamp logic

Moved SetAuditTimestamps from individual DbContext overrides into
a shared base class to reduce duplication across Data projects.

Refs: #205
```

### Change Log updates

If a commit contains information that should go to the change log, make sure you put it there. Don't put things like styling changes or minor things there. This is especially important for the breaking changes and new features.

### CI/build change

```
ci(github-actions): Add fetch-depth 0 for NBGV versioning

NBGV requires full git history to calculate commit height.
Updated all checkout steps across workflows.

Refs: #210
```
