# Work Traceability — Mandatory Tracking Chain

**Scope:** Every substantive unit of work in any session running under `C:\DevNet\my\mrploch` or any sub-folder/repository. "Substantive" means the same bar as the Notion auto-log rule: implementing, fixing, investigating, designing, or configuring — not conceptual Q&A.

## The Rule

Before starting implementation of any substantive unit of work, ensure the **full traceability chain** exists. Create any missing link, in this order:

1. **GitHub issue** — in the repository the work belongs to. Search for an existing issue first (`gh issue list --search`); reuse it if one covers the work — **never create a duplicate**. The issue is the canonical work definition. Commits reference it (`Refs: #N`) and the PR closes it (`Closes #N`).
2. **Notion task** — in the **Personal Dev Tasks** DB (data source `collection://215a5394-d58b-81b5-b167-000b544334f4`, under Software Development → Personal Dev TODOs). Populate:
   - `Name` — imperative title including repo and issue number (e.g. "Implement ploch-common #257: …").
   - `Type` = `Issue` (or the closest fit), `Active State` = `Current`, `Status` = `In progress` (→ `Done` when merged).
   - `GitHub Issue` (url property) — the issue URL.
   - `Main Project` — the matching Personal Projects entry (see the lookup table in the workspace `CLAUDE.md`).
   - Body: a **Traceability** section linking the GitHub issue, the originating context (PR/review/conversation), and the Daily Notes entry.
3. **Personal Dev Daily Notes entry** — per the existing Notion auto-log rule (workspace `CLAUDE.md`). Its body must link the Notion task and the GitHub issue; the task body links back to the Daily Notes entry. (The two DBs have no relation property — cross-link via URLs in both page bodies.)
4. **Workspace note** — per `.claude/rules/notes-keeping.md` (`notes/<repo>/<date>-<slug>.md`), linking the issue and PR.

## Maintenance during the work

- Update the Notion task's **Status log** (append-only) and `Status` property at the same milestones that trigger Daily Notes / workspace-note updates (PR created, CI green, decision made, merged).
- When the PR merges: task `Status` = `Done`, Daily Notes entry `Status` = `Done` with Outcome filled, workspace note moved/updated per notes-keeping rule.

## Why

The user requires full traceability of all work: GitHub issue (what/why) → PR (how) → Notion task (tracking/status) → Daily Notes (session journal) → workspace note (resume script). A unit of work missing any link in this chain is an error — backfill immediately when noticed.
