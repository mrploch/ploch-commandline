---
name: review-pr-comments
description: Resolve every PR comment on the current branch end-to-end. Triage each thread by the seven-category model from `pr-checks-completion-gate.md`, consult Codex and Gemini in parallel for non-trivial cases, apply fixes, post evidence-backed replies, resolve threads, and re-poll until no unaddressed thread remains.
allowed-tools: Bash(git:*), Bash(gh:*), Read, Edit, Write, Glob, Grep, WebSearch, WebFetch, mcp__codex-cli__codex, mcp__codex-cli__review, mcp__gemini__gemini-analyze-code, mcp__gemini__gemini-brainstorm, mcp__gemini__gemini-query, mcp__github__pull_request_read
---

# Review & Resolve PR Comments

Autonomous, multi-model resolution of every open PR comment on the current branch's pull request. Triage → consult (Codex + Gemini for non-trivial threads, in parallel) → fix → reply with evidence → resolve → re-poll until clean.

## Why this exists

The workspace's authoritative completion gate is `.claude/rules/pr-checks-completion-gate.md`. Its **Conversations Must Be Addressed** section requires every thread to end with **us** as the latest contributor with the thread either resolved or carrying a justified reply. A vague reply (e.g. just "false positive") is **insufficient** — the rule demands specific evidence. This skill mechanises that workflow and adds independent second opinions from Codex and Gemini so each resolution is the best available, not the first one we thought of.

## Announce at start

> "I'm using the review-pr-comments skill to resolve all conversations on PR #\<number\>."

## The Process

```
1. Fetch context        ─► repo + PR + all open threads + thread metadata
2. Triage               ─► assign each thread one of 7 categories
3. Trivial fast-path    ─► obvious typos / formatting / stylebook → fix + reply, no consult
4. Multi-model consult  ─► non-trivial threads: Codex + Gemini in PARALLEL, per thread
5. Synthesise           ─► merge both opinions + own analysis → decided resolution
6. Apply                ─► code fix (one commit per logical group) OR justified reply
7. Push                 ─► single push for the batch
8. Wait for CI          ─► all checks green per pr-checks-completion-gate.md
9. Reply + resolve      ─► every thread gets a reply citing commit hash or evidence
10. Re-poll             ─► fetch again; if new threads or check runs → back to step 2
11. Gate                ─► all four conditions in pr-checks-completion-gate.md true
12. Report              ─► summary with thread-by-thread outcomes
```

---

## Phase 1: Fetch Context

```bash
BRANCH=$(git branch --show-current)
REPO_INFO=$(gh repo view --json owner,name --jq '"\(.owner.login) \(.name)"')
OWNER=$(echo "$REPO_INFO" | cut -d' ' -f1)
REPO=$(echo "$REPO_INFO" | cut -d' ' -f2)
PR_NUMBER=$(gh pr view --json number --jq '.number')
PR_HEAD_SHA=$(gh api repos/$OWNER/$REPO/pulls/$PR_NUMBER --jq '.head.sha')

# Inline review threads (the canonical source — includes resolution state)
gh api graphql -f query='
  query($owner:String!,$repo:String!,$pr:Int!){
    repository(owner:$owner,name:$repo){
      pullRequest(number:$pr){
        reviewThreads(first:100){
          nodes{
            id isResolved isOutdated path line diffSide originalLine
            comments(first:100){
              nodes{ id databaseId author{login} body createdAt url }
            }
          }
        }
      }
    }
  }' -F owner=$OWNER -F repo=$REPO -F pr=$PR_NUMBER > /tmp/threads.json

# PR-conversation comments (issue-style, not tied to a line)
gh api repos/$OWNER/$REPO/issues/$PR_NUMBER/comments --paginate > /tmp/issue_comments.json

# Reviews (approve / request-changes / comment summary bodies)
gh api repos/$OWNER/$REPO/pulls/$PR_NUMBER/reviews --paginate > /tmp/reviews.json
```

**In scope for triage:** every reviewThread where `isResolved=false AND isOutdated=false`, plus every issue-style comment whose latest activity is not by us, plus every review with `state IN (CHANGES_REQUESTED, COMMENTED)` whose body raises an open concern.

**Bot authors count.** CodeRabbit, Codacy, SonarCloud / SonarQube, Bito, codeant-ai, Coderabbitai, github-actions[bot], and any other automated reviewer are triaged with the same rigour as human reviewers.

---

## Phase 2: Triage — Seven Categories

For each in-scope thread, assign exactly one category. These are the categories from `pr-checks-completion-gate.md` § "Conversations Must Be Addressed":

| Category | Definition | Resolution path |
|---|---|---|
| `VALID_ISSUE` | The reviewer correctly identified a defect or regression in our code | Fix → push → reply citing commit hash + summary of fix → resolve |
| `SUGGESTION_ACCEPTED` | A non-defect improvement we agree with | Same as `VALID_ISSUE` |
| `FALSE_POSITIVE` | The reviewer (or bot) is wrong; the code is correct | Reply with **specific evidence**: file:line, test name, spec doc, runtime invariant, language-spec citation → resolve |
| `ALREADY_FIXED` | The concern was addressed by a later commit on this branch | Reply citing the commit hash (`git log -S "<term>" -- <path>`) → resolve |
| `SUGGESTION_DECLINED` | Valid feedback, but we are choosing not to do it (out of scope, trade-off, etc.) | Reply with the principled reason + (if it's deferrable) link to the follow-up issue → resolve |
| `QUESTION` | Reviewer asked us something | Reply with the answer → resolve |
| `OUT_OF_SCOPE` | Genuinely worth doing, but doesn't belong in this PR | **Open a follow-up GitHub issue** → reply linking the issue → resolve |

**Trivial fast-path:** if the thread is a single-token typo, a stylebook nit clearly correct on its face, or a missing `using`/import that takes <30 seconds to verify, mark it `VALID_ISSUE` or `SUGGESTION_ACCEPTED` and skip Phase 3.

**Everything else goes through Phase 3 consultation.**

---

## Phase 3: Multi-Model Consultation (non-trivial threads, parallel)

For each non-trivial thread, run Codex and Gemini **in parallel** (single message, two MCP tool calls). The two models have different priors — Codex is stronger on "is this correct given the surrounding code" and Gemini is stronger on "what alternative approaches exist". Running both prevents single-model bias.

### What to pass to each consultation

Both calls must receive the same context bundle so their answers are comparable:

1. The reviewer's full comment body (do not paraphrase — quote verbatim).
2. The author handle and whether they're a human or known bot (CodeRabbit/Codacy/etc.) — bots have specific biases worth knowing.
3. The file and line(s) in question.
4. The relevant code excerpt (read the file at the reported line ± 30 lines via `Read`).
5. Our **proposed category and resolution** (from Phase 2). Frame it as a hypothesis to be challenged, not a decided answer.
6. The diff for the change that triggered the comment: `git show HEAD -- <path>` or `gh pr diff`.
7. Repo-specific context: target framework, the relevant rule file from `.claude/rules/` if applicable (e.g. for a data-access concern, attach `data-access.md`).

### Codex consultation

```
mcp__codex-cli__codex
```

Prompt template:

```
Reviewing a PR comment on <owner>/<repo>#<pr-number>. Independent second opinion needed.

REVIEWER (<bot|human> <handle>):
"""
<verbatim comment body>
"""

LOCATION: <path>:<line>

CODE EXCERPT:
```<language>
<file content ±30 lines>
```

PROPOSED CATEGORY: <one of the seven>
PROPOSED RESOLUTION: <our hypothesis>

QUESTIONS:
1. Is the reviewer right? Be specific — cite the line(s), test, or spec.
2. If wrong, what is the single best on-thread reply we should post (≤6 lines, evidence-first)?
3. If right, what's the minimal correct fix? Highlight edge cases we'd miss with the naïve fix.
4. Anything in the surrounding code that this comment hints at but doesn't say outright?
```

For diff-level review (e.g. a SonarCloud quality-gate complaint about a block of code), use `mcp__codex-cli__review` instead and pass `git diff <base>...HEAD -- <path>`.

### Gemini consultation

```
mcp__gemini__gemini-analyze-code   ← when the thread is about code correctness/quality
mcp__gemini__gemini-brainstorm     ← when the thread is about design/approach with several valid options
mcp__gemini__gemini-query          ← fallback for non-code threads (docs, naming, process)
```

Use the **same prompt body** as the Codex call, but rephrase the final question block to lean on Gemini's strengths:

```
QUESTIONS:
1. What alternatives to <proposed resolution> exist? Rank them by fit for this codebase.
2. Where is the reviewer's reasoning weakest? Where is it strongest?
3. If we kept the code as-is, what specific evidence would persuade the reviewer?
4. Are there *other* spots in this PR likely to attract the same comment (so we can pre-empt them)?
```

### Parallel call pattern

Send both calls in a **single assistant message** with two tool uses. Do not serialise — that doubles the wall-clock cost. Example shape:

```
[same message]
  ├── mcp__codex-cli__codex(prompt=<codex prompt>)
  └── mcp__gemini__gemini-analyze-code(code=<excerpt>, prompt=<gemini prompt>)
```

---

## Phase 4: Synthesise

For each consulted thread, compare the two responses against our Phase-2 hypothesis. There are four outcomes:

| Outcome | Action |
|---|---|
| Both models agree with our hypothesis | Proceed with the planned resolution; the synthesised reply may quote the concurring rationale. |
| Both disagree (and agree with each other) | **Change category and/or resolution.** Two independent models flagging the same gap is a strong signal we missed something. |
| Models disagree with each other | Read both rationales, decide on merit, document the reasoning in the on-thread reply. Do **not** simply pick the one that matches our prior. |
| One or both raise a **new** issue not in the original comment | Address it: either expand the fix, or open a follow-up `OUT_OF_SCOPE` issue and mention it in the reply. |

Capture per-thread synthesis notes in `notes/<repo>/<date>-pr-<number>-comments.md` as you go — these become the audit trail for the resolution.

---

## Phase 5: Apply Resolutions

Group threads by file/feature and apply changes in **batches**, not one-thread-per-commit. Conventional Commit format per `rules/commits.md`:

```
fix(<scope>): Address PR #<pr-number> review feedback

- <thread 1 summary>
- <thread 2 summary>
- ...

Refs: #<linked-issue-number>
```

`OUT_OF_SCOPE` threads: open the follow-up issue **before** posting the reply so you have a URL to cite.

```bash
gh issue create --repo $OWNER/$REPO --title "<concise title>" --body "Follow-up from PR #$PR_NUMBER review.

<context + link back to original thread>"
```

---

## Phase 6: Push + Wait for CI

```bash
git push
gh pr checks $PR_NUMBER --watch
```

Wait until every check shows `pass`. Stale or pending checks block reply-posting — a reply that cites a commit hash before CI verifies that commit is premature. See `.claude/rules/pr-checks-completion-gate.md` § "Static-Analysis Bots Must Pass".

---

## Phase 7: Reply + Resolve

For each thread, post an evidence-backed reply, then resolve.

### Reply quality bar

Every reply must be specific enough to persuade an independent reviewer reading the thread cold. The bar from `pr-checks-completion-gate.md`:

> A "false positive" reply is not just the words "false positive". A useful reply:
> 1. States the classification up front.
> 2. Cites concrete behaviour — file + line, a specific test, a spec doc, a runtime invariant, or the language spec.
> 3. Explains why the analyser or reviewer's mental model diverges from the code's actual behaviour.
> 4. If it would only persuade via trust, it is insufficient.

Templates:

```markdown
# VALID_ISSUE / SUGGESTION_ACCEPTED
Fixed in <commit-sha>. <one sentence on what changed and why this resolves it>.
<optional second line citing the test that locks the fix in>

# FALSE_POSITIVE
False positive. <path>:<line> shows that <specific observed behaviour>. The relevant
test <Namespace.ClassName.TestName> covers this exact case and passes. The analyser's
mental model assumes <X>, but <Y> applies here because <reason>. Marking resolved.

# ALREADY_FIXED
Already addressed in <commit-sha> ("<commit-subject>"). The fix <brief description>.
Marking resolved.

# SUGGESTION_DECLINED
Acknowledged, but declining for this PR. <principled reason — scope, trade-off,
or constraint>. <if deferrable: tracked in #<follow-up-issue>.>

# QUESTION
<direct answer>. <pointer to file/test/doc that backs the answer>.

# OUT_OF_SCOPE
Worth doing, but out of scope for this PR. Tracked in #<follow-up-issue>. Marking
resolved here.
```

### Posting & resolving

```bash
# Reply to a review thread (use the thread's first comment id)
gh api repos/$OWNER/$REPO/pulls/$PR_NUMBER/comments/$COMMENT_ID/replies -f body="$REPLY"

# Reply to an issue-style PR comment
gh api repos/$OWNER/$REPO/issues/$PR_NUMBER/comments -f body="$REPLY"

# Resolve the review thread (GraphQL — only review threads have resolvable state)
gh api graphql -f query='mutation($id:ID!){
  resolveReviewThread(input:{threadId:$id}){thread{isResolved}}
}' -F id=$THREAD_NODE_ID
```

A thread is **never** resolved without first posting a reply, unless we authored the thread ourselves and no other participants have commented.

---

## Phase 8: Re-poll

After the last reply/resolve in a round:

1. Re-run the Phase 1 fetch.
2. Check for **new** threads, **new** issue-style comments, and **new** check runs since the last poll.
3. If any new work appeared → loop back to Phase 2 for the delta.
4. If two consecutive polls (≥60s apart) return identical state → proceed to Phase 9.

---

## Phase 9: Completion Gate

Reproduce the verification sequence from `.claude/rules/pr-checks-completion-gate.md` § "The Pre-Completion Verification" and confirm **all four** gate conditions on the latest pushed commit:

1. Every CI check shows `pass`.
2. Every static-analysis bot has rendered a `success` verdict (stale ≠ pass).
3. Every review thread is resolved or has us as the latest contributor with a justified reply.
4. Re-polling produces no new work.

**Forbidden completion framings** (these are the historical failure modes the workspace rule names explicitly):

- ❌ "Done — Codacy is stale, expected to be green on rescan"
- ❌ "Done — Bito hasn't run yet"
- ❌ "Done — only non-required checks failing"
- ❌ "Done — addressed the most important PR comments"
- ❌ "Done with caveat: external bot dependency"

If you find yourself wanting to write any of these, you are **continuing**, not done. Use `ScheduleWakeup` (~270s, within the prompt-cache window) to wait for a rescan rather than declaring completion.

---

## Phase 10: Report

```markdown
## PR #<number> Comments Resolved — <PR title>

### Outcome by thread
| # | Author | Location | Category | Resolution |
|---|--------|----------|----------|------------|
| 1 | @<author> | `<path>:<line>` | VALID_ISSUE | Fixed in `<sha>` |
| 2 | @coderabbitai | `<path>:<line>` | FALSE_POSITIVE | Replied with evidence (test `<name>` covers it) |
| 3 | @sonarcloud | `<path>:<line>` | ALREADY_FIXED | Cited `<sha>` |
| 4 | @<author> | conversation | OUT_OF_SCOPE | Tracked in #<follow-up-issue> |

### Consultation summary
- Codex consulted on <n> threads, Gemini on <n>. Both agreed on <n>; disagreement resolved with documented reasoning on <n>.

### Gate status
- All <n> CI checks green on commit `<sha>`
- All <n> threads addressed; all <n> resolved
- Two consecutive polls clean

### Follow-ups
- Issue #<follow-up-issue>: <title>
```

---

## When To Pause and Confirm

The skill is autonomous by default — but pause and ask the user before:

- Adding a new exclusion to `.codacy.yml`, `sonar-project.properties`, `.editorconfig` analyser severity, or `GlobalSuppressions.cs`. State which findings would be silenced and why exclusion is the right call.
- Declining a `SUGGESTION_DECLINED` that has user-impact implications.
- Deferring a `VALID_ISSUE` to a follow-up when the user might prefer it fixed here.
- Touching files outside the PR's stated scope.
- Closing a thread the user themselves opened.

Otherwise, **do not pause** — re-polling, retries, fix-push cycles, and gate verification are part of the autonomous loop.

---

## Anti-Patterns — Never

- ❌ Resolving a thread by clicking "Resolve" without posting a reply (unless we authored it and there are no other participants).
- ❌ Reply consisting only of "false positive", "fixed", or "thanks".
- ❌ Skipping the Codex+Gemini consultation on a thread because "I'm sure".
- ❌ Treating bot-authored threads as optional.
- ❌ Citing a commit hash in a reply before that commit's CI checks have gone green.
- ❌ Declaring completion with stale or pending checks.
- ❌ `git add -A` for the resolution commit — stage specific files.
- ❌ Amending a previous commit to address review feedback — always a new commit.
- ❌ Running Codex and Gemini **sequentially** — they go in the same assistant message.

---

## Integration

**Hard-references (must read on each run):**
- `.claude/rules/pr-checks-completion-gate.md` — the four-condition gate, seven-category triage, reply quality bar, forbidden framings.
- `.claude/rules/commits.md` — Conventional Commit format and `Refs: #<issue>` requirement.
- `.claude/rules/notes-keeping.md` — log thread-by-thread synthesis to `notes/<repo>/<date>-pr-<number>-comments.md`.

**Skills:**
- `superpowers:verification-before-completion` — required before claiming the gate is satisfied.
- `commit` — Phase 5 commits.
- `pr` — only if the PR needs body updates (e.g. follow-up issue links).

**MCP tools:**
- `mcp__codex-cli__codex`, `mcp__codex-cli__review` — independent code review.
- `mcp__gemini__gemini-analyze-code`, `mcp__gemini__gemini-brainstorm`, `mcp__gemini__gemini-query` — alternative-perspective consultation.
- `mcp__github__pull_request_read` — structured PR data when GraphQL is overkill.
- `gh` CLI — fetching threads, posting replies, resolving threads, monitoring checks.
