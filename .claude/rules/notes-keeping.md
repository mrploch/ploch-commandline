# Workspace Notes — Auto-Maintenance Rule

The repository at `C:\DevNet\my\mrploch\notes` is a journal of all work in the MrPloch organisation. It exists so the user can resume a task days or weeks later with full context. **You are responsible for keeping it current.**

See [`notes/README.md`](../../notes/README.md) for the layout and [`notes/_template.md`](../../notes/_template.md) for the canonical task-note structure.

## When to update

You **must** update the relevant note file at these moments. Treat each as a hard trigger, not a suggestion.

### Session-end triggers

1. **Before producing your final summary at the end of a session** — the user is about to close the conversation. Update or create the note for whatever task was worked on. This is the most important trigger; never skip it for substantive work.
2. **Before context auto-compaction** — if you sense context approaching the limit (long conversations, many tool results), checkpoint the active task's note so the next session can pick up.

### Milestone triggers (during a session)

Update the relevant note immediately after any of:

3. **A pull request is created or its description is materially updated** — record the PR number and link.
4. **All CI checks go green** on a tracked PR (the four-condition gate from `pr-checks-completion-gate.md` is satisfied) — log it under "Actions taken" and update status if appropriate.
5. **A material decision is made** — anything you'd write `Decided X over Y because Z` for. Includes rejecting an approach, picking a library, choosing scope.
6. **A branch is created, switched, or deleted** in any tracked repo as part of starting/ending a unit of work.
7. **A task transitions** between active / blocked / parked / done. Update the `Status` field; if `done`, move the file to `<repo>/archive/` and write the `Outcome` section.
8. **An idea is mentioned in passing** that's not yet a real task — drop a file in `notes/ideas/` (or update an existing one). See `ideas/_README.md` for the shape.

### Scratch space (`notes/temp/`)

`notes/temp/` is for throwaway working material — quick scribbles, intermediate analysis, command output you might want to glance at later. Use it freely when something doesn't fit a task note yet. Do **not** link to files in `temp/` from elsewhere; they're disposable. If a temp file turns out to be valuable, move it into the proper folder (`<repo>/`, `general/`, or `ideas/`) and reshape it into a task note.

### What you do NOT need to record

- Trivial commits (formatting, typo fixes, comment edits) — the commit log is enough.
- Routine `dotnet build`, `dotnet test`, `gh pr checks` polling output — these are noise.
- Tool exploration or read-only research that didn't change anything.
- ContextStream calls or other internal MCP plumbing.

## How to update

### Identify the right note

For a known task: find the existing file by branch name, PR number, or short slug. The path is `notes/<repo>/<YYYY-MM-DD>-<slug>.md`.

For new work: create the file from `notes/_template.md`. Filename uses **today's date** as the start date and a short slug (PR number + topic, or issue number + topic, or just topic).

If the work spans multiple repos, put the note under `general/`.

### Find the current Claude Code session ID

The active session UUID is the **most-recently-modified `.jsonl` filename** in:

```
C:\Users\krzys\.claude\projects\C--DevNet-my-mrploch\
```

Read the directory listing and pick the newest. Add it to the note's `Claude Code sessions` field (comma-separate if the note already has prior session UUIDs — don't replace them).

### Edit, don't rewrite

Use `Edit` on existing notes. Append to "Actions taken" in **reverse-chronological order** (newest at the top of that section's list). Update `Last updated` and `Status` if changed. Don't rewrite history; the log is the value.

### Decisions belong in the note, not just in commits

When you record a decision under "Decisions", capture:

- The date.
- What was chosen.
- Why (the constraint or trade-off that drove the choice).
- What was rejected and why.

A reviewer should be able to read the Decisions section alone and understand *why* the work looks the way it does.

## Cross-references

- The note's `Last updated` date should match the date of the most recent entry.
- When you create a PR, link it from the note. When you close/merge a PR, update the note status.
- When an `ideas/` entry matures into real work, move the file into the relevant repo folder, rename it with today's date, and link it from the new note's "Related" section.

## Coordination with other rules

- This rule complements `agent.md` (the agent workflow) — note-updates are part of the post-code workflow, not a replacement for testing/verification.
- This rule complements `pr-checks-completion-gate.md` — when the four-condition gate is satisfied, that's a milestone trigger here too.
- Decisions captured here are also fair game for ContextStream `mcp__contextstream__session(action="capture", event_type="decision", ...)` if they have cross-session value, but the file in `notes/` is the canonical, browseable record.

## When in doubt

If you're unsure whether something rises to "milestone", err on the side of recording it. The cost of an extra paragraph in a markdown file is far lower than the cost of the user returning in two weeks and wondering what state they were in.
