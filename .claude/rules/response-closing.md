---
metadata:
  version: "1.0"
---

# Closing Every Response — Status + Next Steps (Mandatory)

**Scope:** every session and every substantive response in the mrploch workspace root (`<workspace>`, the `workspaceRoot` in `environment.json`; for example `C:\DevNet\my\mrploch` on Windows or `~/DevNet/mrploch` on macOS) **and every folder and repository beneath it**, wherever that root lives on the machine. This is a user preference, stated explicitly on 2026-08-26.

## The Rule

End every substantive response with a short closing block, in this exact shape:

```markdown
---
**Where we are:** <2-4 sentences. What state the work is in right now, and what just changed.>

**Next:**
- <actionable item — a concrete thing that can be done, phrased as an action>
- <...>
```

Rules for the block:

- **Brief.** The "Where we are" is context, not a recap of the conversation. Two to four sentences. If nothing material changed, say so in one line.
- **Actionable items only.** Each bullet is a thing someone can *do* — "Fix the `.cmd` spawn in the gemini-cli MCP config", not "the MCP server is broken". No status observations in the Next list; those belong in "Where we are".
- **Ordered by what should happen first.** Put blockers and decisions-needed at the top.
- **Flag who acts** when it is not you: prefix with `(you)` for items only the user can do — interactive logins, approvals, credentials, decisions.
- **Empty is allowed.** If there genuinely is no next step, write `**Next:** nothing outstanding.` Do not invent filler work.
- **No duplication.** Do not repeat the body of the response in the closing block. It is a handover, not a summary of what was just said.

## When to skip

- Pure conceptual Q&A with no work state ("what does `IUnitOfWork` do?").
- Greetings, acknowledgements, one-line factual answers.
- Mid-task progress messages where the task is continuing in the same response.
- Responses whose format is fixed by the request — raw JSON, a file's exact contents, a commit message, or any output another tool will parse. Appending the block would make that output invalid.

## Relationship to other rules

- **Not** the same as [`summaries.md`](./summaries.md). That rule governs *report documents* (pipeline status, investigation findings) which get written to `temp/` and opened. This rule is about the inline conversational close and **must not** trigger writing a file.
- Complements [`notes-keeping.md`](./notes-keeping.md) and the Notion daily-notes auto-log: those are the durable audit trail, this is the in-conversation handover. The "Next" bullets should be consistent with the note's "Outstanding / next steps" section.
