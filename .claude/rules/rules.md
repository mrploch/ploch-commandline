# Documentation and Rules System

## Three-Tier Documentation System

**Tier 1: README.md** – Onboarding, quick start, basic usage (max 150 lines for packages). Copy-pasteable examples. Cross-reference, don't duplicate. Acts as an index for all spec files (list in a Documentation section). Repo/package level only; no module-level READMEs. Modules are covered by spec files where necessary.

**Tier 2: .cursor/rules/\*.mdc** – Engineering standards and workflows. How to write code, use frameworks, configure tools, and set up the environment. Concise, actionable instructions only.

**Tier 3: \*.spec.md** – Business logic, compliance, feature requirements. Explains "why" and "what", not "how". No test scenarios. Must link back to the repo/package README.

**No overlap:** Cross-reference between tiers, never duplicate.

## README Structure

READMEs should include these sections as applicable:

1. **Title** — Package/repo name
2. **Quick Start** — Getting started quickly
3. **Documentation** — List of spec files (`*.spec.md`) with brief descriptions (under a 'Specs' sub-section), along with any other related documentation in separate sub-sections as needed
4. **Development** — Prerequisites, setup, and contribution guidelines
5. **Configuration** — Configuration options (if applicable)

## Rule File Structure

`.cursor/rules` is the source of truth for AI rules.

**Generic rules:** `name.mdc` (no underscore). Universal, repo-agnostic. Specific to a language, framework, tool, platform, etc.

**Repo-specific rules:** `_project.mdc` (required) and, for repos with more than one package, `_packageName.mdc`. Repo or package specific paths, commands, utilities.

**Globs vs AI interpretation:** Use globs for strict file patterns. Without globs (recommended), AI interprets context for better accuracy.

**Guidelines:** Single responsibility per file. Actionable only. Prefer tooling (ESLint, Prettier) over AI rules. If a rule can be enforced by a linter or formatter, it belongs in that tool's config, not here. AI agents should read and respect linter and formatter output.

## MDC File Formatting

- **Frontmatter:** `description` is required; AI uses semantic matching to decide relevance. Optionally add ONE of: `alwaysApply: true` (forces load for every request) OR `globs` (strict file pattern enforcement). Omit both to rely on intelligent description-based pickup.
- One `#` title per file; `##` for sections.
- Rules as `-` bullet points; one concept per bullet.
- Use `**bold**` for emphasis; backticks for `code`, `filenames`, and `commands`.

## Architectural Decisions Hierarchy

- **Spec files:** Major architectural decisions with business impact.
- **`_project.mdc`, `_packageName.mdc`:** Smaller architectural and project-level decisions.
- **Framework rules:** Usage patterns for chosen tools.

## Spec Creation Guidelines

Write spec files for complex architectural decisions: auth, API clients, state management, compliance-heavy workflows.

Skip specs for styling, simple UI, config, and dev tooling.

## Sync Process

Run the following after any rule changes:

- **node:** `pnpm exec ai-rules install` (or `npx @EqualsGroup/ai-rules install`)
- **.NET:** `dotnet ai-rules install`
