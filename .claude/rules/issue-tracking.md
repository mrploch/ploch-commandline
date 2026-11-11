---
metadata:
  version: "1.0"
---

# Issue Tracking — Linear Is the Primary Tracker (Mandatory)

**Scope:** every repository and folder under the mrploch workspace root (`C:\DevNet\my\mrploch` on Windows, `~/DevNet/mrploch` on macOS). User preference stated on 2026-09-19: *"I want Linear to be the primary source of issues … All of the issues should be Linear."*

**Authority:** this file is the single source of truth for **where issues live and how they are referenced**. Every other rule and skill that mentions an issue — branch naming, commit footers, PR bodies, follow-ups, traceability, response links — defers to it. Where an older rule or skill still says "GitHub issue", read it as "Linear issue" and apply the conventions below.

---

## The Rule

1. **Every issue is a Linear issue.** Create new issues in Linear, never with `gh issue create` or the GitHub MCP `issue_write` tool. This applies to features, bugs, follow-ups, out-of-scope PR findings, tech debt and ideas that have become real work.
2. **Look up existing work in Linear first.** Search Linear before creating anything (`list_issues` with `project` and/or `query`). Never create a duplicate.
3. **Refer to issues by their Linear identifier** (`PLO-140`) everywhere: branch names, commit footers, PR bodies, Notion pages, workspace notes, responses.
4. **GitHub issues are mirrors, not sources.** Many older issues were migrated from GitHub and still carry a synced GitHub issue (visible as a GitHub attachment on the Linear issue and a "synced thread" comment). They remain valid. Keep working through the **Linear** issue and mention the GitHub number only as a secondary reference.

## Where issues go

| Setting | Value |
|---|---|
| Workspace | `ploch` |
| Team | **Ploch** (key `PLO`). All repositories share this one team. |
| Project | The Linear project named after the repository (`ploch-data`, `ploch-common`, `ploch-commandline`, `ploch-ai-configuration`, `mrploch-development`, `ploch-devops`, `ploch-ai-site`, `ploch-ai-tools`, `malwa-site`, …). Look it up with `list_projects`. If a repository has no project, ask the user before creating one. |
| Milestone | The project milestone when the work belongs to a release (for example `Release 4.0`). |
| Labels | Reuse existing labels (`list_issue_labels`). Do not invent new ones. |

**The GitHub twin of a new Linear issue lands in the wrong repository.** Linear's GitHub Issues Sync allows **only one two-way repository per team**, and the Ploch team's two-way repository is **`ploch-ai-tools`**. Every issue created in team Ploch therefore gets a GitHub twin in `mrploch/ploch-ai-tools`, whatever its Linear project. Verified on 2026-09-19: [PLO-576](https://linear.app/ploch/issue/PLO-576) in project `ploch-ai-configuration` was mirrored as `ploch-ai-tools#129`. Consequences:

- **A twin counts only when it is in the same repository as the work.** Legacy migrated issues are usually in the same repository. A new issue's `ploch-ai-tools` twin is not, unless the work is in `ploch-ai-tools`.
- **Never write `Closes #<n>` for a twin from another repository.** GitHub resolves `#<n>` against the PR's own repository, so it would close an unrelated issue.
- **Never create a GitHub twin by hand.** Linear is the record.

## Correcting a twin that landed in the wrong repository

Creating the Linear issue is enough. Do not create a second issue when the GitHub attachment points at `mrploch/ploch-ai-tools` and the work is in another repository.

After `save_issue`, check two things: the Linear **project** is the repository's project, and the GitHub attachment URL, if one exists, is `https://github.com/mrploch/<that-repository>/...`. Use the full URL. A bare `#n` is resolved against whichever repository the reader is in.

When the attachment is in the wrong repository:

- Keep the existing Linear issue. Transferring it between synced repositories preserves its UUID, project and state. [Linear's GitHub documentation](https://linear.app/docs/github) describes that transfer. Do not recreate the issue to get a new GitHub number.
- Do not change the team's two-way sync setting. Which repository is the default two-way repository is an owner decision, not something a session changes.
- Until the attachment is in the repository that holds the code, link the pull request with the Linear reference only — `Closes PLO-<n>` when the PR completes the issue, `Refs PLO-<n>` for partial work. Do not add any `#<n>` reference.

## Referencing issues in code artefacts

Linear's GitHub integration links branches, PRs and commits automatically when they carry the issue identifier. Use that instead of GitHub's `#N` auto-linking.

| Artefact | Convention | Effect |
|---|---|---|
| **Branch** | `<change-type>/plo-<n>-<brief-description>`, e.g. `fix/plo-501-release-restore-credentials` | Linear links the branch and its PR to the issue automatically. Lowercase keeps `branch-naming.md`'s lowercase rule; Linear matches the ID case-insensitively. |
| **Commit footer** | `Refs: PLO-<n>` | Conventional-commit footer carrying Linear's non-closing magic word `refs`. |
| **PR body** | `Closes PLO-<n>` (or `Refs PLO-<n>` for partial work) | Closing magic word. Linear moves the issue to Done when the PR merges. |
| **PR title** | Conventional-commit style. The ID is optional because the branch and body already link it. | — |
| **Synced GitHub twin exists in the same repository** | Mirror the Linear reference: **also** add `Closes #<n>` alongside `Closes PLO-<n>`, or `Refs #<n>` alongside `Refs PLO-<n>` for partial work | Closes the mirror explicitly on completion, and only links it for partial work, so an unfinished issue stays open on both sides. Skip this for a twin in another repository (see above). |

Linear's closing words: `close(s/d)`, `fix(es/ed)`, `resolve(s/d)`, `complete(s/d)`, `implement(s/ed)`. Its non-closing words: `ref(s)`, `references`, `part of`, `contributes to`, `toward(s)`. Use `skip PLO-<n>` / `ignore PLO-<n>` in a PR body to stop an unwanted automatic link.

## Presenting issues to the user

- **Every issue reference is a Linear link:** `[PLO-140](https://linear.app/ploch/issue/PLO-140)`. Prefer the `url` field that the Linear MCP returns.
- When a synced GitHub twin exists, put the Linear link **first** and the GitHub link second: `[PLO-140](…) / [#94](…)`.
- When you only know a GitHub number (from `gh`, a commit, or an old note), **resolve it to the Linear issue before presenting it**. The GitHub issue's Linear attachment and the Linear issue's GitHub attachment give the mapping, and a title search in the repository's Linear project also works. Present a GitHub-only link only when no Linear issue exists. If so, say that, and offer to create one.
- **Status comes from Linear.** A synced GitHub issue's open/closed state can lag behind or differ from the Linear workflow state. Report the Linear state.

## Tooling

- **Use:** the Linear MCP tools (`mcp__plugin_general_linear__*`: `list_issues`, `get_issue`, `save_issue`, `save_comment`, `list_projects`, `list_milestones`, `list_issue_labels`).
- **Do not use for issue tracking:** `gh issue create/edit/close/comment` and the GitHub MCP `issue_write` / `add_issue_comment`. Exception: acting on an existing synced GitHub twin when Linear cannot do it, for example replying to a GitHub-only commenter.
- **Unchanged:** `gh pr …`, `gh run …`, `gh api …` for pull requests, CI and code. Pull requests stay on GitHub.

## Red flags — STOP

- About to run `gh issue create`. Create the issue in Linear instead.
- About to write `Refs: #123` or `Closes #123` with no `PLO-` identifier alongside.
- About to name a branch `feature/123-…` after a GitHub issue number.
- About to present an issue list containing bare GitHub numbers, or GitHub links without their Linear issue.
- About to report an issue's status from GitHub when a Linear issue exists.
