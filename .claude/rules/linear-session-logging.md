---
metadata:
  version: "1.0"
---

# Linear Issues Record Their Sessions (Mandatory)

**Scope:** every session running under the mrploch workspace root (for example `C:\DevNet\my\mrploch` on Windows or `~/devnet/my/mrploch` on macOS) **and every folder and repository beneath it**, plus any session that touches a Linear issue from anywhere else on this machine. This is a user preference, stated on 2026-09-20.

**Authority:** this file is the single source of truth for **recording which Claude Code session worked on a Linear issue**. It applies to the [`implement-issue`](../skills/implement-issue/SKILL.md) skill, but is **not limited to it** — it applies equally to ad-hoc work, investigation and one-off questions that touch an issue. Where a skill describes its own issue workflow, this rule is an additional, non-optional step in it.

---

## The Rule

**Whenever you work on a Linear issue, post a comment on that issue naming the Claude Code session and the working folder.** Post it once per session per issue, as the first action after the issue is identified — before the investigation, before the branch, before the first file is read.

"Work on" is deliberately broad. It includes being asked to:

- implement, fix, or complete the issue (`/implement-issue` or a plain request);
- look at, investigate, triage, reproduce, or estimate it;
- review, comment on, re-scope, split, or close it;
- read it in order to do work elsewhere that the issue describes.

It does **not** apply to a delegated sub-agent or an external reviewer (Codex, Gemini, Copilot, Antigravity) run on the main session's behalf: the main session posts the one comment. Nor does it include merely listing issues (`list_issues`), resolving a GitHub number to its Linear identifier, or mentioning an issue in passing while working on something else. The test is simple: **if the session spends effort on the issue's subject matter, the issue gets a comment.**

## The Comment

One comment, this shape:

```markdown
🤖 **Claude Code session**

- **Session:** `4aca8c26-d547-464d-80a4-f6b3f8715b9a`
- **Working folder:** `C:\DevNet\my\mrploch\ploch-common`   <!-- on macOS: `/Users/<you>/DevNet/mrploch/ploch-common` -->
- **Started:** 2026-09-20 21:14 CEST
- **Doing:** Investigating the duplicate-entity race in the upsert path.
```

Field rules:

- **Session** — the **Claude Code** conversation/session UUID. This is *not* the ContextStream `session_id`, and not a Notion page id; see [Finding the session ID](#finding-the-session-id).
- **Working folder** — the **absolute path** Claude Code is operating in, not just the leaf name. This field exists because work on an issue is often done from a folder other than the issue's own repository — from the workspace root, from a sibling repo, or from a git worktree. The absolute path answers "where did this happen?" unambiguously; a bare folder name does not distinguish `ploch-common` from a worktree of it.
- **Started** — local time the session began work on this issue.
- **Doing** — one line on what this session is there to do. Keep it to a sentence; the issue's own description carries the detail.

### One comment per session per issue

Do not post a second identical comment when the same session returns to the same issue later in the conversation. Do post a new comment when:

- a **different** session (a new conversation, a resume with a new id) works the issue;
- the **working folder changes** mid-session — for example moving from the workspace root into the repo, or into a worktree. Post a short follow-up naming the new folder rather than editing the original.

Updating the existing comment is acceptable in place of a follow-up when only the **Doing** line has changed.

## Finding the session ID

In order of preference:

1. **The `[NOTION_AUTO_LOG]` system reminder** injected at session start states it directly: *"This session's Claude Code conversation id is: …"*. This is the same value, and it is the cheapest source. The reminder also names this session's state file, `.claude/.notion-tasks/<session-id>.json`.
2. **Never the retired shared file `.claude/.notion-current-task.json`.** The hook no longer writes or reads it, so a leftover copy names an older or concurrent session.
3. **The transcript filename, as a last resort.** The session is usually the **most-recently-modified `.jsonl`** in the project's transcript directory under `~/.claude/projects/`. The directory name is the working folder path with **every character other than an ASCII letter or digit** replaced by `-` (so `:`, `\`, `/`, `.` and `_` all become `-`): `C:\DevNet\my\mrploch\ploch-common` becomes `C--DevNet-my-mrploch-ploch-common` on Windows, and `/Users/<you>/DevNet/mrploch/ploch-common` becomes `-Users-<you>-DevNet-mrploch-ploch-common` on macOS. The UUID is the filename without its extension. Another session in the same folder can be newer, so confirm the transcript holds this conversation (for example, grep it for a phrase from the current task) before using it; if you cannot confirm it, post `Session: unknown`.

```bash
# Run from the working folder. Git Bash needs `pwd -W` for the native C:/... form; macOS uses plain pwd.
ls -t "$HOME/.claude/projects/$( (pwd -W 2>/dev/null || pwd) | sed -E 's#[^A-Za-z0-9]#-#g')/"*.jsonl | head -1
```

### If the session ID genuinely cannot be determined

**Post the comment anyway.** A comment naming the folder and the time is far more useful than no comment at all. Write `Session: unknown` and say why in one clause:

```markdown
🤖 **Claude Code session**

- **Session:** unknown (no transcript directory for this working folder)
- **Working folder:** `C:\DevNet\my\mrploch`   <!-- on macOS: `/Users/<you>/DevNet/mrploch` -->
- **Started:** 2026-09-20 21:14 CEST
- **Doing:** Triaging the failing release workflow.
```

Never silently skip the comment because the id was inconvenient to find.

## Tooling

Use the Linear MCP, per [`issue-tracking.md`](./issue-tracking.md):

```
save_comment(issueId: "PLO-140", body: "…")
```

Do **not** post this to a synced GitHub twin instead. The Linear issue is the record; the twin is a mirror. If the Linear MCP is unavailable, say so explicitly in the response and offer to post the comment once it is back — do not treat an unavailable tool as permission to skip the step.

## Relationship to other rules

- [`issue-tracking.md`](./issue-tracking.md) establishes that **Linear is the primary tracker**. This rule adds a session record to the issues that rule governs.
- [`traceability.md`](./traceability.md) and [`work-traceability.md`](./work-traceability.md) require the Linear issue ↔ Notion task ↔ daily-note chain. This comment is the **fourth** link: it ties the issue back to the specific conversation that worked it, which is what makes a transcript recoverable months later.
- [`response-links.md`](./response-links.md) applies when reporting back: the issue is presented as a Linear link, never a bare identifier.
- The comment is **not** a substitute for the Notion daily-note entry or the workspace note. It is a breadcrumb on the issue itself, for someone reading the issue rather than the journal.

## Red flags — STOP

- About to start investigating a Linear issue without having posted the comment.
- About to post the comment with a folder name but no absolute path.
- About to skip the comment because "this is only a quick look" — a quick look is work on the issue.
- About to skip the comment because the session ID was hard to find — post it with `Session: unknown` instead.
- About to post the ContextStream `session_id` or a Notion page id in the **Session** field.
- About to comment on the synced GitHub issue instead of the Linear issue.
- About to post a second identical comment for the same session and the same folder.
