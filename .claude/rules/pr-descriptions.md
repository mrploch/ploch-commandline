# Pull Request Description Standards

## Core Rule

Every PR **must** have a detailed description. All changes and decisions **must** be documented in the PR body. A reviewer should be able to understand the full scope and rationale without reading the code first.

## Structure

If a `.github/pull_request_template.md` exists in the repository, follow it. Otherwise, use this structure:

```markdown
## Summary

Brief description of what this PR does and why.

## Changes

- Bullet list of all meaningful changes
- Include file/module scope where helpful
- Group by feature or area if the PR is large

## Design Decisions

Document any non-obvious choices made during implementation:
- Why a particular approach was chosen over alternatives
- Trade-offs considered
- Constraints that influenced the design

## Testing

- What automated tests were added or modified
- What manual testing was performed
- Test coverage impact

## Breaking Changes

List any breaking changes and what consumers must update.
Omit this section entirely if there are no breaking changes.

## Related

- Closes PLO-<n>
- Related to PLO-<other>
- Depends on <other-pr-url> (if cross-repo dependency)
```

## Rules

- **Link the issue:** Use `Closes PLO-<n>` when the PR completes the issue, and `Refs PLO-<n>` for partial work. Both are Linear magic words that link the PR; only `Closes` moves the issue to Done on merge. See [`issue-tracking.md`](./issue-tracking.md).
- **Synced GitHub twin in the same repository:** mirror the same choice. Add `Closes #<n>` only alongside `Closes PLO-<n>`; for partial work write `Refs #<n>`, which links the twin without closing it. Skip the twin for one in another repository.
- **Document decisions:** If you chose approach A over approach B, explain why. Reviewers and future maintainers need this context.
- **Be specific:** "Updated the data layer" is insufficient. "Added `GetBySpecificationAsync` method to `IReadRepositoryAsync<TEntity>` for Ardalis.Specification support" is specific.
- **Include test evidence:** Mention test counts, coverage percentages, or specific scenarios tested.
- **Update on subsequent pushes:** If you push fixes for CI failures or PR comments, update the PR description to reflect the **final** state of the changes, not the initial state.
- **No placeholder PRs:** Only create a PR when implementation and all local verification steps are complete.
