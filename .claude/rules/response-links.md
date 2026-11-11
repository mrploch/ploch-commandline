---
metadata:
  version: "1.0"
---

# Linkable References Are Always Clickable Links (Mandatory)

**Scope:** every response in the mrploch workspace root (for example `C:\DevNet\my\mrploch` on Windows or `~/devnet/my/mrploch` on macOS) **and every folder and repository beneath it**. This is a user preference, stated on 2026-09-12 after it had been asked for before and applied inconsistently.

## The Rule

**Every reference to something that has a URL is rendered as a clickable markdown link.** Never a bare number, never a bare identifier, never a naked URL.

This applies **everywhere in the response** — prose, bullet lists, tables, headings, the closing block from [`response-closing.md`](./response-closing.md), and commit/PR bodies where markdown is supported.

| Reference | Wrong | Right |
|---|---|---|
| **Issue (default: Linear)** | `PLO-315`, `#46` | `[PLO-315](https://linear.app/ploch/issue/PLO-315)`, the Linear issue, never the bare GitHub number. See [`issue-tracking.md`](./issue-tracking.md). |
| PR in the current repo | `PR #52` | `[PR #52](https://github.com/mrploch/<repo>/pull/52)` |
| GitHub-only issue (no Linear issue exists) | `mrploch-development#21` | `[mrploch-development#21](https://github.com/mrploch/mrploch-development/issues/21)`. Say that no Linear issue exists, and offer to create one. |
| Commit | `4a7abe8` | `[4a7abe8](https://github.com/mrploch/<repo>/commit/4a7abe8)` |
| CI run / job | `run 34693716416` | `[run 34693716416](https://github.com/mrploch/<repo>/actions/runs/34693716416)` |
| Review thread / comment | "the CodeRabbit thread" | `[the CodeRabbit thread](https://github.com/mrploch/<repo>/pull/52#discussion_r3996195456)` |
| Notion page | "the daily note" | `[the daily note](https://www.notion.so/<page-id>)` |
| Linear project | "the ploch-commandline project" | `[@kploch ploch-commandline](https://linear.app/ploch/project/<project-slug>)` |
| Linear issue with a synced GitHub twin | `PLO-315 / #32` | `[PLO-315](https://linear.app/ploch/issue/PLO-315) / [#32](https://github.com/mrploch/<repo>/issues/32)` |
| Bare URL in prose | `https://sonarcloud.io/...` | `[SonarCloud quality gate](https://sonarcloud.io/...)` |
| External doc | "the NuGet source-mapping docs" | `[NuGet source-mapping docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping)` |

### Details that matter

- **GitHub auto-links `#N` only inside GitHub itself.** In a terminal response `#46` is inert text, which is precisely why this rule exists. PR bodies and commit messages keep bare magic-word references (`Closes PLO-46`, `Refs: PLO-46`, and `Closes #46` for a synced twin), because Linear and GitHub parse them there — but a *response to the user* never does.
- **Issues and pull requests take different paths** — `/issues/N` and `/pull/N`. GitHub redirects between them, but link to the right one.
- **Cross-repo references always carry the repo name** in the link text, so the reader can see it is not the current repository.
- **Prefer descriptive link text over a raw identifier** when the identifier alone is opaque: `[run 34693716416](...)` is acceptable, `[the failing build](...)` is better when there is one obvious referent.
- **Repeated references stay linked.** Do not link the first mention and leave later ones bare — the user may be reading only the closing block or one table row.
- **Tables included.** A table cell listing issue numbers links each one.
- **Linear is the primary issue identity.** Issues are presented by their Linear ID and link. When a source (`gh`, a commit footer, an old note) only gives a GitHub number, resolve it to the Linear issue before presenting it. The Linear project's issue list and the GitHub attachment on each Linear issue give the mapping. Report the **Linear** status, which can differ from the synced GitHub issue's open/closed state.
- **Lists of issues are lists of links.** Whenever issues are listed, every entry is a link, including the ones in a table, a summary sentence or a count breakdown. A Linear issue with a synced GitHub twin shows the Linear link **first** and the GitHub link second (`[PLO-315](…) / [#32](…)`).
- **Linear URLs.** The workspace is `ploch`; an issue is `https://linear.app/ploch/issue/<ID>` (the title slug after the ID is optional). Use the `url` field the Linear MCP returns for issues and projects rather than guessing a project slug.

### Getting the repository URL right

Derive it rather than assuming, because this workspace has many repositories:

```bash
gh repo view --json url --jq .url          # current repo
git remote get-url origin                  # fallback
```

For a sibling repository, use `https://github.com/mrploch/<sibling>`. The organisation is `mrploch` for every repository in this workspace.

## When not to link

- **A file path** is not a link — `src/Spectre/CommandLine.Spectre/AppBuilder.cs:165` is already clickable in the terminal and must stay plain. Do not wrap file paths in markdown links.
- **An identifier with no URL** — an analyser rule id (`NU1506`, `SA1600`), a package id, a branch name, a git ref. Link the rule id only if citing its documentation page, in which case the link is to those docs.
- **Inside fenced code blocks**, where markdown does not render.
- **Machine-readable or literal output** — JSON, YAML, CSV, configuration values or any content another tool will parse, and text the user asked for verbatim. A URL there stays a plain URL; wrapping it in link syntax would change the value.

## Why

The user reads these responses as a working index and clicks through to the referenced artefacts. A bare `#53` forces them to construct the URL by hand, which is the whole cost this rule removes. It was asked for, applied inconsistently, and restated — treat it as settled.

## Relationship to other rules

- [`response-closing.md`](./response-closing.md) governs the closing status/next-steps block. This rule applies **inside** it: every reference in those bullets is linked.
- [`summaries.md`](./summaries.md) governs report documents written to `temp/`. Those are markdown and are opened in a viewer, so the same rule applies to them.
- [`traceability.md`](./traceability.md) and [`work-traceability.md`](./work-traceability.md) require issue/PR/Notion cross-links in the tracking artefacts themselves; this rule is about how they are surfaced to the user in conversation.
