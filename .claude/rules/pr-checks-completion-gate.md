# PR Checks & Conversations — Hard Completion Gate

**Scope:** Any task that pushes commits to a branch with an open or about-to-be-opened pull request. Applies to all skills (`/implement-issue`, `/dotnet-dev-finishing-touches`, `/pr`, `/commit` followed by push, ad-hoc bug fixes that get pushed) and to ad-hoc work as well.

**Authority:** This rule is **non-negotiable**. It overrides the natural urge to declare completion early, the temptation to defer "stale" checks, and any partial-credit framing.

This rule exists because every prior occurrence of "I'll consider it done with a caveat" cost the user time having to reopen the task. The cost of waiting one more polling cycle is far lower than the cost of a premature completion claim.

## The Rule

Work is **not complete** until **all four** of the following are simultaneously true on the most recently pushed commit:

1. **Every CI check has reported a terminal success status.** No `failure`, no `cancelled`, no `timed_out`, no `action_required`, no `pending`, no `queued`, no `in_progress`, no `neutral`, no `skipped` (unless the check is intentionally and explicitly conditional and the conditional path was taken — and you can prove that). "Required" vs "not required" is irrelevant. Every check listed by `gh pr checks <PR#>` must be `pass`.
2. **Every static-analysis bot has rendered its verdict and that verdict is "no new issues".** See [Static-Analysis Bots](#static-analysis-bots-must-pass) below for the canonical list. A bot that has not yet posted its check is **not** the same as a passing bot — wait for it. For **SonarCloud / SonarQube Cloud**, "no new issues" must be confirmed by querying the SonarCloud platform via the `sonarqube-cloud` MCP server — the green GitHub check is **not** sufficient evidence, because a quality gate can pass while new issues sit below its threshold.
3. **Every PR review thread is either resolved or has an on-thread reply from us with the latest activity being ours.** No reviewer-authored comment may be the most recent activity on an open thread. See [Conversations Must Be Addressed](#conversations-must-be-addressed) below.
4. **Re-polling produces no new work.** Issue comments, review comments, review threads, and CI checks are re-fetched after the last commit settles, and the result is a no-op (no new threads, no new comments, no new check runs that haven't been satisfied).

If any of the four is false, the task **continues** — it is not "done with a caveat", "done pending Codacy", "done aside from a stale check", or "done — waiting for rescan". It is **continuing**, and the next action is to advance the task toward the gate, not to write a completion report.

## Static-Analysis Bots Must Pass

The following are the bots routinely seen in this workspace's PRs. The list is illustrative, not exhaustive — any check that appears in `gh pr checks` and any review/comment posted by an automated reviewer is in scope:

| Bot / check | Typical surface | What "pass" means |
|---|---|---|
| **Codacy Static Code Analysis** | GitHub status check + per-line annotations + PR comment | `conclusion = success`, summary "Your pull request is up to standards!", zero new issues |
| **SonarCloud / SonarQube Cloud** | GitHub status check + a single summary PR comment + **issues that live only in the SonarCloud platform** | Quality gate passes **and** the `sonarqube-cloud` MCP server reports zero `OPEN`/`CONFIRMED` issues and zero `TO_REVIEW` security hotspots for the PR. The GitHub check passing alone is **not** proof — see the note below the table. |
| **CodeQL** | GitHub Actions check (`Analyze (csharp)` etc.) | All matrix jobs `pass`, zero new alerts |
| **CodeRabbit** | PR review with inline comments | Review status `pass` or `Review completed` with all actionable items handled |
| **Bito AI Code Review Agent** | GitHub status check + PR comment + review | Status `pass` (`skipping` is also acceptable when explicitly skipped by config) |
| **Codacy Coverage / Coveralls / Codecov** | Status check + PR comment | Coverage gate satisfied; if a percentage is reported it is non-decreasing on changed lines |
| **`build`, `Test Results`, `Analyze (csharp)`** (GitHub Actions) | Status check | All `pass`, no warnings introduced |
| **Repository-specific custom checks** | Status check | All `pass` |

**SonarCloud findings are not all on GitHub.** Unlike Codacy or CodeRabbit, SonarCloud usually posts only a single summary PR comment — not one thread per finding. The individual bugs, code smells, vulnerabilities, and security hotspots live in the SonarCloud platform and **must** be fetched via the `sonarqube-cloud` MCP server (configured at workspace scope — see `mrploch/CLAUDE.md` § "SonarQube MCP Servers"):

- **Project key:** `.sonarlint/connectedMode.json` → `projectKey`; else `sonar.projectKey` in `sonar-project.properties` or `.github/workflows/*.yml`; else `mcp__sonarqube-cloud__search_my_sonarqube_projects(q="<repo>")`.
- **Issues:** `mcp__sonarqube-cloud__search_sonar_issues_in_projects(projects=["<key>"], pullRequestId="<PR#>", issueStatuses=["OPEN","CONFIRMED"])`.
- **Security hotspots:** `mcp__sonarqube-cloud__search_security_hotspots(projectKey="<key>", pullRequest="<PR#>", status=["TO_REVIEW"])`.
- **Quality gate:** `mcp__sonarqube-cloud__get_project_quality_gate_status(projectKey="<key>", pullRequest="<PR#>")`.

Each returned issue and hotspot is triaged with the **same seven-category model** as a review thread (see [Conversations Must Be Addressed](#conversations-must-be-addressed)). Resolve `VALID_ISSUE`/`SUGGESTION_ACCEPTED` by fixing the code (the next scan auto-marks them `FIXED`). A `FALSE_POSITIVE`, an accepted/won't-fix issue, or a hotspot reviewed as safe is a status change on the platform (`change_sonar_issue_status` / `change_security_hotspot_status`) — and, like adding an analyser exclusion, requires user confirmation first.

For each bot whose verdict is "fail" or "action_required" or "no verdict yet because the bot hasn't run":

- **Fail** → fetch the detailed output (`gh api repos/<owner>/<repo>/commits/<sha>/check-runs` for status checks; `gh api repos/<owner>/<repo>/check-runs/<id>/annotations` for per-line annotations; `gh pr view <PR#> --json comments,reviews` for PR-comment-based feedback). Resolve every individual finding with either a code fix or — for false positives — a justified exclusion documented in the relevant config (`.codacy.yml`, `sonar-project.properties`, `.codeql/`, etc.) with a comment explaining why and a link to the affected PR/issue.
- **Action_required** → treat as fail.
- **Pending / queued / in_progress** → wait. Use a polling cycle (e.g. `ScheduleWakeup` ~270s to stay inside the 5-min cache window). Do not declare completion.
- **Not started** → wait. Some bots (Codacy, SonarCloud) take 1–5 min to start after a push.

**Stale checks are still failures.** A check that says `fail` because the bot hasn't yet rescanned the latest commit is **still failing** by the gate's definition. Either wait for the rescan (preferred) or push a tiny no-op-ish commit to retrigger (last resort, only after waiting >10 min). Reporting "Codacy is stale, ignore it" is **not allowed**.

## Conversations Must Be Addressed

Every PR review thread (`reviewThreads` in the GraphQL schema) and every issue-style PR comment must end with **us** as the latest contributor and be either resolved or have an active reply that justifies leaving it open.

For each thread, classify it into one of seven categories and follow the resolution path:

| Category | Resolution path |
|---|---|
| `VALID_ISSUE` | Fix the code → push → wait for CI → reply on thread citing commit hash + what changed → resolve thread |
| `SUGGESTION_ACCEPTED` | Same as `VALID_ISSUE` |
| `FALSE_POSITIVE` | Reply with **specific evidence** — point at file/line, cite the test or spec that proves the code is correct, explain why the bot or reviewer's mental model diverges from the code → resolve thread |
| `ALREADY_FIXED` | Reply with the commit hash that fixed it (use `git log -S "<term>" -- <file>` to find it) → resolve thread |
| `SUGGESTION_DECLINED` | Reply with the principled reason (out of scope + follow-up issue link, trade-off explanation, etc.) → resolve thread |
| `QUESTION` | Reply with the answer → resolve thread |
| `OUT_OF_SCOPE` | Open a follow-up GitHub issue → reply linking the issue → resolve thread. Always file the issue; never just say "I'll track it later" |

**A thread is never closed without a reply.** "Resolve with no response" is only acceptable when the thread was authored by us, has no other participants, and was made obsolete by our own subsequent commit on the same branch.

**A "false positive" reply is not just the words "false positive".** A useful reply:

1. States the classification up front ("I believe this is a false positive because…").
2. Cites concrete behaviour — file + line, a specific test that covers the case, a spec doc, a runtime invariant, or the language spec itself.
3. Explains why the analyser or reviewer's mental model diverges from the code's actual behaviour.
4. If it would only persuade via trust, it is insufficient — add evidence.

For bot-flagged false positives (SonarCloud, Codacy, codeant-ai, CodeRabbit): the same bar applies. "The bot is wrong" is never enough on its own.

**For bot-driven feedback:** when the same finding is repeated across many threads (e.g. Codacy posting a comment for every line of a markdown file), batch the replies into one body of text but post and resolve every thread individually. Do **not** silently ignore duplicates.

## The Pre-Completion Verification (Run This Before Reporting)

Immediately before writing any completion message, execute this sequence and verify each output. If any step is non-empty (where empty is the success state), the gate fails and the task continues.

```bash
# 1. All checks pass on the latest commit?
gh pr checks <PR#> --repo <owner>/<repo>
# Expected: every line ends with "pass". No "fail", "pending", "queued", "in_progress".

# 2. Latest commit on the PR head is what we expect?
gh api repos/<owner>/<repo>/pulls/<PR#> --jq '.head.sha'
# Cross-check against `git rev-parse HEAD`.

# 3. Codacy / SonarCloud / CodeRabbit / Bito have actually rendered a verdict
#    (not just absent from the list)?
gh api repos/<owner>/<repo>/commits/<sha>/check-runs --jq '.check_runs[] | {name, conclusion, status}'
# Expected: each named bot appears with conclusion in {success}.
# A bot that doesn't appear at all is "not yet run" — wait for it.

# 4. Zero unresolved & non-outdated review threads?
gh api graphql -f query='query($o:String!,$r:String!,$pr:Int!){repository(owner:$o,name:$r){pullRequest(number:$pr){reviewThreads(first:100){nodes{id isResolved isOutdated comments(first:1){nodes{author{login} body}}}}}}}' \
  -F o=<owner> -F r=<repo> -F pr=<PR#> \
  | jq '.data.repository.pullRequest.reviewThreads.nodes
        | map(select(.isResolved==false and .isOutdated==false))
        | length'
# Expected: 0.

# 5. No new PR-level comments since our last poll?
gh api repos/<owner>/<repo>/issues/<PR#>/comments --paginate --jq '.[].id' | tail -5
# Expected: same as your previous poll. If new IDs appear, address them.

# 6. CI annotations from each bot are clear?
for run_id in $(gh api repos/<owner>/<repo>/commits/<sha>/check-runs --jq '.check_runs[] | .id'); do
  gh api repos/<owner>/<repo>/check-runs/$run_id/annotations
done
# Expected: each annotation is either resolved by code change or has a documented exclusion in config.
```

**Step 7 — SonarCloud platform is clean (MCP, not shell).** The bash steps above only see GitHub-surfaced data. Separately confirm via the `sonarqube-cloud` MCP server that the PR has zero open findings:

- `mcp__sonarqube-cloud__search_sonar_issues_in_projects(projects=["<key>"], pullRequestId="<PR#>", issueStatuses=["OPEN","CONFIRMED"])` → expected: empty.
- `mcp__sonarqube-cloud__search_security_hotspots(projectKey="<key>", pullRequest="<PR#>", status=["TO_REVIEW"])` → expected: empty.
- `mcp__sonarqube-cloud__get_project_quality_gate_status(projectKey="<key>", pullRequest="<PR#>")` → expected: `OK`.

Any non-empty result means the gate fails and the task continues — regardless of what the GitHub `SonarQube Cloud` check says.

If you cannot tick every box without caveats, **do not write a completion report**. Continue the loop. Use a polling wakeup if you're waiting on a bot's rescan; do not idle.

## Anti-Patterns — Never Do These

- ❌ "Done — Codacy will rescan shortly, expected to be green" → **wait for the rescan**
- ❌ "Done — Bito is stale, ignore it" → **wait for it or address the issue**
- ❌ "Done with caveat: external bot dependency" → **the bot is part of the gate, no caveats**
- ❌ "Done — only non-required checks failing" → **non-required checks count**
- ❌ "Done — addressed the most important PR comments" → **all comments must be addressed, not just the important ones**
- ❌ "Done — SonarCloud quality gate passed" without querying the platform → **a passing gate is not zero issues; enumerate platform findings via the `sonarqube-cloud` MCP server**
- ❌ Replying to SonarCloud's summary PR comment without resolving the individual issues behind it → **the summary comment is addressed only when every platform finding is fixed or has a confirmed status change**
- ❌ Resolving a thread by clicking "Resolve" without posting a reply → **always reply first**
- ❌ Treating CodeRabbit / Bito / Codacy comments as optional because "they're just bots" → **bot feedback follows the same triage rules as human reviewer feedback**
- ❌ Pushing a fix and immediately writing a completion report without re-running the verification sequence → **always re-verify on the new HEAD**
- ❌ Excluding a path from a static analyser to silence findings without justification → **only exclude paths whose findings are genuinely out of scope (mirror dirs, generated files, scratch docs); never exclude to hide a real issue**

## When To Pause and Confirm With the User

The gate is non-negotiable, but specific resolution choices are not. Pause and confirm before:

- Adding a new exclusion to `.codacy.yml`, `sonar-project.properties`, or similar — or marking a SonarCloud issue `falsepositive`/`accept`, or a security hotspot `SAFE`/`ACKNOWLEDGED`, via the `sonarqube-cloud` MCP server (state which findings would be silenced and why that is the right call).
- Declining a `SUGGESTION_DECLINED` reviewer suggestion that has user-impact implications.
- Deferring a `VALID_ISSUE` finding to a follow-up issue when the user might prefer it fixed in this PR.
- Pushing a fix that touches files outside the PR's stated scope.

Otherwise, the loop runs autonomously. The user should not have to ask "is Codacy still failing?" — if it is, you are still working on it.

## How To Reference This Rule

Skills should reference this rule by linking to it relatively (e.g. `[See: ../../../.claude/rules/pr-checks-completion-gate.md]`) and reproducing the **The Rule** section verbatim near the skill's completion gate. Do not paraphrase the four conditions — the wording is precise on purpose.
