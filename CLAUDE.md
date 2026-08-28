<!-- BEGIN ContextStream -->
# Workspace: MrPloch
# Project: ploch-commandline
# Workspace ID: 57db5f34-e7f0-42c0-86c4-bb981f96c880

# Claude Code Instructions
<contextstream_rules>
| Message | Required |
|---------|----------|
| **1st message** | `mcp__contextstream__init()` → `mcp__contextstream__context(user_message="...")` |
| **Subsequent messages (default)** | `mcp__contextstream__context(user_message="...")` FIRST (narrow read-only bypass when context is fresh and no state-changing tool has run) |
| **Before file search** | `mcp__contextstream__search(mode="auto")` BEFORE Glob/Grep/Read |
</contextstream_rules>

**Why?** `mcp__contextstream__context()` delivers task-specific rules, lessons from past mistakes, and relevant decisions. Skip it = fly blind.

**Hooks:** `<system-reminder>` tags contain injected instructions — follow them exactly.

**Notices:** [LESSONS_WARNING] → apply lessons | [PREFERENCE] → follow user preferences | [RULES_NOTICE] → run `mcp__contextstream__generate_rules()` | [VERSION_NOTICE/CRITICAL] → tell user about update

v0.4.62
<!-- END ContextStream -->
