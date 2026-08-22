# Change log entries

Each user-visible change gets its own Markdown file in this folder, named after
the issue or pull request it came from (for example `257-cancellation-token.md`).

The release workflow concatenates every `*.md` file here into the GitHub Release
notes, then moves them into `archive/` so the next release starts empty.

## What belongs here

Write an entry for anything a consumer of the packages would want to know about:

- New features and new public API.
- Breaking changes — always, with a note on what callers must change.
- Bug fixes that alter observable behaviour.

## What does not

Formatting, refactoring with no behavioural change, test-only changes, CI
tweaks, and dependency bumps that do not affect the public surface. The commit
log already covers those.

## Format

```markdown
### Added

- `AppCommand<TSettings>` now receives a `CancellationToken` (#123).

### Changed

- **Breaking:** `IMessageFormatterProcessor.WriteMessage` returns `bool` instead
  of `void`, so callers can tell whether the message was handled (#124).
```
