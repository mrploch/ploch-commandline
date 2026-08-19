# Agent Behaviour Specification

## Pre-Code Workflow

Before analysing, investigating, or modifying any code:

1. Fetch relevant rules (repo/package + patterns). In Cursor, use `fetch_rules` tool.
2. Read the README and locate linked spec files and relevant documentation.
3. Review all relevant specs and docs.
4. Create a complete TODO list that includes:
   - Implementation tasks
   - Automated testing (unit, visual regression, e2e as appropriate)
   - Manual verification step (e.g. "Manually verify changes in browser", "Test CLI command", "send request via curl")
   - Updating any snapshots if they exist (e.g. visual regressions will have baseline images)

## When to Stop and Confirm

Stop and ask the user before implementing changes that may violate or need more information to stay compliant around:

- **Legal or regulatory rules:** SCA, PCI-DSS, GDPR.
- **Security:** Authentication, session handling, encryption, sensitive data.
- **Business logic:** Permissions, account access, financial limits, payment flows.
- **Data access:** Queries that could expose PII or sensitive data.
- **Specification conflicts:** When the request conflicts with linked spec files.

If unsure whether a change falls into these categories, stop and ask.

## Post-Code Workflow

After implementing changes, **before reporting completion**, you MUST complete BOTH:

1. **Automated testing** — Run relevant tests (unit, integration, visual, e2e). Check project-specific rules, `package.json` scripts, or infer from context. Code compilation alone is insufficient. When working with visual regressions, make sure to update snapshots after you're happy with your changes.
2. **Manual verification** — Verify like a developer or user would. e.g. For web code, use browser MCP tools to navigate to the app, sign in if needed, and visually confirm the change works. For CLI tools, run commands. For APIs, send requests.

**CRITICAL**: Never report completion until BOTH automated AND manual verification pass. If either cannot be performed:

- Explicitly state which verification is blocked and why
- Ask the user how to proceed
- Do NOT mark tasks as complete — leave them as "pending verification"

## Pull Requests

- **Complete testing before creating PR:** Finish ALL automated and manual verification BEFORE creating a pull request. A PR signals the work is ready for review.
- **PR body must follow template:** When creating a PR, read `.github/pull_request_template.md` first (if it exists) and structure the body accordingly. Include ticket links, remove inapplicable sections (e.g. incident links for non-incidents), and add developer testing notes.
- **Never create a placeholder PR:** Only create a PR when implementation and all verification steps are complete.

### CI Check Gate (Mandatory)

**Authoritative reference:** [`pr-checks-completion-gate.md`](./pr-checks-completion-gate.md) — the four-condition gate, the canonical bot list (Codacy, SonarCloud / SonarQube, CodeQL, CodeRabbit, Bito, coverage bots, repository-specific checks), the seven-category triage for PR threads, and the pre-completion verification sequence are all defined there. **Read that rule and apply it.** The summary below is operational shorthand.

After pushing changes or creating/updating a PR, you **must** monitor CI checks and resolve any failures before considering the work complete:

1. **Observe checks:** After pushing, use `gh pr checks <PR-number> --watch` (or `gh run list` / `gh run view`) to monitor the status of all CI checks (build, test, Codacy, SonarCloud, CodeQL, CodeRabbit, Bito, etc.). Required vs not-required is irrelevant — every check counts.
2. **On failure — investigate:** If any check fails, retrieve the logs (`gh run view <run-id> --log-failed` for Actions; `gh api repos/<owner>/<repo>/check-runs/<id>/annotations` for line-level annotations from Codacy / SonarCloud / etc.) to identify the root cause. Do not guess — read the actual failure output.
3. **Fix and push:** Make the necessary code changes to resolve the failure, commit with an appropriate conventional commit message, and push again.
4. **Re-observe:** After pushing the fix, monitor the checks again. Repeat the investigate-fix-push cycle until **all checks pass**. If a bot is slow to rescan, wait — use a polling wakeup (~270s) rather than declaring completion with a "stale" caveat.
5. **PR comments:** After checks pass, address every PR conversation per the seven-category triage in `pr-checks-completion-gate.md`. Bot-authored threads (CodeRabbit, Codacy, Bito, SonarCloud) follow the same rules as human reviewer threads — every thread gets either a code fix + reply + resolve, or an evidence-backed reply + resolve.
6. **Only then declare complete:** Work is not done until all four gate conditions in `pr-checks-completion-gate.md` are simultaneously true on the latest pushed commit.

**Do not:**
- Ignore or dismiss failing checks (including non-required ones).
- Mark work as complete while checks are still running, failing, queued, in_progress, or stale.
- Assume a failure is "flaky" without evidence — investigate first.
- Push multiple speculative fixes without reading the failure logs.
- Report "done with caveat: Codacy is stale" or any equivalent framing — wait for the rescan instead.
- Resolve a thread without an on-thread reply (unless we authored it and it has no other participants).
- Treat bot feedback as optional because "it's just a bot".

## Standards

- Use British English.
- Run commands yourself.
- Clean up after modifications.
- Use browser MCPs if available when testing web code.
- **Never amend commits** unless the user explicitly asks. Always create new commits.
