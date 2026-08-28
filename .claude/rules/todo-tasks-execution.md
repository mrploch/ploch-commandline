# Performing Tasks from TODO.md

**Skill:** Use `/execute-todo` to run the full workflow. See `~/.claude/skills/execute-todo/SKILL.md`.

## Principles

These principles guide TODO task execution. The skill handles the workflow; these rules explain *why*.

- **Autonomous execution** — research before asking. Use web search, sibling repos, docs to resolve uncertainties. Only ask the user when truly blocked.
- **End-to-end quality** — every task must build, pass tests, pass static analysis, and survive self-review before committing.
- **Zero new warnings** — treat analyser output (StyleCop, Roslynator, SonarAnalyzer) as requirements, not suggestions.
- **Comprehensive tests** — coding tasks require unit tests (xUnit v3, FluentAssertions, AutoFixture) per the .NET testing rules.
- **Conventional Commits** — one commit per task, following `commits.md` rules.
- **PR check gate** — when pushing, wait for all CI checks to pass. Resolve failures and PR comments before marking complete.
- **Parallel where possible** — independent tasks should be dispatched to parallel agents.
- **Non-blocking issues** — record out-of-scope questions, suggestions, and follow-ups as GitHub issues (label genuinely high-priority ones `important`), not in a `TODO-important.md` file. Only ask the user if truly blocking.
- **For common libraries** (Ploch.Common, Ploch.Data, Ploch.Web, etc.) — provide code documentation and README files.