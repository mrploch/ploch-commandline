# Work Traceability — Mandatory Tracking Chain

**Scope:** Every substantive unit of work in any session running under the mrploch workspace root (for example `C:\DevNet\my\mrploch` on Windows or `~/devnet/my/mrploch` on macOS) or any sub-folder/repository. "Substantive" means the same bar as the Notion auto-log rule: implementing, fixing, investigating, designing, or configuring — not conceptual Q&A.

## The Rule

Before starting implementation of any substantive unit of work, ensure the **full traceability chain** exists. Create any missing link, in this order:

1. **Linear issue**, in the Linear project named after the repository (team Ploch). See [`issue-tracking.md`](./issue-tracking.md). Search Linear for an existing issue first (`list_issues` with `project` + `query`) and reuse it if one covers the work. **Never create a duplicate**, and never create GitHub issues. The Linear issue is the canonical work definition. Commits reference it (`Refs: PLO-<n>`); the PR that completes it closes it (`Closes PLO-<n>`), and a PR that delivers an agreed part of it only links it (`Refs PLO-<n>`, see [`pr-descriptions.md`](./pr-descriptions.md)).
2. **Notion task** — in the **Personal Dev Tasks** DB (data source `collection://215a5394-d58b-81b5-b167-000b544334f4`, under Software Development → Personal Dev TODOs). Populate:
   - `Name` — imperative title including repo and Linear ID (e.g. "Implement ploch-common PLO-257: …").
   - `Type` = `Issue` (or `TODO` when there is no issue), `Active State` = `Current`, `Status` = `In progress` (→ `Done` when merged), `Priority Level` as appropriate.
   - `GitHub Issue` (url property): only when the Linear issue has a synced GitHub twin **in the same repository as the work** (see [`issue-tracking.md`](./issue-tracking.md)). The Linear URL always goes in the body.
   - `Main Project` — the matching Personal Projects entry (see the lookup table in the workspace `CLAUDE.md`).
   - Body: a **Traceability** section linking the Linear issue, the originating context (PR/review/conversation), and the Daily Notes entry.
3. **Personal Dev Daily Notes entry** — per the existing Notion auto-log rule (workspace `CLAUDE.md`). Set its **`Tasks`** relation to the Notion task (the Daily Notes database has a `Tasks` relation to Personal Dev Tasks). Also cross-link by URL: the entry's body links the Notion task and the Linear issue, and the task body links back to the entry.
4. **Workspace note** — per `.claude/rules/notes-keeping.md` (`notes/<repo>/<date>-<slug>.md`), linking the issue and PR.

## Maintenance during the work

- Update the Notion task's **Status log** (append-only) and `Status` property at the same milestones that trigger Daily Notes / workspace-note updates (PR created, CI green, decision made, merged).
- When the PR that **completes** the issue merges (`Closes PLO-<n>`) **and the Linear issue is actually Done** (check it: Linear waits for every linked PR, so an open sibling PR keeps it open): task `Status` = `Done`, Daily Notes entry `Status` = `Done` with Outcome filled, workspace note moved/updated per notes-keeping rule. The Linear issue closes via that `Closes PLO-<n>`, and a same-repository GitHub twin closes with it.
- When a partial PR (`Refs PLO-<n>`) merges, record it in the status log only: the task, the Daily Notes entry and the Linear issue stay open until the remaining criteria land.
- **Follow-ups and scope changes:** file follow-up Linear issues (never "track it later") and create Notion sub-tasks (`Parent Task` relation) when they represent real planned work.
- **Ordering:** create the chain **before** implementation starts. An unlinked PR or an untracked task is a traceability defect, the same severity as a missing `Refs:` footer.

## Why

The user requires full traceability of all work: Linear issue (what/why) → PR (how) → Notion task (tracking/status) → Daily Notes (session journal) → workspace note (resume script). Linear is the source of truth for issues, GitHub for code state, the Notion task DB for planned and active work, and the daily notes for the session audit trail. A unit of work missing any link in this chain is an error — backfill immediately when noticed.

This is the single authoritative traceability rule. `traceability.md` only points here.
