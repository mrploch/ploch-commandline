# Conventions — ploch-commandline

| Field | Value |
|---|---|
| Last verified | 2026-10-05, against commit `6a2c931` |
| Verified by | Reading the code, `.editorconfig`, git history (41 commits), the 39 merged pull requests |
| Index | [`CLAUDE.md`](../../CLAUDE.md) |

This document records what is actually done. Where practice differs from a written rule,
section 10 says so.

## 1. Order of authority

1. The build: analysers and `.editorconfig`. Warnings are errors, so the build is the final word
   on style.
2. [`CONTRIBUTING.md`](../../CONTRIBUTING.md) for contributor-facing policy.
3. This document for everything the build cannot check.
4. Agent rule files (see [`AGENT-CONFIG.md`](AGENT-CONFIG.md)) for agent workflow.

## 2. Repository layout and naming

- Library-family layout: `src/Spectre/<Dir>/Ploch.<Dir>.csproj`, where the directory is the
  project name without the `Ploch.` prefix.
- Tests mirror sources: `tests/Spectre/<Dir>.Tests/Ploch.<Dir>.Tests.csproj`. A project is a test
  project when its name ends in `Tests`; `Directory.Build.props` keys packaging on that suffix.
- Namespace equals folder path. `AssemblyName`, `RootNamespace` and `PackageId` are never set by
  hand.
- The sample is a self-contained tree under `samples/SampleApp/` with its own solution, build
  props and package versions.
- Solution format is `.slnx`.

## 3. C# code

| Topic | Practice | Example |
|---|---|---|
| Namespaces | File-scoped everywhere | every file |
| Constructors | Primary constructors by default | `CommandAppExecutor`, `AppCommand<TSettings>` |
| Data types | `record` for plain data | `CommandInfo`, `TokenInfo` |
| Nullable | Enabled; flow diagnostics are errors | `.editorconfig` |
| Guards | `x.NotNull()` from `Ploch.Common.ArgumentChecking` | `AppBuilder.ConfigureHost` |
| Async | `Async` suffix; `ConfigureAwait(false)` in library code | `AsyncAppCommand<TSettings>` |
| Fields | Private fields `_camelCase`; constants PascalCase | `.editorconfig` naming rules |
| Fluent calls | Return value ignored without a discard | `IDE0058` disabled |
| Modifiers | Types are open `public class`; `sealed` is rare | DI bridge types are sealed |
| Suppressions | `[SuppressMessage(..., Justification = ...)]` on the member | `AppBuilder.ConfigureAppConfiguration` |

- **XML documentation** is required on every public member in `src/` and written in British
  English. Summaries say what; `<remarks>` carry guarantees and ordering rules.
- **Comments explain why,** often at length, and cite the issue that motivated the code.
- **Language level** is current C#: collection expressions, `params IEnumerable<T>`,
  `System.Threading.Lock`.
- **Line length** is 160 for code (`.editorconfig`).
- **Formatting** follows `.editorconfig`; ReSharper settings are in the same file.

## 4. Tests

- **Stack:** xUnit v3, FluentAssertions (global using), Moq, and `[AutoMockData]` from
  `Ploch.TestingSupport.XUnit3.AutoMoq`. Test projects are executables.
- **Class name:** `<TypeUnderTest>Tests`.
- **Method name:** `<Member>_should_<behaviour>`, lower-case words after the member name. For
  example `ConfigureCommandApp_should_reject_an_application_without_a_name`.
- **Shared state:** a test class that changes `AnsiConsole.Console` or
  `EnvironmentSettings.Current` joins the non-parallel collection `GlobalConsoleState`.
- **Helpers** live in `tests/Spectre/CommandLine.Spectre.Tests/Testing/` (`RecordingConsole`,
  `NoOpOutput`).
- **Commands are tested directly:** construct the command with mocks and call `Execute`; see the
  sample's tests. Calling `Execute` does not run validation — `Validate` is separate.
- **Behaviour changes come with a test that pins them,** usually named after the issue's
  observable effect.
- **Size on the verification date:** 289 tests in the main solution, 49 in the sample.

## 5. Documentation and change log

- A user-visible change adds `change-log/<issue-or-pr>-<slug>.md` using "Added / Changed / Fixed"
  headings; see [`change-log/README.md`](../../change-log/README.md). The release workflow turns
  these into the GitHub Release notes and archives them.
- `RELEASE_NOTES.md` is edited by hand in the same pull request.
- A public API change updates the XML docs, `docs/GETTING_STARTED.md`, the package `README.md`
  under `src/`, and the sample when it demonstrates the feature.
- Snippets in documentation must compile against the current API.
- Prose is British English.

## 6. Commits

- [Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <Subject>`,
  imperative, capitalised, no full stop, at most 72 characters.
- Types seen on `main`: `fix`, `ci`, `build`, `feat`, `docs`, `refactor`, `chore`.
- Scopes seen: `spectre`, `serilog`, `sample`, `solution`, `workflows`, `release`, `qodana`,
  `packaging`, `deps`, `ci`, `build`, `rules`, `github-actions`.
- A breaking change has `!` after the scope and a `BREAKING CHANGE:` footer.
- Footer `Refs: PLO-<n>` links the Linear issue.
- The body explains what changed and why, wrapped at 72 characters.
- Never amend a pushed commit; add a new one.

## 7. Branches and worktrees

- Branch name: `<type>/plo-<n>-<short-description>`, lower-case, hyphenated, where `<type>` is
  `feature`, `fix`, `chore`, `refactor`, `docs`, `test`, `perf`, `ci` or `build`.
- Each task is developed in its own git worktree, created from `origin/main`. A sibling directory
  keeps the relative path to `../mrploch-development` working.
- The default branch is `main`.

## 8. Pull requests and merge

- Title is the Conventional Commit header that the squash commit will carry.
- Body follows [`.github/pull_request_template.md`](../../.github/pull_request_template.md):
  "Describe your changes" (with Summary, Changes, Design decisions, Testing sub-sections),
  "Issue ticket number and link", and the checklist.
- The body carries `Closes PLO-<n>` so Linear closes the issue on merge.
- Pull requests are **squash-merged**; the commit subject ends with `(#<pr>)`.
- A pull request is complete when every check is green, static-analysis services report no new
  findings, and every review thread has a reply and is resolved. Bot threads count.
- Review threads must be resolved before merge; this is enforced by the `main` ruleset.

## 9. Issues

- Issues are Linear issues: team **Ploch**, project **ploch-commandline**, identifier `PLO-<n>`.
  Never create a GitHub issue by hand.
- Older work was tracked as GitHub issues `#<n>`; those were migrated and each has a Linear issue.
- Release work carries the project milestone `Release 1.0`.
- A follow-up found during review becomes a Linear issue, not a comment that says "later".

## 10. Where practice differs from the written rules

| Rule says | Practice | Note |
|---|---|---|
| Every commit has `Refs: PLO-<n>` | History before 2026-09-19 uses `Refs: #<n>` | Linear became the tracker on that date |
| Branches use the Linear identifier | Older branches use the GitHub number | Same cause; new branches comply |
| Target framework is net9.0 | Everything targets net10.0 | Workspace notes are out of date |
| Licence is MIT | The repository is Apache-2.0 | Workspace notes are out of date |
| Committed `commits.md`, `branch-naming.md`, `traceability.md` and `work-traceability.md` rules require GitHub issues and `#<n>` references | Linear issues and `PLO-<n>` are used | The committed rule copies are stale |
| Sub-packages include Autofac | No Autofac package exists | Removed before the rewrite |
| Test stack lists AutoFixture | AutoFixture is used only through `[AutoMockData]` | Indirect |
| One test class per file, named after it | Three test files hold differently named or multiple classes | Listed in `BACKLOG.md` |
| Pull request template covers the process | Template is generic and asks about "analytics" | Listed in `AGENT-CONFIG.md` |
