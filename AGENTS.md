<!-- codex-claude-source:start -->
## Claude source alignment

Before working in this scope, read [CLAUDE.md](CLAUDE.md) and the task-relevant rules under [.claude/rules](.claude/rules) when present. Treat these Claude files as read-only instruction sources. Their applicable instructions take precedence over conflicting derived guidance below; preserve compatible Codex-specific guidance. Adapt Claude-specific tool names to available Codex equivalents, and report unsupported capabilities rather than assuming equivalence. Do not copy Claude runtime or credential stores.
<!-- codex-claude-source:end -->

## Git Worktrees

**Worktrees for New Tasks**: Each new task implemented in this repository MUST be developed in a dedicated git worktree. Do not change the current task, but for any subsequent new tasks, create a new worktree first.
