# Agent configuration — ploch-commandline

| Field | Value |
|---|---|
| Last verified | 2026-10-05, against commit `6a2c931` |
| Verified by | Listing and measuring every committed agent file; comparing with a live Claude Code session |
| Index | [`CLAUDE.md`](../../CLAUDE.md) |

Sizes are bytes of committed files; tokens are estimated as bytes divided by four. Section 6 is a
list of **proposals**. Nothing in it has been applied.

## 1. Scope and layers

An agent working here receives configuration from three layers. Only the first is in this
repository.

| Layer | What it holds | Maintained in |
|---|---|---|
| Repository | Instruction files, rules, skills and editor configuration committed here | This repository |
| Workspace | A parent folder shared by the organisation's repositories: its own instruction file, rule set, skills and MCP servers | The organisation's configuration repository |
| User | Plugins, MCP servers, hooks and settings of the person running the agent | That person's machine |

The repository copy of rules and skills is a mirror of the workspace set. Both load when the
repository is checked out inside the workspace.

## 2. Committed in this repository

| Path | Files | Size | Read by | Verdict |
|---|---|---|---|---|
| `CLAUDE.md` | 1 | 5 KB | Claude Code, every session | Keep; replaced by this work |
| `AGENTS.md` | 1 | 32 KB | Codex | Replace: ContextStream instructions only, no repository knowledge |
| `GEMINI.md` | 1 | 31 KB | Gemini, Antigravity | Replace: near-copy of `AGENTS.md` |
| `.cursorrules` | 1 | 1 KB | Cursor | Remove: ContextStream block |
| `.contextstream/config.json` | 1 | under 1 KB | ContextStream | Remove with ContextStream |
| `.cursor/mcp.json` | 1 | 1 KB | Cursor | Remove or replace: defines only ContextStream |
| `.claude/rules/` | 27 | 154 KB | Claude Code, all of it, every session | Reduce; see section 3 |
| `.claude/skills/` | 9 | 228 KB | Claude Code, on demand | Keep 5 of 6 skills |
| `.claude/mrploch-dev/` | 2 | 35 KB | Nothing; unregistered plugin | Remove |
| `.agents/` | 12 | 183 KB | Codex, Antigravity | Regenerate from one source or remove |
| `.cursor/rules/` | 26 | 137 KB | Cursor | Stale mirror; regenerate or remove |
| `.cursor/skills/` | 23 | 240 KB | Cursor | Remove the 11 WinUI skills; regenerate the rest |

Together that is about 1 MB of agent configuration in a repository with 74 source files.

Not committed: no `.claude/settings.json`, so there is no shared permission allow-list, no hook
and no committed MCP server list for Claude Code.

## 3. Rules

All 27 files in `.claude/rules/` load unconditionally — about 38,600 tokens per session. None has
path scoping.

| Relevance here | Rules | Size |
|---|---|---|
| Not applicable: no database, web API, deployed site or setup scripts | `data-access`, `data-project`, `data-provider-project`, `domain-model`, `qa`, `local-setup-scripts`, `rules` | 42 KB |
| About a tenth applies (library families, test projects, casing) | `project-naming` | 32 KB |
| Partly applies | `project-structure`, `sample-apps`, `dependencies` | 10 KB |
| Applies | `agent`, `branch-naming`, `code-quality`, `commits`, `documentation`, `external-ai-review`, `naming`, `notes-keeping`, `pr-checks-completion-gate`, `pr-descriptions`, `summaries`, `todo-tasks-execution`, `traceability`, `work-traceability`, `worktrees`, `writing-dotnet-tests` | 70 KB |

Problems in the committed rules:

- **They describe the old tracker.** `commits.md` and `branch-naming.md` require
  `Refs: #<issue-number>` and GitHub issue numbers. Practice, and the workspace copies, use Linear
  (`PLO-<n>`). No committed rule mentions Linear.
- **`rules.md` describes a different system:** `.cursor/rules/*.mdc` as the source of truth and an
  `ai-rules install` step that does not exist here.
- **Dead tool references.** `external-ai-review.md` and several skills call MCP tools for Codex and
  Gemini reviewers that are not configured. `todo-tasks-execution.md` points at a skill that does
  not exist.
- **ContextStream** is referenced in ten committed files while being retired.
- **Machine-specific paths** (a Windows drive root) appear in several rules.

## 4. Skills and tools

Committed skills, and whether this repository uses them:

| Skill | Copies | Used here |
|---|---|---|
| `implement-issue` | `.claude`, `.agents`, `.cursor` | Yes |
| `dev-finishing-touches`, `dotnet-dev-finishing-touches` | `.claude`, partly `.agents`, `.cursor` | Yes |
| `dotnet-dev-practical` | all three | Yes |
| `docfx-api-docs` | `.claude`, `.cursor` | Yes; the repository has a DocFX site |
| `commit`, `pr`, `review-pr`, `review-pr-comments`, `qa-explore` | `.agents` only | Yes, but overlap built-in commands |
| `prompt-lookup` | all three | No |
| 11 `winui3-*` skills | `.cursor` only | No; there is no user interface |

What the 2026-10-05 review actually needed, as a guide to what earns its place:

| Needed | Not needed for this repository |
|---|---|
| File reading and search, `git`, `gh`, the `dotnet` CLI | EF Core, WinUI, web API and UI skills |
| Linear and Notion access for the tracking chain | Browser automation |
| SonarCloud and Codacy access at pull-request time | Unrelated service integrations |
| Microsoft Learn and NuGet lookups | Document-format and research skills |

The IDE, Roslyn and build-log MCP servers were available and not required: reading the code and
running the build answered every question. They are worth keeping for refactoring and build
diagnosis, not for routine work.

## 5. Findings

Measured in a live session on the maintainer's machine, an agent starts with roughly 125,000 tokens
of context before the first prompt, about 96,000 of them rules.

| # | Finding | Cost or risk |
|---|---|---|
| 1 | The rule set loads twice: repository copy and workspace copy | About 46,000 tokens per session |
| 2 | Eight rule pairs differ between the copies, and both versions load | Contradictory instructions on reviewers, follow-up issues and tracking |
| 3 | Rules for projects this repository does not have | About 10,600 tokens per loaded copy |
| 4 | Committed rules name the wrong tracker | Wrong commit footers and branch names |
| 5 | Dead references to reviewer tools and a missing skill | Agents stall or improvise |
| 6 | `AGENTS.md` and `GEMINI.md` carry no repository knowledge | Other agents start blind; 63 KB of noise |
| 7 | Three mirrors of rules and skills, each drifted differently | 560 KB to keep in step by hand |
| 8 | No committed permission allow-list | Every `dotnet` and `gh` call prompts, or the user allows everything |
| 9 | Skills for other project types are committed here | Larger skill listing; wrong suggestions |
| 10 | An unregistered plugin folder is committed | 35 KB nobody loads |
| 11 | Session-start hooks of several user plugins inject instructions that conflict with the repository's autonomous workflow | Mixed signals at the start of every session |
| 12 | Three MCP routes to SonarQube, two to Linear | Longer tool list; unclear which to call |

## 6. Proposed changes

Ordered by value. "Where" names the layer that has to change.

### Priority 1 — correctness and the largest savings

| # | Change | Where | Effect |
|---|---|---|---|
| 1 | Keep one copy of the shared rules. Remove the shared rules from this repository and keep only repository-specific ones (`worktrees.md`) | Repository | Halves rule context; ends contradictions |
| 2 | Reconcile the eight differing rule pairs to one version before removing a copy | Workspace | One answer per question |
| 3 | Scope project-type rules with `paths:` front matter, or turn them into on-demand skills: the three `data-*` rules, `domain-model`, `qa`, `local-setup-scripts` | Workspace | About 10,600 tokens saved per loaded copy |
| 4 | Split `project-naming` into a short always-on core and a reference loaded on demand | Workspace | About 7,000 tokens saved |
| 5 | Replace `AGENTS.md` and `GEMINI.md` with a few lines pointing at `CLAUDE.md` and `docs/ai/` | Repository | Every agent gets the same knowledge base |
| 6 | Remove ContextStream leftovers: `.cursorrules`, `.contextstream/`, the server in `.cursor/mcp.json` | Repository | No instructions for a retired tool |
| 7 | Fix or remove the dead reviewer-tool references in `external-ai-review` and the skills that use it | Workspace | Review skills work as written |

### Priority 2 — safety and day-to-day speed

| # | Change | Where | Effect |
|---|---|---|---|
| 8 | Commit `.claude/settings.json` with an allow-list for `dotnet build`, `test`, `restore`, `format`, `msbuild`, read-only `git`, `gh pr`, `gh run`, `gh api`, and a deny-list for credential files and force pushes | Repository | Fewer prompts without allowing everything |
| 9 | Add one verification script that runs what CI runs: the stable-version guard, build, tests, the sample in project-reference mode and DocFX | Repository | One command before a pull request |
| 10 | Add a test project that compiles the documentation snippets | Repository | Stops the drift behind PLO-620 |
| 11 | Require the `build` check in the `main` ruleset | GitHub | A red build cannot merge |
| 12 | Update the pull request template: Linear reference, change-log entry, breaking change, documentation updated | Repository | Template matches the process |
| 13 | Delete `.claude/mrploch-dev/`, `prompt-lookup` and the WinUI skills from this repository | Repository | About 110 KB less; shorter skill list |
| 14 | Generate `.agents/` and `.cursor/` from one source, or stop committing them | Workspace | No hand-maintained mirrors |

### Priority 3 — tidy-up

| # | Change | Where | Effect |
|---|---|---|---|
| 15 | Apply a per-repository capability profile that switches off plugins, skills and MCP servers this repository never uses | Workspace, user | Shorter listings |
| 16 | Keep one SonarQube route and one Linear route | Workspace, user | Clearer tool choice |
| 17 | Turn off session-start injections that contradict the repository workflow | User | Consistent instructions |
| 18 | Replace machine-specific paths in rules with derived ones | Workspace | Rules work on every machine |
| 19 | Add cancel-in-progress concurrency to the pull-request workflows | Repository | Faster feedback |

## 7. Decisions needed

1. **Which layer owns the shared rules?** Proposal 1 assumes the workspace does and this
   repository keeps only what is specific to it. The alternative — the repository owns a full
   copy — keeps it self-contained for outside contributors at the cost of drift.
2. **Is `CLAUDE.md` or `AGENTS.md` the canonical entry point?** This work makes `CLAUDE.md`
   canonical and proposes that the others point at it.
3. **Should agent configuration for other tools be committed at all** in a public library
   repository, or generated locally?
