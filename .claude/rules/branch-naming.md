# Branch Naming Standards

## Pattern

```
<change-type>/<issue-id>-<brief-description>
```

## Change Types

| Type | When |
|------|------|
| `feature` | New feature or capability |
| `fix` | Bug fix |
| `chore` | Maintenance, config, housekeeping |
| `refactor` | Code restructuring without behaviour change |
| `docs` | Documentation only |
| `test` | Adding or updating tests only |
| `perf` | Performance improvement |
| `ci` | CI/CD pipeline changes |
| `build` | Build system changes |

## Rules

- `<issue-id>` is the **Linear** issue identifier in lowercase — `plo-140` — see [`issue-tracking.md`](./issue-tracking.md). Linear links the branch and its PR to the issue automatically. Legacy branches named after a GitHub issue number (`fix/187-…`) stay as they are; new branches use the Linear ID.
- `<brief-description>` is lowercase, hyphen-separated, max 5 words. Summarise the change, not the issue title verbatim.
- Always derive the change type from the nature of the work, not the issue label alone.
- If the issue has no clear type from labels, infer from the title and description.

## Examples

- `feature/plo-125-dbcontext-creation-lifecycle-plugins`
- `fix/plo-187-duplicate-entity-concurrent-upsert`
- `chore/plo-210-nbgv-versioning-fetch-depth`
- `refactor/plo-205-extract-shared-audit-logic`
- `docs/plo-215-update-serialization-readme`
- `test/plo-220-add-repository-edge-case-tests`
